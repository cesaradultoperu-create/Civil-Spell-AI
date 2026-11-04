using System;
using System.Collections.Generic;
using System.Linq;

namespace RelaveAuto {
 // Review-only geometry: translate the previous saved polygon from its frame's
 // A to the current frame's A, then express it in the current preview scale.
 // No point is written back to either measured profile.
 public static class ProfileComparison {
  public static PhotoResult Previous(IEnumerable<PhotoResult> photos,PhotoResult current){
   if(current==null||current.Frame==null)return null;
   return photos.Where(p=>p!=null&&p.Frame!=null&&p.Frame.Number<current.Frame.Number).OrderByDescending(p=>p.Frame.Number).FirstOrDefault();
  }
  public static List<XY> Overlay(PhotoResult previous,PhotoResult current){
   var empty=new List<XY>();
   if(previous==null||current==null||previous.Frame==null||current.Frame==null||!previous.RegistrationValid||!current.RegistrationValid||!previous.AlignmentValid||!current.AlignmentValid||previous.Surface==null||previous.Surface.Count<2)return empty;
   double previousSpan=previous.Frame.MarkerX-previous.Frame.X,currentSpan=current.Frame.MarkerX-current.Frame.X;
   double previousPixels=previous.GridAnchorX-previous.GridOriginX,currentPixels=current.GridAnchorX-current.GridOriginX;
   if(previousSpan<=0||currentSpan<=0||previousPixels<=0||currentPixels<=0)return empty;
   double factor=(previousSpan/previousPixels)/(currentSpan/currentPixels);
   if(double.IsNaN(factor)||double.IsInfinity(factor)||factor<=0)return empty;
   var points=previous.Surface.Select(p=>new XY(current.GridOriginX+(p.X-previous.GridOriginX)*factor,p.Y*factor)).ToList();
   return points.Any(p=>double.IsNaN(p.X)||double.IsNaN(p.Y)||double.IsInfinity(p.X)||double.IsInfinity(p.Y))?empty:points;
  }
 }
 public static class ProfileGeometry {
  public static void SetOrigin(PhotoResult photo,double origin){
   if(!photo.RegistrationValid||double.IsNaN(origin)||double.IsInfinity(origin)||origin<0||origin>=photo.GridAnchorX-2)throw new InvalidOperationException("El inicio A debe quedar dentro de la foto y a la izquierda de la referencia derecha.");
   bool referenceValid=photo.ReferenceAnchorValid||photo.AlignmentValid;
   photo.GridOriginX=origin;photo.OriginManual=true;photo.AlignmentValid=referenceValid;
   if(photo.Surface.Count>=2&&photo.Toe>origin)Normalize(photo);
   photo.Approved=false;photo.NeedsReview=true;photo.Message="Inicio A ajustado. La referencia derecha se conserva. Revise el perfil y valide.";
  }
  public static int SetOriginAll(IEnumerable<PhotoResult> photos,double origin){
   var targets=photos.Where(p=>p.RegistrationValid).ToArray();
   if(targets.Any(p=>origin<0||origin>=p.GridAnchorX-2)||double.IsNaN(origin)||double.IsInfinity(origin))throw new InvalidOperationException("A no puede quedar a la derecha de una referencia. Ajuste esta foto por separado.");
   foreach(var p in targets)SetOrigin(p,origin);return targets.Length;
  }
  public static double Area(IList<XY> p){double sum=0;for(int i=1;i<p.Count;i++)sum+=(p[i].X-p[i-1].X)*(p[i].Y+p[i-1].Y)*.5;return sum;}
  public static double RemoveVertex(PhotoResult photo,int index){
   if(index<=0||index>=photo.Surface.Count-1)throw new InvalidOperationException("El inicio y la punta del perfil estan protegidos. Elija un punto intermedio.");
   double before=Area(photo.Surface);photo.Surface.RemoveAt(index);double change=before>0?(Area(photo.Surface)/before-1)*100:0;
   photo.Approved=false;photo.NeedsReview=true;photo.Message="Punto eliminado. Cambio de area del perfil: "+change.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+"%. Revise y valide; el Excel se recalculara al generar.";
   return change;
  }
  public static int AddVertex(PhotoResult photo,XY point){
   if(photo==null||photo.Surface==null||photo.Surface.Count<2||point==null||double.IsNaN(point.X)||double.IsInfinity(point.X)||double.IsNaN(point.Y)||double.IsInfinity(point.Y)||point.Y<0)
    throw new InvalidOperationException("Marque un punto valido sobre la superficie del relave.");
   var points=photo.Surface;
   if(point.X<=points[0].X+1e-7||point.X>=points[points.Count-1].X-1e-7)
    throw new InvalidOperationException("Agregue el punto entre el inicio A y la punta. Los extremos se conservan.");
   if(points.Any(p=>Math.Abs(p.X-point.X)<=1e-7))throw new InvalidOperationException("Ya existe un punto en esa posicion horizontal. Elija otra posicion.");
   int index=points.FindIndex(p=>p.X>point.X);double before=Area(points);
   points.Insert(index,new XY(point.X,point.Y));double change=before>0?(Area(points)/before-1)*100:0;
   photo.Approved=false;photo.NeedsReview=true;
   photo.Message="Punto agregado. Cambio de area del perfil: "+change.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+"%. Revise y valide; los Excel se recalcularan al generar.";
   return index;
  }
  public static List<XY> Smooth(IList<XY> input,double toe,out double areaChange){
   if(input==null||input.Count<2||toe<=0)throw new InvalidOperationException("Contorno insuficiente para uniformar.");
   var source=input.Select(p=>new XY(p.X,p.Y)).ToList();
   if(source.Count<=8){areaChange=0;return source;}
   double start=source[0].X,span=toe-start;
   int n=Math.Min(401,Math.Max(31,(int)Math.Round(span/3)+1)),j=0;
   var samples=new List<XY>();
   for(int i=0;i<n;i++){
    double x=start+span*i/(n-1.0);while(j<source.Count-2&&source[j+1].X<x)j++;
    var a=source[j];var b=source[j+1];double t=b.X>a.X?(x-a.X)/(b.X-a.X):0;
    samples.Add(new XY(x,Math.Max(0,a.Y+(b.Y-a.Y)*t)));
   }
   int radius=Math.Max(3,(int)Math.Round(n*.04));
   for(int pass=0;pass<2;pass++){
    var fit=new List<XY>();
    for(int i=0;i<n;i++){
     double x=samples[i].X,sw=0,sx=0,sy=0,sxx=0,sxy=0;
     for(int k=Math.Max(0,i-radius);k<=Math.Min(n-1,i+radius);k++){
      double dx=samples[k].X-x,q=Math.Abs(dx)/(span/(n-1.0)*(radius+1)),weight=Math.Pow(1-q*q*q,3);
      sw+=weight;sx+=weight*dx;sy+=weight*samples[k].Y;sxx+=weight*dx*dx;sxy+=weight*dx*samples[k].Y;
     }
     double denominator=sw*sxx-sx*sx;
     fit.Add(new XY(x,Math.Max(0,Math.Abs(denominator)>1e-12?(sy*sxx-sxy*sx)/denominator:samples[i].Y)));
    }
    samples=fit;
   }
   if(Math.Abs(source[source.Count-1].Y)<1e-9)samples[samples.Count-1].Y=0;
   double maxHeight=samples.Max(p=>p.Y),tolerance=Math.Max(.8,maxHeight*.025);
   var reduced=Simplify(samples,tolerance);
   while(reduced.Count>10&&tolerance<Math.Max(2,maxHeight*.06)){tolerance*=1.2;reduced=Simplify(samples,tolerance);}
   double original=Area(source);areaChange=original>1e-9?(Area(reduced)/original-1)*100:0;
   // Preserve the measured integral rather than changing the area for appearance.
   double area=Area(reduced);if(original>0&&area>0){double factor=original/area;foreach(var p in reduced)p.Y*=factor;}
   return reduced;
  }
  static List<XY> Simplify(List<XY> p,double tolerance){
   if(p.Count<=2)return p;
   var a=p[0];var b=p[p.Count-1];double slope=(b.Y-a.Y)/(b.X-a.X),largest=0;int split=0;
   for(int i=1;i<p.Count-1;i++){double distance=Math.Abs(p[i].Y-a.Y-slope*(p[i].X-a.X));if(distance>largest){largest=distance;split=i;}}
   if(largest<=tolerance)return new List<XY>{a,b};
   var left=Simplify(p.GetRange(0,split+1),tolerance);left.RemoveAt(left.Count-1);left.AddRange(Simplify(p.GetRange(split,p.Count-split),tolerance));return left;
  }
  public static void SetToe(PhotoResult photo,double toe){
   if(!photo.RegistrationValid||photo.Surface.Count<2||toe<2||double.IsNaN(toe)||double.IsInfinity(toe))throw new InvalidOperationException("Falta un encuadre o un contorno valido para ajustar A1.");
   var points=photo.Surface.Where(p=>p.X<toe-1e-7).Select(p=>new XY(p.X,p.Y)).ToList();
   if(points.Count==0)throw new InvalidOperationException("La punta A1 debe estar a la derecha del origen A.");
   // The user marks the actual toe, where the surface meets the channel base.
   points.Add(new XY(toe,0));photo.Surface=points;photo.Toe=toe;photo.Approved=false;
   photo.NeedsReview=true;photo.Message=Math.Abs(toe-photo.GridAnchorX)>3?"La punta queda fuera de la marca negra del cuadro. Revise la captura y su referencia antes de validar.":"Punta A1 ajustada. Compruebe el perfil y valide la foto.";
  }
  public static void Normalize(PhotoResult photo,bool atReference=false){
   var source=photo.Surface.OrderBy(p=>p.X).GroupBy(p=>p.X).Select(g=>new XY(g.Key,g.Max(p=>p.Y))).ToList();
   double start=photo.GridOriginX,end=atReference?photo.GridAnchorX:photo.Toe;
   if(source.Count<2||end<=start+2)throw new InvalidOperationException("Falta un perfil o una referencia de cuadricula valida.");
   int j=0;while(j<source.Count-2&&source[j+1].X<start)j++;var a=source[j];var b=source[j+1];
   double height=a.Y+(b.Y-a.Y)*(start-a.X)/(b.X-a.X);
   var points=new List<XY>{new XY(start,Math.Max(0,height))};
   points.AddRange(source.Where(p=>p.X>start+1e-7&&p.X<end-1e-7));points.Add(new XY(end,0));
   photo.Toe=end;photo.Surface=points;
  }
 }
}
