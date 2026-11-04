using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
namespace RelaveAuto {
 static class UiPreview {
  [STAThread] static int Main(string[] args){try{Run(args);return 0;}catch(Exception ex){Console.Error.WriteLine(ex);return 1;}}
  static void Run(string[] args){
   Application.EnableVisualStyles();string output=args[1];Directory.CreateDirectory(output);
   var job=Json.Read<Job>(args[0]);var result=Json.Read<BatchResult>(Path.Combine(job.OutputFolder,"resultado-vision.json"));
   CheckHeightSampling(output);
   CheckAddedPoints(output);
   CheckExitConfirmation(output);
   CheckNavigation(result,output);
   CheckPreviousReference(result,output);
   CheckReferenceSelection(output);
   CheckBulkValidation(result,output);
   if(result.Photos.Any(p=>p.Surface.Count>3))CheckDeletionWindow(result,output);
   CheckAdditionWindow(result,output);
   CheckReviewExit(result,output);
   CheckOriginWindow(result,output);
   CheckRedrawFromA(result,output);
   Save(new SetupWindow(job.ModelPath),Path.Combine(output,"configuracion.png"));
   Save(new CalibrationWindow(PhotoInputs.Reference(job.PhotoFolder,job.ReferenceFile),job.Calibration),Path.Combine(output,"calibracion.png"));
   Save(new ReviewWindow(result),Path.Combine(output,"revision.png"));
   var small=new ReviewWindow(result){Width=950,Height=500};Save(small,Path.Combine(output,"revision-minima.png"));
  }
  static void CheckAddedPoints(string output){
   var p=new PhotoResult{Approved=true,GridOriginX=10,GridAnchorX=110,Toe=110,Surface=new System.Collections.Generic.List<XY>{new XY(10,20),new XY(110,0)}};
   if(ProfileGeometry.AddVertex(p,new XY(60,20))!=1||ProfileGeometry.Area(p.Surface)!=1500||p.Approved||!p.NeedsReview)throw new Exception("Agregar punto no cambia el area o la revision correctamente.");
   ProfileGeometry.AddVertex(p,new XY(35,25));ProfileGeometry.AddVertex(p,new XY(90,5));
   if(!p.Surface.Select(v=>v.X).SequenceEqual(new[]{10d,35d,60d,90d,110d})||p.Toe!=110||p.GridAnchorX!=110||p.GridOriginX!=10)throw new Exception("Agregar puntos cambia referencias o desordena el perfil.");
   string before=JsonText(p);foreach(var point in new[]{new XY(10,5),new XY(110,1),new XY(35,0),new XY(5,3),new XY(120,2),new XY(80,-1),new XY(double.NaN,2),new XY(80,double.PositiveInfinity)}){
    bool rejected=false;try{ProfileGeometry.AddVertex(p,point);}catch(InvalidOperationException){rejected=true;}
    if(!rejected||JsonText(p)!=before)throw new Exception("Agregar punto invalido modifica el perfil.");
   }
   File.WriteAllText(Path.Combine(output,"agregar-geometria-ok.txt"),"Insercion ordenada, integral analitica, invalidacion de aprobacion, extremos y referencias protegidos; puntos invalidos sin cambios.");
  }
  static string JsonText(object value){return new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(value);}
  sealed class ExitProbe:ProtectedWindow {
   public bool Answer;public int Prompts;
   protected override bool ConfirmExit(){Prompts++;return Answer;}
   public void Escape(){ProcessDialogKey(Keys.Escape);}
   public bool AttemptClose(DialogResult result){DialogResult=result;var e=new FormClosingEventArgs(CloseReason.UserClosing,false);OnFormClosing(e);return !e.Cancel;}
  }
  sealed class ReviewExitProbe:ReviewWindow {
   public DialogResult Answer=DialogResult.No;public int Prompts;
   public ReviewExitProbe(BatchResult batch):base(batch){}
   protected override bool ConfirmExit(){Prompts++;return Answer==DialogResult.Yes;}
   public void Escape(){ProcessDialogKey(Keys.Escape);}
  }
  static void CheckReviewExit(BatchResult sample,string output){
   var source=sample.Photos.First(p=>p.Frame.Number>0&&p.Surface.Count>=2&&p.RegistrationValid);
   var photo=new PhotoResult{Frame=source.Frame,Preview=source.Preview,RegistrationValid=true,AlignmentValid=true,GridOriginX=source.GridOriginX,GridAnchorX=source.GridAnchorX,Toe=source.GridAnchorX,Surface=new System.Collections.Generic.List<XY>{new XY(source.GridOriginX,20),new XY(source.GridAnchorX,0)},Approved=true};
   var batch=new BatchResult();batch.Photos.Add(photo);Exception failure=null;
   using(var form=new ReviewExitProbe(batch){ShowInTaskbar=false,Opacity=0}){
    form.Shown+=(s,e)=>form.BeginInvoke(new Action(()=>{try{
     var canvas=form.Controls.OfType<ImageCanvas>().Single();canvas.ZoomAt(3,new Point(canvas.Width/2,canvas.Height/2));Click((Button)form.Controls.Find("addVertex",true).Single());Pick(canvas,new XY((photo.GridOriginX+photo.Toe)/2,15));
     string saved=JsonText(batch);var surface=canvas.Surface;var view=CanvasView(canvas);int selected=form.Controls.OfType<ListBox>().Single().SelectedIndex;
     foreach(var answer in new[]{DialogResult.No,DialogResult.Cancel}){
      form.Answer=answer;int prompts=form.Prompts;form.Escape();
      if(form.IsDisposed||form.DialogResult!=DialogResult.None||form.Prompts!=prompts+1||JsonText(batch)!=saved||canvas.Surface!=surface||CanvasView(canvas)!=view||form.Controls.OfType<ListBox>().Single().SelectedIndex!=selected)throw new Exception("Esc/No/Cancelar perdio correcciones, seleccion, zoom o estado.");
     }
     var cancel=ControlsOf(form).OfType<Button>().Single(b=>b.Text=="Cancelar");int count=form.Prompts;cancel.PerformClick();
     if(form.IsDisposed||form.DialogResult!=DialogResult.None||form.Prompts!=count+1||JsonText(batch)!=saved)throw new Exception("Boton Cancelar pierde modificaciones al rechazar salida.");
     count=form.Prompts;form.Close();if(form.IsDisposed||form.Prompts!=count+1||JsonText(batch)!=saved)throw new Exception("Cerrar X pierde correcciones al rechazar salida.");
     Click((Button)form.Controls.Find("redrawSurface",true).Single());Pick(canvas,new XY(photo.GridOriginX,15));Pick(canvas,new XY((photo.GridOriginX+photo.Toe)/2,10));
     var draft=canvas.Surface;string draftData=JsonText(draft);form.Escape();
     if(canvas.Surface!=draft||JsonText(canvas.Surface)!=draftData||JsonText(batch)!=saved)throw new Exception("Esc descarta el trazo pendiente de Aplicar.");
     Click((Button)form.Controls.Find("undoPoint",true).Single());if(canvas.Surface.Count!=1)throw new Exception("No se puede continuar editando despues de Esc.");
     form.Answer=DialogResult.Yes;form.Escape();
    }catch(Exception ex){failure=ex;form.Answer=DialogResult.Yes;form.Close();}}));
    form.ShowDialog();
   }
   if(failure!=null)throw failure;
   photo.Approved=true;photo.NeedsReview=false;
   using(var form=new ReviewExitProbe(batch){ShowInTaskbar=false,Opacity=0}){
    form.Shown+=(s,e)=>form.BeginInvoke(new Action(()=>ControlsOf(form).OfType<Button>().Single(b=>b.Text=="Generar DWG y Excel").PerformClick()));
    if(form.ShowDialog()!=DialogResult.OK||form.Prompts!=0)throw new Exception("Generar DWG pide confirmacion de descartar.");
   }
   File.WriteAllText(Path.Combine(output,"salida-revision-ok.txt"),"Ventana real modal: Esc con No y Cancelar, boton Cancelar y X rechazados conservan perfil, zoom, seleccion y trazo sin aplicar; se puede continuar y deshacer. Si sale; Generar no pregunta.");
  }
  static System.Collections.Generic.IEnumerable<Control> ControlsOf(Control root){foreach(Control child in root.Controls){yield return child;foreach(var c in ControlsOf(child))yield return c;}}
  static void CheckExitConfirmation(string output){
   using(var form=new ExitProbe()){
    Prepare(form);if(form.AttemptClose(DialogResult.Cancel)||form.DialogResult!=DialogResult.None||form.IsDisposed||form.Prompts!=1)throw new Exception("Cancelar salida no mantiene la ventana.");
    if(form.AttemptClose(DialogResult.None)||form.Prompts!=2)throw new Exception("Cerrar X no pide confirmacion.");
    form.Answer=true;if(!form.AttemptClose(DialogResult.None)||form.Prompts!=3)throw new Exception("Si no permite salir.");
   }
   using(var form=new ExitProbe()){if(!form.AttemptClose(DialogResult.OK)||form.Prompts!=0)throw new Exception("Generar pide confirmar salida.");}
   // Exercise actual Esc routing through a live modal form, without showing it.
   using(var form=new ExitProbe{ShowInTaskbar=false,Opacity=0}){
    form.Shown+=(s,e)=>form.BeginInvoke(new Action(()=>{form.Escape();if(form.IsDisposed||form.DialogResult!=DialogResult.None||form.Prompts!=1)throw new Exception("Esc descarta la sesion sin confirmar.");form.Answer=true;form.Escape();}));
    form.ShowDialog();if(form.Prompts!=2)throw new Exception("Esc repetido no vuelve a confirmar.");
   }
   File.WriteAllText(Path.Combine(output,"confirmar-salida-ok.txt"),"Cancelar/X/Esc pasan por confirmacion; rechazar mantiene DialogResult.None y ventana; aceptar cierra; Generar OK no pregunta.");
  }
  static void CheckAdditionWindow(BatchResult sample,string output){
   int index=sample.Photos.FindIndex(p=>p.Frame.Number>0&&p.Surface.Count>=2&&p.RegistrationValid);var photo=sample.Photos[index];
   var before=photo.Surface.Select(p=>new XY(p.X,p.Y)).ToList();double anchor=photo.GridAnchorX,origin=photo.GridOriginX,toe=photo.Toe;
   using(var form=new ReviewWindow(sample)){
    Prepare(form);form.Controls.OfType<ListBox>().Single().SelectedIndex=index;var canvas=form.Controls.OfType<ImageCanvas>().Single();canvas.ZoomAt(3,new Point(canvas.Width/2,canvas.Height/2));
    var segment=before.Zip(before.Skip(1),(a,b)=>new{a,b}).OrderByDescending(s=>s.b.X-s.a.X).First();var point=new XY((segment.a.X+segment.b.X)/2,Math.Max(2,(segment.a.Y+segment.b.Y)/2+2));
    double zoom=canvas.ZoomFactor;var view=CanvasView(canvas);Click((Button)form.Controls.Find("addVertex",true).Single());Pick(canvas,point);
    if(photo.Surface.Count!=before.Count+1||photo.Approved||photo.GridAnchorX!=anchor||photo.GridOriginX!=origin||photo.Toe!=toe||canvas.ZoomFactor!=zoom||CanvasView(canvas)!=view)throw new Exception("Agregar en ventana pierde zoom o cambia referencias.");
    Click((Button)form.Controls.Find("undoPoint",true).Single());if(JsonText(photo.Surface)!=JsonText(before)||canvas.ZoomFactor!=zoom)throw new Exception("Deshacer insercion no restaura perfil y zoom.");
    // The two editing modes must be mutually exclusive.
    Click((Button)form.Controls.Find("removeVertex",true).Single());Click((Button)form.Controls.Find("addVertex",true).Single());Pick(canvas,point);
    if(photo.Surface.Count!=before.Count+1)throw new Exception("Agregar despues de eliminar no cambia de modo.");
    Click((Button)form.Controls.Find("undoPoint",true).Single());
    Render(form,Path.Combine(output,"agregar-puntos.png"));
   }
   File.WriteAllText(Path.Combine(output,"agregar-ventana-ok.txt"),"Boton inserta sin redibujar, deshacer exacto, cambio de modo, zoom y encuadre conservados.");
  }
  static void CheckHeightSampling(string output){
   var triangle=new[]{new XY(100,20),new XY(102.2,20),new XY(100,20.22)};
   var sections=HeightSampler.Sample(triangle,100,20);
   if(sections.Count!=23||sections.Count(s=>s.Height>0)!=22)throw new Exception("Muestreo de 2.20 m no incluye 0.00, intervalos y punta.");
   foreach(var s in sections)if(Math.Abs(s.Height-(.22-s.Distance*.1))>1e-10||Math.Abs(s.X-(100+s.Distance))>1e-10)throw new Exception("Altura analitica incorrecta.");
   var shortProfile=new[]{new XY(10,5),new XY(10.25,5),new XY(10.1,5.1),new XY(10,5.2)};
   var shortSamples=HeightSampler.Sample(shortProfile,10,5);
   if(shortSamples.Count!=4||Math.Abs(shortSamples.Last().Distance-.25)>1e-9||shortSamples.Last().Height!=0||!shortSamples.Last().AtTip||Math.Abs(shortSamples[2].Height-1.0/30)>1e-9)throw new Exception("No mide la punta fuera de malla o la interpolacion entre vertices.");
   var reversed=HeightSampler.Sample(shortProfile.Reverse().ToArray(),10,5);if(reversed.Where((p,i)=>Math.Abs(p.Height-shortSamples[i].Height)>1e-10).Any())throw new Exception("Cambiar orientacion cambia alturas.");
   bool invalid=false;try{HeightSampler.Sample(triangle,100,20,0);}catch(InvalidOperationException){invalid=true;}if(!invalid)throw new Exception("Se admite paso invalido.");
   invalid=false;try{HeightSampler.Sample(triangle,100,19);}catch(InvalidOperationException){invalid=true;}if(!invalid)throw new Exception("Se admite una base ajena a la polilinea.");
   File.WriteAllText(Path.Combine(output,"muestreo-alturas-ok.txt"),"Triangulo analitico 2.20 m: 23 filas (A, cada 0.10 y punta cero). Perfil corto con punta fuera de malla, interpolacion, orientacion inversa, paso y base invalidos comprobados.");
  }
  static object Canonical(object value){
   if(value is System.Collections.Generic.IDictionary<string,object> dictionary){var sorted=new System.Collections.Generic.SortedDictionary<string,object>(StringComparer.Ordinal);foreach(var entry in dictionary)sorted[entry.Key]=Canonical(entry.Value);return sorted;}
   if(value is object[] values)return values.Select(Canonical).ToArray();return value;
  }
  static bool SameData(string a,string b){var json=new System.Web.Script.Serialization.JavaScriptSerializer{MaxJsonLength=int.MaxValue};return json.Serialize(Canonical(json.DeserializeObject(File.ReadAllText(a))))==json.Serialize(Canonical(json.DeserializeObject(File.ReadAllText(b))));}
  static RectangleF CanvasView(ImageCanvas canvas)=>(RectangleF)typeof(ImageCanvas).GetMethod("View",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(canvas,null);
  static XY ImagePoint(ImageCanvas canvas,Point screen){var r=CanvasView(canvas);return new XY((screen.X-r.Left)*canvas.ImageSize.Width/r.Width,(screen.Y-r.Top)*canvas.ImageSize.Height/r.Height);}
  static void Mouse(ImageCanvas canvas,string name,MouseButtons button,Point p,int delta=0){typeof(ImageCanvas).GetMethod(name,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(canvas,new object[]{new MouseEventArgs(button,1,p.X,p.Y,delta)});}
  static void CheckNavigation(BatchResult batch,string output){
   string before=Path.Combine(output,"navegacion-antes.json");Json.Write(before,batch);
   using(var form=new ReviewWindow(batch)){
    Prepare(form);var canvas=form.Controls.OfType<ImageCanvas>().Single();var r=CanvasView(canvas);var anchor=new Point((int)(r.Left+r.Width*.3),(int)(r.Top+r.Height*.65));
    var initial=ImagePoint(canvas,anchor);int picked=0;XY last=null;canvas.Pick+=p=>{picked++;last=p;};
    Mouse(canvas,"OnMouseWheel",MouseButtons.None,anchor,120);var after=ImagePoint(canvas,anchor);
    if(Math.Abs(canvas.ZoomFactor-1.25)>1e-6||Math.Abs(after.X-initial.X)>.002||Math.Abs(after.Y-initial.Y)>.002||picked!=0)throw new Exception("El zoom no conserva el pixel bajo el cursor.");
    Mouse(canvas,"OnMouseWheel",MouseButtons.None,anchor,480);Render(form,Path.Combine(output,"zoom-al-cursor.png"));
    var hand=(CheckBox)form.Controls.Find("panImage",true).Single();hand.Checked=true;
    var start=new Point(220,120);r=CanvasView(canvas);Mouse(canvas,"OnMouseDown",MouseButtons.Left,start);Mouse(canvas,"OnMouseMove",MouseButtons.Left,new Point(start.X+83,start.Y-47));Mouse(canvas,"OnMouseUp",MouseButtons.Left,new Point(start.X+83,start.Y-47));
    var moved=CanvasView(canvas);if(Math.Abs(moved.Left-r.Left-83)>.02||Math.Abs(moved.Top-r.Top+47)>.02||picked!=0||canvas.Capture)throw new Exception("La mano no desplaza exactamente o coloca puntos al arrastrar.");
    hand.Checked=false;
    foreach(var button in new[]{MouseButtons.Middle,MouseButtons.Right}){r=CanvasView(canvas);Mouse(canvas,"OnMouseDown",button,start);Mouse(canvas,"OnMouseMove",button,new Point(-40,170));Mouse(canvas,"OnMouseUp",button,new Point(-40,170));moved=CanvasView(canvas);if(Math.Abs(moved.Left-r.Left+260)>.02||Math.Abs(moved.Top-r.Top-50)>.02||picked!=0)throw new Exception("Arrastrar rueda o boton derecho fuera del control pierde desplazamiento.");}
    Click((Button)form.Controls.Find("fitImage",true).Single());if(canvas.ZoomFactor!=1)throw new Exception("Ajustar vista no restablece zoom.");
    Mouse(canvas,"OnMouseWheel",MouseButtons.None,new Point(-5,-5),120);if(canvas.ZoomFactor!=1)throw new Exception("La rueda fuera del visor cambia el zoom.");
    r=CanvasView(canvas);anchor=new Point((int)(r.Left+r.Width*.3),(int)(r.Top+r.Height*.65));Mouse(canvas,"OnMouseWheel",MouseButtons.None,anchor,24000);if(canvas.ZoomFactor!=64)throw new Exception("Limite superior de zoom incorrecto.");
    Mouse(canvas,"OnMouseWheel",MouseButtons.None,anchor,-24000);if(canvas.ZoomFactor!=1)throw new Exception("Limite inferior de zoom incorrecto.");
    Click((Button)form.Controls.Find("fitImage",true).Single());r=CanvasView(canvas);anchor=new Point((int)(r.Left+r.Width*.4),(int)(r.Top+r.Height*.5));Mouse(canvas,"OnMouseWheel",MouseButtons.None,anchor,600);after=ImagePoint(canvas,anchor);
    canvas.BeginPicking();Mouse(canvas,"OnMouseDown",MouseButtons.Left,anchor);Mouse(canvas,"OnMouseUp",MouseButtons.Left,anchor);
    if(picked!=1||Math.Abs(last.X-after.X)>.002||Math.Abs(last.Y-after.Y)>.002)throw new Exception("Seleccionar tras zoom no devuelve la coordenada de imagen correcta.");
    hand.Checked=true;Click((Button)form.Controls.Find("redrawSurface",true).Single());if(hand.Checked||canvas.PanMode)throw new Exception("Redibujar no regresa de mano a seleccion de puntos.");
   }
   string final=Path.Combine(output,"navegacion-despues.json");Json.Write(final,batch);if(!SameData(before,final))throw new Exception("Navegar la imagen modifica los datos medidos.");
   File.WriteAllText(Path.Combine(output,"zoom-mano-ok.txt"),"Zoom al pixel del cursor, limites 1x/64x, mano izquierda, rueda/derecho fuera del control, soltar captura, ajustar vista y seleccion precisa despues del zoom comprobados. Navegar no modifica el lote.");
  }
  static void CheckPreviousReference(BatchResult sample,string output){
   var initial=new PhotoResult{Frame=new Frame{Number=0,Label="0.00"}};
   var missing=new PhotoResult{Frame=new Frame{Number=.3,Label="0.30"}};
   var previous=new PhotoResult{Frame=new Frame{Number=.5,Label="0.50",X=100,MarkerX=100.5},RegistrationValid=true,AlignmentValid=true,GridOriginX=10,GridAnchorX=210,Toe=210,Surface=new System.Collections.Generic.List<XY>{new XY(10,20),new XY(110,10),new XY(210,0)}};
   var current=new PhotoResult{Frame=new Frame{Number=.7,Label="0.70",X=200,MarkerX=200.7},RegistrationValid=true,AlignmentValid=true,GridOriginX=20,GridAnchorX=370,Toe=370};
   var input=new[]{current,initial,previous,missing};
   if(ProfileComparison.Previous(input,current)!=previous||ProfileComparison.Previous(input,previous)!=missing||ProfileComparison.Previous(input,initial)!=null)throw new Exception("La referencia anterior no sigue el orden numerico o inventa una foto faltante.");
   var mapped=ProfileComparison.Overlay(previous,current);
   if(mapped.Count!=3||Math.Abs(mapped[0].X-20)>1e-9||Math.Abs(mapped[0].Y-25)>1e-9||Math.Abs(mapped.Last().X-270)>1e-9)throw new Exception("La referencia anterior no conserva A y escala fisica.");
   double before=ProfileGeometry.Area(previous.Surface)*Math.Pow(.5/200,2),after=ProfileGeometry.Area(mapped)*Math.Pow(.7/350,2);
   if(Math.Abs(before-after)>1e-10)throw new Exception("La superposicion cambia el area fisica del perfil anterior.");
   mapped[0].Y=999;if(previous.Surface[0].Y!=20)throw new Exception("La guia comparte vertices editables con el perfil medido.");
   previous.AlignmentValid=false;if(ProfileComparison.Overlay(previous,current).Count>0)throw new Exception("Muestra escala incierta sin alinear.");previous.AlignmentValid=true;
   if(ProfileComparison.Overlay(missing,current).Count>0||ProfileComparison.Overlay(initial,current).Count>0)throw new Exception("Inventa contorno anterior vacio o inicial.");
   int index=sample.Photos.FindIndex(p=>p.Frame.Number==.6&&p.AlignmentValid&&ProfileComparison.Previous(sample.Photos,p)?.AlignmentValid==true);
   if(index<0)index=sample.Photos.FindIndex(p=>p.Frame.Number>0&&p.AlignmentValid&&ProfileComparison.Overlay(ProfileComparison.Previous(sample.Photos,p),p).Count>1);
   if(index<0)throw new Exception("No hay pareja alineada para probar comparacion visual.");
   string snapshot=Path.Combine(output,"comparacion-antes.json");Json.Write(snapshot,sample);
   using(var form=new ReviewWindow(sample)){
    Prepare(form);var list=form.Controls.OfType<ListBox>().Single();list.SelectedIndex=index;
    var canvas=form.Controls.OfType<ImageCanvas>().Single();var toggle=(CheckBox)form.Controls.Find("showPrevious",true).Single();var label=(Label)form.Controls.Find("previousLabel",true).Single();
    var p=sample.Photos[index];var prior=ProfileComparison.Previous(sample.Photos,p);
    if(!toggle.Checked||canvas.PreviousSurface.Count<2||!label.Text.Contains(prior.Frame.Label))throw new Exception("No muestra numero y geometria anterior al abrir.");
    Render(form,Path.Combine(output,"comparacion-encendida.png"));
    toggle.Checked=false;if(canvas.PreviousSurface.Count!=0)throw new Exception("Apagar deja visible la referencia.");Render(form,Path.Combine(output,"comparacion-apagada.png"));
    int other=sample.Photos.FindIndex(q=>q.Frame.Number>p.Frame.Number);if(other>=0){list.SelectedIndex=other;if(canvas.PreviousSurface.Count!=0||toggle.Checked)throw new Exception("Cambiar foto enciende la referencia desactivada.");}list.SelectedIndex=index;
    toggle.Checked=true;double old=prior.Surface[1].Y;prior.Surface[1].Y+=3;list.SelectedIndex=0;list.SelectedIndex=index;
    var expected=ProfileComparison.Overlay(prior,p);if(Math.Abs(canvas.PreviousSurface[1].Y-expected[1].Y)>1e-9)throw new Exception("No recoge la ultima edicion del perfil anterior.");prior.Surface[1].Y=old;
    list.SelectedIndex=0;if(canvas.PreviousSurface.Count!=0||!label.Text.Contains("Sin foto anterior"))throw new Exception("La primera foto muestra una referencia inexistente.");
    list.SelectedIndex=index;var minimum=new ReviewWindow(sample){Width=950,Height=500};Prepare(minimum);minimum.Controls.OfType<ListBox>().Single().SelectedIndex=index;Save(minimum,Path.Combine(output,"comparacion-minima.png"));minimum.Dispose();
    Click((Button)form.Controls.Find("redrawSurface",true).Single());
    if(canvas.Surface.Count!=0||canvas.PreviousSurface.Count<2)throw new Exception("Redibujar borra la guia anterior o no permite comenzar vacio.");
    Pick(canvas,new XY(p.GridOriginX+.5,20));toggle.Checked=false;toggle.Checked=true;
    if(canvas.Surface.Count!=1||canvas.PreviousSurface.Count<2)throw new Exception("Encender la referencia interrumpe el redibujado actual.");
   }
   string final=Path.Combine(output,"comparacion-despues.json");Json.Write(final,sample);
   if(!SameData(snapshot,final))throw new Exception("Encender, apagar o navegar la referencia modifica datos que se exportan.");
   File.WriteAllText(Path.Combine(output,"comparacion-anterior-ok.txt"),"Referencia anterior numerica, huecos sin inventar fotos, A y escala fisica, area conservada, vertices independientes, sin referencia falsa al inicio, toggle persistente, ultimas ediciones reflejadas y datos exportables identicos despues de usar la guia.");
  }
  static void CheckRedrawFromA(BatchResult sample,string output){
   int index=sample.Photos.FindIndex(p=>p.Frame.Number>0&&p.RegistrationValid&&p.AlignmentValid);if(index<0)return;
   var photo=sample.Photos[index];double origin=photo.GridOriginX,anchor=photo.GridAnchorX;
   // A failed detector must still allow the user to draw from an empty contour.
   photo.Surface.Clear();photo.Approved=false;
   using(var form=new ReviewWindow(sample)){
    Prepare(form);form.Controls.OfType<ListBox>().Single().SelectedIndex=index;var canvas=form.Controls.OfType<ImageCanvas>().Single();
    Click((Button)form.Controls.Find("redrawSurface",true).Single());
    Pick(canvas,new XY((origin+anchor)/2,20));if(canvas.Surface.Count!=0)throw new Exception("El redibujado permitio comenzar lejos de A.");
    Pick(canvas,new XY(origin+.7,20));if(canvas.Surface.Count!=1||canvas.Surface[0].X!=origin)throw new Exception("El primer punto no queda exactamente en A.");
    Pick(canvas,new XY((origin+anchor)/2,12));Pick(canvas,new XY(anchor,0));
    if(canvas.Toe!=anchor)throw new Exception("Dibujar movio la referencia derecha en la vista.");
    Click((Button)form.Controls.Find("applySurface",true).Single());
    if(photo.Surface.Count<2||photo.Surface[0].X!=origin||photo.Surface.Last().Y!=0||photo.GridAnchorX!=anchor)throw new Exception("Aplicar trazo no conserva A y la referencia.");
   }
   File.WriteAllText(Path.Combine(output,"redibujar-a-ok.txt"),"Redibujado desde contorno ausente probado: rechaza primer punto lejos de A, lo fija en A y deja quieta la referencia derecha al marcar y aplicar.");
  }
  static void CheckOriginWindow(BatchResult sample,string output){
   var origins=sample.Photos.Select(p=>p.GridOriginX).ToArray();var anchors=sample.Photos.Select(p=>p.GridAnchorX).ToArray();
   int index=sample.Photos.FindIndex(p=>p.Frame.Number>0&&p.RegistrationValid);if(index<0)throw new Exception("No hay foto para comprobar A.");
   using(var form=new ReviewWindow(sample)){
    Prepare(form);form.Controls.OfType<ListBox>().Single().SelectedIndex=index;
    var all=(CheckBox)form.Controls.Find("originAll",true).Single();all.Checked=false;
    bool referenceValid=sample.Photos[index].AlignmentValid||sample.Photos[index].ReferenceAnchorValid;
    Click((Button)form.Controls.Find("adjustOrigin",true).Single());Pick(form.Controls.OfType<ImageCanvas>().Single(),new XY(8,20));
    if(Math.Abs(sample.Photos[index].GridOriginX-8)>1.5||sample.Photos[index].GridAnchorX!=anchors[index]||sample.Photos[index].AlignmentValid!=referenceValid||sample.Photos[index].Approved)throw new Exception("Inicio A cambia la referencia o no actualiza el inicio.");
    if(sample.Photos.Where((p,i)=>i!=index&&p.GridOriginX!=origins[i]).Any())throw new Exception("A individual modifico otras fotos.");
    all.Checked=true;Click((Button)form.Controls.Find("adjustOrigin",true).Single());Pick(form.Controls.OfType<ImageCanvas>().Single(),new XY(5,20));
    if(sample.Photos.Any(p=>p.RegistrationValid&&Math.Abs(p.GridOriginX-5)>1.5))throw new Exception("Inicio A elegido no se aplica a todas las fotos.");
    if(sample.Photos.Where((p,i)=>p.GridAnchorX!=anchors[i]).Any())throw new Exception("Inicio A para todas cambia la derecha.");
    Click((Button)form.Controls.Find("resetOrigin",true).Single());
    if(sample.Photos.Any(p=>p.RegistrationValid&&(p.GridOriginX!=0||p.Approved)))throw new Exception("Restaurar A no se aplico al ensayo.");
    if(sample.Photos.Where((p,i)=>p.GridAnchorX!=anchors[i]).Any())throw new Exception("Restaurar A movio una referencia derecha.");
   }
   File.WriteAllText(Path.Combine(output,"inicio-a-ok.txt"),"Inicio A individual, aplicacion a todas y Restaurar A probados; referencias derechas intactas y referencias inciertas siguen sin validar.");
  }
  static void Click(Button button){typeof(Button).GetMethod("OnClick",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(button,new object[]{EventArgs.Empty});}
  static void Pick(ImageCanvas canvas,XY surfacePoint){
   var view=(RectangleF)typeof(ImageCanvas).GetMethod("View",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(canvas,null);
   int x=(int)Math.Round(view.Left+surfacePoint.X/canvas.ImageSize.Width*view.Width),y=(int)Math.Round(view.Top+(canvas.ImageSize.Height-1-surfacePoint.Y)/canvas.ImageSize.Height*view.Height);
   typeof(ImageCanvas).GetMethod("OnMouseDown",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(canvas,new object[]{new MouseEventArgs(MouseButtons.Left,1,x,y,0)});
  }
  static void CheckDeletionWindow(BatchResult sample,string output){
   int selected=sample.Photos.FindIndex(1,p=>p.Surface.Count>3);if(selected<0)selected=sample.Photos.FindIndex(p=>p.Surface.Count>3);if(selected<0)throw new Exception("No hay perfil para la prueba de eliminar punto.");
   var photo=sample.Photos[selected];var before=photo.Surface.Select(p=>new XY(p.X,p.Y)).ToList();double anchor=photo.GridAnchorX;
   using(var form=new ReviewWindow(sample)){
    Prepare(form);form.Controls.OfType<ListBox>().Single().SelectedIndex=selected;var canvas=form.Controls.OfType<ImageCanvas>().Single();
    Click((Button)form.Controls.Find("removeVertex",true).Single());Pick(canvas,before[before.Count/2]);
    if(photo.Surface.Count!=before.Count-1||photo.Approved||photo.GridAnchorX!=anchor)throw new Exception("El boton Eliminar punto no aplica la edicion o cambio la escala.");
    Click((Button)form.Controls.Find("undoPoint",true).Single());
    if(photo.Surface.Count!=before.Count||photo.Surface.Where((p,i)=>p.X!=before[i].X||p.Y!=before[i].Y).Any())throw new Exception("Deshacer punto no restaura la geometria exacta.");
    Pick(canvas,before[0]);if(photo.Surface.Count!=before.Count)throw new Exception("Se elimino un extremo protegido desde la ventana.");
   }
   File.WriteAllText(Path.Combine(output,"eliminar-punto-ok.txt"),"Boton y clic sobre vertice probados: elimina punto intermedio, conserva referencia de imagen, deshace exactamente y protege extremos.");
  }
  static void CheckReferenceSelection(string output){
   string folder=Path.Combine(output,"Casos_referencia");Directory.CreateDirectory(folder);
   foreach(var name in new[]{"50.png","100.png","220.png"})File.WriteAllBytes(Path.Combine(folder,name),new byte[0]);
   if(Path.GetFileName(PhotoInputs.Reference(folder))!="50.png")throw new Exception("La primera referencia no es la menor numericamente.");
   File.WriteAllBytes(Path.Combine(folder,"0.png"),new byte[0]);if(Path.GetFileName(PhotoInputs.Reference(folder))!="0.png")throw new Exception("No selecciona 0 cuando esta disponible.");
   File.WriteAllBytes(Path.Combine(folder,"0.50.png"),new byte[0]);bool duplicate=false;try{PhotoInputs.Files(folder);}catch(InvalidOperationException){duplicate=true;}if(!duplicate)throw new Exception("No rechaza duplicados numericos.");
   foreach(var file in Directory.GetFiles(folder))File.Delete(file);Directory.Delete(folder);
   File.WriteAllText(Path.Combine(output,"referencia-ok.txt"),"Primera foto numerica sin 0, prioridad de 0 existente y rechazo de duplicados verificados.");
  }
  static void CheckBulkValidation(BatchResult sample,string output){
   Func<double,PhotoResult> make=n=>new PhotoResult{Frame=new Frame{Number=n,Label=n.ToString("0.00")},RegistrationValid=true,AlignmentValid=true,GridAnchorX=10,Toe=10,Surface=new System.Collections.Generic.List<XY>{new XY(0,5),new XY(10,0)}};
   var zero=make(0);zero.Approved=true;
   var normal=make(.2);var warning=make(.3);warning.NeedsReview=true;
   var corrected=make(.4);corrected.Approved=true;corrected.Message="Correccion conservada";
   var badRegistration=make(.5);badRegistration.RegistrationValid=false;badRegistration.Approved=true;
   var noContour=make(.6);noContour.Surface.Clear();
   var nonFinite=make(.7);nonFinite.Toe=double.NaN;
   var wrongStation=make(.8);wrongStation.Toe=40;wrongStation.Surface.Last().X=40;
   var photos=new[]{zero,normal,warning,corrected,badRegistration,noContour,nonFinite,wrongStation};
   var original=corrected.Surface;int count=ReviewActions.ApproveAll(photos);
   if(count!=2||!normal.Approved||!warning.Approved||warning.NeedsReview||!corrected.Approved||corrected.Surface!=original||corrected.Message!="Correccion conservada"||badRegistration.Approved||noContour.Approved||nonFinite.Approved||wrongStation.Approved||!zero.Approved)throw new Exception("Validar todas: fallo en estados o preservacion de correcciones.");
   if(ReviewActions.ApproveAll(photos)!=0)throw new Exception("Validar todas no es idempotente.");
   CheckProfiles();
   using(var form=new ReviewWindow(sample)){
    Prepare(form);var button=(Button)form.Controls.Find("approveAll",true).Single();
    typeof(Button).GetMethod("OnClick",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(button,new object[]{EventArgs.Empty});
    if(sample.Photos.Any(p=>p.Frame.Number>0&&ReviewActions.CanApprove(p)&&!p.Approved))throw new Exception("El boton no valida las propuestas completas.");
   }
   File.WriteAllText(Path.Combine(output,"validar-todas-ok.txt"),"Boton conectado; propuestas y advertencias aceptadas; fallos de encuadre, contorno vacio y NaN pendientes; correcciones conservadas; operacion idempotente.");
  }
  static void CheckProfiles(){
   var originPhoto=new PhotoResult{RegistrationValid=true,ReferenceAnchorValid=false,AlignmentValid=false,GridOriginX=30,GridAnchorX=100,Toe=100};ProfileGeometry.SetOrigin(originPhoto,0);
   if(originPhoto.GridOriginX!=0||originPhoto.GridAnchorX!=100||originPhoto.AlignmentValid)throw new Exception("Mover A valido indebidamente la referencia derecha.");
   var editable=new PhotoResult{Approved=true,AlignmentValid=true,RegistrationValid=true,GridAnchorX=10,Toe=10,Surface=new System.Collections.Generic.List<XY>{new XY(0,10),new XY(5,8),new XY(10,0)}};
   double change=ProfileGeometry.RemoveVertex(editable,1);
   if(editable.Surface.Count!=2||editable.Surface[0].X!=0||editable.Surface[1].X!=10||Math.Abs(ProfileGeometry.Area(editable.Surface)-50)>1e-9||Math.Abs(change+23.0769230769)>1e-8||editable.Approved||!editable.NeedsReview||editable.GridAnchorX!=10)throw new Exception("Eliminar punto no conserva extremos o no actualiza la medicion.");
   bool protectedStart=false,protectedEnd=false;try{ProfileGeometry.RemoveVertex(editable,0);}catch(InvalidOperationException){protectedStart=true;}try{ProfileGeometry.RemoveVertex(editable,editable.Surface.Count-1);}catch(InvalidOperationException){protectedEnd=true;}if(!protectedStart||!protectedEnd)throw new Exception("Los extremos no estan protegidos.");
   var points=Enumerable.Range(0,251).Select(i=>new XY(5+i*4,60-i*.2+Math.Sin(i*1.8)*2)).ToList();points[points.Count-1].Y=0;
   double original=ProfileGeometry.Area(points);var reduced=ProfileGeometry.Smooth(points,1005,out double delta);
   if(reduced.Count>=points.Count/3||Math.Abs(ProfileGeometry.Area(reduced)/original-1)>1e-10||Math.Abs(reduced[0].X-5)>1e-8||Math.Abs(reduced.Last().X-1005)>1e-8||Math.Abs(reduced.Last().Y)>1e-8)throw new Exception("Suavizado: extremos, vertices o area incorrectos.");
   var p=new PhotoResult{GridOriginX=5,GridAnchorX=990,Toe=980,RegistrationValid=true,AlignmentValid=true,Surface=reduced};ProfileGeometry.Normalize(p);
   if(p.Surface[0].X!=5||p.Surface.Last().X!=980||p.Surface.Last().Y!=0||p.Toe!=980)throw new Exception("El perfil no termina en la referencia fija.");
  }
  static void Save(Form form,string path){using(form)Render(form,path);}
  static void Render(Form form,string path){Prepare(form);using(var image=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(image,new Rectangle(0,0,form.Width,form.Height));using(var graphics=Graphics.FromImage(image)){var inset=new Point((form.Width-form.ClientSize.Width)/2,form.Height-form.ClientSize.Height-(form.Width-form.ClientSize.Width)/2);PaintChildren(form,graphics,inset);}image.Save(path);}}
  static void Prepare(Control c){var handle=c.Handle;c.PerformLayout();foreach(Control child in c.Controls)Prepare(child);}
  static void PaintChildren(Control root,Graphics graphics,Point offset){foreach(Control c in root.Controls.Cast<Control>().Reverse()){if(c.Width<=0||c.Height<=0)continue;using(var picture=new Bitmap(c.Width,c.Height)){c.DrawToBitmap(picture,new Rectangle(0,0,c.Width,c.Height));graphics.DrawImageUnscaled(picture,offset.X+c.Left,offset.Y+c.Top);}PaintChildren(c,graphics,new Point(offset.X+c.Left,offset.Y+c.Top));}}
 }
}
