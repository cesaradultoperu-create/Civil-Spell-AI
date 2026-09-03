# Runbook de regresión del MVP en Civil 3D 2024

Fecha de preparación: 2026-08-26

DLL Debug usada para la revalidación final de REG-2026-001 y SIM-03:

`2B4F7168C5D9472D198BDEB893463FA0B0C30B83EF7032270ED551AC508F5E20`

Este runbook ejecuta el Hito 1 del segundo alcance. Debe usarse con una copia
desechable de un dibujo y una clave de OpenAI de prueba con límite de gasto. No
se deben copiar textos reales, claves ni respuestas remotas a las evidencias.

## 1. Reglas de la sesión

- Detener la sesión si un texto cambia sin pulsar **Aplicar**, si una cancelación
  escribe, si se altera un token protegido o si un conflicto produce cambios.
- No continuar con casos remotos después de un fallo de seguridad o atomicidad.
- Registrar la huella SHA-256 de la DLL, fecha, resultado y evidencia de cada
  caso. Usar `INCIDENT_TEMPLATE.md` para cualquier resultado distinto del
  esperado.
- Los comandos `AISPELLTESTCONFLICT` y `AISPELLTESTBATCHCONFLICT` solo existen en
  Debug. Usan snapshots obsoletos deliberados y deben ejecutarse únicamente en
  el dibujo desechable.

## 2. Preparación automatizada

1. Cerrar Civil 3D para liberar la DLL.
2. Desde la raíz del repositorio ejecutar:

   ```powershell
   powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Validate-Mvp.ps1
   ```

3. Confirmar `VALIDACION AUTOMATIZADA CORRECTA`.
4. Conservar la ruta del informe y la huella SHA-256 mostradas por el script.
5. Abrir Civil 3D 2024 y cargar `bin\x64\Debug\CivilSpellAI.dll` con `NETLOAD`.

El informe se genera bajo `artifacts\validation\` y no se versiona.

## 3. Dibujo desechable

Crear un dibujo nuevo con estas entidades en Model Space. Aplicar al fragmento
`PK 1+250.00` del MText algún formato visible, por ejemplo subrayado, para poder
comprobar que se conserva.

| Fixture | Tipo | Contenido inicial | Resultado local esperado |
| --- | --- | --- | --- |
| F-01 | DBText | `LA CARRETERAA ESTA EN UBCACION` | `LA CARRETERA ESTÁ EN UBICACIÓN` |
| F-02 | MText, dos líneas | `ESTRUTURAA PRINCIPAL` y `PENDIENTEE 2.50 % - PK 1+250.00` | `ESTRUCTURA PRINCIPAL` y `PENDIENTE 2.50 % - PK 1+250.00` |
| F-03 | DBText | `The existent surfce` | `The existing surface` |
| F-04 | DBText | `Station 1+250.00 - Pipe Network` | Sin propuesta local. |
| F-05 | DBText | `LA CUNETAA DEL PROYECTOO` | `LA CUNETA DEL PROYECTO` |

Antes de las pruebas locales, abrir `AISPELLSETTINGS` y desactivar OpenAI y el
proveedor simulado. Guardar, cerrar y volver a abrir para comprobar persistencia.

## 4. Secuencia local obligatoria

Registrar cada caso directamente en la tabla de resultados de la sección 7.

1. **IND-02:** ejecutar `AISPELL` sobre F-01 y pulsar **Mantener original**.
   Confirmar que el contenido continúa idéntico.
2. **IND-03:** repetir sobre F-01 pulsando **Cancelar**; repetir una tercera vez
   cerrando con **X**. Confirmar que ninguna variante escribe.
3. **IND-04:** ejecutar `AISPELL` sobre F-02, aplicar y verificar las dos
   correcciones, el salto de línea, `2.50 %`, `PK 1+250.00` y su formato.
4. **UNDO-01:** ejecutar un único `UNDO`. F-02 debe volver exactamente a su
   contenido y formato iniciales.
5. **IND-05:** repetir sobre F-02 con Mantener, Cancelar y **X**; comparar antes
   y después.
6. **SAFE-02:** ejecutar `AISPELLTESTCONFLICT`, seleccionar F-01 y exigir el
   mensaje `PASS SAFE-02`. El contenido debe seguir intacto.
7. **BATCH-02:** ejecutar `AISPELLTESTBATCHCONFLICT` con al menos F-01 y F-02 en
   el dibujo. Exigir `PASS BATCH-02` y comprobar que ninguna entidad cambió.
8. **BATCH-01/UNDO-02:** ejecutar `AISPELLALL`, excluir al menos una fila, aplicar
   las demás y ejecutar un único `UNDO`. Todo el lote aplicado debe revertirse y
   la fila excluida debe permanecer intacta durante todo el caso.

## 5. Proveedor simulado

1. En `AISPELLSETTINGS`, habilitar únicamente el proveedor simulado.
2. Repetir `Successful`, `Timeout`, `InvalidResponse` y
   `UnsafeTechnicalChange`.
3. En los fallos, confirmar que la propuesta local permanece disponible y que
   **Reintentar** no escribe.
4. En `UnsafeTechnicalChange`, usar F-02 y confirmar que una alteración de
   `2.50`, `%` o `1+250.00` queda bloqueada.
5. Seleccionar `SlowSuccessful`, ejecutar `AISPELL` y cerrar la ventana mientras
   indique que carga alternativas. Esperar seis segundos. El dibujo debe
   permanecer intacto, Civil 3D debe responder y no debe aparecer una ventana ni
   escritura tardía.

Estos casos ya tienen evidencia histórica, pero deben repetirse con la misma DLL
del corte para que la regresión sea trazable.

## 6. OpenAI real

Usar únicamente los fixtures anonimizados anteriores y una clave de prueba con
límite de gasto.

1. **AI-01/UNDO-01:** habilitar OpenAI y el consentimiento, ejecutar `AISPELL`
   sobre F-01 o F-03, aplicar una alternativa segura y revertirla con un `UNDO`.
2. **AI-02/UNDO-03:** ejecutar `AISPELLALL`, revisar las filas, aplicar un lote y
   revertirlo con un único `UNDO`.
3. **AI-03:** guardar y cerrar todos los procesos de Civil 3D. Ejecutar
   `scripts\Start-Civil3D-InvalidOpenAiKey.ps1`, cargar la DLL Debug y ejecutar
   `AISPELL`. El lanzador usa una credencial ficticia solo en el proceso hijo y
   no modifica la variable persistente del usuario. Debe mostrarse un error de
   autenticación, conservarse la propuesta local y no escribirse nada. Cerrar
   esa sesión y volver a iniciar Civil 3D normalmente desde su acceso directo.
4. **AI-04:** desconectar temporalmente la red en una sesión controlada, ejecutar
   y reintentar `AISPELL`, cancelar y volver a conectar. Debe informarse un fallo
   recuperable y el dibujo debe permanecer intacto.
5. Deshabilitar OpenAI al terminar la sesión.

## 7. Registro del corte

Completar una fila por caso. `PASS` exige comparar el dibujo antes y después;
una ventana o mensaje correcto por sí solo no basta.

| ID | Resultado | Fecha/hora | SHA-256 DLL | Evidencia anonimizada / incidencia |
| --- | --- | --- | --- | --- |
| IND-02 | PASS | 2026-08-26 | `1390EBA6BCEBEBE6C73D08B78738826DABDE8999FD20071E759975D1D5A68AD7` | **Mantener original** informó el resultado esperado y no modificó `ESTRUCTURAA`. |
| IND-03 | PASS | 2026-08-26 | `1390EBA6BCEBEBE6C73D08B78738826DABDE8999FD20071E759975D1D5A68AD7` | **Cancelar** y cerrar con **X** conservaron el texto con el error original. |
| IND-04 | PASS, revalidado tras aislar la escritura | 2026-08-26 | Corte Hito 1: `1390EBA6…`; Hito 2: `6C99BEB6…212B6F` | El adaptador aplicó `ESTRUCTURAA` → `ESTRUCTURA` en MText y conservó intacto el código de formato `\pxqj;`. |
| IND-05 | PASS | 2026-08-26 | `1390EBA6BCEBEBE6C73D08B78738826DABDE8999FD20071E759975D1D5A68AD7` | Mantener, Cancelar y cerrar con X conservaron contenido, líneas y formato del MText. |
| SAFE-02 | PASS, revalidado tras aislar la escritura | 2026-08-26 | Corte Hito 1: `1390EBA6…`; Hito 2: `6C99BEB6…212B6F` | La nueva frontera/adaptador informó `PASS SAFE-02`, detectó el snapshot obsoleto y conservó el texto intacto. |
| UNDO-01 | PASS, revalidado tras aislar la escritura | 2026-08-26 | Corte Hito 1: `1390EBA6…`; Hito 2: `6C99BEB6…212B6F` | Un único `UNDO` restauró `ESTRUCTURAA` y el formato original del MText. |
| BATCH-01 | PASS | 2026-08-26 | `1390EBA6BCEBEBE6C73D08B78738826DABDE8999FD20071E759975D1D5A68AD7` | Filas seleccionadas aplicadas y fila excluida intacta; confirmado por el propietario. |
| BATCH-02 | PASS, revalidado tras aislar la escritura | 2026-08-26 | Corte Hito 1: `1390EBA6…`; Hito 2: `6C99BEB6…212B6F` | La nueva frontera/adaptador informó `PASS BATCH-02`: el conflicto canceló el lote completo sin modificar textos. |
| UNDO-02 | PASS | 2026-08-26 | `1390EBA6BCEBEBE6C73D08B78738826DABDE8999FD20071E759975D1D5A68AD7` | Un único `UNDO` revirtió todo el lote local aplicado. |
| SIM-01/02/03 | PASS | 2026-08-26 | Corte inicial: `1390EBA6…`; revalidación: `2B4F7168…` | `Successful`, `Timeout`, `InvalidResponse` y `UnsafeTechnicalChange` se comportaron de forma segura. Con `SlowSuccessful`, cerrar durante la carga conservó el texto, no reabrió la ventana ni produjo escritura tardía y Civil 3D siguió respondiendo tras seis segundos. [REG-2026-001](incidents/REG-2026-001_ALTERNATIVE_LIST_MOUSE.md) cerrada. |
| AI-01 | PASS | 2026-08-26 | `1390EBA6BCEBEBE6C73D08B78738826DABDE8999FD20071E759975D1D5A68AD7` | OpenAI real produjo una alternativa segura, se aplicó explícitamente y un `UNDO` restauró el fixture anonimizado. |
| AI-02 | PASS | 2026-08-26 | `1390EBA6BCEBEBE6C73D08B78738826DABDE8999FD20071E759975D1D5A68AD7` | OpenAI real procesó el lote anonimizado, aplicó las filas elegidas y respetó la exclusión. |
| AI-03 | PASS | 2026-08-26 | `1390EBA6BCEBEBE6C73D08B78738826DABDE8999FD20071E759975D1D5A68AD7` | Sesión aislada con credencial ficticia mostró rechazo, conservó propuesta local y no escribió al cancelar; clave persistente no modificada. |
| AI-04 | PASS | 2026-08-26 | `1390EBA6BCEBEBE6C73D08B78738826DABDE8999FD20071E759975D1D5A68AD7` | Sin red se informó fallo/timeout, permanecieron reglas y reintento, cancelar no escribió y Civil 3D siguió operativo al reconectar. |
| UNDO-03 | PASS | 2026-08-26 | `1390EBA6BCEBEBE6C73D08B78738826DABDE8999FD20071E759975D1D5A68AD7` | Un único `UNDO` revirtió todo el lote remoto aplicado. |
| CFG-01 | PASS | 2026-08-26 | `1390EBA6BCEBEBE6C73D08B78738826DABDE8999FD20071E759975D1D5A68AD7` | `Successful` y `REGRESION_CIVILSPELL_2026` persistieron; sin clave, patrón de secreto ni `.tmp` en configuración. |

## 8. Cierre

Hito 1 cerrado el 2026-08-26: todas las filas obligatorias tienen `PASS`, la
única incidencia detectada está enlazada y cerrada, y no quedan defectos
críticos abiertos. La matriz principal, `CURRENT_STATUS.md` y el changelog se
actualizaron con este corte.

## 9. Regresión mínima del adaptador del Hito 2

Ejecutar esta sección con la DLL Debug del corte automatizado de 52 pruebas y
dos dibujos desechables abiertos. No requiere OpenAI real.

1. **H2-SEL-01:** ejecutar `AISPELL` sobre F-01, revisar la alternativa local y
   cancelar. Debe seleccionarse mediante el nuevo contexto y no escribir.
2. **H2-BATCH-01:** ejecutar `AISPELLALL`, comprobar que aparecen F-01, F-02 y
   F-03, excluir una fila, aplicar y revertir con un único `UNDO`.
3. **H2-LIFE-01:** habilitar `SlowSuccessful`, cerrar con **X** durante la carga
   y esperar seis segundos. No debe incorporarse una respuesta tardía ni
   modificarse el dibujo.
4. **H2-SAFE-01:** ejecutar `AISPELLTESTCONFLICT` y
   `AISPELLTESTBATCHCONFLICT`. Ambos comandos deben mostrar `PASS` y conservar
   los textos.
5. **H2-DOC-01:** con dos dibujos desechables abiertos, ejecutar
   `AISPELLTESTDOCUMENTSWITCH`, seleccionar F-01 y exigir `PASS DOC-01`. El
   comando cambia temporalmente el documento activo, intenta aplicar mediante
   el contexto capturado, exige `DocumentMismatch`, restaura el dibujo original
   como activo y comprueba que el texto siga idéntico.

| ID | Resultado | Fecha/hora | SHA-256 DLL | Evidencia anonimizada / incidencia |
| --- | --- | --- | --- | --- |
| H2-SEL-01 | PASS | 2026-08-27 | `E264C705…04B0AC` | `AISPELL` seleccionó F-01; Cancelar conservó exactamente `LA CARRETERAA ESTA EN UBCACION`. Confirmado por el propietario. |
| H2-BATCH-01 | PASS | 2026-08-27 | `E264C705…04B0AC` | `AISPELLALL` mostró DBText/MText sin fallos, respetó la fila inglesa excluida, aplicó cuatro correcciones y un único `UNDO` restauró los cuatro originales. Confirmado por captura y por el propietario. |
| H2-LIFE-01 | PASS | 2026-08-27 | `E264C705…04B0AC` | Cerrar con X durante `SlowSuccessful` conservó el texto, no reabrió la ventana tras seis segundos y Civil 3D permaneció operativo. Confirmado por el propietario. |
| H2-SAFE-01 | PASS | 2026-08-27 | `E264C705…04B0AC` | `AISPELLTESTCONFLICT` mostró `PASS SAFE-02` y `AISPELLTESTBATCHCONFLICT` mostró `PASS BATCH-02`; ambos conservaron los textos y el lote no tuvo escrituras parciales. |
| H2-DOC-01 | PASS | 2026-08-27 | `E264C705…04B0AC` | `AISPELLTESTDOCUMENTSWITCH` mostró `PASS DOC-01`; el cambio temporal de dibujo devolvió `DocumentMismatch`, restauró el documento original como activo y conservó el texto intacto. |

## 10. Regresión del diagnóstico seguro del Hito 3

Ejecutar con la DLL Debug `1CDE491B…0051EB` y un dibujo desechable:

1. Abrir `AISPELLSETTINGS`, activar **Guardar eventos diagnósticos locales para
   soporte** y guardar.
2. Ejecutar `AISPELL` sobre F-01 y cancelar la revisión.
3. Volver a `AISPELLSETTINGS`, pulsar **Exportar eventos…** y guardar un `.jsonl`
   en una ubicación temporal elegida por el usuario.
4. Revisar que cada línea contenga únicamente `timestampUtc`, `version`,
   `command`, `code`, `severity`, `durationMs` e `itemCount`; F-01, rutas, DWG,
   handles, prompts, respuestas y credenciales deben estar ausentes.
5. Pulsar **Borrar eventos**, confirmar y comprobar que una nueva exportación
   informa que no existen eventos. La copia ya exportada debe conservarse.
6. Desactivar el diagnóstico al terminar si no se desea conservarlo activo.

| ID | Resultado | Fecha/hora | SHA-256 DLL | Evidencia anonimizada / incidencia |
| --- | --- | --- | --- | --- |
| H3-DIAG-01 | PASS | 2026-08-27 | `1CDE491B…0051EB` | JSONL limitado a siete campos; copia exportada conservada y registro interno vacío tras borrar, sin abrir el selector. |
| H3-DIAG-02 | PASS | 2026-08-27 13:30 -05:00 | `1CDE491B…0051EB` | El escenario `Timeout` produjo `TMO-001`/`Warning` para un elemento en 19,093 ms y mantuvo disponible la cancelación segura. El JSONL conservó únicamente los siete campos permitidos, sin contenido del texto ni datos del dibujo. |
