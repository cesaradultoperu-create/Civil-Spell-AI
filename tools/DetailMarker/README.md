Generador independiente de un detalle para Layout en AutoCAD/Civil 3D 2024.

El entregable usa MLeader con contenido de bloque y cuatro atributos: LABEL,
REF, DET y TEXTO_INFERIOR. La directriz permite mover la punta en cualquier
direccion sin girar los textos. Una mascara circular evita que la linea cruce
los textos del circulo. La geometria y edicion de atributos son nativas.
La version V2 clona el circulo y la linea central del bloque de referencia,
aplicando la transformacion de su insercion para conservar tamano, color,
grosor y proporciones. Tambien conserva el estilo y la posicion de sus textos.
Los nombres del bloque y del estilo terminan en REFERENCIA_V2 para evitar
que un pegado en un dibujo con la version anterior reutilice su definicion.

La version V3 agrega AutoSide/AutoSide.csproj: un complemento independiente
para Civil 3D/AutoCAD 2024 que cambia entre las definiciones izquierda y derecha
al finalizar una edicion. Conserva los cuatro valores, la posicion del circulo,
la punta, las medidas y los colores. Usa ObjectModified y CommandEnded, con
reintento en Idle. Solo reconoce los bloques de esta familia. El archivo sigue
siendo geometria nativa, pero el cambio automatico requiere cargar ese DLL.
Se distribuye como DetalleAutoLado.bundle y se instala por usuario.

V4 usa DetalleAdaptable.2024.dll en el mismo bundle. Reconoce todas las
referencias anteriores y migra su contenido conservando sus atributos. Mide
los textos con el estilo real de AutoCAD; el ancho mantiene 2 unidades libres
entre el texto mas largo y el circulo. Las variantes de geometria se comparten
por lado y longitud, mientras el ancho minimo manual se guarda en un Xrecord
del MLeader y se conserva al copiarlo. Un GripOverrule filtrado por ese registro
agrega un pinzamiento en el extremo: dispara DETALLEESTIRAR y un DrawJig para
elegir la nueva posicion horizontal. DETALLEACTUALIZARTODOS revisa los planos
abiertos; no los guarda. Las pruebas nativas cubren ambos lados, texto largo,
estiramiento, independencia entre referencias y copia del ancho personalizado.

`DETALLENATIVE` se ejecuta sobre una copia del DWG original: conserva la
referencia anterior en ORIGINAL, coloca el detalle nuevo en Layout1 y crea
cuatro ejemplos en DETALLE LIBRE. Tambien exporta una biblioteca mediante Wblock.
COMPARACION muestra la referencia y el resultado V2 al lado. Si hay directrices
anteriores en Layout1, se conservan en VERSION ANTERIOR dentro de la copia.
`DETALLENATIVECHECK` verifica atributos editables, movimiento independiente de
la punta y conservacion del original; las pruebas se deshacen al terminar.
`DETALLELIBCHECK` verifica la biblioteca exportada y su estilo activo.
`DETALLEPDF` exporta las vistas de los dos Layouts mediante el motor de ploteo.

Compilar DetailMarker.csproj con dotnet y ejecutar la DLL en accoreconsole.exe
2024. La variable DETALLE_OUTPUT indica la carpeta de salida de los comandos.
Si no se define, se usa Test/DetalleLayout-20261021 bajo la carpeta de trabajo.
Render-Pdf.ps1 genera PNG de los PDF con el renderizador nativo de Windows.

La herramienta no esta integrada en CivilSpellAI ni altera su proyecto.

V5 usa DetalleAdaptable.Mascara.2024.dll. Los atributos DET y TEXTO_INFERIOR
son atributos MText nativos con BackgroundFill, UseBackgroundColor y margen
1.1. La mascara esta activada por defecto. DETALLEMASCARA permite elegir Yes
o No por referencia; un Xrecord conserva la opcion al copiar y al cambiar
lado o ancho. Las variantes compartidas incluyen la opcion de mascara.
Al reconstruir una variante se normaliza primero el atributo a texto simple
para que la conversion MText respete el anclaje del nuevo lado y ancho.
BackgroundScaleFactor se asigna antes de BackgroundFill: su setter nativo
activa la mascara; su getter no aplica cuando BackgroundFill es false.
DETALLEPROBARMASCARA y DETALLEPROBARANCHO validan esas opciones en AutoCAD.
El complemento desactiva los manejadores de versiones anteriores ya cargadas
y DETALLEACTUALIZARTODOS5 migra las referencias abiertas sin guardar planos.

V6 (DetalleAdaptable.Textos.2024.dll) centra la linea entre los limites reales
de ambos textos. La separacion es media altura de texto a cada lado de la
linea, tambien con tildes. Tras convertir a MText se desplaza el atributo
completo segun sus limites nativos: editar solo Location del MText no modifica
el anclaje que conserva el atributo. El contenido MLeader toma la alineacion
de la definicion, por lo que las variantes incluyen los dos textos exteriores
en su clave para conservar sus alturas reales. DETALLEPROBARTEXTOS6 prueba
doce cambios seguidos de texto, lado, longitud y mascara por referencia.
DETALLECOMPROBAR6 verifica las separaciones en los dibujos abiertos.
