using System;
using System.IO;
using System.Linq;
using Autodesk.AutoCAD.Runtime;
using LocalizarCasillas;
using AcApp=Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace CasillasField
{
    public class FieldTests
    {
        [CommandMethod("CASILLASEDICIONPROBAR")]
        public void EditTest()
        {
            var doc=AcApp.DocumentManager.MdiActiveDocument;var report=new System.Collections.Generic.List<string>();
            try
            {
                var slots=Catalog.Read(doc.Database);var cell=slots.Single(s=>s.Tag=="R4_BY");var identity=FieldIdentity.Parse(cell.Code);
                Require(SheetSetStore.IsDemo(doc.Name,identity),"La prueba requiere DST local aislado.");
                string original=SheetSetStore.Read(doc.Name,identity);var codes=slots.ToDictionary(s=>s.Handle,s=>s.Code);
                try
                {
                    SheetSetStore.Write(doc.Name,identity,original,"PRUEBA");
                    Require(SheetSetStore.Read(doc.Name,identity)=="PRUEBA","No persistio el cambio central.");
                    bool staleRejected=false;try{SheetSetStore.Write(doc.Name,identity,original,"INCORRECTO");}catch(InvalidOperationException){staleRejected=true;}
                    Require(staleRejected,"No se rechazo una escritura con valor antiguo.");
                    SheetSetStore.Evaluate(doc.Database);
                    doc.Editor.Regen();
                    using(var tr=doc.Database.TransactionManager.StartTransaction())
                    {
                        var a=(Autodesk.AutoCAD.DatabaseServices.AttributeReference)tr.GetObject(cell.AttributeId,Autodesk.AutoCAD.DatabaseServices.OpenMode.ForRead);
                        var f=(Autodesk.AutoCAD.DatabaseServices.Field)tr.GetObject(a.GetField(),Autodesk.AutoCAD.DatabaseServices.OpenMode.ForRead);
                        report.Add("EVAL: "+f.EvaluationStatus.Status+" / "+f.EvaluationStatus.ErrorMessage+" / "+f.GetStringValue()+" / "+f.GetFieldCode());
                        tr.Commit();
                    }
                    var actual=Catalog.Read(doc.Database);var uses=FieldIdentity.Uses(actual,identity);
                    Require(uses.Length==4 && uses.All(s=>s.Value=="PRUEBA"),"El valor no llego a las cuatro celdas vinculadas: "+String.Join(",",uses.Select(s=>s.Tag+"="+s.Value)));
                    Require(actual.All(s=>s.Code==codes[s.Handle]),"La edicion sustituyo o modifico un Field.");
                    report.Add("PASS: valor central guardado y cuatro celdas actualizadas conservando sus Fields.");
                    report.Add("PASS: escritura obsoleta rechazada; DST original fuera del alcance de edicion.");
                    Require(!SheetSetStore.IsDemo(doc.Name,FieldIdentity.Parse(cell.Code.Replace("Casillas-prueba.dst","Proyecto.dst"))),"Se acepto otro DST.");
                }
                finally
                {
                    SheetSetStore.Write(doc.Name,identity,SheetSetStore.Read(doc.Name,identity),original);
                    SheetSetStore.Evaluate(doc.Database);
                    doc.Editor.Regen();
                }
                Require(Catalog.Read(doc.Database).All(s=>s.Code==codes[s.Handle]),"Cambios en codigos despues de restaurar.");
                var plain=slots.Single(s=>s.Tag=="04_R3_NO");
                var edits=new System.Collections.Generic.Dictionary<Autodesk.AutoCAD.DatabaseServices.ObjectId,Tuple<string,string>>();
                edits[plain.AttributeId]=Tuple.Create(plain.Value,"PLANO PRUEBA");
                try
                {
                    TextEdits.Apply(doc.Database,edits);
                    Require(Catalog.Read(doc.Database).Single(s=>s.AttributeId==plain.AttributeId).Value=="PLANO PRUEBA","No se edito la celda sin Field.");
                    bool rejected=false;try{TextEdits.Apply(doc.Database,edits);}catch(InvalidOperationException){rejected=true;}
                    Require(rejected,"Se sobrescribio un valor cambiado.");
                    edits.Clear();edits[cell.AttributeId]=Tuple.Create(cell.Value,"ROMPER FIELD");
                    rejected=false;try{TextEdits.Apply(doc.Database,edits);}catch(InvalidOperationException){rejected=true;}
                    Require(rejected,"Se permitio reemplazar un Field por texto.");
                }
                finally
                {
                    edits.Clear();edits[plain.AttributeId]=Tuple.Create("PLANO PRUEBA",plain.Value);TextEdits.Apply(doc.Database,edits);
                }
                report.Add("PASS: celda sin Field editada y restaurada; cambios concurrentes y sustitucion de Fields rechazados.");
                report.Add("PASS: prueba restaurada al punto original.");report.Add("CASILLAS_EDIT_TEST_OK");
            }
            catch(System.Exception ex){report.Add("CASILLAS_EDIT_FAILED "+ex);}
            File.WriteAllLines(Path.Combine(Path.GetDirectoryName(doc.Name),"pruebas-edicion.txt"),report);
            doc.Editor.WriteMessage("\n"+String.Join("\n",report)+"\n");
        }
        static void Require(bool condition,string text){if(!condition)throw new InvalidOperationException(text);}
        static string Snapshot(System.Collections.Generic.IEnumerable<Slot> slots)=>String.Join("\n",slots.Select(s=>s.Handle+"|"+s.Value+"|"+s.Code));
        [CommandMethod("CASILLASFIELDPROBAR")]
        public void Test()
        {
            var doc=AcApp.DocumentManager.MdiActiveDocument;var report=new System.Collections.Generic.List<string>();
            try
            {
                var slots=Catalog.Read(doc.Database);string before=Snapshot(slots);
                Require(!AutoStart.CanEnable("C:/Proyecto/Plano-real.dwg"),"La ayuda automatica se activo fuera del ejemplo.");
                if(Path.GetFileName(doc.Name)=="Ejemplo-Field-v3.dwg" && File.Exists(SheetSetStore.DemoPath(doc.Name)+".demo"))
                    Require(AutoStart.CanEnable(doc.Name),"El ejemplo preparado no se reconoce para activacion automatica.");
                report.Add("PASS: activacion automatica limitada al nombre y manifiesto del ejemplo local.");
                Require(slots.Count==108,"Cantidad de atributos de referencia incorrecta.");
                var target=slots.Single(s=>s.Tag=="R3_CH");var identity=FieldIdentity.Parse(target.Code);
                Require(identity!=null && identity.Property=="03_R3_CHkd","No se identifica el Field guardado.");
                var uses=FieldIdentity.Uses(slots,identity);
                Require(uses.Length==1 && uses[0].Tag=="R3_CH","El punto produjo una coincidencia incorrecta.");
                var other=slots.Single(s=>s.Tag=="R3_AP");
                Require(FieldIdentity.Uses(slots,FieldIdentity.Parse(other.Code)).Single().Tag=="R3_AP","No se distingue R3_AP de R3_CH.");
                report.Add("PASS: 54 puntos repetidos distinguidos por identidad real del Field; destino y uso son independientes.");
                var shared=FieldIdentity.Uses(slots,FieldIdentity.Parse(slots.Single(s=>s.Tag=="R4_AP").Code));
                Require(String.Join(",",shared.Select(s=>s.Tag).OrderBy(s=>s))=="R4_AP,R4_BY,R5_BY,R8_AP","No se reconocen los cuatro usos existentes de 03_R4_App.");
                report.Add("PASS: 03_R4_App se reconoce en cuatro celdas diferentes: R4_AP, R4_BY, R5_BY y R8_AP.");
                Require(FieldGuide.ResolveTarget(slots,target.BlockId,target.Tag,target.Block,target.Layout)==target,"No se identifica el destino unico.");
                var differentRoot=slots.First(s=>s.BlockId!=target.BlockId).BlockId;
                var duplicate=new Slot {BlockId=differentRoot,Tag=target.Tag,Block=target.Block,Layout=target.Layout};
                Require(FieldGuide.ResolveTarget(slots.Concat(new[]{duplicate}),target.BlockId,target.Tag,target.Block,target.Layout)==null,"Una insercion ambigua se marco como si fuera el destino.");
                report.Add("PASS: dos inserciones con el mismo TAG y nombre no producen una marca de destino incorrecta.");
                var altered=FieldIdentity.Parse(target.Code.Replace(identity.DatabasePath,identity.DatabasePath+".otro"));
                Require(!identity.SameProperty(altered),"Se confundieron DST diferentes.");
                var fake=FieldIdentity.Parse(target.Code.Replace(".03_R3_CHkd", ".Component(\"gOTRA_HOJA\").03_R3_CHkd"));
                Require(!identity.SameProperty(fake),"Se confundio una propiedad de otra hoja.");
                var equivalent=FieldIdentity.Parse(target.Code.Replace(".03_R3_CHkd", ".Component(\""+identity.SheetSet+"\").03_R3_CHkd"));
                Require(identity.SameProperty(equivalent),"No se reconoce el Component del conjunto.");
                report.Add("PASS: mismo nombre en DST o hoja diferente no se considera el mismo vinculo.");
                Require(FieldIdentity.Parse(slots.Single(s=>s.Tag=="R1_DESCRIPTION").Code).Property=="00_Revision","El formato forma parte del nombre.");
                Require(FieldIdentity.Parse("")==null,"Un atributo sin Field se interpreto como vinculado.");
                report.Add("PASS: formatos nativos y atributos sin Field reconocidos.");
                var placeholder=slots.Single(s=>s.Tag=="R4_BY");
                Require(CellStatus.WithoutData(placeholder) && CellStatus.HasField(placeholder),"Un punto vinculado se clasifico como sin Field.");
                Require(placeholder.Property=="03_R4_App" && CellStatus.Column(placeholder)=="1 · DIB" && CellStatus.Row(placeholder)=="Fila R4","No se identifica correctamente la celda numerada.");
                var available=slots.First(s=>CellStatus.WithoutData(s)&&!CellStatus.HasField(s));
                Require(CellStatus.State(available)=="Sin dato · sin Field","No se identifica una casilla sin contenido ni Field.");
                var empty=slots.Where(s=>s.BlockId==target.BlockId && CellStatus.WithoutData(s)).ToArray();
                Require(empty.Length==54 && empty.Count(CellStatus.HasField)==42,"No se identifican todos los espacios con punto del bloque.");
                Require(CellStatus.Column(slots.Single(s=>s.Tag=="04_R3_NO"))=="PLANO" && CellStatus.Column(slots.Single(s=>s.Tag=="04_R3_DESCRIP"))=="TÍTULO","No se reconocen las columnas libres de planos de referencia.");
                Require(CellStatus.HasField(new Slot {Value=".",Code="",HasFields=true}),"Un Field no legible se clasifico como disponible.");
                Require(!CellStatus.WithoutData(new Slot {Value="####"}),"Un error de Field se clasifico como espacio sin dato.");
                bool nameOnly;
                Require(CellMap.Candidates(slots,identity,out nameOnly).Single().Tag=="R3_CH" && !nameOnly,"La sincronizacion no sigue el vinculo real.");
                var sameNameOtherSource=FieldIdentity.Parse(target.Code.Replace(identity.DatabasePath,identity.DatabasePath+".otro"));
                Require(CellMap.Candidates(slots,sameNameOtherSource,out nameOnly).Any(s=>s.Tag=="R3_CH") && nameOnly,"No se advierte la coincidencia de nombre con otro origen.");
                Require(CellMap.Candidates(slots,FieldIdentity.Parse(target.Code.Replace("03_R3_CHkd","PROPIEDAD_SIN_CASILLA")),out nameOnly).Length==0,"Una propiedad sin relacion se asigno a una casilla por el punto.");
                report.Add("PASS: mapa sincronizado con Property distingue vinculo exacto, mismo nombre en otro origen y propiedad sin celda asociada.");
                report.Add("PASS: mapa distingue puntos vinculados, espacios sin Field y contenido existente; un Field no legible nunca se considera libre.");
                var scene=Scene.Read(doc.Database,target.BlockId,slots.Where(s=>s.BlockId==target.BlockId));
                Require(scene.Edges.Count>30 && scene.Texts.Count>=94,"Falta geometria real en la vista de contexto.");
                report.Add("PASS: vista de contexto construida con lineas y valores reales del rotulo ("+scene.Edges.Count+" segmentos).");
                Require(before==Snapshot(Catalog.Read(doc.Database)),"La inspeccion modifico un campo o valor.");
                report.Add("PASS: inspeccion y vistas no modifican valores ni codigos de Field.");
                report.Add("CASILLAS_FIELD_TEST_OK");doc.Editor.WriteMessage("\nCASILLAS_FIELD_TEST_OK\n");
            }
            catch(System.Exception ex){report.Add("CASILLAS_FIELD_FAILED "+ex);doc.Editor.WriteMessage("\nCASILLAS_FIELD_FAILED "+ex+"\n");}
            File.WriteAllLines(Path.Combine(Path.GetDirectoryName(doc.Name),"pruebas-field.txt"),report);
        }
    }
}
