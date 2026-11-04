# SOTRAMI Excel v35

Corrige la omisión de los planos de la etapa final `1107.001.00.16-300-…`.
`PlanCodes` define el alcance común para la lectura de LE, el conjunto, los
vínculos de los rótulos, las alertas y la renumeración. Se conservan las reglas
de revisión, fechas y numeración por disciplina. La tabla añade un encabezado
de etapa final y conserva el orden del Excel.

La v35 crea un grupo raíz **ETAPA FINAL - 16-300** en Sheets, mueve allí
las hojas finales existentes y coloca también las nuevas altas de esa etapa.
Conserva los GUID de las hojas, sus referencias de presentación y los campos
de rótulo. `SOTRAMIETAPAS` reorganiza únicamente el conjunto existente, sin
editar DWG ni sus títulos. SOTRAMIEXCEL mantiene la organización al actualizar.

Prueba nativa del traslado y la persistencia después de reabrir:
`Tests/SubsetTests.cs`, con `IncludeSubsetTests=true`. Evidencia:
`Test/Sotrami-Sheets-v35-20261030/Ensayo4/subset-test.json`.

También se comprobó la creación de dos DWG finales nuevos, su incorporación
directa al grupo final y una segunda sincronización estable. Evidencia:
`Test/Sotrami-16-300-20261029/Ensayo-v35-NewSheets/native-test.json`.
`Tests/Test-Package.ps1` valida instalación y reinstalación en una carpeta
aislada, conservando configuración, registros y otros plugins. No opera sobre
la instalación del usuario ni sobre la sesión abierta de Civil 3D.

Fuentes recuperadas del archivo v33 de la carpeta de programas del usuario.
No se modificó el resto de CivilSpellAI.

Compilación:

```powershell
dotnet build tools/SotramiExcel/ExcelPlanSync.Multi.csproj -c Release -p:NuGetAudit=false
dotnet build tools/SotramiExcel/ExcelPlanSync.Multi.csproj -c Release -p:CadFamily=2025 -p:BaseIntermediateOutputPath=obj/2025/ -p:MSBuildProjectExtensionsPath=obj/2025/ -p:NuGetAudit=false
dotnet run --project tools/SotramiExcel/Tests/PlanData.Tests.csproj -- Test/Sotrami-16-300-20261029/Excel-original.xlsx
```

La prueba nativa requiere `IncludeNativeTests=true` y una configuración aislada
en `Test/Sotrami-16-300-20261029/Ensayo`. No permite ejecutarse en el proyecto Q:.
Comprobó 52 entradas, dos altas a partir de la plantilla, preservación del DWG
final existente, 52 vínculos y una segunda sincronización sin duplicados ni
cambios de identidad de las hojas. Ejecutada en el motor AutoCAD/Civil 2024.
Las versiones 2023 y 2025 se compilaron; no se ejecutaron en esos hosts.

`SOTRAMIDIAGNOSTICO` genera un informe de lectura con altas, retiros y DWG
faltantes. `SOTRAMIEXCEL` mantiene las operaciones existentes de v33: actualiza
la tabla y DST, crea marcos vacíos para códigos sin DWG y archiva los retirados.
