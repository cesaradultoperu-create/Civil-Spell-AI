from pathlib import Path
import json
import numpy as np
import matplotlib.tri as mt
from matplotlib.path import Path as PolygonPath
r=Path(__file__).parent
report=[]
for code,rimfile in [('008','FEATURE_872E.csv'),('009','FEATURE_17D58.csv'),('010','POZAS_1C44A.csv')]:
    def interpolator(kind):
        a=np.loadtxt(r/'Data'/f'{kind}_{code}.csv',delimiter=',').reshape(-1,3,3)
        xy,idx,inverse=np.unique(a[:,:,:2].reshape(-1,2)-[579000,8302000],axis=0,return_index=True,return_inverse=True)
        return mt.LinearTriInterpolator(mt.Triangulation(xy[:,0],xy[:,1],inverse.reshape(-1,3)),a[:,:,2].ravel()[idx])
    ground,design=interpolator('GROUND'),interpolator('DESIGN')
    rim=np.loadtxt(r/'Data'/rimfile,delimiter=',')[:,:2]-[579000,8302000]
    base=json.loads((r/'Data'/f'drawing_{code}.json').read_text(encoding='utf8'))['metrics']
    step=.125
    x,y=np.meshgrid(np.arange(rim[:,0].min()+step/2,rim[:,0].max(),step),np.arange(rim[:,1].min()+step/2,rim[:,1].max(),step))
    p=np.c_[x.ravel(),y.ravel()];p=p[PolygonPath(rim).contains_points(p)]
    gz,dz=ground(p[:,0],p[:,1]),design(p[:,0],p[:,1]);valid=~(np.ma.getmaskarray(gz)|np.ma.getmaskarray(dz));d=np.asarray(gz)[valid]-np.asarray(dz)[valid]
    cut=float(np.maximum(d,0).sum()*step**2);fill=float(np.maximum(-d,0).sum()*step**2)
    report.append(dict(code=code,grid_025_cut=base['cut'],grid_0125_cut=cut,difference_cut=cut-base['cut'],grid_025_fill=base['fill'],grid_0125_fill=fill,difference_fill=fill-base['fill'],coverage=float(valid.mean())))
(r/'validation_metrics.json').write_text(json.dumps(report,indent=2),encoding='utf8')
print(json.dumps(report,indent=2))
