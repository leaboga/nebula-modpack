# -*- coding: utf-8 -*-
import os

with open('MainWindow.xaml.cs', 'r', encoding='utf-8') as f:
    cs = f.read()

cs = cs.replace('private void CloseButton_Click(object sender, RoutedEventArgs e)\n        {\n            this.Close();\n        }', 'private void CloseButton_Click(object sender, RoutedEventArgs e)\n        {\n            _cerrarDeVerdad = true;\n            this.Close();\n        }')

# Texts
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
    cs = cs.replace(bad, good)
    
cs = cs.replace('Ã³', 'ó')
cs = cs.replace('Ã¡', 'á')
cs = cs.replace('Ã©', 'é')
cs = cs.replace('Ã­', 'í')
cs = cs.replace('Ãº', 'ú')
cs = cs.replace('Ã±', 'ñ')
cs = cs.replace('Ã\x83Â³', 'ó')
cs = cs.replace('versiÃ³n', 'versión')
cs = cs.replace('ActualizaciÃ³n', 'Actualización')

with open('MainWindow.xaml.cs', 'w', encoding='utf-8') as f:
    f.write(cs)
