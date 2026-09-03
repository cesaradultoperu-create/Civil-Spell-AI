# Next Task

## Validación interactiva del candidato 1.1.5.0

El candidato 1.1.5.0 está compilado y empaquetado para Civil 3D 2024 x64. Las
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
respaldo recuperable. Su prerelease público permanece sin cambios.

La validación interactiva pasó el 2026-09-03: `AISPELLALL` cargó por invocación,
aplicó tres correcciones en el fixture limpio, mostró «Use U una vez» y una
sola `U` restauró el lote completo según confirmación del propietario.

El ZIP final conserva reproducibilidad y pasó el verificador independiente
después de incorporar esta evidencia.

El fixture se cerró sin guardar y el DWG base conservó exactamente su hash
registrado.

Siguiente trabajo:

1. Solo con autorización expresa, preparar commit, etiqueta y prerelease 1.1.5.0.

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
