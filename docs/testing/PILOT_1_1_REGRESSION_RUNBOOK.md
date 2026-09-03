# Regresión del piloto CivilSpellAI 1.1.5.0

Fecha de preparación: 2026-08-28. Ejecución principal: 2026-09-02.

Esta matriz cierra los Hitos 4, 5 y 6. Debe ejecutarse en Civil 3D 2024 con un
DWG desechable y sin otros dibujos de trabajo abiertos. No se registran textos
reales, respuestas de OpenAI ni credenciales.

## 1. Artefacto autorizado

- ZIP: `CivilSpellAI-1.1.5.0.zip`.
- SHA-256: se toma de `docs/codex/NEXT_TASK.md` y se vuelve a calcular antes de
  instalar. No se incrusta aquí porque este runbook forma parte del propio ZIP.
- Plataforma: Windows x64, Civil 3D R24.3 y .NET Framework 4.8.
- Alcance recomendado para el piloto sin firma: `AllUsers` bajo
  `%PROGRAMFILES%\Autodesk\ApplicationPlugins`.

Detener la sesión ante una escritura sin confirmación, cambio de token técnico,
escritura posterior a cancelar o aplicación parcial de un lote.

## 1.1 Fixture reproducible

Antes de interrumpir una sesión de trabajo, generar el DWG desechable:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\New-PilotFixture.ps1 -Force
```

El generador usa AutoCAD Core Console, crea únicamente
`artifacts\testing\CivilSpellAI-Pilot-Fixture-1.1.5.dwg` y valida que contiene
cuatro entidades `TEXT`/`MTEXT`. Incluye los dos textos exigidos por esta matriz,
un caso inglés con estación y un MText con formato, unidad y diámetro. La huella
se registra al generarlo porque el formato DWG incorpora metadatos de sesión.

## 2. Instalación y autoloader

1. Cerrar Civil 3D y ejecutar `Test-CivilSpellAIEnvironment.ps1`.
2. Instalar con `Manage-CivilSpellAI.ps1 -Action Install -Scope AllUsers` desde
   una consola elevada.
3. Abrir Civil 3D sin usar `NETLOAD`.
4. Ejecutar `AISPELLSETTINGS`, `AISPELL` y `AISPELLALL`. Los tres deben
   registrarse y la DLL debe cargarse por la primera invocación. Si una
   migración de alcance dejó una ruta anterior en caché, ejecutar
   `APPAUTOLOADER` y **Reload** una sola vez; registrar este hecho.
5. Sobre un DBText desechable, cancelar `AISPELL` y confirmar texto idéntico.
6. Cerrar Civil 3D. Ejecutar `Update`, abrir y comprobar los comandos; cerrar,
   ejecutar `Rollback`, abrir y comprobarlos otra vez.
7. Cerrar Civil 3D y ejecutar `Uninstall`. Confirmar que el bundle desaparece
   y que `%LOCALAPPDATA%\CivilSpellAI` se conserva.
8. Reinstalar 1.1.5.0 para continuar la matriz con el candidato autorizado.

## 3. Experiencia de revisión

Abrir el fixture generado, que ya contiene `LA ESTRUTURAA EN COTA 25 m` y
`LA UBCACION DEL PROYECTOO`.

- **H5-MAN-01:** abrir el primero con `AISPELL`, activar edición manual y
  corregir únicamente la ortografía. El diff debe actualizarse y **Aplicar**
  debe quedar habilitado.
- **H5-MAN-02:** cambiar manualmente `25 m` por `999 m`. Debe aparecer la
  advertencia técnica y **Aplicar** debe quedar deshabilitado.
- **H5-BATCH-01:** ejecutar `AISPELLALL`; verificar conteo/progreso y cancelar
  durante la preparación. No debe abrirse la revisión ni modificarse el DWG.
- **H5-BATCH-02:** repetir, buscar por texto y entidad, filtrar por layout,
  origen/estado/validación, cambiar la alternativa de una fila y excluir otra.
  Aplicar y comprobar que una sola orden `U` restaura todo.
- **UNDO-04:** en desarrollo, ejecutar `scripts\Test-BatchUndoIntegration.ps1`.
  Debe aplicar dos cambios con la frontera usada por `AISPELLALL`, ejecutar una
  sola `U` y confirmar la restauración de ambos textos.
- **H5-A11Y-01:** recorrer las tres ventanas con Tab/Shift+Tab, activar botones
  con sus teclas de acceso y comprobar foco visible con escalado de Windows al
  125 %. Ningún control indispensable debe quedar inaccesible.
- **H5-AI-01:** en Ajustes, autorizar el texto fijo y pulsar **Probar conexión**.
  Debe advertir el coste, mostrar literalmente la frase fija antes del envío y
  no guardar ajustes ni respuestas. Cancelar la confirmación no debe enviar.

## 4. Memoria y glosarios

- **H6-MEM-01:** aplicar una corrección marcando **Recordar esta decisión
  localmente**, ejecutar `UNDO` y revisar de nuevo el original. Debe aparecer
  una alternativa identificada como memoria local, sin autoaplicarse.
- **H6-MEM-02:** desde Ajustes buscar el recuerdo, desactivarlo y guardar. Ya no
  debe sugerirse. Reactivarlo, exportarlo y comprobar la advertencia de
  contenido; borrar el recuerdo y confirmar su ausencia.
- **H6-MEM-03:** recordar decisiones en un lote y verificar que solo se guardan
  después de una aplicación atómica correcta.
- **H6-ORG-01:** si el administrador instala
  `%PROGRAMDATA%\CivilSpellAI\organizational-glossary.txt`, confirmar su
  conteo de solo lectura en Ajustes y que un término contenido queda protegido.
  El archivo no debe modificarse desde CivilSpellAI.

## 5. Evidencia de cierre

| ID | Resultado | Fecha/hora | SHA-256 DLL/ZIP | Evidencia anonimizada o incidencia |
| --- | --- | --- | --- | --- |
| H4-AUTO-01 | PASS | 2026-09-03 | DLL 1.1.4 `73FEDCF3…53E687A` | `AISPELLSETTINGS` cargó la instalación final desde `Program Files` sin `NETLOAD`, recarga ni aviso de firma. La migración inicial de alcance requirió un único `APPAUTOLOADER` → `Reload`; REG-2026-004. |
| H4-LIFE-01 | PASS | 2026-09-02 | DLL 1.1.3 `DF6F2AF2…BC4E6` / 1.1.4 `73FEDCF3…53E687A` | Update 1.1.3→1.1.4, rollback, desinstalación y reinstalación pasaron por hash; configuración, memoria y diagnóstico locales permanecieron idénticos. |
| H5-MAN-01/02 | PASS | 2026-09-02 | DLL 1.1.2 `7D734D79…1DAC` | Edición segura aplicó el punto exacto; cambio 25→26 quedó bloqueado. |
| H5-BATCH-01/02 | PASS | 2026-09-02 | DLL 1.1.3 `DF6F2AF2…BC4E6` | Filtros, alternativas, selección, aplicación y único `UNDO` pasan. `SlowSuccessful` confirmó «Preparación cancelada» y cero modificaciones. REG-2026-002/003 cerradas. |
| UNDO-04 | PASS | 2026-09-03 | DLL 1.1.5 `11A33B58…138331` | AutoCAD Core Console restauró dos cambios con una sola `U`; la instalación administrada aplicó tres correcciones en el fixture limpio, mostró «Use U una vez» y el propietario confirmó la reversión completa. REG-2026-005. |
| H5-A11Y-01 | PASS | 2026-09-03 | DLL 1.1.4 `73FEDCF3…53E687A` | Configuración, revisión individual y lote recorrieron Tab/Shift+Tab al 125 % con foco visible, desplazamiento, acciones accesibles y fila sincronizada; sin recortes ni superposición. |
| H5-AI-01 | PASS | 2026-09-02 | DLL 1.1.2 `7D734D79…1DAC` | Cancelación visible sin resultado tardío y conexión de texto fijo completada. |
| H6-MEM-01/02/03 | PASS | 2026-09-02 | DLL 1.1.2 `7D734D79…1DAC` | Recuerdo visible, desactivación persistente, selección y borrado total confirmados. |
| H6-ORG-01 | NO APLICA | 2026-09-02 | | Glosario organizacional no instalado en el equipo piloto. |

La versión 1.1.3.0 corrigió y revalidó REG-2026-002 y REG-2026-003. La 1.1.4.0
sincroniza la fila seleccionada con el foco dentro de sus controles, añade ayuda
accesible para propuestas bloqueadas y nombres a estados dinámicos. Pasó 105/105
pruebas, carga administrada, ciclo de vida real y H5-A11Y-01 al 125 %.

La versión 1.1.5.0 corrige la instrucción de reversión de `AISPELLALL`, protege
el fixture base mediante una copia temporal y cerró `UNDO-04` tanto de forma
automatizada como interactiva.

Los Hitos 4–6 quedaron cerrados el 2026-09-03: todas las filas aplicables están
en PASS, el único caso no aplicable está justificado y no existen incidencias
críticas abiertas.
