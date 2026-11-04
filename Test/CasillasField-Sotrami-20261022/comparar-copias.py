import pathlib, xml.etree.ElementTree as ET, difflib, json, xml.dom.minidom as DOM
root=pathlib.Path(r'Test/CasillasField-Sotrami-20261022')
trees={k:ET.parse(root/(k+'-lectura.xml')) for k in ['inicial','actual','piloto']}

a=DOM.parseString(ET.tostring(trees['inicial'].getroot(),encoding='utf-8')).toprettyxml().splitlines()
b=DOM.parseString(ET.tostring(trees['actual'].getroot(),encoding='utf-8')).toprettyxml().splitlines()
diff=list(difflib.unified_diff(a,b,fromfile='inicial',tofile='actual',n=2))
(root/'diferencias-DST.txt').write_text('\n'.join(diff),encoding='utf-8')
print('\n'.join(diff[:160]))
def props(tree):
 return {e.attrib['ID']:(e.attrib.get('propname'),next((p.text for p in e if p.attrib.get('propname')=='Value'),None)) for e in tree.iter('AcSmCustomPropertyValue')}
p,q=props(trees['inicial']),props(trees['actual'])
changes=[{'ID':k,'antes':p.get(k),'ahora':q.get(k)} for k in p.keys()|q.keys() if p.get(k)!=q.get(k)]
print('CUSTOM_PROPERTY_CHANGES',json.dumps(changes,ensure_ascii=False))
(root/'verificacion-propiedades.json').write_text(json.dumps({'custom_property_changes':changes,'diff_lines':len(diff)},ensure_ascii=False,indent=2),encoding='utf-8')