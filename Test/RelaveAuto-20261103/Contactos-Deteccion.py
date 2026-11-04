import json, pathlib, sys
from PIL import Image, ImageDraw

root=pathlib.Path(__file__).resolve().parent/(sys.argv[1] if len(sys.argv)>1 else 'Deteccion-12')
for group in (1,2,3):
    folders=sorted(p for p in root.glob(f'*Flume{group}*') if p.is_dir())
    sheet=Image.new('RGB',(1560,4*320),(28,32,36)); draw=ImageDraw.Draw(sheet)
    for row,folder in enumerate(folders):
        batch=json.loads((folder/'resultado-vision.json').read_text())
        photos=batch['Photos']; indexes=[0,len(photos)//2,len(photos)-1]
        for col,i in enumerate(indexes):
            p=photos[i];path=folder/'Revision'/f"{pathlib.Path(p['File']).stem}-perfil.png"
            if not path.exists(): path=folder/'Revision'/f"{pathlib.Path(p['File']).stem}-canal.png"
            im=Image.open(path);im.thumbnail((510,255));sheet.paste(im,(col*520,row*320+35))
            draw.text((col*520+8,row*320+8),f"{folder.name.split('Flow')[1]} / {p['File']} / {len(p['Surface'])} puntos",fill='white')
    sheet.save(root/f'Flume{group}-contactos.png')
