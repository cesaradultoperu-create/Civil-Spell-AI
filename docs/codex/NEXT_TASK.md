# Next Task

## Operación del piloto 1.1.5.0

La versión 1.1.5.0 está compilada y empaquetada para Civil 3D 2024 x64. Las
validaciones Debug y Release pasan con 105/105 pruebas, y `UNDO-04` confirmó en
AutoCAD Core Console que una sola orden `U` restaura las dos entidades escritas
por la misma frontera atómica que usa `AISPELLALL`.

REG-2026-005 documenta que no debe añadirse un grupo manual `UNDO Begin/End`:
AutoCAD ya agrupa cada comando .NET y el grupo anidado deja la reversión detenida
ante el inicio del grupo. La corrección visible de 1.1.5.0 indica «Use U una vez
para revertir todo el lote».

La versión 1.1.5.0 ya está instalada bajo
`%PROGRAMFILES%\Autodesk\ApplicationPlugins`. Manifiesto, DLL y los cuatro
archivos del bundle coinciden por hash con el candidato; 1.1.4.0 quedó en un
respaldo recuperable.

La validación interactiva pasó el 2026-09-03: `AISPELLALL` cargó por invocación,
aplicó tres correcciones en el fixture limpio, mostró «Use U una vez» y una
sola `U` restauró el lote completo según confirmación del propietario.

El ZIP final conserva reproducibilidad y pasó el verificador independiente
después de incorporar esta evidencia.

El fixture se cerró sin guardar y el DWG base conservó exactamente su hash
registrado.

El prerelease público quedó publicado en
https://github.com/cesaradultoperu-create/Civil-Spell-AI/releases/tag/1.1.5.0.
La etiqueta apunta al commit `30856f7e825ff1221558ac2db6727ae17f4061ed`
y contiene el ZIP final y su checksum. Una descarga independiente del ZIP
reprodujo exactamente la huella registrada.

Siguiente trabajo:

1. Entregar el piloto solo en equipos Civil 3D 2024 x64 controlados, siguiendo
   `docs/PILOT_INSTALLATION.md` y verificando el hash antes de instalar.
2. Registrar incidencias con la plantilla anonimizada; no ampliar entidades
   hasta reunir evidencia del piloto.

Artefacto: `artifacts/distribution/CivilSpellAI-1.1.5.0.zip`.

SHA-256 ZIP: `FE394373A9B5801D9B42D7EA1C389DAAC7EE74EEA5CE3A6B10CBC396DFE4D68A`.

Checksum distribuible:
`artifacts/distribution/CivilSpellAI-1.1.5.0.zip.sha256`.

SHA-256 DLL Debug:
`196A53E69DCBA47536C7D4AE6E110EF27F86FCBC190E270380351AB3833A8031`.

SHA-256 DLL Release:
`11A33B5825AC38E9B35F9E4A1951A2D02F5E190B624A0B27A7204A45FF138331`.

Fixture desechable:
`artifacts/testing/CivilSpellAI-Pilot-Fixture-1.1.5.dwg`, SHA-256
`2F684893689D88237E983D7A77E34DF37EA4C3046AC43D2DC3CA4CF63AF80470`.
