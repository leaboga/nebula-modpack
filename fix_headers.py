import os
import re

fixes = [
    (r'B\xef\xbf\xbdveda', 'Bóveda'),
    (r'B\ufffdveda', 'Bóveda'),
    (r'EXPLORACI\xef\xbf\xbdN', 'EXPLORACIÓN'),
    (r'EXPLORACI\ufffdN', 'EXPLORACIÓN'),
    (r'CONFIGURACI\ufffdN', 'CONFIGURACIÓN'),
    (r'Sincronizaci\ufffdn', 'Sincronización'),
    (r'versi\ufffdn', 'versión'),
    (r'versi\xef\xbf\xbdn', 'versión'),
    (r'M\ufffdtricas', 'Métricas'),
    (r'M\xef\xbf\xbdtricas', 'Métricas'),
]

def clean_headers(filepath):
    try:
        with open(filepath, 'r', encoding='utf-8', errors='replace') as f:
            content = f.read()

        for bad, good in fixes:
            content = content.replace(bad, good)
        
        # Standardize header colors
        content = re.sub(r'Foreground="#E6F9FF"(.*?FontSize="2[46]")', r'Foreground="White"\1', content, flags=re.DOTALL)
        content = re.sub(r'Foreground="#1C2D3A"(.*?FontSize="[8910]")', r'Foreground="#8E8E93"\1', content, flags=re.DOTALL)

        with open(filepath, 'w', encoding='utf-8') as f:
            f.write(content)
    except Exception as e:
        print(e)

for r, d, f in os.walk('Modules'):
    for file in f:
        if file.endswith('.xaml'):
            clean_headers(os.path.join(r, file))

print('Headers cleaned.')
