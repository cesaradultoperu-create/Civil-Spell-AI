param([int]$ProcessId)
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
 public static IntPtr[] Windows(int process){var result=new List<IntPtr>();EnumWindows((h,p)=>{uint id;GetWindowThreadProcessId(h,out id);if(id==process)result.Add(h);return true;},IntPtr.Zero);return result.ToArray();}
 public static IntPtr[] Children(IntPtr parent){var result=new List<IntPtr>();EnumChildWindows(parent,(h,p)=>{result.Add(h);return true;},IntPtr.Zero);return result.ToArray();}
}
'@
foreach($h in [FieldLive]::Windows($ProcessId)){Write-Output ("WINDOW $h "+[FieldLive]::Class($h)+" "+[FieldLive]::Title($h)); if([FieldLive]::Class($h) -eq "#32770"){foreach($c in [FieldLive]::Children($h)){Write-Output ("  "+[FieldLive]::GetDlgCtrlID($c)+" "+[FieldLive]::Class($c)+" "+[FieldLive]::Text($c))}}}