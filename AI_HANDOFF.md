# KRAKEN Launcher Development Handoff

## Estado actual

- Version publicada mas reciente: `2.7.2`
- Fecha de referencia: `2026-04-14`
- Reglas obligatorias de publicacion: `docs/RELEASE_RULES.md`

## Resumen ejecutivo

Se ha implementado una nueva sección de Diagnóstico y Soporte que centraliza la gestión de permisos, rutas de configuración y análisis de errores. Esta mejora facilita el soporte técnico y permite al usuario elevar privilegios de forma autónoma desde la propia interfaz.

## Version 2.7.2

### Cambios principales

- **Nueva Vista de Diagnóstico y Soporte**:
  - Centraliza herramientas de soporte en un solo lugar.
  - Indicador visual de estado de administrador (Elevado vs Estándar).
  - Botón para relanzar el launcher como administrador de forma segura.
  - Accesos directos para abrir carpetas de la App e Instancias.
  - Botón para copiar la ruta del ejecutable actual.
  - Accesos rápidos a logs del sistema y archivos de configuración (`launcher.log`, `session.json`, `updater.log`).
- **Mejoras en Navegación**:
  - Se habilitó la ruta "crash" en `NavigationService` para cargar correctamente el nuevo `CrashDiagnosticView`.
  - Se expuso el `CrashReporterService` desde `MainWindow` para su uso en módulos.
- **Identidad**:
  - Se mantiene `KrakenLauncher.exe` como el nombre de ensamblado y binario oficial.

### Archivos principales tocados

- `MainWindow.xaml.cs`: Se agregó getter para `CrashReporterService`.
- `Services/NavigationService.cs`: Se implementó la navegación a la vista de diagnóstico.
- `Modules/CrashDiagnosticView.xaml`: Rediseño completo de la interfaz de diagnóstico.
- `Modules/CrashDiagnosticView.xaml.cs`: Lógica de permisos, elevación y gestión de rutas.
- `NebulaLauncher.csproj`: Bump de versión a `2.7.2`.

## Validacion realizada

- Build `Release` exitoso.
- Verificación manual de la UI: la nueva sección de soporte es funcional.
- Verificación de elevación: el launcher se relanza correctamente solicitando UAC.
- Verificación de rutas: los botones de "Abrir" y "Ver" apuntan a las rutas correctas en `AppData`.

## Reglas operativas importantes

Antes de cualquier release o cambio que deba llegar por auto-update:

1. leer `docs/RELEASE_RULES.md`
2. subir version respecto de la ya publicada
3. compilar limpio
4. verificar `ProductVersion` y `FileVersion`
5. publicar release real con el asset correcto
6. actualizar este handoff

## Riesgos pendientes

- `MainWindow.xaml.cs` sigue concentrando demasiada logica y conviene seguir extrayendo responsabilidades.
- Todavia hay strings con encoding roto en algunas zonas historicas del archivo principal.
- Si vuelve a fallar un update en una maquina especifica, revisar primero:
  - `launcher.log`
  - `update-state.json`
  - `updater.log`

## Proximos pasos sugeridos

- consolidar mas mensajes visibles en `KrakenStrings`
- seguir limpiando `MainWindow.xaml.cs`
- agregar una cola visible de tareas para descargas e instalaciones
- reforzar validaciones previas en el panel de publicacion
