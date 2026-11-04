"""Archive the validated DLL, local installer, standalone source and finished plans."""
from pathlib import Path
import hashlib
import html
import json
import re
import shutil
import zipfile

REPO = Path(__file__).resolve().parents[2]
TOOLS = REPO / 'tools' / 'DrainageBorders'
VALIDATED = REPO / 'Test' / 'DrenBordes-20261020-v2'
PACKAGE = REPO / 'artifacts' / 'distribution' / 'DrenBordes-0.1.1-Civil3D2024'


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


def write(path, text, bom=False):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding='utf-8-sig' if bom else 'utf-8')


def copy(source, destination):
    destination.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(source, destination)
    if sha(source) != sha(destination):
        raise RuntimeError(f'Copy mismatch: {destination}')


def inline(line):
    line = html.escape(line)
    line = re.sub(r'\[([^\]]+)\]\(([^)]+)\)', r'<a href="\2">\1</a>', line)
    line = re.sub(r'\*\*(.+?)\*\*', r'<strong>\1</strong>', line)
    return re.sub(r'`([^`]+)`', r'<code>\1</code>', line)


def guide_html(markdown):
    body = []
    table = False
    for line in markdown.splitlines():
        if table and not line.startswith('|'):
            body.append('</table>'); table = False
        if not line.strip():
            continue
        if line.startswith('|'):
            if re.fullmatch(r'[| :\-]+', line):
                continue
            if not table:
                body.append('<table>'); table = True
            body.append('<tr>' + ''.join('<td>' + inline(c.strip()) + '</td>' for c in line.strip('|').split('|')) + '</tr>')
        elif line.startswith('#'):
            size = len(line) - len(line.lstrip('#'))
            body.append(f'<h{size}>' + inline(line[size:].strip()) + f'</h{size}>')
        else:
            body.append('<p>' + inline(line.strip()) + '</p>')
    if table:
        body.append('</table>')
    return '''<!doctype html><html lang="es"><meta charset="utf-8"><title>DrenBordes 0.1.1 — Guía</title>
<style>body{font:17px/1.6 system-ui,sans-serif;color:#1b3447;background:#f4f7fa;max-width:1040px;margin:40px auto;padding:0 24px}h1{font-size:30px}h2{margin-top:40px;color:#16568c}code{background:#e1eaf3;padding:2px 5px;border-radius:4px;overflow-wrap:anywhere}table{border-collapse:collapse;width:100%;background:white}td{padding:9px 12px;border-bottom:1px solid #d8e1e9}tr:first-child{font-weight:bold;background:#e3edf7}a{color:#145bc1}p{overflow-wrap:break-word}</style>
''' + '\n'.join(body) + '</html>\n'


def manifest(root):
    return [{'Path': str(p.relative_to(root)), 'SHA256': sha(p)} for p in sorted(root.rglob('*')) if p.is_file()]


def build():
    if PACKAGE.exists():
        raise FileExistsError(PACKAGE)
    for entry in json.loads((VALIDATED / 'SHA256.json').read_text(encoding='utf-8-sig')):
        if sha(VALIDATED / entry['Path']) != entry['SHA256']:
            raise RuntimeError('Validated delivery changed: ' + entry['Path'])
    bundle = PACKAGE / 'DrenBordes.bundle'
    binaries = bundle / 'Contents' / 'Windows' / '2024'
    for source in (VALIDATED / 'Herramienta').glob('*.dll'):
        copy(source, binaries / source.name)
    licenses = bundle / 'Contents' / 'Resources' / 'Licencias'
    for name in ['NetTopologySuite-License.md', 'DotNetRuntime-License.txt']:
        copy(VALIDATED / 'Herramienta' / 'Licencias' / name, licenses / name)
    write(licenses / 'THIRD_PARTY_NOTICES.txt', '''DrenBordes 0.1.1
NetTopologySuite 2.6.0: BSD-3-Clause y avisos incluidos en NetTopologySuite-License.md.
System.Memory 4.6.0, System.Buffers 4.6.0, System.Numerics.Vectors 4.6.0,
System.Runtime.CompilerServices.Unsafe 6.1.0: Microsoft/.NET Foundation, MIT,
licencia incluida en DotNetRuntime-License.txt.
No se distribuyen DLL de Autodesk: se usan las de Civil 3D 2024 instalado.
''')
    copy(TOOLS / 'installer' / 'PackageContents.xml', bundle / 'PackageContents.xml')
    for name in ['Instalar-DrenBordes.ps1', 'INSTALAR.cmd']:
        copy(TOOLS / 'installer' / name, PACKAGE / name)
    guide = (TOOLS / 'installer' / 'GUIA.md').read_text(encoding='utf-8')
    rendered = guide_html(guide)
    write(PACKAGE / 'GUIA_INSTALACION_Y_USO.md', guide)
    write(PACKAGE / 'ABRIR_GUIA.html', rendered)
    write(bundle / 'Contents' / 'Resources' / 'AYUDA.html', rendered)
    write(PACKAGE / 'EMPEZAR_AQUI.txt', '''DRENBORDES 0.1.1 PARA CIVIL 3D 2024

1. Abre ABRIR_GUIA.html: explica instalacion, carga, uso y solucion de problemas.
2. Cierra Civil 3D y ejecuta INSTALAR.cmd.
3. Abre Civil 3D 2024 y configura la ubicacion de confianza indicada en la guia.
4. Reinicia Civil 3D y ejecuta DRENBORDES.

No necesitas Visual Studio para usarlo. Los tres planos ya terminados estan en
PlanosTerminados. CodigoFuente incluye el proyecto independiente y sus paquetes.
La unidad de red guarda este archivo; el instalador copia el complemento al equipo.
''', bom=True)
    for source in (VALIDATED / 'Resultados').iterdir():
        copy(source, PACKAGE / 'PlanosTerminados' / source.name)
    for source in (VALIDATED / 'Validacion').iterdir():
        copy(source, PACKAGE / 'Validacion' / source.name)
    for name in ['Comparacion-Stage-1.png', 'Resultado-Stage-2-y-3.png']:
        copy(VALIDATED / name, PACKAGE / 'Ejemplos' / name)
    code = PACKAGE / 'CodigoFuente'
    for name in ['Commands.cs', 'DrawingBorders.cs', 'NetworkBorders.cs']:
        copy(TOOLS / name, code / name)
    project = (TOOLS / 'DrenBordes.csproj').read_text(encoding='utf-8')
    project = project.replace('<RestorePackagesPath>../../.packages</RestorePackagesPath>',
        '<RestorePackagesPath>$(MSBuildProjectDirectory)/.packages</RestorePackagesPath>\n    <NuGetAudit>false</NuGetAudit>\n    <Autodesk2024Dir Condition="\'$(Autodesk2024Dir)\' == \'\'">$(ProgramW6432)/Autodesk/AutoCAD 2024</Autodesk2024Dir>')
    project = re.sub(r'  <PropertyGroup Condition="\'\$\(IncludeNativeTests\)\' == \'true\'">.*?</PropertyGroup>\n', '', project, flags=re.S)
    project = re.sub(r'^.*<Compile (?:Remove|Include)="NativeTests.cs"[^\n]*\n', '', project, flags=re.M)
    project = project.replace('$(ProgramW6432)/Autodesk/AutoCAD 2024/', '$(Autodesk2024Dir)/')
    write(code / 'DrenBordes.csproj', project)
    write(code / 'NuGet.config', '''<?xml version="1.0" encoding="utf-8"?>
<configuration><packageSources><clear/><add key="Paquetes incluidos" value="Paquetes"/></packageSources></configuration>
''')
    assets = json.loads((TOOLS / 'obj' / 'project.assets.json').read_text(encoding='utf-8'))
    for key in assets['libraries']:
        for nupkg in (REPO / '.packages' / key.lower()).glob('*.nupkg'):
            copy(nupkg, code / 'Paquetes' / nupkg.name)
    write(PACKAGE / 'BUNDLE-SHA256.json', json.dumps(manifest(bundle), indent=2))
    write(PACKAGE / 'SHA256.json', json.dumps(manifest(PACKAGE), indent=2))
    print(PACKAGE)


def finalize():
    write(PACKAGE / 'SHA256.json', json.dumps([e for e in manifest(PACKAGE) if e['Path'] != 'SHA256.json'], indent=2))
    zip_path = Path(str(PACKAGE) + '.zip')
    if zip_path.exists():
        raise FileExistsError(zip_path)
    with zipfile.ZipFile(zip_path, 'w', zipfile.ZIP_DEFLATED) as archive:
        for file in sorted(PACKAGE.rglob('*')):
            if file.is_file():
                archive.write(file, str(Path(PACKAGE.name) / file.relative_to(PACKAGE)))
    write(Path(str(zip_path) + '.sha256'), sha(zip_path) + '  ' + zip_path.name + '\n')
    print(zip_path)


if __name__ == '__main__':
    import sys
    finalize() if '--finalize' in sys.argv else build()
