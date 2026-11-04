using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace CasillasField
{
    public sealed class DialogState
    {
        public IntPtr Editor,Field;
        public string Tag="",Block="",Code="",SelectedValue="";
    }
    public static class NativeDialogs
    {
        public delegate bool EnumProc(IntPtr window,IntPtr state);
        [StructLayout(LayoutKind.Sequential)] public struct Rect {public int Left,Top,Right,Bottom;}
        [DllImport("user32.dll")] static extern bool EnumWindows(EnumProc proc,IntPtr state);
        [DllImport("user32.dll")] public static extern bool EnumChildWindows(IntPtr owner,EnumProc proc,IntPtr state);
        [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr handle,out uint process);
        [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr handle);
        [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern int GetWindowText(IntPtr window,StringBuilder text,int size);
        [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern int GetClassName(IntPtr window,StringBuilder text,int size);
        [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern IntPtr SendMessage(IntPtr window,uint msg,IntPtr value,StringBuilder text);
        [DllImport("user32.dll")] public static extern int GetDlgCtrlID(IntPtr window);
        [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr window,out Rect rect);
        [DllImport("user32.dll",EntryPoint="SetWindowLongPtrW")] public static extern IntPtr SetOwner(IntPtr window,int index,IntPtr owner);
        [DllImport("user32.dll")] public static extern bool InvalidateRect(IntPtr window,IntPtr rect,bool erase);
        [DllImport("user32.dll")] public static extern bool UpdateWindow(IntPtr window);
        [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr window,IntPtr after,int x,int y,int cx,int cy,uint flags);
        [DllImport("user32.dll")] static extern IntPtr GetParent(IntPtr window);
        [DllImport("user32.dll")] static extern bool PostMessage(IntPtr window,uint message,IntPtr w,IntPtr l);
        public static void RefreshProperty(IntPtr field)
        {
            if(field==IntPtr.Zero)return;
            foreach(var child in Children(field))
                if(IsWindowVisible(child) && Class(child)=="ListBox" && GetDlgCtrlID(child)==1015)
                    PostMessage(GetParent(child),0x111,(IntPtr)(0x10000|1015),child);
        }
        public static string Text(IntPtr handle)
        {
            var buffer=new StringBuilder(32768);SendMessage(handle,0xD,(IntPtr)buffer.Capacity,buffer);return buffer.ToString();
        }
        public static string Class(IntPtr handle){var text=new StringBuilder(256);GetClassName(handle,text,text.Capacity);return text.ToString();}
        public static List<IntPtr> Children(IntPtr parent)
        {
            var result=new List<IntPtr>();EnumChildWindows(parent,(h,p)=>{result.Add(h);return true;},IntPtr.Zero);return result;
        }
        public static DialogState Read()
        {
            int process=Process.GetCurrentProcess().Id;var result=new DialogState();
            EnumWindows((h,p)=>{
                uint id;GetWindowThreadProcessId(h,out id);if(id!=process || !IsWindowVisible(h))return true;
                var title=new StringBuilder(512);GetWindowText(h,title,title.Capacity);
                if(title.ToString()=="Enhanced Attribute Editor")result.Editor=h;
                if(title.ToString()=="Field")result.Field=h;
                return true;
            },IntPtr.Zero);
            if(result.Editor!=IntPtr.Zero)
                foreach(var child in Children(result.Editor))
                {
                    if(Class(child)=="Static" && GetDlgCtrlID(child)==1026)result.Tag=Text(child).Trim();
                    if(Class(child)=="Static" && GetDlgCtrlID(child)==1025)result.Block=Text(child).Trim();
                }
            if(result.Field!=IntPtr.Zero)
            {
                Rect field;GetWindowRect(result.Field,out field);int valueTop=Int32.MaxValue;
                foreach(var child in Children(result.Field))
                {
                    if(!IsWindowVisible(child))continue;
                    string cls=Class(child);
                    if(!cls.Contains("Edit") && !cls.Contains("EDIT"))continue;
                    string text=Text(child);
                    if(text.Contains("\\AcSm") && text.Length>result.Code.Length)result.Code=text;
                    Rect rect;GetWindowRect(child,out rect);
                    if(rect.Left>field.Left+(field.Right-field.Left)/2 && rect.Top<valueTop && !text.Contains("%<"))
                    {result.SelectedValue=text;valueTop=rect.Top;}
                }
            }
            return result;
        }
    }
}
