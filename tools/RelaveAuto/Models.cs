using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
#if !NET8_0_OR_GREATER
using System.Web.Script.Serialization;
#endif

namespace RelaveAuto {
 public sealed class XY {
  public double X {get;set;} public double Y {get;set;}
  public XY(){} public XY(double x,double y){X=x;Y=y;}
 }
 public sealed class Calibration {
  public XY A {get;set;} public XY B {get;set;} public XY Top {get;set;}
  public int Width {get;set;} public int Height {get;set;}
  public string ReferenceHash {get;set;}
  public string ReferenceFile {get;set;}
 }
 public sealed class Frame {
  public string Label {get;set;} public double Number {get;set;}
  public double X {get;set;} public double Y {get;set;} public double Width {get;set;} public double Height {get;set;}
  public double MarkerX {get;set;}
 }
 public sealed class Job {
  public string ModelPath {get;set;} public string PhotoFolder {get;set;} public string OutputFolder {get;set;}
  public Calibration Calibration {get;set;}
  public string ReferenceFile {get;set;}
  public List<Frame> Frames {get;set;}=new List<Frame>();
 }
 public sealed class PhotoResult {
  public string File {get;set;} public Frame Frame {get;set;}
  public double Scale {get;set;}=1; public double Tx {get;set;} public double Ty {get;set;}
  public double RegistrationError {get;set;} public int Matches {get;set;}
  public bool RegistrationValid {get;set;}
  public int PixelWidth {get;set;} public int PixelHeight {get;set;}
  public double Toe {get;set;} public List<XY> Surface {get;set;}=new List<XY>();
  public int DetectedVertices {get;set;} public double SmoothingAreaChange {get;set;}
  public double GridOriginX {get;set;} public double GridAnchorX {get;set;}
  public double GridCenterError {get;set;} public bool AlignmentValid {get;set;}
  public double GridPitch {get;set;}
  public double GridMeasurementError {get;set;} public bool AlignmentManual {get;set;}
  public bool OriginManual {get;set;} public bool ReferenceAnchorValid {get;set;}
  public double DetectedToe {get;set;}
  public string Preview {get;set;} public bool NeedsReview {get;set;} public bool Approved {get;set;}
  public string Message {get;set;} public double Area {get;set;} public string Handle {get;set;}
  public double Seconds {get;set;}
 }
 public sealed class BatchResult {
  public List<PhotoResult> Photos {get;set;}=new List<PhotoResult>();
  public List<HeightSample> Heights {get;set;}=new List<HeightSample>();
  public List<string> Warnings {get;set;}=new List<string>();
  public string ReferenceFile {get;set;} public string DetectionMode {get;set;}
  public double VisionSeconds {get;set;} public double DrawingSeconds {get;set;} public string Version {get;set;}="0.3.5";
 }
 public sealed class HeightSample {
  public int Index {get;set;} public double Distance {get;set;} public double Height {get;set;}
  public double X {get;set;} public double BaseY {get;set;} public double TopY {get;set;}
  public bool AtTip {get;set;} public string Handle {get;set;}
  public double ProfileNumber {get;set;} public string ProfileFile {get;set;} public string ProfileHandle {get;set;}
 }
 public static class PhotoInputs {
  public static double Number(string file){
   string name=Path.GetFileNameWithoutExtension(file).Replace(',','.');
   if(int.TryParse(name,out int integer))return integer>=0?integer/100.0:-1;
   return double.TryParse(name,NumberStyles.Float,CultureInfo.InvariantCulture,out double value)&&value>=0&&!double.IsInfinity(value)?value:-1;
  }
  public static string[] Files(string folder){
   var files=Directory.GetFiles(folder,"*.png").Where(f=>Number(f)>=0).OrderBy(Number).ToArray();
   if(files.Length==0)throw new InvalidOperationException("La carpeta seleccionada no contiene fotos PNG con nombres numericos.");
   if(files.GroupBy(Number).Any(g=>g.Count()>1))throw new InvalidOperationException("Hay dos fotos con el mismo numero. Revise sus nombres.");
   return files;
  }
  public static string Reference(string folder,string selected=null){
   var files=Files(folder);
   if(!string.IsNullOrEmpty(selected)){
    var found=files.FirstOrDefault(f=>string.Equals(Path.GetFileName(f),selected,StringComparison.OrdinalIgnoreCase));
    if(found==null)throw new InvalidOperationException("No existe la foto de referencia "+selected+" en esta carpeta.");return found;
   }
   return files[0];
  }
 }
 internal static class ReviewActions {
  public static bool CanApprove(PhotoResult p){
   if(p==null||p.Frame==null||!p.RegistrationValid||!p.AlignmentValid)return false;
   if(p.Frame.Number==0)return true;
   return p.Surface!=null&&p.Surface.Count>=2&&p.Toe>p.GridOriginX&&!double.IsInfinity(p.Toe)&&!double.IsNaN(p.Toe)
    &&Math.Abs(p.Toe-p.GridAnchorX)<=3&&Math.Abs(p.Surface[p.Surface.Count-1].Y)<1e-6
    &&p.Surface.TrueForAll(v=>v!=null&&!double.IsNaN(v.X)&&!double.IsInfinity(v.X)&&!double.IsNaN(v.Y)&&!double.IsInfinity(v.Y));
  }
  public static int ApproveAll(IEnumerable<PhotoResult> photos){
   int count=0;
   foreach(var p in photos){
    if(p.Frame.Number==0)continue;
    if(!CanApprove(p)){p.Approved=false;p.NeedsReview=true;continue;}
    if(!p.Approved){count++;p.Message="Contorno aceptado con Validar todas.";}
    p.Approved=true;p.NeedsReview=false;
   }
   return count;
  }
 }
 public static class Json {
 #if NET8_0_OR_GREATER
  public static T Read<T>(string path){return System.Text.Json.JsonSerializer.Deserialize<T>(System.IO.File.ReadAllText(path));}
  public static void Write(string path,object value){System.IO.File.WriteAllText(path,System.Text.Json.JsonSerializer.Serialize(value));}
 #else
  public static T Read<T>(string path){return new JavaScriptSerializer{MaxJsonLength=int.MaxValue}.Deserialize<T>(System.IO.File.ReadAllText(path));}
  public static void Write(string path,object value){System.IO.File.WriteAllText(path,new JavaScriptSerializer{MaxJsonLength=int.MaxValue}.Serialize(value));}
 #endif
 }
}
