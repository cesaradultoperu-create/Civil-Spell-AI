"""Prepare copies of validated deliveries without rebuilding or installing them."""
import hashlib
import html
import json
import shutil
import uuid
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
STAGE = ROOT / 'artifacts' / ('programas-ordenados-20261014-' + uuid.uuid4().hex[:8])
STAGE.mkdir(parents=True)
ORIGINS = {}


def digest(path):
    h = hashlib.sha256()
    with path.open('rb') as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b''):
            h.update(block)
    return h.hexdigest().upper()


def write(relative, value):
    path = STAGE / relative
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(value, encoding='utf-8-sig')


def copy(source, relative):
    source = ROOT / source
    target = STAGE / relative
    if source.is_dir():
        for child in sorted(source.rglob('*')):
            if child.is_file():
                copy(child, target / child.relative_to(source))
        return
    if not source.is_file():
        raise FileNotFoundError(source)
    target.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(str(source), str(target))
    if digest(source) != digest(target):
        raise RuntimeError('Copy mismatch: ' + str(source))
    ORIGINS[target.relative_to(STAGE).as_posix()] = str(source)


def verify_zip(relative):
    path = ROOT / relative
    expected = Path(str(path) + '.sha256').read_text(encoding='utf-8-sig').split()[0]
    if digest(path) != expected.upper():
        raise RuntimeError('Original ZIP checksum mismatch: ' + str(path))
    with zipfile.ZipFile(str(path)) as archive:
        if archive.testzip() is not None:
            raise RuntimeError('Original ZIP damaged: ' + str(path))


verify_zip('artifacts/distribution/CivilSpellAI-1.3.7.0.zip')
verify_zip('artifacts/entrega-tramos-dwg/CivilSpellAI-TramosDWG-1.3.3.2.zip')
tin = ROOT / 'Test/BordeTIN3D-20261014-v0.1.3'
for line in (tin / 'SHA256.txt').read_text(encoding='utf-8-sig').splitlines():
    expected, relative = line.split(None, 1)
    if digest(tin / relative) != expected.upper():
        raise RuntimeError('TIN checksum mismatch: ' + relative)

# Explicit project directories; no .git, machine settings, build caches or credentials.
source_dirs = ['Application', 'Autodesk', 'Commands', 'Domain', 'Geometry',
               'Infrastructure', 'Properties', 'Resources', 'Spell', 'Tests',
               'UI', 'docs', 'scripts', 'packaging']
source_files = []
for directory in source_dirs:
    for path in (ROOT / directory).rglob('*'):
        rel = path.relative_to(ROOT)
        if path.is_file() and not any(part.lower() in ('bin', 'obj', '.vs', '.git', '.packages')
                                      for part in rel.parts):
            if not (path.name.endswith(('.user', '.suo', '.log')) or '.local.' in path.name
                    or path.name.startswith('.env')):
                source_files.append(path)
source_files.extend(p for p in ROOT.iterdir() if p.is_file()
                    and (p.suffix in ('.csproj', '.slnx', '.props')
                         or p.name in ('README.md', 'THIRD_PARTY_NOTICES.md', '.gitignore')))
source_zip = STAGE / 'CivilSpellAI-Codigo-20261014.zip'
with zipfile.ZipFile(str(source_zip), 'w', zipfile.ZIP_DEFLATED) as archive:
    for path in sorted(set(source_files)):
        archive.write(str(path), path.relative_to(ROOT).as_posix())
    archive.writestr('LEEME_CODIGO.txt',
        'Captura del codigo de trabajo al 14/10/2026. Abrir CivilSpellAI.slnx.\n'
        'Incluye BORDETIN3D y cambios posteriores a la entrega binaria 1.3.7.0.\n'
        'No se afirma que recompilar reproduzca byte por byte esa entrega.\n'
        'Restaurar paquetes NuGet. Requiere SDK/targeting packs y referencias Autodesk.\n'
        'Al cambiar de ubicacion, revisar HintPath de AutoCAD en CivilSpellAI.csproj.\n'
        'Los binarios comprobados se conservan aparte en Programa; no se recompilaron.\n')

COMMON_PROGRAM = 'artifacts/distribution/CivilSpellAI-1.3.7.0'
COMMON_NOTE = ('Programa contiene el paquete completo CivilSpellAI 1.3.7.0, ya instalado '
               'y verificado el 12/10/2026. Corrector, MLeader, secciones y figuras 3D '
               'comparten la misma DLL: cada carpeta guarda una copia completa del mismo '
               'paquete para poder conservarla individualmente. Se instala una sola vez.\n'
               'Para reinstalar: cerrar Civil 3D y ejecutar desde Programa:\n'
               'powershell -NoProfile -ExecutionPolicy Bypass -File .\\Manage-CivilSpellAI.ps1 '
               '-Action Install -Scope AllUsers\n'
               'El alcance AllUsers requiere administrador. El archivo original LEAME.md '
               'del paquete conserva algunas menciones antiguas a 1.3.4.0; la version '
               'real de la DLL y PackageContents.xml es 1.3.7.0.\n'
               'Pruebas nativas en 2024; variantes 2025/2026 compiladas, sin prueba nativa.\n'
               'Codigo_fuente.zip es la captura actual del proyecto compartido del '
               '14/10/2026; leer LEEME_CODIGO.txt al extraerla.\n')

WORKS = [
    dict(folder='01_Corrector_de_textos', title='Corrector de textos', version='1.3.7.0',
         commands='AISPELL, AISPELLALL, AISPELLSETTINGS', common=True,
         status='Correccion local ES/EN y flujo interactivo confirmados; paquete instalado y verificado.',
         usage='AISPELL revisa un texto. AISPELLALL revisa el conjunto y permite elegir/aplicar cambios. '
               'AISPELLSETTINGS configura diccionarios y proveedores. La revision local funciona sin Internet. '
               'OpenAI necesita configuracion propia; LanguageTool necesita su servidor local aparte.',
         docs=['docs/testing/PILOT_1_2_EXTENDED_TEXT_RUNBOOK.md'],
         files=['Commands/AiSpellCommand.cs', 'Commands/AiSpellAllCommand.cs',
                'Commands/AiSpellSettingsCommand.cs', 'Spell/', 'Application/']),
    dict(folder='02_Guias_angulares_MLeader', title='Guias angulares de MLeader', version='1.3.7.0',
         commands='Automatico al arrastrar el pinzamiento de flecha de un MLeader', common=True,
         status='Guias, cambio de lado, cancelacion, una U y autocarga confirmados en 2024.',
         usage='Seleccionar un MLeader y arrastrar el pinzamiento de la flecha. Aparecen 24 guias '
               'cada 15 grados. Esc cancela; U revierte. La matriz completa de angulos y '
               'MLeader con varias directrices conserva casos pendientes en la guia de validacion.',
         docs=['docs/testing/MLEADER_POLAR_TRACKING_RUNBOOK.md'],
         files=['Autodesk/MLeaderPolarTrackingApplication.cs']),
    dict(folder='03_Limpieza_de_polilineas', title='Limpieza de solapes y tramos diminutos', version='1.3.3.2',
         commands='REVISARTRAMOSDWG, RESTAURARTRAMOSDWG', common=False,
         status='Probado sobre CORTE completo en Core Console 2024; restauracion y reapertura verificadas.',
         usage='Entrega independiente TramosDWG. Seguir Programa/LEEME.txt. Analizar > Modelo, '
               'revisar limite de tramos, Limpiar y Comprobar. Incluye instalador propio. '
               'En 2023 esta compilado pero falta prueba nativa; en 2024 la autocarga del '
               'nuevo bundle requiere comprobacion tras reiniciar. No modifica TIN ni polilineas 3D.',
         docs=['docs/testing/POLYLINE_TINY_SEGMENTS_20261006.md', 'docs/GUIA_CORREGIR_SOLAPES.md'],
         files=['CivilSpellAI.Overlap2023.csproj', 'Commands/PolylineOverlapCommands.cs',
                'Commands/PolylineOverlapEnhancements.cs', 'Commands/PolylineOverlapCorrection.cs',
                'Commands/PolylineReviewWorkflow.cs', 'Commands/PolylineSafeCleanup.cs']),
    dict(folder='04_Secciones_y_medidas_editables', title='Secciones y medidas editables', version='1.3.7.0',
         commands='CREARSECCION, EDITARSECCION', common=True,
         status='Edicion de medidas confirmada; secciones, cotas, copia, U y navegacion comprobadas en 2024.',
         usage='CREARSECCION crea una seccion parametrica. EDITARSECCION permite elegir el bloque, '
               'una cota o una seleccion de objetos. Revisar medidas en la vista previa y aceptar. '
               'Se conservan las funciones de zoom y desplazamiento.',
         docs=['docs/testing/PARAMETRIC_SECTION_RUNBOOK.md'],
         files=['Commands/ParametricSectionCommands.cs', 'Autodesk/ParametricSectionDrawingService.cs',
                'Autodesk/SelectedFigureDrawingService.cs', 'Autodesk/FigureMeasureDrawingService.cs',
                'UI/TransitionSectionWindow.xaml', 'UI/TransitionSectionWindow.xaml.cs',
                'UI/SelectedFigureWindow.cs']),
    dict(folder='05_Figuras_3D_desde_TOP_FRONT', title='Figuras 3D desde TOP y FRONT', version='1.3.7.0',
         commands='CREARFIGURA3D, EDITARFIGURA3D, CREARCOMPONENTE3D', common=True,
         status='Tres parejas y regresiones previas comprobadas en 2024; resaltado confirmado por el usuario.',
         usage='Seleccionar TOP completo, Enter, Referencias, seleccionar FRONT correspondiente, Enter. '
               'Revisar y crear. EDITARFIGURA3D permite editar y resaltar piezas. El reconocimiento '
               'depende de referencias compatibles. Un corte circular aislado no determina pendiente '
               'longitudinal; completar cotas. Sigue pendiente la comparacion visual del ultimo '
               'reconocimiento 1.3.7.0 por el usuario.',
         docs=['docs/testing/REFERENCE_ASSEMBLY_3D_RUNBOOK.md',
               'docs/codex/SESSION_2026-10-12_TOP_FRONT.md'],
         files=['Commands/Figure3DCommands.cs', 'Commands/ReferenceAssembly3DCommands.cs',
                'Autodesk/ReferenceAssemblyDrawingService.cs', 'Autodesk/DrainageReference3DAnalyzer.cs',
                'UI/ReferenceAssembly3DWindow.cs']),
    dict(folder='06_Bordes_TIN_3D', title='BORDETIN3D: exterior y espacios vacios', version='0.1.3',
         commands='BORDETIN3D', common=False,
         status='Usuario confirmo exterior y dos interiores Pick Points correctos el 14/10/2026.',
         usage='NETLOAD > Programa/2024/CivilSpellAI.BordeTin3D.v013.dll. Mantener las seis DLL juntas. '
               'Seleccion genera el exterior; PickPoints > Visibles permite elegir espacios vacios '
               'y Enter genera los contornos. Tolerancia usada: 0.05. Color magenta, 3D cerrada. '
               'Paquete independiente para Civil 3D 2024.',
         docs=['docs/testing/TIN_BOUNDARY_3D_RUNBOOK.md'],
         files=['CivilSpellAI.BordeTin3D.csproj', 'Directory.Build.props',
                'Commands/TinBoundaryCommands.cs', 'Autodesk/TinBoundaryDrawingService.cs',
                'Geometry/TinBoundaryGeometry.cs', 'Geometry/TinBoundaryUnion.cs',
                'Geometry/TinBoundaryDependencies.cs']),
    dict(folder='07_Marcador_de_cortes_CORTE2P', title='Marcador de cortes con dos puntos', version='FINAL / 10.0.0.0',
         commands='CORTE2P', common=False,
         status='Version FINAL conservada; instrucciones registran auditoria sin errores e insercion probada.',
         usage='Abrir el DWG FINAL de Ejemplos, NETLOAD > Programa/SectionMarkerToolFINAL.dll, '
               'CORTE2P, indicar centro y extremo/direccion. El bloque dinamico ya esta preparado; '
               'conservar ese DWG junto con la DLL. Consultar Documentacion/INSTRUCCIONES FINAL.txt.',
         docs=[], files=['SectionMarkerTool.csproj', 'SectionMarkerCommands.cs', 'Properties/AssemblyInfo.cs'])
]

for work in WORKS:
    folder = Path(work['folder'])
    (STAGE / folder).mkdir()
    text = (work['title'] + '\nVersion guardada: ' + work['version'] + '\nFecha: 14/10/2026\n\n'
            + 'COMANDOS\n' + work['commands'] + '\n\nUSO\n' + work['usage']
            + '\n\nVALIDACION REGISTRADA\n' + work['status'] + '\n\n')
    if work['common']:
        copy(COMMON_PROGRAM, folder / 'Programa')
        copy('artifacts/distribution/CivilSpellAI-1.3.7.0.zip', folder / 'CivilSpellAI-1.3.7.0.zip')
        copy('artifacts/distribution/CivilSpellAI-1.3.7.0.zip.sha256', folder / 'CivilSpellAI-1.3.7.0.zip.sha256')
        copy(source_zip, folder / 'Codigo_fuente.zip')
        text += COMMON_NOTE
        copy('artifacts/validation/figure3d/reference-installation-1.3.7-20261012.json',
             folder / 'Documentacion/INSTALACION_VERIFICADA_1.3.7.0.json')
    for document in work['docs']:
        copy(document, folder / 'Documentacion' / Path(document).name)
    write(folder / 'LEEME.txt', text)
    write(folder / 'ARCHIVOS_PRINCIPALES.txt', '\n'.join(work['files']) + '\n')

tramos = Path(WORKS[2]['folder'])
copy('artifacts/entrega-tramos-dwg/CivilSpellAI-TramosDWG-1.3.3.2', tramos / 'Programa')
for extension in ('.zip', '.zip.sha256'):
    copy('artifacts/entrega-tramos-dwg/CivilSpellAI-TramosDWG-1.3.3.2' + extension,
         tramos / ('CivilSpellAI-TramosDWG-1.3.3.2' + extension))
for source in WORKS[2]['files'] + ['Directory.Build.props', 'scripts/Build-Overlap2023Bundle.ps1']:
    copy(source, tramos / 'Codigo_fuente' / source)
write(tramos / 'Codigo_fuente/COMPILAR.txt',
      'Abrir CivilSpellAI.Overlap2023.csproj en Visual Studio.\n'
      'SDK AutoCAD.NET 24.2.0 por NuGet; .NET Framework 4.8, x64.\n'
      'dotnet restore CivilSpellAI.Overlap2023.csproj --packages .packages\n'
      'dotnet build CivilSpellAI.Overlap2023.csproj -c Release\n'
      'El script de empaquetado original depende del repositorio completo.\n'
      'El paquete listo para usar esta en Programa; no necesita recompilar.\n')
for name in ('CORTE-tramos-corregidos-v1_3_3_2.dwg', 'comparacion-cuadro-antes-despues.png',
             'cleanup-comparison.json'):
    copy('artifacts/validation/overlap-corte-20261006/' + name, tramos / 'Ejemplos' / name)

sections = Path(WORKS[3]['folder'])
copy('Test/Drawing3D/Pruebas-Vista-20261002/1107-P-011-MEDIDAS.dwg', sections / 'Ejemplos/1107-P-011-MEDIDAS.dwg')
copy('Test/Drawing3D/Pruebas-Medidas-20261002/Editor-medidas.png', sections / 'Ejemplos/Editor-medidas.png')

figures = Path(WORKS[4]['folder'])
figure_source = ROOT / 'Test/Drawing3D/Reconocimiento-TOP-FRONT-20261012'
for name in ['Tres-parejas-TOP-FRONT.dwg'] + [p.name for p in figure_source.glob('reported-pair-*')]:
    copy(figure_source / name, figures / 'Ejemplos' / name)
copy(figure_source / 'LEEME.md', figures / 'Documentacion/GUIA_TOP_FRONT_1.3.7.0.md')

border = Path(WORKS[5]['folder'])
copy(tin, border / 'Programa')
for source in WORKS[5]['files'] + ['Tests/TinBoundaryNativeCommands.cs', 'Tests/TinBoundarySessionProbe.cs',
                                    'THIRD_PARTY_NOTICES.md']:
    copy(source, border / 'Codigo_fuente' / source)
copy('Resources/Licenses/TinBoundary', border / 'Codigo_fuente/Resources/Licenses/TinBoundary')
write(border / 'Codigo_fuente/COMPILAR.txt',
      'Abrir CivilSpellAI.BordeTin3D.csproj en Visual Studio.\n'
      '.NET Framework 4.8, x64, AutoCAD/Civil 3D 2024 instalado.\n'
      'dotnet restore CivilSpellAI.BordeTin3D.csproj --packages .packages\n'
      'dotnet build CivilSpellAI.BordeTin3D.csproj -c Release\n'
      'Las referencias Autodesk se leen de ProgramW6432/Autodesk/AutoCAD 2024.\n'
      'Conservar las seis DLL de la salida juntas. El paquete ya probado esta en Programa.\n')

marker = Path(WORKS[6]['folder'])
marker_source = ROOT / 'artifacts/dynamic-block-work'
copy(marker_source / 'SectionMarkerTool/bin/Release/SectionMarkerToolFINAL.dll', marker / 'Programa/SectionMarkerToolFINAL.dll')
for source in WORKS[6]['files']:
    copy(marker_source / 'SectionMarkerTool' / source, marker / 'Codigo_fuente' / source)
write(marker / 'Codigo_fuente/COMPILAR.txt',
      'Abrir SectionMarkerTool.csproj en Visual Studio.\n'
      '.NET Framework 4.8, configuracion Release, plataforma x64.\n'
      'Requiere las referencias de AutoCAD 2024 instaladas en C:/Program Files/Autodesk/AutoCAD 2024.\n'
      'Conservar tambien el DWG FINAL: contiene la definicion del bloque dinamico usado por CORTE2P.\n')
copy(marker_source / 'INSTRUCCIONES FINAL.txt', marker / 'Documentacion/INSTRUCCIONES FINAL.txt')
for extension in ('.dwg', '.thumbnail.png', '.inspect.txt'):
    name = 'Dinamico corte en planta - USUARIO FINAL' + extension
    copy(marker_source / name, marker / 'Ejemplos' / name)

# The source checkpoint is duplicated per shared tool so each folder can be copied alone.
source_zip.unlink()
write('LEEME.txt',
      'PROGRAMAS CIVIL 3D - ARCHIVO ORGANIZADO - 14/10/2026\n\n'
      'Abrir INDICE.html para ver los siete trabajos y sus comandos.\n'
      'Cada trabajo tiene LEEME.txt, Programa, codigo fuente y documentacion.\n'
      'Los ejemplos disponibles se conservan como copias. No se modificaron los originales.\n'
      'Corrector, MLeader, secciones y figuras 3D comparten CivilSpellAI 1.3.7.0;\n'
      'cada carpeta contiene una copia del mismo paquete completo. Instalarlo una sola vez.\n'
      'TramosDWG, BORDETIN3D y CORTE2P conservan sus entregas independientes.\n'
      'Se preservaron los binarios entregados sin recompilarlos ni instalarlos.\n'
      'Las guias distinguen confirmacion interactiva, pruebas automaticas y pendientes.\n'
      'La fuente compartida es la captura actual del 14/10, posterior al binario 1.3.7.0.\n'
      'No se incluyeron versiones antiguas descartadas ni proyectos de practica sin validacion.\n'
      'Los LISP SeparadorDiqueTerreno y otros prototipos no tienen confirmacion de cierre registrada.\n'
      'CATALOGO.json registra versiones y procedencia; SHA256.json registra todos los archivos.\n'
      'Verificar: powershell -NoProfile -ExecutionPolicy Bypass -File .\\VERIFICAR_ARCHIVO.ps1\n')
write('CATALOGO.json', json.dumps(dict(date='2026-10-14', works=WORKS, origins=ORIGINS),
                                    ensure_ascii=False, indent=2))
rows = []
for work in WORKS:
    folder = work['folder']
    rows.append('<tr><td><a href="{0}/LEEME.txt">{1}</a></td><td>{2}</td><td>{3}</td>'
                '<td><a href="{0}/Programa/">Programa</a> · <a href="{0}/Documentacion/">Guías</a>'
                '</td></tr>'.format(folder, html.escape(work['title']), html.escape(work['version']),
                                    html.escape(work['commands'])))
write('INDICE.html', '<!doctype html><html lang="es"><meta charset="utf-8"><title>Programas Civil 3D</title>'
      '<style>body{font:16px system-ui;max-width:1200px;margin:40px auto;padding:20px;color:#183044}'
      'table{border-collapse:collapse;width:100%}td,th{padding:14px;border-bottom:1px solid #ddd;text-align:left}'
      'a{color:#1767a5}h1{font-size:28px}</style><h1>Programas para Civil 3D</h1>'
      '<p>Archivo organizado el 14/10/2026. Abre el nombre de cada trabajo para leer sus instrucciones.</p>'
      '<table><tr><th>Trabajo</th><th>Versión</th><th>Comandos</th><th>Archivos</th></tr>'
      + ''.join(rows) + '</table><p>Corrector, MLeader, secciones y figuras 3D comparten CivilSpellAI 1.3.7.0. '
      'Cada carpeta conserva una copia completa; se instala una sola vez. Las guías detallan las pruebas '
      'realizadas y sus límites.</p><p><a href="LEEME.txt">Información del archivo</a></p></html>')
write('VERIFICAR_ARCHIVO.ps1', r'''$ErrorActionPreference = 'Stop'
$manifest = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'SHA256.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$count = 0
foreach ($entry in $manifest.Files) {
    $path = Join-Path $PSScriptRoot $entry.Path
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Falta: $($entry.Path)" }
    if ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $entry.SHA256) { throw "No coincide: $($entry.Path)" }
    $count++
}
Write-Output "VERIFICACION CORRECTA: $count archivos."
''')
entries = [dict(Path=p.relative_to(STAGE).as_posix(), Length=p.stat().st_size, SHA256=digest(p))
           for p in sorted(STAGE.rglob('*')) if p.is_file()]
write('SHA256.json', json.dumps(dict(Date='2026-10-14', Files=entries), ensure_ascii=False, indent=2))
print(json.dumps(dict(stage=str(STAGE), works=len(WORKS), files=len(entries) + 1,
                      bytes=sum(p.stat().st_size for p in STAGE.rglob('*') if p.is_file())), ensure_ascii=False))
