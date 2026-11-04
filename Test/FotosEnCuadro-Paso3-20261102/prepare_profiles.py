"""Adapt the supplied per-photo traced deposit contours to approved step 2.

The source contours are recovered through the original image transforms,
including the two screenshot zoom changes. They are not generic triangles or
new vision estimates. A and A1 snap to their approved reference intersections.
"""
import json
import math
import ntpath
from pathlib import Path

ROOT=Path(__file__).resolve().parent
example=json.loads((ROOT/'ejemplo.json').read_text(encoding='utf-8-sig'))
approved=json.loads((ROOT/'paso2.json').read_text(encoding='utf-8-sig'))
plan=json.loads((ROOT/'plan.json').read_text())
zoom=json.loads((ROOT/'zoom.json').read_text())
old_images={ntpath.basename(r['source']):r for r in example if r['type']=='RasterImage'}
new_images={ntpath.basename(r['source']):r for r in approved if r['type']=='RasterImage'}
reference_profiles=[r for r in example if r['type']=='Polyline' and r['parent']=='' and r.get('closed') and len(r['points'])>4]

def inverse_pixel(row, point):
    dx,dy=point[0]-row['origin'][0],point[1]-row['origin'][1]
    u,v=row['u'],row['v']
    det=u[0]*v[1]-u[1]*v[0]
    return [(dx*v[1]-dy*v[0])/det*row['imageSize'][0],
            (u[0]*dy-u[1]*dx)/det*row['imageSize'][1]]

def map_point(old,new,p,registration):
    q=inverse_pixel(old,p)
    width,height=new['imageSize']
    q=[registration['scale']*q[0]+registration['tx'],
       height-(registration['scale']*(height-q[1])+registration['ty'])]
    return [new['origin'][i]+new['u'][i]*q[0]/width+new['v'][i]*q[1]/height for i in range(2)]

profiles=[]
for item in plan:
    number=int(Path(item['file']).stem)
    if not number:
        continue
    left=item['destinationLeft']
    tip=item['destinationRight']
    original=min(reference_profiles,key=lambda r:abs(r['min'][0]-left[0])+abs(r['min'][1]-left[1]))
    assert abs(original['min'][0]-left[0])+abs(original['min'][1]-left[1])<1e-7
    registration=zoom.get(str(number),dict(scale=1,tx=0,ty=0))
    old,new=old_images[item['file']],new_images[item['file']]
    points=[left[:2],tip[:2]]
    for q in original['points'][2:]:
        x,y=map_point(old,new,q,registration)
        points.append([max(left[0],min(tip[0],x)),max(left[1],y)])
    # Close the profile against the left side of the channel.
    points[-1][0]=left[0]
    clean=[]
    for p in points:
        if not clean or math.hypot(p[0]-clean[-1][0],p[1]-clean[-1][1])>1e-9:
            clean.append(p)
    area=abs(sum(a[0]*b[1]-a[1]*b[0] for a,b in zip(clean,clean[1:]+clean[:1])))/2
    if area<=1e-7 or len(clean)<3:
        raise ValueError('Degenerate deposit profile: '+item['file'])
    profiles.append(dict(file=item['file'],label=item['label'],points=clean,
                         sourceProfile=original['handle'],area=area,
                         origin=left,tip=tip))
(ROOT/'perfiles.json').write_text(json.dumps(profiles,indent=2))
print('21 contornos individuales preparados. 0.00: estado inicial sin area de relave.')
for p in profiles:
    print(p['label'],'vertices',len(p['points']),'area',round(p['area'],7))
