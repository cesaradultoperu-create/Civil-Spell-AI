import os,pathlib,subprocess
test=pathlib.Path(__file__).resolve().parent
repo=test.parents[2]
env=os.environ.copy();env['RELAVEAUTO_JOB']=str(test/'Native/job.json')
with (test/'Native/console.log').open('w',encoding='utf-8') as log:
    process=subprocess.run([r'C:\Program Files\Autodesk\AutoCAD 2024\accoreconsole.exe','/i',str(repo/'Test/RelaveAuto-20261102/Synthetic/modelo.dwg'),'/s',str(test/'Native/comparison.scr'),'/isolate','RelaveAutoTest20261102',str(repo/'Test/RelaveAuto-20261102/profile'),'/l','en-US'],stdout=log,stderr=subprocess.STDOUT,env=env,creationflags=subprocess.CREATE_NO_WINDOW,timeout=90)
target=test/'Native/Prueba_exportacion'
required=['exportacion-propuestas-ok.txt','comparacion-exportacion-ok.txt','geometria-ok.txt','guardas-ok.txt']
for filename in required:
    path=target/filename
    if not path.exists():raise RuntimeError('Falta '+str(path))
    print(path.read_text())
if process.returncode:raise SystemExit(process.returncode)
