"""Align each photograph's deposit toe with its frame marker.

Approved step-1 rotation and the shared channel origin remain fixed. The toe
is recovered from the actual white profile in the user's reference DWG, then
projected onto the approved baseline before scaling A--A1. Different screenshot
zoom is compensated, and no new angle is estimated for an individual image.
"""
import json
import math
import ntpath
import shutil
from pathlib import Path
from PIL import Image

ROOT=Path(__file__).resolve().parent
SOURCE=Path(r'C:\Users\Cesar Ugaz\Pictures\poner fotos en cuadro\fotos y excel')
model=json.loads((ROOT/'modelo.json').read_text(encoding='utf-8-sig'))
example=json.loads((ROOT/'ejemplo.json').read_text(encoding='utf-8-sig'))
previous=json.loads((ROOT/'plan-paso1.json').read_text())
zoom=json.loads((ROOT/'zoom.json').read_text())
images={ntpath.basename(row['source']):row for row in example if row['type']=='RasterImage'}
profiles=[row for row in example if row['type']=='Polyline' and row['parent']=='' and len(row['points'])>4]
base=next(p for p in previous if p['file']=='0.png')
origin=base['sourceLeft']
dx,dy=[b-a for a,b in zip(origin,base['sourceRight'])]
length=math.hypot(dx,dy)
direction=[dx/length,dy/length]
step1_units_per_pixel=2.2/length

def inverse_pixel(row,point):
    dx,dy=point[0]-row['origin'][0],point[1]-row['origin'][1]
    u,v=row['u'],row['v']
    det=u[0]*v[1]-u[1]*v[0]
    return [(dx*v[1]-dy*v[0])/det*row['imageSize'][0],
            (u[0]*dy-u[1]*dx)/det*row['imageSize'][1]]

plan=[]
audit=[]
for original in previous:
    p=dict(original)
    number=int(Path(p['file']).stem)
    if not (SOURCE/p['file']).is_file():
        continue
    left=p['destinationLeft']
    if number:
        profile=min(profiles,key=lambda v:abs(v['min'][0]-left[0])+abs(v['min'][1]-left[1]))
        if abs(profile['min'][0]-left[0])+abs(profile['min'][1]-left[1])>1e-6:
            raise ValueError('No reference profile for '+p['file'])
        toe=inverse_pixel(images[p['file']],profile['points'][1])
        distance=sum((toe[i]-origin[i])*direction[i] for i in range(2))
        if distance<=0:
            raise ValueError('Invalid A--A1 length')
        foot=[origin[i]+distance*direction[i] for i in range(2)]
        offset=abs(sum((toe[i]-origin[i])*[-direction[1],direction[0]][i] for i in range(2)))
        marker_candidates=[v for v in model if v['type']=='Polyline' and len(v['points'])==2
                           and abs(v['min'][0]-v['max'][0])<1e-7
                           and abs(v['min'][1]-left[1])<1e-7
                           and abs(v['max'][1]-v['min'][1]-1)<1e-7
                           and abs(v['min'][0]-(left[0]+number/100))<1e-7]
        if marker_candidates:
            marker=marker_candidates[0]['min'][0]
        elif number==220:
            # At 2.20 the reference marker coincides with the rectangle's edge.
            edges=[v for v in model if v['type']=='Polyline' and v.get('closed')
                   and abs(v['min'][0]-left[0])<1e-7 and abs(v['min'][1]-left[1])<1e-7
                   and abs(v['max'][0]-left[0]-2.2)<1e-7]
            if not edges:
                raise ValueError('No frame edge at 2.20')
            marker=edges[0]['max'][0]
        else:
            raise ValueError('No vertical frame marker for '+p['file'])
        registration=zoom.get(str(number),dict(scale=1,tx=0,ty=0))
        def current_point(point):
            return [registration['scale']*point[0]+registration['tx'],
                    p['height']-(registration['scale']*(p['height']-point[1])+registration['ty'])]
        p['sourceLeft']=current_point(origin)
        p['sourceRight']=current_point(foot)
        p['destinationRight']=[marker,left[1],0]
        p['step2ScaleFactor']=(marker-left[0])/distance/step1_units_per_pixel
        p['calibration']='A_A1_profile_'+profile['handle']
        p['referenceToePixel']=toe
        p['baselineProjectionPixels']=offset
        audit.append(dict(file=p['file'],profileHandle=profile['handle'],toePixelBottomUp=toe,
                          projectedToePixelBottomUp=foot,projectionPixels=offset,
                          markerX=marker,scaleFactor=p['step2ScaleFactor']))
    else:
        p['step2ScaleFactor']=1
        p['calibration']='paso1_0_aprobado'
    for point in [p['sourceLeft'],p['sourceRight']]:
        if not (0<=point[0]<=p['width'] and 0<=point[1]<=p['height']):
            raise ValueError('Anchor outside photo: '+p['file'])
    plan.append(p)
    shutil.copy2(SOURCE/p['file'],ROOT/'Entrega'/'fotos'/p['file'])
(ROOT/'plan.json').write_text(json.dumps(plan,indent=2))
(ROOT/'anclajes.json').write_text(json.dumps(audit,indent=2))
print('22 fotos: 0.00 conservada y 21 ajustadas a A--A1 con el mismo giro.')
for p in plan:
    print(p['file'],'factor de escala respecto al paso 1:',round(p['step2ScaleFactor'],6))
