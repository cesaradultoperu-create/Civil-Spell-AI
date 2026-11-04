#if DEBUG
using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using CivilSpellAI.Autodesk;
using CivilSpellAI.Geometry;
using AcadApplication = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace CivilSpellAI.Commands
{
    public sealed class ReferenceAssembly3DTestCommands
    {
        private static ObjectId undoId,undoDefinition;
        private static string undoModel;
        [CommandMethod("AISPELLVERIFYINCLINEDDETAIL")]
        public void VerifyInclinedDetail()
        {
            var doc=AcadApplication.DocumentManager.MdiActiveDocument;
            try
            {
                using(var tr=doc.Database.TransactionManager.StartOpenCloseTransaction())
                {
                    var space=(BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(doc.Database),OpenMode.ForRead);
                    var block=space.Cast<ObjectId>().Select(id=>tr.GetObject(id,OpenMode.ForRead) as BlockReference)
                        .Single(r=>r!=null && r.Name.StartsWith("CIVILSPELLAI_REFERENCIAS_3D_") && Math.Abs(r.Position.X-68)<1e-7);
                    ReferenceAssembly3D model;
                    Require(new ReferenceAssemblyDrawingService().TryRead(block.ObjectId,tr,out model) && model.Serialize()==File.ReadAllText(
                        Path.Combine(Path.GetDirectoryName(doc.Database.Filename),"inclined-model.json")),"El detalle inclinado cambió al reabrir.");
                }
                doc.Editor.WriteMessage("\nINCLINED_DETAIL_REOPEN_OK");
            }
            catch(System.Exception ex){doc.Editor.WriteMessage("\nINCLINED_DETAIL_REOPEN_FAILED {0}",ex);}
        }
        [CommandMethod("AISPELLPROBEINCLINEDDETAIL")]
        public void ProbeInclinedDetail()
        {
            var doc=AcadApplication.DocumentManager.MdiActiveDocument;
            try
            {
                using(var tr=doc.Database.TransactionManager.StartTransaction())
                {
                    var space=(BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(doc.Database),OpenMode.ForRead);
                    Func<double,double,double,double,SelectedFigureSnapshot> read=(x1,y1,x2,y2)=>{
                        var ids=space.Cast<ObjectId>().Where(id=>{
                            var e=tr.GetObject(id,OpenMode.ForRead) as Entity;
                            if(!(e is Curve) && !(e is Hatch) && !(e is BlockReference))return false;
                            var b=e.GeometricExtents;
                            return b.MinPoint.X>=x1 && b.MaxPoint.X<=x2 && b.MinPoint.Y>=y1 && b.MaxPoint.Y<=y2;
                        }).ToArray();
                        SelectedFigureSnapshot view;string error;
                        Require(new SelectedFigureDrawingService().TryRead(ids,tr,out view,out error),error);return view;
                    };
                    var top=read(23.3,-2.8,27.5,-1.0);var front=read(23.3,-4.95,27.5,-3.0);
                    foreach(var view in new[]{top,front})
                    {
                        doc.Editor.WriteMessage("\nINCLINED_VIEW {0} objects={1} frame={2}",view==top?"TOP":"FRONT",view.SourceIds.Length,
                            view.Measures.FrameBounds==null?"none":string.Join(",",view.Measures.FrameBounds));
                        foreach(var id in view.SourceIds)
                        {
                            var e=(Entity)tr.GetObject(id,OpenMode.ForRead);var b=e.GeometricExtents;var block=e as BlockReference;
                            doc.Editor.WriteMessage("\nINCLINED_ENTITY {0} {1} {2} {3} {4}",id.Handle,e.GetType().Name,b.MinPoint,b.MaxPoint,
                                block==null?"":block.Name+" rotation="+block.Rotation.ToString("R",System.Globalization.CultureInfo.InvariantCulture));
                        }
                        File.WriteAllLines(Path.Combine(Path.GetDirectoryName(doc.Database.Filename),view==top?"inclined-top.txt":"inclined-front.txt"),
                            view.Outlines.Select(line=>string.Join(";",line.Select(p=>p.X.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+","+p.Y.ToString("R",System.Globalization.CultureInfo.InvariantCulture)))));
                    }
                    var model=new DrainageReference3DAnalyzer().Analyze(top,front,ReferenceProfileReader.Read(top.SourceIds,tr),ReferenceProfileReader.Read(front.SourceIds,tr));
                    Require(model.Pipe!=null,"No se reconoció la tubería del detalle actual.");
                    var plan=ReferenceProfileReader.Read(top.SourceIds,tr);var elevation=ReferenceProfileReader.Read(front.SourceIds,tr);
                    var ambiguous=TransformView(top,1,0,0);
                    double topX=top.Origin.X+top.Measures.FrameBounds[0],topY=top.Origin.Y+top.Measures.FrameBounds[2];
                    foreach(double offset in new[]{-.5,.5})ambiguous.Outlines.Add(new[]{
                        new Point2d(topX+model.Pipe.X1,topY+model.Pipe.Y1+.15+offset*model.Pipe.Diameter),
                        new Point2d(topX+model.Pipe.X2,topY+model.Pipe.Y2+.15+offset*model.Pipe.Diameter)});
                    bool ambiguityRejected=false;
                    try{new DrainageReference3DAnalyzer().Analyze(ambiguous,front,plan,elevation);}catch(ArgumentException){ambiguityRejected=true;}
                    Require(ambiguityRejected,"Se eligió arbitrariamente entre dos ejes de tubería en TOP.");
                    var scaled=new DrainageReference3DAnalyzer().Analyze(TransformView(top,1.5,100,10),TransformView(front,1.5,700,-60),
                        TransformProfiles(plan,1.5,100,10),TransformProfiles(elevation,1.5,700,-60));
                    Require(Math.Abs(scaled.Pipe.Diameter-model.Pipe.Diameter*1.5)<1e-8 &&
                        Math.Abs(scaled.Pipe.InclinationDegrees-model.Pipe.InclinationDegrees)<1e-8,"El reconocimiento inclinado depende de coordenadas fijas.");
                    // Keep materials and the two drawn pipe borders, but remove the
                    // separate straight drafting axis. Its offset must not block editing.
                    double boxWidth=model.Parts[0].Top[0].X.Max();
                    var boxSection=elevation.Single(p=>p.Pattern=="AR-CONC" && !p.Contour.Hole && Math.Abs(p.Contour.X.Max()-p.Contour.X.Min()-boxWidth)<1e-7);
                    double frontX=boxSection.Contour.X.Min(),frontBottom=boxSection.Contour.Y.Min();
                    var noAxis=TransformView(front,1,0,0);int removed=noAxis.Outlines.RemoveAll(line=>{
                        if(line.Length!=2 || Math.Abs(line[1].X-line[0].X)<.5)return false;
                        double slope=(line[1].Y-line[0].Y)/(line[1].X-line[0].X);
                        if(Math.Abs(slope-Math.Tan(model.Pipe.InclinationDegrees*Math.PI/180))>1e-6)return false;
                        double center=frontBottom+model.Pipe.Z1+(line[0].X-frontX-model.Pipe.X1)*slope;
                        return Math.Abs(line[0].Y-center)<model.Pipe.Diameter*.4;
                    });
                    Require(removed>0,"La prueba no retiró el eje de delineación.");
                    var withoutAxis=new DrainageReference3DAnalyzer().Analyze(top,noAxis,plan,elevation);
                    Require(Math.Abs(withoutAxis.Pipe.InclinationDegrees-model.Pipe.InclinationDegrees)<1e-8 &&
                        Math.Abs(withoutAxis.Pipe.Z1-model.Pipe.Z1)<1e-8,"El eje de delineación desplazado aún condiciona el tubo.");
                    var sourcePipe=front.SourceIds.Select(id=>tr.GetObject(id,OpenMode.ForRead) as BlockReference).Single(b=>b!=null && b.Name=="tuberia");
                    var oldPose=sourcePipe.BlockTransform;sourcePipe.UpgradeOpen();
                    sourcePipe.TransformBy(Matrix3d.Rotation(-Math.PI/180,Vector3d.ZAxis,sourcePipe.Position));
                    SelectedFigureSnapshot rotated;string readError;
                    Require(new SelectedFigureDrawingService().TryRead(front.SourceIds,tr,out rotated,out readError),readError);
                    var rotatedModel=new DrainageReference3DAnalyzer().Analyze(top,rotated,plan,ReferenceProfileReader.Read(front.SourceIds,tr));
                    Require(Math.Abs(rotatedModel.Pipe.InclinationDegrees-model.Pipe.InclinationDegrees+1)<1e-7 &&
                        Math.Abs(rotatedModel.Pipe.Diameter-model.Pipe.Diameter)<1e-8,"No se reconoció el giro aplicado al BlockReference.");
                    sourcePipe.TransformBy(oldPose*sourcePipe.BlockTransform.Inverse());
                    doc.Editor.WriteMessage("\nINCLINED_BLOCK_ROTATION_AND_AXIS_OK");
                    doc.Editor.WriteMessage("\nINCLINED_PIPE {0}",model.Serialize());
                    var service=new ReferenceAssemblyDrawingService();
                    var id3d=service.Create(doc.Database,tr,new Point3d(68,0,0),model);
                    ReferenceAssembly3D restored;
                    Require(service.TryRead(id3d,tr,out restored) && restored.Serialize()==model.Serialize(),"No se conservó el detalle inclinado.");
                    File.WriteAllText(Path.Combine(Path.GetDirectoryName(doc.Database.Filename),"inclined-model.json"),model.Serialize());
                    Render(model,top,front,Path.ChangeExtension(doc.Database.Filename,".inclined.png"));
                    tr.Commit();
                }
                doc.Editor.WriteMessage("\nINCLINED_DETAIL_RECOGNITION_OK");
            }
            catch(System.Exception ex){doc.Editor.WriteMessage("\nINCLINED_DETAIL_RECOGNITION_FAILED {0}",ex);}
        }
        [CommandMethod("AISPELLVERIFYREPORTEDREFERENCE3D")]
        public void VerifyReported()
        {
            var doc=AcadApplication.DocumentManager.MdiActiveDocument;
            try
            {
                using(var tr=doc.Database.TransactionManager.StartOpenCloseTransaction())
                {
                    var table=(BlockTable)tr.GetObject(doc.Database.BlockTableId,OpenMode.ForRead);
                    var space=(BlockTableRecord)tr.GetObject(table[BlockTableRecord.ModelSpace],OpenMode.ForRead);
                    for(int pair=0;pair<3;pair++)
                    {
                        var block=space.Cast<ObjectId>().Select(id=>tr.GetObject(id,OpenMode.ForRead) as BlockReference)
                            .Single(r=>r!=null && r.Name.StartsWith("CIVILSPELLAI_REFERENCIAS_3D_") && Math.Abs(r.Position.X-40-pair*7)<1e-7);
                        ReferenceAssembly3D model;
                        Require(new ReferenceAssemblyDrawingService().TryRead(block.ObjectId,tr,out model) && model.Serialize()==
                            File.ReadAllText(Path.Combine(Path.GetDirectoryName(doc.Database.Filename),"reported-pair-"+pair+".json")),"Metadatos modificados al reabrir.");
                        var definition=(BlockTableRecord)tr.GetObject(block.BlockTableRecord,OpenMode.ForRead);
                        Require(definition.Cast<ObjectId>().Count(id=>tr.GetObject(id,OpenMode.ForRead) is Solid3d)==model.Parts.Count,"Sólidos perdidos al reabrir.");
                    }
                }
                doc.Editor.WriteMessage("\nREPORTED_REOPEN_PASS");
            }
            catch(System.Exception ex){doc.Editor.WriteMessage("\nREPORTED_REOPEN_FAIL {0}",ex);}
        }
        [CommandMethod("AISPELLPROBEREFERENCE3DREPORTED")]
        public void ProbeReported()
        {
            var doc=AcadApplication.DocumentManager.MdiActiveDocument;
            using(var tr=doc.Database.TransactionManager.StartTransaction())
            {
                var windows=new[]{new[]{19.3,-2.8,22.6,-1.0},new[]{19.3,-4.95,22.7,-3.3},
                    new[]{23.3,-2.8,27.5,-1.0},new[]{23.3,-4.95,27.5,-3.3},
                    new[]{19.0,3.0,23.0,5.5},new[]{19.0,.3,23.0,2.9}};
                for(int pair=0;pair<3;pair++)
                {
                    try
                    {
                        var a=windows[pair*2];var b=windows[pair*2+1];
                        var top=Read(doc.Database,tr,a[0],a[1],a[2],a[3]);
                        var front=Read(doc.Database,tr,b[0],b[1],b[2],b[3]);
                        doc.Editor.WriteMessage("\nREPORTED_PAIR {0} TOP={1} FRONT={2}",pair,top.SourceIds.Length,front.SourceIds.Length);
                        foreach(var view in new[]{top,front})
                        {
                            doc.Editor.WriteMessage("\nVIEW {0} ORIGIN={1} FRAME={2}",view==top?"TOP":"FRONT",view.Origin,
                                view.Measures.FrameBounds==null?"none":string.Join(",",view.Measures.FrameBounds));
                            foreach(var line in view.Outlines)
                                if(line.Length<12) doc.Editor.WriteMessage("\nLINE {0}",string.Join(";",line.Select(p=>p.X.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+","+p.Y.ToString("R",System.Globalization.CultureInfo.InvariantCulture))));
                        }
                        foreach(var p in ReferenceProfileReader.Read(front.SourceIds,tr))
                            if(p.Contour.Bulges!=null && p.Contour.Bulges.Any(v=>Math.Abs(v)>1e-6))
                                doc.Editor.WriteMessage("\nCURVED_PROFILE {0}",new ReferenceAssembly3D{Parts={new ReferencePart3D{Top={p.Contour}}}}.Serialize());
                        var tp=ReferenceProfileReader.Read(top.SourceIds,tr);var fp=ReferenceProfileReader.Read(front.SourceIds,tr);
                        var analyzer=new DrainageReference3DAnalyzer();
                        var model=analyzer.Analyze(top,front,tp,fp);
                        Require(model.Pipe!=null && Math.Abs(model.Pipe.Diameter-.1)<1e-7,"Se perdió la tubería de la referencia.");
                        if(pair==1) Require(model.Pipe.X1<-.99 && Math.Abs(model.Pipe.Z2-model.Pipe.Z1)<1e-7 &&
                            model.Parts.Any(p=>p.Name=="Concreto: soporte interior"),"Falló el tubo izquierdo o el soporte interior.");
                        if(pair==2) Require(Math.Abs(model.Pipe.X1-.65)<1e-7 && Math.Abs(model.Pipe.X2-.65)<1e-7 &&
                            model.Pipe.Y2>1.5 && Math.Abs(model.Pipe.Z1-.3)<1e-7 &&
                            model.Parts.Count(p=>p.Name.StartsWith("Mampostería: fondo"))==2,"Falló la sección circular o los dos canales.");
                        var scaled=analyzer.Analyze(TransformView(top,1.5,100,10),TransformView(front,1.5,700,-60),
                            TransformProfiles(tp,1.5,100,10),TransformProfiles(fp,1.5,700,-60));
                        Require(Math.Abs(scaled.Pipe.X1-model.Pipe.X1*1.5)<1e-7 && Math.Abs(scaled.Pipe.Y2-model.Pipe.Y2*1.5)<1e-7 &&
                            Math.Abs(scaled.Pipe.Z1-model.Pipe.Z1*1.5)<1e-7 && Math.Abs(scaled.Pipe.Diameter-model.Pipe.Diameter*1.5)<1e-7 &&
                            scaled.Parts.Count==model.Parts.Count,"La reconstrucción depende de coordenadas o escala fijas.");
                        if(pair==2)
                        {
                            var conflict=TransformProfiles(fp,1,0,0);
                            foreach(var profile in conflict.Where(p=>p.Pattern==null && p.Contour.X.Length==4 && p.Contour.Bulges!=null && p.Contour.Bulges.All(v=>v>.4)))
                            {
                                double cx=profile.Contour.X.Average(),cy=profile.Contour.Y.Average();
                                profile.Contour.X=profile.Contour.X.Select(v=>cx+(v-cx)*2).ToArray();
                                profile.Contour.Y=profile.Contour.Y.Select(v=>cy+(v-cy)*2).ToArray();
                            }
                            bool rejected=false;
                            try{analyzer.Analyze(top,front,tp,conflict);}catch(ArgumentException){rejected=true;}
                            Require(rejected,"Se aceptó una sección circular de diámetro incompatible con TOP.");
                        }
                        var service=new ReferenceAssemblyDrawingService();
                        var entities=service.Build(model);
                        try
                        {
                            Require(entities.OfType<Solid3d>().Count()==model.Parts.Count,"Faltan sólidos nativos de los componentes.");
                            foreach(var entity in entities) Require(SubDMesh.GetObjectMesh(entity,new MeshFaceterData()).VertexArray.Count>0,"Componente nativo vacío.");
                            if(pair==2)
                                using(var section=((Solid3d)entities[0]).GetSection(new Plane(new Point3d(0,1.225,0),Vector3d.YAxis)))
                                    Require(section!=null && Math.Abs(section.Area-(1.3*.95-Math.PI*.05*.05))<1e-7,
                                        "La tubería perpendicular no abrió el paso circular en el muro.");
                            model.Pipe.Wall=.004;
                            var hollow=service.Build(model);
                            try
                            {
                                double length=new Point3d(model.Pipe.X1,model.Pipe.Y1,model.Pipe.Z1).DistanceTo(new Point3d(model.Pipe.X2,model.Pipe.Y2,model.Pipe.Z2));
                                double expected=Math.PI*(Math.Pow(.05,2)-Math.Pow(.046,2))*length;
                                Require(hollow.Last() is Solid3d && Math.Abs(((Solid3d)hollow.Last()).MassProperties.Volume-expected)<1e-8,"Tubo hueco incorrecto.");
                            }
                            finally{foreach(var entity in hollow)entity.Dispose();model.Pipe.Wall=0;}
                        }
                        finally{foreach(var entity in entities)entity.Dispose();}
                        var id=service.Create(doc.Database,tr,new Point3d(40+pair*7,0,0),model);
                        ReferenceAssembly3D restored;
                        Require(service.TryRead(id,tr,out restored) && restored.Serialize()==model.Serialize(),"Persistencia incompleta.");
                        File.WriteAllText(Path.Combine(Path.GetDirectoryName(doc.Database.Filename),"reported-pair-"+pair+".json"),model.Serialize());
                        Render(model,top,front,Path.Combine(Path.GetDirectoryName(doc.Database.Filename),"reported-pair-"+pair+".png"));
                        doc.Editor.WriteMessage("\nREPORTED_PAIR_PASS {0}",pair);
                    }
                    catch(System.Exception ex){doc.Editor.WriteMessage("\nREPORTED_PAIR_FAIL {0} {1}",pair,ex);}
                }
                tr.Commit();
            }
        }
        [CommandMethod("AISPELLTESTREFERENCE3DCD")]
        public void RunCD()
        {
            var doc=AcadApplication.DocumentManager.MdiActiveDocument;
            try
            {
                using(var tr=doc.Database.TransactionManager.StartTransaction())
                {
                    var top=Read(doc.Database,tr,24.3,-2.8,27.5,-1.0);
                    var front=Read(doc.Database,tr,24.3,-4.9,27.5,-3.3);
                    var plan=ReferenceProfileReader.Read(top.SourceIds,tr);
                    var section=ReferenceProfileReader.Read(front.SourceIds,tr);
                    foreach(var profiles in new[]{plan,section})
                        foreach(var p in profiles.Where(p=>p.Pattern!=null))
                            doc.Editor.WriteMessage("\nCD_PROFILE {0} X={1} Y={2}",p.Pattern,
                                string.Join(",",p.Contour.X.Select(v=>v.ToString("0.######",System.Globalization.CultureInfo.InvariantCulture))),
                                string.Join(",",p.Contour.Y.Select(v=>v.ToString("0.######",System.Globalization.CultureInfo.InvariantCulture))));
                    var model=new DrainageReference3DAnalyzer().Analyze(top,front,plan,section);
                    Require(model.Pipe.X1<-.9 && model.Pipe.X2>0 && model.Pipe.X2<.35,"C+D perdió la tubería izquierda.");
                    Require(Math.Abs(model.Pipe.Z2-model.Pipe.Z1)<1e-7 && Math.Abs(model.Pipe.Diameter-.1)<1e-7,"C+D no conserva el tubo horizontal.");
                    Require(Math.Abs(model.Parts[0].Height-.95)<1e-7 && Math.Abs(model.Cuts[0].Bottom-.15)<1e-7,"C+D reutilizó las cotas de A+B.");
                    Require(model.Parts.Any(p=>p.Name=="Concreto: soporte interior") && model.Parts.Any(p=>p.Name=="Elemento interior: material por confirmar"),"C+D perdió el elemento interior.");
                    // Reposition each view independently and change the drawing scale:
                    // reconstruction must follow geometry, rather than fixture coordinates.
                    var scaled=new DrainageReference3DAnalyzer().Analyze(TransformView(top,1.5,100,10),TransformView(front,1.5,700,-60),
                        TransformProfiles(plan,1.5,100,10),TransformProfiles(section,1.5,700,-60));
                    Require(Math.Abs(scaled.Parts[0].Height-model.Parts[0].Height*1.5)<1e-7 &&
                        Math.Abs(scaled.Pipe.X1-model.Pipe.X1*1.5)<1e-7 && Math.Abs(scaled.Pipe.Z1-model.Pipe.Z1*1.5)<1e-7 &&
                        Math.Abs(scaled.Pipe.Diameter-model.Pipe.Diameter*1.5)<1e-7 && scaled.Parts.Count==model.Parts.Count,
                        "La reconstrucción C+D depende de las coordenadas o dimensiones del ejemplo.");
                    var entities=new ReferenceAssemblyDrawingService().Build(model);
                    try
                    {
                        Require(entities.OfType<Solid3d>().Count()==model.Parts.Count && entities.Last() is global::Autodesk.AutoCAD.DatabaseServices.Surface,"C+D tiene componentes inválidos.");
                        var walls=entities[model.Parts.FindIndex(p=>p.Name=="Mampostería: laterales")];
                        Require(walls.GeometricExtents.MaxPoint.X>1.4 && walls.GeometricExtents.MinPoint.X>.8,"C+D colocó el canal del lado incorrecto.");
                        foreach(var entity in entities)Require(SubDMesh.GetObjectMesh(entity,new MeshFaceterData()).VertexArray.Count>0,"C+D contiene geometría vacía.");
                    }
                    finally{foreach(var entity in entities)entity.Dispose();}
                    new ReferenceAssemblyDrawingService().Create(doc.Database,tr,new Point3d(46,0,0),model);
                    File.WriteAllText(Path.Combine(Path.GetDirectoryName(doc.Database.Filename),"reference-CD-model.json"),model.Serialize());
                    tr.Commit();
                    Render(model,top,front,Path.ChangeExtension(doc.Database.Filename,".reference-CD-3d.png"));
                }
                doc.Editor.WriteMessage("\nREFERENCE3D_CD_OK");
            }
            catch(System.Exception ex){doc.Editor.WriteMessage("\nREFERENCE3D_CD_FAILED "+ex);}
        }
        [CommandMethod("AISPELLTESTREFERENCE3D")]
        public void Run()
        {
            var doc=AcadApplication.DocumentManager.MdiActiveDocument;
            try
            {
                SelectedFigureSnapshot top,front;ReferenceAssembly3D model;
                using(var tr=doc.Database.TransactionManager.StartTransaction())
                {
                    top=Read(doc.Database,tr,19.3,-2.7,22.5,-1.1);
                    front=Read(doc.Database,tr,19.3,-4.9,22.6,-3.5);
                    var tp=ReferenceProfileReader.Read(top.SourceIds,tr);var fp=ReferenceProfileReader.Read(front.SourceIds,tr);
                    doc.Editor.WriteMessage("\nREFERENCE_PROFILES TOP={0} FRONT={1}",tp.Count,fp.Count);
                    foreach(var p in fp.Where(p=>p.Pattern!=null)) doc.Editor.WriteMessage("\nFRONT_MATERIAL {0} vertices={1}",p.Pattern,p.Contour.X.Length);
                    model=new DrainageReference3DAnalyzer().Analyze(top,front,tp,fp);
                    doc.Editor.WriteMessage("\nREFERENCE_MODEL {0}",model.Serialize());
                    var service=new ReferenceAssemblyDrawingService();
                    var entities=service.Build(model);
                    try
                    {
                        Require(entities.OfType<Solid3d>().Count()>=4,"Faltan sólidos estructurales.");
                        Require(model.Parts.Any(p=>p.Name=="Cama de arena: TOP + FRONT" && p.Front.Count==1),"Falta la cama de arena definida por las dos vistas.");
                        foreach(var entity in entities)
                        {
                            var data=SubDMesh.GetObjectMesh(entity,new MeshFaceterData());
                            Require(data.VertexArray.Count>0 && data.FaceArray.Count>0,"Malla nativa vacía.");
                        }
                        var chamber=(Solid3d)entities[0];
                        Require(Math.Abs(chamber.GeometricExtents.MaxPoint.Z-.8)<1e-7,"Altura de concreto incorrecta.");
                        using(var section=chamber.GetSection(new Plane(new Point3d(0,0,.075),Vector3d.ZAxis)))
                            Require(section!=null && Math.Abs(section.Area-1.3)<1e-7,"Fondo perdido en la reconstrucción.");
                        Require(entities.Any(e=>e.GeometricExtents.MinPoint.X<-.4),"Se perdió el saliente TOP.");
                        Require(model.Pipe!=null && model.Pipe.Z2<model.Pipe.Z1 && Math.Abs(model.Pipe.Diameter-.1)<1e-6,"Tubería no relacionada con las dos vistas.");
                        Require(Math.Abs((model.Pipe.Z2-model.Pipe.Z1)/(model.Pipe.X2-model.Pipe.X1)+.404026)<1e-4,"Pendiente sustituida por un valor fijo.");
                        var invert=entities[model.Parts.FindIndex(p=>p.Name=="Mampostería: fondo")];
                        var walls=entities[model.Parts.FindIndex(p=>p.Name=="Mampostería: laterales")];
                        Require(Math.Abs(invert.GeometricExtents.MinPoint.Z-.4)<1e-7 && Math.Abs(invert.GeometricExtents.MaxPoint.Z-.5)<1e-7,
                            "El fondo del canal no conserva las cotas de FRONT.");
                        Require(Math.Abs(walls.GeometricExtents.MinPoint.Z-.5)<1e-7 && Math.Abs(walls.GeometricExtents.MaxPoint.Z-.8)<1e-7,
                            "Los laterales del canal no conservan las cotas de FRONT.");
                        Require(entities.Last() is global::Autodesk.AutoCAD.DatabaseServices.Surface,"Sin espesor conocido la tubería debe permanecer como superficie.");
                        WriteComparison(doc.Database,tr,model,top,front,fp,entities);
                    }
                    finally {foreach(var e in entities)e.Dispose();}
                    model.Pipe.Wall=.004; // Synthetic wall is for the hollow-solid regression only.
                    var hollow=service.Build(model);
                    try
                    {
                        var tube=hollow.Last() as Solid3d;
                        double length=new Point3d(model.Pipe.X1,model.Pipe.Y1,model.Pipe.Z1).DistanceTo(new Point3d(model.Pipe.X2,model.Pipe.Y2,model.Pipe.Z2));
                        double expected=Math.PI*(Math.Pow(model.Pipe.Diameter/2,2)-Math.Pow(model.Pipe.Diameter/2-model.Pipe.Wall,2))*length;
                        Require(tube!=null && Math.Abs(tube.MassProperties.Volume-expected)<1e-8,"La tubería con espesor no es un sólido hueco del volumen esperado.");
                    }
                    finally {foreach(var entity in hollow)entity.Dispose();}
                    var id=service.Create(doc.Database,tr,new Point3d(40,0,0),model);
                    ReferenceAssembly3D roundTrip;
                    Require(service.TryRead(id,tr,out roundTrip) && roundTrip.Serialize()==model.Serialize(),"Persistencia incompleta.");
                    // The delivered example keeps the unspecified pipe wall as a surface.
                    model.Pipe.Wall=0;service.Update(doc.Database,tr,id,model);
                    File.WriteAllText(Path.Combine(Path.GetDirectoryName(doc.Database.Filename),"reference-model.json"),model.Serialize());
                    var inclined=model.Copy();inclined.Pipe.SetInclination(-19.5,true);
                    var inclinedId=service.Create(doc.Database,tr,new Point3d(55,0,0),inclined);
                    Require(service.TryRead(inclinedId,tr,out roundTrip) && roundTrip.Serialize()==inclined.Serialize(),"No se guardó la inclinación.");
                    File.WriteAllText(Path.Combine(Path.GetDirectoryName(doc.Database.Filename),"inclination-model.json"),inclined.Serialize());
                    TestGeneralProfiles();
                    tr.Commit();
                }
                Render(model,top,front,Path.ChangeExtension(doc.Database.Filename,".reference-3d.png"));
                doc.Editor.WriteMessage("\nREFERENCE3D_NATIVE_TOP_FRONT_OK\nREFERENCE3D_COMPONENT_ARCS_HOLES_OK\nREFERENCE3D_PREVIEW_PERSISTENCE_OK");
            }
            catch(System.Exception ex){doc.Editor.WriteMessage("\nREFERENCE3D_FAILED "+ex);}
        }
        [CommandMethod("AISPELLVERIFYREFERENCE3D")]
        public void Verify()
        {
            var doc=AcadApplication.DocumentManager.MdiActiveDocument;
            try
            {
                using(var tr=doc.Database.TransactionManager.StartOpenCloseTransaction())
                {
                    var blocks=(BlockTable)tr.GetObject(doc.Database.BlockTableId,OpenMode.ForRead);
                    var space=(BlockTableRecord)tr.GetObject(blocks[BlockTableRecord.ModelSpace],OpenMode.ForRead);
                    bool found=false;
                    foreach(ObjectId id in space)
                    {
                        ReferenceAssembly3D model;
                        if(new ReferenceAssemblyDrawingService().TryRead(id,tr,out model))
                        {
                            Require(model.Pipe!=null && model.Parts.Count>=4,"Metadatos incompletos al reabrir.");found=true;
                            var reference=(BlockReference)tr.GetObject(id,OpenMode.ForRead);
                            if(Math.Abs(reference.Position.X-55)<1e-7)
                                Require(Math.Abs(model.Pipe.InclinationDegrees+19.5)<1e-10 && model.Serialize()==File.ReadAllText(
                                    Path.Combine(Path.GetDirectoryName(doc.Database.Filename),"inclination-model.json")),"Se perdió la inclinación al reabrir.");
                            var definition=(BlockTableRecord)tr.GetObject(reference.BlockTableRecord,OpenMode.ForRead);
                            Require(definition.Cast<ObjectId>().Count(child=>tr.GetObject(child,OpenMode.ForRead) is Solid3d)>=4,"Sólidos perdidos al guardar.");
                        }
                    }
                    Require(found,"No hay modelo persistente.");
                }
                doc.Editor.WriteMessage("\nREFERENCE3D_REOPEN_OK");
            }
            catch(System.Exception ex){doc.Editor.WriteMessage("\nREFERENCE3D_FAILED "+ex);}
        }
        [CommandMethod("AISPELLREFERENCE3DUNDOCHANGE")]
        public void ChangeForUndo()
        {
            var doc=AcadApplication.DocumentManager.MdiActiveDocument;
            try
            {
                using(var tr=doc.Database.TransactionManager.StartTransaction())
                {
                    var blocks=(BlockTable)tr.GetObject(doc.Database.BlockTableId,OpenMode.ForRead);
                    var space=(BlockTableRecord)tr.GetObject(blocks[BlockTableRecord.ModelSpace],OpenMode.ForRead);
                    var service=new ReferenceAssemblyDrawingService();
                    foreach(ObjectId id in space)
                    {
                        ReferenceAssembly3D model;if(!service.TryRead(id,tr,out model))continue;
                        undoId=id;undoDefinition=((BlockReference)tr.GetObject(id,OpenMode.ForRead)).BlockTableRecord;
                        undoModel=model.Serialize();model.Pipe.SetInclination(-19.5,true);service.Update(doc.Database,tr,id,model);tr.Commit();
                        doc.Editor.WriteMessage("\nREFERENCE3D_UNDO_CHANGE_OK");return;
                    }
                    throw new InvalidOperationException("No hay un modelo para la prueba U.");
                }
            }
            catch(System.Exception ex){doc.Editor.WriteMessage("\nREFERENCE3D_FAILED "+ex);}
        }
        [CommandMethod("AISPELLREFERENCE3DUNDOVERIFY")]
        public void VerifyUndo()
        {
            var doc=AcadApplication.DocumentManager.MdiActiveDocument;
            try
            {
                using(var tr=doc.Database.TransactionManager.StartOpenCloseTransaction())
                {
                    ReferenceAssembly3D model;
                    Require(new ReferenceAssemblyDrawingService().TryRead(undoId,tr,out model) && model.Serialize()==undoModel,
                        "Una U no restauró los parámetros.");
                    Require(((BlockReference)tr.GetObject(undoId,OpenMode.ForRead)).BlockTableRecord==undoDefinition,
                        "Una U no restauró la geometría original.");
                }
                doc.Editor.WriteMessage("\nREFERENCE3D_SINGLE_UNDO_OK");
            }
            catch(System.Exception ex){doc.Editor.WriteMessage("\nREFERENCE3D_FAILED "+ex);}
        }
        private static void TestGeneralProfiles()
        {
            var ring=new ReferenceContour3D {X=new[]{1.0,0,-1.0,0},Y=new[]{0.0,1,0,-1.0},Bulges=Enumerable.Repeat(Math.Tan(Math.PI/8),4).ToArray()};
            var hole=new ReferenceContour3D {X=new[]{.5,0,-.5,0},Y=new[]{0.0,.5,0,-.5},Bulges=Enumerable.Repeat(Math.Tan(Math.PI/8),4).ToArray(),Hole=true};
            var profile=new ReferencePart3D {Name="Anillo con FRONT inclinado",Bottom=0,Height=1,Top={ring,hole},
                Front={new ReferenceContour3D{X=new[]{-1.0,1,1,-1.0},Y=new[]{0.0,0,.5,1}}}};
            using(var solid=ReferenceAssemblyDrawingService.BuildPart(profile))
                Require(Math.Abs(solid.MassProperties.Volume-Math.PI*.75*.75)<1e-6,"Arcos, vacío circular o altura inclinada perdidos.");
            // L footprint must retain its recess when constrained by a matching elevation.
            var l=new ReferencePart3D {Name="L",Bottom=0,Height=2,Top={new ReferenceContour3D{X=new[]{0.0,2,2,1,1,0},Y=new[]{0.0,0,1,1,2,2}}},
                Front={ReferenceProfileReader.Rectangle(0,0,2,2)}};
            using(var solid=ReferenceAssemblyDrawingService.BuildPart(l)) Require(Math.Abs(solid.MassProperties.Volume-6)<1e-8,"La planta L se convirtió en una caja.");
            hole.Hole=false;
            var paired=PairedProfile3DAnalyzer.Analyze(new System.Collections.Generic.List<MaterialReferenceProfile>{
                new MaterialReferenceProfile{Contour=ring},new MaterialReferenceProfile{Contour=hole}},
                new System.Collections.Generic.List<MaterialReferenceProfile>{new MaterialReferenceProfile{
                    Contour=new ReferenceContour3D{X=new[]{-1.0,1,1,-1.0},Y=new[]{0.0,0,.5,1}}}},-1,-1,-1,0);
            using(var solid=ReferenceAssemblyDrawingService.BuildPart(paired.Parts[0]))
                Require(Math.Abs(solid.MassProperties.Volume-Math.PI*.75*.75)<1e-6,"No se relacionaron correctamente los perfiles seleccionados.");
        }
        private static void Render(ReferenceAssembly3D model,SelectedFigureSnapshot top,SelectedFigureSnapshot front,string path)
        {
            var window=new CivilSpellAI.UI.ReferenceAssembly3DWindow(model,top,front);
            var content=(System.Windows.FrameworkElement)window.Content;
            try
            {
                RenderContent(content,path);
                TestHighlight(window,model,Path.ChangeExtension(path,".highlight.png"));
                TestInclination(window,model,Path.ChangeExtension(path,".inclination.png"));
            }
            finally{window.Close();}
        }
        private static IEnumerable<System.Windows.DependencyObject> Controls(System.Windows.DependencyObject root)
        {
            yield return root;
            foreach(var child in System.Windows.LogicalTreeHelper.GetChildren(root).OfType<System.Windows.DependencyObject>())
                foreach(var descendant in Controls(child))yield return descendant;
        }
        private static T WindowField<T>(object window,string name)
        {
            return (T)window.GetType().GetField(name,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(window);
        }
        private static void Focus(System.Windows.Controls.TextBox input)
        {
            input.RaiseEvent(new System.Windows.Input.KeyboardFocusChangedEventArgs(System.Windows.Input.Keyboard.PrimaryDevice,0,null,input)
                {RoutedEvent=System.Windows.Input.Keyboard.GotKeyboardFocusEvent});
        }
        private static void TestHighlight(CivilSpellAI.UI.ReferenceAssembly3DWindow window,ReferenceAssembly3D original,string path)
        {
            var content=(System.Windows.FrameworkElement)window.Content;
            var controls=Controls(content).ToList();
            var inputs=controls.OfType<System.Windows.Controls.TextBox>().ToList();
            int wallIndex=original.Parts.FindIndex(part=>part.Name.StartsWith("Mampostería: laterales"));
            string wallField=original.Parts[wallIndex].Name+" · Cota inferior";
            var wall=inputs.Single(input=>System.Windows.Automation.AutomationProperties.GetName(input)==wallField);
            var meshes=WindowField<List<System.Windows.Media.Media3D.GeometryModel3D>>(window,"componentMeshes");
            var materials=meshes.Select(mesh=>mesh.Material).ToArray();
            var geometries=meshes.Select(mesh=>mesh.Geometry).ToArray();
            var preview=WindowField<CivilSpellAI.UI.Figure3DPreview>(window,"preview");
            var viewport=WindowField<System.Windows.Controls.Viewport3D>(preview,"viewport");
            var camera=viewport.Camera;
            var draft=WindowField<ReferenceAssembly3D>(window,"draft");
            Require(draft.Serialize()==original.Serialize(),"Abrir la vista previa redondeó parámetros sin editarlos.");
            Focus(wall);
            Require(meshes.Where((mesh,index)=>!object.ReferenceEquals(mesh.Material,materials[index])).Count()==1 &&
                !object.ReferenceEquals(meshes[wallIndex].Material,materials[wallIndex]),"El campo de laterales resalta otra pieza o piezas adicionales.");
            Require(meshes.Select((mesh,index)=>object.ReferenceEquals(mesh.Geometry,geometries[index])).All(same=>same) &&
                object.ReferenceEquals(camera,viewport.Camera) && draft.Serialize()==original.Serialize(),
                "Seleccionar un campo cambió parámetros, geometría o cámara.");
            Require(WindowField<System.Windows.Controls.TextBlock>(window,"selection").Text.Contains(wallField),
                "Falta el nombre del componente y del campo seleccionado.");
            RenderContent(content,path);
            string oldText=wall.Text;wall.Text=(original.Parts[wallIndex].Bottom+.02).ToString("R",System.Globalization.CultureInfo.InvariantCulture);
            wall.RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.UIElement.LostFocusEvent));
            Require(Math.Abs(meshes[wallIndex].Bounds.Z-original.Parts[wallIndex].Bottom-.02)<1e-7 &&
                meshes[wallIndex].Material is System.Windows.Media.Media3D.MaterialGroup,
                "Editar la cota no actualizó los laterales o perdió su resaltado.");
            Require(draft.Parts.Where((part,index)=>index!=wallIndex).Zip(original.Parts.Where((part,index)=>index!=wallIndex),
                (part,before)=>part.Bottom==before.Bottom && part.Height==before.Height).All(same=>same) &&
                draft.Pipe.X1==original.Pipe.X1 && draft.Pipe.X2==original.Pipe.X2,"Editar un componente modificó cotas ajenas.");
            wall.Text=oldText;wall.RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.UIElement.LostFocusEvent));
            var pipe=inputs.Single(input=>System.Windows.Automation.AutomationProperties.GetName(input)=="Tubería · Diámetro exterior");
            Focus(pipe);
            Require(meshes.Last().Material is System.Windows.Media.Media3D.MaterialGroup &&
                meshes.Take(meshes.Count-1).All(mesh=>mesh.Material is System.Windows.Media.Media3D.DiffuseMaterial),"Seleccionar tubería no retiró el resaltado previo.");
            var paired=original.Parts.FindIndex(part=>part.Front.Count>0);
            if(paired<0)paired=0;
            controls.OfType<System.Windows.Controls.Button>().Single(button=>System.Windows.Automation.AutomationProperties.GetName(button)==original.Parts[paired].Name)
                .RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
            Require(meshes[paired].Material is System.Windows.Media.Media3D.MaterialGroup,"No se puede identificar una pieza sin campos editables.");
            var pipeY=inputs.Single(input=>System.Windows.Automation.AutomationProperties.GetName(input)=="Tubería · Y final");
            string previousY=pipeY.Text;
            Focus(pipeY);pipeY.Text=(original.Pipe.Y2+.02).ToString("R",System.Globalization.CultureInfo.InvariantCulture);
            pipeY.RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.UIElement.LostFocusEvent));
            Require(Math.Abs(draft.Pipe.Y2-original.Pipe.Y2-.02)<1e-7 && meshes.Last().Material is System.Windows.Media.Media3D.MaterialGroup,
                "El recorrido Y no se puede editar o perdió el resaltado.");
            pipeY.Text=previousY;pipeY.RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.UIElement.LostFocusEvent));
            controls.OfType<System.Windows.Controls.Button>().Single(button=>System.Windows.Automation.AutomationProperties.GetName(button)=="Quitar resaltado")
                .RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
            Require(meshes.All(mesh=>mesh.Material is System.Windows.Media.Media3D.DiffuseMaterial) && draft.Serialize()==original.Serialize(),
                "Quitar el resaltado alteró el modelo o no recuperó los materiales.");
            AcadApplication.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\nREFERENCE3D_HIGHLIGHT_OK");
        }
        private static void TestInclination(CivilSpellAI.UI.ReferenceAssembly3DWindow window,ReferenceAssembly3D original,string path)
        {
            var controls=Controls((System.Windows.FrameworkElement)window.Content).ToList();
            Func<string,System.Windows.Controls.TextBox> input=label=>controls.OfType<System.Windows.Controls.TextBox>()
                .Single(box=>System.Windows.Automation.AutomationProperties.GetName(box)=="Tubería · "+label);
            var angle=input("Inclinación (°)");var start=input("Cota inicial");var end=input("Cota final");
            var anchor=controls.OfType<System.Windows.Controls.ComboBox>().Single();
            var draft=WindowField<ReferenceAssembly3D>(window,"draft");var pipe=draft.Pipe;
            Action refresh=()=>angle.RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.UIElement.LostFocusEvent));
            double span=Math.Sqrt(Math.Pow(pipe.X2-pipe.X1,2)+Math.Pow(pipe.Y2-pipe.Y1,2));
            string startText=start.Text,endText=end.Text;
            anchor.SelectedIndex=0;Focus(angle);angle.Text="22";refresh();
            Require(WindowField<System.Windows.Controls.Button>(window,"accept").IsEnabled &&
                pipe.Z1==original.Pipe.Z1 && Math.Abs(pipe.Z2-pipe.Z1-span*Math.Tan(22*Math.PI/180))<1e-10,
                "El ángulo positivo no conserva la cota inicial o no actualiza la final.");
            Require(Math.Abs(double.Parse(end.Text,System.Globalization.CultureInfo.InvariantCulture)-pipe.Z2)<1e-10,
                "La cota visible no sigue el ángulo.");
            var meshes=WindowField<List<System.Windows.Media.Media3D.GeometryModel3D>>(window,"componentMeshes");
            Require(meshes.Last().Material is System.Windows.Media.Media3D.MaterialGroup,"El ángulo no resalta la tubería.");
            start.Text=startText;end.Text=endText;refresh();
            double final=pipe.Z2;anchor.SelectedIndex=1;angle.Text="-19,5";refresh();
            Require(WindowField<System.Windows.Controls.Button>(window,"accept").IsEnabled && pipe.Z2==final &&
                Math.Abs(pipe.InclinationDegrees+19.5)<1e-10,"El ángulo negativo/coma decimal no conserva la cota final.");
            Require(pipe.X1==original.Pipe.X1 && pipe.Y1==original.Pipe.Y1 && pipe.X2==original.Pipe.X2 && pipe.Y2==original.Pipe.Y2 &&
                pipe.Diameter==original.Pipe.Diameter && pipe.Wall==original.Pipe.Wall,"Inclinar alteró la planta o el diámetro/espesor.");
            var unchanged=original.Copy();unchanged.Pipe.Z1=pipe.Z1;unchanged.Pipe.Z2=pipe.Z2;
            Require(unchanged.Serialize()==draft.Serialize(),"Inclinar cambió otro componente.");
            string tilted=draft.Serialize();refresh();
            Require(draft.Serialize()==tilted,"Refrescar redondeó o acumuló la inclinación.");
            var hollow=draft.Copy();hollow.Pipe.Wall=.004;
            var bodies=new ReferenceAssemblyDrawingService().Build(hollow);
            try
            {
                double length=new Point3d(pipe.X1,pipe.Y1,pipe.Z1).DistanceTo(new Point3d(pipe.X2,pipe.Y2,pipe.Z2));
                double radius=pipe.Diameter/2;
                Require(bodies.Last() is Solid3d && Math.Abs(((Solid3d)bodies.Last()).MassProperties.Volume-
                    Math.PI*(radius*radius-Math.Pow(radius-.004,2))*length)<1e-8,"Tubo inclinado hueco incorrecto.");
            }
            finally{foreach(var body in bodies)body.Dispose();}
            angle.BringIntoView();
            RenderContent((System.Windows.FrameworkElement)window.Content,path);
            foreach(string invalid in new[]{"90","-90","NaN","Infinity","texto"})
            {
                angle.Text=invalid;refresh();
                Require(!WindowField<System.Windows.Controls.Button>(window,"accept").IsEnabled && draft.Serialize()==tilted,
                    "Un ángulo inválido fue aceptado o cambió las cotas.");
            }
            angle.Text="0";refresh();
            Require(pipe.Z1==pipe.Z2 && WindowField<System.Windows.Controls.Button>(window,"accept").IsEnabled,
                "0° no deja el tubo horizontal o no recupera la edición.");
            start.Text=startText;end.Text=endText;refresh();
            Require(draft.Serialize()==original.Serialize() && Math.Abs(double.Parse(angle.Text,System.Globalization.CultureInfo.InvariantCulture)-
                original.Pipe.InclinationDegrees)<1e-6,"Editar cotas no sincroniza el ángulo o no restaura el modelo.");
            var vertical=new ReferencePipe3D{X1=0,Y1=0,X2=0,Y2=0,Z1=0,Z2=1};bool rejected=false;
            try{vertical.SetInclination(19,false);}catch(ArgumentException){rejected=true;}
            Require(rejected && vertical.Z1==0 && vertical.Z2==1,"Un recorrido vertical aceptó una inclinación indefinida.");
            AcadApplication.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\nREFERENCE3D_INCLINATION_OK");
        }
        private static void RenderContent(System.Windows.FrameworkElement content,string path)
        {
            content.Measure(new System.Windows.Size(1140,740));content.Arrange(new System.Windows.Rect(0,0,1140,740));content.UpdateLayout();
            var bitmap=new System.Windows.Media.Imaging.RenderTargetBitmap(1140,740,96,96,System.Windows.Media.PixelFormats.Pbgra32);
            bitmap.Render(content);var encoder=new System.Windows.Media.Imaging.PngBitmapEncoder();
            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
            using(var stream=File.Create(path)) encoder.Save(stream);
        }
        private static SelectedFigureSnapshot Read(Database database,Transaction tr,double x1,double y1,double x2,double y2)
        {
            var blocks=(BlockTable)tr.GetObject(database.BlockTableId,OpenMode.ForRead);
            var space=(BlockTableRecord)tr.GetObject(blocks[BlockTableRecord.ModelSpace],OpenMode.ForRead);
            var selected=space.Cast<ObjectId>().Where(id=>{
                var entity=(Entity)tr.GetObject(id,OpenMode.ForRead);
                if(!(entity is Polyline) && !(entity is Line) && !(entity is Arc) && !(entity is Circle) && !(entity is Hatch))return false;
                var bounds=entity.GeometricExtents;
                return bounds.MinPoint.X>=x1 && bounds.MaxPoint.X<=x2 && bounds.MinPoint.Y>=y1 && bounds.MaxPoint.Y<=y2;
            }).ToArray();
            Require(selected.Length>0,"No hay referencia en la ventana de prueba.");
            SelectedFigureSnapshot view;string error;
            Require(new SelectedFigureDrawingService().TryRead(selected,tr,out view,out error),error);return view;
        }
        private static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
        private static SelectedFigureSnapshot TransformView(SelectedFigureSnapshot source,double scale,double x,double y)
        {
            var result=new SelectedFigureSnapshot {SourceIds=source.SourceIds,Origin=new Point3d(source.Origin.X*scale+x,source.Origin.Y*scale+y,0),
                Width=source.Width*scale,Height=source.Height*scale,AxisAngle=source.AxisAngle,ApproximatePreview=source.ApproximatePreview};
            result.Outlines.AddRange(source.Outlines.Select(line=>line.Select(p=>new Point2d(p.X*scale+x,p.Y*scale+y)).ToArray()));
            result.Measures=FigureMeasureLayout.Detect(result);return result;
        }
        private static List<MaterialReferenceProfile> TransformProfiles(List<MaterialReferenceProfile> profiles,double scale,double x,double y)
        {
            return profiles.Select(p=>new MaterialReferenceProfile {Pattern=p.Pattern,Contour=new ReferenceContour3D {
                X=p.Contour.X.Select(v=>v*scale+x).ToArray(),Y=p.Contour.Y.Select(v=>v*scale+y).ToArray(),
                Bulges=p.Contour.Bulges,Hole=p.Contour.Hole}}).ToList();
        }

        [DataContract]
        private sealed class ComparisonBody
        {
            [DataMember] public string Name;
            [DataMember] public double[][] Vertices;
            [DataMember] public int[] Faces;
            [DataMember] public double[] Minimum,Maximum;
            [DataMember] public double Volume;
        }
        [DataContract]
        private sealed class ComparisonData
        {
            [DataMember] public double[][][] Top,Front;
            [DataMember] public List<ComparisonBody> Reconstructed=new List<ComparisonBody>();
            [DataMember] public List<ComparisonBody> Approximate=new List<ComparisonBody>();
            [DataMember] public string ApproximateAlignment="La extrusión aproximada se desplaza Z=-0.05 para alinear la coronación de 0.85 con el concreto de 0.80; no determina espesores.";
        }
        private static void WriteComparison(Database database,Transaction tr,ReferenceAssembly3D model,
            SelectedFigureSnapshot top,SelectedFigureSnapshot front,List<MaterialReferenceProfile> frontProfiles,List<Entity> entities)
        {
            var b=top.Measures.FrameBounds;
            double tx=top.Origin.X+b[0],ty=top.Origin.Y+b[2];
            var concrete=frontProfiles.First(p=>p.Pattern=="AR-CONC" && !p.Contour.Hole);
            double fx=concrete.Contour.X.Min(),fz=concrete.Contour.Y.Min();
            var data=new ComparisonData {
                Top=top.Outlines.Select(line=>line.Select(p=>new[]{p.X-tx,p.Y-ty}).ToArray()).ToArray(),
                Front=front.Outlines.Select(line=>line.Select(p=>new[]{p.X-fx,p.Y-fz}).ToArray()).ToArray()
            };
            for(int i=0;i<entities.Count;i++) data.Reconstructed.Add(ComparisonMesh(entities[i],i<model.Parts.Count?model.Parts[i].Name:"Tubería",new Vector3d()));
            var blocks=(BlockTable)tr.GetObject(database.BlockTableId,OpenMode.ForRead);
            var space=(BlockTableRecord)tr.GetObject(blocks[BlockTableRecord.ModelSpace],OpenMode.ForRead);
            foreach(ObjectId id in space)
            {
                var solid=tr.GetObject(id,OpenMode.ForRead) as Solid3d;if(solid==null)continue;
                var bounds=solid.GeometricExtents;
                if(bounds.MinPoint.X<19 || bounds.MaxPoint.X>23 || bounds.MinPoint.Y<ty-1e-7 || bounds.MaxPoint.Y>ty+1.3+1e-7)continue;
                data.Approximate.Add(ComparisonMesh(solid,"Extrusión "+solid.Handle,new Vector3d(-tx,-ty,-.05)));
            }
            string path=Path.Combine(Path.GetDirectoryName(database.Filename),"reference-comparison.json");
            using(var stream=File.Create(path)) new DataContractJsonSerializer(typeof(ComparisonData)).WriteObject(stream,data);
        }
        private static ComparisonBody ComparisonMesh(Entity entity,string name,Vector3d offset)
        {
            var mesh=SubDMesh.GetObjectMesh(entity,new MeshFaceterData {FaceterMeshType=2,FaceterDevSurface=.0005,FaceterDevNormal=10});
            var bounds=entity.GeometricExtents;
            return new ComparisonBody {
                Name=name,Vertices=mesh.VertexArray.Cast<Point3d>().Select(p=>new[]{p.X+offset.X,p.Y+offset.Y,p.Z+offset.Z}).ToArray(),
                Faces=mesh.FaceArray.Cast<int>().ToArray(),
                Minimum=new[]{bounds.MinPoint.X+offset.X,bounds.MinPoint.Y+offset.Y,bounds.MinPoint.Z+offset.Z},
                Maximum=new[]{bounds.MaxPoint.X+offset.X,bounds.MaxPoint.Y+offset.Y,bounds.MaxPoint.Z+offset.Z},
                Volume=entity is Solid3d?((Solid3d)entity).MassProperties.Volume:0
            };
        }
    }
}
#endif
