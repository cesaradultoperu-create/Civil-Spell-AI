using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Security.Cryptography;
using Autodesk.AutoCAD.Runtime;
using App=Autodesk.AutoCAD.ApplicationServices.Core.Application;
using UiApp=Autodesk.AutoCAD.ApplicationServices.Application;

namespace RelaveAuto {
 public sealed class Commands:IExtensionApplication {
  public void Initialize(){App.DocumentManager.MdiActiveDocument?.Editor.WriteMessage("\nRELAVEAUTO 0.3.5 — Escriba RELAVEAUTO.\n");}
  public void Terminate(){}
  [CommandMethod("RELAVEAUTO",CommandFlags.Modal)]
  public void Run(){
   var doc=App.DocumentManager.MdiActiveDocument;if(doc==null)return;string output=null;
   try{
    Job job;
    using(var setup=new SetupWindow(doc.Name)){
     if(UiApp.ShowModalDialog(setup)!=DialogResult.OK)return;
     var frames=DrawingEngine.ReadFrames(setup.ModelPath);string reference=PhotoInputs.Reference(setup.PhotoFolder);string hash=Hash(reference);
     string calibrationPath=Path.Combine(setup.PhotoFolder,"relave-calibracion.json");Calibration previous=null;
     if(File.Exists(calibrationPath)){try{var saved=Json.Read<Calibration>(calibrationPath);if(saved.ReferenceHash==hash)previous=saved;}catch{}}
     using(var calibrate=new CalibrationWindow(reference,previous)){
      if(UiApp.ShowModalDialog(calibrate)!=DialogResult.OK)return;
      calibrate.Calibration.ReferenceHash=hash;calibrate.Calibration.ReferenceFile=Path.GetFileName(reference);Json.Write(calibrationPath,calibrate.Calibration);
      output=Path.Combine(setup.PhotoFolder,"RELAVEAUTO_"+DateTime.Now.ToString("yyyyMMdd_HHmmss_fff"));Directory.CreateDirectory(output);
      job=new Job{ModelPath=setup.ModelPath,PhotoFolder=setup.PhotoFolder,OutputFolder=output,Calibration=calibrate.Calibration,Frames=frames,ReferenceFile=Path.GetFileName(reference)};
     }
    }
    BatchResult result;
    using(var progress=new ProgressWindow(token=>RunVision(job,token))){UiApp.ShowModalDialog(progress);progress.ThrowIfFailed();result=progress.Result;}
    if(result==null||result.Photos.Count==0)throw new InvalidOperationException("No se procesaron fotos correspondientes al modelo.");
    using(var review=new ReviewWindow(result))if(UiApp.ShowModalDialog(review)!=DialogResult.OK){doc.Editor.WriteMessage("\nProceso cancelado. No se generó el DWG ni el Excel.\n");return;}
    DrawingEngine.Generate(job,result);
    doc.Editor.WriteMessage("\nRELAVEAUTO: {0} fotos; {1} perfiles. Analisis: {2:F2} s; DWG y Excel: {3:F2} s.\nSalida: {4}\n",result.Photos.Count,result.Photos.Count(p=>p.Frame.Number>0),result.VisionSeconds,result.DrawingSeconds,output);
    string heightsNote=result.Heights.Count>0?"\n\nAlturas del perfil "+result.Heights[0].ProfileNumber.ToString("0.00")+": cada 0.10 m.\nArchivo: Alturas_ultimo_perfil.xlsx":"\n\nNo hay perfil final con relave para medir alturas.";
    MessageBox.Show("Trabajo generado.\n\n"+output+heightsNote+"\n\nAnálisis: "+result.VisionSeconds.ToString("F2")+" s\nDWG y Excel: "+result.DrawingSeconds.ToString("F2")+" s","RELAVEAUTO");
   }catch(OperationCanceledException){doc.Editor.WriteMessage("\nRELAVEAUTO cancelado.\n");}
   catch(System.Exception ex){doc.Editor.WriteMessage("\nRELAVEAUTO: "+ex.Message+"\n");if(output!=null)File.WriteAllText(Path.Combine(output,"error.txt"),ex.ToString());MessageBox.Show(ex.Message,"RELAVEAUTO",MessageBoxButtons.OK,MessageBoxIcon.Information);}
  }
  public static BatchResult RunVision(Job job,CancellationToken cancel){
   string root=Path.GetDirectoryName(typeof(Commands).Assembly.Location),engine=Path.Combine(root,"Engine","RelaveVision.exe");
   if(!File.Exists(engine))engine=Path.GetFullPath(Path.Combine(root,"..","..","Engine","RelaveVision.exe"));
   if(!File.Exists(engine))throw new FileNotFoundException("Falta la carpeta Engine del complemento.",engine);
   string jobFile=Path.Combine(job.OutputFolder,"trabajo.json");Json.Write(jobFile,job);
   var stdout=new StringBuilder();var stderr=new StringBuilder();
   using(var process=new Process{StartInfo=new ProcessStartInfo{FileName=engine,Arguments="\""+jobFile+"\"",WorkingDirectory=root,UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden,RedirectStandardOutput=true,RedirectStandardError=true}}){
    process.OutputDataReceived+=(s,e)=>{if(e.Data!=null)lock(stdout)stdout.AppendLine(e.Data);};process.ErrorDataReceived+=(s,e)=>{if(e.Data!=null)lock(stderr)stderr.AppendLine(e.Data);};
    process.Start();process.BeginOutputReadLine();process.BeginErrorReadLine();
    while(!process.WaitForExit(150)){if(cancel.IsCancellationRequested){try{process.Kill();}catch{}cancel.ThrowIfCancellationRequested();}}
    process.WaitForExit();File.WriteAllText(Path.Combine(job.OutputFolder,"motor.log"),stdout.ToString()+stderr);
    if(process.ExitCode!=0)throw new InvalidOperationException("El análisis no pudo terminar. "+stderr);
   }
   return Json.Read<BatchResult>(Path.Combine(job.OutputFolder,"resultado-vision.json"));
  }
  static string Hash(string file){using(var sha=SHA256.Create())using(var s=File.OpenRead(file))return BitConverter.ToString(sha.ComputeHash(s)).Replace("-","");}
  [CommandMethod("RELAVEAUTOPRUEBA",CommandFlags.Modal)]
  public void Test(){
   string config=Environment.GetEnvironmentVariable("RELAVEAUTO_JOB");if(string.IsNullOrEmpty(config))throw new InvalidOperationException("Prueba interna: falta RELAVEAUTO_JOB.");
   var job=Json.Read<Job>(config);try{
    var clock=Stopwatch.StartNew();job.Frames=DrawingEngine.ReadFrames(job.ModelPath);Directory.CreateDirectory(job.OutputFolder);var result=RunVision(job,CancellationToken.None);
    var review=Environment.GetEnvironmentVariable("RELAVEAUTO_REVIEW");
    if(!string.IsNullOrEmpty(review)){
     var corrections=Json.Read<PhotoResult[]>(review);
     foreach(var correction in corrections){var photo=result.Photos.Single(p=>p.File==correction.File);if(correction.GridAnchorX>0){photo.GridAnchorX=correction.GridAnchorX;photo.AlignmentValid=correction.AlignmentValid;photo.AlignmentManual=true;}photo.Surface=correction.Surface;photo.Toe=correction.Toe;ProfileGeometry.Normalize(photo);photo.Approved=true;photo.NeedsReview=false;photo.Message="Correccion aplicada durante la prueba de revision.";}
     Json.Write(Path.Combine(job.OutputFolder,"revision-aplicada.json"),corrections);
    }
    if(Environment.GetEnvironmentVariable("RELAVEAUTO_BULK_TEST")=="1")ReviewActions.ApproveAll(result.Photos);
    else foreach(var proposal in result.Photos.Where(p=>ReviewActions.CanApprove(p)&&!p.NeedsReview))proposal.Approved=true;
    DrawingEngine.Generate(job,result);File.WriteAllText(Path.Combine(job.OutputFolder,"prueba-ok.txt"),"RELAVEAUTO 0.3.5; "+result.Photos.Count+" fotos; vision="+result.VisionSeconds.ToString("F3")+" s; dwg_excel="+result.DrawingSeconds.ToString("F3")+" s; total="+clock.Elapsed.TotalSeconds.ToString("F3")+" s. Propuestas automáticas pendientes de revisión visual.");
   }catch(System.Exception ex){Directory.CreateDirectory(job.OutputFolder);File.WriteAllText(Path.Combine(job.OutputFolder,"prueba-error.txt"),ex.ToString());}
  }
 }
}
