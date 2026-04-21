using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace KrakenLauncher.Modules
{
    public partial class HubView : UserControl
    {
        public class HubTab
        {
            public string Label { get; set; } = "";
            public string Icon { get; set; } = "";
            public UserControl? View { get; set; }
            public Func<UserControl>? ViewFactory { get; set; }
            public string HeaderLabel { get; set; } = "";
            public string HeaderTitle { get; set; } = "";

            public UserControl GetView()
            {
                if (View == null && ViewFactory != null) View = ViewFactory();
                return View ?? new UserControl();
            }
        }

        private readonly List<HubTab> _tabs;
        private readonly MainWindow _main;

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
            foreach (var tab in _tabs)
            {
                var rb = new RadioButton
                {
                    Content = string.IsNullOrWhiteSpace(tab.Icon) ? tab.Label : $"{tab.Icon}  {tab.Label}",
                    Style = (Style)_main.FindResource("ToggleTab"),
                    Margin = new Thickness(0, 0, 12, 0),
                    Tag = tab
                };
                rb.Checked += Tab_Checked;
                TabContainer.Children.Add(rb);

                if (TabContainer.Children.Count == 1) rb.IsChecked = true;
            }
        }

        private void Tab_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is not RadioButton rb || rb.Tag is not HubTab tab) return;

            StopActive();
            ActiveModuleContainer.Content = tab.GetView();
            OnHeaderUpdateRequested?.Invoke(tab.HeaderLabel, tab.HeaderTitle);

            var sb = (Storyboard)_main.FindResource("TabChangeEffect");
            sb.Begin(ActiveModuleContainer);
        }

        public void StopActive()
        {
            try
            {
                if (ActiveModuleContainer?.Content is SocialView sv) sv.Stop();
                if (ActiveModuleContainer?.Content is PerformanceView pv) pv.Stop();
            }
            catch { }
        }
    }
}
