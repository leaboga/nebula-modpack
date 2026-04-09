## Prompt inicial para Gemini Fast

Quiero que trabajes directamente sobre este repositorio WPF/C# y hagas un rediseño total del launcher, con criterio de producto real, no un cambio cosmético menor.

### Objetivo principal

Transformá por completo la identidad visual del proyecto actual `NebulaLauncher` hacia una marca que represente algo masivo, brutal, dominante y con presencia. Podés proponer y aplicar un nuevo nombre si mejora el resultado. Algunas referencias válidas son nombres tipo `Kraken`, pero no te limites si encontrás una alternativa mejor dentro de esa línea semántica.

El cambio tiene que sentirse como un relanzamiento completo del launcher.

### Contexto del repo actual

Este proyecto es un launcher de Minecraft hecho en WPF con `.NET 8` y `UseWPF=true`.

Archivos y zonas relevantes:

- `MainWindow.xaml`: ventana principal muy grande y actualmente muy acoplada a la UI.
- `MainWindow.xaml.cs`: lógica principal del launcher.
- `Themes/Styles.xaml`: design system actual, hoy muy ligado a Nebula y paleta violeta.
- `App.xaml`: recursos globales y tray icon.
- `Modules/*.xaml`: pantallas/módulos existentes como changelog, rendimiento, screenshots, social, mod manager, crash diagnostics, hosting, modpacks, etc.
- `Services/*.cs`: helpers y servicios reutilizados.
- `NebulaLauncher.csproj`: metadata del producto, versión, autor, icono y branding.

### Lo que quiero que hagas

1. Auditá la estructura actual antes de tocar nada.
2. Rediseñá la UI de manera radical.
3. Rebrandá el proyecto para abandonar la identidad `Nebula` si el nuevo concepto lo requiere.
4. Conservá la funcionalidad existente. El foco es cambiar la experiencia visual, arquitectura visual y branding, no romper features ya implementadas.
5. Si hace falta, reorganizá la UI para que quede más mantenible.

### Dirección creativa

No quiero una UI genérica, violeta, “espacial” estándar ni un launcher que parezca otro clon más.

Busco algo:

- masivo
- agresivo pero elegante
- premium
- oscuro, industrial, oceánico, titánico o monstruoso
- con identidad propia
- moderno y muy distinto al Nebula actual

Podés usar una dirección tipo:

- `Kraken`
- leviatán
- coloso
- abyssal / deep sea / iron fleet
- titan tech

Pero elegí la mejor opción según el resultado visual global.

### Requisitos de diseño

- Cambiá por completo la paleta, tipografía visual, jerarquía, layout y componentes.
- Evitá depender del look violeta actual.
- Definí tokens visuales nuevos en el sistema de estilos.
- Mejorá el uso de espacios, contraste, densidad de información y foco visual.
- Hacé que el launcher se vea intencional y profesional en desktop.
- No uses una estética “AI slop” ni paneles genéricos sin carácter.
- Si mantenés animaciones, que sean pocas pero con propósito.
- El resultado tiene que verse coherente tanto en la ventana principal como en los módulos.

### Requisitos técnicos

- Mantené WPF y la base del proyecto actual.
- Priorizá refactor visual por sobre reescritura funcional innecesaria.
- Si `MainWindow.xaml` está demasiado acoplado, podés reestructurarlo, extraer secciones o simplificarlo, siempre que preserves comportamiento.
- Centralizá colores, brushes, estilos y componentes reutilizables en `Themes/Styles.xaml` o en los diccionarios que creas necesarios.
- Actualizá textos visibles, títulos, nombre de app, tooltip de tray, metadata del `.csproj` y branding asociado.
- Si renombrar archivos físicos genera demasiado riesgo técnico, podés mantener nombres internos como `NebulaLauncher` donde sea prudente, pero el branding visible al usuario final debe quedar actualizado.
- Revisá posibles textos corruptos/encoding raro visibles en XAML y corregilos.

### Entregables esperados

Quiero que dejes:

1. El rediseño completo aplicado en el código.
2. Un nombre final propuesto y usado en la UI, salvo que justifiques mantener otro.
3. Un breve archivo de handoff para próximos chats con:
   - qué cambiaste
   - qué branding elegiste
   - qué partes quedaron pendientes
   - cómo seguir iterando sin romper la nueva dirección visual
4. Instrucciones simples para futuros chats sobre cómo subir cambios a GitHub.

### Archivo extra obligatorio

Creá un archivo tipo `AI_HANDOFF.md` o `docs/AI_HANDOFF.md` con:

- resumen del rebranding
- decisiones visuales
- archivos principales tocados
- deuda pendiente
- pasos para validar cambios
- pasos para publicar futuros cambios

En la sección de publicar futuros cambios, dejá una guía corta tipo:

- revisar `git status`
- commit claro
- push a la rama correspondiente
- verificar que GitHub actualice automáticamente

### GitHub

Cuando termines:

- hacé commit de los cambios
- subilos a GitHub en la rama correcta del repo
- dejá todo listo para que el flujo automático del proyecto siga funcionando

Si detectás que hace falta confirmar rama o remoto, inspeccionalo dentro del repo y actuá con criterio.

### Restricciones

- No elimines features existentes solo para simplificar la UI.
- No rompas login, update flow, mod manager, changelog, rendimiento, screenshots, social, hosting o modpacks sin justificarlo y resolverlo.
- No hagas un parche superficial. Quiero una transformación real.

### Orden de trabajo sugerido

1. Auditar estructura y branding actual.
2. Elegir concepto de marca nuevo.
3. Redefinir sistema visual.
4. Rehacer `MainWindow` y adaptar módulos clave.
5. Corregir textos, labels, metadata e identidad.
6. Validar compilación.
7. Commit, push y handoff documentado.

### Resultado esperado

Quiero abrir el launcher y sentir que ya no es Nebula maquillado, sino otro producto, mucho más fuerte, memorable y serio.
