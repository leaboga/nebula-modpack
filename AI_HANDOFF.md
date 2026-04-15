# KRAKEN Launcher Development Handoff

## Estado actual

- Version publicada mas reciente: `2.7.5`
- Fecha de referencia: `2026-04-14`
- Reglas obligatorias de publicacion: `docs/RELEASE_RULES.md`

## Resumen ejecutivo

Se ha integrado visualmente el sistema de **Config Sync (Setup de Pepa)** en la interfaz principal del launcher. Ahora la funcionalidad de sincronización oficial es una característica de primer nivel, fácilmente descubrible por el usuario a través de la navegación lateral.

## Version 2.7.5

### Cambios principales

- **Integración Visual de Config Sync**:
  - **Nueva Entrada en Navegación**: Se añadió la opción "Setup de Pepa" en la barra lateral (Sidebar), bajo la sección de "Operaciones".
  - **Tooltip Informativo**: La nueva opción incluye un tooltip que explica brevemente la función de sincronización oficial.
  - **Módulo Dedicado**: Se habilitó la ruta `configsync` en el `NavigationService`, la cual dirige al usuario directamente al panel de gestión de configuraciones oficiales.
  - **Identidad de Marca**: Se actualizó el título de la vista a "Setup de Pepa" y la categoría a "SINCRONIZACIÓN" para mantener la consistencia con el ecosistema.
- **Mejoras de Navegación**:
  - Sincronización de handlers entre `MainWindow.xaml` y `MainWindow.xaml.cs`.
  - Refuerzo de la lógica de cambio de vista para asegurar que el contenido se cargue correctamente al pulsar la nueva opción.

### Archivos principales tocados

- `MainWindow.xaml`: Adición del `RadioButton` en la sidebar.
- `MainWindow.xaml.cs`: Handler `Nav_ConfigSync_Checked`.
- `Services/NavigationService.cs`: Implementación del caso `configsync`.
- `NebulaLauncher.csproj`: Bump de versión a `2.7.5`.

## Validacion realizada

- Build `Release` exitoso.
- Verificación de UI: La opción "Setup de Pepa" aparece visible en la sidebar.
- Verificación de Navegación: Al hacer clic, se carga el panel de configuraciones con el título y categoría correctos.
- Verificación de Funcionalidad: Se mantiene la capacidad de aplicar, rechazar y publicar configuraciones oficiales desde el nuevo acceso directo.

## Reglas operativas importantes

Antes de cualquier release o cambio que deba llegar por auto-update:

1. leer `docs/RELEASE_RULES.md`
2. subir version respecto de la ya publicada
3. compilar limpio
4. verificar `ProductVersion` y `FileVersion`
5. publicar release real con el asset correcto
6. actualizar este handoff

## Riesgos pendientes

- Actualmente, el panel de "Setup de Pepa" reutiliza la vista de ajustes generales (`ConfigView`). En versiones futuras se recomienda separar la lógica de presets y sync oficial en una vista dedicada para evitar saturar el panel de ajustes.

## Proximos pasos sugeridos

- Crear un módulo UI exclusivo para "Setup de Pepa" con comparativas visuales de qué cambios trae la config oficial (ej: "Nuevos keybinds", "Mejoras de FPS").
- Implementar indicadores de "visto/no visto" en la sidebar para nuevas configuraciones publicadas.
