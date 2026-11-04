"""Render a review image from the geometry re-read from the saved DWG."""
import json
import math
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parent
rows = json.loads((ROOT / 'resultado.json').read_text(encoding='utf-8-sig'))

def render(bounds, scale, filename):
    xmin, ymin, xmax, ymax = bounds
    canvas = Image.new('RGB', (round((xmax-xmin)*scale), round((ymax-ymin)*scale)), '#212830')
    def world(p):
        return ((p[0]-xmin)*scale, (ymax-p[1])*scale)
    for row in (r for r in rows if r['type'] == 'RasterImage'):
        source = Image.open(ROOT / 'Entrega' / row['source'].replace('\\', '/')).convert('RGB')
        w, h = source.size
        o, u, v = row['origin'], row['u'], row['v']
        origin = world([o[0]+v[0], o[1]+v[1]])
        a, b, d, e = scale*u[0]/w, -scale*v[0]/h, -scale*u[1]/w, scale*v[1]/h
        corners = [world([o[0]+i*u[0]+j*v[0], o[1]+i*u[1]+j*v[1]]) for i,j in [(0,0),(0,1),(1,0),(1,1)]]
        left, top = max(0, math.floor(min(x for x,y in corners))), max(0, math.floor(min(y for x,y in corners)))
        right, bottom = min(canvas.width, math.ceil(max(x for x,y in corners))), min(canvas.height, math.ceil(max(y for x,y in corners)))
        if right <= left or bottom <= top:
            continue
        det = a*e-b*d
        cx, cy = left-origin[0], top-origin[1]
        inverse = (e/det, -b/det, (e*cx-b*cy)/det, -d/det, a/det, (-d*cx+a*cy)/det)
        rgba = source.convert('RGBA').transform((right-left,bottom-top), Image.AFFINE, inverse, Image.BICUBIC)
        canvas.paste(rgba, (left,top), rgba)
    draw = ImageDraw.Draw(canvas)
    for row in (r for r in rows if r['type']=='Polyline'):
        points = [world(p) for p in row['points']]
        if row.get('closed'):
            points.append(points[0])
        if len(points)>1:
            draw.line(points, fill='#ff3030', width=2 if scale>=400 else 1)
    font = ImageFont.truetype(r'C:\Windows\Fonts\arial.ttf', round(.20*scale))
    for row in (r for r in rows if r['type']=='MText'):
        center = world([(row['min'][0]+row['max'][0])/2, row['max'][1]])
        draw.text(center, row['text'], font=font, fill='white', anchor='mt')
    canvas.save(ROOT/'Entrega'/filename)
    return canvas

if __name__ == '__main__':
    render((38.8,-1.2,61,14), 130, 'vista_general.png')
    render((56.9,-1.2,60.9,1.9), 420, 'detalle_0.png')
    render((38.8,-1.2,42.8,1.9), 420, 'detalle_40.png')
    render((47.8,10.8,52,14), 420, 'detalle_220.png')
    print('Vistas generadas a partir del DWG guardado.')
