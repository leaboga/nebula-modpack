# KRAKEN Launcher Development Handoff

## Estado actual

- Version publicada mas reciente: `2.6.8` (objetivo de esta tanda)
- Fecha de referencia: `2026-04-09`
- Release rules obligatorias: `docs/RELEASE_RULES.md`

## Resumen reciente

La base del launcher fue reforzada en varias capas:

- `PathService` centraliza rutas operativas del launcher.
- `LoggerService` centraliza logs persistentes y reduce dependencia de logging local ad hoc.
- `KrakenStrings` centraliza parte del copy visible para mejorar consistencia y facilitar mantenimiento.
- El sistema de updates/publicacion fue endurecido para evitar loops e inconsistencias entre version local, binario publicado y release de GitHub.

## Version 2.6.5

Esta version se describe como una pasada de estabilizacion UX/UI enfocada en discovery y consistencia.

Cambios reportados:
- mejoras en Boveda / Mod Hub y Modpacks con tarjetas mas ricas en metadata
- estados de instalacion mas claros (`INSTALADO`, `DISPONIBLE`, etc.)
- mejor contexto visual del perfil activo (version, loader, ruta de instancia)
- mayor unificacion de headers, spacing y jerarquia de texto
- mejoras en feedback de carga

## Version 2.6.6

Esta sesion agrega una nueva pasada enfocada en mejoras funcionales sin duplicar features ya existentes:

- nuevo `DiscoveryStateService` para persistir favoritos y recientes en Mod Hub y Modpack Hub
- Mod Hub mejorado con:
  - filtro `Solo favoritos`
  - orden por favoritos y recientes
  - marcador visual de favoritos y recientes en cards
  - metadata de descargas mas visible
- Modpack Hub mejorado con:
  - filtro `Solo favoritos`
  - orden por descargas, favoritos, recientes y nombre
  - badges de favorito/reciente
  - mejor contexto del perfil activo en el header
  - estado vacio explicativo
- Capturas mejoradas con:
  - copiar ruta
  - eliminar captura desde el overlay
- Diagnostico mejorado con:
  - exportar reporte tecnico a archivo
  - abrir `launcher.log` desde la UI

## Version 2.6.8

Esta sesion corrige el puente de actualizacion entre instalaciones viejas y el binario renombrado:

- `AssemblyName` cambiado para generar `KrakenLauncher.exe`
- `app.manifest` actualizado a `KrakenLauncher.app`
- updater adaptado para reutilizar el nombre real del `.exe` actual
- auto-update reactivado para que una version anterior descargue y aplique la release nueva sin depender del click manual
- seleccion de asset endurecida para priorizar `KrakenLauncher.exe` sin romper compatibilidad con releases viejas
- release puente con asset principal `KrakenLauncher.exe` y asset de compatibilidad `NebulaLauncher.exe` para clientes anteriores
- icono de aplicacion y de escritorio actualizado a `kraken.ico`
- script de Inno Setup ajustado a `KRAKEN Launcher` y `KrakenLauncher.exe`

Validacion realizada:
- compilacion `Release` correcta a carpeta temporal separada (`temp_build_verify`) para no cerrar el launcher que estaba abierto
- el build normal a `bin\\Release` fallo solo por archivo bloqueado del ejecutable en uso, no por errores de codigo
- la siguiente release debe quedar por encima de `2.6.7` para que las instalaciones en `2.6.5` detecten un salto nuevo

## Reglas operativas obligatorias

Antes de cualquier tarea que implique release, update o cambios que deban llegar al usuario final por auto-update, revisar:

- [RELEASE_RULES.md](/C:/Users/Leandro/source/repos/NebulaLauncher/docs/RELEASE_RULES.md)

Punto critico agregado:
- si se hacen cambios nuevos que el usuario deba recibir en el launcher, es obligatorio subir una nueva version superior a la publicada
- no dejar cambios importantes sobre la misma version ya publicada

Ejemplo:
- si la version publicada actual es `2.6.4` y se hacen cambios nuevos, la siguiente release debe ser `2.6.5` o la que corresponda

## Version 2.6.9

Esta sesión se enfoca en una refactorización arquitectónica profunda y mejoras en la retroalimentación del sistema:

- **Refactorización Arquitectónica**: 
  - Se crearon servicios dedicados (`NavigationService`, `EffectService`, `NotificationService`) para reducir el tamaño y complejidad de `MainWindow.xaml.cs`.
  - La navegación entre módulos ahora está centralizada, eliminando el switch-case gigante del lanzador.
  - Los efectos visuales (partículas, temas, fondos) fueron desacoplados de la lógica de negocio.
- **Sistema de Notificaciones (Toasts)**:
  - Implementación de un sistema de notificaciones no intrusivas (`NotificationToast`).
  - Reemplazo de múltiples `MessageBox` tradicionales por avisos visuales modernos y animados.
- **Discovery Enhancements**:
  - `Modpack Hub` ahora detecta y marca modpacks instalados mediante `ModpackId`.
  - Sincronización automática del estado de instalación con el sistema de perfiles.
- **Limpieza de Deuda**: 
  - Eliminación de lógica de renderizado redundante en el hilo de la UI.
  - Mejora en la estructura del layout principal para soporte de capas de notificación.

Validación:
- Estructura de servicios verificada.
- Notificaciones integradas en `ConfigView`, `ModpackView` y `MainWindow`.

## Riesgos / deuda pendiente

- A pesar de la refactorización, `MainWindow.xaml.cs` aún conserva lógica de alto nivel que podría ser extraída (ej. lógica de login/auth).
- Ampliar el uso de `NotificationService` a todos los módulos restantes (`SocialView`, `ModManagerView`, etc).
- Optimizar la virtualización en el `VaultView` para catálogos muy extensos.

## Proximos pasos sugeridos

1. Migrar la lógica de autenticación (Premium/Offline) a un `AuthService` dedicado.
2. Implementar confirmaciones personalizadas mediante el sistema de toasts para unificar el lenguaje visual.
3. Revisar el rendimiento del Mod Hub en búsquedas con resultados masivos (>50 items).
4. Continuar la expansión de `KrakenStrings` para soportar multi-idioma en el futuro.
5. Mantener la consistencia de versiones en `.csproj` al publicar.

