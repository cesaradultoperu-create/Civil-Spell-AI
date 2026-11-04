import json
from pathlib import Path
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
root=Path(__file__).parent
data=json.loads((root/'model-before.json').read_text(encoding='utf-8-sig'))
fig,axes=plt.subplots(3,2,figsize=(18,18))
colors={1:'red',2:'#b5a000',3:'green',4:'#00aabb',5:'blue',6:'magenta',256:'gray'}
for option,ax in zip(range(1,6),axes.flat):
    for e in data:
        if e['type']!='AcDbPolyline' or f'op. {option})' not in e['layer']:continue
        c=e['coordinates'];x=c[::2];y=c[1::2]
        ax.plot(x,y,color=colors[e['color']],lw=1.5)
        ax.scatter([x[0],x[-1]],[y[0],y[-1]],s=12,color=colors[e['color']])
        ax.text(x[-1],y[-1],e['handle']+' '+e['linetype'],fontsize=7)
        print(option,e['handle'],e['color'],e['linetype'],len(x),'START',round(x[0],2),round(y[0],2),'END',round(x[-1],2),round(y[-1],2))
    ax.set_title('OPCION '+str(option));ax.set_aspect('equal');ax.grid(alpha=.2)
fig.tight_layout();fig.savefig(root/'recorridos-antes.png',dpi=130)
