"""Generate close views of every channel base, using the saved DWG transforms."""
import json
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
from preview import render, ROOT

plan = json.loads((ROOT/'plan.json').read_text())
review = ROOT/'Entrega'/'Revision'
review.mkdir(exist_ok=True)
font = ImageFont.truetype(r'C:\Windows\Fonts\arial.ttf', 27)
tiles = []
for item in plan:
    left = item['destinationLeft']
    right = [left[0]+2.2,left[1],0]
    name = f"Revision/base_{Path(item['file']).stem}.png"
    detail = render((left[0]-.13, left[1]-.12, right[0]+.13, left[1]+.55), 500, name)
    tile = Image.new('RGB', (1230, 390), '#212830')
    tile.paste(detail, (0,55))
    ImageDraw.Draw(tile).text((15,12), f"{item['label']} | {item['file']} | giro 358d41m20s", fill='white', font=font)
    tiles.append(tile)
for start in range(0,len(tiles),8):
    sheet = Image.new('RGB', (2480,4*400), '#30363c')
    for i,tile in enumerate(tiles[start:start+8]):
        sheet.paste(tile, ((i%2)*1250, (i//2)*400))
    sheet.save(review/f'revision_{start//8+1}.png')
print('22 vistas individuales y 3 laminas de revision.')
