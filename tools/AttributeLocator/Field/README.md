# Field: celdas y vínculos · Civil 3D 2024 en inglés

V0.4.0 incorpora el perfil SOTRAMI de este usuario. `ProjectAccess` valida la carpeta
del DWG, la ruta exacta del DST y el GUID del conjunto, sin migrar ni reasignar Fields.
`Install-SOTRAMI.ps1` instala el bundle y registra el perfil de `Projects-SOTRAMI.xml`
en `%LOCALAPPDATA%/CasillasField/projects.xml`, preservando los demás perfiles.
La activación elige el membrete único `MEMB_WWL_P_A1_` de la presentación actual,
incluidas A y A 1; otros documentos no registrados permanecen fuera de la activación.
La edición de valores se permite en las propiedades personalizadas de conjunto
de ese DST. Otras fuentes, propiedades de hoja y propiedades estándar se consultan;
el editor nativo mantiene sus funciones. Los documentos de solo lectura no se editan.
Antes de cada escritura de proyecto, con LockDb y comprobación del valor anterior,
se genera un respaldo del DST y un registro XML local del valor anterior/nuevo.
Los registros de observación también son locales; no se crean auxiliares en Q:.
Guardar un valor central es independiente de Cancelar Field y de UNDO del DWG,
y la ventana de edición explica su efecto sobre otros planos.
El catálogo lee el valor evaluado del Field para evitar la caché antigua de
TextString en presentaciones inactivas. Las vistas adaptan el texto conservando
la geometría y el tamaño nativos; no modifican el Field para mostrarlo.
`CASILLASPROYECTOPROBAR` prueba únicamente Casillas-piloto.dwg marcado y su DST
copiado local: identidad, límites de carpeta, propiedades de hoja rechazadas,
actualización en dos presentaciones, conservación de códigos y respaldos.
Ver `LEEME-SOTRAMI.txt` para uso en el proyecto. Las secciones V3 describen el
ejemplo que continúa disponible, con las ampliaciones de acceso de V0.4.0.

V3.2: las vistas se dibujan con contornos de letras y cajas orientadas obtenidas
del DWG. Se conservan rotación, proporción y líneas de MTEXT distribuidas por
AutoCAD; no se agrandan letras pequeñas artificialmente sobre otras casillas.
La vista azul encuadra una coincidencia a la vez. El selector 1/N recorre todas
las coincidencias, incluidas las de otras inserciones, y sincroniza la casilla
elegida con el mapa. No cambia el atributo que se está editando en Field.
Las vistas roja, azul, del mapa y de edición ofrecen +, −, Ajustar y Ampliar;
también admiten rueda para zoom, arrastre para desplazar y doble clic para ampliar.
La vista ampliada es una consulta visual. Las etiquetas distinguen revisiones,
revisión del plano, firmas y responsables, datos generales y planos de referencia.

V3.1: el bundle CasillasFieldDemo carga el observador al iniciar AutoCAD 2024.
En el ejemplo, el observador se activa para Ejemplo-Field-v3.dwg con DST local y
manifiesto .demo válidos. V0.4.0 añade los proyectos registrados descritos arriba.
Por eso se puede abrir el DWG directamente, entrar a EATTEDIT y abrir el Field
de cualquier atributo del rótulo: la ayuda aparece sin ejecutar CASILLASFIELD.
Si se cierra la ventana auxiliar, el observador se recupera al terminar la
edición nativa. CASILLASFIELDOFF desactiva la recuperación en ese documento.

Abrir-ejemplo.cmd conserva la ruta rápida que abre R3_CH. Si el ejemplo ya está
abierto, solicita esa vista a un controlador oculto de la misma instancia. La
petición solo se atiende en el ejemplo local y mientras no haya otro comando
activo; nunca cancela cuadros Field/EATTEDIT pendientes. No cambia alias, CUI,
LISP de inicio ni variables de seguridad para instalar el bundle.

La V3 acompaña al Field nativo. Se activa sobre un bloque y observa los cuadros
Enhanced Attribute Editor y Field de esa instancia de Civil 3D. Al recorrer
Property se leen la expresión AcSm y el valor nativo, sin escribir en el Field.

Rojo identifica la celda en edición. Azul identifica los usos existentes de la
propiedad elegida, en el DWG actual. La ayuda ofrece dos vistas de contexto del
rótulo real, el valor guardado, el vínculo actual y los archivos DST de origen.
Las marcas sobre el plano son transitorias. Las vistas auxiliares no son una
captura estática: se generan con geometría y valores del bloque identificado.
La vista azul se centra en una coincidencia; las demás se recorren con el selector.
La lista informa también los usos en las otras inserciones y Layouts del DWG.

La identidad compara DST, GUID del conjunto, GUID de hoja cuando corresponde y
nombre de propiedad. Un Component igual al GUID del conjunto es un vínculo de
conjunto. No se asocian atributos por el punto, por su valor ni por nombre de
propiedad aislado. Varias inserciones ambiguas no se identifican automáticamente.
Si el mismo nombre de bloque y TAG se repite en el Layout, no se presume que el
editor nativo corresponde a la inserción seleccionada inicialmente: el destino
se indica como no identificado, mientras se mantienen los usos verificables.

Los usos mostrados pertenecen exclusivamente al DWG inspeccionado. Cero usos en
este DWG no significa que la propiedad esté libre en el resto de planos. Esta
demostración permite escribir en `Casillas-prueba.dst`, junto a la copia DWG
y su manifiesto `.demo`. V0.4.0 también permite las propiedades de conjunto
del DST de proyecto habilitado; los demás vínculos se conservan en consulta.

El mapa enumera todos los atributos del bloque y distingue «sin dato con Field»,
«sin dato sin Field», contenido vinculado y texto fijo. Un punto no significa
ausencia de vínculo. La referencia tiene 54 espacios sin dato: 42 con Field y
12 sin Field. Se identifican las siete columnas de revisión y las columnas de
planos de referencia. La posición se obtiene de las líneas reales del bloque.

«Seguir Property» sincroniza el mapa con la selección nativa. Se prioriza la
identidad completa; si solo coincide el nombre, se muestra una advertencia de
origen o ámbito diferente. Sin relación existente no se inventa una casilla.
Se puede desactivar el seguimiento para recorrer manualmente otras casillas.

«Editar casilla...» (también doble clic) abre el valor con una vista previa de
su posición. Para una celda con Field, se modifica su propiedad central en el
DST autorizado, se enumeran sus usos en este DWG y se conserva el código del Field.
La escritura comprueba el valor inicial bajo LockDb y rechaza datos obsoletos.
«Dejar punto» conserva el enlace y vuelve a reservar el espacio. Esta escritura
es independiente de aceptar o cancelar el cuadro Field, y no forma parte del
UNDO del DWG. Para deshacerla, vuelve a editar el valor en el mismo diálogo.

Para texto sin Field, la edición queda pendiente hasta salir de Field y del
editor de atributos. Se aplica en una transacción solamente si no apareció un
Field ni cambió el texto original; así no se sobrescribe una edición nativa.
La vista auxiliar muestra el nuevo texto inmediatamente. El DWG se regenera al
terminar el editor; todavía debe guardarse normalmente. El dibujo real también
reevalúa los Fields locales al terminar el editor de atributos.

`CASILLASFIELD` selecciona el bloque y activa la ayuda para sus futuras ediciones.
`CASILLASFIELDOFF` la desactiva. `CASILLASFIELDDEMO` activa el rótulo de referencia y
abre R3_CH. El lanzador de ejemplo abre después su Field existente. Al cerrar el
Field, la ayuda se oculta; reaparece al volver a abrir uno. Al confirmar una
edición con el editor nativo, la caché se renueva para la siguiente edición.

Se utiliza lectura Win32 de los controles nativos de Civil 3D 2024 en inglés,
con un temporizador en el hilo de UI. Es una integración específica de esta
versión, no una API pública de selección de Property. Otras versiones o idiomas
requieren validación. Si no se reconoce un Field o destino se indica y no se
inventa una relación. No se buscan Xrefs ni se rastrean otros DWG del Sheet Set.

Compilar: `dotnet build tools/AttributeLocator/Field/Field.csproj -c Release`.
`CASILLASFIELDPROBAR` valida identidades, sincronización y conservación de datos.
`CASILLASPREPARARPRUEBA` crea el DST local solo para una copia nueva llamada
Ejemplo-Field-v3.dwg; remapea 66 Fields personalizados de conjunto y conserva sus
formatos. No sobrescribe DST existentes. La copia de demostración reúne esas
propiedades en un conjunto local; no se utiliza como migración de un proyecto.
`CASILLASEDICIONPROBAR` escribe una propiedad compartida, verifica cuatro celdas,
rechaza escrituras obsoletas y sustitución de Fields, prueba una celda sin Field
y restaura los valores. `Test-Live.ps1` limita su automatización al nombre de
copia V2/V3 elegido y permite capturar y probar los controles de edición.
