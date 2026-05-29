import os
import re

def fix_file(filepath):
    if not os.path.exists(filepath):
        print(f"Not found: {filepath}")
        return
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # 1. Fix broken characters
    fixes = {
        'Direccin': 'Dirección',
        'ACTUALIZACIN': 'ACTUALIZACIÓN',
        'ADMINISTRACIN': 'ADMINISTRACIÓN',
        'SESIN': 'SESIÓN',
        'MTRICAS': 'MÉTRICAS',
        'Deteccion': 'Detección',
        'rpido': 'rápido',
        'ASIGNACION': 'ASIGNACIÓN',
        'VERSION': 'VERSIÓN',
    }
    for bad, good in fixes.items():
        content = content.replace(bad, good)
        
    # Extra fix for "DirecciÃ³n" or similar if they were double-encoded
    content = content.replace('Ã³', 'ó')
    content = content.replace('Ã¡', 'á')
    content = content.replace('Ã©', 'é')
    content = content.replace('Ã­', 'í')
    content = content.replace('Ãº', 'ú')
    content = content.replace('Ã±', 'ñ')
    content = content.replace('Ã\x83Â³', 'ó') # just in case
    
    with open(filepath, 'w', encoding='utf-8') as f:
        f.write(content)

fix_file('MainWindow.xaml')
fix_file('MainWindow.xaml.cs')
fix_file('Modules/HubView.xaml')
fix_file('Modules/HubView.xaml.cs')

# Fix Close logic and add Maximize logic
with open('MainWindow.xaml.cs', 'r', encoding='utf-8') as f:
    cs = f.read()

cs = cs.replace('private void CloseButton_Click(object sender, RoutedEventArgs e)\n        {\n            this.Close();\n        }', 'private void CloseButton_Click(object sender, RoutedEventArgs e)\n        {\n            _cerrarDeVerdad = true;\n            this.Close();\n        }\n\n        private void ToggleMaximize_Click(object sender, RoutedEventArgs e)\n        {\n            this.WindowState = this.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;\n        }')

# Fix the Windows_Closing logic just in case it doesn't allow closing
# Actually _cerrarDeVerdad = true will bypass the cancel.

with open('MainWindow.xaml.cs', 'w', encoding='utf-8') as f:
    f.write(cs)

# Modify MainWindow.xaml buttons for icons
with open('MainWindow.xaml', 'r', encoding='utf-8') as f:
    xaml = f.read()

xaml = xaml.replace('<Button Content="" Style="{DynamicResource TitleBarButton}" Click="MinimizeButton_Click"/>', '<Button Content="&#xE921;" FontFamily="Segoe MDL2 Assets" Style="{DynamicResource TitleBarButton}" Click="MinimizeButton_Click"/>')
xaml = xaml.replace('<Button Content="" Style="{DynamicResource TitleBarButton}" Click="ToggleMaximize_Click" ToolTip="Maximizar/Restaurar"/>', '<Button Content="&#xE922;" FontFamily="Segoe MDL2 Assets" Style="{DynamicResource TitleBarButton}" Click="ToggleMaximize_Click" ToolTip="Maximizar/Restaurar"/>')
xaml = xaml.replace('<Button Content="" Style="{DynamicResource TitleBarButton}" Click="CloseButton_Click" x:Name="MainCloseBtn"/>', '<Button Content="&#xE106;" FontFamily="Segoe MDL2 Assets" Style="{DynamicResource TitleBarCloseButton}" Click="CloseButton_Click" x:Name="MainCloseBtn"/>')

# Change palette to white/black/blue Apple-like.
xaml = xaml.replace('Color="#020507"', 'Color="#F2F2F7"')
xaml = xaml.replace('Color="#07151A"', 'Color="#FFFFFF"')
xaml = xaml.replace('Color="#020405"', 'Color="#F2F2F7"')

# We also have to fix Themes/Styles.xaml
with open('MainWindow.xaml', 'w', encoding='utf-8') as f:
    f.write(xaml)

print("Patch applied to CS and XAML files")
