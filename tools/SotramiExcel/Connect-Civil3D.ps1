$ErrorActionPreference = 'Stop'
if (-not ('SotramiCadApi' -as [type])) {
    Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class SotramiCadApi {
    [DllImport("ole32.dll", CharSet=CharSet.Unicode)] static extern int CLSIDFromProgID(string progid, out Guid clsid);
    [DllImport("oleaut32.dll", PreserveSig=false)] static extern void GetActiveObject(ref Guid clsid, IntPtr reserved, [MarshalAs(UnmanagedType.IUnknown)] out object application);
    public static object Active() {
        Guid id; Marshal.ThrowExceptionForHR(CLSIDFromProgID("AutoCAD.Application.24.3", out id));
        object app; GetActiveObject(ref id, IntPtr.Zero, out app); return app;
    }
}
'@
}
$sotramiCad = [SotramiCadApi]::Active()
