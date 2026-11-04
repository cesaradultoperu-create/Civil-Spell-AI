$ErrorActionPreference='Stop'
$taskRoot=(Resolve-Path -LiteralPath 'C:\Users\Cesar Ugaz\Pictures\poner fotos en cuadro').ProviderPath.TrimEnd('\')
$taskReport=$PSScriptRoot
function Get-TaskSharedHash([string]$taskFilePath){
 $taskStream=[IO.File]::Open($taskFilePath,[IO.FileMode]::Open,[IO.FileAccess]::Read,([IO.FileShare]::ReadWrite -bor [IO.FileShare]::Delete))
 try{$taskSha=[Security.Cryptography.SHA256]::Create();try{return [BitConverter]::ToString($taskSha.ComputeHash($taskStream)).Replace('-','')}finally{$taskSha.Dispose()}}finally{$taskStream.Dispose()}
}
$taskNames=@('Comando RELAVEAUTO prueba 0.3.2')
$taskTargets=@()
foreach($taskName in $taskNames){
 $taskCandidate=Join-Path $taskRoot $taskName
 if(!(Test-Path -LiteralPath $taskCandidate)){continue}
 $taskItem=Get-Item -LiteralPath $taskCandidate -Force
 $taskAbsolute=[IO.Path]::GetFullPath($taskItem.FullName)
 if(!$taskItem.PSIsContainer -or !$taskAbsolute.StartsWith($taskRoot+'\',[StringComparison]::OrdinalIgnoreCase) -or $taskItem.Parent.FullName -ne $taskRoot){throw "Destino fuera del alcance: $taskAbsolute"}
 if(($taskItem.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0){throw "No se procesa un enlace: $taskAbsolute"}
 $taskLinks=@(Get-ChildItem -LiteralPath $taskAbsolute -Force -Recurse | Where-Object {($_.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0})
 if($taskLinks.Count){throw "Hay enlaces dentro de $taskAbsolute"}
 $taskTargets+=$taskAbsolute
}

# Protect the current command, originals and the input folders with before/after hashes.
$taskTrials=Join-Path $taskRoot 'se aplica los 4 pasos para todos'
$taskTrialFolders=@(Get-ChildItem -LiteralPath $taskTrials -Directory -Force | Sort-Object Name)
if($taskTrialFolders.Count -ne 12){throw 'El inventario de ensayos no coincide con los 12 revisados.'}
$taskProtected=@(Get-ChildItem -LiteralPath $taskRoot -File -Force)
$taskProtected+=@(Get-ChildItem -LiteralPath $taskTrials -File -Force)
foreach($taskTrial in $taskTrialFolders){$taskProtected+=@(Get-ChildItem -LiteralPath $taskTrial.FullName -File -Force | Where-Object {$_.Extension -eq '.dwg' -or $_.Extension -eq '.png' -or $_.Name -eq 'relave-calibracion.json'})}
$taskCurrent=Join-Path $taskRoot 'Comando RELAVEAUTO prueba 0.3.3'
$taskProtected+=@(Get-ChildItem -LiteralPath $taskCurrent -File -Recurse -Force)
$taskBefore=@($taskProtected | Sort-Object FullName -Unique | ForEach-Object {[pscustomobject]@{Path=$_.FullName;SHA256=(Get-TaskSharedHash $_.FullName)}})
$taskBefore | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath (Join-Path $taskReport 'protegidos-antes.json') -Encoding UTF8
$taskTargets | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $taskReport 'destinos-verificados.json') -Encoding UTF8

# Windows shell recycle operation with all prompts and error UI suppressed.
# No deletion uses cmd.exe, wildcard expansion or paths outside the verified list.
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class RelaveRecycle20261103 {
 [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]
 struct FileOperation {
  public IntPtr hwnd; public uint operation;
  [MarshalAs(UnmanagedType.LPWStr)] public string from;
  [MarshalAs(UnmanagedType.LPWStr)] public string to;
  public ushort flags;
  [MarshalAs(UnmanagedType.Bool)] public bool aborted;
  public IntPtr mappings;
  [MarshalAs(UnmanagedType.LPWStr)] public string title;
 }
 [DllImport("shell32.dll",CharSet=CharSet.Unicode,EntryPoint="SHFileOperationW")]
 static extern int SHFileOperation(ref FileOperation operation);
 public static int Recycle(string path) {
  var operation=new FileOperation { operation=3,from=path+"\0\0",flags=0x0454 };
  int result=SHFileOperation(ref operation);
  return operation.aborted&&result==0?1223:result;
 }
}
'@
$taskStatus=@()
foreach($taskTarget in $taskTargets){
 $taskLocked=$null
 foreach($taskFile in (Get-ChildItem -LiteralPath $taskTarget -Recurse -File -Force)){
  try{$taskStream=[IO.File]::Open($taskFile.FullName,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None);$taskStream.Dispose()}
  catch{$taskLocked=$taskFile.FullName;break}
 }
 if($taskLocked){$taskStatus+=[pscustomobject]@{Carpeta=$taskTarget;Estado='Conservada: archivo en uso o sin acceso exclusivo';Detalle=$taskLocked};continue}
 $taskCode=[RelaveRecycle20261103]::Recycle($taskTarget)
 $taskExists=Test-Path -LiteralPath $taskTarget
 $taskStatus+=[pscustomobject]@{Carpeta=$taskTarget;Estado=$(if($taskCode -eq 0 -and !$taskExists){'Enviada a la Papelera'}else{'No eliminada'});Detalle="Codigo=$taskCode; existe=$taskExists"}
}
$taskStatus | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $taskReport 'resultado-limpieza.json') -Encoding UTF8
foreach($taskProtectedFile in $taskBefore){
 if(!(Test-Path -LiteralPath $taskProtectedFile.Path)){throw "Falta un archivo protegido: $($taskProtectedFile.Path)"}
 if((Get-TaskSharedHash $taskProtectedFile.Path) -ne $taskProtectedFile.SHA256){throw "Cambio en un archivo protegido: $($taskProtectedFile.Path)"}
}
$taskAfterFolders=@(Get-ChildItem -LiteralPath $taskTrials -Directory -Force | Sort-Object Name)
if(($taskTrialFolders.Name -join '|') -ne ($taskAfterFolders.Name -join '|')){throw 'Cambio el listado de los ensayos.'}
Write-Output 'Verificados: version 0.3.3, originales protegidos y 12 ensayos intactos.'
$taskStatus | Format-List
Get-ChildItem -LiteralPath $taskRoot -Force | Select-Object Name,Mode | Format-Table -AutoSize



