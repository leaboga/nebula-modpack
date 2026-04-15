using System.Windows;
using System.Windows.Controls;

namespace KrakenLauncher
{
    public partial class AddProfileWindow : Window
    {
        public MinecraftProfile? ResultProfile { get; private set; }

        public AddProfileWindow()
        {
            InitializeComponent();
        }

        private void Create_Click(object sender, RoutedEventArgs e)
        {
            string name = ProfileName.Text.Trim();
            if (string.IsNullOrEmpty(name)) name = "Nueva Instancia";

            string version = (VersionCombo.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "1.20.1";
            string loader = (LoaderCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "vanilla";

            ResultProfile = new MinecraftProfile
            {
                Name = name,
                Version = version,
                LoaderType = loader,
                Icon = loader switch {
                    "fabric" => "🧵",
                    "neoforge" => "☄️",
                    "forge" => "⚒️",
                    _ => "🚀"
                }
            };

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
