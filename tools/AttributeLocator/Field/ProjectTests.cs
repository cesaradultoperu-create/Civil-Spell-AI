using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using LocalizarCasillas;
using AcApp=Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace CasillasField
{
    public class ProjectTests
    {
        [CommandMethod("CASILLASPILOTOVISTA")]
        public void OpenPilot()
        {
            var doc=AcApp.DocumentManager.MdiActiveDocument;string folder=Path.GetDirectoryName(doc.Name);
            if(Path.GetFileName(doc.Name)!="Casillas-piloto.dwg" || !File.Exists(Path.Combine(folder,"PRUEBA-LOCAL.txt")))
                throw new InvalidOperationException("Solo se abre la copia local de prueba.");
            ProjectAccess.TestConfiguration=Path.Combine(folder,"projects-test.xml");
            AutoStart.Resume(doc);FieldCommands.ActivateAutomatic(doc);FieldCommands.OpenDemoAttribute(doc);
        }
        static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
        static string Snapshot(IEnumerable<Slot> slots)=>String.Join("\n",slots.Select(s=>s.Handle+"|"+s.Value+"|"+s.Code));
        [CommandMethod("CASILLASPROYECTOPROBAR")]
        public void Test()
        {
            var doc=AcApp.DocumentManager.MdiActiveDocument;
            string folder=Path.GetDirectoryName(doc.Name),dst=Path.Combine(folder,"SOTRAMI-piloto.dst"),config=Path.Combine(folder,"projects-test.xml");
            var report=new List<string>();
            try
            {
                Require(Path.GetFileName(doc.Name)=="Casillas-piloto.dwg" && File.Exists(Path.Combine(folder,"PRUEBA-LOCAL.txt")),"Solo se prueba la copia local marcada, nunca un plano del proyecto.");
                Require(Path.GetPathRoot(folder)!="Q:\\" && File.Exists(dst),"Se requiere una copia local del DST.");
                var slots=Catalog.Read(doc.Database);var before=Snapshot(slots);
                var source=FieldIdentity.Parse(slots.First(s=>s.Tag=="R3_BY").Code);
                Require(source!=null && source.SheetSet.Equals("g931493F8-1627-4510-9D4D-EE92A2AEC256",StringComparison.OrdinalIgnoreCase),"El plano no corresponde al conjunto SOTRAMI esperado.");
                report.Add("Origen verificado: "+source.DatabasePath+" / "+source.SheetSet);
                var sourceCodes=slots.ToDictionary(s=>s.Handle,s=>s.Code);
                var sourceValues=slots.ToDictionary(s=>s.Handle,s=>s.Value);
                var real=new ProjectAccess {Root=@"Q:\Proyectos 2026\1107.001.00.16 - Pad SOTRAMI\05_Drawing",DatabasePath=source.DatabasePath,SheetSet=source.SheetSet,Block="MEMB_WWL_P_A1_",EditValues=true};
                Require(real.Matches(source),"El perfil real no reconoce su propio conjunto.");
                Require(!real.Matches(new FieldIdentity {DatabasePath=source.DatabasePath+".otro",SheetSet=source.SheetSet,Component="",Property=source.Property}),"Se aceptó otro DST.");
                Require(!real.Matches(new FieldIdentity {DatabasePath=source.DatabasePath,SheetSet="gOTRO",Component="",Property=source.Property}),"Se aceptó otro GUID.");
                Require(!real.Matches(new FieldIdentity {DatabasePath=source.DatabasePath,SheetSet=source.SheetSet,Component="gHOJA",Property=source.Property}),"Se habilitó una propiedad de hoja como si fuera del conjunto.");
                Require(ProjectAccess.Within(Path.Combine(real.Root,"Plano.dwg"),real.Root) && !ProjectAccess.Within(real.Root+"-Otro\\Plano.dwg",real.Root),"Límite de carpeta incorrecto.");
                Require(!ProjectAccess.Within(Path.Combine(real.Root,"..","Otro","Plano.dwg"),real.Root),"Se permitió salir de la carpeta por ..");
                Require(before==Snapshot(Catalog.Read(doc.Database)),"La inspección cambió el plano.");
                report.Add("PASS: identidad completa, límites de carpeta, otros DST y propiedades de hoja protegidos.");
                var xml=new XmlDocument();var projects=xml.CreateElement("Projects");xml.AppendChild(projects);var project=xml.CreateElement("Project");projects.AppendChild(project);
                foreach(var pair in new[]{new[]{"name","SOTRAMI - piloto local"},new[]{"root",folder},new[]{"dst",dst},new[]{"sheetSet",source.SheetSet},new[]{"block",real.Block},new[]{"editValues","true"}})project.SetAttribute(pair[0],pair[1]);
                xml.Save(config);ProjectAccess.TestConfiguration=config;
                Require(AutoStart.CanEnable(doc.Name),"La activación no reconoce el proyecto registrado.");
                Require(!AutoStart.CanEnable(Path.Combine(folder+"-Otro","Plano.dwg")),"La ayuda se activa fuera del proyecto.");
                // Redirect only this disposable DWG. No PrepareDemo and no shared DST writes.
                using(var tr=doc.Database.TransactionManager.StartTransaction())
                {
                    foreach(var slot in slots)
                    {
                        var id=FieldIdentity.Parse(slot.Code);
                        // The copied DST keeps its sheet GUIDs. Redirect every Field of that
                        // source in the disposable DWG, including sheet properties. Otherwise
                        // AutoCAD can resolve the duplicate GUID to the copy during regeneration.
                        if(id==null || !String.Equals(id.DatabasePath,source.DatabasePath,StringComparison.OrdinalIgnoreCase) ||
                            !String.Equals(id.SheetSet,source.SheetSet,StringComparison.OrdinalIgnoreCase))continue;
                        var attribute=(AttributeReference)tr.GetObject(slot.AttributeId,OpenMode.ForWrite);
                        var field=(Field)tr.GetObject(attribute.GetField(),OpenMode.ForWrite);
                        field.SetFieldCode(slot.Code.Replace(source.DatabasePath,dst));
                    }
                    tr.Commit();
                }
                slots=Catalog.Read(doc.Database);var codes=slots.ToDictionary(s=>s.Handle,s=>s.Code);
                report.Add("Layouts: "+String.Join(", ",slots.GroupBy(s=>s.Layout).Select(g=>g.Key+"="+g.Count())));
                var cell=slots.First(s=>s.Tag=="R3_BY" && s.Layout=="A 1");var identity=FieldIdentity.Parse(cell.Code);
                Require(SheetSetStore.CanEdit(doc.Name,identity),"No se habilita la propiedad personalizada local.");
                Require(!SheetSetStore.CanEdit(doc.Name,source),"La copia local permite escribir el DST compartido.");
                string original=SheetSetStore.Read(doc.Name,identity);var uses=FieldIdentity.Uses(slots,identity);
                try
                {
                    SheetSetStore.Write(doc.Name,identity,original,"TEST LOCAL");
                    Require(SheetSetStore.Read(doc.Name,identity)=="TEST LOCAL","El cambio no persistió en el DST local.");
                    bool stale=false;try{SheetSetStore.Write(doc.Name,identity,original,"OBSOLETO");}catch(InvalidOperationException){stale=true;}
                    Require(stale,"No se rechazó la edición concurrente.");
                    SheetSetStore.Evaluate(doc.Database);doc.Editor.Regen();
                    var updated=Catalog.Read(doc.Database);
                    report.Add("Valores evaluados: "+String.Join(", ",FieldIdentity.Uses(updated,identity).Select(s=>s.Tag+" ["+s.Layout+"]="+s.Value)));
                    Require(FieldIdentity.Uses(updated,identity).All(s=>s.Value=="TEST LOCAL"),"No se actualizaron todos los usos.");
                    foreach(var changed in updated.Where(s=>s.Code!=codes[s.Handle]))report.Add("FIELD_DIFF "+changed.Tag+" ["+changed.Layout+"]\nANTES="+codes[changed.Handle]+"\nDESPUÉS="+changed.Code);
                    Require(updated.All(s=>s.Code==codes[s.Handle]),"Se alteró un código de Field al editar el valor.");
                    report.Add("PASS: edición central en DST copiado, "+uses.Length+" usos actualizados, Fields conservados y valor obsoleto rechazado.");
                }
                finally
                {
                    SheetSetStore.Write(doc.Name,identity,SheetSetStore.Read(doc.Name,identity),original);
                    SheetSetStore.Evaluate(doc.Database);doc.Editor.Regen();
                }
                var root=slots.Where(s=>s.Block==real.Block && s.Layout=="A 1").Select(s=>s.BlockId).Distinct().Single();
                var cells=Catalog.Read(doc.Database).Where(s=>s.BlockId==root).ToArray();
                report.Add("Casillas detectadas: "+cells.Length+"; delimitadas: "+cells.Count(s=>s.CellBounds.HasValue));
                Require(cells.Length>80 && cells.Where(s=>s.Tag.StartsWith("R3_")).All(s=>s.CellBounds.HasValue),"Faltan casillas de revisión o sus posiciones.");
                Require(cells.Where(s=>s.Tag=="R3_BY").All(s=>CellStatus.Column(s)=="1 · DIB"),"Columna de dibujante incorrecta.");
                var scene=Scene.Read(doc.Database,root,cells);Require(scene.Edges.Count>30,"Falta geometría del membrete.");
                var all=Catalog.Read(doc.Database);var scenes=all.GroupBy(s=>s.BlockId).ToDictionary(g=>g.Key,g=>Scene.Read(doc.Database,g.Key,g));
                using(var map=new CellMap())
                {
                    map.SetCatalog(cells,scenes);var other=all.First(s=>s.Tag=="R3_AP" && s.Layout=="A");
                    map.FocusSlot(other);Require(map.CurrentSlot.AttributeId==other.AttributeId,"El mapa no sigue el uso de otra presentación.");
                }
                report.Add("PASS: al elegir un uso de la otra presentación, el mapa cambia al atributo exacto de esa presentación.");
                report.Add("PASS: "+cells.Length+" casillas del membrete, "+cells.Count(CellStatus.WithoutData)+" sin dato; posiciones y geometría reconocidas.");
                Require(Catalog.Read(doc.Database).All(s=>s.Code==codes[s.Handle]),"Cambios de Field inesperados.");
                var point=Path.Combine(ProjectAccess.LocalFolder,"Backups",DateTime.Now.ToString("yyyy-MM-dd"));
                Require(Directory.GetFiles(point,"*.xml").Any(file=>File.ReadAllText(file).Contains("TEST LOCAL")),"Falta respaldo y registro de edición.");
                report.Add("PASS: respaldo local y registro de valor anterior/nuevo creados.");
                report.Add("CASILLAS_PROJECT_TEST_OK");
            }
            catch(System.Exception ex){report.Add("CASILLAS_PROJECT_FAILED "+ex);}
            finally{ProjectAccess.TestConfiguration=null;}
            File.WriteAllLines(Path.Combine(folder,"pruebas-proyecto.txt"),report);
            doc.Editor.WriteMessage("\n"+String.Join("\n",report)+"\n");
        }
    }
}
