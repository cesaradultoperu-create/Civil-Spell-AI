# SOTRAMI Excel v34

Corrige la omisión de los planos de la etapa final `1107.001.00.16-300-…`.
`PlanCodes` define el alcance común para la lectura de LE, el conjunto, los
vínculos de los rótulos, las alertas y la renumeración. Se conservan las reglas
de revisión, fechas y numeración por disciplina. La tabla añade un encabezado
de etapa final y conserva el orden del Excel.

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
