# DrenBordes 0.1.1

Herramienta independiente para Civil 3D / AutoCAD 2024. `NETLOAD` carga
`DrenBordes.2024.dll`; `DRENBORDES` genera bordes con distancia predeterminada
0.50 a cada lado, capa `0_DCL_Bordes` y color ACI 5 por capa.

La red se divide en sus intersecciones y se unen los tramos con nodos de grado
dos antes del buffer en inglete. La unión de las franjas elimina cruces de bordes
interiores. Se completan los ingletes exteriores de nodos múltiples y se retiran
las tapas de los extremos terminales. El resultado son polilíneas editables.
Las coordenadas y elevaciones de los ejes se conservan. Se ponen en cero los
anchos globales y por segmento de las polilíneas de la capa fuente y de los
bordes. Los bordes con XData propio se reemplazan de forma
atómica al regenerar; los bordes manuales impiden la operación.

El filtro predeterminado usa curvas abiertas en Z=0, coherente con el avance
manual de Stage 1. `Todas` procesa cada elevación horizontal por separado.
Curvas con Z variable se rechazan; arcos se discretizan con error máximo de
cuerda 0.001. Precisión topológica 0.000001. Ingletes limitados a diez offsets.

Compilación (SDK .NET y bibliotecas AutoCAD 2024 instalados):

```powershell
dotnet build tools/DrainageBorders/DrenBordes.csproj -c Release --ignore-failed-sources
```

Validación nativa con copias de los tres DXF, seguida de la prueba del DLL de
producción y del comando interactivo, U/REDO, regeneración y conservación:

```powershell
./tools/DrainageBorders/Test-DrenBordes.ps1 -SourceDirectory 'C:\ruta\copias' -OutputDirectory artifacts/validation/dren-bordes/width-zero-v2
python tools/DrainageBorders/preview.py artifacts/validation/dren-bordes/width-zero-v2
./tools/DrainageBorders/Build-Package.ps1
```

La prueba requiere acceso al perfil de AutoCAD y usa `/isolate` y una copia de
su plantilla. No abre ni guarda los originales en la sesión gráfica del usuario.
Los archivos fuente se verifican mediante SHA-256; la base de datos conserva
exactamente las coordenadas originales. El lote termina también Stage 1 en una
copia nueva, reemplazando únicamente sus bordes de avance en esa copia.
La prueba verifica ancho global y anchos por segmento cero en todos los planos,
incluso tras reabrir DXF/DWG. Undo restaura también los anchos anteriores.
Al serializar y reabrir DXF se admite
un redondeo decimal de hasta 1e-8 unidades. Las muestras de los ejemplos 3.2 y
4.2 se comparan bidireccionalmente con el avance manual de Stage 1.

La implementación no modifica los proyectos ni los comandos de CivilSpellAI.
