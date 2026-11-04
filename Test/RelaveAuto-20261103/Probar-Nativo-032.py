import pathlib,subprocess,json,os,concurrent.futures
repo=pathlib.Path(__file__).resolve().parents[2]
root=repo/'Test/RelaveAuto-20261103/Deteccion-12-entrega032'
console=pathlib.Path(r'C:\Program Files\Autodesk\AutoCAD 2024\accoreconsole.exe')
fixture=repo/'Test/RelaveAuto-20261102/Synthetic/modelo.dwg'
plugin=repo/'artifacts/RelaveAuto-Prueba-0.3.2/RelaveAuto.bundle/Contents/Windows/2024/RelaveAuto.2024.dll'
probe=repo/'tools/RelaveAuto/Tests/bin/Final-0.3.2/RelaveAuto.NativeProbe.dll'
def run(folder):
    script=folder/'nativo.scr'
    lines=['_.NETLOAD',f'"{plugin.as_posix()}"','_.NETLOAD',f'"{probe.as_posix()}"','RAEXPORTPROPOSALS','RAORIGIN','RAQUALITYGUARDS','RAIMAGEINDEPENDENCE','RAEDITAREA','_.QUIT','_Y','']
    script.write_text('\n'.join(lines),encoding='ascii')
    env=os.environ.copy();env['RELAVEAUTO_JOB']=str(folder/'job.json')
    log=folder/'nativo.log'
    with log.open('w',encoding='utf-8') as f:
        process=subprocess.run([str(console),'/i',str(fixture),'/s',str(script),'/isolate','RelaveAutoTest20261102',str(repo/'Test/RelaveAuto-20261102/profile'),'/l','en-US'],env=env,stdout=f,stderr=subprocess.STDOUT,creationflags=subprocess.CREATE_NO_WINDOW,timeout=120)
    result=folder/'Prueba_exportacion'
    required=['exportacion-propuestas-ok.txt','geometria-ok.txt','inicio-a-nativo-ok.txt','guardas-ok.txt','independencia-ok.txt','eliminar-area-ok.txt']
    missing=[p for p in required if not (result/p).exists()]
    row=dict(Trial=folder.name,ReturnCode=process.returncode,Missing=missing)
    if not missing:row['Summary']=(result/required[0]).read_text()
    print(json.dumps(row),flush=True);return row
# Reuse a single isolated AutoCAD profile sequentially; never touch the live Civil session.
rows=[run(p) for p in sorted(root.iterdir()) if p.is_dir() and ('Flume2' in p.name or 'Flume3' in p.name)]
(root/'native-audit.json').write_text(json.dumps(rows,indent=2),encoding='utf-8')
if any(r['Missing'] for r in rows):raise SystemExit(1)
