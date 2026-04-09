using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NebulaLauncher.Services;
using Newtonsoft.Json;

namespace NebulaLauncher.Modules
{
    public partial class ModpackView : UserControl
    {
        private readonly ModrinthService _modrinth = new();
        private readonly MainWindow _mainWindow;
        private readonly HttpClient _http = new();

        public ModpackView()
        {
            InitializeComponent();
            _mainWindow = (MainWindow)Application.Current.MainWindow;
            LoadFeaturedModpacks();
        }

        private async void LoadFeaturedModpacks()
        {
            await ExecuteSearch();
        }

        private async void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteSearch();
        }

        private async void Filter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (this.IsLoaded) await ExecuteSearch();
        }

        private async Task ExecuteSearch()
        {
            if (ModpackList == null) return;
            
            string query = SearchBox?.Text?.Trim() ?? "";
            string version = (FilterVersion?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "";
            string loader = (FilterLoader?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "";
            string category = (FilterCategory?.SelectedItem as ListBoxItem)?.Tag?.ToString() ?? "all";

            var packs = await _modrinth.SearchModpacks(query, version, loader, category);
            ModpackList.ItemsSource = packs;
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) _ = ExecuteSearch();
        }

        private async void BtnInstall_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string projectId)
            {
                LoadingOverlay.Visibility = Visibility.Visible;
                LoadingText.Text = "Instalando Modpack...";
                StatusLabel.Text = "Obteniendo datos de Modrinth...";
                InstallProgress.Value = 10;

                try
                {
                    string? downloadUrl = await _modrinth.GetLatestVersionDownloadUrl(projectId);
                    if (string.IsNullOrEmpty(downloadUrl)) throw new Exception("No se encontró una versión descargable.");

                    StatusLabel.Text = "Descargando archivo .mrpack...";
                    byte[] mrBytes = await _http.GetByteArrayAsync(downloadUrl);
                    InstallProgress.Value = 30;

                    string tempPath = Path.Combine(Path.GetTempPath(), "nebula_pack.mrpack");
                    await File.WriteAllBytesAsync(tempPath, mrBytes);

                    StatusLabel.Text = "Analizando estructura del pack...";
                    string mcVersion = "";
                    string loader = "vanilla";
                    string packName = "";

                    using (ZipArchive archive = ZipFile.OpenRead(tempPath))
                    {
                        var entry = archive.GetEntry("modrinth.index.json");
                        if (entry == null) throw new Exception("El archivo no es un modpack de Modrinth válido.");

                        using var stream = entry.Open();
                        using var reader = new StreamReader(stream);
                        string json = await reader.ReadToEndAsync();
                        dynamic? index = JsonConvert.DeserializeObject(json);

                        if (index == null) throw new Exception("Error al leer el índice del modpack.");

                        mcVersion = index.dependencies.minecraft;
                        packName = index.name;
                        string loaderVer = "";

                        if (index.dependencies["fabric-loader"] != null) { loader = "fabric"; loaderVer = index.dependencies["fabric-loader"]; }
                        else if (index.dependencies["forge"] != null) { loader = "forge"; loaderVer = index.dependencies["forge"]; }
                        else if (index.dependencies["neoforge"] != null) { loader = "neoforge"; loaderVer = index.dependencies["neoforge"]; }

                        StatusLabel.Text = "Creando nuevo perfil Nebula...";
                        var newProfile = new MinecraftProfile
                        {
                            Name = "Pack: " + packName,
                            Version = mcVersion,
                            LoaderType = loader,
                            LoaderVersion = loaderVer
                        };

                        _mainWindow.Session.Profiles.Add(newProfile);
                        _mainWindow.Session.CurrentProfileId = newProfile.Id;
                        _mainWindow.GuardarSesion();

                        string instanceFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                                                            "NebulaLauncher", "instances", newProfile.Id);
                        Directory.CreateDirectory(instanceFolder);
                        Directory.CreateDirectory(Path.Combine(instanceFolder, "mods"));

                        StatusLabel.Text = "Descargando mods asociados (esto puede tardar)...";
                        int count = 0;
                        int total = index.files.Count;
                        foreach (var file in index.files)
                        {
                            count++;
                            string fileUrl = file.downloads[0];
                            string filePath = Path.Combine(instanceFolder, (string)file.path);
                            
                            StatusLabel.Text = $"Descargando [{count}/{total}]: {Path.GetFileName(filePath)}";
                            InstallProgress.Value = 30 + (double)count / total * 60;

                            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                            byte[] fileData = await _http.GetByteArrayAsync(fileUrl);
                            await File.WriteAllBytesAsync(filePath, fileData);
                        }
                        
                        // Overrides
                        StatusLabel.Text = "Aplicando configuraciones locales...";
                        foreach (var zipEntry in archive.Entries)
                        {
                            if (zipEntry.FullName.StartsWith("overrides/"))
                            {
                                string relativePath = zipEntry.FullName.Substring(10);
                                if (string.IsNullOrEmpty(relativePath)) continue;
                                string targetPath = Path.Combine(instanceFolder, relativePath);
                                Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
                                zipEntry.ExtractToFile(targetPath, true);
                            }
                        }
                    }

                    File.Delete(tempPath);
                    InstallProgress.Value = 100;
                    StatusLabel.Text = "¡Pack instalado con éxito!";
                    MessageBox.Show($"✅ El modpack se ha instalado como un nuevo perfil.\nVersión: {mcVersion}\nLoader: {loader}", 
                                    "Instalación Galáctica", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    _mainWindow.RecargarPerfiles();
                    _mainWindow.CambiarVista("home");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error instalando modpack: " + ex.Message, "Fallo de Transmisión");
                }
                finally
                {
                    LoadingOverlay.Visibility = Visibility.Collapsed;
                }
            }
        }
    }
}
