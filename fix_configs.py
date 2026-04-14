"""
Script para parchear MainWindow.xaml.cs con la nueva lógica de configs de Pepita.
"""
path = r'c:\Users\Leandro\source\repos\NebulaLauncher\MainWindow.xaml.cs'
with open(path, encoding='utf-8') as f:
    content = f.read()

# ================================================================
# PATCH 1: Buscar y reemplazar el bloque SkipConfigSync por líneas
# ================================================================
lines = content.split('\n')
patch1_start = None
for i, line in enumerate(lines):
    if '!_session.SkipConfigSync' in line:
        patch1_start = i
        break

if patch1_start is not None:
    # Encontrar el fin del bloque (las próximas 9 líneas: if+{+content+}+else+{+content+})
    end_line = patch1_start + 8  # índice 0-based del último } del else
    print(f"Reemplazando líneas {patch1_start+1} a {end_line+1}")
    for j in range(patch1_start, end_line+1):
        print(f"  {j+1}: {repr(lines[j][:80])}")
    
    replacement_lines = [
        '                    // --- CONFIGS DE PEPITA: verificar hash remoto ---\r',
        '                    PlayButton.Content = "Verificando configs...";\r',
        '                    await AplicarConfigsSiHayCambiosAsync(forzar: false);',
    ]
    lines[patch1_start:end_line+1] = replacement_lines
    content = '\n'.join(lines)
    print("PATCH 1 OK")
else:
    print("PATCH 1 NO ENCONTRADO")

# ================================================================
# PATCH 2: Insertar el método helper después de SincronizarTodoAsync
# ================================================================
helper_method = '''
        /// <summary>
        /// Verifica si las configs de Pepita cambiaron (via hash remoto).
        /// Si cambiaron y el usuario no es Pepita, muestra un dialogo para que ELIJA si aplicar.
        /// Si se llama con forzar=true (desde admin), aplica sin preguntar.
        /// </summary>
        private async Task AplicarConfigsSiHayCambiosAsync(bool forzar)
        {
            try
            {
                bool esPepita = _session.IsAdmin
                             || _session.Username.Equals("Pepita",  StringComparison.OrdinalIgnoreCase)
                             || _session.Username.Equals("Leandro", StringComparison.OrdinalIgnoreCase);

                string? hashRemoto = await _syncer.ObtenerHashConfigsRemoto();
                if (string.IsNullOrEmpty(hashRemoto))
                {
                    AgregarLog("Info: No se pudo verificar configs de Pepita (sin conexion).");
                    return;
                }

                bool hayNuevasConfigs = hashRemoto != _session.LastAppliedConfigHash;

                if (!hayNuevasConfigs)
                {
                    AgregarLog("Configs al dia (sin cambios de Pepita).");
                    return;
                }

                if (esPepita && !forzar)
                {
                    AgregarLog("Pepita: hay configs nuevas publicadas. Podas aplicarlas desde el panel Config.");
                    return;
                }

                bool aplicar = forzar;
                if (!forzar)
                {
                    var resultado = Dispatcher.Invoke(() =>
                        MessageBox.Show(
                            "Pepita actualizo las configuraciones del modpack!\\n\\n" +
                            "Deseas aplicar las configs nuevas?\\n" +
                            "(Tus opciones personales de controles y graficos seran respetadas)",
                            "Configs de Pepita disponibles",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question));
                    aplicar = resultado == MessageBoxResult.Yes;
                }

                if (aplicar)
                {
                    AgregarLog("Aplicando configs de Pepita...");
                    await _syncer.SincronizarConfigs(sobrescribirTodo: false);
                    _session.LastAppliedConfigHash = hashRemoto;
                    GuardarSesion();
                    Services.NotificationService.Instance.ShowSuccess("Configs de Pepita aplicadas correctamente.");
                }
                else
                {
                    AgregarLog("Configs de Pepita omitidas por eleccion del usuario.");
                }
            }
            catch (Exception ex) { AgregarLog($"Error al verificar configs: {ex.Message}"); }
        }
'''

# Buscar el cierre de SincronizarTodoAsync
lines2 = content.split('\n')
sinc_start = None
for i, line in enumerate(lines2):
    if 'public async Task SincronizarTodoAsync' in line:
        sinc_start = i
        break

if sinc_start is not None:
    brace_count = 0
    sinc_end = None
    for j in range(sinc_start, min(sinc_start + 80, len(lines2))):
        stripped = lines2[j].strip()
        brace_count += stripped.count('{') - stripped.count('}')
        if brace_count <= 0 and j > sinc_start:
            sinc_end = j
            break
    if sinc_end:
        print(f"SincronizarTodoAsync cierra en linea {sinc_end+1}: {repr(lines2[sinc_end][:60])}")
        lines2.insert(sinc_end + 1, helper_method)
        content = '\n'.join(lines2)
        print("PATCH 2 OK")
    else:
        print("PATCH 2: No se encontro el cierre")
else:
    print("PATCH 2: SincronizarTodoAsync no encontrado")

# Guardar
with open(path, 'w', encoding='utf-8') as f:
    f.write(content)

print("PARCHE COMPLETO APLICADO")
