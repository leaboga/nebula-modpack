# KRAKEN Launcher Development Handoff

## Estado actual

- Version publicada mas reciente: `3.0.3`
- Fecha de referencia: `2026-05-01`
- Reglas obligatorias de publicacion: `docs/RELEASE_RULES.md`

## Resumen ejecutivo

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
