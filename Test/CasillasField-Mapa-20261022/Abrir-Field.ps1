param([switch]$Validate,[ValidateSet('Ejemplo-Field-v2.dwg','Ejemplo-Field-v3.dwg')][string]$DrawingName='Ejemplo-Field-v3.dwg')
$ErrorActionPreference='Stop'
$root=$PSScriptRoot
$drawing=Join-Path $root $DrawingName
$titlePattern='*'+[IO.Path]::GetFileNameWithoutExtension($DrawingName)+'*'
$plugin=Join-Path $root 'LocalizarCasillas.Field.2024.dll'
$acad=Join-Path $env:ProgramFiles 'Autodesk\AutoCAD 2024\acad.exe'
foreach($file in @($drawing,$plugin,$acad)){if(-not(Test-Path -LiteralPath $file)){throw "No se encontro: $file"}}
Add-Type @'
using System;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;
public static class FieldDemoWindow {
 public delegate bool EnumProc(IntPtr h,IntPtr p);
 [DllImport("user32.dll")] static extern bool EnumWindows(EnumProc proc,IntPtr p);
 [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h,out uint id);
 [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr h);
 [DllImport("user32.dll")] static extern bool EnumChildWindows(IntPtr h,EnumProc proc,IntPtr p);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern int GetWindowText(IntPtr h,StringBuilder text,int size);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern int GetClassName(IntPtr h,StringBuilder text,int size);
 [DllImport("user32.dll")] static extern int GetDlgCtrlID(IntPtr h);
 [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr h,uint message,IntPtr w,IntPtr l);
 [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
 public static string Title(IntPtr h){var s=new StringBuilder(512);GetWindowText(h,s,512);return s.ToString();}
 public static string Class(IntPtr h){var s=new StringBuilder(256);GetClassName(h,s,256);return s.ToString();}
 public static IntPtr[] Windows(int process){var list=new List<IntPtr>();EnumWindows((h,p)=>{uint id;GetWindowThreadProcessId(h,out id);if(id==process&&IsWindowVisible(h))list.Add(h);return true;},IntPtr.Zero);return list.ToArray();}
 public static bool OpenContext(IntPtr editor){IntPtr value=IntPtr.Zero;EnumChildWindows(editor,(h,p)=>{if(GetDlgCtrlID(h)==1029){value=h;return false;}return true;},IntPtr.Zero);return value!=IntPtr.Zero&&PostMessage(value,0x7B,value,(IntPtr)(-1));}
}
'@
$existing=Get-Process acad -ErrorAction SilentlyContinue | Where-Object {
 @([FieldDemoWindow]::Windows($_.Id) | Where-Object {[FieldDemoWindow]::Title($_) -like $titlePattern}).Count -gt 0
} | Select-Object -First 1
if($existing) {
 $main=[FieldDemoWindow]::Windows($existing.Id) | Where-Object {[FieldDemoWindow]::Title($_) -like $titlePattern} | Select-Object -First 1
 [void][FieldDemoWindow]::SetForegroundWindow($main)
 Write-Output 'El ejemplo ya esta abierto. La ayuda aparece al entrar al Field de un atributo.'
 return
}
$folder=$root.Replace('\','/');$dll=$plugin.Replace('\','/')
$script=@'
(setq fieldPreviousTrust (getvar "TRUSTEDPATHS"))
(setvar "TRUSTEDPATHS" (strcat fieldPreviousTrust ";__FOLDER__"))
(command "_.NETLOAD" "__DLL__")
(setvar "TRUSTEDPATHS" fieldPreviousTrust)
(setq fieldPreviousTrust nil)
__TEST__
CASILLASFIELDDEMO
'@
$script=$script.Replace('__FOLDER__',$folder).Replace('__DLL__',$dll).Replace('__TEST__',$(if($Validate){'CASILLASFIELDPROBAR'}else{''}))
$scriptPath=Join-Path $root 'abrir-field.scr';[IO.File]::WriteAllText($scriptPath,$script+[Environment]::NewLine,[Text.Encoding]::ASCII)
Start-Process -FilePath $acad -ArgumentList @('"'+$drawing+'"','/product','C3D','/language','en-US','/b','"'+$scriptPath+'"') | Out-Null
Write-Output 'Abriendo Field de R3_CH y la ayuda visual. Las selecciones se confirman solamente con los botones nativos.'
$contextRequested=$false
for($attempt=0;$attempt -lt 120;$attempt++) {
 $process=Get-Process acad -ErrorAction SilentlyContinue | Where-Object {
  @([FieldDemoWindow]::Windows($_.Id) | Where-Object {[FieldDemoWindow]::Title($_) -like $titlePattern}).Count -gt 0
 } | Select-Object -First 1
 if($process) {
  $windows=[FieldDemoWindow]::Windows($process.Id)
  $field=$windows | Where-Object {[FieldDemoWindow]::Title($_) -eq 'Field'} | Select-Object -First 1
  if($field){[void][FieldDemoWindow]::SetForegroundWindow($field);return}
  if(-not $contextRequested) {
   $editor=$windows | Where-Object {[FieldDemoWindow]::Title($_) -eq 'Enhanced Attribute Editor'} | Select-Object -First 1
   if($editor){$contextRequested=[FieldDemoWindow]::OpenContext($editor)}
  } else {
   $menu=$windows | Where-Object {[FieldDemoWindow]::Class($_) -eq '#32768'} | Select-Object -First 1
   if($menu){[void][FieldDemoWindow]::PostMessage($menu,0x102,[IntPtr]101,[IntPtr]::Zero)}
  }
 }
 Start-Sleep -Milliseconds 500
}
Write-Output 'Si aparece el editor: clic derecho sobre Value > Edit Field. La ayuda se muestra automaticamente.'
