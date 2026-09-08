# Next Task

## Cierre 1.2.0.0

CivilSpellAI 1.2.0.0 amplía `AISPELLALL` a `MLeader` textual, celdas simples
editables de `Table`, textos fijos de bloques locales y atributos por inserción.
Los textos fijos se corrigen una vez en la definición y afectan todas sus
referencias. Xrefs, etiquetas nativas de Civil 3D y celdas complejas permanecen
fuera del alcance.

La validación Debug pasa con 115/115 pruebas. `EXTENDED-01` pasó dentro de
AutoCAD Core Console con cinco destinos, incluidas dos celdas de la misma tabla,
y una sola orden `U` restauró todo el lote. La prueba usa una copia temporal y
un perfil aislado; no modifica dibujos del usuario.

El diccionario abierto Hunspell `es_PE` de Perú está integrado bajo MPL 1.1.
`DICT-01` cargó el motor y los datos dentro de AutoCAD Core Console y corrigió
una tilde inequívoca sin red. RAE/ASALE se conserva solo como referencia
normativa y no se redistribuye su contenido.

El paquete 1.2.0.0 ya pasó Release y el verificador independiente. La
DLL candidata tiene SHA-256
`3D5972B0951DD17EC3CF6BAD1F6E366C49C4406913D244C3A33EDD279F786D5B` y el ZIP
reproducible `2779BD4E787F1D8DE3AE74E783CE77B89CA2F9AD9C9B325EE2591133970A3A53`.
Este corte evita el falso bloqueo de `HA SIDO` sin dejar de proteger unidades
reales como `25 ha`, además de proteger los controles completos de MText. La
instalación administrada contiene ya el candidato: sus 13 archivos coinciden
por hash y la DLL instalada es
`3D5972B0951DD17EC3CF6BAD1F6E366C49C4406913D244C3A33EDD279F786D5B`. El corte
anterior, cuya DLL era
`E4C9F9A7B9CFDF0708DC36A1E24BAD40DCC47DAF8751B71A6D58497B8FE86413`, tiene
respaldo en `backup-20260908-091534-094`.

El propietario autorizó expresamente el 2026-09-08 confirmar, etiquetar y
publicar 1.2.0.0 con el ZIP y su checksum verificados.

La validación visual del 2026-09-08 está completa: el corte instalado detectó
dos `MLeader` y un `DBText` con `TUVERÍA`/`TUVERÍAS`, propuso las formas con B,
conservó los tokens técnicos y una sola `U` inmediata restauró los tres objetos.

La versión estable anterior 1.1.5.0 sigue disponible y dispone de un respaldo
recuperable.

Runbook: `docs/testing/PILOT_1_2_EXTENDED_TEXT_RUNBOOK.md`.
