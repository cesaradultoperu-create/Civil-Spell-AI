# Auditoría técnica

Última actualización: 2026-09-03.

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
