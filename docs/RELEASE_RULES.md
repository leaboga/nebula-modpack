# RELEASE RULES

Estas reglas son obligatorias para cualquier IA o agente que trabaje en este repositorio y tenga que publicar cambios, releases o updates del launcher.

## Objetivo

Garantizar que cada update:
- tenga version correcta
- compile limpio
- se suba a GitHub correctamente
- no rompa el auto-update
- deje trazabilidad clara para proximos chats

## Regla 1: una sola fuente de verdad de version

Antes de publicar, revisar y mantener consistentes:
- `NebulaLauncher.csproj`
- `<Version>`
- `<AssemblyVersion>`
- `<FileVersion>`

No dejar numeros hardcodeados de version en XAML o en textos visibles.

## Regla 2: incremento correcto de version

Salvo que se indique otra cosa:
- usar incremento `patch`
- ejemplo: `2.6.2 -> 2.6.3`

No saltar de segmento incorrecto.
No convertir `x.y.z` en `x.z.y`.
No publicar sin bump de version cuando el cambio debe generar update.

## Regla 3: build/publish limpio obligatorio

Antes de subir una release:
1. compilar/publish limpio
2. verificar que el `.exe` final exista
3. verificar que el `.exe` final reporte internamente la misma version que el proyecto

No subir binarios viejos.
No reutilizar assets de releases anteriores.

## Regla 4: validar el binario real

Antes de publicar, comprobar:
- ruta del `.exe` generado
- `ProductVersion`
- `FileVersion`

La version interna del binario debe coincidir con la release/tag que se va a crear.

## Regla 5: publicacion real en GitHub

Una publicacion correcta incluye:
- commit de cambios
- push de la rama
- tag/release real en GitHub
- asset correcto del launcher adjunto a la release

No alcanza con modificar archivos localmente.
No alcanza con dejar solo commit y push sin release si el objetivo es update automatico.

## Regla 6: asset correcto

Al descargar o publicar updates:
- priorizar el asset real del launcher `.exe`
- no usar `assets[0]` ciegamente
- ignorar zips de source code, symbols u otros archivos auxiliares

## Regla 7: validar auto-update

Despues de publicar una nueva version:
- confirmar que una version local anterior detectaria la remota mayor
- confirmar que local == remota no dispara update
- confirmar que despues de actualizar no reaparece el mismo update en loop

## Regla 8: logs claros

Siempre dejar logs o resumen final con:
- version anterior
- nueva version
- tag publicado
- nombre del asset
- URL de release
- resultado de validacion del update

## Regla 9: handoff obligatorio

Despues de cambios importantes, actualizar `AI_HANDOFF.md` o archivo equivalente con:
- que se cambio
- version publicada
- problemas corregidos
- riesgos pendientes
- proximos pasos sugeridos

## Regla 10: no afirmar sin verificar

No decir "subido", "publicado", "release creada" o "update validado" sin evidencia real.
Siempre verificar el estado final antes de cerrar la tarea.
