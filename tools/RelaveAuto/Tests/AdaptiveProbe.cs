using System;
using System.IO;
using System.Linq;
using OpenCvSharp;
namespace RelaveAuto {
 static class AdaptiveProbe {
  static Mat Image(double toe,double depth,bool shadow=false,bool distant=false){
   var pixels=new Vec3f[724*100];
   for(int y=0;y<100;y++)for(int x=0;x<724;x++){
    bool wet=x<toe&&y>=99-depth*(1-x/toe);
    float light=60,aa=0,bb=-10;
    if(wet){light=55;aa=2;bb=0;}
    if(shadow){light=(float)(30+10.0*y/100);aa=0;bb=-10;}
    if(distant&&x>650&&y>60){light=55;aa=2;bb=0;}
    if(x%50<2||y%25<2)light-=10;
    pixels[y*724+x]=new Vec3f(light,aa,bb);
   }
   using(var lab=new Mat(100,724,MatType.CV_32FC3))using(var bgr=new Mat()){
    lab.SetArray(pixels);Cv2.CvtColor(lab,bgr,ColorConversionCodes.Lab2BGR);var result=new Mat();bgr.ConvertTo(result,MatType.CV_8UC3,255);return result;
   }
  }
  static PhotoResult Photo(double toe)=>new PhotoResult{File="fixture.png",Frame=new Frame{Number=1},RegistrationValid=true,AlignmentValid=true,GridOriginX=0,GridAnchorX=toe,GridPitch=50};
  static int Main(string[] args){
   try{
    using(var reference=Image(100,20))using(var later=Image(700,45)){
     var model=AdaptiveMaterial.Learn(reference,later,700,Photo(100),"later.png",new MaterialColor{A=2,B=10,Strength=Math.Sqrt(104)});
     if(!model.Reliable)throw new Exception("No aprende material visible.");
     var p=Photo(700);model.Trace(later,p);
     double area=ProfileGeometry.Area(p.Surface),expected=700*45*.5;
     if(Math.Abs(area/expected-1)>.1||p.Surface.First().X!=0||p.Surface.Last().Y!=0||p.GridAnchorX!=700||p.Approved||!p.NeedsReview)throw new Exception("El perfil sintetico no conserva area aproximada, A, referencia o estado de revision: "+area);
     using(var shadow=Image(0,0,true)){
      bool rejected=false;try{model.Trace(shadow,Photo(100));}catch(InvalidOperationException){rejected=true;}
      if(!rejected)throw new Exception("Acepto una sombra sin cambio cromatico como relave.");
     }
     using(var reflection=Image(100,20,false,true)){
      var q=Photo(100);model.Trace(reflection,q);if(q.Toe>180)throw new Exception("La reflexion distante reinicio el relave conectado a A.");
     }
     Directory.CreateDirectory(args[0]);File.WriteAllText(Path.Combine(args[0],"adaptativo-ok.txt"),"Material sintetico: error de area="+(100*(area/expected-1)).ToString("F2")+"%; sombra sola rechazada; reflejo remoto excluido; A, anclajes y revision conservados.");
     Console.WriteLine("ADAPTIVE_OK area="+area);return 0;
    }
   }catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
  }
 }
}
