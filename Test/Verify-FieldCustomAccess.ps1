$ErrorActionPreference='Stop'
$cadInstall=Join-Path $env:ProgramFiles 'Autodesk/AutoCAD 2024'
Add-Type -Path (Join-Path $cadInstall 'AcSmComponents.Interop.dll')
Add-Type @'
using System.Runtime.InteropServices;
public static class FieldDependencyPath {
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode)] public static extern bool SetDllDirectory(string path);
}
'@
[void][FieldDependencyPath]::SetDllDirectory($cadInstall)
[xml]$configuration=Get-Content -LiteralPath (Join-Path $env:LOCALAPPDATA 'CasillasField/projects.xml')
$project=$configuration.SelectSingleNode('/Projects/Project[@name="SOTRAMI"]')
if(-not $project -or $project.editValues -ne 'true'){throw 'No se habilito el perfil.'}
Add-Type -ReferencedAssemblies (Join-Path $cadInstall 'AcSmComponents.Interop.dll') @'
using System;
using Autodesk.AutoCAD.Interop;
public static class FieldCustomRead {
 public static string Read(string path,string expected) {
  var manager=new AcSmSheetSetMgrClass();var database=manager.OpenDatabase(path,false);
  try {
   var set=database.GetSheetSet();
   if(!String.Equals(set.GetObjectId().GetHandle(),expected,StringComparison.OrdinalIgnoreCase))throw new Exception("No coincide el conjunto.");
   return Convert.ToString(set.GetCustomPropertyBag().GetProperty("03_R2_Ds").GetValue());
  }finally{manager.Close(database);}
 }
}
'@
$value=[FieldCustomRead]::Read([string]$project.dst,[string]$project.sheetSet)
Write-Output ('VERIFICADO: SOTRAMI, identidad del conjunto y propiedad personalizada 03_R2_Ds = '+$value)
Write-Output 'Lectura completada sin LockDb ni escrituras de valores.'
