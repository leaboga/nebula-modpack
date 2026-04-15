# KRAKEN Launcher Development Handoff

## Estado actual

- Version publicada mas reciente: `2.7.6`
- Fecha de referencia: `2026-04-14`
- Reglas obligatorias de publicacion: `docs/RELEASE_RULES.md`

## Resumen ejecutivo

Se han aplicado correcciones críticas en el motor de auto-actualización y el sistema de detección de versiones. Se detectó que el launcher tenía dificultades para actualizarse cuando existían múltiples copias del ejecutable en la carpeta de descargas o cuando el archivo estaba bloqueado por procesos persistentes.

## Version 2.7.6

### Cambios principales

- **Mejoras en Auto-Updater**:
  - **Target Inteligente**: El launcher ahora siempre intenta actualizar el binario que está *actualmente en ejecución*, independientemente de su nombre (ej: `KrakenLauncher (2).exe`).
  - **Script de Reemplazo Robusto**: Se mejoró el script `.bat` con reintentos más agresivos (10 intentos con esperas de 2s) y detección explícita de errores de copia.
  - **Logging Detallado**: Se añadieron registros claros en `updater.log` para cada intento de copia, incluyendo las rutas exactas de origen y destino con comillas para manejar espacios.
  - **Limpieza de Procesos**: El script ahora asegura el cierre forzado del PID actual antes de intentar el reemplazo.
- **Robustez en Detección de Versiones**:
  - `VersionManager` ahora prioriza `AssemblyInformationalVersion` y limpia automáticamente metadatos de SourceLink/Git (ej: `2.7.6+sha` -> `2.7.6`) para comparaciones semánticas precisas.
  - Se añadió una auditoría de integridad al inicio que loguea la verificación del núcleo.
- **Correcciones de UI**:
  - Se corrigieron problemas de encoding en algunos mensajes de log del sistema.

### Archivos principales tocados

- `MainWindow.xaml.cs`: Rediseño de `AplicarUpdateAsync` y mejora de `CheckForLauncherUpdate`.
- `Services/VersionManager.cs`: Mejora en `GetCurrentVersion`.
- `NebulaLauncher.csproj`: Bump de versión a `2.7.6`.

## Validacion realizada

- Build `Release` exitoso.
- Verificación de Logs: El sistema loguea correctamente el inicio de la verificación de integridad.
- Verificación de Updater: Se comprobó que el script `.bat` generado utiliza rutas absolutas y maneja correctamente los bloqueos de archivo mediante el bucle de reintentos.

## Reglas operativas importantes
... (resto igual)
