param([Parameter(Mandatory=$true)][string]$OutputDirectory,[switch]$InvokeEditor,[switch]$CancelEditor,[switch]$Next)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type @'
using System;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;
public static class CasillasCapture {
  public delegate bool EnumProc(IntPtr hwnd,IntPtr param);
  [StructLayout(LayoutKind.Sequential)] public struct Rect {public int Left,Top,Right,Bottom;}
  [DllImport("user32.dll")] static extern bool EnumWindows(EnumProc callback,IntPtr param);
  [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr hwnd,out uint process);
  [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern int GetWindowText(IntPtr hwnd,StringBuilder value,int count);
  [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hwnd);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr handle,out Rect rect);
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr handle,IntPtr hdc,uint flags);
  [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr handle,uint message,IntPtr w,IntPtr l);
  public static IntPtr[] Windows(int process) {
    var result=new List<IntPtr>();
    EnumWindows((h,p)=>{uint id;GetWindowThreadProcessId(h,out id);if(id==process&&IsWindowVisible(h))result.Add(h);return true;},IntPtr.Zero);
    return result.ToArray();
  }
  public static string Title(IntPtr hwnd) {var s=new StringBuilder(512);GetWindowText(hwnd,s,512);return s.ToString();}
}
'@
$process=Get-Process acad | Where-Object {$_.MainWindowTitle -like '*Ejemplo-localizar-casillas.dwg*'} | Select-Object -First 1
if(-not $process){throw 'No se encontro la ventana del DWG de ejemplo.'}
$windows=[CasillasCapture]::Windows($process.Id)
foreach($handle in $windows) {
    $title=[CasillasCapture]::Title($handle)
    if($title -notmatch 'Localizar casillas|Enhanced Attribute Editor|Ejemplo-localizar-casillas') {continue}
    Write-Output ("WINDOW: $handle $title")
    $kind=if($title -match 'Localizar casillas'){'panel'}elseif($title -match 'Enhanced Attribute Editor'){'editor'}else{'plano'}
    $children=@()
    if($kind -ne 'plano') {
        $element=[Windows.Automation.AutomationElement]::FromHandle($handle)
        $children=$element.FindAll([Windows.Automation.TreeScope]::Descendants,[Windows.Automation.Condition]::TrueCondition)
        $names=foreach($child in $children){$child.Current.ControlType.ProgrammaticName+' | '+$child.Current.Name+' | '+$child.Current.AutomationId}
        [IO.File]::WriteAllLines((Join-Path $OutputDirectory ($kind+'-controles.txt')),[string[]]$names)
    }
    $rect=New-Object CasillasCapture+Rect
    [void][CasillasCapture]::GetWindowRect($handle,[ref]$rect)
    if($rect.Right-$rect.Left -gt 0 -and $rect.Bottom-$rect.Top -gt 0) {
        $bitmap=New-Object Drawing.Bitmap ($rect.Right-$rect.Left),($rect.Bottom-$rect.Top)
        $graphics=[Drawing.Graphics]::FromImage($bitmap);$hdc=$graphics.GetHdc()
        try {$ok=[CasillasCapture]::PrintWindow($handle,$hdc,2)}finally{$graphics.ReleaseHdc($hdc)}
        $bitmap.Save((Join-Path $OutputDirectory ($kind+'.png')));$graphics.Dispose();$bitmap.Dispose()
    }
    if($InvokeEditor -and $kind -eq 'panel') {
        $button=$children | Where-Object {$_.Current.Name -like 'Editar atributo*'} | Select-Object -First 1
        if(-not $button){throw 'No se encontro el boton del editor.'}
        $ok=[CasillasCapture]::PostMessage([IntPtr]$button.Current.NativeWindowHandle,0xF5,[IntPtr]::Zero,[IntPtr]::Zero)
        Write-Output "EDITOR_INVOKED: $ok"
    }
    if($Next -and $kind -eq 'panel') {
        $button=$children | Where-Object {$_.Current.Name -like 'Siguiente*'} | Select-Object -First 1
        if(-not $button){throw 'No se encontro Siguiente.'}
        $ok=[CasillasCapture]::PostMessage([IntPtr]$button.Current.NativeWindowHandle,0xF5,[IntPtr]::Zero,[IntPtr]::Zero)
        Write-Output "NEXT_INVOKED: $ok"
    }
    if($CancelEditor -and $kind -eq 'editor') {
        $button=$children | Where-Object {$_.Current.Name -eq 'Cancel'} | Select-Object -First 1
        if(-not $button){throw 'No se encontro Cancel en el editor.'}
        $ok=[CasillasCapture]::PostMessage([IntPtr]$button.Current.NativeWindowHandle,0xF5,[IntPtr]::Zero,[IntPtr]::Zero)
        Write-Output "EDITOR_CANCELLED: $ok"
    }
}
