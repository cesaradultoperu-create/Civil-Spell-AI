# Next Task

## Instalación completada, 2026-10-12

El propietario confirmó «cerrado» y se instaló 1.3.7.0. Verificados 49/49
archivos, sin duplicado por usuario y respaldo íntegro 1.3.6.0 (49/49).
Comprobante `artifacts/validation/figure3d/reference-installation-1.3.7-20261012.json`.
No reinstalar para empezar. Abrir Civil 3D y probar CREARFIGURA3D con TOP y
FRONT de la pareja central rechazada y después con la superior. En modo solo
lectura puede probar; GUARDARCOMO permite conservar el resultado en otra copia.
Comparar visualmente las referencias seleccionadas y confirmar la pendiente
longitudinal de la salida circular si FRONT es solo un corte.

## Corrección de referencias preparada, 2026-10-12

Leer `SESSION_2026-10-12_TOP_FRONT.md`. 1.3.7.0 está preparado y validado,
**sin instalar**; la instalación activa sigue en 1.3.6.0. Entrega en
`Test/Drawing3D/Reconocimiento-TOP-FRONT-20261012/`.
El nuevo snapshot reproduce las parejas central y superior rechazadas; ambas
pasan con la corrección. No confundir el snapshot de octubre 5 con el actual:
C+D antiguo pasaba, pero el eje del FRONT central actual tiene otro extremo.
Esperar el cierre de Civil 3D antes de instalar el bundle completo; conservar
la sesión y los dibujos del propietario. Después probar CREARFIGURA3D con
ambas parejas y revisar las referencias en la ventana. La salida perpendicular
usa círculo FRONT y recorrido TOP; confirmar cotas/pendiente si FRONT es un
corte y no una proyección. No prometer reconstrucción universal sin datos
suficientes; el modo general por componentes conserva perfiles arbitrarios.

## Resaltado de componentes instalado, 2026-10-06

El propietario ya confirmó que el resaltado funciona perfectamente, con captura
del fondo seleccionado en verde. No repetir esa prueba como pendiente.
Ahora aclarar los requisitos de TOP/FRONT y el alcance para otros dibujos:
el modo general trabaja por componente y dos vistas pueden ser insuficientes.

Leer `SESSION_2026-10-06_RESALTADO.md`. 1.3.6.0 está instalado y verificado
49/49 tras «civil3d CERRADO», con respaldo íntegro de 1.3.5.0. La entrega y el
comprobante están en `Test/Drawing3D/Resaltado-Componentes-20261006/`.
No reinstalar para empezar. Abrir Civil 3D y probar EDITARFIGURA3D:
entrar en «Mampostería: laterales / Cota inferior»
debe iluminar solo los laterales; seleccionar otro campo cambia la pieza,
«Quitar resaltado» recupera los colores. Cambiar una cota conserva el resaltado.
También se puede pulsar el nombre de una pieza sin campos editables.

Las capturas nuevas ya muestran reconstrucción de C+D y apertura del conjunto
en EDITARFIGURA3D. El material de la pieza interior superior sigue pendiente;
no inventar barras o perforaciones. Continúa el objetivo general de crear
otros modelos desde su TOP/FRONT, sin limitarlo al ejemplo A+B.

## Registro anterior a la solicitud de resaltado

## Ampliación para C+D preparada

Actualización posterior completada: el propietario reprodujo el error de
GRAVEL con 1.3.4.0 y después confirmó Civil 3D cerrado. Se instaló 1.3.5.0,
49/49 archivos verificados, con respaldo de 1.3.4.0. No reinstalar para empezar:
abrir Civil 3D y repetir CREARFIGURA3D seleccionando C → Referencias → D.
Falta la comprobación interactiva del resultado y aclarar el elemento interior.

Leer `SESSION_2026-10-06_C_D.md`. El objetivo sigue siendo crear modelos nuevos
desde las referencias seleccionadas; A+B no cierra ese objetivo. C es TOP y
D es FRONT. 1.3.5.0 ya reconoce C+D en prueba nativa y conserva A+B. La copia,
las vistas previas y el paquete verificado están en
`Test/Drawing3D/Reconstruccion-A-B-C-D-20261006/`. Instalación activa 1.3.4.0;
actualizar con Civil 3D cerrado usando el nuevo paquete antes de la prueba
interactiva de C+D. El propietario recibió el flujo CREARFIGURA3D → seleccionar
TOP → Referencias → seleccionar FRONT → Crear 3D → punto de inserción.
Falta aclarar qué material/pieza representa el tramo superior del elemento
interior de D; hasta entonces se conserva su envolvente geométrica, sin inventar
barras o perforaciones. Continuar con otros ejemplos por componentes y pedir
solo relaciones que las vistas no permitan determinar.

## Sesión retomada, 2026-10-06

Actualización ya completada después del aviso «civil3D cerrado»: bundle 1.3.4.0,
49/49 archivos verificados y respaldo de 1.3.3.0 conservado. No volver a instalar
para iniciar esta revisión. Abrir `Referencia-TOP-FRONT-3D.dwg` de la carpeta
del 06/10 y probar `EDITARFIGURA3D`, después `CREARFIGURA3D` con TOP/FRONT.
Comparar el canal contra detalle C/sección 7 del layout A y confirmar interacción.

Consultar primero `SESSION_2026-10-06_TOP_FRONT.md`. La comparación y el paquete
Release con cama de arena están preparados en
`Test/Drawing3D/Reconstruccion-TOP-FRONT-20261006/`. Release multiversión y prueba
nativa pasan; la instalación quedó verificada posteriormente. Revisar con el propietario el canal frente al
detalle C/sección 7 del layout A, resolver la pendiente mínima rotulada frente
al perfil horizontal y confirmar las medidas constructivas de tubería cuando
estén disponibles. Probar selección, giro/zoom, cancelación, creación y edición
interactivas con el candidato instalado.
La comparación usa las referencias guardadas el 05/10; si el propietario las
ha cambiado, capturar una nueva copia sin guardar ni alterar su DWG abierto.

## Pausa de jornada, 2026-10-05

El propietario pidió continuar mañana y guardar la información. Leer primero
[SESSION_2026-10-05_TOP_FRONT.md](SESSION_2026-10-05_TOP_FRONT.md).
El último cambio de cama de arena pasó Debug/prueba nativa, pero todavía debe
incorporarse al Release y al paquete. La carpeta de revisión conserva DWG/PNG;
todavía no incluye el instalador final. No instalar el ZIP anterior como si
contuviera ese último cambio.

## Reconstrucción fiel de TOP y FRONT

La implementación 1.3.4.0 y el paquete de revisión
`Test/Drawing3D/Reconstruccion-TOP-FRONT-20261005` ya incluyen reconstrucción
del detalle de drenaje y perfiles arbitrarios por componente. Revisar la
nueva guía `docs/testing/REFERENCE_ASSEMBLY_3D_RUNBOOK.md`. El siguiente paso
interactivo es comparar el conjunto con el layout A usando la DLL nueva,
tras guardar y cerrar Civil 3D para actualizarla. Confirmar especialmente
la interpretación del canal y la longitud/diámetro/espesor del tubo.
Lo siguiente conserva el planteamiento anterior de la tarea.

El propietario está preparando una extrusión aproximada del diseño esperado
en el DWG de pruebas. Revisar esa referencia cuando esté lista y identificar
los componentes que el modelo de caja omite. El objetivo es relacionar los
contornos y posiciones X/Y de TOP con perfiles y niveles X/Z de FRONT,
incluyendo los componentes del diseño que queden definidos en ambas vistas.
No considerar resuelta la solicitud por corregir el renderizado del modelo
de caja. Validar las proyecciones TOP y FRONT del resultado frente a las
referencias y comprobar los huecos, espesores y posiciones de cada componente.
Si dos vistas no determinan una dimensión o una correspondencia, consultar
ese dato concreto en vez de introducir una forma o medida supuesta.

## Creación 3D desde TOP y FRONT — retomada el 2026-10-05

Continuar el flujo de `CREARFIGURA3D`, `EDITARFIGURA3D` y el botón
**Crear 3D con FRONT…** de `EDITARSECCION`. El viernes quedó instalado el
modelo con pasos laterales de ancho inicial editable 0.10; faltaba el
recorrido interactivo de selección TOP/FRONT, giro/zoom y confirmación.
La captura del propietario identificó falsas ranuras en la vista previa.
La malla corregida conserva la coronación, expone las caras exteriores
y dibuja solo las aristas reales. Debug/Release 2024/2025/2026 pasa 151/151
y el ensayo nativo 2024 verifica ambas parejas TOP/FRONT, volumen de malla
contra sólido, edición, una U y reapertura. Paquete final preparado:
`Test/Drawing3D/Correccion-3D-Contornos-20261005`. Aplicarlo tras guardar
y cerrar Civil 3D, y comprobar giro/zoom y los huecos en la ventana real.
Confirmar si se deben reconstruir también los conductos externos;
esa ampliación sigue pendiente de la respuesta del propietario.
Guía: `docs/testing/FIGURE_3D_TOP_FRONT_RUNBOOK.md`.

## Candidato 1.3.0.0

Ejecutar primero `MLP-01`–`MLP-07` de
`docs/testing/MLEADER_POLAR_TRACKING_RUNBOOK.md` en Civil 3D 2024 con un DWG
desechable. Confirmar especialmente los dos lados del texto, cancelación,
restauración exacta de `AUTOSNAP`/`ORTHOMODE` y `U`. Después, generar y
verificar el bundle multiversión, instalarlo en Civil 3D 2025 y comprobar
`AISPELLSETTINGS`, `AISPELL` y `AISPELLALL`; repetir en Civil 3D 2026 cuando
haya un equipo disponible. La compatibilidad binaria y el cálculo polar están
compilados y automatizados; estas pruebas reales son el cierre pendiente.

## Candidato 1.2.13.0

El 2026-09-14 se repitieron las validaciones Debug y Release con 138/138 pruebas;
Release produjo la misma huella de DLL. Una comprobación interactiva offline adicional sobre una copia
desechable revisó un MText, presentó 16 correcciones locales aprobadas, las
aplicó y restauró el original mediante una sola `U` inmediata. No se observó
ningún defecto funcional nuevo.

Los dos MText completos de la prueba visual son ahora regresiones: 20 cambios
españoles y 7 ingleses, sin alterar siglas, números ni unidades. El núcleo pasa
138/138 pruebas Debug y Release, sin advertencias, incluyendo una anotación
mixta con ambos diccionarios reales, y elimina los falsos
positivos observados en 1.2.12.0. La DLL Release tiene SHA-256
`61FF7C02C2977BE21BFCA54E4D9F17979D81987016371C674AB052900D0C38B5`.
Dos builds produjeron el mismo ZIP verificado con SHA-256
`EE07BCD39648CD8589E642D8ACE05A054B0550BA4C9558A540C75F0BF874A209`.
La instalación coincide 17/17 con el bundle, las regresiones de AutoCAD pasan
y 1.2.10.0 queda como único rollback estable. `AISPELLALL` mostró 20 cambios
españoles y 7 ingleses con cero fallos; los siete ingleses fueron aprobados.
La inspección visual de las últimas entradas españolas del escenario exacto de
20+7 permanece como comprobación no bloqueante. El candidato puede cerrarse en
fuente y debe mantenerse sin publicar hasta autorización expresa.

## Candidato 1.2.12.0

Incluye los diccionarios offline `es_PE` y `en_US`, detección previa de idioma
y enrutamiento `es`/`en-US`/`auto` para LanguageTool. Debug y Release pasan
135/135 pruebas y dos builds produjeron el mismo ZIP con SHA-256
`0E48FC48880D36A35F8EBDB023B4DE5215EFDA1CAA2D0B809CB1B0884337F027`.
Siguiente paso: con autorización y Civil 3D cerrado, instalar el candidato y
validar español, inglés y texto mixto sobre un DWG desechable.

## Candidato 1.2.11.0

Evita registrar cambios cuando una regla produce exactamente el texto original,
por lo que `FINALIDAD` y `CANTIDAD` ya no deben crear filas bloqueadas sin
diferencias. Pasa 131/131 pruebas y el ZIP reproducible tiene SHA-256
`0B86587775F212B431D3D867C4471C11929A31B8E100FBEF9725BB2BEF65CC1E`.
Siguiente paso: instalar con Civil 3D cerrado y repetir `AISPELLALL` en `C1.dwg`.

## Candidato 1.2.10.0

Añade `PROTEJE → PROTEGE` y `DEVE → DEBE` para completar los doce errores
intencionales del texto controlado. Debug y Release pasan 131/131 pruebas. El
ZIP reproducible tiene SHA-256
`3B8BC499C11D6380199107FA983CB821ACFBDE9F9BADE7DAF54016581E89A938`. El
siguiente paso es instalar con Civil 3D cerrado y repetir la prueba visual
esperando 12/12 correcciones sin alterar siglas. Ese paso quedó completado:
13/13 archivos coinciden, la revisión mostró 12/12 cambios y cero fallos, y una
sola `U` restauró el lote aplicado. Mantener el corte sin publicar hasta recibir
autorización expresa.

## Candidato 1.2.9.0

El diccionario local ya trata por igual las erratas seguras en minúsculas,
capitalización inicial y mayúsculas completas. La primera prueba visual encontró
`LLDPE → LLAPE` y se canceló; la fuente corregida conserva esa sigla y el
validador bloquea su alteración. Debug y Release pasan 131/131 pruebas. El ZIP
reproducible tiene SHA-256
`B06F57DCD21CE640F38EE0E57F23E8B964695B621993EC91AD7CD9CC3C9A4FBA`.
Siguiente paso: instalar el nuevo candidato con Civil 3D cerrado y repetir en
una copia del dibujo las palabras que no detectó 1.2.8.0.

## Fuente posterior a 1.2.7.0

Ajustes dispone ahora de una prueba cancelable para LanguageTool local. Usa una
frase fija, no contenido del dibujo, no guarda la respuesta y presenta códigos
de soporte seguros. Debug y Release pasan 131/131 pruebas y la integración real
atraviesa el nuevo servicio. El candidato 1.2.8.0 quedó instalado y validado:
13/13 archivos coinciden, 1.2.7.0 permanece como rollback y la prueba visual
confirmó botón, estado disponible y mensaje de frase fija sin persistencia. El
siguiente paso es regenerar el artefacto con esta evidencia y mantenerlo sin
publicar hasta autorización expresa. El artefacto ya quedó reproducido: DLL
SHA-256 `95F20FAEDECCC0D4697367EDAC6203AD2160A7DC4702EB52BF21477BC4CEFB2D` y ZIP
SHA-256 `4F3165005B2A5DD9A1FE45E92B617F23AD42491E9BC285552520F821BA68CCD1`.

## Fuente posterior a 1.2.6.0

LanguageTool tiene ahora un origen propio y las dos interfaces lo muestran como
`LanguageTool local`, sin confundirlo con las reglas integradas. Los valores
anteriores de `ProposalSource` conservan sus números; la nueva fuente usa la
validación estricta del glosario. Debug, Release y la prueba real pasan. El
candidato 1.2.7.0 está construido e instalado: DLL SHA-256
`9F9669059BA14FC76C07BBD83B04E88DF930118EC7D567D74A79CE9D8334C8F4` y ZIP
reproducible `D6E763595BCFF52CA37C3ED13AF11576FC1EB192F125B701161023750C434823`.
La instalación y la comprobación visual pasaron: 13/13 archivos coinciden,
1.2.6.0 queda como rollback y el lote mostró por separado `Reglas locales` y
`LanguageTool local`, con cuatro filas con propuestas y cero fallos. La ventana
se canceló sin escribir el fixture. El artefacto documentado es reproducible y
debe mantenerse sin publicar hasta nueva autorización.

## Candidato 1.2.6.0

El 2026-09-10 se construyó e instaló el proveedor de LanguageTool local con
`AISPELL`, `AISPELLALL` y `AISPELLSETTINGS`. La opción está desactivada por
defecto, usa el endpoint fijo `http://127.0.0.1:8081`, reutiliza textos
idénticos por lote y reduce la concurrencia cuando está activa. Debug y Release
pasan 128/128 pruebas sin advertencias. La DLL Release tiene SHA-256
`D1891EFD1BD2781BEB5235CC56F4B5E1E1F950675E229418F263137E26BF335D` y el ZIP
reproducible SHA-256
`F410F4F9BB0CBCDC04D30FA97250EFABE239E0777BBBD92A91A86CBB839D4202`.

Temurin 17.0.20.1 y LanguageTool 6.6 quedaron instalados fuera del complemento.
El ZIP oficial tuvo SHA-256
`53600506B399BB5FFE1E4C8DEC794FD378212F14AAF38CCEF9B6F89314D11631`.
El servidor escucha solo en `127.0.0.1:8081` y la prueba C# real de fábrica,
transporte, parser y validación técnica pasó. Los 13 archivos instalados
coinciden con el bundle y 1.2.5.0 quedó como rollback. La revisión visual
confirmó Ajustes, cuatro solicitudes reales, 4/4 filas con propuestas y cero
fallos; se canceló antes de aplicar para no interrumpir el trabajo del usuario.
La siguiente tarea es revisar la presentación de alternativas cuando las reglas
integradas y LanguageTool producen correcciones diferentes, y decidir si la UI
debe distinguir el nombre del proveedor aun cuando ambos usan origen local.
No publicar 1.2.6.0 sin autorización expresa.

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
rollback. La prueba interactiva final también pasó en Civil 3D 2024 sobre un
dibujo desechable: autocarga sin `NETLOAD`, propuesta de `Reglas locales` con
dos cambios y cero fallos, aplicación y restauración exacta mediante una sola
`U` inmediata. Antes de publicar el flujo de primera configuración queda probar
un perfil de Windows realmente nuevo y obtener autorización expresa.

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
