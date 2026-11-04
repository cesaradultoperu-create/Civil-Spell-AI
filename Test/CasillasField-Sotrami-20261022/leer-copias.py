import pathlib, xml.etree.ElementTree as ET, hashlib
root=pathlib.Path(r'Test/CasillasField-Sotrami-20261022')
backups=pathlib.Path(r'C:/Users/Cesar Ugaz/AppData/Local/CasillasField/Backups/2026-10-22')
initial=next(p for p in backups.glob('*.dst') if hashlib.sha256(p.read_bytes()).hexdigest().upper()=='C7DCA2A9E4201F5D03F987374D7BE38E4E710D4B592B73C505FDDFEFB734F48C')
for label,path in [('inicial',initial),('actual',root/'SOTRAMI-lectura-actual.dst'),('piloto',root/'SOTRAMI-piloto.dst')]:
    decoded=bytes(((v^0x9D)-0x11)&255 for v in path.read_bytes())
    tree=ET.fromstring(decoded)
    (root/(label+'-lectura.xml')).write_bytes(decoded)
    print(label,tree.tag,len(list(tree.iter())))
    for el in tree.iter():
        if '03_R3_By' in str(el.attrib): print(ET.tostring(el,encoding='unicode')[:2500])