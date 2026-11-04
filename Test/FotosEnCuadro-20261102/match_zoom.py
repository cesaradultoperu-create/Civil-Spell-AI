"""Measure scale/translation between video screenshots; do not estimate rotation.
Uses static video features, excluding interface chrome and moving sediment.
No source image is modified.
"""
import json
from pathlib import Path
import numpy as np
from PIL import Image

ROOT=Path(__file__).resolve().parent
SOURCE=Path(r'C:\Users\Cesar Ugaz\Pictures\poner fotos en cuadro\fotos y excel')
reference=Image.open(SOURCE/'0.png').convert('L')

def matches(template, target):
    t=np.asarray(template, dtype=float)
    a=np.asarray(target, dtype=float)
    h,w=t.shape
    H,W=a.shape
    if h>H or w>W:
        return -1,0,0
    fftshape=(2**int(np.ceil(np.log2(H+h))),2**int(np.ceil(np.log2(W+w))))
    centered=t-t.mean()
    corr=np.fft.irfft2(np.fft.rfft2(a,fftshape)*np.conj(np.fft.rfft2(centered,fftshape)),fftshape)[:H-h+1,:W-w+1]
    def windowsums(x):
        s=np.pad(x,((1,0),(1,0))).cumsum(0).cumsum(1)
        return s[h:,w:]-s[:-h,w:]-s[h:,:-w]+s[:-h,:-w]
    sums=windowsums(a)
    variances=np.maximum(windowsums(a*a)-sums*sums/(w*h),1e-8)
    scores=corr/np.sqrt(variances*(centered*centered).sum())
    y,x=np.unravel_index(scores.argmax(),scores.shape)
    return float(scores[y,x]),int(x),int(y)

# Corresponding approximate positions are clear from the original captures.
features=[
    ((1590,180,1835,335),(1460,70,1880,350)),
    ((930,220,1160,325),(590,120,1010,335)),
    ((1240,210,1450,325),(1000,120,1420,335)),
]
results={}
for number in [120,160]:
    target=Image.open(SOURCE/f'{number}.png').convert('L')
    found=[]
    for box,searchbox in features:
        best=None
        for scale in np.arange(1.330,1.353,.0005):
            template=reference.crop(box).resize((round((box[2]-box[0])*scale),round((box[3]-box[1])*scale)),Image.BICUBIC)
            score,x,y=matches(template,target.crop(searchbox))
            row=dict(score=score,scale=float(scale),ref=[(box[0]+box[2])/2,(box[1]+box[3])/2],target=[searchbox[0]+x+template.width/2,searchbox[1]+y+template.height/2])
            if best is None or score>best['score']:
                best=row
        found.append(best)
    A=[];b=[]
    for p in found:
        A.extend([[p['ref'][0],1,0],[p['ref'][1],0,1]])
        b.extend(p['target'])
    scale,tx,ty=np.linalg.lstsq(np.array(A),np.array(b),rcond=None)[0]
    residual=np.array(A)@np.array([scale,tx,ty])-b
    if min(f['score'] for f in found)<.90 or max(abs(residual))>2:
        raise ValueError('Unreliable screenshot registration: '+str(found))
    results[str(number)]=dict(scale=float(scale),tx=float(tx),ty=float(ty),maxResidualPixels=float(max(abs(residual))),features=found)
    print(number,json.dumps(results[str(number)]))
(ROOT/'zoom.json').write_text(json.dumps(results,indent=2))
audit=['foto\tzoom_visor\tcoincidencia_minima_referencias_fijas\tdesplazamiento_x_px\tdesplazamiento_y_px']
for photo in sorted(SOURCE.glob('*.png'),key=lambda p:int(p.stem)):
    number=int(photo.stem)
    if str(number) in results:
        p=results[str(number)]
        scores=[v['score'] for v in p['features']]
        tx,ty=p['tx'],p['ty']
        zoom=208
    else:
        target=Image.open(photo).convert('L')
        scores=[]; offsets=[]
        for box,_ in features:
            search=(box[0]-6,box[1]-6,box[2]+6,box[3]+6)
            score,x,y=matches(reference.crop(box),target.crop(search))
            scores.append(score);offsets.append((x-6,y-6))
        tx,ty=np.mean(offsets,axis=0)
        if max(abs(x) for xy in offsets for x in xy)>1:
            raise ValueError('Unexpected movement: '+photo.name+str(offsets))
        zoom=155
    if min(scores)<.90:
        raise ValueError('Static features do not match: '+photo.name)
    audit.append(f'{photo.name}\t{zoom}\t{min(scores):.6f}\t{tx:.6f}\t{ty:.6f}')
(ROOT/'capturas.tsv').write_text('\n'.join(audit))
print('22/22 capturas cotejadas contra los mismos tres elementos fijos del video.')
