param([Parameter(Mandatory=$true)][string]$OutputDirectory,[switch]$OpenContext,[switch]$OpenField,[string]$Property,[switch]$CancelField,[switch]$CancelEditor,[switch]$CloseTest,[switch]$DiscardTest,[switch]$SaveTest,[ValidateSet('Ejemplo-Field-v2.dwg','Ejemplo-Field-v3.dwg','Casillas-piloto.dwg')][string]$DrawingName='Ejemplo-Field-v2.dwg',[string]$MapFilter,[int]$MapRow=-1,[switch]$EditCell,[string]$CellValue,[switch]$SaveCell,[switch]$CancelCell,[string]$UsageTag,[switch]$ExpandPreview,[ValidateSet('UsagePreview','TargetPreview','MapPreview')][string]$Preview='UsagePreview',[switch]$ClosePreview)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type @'
using System;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;
public static class FieldLive {
 public delegate bool EnumProc(IntPtr window,IntPtr value);
 [StructLayout(LayoutKind.Sequential)] public struct Rect {public int Left,Top,Right,Bottom;}
 [DllImport("user32.dll")] static extern bool EnumWindows(EnumProc proc,IntPtr value);
 [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr window,out uint process);
 [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr window);
 [DllImport("user32.dll")] static extern bool EnumChildWindows(IntPtr owner,EnumProc proc,IntPtr value);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern int GetWindowText(IntPtr window,StringBuilder text,int size);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern int GetClassName(IntPtr window,StringBuilder text,int size);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)] public static extern IntPtr SendMessage(IntPtr window,uint message,IntPtr w,StringBuilder text);
 [DllImport("user32.dll",CharSet=CharSet.Unicode,EntryPoint="SendMessageW")] public static extern IntPtr SendNumber(IntPtr window,uint message,IntPtr w,IntPtr l);
 [DllImport("user32.dll",CharSet=CharSet.Unicode,EntryPoint="SendMessageW")] public static extern IntPtr SendText(IntPtr window,uint message,IntPtr w,string l);
 [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr window,uint message,IntPtr w,IntPtr l);
 [DllImport("user32.dll")] public static extern int GetDlgCtrlID(IntPtr window);
 [DllImport("user32.dll")] public static extern IntPtr GetParent(IntPtr window);
 [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr window,out Rect rect);
 [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr window,IntPtr dc,uint flags);
 [DllImport("user32.dll")] public static extern int GetMenuItemCount(IntPtr menu);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)] public static extern int GetMenuString(IntPtr menu,uint index,StringBuilder text,int count,uint flags);
 [DllImport("user32.dll")] public static extern uint GetMenuItemID(IntPtr menu,int position);
 public static string Title(IntPtr window){var text=new StringBuilder(1024);GetWindowText(window,text,text.Capacity);return text.ToString();}
 public static string Class(IntPtr window){var text=new StringBuilder(256);GetClassName(window,text,text.Capacity);return text.ToString();}
 public static string Text(IntPtr window){var text=new StringBuilder(32768);SendMessage(window,0xD,(IntPtr)text.Capacity,text);return text.ToString();}
 public static IntPtr[] Windows(int process){var result=new List<IntPtr>();EnumWindows((h,p)=>{uint id;GetWindowThreadProcessId(h,out id);if(id==process&&IsWindowVisible(h))result.Add(h);return true;},IntPtr.Zero);return result.ToArray();}
 public static IntPtr[] Children(IntPtr parent){var result=new List<IntPtr>();EnumChildWindows(parent,(h,p)=>{result.Add(h);return true;},IntPtr.Zero);return result.ToArray();}
}
'@
$titlePattern='*'+[IO.Path]::GetFileNameWithoutExtension($DrawingName)+'*'
$process=Get-Process acad | Where-Object {
 @([FieldLive]::Windows($_.Id) | Where-Object {[FieldLive]::Title($_) -like $titlePattern}).Count -gt 0
} | Select-Object -First 1
if(-not $process){throw 'No se encontro la copia V2. No se actua en los otros planos.'}
$windows=[FieldLive]::Windows($process.Id)
$editor=$windows | Where-Object {[FieldLive]::Title($_) -eq 'Enhanced Attribute Editor'} | Select-Object -First 1
$field=$windows | Where-Object {[FieldLive]::Title($_) -eq 'Field'} | Select-Object -First 1
$main=$windows | Where-Object {[FieldLive]::Title($_) -like $titlePattern} | Select-Object -First 1
foreach($handle in $windows) {
 $title=[FieldLive]::Title($handle);$class=[FieldLive]::Class($handle)
 if($title -notmatch 'Field|celdas|Editar casilla|Vista ampliada|Enhanced Attribute|Ejemplo-Field' -and $class -ne '#32768'){continue}
 Write-Output ("WINDOW $handle [$class] $title")
 $kind=if($title -eq 'Field'){'field'}elseif($title -like '*celdas*'){'guia'}elseif($title -like 'Editar casilla*'){'valor'}elseif($title -like 'Vista ampliada*'){'ampliada'}elseif($title -eq 'Enhanced Attribute Editor'){'editor'}elseif($class -eq '#32768'){'menu'}else{'plano'}
 $rect=New-Object FieldLive+Rect;[void][FieldLive]::GetWindowRect($handle,[ref]$rect)
 if($rect.Right-$rect.Left -gt 0 -and $rect.Bottom-$rect.Top -gt 0) {
   $bitmap=New-Object Drawing.Bitmap ($rect.Right-$rect.Left),($rect.Bottom-$rect.Top)
   $graphics=[Drawing.Graphics]::FromImage($bitmap);$dc=$graphics.GetHdc()
   try{[void][FieldLive]::PrintWindow($handle,$dc,2)}finally{$graphics.ReleaseHdc($dc)}
   $bitmap.Save((Join-Path $OutputDirectory ($kind+'.png')));$graphics.Dispose();$bitmap.Dispose()
 }
 if($kind -notin @('plano','menu')) {
   $lines=foreach($child in [FieldLive]::Children($handle)) {"$child | $([FieldLive]::GetDlgCtrlID($child)) | $([FieldLive]::Class($child)) | $([FieldLive]::Text($child))"}
   [IO.File]::WriteAllLines((Join-Path $OutputDirectory ($kind+'-controles.txt')),[string[]]$lines)
 }
 if($class -eq '#32768') {
   $menu=[FieldLive]::SendNumber($handle,0x1E1,[IntPtr]::Zero,[IntPtr]::Zero)
   for($index=0;$index -lt [FieldLive]::GetMenuItemCount($menu);$index++) {
     $text=New-Object Text.StringBuilder 512;[void][FieldLive]::GetMenuString($menu,$index,$text,512,0x400)
     $id=[FieldLive]::GetMenuItemID($menu,$index);Write-Output "MENU_ITEM: $id $text"
     if($OpenField -and $text.ToString() -match 'Edit Field') {
       [void][FieldLive]::PostMessage($handle,0x102,[IntPtr]101,[IntPtr]::Zero)
     }
   }
 }
}
if($OpenContext -and $editor) {
 $edit=[FieldLive]::Children($editor) | Where-Object {[FieldLive]::GetDlgCtrlID($_) -eq 1029} | Select-Object -First 1
 if(-not $edit){throw 'No se encontro Value en el editor.'}
 [void][FieldLive]::PostMessage($edit,0x7B,$edit,[IntPtr](-1));Write-Output 'CONTEXT_REQUESTED'
}
if($Property -and $field) {
 foreach($child in [FieldLive]::Children($field)) {
   if([FieldLive]::Class($child) -notmatch 'ListBox'){continue}
   $count=[FieldLive]::SendNumber($child,0x18B,[IntPtr]::Zero,[IntPtr]::Zero).ToInt32()
   for($index=0;$index -lt $count;$index++) {
     $text=New-Object Text.StringBuilder 4096;[void][FieldLive]::SendMessage($child,0x189,[IntPtr]$index,$text)
     if($text.ToString() -eq $Property) {
       [void][FieldLive]::SendNumber($child,0x186,[IntPtr]$index,[IntPtr]::Zero)
       $id=[FieldLive]::GetDlgCtrlID($child);$parent=[FieldLive]::GetParent($child)
       [void][FieldLive]::PostMessage($parent,0x111,[IntPtr](0x10000+$id),$child)
       Write-Output "PROPERTY_SELECTED $Property";break
     }
   }
 }
}
if($CancelField -and $field) {
 $button=[FieldLive]::Children($field) | Where-Object {[FieldLive]::GetDlgCtrlID($_) -eq 2} | Select-Object -First 1
 [void][FieldLive]::PostMessage($button,0xF5,[IntPtr]::Zero,[IntPtr]::Zero)
 Write-Output 'FIELD_CANCELLED'
}
if($CancelEditor -and $editor -and -not $field) {
 $button=[FieldLive]::Children($editor) | Where-Object {[FieldLive]::GetDlgCtrlID($_) -eq 2} | Select-Object -First 1
 [void][FieldLive]::PostMessage($button,0xF5,[IntPtr]::Zero,[IntPtr]::Zero)
 Write-Output 'EDITOR_CANCELLED'
}
if($CloseTest -and -not $field -and -not $editor) {
 [void][FieldLive]::PostMessage($main,0x10,[IntPtr]::Zero,[IntPtr]::Zero);Write-Output 'TEST_CLOSE_REQUESTED'
}
if($DiscardTest) {
 foreach($window in $windows) {
  if([FieldLive]::Title($window) -notmatch 'AutoCAD|Civil 3D'){continue}
  $children=[FieldLive]::Children($window)
  $button=$children | Where-Object {[FieldLive]::GetDlgCtrlID($_) -eq 7 -and [FieldLive]::Text($_) -match 'No'} | Select-Object -First 1
  if($button){[void][FieldLive]::PostMessage($button,0xF5,[IntPtr]::Zero,[IntPtr]::Zero);Write-Output 'TEST_CHANGES_DISCARDED'}
 }
}
if($SaveTest) {
 foreach($window in $windows) {
  if([FieldLive]::Title($window) -notmatch 'AutoCAD|Civil 3D'){continue}
  $children=[FieldLive]::Children($window)
  $button=$children | Where-Object {[FieldLive]::GetDlgCtrlID($_) -eq 6 -and [FieldLive]::Text($_) -match 'Yes|S[ií]'} | Select-Object -First 1
  if($button){[void][FieldLive]::PostMessage($button,0xF5,[IntPtr]::Zero,[IntPtr]::Zero);Write-Output 'TEST_SAVE_ACCEPTED'}
 }
}
if($MapFilter) {
 $guide=$windows | Where-Object {[FieldLive]::Title($_) -like '*celdas y*'} | Select-Object -First 1
 foreach($child in [FieldLive]::Children($guide)) {
  if([FieldLive]::Class($child) -notmatch 'COMBOBOX'){continue}
  $count=[FieldLive]::SendNumber($child,0x146,[IntPtr]::Zero,[IntPtr]::Zero).ToInt32()
  for($i=0;$i -lt $count;$i++) {
   $text=New-Object Text.StringBuilder 4096;[void][FieldLive]::SendMessage($child,0x148,[IntPtr]$i,$text)
   if($text.ToString() -eq $MapFilter) {
    [void][FieldLive]::SendNumber($child,0x14E,[IntPtr]$i,[IntPtr]::Zero)
    [void][FieldLive]::PostMessage([FieldLive]::GetParent($child),0x111,[IntPtr](0x10000 -bor ([FieldLive]::GetDlgCtrlID($child) -band 0xFFFF)),$child)
    Write-Output ('MAP_FILTER_SELECTED '+$MapFilter)
   }
  }
 }
}
if($MapRow -ge 0) {
 $guide=$windows | Where-Object {[FieldLive]::Title($_) -like '*celdas y*'} | Select-Object -First 1
 $grid=[FieldLive]::Children($guide) | Where-Object {[FieldLive]::Class($_) -match 'WindowsForms10.Window' -and [FieldLive]::Text($_) -eq 'CellMapGrid'} | Select-Object -First 1
 if(-not $grid){throw 'No se encontro la tabla del mapa.'}
 $position=[IntPtr](20+((35+$MapRow*22) -shl 16))
 [void][FieldLive]::PostMessage($grid,0x201,[IntPtr]1,$position)
 [void][FieldLive]::PostMessage($grid,0x202,[IntPtr]::Zero,$position)
 Write-Output ('MAP_ROW_CLICKED '+$MapRow)
}
if($EditCell) {
 $guide=$windows | Where-Object {[FieldLive]::Title($_) -like '*celdas y*'} | Select-Object -First 1
 $button=[FieldLive]::Children($guide) | Where-Object {[FieldLive]::Text($_) -eq 'Editar casilla...'} | Select-Object -First 1
 if(-not $button){throw 'No se encontro Editar casilla.'}
 [void][FieldLive]::PostMessage($button,0xF5,[IntPtr]::Zero,[IntPtr]::Zero)
 Write-Output 'CELL_EDITOR_REQUESTED'
}
$valueEditor=$windows | Where-Object {[FieldLive]::Title($_) -like 'Editar casilla*'} | Select-Object -First 1
if($valueEditor) {
 $children=[FieldLive]::Children($valueEditor)
 if($PSBoundParameters.ContainsKey('CellValue')) {
  $input=$children | Where-Object {[FieldLive]::Class($_) -match 'EDIT'} | Select-Object -First 1
  [void][FieldLive]::SendText($input,0xC,[IntPtr]::Zero,$CellValue)
  [void][FieldLive]::PostMessage([FieldLive]::GetParent($input),0x111,[IntPtr](0x3000000 -bor ([FieldLive]::GetDlgCtrlID($input) -band 0xFFFF)),$input)
  Write-Output 'CELL_VALUE_ENTERED'
 }
 if($SaveCell -or $CancelCell) {
  $caption=if($CancelCell){'^Cancelar$'}else{'^Guardar'}
  $button=$children | Where-Object {[FieldLive]::Text($_) -match $caption} | Select-Object -First 1
  [void][FieldLive]::PostMessage($button,0xF5,[IntPtr]::Zero,[IntPtr]::Zero)
  Write-Output 'CELL_EDITOR_FINISHED'
 }
}
if($UsageTag) {
 $guide=$windows | Where-Object {[FieldLive]::Title($_) -like '*celdas y*'} | Select-Object -First 1
 $found=$false
 foreach($child in [FieldLive]::Children($guide)) {
  if([FieldLive]::Class($child) -notmatch 'COMBOBOX'){continue}
  $count=[FieldLive]::SendNumber($child,0x146,[IntPtr]::Zero,[IntPtr]::Zero).ToInt32()
  for($i=0;$i -lt $count;$i++) {
   $text=New-Object Text.StringBuilder 4096;[void][FieldLive]::SendMessage($child,0x148,[IntPtr]$i,$text)
   if($text.ToString().Contains($UsageTag+' [')) {
    [void][FieldLive]::SendNumber($child,0x14E,[IntPtr]$i,[IntPtr]::Zero)
    [void][FieldLive]::PostMessage([FieldLive]::GetParent($child),0x111,[IntPtr](0x10000 -bor ([FieldLive]::GetDlgCtrlID($child) -band 0xFFFF)),$child)
    Write-Output ('USAGE_SELECTED '+$UsageTag);$found=$true;break
   }
  }
 }
 if(-not $found){throw 'La coincidencia solicitada no aparece en el selector.'}
}
if($ExpandPreview) {
 $guide=$windows | Where-Object {[FieldLive]::Title($_) -like '*celdas y*'} | Select-Object -First 1
 $previewWindow=[FieldLive]::Children($guide) | Where-Object {[FieldLive]::Text($_) -eq $Preview} | Select-Object -First 1
 if(-not $previewWindow){throw 'No se encontro la vista previa solicitada.'}
 $button=[FieldLive]::Children($previewWindow) | Where-Object {[FieldLive]::Text($_) -eq 'Ampliar'} | Select-Object -First 1
 [void][FieldLive]::PostMessage($button,0xF5,[IntPtr]::Zero,[IntPtr]::Zero);Write-Output ('PREVIEW_EXPANDED '+$Preview)
}
if($ClosePreview) {
 $large=$windows | Where-Object {[FieldLive]::Title($_) -like 'Vista ampliada*'} | Select-Object -First 1
 if($large){[void][FieldLive]::PostMessage($large,0x10,[IntPtr]::Zero,[IntPtr]::Zero);Write-Output 'PREVIEW_CLOSED'}
}
