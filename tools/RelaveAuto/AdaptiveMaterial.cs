using System;
using System.Collections.Generic;
using System.Linq;
using OpenCvSharp;

namespace RelaveAuto {
 // Weakly colored, transparent deposits need regional evidence rather than
 // connected components of individual quantized Lab pixels. All training is
 // from this trial; geometry and black-grid anchors are never changed here.
 internal sealed class AdaptiveMaterial {
  float[] background;int width,height,nominalWidth;
  public double A,B,L,Strength;public bool Reliable;public string TrainingFile;
  static double Median(IEnumerable<double> values){var a=values.OrderBy(x=>x).ToArray();return a.Length==0?0:a[a.Length/2];}
  static float[] Pixels(Mat image){
   using(var input=new Mat())using(var lab=new Mat()){
    image.ConvertTo(input,MatType.CV_32FC3,1.0/255);Cv2.CvtColor(input,lab,ColorConversionCodes.BGR2Lab);
    lab.GetArray(out Vec3f[] pixels);var result=new float[pixels.Length*3];
    for(int i=0;i<pixels.Length;i++){result[i*3]=pixels[i].Item0*2.55f;result[i*3+1]=pixels[i].Item1+128;result[i*3+2]=pixels[i].Item2+128;}return result;
   }
  }
  public static AdaptiveMaterial Learn(Mat reference,Mat later,int nominalWidth,PhotoResult station,string trainingFile,MaterialColor initial){
   var model=new AdaptiveMaterial{width=reference.Width,height=reference.Height,nominalWidth=nominalWidth,TrainingFile=trainingFile};
   int w=model.width,h=model.height,start=(int)Math.Ceiling(station.GridAnchorX+Math.Max(20,nominalWidth*.06)),end=Math.Min(w-1,nominalWidth-4);
   if(end-start<40)return model;
   var pixels=Pixels(reference);model.background=new float[pixels.Length];
   int top0=Math.Max(1,(int)(h*.1)),top1=Math.Max(top0+1,(int)(h*.28));var row=new double[h,3];var upper=new double[3];
   for(int channel=0;channel<3;channel++){
    for(int y=0;y<h;y++)row[y,channel]=Median(Enumerable.Range(start,end-start).Where(x=>pixels[(y*w+x)*3]>35).Select(x=>(double)pixels[(y*w+x)*3+channel]));
    upper[channel]=Median(Enumerable.Range(top0,top1-top0).Select(y=>row[y,channel]));
    for(int x=0;x<w;x++){
     double column=Median(Enumerable.Range(top0,top1-top0).Where(y=>pixels[(y*w+x)*3]>35).Select(y=>(double)pixels[(y*w+x)*3+channel]))-upper[channel];
     for(int y=0;y<h;y++)model.background[(y*w+x)*3+channel]=(float)(row[y,channel]+column);
    }
   }
   // The thin layer in the first screenshot may include floor reflections.
   // A later capture provides an interior sample above that reflective strip.
   var wet=Pixels(later);int x0=Math.Max(5,(int)(nominalWidth*.05)),x1=Math.Min(nominalWidth-4,(int)(nominalWidth*.25));
   int y0=(int)(h*.6),y1=(int)(h*.85);var samples=new List<int>();
   for(int y=y0;y<y1;y++)for(int x=x0;x<x1;x++)if(wet[(y*w+x)*3]>35)samples.Add((y*w+x)*3);
   model.L=Median(samples.Select(i=>(double)wet[i]-model.background[i]));model.A=Median(samples.Select(i=>(double)wet[i+1]-model.background[i+1]));model.B=Median(samples.Select(i=>(double)wet[i+2]-model.background[i+2]));
   model.Strength=Math.Sqrt(model.A*model.A+model.B*model.B);
   double agreement=initial.Strength>0&&model.Strength>0?(initial.A*model.A+initial.B*model.B)/(initial.Strength*model.Strength):0;
   model.Reliable=samples.Count>=100&&model.Strength>=5&&agreement>.6;return model;
  }
  public void Trace(Mat image,PhotoResult photo){
   if(!Reliable)throw new InvalidOperationException("El contraste del material sigue siendo incierto. Dibuje el perfil de esta foto en la revision.");
   int w=width,h=height;var pixels=Pixels(image);var score=new double[w*h];var projection=new double[w*h];
   var interior=new List<double>();int seedEnd=Math.Min(nominalWidth-5,Math.Max(40,(int)(photo.GridAnchorX*.6)));
   for(int y=(int)(h*.7);y<(int)(h*.92);y++)for(int x=24;x<seedEnd;x++){
    int i=(y*w+x)*3;interior.Add(((pixels[i+1]-background[i+1])*A+(pixels[i+2]-background[i+2])*B)/Strength);
   }
   interior.Sort();double evidence=interior.Count>0?interior[(int)(interior.Count*.85)]:0;
   if(evidence<2.5)throw new InvalidOperationException("La capa de esta foto no presenta suficiente contraste. Dibuje el perfil en la revision.");
   double localStrength=Math.Max(Strength*.4,Math.Min(Strength,evidence));
   for(int i=0;i<w*h;i++){
    double da=pixels[i*3+1]-background[i*3+1],db=pixels[i*3+2]-background[i*3+2],dl=pixels[i*3]-background[i*3];
    double p=(da*A+db*B)/Strength,side=Math.Abs(da*B-db*A)/Strength;projection[i]=p;
    double depth=h-1-i/w,thinLayer=Math.Max(.35,Math.Min(1,depth/(h*.15)));
    // Near the floor a transparent film has a weaker chromatic signal than
    // the thick interior used for training. Do not truncate that thin tail
    // using the same color threshold as the deep deposit.
    double value=Math.Max(-2,Math.Min(2,(p-localStrength*.45*thinLayer)/Math.Max(1,localStrength*.3)))-Math.Max(0,side-Math.Max(3,Strength))*.2;
    // Intensity only corroborates chromatic evidence; it cannot classify a shadow alone.
    if(L < -4)value+=Math.Max(-1,Math.Min(1,dl/L-.4))*.2;
    score[i]=value;
   }
   var xs=new List<int>();for(int x=12;x<Math.Min(nominalWidth,w-12);x+=4)xs.Add(x);
   int n=xs.Count,states=h+1;var costs=new double[n,states];var values=new double[13];var colors=new double[13];
   for(int i=0;i<n;i++){
    int x=xs[i];var features=new double[h];var edge=new double[h];
    for(int y=0;y<h;y++){
     for(int k=-6;k<=6;k++){values[k+6]=score[y*w+x+k];colors[k+6]=projection[y*w+x+k];}
     Array.Sort(values);Array.Sort(colors);features[y]=values[6];edge[y]=colors[6];
    }
    double total=features.Sum(),sum=0;
    for(int y=0;y<=h;y++){
     costs[i,y]=2*sum-total+.02*(h-y);if(y<h)sum+=features[y];
     if(y< h*.12)costs[i,y]=1e6;
     if(y>=4&&y<h-4){double above=0,below=0;for(int k=1;k<=4;k++){above+=edge[y-k];below+=edge[y+k];}costs[i,y]-=Math.Max(0,Math.Min(6,(below-above)/4))*1.2;}
    }
   }
   var previous=new double[states];for(int y=0;y<states;y++)previous[y]=costs[0,y];var parents=new int[n,states];
   for(int i=1;i<n;i++){
    var next=new double[states];
    for(int y=0;y<states;y++){
     double best=double.MaxValue;int parent=y;
     for(int shift=-3;shift<=3;shift++){int source=y+shift;if(source<0||source>=states)continue;double candidate=previous[source]+Math.Abs(shift)*3;if(candidate<best){best=candidate;parent=source;}}
     next[y]=costs[i,y]+best;parents[i,y]=parent;
    }
    previous=next;
   }
   int row=Array.IndexOf(previous,previous.Min());var heights=new double[n];
   for(int i=n-1;i>=0;i--){heights[i]=Math.Max(0,h-1-row);row=parents[i,row];}
   // Follow only the layer connected to A. A remote reflection near the outlet
   // must not restart the deposit after a run of dry floor.
   int last=n-1,dryRun=0;
   for(int i=0;i<n;i++){
    if(heights[i]<2)dryRun++;else dryRun=0;
    if(dryRun>=4&&i>=4){last=i-dryRun;break;}
   }
   while(last>0&&heights[last]<2)last--;
   if(last<2||heights.Take(last+1).Max()<3)throw new InvalidOperationException("No hay evidencia suficiente de un perfil conectado a A. Dibujelo en la revision.");
   double toe=xs[Math.Min(n-1,last+1)];
   photo.Surface=new List<XY>{new XY(photo.GridOriginX,heights[Math.Min(3,last)])};
   for(int i=3;i<=last;i++)photo.Surface.Add(new XY(xs[i],heights[i]));photo.Surface.Add(new XY(toe,0));photo.Toe=toe;photo.DetectedToe=toe;
   if(photo.AlignmentValid&&Math.Abs(toe-photo.GridAnchorX)<=Math.Max(3,photo.GridPitch*.35))ProfileGeometry.Normalize(photo,true);
   photo.DetectedVertices=photo.Surface.Count;photo.Surface=ProfileGeometry.Smooth(photo.Surface,photo.Toe,out double change);photo.SmoothingAreaChange=change;
   photo.NeedsReview=true;photo.Approved=false;photo.Message="Perfil propuesto con color aprendido dentro de este ensayo. Revise la superposicion y la punta; contraste bajo.";
   if(Math.Abs(photo.Toe-photo.GridAnchorX)>3)photo.Message="La punta detectada difiere de la referencia en "+Math.Abs(photo.Toe-photo.GridAnchorX).ToString("F1",System.Globalization.CultureInfo.InvariantCulture)+" px. Use Punta del relave para revisar el extremo real; la escala de la imagen queda fija.";
  }
 }
}
