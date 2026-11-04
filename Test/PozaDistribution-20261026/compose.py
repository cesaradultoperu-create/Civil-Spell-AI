from pathlib import Path
import json, math
import numpy as np
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
import matplotlib.tri as mtri
from matplotlib.path import Path as MPath

ROOT=Path(__file__).parent
ORIGIN=np.array([579000.,8302000.])
configs=[('008','POZA DE MAYORES EVENTOS','FEATURE_872E.csv','FEATURE_872C.csv',250),('009','POZA DE SUBDRENAJE','FEATURE_17D58.csv','FEATURE_17D5A.csv',100),('010','POZA DE AGUA PARA PROCESOS','POZAS_1C44A.csv','POZAS_1DA89.csv',150)]
def read(name): return np.loadtxt(ROOT/'Data'/name,delimiter=',',ndmin=2)
def area_centroid(p):
    p=p[:,:2];q=np.roll(p,-1,axis=0);cross=p[:,0]*q[:,1]-q[:,0]*p[:,1];s=cross.sum()
    return abs(s)/2,((p+q)*cross[:,None]).sum(axis=0)/(3*s)
def model(name):
    a=read(name).reshape(-1,3,3);p=a.copy();p[:,:,:2]-=ORIGIN
    xy,ix,ind=np.unique(p[:,:,:2].reshape(-1,2),axis=0,return_index=True,return_inverse=True)
    z=p[:,:,2].reshape(-1)[ix];tri=mtri.Triangulation(xy[:,0],xy[:,1],ind.reshape(-1,3))
    return p,tri,mtri.LinearTriInterpolator(tri,z),z
def profile(model_data,center,direction,lo,hi):
    a=model_data[0][:,:,:2];aa=a.reshape(-1,2);bb=np.roll(a,-1,axis=1).reshape(-1,2);edge=bb-aa
    cross=lambda v,w:v[...,0]*w[...,1]-v[...,1]*w[...,0]
    den=cross(direction,edge);good=np.abs(den)>1e-10;t=np.zeros(len(den));u=t.copy();t[good]=cross(aa[good]-center,edge[good])/den[good];u[good]=cross(aa[good]-center,direction)/den[good]
    ts=np.unique(np.round(np.r_[lo,hi,t[good&(u>=-1e-8)&(u<=1+1e-8)&(t>=lo)&(t<=hi)]],7))
    xy=center+ts[:,None]*direction;z=model_data[2](xy[:,0],xy[:,1]);return ts,np.asarray(z),~np.ma.getmaskarray(z)
def segments(ts,z,ok):
    ids=np.where(np.diff(np.r_[False,ok,False]))[0]
    return [np.c_[ts[a:b],z[a:b]] for a,b in zip(ids[::2],ids[1::2]) if b-a>=2]
def polygon_crossings(poly,c,d):
    a=poly[:,:2];b=np.roll(a,-1,axis=0);e=b-a;cross=lambda v,w:v[...,0]*w[...,1]-v[...,1]*w[...,0]
    den=cross(d,e);ok=np.abs(den)>1e-10;t=np.zeros(len(e));u=t.copy();t[ok]=cross(a[ok]-c,e[ok])/den[ok];u[ok]=cross(a[ok]-c,d)/den[ok]
    return np.sort(t[ok&(u>=0)&(u<=1)])

summaries=[]
for code,title,rimfile,floorfile,plan_denom in configs:
    print('COMPOSE',code,flush=True)
    rim=read(rimfile);floor=read(floorfile);rim[:,:2]-=ORIGIN;floor[:,:2]-=ORIGIN
    if np.linalg.norm(rim[0,:2]-rim[-1,:2])<1e-6:rim=rim[:-1]
    if np.linalg.norm(floor[0,:2]-floor[-1,:2])<1e-6:floor=floor[:-1]
    area,center=area_centroid(rim);_,fc=area_centroid(floor)
    sampled=[]
    for a,b in zip(rim,np.roll(rim,-1,axis=0)):
        n=max(1,int(np.linalg.norm(b[:2]-a[:2])/.25));sampled.extend(np.linspace(a[:2],b[:2],n,endpoint=False))
    cov=np.cov(np.array(sampled).T);_,axes=np.linalg.eigh(cov);d=axes[:,-1]
    if code=='009':
        edges=np.roll(rim[:,:2],-1,axis=0)-rim[:,:2];d=edges[np.linalg.norm(edges,axis=1).argmax()];d=d/np.linalg.norm(d)
    if d[1]<0:d=-d
    directions=[d,np.array([-d[1],d[0]])]
    ground=model('GROUND_'+code+'.csv');design=model('DESIGN_'+code+'.csv')
    primitives=[];views=[]
    def poly(points,layer='DESIGN',space='model',closed=False):
        pts=np.asarray(points,float)
        if pts.shape[1]==2:pts=np.c_[pts,np.zeros(len(pts))]
        if len(pts)>1:primitives.append(dict(kind='poly',pts=pts.tolist(),layer=layer,space=space,closed=closed))
    def text(x,y,value,h=2.5,width=120,layer='TEXT',space='paper',align='top',rot=0):
        primitives.append(dict(kind='text',x=float(x),y=float(y),text=value,h=h,width=width,layer=layer,space=space,align=align,rot=rot))
    def circle(x,y,r,layer='CUT',space='paper'):
        primitives.append(dict(kind='circle',x=float(x),y=float(y),r=r,layer=layer,space=space))
    def rect(x,y,w,h,layer='FRAME'):
        poly([[x,y],[x+w,y],[x+w,y+h],[x,y+h]],layer,'paper',True)
    # Exact contour intersections of the local TINs.
    for data,step,layer in [(ground,1,'TN'),(design,1,'CONTOUR')]:
        tri,z=data[1],data[3];levels=np.arange(math.ceil(z.min()/step)*step,math.floor(z.max()/step)*step+.001,step)
        fig,ax=plt.subplots();contours=ax.tricontour(tri,z,levels=levels)
        for level,parts in zip(contours.levels,contours.allsegs):
            for v in parts:
                if len(v)>1:poly(v+ORIGIN,layer+'MAJOR' if round(level)%5==0 else layer)
        plt.close(fig)
    # Authoritative crest and floor feature lines.
    poly(rim[:,:2]+ORIGIN,'RIM',closed=True);poly(floor[:,:2]+ORIGIN,'FLOOR',closed=True)
    primitives.append(dict(kind='hatch',pts=np.c_[floor[:,:2]+ORIGIN,np.zeros(len(floor))].tolist(),layer='FILL',space='model'))
    plan_scale=1000/plan_denom;pc=fc+ORIGIN
    views.append(dict(role='plan',cx=float(pc[0]),cy=float(pc[1]),scale=plan_scale))
    hx=543.994417350719/(2*plan_scale);hy=244.75/(2*plan_scale)
    # Coordinate grid and labels, in the same geographic coordinates as SOTRAMI.
    interval=20 if code=='008' else 10
    for x in np.arange(math.ceil((pc[0]-hx)/interval)*interval,pc[0]+hx,interval):
        poly([[x,pc[1]-hy],[x,pc[1]+hy]],'GRID');text(x,pc[1]-hy+.5,f'{x:,.0f} mE'.replace(',',' '),2/plan_scale,20/plan_scale,'GRIDTEXT','model')
    for y in np.arange(math.ceil((pc[1]-hy)/interval)*interval,pc[1]+hy,interval):
        poly([[pc[0]-hx,y],[pc[0]+hx,y]],'GRID');text(pc[0]-hx+.5,y+.6,f'{y:,.0f} mN'.replace(',',' '),2/plan_scale,50/plan_scale,'GRIDTEXT','model')
    # Cutting traces and true overall dimensions of the supplied crest.
    spans=[]
    for j,direction in enumerate(directions):
        proj=(rim[:,:2]-fc)@direction;lo,hi=float(proj.min()),float(proj.max());spans.append((lo,hi))
        ends=fc+np.array([lo-2,hi+2])[:,None]*direction+ORIGIN;poly(ends,'CUT')
        for q in ends:circle(q[0],q[1],3/plan_scale,'CUT','model');text(q[0],q[1],str(j+1),2.5/plan_scale,6/plan_scale,'CUT','model','middle')
        p1=rim[np.argmin(proj),:2]+ORIGIN;p2=rim[np.argmax(proj),:2]+ORIGIN;n=np.array([-direction[1],direction[0]])
        dimpoint=fc+ORIGIN+n*(np.max(np.abs((rim[:,:2]-fc)@n))+3.2)
        primitives.append(dict(kind='dim',pts=np.c_[np.array([p1,p2,dimpoint]),np.zeros(3)].tolist(),rot=float(math.atan2(direction[1],direction[0])),h=2.1/plan_scale,layer='DIM',space='model'))
    text(pc[0],pc[1],title,2.5/plan_scale,55/plan_scale,'PLANLABEL','model','middle')
    # Elevation callouts are values from the source geometry.
    for p,label in [(rim[np.argmin(rim[:,2])],f'CORONACIÓN\nEL. {rim[:,2].min():.2f}'),(floor[np.argmin(floor[:,2])],f'FONDO\nEL. {floor[:,2].min():.2f}')]:
        q=p[:2]+ORIGIN;elbow=q+np.array([5,3])/plan_scale*3;poly([q,elbow,elbow+np.array([10,0])/plan_scale],'CALLOUT');text(elbow[0],elbow[1]+.8/plan_scale,label,2.1/plan_scale,36/plan_scale,'CALLOUT','model')
    text(376,326,title,3,300,align='middle');text(376,318,'VISTA EN PLANTA',2.8,180,align='middle');text(376,312,f'ESCALA 1:{plan_denom}',2,100,align='middle')
    # North arrow in paper space; the plan is shown with WCS north up.
    poly([[74,548],[74,566],[70,554],[74,557],[78,554],[74,566]],'FRAME','paper');text(74,572,'N',2.8,15,align='middle')
    profiles=[]
    for j,direction in enumerate(directions):
        lo,hi=spans[j];margin=5 if j==0 else 3;lo=math.floor(lo-margin);hi=math.ceil(hi+margin)
        tg,zg,okg=profile(ground,fc,direction,lo,hi);td,zd,okd=profile(design,fc,direction,lo,hi)
        values=np.r_[zg[okg],zd[okd]];zmin=math.floor(values.min())-1;zmax=math.ceil(values.max())+1
        vex=1 if code=='009' else 2
        ox=10000+j*200;oy=10000;L=hi-lo;H=(zmax-zmin)*vex
        paperw=320.738165580128 if j==0 else 240.919540491021
        maxscale=min((paperw-24)/(L+6),(133.60020130749-14)/(H+2));denom=int(math.ceil(1000/maxscale/25)*25);scale=1000/denom;ph=2.4/scale
        for t in np.arange(0,L+.01,5):poly([[ox+t,oy],[ox+t,oy+H]],'PROFILEGRID');text(ox+t,oy-.7,f'{t:.0f}',ph,3,'PROFILETEXT','model','middle')
        for z in np.arange(zmin,zmax+.01,2):poly([[ox,oy+(z-zmin)*vex],[ox+L,oy+(z-zmin)*vex]],'PROFILEGRID');text(ox-1.3,oy+(z-zmin)*vex,f'{z:.0f}',ph,5,'PROFILETEXT','model','middle')
        poly([[ox,oy],[ox+L,oy],[ox+L,oy+H],[ox,oy+H]],'PROFILEAXIS',closed=True)
        for data,layer in [(segments(tg,zg,okg),'PROFILETN'),(segments(td,zd,okd),'PROFILEDESIGN')]:
            for seg in data:poly(np.c_[ox+seg[:,0]-lo,oy+(seg[:,1]-zmin)*vex],layer)
        views.append(dict(role='section'+str(j+1),cx=ox+L/2-1,cy=oy+H/2-.5,scale=scale))
        px=197.836392541923 if j==0 else 475.411513280285
        circle(px-47,132,4.4);text(px-47,132,str(j+1),2.8,8,layer='CUT',align='middle')
        text(px+7,136,'SECCIÓN '+str(j+1)+' - '+('LONGITUDINAL' if j==0 else 'TRANSVERSAL'),2.7,135,align='middle');text(px+7,130,f'ESCALA H 1:{denom} / V 1:{denom/vex:g}',2.2,100,align='middle')
        text(ox+L/2,oy-2,'DISTANCIA (m)',ph,16,'PROFILETEXT','model','middle')
        profiles.append((direction,lo,hi,tg,zg,okg,td,zd,okd,zmin,zmax))
    # Enlarged real cross-section through the crest, with no invented liner thickness.
    direction=directions[0];crossings=polygon_crossings(rim,fc,direction);at=float(crossings.min())
    dlo,dhi=at-1.2,at+2.;td,zd,okd=profile(design,fc,direction,dlo,dhi);tg,zg,okg=profile(ground,fc,direction,dlo,dhi)
    vals=np.r_[zd[okd],zg[okg]];base=math.floor(vals.min())-.3;top=math.ceil(vals.max())+.3;ox=11000;oy=10000
    for ts,zs,ok,layer in [(td,zd,okd,'PROFILEDESIGN'),(tg,zg,okg,'PROFILETN')]:
        for seg in segments(ts,zs,ok):poly(np.c_[ox+seg[:,0]-dlo,oy+seg[:,1]-base],layer)
    detail_denom=int(math.ceil(max(1000*(dhi-dlo+1)/95.133,1000*(top-base+.6)/77.27)/5)*5)
    views.append(dict(role='detail',cx=ox+(dhi-dlo)/2,cy=oy+(top-base)/2,scale=1000/detail_denom))
    text(ox+(dhi-dlo)/2,oy+top-base-.15,'SUPERFICIE DE DISEÑO',2.1/(1000/detail_denom),3.8,'PROFILETEXT','model','middle')
    text(ox+(dhi-dlo)/2,oy+.2,'TERRENO NATURAL',2.1/(1000/detail_denom),3.8,'PROFILETEXT','model','middle')
    text(652,451,'DETALLE A - CORONACIÓN Y TALUD',2.4,116,align='middle');text(652,444,f'GEOMETRÍA DE DISEÑO - ESCALA 1:{detail_denom}',1.9,116,align='middle')
    corner=rim[0,:2]+ORIGIN;views.append(dict(role='corner',cx=float(corner[0]),cy=float(corner[1]),scale=10))
    text(636,572,'A',3,12,align='middle');text(636,566,'CORONA',1.7,25,align='middle')
    # Project location image (same region as the supplied SOTRAMI files).
    rect(725.5,478.5,100,100);text(775.5,473,'MAPA DE LOCALIZACIÓN',2.6,100,align='middle');text(775.5,467,'SOTRAMI - SANTA FILOMENA',2,100,align='middle')
    # Legend follows exactly the right-hand reference column.
    rect(725.5,311.5,100,144);text(775.5,450,'LEYENDA',2.5,95,align='middle')
    for y,layer,label in [(437,'TN','TERRENO NATURAL'),(420,'TNMAJOR','CURVA MAESTRA TN'),(403,'CONTOUR','SUPERFICIE DE DISEÑO'),(386,'RIM','CORONACIÓN DE POZA'),(369,'FLOOR','FONDO DE POZA'),(352,'CUT','LÍNEA DE SECCIÓN'),(335,'PROFILEDESIGN','PERFIL DE DISEÑO')]:
        poly([[731,y],[750,y]],layer,'paper');text(755,y+2,label,2.1,66)
    # Volumes integrated by grid; report the method and coverage explicitly.
    step=.25;xx=np.arange(rim[:,0].min()+step/2,rim[:,0].max(),step);yy=np.arange(rim[:,1].min()+step/2,rim[:,1].max(),step);gx,gy=np.meshgrid(xx,yy);coords=np.c_[gx.ravel(),gy.ravel()];inside=MPath(rim[:,:2]).contains_points(coords);pts=coords[inside]
    gz=ground[2](pts[:,0],pts[:,1]);dz=design[2](pts[:,0],pts[:,1]);valid=~(np.ma.getmaskarray(gz)|np.ma.getmaskarray(dz));coverage=float(valid.sum()/len(pts))
    delta=np.asarray(gz)[valid]-np.asarray(dz)[valid];cut=float(np.maximum(delta,0).sum()*step**2);fill=float(np.maximum(-delta,0).sum()*step**2)
    # Lining surface area uses the design triangle slopes at grid points.
    dx,dy=design[2].gradient(pts[:,0],pts[:,1]);validarea=~(np.ma.getmaskarray(dx)|np.ma.getmaskarray(dy));lining=float(np.sqrt(1+np.asarray(dx)[validarea]**2+np.asarray(dy)[validarea]**2).sum()*step**2)
    level=2444. if code=='008' else float(rim[:,2].min());storage=float(np.maximum(level-np.asarray(dz)[valid],0).sum()*step**2)
    def table(x,y,w,title,rows,rowh=7):
        primitives.append(dict(kind='hatch',pts=[[x-.5,y+9,0],[x+w+.5,y+9,0],[x+w+.5,y-len(rows)*rowh-.5,0],[x-.5,y-len(rows)*rowh-.5,0]],layer='WHITEMASK',space='paper'))
        text(x+w/2,y+5,title,2.5,w,align='middle');rect(x,y-len(rows)*rowh,w,len(rows)*rowh)
        split=x+w*.70;poly([[split,y],[split,y-len(rows)*rowh]],'FRAME','paper')
        for i,(label,value) in enumerate(rows):
            if i:poly([[x,y-i*rowh],[x+w,y-i*rowh]],'FRAME','paper')
            text(x+2,y-i*rowh-1.1,label,2.3,w*.69-3);text(split+(x+w-split)/2,y-i*rowh-rowh/2,value,2.3,w*.29-2,align='middle')
    table(585.058,420,126.756,'DATOS GEOMÉTRICOS / REVESTIMIENTO',[
        ('Área en planta delimitada',f'{area:.1f} m²'),('Área geométrica de revestimiento',f'{lining:.1f} m²'),('Cota mínima de coronación',f'{rim[:,2].min():.2f} m'),('Cota mínima de fondo',f'{floor[:,2].min():.2f} m'),('Tipo y espesor de revestimiento','VER EETT')])
    table(535.583,364,176.231,'MOVIMIENTO DE TIERRAS / VOLUMEN GEOMÉTRICO',[
        ('Corte entre TN y superficie de diseño',f'{cut:.1f} m³'),('Relleno entre TN y superficie de diseño',f'{fill:.1f} m³'),(f'Volumen geométrico hasta EL. {level:.2f}',f'{storage:.1f} m³'),('Método: malla 0.25 m / límite coronación',f'{coverage*100:.1f}% DATOS')],rowh=6.8)
    notes=[
        'NOTAS GENERALES:',
        '1. Todas las dimensiones y cotas están expresadas en metros, salvo indicación distinta.',
        '2. Sistema de coordenadas: UTM WGS84, zona 18S, según los datos del proyecto SOTRAMI.',
        '3. Geometría de diseño obtenida de POZAS.dwg. Terreno natural obtenido de TERRENO EXISTENTE.dwg. Se conservan superficies TIN locales y editables en este archivo.',
        '4. Las secciones se obtienen por intersección con las caras TIN. Exageración vertical '+('1:1' if code=='009' else '2:1')+'; las escalas horizontal y vertical se indican en cada sección.',
        'REVESTIMIENTO:',
        '5. El área indicada es la superficie geométrica dentro de la coronación. No incluye traslapes, anclajes adicionales ni desperdicios.',
        '6. El material, espesor, protección y anclaje del revestimiento deberán corresponder a las especificaciones técnicas de SOTRAMI. Este plano no asigna espesores nuevos.',
        '7. Antes de colocar el revestimiento, verificar una superficie regular, libre de elementos punzocortantes y material suelto.',
        'METRADOS Y VERIFICACIÓN:',
        '8. Los volúmenes TN/diseño se integran en malla de 0.25 m dentro del límite de coronación. Corresponden a comparación de superficies; no incluyen sobreexcavación por capas de revestimiento.',
        f'9. El volumen geométrico de almacenamiento se calcula hasta EL. {level:.2f} m'+(' (lámina de diseño indicada en el archivo fuente).' if code=='008' else ' (coronación mínima); no descuenta borde libre operativo.'),
        '10. Verificar cotas y replanteo en campo. Resolver discrepancias con los planos y las especificaciones del proyecto antes de ejecutar.',
        'FUENTES: POZAS.dwg (21/10/2026), TERRENO EXISTENTE.dwg (07/08/2026). Distribución gráfica: plano de referencia NORCOBRE.'
    ]
    text(610.5,301.5,'\n\n'.join(notes),2.5,209)
    # Small professional footer identifies this coordinated review set.
    text(36.5,87,'SOTRAMI | PLANTA, SECCIONES Y DETALLES',2.2,400)
    text(36.5,81,'SUPERFICIES LOCALES - COORDENADAS ORIGINALES DEL PROYECTO',1.8,400)
    result=dict(code=code,title=title,primitives=primitives,views=views,metrics=dict(area=area,lining=lining,cut=cut,fill=fill,storage=storage,level=level,coverage=coverage),center=pc.tolist())
    (ROOT/'Data'/('drawing_'+code+'.json')).write_text(json.dumps(result,ensure_ascii=False,separators=(',',':')),encoding='utf-8')
    summaries.append(dict(code=code,title=title,center=pc.tolist(),metrics=result['metrics'],primitives=len(primitives)))
(ROOT/'metrics.json').write_text(json.dumps(summaries,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(summaries,ensure_ascii=False,indent=2))
