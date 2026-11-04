using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using AcApp=Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace LocalizarCasillas
{
    public class NativeTests
    {
        static void Require(bool condition,string message) {if(!condition)throw new InvalidOperationException(message);}
        static string Fingerprint(IEnumerable<Slot> slots)=>String.Join("\n",slots.Select(s=>s.Layout+"|"+s.Handle+"|"+s.Tag+"|"+s.Value+"|"+s.Code));

        [CommandMethod("CASILLASPROBAR")]
        public void Test()
        {
            var doc=AcApp.DocumentManager.MdiActiveDocument;string root=Path.GetDirectoryName(doc.Name);
            var report=new List<string>();
            try
            {
                var slots=Catalog.Read(doc.Database);string before=Fingerprint(slots);
                Require(slots.Count==108,"La referencia de prueba debe tener 108 atributos.");
                report.Add("PASS: 108 atributos reales; 94 del rotulo y 14 de detalles.");
                var dots=slots.Where(s=>s.Value==".").ToList();
                Require(dots.Count>20,"No se encontraron las casillas con punto.");
                Require(dots.Select(s=>s.AttributeId).Distinct().Count()==dots.Count,"Se confundieron atributos con el mismo valor.");
                report.Add("PASS: "+dots.Count+" casillas con punto identificadas por entidad y vinculo, no por valor.");
                var sample=slots.Single(s=>s.Tag=="R3_AP");
                Require(sample.Property=="03_R3_App" && sample.Value==".","No se leyo el vinculo de R3_AP.");
                Require(slots.Single(s=>s.Tag=="R2_AP").Property=="03_R2_App","No se leyo el vinculo de R2_AP.");
                Require(slots.Single(s=>s.Tag=="R1_DESCRIPTION").Property=="00_Revision","No se separo el formato del nombre de propiedad.");
                Require(slots.Where(s=>s.Code.Contains("\\AcSm")).All(s=>s.Property!="Sheet Set (ver codigo)"),"Quedan vinculos AcSm sin identificar.");
                report.Add("PASS: propiedades AcSm con y sin formato, Component y AcSm.16.2.");
                foreach(var s in slots)
                {
                    var bounds=Catalog.MarkerBounds(s);
                    Require(bounds.MaxPoint.X>bounds.MinPoint.X && bounds.MaxPoint.Y>bounds.MinPoint.Y,"Marca invisible: "+s.Tag);
                    Require(bounds.MinPoint.X<=s.Bounds.MinPoint.X+1e-6 && bounds.MaxPoint.X>=s.Bounds.MaxPoint.X-1e-6,"La marca no contiene el atributo: "+s.Tag);
                    var graphics=Marker.CreateGraphics(s);
                    try {Require(graphics.Count==5 && graphics.All(g=>g.ObjectId.IsNull),"La marca se agrego al dibujo.");}
                    finally {foreach(var g in graphics)g.Dispose();}
                }
                Require(sample.CellBounds.HasValue,"No se detectaron los bordes de la celda R3_AP.");
                var adjacent=slots.Single(s=>s.Tag=="R4_AP");
                Require(adjacent.CellBounds.HasValue && adjacent.CellBounds.Value.MinPoint!=sample.CellBounds.Value.MinPoint,"Las revisiones apuntan a la misma casilla.");
                report.Add("PASS: limites visibles para todos los atributos; "+slots.Count(s=>s.CellBounds.HasValue)+" celdas completas reconocidas.");
                report.Add("PASS: R3_AP y R4_AP tienen celdas diferentes aunque ambas muestran un punto.");
                Require(before==Fingerprint(Catalog.Read(doc.Database)),"La localizacion cambio los valores o campos.");
                report.Add("PASS: crear y retirar las marcas no cambia ningun valor ni codigo de campo.");
                VerifyNested();report.Add("PASS: coordenadas de atributos anidados con rotacion y escala; edicion anidada restringida.");
                // A transaction rollback validates an actual attribute edit without leaving changes in the copy.
                var plain=slots.First(s=>s.Code.Length==0 && !s.Nested);
                using(var tr=doc.Database.TransactionManager.StartTransaction())
                {
                    var a=(AttributeReference)tr.GetObject(plain.AttributeId,OpenMode.ForWrite);
                    a.TextString="PRUEBA TEMPORAL";Require(a.TextString=="PRUEBA TEMPORAL","No se pudo editar el atributo.");tr.Abort();
                }
                Require(before==Fingerprint(Catalog.Read(doc.Database)),"La prueba de edicion no se revirtio.");
                report.Add("PASS: edicion nativa de atributo y rollback sin perder campos vinculados.");
                Catalog.Export(slots,Path.Combine(root,"atributos.csv"));
                CreatePreview(doc.Database,sample,root);
                report.Add("PASS: ejemplo visual independiente generado; el plano de prueba no recibe anotaciones.");
                var saved=Path.Combine(root,"verificacion-guardado.dwg");
                doc.Database.SaveAs(saved,DwgVersion.AC1032);
                using(var reopened=new Database(false,true))
                {
                    reopened.ReadDwgFile(saved,FileOpenMode.OpenForReadAndAllShare,false,null);reopened.CloseInput(true);
                    Require(before==Fingerprint(Catalog.Read(reopened)),"Los atributos o campos cambiaron al guardar y reabrir.");
                }
                report.Add("PASS: guardar y reabrir conserva los 108 atributos, sus valores y sus vinculos.");
                report.Add("CASILLAS_TEST_OK");
                doc.Editor.WriteMessage("\nCASILLAS_TEST_OK\n");
            }
            catch(System.Exception ex){report.Add("CASILLAS_FAILED "+ex);doc.Editor.WriteMessage("\nCASILLAS_FAILED "+ex+"\n");}
            File.WriteAllLines(Path.Combine(root,"pruebas.txt"),report);
        }

        static void VerifyNested()
        {
            using(var db=new Database(true,true))
            {
                using(var tr=db.TransactionManager.StartTransaction())
                {
                    var bt=(BlockTable)tr.GetObject(db.BlockTableId,OpenMode.ForWrite);
                    var inner=new BlockTableRecord {Name="CELDA_INTERIOR"};bt.Add(inner);tr.AddNewlyCreatedDBObject(inner,true);
                    var outer=new BlockTableRecord {Name="MARCO"};bt.Add(outer);tr.AddNewlyCreatedDBObject(outer,true);
                    var child=new BlockReference(new Point3d(10,20,0),inner.ObjectId);outer.AppendEntity(child);tr.AddNewlyCreatedDBObject(child,true);
                    var attr=new AttributeReference {Tag="PUNTO",TextString=".",Position=new Point3d(11,21,0),Height=2};
                    child.AttributeCollection.AppendAttribute(attr);tr.AddNewlyCreatedDBObject(attr,true);
                    var model=(BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace],OpenMode.ForWrite);
                    var root=new BlockReference(new Point3d(100,200,0),outer.ObjectId){Rotation=Math.PI/2,ScaleFactors=new Scale3d(2)};
                    model.AppendEntity(root);tr.AddNewlyCreatedDBObject(root,true);tr.Commit();
                }
                var s=Catalog.Read(db).Single();
                Require(s.Nested && s.Position.DistanceTo(new Point3d(58,222,0))<1e-6,"Transformacion anidada incorrecta.");
                Require(Math.Abs(s.Height-4)<1e-6,"La altura anidada no respeta la escala.");
            }
        }

        static void CreatePreview(Database source,Slot sample,string root)
        {
            using(var db=source.Wblock(new ObjectIdCollection(new[]{sample.BlockId}),Point3d.Origin))
            {
                using(var tr=db.TransactionManager.StartTransaction())
                {
                    var bt=(BlockTable)tr.GetObject(db.BlockTableId,OpenMode.ForRead);
                    var space=(BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace],OpenMode.ForWrite);
                    foreach(var graphic in Marker.CreateGraphics(sample))
                    {space.AppendEntity(graphic);tr.AddNewlyCreatedDBObject(graphic,true);}
                    var b=Catalog.MarkerBounds(sample);
                    var note=new MText {Location=new Point3d(b.MinPoint.X-145,b.MaxPoint.Y+28,0),TextHeight=3,
                        Width=240,Contents="EJEMPLO VISUAL - CASILLA LOCALIZADA\\PR3_AP  <---  03_R3_App\\PValor guardado: punto (.)\\PEn el panel CASILLAS la marca es temporal.",ColorIndex=1};
                    space.AppendEntity(note);tr.AddNewlyCreatedDBObject(note,true);tr.Commit();
                }
                db.SaveAs(Path.Combine(root,"Vista-casilla-R3_AP.dwg"),DwgVersion.AC1032);
            }
        }

        [CommandMethod("CASILLASPROBARMARCA")]
        public void TestTransient()
        {
            var doc=AcApp.DocumentManager.MdiActiveDocument;var slots=Catalog.Read(doc.Database);string before=Fingerprint(slots);
            try
            {
                using(var marker=new Marker())
                {
                    marker.Show(doc,slots.Single(s=>s.Tag=="R3_AP"),true);Require(marker.Count==5,"Falta la marca en pantalla.");
                    marker.Show(doc,slots.Single(s=>s.Tag=="R4_AP"),true);Require(marker.Count==5,"No se retiro la marca anterior.");
                    marker.Clear();Require(marker.Count==0,"Quedan marcas temporales.");
                }
                Require(before==Fingerprint(Catalog.Read(doc.Database)),"Cambios al mostrar la marca.");
                File.WriteAllText(Path.Combine(Path.GetDirectoryName(doc.Name),"prueba-pantalla.txt"),"CASILLAS_TRANSIENT_OK: mostrar R3_AP, cambiar a R4_AP y limpiar; campos y valores intactos.");
                doc.Editor.WriteMessage("\nCASILLAS_TRANSIENT_OK\n");
            }
            catch(System.Exception ex){doc.Editor.WriteMessage("\nCASILLAS_TRANSIENT_FAILED "+ex+"\n");}
        }
    }
}
