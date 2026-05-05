using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace KrakenLauncher.Modules
{
    public partial class HubView : UserControl
    {
        public class HubTab
        {
            public string Label { get; set; } = "";
            public string Icon { get; set; } = "⚙️";
            public UserControl? View { get; set; }
            public string HeaderLabel { get; set; } = "";
            public string HeaderTitle { get; set; } = "";
        }

        private List<HubTab> _tabs = new List<HubTab>();
        private readonly List<RadioButton> _tabButtons = new();
        private MainWindow _main;
        private UserControl? _currentView;

        public event Action<string, string>? OnHeaderUpdateRequested;

        public HubView(MainWindow main, List<HubTab> tabs)
        {
            InitializeComponent();
            _main = main;
            _tabs = tabs;
            
            InitializeTabs();
        }

        private void InitializeTabs()
        {
            TabContainer.Children.Clear();
            _tabButtons.Clear();
            foreach (var tab in _tabs)
            {
                var rb = new RadioButton
                {
                    Content = CreateTabContent(tab.Label),
                    Style = (Style)_main.FindResource("ToggleTab"),
                    Foreground = new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xA6)),
                    FontSize = 13,
                    FontWeight = FontWeights.SemiBold,
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Padding = new Thickness(14, 8, 14, 8),
                    MinWidth = 92,
                    Margin = new Thickness(0, 0, 12, 0),
                    Tag = tab
                };
                rb.Checked += Tab_Checked;
                TabContainer.Children.Add(rb);
                _tabButtons.Add(rb);

                if (TabContainer.Children.Count == 1) rb.IsChecked = true;
            }
        }

        private static Border CreateTabContent(string label)
        {
            return new Border
            {
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(12, 8, 12, 8),
                Child = new TextBlock
                {
                    Text = label,
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.NoWrap
                }
            };
        }

        private void Tab_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.Tag is HubTab tab)
            {
                UpdateTabVisuals(rb);
                StopView(_currentView);
                _currentView = tab.View;
                ActiveModuleContainer.Content = tab.View;
                OnHeaderUpdateRequested?.Invoke(tab.HeaderLabel, tab.HeaderTitle);
                
                // Animation
                var sb = (Storyboard)_main.FindResource("TabChangeEffect");
                sb.Begin(ActiveModuleContainer);
            }
        }

        private void UpdateTabVisuals(RadioButton active)
        {
            foreach (var rb in _tabButtons)
            {
                bool isActive = rb == active;
                rb.Foreground = isActive
                    ? Brushes.White
                    : new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xA6));

                if (rb.Content is Border border)
                {
                    border.Background = isActive
                        ? new SolidColorBrush(Color.FromRgb(0x2C, 0x2C, 0x2E))
                        : Brushes.Transparent;
                }
            }
        }

        public void StopActiveModule()
        {
            StopView(_currentView);
        }

        private static void StopView(UserControl? view)
        {
            switch (view)
            {
                case SocialView social:
                    social.Stop();
                    break;
                case PerformanceView perf:
                    perf.Stop();
                    break;
            }
        }
    }
}
