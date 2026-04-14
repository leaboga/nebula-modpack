using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
            LoadInfo();
        }

        private void LoadInfo()
        {
            try
            {
                bool isAdmin = IsRunningAsAdmin();
                AdminStatusLabel.Text = isAdmin ? "ADMINISTRADOR (ELEVADO)" : "USUARIO ESTÁNDAR";
                AdminStatusLabel.Foreground = isAdmin ? new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81)) : new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44));
                ElevateBtn.Visibility = isAdmin ? Visibility.Collapsed : Visibility.Visible;

                AppPathLabel.Text = PathService.AppFolder;
                InstancesPathLabel.Text = PathService.InstancesFolder;
                ExePathLabel.Text = Environment.ProcessPath ?? "KrakenLauncher.exe";
            }
            catch { }
        }

        private bool IsRunningAsAdmin()
        {
            try
            {
                using var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch { return false; }
        }

        private void Elevate_Click(object sender, RoutedEventArgs e)
        {
            string exe = Environment.ProcessPath ?? "";
            if (string.IsNullOrEmpty(exe))
            {
                MessageBox.Show("No se pudo determinar la ruta del ejecutable para elevar privilegios.", "Error de Elevación");
                return;
            }

            try
            {
                LoggerService.Log("[ADMIN] Solicitando relanzamiento con privilegios elevados...");
                Process.Start(new ProcessStartInfo
                {
                    FileName = exe,
                    UseShellExecute = true,
                    Verb = "runas"
                });
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                LoggerService.Log($"[ADMIN] Falló el intento de elevación: {ex.Message}");
                MessageBox.Show("El usuario canceló la elevación o ocurrió un error: " + ex.Message, "Elevación Cancelada");
            }
        }

        private void OpenAppFolder_Click(object sender, RoutedEventArgs e)
        {
            try { Process.Start("explorer.exe", PathService.AppFolder); } catch { }
        }

        private void OpenInstancesFolder_Click(object sender, RoutedEventArgs e)
        {
            try { Process.Start("explorer.exe", PathService.InstancesFolder); } catch { }
        }

        private void CopyExePath_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(Environment.ProcessPath ?? "KrakenLauncher.exe");
                MessageBox.Show("Ruta del ejecutable copiada al portapapeles.", "KRAKEN");
            }
            catch { }
        }

        private void OpenLogs_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(PathService.LogFile))
                try { Process.Start(new ProcessStartInfo { FileName = "notepad.exe", Arguments = $"\"{PathService.LogFile}\"", UseShellExecute = true }); } catch { }
            else
                MessageBox.Show("El archivo de log no existe aún.", "KRAKEN");
        }

        private void OpenSession_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(PathService.SessionFile))
                try { Process.Start(new ProcessStartInfo { FileName = "notepad.exe", Arguments = $"\"{PathService.SessionFile}\"", UseShellExecute = true }); } catch { }
            else
                MessageBox.Show("El archivo de sesión no existe aún.", "KRAKEN");
        }

        private void OpenUpdaterLog_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(PathService.UpdaterLogFile))
                try { Process.Start(new ProcessStartInfo { FileName = "notepad.exe", Arguments = $"\"{PathService.UpdaterLogFile}\"", UseShellExecute = true }); } catch { }
            else
                MessageBox.Show("El log del updater no existe aún.", "KRAKEN");
        }

        private void Analyze_Click(object sender, RoutedEventArgs e)
        {
            var analysis = _reporter.AnalyzeLastCrash(DateTime.Now.AddDays(-7));
            if (analysis != null)
            {
                StatusLabel.Text = "Crash Detectado";
                StatusLabel.Foreground = Brushes.OrangeRed;
                DetailLabel.Text = $"Error: {analysis.DetectedError}";
                MessageBox.Show($"Último error: {analysis.DetectedError}\n\nSolución: {analysis.UserSolution}", "Análisis de Crash");
            }
            else
            {
                MessageBox.Show("No se detectaron errores críticos en los últimos 7 días.", "Estado Óptimo");
            }
        }

        private void ExportReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var analysis = _reporter.AnalyzeLastCrash(DateTime.Now.AddDays(-7));
                string reportPath = Path.Combine(PathService.AppFolder, $"kraken-report-{DateTime.Now:yyyyMMdd}.txt");
                var sb = new StringBuilder();
                sb.AppendLine("KRAKEN SYSTEM REPORT");
                sb.AppendLine($"Fecha: {DateTime.Now}");
                sb.AppendLine($"Admin: {IsRunningAsAdmin()}");
                sb.AppendLine($"Version: {VersionManager.GetCurrentVersion()}");
                sb.AppendLine();
                if (analysis != null)
                {
                    sb.AppendLine("ÚLTIMO CRASH:");
                    sb.AppendLine($"Archivo: {analysis.FileName}");
                    sb.AppendLine($"Error: {analysis.DetectedError}");
                }
                else sb.AppendLine("Estado: No se detectaron fallos recientes.");

                File.WriteAllText(reportPath, sb.ToString());
                Process.Start("explorer.exe", $"/select,\"{reportPath}\"");
            }
            catch (Exception ex) { MessageBox.Show("Error al exportar: " + ex.Message); }
        }
    }
}
