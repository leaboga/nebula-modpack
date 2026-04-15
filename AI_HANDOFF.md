# KRAKEN Launcher Development Handoff

## Estado actual

- Version publicada mas reciente: `2.7.8`
- Fecha de referencia: `2026-04-15`
- Reglas obligatorias de publicacion: `docs/RELEASE_RULES.md`

## Resumen ejecutivo

Se ha completado la **transición total de marca (branding overhaul)**. El launcher ha dejado de identificarse como "Nebula" para ser oficialmente **KRAKEN Launcher**. Esto incluye cambios en namespaces, nombres de archivos de proyecto, rutas de carpetas de usuario y mensajes de sistema.

## Version 2.7.8 (The Kraken Core)

### Cambios principales

- **Identidad KRAKEN**:
  - Namespace base renombrado de `NebulaLauncher` a `KrakenLauncher`.
  - Proyecto renombrado a `KrakenLauncher.csproj`.
  - Todos los diálogos, títulos de ventanas y logs ahora usan la marca **KRAKEN**.
- **Migración de Datos**:
  - `PathService` ahora usa `%AppData%\KrakenLauncher`.
  - Se implementó una rutina de migración que renombra automáticamente la carpeta vieja `NebulaLauncher` a `KrakenLauncher` si existe, evitando que los usuarios pierdan sus perfiles o sesiones.
- **Funcionalidad Heredada**:
  - Mantiene las mejoras de la v2.7.7: Java optimizado (Adoptium) y sistema de instalación limpia para perfiles nuevos.

### Archivos principales tocados

- `KrakenLauncher.csproj`: Renombrado y actualizado (Version, Product, Company).
- `PathService.cs`: Cambio de ruta base y lógica de migración.
- `App.xaml.cs` & `MainWindow.xaml.cs`: Actualización de namespaces y literales de texto (Identity Fix).
- `rename_to_kraken.py`: Script de utilidad creado para el procesamiento masivo.

## Validacion realizada

- Build `Release` del nuevo `.csproj` exitoso.
- Publicación: Release `v2.7.8` creada en GitHub con el asset `KrakenLauncher.exe`.
- Repositorio: Los cambios han sido pusheados con el nuevo esquema de nombres.
