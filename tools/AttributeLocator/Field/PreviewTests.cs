using System;
using System.Drawing;
using System.IO;
using System.Linq;
using Autodesk.AutoCAD.Runtime;
using LocalizarCasillas;
using AcApp=Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace CasillasField
{
    public class PreviewTests
    {
        static void Require(bool ok,string text){if(!ok)throw new InvalidOperationException(text);}
        static string Snapshot(System.Collections.Generic.IEnumerable<Slot> slots)=>String.Join("\n",slots.Select(s=>s.Handle+"|"+s.Value+"|"+s.Code));
        static void Render(string folder,string name,Scene scene,Slot slot,Slot[] uses,int width,int height)
        {
            using(var preview=new CellPreview {Size=new Size(width,height)})
            using(var bitmap=new Bitmap(width,height))
            {
                preview.Update(scene,slot,Color.RoyalBlue,uses);preview.CreateControl();
                preview.DrawToBitmap(bitmap,new Rectangle(0,0,width,height));bitmap.Save(Path.Combine(folder,name+".png"));
            }
        }
        [CommandMethod("CASILLASVISTAPROBAR")]
        public void Test()
        {
            var doc=AcApp.DocumentManager.MdiActiveDocument;var report=new System.Collections.Generic.List<string>();
            string folder=Path.Combine(Path.GetDirectoryName(doc.Name),"pruebas-vistas");Directory.CreateDirectory(folder);
            try
            {
                var slots=Catalog.Read(doc.Database);string before=Snapshot(slots);
                var first=slots.Single(s=>s.Tag=="R2_NO");var second=slots.Single(s=>s.Tag=="REV");var target=slots.Single(s=>s.Tag=="R3_BY");
                var scene=Scene.Read(doc.Database,first.BlockId,slots.Where(s=>s.BlockId==first.BlockId));
                var vertical=scene.Texts.First(t=>t.Text.Contains("REVISIONES"));
                Require(Math.Abs(Math.Sin(vertical.Rotation))>0.9 && vertical.Width>vertical.Height*3,"El texto REVISIONES no conserva su orientacion vertical.");
                Require(vertical.Bounds.MaxPoint.Y-vertical.Bounds.MinPoint.Y>vertical.Bounds.MaxPoint.X-vertical.Bounds.MinPoint.X,"La caja del texto vertical se dibujo horizontalmente.");
                report.Add("PASS: REVISIONES conserva giro, caja y proporcion real; MTEXT se distribuye en lineas nativas.");
                var focus=CellPreview.FocusBounds(first);var other=CellPreview.FocusBounds(second);
                Require(Math.Abs((focus.MaxPoint.X+focus.MinPoint.X)-(other.MaxPoint.X+other.MinPoint.X))/2>(focus.MaxPoint.X-focus.MinPoint.X)*2,"Las dos casillas de prueba no estan separadas.");
                Require(CellStatus.Row(second)=="Revisión del plano" && CellStatus.Row(slots.Single(s=>s.Tag=="02_DRAWN"))=="Firmas y responsables","No se identifican las zonas del membrete.");
                report.Add("PASS: las coincidencias R2_NO y REV se encuadran individualmente y distinguen revisiones de revision del plano.");
                var uses=new[]{first,second};
                Render(folder,"01-revision-fila",scene,first,uses,500,280);
                Render(folder,"02-revision-plano",scene,second,uses,500,280);
                Render(folder,"03-casilla-punto",scene,target,new[]{target},500,280);
                Render(folder,"04-vista-ampliada",scene,first,uses,1050,600);
                var emptyScene=new Scene();
                var emptySlot=new Slot {Tag="VACIA",Value="",Height=2,Bounds=new Autodesk.AutoCAD.DatabaseServices.Extents3d(Autodesk.AutoCAD.Geometry.Point3d.Origin,new Autodesk.AutoCAD.Geometry.Point3d(1.3,2,0))};
                using(var text=new Autodesk.AutoCAD.DatabaseServices.DBText())
                using(var tr=doc.Database.TransactionManager.StartTransaction())
                {
                    text.SetDatabaseDefaults(doc.Database);text.Height=2;text.TextString="";
                    Scene.AddText(text,Autodesk.AutoCAD.Geometry.Matrix3d.Identity,emptyScene,tr,emptySlot);tr.Commit();
                }
                emptySlot.Value="DATO";Render(folder,"06-editar-casilla-vacia",emptyScene,emptySlot,new[]{emptySlot},500,280);
                using(var bitmap=new Bitmap(Path.Combine(folder,"06-editar-casilla-vacia.png")))
                {
                    int ink=0;for(int y=27;y<240;y++)for(int x=1;x<499;x++){var pixel=bitmap.GetPixel(x,y);if(pixel.R<100 && pixel.G<100 && pixel.B<100)ink++;}
                    Require(ink>30,"No se ve el texto escrito en una casilla originalmente vacia.");
                }
                report.Add("PASS: el texto nuevo de una casilla inicialmente vacia es visible en la vista previa.");
                using(var form=new GuideForm())
                using(var bitmap=new Bitmap(form.Width,form.Height))
                {
                    var scenes=new System.Collections.Generic.Dictionary<Autodesk.AutoCAD.DatabaseServices.ObjectId,Scene>{{first.BlockId,scene}};
                    form.SetCatalog(slots.Where(s=>s.BlockId==first.BlockId),scenes);
                    form.UpdateInfo(target,first.Value,FieldIdentity.Parse(first.Code),uses,scenes);
                    form.StartPosition=System.Windows.Forms.FormStartPosition.Manual;form.Location=new Point(-30000,-30000);form.Show();form.PerformLayout();
                    Require(form.Controls[0].Created,"No se crearon los controles del panel para la captura.");
                    form.DrawToBitmap(bitmap,new Rectangle(0,0,form.Width,form.Height));bitmap.Save(Path.Combine(folder,"05-panel.png"));
                    form.Hide();
                }
                Require(before==Snapshot(Catalog.Read(doc.Database)),"La vista previa modifico valores o Fields del dibujo.");
                report.Add("PASS: imagenes generadas desde geometria y contornos de letras, sin modificar atributos ni Fields.");
                report.Add("CASILLAS_PREVIEW_TEST_OK");
            }
            catch(System.Exception ex){report.Add("CASILLAS_PREVIEW_FAILED "+ex);}
            File.WriteAllLines(Path.Combine(folder,"pruebas.txt"),report);doc.Editor.WriteMessage("\n"+String.Join("\n",report)+"\n");
        }
    }
}
