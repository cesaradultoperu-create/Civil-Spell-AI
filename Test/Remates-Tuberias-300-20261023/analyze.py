import json, math, collections
from pathlib import Path
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt

ROOT = Path(__file__).resolve().parent
def value(e, code, default=None):
    return next((r[1] for r in e if r[0] == code), default)
def points(e):
    return [r[1:3] for r in e if r[0] == 10]
def distance(a,b):
    return math.dist(a,b)
def segment_distance(p,a,b):
    d=[b[i]-a[i] for i in range(2)]
    t=max(0,min(1,sum((p[i]-a[i])*d[i] for i in range(2))/sum(x*x for x in d)))
    return distance(p,[a[i]+t*d[i] for i in range(2)])
def nearest(p, others, exclude=None):
    return min((segment_distance(p,a,b),value(e,5)) for e in others if value(e,5)!=exclude for a,b in zip(points(e),points(e)[1:]))
es=[json.loads(x) for x in (ROOT/'entities.jsonl').read_text().splitlines()]
target=[e for e in es if value(e,8)=='WWW_Tub. 300 Perf.' and value(e,90)==2]
network=[e for e in es if len(points(e))>=2 and sum(distance(a,b) for a,b in zip(points(e),points(e)[1:]))>2]
sample=next(e for e in es if value(e,5)=='1A491')
vs=points(sample)
anchor=[(vs[1][i]+vs[2][i])/2 for i in range(2)]
closest=sorted((distance(p,anchor),value(e,5),j,p) for e in target for j,p in enumerate(points(e)))
print('SAMPLE',vs, 'ANCHOR',anchor,'CLOSEST',closest[:2])
print('SAMPLE lengths',[distance(a,b) for a,b in zip(vs,vs[1:])])
records=[]
for e in target:
    ps=points(e)
    ds=[nearest(p,network,value(e,5)) for p in ps]
    records.append({'handle':value(e,5),'points':ps,'nearest':ds,'z':value(e,38),'width':value(e,43)})
for tol in [.001,.01,.05,.15,.3,.5]:
    print('TOL',tol,collections.Counter(sum(d[0]<=tol for d in r['nearest']) for r in records))
for r in records:
    if sum(d[0]<=.05 for d in r['nearest'])!=1:
        print('REVIEW',r)
(ROOT/'analysis.json').write_text(json.dumps({'sample':vs,'anchor':anchor,'nearest_sample':closest[0],'records':records},indent=2))
fig,ax=plt.subplots(figsize=(10,13))
for e in network:
    ps=points(e)
    if ps[0][0]>580000: continue
    ax.plot([p[0]-579200 for p in ps],[p[1]-8302200 for p in ps],color='#60c5df' if value(e,8)=='WWW_Tub. 300 Perf.' else '#1446fd',lw=1)
for r in records:
    for j,(p,d) in enumerate(zip(r['points'],r['nearest'])):
        if d[0]>.05:
            ax.scatter(p[0]-579200,p[1]-8302200,c='orange',s=10)
            ax.annotate(r['handle']+':'+str(j),(p[0]-579200,p[1]-8302200),fontsize=5)
ax.set_aspect('equal'); ax.grid(alpha=.2)
fig.savefig(ROOT/'revision-extremos.png',dpi=180,bbox_inches='tight')
