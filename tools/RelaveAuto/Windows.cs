using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;
using System.Threading;

namespace RelaveAuto {
 internal class ProtectedWindow:Form {
  protected bool ExitAllowed;
  protected virtual bool ConfirmExit(){
   return MessageBox.Show(this,"¿Está seguro de que quiere salir del comando? Perderá las selecciones y modificaciones de esta sesión que no haya generado.\n\nSí: salir.\nNo o Cancelar: seguir trabajando.","RELAVEAUTO — Confirmar salida",MessageBoxButtons.YesNoCancel,MessageBoxIcon.Warning,MessageBoxDefaultButton.Button2)==DialogResult.Yes;
  }
  protected override bool ProcessDialogKey(Keys keyData){
   if((keyData&Keys.KeyCode)==Keys.Escape){Close();return true;}
   return base.ProcessDialogKey(keyData);
  }
  protected override void OnFormClosing(FormClosingEventArgs e){
   base.OnFormClosing(e);
   if(!e.Cancel&&!ExitAllowed&&DialogResult!=DialogResult.OK){
    if(ConfirmExit())ExitAllowed=true;
    else {e.Cancel=true;DialogResult=DialogResult.None;}
   }
  }
 }
 internal sealed class SetupWindow:ProtectedWindow {
  readonly TextBox model=new TextBox{Dock=DockStyle.Fill},photos=new TextBox{Dock=DockStyle.Fill};
  public string ModelPath=>model.Text.Trim();public string PhotoFolder=>photos.Text.Trim();
  public SetupWindow(string active){
   Text="RELAVEAUTO 0.3.5 — Nuevo trabajo";Width=780;Height=290;StartPosition=FormStartPosition.CenterScreen;Font=new Font("Segoe UI",10);MinimizeBox=false;MaximizeBox=false;FormBorderStyle=FormBorderStyle.FixedDialog;
   var layout=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(18),ColumnCount=3,RowCount=4};layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,125));layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,100));
   layout.Controls.Add(new Label{Text="Modelo DWG",AutoSize=true},0,0);layout.Controls.Add(model,1,0);var bm=new Button{Text="Buscar...",Dock=DockStyle.Fill};layout.Controls.Add(bm,2,0);
   layout.Controls.Add(new Label{Text="Carpeta de fotos",AutoSize=true},0,1);layout.Controls.Add(photos,1,1);var bp=new Button{Text="Buscar...",Dock=DockStyle.Fill};layout.Controls.Add(bp,2,1);
   var note=new Label{Text="Se creará una carpeta nueva con el DWG, las fotos, el Excel y las vistas de revisión.",AutoSize=true,MaximumSize=new Size(700,0)};layout.Controls.Add(note,0,2);layout.SetColumnSpan(note,3);
   var buttons=new FlowLayoutPanel{FlowDirection=FlowDirection.RightToLeft,Dock=DockStyle.Fill,AutoSize=true};var next=new Button{Text="Calibrar referencia",AutoSize=true};var cancel=new Button{Name="cancelCommand",Text="Cancelar",AutoSize=true};cancel.Click+=(s,e)=>Close();buttons.Controls.Add(next);buttons.Controls.Add(cancel);layout.Controls.Add(buttons,0,3);layout.SetColumnSpan(buttons,3);Controls.Add(layout);
   void ReferenceNote(){if(!Directory.Exists(PhotoFolder))return;try{note.Text="Referencia: "+Path.GetFileName(PhotoInputs.Reference(PhotoFolder))+" (primera foto disponible). Los numeros sin foto quedan libres.\nSe creara una carpeta nueva con el DWG, las fotos y el Excel.";}catch(Exception ex){note.Text=ex.Message;}}
   void UseModelFolder(){if(!File.Exists(ModelPath))return;string folder=Path.GetDirectoryName(ModelPath);if(Directory.GetFiles(folder,"*.png").Any(f=>PhotoInputs.Number(f)>=0))photos.Text=folder;}
   photos.TextChanged+=(s,e)=>ReferenceNote();
   if(File.Exists(active)){model.Text=active;UseModelFolder();}
   bm.Click+=(s,e)=>{using(var d=new OpenFileDialog{Filter="Dibujo DWG|*.dwg",Title="Seleccione el modelo de cuadros"})if(d.ShowDialog(this)==DialogResult.OK){model.Text=d.FileName;UseModelFolder();}};
   bp.Click+=(s,e)=>{using(var d=new FolderBrowserDialog{Description="Seleccione la carpeta de las fotos de este ensayo; no es obligatorio tener 0.png"})if(d.ShowDialog(this)==DialogResult.OK)photos.Text=d.SelectedPath;};
   next.Click+=(s,e)=>{if(!File.Exists(ModelPath)){MessageBox.Show(this,"El modelo DWG seleccionado no existe.");return;}if(!Directory.Exists(PhotoFolder)){MessageBox.Show(this,"Seleccione la carpeta de fotos de este ensayo.");return;}try{PhotoInputs.Reference(PhotoFolder);}catch(Exception ex){MessageBox.Show(this,ex.Message);return;}DialogResult=DialogResult.OK;Close();};CancelButton=cancel;
  }
 }
 internal sealed class ImageCanvas:Control {
  Image image;float zoom=1;PointF offset;Point drag;bool panning,panMode,spacePan;
  public List<XY> Markers=new List<XY>();public List<XY> Surface=new List<XY>();public List<XY> Guide=new List<XY>();public List<XY> PreviousSurface=new List<XY>();public double Toe,Origin;public bool IsContour,ShowVertices;
  public event Action<XY> Pick;
  public event Action ViewChanged;
  public double ZoomFactor=>zoom;
  public bool PanMode {get=>panMode;set{if(panMode==value)return;panMode=value;UpdateCursor();ViewChanged?.Invoke();}}
  public ImageCanvas(){DoubleBuffered=true;SetStyle(ControlStyles.Selectable,true);BackColor=Color.FromArgb(32,38,44);Dock=DockStyle.Fill;TabStop=true;Cursor=Cursors.Cross;}
  public void LoadImage(string path){image?.Dispose();image=null;Markers.Clear();Surface=new List<XY>();Guide=new List<XY>();PreviousSurface=new List<XY>();Toe=0;EndPan();if(File.Exists(path))using(var src=Image.FromFile(path))image=new Bitmap(src);Fit();}
  public Size ImageSize=>image?.Size??Size.Empty;
  RectangleF View(){if(image==null)return RectangleF.Empty;float scale=Math.Min((Width-20f)/image.Width,(Height-20f)/image.Height)*zoom;return new RectangleF((Width-image.Width*scale)/2+offset.X,(Height-image.Height*scale)/2+offset.Y,image.Width*scale,image.Height*scale);}
  PointF Display(XY p){var v=View();return new PointF(v.Left+(float)p.X/image.Width*v.Width,v.Top+(float)p.Y/image.Height*v.Height);}
  public void Fit(){zoom=1;offset=PointF.Empty;Invalidate();ViewChanged?.Invoke();}
  public void ZoomAt(double factor,Point anchor){
   if(image==null||factor<=0||double.IsNaN(factor)||double.IsInfinity(factor))return;
   var before=View();if(before.Width<=0||before.Height<=0)return;
   double x=Math.Max(before.Left,Math.Min(before.Right,anchor.X)),y=Math.Max(before.Top,Math.Min(before.Bottom,anchor.Y));
   double u=(x-before.Left)/before.Width,v=(y-before.Top)/before.Height;
   float next=(float)Math.Max(1,Math.Min(64,zoom*factor));double ratio=next/zoom;
   offset=new PointF((float)(x-Width/2.0-(u-.5)*before.Width*ratio),(float)(y-Height/2.0-(v-.5)*before.Height*ratio));zoom=next;
   Invalidate();ViewChanged?.Invoke();
  }
  public void BeginPicking(){PanMode=false;spacePan=false;EndPan();Focus();}
  void UpdateCursor(){Cursor=panning?Cursors.SizeAll:panMode||spacePan?Cursors.Hand:Cursors.Cross;}
  void EndPan(){panning=false;if(Capture)Capture=false;UpdateCursor();}
  public int NearestVertex(XY picked){if(image==null||Surface.Count==0)return -1;var click=Display(picked);int nearest=-1;double best=14*14;for(int i=0;i<Surface.Count;i++){var point=Display(new XY(Surface[i].X,image.Height-1-Surface[i].Y));double distance=Math.Pow(point.X-click.X,2)+Math.Pow(point.Y-click.Y,2);if(distance<=best){best=distance;nearest=i;}}return nearest;}
  protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);if(image==null)return;var v=View();e.Graphics.DrawImage(image,v);e.Graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
   using(var pen=new Pen(Color.Lime,2))using(var font=new Font("Segoe UI",11,FontStyle.Bold)){
    if(Markers.Count>=2)e.Graphics.DrawLine(pen,Display(Markers[0]),Display(Markers[1]));
    for(int i=0;i<Markers.Count;i++){var p=Display(Markers[i]);e.Graphics.DrawEllipse(pen,p.X-5,p.Y-5,10,10);e.Graphics.DrawString(new[]{"A","B","Superior"}[Math.Min(i,2)],font,Brushes.Lime,p.X+7,p.Y+3);}
   }
   if(IsContour){
    if(PreviousSurface.Count>1)using(var pen=new Pen(Color.Gold,2)){
     var polygon=PreviousSurface.Select(p=>Display(new XY(p.X,image.Height-1-p.Y))).ToList();
     polygon.Add(Display(new XY(PreviousSurface[0].X,image.Height-1)));e.Graphics.DrawPolygon(pen,polygon.ToArray());
    }
    using(var pen=new Pen(Color.Red,1)){e.Graphics.DrawLine(pen,Display(new XY(0,image.Height-1)),Display(new XY(image.Width-1,image.Height-1)));e.Graphics.DrawLine(pen,Display(new XY(Origin,0)),Display(new XY(Origin,image.Height-1)));if(Toe>0)e.Graphics.DrawLine(pen,Display(new XY(Toe,0)),Display(new XY(Toe,image.Height-1)));}
    if(Guide.Count>1)using(var pen=new Pen(Color.Gray,1)){e.Graphics.DrawLines(pen,Guide.Select(p=>Display(new XY(p.X,image.Height-1-p.Y))).ToArray());}
    if(Surface.Count==1){var p=Display(new XY(Surface[0].X,image.Height-1-Surface[0].Y));e.Graphics.FillEllipse(Brushes.White,p.X-4,p.Y-4,8,8);}
    if(Surface.Count>1)using(var pen=new Pen(Color.White,2)){var pts=Surface.Select(p=>Display(new XY(p.X,image.Height-1-p.Y))).ToArray();e.Graphics.DrawLines(pen,pts);}
    if(ShowVertices)for(int i=0;i<Surface.Count;i++){var p=Display(new XY(Surface[i].X,image.Height-1-Surface[i].Y));e.Graphics.FillRectangle(i==0||i==Surface.Count-1?Brushes.Orange:Brushes.Cyan,p.X-4,p.Y-4,8,8);}
   }
  }
  protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);Focus();if(image!=null&&(e.Button==MouseButtons.Right||e.Button==MouseButtons.Middle||(e.Button==MouseButtons.Left&&(panMode||spacePan)))){panning=true;drag=e.Location;Capture=true;UpdateCursor();return;}if(e.Button!=MouseButtons.Left||image==null)return;var v=View();var p=new XY((e.X-v.Left)/v.Width*image.Width,(e.Y-v.Top)/v.Height*image.Height);if(p.X>=0&&p.X<image.Width&&p.Y>=0&&p.Y<image.Height)Pick?.Invoke(p);}
  protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(panning){offset=new PointF(offset.X+e.X-drag.X,offset.Y+e.Y-drag.Y);drag=e.Location;Invalidate();}}
  protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);EndPan();}
  protected override void OnMouseCaptureChanged(EventArgs e){base.OnMouseCaptureChanged(e);if(!Capture){panning=false;UpdateCursor();}}
  protected override void OnMouseEnter(EventArgs e){base.OnMouseEnter(e);Focus();}
  protected override void OnMouseWheel(MouseEventArgs e){if(ClientRectangle.Contains(e.Location)){if(e is HandledMouseEventArgs handled)handled.Handled=true;ZoomAt(Math.Pow(1.25,e.Delta/120.0),e.Location);}base.OnMouseWheel(e);}
  protected override void OnKeyDown(KeyEventArgs e){base.OnKeyDown(e);if(e.KeyCode==Keys.Space){spacePan=true;UpdateCursor();e.Handled=true;e.SuppressKeyPress=true;}else if(e.KeyCode==Keys.Home){Fit();e.Handled=true;}}
  protected override void OnKeyUp(KeyEventArgs e){base.OnKeyUp(e);if(e.KeyCode==Keys.Space){spacePan=false;UpdateCursor();e.Handled=true;}}
  protected override void OnLostFocus(EventArgs e){base.OnLostFocus(e);spacePan=false;EndPan();}
  protected override void Dispose(bool disposing){if(disposing)image?.Dispose();base.Dispose(disposing);}
 }
 internal sealed class ImageViewToolbar:Panel {
  public ImageViewToolbar(ImageCanvas canvas){
   Name="viewToolbar";Dock=DockStyle.Top;Height=34;Padding=new Padding(8,0,8,0);
   var buttons=new FlowLayoutPanel{Dock=DockStyle.Left,Width=375,WrapContents=false};
   var minus=new Button{Name="zoomOut",Text="-",Width=34};var plus=new Button{Name="zoomIn",Text="+",Width=34};var fit=new Button{Name="fitImage",Text="Ajustar vista",AutoSize=true};
   var hand=new CheckBox{Name="panImage",Text="Mano",Appearance=Appearance.Button,AutoSize=true};var zoomLabel=new Label{Name="zoomLabel",AutoSize=true,Margin=new Padding(8,8,0,0)};
   buttons.Controls.AddRange(new Control[]{minus,plus,fit,hand,zoomLabel});
   var hint=new Label{Dock=DockStyle.Fill,Text="Rueda: zoom al cursor. Mano + arrastrar: desplazar.",AutoEllipsis=true,TextAlign=ContentAlignment.MiddleLeft};Controls.Add(hint);Controls.Add(buttons);
   void Update(){hand.Checked=canvas.PanMode;zoomLabel.Text="Zoom x"+canvas.ZoomFactor.ToString("F1");}
   plus.Click+=(s,e)=>{canvas.ZoomAt(1.25,new Point(canvas.Width/2,canvas.Height/2));canvas.Focus();};minus.Click+=(s,e)=>{canvas.ZoomAt(.8,new Point(canvas.Width/2,canvas.Height/2));canvas.Focus();};fit.Click+=(s,e)=>{canvas.Fit();canvas.Focus();};
   hand.CheckedChanged+=(s,e)=>canvas.PanMode=hand.Checked;canvas.ViewChanged+=Update;Update();
  }
 }
 internal sealed class CalibrationWindow:ProtectedWindow {
  readonly ImageCanvas canvas=new ImageCanvas();readonly Label guide=new Label{Dock=DockStyle.Top,Height=58,Padding=new Padding(10)};readonly List<XY> points=new List<XY>();
  public Calibration Calibration {get;private set;}
  public CalibrationWindow(string image,Calibration previous){
   Text="RELAVEAUTO 0.3.5 — Calibracion de "+Path.GetFileName(image);Width=1150;Height=790;StartPosition=FormStartPosition.CenterScreen;Font=new Font("Segoe UI",10);MinimumSize=new Size(800,500);
   canvas.LoadImage(image);if(previous!=null){points.Add(previous.A);points.Add(previous.B);points.Add(previous.Top);}canvas.Markers=points;
   var bar=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=48,FlowDirection=FlowDirection.RightToLeft,Padding=new Padding(8)};var ok=new Button{Text="Usar referencia",AutoSize=true};var reset=new Button{Text="Volver a marcar",AutoSize=true};var cancel=new Button{Name="cancelCommand",Text="Cancelar",AutoSize=true};cancel.Click+=(s,e)=>Close();bar.Controls.Add(ok);bar.Controls.Add(reset);bar.Controls.Add(cancel);Controls.Add(canvas);Controls.Add(new ImageViewToolbar(canvas));Controls.Add(guide);Controls.Add(bar);
   void UpdateGuide(){guide.Text=points.Count==0?"1. Marque A: esquina inferior izquierda del canal.":points.Count==1?"2. Marque B: esquina inferior derecha del canal.":points.Count==2?"3. Marque el borde superior de la pared del canal (sobre A).":"Referencia lista. Verifique A, B y el borde superior antes de continuar.";guide.Text+="\nRueda: zoom al cursor. Mano, rueda pulsada o botón derecho: desplazar.";ok.Enabled=points.Count==3;canvas.Invalidate();}
   canvas.Pick+=p=>{if(points.Count<3){points.Add(p);UpdateGuide();}};reset.Click+=(s,e)=>{canvas.BeginPicking();points.Clear();UpdateGuide();};
   ok.Click+=(s,e)=>{double dx=points[1].X-points[0].X,dy=points[1].Y-points[0].Y,l=Math.Sqrt(dx*dx+dy*dy);double height=(points[2].X-points[0].X)*dy/l-(points[2].Y-points[0].Y)*dx/l;if(dx<=0||l<100||height<15||height>l*.6){MessageBox.Show(this,"La referencia no es válida. Marque A a la izquierda, B a la derecha y el borde superior del canal.");return;}Calibration=new Calibration{A=points[0],B=points[1],Top=points[2],Width=canvas.ImageSize.Width,Height=canvas.ImageSize.Height};DialogResult=DialogResult.OK;Close();};UpdateGuide();CancelButton=cancel;
  }
 }
 internal sealed class ProgressWindow:ProtectedWindow {
  readonly BackgroundWorker worker=new BackgroundWorker();readonly CancellationTokenSource cancel=new CancellationTokenSource();Exception failure;
  public BatchResult Result {get;private set;}
  public ProgressWindow(Func<CancellationToken,BatchResult> action){
   Text="RELAVEAUTO 0.3.5 — Procesando fotografías";Width=540;Height=175;StartPosition=FormStartPosition.CenterScreen;ControlBox=false;FormBorderStyle=FormBorderStyle.FixedDialog;Font=new Font("Segoe UI",10);
   Controls.Add(new Label{Text="Reconociendo encuadres y proponiendo contornos...",Dock=DockStyle.Top,Height=45,Padding=new Padding(10)});Controls.Add(new ProgressBar{Dock=DockStyle.Top,Height=22,Style=ProgressBarStyle.Marquee});var button=new Button{Text="Cancelar",Dock=DockStyle.Bottom,Height=34};Controls.Add(button);button.Click+=(s,e)=>RequestCancellation();
   worker.DoWork+=(s,e)=>{try{Result=action(cancel.Token);}catch(Exception ex){failure=ex;}};worker.RunWorkerCompleted+=(s,e)=>{ExitAllowed=true;DialogResult=failure==null?DialogResult.OK:DialogResult.Cancel;Close();};Shown+=(s,e)=>worker.RunWorkerAsync();
  }
  void RequestCancellation(){if(!cancel.IsCancellationRequested&&ConfirmExit())cancel.Cancel();}
  protected override void OnFormClosing(FormClosingEventArgs e){if(!ExitAllowed){e.Cancel=true;RequestCancellation();return;}base.OnFormClosing(e);}
  public void ThrowIfFailed(){if(failure!=null)throw failure;}
 }
 internal class ReviewWindow:ProtectedWindow {
  readonly ListBox list=new ListBox{Dock=DockStyle.Left,Width=185};readonly ImageCanvas canvas=new ImageCanvas{IsContour=true};readonly Label info=new Label{Dock=DockStyle.Top,Height=70,Padding=new Padding(10)};
  readonly BatchResult batch;PhotoResult current;bool editing,adjustingToe,adjustingMaterialToe,removingPoint,addingPoint,adjustingOrigin,updatingList;List<XY> edits;readonly Stack<List<XY>> deletedPoints=new Stack<List<XY>>();
  public ReviewWindow(BatchResult result){
   batch=result;Text="RELAVEAUTO 0.3.5 — Revisar los perfiles";Width=1280;Height=650;MinimumSize=new Size(950,500);StartPosition=FormStartPosition.CenterScreen;Font=new Font("Segoe UI",10);
   var bar=new TableLayoutPanel{Dock=DockStyle.Bottom,Height=132,Padding=new Padding(8),ColumnCount=1,RowCount=3};for(int row=0;row<3;row++)bar.RowStyles.Add(new RowStyle(SizeType.Percent,100f/3));
   var editBar=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false};var referenceBar=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false};var actionBar=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false};bar.Controls.Add(editBar,0,0);bar.Controls.Add(referenceBar,0,1);bar.Controls.Add(actionBar,0,2);
   var redraw=new Button{Name="redrawSurface",Text="Redibujar superficie",AutoSize=true};var undo=new Button{Name="undoPoint",Text="Deshacer punto",AutoSize=true};var apply=new Button{Name="applySurface",Text="Aplicar trazo",AutoSize=true};var approve=new Button{Text="Validar esta foto",AutoSize=true};var approveAll=new Button{Name="approveAll",Text="Validar todas",AutoSize=true};var save=new Button{Text="Generar DWG y Excel",AutoSize=true};var cancel=new Button{Name="cancelCommand",Text="Cancelar",AutoSize=true};cancel.Click+=(s,e)=>Close();
   var toeButton=new Button{Name="adjustToe",Text="Centrar línea roja",AutoSize=true};var uniform=new Button{Name="smoothProfile",Text="Uniformar perfil",AutoSize=true};var materialToe=new Button{Name="materialToe",Text="Punta del relave",AutoSize=true};
   var remove=new Button{Name="removeVertex",Text="Eliminar punto",AutoSize=true};
   var add=new Button{Name="addVertex",Text="Agregar puntos",AutoSize=true};
   var originButton=new Button{Name="adjustOrigin",Text="Inicio A",AutoSize=true};var originAll=new CheckBox{Name="originAll",Text="Aplicar A a todas",Checked=true,AutoSize=true,Margin=new Padding(8,8,10,0)};var resetOrigin=new Button{Name="resetOrigin",Text="Restaurar A",AutoSize=true};
   var comparisonBar=new Panel{Name="comparisonBar",Dock=DockStyle.Top,Height=36,Padding=new Padding(8,0,8,0)};
   var showPrevious=new CheckBox{Name="showPrevious",Text="Mostrar polilínea anterior",Checked=true,Dock=DockStyle.Left,Width=245};
   var previousLabel=new Label{Name="previousLabel",Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,AutoEllipsis=true};comparisonBar.Controls.Add(previousLabel);comparisonBar.Controls.Add(showPrevious);
   editBar.Controls.AddRange(new Control[]{redraw,undo,apply,uniform,remove,add});referenceBar.Controls.AddRange(new Control[]{originButton,resetOrigin,originAll,toeButton,materialToe});actionBar.Controls.AddRange(new Control[]{approve,approveAll,save,cancel});Controls.Add(canvas);Controls.Add(list);Controls.Add(new ImageViewToolbar(canvas));Controls.Add(comparisonBar);Controls.Add(info);Controls.Add(bar);
   void UpdateComparison(){
    canvas.PreviousSurface=new List<XY>();var previous=ProfileComparison.Previous(batch.Photos,current);
    if(!showPrevious.Checked)previousLabel.Text="Referencia anterior apagada.";
    else if(previous==null)previousLabel.Text="Sin foto anterior en este ensayo.";
    else if(previous.Frame.Number==0)previousLabel.Text="Referencia anterior 0.00: estado inicial sin relave.";
    else if(previous.Surface==null||previous.Surface.Count<2)previousLabel.Text="Referencia anterior "+previous.Frame.Label+": sin contorno.";
    else {
     canvas.PreviousSurface=ProfileComparison.Overlay(previous,current);
     previousLabel.Text="Referencia polilínea anterior: "+previous.Frame.Label+(canvas.PreviousSurface.Count==0?" — revise su alineación.":" · amarillo"+(previous.Approved?"":" · por validar"));
    }
    canvas.Invalidate();
   }
   showPrevious.CheckedChanged+=(s,e)=>UpdateComparison();
   void UpdateList(bool preserveView=false){int selected=list.SelectedIndex;updatingList=true;list.Items.Clear();foreach(var p in batch.Photos)list.Items.Add(p.Frame.Label+"  "+(p.Frame.Number==0?"Inicial":p.Approved?"Revisada":!p.RegistrationValid?"ENCUADRE":!p.AlignmentValid?"CENTRAR":p.Surface.Count<2?"DIBUJAR":Math.Abs(p.Toe-p.GridAnchorX)>3?"PUNTA":p.NeedsReview?"REVISAR":"Propuesta"));if(selected>=0)list.SelectedIndex=selected;updatingList=false;if(preserveView){canvas.Surface=current.Surface;canvas.Toe=current.GridAnchorX;canvas.Origin=current.GridOriginX;UpdateComparison();}else Select();}
   void Select(){if(updatingList||list.SelectedIndex<0)return;var selected=batch.Photos[list.SelectedIndex];if(!ReferenceEquals(current,selected))deletedPoints.Clear();current=selected;editing=false;adjustingOrigin=false;adjustingToe=false;adjustingMaterialToe=false;removingPoint=false;addingPoint=false;canvas.ShowVertices=false;canvas.LoadImage(current.Preview);canvas.IsContour=true;canvas.Surface=current.Surface;canvas.Toe=current.GridAnchorX;canvas.Origin=current.GridOriginX;UpdateComparison();canvas.Invalidate();info.Text=current.Frame.Label+" — "+current.Message+"\nRevise el borde y la punta A1. Rueda: zoom al cursor. Mano, rueda pulsada o botón derecho: desplazar.";}
   list.SelectedIndexChanged+=(s,e)=>Select();UpdateList();if(list.Items.Count>0)list.SelectedIndex=0;
   void ApplyOrigin(double origin){int count=1;if(originAll.Checked)count=ProfileGeometry.SetOriginAll(batch.Photos,origin);else ProfileGeometry.SetOrigin(current,origin);adjustingOrigin=false;UpdateList();info.Text="Inicio A ajustado en "+count+" fotos. La referencia derecha se conserva. Revise el perfil antes de validar.";}
   resetOrigin.Click+=(s,e)=>{if(current==null)return;if(editing||adjustingToe||adjustingMaterialToe){MessageBox.Show(this,"Termine el ajuste actual antes de restaurar A.");return;}try{ApplyOrigin(0);}catch(Exception ex){MessageBox.Show(this,ex.Message);}};
   redraw.Click+=(s,e)=>{if(current==null||current.Frame.Number==0||!current.RegistrationValid||canvas.ImageSize.IsEmpty)return;deletedPoints.Clear();adjustingOrigin=false;removingPoint=false;addingPoint=false;canvas.ShowVertices=false;adjustingToe=false;adjustingMaterialToe=false;canvas.BeginPicking();editing=true;edits=new List<XY>();canvas.Guide=current.Surface.Select(v=>new XY(v.X,v.Y)).ToList();canvas.Surface=edits;canvas.Invalidate();info.Text="Primer punto sobre la vertical A, a la altura del relave. Continue de izquierda a derecha. Último punto: punta del relave. Después pulse Aplicar trazo.";};
   toeButton.Click+=(s,e)=>{if(current==null||current.Frame.Number==0||!current.RegistrationValid||canvas.ImageSize.IsEmpty)return;if(editing||adjustingOrigin){MessageBox.Show(this,"Termine el ajuste antes de centrar la línea.");return;}deletedPoints.Clear();removingPoint=false;addingPoint=false;canvas.ShowVertices=false;adjustingMaterialToe=false;canvas.BeginPicking();adjustingToe=!adjustingToe;info.Text=adjustingToe?"Marque el CENTRO de la línea NEGRA de referencia de esta foto. La roja del DWG quedará centrada allí.\nPulse Centrar línea roja otra vez para cancelar.":"Ajuste de línea cancelado.";};
   uniform.Click+=(s,e)=>{if(current==null||current.Frame.Number==0||current.Surface.Count<2)return;if(editing||adjustingToe||adjustingMaterialToe||adjustingOrigin){MessageBox.Show(this,"Termine el ajuste actual antes de uniformar.");return;}deletedPoints.Clear();int previous=current.Surface.Count;current.Surface=ProfileGeometry.Smooth(current.Surface,current.Toe,out double change);current.SmoothingAreaChange=change;current.Approved=false;current.NeedsReview=true;current.Message="Perfil uniformado: "+previous+" a "+current.Surface.Count+" puntos. Área del trazo conservada. Revise y valide.";UpdateList();};
   materialToe.Click+=(s,e)=>{if(current==null||current.Frame.Number==0||!current.RegistrationValid||canvas.ImageSize.IsEmpty)return;if(editing||adjustingOrigin){MessageBox.Show(this,"Termine el ajuste antes de ajustar la punta.");return;}deletedPoints.Clear();removingPoint=false;addingPoint=false;canvas.ShowVertices=false;adjustingToe=false;canvas.BeginPicking();adjustingMaterialToe=!adjustingMaterialToe;info.Text=adjustingMaterialToe?"Marque la punta REAL del relave. La foto y su escala permanecerán fijas.\nLa línea roja indica la referencia de la cuadrícula; no se moverá con el contorno.":"Ajuste de punta cancelado.";};
   remove.Click+=(s,e)=>{if(current==null||current.Frame.Number==0||current.Surface.Count<3)return;if(editing||adjustingToe||adjustingMaterialToe||adjustingOrigin){MessageBox.Show(this,"Termine el ajuste actual antes de eliminar puntos.");return;}canvas.BeginPicking();addingPoint=false;removingPoint=!removingPoint;canvas.ShowVertices=removingPoint;canvas.Invalidate();info.Text=removingPoint?"Haga clic en un punto cian intermedio para eliminarlo. Los extremos naranjas estan protegidos.\nDeshacer punto restaura la eliminacion. El area se recalcula al generar.":"Eliminacion de puntos desactivada.";};
   add.Click+=(s,e)=>{if(current==null||current.Frame.Number==0||current.Surface.Count<2||!current.RegistrationValid||canvas.ImageSize.IsEmpty)return;if(editing||adjustingToe||adjustingMaterialToe||adjustingOrigin){MessageBox.Show(this,"Termine el ajuste actual antes de agregar puntos.");return;}canvas.BeginPicking();removingPoint=false;addingPoint=!addingPoint;canvas.ShowVertices=addingPoint;canvas.Invalidate();info.Text=addingPoint?"Haga clic sobre el borde del relave para agregar puntos entre A y la punta. Se insertan en orden de izquierda a derecha.\nDeshacer punto revierte cada cambio. Valide el perfil al terminar.":"Agregar puntos desactivado.";};
   originButton.Click+=(s,e)=>{if(current==null||!current.RegistrationValid||canvas.ImageSize.IsEmpty)return;if(editing||adjustingToe||adjustingMaterialToe){MessageBox.Show(this,"Termine el ajuste actual antes de mover A.");return;}deletedPoints.Clear();removingPoint=false;addingPoint=false;canvas.ShowVertices=false;canvas.BeginPicking();adjustingOrigin=!adjustingOrigin;info.Text=adjustingOrigin?"Marque el INICIO REAL A en el borde izquierdo del canal. La referencia derecha se conserva.\nAplicar A a todas usa ese mismo inicio en las fotos de este ensayo. Pulse Inicio A para cancelar.":"Ajuste de inicio A cancelado.";};
   canvas.Pick+=p=>{if(addingPoint){try{var before=current.Surface.Select(v=>new XY(v.X,v.Y)).ToList();ProfileGeometry.AddVertex(current,new XY(p.X,canvas.ImageSize.Height-1-p.Y));deletedPoints.Push(before);UpdateList(true);canvas.ShowVertices=true;canvas.Invalidate();info.Text=current.Message+"\nPuede agregar otro punto o pulsar Deshacer punto.";}catch(Exception ex){info.Text=ex.Message;}return;}if(adjustingOrigin){try{ApplyOrigin(p.X);}catch(Exception ex){MessageBox.Show(this,ex.Message);}return;}if(removingPoint){int index=canvas.NearestVertex(p);if(index<=0||index>=current.Surface.Count-1){info.Text="Elija un punto cian intermedio. El inicio y la punta estan protegidos.";return;}deletedPoints.Push(current.Surface.Select(v=>new XY(v.X,v.Y)).ToList());ProfileGeometry.RemoveVertex(current,index);UpdateList(true);removingPoint=true;canvas.ShowVertices=true;canvas.Invalidate();return;}if(adjustingMaterialToe){try{ProfileGeometry.SetToe(current,Math.Abs(p.X-current.GridAnchorX)<=3?current.GridAnchorX:p.X);adjustingMaterialToe=false;UpdateList();}catch(Exception ex){MessageBox.Show(this,ex.Message);}return;}if(adjustingToe){try{if(p.X<=current.GridOriginX+2)throw new InvalidOperationException("La referencia debe quedar a la derecha del origen.");current.GridAnchorX=p.X;current.ReferenceAnchorValid=true;current.AlignmentValid=true;current.AlignmentManual=true;if(current.Surface.Count>=2)ProfileGeometry.Normalize(current);current.Approved=false;current.NeedsReview=true;current.Message="Centro de la linea negra ajustado. Revise y valide.";adjustingToe=false;UpdateList();}catch(Exception ex){MessageBox.Show(this,ex.Message);}return;}if(!editing)return;if(edits.Count==0&&Math.Abs(p.X-current.GridOriginX)>Math.Max(10,canvas.ImageSize.Width*.015)){info.Text="Comience sobre la vertical A, a la altura donde empieza la superficie del relave.";return;}if(edits.Count>0&&p.X<=edits.Last().X){MessageBox.Show(this,"Los puntos deben avanzar de izquierda a derecha.");return;}edits.Add(new XY(edits.Count==0?current.GridOriginX:p.X,canvas.ImageSize.Height-1-p.Y));canvas.Surface=edits;canvas.Toe=current.GridAnchorX;canvas.Invalidate();};
   undo.Click+=(s,e)=>{if(editing&&edits.Count>0){edits.RemoveAt(edits.Count-1);canvas.Invalidate();return;}if(current!=null&&deletedPoints.Count>0){current.Surface=deletedPoints.Pop();current.Approved=false;current.NeedsReview=true;current.Message="Cambio de punto deshecho. Revise y valide el perfil.";UpdateList(true);canvas.ShowVertices=removingPoint||addingPoint;canvas.Invalidate();}};
   apply.Click+=(s,e)=>{if(current==null||!current.RegistrationValid||!editing||edits.Count<2){MessageBox.Show(this,"Marque al menos dos puntos de la superficie.");return;}current.Surface=edits;current.Toe=edits.Last().X;ProfileGeometry.Normalize(current);current.Approved=true;current.NeedsReview=false;current.Message="Contorno corregido y revisado.";editing=false;UpdateList();};
   approve.Click+=(s,e)=>{if(current==null)return;if(editing||adjustingToe||adjustingMaterialToe||adjustingOrigin){MessageBox.Show(this,"Termine el ajuste actual antes de validar.");return;}if(current.Frame.Number>0&&(current.Surface.Count<2||current.Toe<=0)){MessageBox.Show(this,"Dibuje primero el contorno de esta foto.");return;}if(!ReviewActions.CanApprove(current)){MessageBox.Show(this,"Falta revisar el contorno, el encuadre o el centro de la linea negra.");return;}current.Approved=true;current.NeedsReview=false;current.Message="Contorno revisado.";UpdateList();};
   approveAll.Click+=(s,e)=>{
    if(editing||adjustingToe||adjustingMaterialToe||adjustingOrigin){MessageBox.Show(this,"Termine el ajuste actual antes de validar todas.");return;}
    int accepted=ReviewActions.ApproveAll(batch.Photos);UpdateList();
    var pending=batch.Photos.Where(p=>p.Frame.Number>0&&!p.Approved).ToArray();
    info.Text=accepted+" fotos aceptadas con Validar todas. "+(pending.Length==0?"Puede generar el DWG y el Excel.":"Siguen pendientes por encuadre o contorno: "+string.Join(", ",pending.Select(p=>p.Frame.Label)))+"\nEsta opción acepta los contornos actuales; no corrige su trazado.";
   };
   save.Click+=(s,e)=>{if(editing||adjustingToe||adjustingMaterialToe||adjustingOrigin){MessageBox.Show(this,"Termine el ajuste actual antes de generar.");return;}var pending=batch.Photos.Where(p=>p.Frame.Number>0&&(!ReviewActions.CanApprove(p)||!p.Approved)).ToArray();if(pending.Length>0){MessageBox.Show(this,"Quedan fotos por corregir o validar: "+string.Join(", ",pending.Select(p=>p.Frame.Label)));return;}DialogResult=DialogResult.OK;Close();};CancelButton=cancel;
  }
 }
}
