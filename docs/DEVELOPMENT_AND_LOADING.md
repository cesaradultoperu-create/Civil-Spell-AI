# Desarrollo, pruebas y carga en Civil 3D 2024, 2025 y 2026

## Requisitos locales

- Windows con .NET Framework 4.8.
- Civil 3D 2024, 2025 o 2026 de 64 bits instalado para la prueba interactiva.
- Visual Studio 2022 o .NET SDK que pueda compilar proyectos clásicos de .NET
  Framework.
- Referencias disponibles en las rutas definidas en `CivilSpellAI.csproj`:
  `acmgd.dll`, `acdbmgd.dll`, `accoremgd.dll` y `AeccDbMgd.dll`.
- Opcional para la revisión gramatical ampliada: un servidor LanguageTool local
  iniciado en `http://127.0.0.1:8081`. CivilSpellAI no instala ni inicia Java o
  LanguageTool y nunca admite un endpoint remoto. La instalación standalone y
  el comando del servidor se describen en la
  [documentación oficial](https://dev.languagetool.org/http-server.html).

El complemento está dirigido a x64. En Visual Studio seleccionar la
configuración `Debug|x64` o `Release|x64` antes de compilar.

## Compilar el complemento

1. Cerrar Civil 3D/AutoCAD, o descargar el complemento, antes de recompilar la
   misma DLL que está cargada.
2. Compilar la solución con Visual Studio o ejecutar:

   ```powershell
   dotnet build CivilSpellAI.slnx --configuration Debug --no-restore
   ```

3. Para `x64`, seleccionar esa plataforma en Visual Studio. La salida esperada
   es `bin\x64\Debug\CivilSpellAI.dll`; la configuración `AnyCPU` actual usa
   `bin\Debug\CivilSpellAI.dll` aunque el ensamblado se dirige a x64.

## Cargar y probar manualmente

1. Abrir Civil 3D 2024.
2. Ejecutar `NETLOAD`.
3. Seleccionar la DLL compilada.
4. Ejecutar `AISPELL` y seleccionar un `DBText`, `MText`, `MLeader` textual o
   atributo de una copia de prueba del dibujo.

`AISPELL` abre una ventana WPF modal con el texto original, idioma detectado,
propuesta local, diff calculado y estado de validación. **Aplicar** solo se
habilita para una propuesta segura. **Mantener original**, **Cancelar** y cerrar
la ventana no escriben en el dibujo.

`AISPELLSETTINGS` abre la configuración local. Permite habilitar LanguageTool
local, la IA simulada, elegir un escenario determinista y editar el glosario
personal. LanguageTool debe estar iniciado antes de la revisión; si no responde,
el fallo queda contenido y las reglas integradas continúan disponibles. Los archivos se
guardan en `%LOCALAPPDATA%\CivilSpellAI\` y no contienen claves ni texto de
dibujos. Para probar varias alternativas, seleccionar `Successful`; para probar
degradación segura usar `Unavailable`, `Timeout` o `InvalidResponse`; para
comprobar el bloqueo técnico usar `UnsafeTechnicalChange` con un texto que
contenga un número.

En el equipo de desarrollo, administrar la instancia local con:

```powershell
.\scripts\Manage-LanguageToolLocal.ps1 -Action Start
.\scripts\Manage-LanguageToolLocal.ps1 -Action Status
.\scripts\Test-LanguageToolLocalIntegration.ps1 -Configuration Debug
.\scripts\Manage-LanguageToolLocal.ps1 -Action Stop
```

El inicio usa una ventana oculta y registra PID, salida y errores junto a la
instalación local versionada. `Stop` solo termina el proceso si su PID coincide
con el listener administrado del puerto 8081.

`AISPELLALL` escanea `DBText`, `MText`, `MLeader` textual, celdas simples de
`Table`, atributos y textos fijos de bloques locales. La ventana preselecciona
la propuesta segura más completa por destino; cada fila puede excluirse.
**Aplicar seleccionados** revalida todo el lote antes de escribir: un conflicto
impide cualquier cambio y una sola orden `U` revierte la operación completa.
Los textos fijos se corrigen en la definición y afectan todas las inserciones
del bloque. No se inspeccionan Xrefs, celdas con fórmula/campo/vínculo de datos
ni etiquetas nativas de Civil 3D.

Matriz manual mínima antes de cerrar la estabilización del MVP:

1. Revisar y aplicar un `DBText` con una regla conocida.
2. Ejecutar `U` y comprobar que restaura el texto original.
3. Repetir con un `MText`, incluyendo un salto o formato existente.
4. Repetir usando **Mantener original**, **Cancelar** y el cierre de ventana;
   comprobar que el objeto permanece idéntico.
5. Provocar un cambio del objeto después del snapshot y comprobar que se
   informa el conflicto sin sobrescribirlo.

Para comprobar la ayuda angular de MLeader, seleccionar un MLeader nativo y
mover el pinzamiento de su flecha. Deben aparecer guías transitorias cada 15°,
el tooltip debe mostrar el ángulo ajustado y la distancia al vértice del lado
del texto debe permanecer constante. Confirmar también que `Esc` no altera la
entidad y que los valores anteriores de `AUTOSNAP` y `ORTHOMODE` se restauran
tanto al aceptar como al cancelar. La matriz completa está en
[MLEADER_POLAR_TRACKING_RUNBOOK.md](testing/MLEADER_POLAR_TRACKING_RUNBOOK.md).

## DLL bloqueada

Si MSBuild informa que no puede copiar `CivilSpellAI.dll` porque otro proceso la
usa, Civil 3D/AutoCAD mantiene el complemento cargado. No se deben borrar ni
sobrescribir archivos a la fuerza. Cerrar el proceso que lo usa o descargar el
complemento y volver a compilar.

Para validar una compilación sin tocar la salida cargada, usar un directorio
temporal aislado:

```powershell
$validationRoot = Join-Path $env:TEMP "CivilSpellAI-validation"
dotnet build CivilSpellAI.csproj --configuration Debug --no-restore `
  -p:OutputPath="$validationRoot\bin\" `
  -p:BaseIntermediateOutputPath="$validationRoot\obj\"
```

## Pruebas del núcleo de corrección

Las pruebas de la carpeta `Tests/` no dependen de AutoCAD ni de paquetes NuGet.
Compilan directamente los archivos de `Spell/` para comprobar el comportamiento
del motor y evitar bloquear la DLL del complemento.

Ejecutar:

```powershell
.\scripts\Run-SpellCoreTests.ps1
```

Si la política local bloquea scripts, usar:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Run-SpellCoreTests.ps1
```

El script compila `Tests\CivilSpellAI.Tests.csproj` y ejecuta su runner. Debe
terminar con `Resultados: 150 correctos, 0 fallidos.` Los casos de aceptación
anonimizados para el futuro validador técnico están en
`Tests\TestCases\civil3d-annotations.json`.

La matriz completa, incluidos OpenAI, conflicto, cancelación y `UNDO`, está en
[product/10_MVP_STATUS_AND_TEST_PLAN.md](product/10_MVP_STATUS_AND_TEST_PLAN.md).

## Construir el paquete piloto

Ejecutar:

```powershell
.\scripts\Build-PilotBundle.ps1 -Version 1.3.3.0
```

El script vuelve a validar `Release|x64`, comprueba que la versión del
ensamblado y del manifiesto coincidan y genera el Application Bundle y su ZIP
bajo `artifacts\distribution`. También rechaza comandos no públicos, una
plataforma distinta de Civil 3D 2024/2025/2026 x64 o DLL de Autodesk copiadas.

La instalación, actualización, reversión y desinstalación del paquete están en
[PILOT_INSTALLATION.md](PILOT_INSTALLATION.md). Al cambiar de versión se debe
actualizar `AssemblyInfo.cs`, `AppVersion` y `ProductCode`; `UpgradeCode` debe
permanecer estable.

## Archivos generados

`bin/`, `obj/` y `.vs/` se excluyen del repositorio. No deben editarse ni
confirmarse; son generados por la compilación o el IDE. Las DLL de Autodesk se
resuelven desde la instalación local de Civil 3D y tampoco forman parte del
código fuente del proyecto.
