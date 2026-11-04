import json,pathlib
repo=pathlib.Path(__file__).resolve().parents[3]
root=pathlib.Path(__file__).resolve().parent
source=repo/'Test/RelaveAuto-20261103/Deteccion-12-entrega032/Talison-TSF2-Fine-Flume1-_S1-Flow3-45Hz/job.json'
job=json.loads(source.read_text(encoding='utf-8-sig'));out=root/'Native';out.mkdir(exist_ok=True);job['OutputFolder']=str(out)
(out/'job.json').write_text(json.dumps(job),encoding='utf-8')
plugin=repo/'artifacts/RelaveAuto-Prueba-0.3.4/RelaveAuto.bundle/Contents/Windows/2024/RelaveAuto.2024.dll'
probe=repo/'tools/RelaveAuto/Tests/bin/Final-0.3.4/RelaveAuto.NativeProbe.dll'
lines=['_.NETLOAD',f'"{plugin.as_posix()}"','_.NETLOAD',f'"{probe.as_posix()}"','RELAVEAUTOPRUEBA','RACHECKRESULT','RACOMPARISON','RAHEIGHTS','RANOFINALPHOTO','RAEDITLASTHEIGHT','RAORIGIN','RAIMAGEINDEPENDENCE','RAEDITAREA','RAQUALITYGUARDS','_.QUIT','_Y','']
(out/'prueba.scr').write_text('\n'.join(lines),encoding='ascii')
