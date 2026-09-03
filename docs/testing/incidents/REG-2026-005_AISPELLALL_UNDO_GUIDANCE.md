# REG-2026-005: instrucción ambigua de UNDO en AISPELLALL

- Fecha: 2026-09-03.
- Severidad: baja; la escritura y la reversión seguían siendo atómicas.
- Versión observada: 1.1.4.0.
- Versión corregida: 1.1.5.0.

## Síntoma

Después de aplicar un lote, `AISPELLALL` indicaba «Use UNDO». El comando
completo `UNDO` abre primero su solicitud de cantidad u opciones, por lo que la
instrucción no describía una acción inmediata y podía parecer que el lote no se
había revertido.

## Análisis y corrección

AutoCAD crea una marca de deshacer para cada comando .NET. `AISPELLALL` escribe
todo el lote dentro de una sola transacción y del mismo comando, por lo que una
sola orden `U` restaura todas las entidades. No se añadió un `UNDO Begin/End`
anidado: la prueba demostró que esa alternativa deja al usuario detenido ante
el inicio del grupo y rompe la operación inmediata.

El mensaje final dice ahora «Use U una vez para revertir todo el lote».

## Evidencia

`scripts/Test-BatchUndoIntegration.ps1` carga la DLL Debug en AutoCAD Core
Console sobre una copia temporal del fixture desechable, aplica dos cambios por
`ApplyBatch`, ejecuta una sola `U` y verifica ambos textos originales. La copia
se descarta y elimina incluso si la prueba falla. Resultado del 2026-09-03:
`PASS UNDO-04: ONE U RESTORED THE ENTIRE BATCH.`

La suite autocontenida permanece en 105/105 y Release excluye los comandos de
diagnóstico `AISPELLTESTBATCHUNDO` y `AISPELLTESTBATCHUNDOVERIFY`.
