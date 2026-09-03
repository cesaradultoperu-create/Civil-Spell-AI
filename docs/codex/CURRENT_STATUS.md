# Current Status

Última verificación: 2026-09-03.

## Corte actual

- El candidato CivilSpellAI 1.1.5.0 compila para .NET Framework 4.8/x64 sin
  advertencias.
- La suite autocontenida tiene 105 pruebas deterministas y no usa Autodesk ni
  OpenAI real.
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
- El prerelease público 1.1.4.0 contiene el ZIP y checksum finales; su etiqueta
  apunta al commit `787d824fe932543b2787dfc2b41bfbbad02d6609`:
  https://github.com/cesaradultoperu-create/Civil-Spell-AI/releases/tag/1.1.4.0.
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
- La validación del candidato 1.1.5.0 pasó en Debug y Release con 105/105
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

La 1.1.5.0 está instalada de forma administrada, coincide con el candidato y
pasó `UNDO-04` automatizado e interactivo. Autoload, mensaje y reversión del
lote quedaron confirmados en Civil 3D 2024. El artefacto final reproducible y
su verificación independiente están cerrados; el fixture también quedó intacto.
Solo con autorización expresa queda preparar commit, etiqueta y prerelease. El
prerelease público 1.1.4.0 permanece sin cambios.

## Fuera del alcance actual

- Atributos de bloque, tablas, etiquetas nativas de Civil 3D, referencias
  externas y texto anidado.
- Firma con certificado de una autoridad confiable; el piloto interno actual es
  sin firma, instalado de forma administrada en `Program Files`, limitado a
  equipos controlados y verificado por hash.

Fuente de verdad: `docs/product/10_MVP_STATUS_AND_TEST_PLAN.md`.
