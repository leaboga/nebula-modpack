# KRAKEN Launcher Development Handoff

## Estado actual

- Version publicada mas reciente: `2.7.1`
- Fecha de referencia: `2026-04-11`
- Reglas obligatorias de publicacion: `docs/RELEASE_RULES.md`

## Resumen ejecutivo

La base del launcher ya no debe publicarse como `NebulaLauncher.exe`. El binario visible, el asset de release y la identidad del producto deben permanecer alineados en `KrakenLauncher.exe`.

En esta sesion se cerro una nueva release enfocada en tres frentes:

- observabilidad del sistema de update
- feedback tecnico mas util dentro de la UI
- mejor contexto de compatibilidad en Mod Hub y Modpack Hub

## Version 2.7.1

### Cambios principales

- Nuevo `UpdateDiagnosticsService` para persistir el estado del updater:
  - version local detectada
  - version remota detectada
  - asset seleccionado
  - URL descargada
  - ruta del ejecutable actual
  - ruta objetivo del reemplazo
  - estado y ultimo error registrado
- `PathService` ahora expone rutas dedicadas para:
  - `update-state.json`
  - `updater.log`
- `LoggerService` ahora mantiene un buffer reciente en memoria y soporta:
  - lectura de entradas recientes
  - limpieza del archivo de log
- `ConsoleView` mejorado con:
  - filtro de logs
  - exportar contenido visible
  - abrir archivo `launcher.log`
  - limpiar log
- `CrashDiagnosticView` mejorado con accesos rapidos para:
  - abrir `update-state.json`
  - abrir `updater.log`
- `VaultView` ahora muestra estado de compatibilidad basico con el perfil activo:
  - `Sin perfil`
  - `Defini version`
  - `Defini loader`
  - `Compatible`
- `ModpackView` ahora muestra:
  - resumen de loader
  - resumen de version
  - badge de compatibilidad con el perfil activo
- El flujo de update sigue priorizando el binario real del launcher:
  - nombre actual del `.exe`
  - `KrakenLauncher.exe`
  - fallback a primer `.exe` valido

### Archivos principales tocados

- `MainWindow.xaml.cs`
- `Modules/ConsoleView.xaml`
- `Modules/ConsoleView.xaml.cs`
- `Modules/CrashDiagnosticView.xaml`
- `Modules/CrashDiagnosticView.xaml.cs`
- `Modules/VaultView.xaml`
- `Modules/VaultView.xaml.cs`
- `Modules/ModpackView.xaml`
- `Modules/ModpackView.xaml.cs`
- `Services/LoggerService.cs`
- `Services/ModrinthService.cs`
- `Services/PathService.cs`
- `Services/UpdateDiagnosticsService.cs`
- `NebulaLauncher.csproj`
- `app.manifest`
- `NebulaSetup.iss`

## Validacion realizada

- Build `Release` exitoso en carpeta temporal separada para evitar bloqueo del ejecutable abierto.
- Metadata de version alineada en:
  - `NebulaLauncher.csproj`
  - `app.manifest`
  - `NebulaSetup.iss`
- La siguiente publicacion debe incluir como asset principal `KrakenLauncher.exe`.

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
