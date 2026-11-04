using System;
using System.IO;
using System.Linq;
using System.Globalization;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.PlottingServices;
using App=Autodesk.AutoCAD.ApplicationServices.Core.Application;

public class FixedPaperPosition
{
    const string Root=@"C:\Users\Cesar Ugaz\civil3D traductor\repos\CivilSpellAI";
    const double X=725.878,Y=478.500;
    static string Folder {get{return Path.Combine(Root,"artifacts","drawings");}}
    static string Work {get{return Path.Combine(Root,"Test","UbicacionGrid-20261028");}}
    static string Output {get{return Path.Combine(Folder,"Xref_Ubicacion_Hidraulica_LAYOUT_POSICION_FIJA.dwg");}}
    static string Number(double v){return v.ToString("0.000",CultureInfo.InvariantCulture);}
    [CommandMethod("UGFIXEDPAPER")]
    public void Build()
    {
        var original=HostApplicationServices.WorkingDatabase;
        try
        {
            var report=new System.Text.StringBuilder();
            using(var db=new Database(false,true))
            {
                db.ReadDwgFile(Path.Combine(Folder,"Xref_Ubicacion_Hidraulica_PARA_LAYOUT_100x100.dwg"),FileOpenMode.OpenForReadAndAllShare,true,null);
                db.CloseInput(true);HostApplicationServices.WorkingDatabase=db;
                db.Insbase=Point3d.Origin;db.Insunits=UnitsValue.Meters;
                bool exactFrame=false;
                using(var tr=db.TransactionManager.StartTransaction())
                {
                    var ms=(BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db),OpenMode.ForRead);
                    foreach(ObjectId id in ms)
                    {
                        var e=tr.GetObject(id,OpenMode.ForWrite) as Entity;if(e==null)continue;
                        e.TransformBy(Matrix3d.Displacement(new Vector3d(X,Y,0)));
                        var p=e as Polyline;
                        if(p!=null&&p.Closed&&p.NumberOfVertices==4)
                        {
                            var bounds=p.GeometricExtents;
                            if(Math.Abs(bounds.MinPoint.X-X)<1e-7&&Math.Abs(bounds.MinPoint.Y-Y)<1e-7&&Math.Abs(bounds.MaxPoint.X-X-100)<1e-7&&Math.Abs(bounds.MaxPoint.Y-Y-100)<1e-7)exactFrame=true;
                        }
                    }
                    if(!exactFrame)throw new InvalidOperationException("The 100 x 100 frame did not reach the supplied coordinates.");
                    tr.Commit();
                }
                db.SaveAs(Output,DwgVersion.Current);
            }
            HostApplicationServices.WorkingDatabase=original;
            report.AppendLine("File="+Output);
            report.AppendLine("INSBASE=0,0,0; INSUNITS=Meters, matching the project");
            report.AppendLine("1 superior izquierda="+Number(X)+","+Number(Y+100)+",0.000");
            report.AppendLine("2 superior derecha="+Number(X+100)+","+Number(Y+100)+",0.000");
            report.AppendLine("3 inferior izquierda="+Number(X)+","+Number(Y)+",0.000");
            report.AppendLine("4 inferior derecha="+Number(X+100)+","+Number(Y)+",0.000");
            Test(App.DocumentManager.MdiActiveDocument.Database,report);
            File.WriteAllText(Path.Combine(Work,"posicion-fija-verificada.txt"),report.ToString());
        }
        catch(System.Exception ex){File.WriteAllText(Path.Combine(Work,"posicion-fija-error.txt"),ex.ToString());}
        finally{HostApplicationServices.WorkingDatabase=original;}
    }
    static void Test(Database db,System.Text.StringBuilder report)
    {
        using(var tr=db.TransactionManager.StartTransaction())
        {if(((BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db),OpenMode.ForRead)).Cast<ObjectId>().Any())throw new InvalidOperationException("An empty proof drawing is required.");}
        db.Insunits=UnitsValue.Meters;
        LayoutManager.Current.CurrentLayout="Layout1";
        var lid=LayoutManager.Current.GetLayoutId("Layout1");
        var xref=db.AttachXref(Output,"UBICACION_POSICION_FIJA");
        using(var tr=db.TransactionManager.StartTransaction())
        {
            var layout=(Layout)tr.GetObject(lid,OpenMode.ForRead);
            var ps=(BlockTableRecord)tr.GetObject(layout.BlockTableRecordId,OpenMode.ForWrite);
            foreach(ObjectId id in ps)
            {var vp=tr.GetObject(id,OpenMode.ForRead) as Viewport;if(vp!=null&&vp.Number>1){vp.UpgradeOpen();vp.On=false;}}
            var br=new BlockReference(Point3d.Origin,xref);
            ps.AppendEntity(br);tr.AddNewlyCreatedDBObject(br,true);
            var child=(BlockTableRecord)tr.GetObject(xref,OpenMode.ForRead);
            if(child.Origin.DistanceTo(Point3d.Origin)>1e-7)throw new InvalidOperationException("Unexpected external-reference base point.");
            bool verified=false;
            foreach(ObjectId id in child)
            {
                var p=tr.GetObject(id,OpenMode.ForRead) as Polyline;
                if(p==null||!p.Closed||p.NumberOfVertices!=4)continue;
                var a=p.GetPoint3dAt(0).TransformBy(br.BlockTransform);
                var c=p.GetPoint3dAt(2).TransformBy(br.BlockTransform);
                if(a.DistanceTo(new Point3d(X,Y,0))<1e-7&&c.DistanceTo(new Point3d(X+100,Y+100,0))<1e-7)verified=true;
            }
            if(!verified)throw new InvalidOperationException("The attached Xref does not coincide with the four requested points.");
            report.AppendLine("PROOF: saved file attached to PAPER at 0,0,0; scale 1; rotation 0; no model viewport enabled.");
            report.AppendLine("VERIFIED: attached reference frame coincides with all four supplied points.");
            tr.Commit();
        }
        db.SaveAs(Path.Combine(Work,"Prueba_Posicion_Fija_En_Layout.dwg"),DwgVersion.Current);
        using(var tr=db.TransactionManager.StartTransaction())
        {
            var layout=(Layout)tr.GetObject(lid,OpenMode.ForRead);
            using(var settings=new PlotSettings(false))using(var info=new PlotInfo{Layout=lid})using(var validator=new PlotInfoValidator())
            {
                settings.CopyFrom(layout);var v=PlotSettingsValidator.Current;
                v.SetPlotConfigurationName(settings,"PublishToWeb PNG.pc3","Sun_Hi-Res_(1600.00_x_1280.00_Pixels)");
                v.SetPlotWindowArea(settings,new Extents2d(X-1,Y-1,X+101,Y+101));
                v.SetPlotType(settings,Autodesk.AutoCAD.DatabaseServices.PlotType.Window);
                v.SetPlotRotation(settings,PlotRotation.Degrees000);
                v.SetUseStandardScale(settings,true);v.SetStdScaleType(settings,StdScaleType.ScaleToFit);v.SetPlotCentered(settings,true);
                settings.PlotPlotStyles=false;settings.DrawViewportsFirst=true;
                info.OverrideSettings=settings;validator.MediaMatchingPolicy=MatchingPolicy.MatchEnabled;validator.Validate(info);
                using(var engine=PlotFactory.CreatePublishEngine())
                {
                    engine.BeginPlot(null,null);engine.BeginDocument(info,"Prueba posicion fija",null,1,true,Path.Combine(Work,"Prueba_Posicion_Fija_En_Layout.png"));
                    using(var page=new PlotPageInfo()){engine.BeginPage(page,info,true,null);engine.BeginGenerateGraphics(null);engine.EndGenerateGraphics(null);engine.EndPage(null);}engine.EndDocument(null);engine.EndPlot(null);
                }
            }
        }
        report.AppendLine("PROOF_RENDERED");
    }
}
