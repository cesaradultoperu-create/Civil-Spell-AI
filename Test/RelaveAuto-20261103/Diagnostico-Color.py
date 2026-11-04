exec(open(__file__.replace('Diagnostico-Color.py','Prototipo-Perfil.py')).read().split('for folder in sorted')[0])
for folder in sorted(p for p in root.glob('*Flume*') if p.is_dir()):
    if 'Flume1' in folder.name:continue
    batch=json.loads((folder/'resultado-vision.json').read_text());photos=batch['Photos']; ref=photos[0]
    arr=lab(Image.open(ref['Preview']).convert('RGB'));h,w=arr.shape[:2]
    start=int(ref['GridAnchorX']+max(20,(w-24)*.06));end=w-28
    row=median(arr[:,start:end,:],1);top0=int(h*.1);top1=int(h*.28);upper=median(row[top0:top1],0)
    bg=row[:,None,:]+(median(arr[top0:top1],0)-upper)[None,:,:]
    import sys
    last=lab(Image.open(photos[0 if '--first' in sys.argv else -1]['Preview']).convert('RGB'))-bg
    print(folder.name)
    first=arr-bg
    seed=first[int(h*.7):int(h*.92),24:max(40,int(ref['GridAnchorX']*.6)),:]
    print('first seed B quantiles',np.round(np.percentile(seed[:,:,2],[50,75,85,90]),2))
    for y0,y1 in [(.9,.98),(.75,.85),(.6,.7),(.4,.5),(.2,.3)]:
        print(y0, np.round(np.median(last[int(h*y0):int(h*y1),int(w*.05):int(w*.25),:].reshape(-1,3),axis=0),2))
