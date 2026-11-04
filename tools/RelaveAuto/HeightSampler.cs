using System;
using System.Collections.Generic;
using System.Linq;
namespace RelaveAuto {
 // Coordinates are read from the final native DWG polygon, in metres.
 public static class HeightSampler {
  static bool Finite(double value)=>!double.IsNaN(value)&&!double.IsInfinity(value);
  public static List<HeightSample> Sample(IList<XY> polygon,double originX,double baseY,double step=.10){
   const double tolerance=1e-8;
   if(polygon==null||polygon.Count<3||!Finite(originX)||!Finite(baseY)||!Finite(step)||step<=0||polygon.Any(p=>p==null||!Finite(p.X)||!Finite(p.Y)))throw new InvalidOperationException("La geometria no es valida para medir alturas.");
   double end=polygon.Max(p=>p.X),length=end-originX;
   if(Math.Abs(polygon.Min(p=>p.X)-originX)>tolerance||length<=tolerance||Math.Abs(polygon.Min(p=>p.Y)-baseY)>tolerance)throw new InvalidOperationException("La base o el origen A no coincide con el perfil de alturas.");
   if(length/step>100000)throw new InvalidOperationException("El perfil requiere demasiadas secciones. Compruebe las unidades del modelo.");
   var distances=new List<double>();int count=(int)Math.Floor((length+tolerance)/step);
   for(int i=0;i<=count;i++)distances.Add(i*step);
   if(Math.Abs(distances.Last()-length)>tolerance)distances.Add(length);
   var result=new List<HeightSample>();
   foreach(double distance in distances){
    double x=Math.Max(originX,Math.Min(end,originX+distance));var intersections=new List<double>();
    for(int i=0;i<polygon.Count;i++){
     var a=polygon[i];var b=polygon[(i+1)%polygon.Count];double dx=b.X-a.X;
     if(Math.Abs(dx)<=tolerance){if(Math.Abs(x-a.X)<=tolerance)intersections.Add(Math.Max(a.Y,b.Y));}
     else if(x>=Math.Min(a.X,b.X)-tolerance&&x<=Math.Max(a.X,b.X)+tolerance){double t=Math.Max(0,Math.Min(1,(x-a.X)/dx));intersections.Add(a.Y+(b.Y-a.Y)*t);}
    }
    if(intersections.Count==0)throw new InvalidOperationException("No se encuentra el borde del relave en la seccion "+distance.ToString("F2",System.Globalization.CultureInfo.InvariantCulture));
    double top=intersections.Max(),height=Math.Max(0,top-baseY);if(height<tolerance){height=0;top=baseY;}
    result.Add(new HeightSample{Index=result.Count,Distance=distance,X=x,BaseY=baseY,TopY=top,Height=height,AtTip=Math.Abs(distance-length)<=tolerance});
   }
   return result;
  }
 }
}
