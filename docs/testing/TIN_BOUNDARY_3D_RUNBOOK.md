# BORDETIN3D

Versión 0.1.3, 2026-10-14. Comando para Civil 3D que genera polilíneas 3D cerradas a partir de los bordes de superficies TIN.

Archivo de prueba confirmado por el usuario: `Q:\Proyectos 2026\1107.001.00.16 - Pad SOTRAMI\05_Drawing\Xref\old\prueba estructura proyectada.dwg`. Fue movido de Xref a Xref/old durante la sesión. No se modifica el dibujo manual ni se guarda automáticamente sobre el original.

## Cargar en Civil 3D 2024

1. Ejecutar `NETLOAD`.
2. Abrir `Test/BordeTIN3D-20261014-v0.1.3/2024/CivilSpellAI.BordeTin3D.v013.dll`.
3. Ejecutar `BORDETIN3D` en Modelo.

La DLL es independiente del complemento instalado y puede cargarse con Civil 3D abierto. La actualización tiene un nombre de ensamblado distinto para poder cargarla después de las versiones anteriores; conserva el comando `BORDETIN3D`. No necesita reemplazar CivilSpellAI ni instalar archivos. Este paquete es para 2024.

Conservar **todos los archivos de la carpeta 2024 juntos**. El comando incluye NetTopologySuite y sus dependencias de Microsoft; solo se carga con NETLOAD `CivilSpellAI.BordeTin3D.v013.dll`. Al iniciar debe aparecer `BORDETIN3D 0.1.3` en la línea de comandos. No modificar acad.exe.config: las dependencias se resuelven desde esa carpeta para las operaciones de este comando.

## Corrección verificada con nueve TIN

La reproducción exacta se obtuvo del dibujo abierto antes de cerrar Civil 3D: nueve TIN, incluida Cimentación global, cuyos bordes interiores casi coinciden con otras superficies. El archivo guardado de prueba contiene ocho TIN; por eso las pruebas anteriores con ese archivo no reproducían el fallo de la sesión de nueve. La versión 0.1.3 reemplaza el cálculo manual de unión por la superposición poligonal robusta de NetTopologySuite, con precisión numérica 0.0000001 y origen local. Las cotas se obtienen después de los segmentos de borde originales.

Prueba nativa con los nueve bordes reales exportados: un exterior de **978 vértices** a tolerancia **0.05** y tres espacios interiores significativos seleccionados por Pick Points. Se escribieron las cuatro polilíneas 3D magenta, se guardaron y se comprobaron al reabrir. La unión, reducción y escritura tardaron 0.705 s en esa prueba; ese tiempo excluye la lectura inicial de los TIN de Civil 3D. La ejecución completa de BORDETIN3D sobre la copia de ocho TIN de la carpeta old tardó 0.61 s y generó 969 vértices. No son garantías de tiempo para otros dibujos.

`Vista-previa-nueve-TIN.png` muestra el resultado del conjunto de nueve: exterior magenta y ejemplos de espacios interiores en celeste. Los resultados nativos del test son magenta en ambos modos. La entrega incluye también una copia del DWG de old con su exterior generado; esa copia mantiene sus ocho TIN guardados.

## Seleccion: contorno exterior del conjunto

Seleccionar los TIN que forman el conjunto y pulsar Enter. Indicar la desviación máxima de simplificación en unidades del dibujo. Se crea una sola polilínea 3D cerrada **magenta (ACI 6)** en `CIVILSPELLAI_BORDE_TIN_3D`, siguiendo el contorno exterior de la unión en planta. El color se asigna directamente a la entidad, incluso si la capa ya existía con otro color. Se eliminan los bordes compartidos e interiores y se conservan los entrantes. No se agregan puentes entre superficies separadas. Un contacto en un solo punto no constituye un contorno simple único.

La línea de comandos confirma `CREACION CORRECTA`, el color, el número de polilíneas, de vértices y el tiempo del cálculo. Ante un fallo anterior a la escritura, indica explícitamente `NO SE CREO NINGUNA POLILINEA 3D`. Los errores del cálculo no demuestran que un TIN original esté abierto.

El cálculo se realiza con un origen local para mejorar la precisión en UTM. La unión tiene en cuenta los huecos de cada superficie, incluso cuando otra superficie los rellena y sus coordenadas coinciden solo dentro de la precisión numérica. La simplificación restaura únicamente los tramos donde acortar el borde introduce un cruce.

## Pick Points: espacio vacío entre superficies

1. Elegir `PickPoints` (la opción aparece como **Pick Points**).
2. Elegir `Visibles`, o `Seleccion` para limitar los TIN que intervienen.
3. Indicar la tolerancia de simplificación.
4. Pulsar dentro de un espacio vacío cerrado entre los TIN.
5. Se pueden indicar otros espacios; repetir un recinto no lo duplica.
6. Pulsar Enter para generar una polilínea por recinto, todas en una sola operación. Esc descarta la selección de recintos antes de escribir.

Un punto dentro de un TIN, sobre un borde o en un espacio abierto se rechaza y permite volver a indicar un punto. El comando calcula los recintos a partir de los bordes TIN; la triangulación y las curvas de nivel no subdividen el espacio. Si hay un terreno general cubriendo el espacio, utilizar `Seleccion` para incluir solamente los TIN que lo delimitan. `Visibles` considera entidades TIN de Modelo visibles en capas encendidas y descongeladas; no evalúa el estilo de cada componente ni limita la búsqueda a la ventana actual.

## Cotas y vértices

Los vértices originales reciben la elevación consultada en el TIN, incluso cuando el estilo de extracción aplana el borde. En las intersecciones nuevas se interpola Z sobre el segmento original. Cuando los bordes llegan al mismo XY con distintas cotas, se conservan ambas mediante un tramo vertical. Sus dos extremos se conservan incluso si su altura es menor que la tolerancia de simplificación. El comando informa cuántos encuentros tienen escalones y su diferencia máxima de Z. En tramos de borde coincidentes se conserva la cota de la primera superficie seleccionada que aporta el tramo, y se informa de esa elección.

La tolerancia por defecto es **0.001 unidades**, aplicada tanto a la desviación horizontal como a Z. En un dibujo en metros equivale a 1 mm. Aumentarla permite quitar más vértices y aproxima el borde dentro de esa desviación. Con **0** se conservan todos los vértices del contorno calculado, incluidas las intersecciones necesarias. La reducción conserva quiebres de pendiente que superen la tolerancia y comprueba que el resultado tenga al menos tres vértices, mantenga la orientación y no introduzca cruces nuevos. Los contactos que ya existían en los bordes originales se conservan. No se garantiza un número fijo de vértices.

En metros, 0.05 permite una desviación de hasta 5 cm tanto en planta como en Z. La entrega incluye un DWG de resultado con tolerancia 0.05, guardado y verificado tras reabrir. El original no fue modificado. Los resultados actuales y la distinción entre los ocho TIN guardados y los nueve de la sesión se detallan arriba.

La tolerancia numérica para la topología es 0.0000001 unidades y para la continuidad de Z es 0.00001 unidades. No se cierran huecos geométricos deliberadamente. Los cálculos tienen límites de trabajo y segmentos; ante un conjunto demasiado grande se solicita seleccionar menos superficies.

Se admiten TIN directos del dibujo actual y referencias de datos válidas. Si una referencia está desactualizada, se informa y se usa la geometría almacenada en el DWG, sin sincronizar ni modificar la superficie original. Es el caso de `Poza de agua` en la copia de prueba. Para TIN contenidos en un bloque o referencia externa, abrir el DWG de origen. No se recorren Xrefs/bloques ni se admiten superficies de volumen. No se guarda el DWG automáticamente.

## Validación

Pruebas de geometría incluidas en `Tests/TinBoundaryGeometryTests.cs`: unión, solapes parciales, intersecciones, huecos, superficies contenidas, recintos abiertos, selección de espacio vacío, superficies separadas, contactos puntuales, conflictos de Z, pendientes, orientación, coordenadas UTM, reducción de 4000 vértices, desviación XY/Z y 40 uniones con un oráculo de área independiente.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Run-SpellCoreTests.ps1 -Configuration Release
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-TinBoundary.ps1 -TryCivilSurfaces
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-TinBoundaryNineSession.ps1 -GeometryPath 'artifacts/validation/tin-boundary/exact-nine-probe/plugin/session-nine.tsv'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-TinBoundaryDrawing.ps1 -DrawingPath 'artifacts/validation/tin-boundary/prueba-estructura-proyectada-source.dwg' -Tolerance 0.05
dotnet build CivilSpellAI.BordeTin3D.csproj --configuration Release --no-restore
```

La prueba nativa utiliza un perfil temporal aislado de AutoCAD Core Console 2024 y un DWG de prueba. Comprueba la creación de `Polyline3d`, XYZ, cierre, capa, Deshacer/Rehacer y reapertura. Con `-TryCivilSurfaces` crea cuatro TIN reales, verifica la extracción sin objetos temporales y ejecuta los dos modos del comando, incluyendo un UCS desplazado, un punto inválido y otro repetido. No abre ni modifica el dibujo de SOTRAMI. El script escribe registros en `artifacts/validation/tin-boundary/`.

Referencias de API: [ExtractBorder](https://help.autodesk.com/cloudhelp/2020/ENU/Civil3D-API/files/html/2eaac15d-3cda-a958-5a52-5bbb1376f17a.htm), [crear un TIN](https://help.autodesk.com/cloudhelp/2022/ENU/Civil3D-DevGuide/files/GUID-1985347E-FC83-479B-B25C-4B381CB1548B.htm) y [coordenadas UCS](https://help.autodesk.com/cloudhelp/2025/ENU/OARX-DevGuide-Managed/files/GUID-096085E3-5AD5-4454-BF10-C9177FDB5979.htm).
