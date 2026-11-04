#if DEBUG
using System;
using System.IO;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using CivilSpellAI.Autodesk;
using CivilSpellAI.Geometry;
using App = Autodesk.AutoCAD.ApplicationServices.Core.Application;
namespace CivilSpellAI.Commands
{
    public sealed class CorrugatedPipe3DTestCommands
    {
        [CommandMethod("AISPELLTESTPIPE300")]
        public void Test()
        {
            var doc=App.DocumentManager.MdiActiveDocument;
            try
            {
                using(var tr=doc.Database.TransactionManager.StartTransaction())
                {
                    var top=Read(doc.Database,tr,new[]{"7092","7093","7094","7095"});
                    var front=Read(doc.Database,tr,new[]{"7351","7352"});
                    ReferenceAssembly3D model;
                    Require(CorrugatedPipe3DAnalyzer.TryAnalyze(top,front,tr,out model),"No se detecta la tubería.");
                    Require(model.Parts.Count==0 && model.PipeRoutes.Count==5,"Se pierden ramales o tapas.");
                    Require(Math.Abs(model.PipeRoutes[0].Diameter-.3)<.0001,"Diámetro incorrecto.");
                    Require(model.PipeRoutes[0].Z[0]>model.PipeRoutes[0].Z[2]+2,"Pendiente FRONT perdida.");
                    Require(model.PipeRoutes[0].X[2]<5.8 && model.PipeRoutes[0].X[2]>5.4,"FRONT no manda en longitud.");
                    Require(model.PipeRoutes[1].Y[0]<-.7 && model.PipeRoutes[1].Y[1]>.7,"Se pierden las dos salidas T.");
                    Require(model.PipeRoutes.Take(2).All(r=>r.CorrugationProfile!=null && r.CorrugationPitch>.037 && r.CorrugationPitch<.039),"Falta el perfil o paso real del corrugado.");
                    Require(model.PipeRoutes.Skip(2).All(r=>r.CapProfile!=null && r.CapProfile.Y.Max()-r.CapProfile.Y.Min()>.04),"Se pierde el relieve de las tapas.");
                    var legacy=model.Copy();
                    foreach(var pipe in legacy.PipeRoutes)
                    {
                        if(!pipe.Cap)pipe.Wall=pipe.Diameter/2-ReferenceAssemblyDrawingService.BoreRadius(pipe);
                        pipe.CorrugationProfile=null;pipe.CapProfile=null;pipe.CorrugationPitch=0;
                    }
                    Require(CorrugatedPipe3DAnalyzer.UpgradeCorrugation(legacy,doc.Database,tr),"No se puede actualizar un modelo liso anterior.");
                    Require(Math.Abs(ReferenceAssemblyDrawingService.BoreRadius(legacy.PipeRoutes[0])-ReferenceAssemblyDrawingService.BoreRadius(model.PipeRoutes[0]))<1e-7,"La actualización altera el diámetro interior.");
                    var service=new ReferenceAssemblyDrawingService();
                    var bodies=service.Build(model);
                    try
                    {
                        Require(bodies.Count==5,"Cantidad de sólidos incorrecta.");
                        for(int i=0;i<bodies.Count;i++)
                        {
                            var solid=bodies[i] as Solid3d;
                            Require(solid!=null && solid.MassProperties.Volume>1e-8,"Sólido vacío.");
                            doc.Editor.WriteMessage("\nPIPE300_SOLID {0} volume={1:R} bounds={2}",i,solid.MassProperties.Volume,solid.GeometricExtents);
                        }
                        var route=model.PipeRoutes[0];
                        double run=Math.Sqrt(Math.Pow(route.X[1],2)+Math.Pow(route.Z[1]-route.Z[0],2))+(route.X[2]-route.X[1]);
                        double full=Math.PI*Math.Pow(route.Diameter/2,2)*run;
                        Require(((Solid3d)bodies[0]).MassProperties.Volume<full*.5,"El tubo no está hueco.");
                        var branchRoute=model.PipeRoutes[1];var profile=branchRoute.CorrugationProfile;
                        int crest=Array.IndexOf(profile.Y,profile.Y.Max()),valley=Array.IndexOf(profile.Y,profile.Y.Min());
                        double crestAt=(profile.X[crest]+profile.X[crest+1])/2;
                        double valleyAt=profile.X[valley];
                        foreach(var sample in new[]{Tuple.Create(crestAt,true),Tuple.Create(valleyAt,false)})
                            using(var probe=new Solid3d())
                            {
                                probe.CreateSphere(.0008);
                                probe.TransformBy(Matrix3d.Displacement(new Vector3d(branchRoute.X[0]+.145,branchRoute.Y[0]+sample.Item1,branchRoute.Z[0])));
                                Require(((Solid3d)bodies[1]).CheckInterference(probe)==sample.Item2,"La cresta o el valle no tiene relieve físico.");
                            }
                        doc.Editor.WriteMessage("\nPIPE300_REAL_CORRUGATION_PASS");
                        using(var probe=new Solid3d())
                        {
                            probe.CreateSphere(.004);
                            probe.TransformBy(Matrix3d.Displacement(new Vector3d(route.X[2]-.14,0,route.Z[2])));
                            foreach(var body in bodies.Take(2))
                                using(var intersection=(Solid3d)body.Clone())
                                using(var tool=(Solid3d)probe.Clone())
                                {
                                    intersection.BooleanOperation(BooleanOperationType.BoolIntersect,tool);
                                    Require(intersection.IsNull || intersection.MassProperties.Volume<1e-9,"La pared del ramal bloquea el interior de la T.");
                                }
                        }
                    }
                    finally{foreach(var body in bodies)body.Dispose();}
                    var id=service.Create(doc.Database,tr,new Point3d(579168,8297887,0),model);
                    ReferenceAssembly3D read;
                    Require(service.TryRead(id,tr,out read) && read.Serialize()==model.Serialize(),"Persistencia incorrecta.");
                    var block=(BlockReference)tr.GetObject(id,OpenMode.ForRead);
                    var changed=model.Copy();changed.PipeRoutes[0].Wall=.02;
                    service.Update(doc.Database,tr,id,changed);
                    Require(service.TryRead(id,tr,out read) && read.PipeRoutes[0].Wall==.02,"Edición incorrecta.");
                    service.Update(doc.Database,tr,id,model);
                    File.WriteAllText(Path.Combine(Path.GetDirectoryName(doc.Database.Filename),"pipe300-model.json"),model.Serialize());
                    // Existing non-pipe figure selection remains on its own analyzer.
                    var blank=new SelectedFigureSnapshot{SourceIds=new ObjectId[0],Origin=Point3d.Origin};
                    ReferenceAssembly3D unused;
                    Require(!CorrugatedPipe3DAnalyzer.TryAnalyze(blank,blank,tr,out unused),"Detección fuera de alcance.");
                    tr.Commit();
                    doc.Editor.WriteMessage("\nPIPE300_NATIVE_PASS");
                }
            }
            catch(System.Exception ex){doc.Editor.WriteMessage("\nPIPE300_NATIVE_FAIL {0}",ex);}
        }
        [CommandMethod("AISPELLVERIFYPIPE300")]
        public void Verify()
        {
            var doc=App.DocumentManager.MdiActiveDocument;
            try
            {
                using(var tr=doc.Database.TransactionManager.StartOpenCloseTransaction())
                {
                    var bt=(BlockTable)tr.GetObject(doc.Database.BlockTableId,OpenMode.ForRead);
                    var space=(BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace],OpenMode.ForRead);
                    var service=new ReferenceAssemblyDrawingService();int count=0;
                    foreach(ObjectId id in space)
                    {
                        ReferenceAssembly3D model;if(!service.TryRead(id,tr,out model))continue;
                        if(model.PipeRoutes.Count!=5)continue;
                        Require(model.Serialize()==File.ReadAllText(Path.Combine(Path.GetDirectoryName(doc.Database.Filename),"pipe300-model.json")),"Cambió el modelo al reabrir.");
                        var block=(BlockReference)tr.GetObject(id,OpenMode.ForRead);
                        var def=(BlockTableRecord)tr.GetObject(block.BlockTableRecord,OpenMode.ForRead);
                        Require(def.Cast<ObjectId>().All(child=>tr.GetObject(child,OpenMode.ForRead) is Solid3d),"Sólidos perdidos.");
                        count++;
                    }
                    Require(count==1,"No se encuentra el modelo guardado.");
                }
                doc.Editor.WriteMessage("\nPIPE300_REOPEN_PASS");
            }
            catch(System.Exception ex){doc.Editor.WriteMessage("\nPIPE300_REOPEN_FAIL {0}",ex);}
        }
        [CommandMethod("AISPELLTESTPIPE300UPGRADE")]
        public void UpgradeTest()
        {
            var doc=App.DocumentManager.MdiActiveDocument;
            try
            {
                using(var tr=doc.Database.TransactionManager.StartOpenCloseTransaction())
                {
                    var top=Read(doc.Database,tr,new[]{"7092","7093","7094","7095"});
                    var front=Read(doc.Database,tr,new[]{"7351","7352"});
                    ReferenceAssembly3D fresh;Require(CorrugatedPipe3DAnalyzer.TryAnalyze(top,front,tr,out fresh),"No se detecta la referencia.");
                    var before=fresh.Copy();
                    foreach(var route in before.PipeRoutes)
                    {
                        if(!route.Cap)route.Wall=route.Diameter/2-ReferenceAssemblyDrawingService.BoreRadius(route);
                        route.CorrugationProfile=null;route.CapProfile=null;route.CorrugationPitch=0;
                    }
                    // Round-trip the old representation to check absent metadata.
                    before=ReferenceAssembly3D.Deserialize(before.Serialize());before.Validate();
                    Require(CorrugatedPipe3DAnalyzer.UpgradeCorrugation(before,doc.Database,tr),"No se actualiza la representación lisa anterior.");
                    for(int i=0;i<2;i++)
                        Require(Math.Abs(ReferenceAssemblyDrawingService.BoreRadius(before.PipeRoutes[i])-ReferenceAssemblyDrawingService.BoreRadius(fresh.PipeRoutes[i]))<1e-7 &&
                            before.PipeRoutes[i].CorrugationProfile!=null,"La actualización pierde el perfil o modifica el diámetro interior.");
                    Require(before.PipeRoutes.Skip(2).All(r=>r.CapProfile!=null),"La actualización pierde las bandas de las tapas.");
                    Require(!CorrugatedPipe3DAnalyzer.UpgradeCorrugation(before,doc.Database,tr),"La actualización se repite innecesariamente.");
                    before.Validate();
                }
                doc.Editor.WriteMessage("\nPIPE300_UPGRADE_PASS");
            }
            catch(System.Exception ex){doc.Editor.WriteMessage("\nPIPE300_UPGRADE_FAIL {0}",ex);}
        }
        private static SelectedFigureSnapshot Read(Database db,Transaction tr,string[] handles)
        {
            var ids=handles.Select(h=>db.GetObjectId(false,new Handle(Convert.ToInt64(h,16)),0)).ToArray();
            SelectedFigureSnapshot snapshot;string error;
            if(!new SelectedFigureDrawingService().TryRead(ids,tr,out snapshot,out error))throw new InvalidOperationException(error);
            return snapshot;
        }
        private static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
    }
}
#endif
