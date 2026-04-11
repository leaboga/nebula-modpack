using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NebulaLauncher.Services;

namespace NebulaLauncher.Modules
{
    public partial class ScreenshotsView : UserControl
    {
        private readonly string _screenshotsFolder;
        private string? _selectedPath;

        public ScreenshotsView(string gameFolder)
        {
            InitializeComponent();
            _screenshotsFolder = Path.Combine(gameFolder, "screenshots");
            LoadScreenshots();
        }

        private void LoadScreenshots()
        {
            GalleryPanel.Children.Clear();

            if (!Directory.Exists(_screenshotsFolder))
            {
                EmptyState.Visibility = Visibility.Visible;
                return;
            }

            var files = Directory.GetFiles(_screenshotsFolder, "*.png");
            Array.Sort(files, (a, b) => File.GetLastWriteTime(b).CompareTo(File.GetLastWriteTime(a)));

            if (files.Length == 0)
            {
                EmptyState.Visibility = Visibility.Visible;
                CountLabel.Text       = "GALERÍA DE SCREENSHOTS";
                return;
            }

            EmptyState.Visibility  = Visibility.Collapsed;
            CountLabel.Text        = $"{files.Length} CAPTURAS";

            foreach (var file in files)
            {
                try { GalleryPanel.Children.Add(BuildThumbnail(file)); }
                catch { }
            }
        }

        private UIElement BuildThumbnail(string path)
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource            = new Uri(path);
            bmp.DecodePixelWidth     = 280;
            bmp.CacheOption          = BitmapCacheOption.OnLoad;
            bmp.EndInit();
            bmp.Freeze();

            var date = File.GetLastWriteTime(path).ToString("dd/MM  HH:mm");

            var container = new Border
            {
                Width         = 200,
                Height        = 130,
                Margin        = new Thickness(0, 0, 12, 12),
                CornerRadius  = new CornerRadius(12),
                ClipToBounds  = true,
                Cursor        = Cursors.Hand,
                BorderBrush   = new SolidColorBrush(Color.FromRgb(0x2E, 0x26, 0x48)),
                BorderThickness = new Thickness(1)
            };

            var grid = new Grid();

            var img = new Image
            {
                Source  = bmp,
                Stretch = Stretch.UniformToFill
            };

            var overlay = new Border
            {
                VerticalAlignment = VerticalAlignment.Bottom,
                Background        = new SolidColorBrush(Color.FromArgb(0xCC, 0x0A, 0x07, 0x14)),
                Padding           = new Thickness(8, 4, 8, 4)
            };
            overlay.Child = new TextBlock
            {
                Text       = date,
                Foreground = new SolidColorBrush(Color.FromRgb(0x7B, 0x6F, 0xA0)),
                FontSize   = 10
            };

            grid.Children.Add(img);
            grid.Children.Add(overlay);
            container.Child = grid;

            container.MouseDown += (_, _) => OpenFullscreen(path, bmp, date);

            // Hover effect
            container.MouseEnter += (_, _) => container.BorderBrush = new SolidColorBrush(Color.FromRgb(0x7C, 0x3A, 0xED));
            container.MouseLeave += (_, _) => container.BorderBrush = new SolidColorBrush(Color.FromRgb(0x2E, 0x26, 0x48));

            return container;
        }

        private void OpenFullscreen(string path, BitmapImage bmp, string label)
        {
            _selectedPath            = path;
            FullscreenImage.Source   = bmp;
            FullscreenLabel.Text     = $"{Path.GetFileName(path)}  ·  {label}";
            FullscreenOverlay.Visibility = Visibility.Visible;
        }

        private void FullscreenOverlay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Close if clicking the background blur
            if (e.OriginalSource == sender || e.OriginalSource is Border b && b.Background is SolidColorBrush sc && sc.Opacity < 1)
            {
                FullscreenOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnCopy_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedPath != null && File.Exists(_selectedPath))
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(_selectedPath);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    Clipboard.SetImage(bitmap);
                    MessageBox.Show("\u2705 Imagen copiada al portapapeles.", "Nebula Screenshots");
                }
                catch (Exception ex) { MessageBox.Show("Error al copiar: " + ex.Message); }
            }
        }

        private void BtnOpenInFolder_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedPath != null && File.Exists(_selectedPath))
            {
                Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{_selectedPath}\"") { UseShellExecute = true });
            }
        }

        private void BtnCopyPath_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedPath == null || !File.Exists(_selectedPath)) return;
            Clipboard.SetText(_selectedPath);
            NotificationService.Instance.ShowSuccess("Ruta copiada al portapapeles.");
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedPath == null || !File.Exists(_selectedPath)) return;
            if (MessageBox.Show($"Eliminar '{Path.GetFileName(_selectedPath)}'?", "KRAKEN Capturas", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            try
            {
                File.Delete(_selectedPath);
                _selectedPath = null;
                FullscreenOverlay.Visibility = Visibility.Collapsed;
                LoadScreenshots();
                NotificationService.Instance.ShowSuccess("Captura eliminada.");
            }
            catch (Exception ex)
            {
                NotificationService.Instance.ShowError("No se pudo eliminar la captura: " + ex.Message);
            }
        }

        private void RefreshBtn_Click(object sender, RoutedEventArgs e) => LoadScreenshots();
        private void CloseFlyout_Click(object sender, RoutedEventArgs e) => FullscreenOverlay.Visibility = Visibility.Collapsed;

        private void OpenFolderBtn_Click(object sender, RoutedEventArgs e)
        {
            Directory.CreateDirectory(_screenshotsFolder);
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{_screenshotsFolder}\"") { UseShellExecute = true });
        }
    }
}
