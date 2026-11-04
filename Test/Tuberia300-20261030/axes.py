exec(open(__file__.replace('axes.py','plot.py')).read().split('fig,ax=')[0])
def lines(name,M,out,depth=0):
 for e in blocks[name]:
  if e['type']=='INSERT':
   a=e['rotation'];s=e['scale'];p=e['pos'];c=math.cos(a);t=math.sin(a)
   lines(e['block'],M@np.array([[c*s[0],-t*s[1],p[0]],[t*s[0],c*s[1],p[1]],[0,0,1]]),out,depth+1)
  if e['type']=='LINE' and 'CORRUGADO' in e['layer']:
   a,b=[(M@np.array(p[:2]+[1]))[:2] for p in e['points']]
   l=np.linalg.norm(b-a)
   if .23<l<.31:
    n=(b-a)/l;d=np.array([n[1],-n[0]]);
    if d[0]<-.01 or abs(d[0])<.01 and d[1]<0:d=-d
    out.append((d,(a+b)/2,l))
for name in ['T','DF_CODO_MEJORADO_V2']:
 out=[];lines(name,np.eye(3),out);groups=[]
 for d,p,l in out:
  match=next((g for g in groups if abs(np.cross(g[0],d))<.01 and abs(np.cross(g[0],p-g[1][0][0]))<.02),None)
  if match is None:groups.append([d,[(p,l)]])
  else:match[1].append((p,l))
 print(name)
 for d,ps in groups:
  if len(ps)<3:continue
  print(len(ps),'d',d,'first',min(ps,key=lambda p:np.dot(d,p[0])),'last',max(ps,key=lambda p:np.dot(d,p[0])))
