import sys, json, hashlib, math
from pathlib import Path
from collections import Counter

root=Path(__file__).resolve().parent
sys.path.insert(0,str(root/'python-libs'))
import fitz
out=Path(r'C:\Users\Cesar Ugaz\Pictures\TRABAJO DE HOY 29\DETALLES DE DETECCION DE FUGA')
rows=json.loads((root/'published-reopened.json').read_text(encoding='utf-8-sig'))
initial=json.loads((root/'inicial.json').read_text(encoding='utf-8-sig'))
v=[r for r in rows if r.get('type')=='Viewport' and r.get('layer')=='DF_VENTANAS']
assert len(v)==3 and all(r['on'] for r in v)
assert sorted(round(r['scale'],6) for r in v)==[40,40,100]
values=sorted(r['measurement'] for r in rows if r.get('type')=='RotatedDimension')
expected=sorted([4,4,2,2,.8,.35,2,.5,1,2,.2,.6,.5,1,.5])
assert len(values)==15 and all(abs(a-b)<1e-8 for a,b in zip(values,expected))
border=next(r for r in rows if r.get('h')=='89E6')
original=next(r for r in initial if r.get('h')=='89E6')
assert border['block']==original['block']=='MEMB_WWL_P_A1_'
attrs={r['tag']:r['text'] for r in border['attrs']}
source_attrs={r['tag']:r['text'] for r in original['attrs']}
for k in ('CLIENT','PROJECT','TITTLE_1','TITTLE_2'):
    assert attrs[k]==source_attrs[k]
assert attrs['DRAWING_NO']=='1007.001.00.14-200-15-P-004'
assert not [r for r in rows if r['kind']=='xref']
images=[r for r in rows if r['kind']=='image']
assert len(images)==3 and all(Path(r['path']).is_file() for r in images)
pdf_path=out/'Detalles de deteccion de fuga - Layout A.pdf'
final=fitz.open(pdf_path)
check=fitz.open(root/'published-reopened.pdf')
assert len(final)==len(check)==1
assert len(final[0].get_images())==3
final_pix=final[0].get_pixmap(matrix=fitz.Matrix(.8,.8))
check_pix=check[0].get_pixmap(matrix=fitz.Matrix(.8,.8))
assert final_pix.samples==check_pix.samples,'La impresión del DWG reabierto no coincide.'
final_pix.save(str(out/'Vista previa.png'))
sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest()
source=out.parent/'1007.001.00.14-200-15-P-004- NUEVO DISEÑO.dwg'
source_unchanged=sha(source)==sha(root/'Fuente'/'Base.dwg')
report={
 'layout':'A','template_preservado':border['block'],
 'cliente_y_proyecto_originales_conservados':True,
 'ventanas':[{'escala':r['scale'],'encendida':r['on']} for r in v],
 'cotas_nativas_verificadas':15,'llamadas_nativas':sum(r.get('type')=='Leader' for r in rows),
 'entidades_modelo':dict(Counter(r.get('type') for r in rows if r.get('layout')=='Model' and r['kind']=='entity')),
 'imagenes_existentes':3,'referencias_DWG_externas':0,
 'reapertura_e_impresion_identicas':True,'original_sin_cambios_desde_la_copia':source_unchanged,
 'hash_dwg':sha(out/'1007.001.00.14-200-15-P-004 - DETALLES DE DETECCION DE FUGA.dwg'),
 'hash_pdf':sha(pdf_path)
}
(root/'verificacion-final.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
(out/'LEEME.txt').write_text('''DETALLES DE DETECCION DE FUGA - 29/10/2026

Abra el DWG y seleccione el layout A.
El membrete, cliente y datos del proyecto se conservan del dibujo recibido.
La lámina contiene planta, sección de trinchera, sección longitudinal,
cotas, llamadas, notas constructivas y leyenda de materiales.

Los detalles son entidades CAD editables, en metros y coordenadas locales.
Escalas: 1:25 para planta y sección longitudinal; 1:10 para la trinchera.
Formato A1. Las escalas equivalentes a reducción A3 se indican en la hoja.
El PDF se generó desde el mismo layout A y se comprobó al reabrir el DWG.

El mapa de localización se conserva como imagen; los logotipos son los
del template original. Las tres imágenes están en la carpeta Assets.
Sus rutas apuntan a esta carpeta de entrega. Si traslada el trabajo a otra
ubicación, vuelva a vincular estas imágenes mediante la paleta XREF.

Referencia: 100591-01-B-515-4.pdf. Adaptación de detalles de referencia;
se conserva el estado REVISIÓN INTERNA del membrete.
El DWG y el PDF originales permanecen en la carpeta superior.
''',encoding='utf-8-sig')
print(json.dumps(report,ensure_ascii=False,indent=2))
