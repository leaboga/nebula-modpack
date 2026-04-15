using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace KrakenLauncher.Modules
{
    public partial class PerformanceView : UserControl
    {
        private readonly DispatcherTimer _timer;
        private readonly MainWindow _parent;
        private readonly Random _rnd = new();
        private readonly List<double> _history = new();

        public PerformanceView(MainWindow parent)
        {
            InitializeComponent();
            _parent = parent;

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _timer.Tick += (s, e) => UpdateMetrics();
            _timer.Start();

            UpdateMetrics();
        }

        private void UpdateMetrics()
        {
            try
            {
                // Launcher RAM
                long mem = Process.GetCurrentProcess().PrivateMemorySize64 / 1024 / 1024;
                RamValueText.Text = $"{mem} MB";

                // CPU Dummy (Improvement: Real CPU usage)
                double cpu = _rnd.Next(1, 5); 
                CpuValueText.Text = $"{cpu}%";

                // Play Time from history
                TotalPlayTimeText.Text = _parent.TotalTimeLabel.Text;

                UpdateGraph(cpu + (mem / 20.0)); // Aggregate for visual feel
            }
            catch { }
        }

        private void UpdateGraph(double value)
        {
            _history.Add(value);
            if (_history.Count > 50) _history.RemoveAt(0);

            ActivityLine.Points.Clear();
            double width = GraphCanvas.ActualWidth;
            double height = GraphCanvas.ActualHeight;

            if (width <= 0 || height <= 0) return;

            double step = width / 50;
            for (int i = 0; i < _history.Count; i++)
            {
                double x = i * step;
                double y = height - (Math.Clamp(_history[i] / 50.0, 0, 1) * height);
                ActivityLine.Points.Add(new System.Windows.Point(x, y));
            }
        }

        public void Stop() => _timer.Stop();
    }
}
