import os,pathlib,subprocess
root=pathlib.Path(__file__).resolve().parent;repo=root.parents[2]
env=os.environ.copy();env['RELAVEAUTO_JOB']=str(root/'Native-final/job.json');env['RELAVEAUTO_BULK_TEST']='1'
with (root/'Native-final/console.log').open('w',encoding='utf-8') as log:
    run=subprocess.run([r'C:\Program Files\Autodesk\AutoCAD 2024\accoreconsole.exe','/i',str(repo/'Test/RelaveAuto-20261102/Synthetic/modelo.dwg'),'/s',str(root/'Native-final/prueba.scr'),'/isolate','RelaveAutoTest20261102',str(repo/'Test/RelaveAuto-20261102/profile'),'/l','en-US'],env=env,stdout=log,stderr=subprocess.STDOUT,creationflags=subprocess.CREATE_NO_WINDOW,timeout=120)
required=['prueba-ok.txt','geometria-ok.txt','comparacion-exportacion-ok.txt','alturas-nativas-ok.txt','ultima-disponible-ok.txt','alturas-editadas-ok.txt','inicio-a-nativo-ok.txt','independencia-ok.txt','eliminar-area-ok.txt','guardas-ok.txt','agregar-area-nativa-ok.txt']
for filename in required:
    path=root/'Native-final'/filename
    if not path.exists():raise RuntimeError('Falta '+str(path))
    print(path.read_text(),flush=True)
if run.returncode:raise SystemExit(run.returncode)
