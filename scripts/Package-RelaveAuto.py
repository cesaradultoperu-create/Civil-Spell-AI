"""Create portable and source archives from an already built RELAVEAUTO release."""
from pathlib import Path
import csv
import hashlib
import shutil
import sys
import zipfile

repo = Path(__file__).resolve().parents[1]
version = sys.argv[1]
package = repo / f'artifacts/RelaveAuto-{version}'
full = repo / f'artifacts/RelaveAuto-{version}-Respaldo'
if full.exists():
    raise SystemExit('El respaldo local ya existe; revisar antes de repetir.')
shutil.copytree(package, full)
previous = repo / 'artifacts/RelaveAuto-0.3.4-Entrega-aprobada'
shutil.copytree(previous / 'Plantilla', full / 'Plantilla')
source = full / 'Codigo_fuente/tools/RelaveAuto'
allowed = {'.cs', '.csproj', '.props', '.xml', '.txt', '.ps1', '.cmd', '.py'}
for file in (repo / 'tools/RelaveAuto').rglob('*'):
    relative = file.relative_to(repo / 'tools/RelaveAuto')
    if file.is_file() and file.suffix in allowed and not any(p in {'bin', 'obj'} for p in relative.parts):
        target = source / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(file, target)
scripts = full / 'Codigo_fuente/scripts'
scripts.mkdir(parents=True)
for name in ['Build-RelaveAuto.ps1', 'Package-RelaveAuto.py']:
    shutil.copy2(repo / 'scripts' / name, scripts / name)
for name in ['RelaveAuto.sln', 'COMPILAR.txt']:
    old = previous / 'Codigo_fuente' / name
    text = old.read_text(encoding='utf-8-sig').replace('0.3.4', version).replace(f'RelaveAuto-Prueba-{version}', f'RelaveAuto-{version}')
    (full / 'Codigo_fuente' / name).write_text(text, encoding='utf-8')
share = repo / f'artifacts/RelaveAuto-{version}-Para-compartir'
if share.exists():
    raise SystemExit('La carpeta para compartir ya existe.')
shutil.copytree(package, share)
shutil.copytree(full / 'Plantilla', share / 'Plantilla')

def sha(file):
    return hashlib.sha256(file.read_bytes()).hexdigest().upper()

for folder, suffix in [(full, 'Respaldo'), (share, 'PARA_COMPARTIR')]:
    files = sorted(p for p in folder.rglob('*') if p.is_file())
    with (folder / 'MANIFIESTO_SHA256.csv').open('w', encoding='utf-8-sig', newline='') as stream:
        writer = csv.writer(stream)
        writer.writerow(['Archivo', 'Bytes', 'SHA256'])
        writer.writerows([str(p.relative_to(folder)), p.stat().st_size, sha(p)] for p in files)
    archive = repo / f'artifacts/RELAVEAUTO_{version}_Civil3D_2023-2026_{suffix}.zip'
    if archive.exists():
        raise SystemExit('El ZIP ya existe: ' + str(archive))
    with zipfile.ZipFile(archive, 'w', zipfile.ZIP_DEFLATED) as zipout:
        for file in folder.rglob('*'):
            if file.is_file():
                zipout.write(file, file.relative_to(folder).as_posix())
    with zipfile.ZipFile(archive) as zipin:
        assert zipin.testzip() is None
        for file in folder.rglob('*'):
            if file.is_file():
                assert hashlib.sha256(zipin.read(file.relative_to(folder).as_posix())).hexdigest().upper() == sha(file)
    print(f'{archive.name}: {archive.stat().st_size} bytes; contenido verificado por SHA256.')
