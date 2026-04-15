using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace KrakenLauncher.Services
{
    public class EffectService
    {
        private static EffectService? _instance;
        public static EffectService Instance => _instance ??= new EffectService();

        private readonly List<(Ellipse dot, double vx, double vy)> _particles = new();
        private readonly Random _rnd = new();
        private Canvas? _particleCanvas;
        private Image? _backgroundImage;

        public void Initialize(Canvas particleCanvas, Image backgroundImage)
        {
            _particleCanvas = particleCanvas;
            _backgroundImage = backgroundImage;
        }

        public void StartParticles()
        {
            if (_particleCanvas == null) return;
            _particleCanvas.Children.Clear();
            _particles.Clear();

            for (int i = 0; i < 45; i++)
            {
                double size = _rnd.NextDouble() * 3 + 1;
                double opacity = _rnd.NextDouble() * 0.35 + 0.05;
                var dot = new Ellipse
                {
                    Width = size,
                    Height = size,
                    Fill = new SolidColorBrush(Color.FromArgb(
                        (byte)(opacity * 255),
                        (byte)_rnd.Next(100, 200),
                        (byte)_rnd.Next(50, 150),
                        (byte)_rnd.Next(200, 255)))
                };
                
                Canvas.SetLeft(dot, _rnd.NextDouble() * 1020);
                Canvas.SetTop(dot, _rnd.NextDouble() * 660);
                _particleCanvas.Children.Add(dot);

                double speed = _rnd.NextDouble() * 0.3 + 0.05;
                double angle = _rnd.NextDouble() * Math.PI * 2;
                _particles.Add((dot, Math.Cos(angle) * speed, Math.Sin(angle) * speed));
            }

            CompositionTarget.Rendering -= OnRendering;
            CompositionTarget.Rendering += OnRendering;
        }

        private void OnRendering(object? sender, EventArgs e)
        {
            if (_particleCanvas == null) return;
            double w = _particleCanvas.ActualWidth;
            double h = _particleCanvas.ActualHeight;
            if (w <= 0 || h <= 0) return;

            for (int i = 0; i < _particles.Count; i++)
            {
                var (dot, vx, vy) = _particles[i];
                double x = Canvas.GetLeft(dot) + vx;
                double y = Canvas.GetTop(dot) + vy;

                if (x < -10) x = w + 10;
                else if (x > w + 10) x = -10;

                if (y < -10) y = h + 10;
                else if (y > h + 10) y = -10;

                Canvas.SetLeft(dot, x);
                Canvas.SetTop(dot, y);
            }
        }

        public void UpdateBackground(UserSession session)
        {
            if (_backgroundImage == null) return;
            try
            {
                if (!string.IsNullOrWhiteSpace(session.BackgroundImagePath) && System.IO.File.Exists(session.BackgroundImagePath))
                {
                    var uri = new Uri(session.BackgroundImagePath);
                    var bmp = new BitmapImage();
                    bmp.BeginInit(); bmp.UriSource = uri; bmp.CacheOption = BitmapCacheOption.OnLoad; bmp.EndInit();
                    _backgroundImage.Source = bmp;
                    _backgroundImage.Opacity = 1.0;
                    return;
                }

                int hour = DateTime.Now.Hour;
                string nebulaUrl = "https://images.unsplash.com/photo-1551244072-5d12893278ab?q=80&w=1000";
                if (hour >= 6 && hour < 12) nebulaUrl = "https://images.unsplash.com/photo-1439066615861-d1af74d74000?q=80&w=1000";
                if (hour >= 12 && hour < 19) nebulaUrl = "https://images.unsplash.com/photo-1505118380757-91f5f45d8de4?q=80&w=1000";

                var img = new BitmapImage(new Uri(nebulaUrl));
                _backgroundImage.Source = img;
                _backgroundImage.Opacity = 0.15;
            }
            catch { _backgroundImage.Source = null; }
        }

        public void ApplyThemeColor(UserSession session, TextBlock? avatarInitial, TextBlock? percentageLabel)
        {
            try
            {
                if (string.IsNullOrEmpty(session.AccentColor)) return;
                var color = (Color)ColorConverter.ConvertFromString(session.AccentColor);

                Application.Current.Resources["AccentColor"] = color;
                Application.Current.Resources["AccentBrush"] = new SolidColorBrush(color);
                Application.Current.Resources["GlowColor"] = color;

                var hoverColor = Color.FromArgb(color.A,
                    (byte)Math.Min(255, color.R + 30),
                    (byte)Math.Min(255, color.G + 30),
                    (byte)Math.Min(255, color.B + 30));
                Application.Current.Resources["AccentHoverColor"] = hoverColor;

                if (avatarInitial != null) avatarInitial.Foreground = new SolidColorBrush(color);
                if (percentageLabel != null) percentageLabel.Foreground = new SolidColorBrush(color);
            }
            catch { }
        }
    }
}
