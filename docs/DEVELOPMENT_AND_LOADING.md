# Desarrollo, pruebas y carga en Civil 3D 2024

## Requisitos locales

- Windows con .NET Framework 4.8.
- Civil 3D 2024 / AutoCAD 2024 de 64 bits instalado.
- Visual Studio 2022 o .NET SDK que pueda compilar proyectos clásicos de .NET
  Framework.
- Referencias disponibles en las rutas definidas en `CivilSpellAI.csproj`:
  `acmgd.dll`, `acdbmgd.dll`, `accoremgd.dll` y `AeccDbMgd.dll`.

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
4. Ejecutar `AISPELL` y seleccionar un `DBText` o `MText` de una copia de prueba
   del dibujo.

`AISPELL` abre una ventana WPF modal con el texto original, idioma detectado,
propuesta local, diff calculado y estado de validación. **Aplicar** solo se
habilita para una propuesta segura. **Mantener original**, **Cancelar** y cerrar
la ventana no escriben en el dibujo.

`AISPELLSETTINGS` abre la configuración local. Permite habilitar la IA simulada,
elegir un escenario determinista y editar el glosario personal. Los archivos se
guardan en `%LOCALAPPDATA%\CivilSpellAI\` y no contienen claves ni texto de
dibujos. Para probar varias alternativas, seleccionar `Successful`; para probar
degradación segura usar `Unavailable`, `Timeout` o `InvalidResponse`; para
comprobar el bloqueo técnico usar `UnsafeTechnicalChange` con un texto que
contenga un número.

`AISPELLALL` escanea todos los `DBText` y `MText` directos de los espacios de
modelo y presentación. La ventana preselecciona la propuesta segura más completa
por objeto; cada fila puede excluirse. **Aplicar seleccionados** revalida todo el
lote antes de escribir: un conflicto impide cualquier cambio y un único `UNDO`
revierte la operación completa. No se inspeccionan todavía atributos de bloque,
tablas ni etiquetas nativas de Civil 3D.

Matriz manual mínima antes de cerrar la estabilización del MVP:

1. Revisar y aplicar un `DBText` con una regla conocida.
2. Ejecutar `UNDO` y comprobar que restaura el texto original.
3. Repetir con un `MText`, incluyendo un salto o formato existente.
4. Repetir usando **Mantener original**, **Cancelar** y el cierre de ventana;
   comprobar que el objeto permanece idéntico.
5. Provocar un cambio del objeto después del snapshot y comprobar que se
   informa el conflicto sin sobrescribirlo.

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
terminar con `Resultados: 73 correctos, 0 fallidos.` Los casos de aceptación
anonimizados para el futuro validador técnico están en
`Tests\TestCases\civil3d-annotations.json`.

La matriz completa, incluidos OpenAI, conflicto, cancelación y `UNDO`, está en
[product/10_MVP_STATUS_AND_TEST_PLAN.md](product/10_MVP_STATUS_AND_TEST_PLAN.md).

## Construir el paquete piloto

Ejecutar:

```powershell
.\scripts\Build-PilotBundle.ps1 -Version 1.1.4.0
```

El script vuelve a validar `Release|x64`, comprueba que la versión del
ensamblado y del manifiesto coincidan y genera el Application Bundle y su ZIP
bajo `artifacts\distribution`. También rechaza comandos no públicos, una
plataforma distinta de Civil 3D 2024 x64 o DLL de Autodesk copiadas.

La instalación, actualización, reversión y desinstalación del paquete están en
[PILOT_INSTALLATION.md](PILOT_INSTALLATION.md). Al cambiar de versión se debe
actualizar `AssemblyInfo.cs`, `AppVersion` y `ProductCode`; `UpgradeCode` debe
permanecer estable.

## Archivos generados

`bin/`, `obj/` y `.vs/` se excluyen del repositorio. No deben editarse ni
confirmarse; son generados por la compilación o el IDE. Las DLL de Autodesk se
resuelven desde la instalación local de Civil 3D y tampoco forman parte del
código fuente del proyecto.
