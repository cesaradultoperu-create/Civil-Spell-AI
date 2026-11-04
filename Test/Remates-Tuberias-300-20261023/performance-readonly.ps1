$ErrorActionPreference = 'Stop'
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class CadMemoryProbe {
 [StructLayout(LayoutKind.Sequential)] public class Status {
  public uint Length=(uint)Marshal.SizeOf(typeof(Status)); public uint Load;
  public ulong TotalPhysical,AvailablePhysical,TotalPage,AvailablePage,TotalVirtual,AvailableVirtual,AvailableExtended;
 }
 [DllImport("kernel32.dll",SetLastError=true)] public static extern bool GlobalMemoryStatusEx([In,Out] Status state);
}
'@
$state = New-Object CadMemoryProbe+Status
[void][CadMemoryProbe]::GlobalMemoryStatusEx($state)
$report = [ordered]@{MemoryLoadPercent=$state.Load; TotalRAM_GB=[math]::Round($state.TotalPhysical/1GB,2); AvailableRAM_GB=[math]::Round($state.AvailablePhysical/1GB,2)}
try {
 . "$PSScriptRoot/../CasillasField-Sotrami-20261022/session-api.ps1"
 $app = [FieldDemoWindow]::Active()
 $acadProcess = Get-Process acad | Select-Object -First 1
 $report.Windows = @([FieldDemoWindow]::Windows($acadProcess.Id) | ForEach-Object { [ordered]@{Title=[FieldDemoWindow]::Title($_); Class=[FieldDemoWindow]::Class($_); Enabled=[FieldDemoWindow]::IsWindowEnabled($_)} })
 $report.Quiescent = $app.GetAcadState().IsQuiescent
 $report.Documents = @($app.Documents | ForEach-Object { [ordered]@{Name=$_.Name; Path=$_.FullName; Saved=$_.Saved} })
 $doc = $app.ActiveDocument
 $report.ActiveDrawing = $doc.Name
 $vars = [ordered]@{}
 foreach ($key in @('CMDNAMES','CMDACTIVE','FILEDIA','SDI','DBMOD','CTAB','SELECTIONPREVIEW','SELECTIONEFFECT','PRESELECTIONEFFECT','ROLLOVERTIPS','SELECTIONCYCLING','DYNMODE','GRIPOBJLIMIT','WHIPTHREAD','LAYOUTREGENCTL','HPQUICKPREVIEW')) {
   try { $vars[$key]=$doc.GetVariable($key) } catch { $vars[$key]=$_.Exception.Message }
 }
 $report.Variables = $vars
} catch { $report.LiveReadError = $_.Exception.Message }
$report | ConvertTo-Json -Depth 6
