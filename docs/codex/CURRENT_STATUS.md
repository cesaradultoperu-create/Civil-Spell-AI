# Current Status

Última verificación: 2026-09-14.

## Corte actual

- El 2026-09-14 se revalidó el candidato 1.2.13.0 sobre
  `22 PRUEBA_CivilSpellIAI_antes.dwg`, una copia desechable. Con OpenAI y
  LanguageTool desactivados, `AISPELLALL` revisó un MText, mostró una propuesta
  de reglas locales con 16 cambios, cero fallos de proveedor y validación
  técnica aprobada. Las 16 correcciones fueron inspeccionadas, se aplicaron y
  una sola `U` inmediata restauró los errores originales. Las validaciones
  Debug y Release volvieron a pasar 138/138 pruebas, sin advertencias; Release
  produjo la misma DLL
  SHA-256 `61FF7C02C2977BE21BFCA54E4D9F17979D81987016371C674AB052900D0C38B5`.
  La integración C# real de LanguageTool pasó en Release contra el servicio
  temporal en `127.0.0.1:8081`, que se detuvo después de la prueba.

- El candidato 1.2.13.0 fija como regresión los dos MText completos creados por
  el propietario. Corrige localmente 20 faltas españolas y 7 inglesas, conserva
  siglas, números y unidades, evita `CONECCIONES → COLECCIONES` y corrige el
  contexto técnico a `SOLDADURA POR EXTRUSIÓN`. Una prueba adicional cubre
  español e inglés en el mismo MText. Debug y Release pasan 138/138 pruebas sin
  advertencias. La DLL Release tiene SHA-256
  `61FF7C02C2977BE21BFCA54E4D9F17979D81987016371C674AB052900D0C38B5`.
  Dos builds produjeron el mismo ZIP verificado con SHA-256
  `EE07BCD39648CD8589E642D8ACE05A054B0550BA4C9558A540C75F0BF874A209`.
  Está instalado y coincide 17/17 con el bundle; 1.2.10.0 queda como único
  rollback estable. `UNDO-04`, `DICT-01` y `EXTENDED-01` pasan. La revisión
  visual mostró 20 cambios españoles y 7 ingleses, con cero fallos; los siete
  ingleses fueron aprobados. La DLL instalada también corrigió una anotación
  `Mixed` usando ambos diccionarios. Solo queda la inspección visual final de
  las últimas entradas españolas cuando no interfiera con el trabajo.

- El candidato 1.2.12.0 añade corrección ortográfica inglesa offline con el
  diccionario abierto `en_US`, conserva `es_PE` y detecta español, inglés o
  texto mixto antes de consultar proveedores. LanguageTool recibe `es`,
  `en-US` o `auto`. Debug y Release pasan 135/135 pruebas sin advertencias. La
  DLL Release tiene SHA-256
  `8E864DA06082EA8ACF472CA31259817B4BE871BCE3EE676007FECD256C3D2830`; dos
  builds produjeron el mismo ZIP verificado con SHA-256
  `0E48FC48880D36A35F8EBDB023B4DE5215EFDA1CAA2D0B809CB1B0884337F027`.
  Se instaló con archivos idénticos al bundle; `DICT-01`, `UNDO-04` y
  `EXTENDED-01` pasaron. La prueba visual bilingüe produjo dos propuestas y
  cero fallos, pero fue cancelada al observar los falsos positivos corregidos
  por 1.2.13.0.

- La fuente 1.2.11.0 omite coincidencias cuyo reemplazo es idéntico al texto
  original. Elimina así las filas bloqueadas sin diferencias observadas con
  `FINALIDAD` y `CANTIDAD`. Debug y Release pasan 131/131 pruebas. La DLL tiene
  SHA-256 `A4509BDF5AD03838BD74EFB549FDECC554EDA58C0C4B07DF82A984053DDBAAC9` y el
  ZIP reproducible `0B86587775F212B431D3D867C4471C11929A31B8E100FBEF9725BB2BEF65CC1E`.
  Falta instalar y comprobar visualmente en `C1.dwg`.
- La fuente 1.2.10.0 incorpora `PROTEJE → PROTEGE` y `DEVE → DEBE`, los dos
  casos omitidos de la prueba controlada. La cobertura esperada del mismo texto
  pasa de 10/12 a 12/12, manteniendo la protección de siglas de 1.2.9.0. Debug
  y Release pasan 131/131 pruebas. La DLL Release tiene SHA-256
  `651884C9F28B567E5694007320B7F4D571D0E012C1FF810DD411AAAD265DABAB` y el ZIP
  reproducible `3B8BC499C11D6380199107FA983CB821ACFBDE9F9BADE7DAF54016581E89A938`;
  la instalación coincide 13/13. La prueba visual detectó 12/12 errores, conservó
  `LLDPE`, `SST`, `GCL` y `CQA`, aplicó el lote y una sola `U` restauró el texto.
- La fuente 1.2.9.0 elimina la restricción especial que impedía al diccionario
  corregir erratas seguras escritas completamente en mayúsculas. Conserva la
  capitalización y mantiene protegidos nombres tras iniciales, términos
  técnicos ya corregidos, códigos, glosario y resultados ambiguos. La primera
  prueba visual detectó y no aplicó `LLDPE → LLAPE`; la fuente ahora reconoce
  siglas cortas y abreviaturas definidas con `=`, conserva `LLDPE` y bloquea su
  alteración en el validador. Debug y
  Release pasan 131/131 pruebas sin advertencias. La DLL Release tiene SHA-256
  `951AAE7126E15C66F7798BAB7B20AD685E28B412AA35EB9352210B841CF563D0` y el ZIP
  reproducible `B06F57DCD21CE640F38EE0E57F23E8B964695B621993EC91AD7CD9CC3C9A4FBA`.
  Falta instalarlo y comprobarlo visualmente en Civil 3D.
- La fuente posterior a 1.2.7.0 añade a Ajustes una prueba cancelable de
  LanguageTool local. Usa sólo una frase sintética fija, no texto del dibujo,
  no persiste la respuesta y clasifica fallos sin detalles internos. Debug y
  Release pasan 131/131 pruebas; la integración C# real recorre el mismo
  servicio. El candidato 1.2.8.0 quedó instalado con 13/13 archivos idénticos y
  1.2.7.0 como rollback. La prueba visual confirmó **Probar servicio local**,
  el estado disponible y el mensaje de frase fija sin persistencia, sin recortes
  en la ventana desplazable y sin escribir el fixture. La DLL Release tiene
  SHA-256 `95F20FAEDECCC0D4697367EDAC6203AD2160A7DC4702EB52BF21477BC4CEFB2D` y el
  ZIP reproducible `4F3165005B2A5DD9A1FE45E92B617F23AD42491E9BC285552520F821BA68CCD1`.
- La fuente posterior a 1.2.6.0 asigna a LanguageTool un origen propio y lo
  muestra como `LanguageTool local` tanto en `AISPELL` como en `AISPELLALL`.
  Conserva la prioridad de las reglas integradas, la compatibilidad numérica de
  los orígenes anteriores y la protección estricta del glosario. Debug, Release
  y la integración real local pasan sin advertencias. El candidato 1.2.7.0 ya
  está empaquetado: DLL SHA-256
  `9F9669059BA14FC76C07BBD83B04E88DF930118EC7D567D74A79CE9D8334C8F4` y ZIP
  reproducible `D6E763595BCFF52CA37C3ED13AF11576FC1EB192F125B701161023750C434823`.
  El verificador y sus cinco pruebas negativas pasan. Los 13/13 archivos
  instalados coinciden con el bundle y 1.2.6.0 queda como rollback. La prueba
  visual mostró tres filas `Reglas locales` y una fila
  `Alternativa 2 · LanguageTool local`, 4/4 textos con propuestas, cero fallos
  y validación técnica aprobada; se canceló sin escribir el fixture.
- El candidato instalado 1.2.6.0 conecta LanguageTool local como opción
  explícita de Ajustes para `AISPELL` y `AISPELLALL`. Solo usa
  `http://127.0.0.1:8081`, permanece desactivado por defecto, reutiliza textos
  repetidos en cada lote y conserva las reglas integradas ante fallos. Debug
  y Release pasan 128/128 pruebas sin advertencias. La DLL Release tiene
  SHA-256 `D1891EFD1BD2781BEB5235CC56F4B5E1E1F950675E229418F263137E26BF335D`.
  Dos builds produjeron el mismo ZIP con SHA-256
  `F410F4F9BB0CBCDC04D30FA97250EFABE239E0777BBBD92A91A86CBB839D4202` y el
  verificador independiente rechazó sus cinco variantes inválidas.
  Temurin 17.0.20.1 y LanguageTool 6.6 quedaron instalados
  fuera del complemento; el servicio escucha solo en `127.0.0.1:8081` y la
  prueba C# real pasó.
- Los 13/13 archivos instalados coinciden con el bundle; ensamblado y manifiesto
  indican 1.2.6.0 y el respaldo inmediato contiene 1.2.5.0. `DICT-01`,
  `UNDO-04` y `EXTENDED-01` pasaron en AutoCAD Core Console.
- La prueba visual cargó el dibujo desechable sin `NETLOAD`, habilitó
  LanguageTool con timeout de 10 segundos y confirmó cuatro solicitudes reales.
  `AISPELLALL` mostró propuestas en 4/4 filas y cero fallos. Se canceló antes de
  aplicar para permitir que el usuario volviera a su trabajo; la escritura y
  una sola `U` están verificadas por las regresiones automatizadas de AutoCAD.
- El candidato 1.2.5.0 incorpora el aislamiento de la futura conexión con
  LanguageTool local: sin proxy, cookies ni redirecciones HTTP, y con un límite
  de 20 000 caracteres por corrección antes del diff y la interfaz. Dos builds
  Release consecutivos pasaron 127/127 pruebas sin advertencias y produjeron el
  mismo ZIP. La DLL tiene SHA-256
  `34CD11222D57A02B025CCCC39BAB711BC1ADBA4809186E4051527505761B4DBE` y el
  ZIP reproducible
  `09DCC19BC1BA1B10B351D5C452FAC6AB8DCC5F04CC615279A5EA1498609068D3`.
  El verificador independiente y sus cinco pruebas negativas pasaron.
- El 2026-09-09 se actualizó la instalación administrada a 1.2.5.0 con Civil 3D
  cerrado. Los 13/13 archivos coinciden con el bundle; `UNDO-04`, `DICT-01` y
  `EXTENDED-01` pasaron en AutoCAD Core Console. Solo queda el paquete 1.2.5.0
  y un rollback 1.2.4.0 en `backup-20260909-170327-637`.
- La regresión interactiva final de 1.2.5.0 pasó el 2026-09-09 en Civil 3D 2024
  sobre un dibujo desechable: autocarga sin `NETLOAD`, una fila de origen
  `Reglas locales`, dos correcciones de concordancia, cero fallos, validación
  técnica aprobada, aplicación explícita y restauración exacta del original
  mediante una sola `U` inmediata. No se ha autorizado la publicación.
- El candidato 1.2.4.0 amplía la corrección gramatical local y la instrucción
  remota, reutiliza en memoria la respuesta de textos idénticos durante cada
  lote y deja `No` seleccionado en todo consentimiento con costo. Dos builds
  Release consecutivos pasaron 125/125 pruebas sin advertencias y produjeron el
  mismo ZIP. La DLL tiene SHA-256
  `7CEC88AE30FC6145B580A43E8C71CE4AF1B5E10F68322DBCD14D8A04376D06C2` y
  el ZIP reproducible
  `AA2B4B7A399C537867F5D8FDB59D165295521EC5703FC6B55F179CFB2A96C46F`.
  El verificador independiente y sus cinco pruebas negativas pasaron.
- El 2026-09-09 se actualizó la instalación administrada a 1.2.4.0 con Civil 3D
  cerrado. Los 13/13 archivos instalados coinciden por hash con el bundle, y
  ensamblado y manifiesto indican 1.2.4.0. En ese momento 1.2.3.0 quedó como
  respaldo; al instalar 1.2.5.0 fue sustituido por el rollback inmediato
  1.2.4.0.
  `UNDO-04`, `DICT-01` y `EXTENDED-01` volvieron a pasar en AutoCAD Core
  Console con perfiles y DWG temporales aislados; una sola `U` restauró los
  lotes y no quedaron archivos de prueba pendientes.
  La prueba interactiva final pasó el 2026-09-09: autoload sin `NETLOAD`, una
  corrección gramatical local con dos cambios, una propuesta real de origen
  `IA` con dos cambios y cero fallos, aplicación y restauración exacta mediante
  una sola `U` inmediata. No se ha autorizado todavía la publicación.
- El candidato 1.2.1.0 añade una guía de primera ejecución de `AISPELLALL` y
  administración enmascarada de la clave API propia desde Ajustes. La clave se
  conserva únicamente como variable de entorno del usuario de Windows; no se
  muestra ni entra en configuración, DWG o instalador. Debug y Release pasan
  117/117 pruebas; la DLL Release tiene SHA-256
  `F000FAE02313F2BBD6CFFDB1AECDCD4A970657A703B08F9E507587489AAC6689` y el
  ZIP 1.2.1.0 reproducible tiene SHA-256
  `F1224C385EBA6BA6EE9A6B94F431F706A5371307860A1F75F488BDC73C570603`.
  El paquete superó el verificador independiente. Todavía no está publicado.
- El candidato 1.2.1.0 quedó instalado y verificado archivo por archivo el
  2026-09-08: 13/13 archivos coinciden con el bundle y la DLL instalada conserva
  SHA-256 `F000FAE02313F2BBD6CFFDB1AECDCD4A970657A703B08F9E507587489AAC6689`.
  `AISPELLSETTINGS` detectó la credencial sin mostrarla; la prueba de conexión
  real fue correcta. En un dibujo nuevo, `AISPELLALL` envió únicamente un MText
  de 40 caracteres, mostró una propuesta de origen `IA`, corrigió concordancia
  y tilde, aplicó el cambio y una sola `U` inmediata restauró el original. La
  versión 1.2.0.0 permanece recuperable en `backup-20260908-142447-619`.
- La prueba ampliada sobre `PRUEBA_CivilSpellAI_antes.dwg` revisó 593 textos y
  aproximadamente 10 052 caracteres con OpenAI: devolvió 52 filas con
  propuestas, cero fallos de proveedor y 21 correcciones técnicamente seguras
  preseleccionadas. El propietario revisó, aplicó las 21 y confirmó visualmente
  que el resultado completo era correcto.
- CivilSpellAI 1.2.0.0 compila para .NET Framework 4.8/x64 sin
  advertencias y el corte que protege los controles completos de `MText` quedó
  instalado de forma administrada. Los 13 archivos instalados coinciden por
  hash con el bundle. La publicación de este corte fue autorizada el
  2026-09-08; 1.1.5.0 continúa disponible como respaldo estable.
- La suite autocontenida tiene 115 pruebas deterministas y no usa Autodesk ni
  OpenAI real.
- La fuente incorpora el diccionario abierto Hunspell `es_PE` de Perú mediante
  una interfaz independiente del motor. Corrige solo tildes inequívocas y
  erratas de distancia uno con sugerencia única; protege códigos y
  ambigüedades. La procedencia, huellas y licencia MPL 1.1 están incluidas y no
  se copia contenido de RAE/ASALE.
- `AISPELLALL` incluye ahora `MLeader` textual, celdas simples editables de
  `Table`, textos fijos de bloques locales y atributos por inserción, además de
  `DBText` y `MText`. Omite Xrefs, etiquetas nativas y celdas complejas.
- `EXTENDED-01` pasó en AutoCAD Core Console: un `MLeader`, dos celdas de una
  misma tabla, un texto de definición y un atributo cambiaron atómicamente; una
  sola `U` restauró exactamente los cinco destinos.
- `DICT-01` cargó `WeCantSpell.Hunspell` y `es_PE` dentro de AutoCAD Core
  Console y corrigió `ADMINISTRACION` a `ADMINISTRACIÓN` sin red.
- Debug y Release pasan 115/115 pruebas. La DLL Debug tiene SHA-256
  `909A72A808E3227FDA2E67462CEC18DB344F170BBE6AB7859AB418147F4865A6` y
  la DLL Release candidata
  `3D5972B0951DD17EC3CF6BAD1F6E366C49C4406913D244C3A33EDD279F786D5B`;
  el ZIP verificado y reproducible tiene SHA-256
  `2779BD4E787F1D8DE3AE74E783CE77B89CA2F9AD9C9B325EE2591133970A3A53`.
- `AISPELL` admite alternativa local/remota, edición manual explícita, diff y
  validación técnica antes de habilitar la aplicación.
- `AISPELLALL` prepara con progreso y cancelación, permite buscar y filtrar por
  layout/entidad/estado/origen, cambiar la alternativa de cada fila y conserva
  la escritura atómica; el mensaje final indica `U` y una sola orden restaura
  el lote completo.
- OpenAI sigue siendo optativo. El lote muestra cantidad de textos y caracteres
  antes del envío y permite continuar solo con reglas locales. Los modelos
  admitidos son `gpt-5.6-luna`, `gpt-5.6-terra` y `gpt-5.6-sol`.
- La memoria local esquema 1 registra únicamente decisiones que el usuario
  marca expresamente. Una preferencia reaparece como alternativa visible y
  nunca se autoaplica; puede buscarse, desactivarse, exportarse o borrarse.
- El glosario organizacional es de solo lectura y se carga desde
  `%PROGRAMDATA%\CivilSpellAI\organizational-glossary.txt` cuando existe.
- El diagnóstico local optativo conserva únicamente campos estructurados, sin
  texto del dibujo, prompts, respuestas, rutas, handles ni secretos.
- El paquete Autodesk Application Bundle 1.1.5.0 contiene solo los tres
  comandos públicos, carga por invocación y no incluye DLL de Autodesk ni PDB.
- ZIP candidato: `CivilSpellAI-1.1.5.0.zip`, SHA-256
  `FE394373A9B5801D9B42D7EA1C389DAAC7EE74EEA5CE3A6B10CBC396DFE4D68A`;
  el archivo `.zip.sha256` adyacente contiene la misma huella.
- El prerelease público 1.1.5.0 contiene el ZIP y checksum finales; su etiqueta
  apunta al commit `30856f7e825ff1221558ac2db6727ae17f4061ed`:
  https://github.com/cesaradultoperu-create/Civil-Spell-AI/releases/tag/1.1.5.0.
- La DLL 1.1.4.0 pasó Debug (`016BA4814D7551001379B49FB936C6F0CB962EBB9A930EDB40F8F806F43D8FFB`)
  y Release (`73FEDCF3911367B5A19D3B62FF68A1626EE48E459CC018170690106BA53E687A`).
- El 2026-09-02 la versión 1.1.3.0 se instaló bajo
  `%PROGRAMFILES%\Autodesk\ApplicationPlugins`; manifiesto, DLL, glosario y ayuda
  coincidieron por hash con el artefacto. La copia `CurrentUser` se retiró a un
  respaldo recuperable y los 18 archivos locales comprobados permanecieron
  idénticos.
- El mismo día se actualizó realmente a 1.1.4.0, se comprobó el rollback exacto
  a 1.1.3.0, la desinstalación y la reinstalación final de 1.1.4.0. Los datos
  locales permanecieron idénticos y la instalación final coincide con la DLL
  Release candidata.
- El 2026-09-03 se actualizó la instalación administrada de 1.1.4.0 a 1.1.5.0.
  Los cuatro archivos instalados coinciden por hash con el bundle candidato, la
  DLL instalada tiene SHA-256
  `11A33B5825AC38E9B35F9E4A1951A2D02F5E190B624A0B27A7204A45FF138331` y
  1.1.4.0 quedó en un respaldo recuperable.
- `AISPELLSETTINGS`, `AISPELL` y `AISPELLALL` cargaron 1.1.2.0 por invocación,
  sin `NETLOAD`. La sesión confirmó cancelación sin respuesta tardía, edición
  segura y bloqueada, aplicación por lote con un único `UNDO`, memoria local y
  su revocación/borrado.
- El corte 1.1.4.0 incorpora una segunda pasada de
  accesibilidad WPF (alto contraste, etiquetas, estados anunciables y teclas de
  acceso), sincroniza la selección de filas con el foco de sus controles y
  expone los motivos de validación como ayuda accesible. La validación comprueba
  estas garantías en las cuatro ventanas WPF.
- El corte 1.1.2.0 añadió cancelación visible y estado
  anunciable a la prueba de conexión de Ajustes, y descarta como canceladas las
  respuestas que lleguen tarde aunque el proveedor ignore el token. El foco se
  mueve entre Probar y Detener según el control disponible, y cancelar no abre
  un diálogo modal redundante. Los fallos muestran orientación y códigos de
  soporte estables sin volcar detalles internos. Estas mejoras pasaron la
  regresión interactiva del 2026-09-02.
- La sesión encontró y cerró dos defectos no destructivos: una corrección local
  inglesa hacia `SURFACE` era bloqueada por el glosario, y una preferencia
  duplicada podía desplazar la regla local. 1.1.3.0 los corrigió y la regresión
  interactiva confirmó la propuesta inglesa seleccionable y una única
  alternativa de reglas locales después de recordar, aplicar y ejecutar `U`.
- Las fronteras asíncronas individual y por lote devuelven el control al
  cancelar aunque un proveedor no coopere; sus tareas tardías quedan observadas
  y no pueden incorporarse a la revisión.
- Los fallos inesperados de carga o reintento de IA quedan contenidos en el
  ViewModel, conservan las propuestas locales y muestran un código seguro.
- La preparación global inicia con foco en Cancelar y el reintento individual
  evita dejar el foco sobre un botón deshabilitado.
- La prueba de conexión refresca el estado de `OPENAI_API_KEY` antes de pedir
  confirmación y se bloquea temprano cuando la credencial no está disponible.
- Configuración, glosario y memoria recuperan temporales válidos tras una
  escritura interrumpida. La memoria normaliza registros, protege exportación y
  elimina temporales al borrarse.
- El diff limita su matriz a un millón de celdas; OpenAI limita respuestas y
  alternativas extensas, y el validador protege más unidades y símbolos civiles.
- El transporte OpenAI limita la respuesta mientras la recibe y usa el timeout
  configurado sin depender del límite global de `HttpClient`.
- Los errores visibles de comandos y almacenamiento omiten mensajes internos y
  conservan un código de soporte estable, incluso para excepciones anidadas.
- La escritura por lote rechaza una operación sin cambios antes de escribir;
  un fallo opcional al recordar preferencias no invalida una aplicación exitosa.
- La validación de la versión 1.1.5.0 pasó en Debug y Release con 105/105
  pruebas; SHA-256 Debug
  `196A53E69DCBA47536C7D4AE6E110EF27F86FCBC190E270380351AB3833A8031` y
  Release `11A33B5825AC38E9B35F9E4A1951A2D02F5E190B624A0B27A7204A45FF138331`.
- `UNDO-04` pasó en AutoCAD Core Console con la DLL Debug 1.1.5.0: dos cambios
  aplicados por la frontera de `AISPELLALL` fueron restaurados exactamente por
  una sola orden `U`. REG-2026-005 documenta la corrección de la instrucción.
- `UNDO-04` pasó también interactivamente en la instalación administrada
  1.1.5.0: `AISPELLALL` aplicó tres correcciones sobre el fixture limpio, mostró
  «Use U una vez» y el propietario confirmó la reversión completa.
- `scripts\New-PilotFixture.ps1` genera mediante AutoCAD Core Console un DWG
  desechable con cuatro entidades `TEXT`/`MTEXT`; el generador se ejecutó y
  validó sin abrir dibujos del usuario. El fixture 1.1.5 tiene SHA-256
  `2F684893689D88237E983D7A77E34DF37EA4C3046AC43D2DC3CA4CF63AF80470`.
- La regresión `UNDO-04` trabaja sobre una copia temporal del fixture, descarta
  los cambios al cerrar y elimina la copia; el DWG base conserva el mismo hash.
- Tras la validación interactiva, Civil 3D se cerró sin guardar y el fixture base
  conservó la huella SHA-256 registrada.
- El empaquetador ordena las entradas y fija sus fechas internas. Dos builds
  consecutivos de 1.1.5.0 produjeron el mismo ZIP y SHA-256.
- El verificador independiente de entrega acepta el ZIP final y rechaza tanto
  un checksum asociado a otro nombre, una huella SHA-256 alterada o cualquier
  archivo no previsto. El empaquetador ejecuta esta verificación antes de
  declarar correcto el paquete.

## Hitos

- Hito 1 cerrado: regresión base y OpenAI real comprobados.
- Hito 2 cerrado: fronteras de selección/documento/escritura y atomicidad.
- Hito 3 cerrado: diagnóstico seguro, incluyendo `TMO-001` real y privacidad.
- Hito 4 cerrado: preflight, instalación administrada, autoload final,
  actualización, rollback, desinstalación y reinstalación reales en PASS.
- Hito 5 cerrado: matriz funcional, cancelación durante preparación y recorrido
  completo con Tab/Shift+Tab al 125 % en Configuración, revisión individual y
  lote en PASS, sin recortes ni superposición.
- Hito 6 cerrado: memoria, revocación, borrado, ausencia de autoaplicación y
  prioridad de reglas locales en PASS; glosario organizacional no aplicable al
  equipo piloto.

## Estado operativo actual

La compilación 1.2.0.0 candidata quedó instalada en `Program Files`; sus 13
archivos coinciden por hash y la DLL tiene SHA-256
`3D5972B0951DD17EC3CF6BAD1F6E366C49C4406913D244C3A33EDD279F786D5B`,
`WeCantSpell.Hunspell` es 2.0.1.0 y el diccionario `es_PE` tiene SHA-256
`1D810A0A319FEE329000538CF4DF76621BB71228172558B87490F8B9C19B9D75`.
`DICT-01`, `UNDO-04` y `EXTENDED-01` están en PASS. El corte instalado anterior,
cuya DLL tenía SHA-256
`E4C9F9A7B9CFDF0708DC36A1E24BAD40DCC47DAF8751B71A6D58497B8FE86413`,
quedó recuperable en
`%LOCALAPPDATA%\CivilSpellAI\deployment-backups\AllUsers\backup-20260908-091534-094`.
El nuevo corte distingue el verbo `HA` de la unidad hectárea `ha`, protege
los controles completos de formato MText y evita modificar nombres de fuente
como `Arial Narrow`. Pasó 115/115 pruebas y la instalación se verificó archivo
por archivo. La prueba visual se ejecutó únicamente sobre la copia que conserva
los errores intencionales; ningún dibujo forma parte de la publicación.

La comprobación interactiva más reciente no alcanzó la marca de `AISPELLALL`:
las órdenes `U` mostraron `INTELLIPAN` en la línea de comandos porque se había
movido la vista después de aplicar. La integración aislada se repitió el
2026-09-07 y confirmó de nuevo que una sola `U` inmediata restaura el lote. El
mensaje de resultado se aclaró para advertir que no debe hacerse zoom, pan ni
otro comando antes de `U`. El ajuste pasó 115/115 pruebas y quedó instalado y
verificado archivo por archivo el 2026-09-08.
La corrección posterior para `TUVERÍA` dentro de `MLeader` pasó también 115/115
pruebas, generó el candidato Release
`3D5972B0951DD17EC3CF6BAD1F6E366C49C4406913D244C3A33EDD279F786D5B`
y quedó instalado y verificado archivo por archivo.
La validación interactiva del 2026-09-08 confirmó tres propuestas nuevas: dos
`MLeader` y un `DBText`. `TUVERÍA`/`TUVERÍAS` cambió a
`TUBERÍA`/`TUBERÍAS` sin alterar `HDPE`, `PD`, `100/300` ni `mm`; una sola `U`
inmediata restauró conjuntamente los tres objetos.

## Fuera del alcance actual

- Etiquetas nativas de Civil 3D, referencias externas y celdas de tabla con
  fórmula, campo, vínculo de datos, valor no textual o contenido de bloque.
- Firma con certificado de una autoridad confiable; el piloto interno actual es
  sin firma, instalado de forma administrada en `Program Files`, limitado a
  equipos controlados y verificado por hash.

Fuente de verdad: `docs/product/10_MVP_STATUS_AND_TEST_PLAN.md`.
