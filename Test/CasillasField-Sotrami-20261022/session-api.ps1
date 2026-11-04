Add-Type @'
using System;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
public static class FieldDemoWindow {
 [DllImport("ole32.dll", CharSet=CharSet.Unicode)] static extern int CLSIDFromProgID(string progid,out Guid clsid);
 [DllImport("oleaut32.dll",PreserveSig=false)] static extern void GetActiveObject(ref Guid clsid,IntPtr reserved,[MarshalAs(UnmanagedType.IUnknown)] out object application);
 public static object Active(){Guid id;Marshal.ThrowExceptionForHR(CLSIDFromProgID("AutoCAD.Application.24.3",out id));object app;GetActiveObject(ref id,IntPtr.Zero,out app);return app;}
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
