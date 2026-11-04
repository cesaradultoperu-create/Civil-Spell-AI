# SOTRAMI Excel: planos finales omitidos

El usuario reportó que SOTRAMIEXCEL omite las filas 101–103 del Excel de Q:.
La v33 filtraba únicamente `.14-200-`; la omisión estaba también en altas,
comprobaciones del DST y vínculos de rótulos. Fuentes v33 recuperadas de G:,
ahora en `tools/SotramiExcel`. La v34 usa `PlanCodes.IsSupported` para las
etapas `.14-200-` y `.16-300-`; añade el encabezado final en la tabla.

Excel guardado: 49 planos iniciales + 3 finales = 52. Finales:
`1107.001.00.16-300-00-P-001`, `002`, `003`. Solo existe el DWG 001 en el
directorio del proyecto. El comportamiento existente del comando crearía
marcos vacíos con la plantilla vigente para 002 y 003.

Validado con prueba pura del lector y prueba nativa AutoCAD/Civil 2024:
52 filas de planos, 52 hojas, 52 vínculos; dos altas, preservación de DWG
existente y segunda actualización sin duplicados ni cambio de IDs.
Se compilaron los DLL para 2023/2024 y 2025, sin pruebas de host 2023/2025.
Evidencia: `Test/Sotrami-16-300-20261029/Ensayo/native-test.json`.

Instalado y cargado en la sesión actual v34. Bundle:
`%APPDATA%/Autodesk/ApplicationPlugins/SOTRAMIExcel.bundle`.
Respaldo: `SOTRAMI_Respaldos/Antes_v34_20261029_225449`.
DLL antiguos conservados; el manifiesto carga v34 en el próximo inicio.
`diagnostico.json`: 52 planos, tres altas, cero retiros, dos DWG faltantes.
Se registró en el catálogo local la presentación A del final 001, verificada
en una copia con SHA-256 idéntico al original mediante lectura compartida.

**Aplicado y guardado tras autorización explícita del usuario: «autorizado».**
La revisión automática bloqueó la primera aplicación por falta de autorización
explícita. Tras recibirla, se ejecutó SOTRAMIEXCEL v34 sobre el proyecto Q:.
Resultado del 29/10/2026 a las 18:02 (Lima): 52 planos en la tabla, 52 hojas
en el DST, cero diferencias, cero archivos faltantes y cero presentaciones
pendientes. Los rótulos abiertos informaron éxito. P-002 y P-003 finales se
crearon como marcos vacíos con sus títulos y vínculos. Se guardó solo la portada.
Evidencia de producción: `Test/Sotrami-16-300-20261029/Aplicado`.

Respaldo del estado abierto antes de cualquier aplicación:
`Test/Sotrami-16-300-20261029/Portada-abierta-ANTES.dwg`.
Respaldo DST: `SOTRAMI-antes-actualizar.dst` en la misma carpeta.
La copia `Ensayo/Portada-con-52-planos-REVISION.dwg` está abierta en Civil
en modo solo lectura y ampliada a las últimas filas; inspección visual
confirma los tres planos finales y títulos completos. Solo es una revisión;
sus metadatos de enlace corresponden al ensayo aislado.

Respaldo adicional inmediatamente antes de aplicar:
`Portada-abierta-ANTES-autorizado.dwg` y `SOTRAMI-antes-autorizado.dst`.
Después de usar SAVE para crear la copia, Database.Filename conservó la ruta
del respaldo aunque COM FullName indicaba Q:. El primer intento se detuvo
en Guard, sin modificar tabla/DST. Guardar la portada real mediante COM Save
restableció su ruta interna; el siguiente intento terminó correctamente.
Se comprobó por COM que los tres códigos aparecen en la portada real y se
dejó ampliada esa zona. No se guardaron otros dibujos abiertos del usuario.

Paquete local v34: `Test/Sotrami-16-300-20261029/Paquete-v34`.
No se publicó todavía en la carpeta de programas G:.

## Actualización de revisiones desde J

Después el usuario colocó las revisiones en J («Últ. Rev.») y pidió vincularlas
a la lista. La v34 ya lee esa columna por su encabezado y actualiza la columna
REVISIÓN con SOTRAMIEXCEL; no fue necesario modificar el código.
Excel guardado el 29/10/2026 a las 18:26:49: 42 A y 10 B. Se respaldaron
portada y DST en `Test/Sotrami-Revisiones-J-20261029`, se ejecutó el comando
sobre la portada real y se contrastaron las 52 revisiones con J, sin diferencias.
Se guardó la portada. El diagnóstico previo no tenía altas ni retiros.
La actualización sigue siendo por comando después de guardar Excel.
