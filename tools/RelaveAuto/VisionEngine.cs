using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using OpenCvSharp;
using OpenCvSharp.Features2D;

namespace RelaveAuto {
 public static class VisionEngine {
  sealed class Registration {public double S=1,Tx,Ty,Error,Angle;public int Matches;}
  static Point2f P(XY p){return new Point2f((float)p.X,(float)p.Y);}
  static Point2f Map(XY p,Registration r){return new Point2f((float)(r.S*p.X+r.Tx),(float)(r.S*p.Y+r.Ty));}
  public static BatchResult Run(Job job){
   var clock=Stopwatch.StartNew();var result=new BatchResult();Directory.CreateDirectory(Path.Combine(job.OutputFolder,"Revision"));
   var c=job.Calibration;if(c==null||c.A==null||c.B==null||c.Top==null)throw new InvalidOperationException("Falta calibrar la imagen de referencia.");
   double length=Math.Sqrt(Math.Pow(c.B.X-c.A.X,2)+Math.Pow(c.B.Y-c.A.Y,2));
   double nx=(c.B.Y-c.A.Y)/length,ny=-(c.B.X-c.A.X)/length;
   double height=(c.Top.X-c.A.X)*nx+(c.Top.Y-c.A.Y)*ny;
   if(length<100||height<15||height>length*.6)throw new InvalidOperationException("La base y el borde superior del canal no forman una calibracion valida.");
   int w=(int)Math.Round(Math.Min(1600,length)),h=(int)Math.Round(height*w/length);double ratio=w/length;
   var referenceFile=PhotoInputs.Reference(job.PhotoFolder,job.ReferenceFile);bool emptyReference=PhotoInputs.Number(referenceFile)==0;
   job.ReferenceFile=Path.GetFileName(referenceFile);result.ReferenceFile=job.ReferenceFile;result.DetectionMode=emptyReference?"Referencia inicial sin relave":"Color aprendido de zonas secas de la referencia";
   using(var reference=Cv2.ImRead(referenceFile))using(var referenceGray=new Mat())using(var featureMask=FeatureMask(reference,c,length,height))
   using(var sift=SIFT.Create(2500))using(var descriptors=new Mat()){
    if(reference.Empty()||reference.Width!=c.Width||reference.Height!=c.Height)throw new InvalidOperationException("La foto de referencia cambio de dimensiones. Recalibre.");
    using(var sha=SHA256.Create())using(var stream=File.OpenRead(referenceFile)){var hash=BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","");if(!string.IsNullOrEmpty(c.ReferenceHash)&&!string.Equals(hash,c.ReferenceHash,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("La foto de referencia cambio desde la calibracion. Marque nuevamente los puntos.");}
    Cv2.CvtColor(reference,referenceGray,ColorConversionCodes.BGR2GRAY);
    sift.DetectAndCompute(referenceGray,featureMask,out KeyPoint[] referenceKeys,descriptors);
    if(referenceKeys.Length<25)throw new InvalidOperationException("Se necesitan mas detalles fijos sobre el canal para reconocer el encuadre.");
    using(var baseline=Rectify(reference,c,new Registration(),height,w,h))using(var baselineLab=new Mat()){
     var grid=VisionGrid.Centers(baseline);Json.Write(Path.Combine(job.OutputFolder,"cuadricula-referencia.json"),grid);
     Cv2.CvtColor(baseline,baselineLab,ColorConversionCodes.BGR2Lab);baselineLab.GetArray(out Vec3b[] emptyPixels);
     MaterialColor material=null;AdaptiveMaterial adaptive=null;
     if(!emptyReference){
      var referenceFrame=job.Frames.SingleOrDefault(f=>Math.Abs(f.Number-PhotoInputs.Number(referenceFile))<1e-7);
      if(referenceFrame==null)throw new InvalidOperationException("La foto de referencia "+job.ReferenceFile+" no tiene un cuadro con ese numero en el modelo.");
      var station=new PhotoResult{Frame=referenceFrame};
      VisionGrid.SetAnchors(station,grid,grid,w);material=MaterialColor.Learn(emptyPixels,baseline.Width,h,w,station);
      if(material.Strength<6){
       try{
       string laterFile=PhotoInputs.Files(job.PhotoFolder).Last();
       using(var later=Cv2.ImRead(laterFile)){
        var registration=string.Equals(laterFile,referenceFile,StringComparison.OrdinalIgnoreCase)?new Registration():Register(later,referenceKeys,descriptors,sift);
        if(registration.Error<=2.5&&Math.Abs(registration.Angle)<=.2)using(var laterRectified=Rectify(later,c,registration,height,w,h))adaptive=AdaptiveMaterial.Learn(baseline,laterRectified,w,station,Path.GetFileName(laterFile),material);
       }
       if(adaptive!=null)Json.Write(Path.Combine(job.OutputFolder,"modelo-adaptativo.json"),new{adaptive.A,adaptive.B,adaptive.L,adaptive.Strength,adaptive.Reliable,adaptive.TrainingFile});
       if(adaptive!=null&&adaptive.Reliable)result.DetectionMode="Color y continuidad del material aprendidos dentro de este ensayo";
       }catch(Exception ex){result.Warnings.Add("No se pudo ampliar el aprendizaje del material: "+ex.Message);}
      }
      Json.Write(Path.Combine(job.OutputFolder,"modelo-color.json"),new{material.A,material.B,material.L,material.Strength,material.Reliable,material.UseLuminance,ReferenceFile=job.ReferenceFile});
      emptyPixels=material.Background;
     }
     var files=PhotoInputs.Files(job.PhotoFolder);
     foreach(var file in files){
      double number=PhotoInputs.Number(file);var frame=job.Frames.SingleOrDefault(f=>Math.Abs(f.Number-number)<1e-7);
      if(frame==null){result.Warnings.Add(Path.GetFileName(file)+": no existe un cuadro con ese numero.");continue;}
      var photo=new PhotoResult{File=Path.GetFileName(file),Frame=frame};var tick=Stopwatch.StartNew();
      try{
       using(var image=Cv2.ImRead(file)){
        if(image.Empty())throw new InvalidOperationException("No se puede leer la foto.");
        photo.PixelWidth=image.Width;photo.PixelHeight=image.Height;
        var reg=string.Equals(file,referenceFile,StringComparison.OrdinalIgnoreCase)?new Registration{Matches=referenceKeys.Length}:Register(image,referenceKeys,descriptors,sift);
        photo.Scale=reg.S;photo.Tx=reg.Tx;photo.Ty=reg.Ty;photo.RegistrationError=reg.Error;photo.Matches=reg.Matches;
        if(reg.Error>2.5||Math.Abs(reg.Angle)>.2)throw new InvalidOperationException("El encuadre no coincide con la referencia. Separe esta captura y recalibre su conjunto antes de continuar.");
        photo.RegistrationValid=true;
        using(var rectified=Rectify(image,c,reg,height,w,h)){
         string raw=Path.Combine(job.OutputFolder,"Revision",Path.GetFileNameWithoutExtension(file)+"-canal.png");Cv2.ImWrite(raw,rectified);
         photo.Preview=raw;
         VisionGrid.SetAnchors(photo,grid,VisionGrid.Centers(rectified),w);
         VisionGrid.ValidateCenter(rectified,photo);
         if(number!=0){if(adaptive!=null&&adaptive.Reliable)adaptive.Trace(rectified,photo);else Trace(rectified,emptyPixels,photo,material);}
         else{photo.Message="Estado inicial sin area de relave.";photo.Approved=true;}
         if(!photo.AlignmentValid)photo.Message="Falta validar el centro de la linea negra. Use Centrar linea roja y compruebe la punta antes de aceptar.";
         photo.Preview=raw;
         SavePreview(rectified,photo,Path.Combine(job.OutputFolder,"Revision",Path.GetFileNameWithoutExtension(file)+"-perfil.png"));
        }
       }
      }catch(Exception ex){photo.NeedsReview=true;photo.Message=ex.Message;}
      photo.Seconds=tick.Elapsed.TotalSeconds;result.Photos.Add(photo);
     }
    }
   }
   result.VisionSeconds=clock.Elapsed.TotalSeconds;return result;
  }
  static double PhotoNumber(string file){string s=Path.GetFileNameWithoutExtension(file).Replace(',','.');int n;if(int.TryParse(s,out n))return n>=0?n/100.0:-1;return double.TryParse(s,NumberStyles.Float,CultureInfo.InvariantCulture,out double value)&&value>=0?value:-1;}
  static Mat FeatureMask(Mat image,Calibration c,double length,double height){
   var mask=new Mat(image.Size(),MatType.CV_8UC1,Scalar.Black);
   int x0=Math.Max(0,(int)(c.A.X-.12*length)),x1=Math.Min(image.Width,(int)(c.B.X+.05*length));
   int y0=Math.Max((int)(image.Height*.085),(int)(c.A.Y-.34*length)),y1=Math.Min(image.Height,(int)(Math.Min(c.A.Y,c.B.Y)-height*1.05));
   if(x1<=x0||y1<=y0)throw new InvalidOperationException("Calibracion fuera de la imagen.");
   Cv2.Rectangle(mask,new Rect(x0,y0,x1-x0,y1-y0),Scalar.White,-1);
   using(var hsv=new Mat())using(var warm=new Mat()){
    Cv2.CvtColor(image,hsv,ColorConversionCodes.BGR2HSV);Cv2.InRange(hsv,new Scalar(0,95,40),new Scalar(32,255,255),warm);mask.SetTo(Scalar.Black,warm);
   }
   return mask;
  }
  static Registration Register(Mat image,KeyPoint[] referenceKeys,Mat referenceDescriptors,SIFT sift){
   using(var gray=new Mat())using(var mask=new Mat(image.Size(),MatType.CV_8UC1,Scalar.Black))using(var desc=new Mat())using(var matcher=new BFMatcher(NormTypes.L2)){
    Cv2.CvtColor(image,gray,ColorConversionCodes.BGR2GRAY);
    Cv2.Rectangle(mask,new Rect(0,(int)(image.Height*.085),image.Width,(int)(image.Height*.57)),Scalar.White,-1);
    using(var hsv=new Mat())using(var warm=new Mat()){Cv2.CvtColor(image,hsv,ColorConversionCodes.BGR2HSV);Cv2.InRange(hsv,new Scalar(0,95,40),new Scalar(32,255,255),warm);mask.SetTo(Scalar.Black,warm);}
    sift.DetectAndCompute(gray,mask,out KeyPoint[] keys,desc);
    if(desc.Empty())throw new InvalidOperationException("Sin referencias visuales para alinear la foto.");
    var pairs=matcher.KnnMatch(referenceDescriptors,desc,2).Where(p=>p.Length==2&&p[0].Distance<.72*p[1].Distance).Select(p=>p[0]).ToArray();
    if(pairs.Length<20)throw new InvalidOperationException("Pocas coincidencias de encuadre. Hace falta revisar esta foto.");
    var src=pairs.Select(p=>referenceKeys[p.QueryIdx].Pt).ToArray();var dst=pairs.Select(p=>keys[p.TrainIdx].Pt).ToArray();
    using(var inliers=new Mat())using(var affine=Cv2.EstimateAffinePartial2D(InputArray.Create(src),InputArray.Create(dst),inliers,RobustEstimationAlgorithms.RANSAC,2.5,2500,.995,10)){
     if(affine.Empty())throw new InvalidOperationException("No se pudo comprobar el encuadre.");
     inliers.GetArray(out byte[] flags);var keep=Enumerable.Range(0,pairs.Length).Where(i=>flags[i]!=0).ToArray();
     if(keep.Length<20)throw new InvalidOperationException("No hay suficientes puntos fijos comunes.");
     double ax=keep.Average(i=>(double)src[i].X),ay=keep.Average(i=>(double)src[i].Y),bx=keep.Average(i=>(double)dst[i].X),by=keep.Average(i=>(double)dst[i].Y);
     double denominator=keep.Sum(i=>Math.Pow(src[i].X-ax,2)+Math.Pow(src[i].Y-ay,2));
     double scale=keep.Sum(i=>(src[i].X-ax)*(dst[i].X-bx)+(src[i].Y-ay)*(dst[i].Y-by))/denominator;
     var r=new Registration{S=scale,Tx=bx-scale*ax,Ty=by-scale*ay,Matches=keep.Length,Angle=Math.Atan2(affine.At<double>(1,0),affine.At<double>(0,0))*180/Math.PI};
     r.Error=Math.Sqrt(keep.Average(i=>Math.Pow(r.S*src[i].X+r.Tx-dst[i].X,2)+Math.Pow(r.S*src[i].Y+r.Ty-dst[i].Y,2)));
     if(r.S<.4||r.S>2.5)throw new InvalidOperationException("Cambio de escala demasiado grande. Recalibre la captura.");
     return r;
    }
   }
  }
  static Mat Rectify(Mat image,Calibration c,Registration r,double height,int w,int h){
   double length=Math.Sqrt(Math.Pow(c.B.X-c.A.X,2)+Math.Pow(c.B.Y-c.A.Y,2));
   var top=new XY(c.A.X+(c.B.Y-c.A.Y)/length*height,c.A.Y-(c.B.X-c.A.X)/length*height);
   using(var matrix=Cv2.GetAffineTransform(new[]{Map(c.A,r),Map(c.B,r),Map(top,r)},new[]{new Point2f(0,h-1),new Point2f(w-1,h-1),new Point2f(0,0)})){
    var result=new Mat();Cv2.WarpAffine(image,result,matrix,new Size(w+24,h),InterpolationFlags.Linear,BorderTypes.Constant,Scalar.Black);return result;
   }
  }
  static void Trace(Mat image,Vec3b[] empty,PhotoResult photo,MaterialColor material=null){
   int w=image.Width,h=image.Height;
   if(material!=null&&!material.Reliable)throw new InvalidOperationException("No se distingue con fiabilidad el color del relave en esta referencia. Dibuje el perfil en la revision.");
   using(var lab=new Mat())using(var binary=new Mat(h,w,MatType.CV_8UC1)){
    Cv2.CvtColor(image,lab,ColorConversionCodes.BGR2Lab);lab.GetArray(out Vec3b[] pixels);var bytes=new byte[w*h];
    for(int y=0;y<h;y++)for(int x=3;x<w-3;x++){
     int i=y*w+x;double da=pixels[i].Item1-empty[i].Item1,db=pixels[i].Item2-empty[i].Item2;
     if((material!=null?material.IsMaterial(da,db,pixels[i].Item0-empty[i].Item0):(db>3&&da>-3&&da+db>5))&&pixels[i].Item0>25)bytes[i]=255;
    }
    binary.SetArray(bytes);
    using(var close=Cv2.GetStructuringElement(MorphShapes.Rect,new Size(9,3)))using(var open=Cv2.GetStructuringElement(MorphShapes.Rect,new Size(3,2))){Cv2.MorphologyEx(binary,binary,MorphTypes.Close,close);Cv2.MorphologyEx(binary,binary,MorphTypes.Open,open);}
    using(var labels=new Mat())using(var stats=new Mat())using(var centroids=new Mat()){
     int count=Cv2.ConnectedComponentsWithStats(binary,labels,stats,centroids,PixelConnectivity.Connectivity8,MatType.CV_32S);
     int best=0,bestArea=0;
     for(int i=1;i<count;i++){
      int x=stats.At<int>(i,0),y=stats.At<int>(i,1),cw=stats.At<int>(i,2),ch=stats.At<int>(i,3),area=stats.At<int>(i,4);
      if(x<Math.Max(50,w*.08)&&y+ch>=h-12&&area>bestArea&&cw>10){best=i;bestArea=area;}
     }
     if(best==0)throw new InvalidOperationException("No se distingue el relave conectado a la base izquierda. Trace el contorno en la revision.");
     labels.GetArray(out int[] components);int[] heights=new int[w];
     for(int x=0;x<w;x++)for(int y=0;y<h;y++)if(components[y*w+x]==best){heights[x]=h-1-y;break;}
     int toe=Array.FindLastIndex(heights,v=>v>=1);if(toe<10)throw new InvalidOperationException("El frente del relave no esta definido.");
     var smooth=new List<Point>();
     for(int x=0;x<=toe;x+=3){var sample=Enumerable.Range(Math.Max(0,x-8),Math.Min(toe,x+8)-Math.Max(0,x-8)+1).Select(i=>heights[i]).Where(v=>v>2).OrderBy(v=>v).ToArray();int value=sample.Length==0?0:sample[sample.Length/2];smooth.Add(new Point(x,value));}
     if(smooth.Count<2)throw new InvalidOperationException("Contorno insuficiente.");
     int stable=smooth.FindIndex(p=>p.X>=Math.Min(24,toe*.2)&&p.Y>=3);
     if(stable>=0)for(int i=0;i<stable;i++)smooth[i]=new Point(smooth[i].X,smooth[stable].Y);
     smooth.Add(new Point(toe,Math.Max(1,heights[toe])));
     photo.Toe=toe;photo.Surface=Cv2.ApproxPolyDP(smooth,1.5,false).Select(p=>new XY(p.X,p.Y)).ToList();photo.Surface[0].X=0;
     photo.DetectedToe=toe;
     // Captures are taken at the numbered black station, as specified by the
     // experimental protocol. Only reconcile a nearby segmented toe; distant
     // fronts stay unresolved and cannot be accepted by bulk validation.
     if(photo.AlignmentValid)ProfileGeometry.Normalize(photo,Math.Abs(toe-photo.GridAnchorX)<=Math.Max(3,photo.GridPitch*.35));
     photo.DetectedVertices=photo.Surface.Count;
     photo.Surface=ProfileGeometry.Smooth(photo.Surface,photo.Toe,out double smoothingChange);photo.SmoothingAreaChange=smoothingChange;
     if(Math.Abs(smoothingChange)>2){photo.NeedsReview=true;photo.Message="El perfil necesita revisar el suavizado sobre la fotografia.";}
     double expected=photo.GridAnchorX;
     if(Math.Abs(toe-expected)>3){photo.NeedsReview=true;photo.Message="La punta detectada difiere de la marca negra en "+Math.Abs(toe-expected).ToString("F1",CultureInfo.InvariantCulture)+" px. Revise la punta del relave sin cambiar la escala.";}
     double nearTop=photo.Surface.Max(p=>p.Y)/(h-1.0);
     if(photo.Surface.Max(p=>p.Y)<20){photo.NeedsReview=true;photo.Message="Relave de poca altura: revise el borde y la punta con aumento antes de validar.";}
     if(nearTop>.96){photo.NeedsReview=true;photo.Message="El relave llega al borde del recorte. Revise el borde superior del canal.";}
     if(material!=null&&material.Strength<5){photo.NeedsReview=true;photo.Message=(photo.Message??"")+" Contraste bajo: revise la propuesta sobre el material visible.";}
     if(string.IsNullOrEmpty(photo.Message))photo.Message="Contorno propuesto. Verifique la superposicion.";
    }
   }
  }
  public static void SavePreview(Mat image,PhotoResult photo,string path){
   using(var view=new Mat(image.Height+85,image.Width+40,MatType.CV_8UC3,new Scalar(40,35,30))){
    using(var roi=new Mat(view,new Rect(20,55,image.Width,image.Height)))image.CopyTo(roi);
    Cv2.PutText(view,photo.Frame.Label+"  "+photo.File+(photo.NeedsReview?"  REVISAR":""),new Point(20,30),HersheyFonts.HersheySimplex,.7,Scalar.White,1);
    Cv2.Line(view,new Point(20,55+image.Height-1),new Point(20+image.Width,55+image.Height-1),new Scalar(0,0,255),1);
    if(photo.Surface.Count>1){var points=photo.Surface.Select(p=>new Point((int)Math.Round(p.X)+20,55+image.Height-1-(int)Math.Round(p.Y))).ToArray();Cv2.Polylines(view,new[]{points},false,Scalar.White,1);Cv2.Line(view,new Point(20+(int)photo.GridAnchorX,55),new Point(20+(int)photo.GridAnchorX,55+image.Height-1),new Scalar(0,0,255),1);}
    Cv2.ImWrite(path,view);
   }
  }
 }
}
