using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Newtonsoft.Json;

namespace NebulaLauncher.Modules
{
    public class ModrinthItem
    {
        public string ProjectId { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Author { get; set; } = "";
        public string IconUrl { get; set; } = "";
        public string DownloadsText { get; set; } = "";
        public string ButtonLabel { get; set; } = "📥 Instalar";
        public Brush ButtonColor { get; set; } = Brushes.Transparent;
        public Visibility IsInstalledVisibility { get; set; } = Visibility.Collapsed;
        public List<string> Categories { get; set; } = new List<string>();
    }

    public class LocalFileItem
    {
        public string Name { get; set; } = "";
        public string SizeText { get; set; } = "";
        public string FilePath { get; set; } = "";
    }

    public partial class VaultView : UserControl
    {
        private readonly string _gameFolder;
        private readonly MinecraftProfile? _profile;
        private static readonly HttpClient _http = new HttpClient() { Timeout = TimeSpan.FromSeconds(15) };
        private string _currentType = "mod";
        private bool _showInstalled = false;
        private readonly ObservableCollection<ModrinthItem> _results = new ObservableCollection<ModrinthItem>();
        private bool _isSearching = false;

        public VaultView(string gameFolder, MinecraftProfile? profile = null)
        {
            try
            {
                InitializeComponent();
                _gameFolder = gameFolder;
                _profile = profile;
                ResultsList.ItemsSource = _results;

                if (!_http.DefaultRequestHeaders.Contains("User-Agent"))
                    _http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) NebulaLauncher/1.5");

                _ = SearchModrinth("");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al inicializar Mod Hub: " + ex.Message);
            }
        }

        private void FilterType_Changed(object sender, RoutedEventArgs e)
        {
            if (FilterMods == null || _isSearching) return;
            if (FilterMods.IsChecked == true) _currentType = "mod";
            else if (FilterShaders.IsChecked == true) _currentType = "shader";
            else if (FilterResourcePacks.IsChecked == true) _currentType = "resourcepack";

            _showInstalled = false;
            if (FilterInstalled != null) FilterInstalled.IsChecked = false;
            _ = SearchModrinth(SearchBox?.Text ?? "");
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
            if (e.Key == Key.Enter) _ = SearchModrinth(SearchBox.Text ?? "");
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e) => _ = SearchModrinth(SearchBox?.Text ?? "");

        private async Task SearchModrinth(string query)
        {
            if (_isSearching) return;
            _isSearching = true;

            SetLoading(true);
            try
            {
                // MODRINTH FACETS: AND logic must use separate nested arrays: [["facet1:val1"], ["facet2:val2"]]
                var facetGroups = new List<string> { $"[\"project_type:{_currentType}\"]" };
                
                if (_profile != null && !string.IsNullOrEmpty(_profile.Version))
                    facetGroups.Add($"[\"versions:{_profile.Version}\"]");
                
                if (_profile != null && _currentType == "mod" && !string.IsNullOrEmpty(_profile.LoaderType) && _profile.LoaderType != "vanilla")
                    facetGroups.Add($"[\"categories:{_profile.LoaderType}\"]");
                
                string facets = $"[{string.Join(",", facetGroups)}]";

                string escapedQuery = Uri.EscapeDataString(string.IsNullOrWhiteSpace(query) ? "" : query);
                string url = $"https://api.modrinth.com/v2/search?query={escapedQuery}&facets={Uri.EscapeDataString(facets)}&limit=40";
                
                Debug.WriteLine("[ModHub] Searching url: " + url);
                
                var response = await _http.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    string errContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Modrinth API Error ({response.StatusCode}): {errContent}");
                }

                var json = await response.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<dynamic>(json);
                
                // Fallback: Si no hay hits, reintentar sin filtros de versión/categoría
                if (data == null || data.hits == null || data.hits.Count == 0)
                {
                    Debug.WriteLine("[ModHub] No results with filters, retrying without filters...");
                    string simpleFacets = $"[[\"project_type:{_currentType}\"]]";
                    string simpleUrl = $"https://api.modrinth.com/v2/search?query={escapedQuery}&facets={Uri.EscapeDataString(simpleFacets)}&limit=40";
                    json = await _http.GetStringAsync(simpleUrl);
                    data = JsonConvert.DeserializeObject<dynamic>(json);
                }

                if (data == null || data.hits == null) return;

                var installDir = GetInstallDir();
                var installedFiles = Directory.Exists(installDir) 
                    ? Directory.GetFileSystemEntries(installDir).Select(f => Path.GetFileNameWithoutExtension(f) ?? "").ToList() 
                    : new List<string>();

                var tempResults = new List<ModrinthItem>();

                foreach (var hit in data.hits)
                {
                    string pid = (string)hit.project_id;
                    string title = (string)hit.title;
                    string desc = (string)hit.description ?? "";
                    string author = (string)hit.author;
                    string icon = (string)hit.icon_url ?? "";
                    long dl = (long)hit.downloads;

                    bool installed = IsInstalledOptimized(installedFiles, title);

                    SolidColorBrush btnBrush = installed ? new SolidColorBrush(Color.FromRgb(34, 197, 94)) : new SolidColorBrush(Color.FromRgb(124, 58, 237));
                    btnBrush.Freeze(); 

                    tempResults.Add(new ModrinthItem
                    {
                        ProjectId = pid,
                        Title = title,
                        Description = desc,
                        Author = author,
                        IconUrl = icon,
                        DownloadsText = FormatDownloads(dl),
                        ButtonLabel = installed ? "✓ INSTALADO" : "📥 INSTALAR",
                        ButtonColor = btnBrush,
                        IsInstalledVisibility = installed ? Visibility.Visible : Visibility.Collapsed
                    });
                }

                Dispatcher.Invoke(() =>
                {
                    _results.Clear();
                    foreach (var item in tempResults) _results.Add(item);
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => { LoadingLabel.Text = $"Error: {ex.Message}"; });
            }
            finally 
            { 
                _isSearching = false; 
                Dispatcher.Invoke(() =>
                {
                    SetLoading(false);
                    EmptyPanel.Visibility = _results.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                    ResultsScroll.Visibility = _results.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
                });
            }
        }

        private async void InstallBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            string pid = btn.Tag?.ToString() ?? "";
            if (string.IsNullOrEmpty(pid)) return;

            btn.IsEnabled = false;
            btn.Content = "⏳ DESCARGANDO...";

            try
            {
                string vUrl = $"https://api.modrinth.com/v2/project/{pid}/version";
                
                if (_profile != null)
                {
                    string loaders = JsonConvert.SerializeObject(new[] { _profile.LoaderType });
                    string versions = JsonConvert.SerializeObject(new[] { _profile.Version });
                    vUrl += $"?loaders={Uri.EscapeDataString(loaders)}&game_versions={Uri.EscapeDataString(versions)}";
                }

                var json = await _http.GetStringAsync(vUrl);
                var versionsData = JsonConvert.DeserializeObject<dynamic>(json);
                if (versionsData == null || versionsData.Count == 0)
                {
                    // Fallback to all versions if filtered returns nothing
                    if (_profile != null)
                    {
                        json = await _http.GetStringAsync($"https://api.modrinth.com/v2/project/{pid}/version");
                        versionsData = JsonConvert.DeserializeObject<dynamic>(json);
                    }
                    if (versionsData == null || versionsData.Count == 0) throw new Exception("No hay versiones compatibles.");
                }

                var latest = versionsData[0];
                var file = latest?.files?[0];
                if (file == null) throw new Exception("Archivo no disponible.");
                string fileUrl = (string)file.url ?? "";
                string fileName = (string)file.filename ?? "";

                string destDir = GetInstallDir();
                if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir);
                string destPath = Path.Combine(destDir, fileName);

                var bytes = await _http.GetByteArrayAsync(fileUrl);
                await File.WriteAllBytesAsync(destPath, bytes);

                btn.Content = "✓ COMPLETADO";
                SolidColorBrush ok = new SolidColorBrush(Color.FromRgb(34, 197, 94)); ok.Freeze();
                btn.Background = ok;
            }
            catch (Exception ex) 
            { 
                MessageBox.Show("Error de descarga: " + ex.Message, "Nebula Mod Hub", MessageBoxButton.OK, MessageBoxImage.Error); 
                btn.IsEnabled = true; 
                btn.Content = "❌ ERROR";
            }
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
                LoadingPanel.Visibility = Visibility.Collapsed;
                LocalList.ItemsSource = items;
                LocalScroll.Visibility = Visibility.Visible;
                EmptyPanel.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch { }
        }

        private void OpenFileBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string path)
            {
                try { Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"") { UseShellExecute = true }); } catch { }
            }
        }

        private void DeleteFileBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not string path) return;
            if (MessageBox.Show($"¿Eliminar '{Path.GetFileName(path)}' permanentemente?", "Nebula Hub", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
            try { if (File.Exists(path)) File.Delete(path); ShowLocalFiles(); } catch { }
        }

        private void BtnOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string dir = GetInstallDir();
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                Process.Start(new ProcessStartInfo("explorer.exe", $"\"{dir}\"") { UseShellExecute = true });
            }
            catch { }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e) => _ = SearchModrinth(SearchBox?.Text ?? "");

        private string GetInstallDir() => _currentType switch { "shader" => Path.Combine(_gameFolder, "shaderpacks"), "resourcepack" => Path.Combine(_gameFolder, "resourcepacks"), _ => Path.Combine(_gameFolder, "mods") };
        
        private bool IsInstalledOptimized(List<string> installed, string title)
        {
            if (string.IsNullOrWhiteSpace(title)) return false;
            string cleanTitle = System.Text.RegularExpressions.Regex.Replace(title, @"[^a-zA-Z0-9]", "").ToLowerInvariant();
            return installed.Any(fn => {
                if (string.IsNullOrEmpty(fn)) return false;
                string cleanFn = System.Text.RegularExpressions.Regex.Replace(fn, @"[^a-zA-Z0-9]", "").ToLowerInvariant();
                return cleanFn.Contains(cleanTitle) || cleanTitle.Contains(cleanFn);
            });
        }

        private string FormatDownloads(long n) => n switch { >= 1_000_000 => $"{n / 1000000.0:F1}M", >= 1000 => $"{n / 1000.0:F0}K", _ => n.ToString() };
        
        private void SetLoading(bool loading)
        {
            Dispatcher.Invoke(() => { 
                LoadingPanel.Visibility = loading ? Visibility.Visible : Visibility.Collapsed; 
                ResultsScroll.Visibility = loading ? Visibility.Collapsed : Visibility.Visible; 
                LocalScroll.Visibility = Visibility.Collapsed; 
                EmptyPanel.Visibility = Visibility.Collapsed; 
            });
        }
    }
}

