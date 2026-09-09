# Auditoría técnica

## Fuente posterior al candidato 1.2.4.0 (2026-09-09)

- El transporte preparado para LanguageTool local deshabilita proxy, cookies y
  redirecciones HTTP. Aunque el punto inicial sea `localhost`, ninguna respuesta
  puede redirigir el contenido del dibujo hacia otra dirección.
- Las correcciones locales superiores a 20 000 caracteres se rechazan antes de
  calcular el diff o alcanzar la interfaz.
- Debug y Release pasan 127/127 pruebas sin advertencias. Este endurecimiento
  permanece en desarrollo y aún no sustituye la instalación 1.2.4.0.

## Candidato 1.2.4.0 (2026-09-09)

- Dos builds Release consecutivos pasaron 125/125 pruebas y generaron el mismo
  ZIP. La DLL tiene SHA-256
  `7CEC88AE30FC6145B580A43E8C71CE4AF1B5E10F68322DBCD14D8A04376D06C2` y el
  ZIP `AA2B4B7A399C537867F5D8FDB59D165295521EC5703FC6B55F179CFB2A96C46F`.
- Los 13 archivos instalados en `Program Files` coinciden con el bundle.
  `DICT-01`, `UNDO-04` y `EXTENDED-01` pasaron de nuevo en perfiles aislados de
  AutoCAD Core Console. Solo se conserva 1.2.3.0 como rollback de emergencia.
- La prueba interactiva final pasó el 2026-09-09 sobre un dibujo desechable:
  autoload sin `NETLOAD`, corrección local de dos concordancias, propuesta real
  de origen `IA` con dos cambios y cero fallos, aplicación y restauración exacta
  mediante una sola `U` inmediata.

## Candidato 1.2.1.0 (2026-09-08)

- `AISPELLALL` detecta una instalación sin configuración válida y presenta una
  guía única antes de escanear el dibujo. Guardar la configuración predeterminada
  actúa como marca local sin introducir otro archivo ni conservar secretos.
- Ajustes acepta la clave propia en un `PasswordBox`, valida longitud y espacios,
  y la guarda o retira mediante `OPENAI_API_KEY` del usuario y del proceso. El
  valor existente nunca se lee hacia la interfaz ni se serializa.
- La conexión sigue siendo voluntaria: solo **Probar conexión** envía una frase
  fija después del consentimiento, y cada lote remoto requiere confirmación.
- Debug y Release pasan 117/117 pruebas sin advertencias. El ZIP 1.2.1.0 pasa
  el verificador independiente y todas sus pruebas negativas. La DLL Release
  tiene SHA-256
  `F000FAE02313F2BBD6CFFDB1AECDCD4A970657A703B08F9E507587489AAC6689` y el ZIP
  reproducible `F1224C385EBA6BA6EE9A6B94F431F706A5371307860A1F75F488BDC73C570603`.
  La instalación quedó verificada con 13/13 archivos idénticos al bundle. La
  prueba visual confirmó que la credencial se detecta sin mostrarse, la conexión
  real funciona y `AISPELLALL` corrigió mediante una propuesta `IA` un MText de
  40 caracteres; una sola `U` inmediata restauró exactamente el original. El
  primer inicio real se probará en otra computadora o perfil de Windows sin
  alterar la configuración existente. La instalación 1.2.0.0 sustituida quedó
  recuperable en `backup-20260908-142447-619` con sus 13 archivos.
- La prueba ampliada de OpenAI sobre una copia del DWG revisó 593 textos y unos
  10 052 caracteres. Produjo 52 filas con propuestas, cero fallos de proveedor
  y 21 correcciones preseleccionadas que el propietario revisó, aplicó y validó
  visualmente como correctas.

## Publicado 1.2.0.0 (2026-09-08)

- `AISPELLALL` amplía el adaptador Autodesk a `MLeader` textual, celdas simples
  de `Table`, textos fijos de definiciones locales y atributos por inserción.
- `EXTENDED-01` aplicó cinco cambios dentro de AutoCAD Core Console y una sola
  `U` restauró un `MLeader`, dos celdas de la misma tabla, texto de bloque y
  atributo. `UNDO-04` también continúa en PASS.
- La revisión local incorpora el diccionario abierto Hunspell `es_PE` de Perú
  detrás de `ISpellingDictionary`. `SpellEngine` no conoce archivos ni la
  biblioteca concreta; el adaptador administrado vive en infraestructura.
- La selección automática se limita a tildes inequívocas o distancia de edición
  uno con una única sugerencia. El caché es concurrente, los códigos cortos y
  mayúsculas internas se omiten y una carga fallida degrada a reglas conocidas.
- La fuente fija los archivos de LibreOffice/RLA-ES por commit y SHA-256 e
  incluye los avisos de `WeCantSpell.Hunspell` 2.0.1 y el texto MPL 1.1. No
  contiene datos del DLE ni del DPD.
- Debug y Release pasan 115/115 pruebas, sin advertencias. La DLL Debug tiene
  SHA-256 `909A72A808E3227FDA2E67462CEC18DB344F170BBE6AB7859AB418147F4865A6`
  y la DLL Release
  `3D5972B0951DD17EC3CF6BAD1F6E366C49C4406913D244C3A33EDD279F786D5B`.
- `DICT-01` cargó el ensamblado administrado y el diccionario `es_PE` dentro de
  AutoCAD Core Console, y restauró correctamente una tilde. `EXTENDED-01` y
  `UNDO-04` siguen en PASS.
- El ZIP verificado y reproducido dos veces tiene SHA-256
  `2779BD4E787F1D8DE3AE74E783CE77B89CA2F9AD9C9B325EE2591133970A3A53`.
- El candidato distingue el verbo `HA` de la unidad hectárea `ha`: `HA SIDO`
  puede corregirse y una cantidad como `25 ha` continúa protegida. También
  conserva los controles completos de formato MText, incluidos nombres de
  fuente como `Arial Narrow`.
- La instalación administrada candidata quedó verificada archivo por archivo:
  sus 13 archivos coinciden con el bundle y la DLL tiene SHA-256
  `3D5972B0951DD17EC3CF6BAD1F6E366C49C4406913D244C3A33EDD279F786D5B`,
  `WeCantSpell.Hunspell` es 2.0.1.0 y el diccionario `es_PE` conserva su huella
  esperada. El bundle anterior quedó recuperable en el respaldo
  `backup-20260908-091534-094`; su DLL conserva SHA-256
  `E4C9F9A7B9CFDF0708DC36A1E24BAD40DCC47DAF8751B71A6D58497B8FE86413`.
- La prueba visual del propietario pasó el 2026-09-08: se aplicaron juntas las
  correcciones de dos `MLeader` y un `DBText`, y una sola `U` inmediata restauró
  exactamente los tres objetos.
- La errata `TUVERÍA` observada en un `MLeader` se cubrió explícitamente sin
  alterar `HDPE`, `PD`, `100/300` ni `mm`; el nuevo corte quedó instalado.

Última actualización: 2026-09-08.

## Resultado

La implementación raíz es el producto canónico. La versión 1.1.5.0 compila para
.NET Framework 4.8/x64 sin advertencias ni errores y su runner autocontenido
completa 105 pruebas. Incluye revisión individual y por lote, edición manual
validada, progreso/cancelación, filtros, memoria local explícita, glosarios,
diagnóstico seguro, simulación y OpenAI optativo.

## Invariantes verificados

- `SpellEngine` y `Domain` no dependen de Autodesk, WPF ni red.
- Ninguna transacción de Autodesk permanece abierta durante UI o red.
- Toda propuesta —remota, manual o aprendida— recalcula diff y pasa por el
  mismo validador de tokens técnicos.
- La escritura individual revalida el snapshot y el lote se confirma de forma
  atómica; cancelar o detectar un conflicto produce cero escrituras.
- AutoCAD agrupa la escritura de `AISPELLALL` por comando; `UNDO-04` confirma
  que una sola orden `U` restaura dos entidades y evita introducir un grupo
  manual `UNDO Begin/End`, que interferiría con esa marca nativa.
- La instalación administrada 1.1.5.0 reprodujo el resultado sobre tres
  correcciones del fixture limpio y mostró la instrucción `U` esperada.
- La memoria requiere una casilla explícita y nunca autoaplica preferencias.
- OpenAI recibe solo el texto que se corrige, usa `store: false` y no recibe
  layout, DWG, geometría, coordenadas, capa, handle ni glosarios.
- La clave se lee de `OPENAI_API_KEY`; no se persiste ni se registra.
- El paquete Release excluye ensamblados de Autodesk, PDB y configuración del
  usuario.

## Evidencia

- 105 pruebas cubren motor, dominio, coordinación, ciclo asíncrono, ViewModels,
  privacidad, configuración, diagnóstico, edición manual, filtros, progreso,
  memoria y glosario organizacional.
- Los Hitos 1 a 3 tienen regresión interactiva completa en Civil 3D 2024.
- El ciclo de instalación, actualización, rollback y desinstalación del bundle
  pasó en directorios aislados; la instalación 1.1.0.0 y actualización real a
  1.1.1.0 también pasaron, conservando un respaldo recuperable.
- La actualización administrada real 1.1.3.0 → 1.1.4.0, rollback exacto,
  desinstalación y reinstalación final también pasaron. Los datos locales fueron
  comparados por ruta y hash y permanecieron idénticos.
- La actualización administrada 1.1.4.0 → 1.1.5.0 pasó el 2026-09-03; los
  cuatro archivos instalados coinciden con el bundle y el respaldo 1.1.4.0 es
  recuperable.
- `AISPELLSETTINGS` cargó 1.1.1.0 por primera invocación desde el bundle, sin
  `NETLOAD`, y dejó evidencia diagnóstica estructurada de la versión.
- El candidato ZIP 1.1.4.0 tiene SHA-256
  `C5335CF1ABC95FB1F3368CE2C343AEF859B963923B1543D98CBDDD892229B570`
  y se entrega con un archivo `.zip.sha256` verificable.
- El prerelease público conserva ambos archivos bajo la etiqueta inmutable
  `1.1.4.0`, asociada al commit
  `787d824fe932543b2787dfc2b41bfbbad02d6609`.
- El candidato pasó Debug/Release con 105/105 pruebas; la DLL Release tiene
  SHA-256 `73FEDCF3911367B5A19D3B62FF68A1626EE48E459CC018170690106BA53E687A`.
- La versión 1.1.5.0 pasó Debug/Release con 105/105 pruebas; la DLL Release
  tiene SHA-256
  `11A33B5825AC38E9B35F9E4A1951A2D02F5E190B624A0B27A7204A45FF138331`
  y el ZIP `FE394373A9B5801D9B42D7EA1C389DAAC7EE74EEA5CE3A6B10CBC396DFE4D68A`.
- El prerelease público 1.1.5.0 quedó asociado al commit
  `30856f7e825ff1221558ac2db6727ae17f4061ed` y contiene el ZIP y checksum;
  la descarga pública del ZIP reprodujo exactamente la huella registrada.
- Dos construcciones consecutivas de la versión 1.1.5.0 produjeron ese mismo
  SHA-256 del ZIP.
- Dos construcciones consecutivas del mismo contenido produjeron ese mismo
  hash; el ZIP ordena entradas y fija sus marcas de tiempo internas.
- El control independiente del artefacto comprueba checksum, rutas internas,
  entradas obligatorias, manifiesto, versión de la DLL y archivos prohibidos.
  Su prueba negativa confirmó que rechaza un nombre incoherente y una huella
  SHA-256 alterada, incluso si el ZIP modificado trae un checksum recalculado;
  también rechaza cualquier archivo no previsto dentro de la entrega.
- La validación estática confirma las garantías XAML de foco, estado anunciable
  y ayuda de validación. El recorrido interactivo 1.1.4.0 al 125 % confirmó
  Tab/Shift+Tab, desplazamiento, selección sincronizada y ausencia de recortes
  en las tres ventanas principales.

## Riesgos restantes

- La migración del mismo `ProductCode` desde el perfil del usuario a
  `Program Files` puede conservar temporalmente la ruta anterior en la caché de
  Civil 3D. Un único `APPAUTOLOADER` → `Reload` registró la ruta administrada y
  `AISPELLSETTINGS` cargó sin `NETLOAD` ni advertencia de firma; REG-2026-004
  documenta el procedimiento.
- El paquete no está firmado con un certificado de confianza. Debe acordarse la
  política del piloto antes de desplegarlo en equipos que exijan firma.
- Solo se soportan `DBText` y `MText` directos en Civil 3D 2024 x64.

La matriz operativa vive en
[product/10_MVP_STATUS_AND_TEST_PLAN.md](product/10_MVP_STATUS_AND_TEST_PLAN.md).
