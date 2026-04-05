using System;
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
                StatusLabel.Text = "⚠ Crash Encontrado";
                StatusLabel.Foreground = System.Windows.Media.Brushes.OrangeRed;
                DetailLabel.Text = $"Se encontró un error registrado en: {analysis.FileName}";
                
                // Show the specialized view
                // This would ideally be a popup or a switch in the main container
                MessageBox.Show($"Último error: {analysis.DetectedError}\n\n{analysis.UserSolution}", "Resultado de Análisis");
            }
            else
            {
                MessageBox.Show("No se encontraron rastros de errores en los últimos 7 días.", "Salud Óptima");
            }
        }
    }
}
