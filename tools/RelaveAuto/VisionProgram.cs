using System;
using System.IO;
using System.Runtime.InteropServices;
namespace RelaveAuto {
 static class VisionProgram {
  [DllImport("kernel32",CharSet=CharSet.Unicode,SetLastError=true)] static extern IntPtr LoadLibrary(string path);
  static int Main(string[] args){
   try{
    if(args.Length!=1)throw new ArgumentException("Uso: RelaveVision.exe trabajo.json");
    string root=AppDomain.CurrentDomain.BaseDirectory;
    var native=Path.Combine(root,"runtimes","win-x64","native","OpenCvSharpExtern.dll");
    if(File.Exists(native)&&LoadLibrary(native)==IntPtr.Zero)throw new InvalidOperationException("No se pudo cargar OpenCV. Verifique Visual C++ x64.");
    var job=Json.Read<Job>(args[0]);var result=VisionEngine.Run(job);
    Json.Write(Path.Combine(job.OutputFolder,"resultado-vision.json"),result);
    Console.WriteLine("RELAVE_VISION_OK "+result.Photos.Count+" fotos; "+result.VisionSeconds.ToString("F2")+" s");return 0;
   }catch(Exception ex){Console.Error.WriteLine(ex.ToString());return 1;}
  }
 }
}
