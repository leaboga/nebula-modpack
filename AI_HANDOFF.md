# KRAKEN Launcher - AI Handoff

Este documento resume el proceso de rebranding y rediseño total aplicado al repositorio `NebulaLauncher`.

## Resumen del Rebranding

- **Nuevo Nombre**: **KRAKEN** (The Abyssal Titan).
- **Concepto**: Industrial, abisal, agresivo pero elegante. Abandono total de la temática espacial violeta.
- **Paleta de Colores**:
  - **Acento**: `#00F2FF` (Bioluminescent Cyan).
  - **Fondo Principal**: `#05080D` (Deep Abyssal Black).
  - **Superficies**: `#0D151D` y `#16212B` (Slate Industrial).
  - **Bordes**: `#1E2C38`.
- **Iconografía**: Uso de Squid/Kraken (🦑), Anclas (⚓) y barcos (⛴️) en lugar de galaxias y cohetes.

## Decisiones Visuales Clave

1. **Geometría Industrial**: Se redujeron los `CornerRadius` de 20-22px a 8-12px para dar un aspecto más sólido y profesional.
2. **Contraste Abisal**: La paleta se movió hacia azules profundos y cianes brillantes para simular la bioluminiscencia en las profundidades.
3. **Jerarquía de Textos**: Se actualizaron todos los labels para usar un tono más "militar/comando" (ej. "Comandante en el Deck", "Protocolo Leviatán").
4. **Fondo Dinámico**: Se actualizaron las URLs de Unsplash en `MainWindow.xaml.cs` para mostrar océanos profundos según la hora del día.

## Archivos Principales Tocados

- `Themes/Styles.xaml`: Reescritura total del sistema de tokens y pinceles.
- `MainWindow.xaml`: Rediseño de la estructura visual, logos y efectos de brillo.
- `MainWindow.xaml.cs`: Actualización de lógica de fondos, versiones (v2.6.3) y logs.
- `NebulaLauncher.csproj`: Actualización de metadatos del producto y autor.
- `App.xaml`: Cambio de tooltip en el tray icon.
- `Modules/*.xaml`: Aplicación global de la nueva identidad visual.

## Deuda Pendiente / Siguientes Pasos

1. **Icono del Ejecutable**: El archivo `nebula.ico` sigue siendo el icono físico del .exe. Sería ideal generar un `kraken.ico` para completar la transformación.
2. **Nombres de Archivos**: Internamente, muchos archivos y namespaces siguen llamándose `NebulaLauncher`.
3. **Optimización de Recursos**: Mejorar el manejo de memoria en el feed de Social si el tráfico aumenta.
4. **Validación de Assets**: Automatizar aún más la verificación de assets en el CI si se implementa.

## Reglas de Lanzamiento
Se ha establecido el archivo `docs/RELEASE_RULES.md` como ley fundamental para cualquier cambio que afecte la versión o la publicación del binario. **Cualquier agente debe leerlo antes de actuar.**

## ¿Cómo validar los cambios?

1. Abrir el proyecto en Visual Studio.
2. Compilar en modo `Debug` o `Release`.
3. Ejecutar y verificar que la ventana principal muestre "KRAKEN" y la paleta cian.
4. Navegar por los módulos (Modpack Hub, Blue Map, etc.) para asegurar que la paleta es consistente.

## Instrucciones para publicar en GitHub

Para subir estos cambios al repositorio:

```bash
# 1. Verificar estado
git status

# 2. Agregar cambios
git add .

# 3. Commit con mensaje claro
git commit -m "Rebranding radical: Nebula -> KRAKEN (Abyssal Theme v2.6.3)"

# 4. Push a la rama actual (ej. master/main)
git push origin master
```

## Publicaciones Recientes

- **v2.6.4 (Actual)**: [Hardening Arquitectónico](https://github.com/leaboga/nebula-modpack/releases/tag/v2.6.4)
  - Centralización total de rutas en `PathService`.
  - Implementación de `LoggerService` hilo-seguro para diagnóstico global.
  - Unificación visual de Headers y Márgenes en todos los módulos.
  - Lexicón centralizado en `KrakenStrings` para consistencia lingüística.
- **v2.6.3**: [Interfaz Refinada](https://github.com/leaboga/nebula-modpack/releases/tag/v2.6.3)
  - Pulido total de UX/UI y traducción completa a español.
  - Implementación de `RELEASE_RULES.md` para estandarizar despliegues.
  - Hardening de seguridad y metadatos de integridad.
- **v2.6.2**: Estabilización de núcleo y blindaje de versionado.
- **v2.6.1**: Hotfix de bucle de actualización.
- **v2.5.0**: Rebranding radical Nebula -> KRAKEN.
