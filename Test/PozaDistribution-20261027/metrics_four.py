from pathlib import Path
import json
import numpy as np
import matplotlib.tri as mtri
from matplotlib.path import Path as Polygon

root=Path(__file__).parent
data=root.parent/'PozaDistribution-20261026'/'Data'
origin=np.array([579000.,8302000.])
def surface(file):
    a=np.loadtxt(data/file,delimiter=',').reshape(-1,3,3)
    a[:,:,:2]-=origin
    xy,ix,ind=np.unique(a[:,:,:2].reshape(-1,2),axis=0,return_index=True,return_inverse=True)
    z=a[:,:,2].reshape(-1)[ix]
    return mtri.LinearTriInterpolator(mtri.Triangulation(xy[:,0],xy[:,1],ind.reshape(-1,3)),z)
result={}
for code,rimfile,source in [('007','FEATURE_182EA.csv','008'),('008','FEATURE_872E.csv','008'),('009','FEATURE_17D58.csv','009'),('010','POZAS_1C44A.csv','010')]:
    rim=np.loadtxt(data/rimfile,delimiter=',')
    rim[:,:2]-=origin
    if np.linalg.norm(rim[0,:2]-rim[-1,:2])<1e-6:rim=rim[:-1]
    ground=surface('GROUND_'+source+'.csv');design=surface('DESIGN_'+source+'.csv')
    step=.25
    xx=np.arange(rim[:,0].min()+step/2,rim[:,0].max(),step)
    yy=np.arange(rim[:,1].min()+step/2,rim[:,1].max(),step)
    gx,gy=np.meshgrid(xx,yy);pts=np.c_[gx.ravel(),gy.ravel()]
    pts=pts[Polygon(rim[:,:2]).contains_points(pts)]
    gz=ground(pts[:,0],pts[:,1]);dz=design(pts[:,0],pts[:,1])
    good=~(np.ma.getmaskarray(gz)|np.ma.getmaskarray(dz))
    if not good.all():raise RuntimeError('Incomplete TIN coverage '+code)
    diff=np.asarray(gz)-np.asarray(dz)
    dx,dy=design.gradient(pts[:,0],pts[:,1])
    result[code]={'cut':float(np.maximum(diff,0).sum()*step**2),'fill':float(np.maximum(-diff,0).sum()*step**2),'lining':float(np.sqrt(1+np.asarray(dx)**2+np.asarray(dy)**2).sum()*step**2),'perimeter':float(np.linalg.norm(np.roll(rim[:,:2],-1,axis=0)-rim[:,:2],axis=1).sum()),'area':len(pts)*step**2,'coverage':float(good.mean()),'rim':(rim[:,:2]+origin).tolist()}
    print(code,{k:round(v,2) for k,v in result[code].items() if k!='rim'},flush=True)
(root/'metrics.json').write_text(json.dumps(result,indent=2),encoding='utf8')
