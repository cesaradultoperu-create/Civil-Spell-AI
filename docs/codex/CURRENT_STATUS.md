# Current Status

Última verificación: 2026-09-09.

## Corte actual

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
  ensamblado y manifiesto indican 1.2.4.0. La versión 1.2.3.0 quedó recuperable
  en `backup-20260909-133624-484`, con DLL SHA-256
  `3CA5212C717C66402CE2E3C4D04ED117458A875CE7FB02115BCCD8D332ECD2AC`.
  `UNDO-04`, `DICT-01` y `EXTENDED-01` volvieron a pasar en AutoCAD Core
  Console con perfiles y DWG temporales aislados; una sola `U` restauró los
  lotes y no quedaron archivos de prueba pendientes. Se retiraron las entregas
  1.2.1.0–1.2.3.0 y todos los respaldos anteriores al único rollback 1.2.3.0.
  Falta únicamente la prueba interactiva controlada antes de decidir la
  publicación.
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
