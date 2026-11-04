import json
import sys
import numpy as np
import matplotlib.pyplot as plt
from mpl_toolkits.mplot3d.art3d import Poly3DCollection
from pathlib import Path
root=Path(sys.argv[1]) if len(sys.argv)>1 else Path(__file__).parent
blocks=json.loads((root/'geometry.json').read_text())
solids=next(b['entities'] for b in reversed(blocks) if b['block'].startswith('CIVILSPELLAI_REFERENCIAS_3D_') and any('vertices' in e for e in b['entities']))
fig=plt.figure(figsize=(13,8),facecolor='white');ax=fig.add_subplot(projection='3d')
allpts=[]
for e in solids:
 if 'vertices' not in e:continue
 vertices=np.array(e['vertices']);allpts.extend(vertices);faces=e['faces'];i=0;polys=[]
 while i<len(faces):
  n=faces[i];i+=1;polys.append(vertices[faces[i:i+n]]);i+=n
 mesh=Poly3DCollection(polys,facecolors='#d1ac62' if e['color']==2 else '#5593af',linewidth=0,shade=True)
 ax.add_collection3d(mesh)
ps=np.array(allpts);lo=ps.min(axis=0);hi=ps.max(axis=0)
ax.set_xlim(lo[0]-.1,hi[0]+.1);ax.set_ylim(lo[1]-.1,hi[1]+.1);ax.set_zlim(lo[2]-.1,hi[2]+.1)
ax.set_box_aspect(hi-lo);ax.view_init(elev=25,azim=-64)
ax.set_xlabel('X (m)');ax.set_ylabel('Y (m)');ax.set_zlabel('Z (m)')
ax.set_title('Tubería 300 mm · TOP + FRONT\nMedidas gobernadas por FRONT',fontsize=15,pad=15)
fig.text(.5,.045,'Corrugas en relieve tomadas del perfil 2D · Tapas con bandas y remate · Espesor editable',ha='center',fontsize=10,color='#555555')
fig.savefig(root/'Vista-3D.png',dpi=140,bbox_inches='tight')
