param([switch]$Validate,[ValidateSet('Ejemplo-Field-v2.dwg','Ejemplo-Field-v3.dwg')][string]$DrawingName='Ejemplo-Field-v3.dwg')
$ErrorActionPreference='Stop'
$root=$PSScriptRoot
$drawing=Join-Path $root $DrawingName
$titlePattern='*'+[IO.Path]::GetFileNameWithoutExtension($DrawingName)+'*'
$plugin=Join-Path $root 'LocalizarCasillas.Field.2024.dll'
$automaticPlugin=Join-Path $env:APPDATA 'Autodesk/ApplicationPlugins/CasillasFieldDemo.bundle/Contents/LocalizarCasillas.Field.2024.dll'
if(Test-Path -LiteralPath $automaticPlugin){$plugin=$automaticPlugin}
$acad=Join-Path $env:ProgramFiles 'Autodesk\AutoCAD 2024\acad.exe'
foreach($file in @($drawing,$plugin,$acad)){if(-not(Test-Path -LiteralPath $file)){throw "No se encontro: $file"}}
Add-Type @'
using System;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
public static class FieldDemoWindow {
 [StructLayout(LayoutKind.Sequential)] public struct Rect {public int Left,Top,Right,Bottom;}
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
 [DllImport("user32.dll")] public static extern bool IsWindowEnabled(IntPtr h);
 [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr h,out Rect rect);
 [DllImport("user32.dll",EntryPoint="SendMessageW")] static extern IntPtr SendNumber(IntPtr h,uint message,IntPtr w,IntPtr l);
 [DllImport("user32.dll")] static extern int GetMenuItemCount(IntPtr menu);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern int GetMenuString(IntPtr menu,uint index,StringBuilder text,int count,uint flags);
 [DllImport("ole32.dll")] static extern int GetRunningObjectTable(int reserved,out IRunningObjectTable table);
 [DllImport("ole32.dll")] static extern int CreateBindCtx(int reserved,out IBindCtx context);
 public static object[] Documents(string path) {
  IRunningObjectTable table;Marshal.ThrowExceptionForHR(GetRunningObjectTable(0,out table));
  IEnumMoniker entries;table.EnumRunning(out entries);IBindCtx ctx;Marshal.ThrowExceptionForHR(CreateBindCtx(0,out ctx));
  var item=new IMoniker[1];var found=new List<object>();
  try {while(entries.Next(1,item,IntPtr.Zero)==0) {
   try {string name;item[0].GetDisplayName(ctx,null,out name);
    if(String.Equals(System.IO.Path.GetFileName(name),System.IO.Path.GetFileName(path),StringComparison.OrdinalIgnoreCase)){object doc;table.GetObject(item[0],out doc);found.Add(doc);}
   } catch(COMException){} finally {Marshal.ReleaseComObject(item[0]);}
  }} finally {Marshal.ReleaseComObject(entries);Marshal.ReleaseComObject(ctx);Marshal.ReleaseComObject(table);}
  return found.ToArray();
 }
 public static string Title(IntPtr h){var s=new StringBuilder(512);GetWindowText(h,s,512);return s.ToString();}
 public static string Class(IntPtr h){var s=new StringBuilder(256);GetClassName(h,s,256);return s.ToString();}
 public static IntPtr[] Windows(int process){var list=new List<IntPtr>();EnumWindows((h,p)=>{uint id;GetWindowThreadProcessId(h,out id);if(id==process&&IsWindowVisible(h))list.Add(h);return true;},IntPtr.Zero);return list.ToArray();}
 public static IntPtr Controller(int process){IntPtr result=IntPtr.Zero;EnumWindows((h,p)=>{uint id;GetWindowThreadProcessId(h,out id);if(id==process&&Title(h)=="CasillasFieldDemo.Controller.0.3.1"){result=h;return false;}return true;},IntPtr.Zero);return result;}
 public static bool OpenContext(IntPtr editor,int offset){IntPtr value=IntPtr.Zero;EnumChildWindows(editor,(h,p)=>{if(GetDlgCtrlID(h)==1029){value=h;return false;}return true;},IntPtr.Zero);if(value==IntPtr.Zero)return false;if(offset<0)return PostMessage(value,0x7B,value,(IntPtr)(-1));Rect rect;GetWindowRect(value,out rect);var client=(IntPtr)(offset+(8<<16));SendNumber(value,0x201,(IntPtr)1,client);SendNumber(value,0x202,IntPtr.Zero,client);var screen=(IntPtr)(((rect.Left+offset)&0xFFFF)|((rect.Top+8)<<16));return PostMessage(value,0x7B,value,screen);}
 public static bool HasEditField(IntPtr window){var menu=SendNumber(window,0x1E1,IntPtr.Zero,IntPtr.Zero);for(uint i=0;i<GetMenuItemCount(menu);i++){var text=new StringBuilder(256);GetMenuString(menu,i,text,256,0x400);if(text.ToString().Replace("&","").StartsWith("Edit Field"))return true;}return false;}
}
'@
$existing=Get-Process acad -ErrorAction SilentlyContinue | Where-Object {
 @([FieldDemoWindow]::Windows($_.Id) | Where-Object {[FieldDemoWindow]::Title($_) -like $titlePattern}).Count -gt 0
} | Select-Object -First 1
$folder=([IO.Path]::GetDirectoryName($plugin)).Replace('\','/');$dll=$plugin.Replace('\','/')
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
$scriptPath=Join-Path $root 'abrir-field.scr'
if($existing) {
 $windows=[FieldDemoWindow]::Windows($existing.Id)
 $main=$windows | Where-Object {[FieldDemoWindow]::Title($_) -like $titlePattern} | Select-Object -First 1
 $field=$windows | Where-Object {[FieldDemoWindow]::Title($_) -eq 'Field'} | Select-Object -First 1
 $guide=$windows | Where-Object {[FieldDemoWindow]::Title($_) -like 'Field*celdas y*'} | Select-Object -First 1
 if($field -and $guide){[void][FieldDemoWindow]::SetForegroundWindow($field);Write-Output 'La ayuda ya esta abierta junto al Field del ejemplo.';return}
 if(-not [FieldDemoWindow]::IsWindowEnabled($main)){throw 'Cierra primero Field y el editor de atributos con Aceptar o Cancelar; despues vuelve a ejecutar Abrir-ejemplo.cmd.'}
 $controller=[FieldDemoWindow]::Controller($existing.Id)
 if($controller -ne [IntPtr]::Zero) {
  [void][FieldDemoWindow]::SetForegroundWindow($main)
  if(-not [FieldDemoWindow]::PostMessage($controller,0x8049,[IntPtr]::Zero,[IntPtr]::Zero)){throw 'No se pudo solicitar el Field del ejemplo.'}
 } else {
 $target=$null
 foreach($candidate in [FieldDemoWindow]::Documents($drawing)) {
  if([long]$candidate.Application.HWND -eq $main.ToInt64() -and [string]::Equals([string]$candidate.FullName,$drawing,[StringComparison]::OrdinalIgnoreCase)){$target=$candidate;break}
 }
 if(-not $target){throw 'No se pudo conectar con la copia abierta. Cierra solo el ejemplo y vuelve a ejecutar Abrir-ejemplo.cmd.'}
 if($target.GetVariable('CMDACTIVE') -ne 0){throw 'Termina el comando activo del ejemplo y vuelve a ejecutar Abrir-ejemplo.cmd.'}
 [void][FieldDemoWindow]::SetForegroundWindow($main)
 $target.Activate()
 # One expression restores TRUSTEDPATHS before starting the interactive command.
 $load='(progn (setq fieldPreviousTrust (getvar "TRUSTEDPATHS")) (setvar "TRUSTEDPATHS" (strcat fieldPreviousTrust ";'+$folder+'")) (command "_.NETLOAD" "'+$dll+'") (setvar "TRUSTEDPATHS" fieldPreviousTrust) (setq fieldPreviousTrust nil) (princ))'
 $target.SendCommand($load+[Environment]::NewLine+'CASILLASFIELDDEMO'+[Environment]::NewLine)
 }
} else {
 [IO.File]::WriteAllText($scriptPath,$script+[Environment]::NewLine,[Text.Encoding]::ASCII)
 Start-Process -FilePath $acad -ArgumentList @('"'+$drawing+'"','/product','C3D','/language','en-US','/b','"'+$scriptPath+'"') | Out-Null
}
Write-Output 'Abriendo Field de R3_CH y la ayuda visual. Las selecciones se confirman solamente con los botones nativos.'
$contextRequested=$false
$contextAttempts=0
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
   if($editor){$contextRequested=[FieldDemoWindow]::OpenContext($editor,(@(-1,5,4,6)[$contextAttempts%4]));$contextAttempts++}
  } else {
   $menu=$windows | Where-Object {[FieldDemoWindow]::Class($_) -eq '#32768'} | Select-Object -First 1
   if($menu){
    if([FieldDemoWindow]::HasEditField($menu)){[void][FieldDemoWindow]::PostMessage($menu,0x102,[IntPtr]101,[IntPtr]::Zero)}
    else{[void][FieldDemoWindow]::PostMessage($menu,0x100,[IntPtr]27,[IntPtr]::Zero);$contextRequested=$false}
   }
  }
 }
 Start-Sleep -Milliseconds 500
}
Write-Output 'Si aparece el editor: clic derecho sobre Value > Edit Field. La ayuda se muestra automaticamente.'
