# Next Task

## Publicación y operación del piloto 1.1.4.0

La versión 1.1.4.0 está instalada de forma administrada bajo
`%PROGRAMFILES%\Autodesk\ApplicationPlugins`. Manifiesto, DLL, glosario y ayuda
coinciden por hash con el artefacto; la copia `CurrentUser` se retiró a un
respaldo y los datos locales permanecieron idénticos.

La regresión interactiva cerró REG-2026-002 y REG-2026-003. La migración de
alcance exigió un único `APPAUTOLOADER` → `Reload`; después,
`AISPELLSETTINGS` abrió sin `NETLOAD` ni aviso de firma. El procedimiento quedó
documentado en REG-2026-004. La actualización real a 1.1.4.0, rollback a
1.1.3.0, desinstalación y reinstalación final pasaron el 2026-09-02. Los hashes
y datos locales coincidieron antes y después.

La regresión final del 2026-09-03 confirmó autoload sin `NETLOAD` ni aviso de
seguridad y completó Configuración, revisión individual y lote con Tab/Shift+Tab
al 125 %. El foco, desplazamiento, sincronización de filas y acciones quedaron
accesibles sin recortes ni superposición. Los Hitos 4–6 están cerrados.

Siguiente trabajo:

1. Publicar el ZIP final reproducible y conservar su SHA-256 junto a la DLL.
2. Entregar el piloto solo en equipos Civil 3D 2024 x64 controlados, siguiendo
   `docs/PILOT_INSTALLATION.md` y verificando el hash antes de instalar.
3. Registrar incidencias con la plantilla anonimizada; no ampliar entidades
   hasta reunir evidencia del piloto.

La preparación local de la entrega está cerrada: el empaquetador verifica el
ZIP después de construirlo y su prueba independiente confirma que acepta el
artefacto válido y rechaza nombres, huellas o contenidos incoherentes.

Artefacto: `artifacts/distribution/CivilSpellAI-1.1.4.0.zip`.

SHA-256 ZIP: `C5335CF1ABC95FB1F3368CE2C343AEF859B963923B1543D98CBDDD892229B570`.

Checksum distribuible: `artifacts/distribution/CivilSpellAI-1.1.4.0.zip.sha256`.

SHA-256 DLL Release:
`73FEDCF3911367B5A19D3B62FF68A1626EE48E459CC018170690106BA53E687A`.

Fixture desechable:
`artifacts/testing/CivilSpellAI-Pilot-Fixture-1.1.4.dwg`, SHA-256
`7259CD0AED57BCB1F16970C9D2E2F4E44AD14BB4B3F0D45FA7409F621C025EE0`.
