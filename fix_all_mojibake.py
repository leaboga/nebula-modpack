import os

fixes = {
    'B\xef\xbf\xbdveda': 'Bóveda',
    'B\xc3\xb3veda': 'Bóveda',
    'Configuraci\xc3\xb3n': 'Configuración',
    'M\xc3\xa9tricas': 'Métricas',
    'Optimizaci\xc3\xb3n': 'Optimización',
    'Sincronizaci\xc3\xb3n': 'Sincronización',
    'Versi\xc3\xb3n': 'Versión',
    'Ã³': 'ó',
    'Ã¡': 'á',
    'Ã©': 'é',
    'Ã­': 'í',
    'Ãº': 'ú',
    'Ã±': 'ñ',
    'DirecciÃ³n': 'Dirección',
    'BÃ³veda': 'Bóveda',
    'ConfiguraciÃ³n': 'Configuración',
    'MÃ©tricas': 'Métricas',
    'OptimizaciÃ³n': 'Optimización',
    'SincronizaciÃ³n': 'Sincronización',
    'versiÃ³n': 'versión',
    'VersiÃ³n': 'Versión',
    'AÃ±adido': 'Añadido',
    'AÃ±adir': 'Añadir',
    'aÃ±adido': 'añadido',
    'aÃ±adir': 'añadir',
}

for r, d, f in os.walk('Modules'):
    for file in f:
        if file.endswith('.xaml') or file.endswith('.cs'):
            path = os.path.join(r, file)
            try:
                with open(path, 'r', encoding='utf-8') as fh:
                    content = fh.read()
                
                original = content
                for bad, good in fixes.items():
                    content = content.replace(bad, good)
                
                if content != original:
                    with open(path, 'w', encoding='utf-8') as fh:
                        fh.write(content)
                    print(f'Fixed encoding in {path}')
            except Exception as e:
                print(f"Error {path}: {e}")
