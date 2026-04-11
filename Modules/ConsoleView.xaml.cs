using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using NebulaLauncher.Services;

namespace NebulaLauncher.Modules
{
    public partial class ConsoleView : UserControl
    {
        private readonly System.Collections.Generic.List<string> _allLogs = new System.Collections.Generic.List<string>();

        public ConsoleView()
        {
            InitializeComponent();
            LoggerService.OnLogReceived += HandleLogReceived;
            
            // Initial load if file exists
            try
            {
                if (File.Exists(PathService.LogFile))
                {
                    var lastLines = File.ReadAllLines(PathService.LogFile);
                    int start = Math.Max(0, lastLines.Length - 200);
                    for (int i = start; i < lastLines.Length; i++)
                        _allLogs.Add(lastLines[i]);
                    RefreshLogView();
                }
            }
            catch { }
        }

        private void HandleLogReceived(string msg)
        {
            Dispatcher.Invoke(() =>
            {
                _allLogs.Add(msg);
                if (_allLogs.Count > 400)
                    _allLogs.RemoveAt(0);
                RefreshLogView();
            });
        }

        private void RefreshLogView()
        {
            string filter = FilterBox?.Text?.Trim() ?? string.Empty;
            var lines = string.IsNullOrWhiteSpace(filter)
                ? _allLogs
                : _allLogs.Where(line => line.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

            LogOutput.Text = string.Join(Environment.NewLine, lines);
            LogOutput.ScrollToEnd();
            LogScroll.ScrollToEnd();
        }

        private void FilterBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RefreshLogView();
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string exportPath = Path.Combine(PathService.AppFolder, $"kraken-console-{DateTime.Now:yyyyMMdd-HHmmss}.txt");
                File.WriteAllLines(exportPath, _allLogs);
                Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{exportPath}\"") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo exportar la consola: " + ex.Message, "KRAKEN Console");
            }
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!File.Exists(PathService.LogFile))
                {
                    MessageBox.Show("Todavia no existe launcher.log.", "KRAKEN Console");
                    return;
                }

                Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{PathService.LogFile}\"") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo abrir launcher.log: " + ex.Message, "KRAKEN Console");
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LoggerService.ClearLogFile();
                _allLogs.Clear();
                RefreshLogView();
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo limpiar la consola: " + ex.Message, "KRAKEN Console");
            }
        }
    }
}
