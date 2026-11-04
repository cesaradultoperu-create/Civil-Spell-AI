using System;
using System.Collections.Generic;
using System.Linq;
using OpenCvSharp;
namespace RelaveAuto {
 // Reconstruct the dry plastic from dry regions of the SAME first capture.
 // A nonzero reference contains material and must never be subtracted as empty.
 internal sealed class MaterialColor {
  public Vec3b[] Background;public double A,B,L,Strength;public bool Reliable,UseLuminance;
  static double Median(IEnumerable<double> source){var a=source.OrderBy(x=>x).ToArray();return a.Length==0?0:a[a.Length/2];}
  public static MaterialColor Learn(Vec3b[] pixels,int width,int height,int nominalWidth,PhotoResult station){
   var model=new MaterialColor{Background=new Vec3b[pixels.Length]};
   int start=(int)Math.Ceiling(station.GridAnchorX+Math.Max(20,nominalWidth*.06)),end=Math.Min(width-1,nominalWidth-4);
   if(end-start<40)return model;
   int top0=Math.Max(1,(int)(height*.1)),top1=Math.Max(top0+1,(int)(height*.28));
   var rowA=new double[height];var rowB=new double[height];var rowL=new double[height];
   for(int y=0;y<height;y++){
    var dry=Enumerable.Range(start,end-start).Select(x=>pixels[y*width+x]).Where(p=>p.Item0>35).ToArray();
    rowA[y]=Median(dry.Select(p=>(double)p.Item1));rowB[y]=Median(dry.Select(p=>(double)p.Item2));rowL[y]=Median(dry.Select(p=>(double)p.Item0));
   }
   double upperA=Median(rowA.Skip(top0).Take(top1-top0)),upperB=Median(rowB.Skip(top0).Take(top1-top0)),upperL=Median(rowL.Skip(top0).Take(top1-top0));
   for(int x=0;x<width;x++){
    var dry=Enumerable.Range(top0,top1-top0).Select(y=>pixels[y*width+x]).Where(p=>p.Item0>35).ToArray();
    double columnA=Median(dry.Select(p=>(double)p.Item1))-upperA,columnB=Median(dry.Select(p=>(double)p.Item2))-upperB,columnL=Median(dry.Select(p=>(double)p.Item0))-upperL;
    for(int y=0;y<height;y++)model.Background[y*width+x]=new Vec3b((byte)Math.Max(0,Math.Min(255,Math.Round(rowL[y]+columnL))),(byte)Math.Max(0,Math.Min(255,Math.Round(rowA[y]+columnA))),(byte)Math.Max(0,Math.Min(255,Math.Round(rowB[y]+columnB))));
   }
   int left=(int)Math.Ceiling(station.GridOriginX+5),right=(int)(station.GridOriginX+(station.GridAnchorX-station.GridOriginX)*.6);
   int bottomStart=Math.Max(0,height-Math.Max(5,(int)(height*.08)));
   var deltaA=new List<double>();var deltaB=new List<double>();var deltaL=new List<double>();
   for(int y=bottomStart;y<height-1;y++)for(int x=left;x<right;x++){
    int i=y*width+x;if(pixels[i].Item0<35)continue;
    deltaA.Add(pixels[i].Item1-model.Background[i].Item1);deltaB.Add(pixels[i].Item2-model.Background[i].Item2);deltaL.Add(pixels[i].Item0-model.Background[i].Item0);
   }
   model.A=Median(deltaA);model.B=Median(deltaB);model.L=Median(deltaL);model.Strength=Math.Sqrt(model.A*model.A+model.B*model.B);
   // Luminance alone confuses panel shadows with material. Keep the measured
   // intensity for diagnostics; weak chromatic proposals require visual review.
   model.UseLuminance=false;
   model.Reliable=deltaA.Count>=20&&model.Strength>=3.5;
   return model;
  }
  public bool IsMaterial(double da,double db,double dl){
   double projection=(da*A+db*B)/Strength,side=Math.Abs(da*B-db*A)/Strength;
   return projection>Math.Max(3,Strength*.25)&&side<Math.Max(5,Strength*.6)+Math.Max(0,projection)*.35;
  }
 }
}
