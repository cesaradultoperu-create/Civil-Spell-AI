# Current Status

Última verificación automatizada: 2026-10-14.

## Corte actual

- 2026-10-14: siete trabajos archivados individualmente por petición del usuario en
  `G:/01. PERSONALES/Cesar/Programas con el Visual Studio para el CIVIL3D`.
  Incluye programas, fuentes, guías y ejemplos; 331/331 archivos copiados y
  verificados por SHA-256. Abrir INDICE.html en esa carpeta. BORDETIN3D 0.1.3
  confirmado por el usuario para exterior y dos interiores Pick Points. No se
  recompilaron ni instalaron los paquetes. Detalles y límites de cada trabajo:
  `SESSION_2026-10-14_ARCHIVO_PROGRAMAS.md`.

- 1.3.7.0 **instalado** el 2026-10-12 después de «cerrado». Se comprobó que
  acad/accoreconsole no estaban ejecutándose y se instaló el bundle completo.
  Verificación independiente: 49/49 archivos por SHA-256, sin duplicado por
  usuario. Respaldo íntegro 1.3.6.0, 49/49 archivos:
  `C:/Users/Cesar Ugaz/AppData/Local/CivilSpellAI/deployment-backups/AllUsers/backup-20261012-104155-909`.
  Comprobante: `artifacts/validation/figure3d/reference-installation-1.3.7-20261012.json`.
  Siguiente paso: abrir Civil 3D y probar CREARFIGURA3D con la pareja central
  y después la superior. Puede probar el plano en solo lectura; guardar el
  resultado requiere GUARDARCOMO con otro nombre. No reinstalar para empezar.

- 1.3.7.0 preparado, **sin instalar**. Se reprodujeron y corrigieron los
  rechazos de la pareja central (eje/bordes con extremos distintos) y superior
  (tubo perpendicular a FRONT representado por círculo, canales a ambos lados).
  Las tres parejas del plano actual pasan geometría nativa, escala/traslado,
  tubo hueco, vaciado circular del muro, resaltado, edición Y y reapertura.
  Regresión anterior A+B/C+D correcta. Release 2024/2025/2026 sin advertencias,
  151 pruebas y verificador de ZIP correctos. Entrega:
  `Test/Drawing3D/Reconocimiento-TOP-FRONT-20261012/`.
  Instalación activa sigue siendo 1.3.6.0; no actualizar mientras Civil 3D
  permanezca abierto. Leer `SESSION_2026-10-12_TOP_FRONT.md`.
  Un corte circular aislado no determina la pendiente longitudinal: el editor
  explica la representación a cota constante y permite revisar cotas Y/Z.

- El propietario confirmó que el resaltado funciona perfectamente y mostró
  «Mampostería: fondo / Cota inferior» con la pieza correcta en verde.
  Comprobación interactiva del resaltado 1.3.6.0 confirmada. Pregunta actual:
  requisitos TOP/FRONT y alcance de reconstrucción para otros dibujos.

- Actualización de resaltado completada tras «civil3d CERRADO»: 1.3.6.0
  instalado, 49/49 archivos verificados por SHA-256 y sin bundle duplicado
  por usuario. Respaldo íntegro de 1.3.5.0, también 49 archivos verificados:
  `C:/Users/Cesar Ugaz/AppData/Local/CivilSpellAI/deployment-backups/AllUsers/backup-20261006-105627-712`.
  Comprobante: `artifacts/validation/figure3d/reference-highlight-installation-1.3.6-20261006.json`.
  Abrir Civil 3D y probar EDITARFIGURA3D: seleccionar el campo de laterales,
  cambiar de componente y quitar el resaltado. Registro:
  `docs/codex/SESSION_2026-10-06_RESALTADO.md`.

- 1.3.6.0 preparado y validado: el propietario pidió iluminar en la vista 3D
  el componente del campo seleccionado. Los laterales, el fondo, la tubería y
  las piezas sin campos se identifican individualmente por foco/clic; se conserva
  el resaltado al editar y se puede quitar. A+B/C+D pasan prueba nativa,
  edición/una U y reapertura. Release 2024/2025/2026 sin advertencias, 151 pruebas
  correctas y verificador del ZIP correcto. Paquete:
  `Test/Drawing3D/Resaltado-Componentes-20261006/`.
  Instalación activa sigue 1.3.5.0; Civil 3D abierto (acad 9548 al preparar).
  Instalar el paquete completo cuando el propietario confirme el cierre.
  Sus capturas ya muestran creación C+D y apertura posterior en EDITARFIGURA3D.
  Registro: `docs/codex/SESSION_2026-10-06_RESALTADO.md`.

- 1.3.5.0 instalado el 2026-10-06 tras el aviso «cerrado civil3d»: 49/49
  archivos verificados por SHA-256 para 2024/2025/2026, manifiesto 1.3.5.0 y
  ninguna copia duplicada por usuario. Respaldo de 1.3.4.0 en
  `C:/Users/Cesar Ugaz/AppData/Local/CivilSpellAI/deployment-backups/AllUsers/backup-20261006-103601-819`.
  Comprobante: `artifacts/validation/figure3d/reference-CD-installation-1.3.5-20261006.json`.
  Siguiente paso: repetir CREARFIGURA3D con C como TOP y D como FRONT para
  comprobar el flujo interactivo con la corrección ya instalada.

- Nueva ampliación 1.3.5.0 preparada, todavía sin instalar: el propietario
  recordó que CREARFIGURA3D debe generar otros ejemplos y señaló C (TOP) y D
  (FRONT). Se corrigió la orientación fija del reconocimiento: tubería a ambos
  lados y horizontal/inclinada, canal opuesto y componentes interiores cuando
  sus perfiles se relacionan. A+B y C+D pasan Core Console, edición/una U y
  reapertura; C+D pasa también desplazado y a escala 1.5. Release 2024/25/26
  pasa 151 pruebas sin advertencias. Paquete de revisión:
  `Test/Drawing3D/Reconstruccion-A-B-C-D-20261006/`. La instalación sigue en
  1.3.4.0 mientras Civil 3D permanece abierto. Material superior del elemento
  interior C+D pendiente de aclaración. Registro:
  `docs/codex/SESSION_2026-10-06_C_D.md`.

- Actualización completada el 2026-10-06 después de que el propietario indicó
  que Civil 3D estaba cerrado. Bundle 1.3.4.0 instalado para 2024/2025/2026;
  49/49 archivos coinciden por SHA-256 con el paquete validado. Sin copia
  duplicada por usuario. La instalación anterior 2024 1.3.3.0 queda en
  `C:/Users/Cesar Ugaz/AppData/Local/CivilSpellAI/deployment-backups/AllUsers/backup-20261006-091753-423`.
  Comprobante: `artifacts/validation/figure3d/reference-installation-20261006.json`.
  Siguiente paso: abrir el DWG de revisión y probar EDITARFIGURA3D y la selección
  TOP/FRONT, comparar el canal con layout A y revisar giro/zoom/cancelación.

- Sesión retomada 2026-10-06: Release 1.3.4.0 regenerado con cama de arena,
  151 pruebas y compilaciones 2024/2025/2026 sin advertencias. La prueba nativa
  2024 comprueba también las cotas del canal y el volumen hueco del tubo.
  Comparación con la extrusión aproximada y paquete de revisión completos en
  `Test/Drawing3D/Reconstruccion-TOP-FRONT-20261006/`, con DWG, PNG/PDF, ZIP y
  actualizador del bundle completo. Los tres comandos 3D figuran en el manifiesto
  y el verificador rechaza su omisión. Instalación 2024 1.3.3.0 conservada.
  Pendientes: revisión visual del layout A y prueba interactiva de selección,
  giro/zoom, cancelación y edición después de actualizar con Civil 3D cerrado.
  Detalles: `docs/codex/SESSION_2026-10-06_TOP_FRONT.md`.

- Fin de jornada 2026-10-05: el propietario pidió guardar la información y
  continuar mañana. Estado preciso, artefactos y pendientes en
  `docs/codex/SESSION_2026-10-05_TOP_FRONT.md`. El último añadido de cama de arena
  pasa Debug/nativo en `reference-complete-20261005`; requiere regenerar Release
  y bundle antes de instalar. La carpeta de revisión contiene DWG/PNG y aún
  no tiene instalador final. La instalación activa anterior sigue conservada.

- 2026-10-05: se implementó la reconstrucción por componentes desde TOP/FRONT.
  Se analizó la extrusión aproximada y el layout A del DWG abierto. Un WBLOCK
  conserva su estado sin guardar el original. `CREARFIGURA3D` propone
  Referencias y reconoce el detalle de drenaje con canal/salientes GRAVEL,
  solado de 0.05, caja de 0.80 y tubo circular con pendiente derivada de ambas
  vistas. Componente acepta perfiles cerrados arbitrarios, arcos y vacíos;
  Caja mantiene compatibilidad anterior. La reconstrucción general de un
  ensamblaje desconocido requiere correspondencias por pieza; no se declara
  inferencia universal desde dos vistas. Tubo sin espesor conocido: superficie
  abierta; con espesor positivo: sólido hueco. Guía actual:
  `docs/testing/REFERENCE_ASSEMBLY_3D_RUNBOOK.md`. Candidato 1.3.4.0 preparado,
  sin instalar mientras Civil 3D está abierto. Core Console verifica la
  referencia actual, perfiles L/circulares con vacíos y FRONT inclinado,
  malla nativa, edición y reapertura; Release 2024/25/26 pasa 151 pruebas.

Los puntos siguientes conservan el historial anterior a la reconstrucción
por componentes y no describen el alcance actual.

- El propietario aclaró que el resultado esperado es reconstruir el diseño
  completo relacionando TOP y FRONT, no solo mejorar la vista de una caja.
  Va a preparar una extrusión aproximada en el DWG de pruebas como referencia
  de forma. El analizador actual reduce TOP a un marco exterior/interior y
  FRONT a un perfil en U, por lo que la fidelidad geométrica sigue sin resolver.
  Las correcciones de sombreado/contornos no cierran este requisito.
  Próximo trabajo: identificar componentes y correspondencias entre vistas,
  mantener contornos de planta y perfiles/alturas de elevación y contrastar
  el resultado con la referencia manual. Pedir solo medidas o relaciones
  que no queden determinadas por las vistas; no inventar piezas o dimensiones.

- Con Civil 3D cerrado se comprobó que la instalación 2024 ya contiene
  la versión 1.3.3.0, SHA-256
  `871DA3C22B00EC26C66C27BEAAC3A028435DA5D457540734013DCC828DC51719`.
  Coincide con el Release validado de 151 pruebas e incluye la corrección
  3D de superficies y contornos. Se invocaron Build y BuildEdges desde la
  propia DLL instalada y se verificaron normales, aberturas y altura 0.80.
  Se conservó esa instalación; no se reemplazó por los paquetes 1.3.2.0
  preparados antes. Comprobante:
  `artifacts/validation/figure3d/preview-fix-installation-20261005.json`.

- Se verificó además la vista corregida con contornos de aristas reales,
  omitiendo las líneas de subdivisión de la malla. La referencia izquierda
  de la captura conserva altura 0.80 y paso izquierdo a cota 0.40; la pareja
  central conserva altura 0.95. Debug/Release 2024/2025/2026 pasa 151/151,
  sin advertencias. Core Console verifica ambas referencias, volumen de
  malla contra sólido, coronación completa, cotas, edición, U y reapertura.
  Evidencia final: `artifacts/validation/figure3d/preview-edges-native-20261005/`
  y `preview-edges-release-20261005.md` de la misma carpeta padre.
  Paquete con vista renderizada y dibujo de prueba:
  `Test/Drawing3D/Correccion-3D-Contornos-20261005`.
  DLL 2024 SHA-256:
  `934680539B35241E36546DA6A080017B61072BF0592F78013E9D4679FC26C44F`.
  Preparado para actualizar después de guardar y cerrar Civil 3D.
  Sigue pendiente precisar si se requieren también los conductos externos.

- Corrección de vista 3D del 2026-10-05 preparada, sin instalar por petición
  del propietario (la aplicará el lunes). La captura sí contiene la pareja
  TOP/FRONT izquierda; reconoce 1.00 × 1.30 × 0.80, base y muros 0.15.
  El sombreado compartido entre caras y fragmentos creaba falsas hendiduras
  y ranuras en la coronación. La nueva malla expone solo la frontera del cuerpo,
  con normales independientes por cara. Core Console compara su volumen y
  área superior con el sólido nativo, edición, copias, U y persistencia.
  Release 2024/2025/2026 pasa 151/151 pruebas. Paquete:
  `Test/Drawing3D/Correccion-Vista-3D-20261005`.
  La corrección representa caja y aberturas: no añade conductos ni tapa.
  Se consultó si el propietario también quiere reconstruir esos componentes;
  la respuesta sigue pendiente. No considerar cerrada la fidelidad de la
  figura completa sin esa confirmación y sus medidas geométricas.

- El 2026-10-05 se retomó la corrección de creación 3D del viernes.
  Debug 2024/2025/2026 compila sin advertencias y pasa 151/151 pruebas.
  Core Console 2024 vuelve a verificar TOP/FRONT, sólido nativo, volumen,
  cotas, edición, copias independientes, giro, rollback, una U y
  persistencia. La DLL Release instalada conserva la huella del viernes
  `1559FAB0C2E560D60D35128E2A1F6BE93ED4EE0110E21885D1C14B312E3FBDAC`.
  Evidencia: `artifacts/validation/figure3d/resume-debug-20261005.md` y
  `artifacts/validation/figure3d/resume-native-isolated-20261005/`.
  El primer intento nativo no abrió el fixture por una restricción de acceso
  al registro; la repetición con perfil aislado y acceso autorizado pasó.
  Pendiente: precisar con el propietario el error o la mejora interactiva y
  reproducirla. Esta revalidación no cambia el código ni la instalación.

- El propietario confirmó 0.10 como medida inicial editable de los pasos
  laterales 3D. El paso derecho ya comienza activo con ancho 0.10, aunque TOP
  no lo acote. Ancho, alto, centro y cota inferior siguen siendo parámetros
  independientes. Core Console verifica la edición a 0.20 × 0.12, volumen,
  cotas, copia independiente, una U y persistencia de todas las medidas.
  Release 2024/2025/2026 compila sin advertencias y pasa 151/151 pruebas.
  Paquete actualizado: `Test/Drawing3D/Pruebas-3D-Editable-20261002`.
  El propietario confirmó el cierre de Civil 3D y se instaló la DLL 2024.
  Fuente e instalación coinciden por SHA-256
  `1559FAB0C2E560D60D35128E2A1F6BE93ED4EE0110E21885D1C14B312E3FBDAC`.
  La DLL anterior queda verificada en
  `artifacts/drawing3d-backups/CivilSpellAI-2024-084bfb750a584ed3a953ae05afddc1f9.dll`.
  Comprobante: `artifacts/validation/figure3d/editable-010-installation.json`.
  Pendiente: recorrido interactivo después de abrir de nuevo Civil 3D.

- El 2026-10-02 se completó la creación de una caja rectangular 3D editable
  desde TOP y FRONT: `CREARFIGURA3D`, `EDITARFIGURA3D`, integración con
  `EDITARSECCION` y botón **Crear 3D con FRONT…**. El P-011 aporta ancho 1.00,
  profundidad 1.30, altura 0.95 y base 0.15. TOP no define el ancho del paso
  derecho: queda pendiente de entrada manual. Los bloques guardan parámetros,
  un sólido nativo y cotas XY/XZ; las copias se editan independientemente.
  Release 2024/2025/2026 y Debug 2024 compilan sin advertencias, pasan las
  151 pruebas generales, el ensayo nativo 3D y la regresión 2D. Se verificó
  volumen, cotas, copia, giro, rollback, una U, persistencia y renderizado WPF.
  Paquete: `Test/Drawing3D/Pruebas-3D-20261002`. Pendiente: instalar tras cerrar
  Civil 3D y recorrer el flujo interactivo. Alcance y guía:
  `docs/testing/FIGURE_3D_TOP_FRONT_RUNBOOK.md`.

- El 2026-10-02 el propietario confirmó que la edición de medidas interiores
  alcanzó el resultado esperado en Civil 3D 2024. Se añadió zoom, desplazamiento
  y encuadre a la vista previa de `EDITARSECCION`. Debug/Release compilan las
  tres variantes sin advertencias; pasan 151/151 pruebas y la regresión de
  medidas en Core Console 2024, incluida navegación y renderizado ampliado.
  Paquete: `Test/Drawing3D/Pruebas-Vista-20261002`. Pendiente: instalar después
  de cerrar Civil 3D y comprobar los gestos de ratón en la ventana real.

- La prueba técnica extensa informada por el propietario quedó incorporada
  como regresión de sus 24 omisiones. La ruta offline corrige cada aparición
  dentro de un contexto comprobado y una segunda revisión no produce cambios.
  Los cuatro remanentes finales (`calculo sin tilde`, `haci`, `devido` y
  `alguno ... están mal configurados`) también quedan cubiertos. `AISPELLALL`
  La prueba de excavación añade cobertura para `SANJA`, `ESCAVACION`,
  `ESSEDENTE` y `ALINIAMIENTOS`. El informe de movimiento de tierras añade
  `se realizo`, `se verifico`, `polilineas` y `actualisacion`. `AISPELLALL`
  El informe difícil en mayúsculas cubre además diez omisiones de B/V, C/S,
  H, LL/Y y tildes. `AISPELLALL` identifica esta compilación como `revisión
  validación automatizada 150/150`. Debug y Release compilan las DLL 2024/2025/2026 sin
  advertencias y pasan 150/150 pruebas.
- La prueba interactiva del 2026-09-17 confirmó las 24 guías, el cambio B/C al
  cruzar el centro del texto, el ángulo de 15°, la cancelación exacta y una sola
  `U`. También detectó que la ayuda solo se activaba después de invocar
  `AISPELLSETTINGS`: el manifiesto usaba `LoadReasons` en vez del parámetro
  válido `LoadOnAutoCADStartup="True"`. La fuente y los verificadores del
  paquete ya exigen la sintaxis correcta. Tras reinstalar 49/49 archivos, el
  reinicio confirmó que las guías aparecen directamente, sin ejecutar antes
  ningún comando de CivilSpellAI.
- El 2026-09-17 se completó la base de la ayuda angular para la flecha de
  `MLeader`: guías transitorias cada 15°, ajuste al ángulo más cercano y radio
  constante alrededor del vértice del lado del texto. Los eventos restauran
  `AUTOSNAP` y `ORTHOMODE` al finalizar o cancelar, y contienen cualquier fallo
  durante el arrastre. Debug y Release compilan las tres variantes sin
  advertencias y pasan ahora 150/150 pruebas. Falta completar `MLP-02`,
  `MLP-06` y `MLP-07` en Civil 3D real.
- El candidato actual es 1.3.0.0. Compila una DLL .NET Framework 4.8 para Civil
  3D 2024 y DLL .NET 8 separadas para Civil 3D 2025 y 2026. El manifiesto del
  bundle registra `R24.3`, `R25.0` y `R25.1` y dirige cada producto a su carpeta
  `Contents/Windows/2024`, `2025` o `2026`. Las tres variantes compilan sin
  advertencias y las 150 pruebas del núcleo pasan. El ZIP reproducible anterior tiene
  SHA-256 `7030D87B2F3AC49662498C868FB5DCD78ACA825CC44DB801DC5690B9C3043BD2`.
  El 2026-09-18 se instaló el candidato actualizado en alcance `AllUsers`:
  49/49 archivos coincidieron con el paquete y la versión anterior quedó
  respaldada antes de la sustitución.
  Falta confirmar la carga y los comandos en instalaciones reales de Civil 3D
  2025 y 2026.

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
