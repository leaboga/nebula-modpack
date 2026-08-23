# KRAKEN Launcher Development Handoff

## Estado actual

- Version publicada mas reciente: `3.2.3`
- Fecha de referencia: `2026-05-29`
- Reglas obligatorias de publicacion: `docs/RELEASE_RULES.md`

## Resumen ejecutivo

## Version 3.2.3 (Sincronización de servidor)

### Cambios principales

- `MainWindow.xaml.cs`
  - Los perfiles con `SyncWithServer` ahora seleccionan siempre la versión más reciente del índice remoto al iniciar.
  - Al cargar el manifiesto, se actualizan Minecraft, loader y versión de NeoForge con los valores del servidor antes de sincronizar mods.

### Validación

- La release `v3.2.3` debe incluir `KrakenLauncher.exe` con versión interna `3.2.3`.

## Version 3.2.2 (Explorador de archivos)

### Cambios principales

- `Modules/ServerHostView.xaml`
  - Se agregó un botón `📂` junto a la ruta del servidor para abrirla directamente.
  - El botón `...` queda reservado exclusivamente para elegir otra carpeta del servidor.
- `Modules/ServerHostView.xaml.cs`
  - El nuevo acceso usa `explorer.exe` con `UseShellExecute`, abriendo el Explorador de archivos original de Windows y mostrando los archivos `.jar`.

### Validación

- La release `v3.2.2` debe incluir el asset `KrakenLauncher.exe` y la misma versión interna del binario.

En esta pasada se cerro una mezcla peligrosa entre dos sistemas de sync de configs:

- un flujo viejo basado solo en hash remoto
- un flujo nuevo basado en `ConfigVersion`

Eso provocaba comportamientos inconsistentes como:

- pedir nombres manuales para revisiones locales
- no volver a notificar una config oficial nueva despues de un rechazo previo
- mantener estados cruzados entre arranque, pantalla de config y primera instalacion

Tambien se hizo una ronda de limpieza de UX y rendimiento:

- navegacion con cache de hubs para no recrear vistas pesadas
- detencion correcta de modulos activos al cambiar de tab
- fondo/particulas mas livianos
- estilos globales mas suaves y ordenados

## Version 3.2.1 (Server Restart Reliability)

### Cambios principales

- `Modules/ServerHostView.xaml`
  - Se agregó el botón `↻` junto a `Control de Ejecución` para reiniciar el servidor.
- `Modules/ServerHostView.xaml.cs`
  - El reinicio espera la salida real del proceso antes de iniciar uno nuevo.
  - El apagado conserva una referencia al proceso detenido, evitando que el timeout mate un proceso iniciado después.
  - El auto-reinicio solo actúa ante caídas no solicitadas; una detención manual ya no dispara un reinicio inesperado.

### Validación

- Compilación `Release` y publicación autocontenida `win-x64` completadas correctamente.
- El ejecutable validado queda en `bin/Release/net8.0-windows/win-x64/publish/KrakenLauncher.exe`.
- Release publicada: `v3.2.1`, con asset `KrakenLauncher.exe`.
- La comprobación automática usa la release más reciente y compara versiones semánticas; una instalación `3.2.0` detecta `3.2.1` y una `3.2.1` no vuelve a descargarla.

## Version 3.1.8 (System Optimizations & Performance)

### Cambios principales

- `Modules/BlueMapView.xaml.cs` & `Modules/HubView.xaml.cs`
  - Implementación de `.Dispose()` para el control WebView2 del Mapa Abisal, eliminando fugas de memoria críticas al cambiar de vista.
- `MainWindow.xaml`
  - Implementación de `<Canvas.CacheMode><BitmapCache/></Canvas.CacheMode>` en el sistema de partículas y elementos estáticos pesados para descarga del CPU.
- `MinecraftLauncher.cs`
  - Integración por reflexión para anular `CheckHash` en `CmlLib.Core` al activar el Modo Turbo, reduciendo a segundos la validación local de assets.
- `MainWindow.xaml.cs` & `Services/GameLaunchManager.cs`
  - Desintegración de responsabilidad cruzada. La lógica de lanzamiento se extrajo completamente a `GameLaunchManager`.
- `Services/Logger.cs`
  - Nuevo servicio global persistente para registrar excepciones en `%AppData%/KrakenLauncher/kraken_debug.log`.

### Validacion realizada

- Compilación limpia local (0 errores, warnings obsoletos).
- Verificación del proceso de background de msedgewebview2.exe cerrándose correctamente.
- Auto-publish a GitHub mediante script.

## Version 3.1.7 (Versioning & UI Polish)

### Cambios principales

- `KrakenLauncher.csproj`, `app.manifest`, `KrakenSetup.iss`
  - Bump de versión a `3.1.7`.
- `MainWindow.xaml.cs`
  - Implementación de lógica dinámica para el selector de versiones del perfil.
- `Modules/ConfigView.xaml`
  - Se ocultó el panel de configuración de instancias por estar obsoleto.
- `changelog.json`
  - Actualizado con las novedades de la versión.

### Validacion realizada

- Build `Release` exitoso.
- Verificación de consistencia de versiones en todos los archivos de manifiesto.
- Creación de release en GitHub con el binario compilado.


## Version 3.0.4 (Startup Resource Hotfix)

### Cambios principales

- `Themes/Styles.xaml`
  - se restauraron recursos base que la UI ya estaba consumiendo pero no existian en el diccionario:
    - `BodyFont`
    - `DisplayFont`
    - `MonoFont`
    - `BoolToVis`
    - `GlowColor`
    - `AppleSlider`
- esto corrige el `XamlParseException` de arranque relacionado con `StaticResourceExtension`
- `KrakenLauncher.csproj`, `app.manifest`, `KrakenSetup.iss`
  - bump a `3.0.4`

### Validacion realizada

- Build `Release` exitoso tras restaurar los recursos faltantes
- Se verifico que las keys consumidas por la UI de arranque ahora existen en `Themes/Styles.xaml`

## Version 3.0.3 (Updater Hardening)

### Cambios principales

- `MainWindow.xaml.cs`
  - el updater deja de depender de un `.bat` fragil con `copy /Y`
  - ahora genera un script de PowerShell temporal que:
    - espera explicitamente el cierre del PID original
    - intenta reemplazar el binario varias veces
    - escribe evidencia en `updater.log`
    - relanza `KrakenLauncher.exe` despues de copiar
  - la descarga usa el nombre real del asset remoto, no solo el nombre del ejecutable actual
- `KrakenLauncher.csproj`
  - bump a `3.0.3`
- `app.manifest`
  - version alineada a `3.0.3.0`
- `KrakenSetup.iss`
  - version de instalador alineada a `3.0.3`

### Validacion realizada

- Build `Release` exitoso
- Publish `win-x64` self-contained exitoso
- Binario final verificado:
  - `KrakenLauncher.exe`
  - `FileVersion 3.0.3.0`
  - `ProductVersion 3.0.3+<commit>`

### Riesgo que corrige

- Casos donde el launcher detectaba la nueva version, se cerraba, pero no lograba reemplazar el `.exe` correctamente y al abrir de nuevo seguia en la version anterior.

## Version 3.0.2 (Configs & Performance Hardening)

### Cambios principales

- `MainWindow.xaml.cs`
  - chequeo y aplicado de config oficial unificados con `OfficialConfigInfo`
  - instalacion nueva actualiza `AppliedConfigVersions` y limpia `RejectedConfigVersions`
  - el cambio de perfil invalida cache de navegacion para no arrastrar estados viejos
  - el cierre del launcher detiene particulas y modulos activos
- `ModSyncer.cs`
  - `config-hash.json` pasa a representar metadata oficial completa:
    - `Hash`
    - `ConfigVersion`
    - `RecommendedRam`
    - `PublishedAt`
    - `PublishedBy`
  - publicar configs incrementa la revision automaticamente
- `PresetService.cs`
  - snapshots numerados con `VersionNumber`
  - ya no hace falta escribir nombres manuales para guardar una revision local
- `Modules/ConfigView.xaml`
  - copy mas clara para revisiones locales y config oficial
  - panel de usuario y admin mas visibles
- `Modules/ConfigView.xaml.cs`
  - estado remoto/local/rechazado trabajado con `ConfigVersion`
  - aplicacion de config oficial y re-aplicado sincronizados con la sesion
- `Services/NavigationService.cs`
  - hubs cacheados para `Sistemas`, `Recursos` y `Red`
  - nombres de tabs mas claros para uso real
- `Modules/HubView.xaml.cs`
  - al cambiar de tab se detienen vistas activas con timers o polling
- `Services/EffectService.cs`
  - particulas con timer liviano en vez de render loop constante
  - imagenes de fondo cargadas con decode optimizado
- `Themes/Styles.xaml`
  - bordes, fondos, sombras y tabs suavizados para un look mas limpio
- `KrakenLauncher.csproj`
  - bump a `3.0.2`
  - `AssemblyName` alineado a `KrakenLauncher` para que publish y updater usen el mismo ejecutable
- `app.manifest`
  - version alineada a `3.0.2.0`
- `KrakenSetup.iss`
  - version de instalador alineada a `3.0.2`

### Validacion realizada

- Build `Release` exitoso:
  - `dotnet build KrakenLauncher.csproj -c Release /p:DisableImplicitNuGetFallbackFolder=true`
- Se verifico que no quedan referencias activas a:
  - `ObtenerHashConfigsRemoto`
  - `RunGhApiUpdateConfigVersion`
  - `NewPresetNameBox`
- El ejecutable vuelve a quedar previsto como `KrakenLauncher.exe`

### Riesgos pendientes

- Queda texto viejo con encoding roto en varias zonas del proyecto; no bloquea build, pero conviene una pasada dedicada de copy/encoding.
- Siguen warnings heredados de paquetes y nulabilidad. No rompen la release, pero hay deuda tecnica para bajar.
- Conviene probar en runtime el caso exacto:
  - usuario rechaza config oficial `vN`
  - Pepa publica `vN+1`
  - el popup vuelve a aparecer solo para la revision nueva

### Proximos pasos sugeridos

1. Validar en una PC cliente el flujo completo de config oficial con rechazo y nueva revision.
2. Publicar limpieza de encoding/copy en una release separada.
3. Seguir moviendo logica pesada fuera de `MainWindow.xaml.cs`.
