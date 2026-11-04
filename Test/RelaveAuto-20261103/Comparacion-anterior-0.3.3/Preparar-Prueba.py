import json,pathlib
repo=pathlib.Path(__file__).resolve().parents[3]
test=pathlib.Path(__file__).resolve().parent
source=repo/'Test/RelaveAuto-20261103/Deteccion-12-entrega032/Talison-TSF2-Fine-Flume1-_S1-Flow3-45Hz/job.json'
job=json.loads(source.read_text(encoding='utf-8-sig'))
target=test/'Native';target.mkdir(exist_ok=True);job['OutputFolder']=str(target)
(target/'job.json').write_text(json.dumps(job),encoding='utf-8')
# The serialized batch is the exact data AFTER displaying, toggling and changing
# the previous-reference view; the UI harness also compared it byte for byte
# against the batch before those visual operations.
(target/'resultado-vision.json').write_bytes((test/'UI-final/comparacion-despues.json').read_bytes())
plugin=repo/'artifacts/RelaveAuto-Prueba-0.3.3/RelaveAuto.bundle/Contents/Windows/2024/RelaveAuto.2024.dll'
probe=repo/'tools/RelaveAuto/Tests/bin/Final-0.3.3/RelaveAuto.NativeProbe.dll'
lines=['_.NETLOAD',f'"{plugin.as_posix()}"','_.NETLOAD',f'"{probe.as_posix()}"','RAEXPORTPROPOSALS','RACOMPARISON','RAQUALITYGUARDS','_.QUIT','_Y','']
(target/'comparison.scr').write_text('\n'.join(lines),encoding='ascii')
print(target)
