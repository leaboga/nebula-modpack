using System.Windows;
using System.Windows.Input;

namespace NebulaLauncher
{
    public partial class AdminLoginWindow : Window
    {
        public string Clave { get; private set; } = "";

        public AdminLoginWindow()
        {
            InitializeComponent();
            Loaded += (_, _) => ClaveBox.Focus();
        }

        private void Confirmar_Click(object sender, RoutedEventArgs e)
        {
            Clave = ClaveBox.Password;
            DialogResult = true;
        }

        private void ClaveBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Clave = ClaveBox.Password;
                DialogResult = true;
            }
        }
    }
}