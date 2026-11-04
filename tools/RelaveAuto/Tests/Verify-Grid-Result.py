"""Independent checks of saved native areas, XLSX cells and black-line centers."""
import csv
import json
import math
from pathlib import Path
import statistics
import sys
import xml.etree.ElementTree as ET
import zipfile
from PIL import Image

base = Path(sys.argv[1])
data = json.loads((base / 'resultado.json').read_text(encoding='utf-8-sig'))
job_path = base / 'trabajo.json'
if not job_path.exists():
    job_path = base.parent / 'trabajo.json'
job = json.loads(job_path.read_text(encoding='utf-8-sig'))
c = job['Calibration']
dx, dy = c['B']['X'] - c['A']['X'], c['B']['Y'] - c['A']['Y']
length = math.hypot(dx, dy)
height = ((c['Top']['X'] - c['A']['X']) * dy - (c['Top']['Y'] - c['A']['Y']) * dx) / length
w, h = round(min(1600, length)), round(height * min(1600, length) / length)
photos = sorted((p for p in data['Photos'] if p['Frame']['Number'] > 0), key=lambda p: p['Frame']['Number'])
rows = []
for p in photos:
    assert p['RegistrationValid'] and p['AlignmentValid']
    image = Image.open(p['Preview']).convert('L')
    iw, ih = image.size
    anchor = p['GridAnchorX']
    lo, hi = max(0, round(anchor) - 5), min(iw - 1, round(anchor) + 5)
    gray = {x: statistics.mean(image.getpixel((x, y)) for y in range(round(ih * .25), round(ih * .75))) for x in range(lo, hi + 1)}
    low, high = min(gray.values()), max(gray.values())
    selected = [(x, high - value) for x, value in gray.items() if value <= low + (high - low) * .3]
    center = sum(x * weight for x, weight in selected) / sum(weight for _, weight in selected)
    error = abs(center - anchor)
    assert error < 2, (p['File'], error)
    surface = p['Surface']
    assert abs(surface[0]['X'] - p['GridOriginX']) < 1e-7
    assert abs(surface[-1]['X'] - p['Toe']) < 1e-7 and abs(surface[-1]['Y']) < 1e-7
    pixel_area = sum((b['X'] - a['X']) * (a['Y'] + b['Y']) / 2 for a, b in zip(surface, surface[1:]))
    world_x = (p['Frame']['MarkerX'] - p['Frame']['X']) / (anchor - p['GridOriginX'])
    world_y = world_x * height / (h - 1) / (length / (w - 1))
    assert abs(pixel_area * world_x * world_y - p['Area']) < 1e-9, p['File']
    assert abs(p['Toe'] - p['GridAnchorX']) < 1e-7, ('Punta fuera de estacion', p['File'])
    rows.append({'Foto': p['File'], 'Referencia': p['Frame']['Label'], 'Centro_negro_px': center,
                 'Anclaje_px': anchor, 'Error_centrado_px': error, 'Vertices_superficie': len(surface),
                 'Area_nativa_m2': p['Area'], 'Alcance_contorno_m': (p['Toe'] - p['GridOriginX']) * world_x})
ns = {'s': 'http://schemas.openxmlformats.org/spreadsheetml/2006/main'}
with zipfile.ZipFile(base / 'Areas_relave.xlsx') as archive:
    xml_rows = ET.fromstring(archive.read('xl/worksheets/sheet1.xml')).findall('s:sheetData/s:row', ns)[1:]
    assert len(xml_rows) == len(photos)
    for row, p in zip(xml_rows, photos):
        cells = {cell.attrib['r'][0]: cell for cell in row}
        assert float(cells['A'].find('s:v', ns).text) == p['Frame']['Number']
        assert float(cells['B'].find('s:v', ns).text) == p['Area']
        assert cells['E'].find('s:is/s:t', ns).text == p['Handle']
with (base / 'verificacion-cuadricula.tsv').open('w', newline='', encoding='utf-8-sig') as stream:
    writer = csv.DictWriter(stream, fieldnames=rows[0].keys(), delimiter='\t')
    writer.writeheader()
    writer.writerows(rows)
print(f"{len(rows)} perfiles: area independiente y Excel coinciden. Error de centro maximo {max(r['Error_centrado_px'] for r in rows):.3f} px; media {statistics.mean(r['Error_centrado_px'] for r in rows):.3f} px.")
print(f"Superficies de {min(r['Vertices_superficie'] for r in rows)} a {max(r['Vertices_superficie'] for r in rows)} vertices.")
