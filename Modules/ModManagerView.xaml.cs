using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace NebulaLauncher.Modules
{
    public class ModItem
    {
        public string FileName   { get; set; } = "";
        public string FileSize   { get; set; } = "—";
        public bool   IsEnabled  { get; set; } = true;
        public string StatusText => IsEnabled ? "ACTIVO" : "INACTIVO";
        
        public Brush StatusColor 
        {
            get
            {
                var color = IsEnabled ? Color.FromRgb(34, 197, 94) : Color.FromRgb(74, 66, 102);
                var brush = new SolidColorBrush(color);
                brush.Freeze(); 
                return brush;
            }
        }
    }

    public partial class ModManagerView : UserControl
    {
        private readonly string _gameFolder;
        private readonly ObservableCollection<ModItem> _mods = new ObservableCollection<ModItem>();

        public ModManagerView(string gameFolder)
        {
            InitializeComponent();
            _gameFolder = gameFolder;
            ModItemsControl.ItemsSource = _mods;
            CargarMods();
        }

        private void CargarMods()
        {
            string modsPath = Path.Combine(_gameFolder, "mods");
            if (!Directory.Exists(modsPath))
            {
                Directory.CreateDirectory(modsPath);
                NoModsAlert.Visibility = Visibility.Visible;
                _mods.Clear();
                return;
            }

            _mods.Clear();
            try
            {
                if (!Directory.Exists(modsPath)) return;

                var files = Directory.EnumerateFiles(modsPath, "*")
                    .Where(f => f.EndsWith(".jar") || f.EndsWith(".jar.disabled"))
                    .OrderBy(f => f)
                    .ToList();

                if (files.Count == 0)
                {
                    NoModsAlert.Visibility = Visibility.Visible;
                    return;
                }

                NoModsAlert.Visibility = Visibility.Collapsed;
                foreach (var file in files)
                {
                    var info = new FileInfo(file);
                    bool enabled = !file.EndsWith(".disabled");
                    string displayName = enabled ? info.Name : info.Name.Replace(".jar.disabled", ".jar");
                    
                    _mods.Add(new ModItem
                    {
                        FileName = displayName,
                        FileSize = info.Length > 1024 * 1024 ? $"{info.Length / (1024.0 * 1024):F1} MB" : $"{info.Length / 1024.0:F0} KB",
                        IsEnabled = enabled
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error listando mods galácticos: " + ex.Message, "Nebula Mod Manager");
            }
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
                    bool turnOn = box.IsChecked == true;
                    if (turnOn)
                    {
                        if (File.Exists(disabledPath)) File.Move(disabledPath, enabledPath, true);
                    }
                    else
                    {
                        if (File.Exists(enabledPath)) File.Move(enabledPath, disabledPath, true);
                    }
                    
                    CargarMods(); 
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Problema al mover el mod '{modName}':\n" + ex.Message, "Error de Sistema", MessageBoxButton.OK, MessageBoxImage.Warning);
                    CargarMods(); 
                }
            }
        }

        private void DeleteMod_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string modName)
            {
                var result = MessageBox.Show($"¿Estas seguro de eliminar '{modName}' permanentemente?", "Nebula Launcher", MessageBoxButton.YesNo, MessageBoxImage.Warning);
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
                catch (Exception ex)
                {
                    MessageBox.Show("No se pudo eliminar el mod: " + ex.Message);
                }
            }
        }

        private void BtnOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string modsPath = Path.Combine(_gameFolder, "mods");
                if (!Directory.Exists(modsPath)) Directory.CreateDirectory(modsPath);
                Process.Start(new ProcessStartInfo("explorer.exe", modsPath) { UseShellExecute = true });
            }
            catch { }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e) => CargarMods();
    }
}

