# KRAKEN Launcher Development Handoff

## Estado actual

- Version publicada mas reciente: `2.7.7`
- Fecha de referencia: `2026-04-15`
- Reglas obligatorias de publicacion: `docs/RELEASE_RULES.md`

## Resumen ejecutivo

Se ha implementado un sistema robusto de **Instalación Limpia** y **Gestión de Java Optimizado (Adoptium)**. El launcher ahora garantiza que los usuarios sin dependencias previas puedan jugar inmediatamente sin configurar Java manualmente.

## Version 2.7.7

### Cambios principales

- **Gestión de Java (Adoptium Temurin)**:
  - Nuevo `JavaService` que descarga el JRE optimizado (8, 17 o 21) según la versión de Minecraft.
  - Los binarios se instalan en `%AppData%\KrakenLauncher\runtime`.
- **Detección de Instalación Limpia**:
  - `PlayButton_Click` ahora detecta si el perfil está vacío (sin `versions` o `mods`).
  - Fuerza una sincronización completa de mods y configuraciones oficiales de Pepita en la primera ejecución.
- **Robustez en UI**:
  - Corregida ambigüedad de referencia entre `System.Windows.Shapes.Path` y `System.IO.Path`.
  - Mejorada la gestión de estados de carga durante la instalación.

### Archivos principales tocados

- `Services/JavaService.cs`: Nuevo servicio para descarga de Java.
- `MinecraftLauncher.cs`: Integración de `JavaService` para obtención dinámica de ruta.
- `MainWindow.xaml.cs`: Lógica de detección de perfiles vacíos y flujo de sincronización forzada.
- `KrakenLauncher.csproj`: Bump de versión a `2.7.7`.

## Validacion realizada

- Build `Release` exitoso.
- Verificación de binario: `ProductVersion` reporta `2.7.7`.
- Upload exitoso: Release `v2.7.7` creada en GitHub con el asset `KrakenLauncher.exe`.
- Auto-update: El launcher detecta la versión remota mayor y aplica el parche correctamente.
