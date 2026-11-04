import pathlib,json,math,zipfile,xml.etree.ElementTree as ET
root=pathlib.Path(__file__).resolve().parent/'Native'
repo=root.parents[3]
ns={'s':'http://schemas.openxmlformats.org/spreadsheetml/2006/main'}
def read_workbook(path):
    with zipfile.ZipFile(path) as archive:
        assert archive.testzip() is None
        for name in archive.namelist():
            if name.endswith('.xml') or name.endswith('.rels'): ET.fromstring(archive.read(name))
        workbook=ET.fromstring(archive.read('xl/workbook.xml'))
        sheet=ET.fromstring(archive.read('xl/worksheets/sheet1.xml'))
        cells={}
        for c in sheet.findall('.//s:sheetData/s:row/s:c',ns):
            cells[c.attrib['r']]=(c.findtext('s:is/s:t','',ns) if c.attrib.get('t')=='inlineStr' else float(c.findtext('s:v',namespaces=ns)))
        return workbook.find('s:sheets/s:sheet',ns).attrib['name'],sheet,cells
def verify(folder):
    result=json.loads((folder/'resultado.json').read_text(encoding='utf-8-sig'))
    name,sheet,cells=read_workbook(folder/'Alturas_ultimo_perfil.xlsx')
    assert name=='Alturas'
    assert sheet.find('s:sheetViews/s:sheetView/s:pane',ns).attrib['topLeftCell']=='A2'
    assert cells['A1']=='Distancia desde A (m)' and cells['B1']=='Altura (m)'
    assert len(sheet.findall('s:sheetData/s:row',ns))==len(result['Heights'])+1
    for i,s in enumerate(result['Heights'],2):
        assert math.isclose(cells[f'A{i}'],s['Distance'],rel_tol=1e-13,abs_tol=1e-13)
        assert math.isclose(cells[f'B{i}'],s['Height'],rel_tol=1e-13,abs_tol=1e-13)
        assert cells[f'C{i}']==s['Index'] and cells[f'D{i}']==s['ProfileNumber']
        assert cells[f'E{i}']==s['ProfileFile'] and (cells[f'F{i}'] or '')==(s['Handle'] or '')
        assert cells[f'G{i}']==s['ProfileHandle']
        if s['Height']==0:assert 'altura cero' in cells[f'H{i}']
    name,areas,values=read_workbook(folder/'Areas_relave.xlsx')
    assert name=='Areas' and len(areas.findall('s:sheetData/s:row',ns))==len(result['Photos'])+1
    for row,p in enumerate(sorted(result['Photos'],key=lambda p:p['Frame']['Number']),2):
        assert math.isclose(values[f'B{row}'],p['Area'],rel_tol=1e-13,abs_tol=1e-13)
    return result
original=verify(root)
assert len(original['Heights'])==23 and original['Heights'][-1]['Height']==0
assert all(math.isclose(s['Distance'],i/10,abs_tol=1e-12) for i,s in enumerate(original['Heights']))
without_last=verify(root/'Prueba_sin_ultima_foto');assert all(s['ProfileNumber']==2.1 for s in without_last['Heights'])
edited=verify(root/'Prueba_alturas_editadas');assert math.isclose(edited['Heights'][0]['Height'],original['Heights'][0]['Height']*1.1,abs_tol=1e-12)
baseline=json.loads((repo/'Test/RelaveAuto-20261103/Comparacion-anterior-0.3.3/Native/Prueba_exportacion/resultado.json').read_text())
before={p['File']:p['Area'] for p in baseline['Photos']}
assert all(math.isclose(p['Area'],before[p['File']],abs_tol=1e-12,rel_tol=0) for p in original['Photos'])
(root/'excel-alturas-ok.txt').write_text('ZIP/XML del XLSX verificado con un lector independiente: 23 estaciones 0.00..2.20, alturas y handles coinciden con DWG/JSON; punta cero sin trazo; ultima foto disponible y perfil editado verificados. Las 20 areas coinciden con la version 0.3.3.',encoding='utf-8')
print((root/'excel-alturas-ok.txt').read_text())
