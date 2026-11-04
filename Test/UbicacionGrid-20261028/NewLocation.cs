using System;
using System.IO;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using App = Autodesk.AutoCAD.ApplicationServices.Core.Application;

public class NewLocation
{
    const string Root = @"C:\Users\Cesar Ugaz\civil3D traductor\repos\CivilSpellAI";
    const string Source = @"Q:\Proyectos 2026\1107.001.00.16 - Pad SOTRAMI\05_Drawing\Xref\Xref_Ubicación Hidráulica_ubicacion.dwg";
    const double CX = 579257.262041458, CY = 8302264.916087537;
    static string Output { get { return Path.Combine(Root, "artifacts", "drawings", "Ubicacion_Hidraulica_100x100_1-5000.dwg"); } }
    static ObjectId Add(Entity e, BlockTableRecord space, Transaction tr)
    {
        var id = space.AppendEntity(e); tr.AddNewlyCreatedDBObject(e, true); return id;
    }
    static bool Label(Point3d p)
    {
        foreach (double x in new double[] {579100,579200,579300,579400})
            if (p.X > 775.5 + .2*(x-24-CX) && p.X < 775.5+.2*(x-8-CX) && p.Y > 528.5+.2*(8302390-CY) && p.Y < 528.5+.2*(8302506-CY)) return true;
        foreach (double y in new double[] {8302100,8302200,8302300})
            if (p.X > 775.5+.2*(579382-CX) && p.X < 775.5+.2*(579496-CX) && p.Y > 528.5+.2*(y+11-CY) && p.Y < 528.5+.2*(y+27-CY)) return true;
        return false;
    }
    static void Dashes(Point3d p, Point3d q, BlockTableRecord ps, Transaction tr)
    {
        var delta = q-p; double length=delta.Length; var direction=delta.GetNormal();
        for(double s=0;s<length;s+=3.6)
        {
            var a=p+direction*s; var b=p+direction*Math.Min(s+2.6,length);
            if(Label(a)||Label(b)||Label(a+(b-a)*.5))continue;
            Add(new Line(a,b) { Layer="CUADRICULA_100m", Color=Color.FromRgb(145,145,145), LineWeight=LineWeight.LineWeight009 },ps,tr);
        }
    }
    [CommandMethod("UGNUEVODWG",CommandFlags.Session)]
    public void Create()
    {
        var previous=HostApplicationServices.WorkingDatabase;
        try
        {
            if(!File.Exists(Source))throw new FileNotFoundException("No se encuentra la Xref",Source);
            if(File.Exists(Output))throw new InvalidOperationException("El archivo nuevo ya existe: "+Output);
            Directory.CreateDirectory(Path.GetDirectoryName(Output));
            using(var db=new Database(true,true))
            {
                HostApplicationServices.WorkingDatabase=db;
                db.Insunits=UnitsValue.Meters;
                db.TileMode=false;
                var xref=db.AttachXref(Source,"Xref_Ubicación Hidráulica_ubicacion");
                using(var tr=db.TransactionManager.StartTransaction())
                {
                    var bt=(BlockTable)tr.GetObject(db.BlockTableId,OpenMode.ForRead);
                    var ms=(BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace],OpenMode.ForWrite);
                    Add(new BlockReference(Point3d.Origin,xref),ms,tr);
                    var layouts=(DBDictionary)tr.GetObject(db.LayoutDictionaryId,OpenMode.ForRead);
                    Layout layout=null;
                    foreach(DBDictionaryEntry entry in layouts)
                    {
                        var candidate=(Layout)tr.GetObject(entry.Value,OpenMode.ForRead);
                        if(!candidate.ModelType && layout==null) { candidate.UpgradeOpen(); layout=candidate; }
                    }
                    if(layout==null)throw new InvalidOperationException("No hay espacio papel");
                    layout.LayoutName="UBICACION";
                    layout.TabOrder=1;
                    var settings=PlotSettingsValidator.Current;
                    settings.SetPlotConfigurationName(layout,"DWG To PDF.pc3","ISO_full_bleed_A1_(841.00_x_594.00_MM)");
                    settings.SetPlotPaperUnits(layout,PlotPaperUnit.Millimeters);
                    settings.SetPlotType(layout,Autodesk.AutoCAD.DatabaseServices.PlotType.Layout);
                    settings.SetUseStandardScale(layout,true);
                    settings.SetStdScaleType(layout,StdScaleType.StdScale1To1);
                    settings.SetPlotRotation(layout,PlotRotation.Degrees000);
                    layout.DrawViewportsFirst=true;
                    var ps=(BlockTableRecord)tr.GetObject(layout.BlockTableRecordId,OpenMode.ForWrite);
                    var lt=(LayerTable)tr.GetObject(db.LayerTableId,OpenMode.ForWrite);
                    var gridLayer=new LayerTableRecord {Name="CUADRICULA_100m",Color=Color.FromRgb(145,145,145),LineWeight=LineWeight.LineWeight009};
                    lt.Add(gridLayer);tr.AddNewlyCreatedDBObject(gridLayer,true);
                    var frame=new Polyline(4) {Closed=true};
                    frame.AddVertexAt(0,new Point2d(725.5,478.5),0,0,0);
                    frame.AddVertexAt(1,new Point2d(825.5,478.5),0,0,0);
                    frame.AddVertexAt(2,new Point2d(825.5,578.5),0,0,0);
                    frame.AddVertexAt(3,new Point2d(725.5,578.5),0,0,0);
                    var frameId=Add(frame,ps,tr);
                    var vp=new Viewport(); Add(vp,ps,tr);
                    vp.CenterPoint=new Point3d(775.5,528.5,0);
                    vp.Width=100;vp.Height=100;
                    vp.ViewDirection=Vector3d.ZAxis;vp.ViewTarget=Point3d.Origin;vp.TwistAngle=0;
                    vp.ViewCenter=new Point2d(CX,CY);vp.ViewHeight=500;vp.CustomScale=.2;
                    vp.NonRectClipEntityId=frameId;vp.NonRectClipOn=true;vp.On=true;vp.Locked=true;
                    foreach(double x in new double[]{579100,579200,579300,579400,579500})
                    {double px=775.5+.2*(x-CX);Dashes(new Point3d(px,478.5,0),new Point3d(px,578.5,0),ps,tr);}
                    foreach(double y in new double[]{8302100,8302200,8302300,8302400,8302500})
                    {double py=528.5+.2*(y-CY);Dashes(new Point3d(725.5,py,0),new Point3d(825.5,py,0),ps,tr);}
                    tr.Commit();
                }
                db.SaveAs(Output,DwgVersion.Current);
            }
            HostApplicationServices.WorkingDatabase=previous;
            var doc=App.DocumentManager.Open(Output,false);
            App.DocumentManager.MdiActiveDocument=doc;
            doc.SendStringToExecute("(progn (setvar \"CTAB\" \"UBICACION\") (setvar \"INSUNITS\" 6) (command \"_.PSPACE\") (command \"_.ZOOM\" \"_W\" \"720,473\" \"831,584\") (vla-save (vla-get-ActiveDocument (vlax-get-acad-object))))\n",true,false,false);
            File.WriteAllText(Path.Combine(Root,"Test","UbicacionGrid-20261028","nuevo-creado.txt"),Output);
        }
        catch(System.Exception ex){File.WriteAllText(Path.Combine(Root,"Test","UbicacionGrid-20261028","nuevo-error.txt"),ex.ToString());}
        finally{HostApplicationServices.WorkingDatabase=previous;}
    }
}
