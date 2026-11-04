import concurrent.futures, json, pathlib, subprocess, sys

repo = pathlib.Path(__file__).resolve().parents[2]
root = pathlib.Path(r'C:\Users\Cesar Ugaz\Pictures\poner fotos en cuadro\se aplica los 4 pasos para todos')
out = repo / 'Test/RelaveAuto-20261103' / (sys.argv[1] if len(sys.argv)>1 else 'Deteccion-12')
worker = repo / 'tools/RelaveAuto/bin/Release/net48/RelaveVision.exe'
template = json.loads((repo / 'Test/RelaveAuto-20261103/Inicio-A-0.3.1/job.json').read_text(encoding='utf-8-sig'))

def run(folder):
    target = out / folder.name
    target.mkdir(parents=True, exist_ok=True)
    job = dict(template)
    job.update(ModelPath=str(folder/'modelo_.dwg'), PhotoFolder=str(folder), OutputFolder=str(target),
               Calibration=json.loads((folder/'relave-calibracion.json').read_text(encoding='utf-8-sig')))
    job['ReferenceFile'] = job['Calibration']['ReferenceFile']
    source = target / 'job.json'
    source.write_text(json.dumps(job), encoding='utf-8')
    proc = subprocess.run([str(worker), str(source)], capture_output=True, text=True)
    (target/'motor.log').write_text(proc.stdout+proc.stderr, encoding='utf-8')
    if proc.returncode:
        return dict(Trial=folder.name, Error=proc.stderr)
    result = json.loads((target/'resultado-vision.json').read_text(encoding='utf-8-sig'))
    color_file = target/'modelo-color.json'
    color = json.loads(color_file.read_text()) if color_file.exists() else None
    photos=result['Photos']
    row=dict(Trial=folder.name, Photos=len(photos), Profiles=sum(bool(p['Surface']) for p in photos),
             Registration=sum(p['RegistrationValid'] for p in photos), Alignment=sum(p['AlignmentValid'] for p in photos), Color=color,
             Messages=sorted(set(p['Message'] for p in photos if not p['Surface'])))
    print(json.dumps(row),flush=True)
    return row

with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:
    rows=list(pool.map(run, sorted(p for p in root.iterdir() if p.is_dir())))
(out/'audit.json').write_text(json.dumps(rows,indent=2),encoding='utf-8')
