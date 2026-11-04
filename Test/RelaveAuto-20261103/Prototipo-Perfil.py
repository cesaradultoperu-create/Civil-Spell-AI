import pathlib,json, numpy as np
from PIL import Image,ImageDraw

root=pathlib.Path(__file__).resolve().parent/'Deteccion-12'
def lab(im):
    a=np.asarray(im,dtype=float)/255
    a=np.where(a>.04045,((a+.055)/1.055)**2.4,a/12.92)
    xyz=a@np.array([[.4124564,.2126729,.0193339],[.3575761,.7151522,.119192],[.1804375,.072175,.9503041]])/np.array([.95047,1,1.08883])
    f=np.where(xyz>.008856,xyz**(1/3),7.787*xyz+16/116)
    return np.stack(((116*f[:,:,1]-16)*2.55,500*(f[:,:,0]-f[:,:,1])+128,200*(f[:,:,1]-f[:,:,2])+128),axis=2)
def median(a,axis):return np.median(a,axis=axis)
def run(folder):
    batch=json.loads((folder/'resultado-vision.json').read_text());photos=batch['Photos']
    ref=photos[0];im=Image.open(ref['Preview']).convert('RGB');arr=lab(im);h,w=arr.shape[:2]
    start=int(ref['GridAnchorX']+max(20,(w-24)*.06));end=w-28
    if end-start<40:return
    top0=int(h*.1);top1=int(h*.28)
    row=median(arr[:,start:end,:],1);upper=median(row[top0:top1],0)
    column=median(arr[top0:top1],0)-upper
    bg=row[:,None,:]+column[None,:,:]
    delta=arr-bg
    left=5;right=int(ref['GridAnchorX']*.6)
    late=lab(Image.open(photos[-1]['Preview']).convert('RGB'))-bg
    wet=median(late[int(h*.6):int(h*.85),int(w*.05):int(w*.25),:].reshape(-1,3),0)
    strength=np.linalg.norm(wet[1:]); print(folder.name,wet,strength)
    output=folder/'Proto';output.mkdir(exist_ok=True)
    for p in photos:
        if p['Frame']['Number']==0:continue
        im=Image.open(p['Preview']).convert('RGB');a=lab(im);d=a-bg
        projection=(d[:,:,1]*wet[1]+d[:,:,2]*wet[2])/strength
        # combine reliable hue with intensity as corroboration only
        side=np.abs(d[:,:,1]*wet[2]-d[:,:,2]*wet[1])/strength
        score=np.clip((projection-strength*.45)/max(1,strength*.3),-2,2)
        score-=np.maximum(0,side-max(3,strength))*.2
        # wet evidence must retain the learned chromatic direction
        if wet[0]<-4:score+=np.clip(d[:,:,0]/wet[0]-.4,-1,1)*.2
        # suppress grid/shadow with median over neighboring columns
        xs=np.arange(12,w-12,4)
        features=np.stack([np.median(score[:,x-6:x+7],axis=1) for x in xs],axis=0)
        # candidate row is boundary; wet expected below it, dry above it
        cumulative=np.concatenate((np.zeros((len(xs),1)),np.cumsum(features,axis=1)),axis=1)
        cost=2*cumulative-cumulative[:,-1:]
        edge=np.stack([np.median(projection[:,x-6:x+7],axis=1) for x in xs],axis=0)
        for y in range(4,h-4):
            cost[:,y]-=np.clip(np.mean(edge[:,y+1:y+5],axis=1)-np.mean(edge[:,y-4:y],axis=1),0,6)*1.2
        # slight preference for no material when signal is inconclusive
        cost+=.02*(h-np.arange(h+1))[None,:]
        cost[:,:int(h*.12)]=1e6
        prev=cost[0].copy();parents=np.zeros((len(xs),h+1),dtype=np.int32)
        for i in range(1,len(xs)):
            options=[]
            for offset in range(-3,4):
                source=np.arange(h+1)+offset
                valid=(source>=0)&(source<=h)
                option=prev[np.clip(source,0,h)]+abs(offset)*3
                option[~valid]=1e6;options.append(option)
            choices=np.argmin(options,axis=0)
            parents[i]=np.arange(h+1)+choices-3
            prev=cost[i]+np.min(options,axis=0)
        y=int(np.argmin(prev));path=[]
        for i in range(len(xs)-1,-1,-1):
            path.append((int(xs[i]),y));y=int(parents[i,y])
        path.reverse()
        # preserve raw path for quantitative comparisons
        (output/(p['File']+'.json')).write_text(json.dumps(path))
        view=im.resize((w*2,h*2));draw=ImageDraw.Draw(view)
        draw.line([(x*2,min(h-1,y)*2) for x,y in path],fill='yellow',width=2)
        draw.line([(p['GridAnchorX']*2,0),(p['GridAnchorX']*2,h*2)],fill='red',width=1)
        view.save(output/(p['File']))

for folder in sorted(p for p in root.glob('*Flume*') if p.is_dir()):
    if 'Flume1' not in folder.name:run(folder)
