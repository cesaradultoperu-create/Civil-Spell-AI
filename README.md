# CivilSpellAI

Complemento para Civil 3D 2024 que revisa texto técnico en español e inglés con
reglas locales conservadoras, los diccionarios abiertos `es_PE` y `en_US` de Hunspell y,
opcionalmente, LanguageTool ejecutado en esta computadora u OpenAI. Las reglas
integradas y Hunspell funcionan sin conexión. `AISPELL` analiza un
`DBText`, `MText`, `MLeader` textual o atributo seleccionable, muestra
el original, la propuesta, el diff y la validación técnica en una ventana modal.
También permite editar manualmente el resultado, pero solo escribe después de
que el validador técnico lo aprueba y el usuario pulsa **Aplicar**.

`AISPELLSETTINGS` administra glosario personal, LanguageTool local, proveedor
simulado, consentimiento para OpenAI y memoria local explícita. LanguageTool se
conecta únicamente a `http://127.0.0.1:8081`, no consume API y permanece
desactivado hasta que el usuario lo habilita; su servidor y Java no forman parte
del complemento. La primera ejecución de
`AISPELLALL` permite abrir esta configuración o continuar localmente. Al
habilitar OpenAI solo se envía el contenido de los textos; la clave propia puede
guardarse en la variable de entorno de usuario `OPENAI_API_KEY` desde un campo
enmascarado y nunca se copia al proyecto ni al instalador.

`AISPELLALL` revisa `DBText`, `MText`, `MLeader` textual, celdas simples
editables de `Table` y textos/atributos de bloques locales en modelo y
presentaciones. Muestra progreso y permite cancelar, buscar, filtrar y elegir
la alternativa de cada fila antes de aplicar todo como un solo lote atómico.

- Compilación: `dotnet build CivilSpellAI.slnx --configuration Debug --no-restore`
- Pruebas: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Run-SpellCoreTests.ps1`
- LanguageTool local: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Manage-LanguageToolLocal.ps1 -Action Start`
- Prueba real de LanguageTool: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-LanguageToolLocalIntegration.ps1`
- Validación del corte: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Validate-Mvp.ps1`
- Paquete piloto: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Build-PilotBundle.ps1`
- Verificación de entrega: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-ReleaseArtifact.ps1 -ArchivePath artifacts/distribution/CivilSpellAI-1.2.13.0.zip`
- Prueba del verificador: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-ReleaseArtifactVerifier.ps1`
- Fixture piloto: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/New-PilotFixture.ps1 -Force`
- Regresión real de UNDO: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-BatchUndoIntegration.ps1`
- Regresión real de textos extendidos: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-ExtendedTextIntegration.ps1`
- Estado del MVP y matriz de regresión: [docs/product/10_MVP_STATUS_AND_TEST_PLAN.md](docs/product/10_MVP_STATUS_AND_TEST_PLAN.md)
- Segundo alcance: [docs/product/11_SCOPE_2_ROADMAP.md](docs/product/11_SCOPE_2_ROADMAP.md)
- Desarrollo y carga: [docs/DEVELOPMENT_AND_LOADING.md](docs/DEVELOPMENT_AND_LOADING.md)
- Memoria y glosarios: [docs/LEARNING_AND_GLOSSARIES.md](docs/LEARNING_AND_GLOSSARIES.md)
- Diseño vigente: [docs/product/README.md](docs/product/README.md)

La fuente posterior a 1.2.7.0 añade en Ajustes una prueba cancelable del servidor
LanguageTool mediante una frase sintética fija, sin contenido del dibujo ni
persistencia de la respuesta. Pasa 131/131 pruebas Debug y Release y la
integración C# real atraviesa el mismo servicio usado por el botón. El candidato
1.2.8.0 quedó instalado con 13/13 archivos idénticos y 1.2.7.0 como rollback;
la comprobación visual confirmó el botón, el estado disponible y el mensaje de
privacidad sin recortes en la ventana desplazable. La DLL Release tiene SHA-256
`95F20FAEDECCC0D4697367EDAC6203AD2160A7DC4702EB52BF21477BC4CEFB2D` y el ZIP
reproducible `4F3165005B2A5DD9A1FE45E92B617F23AD42491E9BC285552520F821BA68CCD1`.

La fuente 1.2.13.0 incorpora como regresión los dos MText completos del piloto
bilingüe. Corrige 20 faltas españolas y 7 inglesas sin alterar `LLDPE`, `HDPE`,
`SST`, `PD`, medidas ni unidades. Añade reglas seguras para `CONECCIONES`,
`DETUBERÍAS`, `HIDROFILICOS`, la frase técnica `SOLDADURA POR EXTRUCCIÓN`,
`CONECTIONS`, `THICKNES` y `PRESURE`, evitando los falsos positivos
`COLECCIONES` y `EXTRACCIÓN`. Una prueba adicional corrige español e inglés
dentro del mismo MText con ambos diccionarios reales. Pasa 138/138 pruebas Debug y Release sin
advertencias. La DLL Release tiene SHA-256
`61FF7C02C2977BE21BFCA54E4D9F17979D81987016371C674AB052900D0C38B5` y dos
construcciones produjeron el mismo ZIP verificado con SHA-256
`EE07BCD39648CD8589E642D8ACE05A054B0550BA4C9558A540C75F0BF874A209`.
La instalación coincide 17/17 con el bundle y las regresiones de AutoCAD pasan;
1.2.10.0 se conserva como único rollback estable.

La fuente 1.2.12.0 añade el diccionario offline `en_US`, detecta español,
inglés o texto mixto antes de consultar proveedores y usa autodetección en
LanguageTool cuando el idioma no es inequívoco. El paquete lleva ambos
diccionarios y sus licencias. Pasa 135/135 pruebas automatizadas en Debug y
Release. La DLL Release tiene SHA-256
`8E864DA06082EA8ACF472CA31259817B4BE871BCE3EE676007FECD256C3D2830` y dos
construcciones produjeron el mismo ZIP con SHA-256
`0E48FC48880D36A35F8EBDB023B4DE5215EFDA1CAA2D0B809CB1B0884337F027`.

La fuente 1.2.11.0 evita que coincidencias idénticas como
`FINALIDAD → FINALIDAD` creen filas bloqueadas sin cambios. Pasa 131/131 pruebas
en Debug y Release. La DLL Release tiene SHA-256
`A4509BDF5AD03838BD74EFB549FDECC554EDA58C0C4B07DF82A984053DDBAAC9` y el ZIP
reproducible `0B86587775F212B431D3D867C4471C11929A31B8E100FBEF9725BB2BEF65CC1E`;
la comprobación visual está pendiente.

La fuente 1.2.10.0 completa el texto controlado de doce errores con las reglas
`PROTEJE → PROTEGE` y `DEVE → DEBE`. Las 131 pruebas pasan en Debug y Release;
la DLL Release tiene SHA-256
`651884C9F28B567E5694007320B7F4D571D0E012C1FF810DD411AAAD265DABAB` y el ZIP
reproducible `3B8BC499C11D6380199107FA983CB821ACFBDE9F9BADE7DAF54016581E89A938`.
La instalación coincide 13/13; la comprobación visual detectó los 12/12 errores
controlados, conservó las cuatro siglas técnicas, aplicó el lote y una sola `U`
restauró el texto.

La fuente 1.2.9.0 corrige la diferencia observada entre mayúsculas y minúsculas
en el diccionario local: las sugerencias únicas de una letra y las tildes
inequívocas conservan ahora la capitalización original sin excluir palabras por
estar completamente en mayúsculas. Las siglas técnicas cortas y las definidas
mediante `SIGLA = descripción` se conservan y su alteración queda bloqueada por
el validador. Las 131 pruebas pasan en Debug y Release;
la DLL Release tiene SHA-256
`951AAE7126E15C66F7798BAB7B20AD685E28B412AA35EB9352210B841CF563D0` y el ZIP
reproducible `B06F57DCD21CE640F38EE0E57F23E8B964695B621993EC91AD7CD9CC3C9A4FBA`.
La instalación y la comprobación visual del nuevo candidato están pendientes.

La fuente posterior a 1.2.6.0 identifica por separado las alternativas de
`LanguageTool local` y `Reglas locales`, manteniendo las reglas integradas como
primera opción en caso de empate. El candidato 1.2.7.0 pasa 128/128 pruebas;
su DLL Release tiene SHA-256
`9F9669059BA14FC76C07BBD83B04E88DF930118EC7D567D74A79CE9D8334C8F4` y su ZIP
reproducible `D6E763595BCFF52CA37C3ED13AF11576FC1EB192F125B701161023750C434823`.
Quedó instalado con 13/13 archivos correctos y 1.2.6.0 como rollback. La
comprobación visual mostró por separado ambos orígenes en un lote de cuatro
textos, con cuatro filas con propuestas y cero fallos. El candidato 1.2.6.0,
que conectó inicialmente LanguageTool
local de forma opcional con `AISPELL`, `AISPELLALL` y Ajustes. Pasa 128/128
pruebas en Debug y Release sin advertencias; la DLL Release tiene SHA-256
`D1891EFD1BD2781BEB5235CC56F4B5E1E1F950675E229418F263137E26BF335D` y el ZIP
reproducible `F410F4F9BB0CBCDC04D30FA97250EFABE239E0777BBBD92A91A86CBB839D4202`.
Temurin 17 y LanguageTool 6.6 quedaron instalados localmente y el servicio
escucha solo en `127.0.0.1:8081`. La prueba C# real pasó y la revisión visual
en Civil 3D confirmó la nueva opción, cuatro solicitudes locales, 4/4 filas con
propuestas y cero fallos de proveedor. La aplicación interactiva se canceló
para no interferir con el trabajo del usuario; aplicación y `UNDO` atómicos
permanecen cubiertos por AutoCAD Core Console. El candidato anterior 1.2.5.0
queda disponible como rollback.
El diccionario `es_PE` restaura tildes inequívocas y corrige
erratas de una letra con una única sugerencia; términos técnicos, códigos y
casos ambiguos permanecen protegidos. Una regresión dentro de AutoCAD Core Console corrigió en el
mismo lote un `MLeader`, dos celdas de una `Table`, un `DBText` de definición de
bloque y un `AttributeReference`; una sola orden `U` restauró los cinco textos.
El resultado de `AISPELLALL` advierte que `U` debe usarse inmediatamente, antes
de zoom, desplazamiento u otro comando, para que el historial de AutoCAD llegue
directamente al lote.
La versión publicada 1.2.0.0 permanece disponible. El candidato 1.2.5.0 está
instalado y completó su regresión interactiva en un dibujo desechable: autocarga,
corrección local, aplicación y restauración exacta con una sola `U` inmediata.
Cualquier publicación requiere todavía autorización expresa.

Los archivos de Hunspell proceden de LibreOffice/RLA-ES bajo MPL 1.1. La
procedencia, huellas y licencias se conservan en
[THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md). RAE/ASALE se utiliza solo
como referencia normativa y sus diccionarios no se copian ni redistribuyen.
