param([string]$ValidationDirectory, [string]$DestinationDirectory)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path
if (-not $ValidationDirectory) { $ValidationDirectory = Join-Path $repo 'artifacts\validation\dren-bordes\width-zero-v2' }
if (-not $DestinationDirectory) { $DestinationDirectory = Join-Path $repo 'Test\DrenBordes-20261020-v2' }
if (Test-Path -LiteralPath $DestinationDirectory) { throw 'El paquete de destino ya existe. Use una carpeta nueva.' }
$consoleLog = [IO.File]::ReadAllText((Join-Path $ValidationDirectory 'console.log'))
$interactiveLog = [IO.File]::ReadAllText((Join-Path $ValidationDirectory 'interactive.log'))
if ($consoleLog -notmatch 'DREN_ALL_OK' -or $consoleLog -notmatch 'DREN_STAGE_1_OK' -or $consoleLog -match 'DREN_TEST_FAILED' -or
    $interactiveLog -notmatch 'DREN_INTERACTIVE_UNDO_OK' -or $interactiveLog -match 'DREN_INTERACTIVE_FAILED') {
    throw 'Se requiere una validacion correcta antes de preparar el paquete.'
}
foreach ($directory in @('', 'Resultados', 'Herramienta', 'Herramienta\Licencias', 'Validacion')) {
    [void][IO.Directory]::CreateDirectory((Join-Path $DestinationDirectory $directory))
}
foreach ($stage in @(1,2,3)) {
    foreach ($extension in @('dxf','dwg')) {
        $name = "Drainage Stage $stage - Bordes 0.50.$extension"
        Copy-Item -LiteralPath (Join-Path $ValidationDirectory $name) -Destination (Join-Path $DestinationDirectory ('Resultados\' + $name))
    }
    Copy-Item -LiteralPath (Join-Path $ValidationDirectory "stage-$stage-report.txt") -Destination (Join-Path $DestinationDirectory 'Validacion')
}
foreach ($name in @('DrenBordes.2024.dll', 'NetTopologySuite.dll', 'System.Memory.dll', 'System.Buffers.dll',
    'System.Numerics.Vectors.dll', 'System.Runtime.CompilerServices.Unsafe.dll')) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot ('bin\Release\net48\' + $name)) -Destination (Join-Path $DestinationDirectory 'Herramienta')
}
foreach ($file in Get-ChildItem -LiteralPath (Join-Path $repo 'Resources\Licenses\TinBoundary') -File) {
    Copy-Item -LiteralPath $file.FullName -Destination (Join-Path $DestinationDirectory 'Herramienta\Licencias')
}
foreach ($name in @('Comparacion-Stage-1.png','Resultado-Stage-2-y-3.png')) {
    Copy-Item -LiteralPath (Join-Path $ValidationDirectory $name) -Destination $DestinationDirectory
}
foreach ($name in @('console.log', 'interactive.log', 'reference-comparison.txt', 'source-sha256.json')) {
    Copy-Item -LiteralPath (Join-Path $ValidationDirectory $name) -Destination (Join-Path $DestinationDirectory 'Validacion')
}
$instructions = @'
DRENBORDES 0.1.1 - Civil 3D / AutoCAD 2024
20 de octubre de 2026

RESULTADOS LISTOS PARA ABRIR
La carpeta Resultados contiene Stage 1, Stage 2 y Stage 3 en DXF y DWG 2013.
Cada archivo conserva los ejes, las referencias elevadas y los rectangulos originales.
Ancho inicial, ancho final y ancho global de TODAS sus polilineas: 0.000.
Las coordenadas y elevaciones originales se conservan; los ejes de drenaje estan en Z=0.
Los bordes nuevos estan a 0.50 a cada lado (ancho total 1.00), en la capa
0_DCL_Bordes, color Blue / ACI 5, con color de objeto ByLayer.
Los encuentros en T, los cruces y las esquinas se recortaron y unieron.
Los extremos libres quedan abiertos, como en la referencia de Stage 1.

Stage 1: 53 ejes, 51 encuentros, 54 polilineas de borde.
Stage 2: 72 ejes, 71 encuentros, 74 polilineas de borde.
Stage 3: 85 ejes, 82 encuentros, 85 polilineas de borde.
Una polilinea de borde puede recorrer varios ejes despues de unir los encuentros.

No se generaron offsets de los rectangulos cerrados ni de las
polilineas elevadas de cada plano. Su ancho visual tambien se puso en cero.
Este alcance coincide con los bordes manuales en Z=0 de Stage 1.
Los tres DXF de origen permanecen intactos. Stage 1 se termino en una nueva copia,
regenerando todos sus bordes y sus uniones, incluidos los que faltaban ajustar.

USAR LA HERRAMIENTA EN OTRO DIBUJO
1. Conserva juntos los seis DLL de la carpeta Herramienta.
2. En Civil 3D 2024 escribe NETLOAD y selecciona Herramienta\DrenBordes.2024.dll.
3. Abre el plano que quieras procesar y escribe DRENBORDES.
4. Elige el numero de la capa de los ejes. La herramienta muestra sus nombres.
5. Acepta 0.50 como distancia a CADA lado.
6. Acepta Ejes: procesa solamente curvas abiertas en Z=0.
7. Revisa el resultado y guarda el dibujo con otro nombre.
Si habias cargado la version anterior, reinicia Civil 3D antes de cargar esta DLL.

Para repetirlo en otro plano abierto, basta ejecutar DRENBORDES otra vez.
Si ejecutas de nuevo sobre el mismo plano, sustituye sus bordes generados
anteriormente y no duplica objetos. Una U deshace el lote; REDO lo rehace
si se ejecuta inmediatamente despues de U.
Si hay bordes manuales en 0_DCL_Bordes, el comando se detiene y los conserva.

La opcion Todas tambien incluye curvas cerradas y curvas a otras cotas.
Cada elevacion se procesa de forma independiente; no une cruces entre cotas.
La distancia se expresa en unidades del dibujo.
Conserva las coordenadas de los ejes, pone los anchos de las polilineas de la capa
fuente en cero y no guarda el archivo automaticamente.

ALCANCE
Esta entrega corresponde a Civil 3D / AutoCAD 2024 para Windows.
Admite lineas, arcos y polilineas simples horizontales.
Los arcos se aproximan con error de cuerda maximo 0.001 unidades;
los planos entregados contienen segmentos rectos, sin arcos.
No aplana ejes con cotas variables. No prolonga ejes desconectados.
Las operaciones de union usan una precision de 0.000001 unidades.
Las esquinas extremadamente agudas se limitan a 10 veces el offset.

COMPROBACIONES
Probado con el motor AutoCAD 2024: capa azul, offset, T y cruces, coordenadas
originales intactas, anchos globales y por segmento cero, repeticion sin duplicados,
Deshacer/Rehacer (incluida la restauracion de los anchos originales) y reapertura
de ambos formatos. La comparacion incluye los ejemplos 3.2 y 4.2 de Stage 1.
Consulta Comparacion-Stage-1.png y los registros de la carpeta Validacion.
'@
[IO.File]::WriteAllText((Join-Path $DestinationDirectory 'LEEME.txt'), $instructions, [Text.Encoding]::UTF8)
$html = @'
<!doctype html><html lang="es"><meta charset="utf-8"><title>DrenBordes · Resultados</title>
<style>body{font:17px/1.65 system-ui,sans-serif;max-width:1080px;margin:40px auto;padding:0 24px;color:#203246;background:#f2f5fa}h1{font-size:32px}a{color:#1658c4}table{border-collapse:collapse;background:white;width:100%}td,th{padding:12px 18px;text-align:left;border-bottom:1px solid #dae1e9}img{width:100%;border-radius:12px;margin:18px 0}code{background:#e3eaf4;padding:2px 6px;border-radius:4px}.box{background:white;padding:20px 26px;border-radius:12px;margin:24px 0}</style>
<h1>Bordes de drenaje a 0.50</h1>
<p>Stage 1, Stage 2 y Stage 3 terminados. Offset de <b>0.50 a cada lado</b>, ancho total 1.00, capa <code>0_DCL_Bordes</code>, color <b>Blue</b>. Las uniones se ajustaron siguiendo los ejemplos 3.2 y 4.2 de Stage 1.</p>
<p><b>Ancho inicial, ancho final y ancho global: 0.000</b> en todas las polilíneas, incluidos los ejes centrales. Los ejes de drenaje conservan su elevación 0.000.</p>
<table><tr><th>Plano</th><th>Ejes</th><th>Encuentros</th><th>Archivos</th></tr>
<tr><td>Stage 1</td><td>53</td><td>51</td><td><a href="Resultados/Drainage%20Stage%201%20-%20Bordes%200.50.dwg">Abrir DWG</a> · <a href="Resultados/Drainage%20Stage%201%20-%20Bordes%200.50.dxf">Abrir DXF</a></td></tr>
<tr><td>Stage 2</td><td>72</td><td>71</td><td><a href="Resultados/Drainage%20Stage%202%20-%20Bordes%200.50.dwg">Abrir DWG</a> · <a href="Resultados/Drainage%20Stage%202%20-%20Bordes%200.50.dxf">Abrir DXF</a></td></tr>
<tr><td>Stage 3</td><td>85</td><td>82</td><td><a href="Resultados/Drainage%20Stage%203%20-%20Bordes%200.50.dwg">Abrir DWG</a> · <a href="Resultados/Drainage%20Stage%203%20-%20Bordes%200.50.dxf">Abrir DXF</a></td></tr></table>
<p>Los tres DXF de origen se conservaron intactos. Stage 1 se terminó en una nueva copia. Los rectángulos y las polilíneas elevadas permanecen como referencia, sin generarles bordes y con ancho visual cero.</p>
<div class="box"><h2>Repetirlo en otro dibujo</h2><ol>
<li>En Civil 3D 2024 ejecuta <code>NETLOAD</code> y elige <code>Herramienta/DrenBordes.2024.dll</code>. Conserva todos los DLL juntos.</li>
<li>Abre tu plano y ejecuta <code>DRENBORDES</code>.</li>
<li>Elige la capa de ejes, acepta <code>0.50</code> y la opción <code>Ejes</code>.</li>
<li>Revisa los bordes y guarda el dibujo con otro nombre.</li></ol>
<p>Si cargaste la versión anterior de la herramienta, reinicia Civil 3D antes de cargar la versión 0.1.1.</p>
<p>Puedes regenerarlos sin duplicados. Una <code>U</code> deshace el lote. Si hay bordes manuales, el comando los conserva y se detiene.</p>
<p><a href="LEEME.txt">Instrucciones completas y alcance de la herramienta</a></p></div>
<h2>Comparación con tu avance de Stage 1</h2><p>Izquierda: tus bordes manuales. Derecha: bordes generados por la herramienta en las mismas zonas.</p>
<img src="Comparacion-Stage-1.png" alt="Comparación de los ejemplos 3.2 y 4.2 de Stage 1 con el resultado automático">
<h2>Vista general de Stage 2 y Stage 3</h2><img src="Resultado-Stage-2-y-3.png" alt="Vista general de ambos planos procesados">
<p>Comprobado con AutoCAD 2024: generación, capa y color, repetición, Deshacer/Rehacer, conservación del origen y reapertura DXF/DWG.</p>
</html>
'@
[IO.File]::WriteAllText((Join-Path $DestinationDirectory 'ABRIR_RESULTADOS.html'), $html, [Text.Encoding]::UTF8)
$root = (Resolve-Path -LiteralPath $DestinationDirectory).Path
$manifest = Get-ChildItem -LiteralPath $root -File -Recurse | Sort-Object FullName | ForEach-Object {
    [PSCustomObject]@{ Path = $_.FullName.Substring($root.Length + 1); SHA256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash }
}
$manifest | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $root 'SHA256.json') -Encoding UTF8
Write-Output "Paquete preparado: $root"
