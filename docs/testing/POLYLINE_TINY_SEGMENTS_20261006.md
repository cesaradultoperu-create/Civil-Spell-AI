# Tramos diminutos en !CORTE.dwg — 2026-10-06

El usuario exige revisar todo el modelo. El cuadro rojo es un caso de prueba,
no el alcance de la reparación. El ejemplo incluye un tramo dimensionado
aproximadamente como 0.0013 unidades. La captura se corresponde con la entidad
5008F: cerrada, Z=0, 179 vértices, longitud 30.549695 y área 12.327413.

Se copió sin modificar el archivo guardado a las 13:39:08 desde
`Q:\Proyectos 2026\1107.001.00.16 - Pad SOTRAMI\03_Designs\!CORTE.dwg`.
La copia y todas las evidencias iniciales están en
`artifacts/validation/overlap-corte-20261006`.
SHA256: `9EDA70E5EE799052695249020D182A96575ED44E3B398B32B03C1454DF04EB18`.

La tolerancia anterior de 1e-6 no detectaba como diminutos los segmentos de
0.0001586235 (dentro del cuadro 5008D) y 0.0013118928 de 5008F. No eran
solapes exactos. La modificación separa el umbral de longitudes diminutas del
umbral de solapes y de simplificación colineal. En unidades comunes declaradas
se propone 1.5 mm; en unidades no reconocidas se mantiene la tolerancia previa.
El menú `Tramos` permite cambiarlo. La confirmación de limpieza muestra el
límite y avisa del pequeño cambio de forma. La inserción en metros no prueba
por sí sola la escala real de toda la geometría; el usuario debe comprobarla.

`SafeCleanup` retiene los índices originales y valida todos los puntos retirados
contra cada cuerda final. Rechaza desplazamientos acumulados que excedan el
límite después de combinar colapso y simplificación. Mantiene extremos abiertos,
cierre, normal y elevación; sigue protegiendo arcos, anchos y objetos vinculados.
Los segmentos diminutos mayores que la tolerancia de solape también participan
en la comparación de coincidencias. El historial distingue ambos umbrales.

Resultado global de la prueba real final:

| Magnitud | Antes | Después |
| --- | ---: | ---: |
| Entidades LWPOLYLINE en Model | 571 | 571 |
| Tramos <= 0.0015 | 128 | 0 |
| Vértices de 5008F | 179 | 175 |
| Pares superpuestos a 1e-6 en 3D | 0 | 0 |

Se ajustaron cinco entidades y se retiraron 125 vértices. Máximo desplazamiento
global: 0.0014702461; máximo de 5008F: 0.0001851132. Se retiraron sus vértices
originales 13, 16, 18 y 26 (numeración desde 1). La segunda pasada no modifica
ninguna entidad. La restauración devuelve exactamente todos los vértices
originales. La comparación independiente coincide en todos los conteos y no
encuentra cruces interiores nuevos en las cinco entidades modificadas.

Se conservan exactamente los datos DXF en memoria de las entidades no
LWPOLYLINE, incluidos vértices de POLYLINE 3D. La exportación de primitivas
antes y después de guardar/reabrir es idéntica byte por byte. El modelo sigue
conteniendo 798 entidades: 571 LWPOLYLINE, 216 LINE, dos POLYLINE 3D, seis
AECC_TIN_SURFACE y tres ACAD_PROXY_ENTITY.

La copia corregida se guardó antes de restaurar en memoria. Se reabrió con la
DLL Release final y dio 175 vértices y cero tramos diminutos. Ruta para revisar:
`artifacts/validation/overlap-corte-20261006/CORTE-tramos-corregidos-v1_3_3_2.dwg`.
La imagen `comparacion-cuadro-antes-despues.png` muestra la zona correcta del
cuadro, junto con el microtramo medido. La imagen inicial `quiebres-5008F.png`
ilustra otro sector de esa entidad; no identifica el cuadro rojo.

Evidencias:

- Prueba real: `artifacts/validation/overlap-corte-tiny-936cf5d4fb7a44aa84bfcdccb3d2b981/console.log` (10 controles).
- Debug final: `artifacts/validation/overlap-correction-9b3293eaf7fc432f88edba82a4b849a3/console.log` (15 controles).
- Release final: `artifacts/validation/overlap-correction-e1838eb2ba8d4f9c81e6bb082f00f5f8/console.log` (12 controles).
- `artifacts/validation/overlap-corte-20261006/reopen.log`, `cleanup-comparison.json`, `primitives*.csv`.
- El proyecto completo 2024 compila en Release sin errores ni advertencias;
  su versión 1.3.6.0 no se cambió ni se empaquetaron sus herramientas ajenas.

Reproducción sobre la copia local:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-CorteTinyCleanup.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-OverlapCorrection.ps1 -Target2023 -Configuration Debug
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-OverlapCorrection.ps1 -Target2023 -Configuration Release
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Build-Overlap2023Bundle.ps1
```

Entrega independiente 1.3.3.2:
`artifacts/entrega-tramos-dwg/CivilSpellAI-TramosDWG-1.3.3.2.zip`.
SDK 2023 (AutoCAD.NET 24.2.0), .NET Framework 4.8/x64, manifiesto Civil3D
R24.2–R24.3. AssemblyName `CivilSpellAI.TramosDWG`, bundle y claves de respaldo
independientes. Comando `REVISARTRAMOSDWG` y auxiliares propios para evitar que
una instalación anterior ejecute los comandos homónimos. El instalador
administrado se reutiliza con copia verificada y respaldo propio. El ZIP
contiene cinco archivos, sin DLL de Autodesk; la DLL coincide con la Release
probada. No se instaló sobre el equipo del usuario mientras Civil 3D estaba
abierto. El ZIP incluye `INSTALAR.cmd` y `LEEME.txt`.

Límites: todas las ejecuciones se realizaron en Core Console 2024. Falta
validar ejecución y autocarga en Civil 3D 2023 real, y autocarga del nuevo
bundle en la interfaz de Civil 3D 2024. El flujo no repara cualquier esquina
ni objetos Civil, LINE, POLYLINE antiguas/3D, bloques o XREF. El contorno 3D
49B99 comparte siete subtramos en proyección XY con 5008F, pero está cerca de
Z=2447 y la entidad plana en Z=0. No se recortó ese límite de superficie.
