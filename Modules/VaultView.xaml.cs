using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NebulaLauncher.Modules
{
    public class ModrinthItem : INotifyPropertyChanged
    {
        public string ProjectId { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Author { get; set; } = "";
        public string IconUrl { get; set; } = "";
        public string DownloadsText { get; set; } = "";

        private bool _isInstalled;
        public bool IsInstalled 
        { 
            get => _isInstalled; 
            set { _isInstalled = value; OnPropertyChanged(); OnPropertyChanged(nameof(ShowButton)); OnPropertyChanged(nameof(ButtonLabel)); OnPropertyChanged(nameof(ButtonBrush)); } 
        }

        private bool _isDownloading;
        public bool IsDownloading 
        { 
            get => _isDownloading; 
            set { _isDownloading = value; OnPropertyChanged(); OnPropertyChanged(nameof(ShowButton)); } 
        }

        public bool ShowButton => !IsDownloading;
        public string ButtonLabel => IsInstalled ? "✓ INSTALADO" : "📥 INSTALAR";

        public Brush ButtonBrush 
        {
            get 
            {
                var brush = IsInstalled ? new SolidColorBrush(Color.FromRgb(16, 185, 129)) : new SolidColorBrush(Color.FromRgb(124, 58, 237));
                brush.Freeze();
                return brush;
            }
        }

        private double _downloadProgress;
        public double DownloadProgress 
        { 
            get => _downloadProgress; 
            set { _downloadProgress = value; OnPropertyChanged(); } 
        }

        private string _progressText = "0%";
        public string ProgressText 
        { 
            get => _progressText; 
            set { _progressText = value; OnPropertyChanged(); } 
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class LocalFileItem
    {
        public string Name { get; set; } = "";
        public string SizeText { get; set; } = "";
        public string FilePath { get; set; } = "";
    }

    public class NebulaManifest
    {
        public Dictionary<string, string> InstalledMods { get; set; } = new Dictionary<string, string>();
    }

    public partial class VaultView : UserControl
    {
        private readonly string _gameFolder;
        private readonly MinecraftProfile? _profile;
        private static readonly HttpClient _http = new HttpClient() { Timeout = TimeSpan.FromSeconds(20) };
        private string _currentType = "mod";
        private bool _showInstalled = false;
        private readonly ObservableCollection<ModrinthItem> _results = new ObservableCollection<ModrinthItem>();
        private bool _isSearching = false;

        private int _currentPage = 1;
        private int _pageSize = 24;
        private NebulaManifest _manifest = new NebulaManifest();

        public VaultView(string gameFolder, MinecraftProfile? profile = null)
        {
            try
            {
                InitializeComponent();
                _gameFolder = gameFolder;
                _profile = profile;
                ResultsList.ItemsSource = _results;

                if (!_http.DefaultRequestHeaders.Contains("User-Agent"))
                    _http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) NebulaLauncher/2.0");

                LoadManifest();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error crítico en Nebula Mod Hub: " + ex.Message);
            }
        }

        private void VaultView_Loaded(object sender, RoutedEventArgs e)
        {
            _ = SearchModrinth("", true);
        }

        private void FilterType_Changed(object sender, RoutedEventArgs e)
        {
            if (FilterMods == null || _isSearching) return;
            if (FilterMods.IsChecked == true) _currentType = "mod";
            else if (FilterShaders.IsChecked == true) _currentType = "shader";
            else if (FilterResourcePacks.IsChecked == true) _currentType = "resourcepack";

            _showInstalled = false;
            if (FilterInstalled != null) FilterInstalled.IsChecked = false;
            if (this.IsLoaded) _ = SearchModrinth(SearchBox?.Text ?? "", true);
        }

        private void FilterInstalled_Changed(object sender, RoutedEventArgs e)
        {
            _showInstalled = FilterInstalled?.IsChecked == true;
            if (_showInstalled) ShowLocalFiles();
            else
            {
                LocalScroll.Visibility = Visibility.Collapsed;
                ResultsScroll.Visibility = Visibility.Visible;
                SetLoading(false);
            }
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) _ = SearchModrinth(SearchBox.Text ?? "", true);
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e) => _ = SearchModrinth(SearchBox?.Text ?? "", true);

        private void SortCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.IsLoaded && !_isSearching) _ = SearchModrinth(SearchBox?.Text ?? "", true);
        }

        private void CategoryCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.IsLoaded && !_isSearching) _ = SearchModrinth(SearchBox?.Text ?? "", true);
        }

        private void BtnPrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1) {
                _currentPage--;
                _ = SearchModrinth(SearchBox?.Text ?? "", false);
            }
        }

        private void BtnNextPage_Click(object sender, RoutedEventArgs e)
        {
            _currentPage++;
            _ = SearchModrinth(SearchBox?.Text ?? "", false);
        }

        private async Task SearchModrinth(string query, bool resetPage)
        {
            if (_isSearching) return;
            _isSearching = true;

            if (resetPage) _currentPage = 1;
            TxtPage.Text = $"Página {_currentPage}";

            SetLoading(true);
            try
            {
                LoadManifest();

                int offset = (_currentPage - 1) * _pageSize;
                string sort = (SortCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "relevance";
                string cat = (CategoryCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "";

                var facetGroups = new List<string> { $"[\"project_type:{_currentType}\"]" };
                
                string cleanVersion = GetCleanGameVersion();
                if (!string.IsNullOrEmpty(cleanVersion))
                {
                    facetGroups.Add($"[\"versions:{cleanVersion}\"]");
                }
                
                string loader = GetCleanLoaderType();
                if (_currentType == "mod" && !string.IsNullOrEmpty(loader))
                {
                    facetGroups.Add($"[\"categories:{loader}\"]");
                }

                if (!string.IsNullOrEmpty(cat))
                {
                    facetGroups.Add($"[\"categories:{cat}\"]");
                }
                
                string facets = $"[{string.Join(",", facetGroups)}]";
                string escapedQuery = Uri.EscapeDataString(string.IsNullOrWhiteSpace(query) ? "" : query);
                string url = $"https://api.modrinth.com/v2/search?query={escapedQuery}&facets={Uri.EscapeDataString(facets)}&limit={_pageSize}&offset={offset}&index={sort}";
                
                string json = "";
                var response = await _http.GetAsync(url).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        string simpleFacets = $"[[\"project_type:{_currentType}\"]]";
                        string simpleUrl = $"https://api.modrinth.com/v2/search?query={escapedQuery}&facets={Uri.EscapeDataString(simpleFacets)}&limit={_pageSize}&offset={offset}&index={sort}";
                        json = await _http.GetStringAsync(simpleUrl).ConfigureAwait(false);
                    }
                    else
                    {
                        throw new Exception($"Error de Enlace Galáctico: {response.StatusCode}");
                    }
                }
                else
                {
                    json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                }

                var data = JObject.Parse(json);
                var hits = data["hits"] as JArray;
                if (hits == null) return;

                var installDir = GetInstallDir();
                var installedFiles = Directory.Exists(installDir) 
                    ? Directory.GetFileSystemEntries(installDir).Select(f => Path.GetFileNameWithoutExtension(f) ?? "").ToList() 
                    : new List<string>();

                var tempResults = new List<ModrinthItem>();

                foreach (var hit in hits)
                {
                    string pid = hit["project_id"]?.ToString() ?? "";
                    string title = hit["title"]?.ToString() ?? "";
                    string desc = hit["description"]?.ToString() ?? "";
                    string author = hit["author"]?.ToString() ?? "";
                    string icon = hit["icon_url"]?.ToString() ?? "";
                    long dl = hit["downloads"]?.ToObject<long>() ?? 0;

                    bool installed = IsInstalledOptimized(installedFiles, pid, title);

                    tempResults.Add(new ModrinthItem
                    {
                        ProjectId = pid,
                        Title = title,
                        Description = desc,
                        Author = author,
                        IconUrl = string.IsNullOrEmpty(icon) ? "pack://application:,,,/nebula.ico" : icon,
                        DownloadsText = FormatDownloads(dl),
                        IsInstalled = installed
                    });
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    _results.Clear();
                    foreach (var item in tempResults) _results.Add(item);
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ModHub] Error: {ex}");
            }
            finally 
            { 
                _isSearching = false; 
                Application.Current.Dispatcher.Invoke(() =>
                {
                    SetLoading(false);
                    EmptyPanel.Visibility = _results.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                    ResultsScroll.Visibility = _results.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
                    PaginationPanel.Visibility = _results.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
                });
            }
        }

        private async void InstallBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            string pid = btn.Tag?.ToString() ?? "";
            if (string.IsNullOrEmpty(pid)) return;

            var item = _results.FirstOrDefault(x => x.ProjectId == pid);
            if (item == null || item.IsDownloading) return;

            try
            {
                await InstallModAndDependenciesAsync(pid, item);
            }
            catch (Exception ex) 
            { 
                MessageBox.Show($"Fallo en la descarga cuántica: {ex.Message}", "Nebula Hub", MessageBoxButton.OK, MessageBoxImage.Error); 
            }
        }

        private async Task InstallModAndDependenciesAsync(string projectId, ModrinthItem? uiItem)
        {
            var downloadedProjects = new HashSet<string>();
            
            foreach(var kvp in _manifest.InstalledMods)
                downloadedProjects.Add(kvp.Key);

            await ResolveAndDownloadAsync(projectId, uiItem, downloadedProjects);
            
            if (uiItem != null)
            {
                uiItem.IsInstalled = true;
                uiItem.IsDownloading = false;
            }
        }

        private async Task ResolveAndDownloadAsync(string projectId, ModrinthItem? uiItem, HashSet<string> downloaded)
        {
            if (downloaded.Contains(projectId)) return;
            downloaded.Add(projectId);

            try
            {
                if (uiItem != null) { uiItem.IsDownloading = true; uiItem.ProgressText = "Resolviendo..."; }

                string vUrl = $"https://api.modrinth.com/v2/project/{projectId}/version";
                
                var queryParams = new List<string>();
                string loader = GetCleanLoaderType();
                string version = GetCleanGameVersion();
                
                if (!string.IsNullOrEmpty(loader) && _currentType == "mod")
                    queryParams.Add($"loaders={Uri.EscapeDataString("[\""+loader+"\"]")}");
                    
                if (!string.IsNullOrEmpty(version))
                    queryParams.Add($"game_versions={Uri.EscapeDataString("[\""+version+"\"]")}");

                if (queryParams.Count > 0)
                    vUrl += "?" + string.Join("&", queryParams);

                var json = await _http.GetStringAsync(vUrl);
                var versionsData = JArray.Parse(json);

                if (versionsData.Count == 0)
                {
                    vUrl = $"https://api.modrinth.com/v2/project/{projectId}/version";
                    if (!string.IsNullOrEmpty(version)) vUrl += $"?game_versions={Uri.EscapeDataString("[\""+version+"\"]")}";
                    json = await _http.GetStringAsync(vUrl);
                    versionsData = JArray.Parse(json);
                    if (versionsData.Count == 0) throw new Exception("Sin versión compatible.");
                }

                var latestVersion = versionsData[0];
                
                var dependencies = latestVersion["dependencies"] as JArray;
                if (dependencies != null)
                {
                    foreach(var dep in dependencies)
                    {
                        if (dep["dependency_type"]?.ToString() == "required")
                        {
                            string depProjectId = dep["project_id"]?.ToString() ?? "";
                            if (!string.IsNullOrEmpty(depProjectId))
                            {
                                if (uiItem != null) uiItem.ProgressText = "Obteniendo librerías...";
                                await ResolveAndDownloadAsync(depProjectId, uiItem, downloaded);
                            }
                        }
                    }
                }

                var fileObj = latestVersion["files"]?[0];
                if (fileObj == null) throw new Exception("Archivo no encontrado.");
                
                string fileUrl = fileObj["url"]?.ToString() ?? "";
                string fileName = fileObj["filename"]?.ToString() ?? "";

                string destDir = GetInstallDir();
                if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir);
                string destPath = Path.Combine(destDir, fileName);

                if (uiItem != null) uiItem.ProgressText = "Iniciando...";

                var progress = new Progress<double>(p => {
                    if (uiItem != null) {
                        uiItem.DownloadProgress = p * 100;
                        uiItem.ProgressText = $"{(int)(p * 100)}%";
                    }
                });

                await DownloadFileStreamAsync(fileUrl, destPath, progress);

                _manifest.InstalledMods[projectId] = latestVersion["id"]?.ToString() ?? fileName;
                SaveManifest();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ModHub] Fallo en {projectId}: {ex.Message}");
                if (uiItem != null) {
                    uiItem.IsDownloading = false;
                    uiItem.ProgressText = "Fallo cósmico";
                }
                throw;
            }
        }

        private async Task DownloadFileStreamAsync(string url, string destPath, IProgress<double> progress)
        {
            using var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            
            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            using var stream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
            
            var buffer = new byte[8192];
            int bytesRead;
            long totalRead = 0;
            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead);
                totalRead += bytesRead;
                if (totalBytes != -1) {
                    progress?.Report((double)totalRead / totalBytes);
                }
            }
        }

        private async void Card_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border b && b.DataContext is ModrinthItem item)
            {
                DetailTitle.Text = item.Title;
                DetailAuthor.Text = "por " + item.Author;
                DetailIcon.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(item.IconUrl));
                DetailDescription.Text = "Descifrando información...";
                DetailGallery.ItemsSource = null;

                DetailFlyout.Visibility = Visibility.Visible;
                var sb = (Storyboard)this.Resources["FlyoutOpen"];
                sb.Begin(DetailFlyout);

                try
                {
                    string url = $"https://api.modrinth.com/v2/project/{item.ProjectId}";
                    var json = await _http.GetStringAsync(url);
                    var data = JObject.Parse(json);
                    
                    var body = data["body"]?.ToString() ?? "";
                    body = Regex.Replace(body, @"<[^>]*>", "");
                    body = Regex.Replace(body, @"[#*`_]", "");
                    body = Regex.Replace(body, @"\n{3,}", "\n\n");
                    if (body.Length > 1500) body = body.Substring(0, 1500) + "...";
                    
                    DetailDescription.Text = data["description"]?.ToString() + "\n\n" + body;

                    var gallery = data["gallery"] as JArray;
                    if (gallery != null && gallery.Count > 0)
                    {
                        var imgs = new List<string>();
                        foreach(var g in gallery) imgs.Add(g["url"]?.ToString() ?? "");
                        DetailGallery.ItemsSource = imgs;
                    }
                }
                catch { }
            }
        }

        private void CloseFlyout_Click(object sender, RoutedEventArgs e)
        {
            var sb = (Storyboard)this.Resources["FlyoutClose"];
            sb.Completed += (s, ev) => DetailFlyout.Visibility = Visibility.Collapsed;
            sb.Begin(DetailFlyout);
        }

        private void Card_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement el)
            {
                var sb = (Storyboard)this.Resources["FadeInSlideUp"];
                sb.Begin(el);
            }
        }

        private void Skeleton_Loaded(object sender, RoutedEventArgs e)
        {
            var sb = (Storyboard)this.Resources["SkeletonAnimation"];
            sb.Begin((FrameworkElement)sender);
        }

        private void ShowLocalFiles()
        {
            try
            {
                string dir = GetInstallDir();
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                var items = new List<LocalFileItem>();
                foreach (var f in Directory.GetFiles(dir))
                {
                    long size = new FileInfo(f).Length;
                    items.Add(new LocalFileItem { 
                        Name = Path.GetFileName(f), 
                        SizeText = size > 1_000_000 ? $"{size / 1000000.0:F1} MB" : $"{size / 1024.0:F0} KB", 
                        FilePath = f 
                    });
                }
                ResultsScroll.Visibility = Visibility.Collapsed;
                PaginationPanel.Visibility = Visibility.Collapsed;
                SkeletonPanel.Visibility = Visibility.Collapsed;
                LocalList.ItemsSource = items;
                LocalScroll.Visibility = Visibility.Visible;
                EmptyPanel.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch { }
        }

        private void DeleteFileBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not string path) return;
            if (MessageBox.Show($"¿Desintegrar '{Path.GetFileName(path)}' de este universo?", "Nebula Hub", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
            try { if (File.Exists(path)) File.Delete(path); ShowLocalFiles(); } catch { }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e) 
        {
            SearchBox.Text = "";
            CategoryCombo.SelectedIndex = 0;
            _ = SearchModrinth("", true);
        }

        private string GetInstallDir() => _currentType switch { "shader" => Path.Combine(_gameFolder, "shaderpacks"), "resourcepack" => Path.Combine(_gameFolder, "resourcepacks"), _ => Path.Combine(_gameFolder, "mods") };

        private string GetCleanGameVersion()
        {
            string rawVersion = _profile?.Version ?? "";
            return Regex.Match(rawVersion, @"\d+\.\d+(\.\d+)?").Value;
        }

        private string GetCleanLoaderType()
        {
            string loader = (_profile?.LoaderType ?? "").Trim().ToLowerInvariant();
            return loader == "vanilla" ? "" : loader;
        }
        
        private void LoadManifest()
        {
            string path = Path.Combine(GetInstallDir(), "nebula_manifest.json");
            if (File.Exists(path)) {
                try {
                    _manifest = JsonConvert.DeserializeObject<NebulaManifest>(File.ReadAllText(path)) ?? new NebulaManifest();
                } catch { _manifest = new NebulaManifest(); }
            } else {
                _manifest = new NebulaManifest();
            }
        }

        private void SaveManifest()
        {
            try
            {
                string dir = GetInstallDir();
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                string path = Path.Combine(dir, "nebula_manifest.json");
                File.WriteAllText(path, JsonConvert.SerializeObject(_manifest, Formatting.Indented));
            }
            catch { }
        }

        private bool IsInstalledOptimized(List<string> installedFiles, string projectId, string title)
        {
            if (_manifest.InstalledMods.ContainsKey(projectId)) return true;

            if (string.IsNullOrWhiteSpace(title)) return false;
            string cleanTitle = Regex.Replace(title, @"[^a-zA-Z0-9]", "").ToLowerInvariant();
            return installedFiles.Any(fn => {
                if (string.IsNullOrEmpty(fn)) return false;
                string cleanFn = Regex.Replace(fn, @"[^a-zA-Z0-9]", "").ToLowerInvariant();
                return cleanFn.Contains(cleanTitle) || cleanTitle.Contains(cleanFn);
            });
        }

        private string FormatDownloads(long n) => n switch { >= 1_000_000 => $"{n / 1000000.0:F1}M", >= 1000 => $"{n / 1000.0:F0}K", _ => n.ToString() };
        
        private void SetLoading(bool loading)
        {
            SkeletonPanel.Visibility = loading ? Visibility.Visible : Visibility.Collapsed; 
            ResultsScroll.Visibility = loading ? Visibility.Collapsed : Visibility.Visible; 
            LocalScroll.Visibility = Visibility.Collapsed; 
            EmptyPanel.Visibility = Visibility.Collapsed; 
            if (loading) PaginationPanel.Visibility = Visibility.Collapsed;
        }
    }
}