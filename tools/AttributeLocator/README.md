# Localizar casillas · prueba independiente para Civil 3D 2024

Este complemento no modifica CivilSpellAI, no se instala globalmente y no se
carga en otros planos automáticamente. El ejemplo está en `Test/Casillas-20261021`.

`CASILLAS` abre un panel de atributos del DWG activo. Se puede buscar por TAG,
nombre de propiedad, valor, bloque, Layout o código de campo. Cada selección
marca el atributo mediante gráficos transitorios: recuadro rojo, flecha y TAG.
Los bordes de celda se reconocen únicamente si se encuentran cuatro segmentos
rectos que delimitan y contienen el atributo. En los demás casos se marca su
posición con un tamaño mínimo visible. No se inventa una asociación por el valor.

`CASILLASDEMO` abre la búsqueda `03_R3_`, con las casillas reales que muestran
un punto. `CASILLASBLOQUE` restringe la lista a una inserción. El panel permite
recorrer coincidencias, ver el bloque completo, retirar la marca y abrir EATTEDIT.

La edición usa el editor nativo de atributos y una selección con punto de captura.
El usuario debe comprobar el TAG mostrado en EATTEDIT antes de editar. El panel
no asigna TextString, no convierte los campos a texto y no modifica propiedades
en un DST compartido. El cambio de valor de una propiedad del Sheet Set se hace
en el administrador de Sheet Sets, con acceso al DST correspondiente. El código
original del campo permanece visible para comprobar origen y alcance.

Se omiten referencias externas: no se busca dentro de archivos Xref. Los
atributos anidados se localizan con su transformación de inserción, pero su
edición se restringe porque podría cambiar una definición compartida. Los
atributos invisibles se identifican y se marca su posición guardada. No se
guardan automáticamente los dibujos ni se añaden marcas a la base de datos.

Compilar: `dotnet build tools/AttributeLocator/AttributeLocator.csproj -c Release`.
El proyecto usa .NET Framework 4.8 y las referencias locales de AutoCAD 2024.

`CASILLASPROBAR` es una prueba específica de la copia de referencia de 108
atributos: lectura AcSm, puntos repetidos, celdas distintas, gráficos sin
ObjectId, transformaciones anidadas, rollback de edición y guardar/reabrir.
Genera un informe, CSV y un DWG visual separado con marcas permanentes de
ejemplo. `CASILLASPROBARMARCA` verifica transitorios en una sesión con pantalla.
