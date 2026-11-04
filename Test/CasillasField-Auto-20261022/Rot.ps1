Add-Type @"
using System;using System.Collections.Generic;using System.Runtime.InteropServices;using System.Runtime.InteropServices.ComTypes;
public static class RotNames {
[DllImport("ole32.dll")]static extern int GetRunningObjectTable(int r,out IRunningObjectTable t);
[DllImport("ole32.dll")]static extern int CreateBindCtx(int r,out IBindCtx c);
public static string[] Names(){IRunningObjectTable t;GetRunningObjectTable(0,out t);IEnumMoniker e;t.EnumRunning(out e);IBindCtx c;CreateBindCtx(0,out c);var a=new IMoniker[1];var n=new List<string>();try{while(e.Next(1,a,IntPtr.Zero)==0){try{string s;a[0].GetDisplayName(c,null,out s);if(s.IndexOf("dwg",StringComparison.OrdinalIgnoreCase)>=0||s.IndexOf("AutoCAD",StringComparison.OrdinalIgnoreCase)>=0)n.Add(s);}finally{Marshal.ReleaseComObject(a[0]);}}}finally{Marshal.ReleaseComObject(e);Marshal.ReleaseComObject(c);Marshal.ReleaseComObject(t);}return n.ToArray();}
}
"@
[RotNames]::Names()