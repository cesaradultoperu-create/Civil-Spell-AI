param([switch]$ValidateScreen,[switch]$InspectOnly)
$ErrorActionPreference = 'Stop'
$demoRoot = $PSScriptRoot
$drawing = Join-Path $demoRoot 'Ejemplo-localizar-casillas.dwg'
$plugin = Join-Path $demoRoot 'LocalizarCasillas.2024.dll'
$acad = Join-Path $env:ProgramFiles 'Autodesk\AutoCAD 2024\acad.exe'
foreach ($path in @($drawing, $plugin, $acad)) {
    if (-not (Test-Path -LiteralPath $path)) { throw "No se encontro: $path" }
}
Add-Type @'
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
public static class CasillasActivateWindow {
  public delegate bool EnumProc(IntPtr hwnd,IntPtr param);
  [StructLayout(LayoutKind.Sequential)] public struct Rect {public int Left,Top,Right,Bottom;}
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr handle,int command);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr handle);
  [DllImport("user32.dll")] public static extern bool IsWindowEnabled(IntPtr handle);
  [DllImport("user32.dll")] static extern bool EnumWindows(EnumProc proc,IntPtr param);
  [DllImport("user32.dll")] static extern bool EnumChildWindows(IntPtr owner,EnumProc proc,IntPtr param);
  [DllImport("user32.dll")] static extern bool PostMessage(IntPtr handle,uint message,IntPtr w,IntPtr l);
  [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr handle,out uint process);
  [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern int GetWindowText(IntPtr handle,StringBuilder text,int size);
  [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr handle);
  [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr handle,out Rect rect);
  [DllImport("user32.dll")] static extern bool MoveWindow(IntPtr handle,int x,int y,int width,int height,bool repaint);
  [DllImport("ole32.dll")] static extern int GetRunningObjectTable(int reserved,out IRunningObjectTable table);
  [DllImport("ole32.dll")] static extern int CreateBindCtx(int reserved,out IBindCtx context);
  public static object[] Documents(string fileName) {
    IRunningObjectTable table;GetRunningObjectTable(0,out table);
    IEnumMoniker list;table.EnumRunning(out list);IBindCtx ctx;CreateBindCtx(0,out ctx);
    var item=new IMoniker[1];var result=new List<object>();
    try {while(list.Next(1,item,IntPtr.Zero)==0) {
      try {string name;item[0].GetDisplayName(ctx,null,out name);
        if(String.Equals(Path.GetFileName(name),fileName,StringComparison.OrdinalIgnoreCase)) {
          object doc;table.GetObject(item[0],out doc);result.Add(doc);
        }
      } finally {Marshal.ReleaseComObject(item[0]);}
    }} finally {Marshal.ReleaseComObject(list);Marshal.ReleaseComObject(ctx);Marshal.ReleaseComObject(table);}
    return result.ToArray();
  }
  public static bool ShowPanel(int process,IntPtr main) {
    bool found=false;Rect owner;if(!GetWindowRect(main,out owner))return false;
    EnumWindows((handle,p)=>{
      uint id;GetWindowThreadProcessId(handle,out id);if(id!=process)return true;
      var title=new StringBuilder(256);GetWindowText(handle,title,256);
      if(!title.ToString().StartsWith("Localizar casillas"))return true;
      Rect panel;if(!GetWindowRect(handle,out panel))return true;
      int width=panel.Right-panel.Left,height=panel.Bottom-panel.Top;
      int x=Math.Max(owner.Left+15,owner.Right-width-25),y=owner.Top+85;
      if(y+height>owner.Bottom)y=Math.Max(owner.Top+35,owner.Bottom-height-20);
      MoveWindow(handle,x,y,width,height,true);ShowWindow(handle,5);SetForegroundWindow(handle);
      EnumChildWindows(handle,(button,unused)=>{
        var text=new StringBuilder(128);GetWindowText(button,text,128);
        if(text.ToString()!="Ubicar")return true;
        PostMessage(button,0xF5,IntPtr.Zero,IntPtr.Zero);return false;
      },IntPtr.Zero);
      found=true;return false;
    },IntPtr.Zero);
    return found;
  }
}
'@
$existing = Get-Process acad -ErrorAction SilentlyContinue | Where-Object {
    $_.MainWindowTitle -like '*Ejemplo-localizar-casillas.dwg*'
} | Select-Object -First 1
$folder = $demoRoot.Replace('\','/')
$dll = $plugin.Replace('\','/')
if ($existing) {
    # Never interrupt a modal Field/EATTEDIT dialog or send Escape to discard the user's edit.
    if (-not [CasillasActivateWindow]::IsWindowEnabled($existing.MainWindowHandle)) {
        Write-Host 'Cierra primero Field y el editor de atributos con Aceptar o Cancelar, segun corresponda.'
        Write-Host 'Despues vuelve a ejecutar Abrir-ejemplo.cmd para mostrar Localizar casillas.'
        if($InspectOnly){Write-Output 'EXISTING_DEMO_MODAL';return}
        exit 1
    }
    if(-not $InspectOnly -and [CasillasActivateWindow]::ShowPanel($existing.Id,$existing.MainWindowHandle)) {
        Write-Output 'Panel Localizar casillas restaurado junto al plano; se ha solicitado la marca roja.'
        return
    }
    $target = $null
    foreach ($candidate in [CasillasActivateWindow]::Documents('Ejemplo-localizar-casillas.dwg')) {
        if ([long]$candidate.Application.HWND -eq $existing.MainWindowHandle.ToInt64()) {$target=$candidate;break}
    }
    if(-not $target){
        if($InspectOnly){Write-Output 'EXISTING_DEMO_NOT_REGISTERED';return}
        [void][CasillasActivateWindow]::SetForegroundWindow($existing.MainWindowHandle)
        Write-Host 'En la linea de comandos de este plano escribe CASILLASDEMO y pulsa Enter.'
        Write-Host 'El panel se llama Localizar casillas; selecciona una fila y pulsa Ubicar.'
        for($attempt=0;$attempt -lt 100;$attempt++) {
            if([CasillasActivateWindow]::ShowPanel($existing.Id,$existing.MainWindowHandle)){return}
            Start-Sleep -Milliseconds 500
        }
        exit 1
    }
    if($InspectOnly){Write-Output ('EXISTING_DEMO_READY: '+$target.FullName);return}
    if($target.GetVariable('CMDACTIVE') -ne 0){throw 'Termina el comando activo y vuelve a abrir el ejemplo.'}
    [void][CasillasActivateWindow]::ShowWindow($existing.MainWindowHandle,9)
    [void][CasillasActivateWindow]::SetForegroundWindow($existing.MainWindowHandle)
    $target.Activate()
    $load='(progn (setq casillasPreviousTrust (getvar "TRUSTEDPATHS")) (setvar "TRUSTEDPATHS" (strcat casillasPreviousTrust ";'+$folder+'")) (command "_.NETLOAD" "'+$dll+'") (setvar "TRUSTEDPATHS" casillasPreviousTrust) (setq casillasPreviousTrust nil) (princ))'
    $target.SendCommand($load+[Environment]::NewLine+'CASILLASDEMO'+[Environment]::NewLine)
    for($attempt=0;$attempt -lt 30;$attempt++) {
        if([CasillasActivateWindow]::ShowPanel($existing.Id,$existing.MainWindowHandle)){break}
        Start-Sleep -Milliseconds 200
    }
    Write-Output 'Panel Localizar casillas abierto en la pantalla del ejemplo. Selecciona una fila para ver la marca roja.'
    return
}
if($InspectOnly){Write-Output 'NO_EXISTING_DEMO';return}
$script = @'
(setq casillasPreviousTrust (getvar "TRUSTEDPATHS"))
(setvar "TRUSTEDPATHS" (strcat casillasPreviousTrust ";__FOLDER__"))
(command "_.NETLOAD" "__DLL__")
(setvar "TRUSTEDPATHS" casillasPreviousTrust)
(setq casillasPreviousTrust nil)
__TEST__
CASILLASDEMO
'@
$test = if ($ValidateScreen) { 'CASILLASPROBARMARCA' } else { '' }
$script = $script.Replace('__FOLDER__',$folder).Replace('__DLL__',$dll).Replace('__TEST__',$test)
$scriptPath = Join-Path $demoRoot 'abrir-ejemplo.scr'
[IO.File]::WriteAllText($scriptPath, $script + [Environment]::NewLine, [Text.Encoding]::ASCII)
# Opening this example is deliberately interactive: it is the user-facing demonstration.
Start-Process -FilePath $acad -ArgumentList @('"'+$drawing+'"','/product','C3D','/language','en-US','/b','"'+$scriptPath+'"') | Out-Null
Write-Output 'Se esta abriendo Civil 3D 2024 con la copia de prueba y el panel CASILLAS.'
for($attempt=0;$attempt -lt 100;$attempt++) {
    $started=Get-Process acad -ErrorAction SilentlyContinue | Where-Object {$_.MainWindowTitle -like '*Ejemplo-localizar-casillas.dwg*'} | Select-Object -First 1
    if($started -and [CasillasActivateWindow]::ShowPanel($started.Id,$started.MainWindowHandle)){break}
    Start-Sleep -Milliseconds 500
}
