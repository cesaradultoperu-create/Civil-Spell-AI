import json
from pathlib import Path
root=Path(__file__).parent
def read(name):return json.loads((root/name).read_text(encoding='utf-8-sig'))
before={e['handle']:e for e in read('model-before.json')}
after={e['handle']:e for e in read('model-after.json')}
failures=[]
for handle,original in before.items():
    current=after.get(handle)
    if current is None:
        failures.append('Missing original '+handle);continue
    for key,value in original.items():
        if key=='layer' and handle=='7297':
            if current[key]!='FIG_ORIGINAL_AS03_OP1':failures.append('Incorrect auxiliary layer')
            continue
        if current.get(key)!=value:failures.append('Original changed: '+handle+' '+key)
manifest=read('preparation.json')
assert len(manifest)==24
for line in manifest:
    actual=after[line['created']];template=before[line['template']]
    if actual['layer']!='FIG_OPCION_'+str(line['option']):failures.append('Wrong presentation layer')
    if actual['width']!=15:failures.append('Wrong width')
    for key in ['color','linetype','ltscale']:
        if actual[key]!=template[key]:failures.append('Wrong style '+line['created']+' '+key)
oldlayouts={l['name']:l for l in read('layouts-before.json')}
newlayouts={l['name']:l for l in read('layouts-after.json')}
for name,l in oldlayouts.items():
    old=[i for i in l['items'] if i['type']!='AcDbViewport']
    new=[i for i in newlayouts[name]['items'] if i['type']!='AcDbViewport']
    if old!=new:failures.append('Paper content changed: '+name)
v=read('validation.json')
assert len(v['entities'])==24
assert len(v['viewports'])==5
for vp in v['viewports']:
    n=int(vp['layout'].split()[-1]);frozen=set(vp['frozen'])
    if not vp['locked']:failures.append('Unlocked viewport '+vp['layout'])
    for alternative in range(2,6):
        is_frozen='FIG_OPCION_'+str(alternative) in frozen
        if is_frozen != (alternative!=n):failures.append('Wrong layer isolation '+vp['layout'])
    if n!=1 and '0_Planta de pro (op. 1)' not in frozen:failures.append('Template leaks '+vp['layout'])
    if n!=1 and 'FIG_ORIGINAL_AS03_OP1' not in frozen:failures.append('Auxiliary blue leaks '+vp['layout'])
    if abs(vp['scale']-0.049999978337232812)>1e-12:failures.append('Changed scale')
for n in range(2,6):
    pdf=root/('OPCION_'+str(n)+'.pdf')
    if pdf.stat().st_size<100000:failures.append('Empty PDF '+str(n))
    if not pdf.read_bytes().startswith(b'%PDF-'):failures.append('Invalid PDF '+str(n))
result={'passed':not failures,'original_entities_preserved':len(before),'styled_display_polylines':len(manifest),'layouts_checked':len(v['viewports']),'pdfs_checked':4,'failures':failures}
(root/'verification.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print(json.dumps(result,indent=2))
if failures:raise SystemExit(1)
