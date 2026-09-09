# Next Task

## Candidato 1.2.5.0

El 2026-09-09 quedó construido e instalado el candidato 1.2.5.0. La futura
integración con LanguageTool local no admite proxy, cookies ni redirecciones
HTTP y rechaza correcciones superiores a 20 000 caracteres antes del diff y la
interfaz. Dos builds Release pasaron 127/127 pruebas sin advertencias y
generaron el mismo ZIP. La DLL tiene SHA-256
`34CD11222D57A02B025CCCC39BAB711BC1ADBA4809186E4051527505761B4DBE` y el ZIP
reproducible
`09DCC19BC1BA1B10B351D5C452FAC6AB8DCC5F04CC615279A5EA1498609068D3`.
Los 13 archivos instalados coinciden con el bundle; `UNDO-04`, `DICT-01` y
`EXTENDED-01` pasaron en AutoCAD Core Console. Solo se conserva 1.2.4.0 como
rollback. El siguiente paso es la prueba interactiva breve de autoload y una
corrección local antes de decidir la publicación.

El candidato 1.2.4.0 quedó construido e instalado el 2026-09-09. Dos builds
Release consecutivos pasaron 125/125 pruebas sin advertencias y generaron el
mismo ZIP. La DLL tiene SHA-256
`7CEC88AE30FC6145B580A43E8C71CE4AF1B5E10F68322DBCD14D8A04376D06C2` y el
ZIP reproducible
`AA2B4B7A399C537867F5D8FDB59D165295521EC5703FC6B55F179CFB2A96C46F`.
El verificador independiente aceptó el artefacto y rechazó correctamente sus
cinco variantes inválidas.

La instalación administrada 1.2.4.0 contenía 13/13 archivos idénticos al bundle.
`UNDO-04`, `DICT-01` y `EXTENDED-01` pasaron en AutoCAD Core Console con copias
temporales aisladas. La prueba
interactiva sobre un dibujo desechable también pasó el 2026-09-09: autoload,
corrección gramatical local, revisión remota consentida con origen `IA`, cero
fallos de proveedor, aplicación y restauración exacta con una sola `U`
inmediata. No publicar sin autorización expresa.

Se implementó una guía de primera ejecución de `AISPELLALL` por usuario. Permite
abrir Ajustes para configurar OpenAI o continuar localmente sin conexión ni
coste; una configuración válida evita repetir la guía. Ajustes admite pegar una
clave propia en un campo enmascarado, guardarla como `OPENAI_API_KEY` del usuario
de Windows o retirarla. La clave nunca se muestra ni entra en el JSON, el DWG o
el instalador, y la prueba de conexión sigue siendo voluntaria.

Debug y Release pasan 117/117 pruebas sin advertencias. El paquete 1.2.1.0
también pasó el verificador independiente y sus pruebas negativas. La DLL
Release tiene SHA-256
`F000FAE02313F2BBD6CFFDB1AECDCD4A970657A703B08F9E507587489AAC6689` y el ZIP
reproducible `F1224C385EBA6BA6EE9A6B94F431F706A5371307860A1F75F488BDC73C570603`.
El candidato quedó instalado y sus 13 archivos coinciden con el bundle. La
prueba visual confirmó credencial enmascarada, conexión real, una propuesta de
origen `IA`, aplicación y restauración exacta con una sola `U` inmediata sobre
un MText de prueba. Pendiente únicamente comprobar la guía de primera
configuración en otra computadora o perfil de Windows sin configuración previa,
sin retirar los datos reales de este usuario, y obtener autorización expresa
antes de publicar 1.2.1.0.

La prueba ampliada ya está aprobada: 593 textos revisados, 52 filas con
propuestas, cero fallos de proveedor y 21 correcciones seguras aplicadas con
resultado visual correcto. La deduplicación exacta ya está implementada. Agrupar
varios textos distintos en una sola solicitud remota queda como optimización
posterior y exigirá conservar la asociación por objeto, la validación individual
y el `UNDO` único. La prioridad pasa a ser ampliar la corrección gramatical
local sin consumo de API mediante componentes con licencia redistribuible.

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
