# Prueba de sección paramétrica

## Alcance de la primera versión

`CREARSECCION` genera en Model un bloque compuesto por cinco contornos cerrados
con hatch asociativo: relleno central, capas interiores superior e inferior y
capas exteriores superior e inferior. También crea trece cotas independientes y
movibles para longitudes,
alturas libres, espesores y medidas totales. `EDITARSECCION` recupera los
parámetros guardados en el bloque y regenera contornos, hatch y cotas. Las cotas
inferiores nacen en el contorno inferior y el hatch admite colores ACI de 0 a 256.
`EDITARSECCION` acepta tanto el bloque como cualquiera de sus cotas asociadas,
por lo que no es necesario acertar sobre una línea blanca del contorno.
Los dos muros 3D se elevan con la altura indicada y se inclinan hacia el eje de
la sección según el ángulo configurado; la prueba automatizada comprueba la
inclinación geométrica en ambos sentidos, además del volumen y los metadatos.

La ventana incluye `Crear sección desde objeto`. Al seleccionar un bloque
paramétrico o sus cotas se recuperan todos sus valores guardados, incluidos la
altura y el ángulo del muro. Al seleccionar geometría original se abre
`Editar figura seleccionada`, con contornos y medidas de esa selección.
`EDITARSECCION` acepta una selección previa o una ventana de varios objetos;
terminar la selección con Enter. La figura original puede contener líneas,
polilíneas, arcos, círculos, hatch y bloques sin parámetros tipo A/B.

Al elegir otra sección paramétrica desde la ventana de `EDITARSECCION`, la
edición se aplica al nuevo bloque seleccionado. Para una figura original, la
ventana permite ajustar ancho y alto y elegir entre aplicar el cambio al conjunto
seleccionado o crear una copia en otro punto. Al aplicar al original, los objetos
se agrupan en un bloque editable y se retiran los originales en la misma
transacción. Las cotas lineales seleccionadas se mantienen fuera del bloque,
vinculadas a él y actualizadas con sus medidas. `U` revierte la operación.
Desde `CREARSECCION`, la casilla `Crear una copia editable` nace activada.
La selección de varios bloques paramétricos, o de uno mezclado con otra
geometría, se rechaza. Una cancelación o selección fallida conserva los valores
válidos que se habían escrito en la ventana.

El ajuste de ancho/alto es global: también escala espesores y símbolos. No infiere
parámetros independientes de cada muro, diámetro o hueco. Las medidas excluyen
las cotas y textos del límite físico. Un texto, punto o cota sueltos no definen
una figura con ancho/alto. Las cotas angulares/radiales no admiten factores
distintos en X e Y; el cambio falla sin commit si no es compatible.
La vista previa dibuja contornos en planta, sin patrones ni textos; si no hay
contornos disponibles muestra los límites exteriores e informa esa aproximación.
Los objetos se clonan íntegros en el DWG, incluidos los hatch.

Las flechas internas, el texto de pendiente y otros símbolos quedan fuera de
esta versión. El dibujo usa una unidad de dibujo por cada unidad ingresada en
la ventana.

## Prueba manual mínima

1. Abrir una copia de prueba del DWG en Model.
2. Cargar la DLL mediante `NETLOAD`.
3. Ejecutar `CREARSECCION` y conservar inicialmente los valores propuestos.
4. Indicar el centro del extremo izquierdo de la sección.
5. Comprobar alturas totales de `0.80` y `1.70`, longitud de transición `1.20`
   y longitud recta final `1.20`.
6. Ejecutar `EDITARSECCION`, seleccionar el bloque y cambiar cada longitud,
   altura libre y espesor.
7. Confirmar que los cinco contornos, hatch y trece cotas se regeneran y que los
   espesores permanecen uniformes.
8. Copiar el bloque, editar solo una copia y comprobar que la otra no cambia.
9. Ejecutar `U` una vez y comprobar que la edición completa se revierte.
10. Cancelar la ventana y comprobar que el dibujo no cambia.
11. Desde `CREARSECCION > Crear sección desde objeto`, seleccionar un bloque
    recién creado, luego repetir seleccionando solo una de sus cotas. Verificar
    que se recuperan también la altura y el ángulo del muro sin reiniciarlos.
12. Desde `EDITARSECCION`, seleccionar la sección A, pulsar `Crear sección desde
    objeto`, seleccionar la sección B y aceptar una medida distinta. Comprobar
    que cambia B y que A conserva sus medidas; `U` debe revertir la edición.
13. Desde `EDITARSECCION`, seleccionar con una ventana un detalle rectangular
    original con tuberías y presionar Enter. Confirmar que aparece su propia
    geometría en `Editar figura seleccionada`. Cambiar ancho/alto y comprobar
    las cotas lineales seleccionadas. Probar crear una copia y aplicar al original,
    cancelación y `U`; comprobar que el resto del dibujo conserva su geometría.
14. Seleccionar dos bloques con una ventana. Comprobar el mensaje de ambigüedad.
    Cambiar un valor válido antes de importar, cancelar la selección y verificar
    que ese valor sigue en la ventana.

## Regresión automatizada de selección

Ejecutar `powershell -NoProfile -ExecutionPolicy Bypass -File
scripts/Test-ParametricSectionObjects.ps1`. Para usar otro plano, agregar
`-DrawingPath "ruta\plano.dwg"`. El script compila Debug, copia el DWG a
`artifacts/validation`, ejecuta AutoCAD Core Console con un perfil aislado,
comprueba los marcadores de éxito y verifica que la fuente conserva su SHA-256.
Las pruebas cubren cotas sin seleccionar el bloque, selecciones repetidas,
destino del segundo bloque, rechazo de selecciones ambiguas y mixtas,
recuperación de medidas de bloques convencionales y contornos de hatch.
El fixture de selección se revierte al finalizar su transacción.
También comprueba un bloque convencional girado 90 grados.

`AISPELLTESTSELECTEDFIGURE` (Debug) comprueba reconocimiento de una figura
rectangular que contiene polilínea, línea, círculo, hatch asociativo y cotas;
clonado conservando la fuente y las asociaciones del hatch; escala independiente
en X/Y con medidas correctas de las cotas; copia de una figura ya editable;
resolución desde una cota; edición del original y línea sin cotas ni alto.
`AISPELLTESTSELECTEDFIGUREFILE` analiza una selección previa del plano y verifica
su copia sin commit. Exporta una vista de contornos a `.preview.png`.
`AISPELLFIGUREUNDOSETUP`, `AISPELLFIGUREUNDOCHANGE`, `U` y
`AISPELLFIGUREUNDOCHECK` verifican que una sola operación de deshacer restaura
las entidades originales, sus IDs y sus medidas después de convertir el conjunto
en un bloque. La prueba se ejecutó correctamente en una copia local.
Se verificó el detalle rectangular del P-011 en una copia local: la ventana de
prueba tomó 51 objetos y midió ancho `2.565849` y alto `1.3`. Este conjunto incluye
contornos y tuberías, y su vista previa conserva la figura rectangular.
La ventana de selección puede incluir más/menos objetos que la captura del usuario.
`Test/Drawing3D/Pruebas-Figuras-20261002/1107-P-011-FIGURAS.dwg` conserva la
geometría original y agrega una copia editable de ese detalle a la derecha.
La carpeta incluye una DLL Release para 2024 y `ACTUALIZAR_DRAWING3D.cmd`.
El actualizador requiere cerrar Civil 3D, solicita elevación para la instalación
existente en Program Files y guarda un respaldo en `artifacts/drawing3d-backups`.
La actualización de la primera versión genérica se instaló y verificó con
SHA-256 después de cerrar Civil 3D, con respaldo de la DLL anterior.

## Medidas interiores por letras

Para crear una caja 3D editable usando TOP como planta y FRONT como
referencia de altura/base, consulte
[FIGURE_3D_TOP_FRONT_RUNBOOK.md](FIGURE_3D_TOP_FRONT_RUNBOOK.md).

El propietario confirmó el 2026-10-02 que la edición de medidas produjo el
resultado buscado en Civil 3D 2024. La mejora siguiente añade navegación en
la vista previa: rueda para acercar/alejar alrededor del cursor, botones +/−,
arrastre con botón central y **Encuadrar** para recuperar la figura completa.
Los marcadores mantienen su tamaño y se seleccionan sobre la vista ampliada.
La navegación conserva los valores editados y no escribe entidades del DWG.

La compilación de esta mejora pasa Debug/Release para 2024/2025/2026 sin
advertencias y la suite general pasa 151/151 pruebas. Core Console 2024
comprueba el anclaje del zoom, desplazamiento, límites y encuadre, además de
las regresiones de geometría, hatch, cotas, copia y una sola U. Se renderizó
la ventana y el detalle ampliado del DWG real; falta comprobar los gestos
de ratón en Civil 3D interactivo después de instalar.
El paquete está en `Test/Drawing3D/Pruebas-Vista-20261002`; la sesión abierta
mantiene la DLL anterior hasta cerrar Civil 3D y ejecutar el actualizador.

`EDITARSECCION` detecta distancias horizontales y verticales entre bordes
rectos de la selección y las muestra con letras en la vista previa y campos
en la lista lateral. Al seleccionar una letra, sus extremos y flechas se
resaltan en verde. El cambio estira la distancia elegida; los intervalos
fuera de ella conservan sus longitudes y las distancias relacionadas se
recalculan. Se reconoce primero un marco rectangular con conductos, incluso
si sus lados están compuestos por varias polilíneas y pequeñas aberturas.
En el detalle del P-011 analizado (51 objetos), se detectaron 58 distancias;
las primeras fueron A=0.10, B=0.15, C=0.70 y D=0.80. Las letras dependen de
los contornos seleccionados, y deben revisarse mediante las flechas.

`Test/Drawing3D/Pruebas-Medidas-20261002/1107-P-011-MEDIDAS.dwg` contiene dos
ejemplos editables: el detalle original a X=50 y una copia a X=56 con ancho
interior 0.90. Guardar, reabrir y volver a editar ambos pasó la prueba.
La carpeta incluye la DLL Release para 2024, el actualizador y LEEME.txt.
La versión de medidas interiores se instaló en el bundle de Civil 3D 2024
con respaldo después de confirmar el cierre del programa. La DLL instalada
coincide con el paquete: SHA-256
`DD42F651A3D2CE2789D057BE61116F5B113A25BD1532454F6B8507648EE5E87F`.

`scripts/Test-FigureInternalMeasures.ps1` ejecuta pruebas en Core Console
con copias y perfiles aislados. Cubre medidas internas, conservación del
espesor vecino, hatch con hueco interior, cotas lineales con valor real,
copia sin alterar definiciones compartidas, giro, rechazo de valores
inválidos, edición en sitio, una sola operación U y el detalle real. La DLL
Debug se pasa mediante `-PluginPath`. `scripts/New-FigureMeasureTestCopy.ps1`
prepara los dos ejemplos y verifica persistencia sin cambiar la fuente.

Alcance: distancias horizontales/verticales en XY. Las medidas inferidas
no constituyen un sistema completo de restricciones de ingeniería. Las
distancias contenidas dentro de un tramo modificado también cambian. Las
curvas deformadas pueden convertirse en polilíneas aproximadas; una curva
que solo se desplaza conserva su tipo. Los hatch mantienen su patrón y se
reconstruyen con contornos ajustados, sin conservar la asociación nativa a
las polilíneas; EDITARSECCION vuelve a ajustar ambos juntos. Los bloques
con atributos, referencias externas y entidades no compatibles se rechazan
para edición interior antes de confirmar la transacción. Las secciones
paramétricas A/B conservan su flujo propio.

`scripts/New-ParametricSectionTestCopy.ps1 -DrawingPath "ruta\plano.dwg"`
crea una copia dentro del repositorio con una zona de pruebas etiquetada a la
derecha de la geometría original. Cada grupo reconocido se convierte en un
bloque paramétrico nuevo y se agrega una instancia vertical. El original se
abre solo para lectura compartida y se comprueba su SHA-256 antes y después.
En `Test/Drawing3D/Pruebas-20261002/1107-P-011-PRUEBAS.dwg` se guardaron siete
instancias; al reabrir el archivo, las siete conservaron parámetros válidos.
La vista inicial muestra la zona de pruebas. Usar `EDITARSECCION` directamente
sobre un contorno o una cota amarilla. La copia incluye `LEEME.txt` y la DLL de
Civil 3D 2024; las medidas inferidas siguen requiriendo revisión visual.

El 2026-10-02 se inspeccionó una copia de
`Q:\Proyectos 2026\1107.001.00.16 - Pad SOTRAMI\05_Drawing\1107.001.00.14-200-14-P-011.dwg`.
El Model contiene 814 entidades, entre ellas 488 polilíneas, 123 hatch y cuatro
sólidos. El reconocimiento por grupos de polilíneas detectó tres perfiles tipo A
y dos tipo B. La apertura y las pruebas no guardaron cambios en la fuente.
Esto comprueba el reconocimiento geométrico; falta validar visualmente en
Civil 3D las medidas inferidas contra las cotas del plano y el flujo de cambio
de destino de la ventana (pasos 11–14).

## Pendiente para validar con el DWG original

La imagen no acota la longitud recta izquierda. La primera versión la presenta
como parámetro independiente, con valor inicial `1.20`. Antes de igualar capas,
patrón y escala de hatch con producción se debe revisar el archivo original.
