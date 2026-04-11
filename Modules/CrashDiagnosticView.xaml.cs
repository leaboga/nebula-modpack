using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using NebulaLauncher.Services;

namespace NebulaLauncher.Modules
{
    public partial class CrashDiagnosticView : UserControl
    {
        private readonly CrashReporterService _reporter;

        public CrashDiagnosticView(CrashReporterService reporter)
        {
            InitializeComponent();
            _reporter = reporter;
        }

        private void Analyze_Click(object sender, RoutedEventArgs e)
        {
            var analysis = _reporter.AnalyzeLastCrash(DateTime.Now.AddDays(-7));
            if (analysis != null)
            {
                StatusLabel.Text = "Crash encontrado";
                StatusLabel.Foreground = System.Windows.Media.Brushes.OrangeRed;
                DetailLabel.Text = $"Se encontro un error registrado en: {analysis.FileName}";
                MessageBox.Show($"Ultimo error: {analysis.DetectedError}\n\n{analysis.UserSolution}", "Resultado de analisis");
            }
            else
            {
                MessageBox.Show("No se encontraron rastros de errores en los ultimos 7 dias.", "Salud optima");
            }
        }

        private void ExportReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var analysis = _reporter.AnalyzeLastCrash(DateTime.Now.AddDays(-7));
                string reportPath = Path.Combine(PathService.AppFolder, $"kraken-diagnostic-{DateTime.Now:yyyyMMdd-HHmmss}.txt");
                var sb = new StringBuilder();
                sb.AppendLine("KRAKEN DIAGNOSTIC REPORT");
                sb.AppendLine($"Generado: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"Launcher: {VersionManager.GetCurrentVersion()}");
                sb.AppendLine();

                if (analysis == null)
                {
                    sb.AppendLine("Estado: sin crashes recientes detectados.");
                }
                else
                {
                    sb.AppendLine($"Archivo: {analysis.FileName}");
                    sb.AppendLine($"Error detectado: {analysis.DetectedError}");
                    sb.AppendLine($"Solucion sugerida: {analysis.UserSolution}");
                    sb.AppendLine();
                    sb.AppendLine("Extracto del crash:");
                    sb.AppendLine(analysis.FullLog.Length > 4000 ? analysis.FullLog[..4000] : analysis.FullLog);
                }

                if (File.Exists(PathService.LogFile))
                {
                    sb.AppendLine();
                    sb.AppendLine("Ultimas lineas del launcher.log:");
                    string[] lines = File.ReadAllLines(PathService.LogFile);
                    foreach (var line in lines[^Math.Min(lines.Length, 60)..])
                        sb.AppendLine(line);
                }

                File.WriteAllText(reportPath, sb.ToString(), Encoding.UTF8);
                Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{reportPath}\"") { UseShellExecute = true });
                MessageBox.Show($"Reporte exportado en:\n{reportPath}", "KRAKEN Diagnostico");
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo exportar el reporte: " + ex.Message, "KRAKEN Diagnostico");
            }
        }

        private void OpenLogs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!File.Exists(PathService.LogFile))
                {
                    MessageBox.Show("Todavia no existe launcher.log.", "KRAKEN Diagnostico");
                    return;
                }

                Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{PathService.LogFile}\"") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo abrir el log: " + ex.Message, "KRAKEN Diagnostico");
            }
        }

        private void OpenUpdateState_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!File.Exists(PathService.UpdateStateFile))
                {
                    MessageBox.Show("Todavia no existe update-state.json.", "KRAKEN Diagnostico");
                    return;
                }

                Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{PathService.UpdateStateFile}\"") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo abrir el estado de update: " + ex.Message, "KRAKEN Diagnostico");
            }
        }

        private void OpenUpdaterLog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!File.Exists(PathService.UpdaterLogFile))
                {
                    MessageBox.Show("Todavia no existe updater.log.", "KRAKEN Diagnostico");
                    return;
                }

                Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{PathService.UpdaterLogFile}\"") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo abrir updater.log: " + ex.Message, "KRAKEN Diagnostico");
            }
        }
    }
}
