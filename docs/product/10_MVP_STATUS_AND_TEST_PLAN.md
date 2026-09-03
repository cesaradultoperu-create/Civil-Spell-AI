# Estado verificable del MVP y plan de pruebas

Fecha de corte funcional: 2026-08-25

Última verificación automatizada: 2026-09-03

Estado del documento: vigente; fuente de verdad operativa del MVP.

Este documento describe lo que existe hoy, lo que fue comprobado y lo que aún
debe probarse. Los documentos `00` a `03` conservan el diseño y los requisitos
originales; los documentos `04` a `09` son registros de implementación por
fase. Ante una diferencia de estado, prevalece este documento.

## 1. Resumen ejecutivo

CivilSpellAI alcanzó un MVP utilizable en Civil 3D 2024. Corrige anotaciones
`DBText` y `MText` mediante reglas locales y, opcionalmente, OpenAI. Ninguna
propuesta se escribe automáticamente: el usuario revisa el resultado y confirma
la aplicación. La escritura relee el objeto para impedir que un resultado
obsoleto sobrescriba cambios posteriores.

Evidencia reproducible del corte:

- `dotnet build CivilSpellAI.slnx --configuration Debug --no-restore`: correcto,
  cero advertencias y cero errores.
- `scripts/Run-SpellCoreTests.ps1`: 105 pruebas correctas y cero fallidas.
- `scripts/Validate-Mvp.ps1`: Debug y Release x64 correctos, glosario incluido,
  huella SHA-256 generada y ninguna DLL de Autodesk copiada.
- Huellas del primer corte del Hito 2: Debug
  `6C99BEB6BAF3A7BB785048317D242C55B08D708BAE156D5582DD4783AB212B6F` y
  Release `B56E25740A1756071C9032AE5B14240A5F6D0ADF1B3C3C6432441B49F6D4EB23`.
- Huellas del corte automatizado actual del Hito 2: Debug
  `E264C7055AA5D4CC24DFCA23C08496F9118B15514D6963F445A24D802F04B0AC` y
  Release `61888DC33C23DE86106809768AE2265278CCA2C735AA38F1E116A58D318F3903`.
- Huellas del corte actual del Hito 3: Debug
  `1CDE491B52D550AB1B1F31E82D6B1CADED284B08BEF892CB3505D1D9AB0051EB` y
  Release `336C758DE1FE2456C6B3F6416726DAA939EE156BE9F50279EA748FA2ECF3D9D4`.
- Corte 1.1.0.0: Release
  `E11CE47C6A50A4E2BB5B17B0E1E3EEA1207C89F1463B9076227E43AFB8945E7B` y ZIP
  `B6DCCEB516A9AC589AB61BA8CE4BB2F1158D4A9FE3B26296B42EB8F2BE74AC7F`.
- Corte 1.1.1.0: Release
  `2F25EFDF5E19F7776EFD3E458BD902BD3DBD700CC4F7A9D64FECADA4E3D37257` y ZIP
  `78CF84633F102D614F36E8EFF19E68E3E8E897C5177653F5CF070F8ABAAB65D8`.
- Candidato 1.1.3.0: Debug
  `04E74917C5D644CCD78BBE9611C4EDC556994B265EEDBDE31FF931A9B9878C3E`,
  Release `DF6F2AF2B71D96B94E3FE2D49250759AC6EB0A2EB55EEE9F55ED526CE08BC4E6`
  y ZIP `3782FB8F47475341109435642A2089CE642A7EE857D1BBA54744927418CE3F81`.
- Candidato 1.1.4.0: Debug
  `016BA4814D7551001379B49FB936C6F0CB962EBB9A930EDB40F8F806F43D8FFB`,
  Release `73FEDCF3911367B5A19D3B62FF68A1626EE48E459CC018170690106BA53E687A`
  y ZIP `C5335CF1ABC95FB1F3368CE2C343AEF859B963923B1543D98CBDDD892229B570`,
  acompañado por su archivo `.zip.sha256`.
- OpenAI real: flujo satisfactorio confirmado por el propietario tanto en
  `AISPELL` como en `AISPELLALL`.
- La matriz manual del Hito 1 está completa; no quedan defectos críticos
  abiertos.
- La regresión mínima del Hito 2 está completa: selección, lote, cancelación
  lenta, conflictos y cambio de documento quedaron en PASS el 2026-08-27.
- La regresión del Hito 3 está completa: exportación/borrado y clasificación
  reproducible de un timeout como `TMO-001`, sin contenido sensible, quedaron
  en PASS el 2026-08-27.
- El candidato 1.1.3.0 se instaló bajo `Program Files`; sus archivos coincidieron
  con el artefacto y `AISPELLSETTINGS` cargó sin `NETLOAD` ni advertencia de
  firma después de una recarga única del autoloader por la migración de alcance.
- REG-2026-002 y REG-2026-003 quedaron revalidadas en el fixture 1.1.3.0.
- La 1.1.4.0 incorpora sincronización de foco/selección y metadatos accesibles;
  las garantías XAML y el recorrido manual al 125 % están en PASS.
- La actualización real 1.1.3.0 → 1.1.4.0, rollback, desinstalación y
  reinstalación final pasaron por hash; los datos locales permanecieron
  idénticos.
- El verificador independiente del release aceptó el artefacto final y sus
  pruebas negativas rechazaron un checksum asociado a otro nombre y una huella
  SHA-256 alterada, además de un ZIP con checksum válido pero contenido extra.

## 2. Capacidades disponibles

### `AISPELL`

- Selecciona un único `DBText` o `MText` y crea un snapshot sin mantener abierta
  la transacción de lectura.
- Genera una propuesta local y puede solicitar alternativas adicionales al
  proveedor simulado o a OpenAI.
- Muestra original, idioma, explicación, diff recalculado y validación técnica
  en una ventana WPF modal.
- Permite aplicar una propuesta validada, mantener el original o cancelar.
- Antes de escribir, verifica documento, handle, tipo y texto original.

### `AISPELLALL`

- Escanea los `DBText` y `MText` directos de Model Space y layouts.
- Analiza los textos sin mantener transacciones abiertas durante la UI o la red.
- Muestra las filas con propuestas, preselecciona la alternativa aplicable más
  completa y permite excluir filas.
- Revalida todo el lote antes de la primera escritura. Un conflicto cancela el
  lote completo y la aplicación se confirma en una única transacción.

### `AISPELLSETTINGS`

- Administra proveedor simulado, escenario de prueba y glosario personal.
- Habilita OpenAI solo con consentimiento explícito de envío de texto.
- Muestra si `OPENAI_API_KEY` está disponible sin almacenar ni revelar la clave.
- Persiste configuración versionada bajo `%LOCALAPPDATA%\CivilSpellAI`.
- Activa de forma opcional el diagnóstico local seguro y permite exportar o
  borrar sus eventos.

## 3. Arquitectura y datos

| Capa | Responsabilidad vigente |
| --- | --- |
| `Commands` | Entradas de Civil 3D y coordinación del flujo interactivo. |
| `Autodesk` | Selección, snapshots, escaneo y adaptación de transacciones AutoCAD. |
| `Application` | Coordinación individual/por lote, proveedores y prevalidación de escritura atómica. |
| `Domain` | Solicitudes, propuestas, decisiones, diff y validación técnica. |
| `Infrastructure` | Configuración, glosario, diagnóstico seguro, simulación y transporte de OpenAI. |
| `Spell` | Motor determinista independiente de Autodesk. |
| `UI` | Ventanas WPF y ViewModels sin transacciones de AutoCAD. |

La configuración usa el esquema 3. El glosario personal, el consentimiento y la
activación opcional del diagnóstico se guardan localmente. La credencial se
obtiene de `OPENAI_API_KEY`; no se escribe en el repositorio ni en la
configuración. La memoria de aprendizaje usa un archivo esquema 1 separado,
solo registra decisiones marcadas expresamente y puede administrarse desde
`AISPELLSETTINGS`.

Cuando se usa OpenAI, la única información variable extraída del dibujo que se
envía es `CorrectionRequest.Text`. La solicitud usa `store: false` y excluye el
DWG, nombre de archivo, geometría, coordenadas, capas, handles, tipos de entidad,
metadatos y glosarios. Las pruebas automatizadas usan un transporte simulado y
no acceden a Internet.

## 4. Estado funcional

| Capacidad | Estado | Evidencia o pendiente |
| --- | --- | --- |
| Reglas locales en español, inglés y texto mixto | Validado | Pruebas automatizadas del motor. |
| Protección de números, unidades, estaciones, códigos y glosario | Validado | Propuestas inseguras bloqueadas en pruebas y proveedor simulado. |
| Protección de códigos de formato MText | Validado | Prueba automatizada específica. |
| Diagnóstico local sin contenido del plano | Validado | Siete pruebas automatizadas y dos casos interactivos cubren códigos, privacidad, timeout `TMO-001`, rotación, exportación y borrado. |
| Revisión individual WPF y aplicación sobre `DBText` | Validado | Prueba manual registrada en fase 2. |
| Revisión individual completa sobre `MText` | Validado | Aplicar, mantener, cancelar, cerrar y conservar formato comprobados. |
| Proveedor simulado y degradación segura | Validado | Éxito, timeout, respuesta inválida, bloqueo y cancelación manuales. |
| Revisión local por lote | Validado | Aplicación selectiva y `UNDO` único reconfirmados el 2026-08-26. |
| OpenAI real en `AISPELL` | Validado | Aplicación y `UNDO` reconfirmados con fixture anonimizado el 2026-08-26. |
| OpenAI real en `AISPELLALL` | Validado | Selección, aplicación y `UNDO` reconfirmados el 2026-08-26. |
| Errores reales de autenticación y red | Validado | Credencial inválida y desconexión comprobadas sin escrituras. |
| Conflicto entre snapshot y escritura | Validado | `AISPELLTESTCONFLICT` confirmó detección y texto intacto en Civil 3D el 2026-08-26. |
| Frontera de escritura atómica | Validado | Ocho pruebas deterministas cubren documento/objeto/tipo/texto, lote vacío, conflicto parcial, rollback y commit. |
| `UNDO` individual, local por lote y remoto | Validado | Los tres niveles fueron comprobados el 2026-08-26. |
| Edición manual validada | Validado | Recalcula diff, bloquea cambios de tokens técnicos y pasó la regresión WPF en Civil 3D. |
| Progreso, cancelación, filtros y alternativa por fila | Validado | Filtros, alternativas, selección, aplicación, `UNDO` y cancelación durante la preparación pasaron en Civil 3D. |
| Memoria local de decisiones | Validado | Opt-in explícito, nunca autoaplicada; búsqueda, activación, exportación, borrado y prioridad de reglas revalidados. |
| Accesibilidad WPF al 125 % | Validado | Configuración, revisión individual y lote pasan Tab/Shift+Tab, foco visible, desplazamiento, selección sincronizada y ausencia de recortes. |
| Glosario organizacional | Implementado | Archivo de solo lectura bajo `%PROGRAMDATA%`; unión local y privacidad cubiertas por pruebas. |
| Paquete, autocarga, actualización y rollback | Validado | 1.1.4.0 quedó instalada en `Program Files`; actualización desde 1.1.3.0, rollback, desinstalación y reinstalación pasaron conservando los datos locales. El autoload final y el recorrido de accesibilidad quedaron confirmados el 2026-09-03. |
| Atributos, tablas y etiquetas de Civil 3D | Diferido | Fuera del MVP; evaluación posterior al piloto. |

`Implementado` significa que existe código pero falta evidencia manual del flujo
integrado. `Validado` significa que existe evidencia automatizada o manual
aplicable. `Pendiente` identifica una comprobación necesaria. `Diferido`
identifica una capacidad reservada para el segundo alcance o una etapa posterior.

## 5. Matriz de regresión manual

Todas las pruebas deben realizarse sobre una copia de un dibujo sin información
sensible. La evidencia mínima es fecha, versión de DLL, resultado y captura o
nota reproducible. Un fallo debe registrarse sin copiar el texto real del plano.
Los pasos ejecutables, fixtures y formato de resultados están en
`../testing/MVP_REGRESSION_RUNBOOK.md`.

| ID | Caso y precondición | Procedimiento resumido | Resultado esperado | Estado / fecha / evidencia |
| --- | --- | --- | --- | --- |
| IND-01 | `DBText` con error local conocido | Ejecutar `AISPELL`, elegir propuesta y aplicar. | Solo cambia el objeto seleccionado. | Validado 2026-08-25; registro de fase 2. |
| IND-02 | `DBText` con propuesta | Pulsar **Mantener original**. | No se abre escritura y el texto queda idéntico. | Validado 2026-08-26; mensaje esperado y `ESTRUCTURAA` intacto. |
| IND-03 | `DBText` con propuesta | Pulsar **Cancelar** y repetir cerrando la ventana. | No cambia el dibujo. | Validado 2026-08-26; ambas variantes conservaron el texto original. |
| IND-04 | `MText` con salto y código de formato | Aplicar una corrección segura. | Corrige texto y conserva contenido y formato técnico. | Revalidado con el adaptador del Hito 2 (`6C99BEB6…`): corrección aplicada y código `\pxqj;` intacto. |
| IND-05 | `MText` con propuesta | Probar mantener, cancelar y cerrar. | El objeto permanece idéntico. | Validado 2026-08-26; contenido, líneas y formato intactos. |
| SAFE-01 | Texto con número, unidad, estación, código y término protegido | Usar escenario `UnsafeTechnicalChange`. | La alternativa insegura queda visible, explicada y bloqueada. | Validado 2026-08-26; `999 m` bloqueado y cancelación intacta. REG-2026-001 cerrada tras revalidar la selección con mouse. |
| SAFE-02 | Snapshot obsoleto controlado en Civil 3D | Ejecutar `AISPELLTESTCONFLICT` en Debug. | Se informa conflicto y no se sobrescribe. | Revalidado 2026-08-26 con el adaptador del Hito 2 (`6C99BEB6…`); mensaje PASS y texto intacto. |
| UNDO-01 | Corrección individual aplicada | Ejecutar un único `UNDO`. | Restaura exactamente el texto anterior. | Revalidado con el adaptador del Hito 2 (`6C99BEB6…`): `ESTRUCTURAA` y formato MText restaurados. |
| BATCH-01 | Dibujo con textos correctos e incorrectos | Ejecutar `AISPELLALL`, excluir una fila y aplicar. | Solo cambian las filas seleccionadas. | Validado 2026-08-26; selección respetada. |
| BATCH-02 | Lote con snapshot obsoleto controlado | Ejecutar `AISPELLTESTBATCHCONFLICT` en Debug. | Ningún elemento del lote se modifica. | Revalidado 2026-08-26 con el adaptador del Hito 2 (`6C99BEB6…`); mensaje PASS y lote intacto. |
| UNDO-02 | Lote local aplicado | Ejecutar un único `UNDO`. | Se revierte todo el lote. | Validado 2026-08-26; una operación revirtió el lote. |
| SIM-01 | Simulación `Successful` | Revisar y aplicar una alternativa simulada. | Alternativa segura disponible junto a reglas locales. | Reconfirmado 2026-08-26; aplicación segura y `UNDO` correctos. |
| SIM-02 | Simulación `Timeout` o `InvalidResponse` | Ejecutar `AISPELL` y reintentar. | Se informa el fallo y la propuesta local sigue disponible. | Ambos escenarios reconfirmados 2026-08-26 sin escrituras. |
| SIM-03 | Solicitud simulada pendiente | Cancelar o cerrar la ventana. | No hay escritura posterior ni inestabilidad en Civil 3D. | Reconfirmado 2026-08-26 con `SlowSuccessful` y DLL `2B4F7168…`: texto intacto, sin reapertura ni escritura tardía; Civil 3D operativo. |
| AI-01 | OpenAI habilitado, clave de prueba y texto sin datos sensibles | Ejecutar `AISPELL` y aplicar una propuesta segura. | La propuesta remota se valida localmente antes de escribir. | Validado 2026-08-26; aplicación y `UNDO` correctos. |
| AI-02 | OpenAI habilitado y dibujo de prueba | Ejecutar `AISPELLALL`, revisar filas y aplicar. | El lote remoto respeta selección y validación local. | Validado 2026-08-26; selección y aplicación correctas. |
| AI-03 | Clave de prueba inválida | Ejecutar una revisión remota. | Mensaje de autenticación, reglas locales disponibles y cero escrituras. | Validado 2026-08-26 en sesión aislada sin alterar la clave real. |
| AI-04 | Equipo desconectado | Ejecutar y reintentar una revisión remota. | Error recuperable, cancelación segura y cero escrituras. | Validado 2026-08-26; recuperación confirmada tras reconectar. |
| UNDO-03 | Lote con propuestas de OpenAI aplicado | Ejecutar un único `UNDO`. | Se revierte todo el lote remoto. | Validado 2026-08-26; una operación revirtió el lote. |
| CFG-01 | Cambios de glosario y consentimiento | Guardar, cerrar y volver a abrir configuración. | Persisten ajustes y términos; la clave no aparece en archivos. | Validado 2026-08-26; persistencia y ausencia de secretos comprobadas en disco. |

## 6. Riesgos y límites conocidos

- No existen pruebas automatizadas dentro del proceso de AutoCAD/Civil 3D. La
  prevalidación, atomicidad, selección sustituible y guardas del ciclo de vida
  ya se prueban fuera del proceso; el adaptador real aún requiere regresión
  manual mínima.
- La autenticación inválida, la desconexión y `UNDO` remoto tienen evidencia
  manual, pero todavía no cuentan con pruebas automatizadas en los adaptadores
  de Civil 3D.
- El lote muestra progreso, permite cancelar y revela cantidad de textos y
  caracteres antes de usar OpenAI, pero el coste final depende del modelo y la
  respuesta del servicio.
- La configuración restringe el modelo a las opciones admitidas. La prueba de
  conexión es voluntaria, advierte el coste y envía únicamente una frase fija,
  sin texto del dibujo ni persistencia de respuesta.
- El Application Bundle cargó realmente desde `Program Files`. Una migración de
  alcance puede requerir una única recarga con `APPAUTOLOADER`; actualización,
  rollback, desinstalación y reinstalación del candidato final ya pasaron. El
  piloto interno se limita a equipos controlados, sin firma y con verificación
  obligatoria del hash.
- Solo se revisan `DBText` y `MText` directos. No se incluyen atributos, tablas,
  etiquetas nativas, referencias externas ni texto anidado.
- La corrección depende del alcance de reglas y del proveedor; no realiza
  cálculos de ingeniería, traducción integral ni aplicación automática.

## 7. Criterios para declarar estable el MVP

El MVP podrá pasar a piloto cuando:

1. las pruebas automatizadas y el build pasen en una instalación limpia;
2. `IND-02` a `IND-05`, `SAFE-02`, `UNDO-01`, `BATCH-02` y `UNDO-02` estén
   validadas en Civil 3D 2024; cumplido el 2026-08-26;
3. `AI-03`, `AI-04` y `UNDO-03` tengan evidencia sin datos sensibles; cumplido
   el 2026-08-26;
4. no existan defectos abiertos que puedan sobrescribir texto, modificar tokens
   técnicos, dejar escrituras después de cancelar o cerrar Civil 3D; cumplido
   para la matriz del Hito 1 el 2026-08-26;
5. exista un paquete Release reproducible con instrucciones de instalación,
   actualización y rollback; cumplido en aislamiento y con el ciclo real hasta
   1.1.4.0;
6. el autoloader y las funciones nuevas de los Hitos 5 y 6 pasen la regresión
   interactiva de `docs/codex/NEXT_TASK.md`; cumplido el 2026-09-03.
7. el piloto tenga responsable, dataset anonimizado y mecanismo de registro de
   incidencias sin contenido de planos.

La matriz específica del corte 1.1 está en
`../testing/PILOT_1_1_REGRESSION_RUNBOOK.md`.
