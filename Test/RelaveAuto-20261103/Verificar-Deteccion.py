import json,math,pathlib,csv,sys
root=pathlib.Path(__file__).resolve().parent
before=root/'Deteccion-12';after=root/(sys.argv[1] if len(sys.argv)>1 else 'Deteccion-12-final032')
rows=[];pending=[];unchanged=0;count=0
for folder in sorted(p for p in after.iterdir() if p.is_dir() and (p/'resultado-vision.json').exists()):
    batch=json.loads((folder/'resultado-vision.json').read_text())
    previous=json.loads((before/folder.name/'resultado-vision.json').read_text())
    photos=batch['Photos'];low='Flume1' not in folder.name
    for p in photos:
        old=next(q for q in previous['Photos'] if q['File']==p['File'])
        if not low:
            for field in ['Surface','Toe','GridOriginX','GridAnchorX','Scale','Tx','Ty','RegistrationValid','AlignmentValid']:
                assert p[field]==old[field],(folder.name,p['File'],field)
            unchanged+=1
        elif p['Frame']['Number']>0:
            assert p['Surface'] and p['Surface'][0]['X']==0 and p['Surface'][-1]['Y']==0
            assert not p['Approved'] and p['NeedsReview']
            assert all(math.isfinite(v[k]) and v[k]>=0 for v in p['Surface'] for k in ['X','Y'])
            assert all(b['X']>a['X'] for a,b in zip(p['Surface'],p['Surface'][1:]))
            assert p['GridOriginX']==0
            count+=1
    eligible=[p for p in photos if p['RegistrationValid'] and p['AlignmentValid'] and p['Surface'] and abs(p['Toe']-p['GridAnchorX'])<=3 and p['Surface'][-1]['Y']==0]
    if low:
        for p in photos:
            if p not in eligible:pending.append(dict(Ensayo=folder.name,Foto=p['File'],Ajuste='CENTRAR' if not p['AlignmentValid'] else 'PUNTA',Diferencia_punta_px=round(p['Toe']-p['GridAnchorX'],3)))
    row=dict(Ensayo=folder.name,Fotos=len(photos),Perfiles=sum(bool(p['Surface']) for p in photos),Referencias_pendientes=sum(not p['AlignmentValid'] for p in photos),Propuestas_completas=len(eligible),Puntos_min=min(len(p['Surface']) for p in photos if p['Surface']),Puntos_max=max(len(p['Surface']) for p in photos if p['Surface']))
    if low:print(folder.name,[(p['File'],round(p['Toe']-p['GridAnchorX'],1),p['AlignmentValid']) for p in photos if abs(p['Toe']-p['GridAnchorX'])>3])
    rows.append(row)
with (after/'VERIFICACION_12_ENSAYOS.tsv').open('w',newline='',encoding='utf-8-sig') as f:
    writer=csv.DictWriter(f,fieldnames=rows[0].keys(),delimiter='\t');writer.writeheader();writer.writerows(rows)
with (after/'FOTOS_PENDIENTES_DE_AJUSTE.tsv').open('w',newline='',encoding='utf-8-sig') as f:
    writer=csv.DictWriter(f,fieldnames=pending[0].keys(),delimiter='\t');writer.writeheader();writer.writerows(pending)
assert unchanged==80 and count==152,(unchanged,count)
(after/'regresion-ok.txt').write_text(f'{unchanged} fotos de Flume1: geometria y contornos identicos a 0.3.1. {count} propuestas de Flume2/3: A=0, puntos finitos, ordenados y cierre en base; ninguna aprobada automaticamente. No equivale a exactitud fisica de los contornos.',encoding='utf-8')
print(json.dumps(rows,indent=2));print('REGRESSION_OK',unchanged,count)
