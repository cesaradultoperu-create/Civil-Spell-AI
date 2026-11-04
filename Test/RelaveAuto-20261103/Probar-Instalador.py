import pathlib,subprocess,hashlib,json,sys
repo=pathlib.Path(__file__).resolve().parents[2]
version=sys.argv[1] if len(sys.argv)>1 else '0.3.3'
package=repo/f'artifacts/RelaveAuto-{version}-Respaldo'
root=repo/f'Test/RelaveAuto-20261103/Instalador-{version}'
target=root/'Instalacion-nueva/RelaveAuto.bundle'
root.mkdir(exist_ok=True)
def hashes(folder):
    return {str(p.relative_to(folder)):hashlib.sha256(p.read_bytes()).hexdigest() for p in folder.rglob('*') if p.is_file()}
def install():
    return subprocess.run(['powershell.exe','-NoProfile','-ExecutionPolicy','Bypass','-File',str(package/'Instalar.ps1'),'-Destino',str(target)],capture_output=True,text=True,creationflags=subprocess.CREATE_NO_WINDOW)
first=install();(root/'primera-instalacion.log').write_text(first.stdout+first.stderr,encoding='utf-8')
assert first.returncode==0,first.stderr
source=hashes(package/'RelaveAuto.bundle');assert source==hashes(target)
second=install();(root/'reinstalacion-identica.log').write_text(second.stdout+second.stderr,encoding='utf-8')
assert second.returncode==0 and source==hashes(target)
# Only alter the disposable installation under the explicit workspace test root.
manifest=target/'PackageContents.xml'
assert manifest.resolve().is_relative_to(root.resolve()) if hasattr(pathlib.Path,'is_relative_to') else str(manifest.resolve()).startswith(str(root.resolve())+'\\')
manifest.write_text(manifest.read_text(encoding='utf-8').replace(f'AppVersion="{version}"','AppVersion="0.0.0"'),encoding='utf-8')
conflict=hashes(target)
third=install();(root/'version-distinta-bloqueada.log').write_text(third.stdout+third.stderr,encoding='utf-8')
assert third.returncode!=0 and conflict==hashes(target)
summary=dict(FirstInstall='OK',IdenticalInstall='OK',DifferentInstall='Blocked without overwriting',BundleFiles=len(source),DefaultAppDataChanged=False)
(root/'instalador-ok.json').write_text(json.dumps(summary,indent=2),encoding='utf-8');print(json.dumps(summary))
