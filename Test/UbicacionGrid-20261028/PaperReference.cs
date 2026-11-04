using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.DatabaseServices.Filters;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.PlottingServices;
using App=Autodesk.AutoCAD.ApplicationServices.Core.Application;

public class PaperReference
{
    const string Root=@"C:\Users\Cesar Ugaz\civil3D traductor\repos\CivilSpellAI";
    const string Source=@"Q:\Proyectos 2026\1107.001.00.16 - Pad SOTRAMI\05_Drawing\Xref\Xref_Ubicación Hidráulica_ubicacion.dwg";
    const double CX=579257.262041458, CY=8302264.916087537;
    static string Folder {get{return Path.Combine(Root,"artifacts","drawings");}}
    static string Target {get{return Path.Combine(Folder,"Xref_Ubicacion_Hidraulica_PARA_LAYOUT_100x100.dwg");}}
    static string Work {get{return Path.Combine(Root,"Test","UbicacionGrid-20261028");}}
    static void Log(string s){File.AppendAllText(Path.Combine(Work,"paper-reference.log"),s+Environment.NewLine);}
    static ObjectId Add(Entity e,BlockTableRecord space,Transaction tr){var id=space.AppendEntity(e);tr.AddNewlyCreatedDBObject(e,true);return id;}
    [CommandMethod("UGPAPERBUILD")]
    public void BuildAndTest()
    {
        var active=App.DocumentManager.MdiActiveDocument;
        var previous=HostApplicationServices.WorkingDatabase;
        try
        {
            Directory.CreateDirectory(Folder);
            using(var db=new Database(true,true))
            {
                HostApplicationServices.WorkingDatabase=db;
                db.Insunits=UnitsValue.Meters;db.Insbase=Point3d.Origin;
                ObjectId rootId=db.AttachXref(Source,"UBICACION_BASE"), insertId;
                var rasters=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
                using(var tr=db.TransactionManager.StartTransaction())
                {
                    foreach(ObjectId id in (BlockTable)tr.GetObject(db.BlockTableId,OpenMode.ForRead))
                    {
                        var b=(BlockTableRecord)tr.GetObject(id,OpenMode.ForRead);
                        if(!b.IsFromExternalReference)continue;
                        var child=b.GetXrefDatabase(false);if(child==null)continue;
                        using(var ct=child.TransactionManager.StartTransaction())
                        {
                            var dictId=RasterImageDef.GetImageDictionary(child);if(dictId.IsNull)continue;
                            foreach(DBDictionaryEntry item in (DBDictionary)ct.GetObject(dictId,OpenMode.ForRead))
                            {
                                var def=ct.GetObject(item.Value,OpenMode.ForRead) as RasterImageDef;if(def==null)continue;
                                var raw=def.SourceFileName;
                                var found=new[]{raw,Path.Combine(Path.GetDirectoryName(child.Filename),raw),Path.Combine(Path.GetDirectoryName(Source),Path.GetFileName(raw))}.FirstOrDefault(File.Exists);
                                if(found!=null)rasters[Path.GetFileName(raw)]=found;
                            }
                        }
                    }
                    var ms=(BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db),OpenMode.ForWrite);
                    insertId=Add(new BlockReference(Point3d.Origin,rootId),ms,tr);tr.Commit();
                }
                db.BindXrefs(new ObjectIdCollection(new[]{rootId}),false);
                double xmin=CX-250,ymin=CY-250;
                using(var tr=db.TransactionManager.StartTransaction())
                {
                    var br=(BlockReference)tr.GetObject(insertId,OpenMode.ForWrite);
                    var master=(BlockTableRecord)tr.GetObject(br.BlockTableRecord,OpenMode.ForRead);
                    // The paper grid and exact frame are supplied separately below.
                    foreach(ObjectId id in master)
                    {
                        var e=tr.GetObject(id,OpenMode.ForRead) as Entity;
                        if(e==null)continue;
                        var line=e as Line;
                        bool hide=false;
                        if(line!=null)
                        {
                            double dx=Math.Abs(line.EndPoint.X-line.StartPoint.X),dy=Math.Abs(line.EndPoint.Y-line.StartPoint.Y);
                            hide=(dx<5&&dy>450&&dy<550&&line.StartPoint.X>579000&&line.StartPoint.X<579510)
                                ||(dy<.01&&dx>450&&dx<550&&line.StartPoint.Y>8302000&&line.StartPoint.Y<8302520);
                        }
                        var poly=e as Polyline;
                        if(poly!=null&&poly.Closed&&poly.NumberOfVertices==4)
                        {
                            var bounds=poly.GeometricExtents;
                            double w=bounds.MaxPoint.X-bounds.MinPoint.X,h=bounds.MaxPoint.Y-bounds.MinPoint.Y;
                            hide=w>450&&w<550&&h>450&&h<550;
                        }
                        if(hide){e.UpgradeOpen();e.Visible=false;}
                    }
                    var eastLabels=master.Cast<ObjectId>().Select(id=>tr.GetObject(id,OpenMode.ForRead)).OfType<MText>().Where(t=>t.Text=="8 302 018 mN").OrderBy(t=>t.Location.X).ToArray();
                    for(int i=0;i<eastLabels.Length;i++){eastLabels[i].UpgradeOpen();eastLabels[i].Contents="579 "+(100+i*100).ToString()+" mE";}
                    br.CreateExtensionDictionary();
                    var ext=(DBDictionary)tr.GetObject(br.ExtensionDictionary,OpenMode.ForWrite);
                    var filters=new DBDictionary();ext.SetAt("ACAD_FILTER",filters);tr.AddNewlyCreatedDBObject(filters,true);
                    var filter=new SpatialFilter{Definition=new SpatialFilterDefinition(new Point2dCollection(new[]{new Point2d(xmin,ymin),new Point2d(xmin+500,ymin+500)}),Vector3d.ZAxis,0,0,0,true)};
                    filters.SetAt("SPATIAL",filter);tr.AddNewlyCreatedDBObject(filter,true);
                    br.TransformBy(Matrix3d.Displacement(new Vector3d(-xmin*.2,-ymin*.2,0))*Matrix3d.Scaling(.2,Point3d.Origin));
                    var ms=(BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db),OpenMode.ForWrite);
                    var frame=new Polyline(4){Closed=true};
                    frame.AddVertexAt(0,new Point2d(0,0),0,0,0);frame.AddVertexAt(1,new Point2d(100,0),0,0,0);
                    frame.AddVertexAt(2,new Point2d(100,100),0,0,0);frame.AddVertexAt(3,new Point2d(0,100),0,0,0);Add(frame,ms,tr);
                    var dictId=RasterImageDef.GetImageDictionary(db);
                    if(!dictId.IsNull)foreach(DBDictionaryEntry item in (DBDictionary)tr.GetObject(dictId,OpenMode.ForRead))
                    {
                        var def=tr.GetObject(item.Value,OpenMode.ForWrite) as RasterImageDef;if(def==null)continue;
                        string path;if(rasters.TryGetValue(Path.GetFileName(def.SourceFileName),out path))def.SourceFileName=path;
                        if(!File.Exists(def.SourceFileName))throw new FileNotFoundException("Missing raster",def.SourceFileName);
                        try{def.Load();}catch(System.Exception ex){Log("Image load: "+ex.Message);}
                        Log("Raster: "+def.SourceFileName);
                    }
                    int refs=((BlockTable)tr.GetObject(db.BlockTableId,OpenMode.ForRead)).Cast<ObjectId>().Select(id=>(BlockTableRecord)tr.GetObject(id,OpenMode.ForRead)).Count(b=>b.IsFromExternalReference);
                    if(refs!=0)throw new InvalidOperationException("Unbound DWG references: "+refs);
                    tr.Commit();
                }
                // Reuse the verified 100 m dashed grid, projected to local paper coordinates.
                using(var src=new Database(false,true))
                {
                    src.ReadDwgFile(Path.Combine(Folder,"Ubicacion_Hidraulica_100x100_1-5000.dwg"),FileOpenMode.OpenForReadAndAllShare,true,null);
                    src.CloseInput(true);var ids=new ObjectIdCollection();
                    using(var st=src.TransactionManager.StartTransaction())
                    {
                        foreach(DBDictionaryEntry entry in (DBDictionary)st.GetObject(src.LayoutDictionaryId,OpenMode.ForRead))
                        {
                            var layout=(Layout)st.GetObject(entry.Value,OpenMode.ForRead);if(layout.ModelType||layout.LayoutName!="UBICACION")continue;
                            foreach(ObjectId id in (BlockTableRecord)st.GetObject(layout.BlockTableRecordId,OpenMode.ForRead))
                            {var ent=st.GetObject(id,OpenMode.ForRead) as Entity;if(ent!=null&&ent.Layer=="CUADRICULA_100m")ids.Add(id);}
                        }
                    }
                    var map=new IdMapping();src.WblockCloneObjects(ids,SymbolUtilityServices.GetBlockModelSpaceId(db),map,DuplicateRecordCloning.MangleName,false);
                    using(var tr=db.TransactionManager.StartTransaction())
                    {foreach(IdPair pair in map)if(pair.IsPrimary){var e=tr.GetObject(pair.Value,OpenMode.ForWrite) as Entity;if(e!=null)e.TransformBy(Matrix3d.Displacement(new Vector3d(-725.5,-478.5,0)));}tr.Commit();}
                    Log("Grid segments: "+ids.Count);
                }
                db.TileMode=true;db.SaveAs(Target,DwgVersion.Current);
                Log("BUILT "+Target+"; origin 0,0; visible frame 100 x 100; units Meters matching project; no nested DWGs; scale to real terrain 1:5000.");
            }
            HostApplicationServices.WorkingDatabase=previous;
            Test(active.Database);
        }
        catch(System.Exception ex){File.WriteAllText(Path.Combine(Work,"paper-reference-error.txt"),ex.ToString());}
        finally{HostApplicationServices.WorkingDatabase=previous;}
    }
    static void Test(Database db)
    {
        using(var tr=db.TransactionManager.StartTransaction())
        {if(((BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db),OpenMode.ForRead)).Cast<ObjectId>().Any())throw new InvalidOperationException("Proof requires empty drawing");}
        db.Insunits=UnitsValue.Meters;db.Insbase=Point3d.Origin;
        LayoutManager.Current.CurrentLayout="Layout1";var lid=LayoutManager.Current.GetLayoutId("Layout1");
        var xref=db.AttachXref(Target,"PRUEBA_EN_PAPEL");
        using(var tr=db.TransactionManager.StartTransaction())
        {
            var layout=(Layout)tr.GetObject(lid,OpenMode.ForRead);
            var ps=(BlockTableRecord)tr.GetObject(layout.BlockTableRecordId,OpenMode.ForWrite);
            foreach(ObjectId id in ps){var vp=tr.GetObject(id,OpenMode.ForRead) as Viewport;if(vp!=null&&vp.Number>1){vp.UpgradeOpen();vp.On=false;}}
            Add(new BlockReference(new Point3d(725.5,478.5,0),xref),ps,tr);tr.Commit();
        }
        db.SaveAs(Path.Combine(Work,"Prueba_Xref_En_Layout.dwg"),DwgVersion.Current);
        Log("PROOF: Attached saved file directly to PAPER at 725.5,478.5,0; scale 1; no model viewport enabled.");
        using(var tr=db.TransactionManager.StartTransaction())
        {
            var layout=(Layout)tr.GetObject(lid,OpenMode.ForRead);
            using(var settings=new PlotSettings(false))using(var info=new PlotInfo{Layout=lid})using(var validator=new PlotInfoValidator())
            {
                settings.CopyFrom(layout);var v=PlotSettingsValidator.Current;
                v.SetPlotConfigurationName(settings,"PublishToWeb PNG.pc3",null);
                var media=v.GetCanonicalMediaNameList(settings).Cast<string>().ToArray();Log("PNG media: "+String.Join(";",media));
                string selected=media.FirstOrDefault(m=>m.Contains("1600")&&m.Contains("1280"))??media.First();v.SetCanonicalMediaName(settings,selected);
                v.SetPlotWindowArea(settings,new Extents2d(724.5,477.5,826.5,579.5));
                v.SetPlotType(settings,Autodesk.AutoCAD.DatabaseServices.PlotType.Window);
                v.SetPlotRotation(settings,PlotRotation.Degrees000);
                v.SetUseStandardScale(settings,true);v.SetStdScaleType(settings,StdScaleType.ScaleToFit);v.SetPlotCentered(settings,true);
                settings.PlotPlotStyles=false;settings.DrawViewportsFirst=true;
                info.OverrideSettings=settings;validator.MediaMatchingPolicy=MatchingPolicy.MatchEnabled;validator.Validate(info);
                using(var engine=PlotFactory.CreatePublishEngine())
                {
                    engine.BeginPlot(null,null);engine.BeginDocument(info,"Prueba Xref en papel",null,1,true,Path.Combine(Work,"Prueba_Xref_En_Layout.png"));
                    using(var page=new PlotPageInfo()){engine.BeginPage(page,info,true,null);engine.BeginGenerateGraphics(null);engine.EndGenerateGraphics(null);engine.EndPage(null);}engine.EndDocument(null);engine.EndPlot(null);
                }
            }
        }
        Log("PROOF_RENDERED");
    }
}
