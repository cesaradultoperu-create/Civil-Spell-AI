from pathlib import Path
import sys,json,hashlib
from collections import Counter

root=Path(__file__).resolve().parent
sys.path.insert(0,str(root/'python-libs'))
import fitz
work=root/'V2'
out=Path(r'C:\Users\Cesar Ugaz\Pictures\TRABAJO DE HOY 29\DETALLES DE DETECCION DE FUGA - V2')
read=lambda p:json.loads(Path(p).read_text(encoding='utf-8-sig'))
rows=read(work/'reopened.json')
source=read(root/'v2-source.json')
library=read(work/'block-reopened.json')
block_audit=read(work/'block-audit.json')
assert block_audit['entidades_originales']==408
assert block_audit['entidades_mejoradas']>408
assert block_audit['texto_compatible_fondo_oscuro']
assert block_audit['unidades']=='Meters'
viewports=[r for r in rows if r.get('type')=='Viewport' and r.get('layer')=='DF_VENTANAS']
assert len(viewports)==3 and all(v['on'] for v in viewports)
assert sorted(round(v['scale'],6) for v in viewports)==[40,50,100]
expected=sorted([4,4,2,2,.8,.35,2,.5,1,2,.2,.6,.5,.2,1,.5])
actual=sorted(r['measurement'] for r in rows if r.get('type')=='RotatedDimension')
assert len(actual)==16 and all(abs(a-b)<1e-8 for a,b in zip(actual,expected))
border=next(r for r in rows if r.get('h')=='89E6')
initial=next(r for r in source if r.get('h')=='89E6')
assert border['block']==initial['block']=='MEMB_WWL_P_A1_'
attrs={r['tag']:r['text'] for r in border['attrs']}
original_attrs={r['tag']:r['text'] for r in initial['attrs']}
assert all(attrs[k]==original_attrs[k] for k in ('CLIENT','PROJECT','TITTLE_1','TITTLE_2'))
assert attrs['DRAWING_NO']=='1007.001.00.14-200-15-P-004'
assert not [r for r in rows if r['kind']=='xref']
assert not [r for r in library if r['kind'] in ('xref','image')]
assert len([r for r in library if r.get('type')=='RotatedDimension'])==10
assert any(r.get('block')=='DF_CODO_MEJORADO_V2' for r in library)
images=[r for r in rows if r['kind']=='image']
assert len(images)==3 and all(Path(r['path']).is_file() for r in images)
overlaps=[]
for space in ('Model','A'):
    texts=[r for r in rows if r.get('type')=='MText' and r.get('layout')==space and r.get('layer')=='DF_TEXTOS']
    for i,a in enumerate(texts):
        for b in texts[i+1:]:
            dx=min(a['max'][0],b['max'][0])-max(a['min'][0],b['min'][0])
            dy=min(a['max'][1],b['max'][1])-max(a['min'][1],b['min'][1])
            if dx>1e-5 and dy>1e-5:overlaps.append((a['h'],b['h']))
assert not overlaps
pdf=out/'Deteccion de fugas - Presentacion V2.pdf'
with fitz.open(pdf) as final,fitz.open(work/'reopened.pdf') as reopened:
    assert len(final)==len(reopened)==1
    assert len(final[0].get_images())==3
    assert final[0].get_pixmap().samples==reopened[0].get_pixmap().samples
    final[0].get_pixmap(matrix=fitz.Matrix(.9,.9)).save(str(out/'Vista previa V2.png'))
sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest()
report={
 'layout':'A','membrete_original_conservado':True,
 'cliente_y_proyecto_conservados':True,'escalas_A1':{'planta':'1:25','seccion_transversal':'1:10','seccion_longitudinal':'1:20'},
 'cotas_nativas_verificadas':16,'superposiciones_entre_rotulos':0,
 'bloque':block_audit,'referencias_DWG_externas':0,'imagenes_del_membrete_verificadas':3,
 'DWG_reabierto_e_impresion_identica':True,'bloque_independiente_reabierto':True,
 'fuente_guardada_sin_cambios_desde_la_copia':sha(out.parent/'1007.001.00.14-200-15-P-004- NUEVO DISEÑO.dwg')==sha(root/'Fuente'/'Avance-con-referencias.dwg'),
 'sha256_pdf':sha(pdf),'sha256_dwg':sha(out/'1007.001.00.14-200-15-P-004 - DETECCION DE FUGAS - PRESENTACION V2.dwg')
}
(work/'verificacion-final.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
(out/'LEEME V2.txt').write_text('''DETECCIÓN DE FUGAS - PRESENTACIÓN V2 - 29/10/2026

LÁMINA COMPLETA
Abra el DWG cuyo nombre termina en PRESENTACION V2 y seleccione el layout A.
Se conserva el membrete original del cliente, con sus datos de proyecto y logotipos.
La lámina contiene planta, sección transversal, sección longitudinal y notas.

MEJORAS
Se utilizó el bloque codo original y las tapas HDPE de las referencias añadidas.
El bloque actualizado se llama DF_CODO_MEJORADO_V2.
Incluye unión curvada, corrugado completado y símbolo de sección de tubería.
La geometría del detalle, las tramas, las cotas y los textos son entidades CAD.
Se diferenciaron grava, terreno, sacos de arena, concreto, geonet, geomembranas
y GCL. Se detallaron soporte, bota, trinchera de anclaje, losa y plancha metálica.
El mapa de localización y los logotipos son imágenes del membrete.

BLOQUE INDEPENDIENTE
El archivo Bloque - Sistema de deteccion de fugas V2.dwg contiene la sección
longitudinal completa para insertarla en otro dibujo. Unidades: metros.
El dibujo principal también conserva el bloque codo original en su biblioteca.

ESCALAS Y FORMATO
A1: planta 1:25; sección transversal 1:10; sección longitudinal 1:20.
La sección longitudinal se amplió a 1:20 para mejorar la lectura de los detalles.
A3 al 50%: escalas 1:50, 1:20 y 1:40, respectivamente.
Los detalles se dibujaron en coordenadas locales, en metros.

COMPROBACIÓN
Se reabrieron ambos DWG. La impresión del dibujo reabierto coincide con el PDF.
Se comprobaron 16 cotas nativas y la separación entre los rótulos.
El membrete conserva su estado REVISIÓN INTERNA.

ARCHIVOS DE APOYO
Mantenga Assets junto con el dibujo. Las imágenes apuntan a esta carpeta de
entrega; si traslada el conjunto a otro lugar, reubíquelas desde la paleta XREF.
La entrega anterior y el dibujo fuente se conservan por separado.
''',encoding='utf-8-sig')
print(json.dumps(report,ensure_ascii=False,indent=2))
