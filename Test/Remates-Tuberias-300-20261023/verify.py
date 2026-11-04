import json, math
from pathlib import Path
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
ROOT=Path(__file__).resolve().parent
def value(e,c): return next((r[1] for r in e if r[0]==c),None)
def pts(e): return [r[1:3] for r in e if r[0]==10]
before=[json.loads(x) for x in (ROOT/'entities.jsonl').read_text().splitlines()]
after=[json.loads(x) for x in (ROOT/'after.jsonl').read_text().splitlines()]
original={value(e,5):e for e in before}
current={value(e,5):e for e in after}
assert len(after)==len(before)+85,(len(before),len(after))
for h,e in original.items(): assert current[h]==e,('Original changed',h)
specs=json.loads((ROOT/'placement-manifest.json').read_text())
caps=[e for e in after if value(e,8)=='WWW_Tub. 300 Perf.' and value(e,90)==4]
assert len(caps)==86
for spec in specs:
    matches=[e for e in caps if abs(value(e,38)-spec['elevation'])<1e-6 and
        all(math.dist(a,b)<1e-6 for a,b in zip(pts(e),spec['vertices']))]
    assert len(matches)==1,(spec['handle'],len(matches))
    vs=pts(matches[0]); p=spec['pipe'][spec['end']]; q=spec['pipe'][1-spec['end']]
    assert math.dist(p,[(vs[1][i]+vs[2][i])/2 for i in range(2)])<1e-6
    opening=[(vs[0][i]+vs[3][i]-vs[1][i]-vs[2][i])/2 for i in range(2)]
    assert sum(opening[i]*(q[i]-p[i]) for i in range(2))>0
    assert abs(value(matches[0],43)-.15)<1e-9
log=(ROOT/'verify.log').read_text(encoding='utf-8-sig').replace('\x00','')
assert 'Total errors found 0' in log,log[-2500:]
result={'original_entities_preserved':len(before),'new_caps':85,'total_300_caps':86,
    'placement_and_direction':'PASS','elevations_and_widths':'PASS','dwg_audit':'0 errors',
    'source_user_file_sha256':'EBAF16C1C31B4AF6FC5FEA7799E06825E7788FA6D3924793B7CE1015D4B866DA'}
(ROOT/'verification.json').write_text(json.dumps(result,indent=2))
print(json.dumps(result,indent=2))
fig,axes=plt.subplots(2,3,figsize=(12,8),facecolor='#202830')
choices=[specs[i] for i in [0,10,24,40,60,84]]
for ax,s in zip(axes.flat,choices):
    p=s['pipe'][s['end']]; q=s['pipe'][1-s['end']]
    d=math.dist(p,q); end=[p[i]+(q[i]-p[i])*1.8/d for i in range(2)]
    ax.set_facecolor('#202830')
    ax.plot([0,end[0]-p[0]],[0,end[1]-p[1]],lw=5,c='#7edcf4',solid_capstyle='butt')
    vs=s['vertices']; ax.plot([v[0]-p[0] for v in vs],[v[1]-p[1] for v in vs],lw=5,c='#7edcf4',solid_capstyle='butt',solid_joinstyle='miter')
    ax.set_xlim(-2,2); ax.set_ylim(-2,2); ax.set_aspect('equal'); ax.axis('off')
    ax.set_title('Ramal '+s['handle'],color='white',fontsize=10)
fig.suptitle('Remates colocados: misma forma y giro segun cada ramal',color='white',fontsize=14)
fig.savefig(ROOT/'Vista-previa-remates.png',dpi=160,bbox_inches='tight',facecolor=fig.get_facecolor())
