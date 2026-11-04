import json, math
import numpy as np
import matplotlib.pyplot as plt
from pathlib import Path
root=Path(__file__).parent
blocks={b['block']:b['entities'] for b in json.loads((root/'geometry.json').read_text())}
fig,ax=plt.subplots(figsize=(15,10))
origin=np.array([579168,8297891,0])
def draw(name,M,depth=0):
 if depth>15:return
 for e in blocks[name]:
  if e['type']=='INSERT':
   a=e['rotation'];s=e['scale'];p=e['pos'];c=math.cos(a);t=math.sin(a)
   N=np.array([[c*s[0],-t*s[1],p[0]],[t*s[0],c*s[1],p[1]],[0,0,1]])
   draw(e['block'],M@N,depth+1);continue
  pts=e.get('points')
  if e['type'] in ('CIRCLE','ARC'):
   a=e.get('start',0);b=e.get('end',2*math.pi)
   if b<a:b+=2*math.pi
   ts=np.linspace(a,b,80);pts=[[e['center'][0]+e['radius']*math.cos(t),e['center'][1]+e['radius']*math.sin(t)] for t in ts]
  if not pts:continue
  if e.get('closed'):pts=pts+[pts[0]]
  ps=np.array([M@np.array([p[0],p[1],1]) for p in pts])
  ax.plot(ps[:,0],ps[:,1],lw=.45,color='#333333' if 'CORRUG' in e['layer'] else '#008aaa')
M=np.array([[1,0,-origin[0]],[0,1,-origin[1]],[0,0,1]])
draw('*Model_Space',M)
for e in blocks['*Model_Space']:
 p=M@np.array(e['pos'][:2]+[1]);ax.text(p[0],p[1]+.4,e['block']+' / '+e['handle'],fontsize=8,color='red')
ax.set_aspect('equal');ax.grid(alpha=.2);fig.tight_layout();fig.savefig(root/'referencias.png',dpi=140)
