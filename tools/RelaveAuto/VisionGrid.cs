using System;
using System.Linq;
using System.Collections.Generic;
using OpenCvSharp;
namespace RelaveAuto {
 internal static class VisionGrid {
  public static void ValidateCenter(Mat image,PhotoResult photo){
   if(!photo.AlignmentValid||photo.Frame.Number==0)return;
   using(var gray=new Mat()){
    Cv2.CvtColor(image,gray,ColorConversionCodes.BGR2GRAY);gray.GetArray(out byte[] pixels);
    int low=Math.Max(0,(int)Math.Round(photo.GridAnchorX)-5),high=Math.Min(image.Width-1,(int)Math.Round(photo.GridAnchorX)+5),y0=(int)Math.Round(image.Height*.25),y1=(int)Math.Round(image.Height*.75);
    var means=new double[high-low+1];for(int x=low;x<=high;x++){double total=0;for(int y=y0;y<y1;y++)total+=pixels[y*image.Width+x];means[x-low]=total/(y1-y0);}
    double minimum=means.Min(),maximum=means.Max(),weight=0,weightedX=0;
    for(int x=low;x<=high;x++)if(means[x-low]<=minimum+(maximum-minimum)*.3){double w=maximum-means[x-low];weight+=w;weightedX+=x*w;}
    photo.GridMeasurementError=weight>0?Math.Abs(weightedX/weight-photo.GridAnchorX):double.MaxValue;
    if(photo.GridMeasurementError>2){photo.AlignmentValid=false;photo.ReferenceAnchorValid=false;photo.NeedsReview=true;photo.Message="El centro de la marca negra es incierto. Use Centrar linea roja antes de validar.";}
   }
  }
  public static List<double> Centers(Mat image){
   int w=image.Width,h=image.Height;var column=new double[w];
   using(var gray=new Mat()){
    Cv2.CvtColor(image,gray,ColorConversionCodes.BGR2GRAY);gray.GetArray(out byte[] pixels);
    int y0=Math.Max(1,(int)(h*.12)),y1=Math.Min(h-2,(int)(h*.85));
    for(int x=0;x<w;x++){var values=new byte[y1-y0+1];for(int y=y0;y<=y1;y++)values[y-y0]=pixels[y*w+x];Array.Sort(values);column[x]=values[values.Length/2];}
   }
   var response=new double[w];
   for(int x=0;x<w;x++){double sum=0;int count=0;for(int k=Math.Max(0,x-12);k<=Math.Min(w-1,x+12);k++){if(Math.Abs(k-x)<4)continue;sum+=column[k];count++;}response[x]=sum/count-column[x];}
   var candidates=new List<Tuple<double,double>>();
   for(int x=0;x<w;x++)if(response[x]>=7&&(x==0||response[x]>=response[x-1])&&(x==w-1||response[x]>=response[x+1])){
    int a=x,b=x;double limit=Math.Max(3,response[x]*.4);while(a>0&&response[a-1]>limit)a--;while(b<w-1&&response[b+1]>limit)b++;
    double sx=0,sw=0;for(int k=a;k<=b;k++){sx+=k*Math.Max(0,response[k]);sw+=Math.Max(0,response[k]);}candidates.Add(Tuple.Create(sx/sw,response[x]));
   }
   var selected=new List<double>();foreach(var p in candidates.OrderByDescending(p=>p.Item2))if(selected.All(x=>Math.Abs(x-p.Item1)>Math.Max(8,w*.015)))selected.Add(p.Item1);
   selected.Sort();return selected;
  }
  public static void SetAnchors(PhotoResult photo,List<double> reference,List<double> current,int width){
   photo.GridOriginX=0;photo.GridAnchorX=(width-1)*(photo.Frame.Number==0?1:photo.Frame.Number/photo.Frame.Width);
   if(reference.Count<4||current.Count<4){photo.Message="No se reconocen suficientes lineas negras. Centre la referencia manualmente.";photo.NeedsReview=true;return;}
   var gaps=reference.Zip(reference.Skip(1),(a,b)=>b-a).Where(x=>x>Math.Max(8,width*.01)).OrderBy(x=>x).ToArray();
   if(gaps.Length==0)return;double pitch=gaps[gaps.Length/2];photo.GridPitch=pitch;
   // A is the physical channel start explicitly calibrated by the user.
   // It must not jump to the first visible internal grid line as flow hides it.
   double origin=0,end=reference.OrderBy(x=>Math.Abs(x-width+1)).First();
   double target=origin+(end-origin)*(photo.Frame.Number==0?1:photo.Frame.Number/photo.Frame.Width);
   double expected=reference.OrderBy(x=>Math.Abs(x-target)).First();
   double anchor=current.OrderBy(x=>Math.Abs(x-expected)).First(),actualOrigin=0;
   bool edge=photo.Frame.Number==0||Math.Abs(photo.Frame.Number-photo.Frame.Width)<1e-7;
   if(edge){expected=end;anchor=current.OrderBy(x=>Math.Abs(x-end)).First();target=end;}
   photo.GridOriginX=actualOrigin;photo.GridAnchorX=anchor;photo.GridCenterError=Math.Abs(anchor-expected);
   photo.ReferenceAnchorValid=Math.Abs(width-1-end)<pitch*.45&&Math.Abs(expected-target)<pitch*.4&&photo.GridCenterError<Math.Min(6,pitch*.15)&&anchor>actualOrigin+3;
   photo.AlignmentValid=photo.ReferenceAnchorValid;
   // When a reference mark is hidden, show its projected position instead of
   // jumping to an unrelated interior stripe. It remains unresolved and cannot
   // be approved until the user centers the actual mark.
   if(Math.Abs(expected-target)>=pitch*.4)photo.GridAnchorX=target;
   if(!photo.AlignmentValid){photo.NeedsReview=true;photo.Message="Compruebe el centro de la linea negra con Centrar linea roja.";}
  }
 }
}
