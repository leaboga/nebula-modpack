# KRAKEN Launcher Development Handoff

## Estado actual

- Version publicada mas reciente: `2.7.3`
- Fecha de referencia: `2026-04-14`
- Reglas obligatorias de publicacion: `docs/RELEASE_RULES.md`

## Resumen ejecutivo

Se ha implementado el sistema de **Presets de Configuración**, una funcionalidad clave que permite a los usuarios guardar, gestionar y replicar su configuración de juego (controles, gráficos, mods, shaders) entre diferentes perfiles e instancias.

## Version 2.7.3

### Cambios principales

- **Sistema de Presets de Juego**:
  - Nuevo `PresetService` para la gestión de archivos de configuración.
  - Capacidad de guardar "Presets" que incluyen `options.txt`, `servers.dat`, `hotbar.nbt` y carpetas de `config`, `shaderpacks` y `resourcepacks`.
  - Nueva tarjeta en `ConfigView` para gestionar presets guardados.
  - **Aplicación Selectiva**: El usuario puede elegir qué parte del preset aplicar (solo controles, solo gráficos, solo configs de mods, o todo).
  - **Filtro de options.txt**: Al aplicar solo controles o solo gráficos, se realiza un merge inteligente de las líneas correspondientes en el archivo `options.txt` sin sobrescribir el resto.
  - **Backup Automático**: Antes de aplicar un preset, se realiza una copia de seguridad de la configuración actual en la carpeta `backups` de la instancia.
- **UI/UX**:
  - Lista interactiva de presets con fecha y versión de Minecraft.
  - Botones para Guardar, Aplicar y Eliminar presets.
  - Feedback visual en la consola de telemetría del launcher.

### Archivos principales tocados

- `Services/PresetService.cs`: Lógica de persistencia y aplicación de presets.
- `Modules/ConfigView.xaml`: Nueva interfaz de gestión de presets.
- `Modules/ConfigView.xaml.cs`: Handlers para la interacción con el usuario.
- `NebulaLauncher.csproj`: Bump de versión a `2.7.3`.

## Validacion realizada

- Build `Release` exitoso.
- Verificación de guardado: Los archivos se copian correctamente a la carpeta `presets` de `AppData`.
- Verificación de aplicación: Se comprobó que el backup se genera y los archivos se sobrescriben/mezclan correctamente en la instancia destino.
- Verificación de borrado: La carpeta del preset se elimina correctamente.

## Reglas operativas importantes

Antes de cualquier release o cambio que deba llegar por auto-update:

1. leer `docs/RELEASE_RULES.md`
2. subir version respecto de la ya publicada
3. compilar limpio
4. verificar `ProductVersion` y `FileVersion`
5. publicar release real con el asset correcto
6. actualizar este handoff

## Riesgos pendientes

- Si un preset es de una versión de Minecraft muy diferente (ej: 1.8 vs 1.21), algunas opciones de `options.txt` podrían no ser compatibles, aunque el merge mitiga gran parte del riesgo.

## Proximos pasos sugeridos

- Agregar soporte para exportar/importar presets como archivos `.kraken` para compartir con otros jugadores.
- Sincronización de presets en la nube mediante el `CloudService` existente.
