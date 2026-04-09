using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NebulaLauncher.Modules
{
    public class ModItem
    {
        public string FileName   { get; set; } = "";
        public string SizeText   { get; set; } = "—";
        public bool   IsEnabled  { get; set; } = true;
        public ImageSource? Icon { get; set; }
        
        public string StatusText => IsEnabled ? "ACTIVO" : "INACTIVO";
        public Brush StatusColor => IsEnabled ? new SolidColorBrush(Color.FromRgb(0x7C, 0x3A, 0xED)) : new SolidColorBrush(Color.FromRgb(0x4A, 0x42, 0x66));
    }

    public partial class ModManagerView : UserControl
    {
        private readonly string _gameFolder;
        private readonly MinecraftProfile? _profile;
        private readonly ObservableCollection<ModItem> _mods = new ObservableCollection<ModItem>();
        
        public event Func<Task>? OnSyncRequested;

        public ModManagerView(string gameFolder, MinecraftProfile? profile = null)
        {
            InitializeComponent();
            _gameFolder = gameFolder;
            _profile = profile;
            ModList.ItemsSource = _mods;
            CargarMods();
            ActualizarEstadoSync();
        }

        private void ActualizarEstadoSync()
        {
            if (_profile == null || SyncStatusLabel == null) return;
            SyncStatusLabel.Text = _profile.LastSyncDate == "Nunca" ? "⚠ REQUIERE SINCRONIZACIÓN" : $"✓ ÚLTIMA SINCRO: {_profile.LastSyncDate}";
            SyncStatusLabel.Foreground = _profile.LastSyncDate == "Nunca" ? (Brush)new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44)) : (Brush)new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81));
        }

        private void CargarMods()
        {
            string modsPath = Path.Combine(_gameFolder, "mods");
            if (!Directory.Exists(modsPath))
            {
                Directory.CreateDirectory(modsPath);
                EmptyPanel.Visibility = Visibility.Visible;
                _mods.Clear();
                return;
            }

            _mods.Clear();
            try
            {
                var files = Directory.EnumerateFiles(modsPath, "*")
                    .Where(f => f.EndsWith(".jar") || f.EndsWith(".jar.disabled"))
                    .OrderBy(f => f)
                    .ToList();

                if (files.Count == 0)
                {
                    EmptyPanel.Visibility = Visibility.Visible;
                    return;
                }

                EmptyPanel.Visibility = Visibility.Collapsed;
                foreach (var file in files)
                {
                    var info = new FileInfo(file);
                    bool enabled = !file.EndsWith(".disabled");
                    string baseName = enabled ? info.Name : info.Name.Replace(".jar.disabled", ".jar");
                    
                    var item = new ModItem
                    {
                        FileName = baseName,
                        SizeText = info.Length > 1024 * 1024 ? $"{info.Length / (1024.0 * 1024):F1} MB" : $"{info.Length / 1024.0:F0} KB",
                        IsEnabled = enabled,
                        Icon = ExtraerIconoDeMod(file)
                    };
                    
                    _mods.Add(item);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error listando mods: " + ex.Message, "Nebula Mod Manager");
            }
        }

        private ImageSource? ExtraerIconoDeMod(string jarPath)
        {
            try
            {
                using (ZipArchive archive = ZipFile.OpenRead(jarPath))
                {
                    // Patrones comunes de iconos en mods (Forge/Fabric)
                    string[] possiblePaths = { "icon.png", "logo.png", "assets/icon.png", "pack.png" };
                    
                    ZipArchiveEntry? entry = null;
                    foreach (var path in possiblePaths)
                    {
                        entry = archive.GetEntry(path);
                        if (entry == null) 
                        {
                            // Búsqueda profunda si no está en la raíz
                            entry = archive.Entries.FirstOrDefault(e => e.Name.Equals("icon.png", StringComparison.OrdinalIgnoreCase) || 
                                                                        e.Name.Equals("logo.png", StringComparison.OrdinalIgnoreCase));
                        }
                        if (entry != null) break;
                    }

                    if (entry != null)
                    {
                        using (Stream stream = entry.Open())
                        {
                            var ms = new MemoryStream();
                            stream.CopyTo(ms);
                            ms.Position = 0;

                            var bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.StreamSource = ms;
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.DecodePixelWidth = 64; // Optimización de memoria
                            bitmap.EndInit();
                            bitmap.Freeze();
                            return bitmap;
                        }
                    }
                }
            }
            catch { }
            return null; // Fallback al icono por defecto en el XAML
        }

        private void ModToggle_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox box && box.Tag is string modName)
            {
                string modsPath = Path.Combine(_gameFolder, "mods");
                string enabledPath = Path.Combine(modsPath, modName);
                string disabledPath = Path.Combine(modsPath, modName + ".disabled");

                try
                {
                    if (box.IsChecked == true)
                    {
                        if (File.Exists(disabledPath)) File.Move(disabledPath, enabledPath, true);
                    }
                    else
                    {
                        if (File.Exists(enabledPath)) File.Move(enabledPath, disabledPath, true);
                    }
                    CargarMods(); 
                }
                catch (Exception ex) { MessageBox.Show("Error al alternar mod: " + ex.Message); CargarMods(); }
            }
        }

        private void DeleteMod_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string modName)
            {
                var result = MessageBox.Show($"\u00BFEst\u00E1s seguro de eliminar '{modName}'?", "KRAKEN Launcher", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result != MessageBoxResult.Yes) return;

                try
                {
                    string modsPath = Path.Combine(_gameFolder, "mods");
                    string path1 = Path.Combine(modsPath, modName);
                    string path2 = Path.Combine(modsPath, modName + ".disabled");

                    if (File.Exists(path1)) File.Delete(path1);
                    if (File.Exists(path2)) File.Delete(path2);

                    CargarMods();
                }
                catch (Exception ex) { MessageBox.Show("Error al eliminar: " + ex.Message); }
            }
        }

        private void BtnOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            try { Process.Start(new ProcessStartInfo("explorer.exe", Path.Combine(_gameFolder, "mods")) { UseShellExecute = true }); } catch { }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e) => CargarMods();

        private async void BtnSync_Click(object sender, RoutedEventArgs e)
        {
            if (BtnSync == null) return;
            BtnSync.IsEnabled = false;
            BtnSync.Content = "🚀 Sincronizando...";
            
            if (OnSyncRequested != null)
            {
                await OnSyncRequested.Invoke();
            }
            
            CargarMods();
            ActualizarEstadoSync();
            BtnSync.Content = "🚀 Sincronizar";
            BtnSync.IsEnabled = true;
        }
    }
}
