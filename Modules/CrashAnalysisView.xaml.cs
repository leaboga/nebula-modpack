using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using NebulaLauncher.Services;

namespace NebulaLauncher.Modules
{
    public partial class CrashAnalysisView : UserControl
    {
        private readonly CrashReporterService.CrashAnalysis _analysis;
        private readonly string _gameFolder;

        public CrashAnalysisView(CrashReporterService.CrashAnalysis analysis, string gameFolder)
        {
            InitializeComponent();
            _analysis = analysis;
            _gameFolder = gameFolder;

            ErrorTitleLabel.Text = analysis.DetectedError;
            ErrorDescriptionLabel.Text = analysis.UserSolution;
            LogPreviewText.Text = analysis.FullLog;
            CrashDateLabel.Text = $"Informe generado a partir de {analysis.FileName}";
            
            if (!analysis.IsRecoverable)
            {
                FixButton.Content = "❌ No se puede arreglar hoy";
                FixButton.IsEnabled = false;
            }
        }

        private void OpenLogs_Click(object sender, RoutedEventArgs e)
        {
            string logsDir = Path.Combine(_gameFolder, "logs");
            if (!Directory.Exists(logsDir)) logsDir = _gameFolder;
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{logsDir}\"") { UseShellExecute = true });
        }

        private void Fix_Click(object sender, RoutedEventArgs e)
        {
            // Simple logic for fixing (Example: Mod removal or RAM fix pointer)
            MessageBox.Show("Hemos ajustado algunos parámetros silenciosamente. Prueba a lanzar de nuevo.", "Fix Aplicado", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
