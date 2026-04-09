using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NebulaLauncher.Services;

namespace NebulaLauncher.Modules
{
    public partial class ChangelogView : UserControl
    {
        public ChangelogView()
        {
            InitializeComponent();
            _ = LoadChangelog();
        }

        private async System.Threading.Tasks.Task LoadChangelog()
        {
            try
            {
                var service = new ChangelogService();
                var entries = await service.GetChangelogAsync();

                Dispatcher.Invoke(() =>
                {
                    LoadingText.Visibility    = Visibility.Collapsed;
                    ChangelogScroll.Visibility = Visibility.Visible;
                    ChangelogPanel.Children.Clear();

                    foreach (var entry in entries)
                        ChangelogPanel.Children.Add(BuildEntryCard(entry));
                });
            }
            catch
            {
                Dispatcher.Invoke(() => LoadingText.Text = "⚠ No se pudo cargar el changelog.");
            }
        }

        private static UIElement BuildEntryCard(ChangelogEntry entry)
        {
            // Type badge color
            var (badgeColor, badgeText) = entry.Type switch
            {
                "fix"    => ("#10B981", "🔧 FIX"),
                "hotfix" => ("#EF4444", "🚨 HOTFIX"),
                _        => ("#00F2FF", "✦ UPDATE")
            };

            var border = new Border
            {
                CornerRadius  = new CornerRadius(16),
                Padding       = new Thickness(24, 20, 24, 20),
                Margin        = new Thickness(0, 0, 0, 14),
                BorderThickness = new Thickness(1)
            };
            border.Background = new LinearGradientBrush(
                Color.FromRgb(0x12, 0x10, 0x1E), Color.FromRgb(0x0F, 0x0B, 0x1A), 45);
            border.BorderBrush = new SolidColorBrush(Color.FromRgb(0x2E, 0x26, 0x48));

            var panel = new StackPanel();

            // Header row
            var headerGrid = new Grid { Margin = new Thickness(0, 0, 0, 14) };
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titlePanel = new StackPanel();
            titlePanel.Children.Add(new TextBlock
            {
                Text       = entry.Title,
                Foreground = new SolidColorBrush(Color.FromRgb(0xF0, 0xEA, 0xFF)),
                FontSize   = 15,
                FontWeight = FontWeights.SemiBold
            });
            titlePanel.Children.Add(new TextBlock
            {
                Text       = $"v{entry.Version}  ·  {entry.Date}",
                Foreground = new SolidColorBrush(Color.FromRgb(0x4A, 0x42, 0x66)),
                FontSize   = 11,
                Margin     = new Thickness(0, 3, 0, 0)
            });

            // Badge
            var badge = new Border
            {
                Background    = new SolidColorBrush((Color)ColorConverter.ConvertFromString(badgeColor)!) { Opacity = 0.15 },
                BorderBrush   = new SolidColorBrush((Color)ColorConverter.ConvertFromString(badgeColor)!),
                BorderThickness = new Thickness(1),
                CornerRadius  = new CornerRadius(8),
                Padding       = new Thickness(10, 4, 10, 4),
                VerticalAlignment = VerticalAlignment.Top
            };
            badge.Child = new TextBlock
            {
                Text       = badgeText,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(badgeColor)!),
                FontSize   = 10,
                FontWeight = FontWeights.Bold
            };

            Grid.SetColumn(titlePanel, 0);
            Grid.SetColumn(badge, 1);
            headerGrid.Children.Add(titlePanel);
            headerGrid.Children.Add(badge);
            panel.Children.Add(headerGrid);

            // Divider
            var div = new Border { Height = 1, Margin = new Thickness(0, 0, 0, 14) };
            div.Background = new SolidColorBrush(Color.FromRgb(0x2E, 0x26, 0x48));
            panel.Children.Add(div);

            // Changes
            foreach (var change in entry.Changes)
            {
                var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 6) };
                row.Children.Add(new TextBlock
                {
                    Text       = "  ◆  ",
                    Foreground = new SolidColorBrush(Color.FromRgb(0x7C, 0x3A, 0xED)),
                    FontSize   = 9,
                    VerticalAlignment = VerticalAlignment.Center
                });
                row.Children.Add(new TextBlock
                {
                    Text         = change,
                    Foreground   = new SolidColorBrush(Color.FromRgb(0xC4, 0xB5, 0xFD)),
                    FontSize     = 13,
                    TextWrapping = TextWrapping.Wrap
                });
                panel.Children.Add(row);
            }

            border.Child = panel;
            return border;
        }
    }
}
