# Validación de ayuda angular para MLeader

## Objetivo

Confirmar en una copia desechable que el pinzamiento de la flecha de un
`MLeader` nativo gira alrededor del vértice del lado del texto en intervalos de
15°, conserva la longitud del tramo y no deja geometría auxiliar ni cambios de
configuración.

## Preparación

1. Cargar la DLL correspondiente a la versión de Civil 3D mediante el bundle o
   `NETLOAD`.
2. Abrir un dibujo desechable y crear dos MLeader: uno con el texto a la derecha
   y otro con el texto a la izquierda.
3. Anotar los valores de `AUTOSNAP` y `ORTHOMODE`.
4. Medir o anotar la longitud del tramo entre la flecha y el último vértice de
   cada línea directriz.

## Casos

| Caso | Acción | Resultado esperado |
|---|---|---|
| MLP-01 | Seleccionar un MLeader y mover el pinzamiento de la flecha. | Aparecen 24 guías radiales transitorias; la guía activa y el tooltip avanzan en múltiplos de 15°. |
| MLP-02 | Aceptar posiciones cercanas a 15°, 30°, 90°, 180° y 330°. | La flecha termina en el múltiplo de 15° más cercano y la longitud original del tramo se conserva dentro de la tolerancia del dibujo. |
| MLP-03 | Durante el mismo arrastre, cruzar lentamente el centro del texto hacia ambos lados. | Las guías saltan entre las conexiones nativas B y C justo al cruzar el centro, mantienen el mismo radio y no desplazan el texto. |
| MLP-04 | Iniciar el arrastre y pulsar `Esc`. | La geometría queda idéntica al original; `AUTOSNAP` y `ORTHOMODE` recuperan sus valores previos. |
| MLP-05 | Aceptar el arrastre y ejecutar `U` inmediatamente. | Una sola orden restaura el MLeader original. |
| MLP-06 | Mover otro pinzamiento del MLeader y un pinzamiento de otra entidad. | La ayuda angular no interviene y el comportamiento nativo se conserva. |
| MLP-07 | Repetir con un MLeader de varias líneas directrices. | Solo responde la flecha activa; las demás líneas no cambian. |

## Comprobaciones de cierre

- No deben existir líneas, rayos, círculos ni otros objetos creados por las
  guías visuales.
- `AUTOSNAP` y `ORTHOMODE` deben coincidir exactamente con los valores anotados,
  incluso después de cancelar.
- No debe aparecer una excepción administrada ni quedar bloqueado el cursor.
- Registrar versión de Civil 3D, versión de la DLL y resultado de cada caso.

La lógica independiente de AutoCAD se valida en la suite autocontenida: ajuste
al múltiplo más cercano, normalización a 0–360°, conservación del radio y
reconocimiento exclusivo del pinzamiento de flecha. La configuración temporal
se aplica recién después de esa coincidencia, de modo que los demás
pinzamientos conservan el flujo nativo.

## Resultado interactivo 2026-09-17 — Civil 3D 2024

- `MLP-01`: PASS; aparecieron 24 guías rojas, guía activa y tooltip angular.
- `MLP-03`: PASS; al cruzar el centro, las guías cambiaron entre las conexiones
  B y C conservando el radio.
- `MLP-04`: PASS; `Esc` devolvió el MLeader exactamente a su posición original.
- `MLP-05`: PASS; tras aceptar el giro, una sola `U` inmediata restauró el
  original.
- Carga automática: PASS tras corregir el manifiesto a
  `LoadOnAutoCADStartup="True"`; funcionó en el primer arrastre después de
  reiniciar, sin invocar `AISPELLSETTINGS` ni usar `NETLOAD`.
- `MLP-02`, `MLP-06` y `MLP-07`: pendientes de completar en toda su matriz.
