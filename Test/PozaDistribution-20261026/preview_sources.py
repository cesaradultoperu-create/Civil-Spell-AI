from pathlib import Path
import numpy as np
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
from matplotlib.collections import PolyCollection
root=Path(__file__).parent
fig,axes=plt.subplots(1,2,figsize=(14,10))
for ax,name,prefix in zip(axes,['Pozas','Poza_de_agua'],['Xref_LD-pozas','Xref_LD-Poza de agua']):
    a=np.loadtxt(root/'Data'/f'TIN_{name}.csv',delimiter=',').reshape(-1,3,3)
    p=a[:,:,:2].copy(); p[:,:,0]-=579000;p[:,:,1]-=8302000
    collection=PolyCollection(p,array=a[:,:,2].mean(axis=1),cmap='terrain',edgecolors='none')
    ax.add_collection(collection);fig.colorbar(collection,ax=ax,label='Cota de diseño (m)',shrink=.5)
    for path in (root/'Data').glob(prefix+'_*.csv'):
        v=np.loadtxt(path,delimiter=',',ndmin=2)
        ax.plot(v[:,0]-579000,v[:,1]-8302000,'k-',lw=.7)
        if np.ptp(v[:,0])+np.ptp(v[:,1])>15:
            mid=v.mean(axis=0);ax.text(mid[0]-579000,mid[1]-8302000,path.stem.split('_')[-1],fontsize=6,color='red')
    ax.autoscale_view();ax.set_aspect('equal');ax.grid(alpha=.2);ax.set_title(name);ax.set_xlabel('Este - 579000');ax.set_ylabel('Norte - 8302000')
fig.tight_layout();fig.savefig(root/'source-overview.png',dpi=150)
