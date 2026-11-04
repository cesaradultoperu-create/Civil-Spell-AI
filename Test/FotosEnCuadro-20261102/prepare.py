"""Reuse the approved 0.00 calibration for every frame of the same video."""
import json
import math
import shutil
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parent
SOURCE = Path(r'C:\Users\Cesar Ugaz\Pictures\poner fotos en cuadro\fotos y excel')
DELIVERY = ROOT / 'Entrega'
DELIVERY.mkdir(exist_ok=True)
(DELIVERY / 'fotos').mkdir(exist_ok=True)
model = json.loads((ROOT / 'modelo.json').read_text(encoding='utf-8-sig'))
rectangles = {}
for row in model:
    if (row['type'] == 'Polyline' and row.get('closed')
            and abs(row['max'][0] - row['min'][0] - 2.2) < 1e-6
            and abs(row['max'][1] - row['min'][1] - 1) < 1e-6):
        rectangles[tuple(round(v, 7) for v in row['min'][:2])] = row
labels = {}
for label in (row for row in model if row['type'] == 'MText'):
    center = (label['min'][0] + label['max'][0]) / 2
    rect = min(rectangles.values(), key=lambda p:
               abs((p['min'][0] + p['max'][0]) / 2 - center)
               + abs(p['max'][1] - (label['min'][1] - .3)))
    labels[round(float(label['text']) * 100)] = rect
reference = next(row for row in model if row['type'] == 'RasterImage')

def inverse_pixel(row, world):
    dx, dy = world[0] - row['origin'][0], world[1] - row['origin'][1]
    u, v = row['u'], row['v']
    det = u[0] * v[1] - u[1] * v[0]
    return [(dx * v[1] - dy * v[0]) / det * row['imageSize'][0],
            (u[0] * dy - u[1] * dx) / det * row['imageSize'][1]]

reference_frame = labels[0]
reference_left = inverse_pixel(reference, reference_frame['min'])
reference_right = inverse_pixel(reference, [reference_frame['max'][0], reference_frame['min'][1], 0])
zoom = json.loads((ROOT/'zoom.json').read_text())
plan = []
for photo in sorted(SOURCE.glob('*.png'), key=lambda p: int(p.stem)):
    number = int(photo.stem)
    if number not in labels:
        raise ValueError('No matching frame for ' + photo.name)
    frame = labels[number]
    left = [frame['min'][0], frame['min'][1], 0]
    right = [frame['max'][0], frame['min'][1], 0]
    size = list(Image.open(photo).size)
    if size != reference['imageSize']:
        raise ValueError('Calibration size mismatch: ' + photo.name)
    registration = zoom.get(str(number),dict(scale=1,tx=0,ty=0))
    def source_point(point):
        return [registration['scale']*point[0]+registration['tx'],
                size[1]-(registration['scale']*(size[1]-point[1])+registration['ty'])]
    item = dict(file=photo.name, label=f'{number / 100:.2f}',
                width=size[0], height=size[1],
                sourceLeft=source_point(reference_left),
                sourceRight=source_point(reference_right),
                destinationLeft=left, destinationRight=right,
                calibration='modelo_0_para_todas', screenshotScale=registration['scale'])
    for p in [item['sourceLeft'], item['sourceRight']]:
        if not (0 <= p[0] <= size[0] and 0 <= p[1] <= size[1]):
            raise ValueError('Guide outside photograph: ' + photo.name)
    plan.append(item)
    shutil.copy2(photo, DELIVERY / 'fotos' / photo.name)
(ROOT / 'plan.json').write_text(json.dumps(plan, indent=2), encoding='utf-8')
print(f'{len(plan)} fotos; cuadros libres: ' + ', '.join(
    f'{n / 100:.2f}' for n in sorted(labels) if not (SOURCE / f'{n}.png').exists()))
print('Giro comun: %.12f grados' % (math.degrees(math.atan2(reference['u'][1], reference['u'][0])) % 360))
