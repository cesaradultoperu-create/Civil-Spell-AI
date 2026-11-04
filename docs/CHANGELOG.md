# Changelog

## Sin publicar

- 2026-10-30: `CREARFIGURA3D` reconoce la tubería corrugada 300 mm dentro
  de bloques TOP/FRONT anidados y reflejados. FRONT gobierna la longitud y
  elevaciones; TOP aporta los ramales T. Genera tubos huecos con codo y paso
  interior libre y parámetros conservados para edición. Corrugas sólidas por
  revolución del perfil redondeado 2D, paso ~38,103 mm; tapas con bandas y
  remate. Espesor editable medido en el valle. Actualiza modelos lisos previos
  conservando el diámetro interior. Pruebas nativas de crestas/valles físicos,
  creación, edición, migración, reapertura y regresión TOP/FRONT.

- 1.3.7.0 (2026-10-12): el reconocimiento de tuberías laterales compara el
  tramo común del eje y sus bordes, admitiendo extremos recortados diferentes.
  Reconoce también un recorrido perpendicular a FRONT mediante los bordes de
  TOP y su sección circular del mismo diámetro/posición en FRONT. Reconstruye
  canales de mampostería a ambos lados y permite editar las coordenadas Y del
  tubo. La revisión advierte que un corte circular aislado no determina una
  pendiente longitudinal. Pruebas nativas sobre las tres parejas del plano
  actual, escala/desplazamiento, diámetro incompatible, perforación real del
  muro, tubo hueco, resaltado y reapertura; se conservan las regresiones previas.

- 1.3.6.0 (2026-10-06): seleccionar un campo o el nombre de un componente
  resalta esa pieza en verde en la vista previa TOP/FRONT y muestra su nombre.
  El resaltado permanece al editar y se puede quitar; no se guarda en el DWG.
  Abrir el editor conserva la precisión de los parámetros sin cambios.
  Verificación nativa de selección, edición, colores y cámara con A+B y C+D.

- 1.3.5.0 (2026-10-06): el reconocimiento TOP/FRONT admite tubería a izquierda
  o derecha, horizontal o inclinada, y bordes dibujados en segmentos solapados.
  El caso C+D conserva sus propias cotas, el canal derecho, la cama de arena
  izquierda y el elemento interior con soporte de concreto; el material de la
  pieza superior queda por confirmar. La prueba nativa conserva A+B y verifica
  C+D también desplazado y a otra escala. El comando sigue comenzando por
  seleccionar TOP y después FRONT; no depende de las letras o posición del ejemplo.

- 1.3.4.0 (2026-10-05): `CREARFIGURA3D` usa referencias emparejadas por defecto.
  Reconstruye el detalle de drenaje con canal de mampostería, salientes,
  base/solado y eje circular inclinado; el modo Componente conserva perfiles
  cerrados arbitrarios y huecos mediante TOP y FRONT. El modo Caja anterior
  permanece explícito. Previsualización desde geometría nativa y parámetros
  de componentes persistentes. La tubería necesita un espesor conocido para
  ser un sólido hueco; con 0 se conserva como superficie abierta.

- 1.3.3.0 (2026-10-05): Limpiar retira vertices colineales intermedios tambien
  en recorridos sin retroceso y en el cierre de polilineas, conservando las
  esquinas reales. Regresion del caso reportado: 70 -> 61 vertices, sin solapes
  antes ni despues y con restauracion exacta del conjunto de prueba.

- La vista previa de la caja 3D conserva caras planas y aristas rectas, sin
  sombrear cada fragmento del muro como un bloque independiente. Emite solo
  superficies expuestas y elimina caras interiores coincidentes. La regresión
  nativa compara volumen, cotas exteriores y área de coronación con el sólido,
  incluyendo la pareja izquierda de la captura (altura 0.80). Seleccionar
  objetos de TOP otra vez como FRONT muestra ahora un mensaje específico.
  Actualización de vista preparada para aplicar después, sin instalarla.

- Los pasos laterales 3D comienzan en 0.10 editable según lo indicado por el
  propietario. El ancho pendiente del paso derecho se inicializa en 0.10 y
  el paso comienza activo. La regresión nativa comprueba cambios a 0.20 × 0.12,
  posiciones editadas, sólido/cotas, copia independiente, una U y persistencia.

- `CREARFIGURA3D` crea una caja rectangular 3D editable desde selecciones
  TOP y FRONT, con base, cuatro muros, pasos rectangulares y cotas reales
  de planta/elevación. `EDITARFIGURA3D` y `EDITARSECCION` regeneran sólido
  y cotas. El editor de TOP incorpora **Crear 3D con FRONT…** y la nueva
  ventana incluye vista 3D giratoria y referencias separadas. Las medidas
  ambiguas de pasos laterales requieren completarse. Core Console 2024
  verifica sólido, volumen, cotas, copia, giro, una U y persistencia;
  Release compila las tres variantes y pasa 151/151 pruebas.

- La vista previa de medidas interiores de `EDITARSECCION` incorpora zoom
  alrededor del cursor, botones +/−, desplazamiento con botón central y
  encuadre. Las letras conservan su tamaño y siguen seleccionando medidas
  en la vista ampliada. Verificado mediante renderizado y navegación en
  Core Console 2024; Debug/Release compilan 2024/2025/2026 y pasan 151 pruebas.

- Entrega interna 1.3.2.0 (2026-10-02): revisión, limpieza y corrección de
  solapes entre LWPOLYLINE compatibles, con fragmentos conservados y restauración.
  Guía de instalación y uso incluida en el ZIP. Release pasa 12 verificaciones
  del flujo en Core Console 2024 y 151 pruebas generales; 2025/2026 compilados.

- El candidato 1.3.1.0 incorpora `CREARSECCION`, la creación desde objetos
  existentes y el reconocimiento comprobado de las secciones A y B. En la
  sección B, E1 termina en el quiebre A mientras E2 continúa hasta H3; las
  medidas, cotas y propiedades del hatch permanecen editables.
- El corrector local cubre ahora las 24 omisiones observadas en la prueba
  técnica extensa, incluidas formas ambiguas como `esta/está`,
  `calculo/cálculo`, `indico/indicó`, `realizo/realizó`, `sera/será` y
  `que/qué`. Estas formas solo se cambian dentro de contextos comprobados. El
  texto queda como regresión, una segunda revisión es estable y Debug/Release
  corrige además los cuatro remanentes finales: `calculo sin tilde`, `haci`,
  `devido` y la concordancia `alguno ... están mal configurados`. Debug/Release
  La prueba de excavación cubre también `SANJA`, `ESCAVACION`, `ESSEDENTE` y
  `ALINIAMIENTOS`, incluidos los casos repetidos. Debug/Release pasan 146/146
  El informe de movimiento de tierras cubre además `se realizo`, `se verifico`,
  `polilineas` y `actualisacion`. Debug/Release pasan 147/147 pruebas sin
  advertencias. `AISPELLALL` muestra el identificador `revisión lingüística
  El informe difícil en mayúsculas añade `BOLUMENES`, `PROLLÉCTO`, `REBICION`,
  `HERRORES`, `SERRADAS` en el contexto de polilíneas, `REBISADOS`, `CALCULO`
  después de `DEL` y `NUEBA`. Debug/Release pasan 150/150 pruebas y
  `AISPELLALL` muestra `validación automatizada 150/150`.
- La ayuda angular ahora espera a confirmar que el pinzamiento activo es la
  flecha antes de modificar temporalmente `AUTOSNAP` y `ORTHOMODE`. Mover otro
  pinzamiento del MLeader o una entidad incluida en la misma selección conserva
  el comportamiento nativo. La restauración de ambas variables también es
  independiente: un fallo al restaurar una ya no impide intentar la otra.
- El manifiesto declara ahora `LoadOnAutoCADStartup="True"` con la sintaxis
  booleana esperada por Autodesk. La declaración anterior usaba un atributo
  `LoadReasons` no válido y hacía que la ayuda angular se activara recién al
  invocar un comando como `AISPELLSETTINGS`. Tras reinstalar el bundle, la
  prueba interactiva en Civil 3D 2024 confirmó que las guías aparecen desde el
  primer arrastre, sin ejecutar previamente ningún comando del complemento.
- La ayuda angular de MLeader actúa al mover el pinzamiento de la flecha:
  muestra 24 guías transitorias, ajusta al múltiplo de 15 grados más cercano y
  conserva la longitud del tramo. No crea entidades y restaura `AUTOSNAP` y
  `ORTHOMODE` al terminar o cancelar. El cálculo polar tiene regresiones
  deterministas y elevó inicialmente la suite a 141/141 pruebas. En Civil 3D
  2024 quedaron
  confirmadas las guías, el cambio B/C, la cancelación exacta y la restauración
  mediante una sola `U`; quedan pendientes los casos de otros pinzamientos y
  MLeader de varias líneas.
- El candidato 1.3.0.0 añade compilaciones específicas para Civil 3D 2025
  (`R25.0`, .NET 8) y Civil 3D 2026 (`R25.1`, .NET 8), conservando la de Civil
  3D 2024 (`R24.3`, .NET Framework 4.8). El Application Bundle contiene las
  tres DLL y selecciona automáticamente la compatible. Las tres compilaciones
  terminan sin advertencias y el núcleo mantiene ahora 142/142 pruebas
  correctas. El ZIP reproducible y verificado tiene SHA-256
  `7030D87B2F3AC49662498C868FB5DCD78ACA825CC44DB801DC5690B9C3043BD2`.
  La prueba interactiva en 2025 y 2026 queda pendiente por no estar instaladas
  en el equipo de desarrollo.
- Revalidación del 2026-09-14: las 138 pruebas pasan nuevamente en Debug y
  Release sin advertencias; Release reproduce la DLL 1.2.13.0. Una revisión offline adicional en
  Civil 3D aplicó 16 correcciones locales inspeccionadas sobre un MText de una
  copia desechable; una sola `U` inmediata restauró el original.
- El candidato 1.2.13.0 convierte los dos MText completos de la prueba visual
  bilingüe en regresiones automatizadas. La ruta offline corrige 20 faltas
  españolas y 7 inglesas, conserva siglas, números y unidades, y elimina los
  falsos positivos `CONECCIONES → COLECCIONES` y
  `SOLDADURA POR EXTRUCCIÓN → SOLDADURA POR EXTRACCIÓN`. Añade reglas
  conservadoras para `DETUBERÍAS`, `HIDROFILICOS`, `CONECTIONS`, `THICKNES` y
  `PRESURE`. También cubre una anotación mixta con ambos diccionarios reales.
  Pasa 138/138 pruebas Debug y Release sin advertencias. Dos builds
  produjeron el mismo ZIP con SHA-256
  `EE07BCD39648CD8589E642D8ACE05A054B0550BA4C9558A540C75F0BF874A209`.
  La instalación coincide 17/17, las regresiones de AutoCAD pasan y 1.2.10.0
  queda como único rollback estable. La revisión visual mostró 20 cambios
  españoles y 7 ingleses con cero fallos; los siete ingleses fueron aprobados.

- El candidato 1.2.12.0 añade el diccionario Hunspell `en_US` al complemento y
  al ZIP para corregir erratas inglesas seguras sin red. `AISPELL` y
  `AISPELLALL` detectan primero español, inglés o texto mixto; LanguageTool
  recibe `es`, `en-US` o `auto` y la autodetección prefiere `en-US`. El
  verificador exige ambos diccionarios, licencias y huellas. Pasa 135/135
  pruebas en Debug y Release. Dos builds produjeron el mismo ZIP con SHA-256
  `0E48FC48880D36A35F8EBDB023B4DE5215EFDA1CAA2D0B809CB1B0884337F027`.

- El candidato 1.2.11.0 no registra una corrección cuando el reemplazo es
  idéntico al texto original. Esto elimina filas bloqueadas sin diferencias,
  observadas con palabras correctas como `FINALIDAD` y `CANTIDAD`. Pasa 131/131
  pruebas en Debug y Release. Dos builds produjeron el mismo ZIP con SHA-256
  `0B86587775F212B431D3D867C4471C11929A31B8E100FBEF9725BB2BEF65CC1E`.
- El candidato 1.2.10.0 añade `PROTEJE → PROTEGE` y `DEVE → DEBE`, los dos
  errores omitidos en la prueba controlada de doce casos. Las reglas conservan
  la capitalización y elevan la cobertura esperada de ese texto de 10/12 a
  12/12 sin desproteger `LLDPE`, `SST`, `GCL` ni `CQA`. Pasa 131/131 pruebas en
  Debug y Release. Dos builds produjeron el mismo ZIP con SHA-256
  `3B8BC499C11D6380199107FA983CB821ACFBDE9F9BADE7DAF54016581E89A938`.
  La instalación coincide 13/13 y la prueba visual confirmó las 12 correcciones,
  cero fallos, aplicación correcta y restauración completa con una sola `U`.
- El diccionario local deja de excluir erratas únicamente por estar escritas
  completamente en mayúsculas. Una sugerencia única y segura se aplica ahora
  con el mismo criterio a `contruccion`, `CONTRUCCION` y `Contruccion`,
  conservando la capitalización. Se mantienen las protecciones de nombres tras
  iniciales, glosario, códigos, términos técnicos ya corregidos y alternativas
  ambiguas. Una regresión visual encontró `LLDPE → LLAPE`; el candidato ahora
  reconoce siglas técnicas cortas y abreviaturas definidas con `=`, las conserva
  localmente y bloquea su alteración en la validación. El caso real conserva
  `LLDPE` mientras corrige `EXESO → EXCESO`. El candidato 1.2.9.0 pasa 131/131
  pruebas en Debug y Release. Dos
  builds produjeron el mismo ZIP con SHA-256
  `B06F57DCD21CE640F38EE0E57F23E8B964695B621993EC91AD7CD9CC3C9A4FBA`.
- Ajustes incorpora **Probar servicio local** para verificar LanguageTool antes
  de revisar un dibujo. Envía únicamente la frase sintética fija
  `La estructura esta instalada.`, no guarda la respuesta, permite cancelar y
  presenta fallos seguros y accionables. La suite aumenta a 131 pruebas y la
  integración real atraviesa el nuevo servicio. El candidato 1.2.8.0 quedó
  instalado con 13/13 archivos correctos y 1.2.7.0 como rollback; la prueba
  visual confirmó el estado disponible y el mensaje de privacidad. Dos builds
  produjeron el mismo ZIP con SHA-256
  `4F3165005B2A5DD9A1FE45E92B617F23AD42491E9BC285552520F821BA68CCD1`.
- Las alternativas producidas por LanguageTool ahora se identifican como
  `LanguageTool local` en `AISPELL` y `AISPELLALL`, en vez de confundirse con
  `Reglas locales`. Los valores previos del enum conservan sus números y las
  propuestas de LanguageTool usan la validación estricta del glosario. El
  candidato 1.2.7.0 pasa 128/128 pruebas; dos builds producen el mismo ZIP con
  SHA-256 `D6E763595BCFF52CA37C3ED13AF11576FC1EB192F125B701161023750C434823`.
  La instalación contiene 13/13 archivos idénticos al bundle y conserva 1.2.6.0
  como rollback. La revisión visual mostró ambos orígenes por separado en un
  lote de cuatro textos, con cuatro filas con propuestas y cero fallos.
- LanguageTool local ya puede habilitarse explícitamente desde
  `AISPELLSETTINGS` y aporta propuestas gramaticales a `AISPELL` y
  `AISPELLALL`. Permanece desactivado de forma predeterminada y usa únicamente
  `http://127.0.0.1:8081`.
- `AISPELLALL` reutiliza también las respuestas de LanguageTool para textos
  idénticos y limita a dos las revisiones concurrentes cuando está activo. Un
  fallo del servicio conserva las propuestas de las reglas integradas.
- La configuración explica que LanguageTool y Java son componentes locales
  externos que CivilSpellAI no instala ni inicia. La fuente pasa 128/128
  pruebas Debug y Release sin advertencias. La prueba C# real pasó con
  LanguageTool 6.6 y Temurin 17, escuchando solo en `127.0.0.1:8081`.
- El candidato 1.2.6.0 quedó instalado con 13/13 archivos idénticos al bundle y
  1.2.5.0 disponible como rollback. `AISPELLSETTINGS` guardó la activación local
  y `AISPELLALL` realizó cuatro solicitudes reales, mostró propuestas para los
  cuatro textos y reportó cero fallos. `DICT-01`, `UNDO-04` y `EXTENDED-01`
  permanecen en PASS dentro de AutoCAD Core Console. Dos builds Release
  produjeron el mismo ZIP con SHA-256
  `F410F4F9BB0CBCDC04D30FA97250EFABE239E0777BBBD92A91A86CBB839D4202`.
- Se añaden scripts separados para iniciar, consultar, detener y probar el
  servidor local sin incluir Java ni LanguageTool en el paquete del complemento.
- El candidato 1.2.5.0 pasa 127/127 pruebas en Debug y Release. Dos builds
  producen el mismo ZIP; la DLL tiene SHA-256
  `34CD11222D57A02B025CCCC39BAB711BC1ADBA4809186E4051527505761B4DBE` y el ZIP
  `09DCC19BC1BA1B10B351D5C452FAC6AB8DCC5F04CC615279A5EA1498609068D3`.
  Los 13 archivos instalados coinciden con el bundle y las regresiones
  `DICT-01`, `UNDO-04` y `EXTENDED-01` pasaron en AutoCAD Core Console.
- La regresión interactiva final de 1.2.5.0 pasó en Civil 3D 2024 sobre un
  dibujo desechable. `AISPELLALL` cargó sin `NETLOAD`, revisó un MText mediante
  `Reglas locales`, produjo dos correcciones de concordancia y cero fallos,
  aplicó la propuesta aprobada y una sola `U` inmediata restauró exactamente
  el texto original.
- El modo local corrige concordancias frecuentes observadas en informes
  técnicos, sin conexión ni consumo de API.
- El proveedor de LanguageTool se ejecuta exclusivamente en `localhost`,
  rechaza direcciones externas y conserva la validación de tokens técnicos.
  El ejecutable local y Java no forman parte del paquete.
- La conexión con LanguageTool local deshabilita proxies, cookies y
  redirecciones HTTP para impedir que un servicio en `localhost` reenvíe texto
  del dibujo hacia otra dirección.
- Las correcciones de LanguageTool local quedan limitadas a 20 000 caracteres,
  igual que sus entradas y las alternativas remotas, antes de calcular el diff
  o mostrarlas en la interfaz.
- La proyección de MText/MLeader oculta los controles de AutoCAD durante el
  análisis gramatical local y conserva sus posiciones para reinsertar cada
  corrección sin perder el formato.
- Incorpora las erratas verificadas en el MText de práctica, incluidas
  confusiones `s/c`, `b/v`, `m/n`, letras omitidas y tildes incorrectas.
- El candidato 1.2.5.0 alcanzó 127/127 pruebas sin llamadas reales a OpenAI.
- El candidato instalado 1.2.4.0 completó la regresión interactiva sobre un
  dibujo desechable: autoload, corrección local, propuesta real `IA` sin fallos,
  aplicación y restauración exacta mediante una sola `U` inmediata.
- La revisión con OpenAI exige ahora una corrección integral en contexto:
  ortografía, tildes, puntuación, concordancia, tiempos verbales, artículos y
  preposiciones. La primera alternativa debe aplicar el cambio completo más
  conservador; una segunda alternativa puede mejorar la redacción técnica sin
  alterar el significado.
- `AISPELLALL` reutiliza durante el lote las respuestas remotas de textos
  idénticos. Cada objeto conserva su fila y escritura independiente, pero no se
  repite la misma solicitud; el consentimiento muestra el número máximo de
  solicitudes únicas.
- Los avisos de OpenAI indican expresamente que la operación consume saldo de
  API y puede generar cobros. Tanto el lote como la prueba de conexión dejan
  seleccionada la respuesta `No` de forma predeterminada.
- `AISPELLALL` muestra una guía solo en la primera ejecución de cada usuario.
  Permite abrir Ajustes para configurar una clave API propia o continuar de
  inmediato con el diccionario local sin internet ni consumo de créditos.
- La configuración explica que cada computadora debe usar una clave autorizada
  para su usuario o proyecto. La clave continúa fuera del archivo de ajustes y
  del instalador; la conexión solo se prueba por acción explícita.
- Ajustes incorpora un campo enmascarado para guardar o retirar la clave propia
  en la variable de entorno del usuario de Windows. Una clave existente nunca
  se muestra y las entradas vacías, excesivas o con espacios se rechazan.

## 1.2.0.0 - 2026-09-08

- Se integra el diccionario abierto Hunspell `es_PE` de LibreOffice/RLA-ES para
  ampliar la corrección local sin conexión. Solo se aplican automáticamente
  tildes inequívocas y erratas de una letra con una única sugerencia; se
  conservan ambigüedades, códigos cortos y nombres en formato interno mixto.
  En textos completamente en mayúsculas, las sustituciones genéricas se omiten
  para proteger apellidos e identificadores; las tildes inequívocas y reglas
  locales verificadas continúan aplicándose.
- El motor administrado `WeCantSpell.Hunspell` se carga una vez por sesión y no
  añade dependencias nativas. El paquete incluye procedencia, huellas, avisos y
  el texto completo de MPL 1.1. RAE/ASALE permanece únicamente como referencia
  normativa: no se redistribuye contenido del DLE ni del DPD.
- La regresión `DICT-01` confirma dentro de AutoCAD Core Console que el motor y
  `es_PE` cargan desde el bundle de compilación y restauran una tilde; la suite
  aumenta a 115 casos y `UNDO-04`/`EXTENDED-01` continúan en PASS.
- La revisión muestra una vista legible de `MText` y `MLeader`, sin exponer los
  controles internos `\P`, `\f`, `\C` y similares. La propuesta y la escritura
  conservan por separado el contenido formateado original; la validación sigue
  bloqueando cualquier alteración de esos controles.
- El motor protege ahora cada control completo de `MText` antes de consultar el
  diccionario. Esto evita falsos positivos dentro de parámetros internos, como
  `Arial Narrow` → `Arial Narro`, y permite aprobar las correcciones del texto
  visible sin modificar fuentes, unidades ni formato de AutoCAD.
- El validador distingue el verbo `ha` de la unidad hectárea: `A SIDO` puede
  corregirse a `HA SIDO` sin un falso bloqueo, mientras cantidades como `25 ha`
  siguen protegidas como unidades técnicas.
- El mensaje posterior a `AISPELLALL` aclara que `U` debe ejecutarse
  inmediatamente, antes de zoom, desplazamiento u otro comando, porque esas
  acciones posteriores ocupan su propio lugar en el historial de AutoCAD.
- Candidato 1.2.0.0: `AISPELLALL` incorpora `MLeader` con contenido `MText`,
  celdas simples editables de `Table`, `DBText`/`MText` de definiciones de
  bloques locales y sus `AttributeReference` por inserción.
- Los textos fijos de una definición de bloque se presentan una sola vez y su
  corrección afecta todas sus inserciones; los atributos se revisan por cada
  referencia. Se omiten Xrefs/overlays, fórmulas, campos, vínculos de datos,
  contenido de bloque en celdas y etiquetas nativas de Civil 3D.
- Nueva regresión `EXTENDED-01` dentro de AutoCAD Core Console: aplica cinco
  cambios sobre un `MLeader`, dos celdas de la misma tabla, texto de bloque y
  atributo, y confirma que una sola `U` restaura el lote completo.
- Las pruebas de Core Console usan un perfil temporal aislado, evitando
  depender del perfil interactivo de Civil 3D y eliminándolo al terminar.
- Se añadieron reglas locales para errores técnicos observados: `geomenbrana`,
  `suvrasante`, `sovrrevestimiento`, `qbofedal`, `investigaccion` y variantes
  de `jeotécnico`. La regresión conserva `LUDENA` como nombre propio y produce
  `BOFEDAL` e `INVESTIGACIÓN GEOTÉCNICA` en el caso real observado.
- La errata `tuvería`/`tuverias` observada en un `MLeader` se corrige a
  `tubería`/`tuberías` sin alterar `HDPE`, relaciones numéricas ni unidades.
- La revisión visual del DWG amplió el vocabulario local con encabezados,
  tildes y errores observados en tablas/notas, entre ellos `uvicación`,
  `ubicacion`, `numero`, `titulo`, `revision`, `emision`, `geologia`, `detaye`,
  `conección`, `a sido`, `huvicar`, `triquetados`, `limites`, `construccion`,
  `deberan`, `subterranea`, `perimetro`, `lixiviacion`, `identificacion`,
  `lineas` y `evaluacion`. Dos regresiones reproducen los textos anonimizados
  de la captura y conservan mayúsculas, números y códigos técnicos.
- Se retiraron del control de versiones las salidas generadas de `bin/` y
  `obj/`. Permanecen ignoradas y se regeneran durante la compilación, evitando
  cambios binarios accidentales en futuros commits.
- Se sincronizó la documentación operativa con la instalación y publicación
  verificadas de 1.1.5.0 y con la cobertura automatizada de `UNDO-04`.

## 1.1.5.0

- `AISPELLALL` indica ahora la orden inmediata `U` en vez de
  la instrucción ambigua `UNDO`; una sola ejecución revierte el lote completo.
- Nueva regresión de integración en AutoCAD Core Console que modifica dos
  entidades mediante la misma frontera de lote, ejecuta una sola `U` y verifica
  la restauración exacta de ambas. La prueba usa y elimina una copia temporal,
  por lo que nunca modifica el fixture base aunque falle.
- Los comandos auxiliares de esa regresión solo existen en Debug y la validación
  Release comprueba que no se distribuyan.
- La actualización administrada y la regresión interactiva pasaron: tres
  correcciones aplicadas, instrucción `U` visible y reversión completa confirmada.
- Prerelease publicado con ZIP y checksum verificables bajo la etiqueta
  `1.1.5.0`, asociada al commit
  `30856f7e825ff1221558ac2db6727ae17f4061ed`.

## 1.1.4.0

- Prerelease interno publicado con ZIP y checksum verificables bajo la etiqueta
  `1.1.4.0`, asociada al commit
  `787d824fe932543b2787dfc2b41bfbbad02d6609`.
- El runbook distribuido usa ahora la ruta real del generador de fixture dentro
  del ZIP y reinstala el candidato autorizado 1.1.4.0 después de comprobar el
  ciclo de vida, evitando continuar la regresión con una versión obsoleta.
- El verificador de entrega comprueba ambas instrucciones y sus pruebas
  negativas rechazan una ruta de fixture inválida o una versión de
  reinstalación obsoleta aunque el ZIP tenga un checksum recalculado.
- La fuente de verdad registra correctamente que el autoload final ya quedó en
  PASS junto con el recorrido de accesibilidad del 2026-09-03.
- Candidato 1.1.4.0 con 105/105 pruebas en Debug y Release y paquete
  reproducible. El foco de teclado selecciona ahora la fila correspondiente en
  revisión por lote y memoria, y los estados, resúmenes y bloqueos exponen
  nombres o descripciones accesibles.
- La validación del corte comprueba estáticamente las garantías XAML de foco,
  estado anunciable y ayuda de validación en las cuatro ventanas WPF.
- La actualización real 1.1.3.0 → 1.1.4.0, rollback, desinstalación y
  reinstalación administrada pasaron conservando sin cambios los datos locales;
  el validador de ciclo comprueba hashes en cada transición.
- El empaquetador genera un archivo `.zip.sha256` estándar junto a la entrega y
  el LEAME incluye una comprobación directa antes de extraer o instalar.
- Un nuevo control de publicación valida el checksum, rutas seguras, entradas
  obligatorias, manifiesto, versión de DLL, plataforma, comandos y ausencia de
  ensamblados Autodesk o PDB dentro del ZIP.
- Una prueba aislada del control de publicación confirma el caso válido y
  demuestra que un nombre incoherente, una huella SHA-256 alterada o un archivo
  no previsto detienen la entrega.
- Candidato 1.1.3.0 con 105/105 pruebas en Debug y Release. Las reglas locales
  preceden ahora a recuerdos duplicados, conservando idioma y origen correctos.
- Una regla local confiable puede corregir una palabra hacia un término del
  glosario sin que el validador la bloquee; IA y edición manual conservan la
  validación estricta de términos protegidos.
- El preflight advierte explícitamente cuando una DLL sin firma instalada por
  usuario puede requerir autorización de `SECURELOAD`, incluyendo el flujo
  verificable de primera carga.
- El alcance administrado instala ahora bajo `Program Files`, ubicación que
  Autodesk confía implícitamente, para evitar avisos recurrentes de una DLL sin
  firma en el perfil del usuario.
- Preflight e instalador detectan bundles en el alcance opuesto; la migración
  conserva primero ambas copias hasta verificar la administrada y bloquea crear
  después una copia por usuario que pueda ocultarla.
- El preflight convierte la falta de elevación para `Program Files` en un
  resultado accionable de la tabla en vez de terminar con una excepción.
- El instalador y el runbook explican la recarga única con `APPAUTOLOADER` cuando
  una migración de alcance deja en caché la ruta anterior del mismo paquete.
- La cancelación durante la preparación global quedó revalidada con el escenario
  `SlowSuccessful`: no abrió la revisión ni modificó textos.
- Candidato 1.1.2.0 con 103/103 pruebas en Debug y Release, restauración de la
  prueba de invariantes del lote y conteo único usado por validación y paquete.
- Ajustes solo suprime el evento diagnóstico de cierre después de confirmar que
  el borrado del registro terminó correctamente.
- Los scripts con mensajes en español usan UTF-8 con BOM para mostrarse
  correctamente en Windows PowerShell 5.1.
- La validación Release impide publicar comandos de diagnóstico y el
  empaquetador rechaza scripts incompatibles con la codificación de PowerShell
  5.1.

## 1.1.1.0

- Corte de mantenimiento 1.1.1.0 para empaquetar las mejoras de accesibilidad,
  manejo de errores locales y exportación diagnóstica segura.

- Consolidación en un único proyecto y eliminación del prototipo duplicado.
- Confirmación explícita antes de aplicar correcciones.
- Detección de conflictos si el texto cambia entre lectura y escritura.
- Ampliación de reglas conservadoras y de la cobertura automatizada.
- Contratos de dominio, proveedor local, diff y validación de tokens técnicos.
- Coordinador de revisión que normaliza, deduplica y valida propuestas.
- Ventana WPF modal con original, idioma, propuesta, diff y advertencias.
- Aplicación segura mediante snapshot y transacción corta revalidada.
- Veinticuatro pruebas del motor, dominio, coordinador y ViewModel.
- Configuración JSON versionada y glosario personal por usuario.
- Proveedor de IA simulado con éxito, timeout, indisponibilidad, respuesta
  inválida y alteración técnica bloqueada.
- Estados de carga, error, reintento y cancelación integrados en la revisión.
- Comando `AISPELLSETTINGS` para administrar la simulación y el glosario.
- Treinta pruebas automatizadas del flujo local y simulado.
- Comando `AISPELLALL` para revisar todos los `DBText`/`MText` del dibujo.
- Lista global con selección por fila y propuesta válida más completa.
- Escritura atómica del lote con revalidación y un único `UNDO`.
- Treinta y dos pruebas automatizadas.
- Proveedor real de OpenAI para español e inglés mediante Responses API y
  salida JSON estructurada.
- Consentimiento explícito y envío exclusivo del contenido de `DBText`/`MText`;
  no se envían archivos, geometría ni metadatos del dibujo.
- Clave leída desde `OPENAI_API_KEY`, `store: false` y modelo configurable.
- Protección adicional de códigos de formato MText y 35 pruebas automatizadas.
- Limpieza de artefactos generados y documentación obsoleta.
- Confirmación del flujo feliz de OpenAI real en `AISPELL` y `AISPELLALL`.
- Consolidación del estado verificable del MVP, matriz de regresión y riesgos
  pendientes en una única fuente operativa.
- Definición del segundo alcance con prioridad en estabilización, pruebas de
  integración, diagnóstico seguro, distribución y experiencia de usuario.
- Validador reproducible del MVP con huella SHA-256, comprobación de 44 pruebas,
  glosario y exclusión de DLL de Autodesk.
- Runbook de regresión, fixtures anonimizados y plantilla única de incidencias.
- Comandos de diagnóstico exclusivos de Debug para verificar conflicto
  individual y atomicidad del lote sin escribir cuando la protección funciona.
- Escenario simulado lento y prueba automatizada de cancelación durante carga.
- Corrección del espacio de alternativas para mostrar y seleccionar con mouse
  hasta tres propuestas; REG-2026-001 cerrada tras revalidación manual.
- Cierre del Hito 1 de regresión: matriz manual completa, cancelación lenta sin
  escrituras tardías y cero defectos críticos abiertos.
- Frontera sustituible de escritura atómica y adaptador Autodesk sin cambios en
  los comandos públicos.
- Ocho pruebas de integración para documento/objeto/tipo/texto, lote vacío,
  conflicto parcial, rollback antes del commit y commit atómico; 44/44 pruebas.
- Revalidación del adaptador en Civil 3D: conflictos seguros, aplicación MText
  con formato preservado y reversión exacta mediante `UNDO`.
- Frontera sustituible de selección y documento: los comandos ya no coordinan
  directamente `Document`, `Editor`, `Database` ni `ObjectId`.
- Bloqueo previo de selección y escritura cuando el dibujo capturado se cerró o
  dejó de ser el documento activo.
- Protección frente a respuestas tardías o sustituidas del proveedor durante
  cancelación, cierre y reintento.
- Dataset anonimizado versionado con español, inglés, texto mixto, MText largo,
  códigos de formato, estaciones, unidades y repeticiones.
- Cincuenta y dos pruebas automatizadas del Hito 2.
- Cierre del Hito 2 tras regresión interactiva completa del nuevo adaptador:
  selección, lote/UNDO, cancelación lenta, conflictos y cambio de documento en
  PASS, sin escrituras tardías ni parciales.
- Primer corte del Hito 3 con catálogo estable de diagnóstico, clasificación de
  fallos, registro local optativo, esquema de configuración 3, rotación a dos
  archivos, exportación revisable y borrado confirmado desde Ajustes.
- Siete pruebas de diagnóstico y privacidad; suite total 59/59.
- Ajuste responsivo de `AISPELLSETTINGS` con desplazamiento vertical para que
  Guardar y Cancelar permanezcan accesibles en pantallas de menor altura.
- Exportación diagnóstica sin selector innecesario cuando el registro está vacío
  y borrado que no se revierte al cerrar la misma sesión de configuración.
- Cierre del Hito 3 tras reproducir un timeout como `TMO-001` y verificar que
  el registro mantiene únicamente los siete campos permitidos, sin contenido
  del texto ni datos del dibujo.
- Primer paquete piloto 1.0.0.0 como Autodesk Application Bundle para Civil 3D
  2024 x64, con carga por invocación y solo los tres comandos públicos.
- Construcción Release reproducible con validación de versión, plataforma,
  comandos, rutas y exclusión de DLL de Autodesk.
- Preflight de plataforma, .NET, permisos, configuración y credencial opcional
  sin revelar su valor.
- Instalación, actualización, rollback y desinstalación administradas con
  copias recuperables; ciclo completo validado en directorios aislados.
- Corte 1.1.0.0 con edición manual sometida al mismo diff y validador técnico.
- Preparación masiva con progreso y cancelación, búsqueda, filtros por layout,
  entidad, estado y origen, y elección de alternativa por fila.
- Selector de modelos OpenAI admitidos y confirmación previa con volumen y
  advertencia de coste antes de enviar un lote.
- Memoria local esquema 1, opt-in y nunca autoaplicada, con búsqueda,
  activación, exportación y borrado desde Ajustes.
- Glosario organizacional de solo lectura bajo `%PROGRAMDATA%`, combinado
  localmente con los términos integrados y personales.
- Mejoras iniciales de foco, teclado y nombres accesibles en las ventanas WPF.
- Diecisiete pruebas nuevas; suite total 76/76.
- Prueba de conexión OpenAI voluntaria con frase fija, advertencia de coste y
  sin persistencia de ajustes ni respuesta.
- Cancelación visible de la prueba de conexión, estado anunciable y regresión
  automatizada; suite total 77/77.
- Las respuestas tardías de proveedores que ignoran la cancelación ya no pueden
  convertir una prueba cancelada o una ventana cerrada en un éxito; suite total
  78/78.
- La prueba de conexión mueve el foco al control válido durante cada transición
  y comunica la cancelación mediante el estado anunciable, sin abrir un diálogo
  modal redundante.
- Los fallos de la prueba de conexión ofrecen orientación segura y códigos de
  soporte estables sin mostrar mensajes internos de excepción; suite total
  79/79.
- Una frontera de cancelación común devuelve el control aunque un proveedor o
  coordinador ignore el token, observa las tareas abandonadas y descarta sus
  resultados tardíos en revisión individual, global y prueba de conexión;
  suite total 81/81.
- La revisión individual contiene excepciones inesperadas durante carga o
  reintento, conserva las alternativas locales y muestra códigos de soporte sin
  detalles internos; los fallos conocidos también se presentan de forma segura;
  suite total 82/82.
- La preparación global enfoca inicialmente su acción de cancelación y el
  reintento individual conserva el foco en un control habilitado durante carga,
  éxito y nuevo fallo.
- Ajustes vuelve a comprobar `OPENAI_API_KEY` justo antes de probar la conexión,
  actualiza el estado visible y evita mostrar la confirmación de coste cuando la
  credencial aún no está configurada.
- La exportación de memoria rechaza explícitamente su propio archivo interno y
  comprueba que un destino inválido no altera los recuerdos guardados.
- La carga de memoria descarta registros incompletos e IDs repetidos,
  reconstruye claves e idiomas, combina decisiones duplicadas sin desbordar
  conteos y limita a 500 elementos antes de exponerlos a Ajustes.
- La configuración local intenta recuperar, en orden, el archivo actual, una
  escritura temporal interrumpida y los esquemas v2/v1 antes de volver a los
  valores predeterminados.
- El glosario personal y la memoria local también recuperan sus temporales tras
  una escritura interrumpida; la memoria solo acepta el temporal si su esquema
  y contenido pueden deserializarse y normalizarse.
- Borrar toda la memoria elimina también cualquier temporal recuperable, y la
  exportación serializa los registros validados en vez de copiar un archivo
  principal corrupto; tanto el archivo interno como su temporal están
  protegidos como destinos.
- El cálculo de diferencias limita la matriz cuadrática a un millón de celdas;
  textos largos degradan a un único segmento visible sin omitir la validación
  de números, unidades, estaciones, códigos, glosario o formato.
- El proveedor OpenAI rechaza respuestas mayores de 200 000 caracteres y
  alternativas que superen el límite seguro de entrada antes de procesarlas
  como correcciones.
- La validación técnica amplía unidades protegidas a fuerza, presión, caudal,
  velocidad, área, volumen, masa y ángulo usadas en documentación civil, además
  de las unidades básicas existentes.
- La escritura atómica por lote rechaza cualquier operación sin cambios antes
  del primer intento de escritura y conserva intacto el resto del lote.
- Los fallos de comandos y almacenamiento muestran únicamente orientación y
  códigos de soporte; el clasificador recupera la causa útil incluso cuando
  viene envuelta en otra excepción.
- El transporte OpenAI usa lectura en streaming con límite estricto y timeout
  propio, evitando almacenar respuestas remotas de tamaño no acotado; suite
  total 89/89.
- Filtro de validación incorporado a la búsqueda del lote.
- Endurecimiento posterior del XAML para alto contraste y escalado: colores del
  sistema, ajuste de píxeles y eliminación de colores fijos en las cuatro
  ventanas.
- Etiquetas y estados anunciables para tecnologías de asistencia, teclas de
  acceso adicionales y asociación explícita entre etiquetas y controles.
- El escenario del proveedor simulado queda deshabilitado visualmente mientras
  dicho proveedor está apagado.
- Las acciones de exportar o borrar toda la memoria local se deshabilitan
  cuando no existen preferencias guardadas.
- Los fallos esperados de permisos o escritura en Ajustes se contienen y se
  explican al usuario sin propagarse al host de Autodesk.
- La exportación diagnóstica rechaza el propio archivo interno como destino y
  evita truncar accidentalmente el registro original.
- Generador reproducible de un DWG desechable con cuatro casos `TEXT`/`MTEXT`,
  ejecutado y validado mediante AutoCAD Core Console sin tocar dibujos reales.
- Empaquetado ZIP determinista mediante orden estable y fechas internas fijas;
  dos builds idénticos producen la misma huella SHA-256.

## v1.0
- Plugin AISPELL funcional.
- Selección de texto.
- Corrección y reemplazo en Civil 3D.
