# Corrección de CREARFIGURA3D — 1.3.7.0

Las tres parejas de tu captura se reconstruyeron sobre una copia del plano.
El original no se guardó ni se modificó.

La pareja central fallaba porque el eje del tubo empieza ligeramente después
de sus bordes. Ahora se compara el tramo común. La superior fallaba porque
la salida es perpendicular a FRONT y aparece como círculo; ahora se relaciona
ese círculo con el recorrido de TOP y se conservan los canales de ambos lados.

## Revisar

Abrir `Tres-parejas-TOP-FRONT.dwg`. Los tres modelos están a la derecha de las
referencias: izquierda en X=40, central en X=47, superior en X=54. Los PNG
`reported-pair-0`, `1` y `2` muestran sus vistas previas respectivas.
El DWG conserva las referencias originales para comparar. Los JSON guardan
los parámetros reconocidos; no son necesarios para abrirlo.

El círculo del FRONT superior aporta diámetro y cota, y TOP aporta recorrido
y longitud. Se representa inicialmente a cota constante. Si ese FRONT es solo
un corte y existe una pendiente longitudinal, indicar cotas inicial/final o
aportar el corte longitudinal antes de dar el modelo por definitivo. El editor
avisa de ello y permite modificar X/Y y las cotas del tubo.

Espesor de tubo 0 significa superficie, como antes. Para obtener un sólido
hueco, ingresar su espesor real conocido. No se asignó un espesor supuesto.

## Actualizar

1.3.7.0 ya está instalada después de tu aviso de cierre del 12/10/2026.
Verificados 49/49 archivos; `INSTALACION_VERIFICADA.json` contiene el comprobante.
Puedes abrir Civil 3D y probar directamente; no necesitas repetir la instalación.
Los pasos siguientes quedan para una reinstalación posterior.

1. Guardar el trabajo y cerrar Civil 3D/AutoCAD.
2. Ejecutar `ACTUALIZAR_DRAWING3D.cmd`. Verifica las DLL y actualiza el bundle
   completo, conservando un respaldo. No copiar DLL individuales ni usar
   NETLOAD sobre la sesión que ya cargó la versión anterior.
3. Abrir Civil 3D y ejecutar `CREARFIGURA3D`: seleccionar TOP completo,
   Enter, **Referencias**, FRONT de la misma pieza completo, Enter.
4. Revisar las pestañas TOP/FRONT y el 3D, después **Crear 3D** y ubicarlo.
   `EDITARFIGURA3D` permite revisar las piezas; seleccionar un campo las
   resalta en verde. Cancelar conserva el dibujo y una `U` deshace la operación.

## Alcance y comprobación

El objetivo sigue siendo construir la geometría que aportan ambas vistas.
Para otras piezas, **Componente** / `CREARCOMPONENTE3D` relaciona perfiles
cerrados de TOP y FRONT con arcos y vacíos. Las vistas deben corresponder al
mismo componente y escala. Cuando falta información o hay varias soluciones,
se necesita completar la referencia; no basta con cualquier par de imágenes.

Se comprobaron las tres parejas en AutoCAD Core Console 2024: geometría nativa,
escala/desplazamiento independientes, rechazo de diámetros incompatibles,
vaciado circular real del muro, tubo hueco, edición Y, resaltado y reapertura.
También pasan las regresiones anteriores y las 151 pruebas del proyecto.
2024/2025/2026 compilan sin advertencias; 2025/2026 no se probaron en una sesión
nativa. Queda pendiente tu comparación visual de los resultados.
