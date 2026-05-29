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
                var txt = new TextBlock
                {
                    Text = " " + tab.Label.ToUpperInvariant(),
                    Foreground = new SolidColorBrush(Color.FromRgb(220, 225, 230)),
                    FontSize = 13,
                    FontWeight = FontWeights.Bold,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var rb = new RadioButton
                {
                    Content = txt,
                    Style = (Style)_main.FindResource("ToggleTab"),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Padding = new Thickness(12, 5, 12, 5),
                    MinWidth = 100,
                    Margin = new Thickness(0, 0, 8, 0),
                    Tag = tab
                };
                rb.Checked += Tab_Checked;
                TabContainer.Children.Add(rb);
                _tabButtons.Add(rb);

                if (TabContainer.Children.Count == 1) rb.IsChecked = true;
            }
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
                    : new SolidColorBrush(Color.FromRgb(0xC8, 0xD0, 0xD8));
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
                case BlueMapView blueMap:
                    blueMap.Stop();
                    break;
                case PerformanceView perf:
                    perf.Stop();
                    break;
            }
        }
    }
}
