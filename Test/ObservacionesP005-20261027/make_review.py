from pathlib import Path
import json
import fitz

ROOT = Path(__file__).resolve().parent
OUT = ROOT / 'EntregaFinal'
SOURCE = ROOT / 'Fuente' / '1107.001.00.14-200-14-P-005_JR.pdf'
FONT = r'C:\Windows\Fonts\calibri.ttf'
font = fitz.Font(fontfile=FONT)
GREEN = (0.0, .43, .24)
ORANGE = (.72, .34, .03)
INK = (.12, .17, .22)

# The IDs follow the yellow rectangles in the review PDF, so repeated comments stay distinct.
DATA = {
1: ('Línea continua', 'Se unificó la tubería en planta y su símbolo de leyenda con trazo continuo azul.'),
2: ('Borde del cuadro', 'Se retiró de impresión la ventana del cuadro antiguo y su borde. La tabla nueva de control usa el formato de las otras tablas.'),
3: ('Color y formato WWL', 'Se normalizaron las tablas, sus textos y su presentación; se conservan el membrete y los recursos del proyecto.'),
4: ('Uniformizar tabla geométrica', 'Se reconstruyó la tabla de características con el mismo formato de la tabla de control y de volúmenes.'),
5: ('Pared doble o sólida', 'PENDIENTE: los archivos identifican HDPE de 4 pulgadas, pero no definen pared, SDR ni clase de presión. No se añadió una especificación supuesta.'),
6: ('Numerar tabla XX', 'La tabla reemplazada queda identificada como TABLA 01 - PUNTOS DE CONTROL.'),
7: ('Consultar nota a Royer', 'PENDIENTE: falta identificar la nota y el texto que debe confirmar Royer. No se contactó a terceros.'),
8: ('Mismo comentario del P-004', 'PENDIENTE: no se recibió la observación específica del P-004 a la que remite este comentario. Se hicieron las correcciones de formato visibles en el P-005.'),
9: ('Estructura de descarga en leyenda', 'Se retiraron el símbolo y el rótulo de estructura de descarga que no tenían una estructura correspondiente identificada en la planta. No se diseñó una estructura nueva.'),
10: ('Tablas a la derecha y ampliar vistas', 'Se llevaron las tablas al espacio inferior derecho. Planta y perfil se presentan a 1:175 en A1; las escalas gráficas se actualizaron.'),
11: ('Ampliar perfil para la caja inicial', 'Se amplió el perfil, conservando las cajas representadas y despejando sus llamadas. Las cotas y dimensiones definitivas de la caja quedan sujetas a la observación 39.'),
12: ('Ampliar el extremo del perfil', 'Se amplió la vista completa y se mantuvo visible la caja final y la progresiva de término.'),
13: ('Canal, color y leyenda', 'El tramo señalado se identifica como canal de descarga; se diferencia de la tubería por color y tiene entrada propia en la leyenda.'),
14: ('Mejorar legibilidad de leyenda', 'Se ampliaron los textos de la leyenda, se normalizaron sus anchos y se retiraron las entradas sin correspondencia visible.'),
15: ('Retirar llamado del plano', 'Se quitó el número de plano del llamado del canal de descarga. La referencia queda en el cuadro de referencias del membrete.'),
16: ('Extender las tuberías de contingencia', 'Se prolongaron las líneas gráficas existentes hasta el límite de la vista y se indicó CONTINÚA. Esta extensión de presentación no altera el trazado nativo del proyecto.'),
17: ('Flecha sin destino claro', 'Se retiró la flecha aislada situada sobre el sector de contingencia.'),
18: ('Referencia innecesaria', 'Se retiró el número de plano del llamado de contingencia y se conservó su identificación. El P-006 figura en las referencias del membrete.'),
19: ('Agregar talud 1.33', 'Se añadió el llamado TALUD 1.33H:1V con el valor indicado en la observación.'),
20: ('Cota en las banquetas', 'PENDIENTE: se debe confirmar qué puntos y qué superficie de diseño corresponden a las banquetas que se desea acotar. No se rotularon cotas inventadas.'),
21: ('Quitar y hacer coincidir con el perfil', 'Se retiraron los rectángulos sobredimensionados en planta; las llamadas E y G se llevaron a los extremos reales del eje y a sus cajas correspondientes.'),
22: ('Quitar rótulo señalado en planta', 'Se ocultaron los rótulos de pendiente de superficie que interferían con la identificación de la tubería en planta.'),
23: ('Tabla de puntos de control', 'Se reemplazó el cuadro de tangentes por puntos BP, PC, PT y EP, con coordenadas y rasante calculadas sobre el alineamiento y perfil nativos.'),
24: ('Imagen satelital', 'Se incorporó la imagen satelital del proyecto conservando su orientación y georreferenciación originales. La imagen ECW se incluye con la entrega.'),
25: ('Uniformizar el cuadro adicional con P-004', 'Se retiró de impresión el cuadro adicional de curvas del P-005 y se dejó una sola tabla de puntos de control. Los elementos de curvas siguen conservados en el modelo nativo.'),
26: ('Metrado con Álvaro', 'PENDIENTE: recibir el metrado validado por Álvaro. La tabla de corte indica POR DEFINIR; no se convirtió una cantidad no aprobada en dato de diseño.'),
27: ('Ver nota X de volúmenes netos', 'Se añadió la nota 3: los valores de la Tabla 02 son netos y no consideran factores de esponjamiento ni contingencias.'),
28: ('Estructura existente en leyenda', 'Se retiraron el texto y la muestra de estructura existente que no tenían correspondencia identificada en esta planta.'),
29: ('Agregar canal de descarga', 'Se incorporó la identificación de canal de descarga y su muestra diferenciada en la leyenda.'),
30: ('Nota de factores y contingencias', 'Se incorporó el texto solicitado como nota 3 y se vinculó con la Tabla 02.'),
31: ('Quitar rótulo intermedio', 'Se retiró el llamado de camino de anclaje de la planta para despejar el tramo señalado; su identificación se mantiene en el perfil.'),
32: ('Terreno existente', 'El llamado se identifica como TERRENO EXISTENTE y su punta corresponde al perfil nativo de terreno.'),
33: ('Subrasante de camino de anclaje', 'Se corrigió el nombre y se vinculó el llamado al perfil de subrasante. Su proyección se diferencia de la tubería en el perfil.'),
34: ('Redondear a un decimal', 'Las cotas añadidas a las bandas y los PVI se muestran a un decimal, a partir de los valores nativos. Los datos de geometría conservan su precisión original.'),
35: ('Línea del perfil', 'Se regularizaron el color, el grosor y la continuidad de las líneas de la tubería y se diferenció la subrasante del camino.'),
36: ('Superposición al inicio', 'Se conservaron los PVI nativos y se ocultaron sus rótulos superpuestos; se añadieron rótulos CAD separados y llamadas legibles a las mismas cotas.'),
37: ('Inicio de tubería dentro de la caja', 'Las cajas en planta se centraron sobre los extremos del eje sin modificar el alineamiento. La línea azul inicia dentro de la caja representada.'),
38: ('Final dentro de la caja y longitud 85 m', 'PARCIAL: el encuentro gráfico final se corrigió. Queda por confirmar si se exige 85.00 m en planta o sobre la pendiente: el eje horizontal conserva 84.642456 m.'),
39: ('Revisar detalle E', 'PENDIENTE: E sí corresponde a CAJA DE PASO 1 del P-012, pero sus cotas de origen no coinciden con el inicio del perfil P-005. Deben compatibilizarse las interfaces E/G y la sección incorporada.'),
40: ('Sección trapezoidal', 'Se sustituyó la sección rectangular por la sección 12 trapezoidal del P-012, con su modelo, dimensiones, cotas de origen y referencia. La compatibilidad de cotas se mantiene pendiente en 39.'),
41: ('Revisar referencias', 'Se verificaron las identificaciones P-001 y P-012 y se añadieron las referencias P-003 y P-006 que corresponden a los llamados de contexto.'),
}
PENDING = {5, 7, 8, 20, 26, 38, 39}

def text(page, rect, value, size=10, color=INK, align=0):
    page.insert_font(fontname='cal', fontfile=FONT)
    remain = page.insert_textbox(fitz.Rect(rect), value, fontname='cal', fontsize=size, color=color, align=align)
    if remain < 0:
        raise RuntimeError(f'Text overflow ({remain}): {value[:90]}')

def wrap(value, width, size):
    result=[]
    for para in value.split('\n'):
        line=''
        for word in para.split():
            trial=(line+' '+word).strip()
            if font.text_length(trial, fontsize=size)>width and line:
                result.append(line)
                line=word
            else:
                line=trial
        result.append(line)
    return result

def new_register(doc, heading, sub):
    p=doc.new_page(width=595.28,height=841.89)
    p.draw_rect(fitz.Rect(0,0,595.28,10),fill=GREEN,color=GREEN)
    text(p,(36,32,559,61),heading,19)
    text(p,(36,68,559,102),sub,10)
    p.draw_line((36,108),(559,108),color=(.78,.81,.83),width=.7)
    return p,124

def build():
    boxes=json.loads((ROOT/'obs-boxes.json').read_text())
    assert len(boxes)==41 and set(DATA)==set(range(1,42))
    doc=fitz.open(SOURCE)
    p=doc[0]
    # Stamps sit at the outside left of each yellow box, so the comment remains readable.
    for b in boxes:
        i=b['id']; x0,y0,x1,y1=b['rect']; x=max(3,x0-15); y=max(2,y0+1)
        color=ORANGE if i in PENDING else GREEN
        p.draw_rect(fitz.Rect(x-1,y,x+12,y+9),fill=(1,1,1),color=color,width=.5,overlay=True)
        text(p,(x,y-.5,x+12,y+10),f'{i:02}',6.5,color,1)
        if i not in PENDING:
            cy=y+13
            p.draw_rect(fitz.Rect(x,cy-1,x+11,cy+11),fill=(1,1,1),color=None,overlay=True)
            p.draw_line((x+1,cy),(x+10,cy+9),color=GREEN,width=1.8,overlay=True)
            p.draw_line((x+10,cy),(x+1,cy+9),color=GREEN,width=1.8,overlay=True)
    done=41-len(PENDING)
    p,y=new_register(doc,'Control de observaciones - P-005',f'27 de octubre de 2026 | {done} levantadas con X verde | {len(PENDING)} pendientes o parciales\nLos números de esta relación corresponden a los recuadros amarillos de la primera página.')
    text(p,(36,y,559,y+39),'X verde: corrección de dibujo realizada y comprobada. Sin X: requiere definición o compatibilización. Las cruces azules de la imagen original pertenecen al revisor y se conservan.',9)
    y+=47
    for i,(heading,body) in DATA.items():
        lines=wrap(body,448,9.4)
        row_height=25+len(lines)*11.7+10
        if y+row_height>786:
            p,y=new_register(doc,'Control de observaciones - continuación','P-005 | Revisión para comprobación del usuario | Se conserva la geometría nativa del proyecto.')
        state='PARCIAL' if i==38 else 'PENDIENTE' if i in PENDING else 'CON X'
        col=ORANGE if i in PENDING else GREEN
        p.draw_rect(fitz.Rect(36,y,84,y+18),fill=col,color=col)
        text(p,(38,y+2,82,y+18),f'{i:02}  {state}',6.4,(1,1,1),1)
        text(p,(96,y-1,558,y+20),heading,11)
        text(p,(96,y+22,558,y+22+len(lines)*12+5),'\n'.join(lines),9.4)
        y+=row_height
        p.draw_line((36,y-5),(559,y-5),color=(.87,.89,.90),width=.4)

    p,y=new_register(doc,'Consultas al final de la revisión','Información por confirmar con Álvaro, Royer y el responsable del diseño.\nSe agrupan aquí por indicación del usuario; no se enviaron mensajes a terceros.')
    queries=[
        ('Metrado - observación 26','Recibir el volumen de corte validado por Álvaro, con alcance y unidad. El cuadro conserva POR DEFINIR y la nota de volúmenes netos.'),
        ('Tubería - observación 5','Confirmar pared sólida o doble, SDR/clase de presión y especificación aplicable a la tubería HDPE de 4 pulgadas.'),
        ('Longitud - observación 38','Definir si los 85.00 m se exigen en proyección horizontal o a lo largo de la pendiente. El eje en planta mide 84.642456 m; la polilínea longitudinal representada mide aproximadamente 88.541 m con H=V.'),
        ('Cajas y cotas - observación 39','Compatibilizar las cotas de conexión E/G del P-012 con el perfil P-005. Ejemplo: en el detalle E aparecen cotas de tubería 2451.05, mientras la rasante inicial del P-005 es 2474.19. La sección 12 mantiene las cotas del P-012.'),
        ('Banquetas - observación 20','Identificar las banquetas y puntos exactos a acotar y validar la superficie o cotas de diseño que les corresponden.'),
        ('Nota de Royer - observación 7','Confirmar qué nota debe consultarse y cuál es el texto o criterio aprobado que debe incorporarse.'),
        ('Remisión al P-004 - observación 8','Facilitar el comentario específico del P-004 al que remite la anotación «mismo comentario». No se modificó ese plano de referencia.'),
    ]
    for title,body in queries:
        lines=wrap(body,510,10)
        h=24+len(lines)*13+12
        if y+h>786:
            p,y=new_register(doc,'Consultas - continuación','Pendientes de definición técnica, sin X en la imagen de observaciones.')
        text(p,(36,y,559,y+23),title,12,ORANGE)
        text(p,(36,y+25,559,y+25+len(lines)*13+4),'\n'.join(lines),10)
        y+=h
    for n in range(1,len(doc)):
        text(doc[n],(36,811,559,830),f'P-005 | PARA REVISIÓN | {n+1} / {len(doc)}',8,(.4,.44,.48),2)
    doc.set_metadata({'title':'P-005 - Observaciones marcadas y consultas pendientes','subject':f'{done} observaciones con X; {len(PENDING)} pendientes o parciales','author':'Revisión de dibujo para el usuario'})
    out=OUT/'OBSERVACIONES-MARCADAS.pdf'
    doc.save(out,garbage=4,deflate=True)
    # Render deliverables and review pages for QA.
    check=fitz.open(out)
    check[0].get_pixmap(matrix=fitz.Matrix(1.2,1.2)).save(OUT/'OBSERVACIONES-CON-X.png')
    check[1].get_pixmap(matrix=fitz.Matrix(1.2,1.2)).save(ROOT/'control-preview.png')
    check[-1].get_pixmap(matrix=fitz.Matrix(1.2,1.2)).save(ROOT/'consultas-preview.png')
    register=[{'id':i,'observacion':t,'estado':'parcial' if i==38 else 'pendiente' if i in PENDING else 'levantada','resultado':b} for i,(t,b) in DATA.items()]
    (OUT/'CONTROL-OBSERVACIONES.json').write_text(json.dumps(register,ensure_ascii=False,indent=2),encoding='utf-8')
    print(json.dumps({'pdf':str(out),'paginas':len(check),'con_X':done,'sin_X':len(PENDING),'ids_pendientes':sorted(PENDING)},ensure_ascii=False))

if __name__=='__main__':
    build()
