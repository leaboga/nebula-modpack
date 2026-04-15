# KRAKEN Launcher Development Handoff

## Estado actual

- Version publicada mas reciente: `2.7.4`
- Fecha de referencia: `2026-04-14`
- Reglas obligatorias de publicacion: `docs/RELEASE_RULES.md`

## Resumen ejecutivo

Se ha implementado el sistema de **Configuración Oficial Vinculada (Config Sync v2)**. Este sistema permite a los administradores (Pepita/Leandro) publicar configuraciones recomendadas directamente desde el launcher hacia GitHub, y a los usuarios recibirlas y aplicarlas de forma opcional y no intrusiva.

## Version 2.7.4

### Cambios principales

- **Sistema de Configuración Oficial (Cloud Sync)**:
  - **Publicación Protegida**: Nueva ventana de login (`AdminLoginWindow`) para proteger la publicación de configs con contraseña.
  - **Versionado Independiente**: Las configs ahora tienen su propia versión (`ConfigVersion` en el manifest) separada de la versión del launcher.
  - **Incremento Automático**: Al publicar, el launcher detecta la versión actual y propone el incremento a la siguiente (v1 -> v2).
  - **Detección Inteligente**: El launcher verifica al inicio si existe una `ConfigVersion` más reciente que la aplicada por el usuario para ese perfil.
  - **Popup No Intrusivo**: Si hay una actualización, se muestra un aviso. Si el usuario elige "No", la versión se marca como rechazada y no vuelve a aparecer el popup hasta que se publique una versión NUEVA.
  - **Botón de Aplicación Manual**: El usuario puede aplicar la config oficial en cualquier momento desde el panel de Ajustes.
  - **Backup Automático**: Se realiza un backup previo en la carpeta `backups` de la instancia antes de aplicar la configuración oficial.
- **Modelos de Datos**:
  - `UserSession` ahora persiste `AppliedConfigVersions` y `RejectedConfigVersions` mapeadas por ID de perfil.
  - `ModManifest` incluye el campo `ConfigVersion`.
- **Infraestructura**:
  - Integración con `gh api` para actualizar el archivo `manifest.json` remoto directamente desde el código tras subir los assets.

### Archivos principales tocados

- `Models.cs`: Persistencia de versiones aplicadas/rechazadas.
- `ModSyncer.cs`: Soporte para `ConfigVersion` en manifiesto.
- `MainWindow.xaml.cs`: Lógica de detección de updates al cargar versiones y aplicación automática/manual.
- `Modules/ConfigView.xaml.cs`: Panel de administración protegido y gestión de estados de config oficial.
- `AdminLoginWindow.xaml/.cs`: Interfaz de acceso protegido.
- `NebulaLauncher.csproj`: Bump de versión a `2.7.4`.

## Validacion realizada

- Build `Release` exitoso.
- Verificación de Publicación: Se probó el login protegido y la subida de assets + actualización de manifest en GitHub.
- Verificación de Detección: Al simular una versión de config superior en el manifest, el launcher muestra el aviso correctamente.
- Verificación de Rechazo: Tras pulsar "No", el aviso no se repite al reiniciar. Al publicar una v+1, el aviso vuelve a aparecer.

## Reglas operativas importantes

Antes de cualquier release o cambio que deba llegar por auto-update:

1. leer `docs/RELEASE_RULES.md`
2. subir version respecto de la ya publicada
3. compilar limpio
4. verificar `ProductVersion` y `FileVersion`
5. publicar release real con el asset correcto
6. actualizar este handoff

## Riesgos pendientes

- La clave de administración está hardcodeada (`pepita2026`) para este despliegue. En futuras versiones se recomienda moverla a una validación vía API o hash remoto.

## Proximos pasos sugeridos

- Notificaciones push más visuales para cambios de configuración.
- Historial de versiones de configuración para permitir "rollback" a una configuración oficial anterior.
