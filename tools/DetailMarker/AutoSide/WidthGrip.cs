using System;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.Runtime;
using App=Autodesk.AutoCAD.ApplicationServices.Core.Application;
namespace DetalleAutoLado
{
    public sealed class DetailWidthGrip:GripData
    {
        public DetailWidthGrip(Point3d p){GripPoint=p;TriggerGrip=true;}
        public override string GetTooltip(){return "Estirar linea y desplazar los dos textos";}
        public override ReturnValue OnHotGrip(ObjectId id,Context context)
        {if(id.IsNull)return ReturnValue.Failure;WidthCommands.Pending=id;App.DocumentManager.MdiActiveDocument.SendStringToExecute("DETALLEESTIRAR6 ",true,false,false);return ReturnValue.GripHotToWarm;}
    }
    public sealed class DetailGripOverrule:GripOverrule
    {
        public override void GetGripPoints(Entity entity,GripDataCollection grips,double viewSize,int gripSize,Vector3d direction,GetGripPointsFlags flags)
        {
            base.GetGripPoints(entity,grips,viewSize,gripSize,direction,flags);var m=entity as MLeader;if(m==null||m.Database==null)return;
            using(var tr=m.Database.TransactionManager.StartOpenCloseTransaction())if(SideSwitch.IsDetail(m,tr))grips.Add(new DetailWidthGrip(SideSwitch.Endpoint(m,tr)));
        }
    }
    public sealed class WidthCommands
    {
        public static ObjectId Pending=ObjectId.Null;
        public static void Resize(Database db,ObjectId id,double width)
        {using(var tr=db.TransactionManager.StartTransaction()){var m=(MLeader)tr.GetObject(id,OpenMode.ForWrite);if(!SideSwitch.IsDetail(m,tr))return;SideSwitch.SetMinimum(m,tr,width);SideSwitch.Update(m,tr);tr.Commit();}}
        [CommandMethod("DETALLEESTIRAR6")]
        public void Stretch()
        {
            var doc=App.DocumentManager.MdiActiveDocument;var db=doc.Database;var ed=doc.Editor;var id=Pending;Pending=ObjectId.Null;
            if(id.IsNull||!id.IsValid||id.IsErased||id.Database!=db)
            {
                var options=new PromptEntityOptions("\nSelecciona el detalle para estirar su linea: ");options.SetRejectMessage("\nSelecciona una directriz de detalle.");options.AddAllowedClass(typeof(MLeader),true);
                var selection=ed.GetEntity(options);if(selection.Status!=PromptStatus.OK)return;id=selection.ObjectId;
            }
            WidthJig jig;
            using(var tr=db.TransactionManager.StartTransaction())
            {
                var m=(MLeader)tr.GetObject(id,OpenMode.ForRead);if(!SideSwitch.IsDetail(m,tr)){ed.WriteMessage("\nEste simbolo no pertenece a la version adaptable.");return;}
                var b=(BlockTableRecord)tr.GetObject(m.BlockContentId,OpenMode.ForRead);jig=new WidthJig(SideSwitch.Transform(m),SideSwitch.IsRight(b.Name),SideSwitch.Width(b,tr));tr.Abort();
            }
            if(ed.Drag(jig).Status!=PromptStatus.OK)return;Resize(db,id,jig.Width);ed.Regen();
        }
    }
    sealed class WidthJig:DrawJig
    {
        readonly Matrix3d transform;readonly bool right;readonly Point3d start;public double Width {get;private set;}
        public WidthJig(Matrix3d matrix,bool side,double width){transform=matrix;right=side;Width=width;start=new Point3d(right?width:-width,0,0).TransformBy(transform);}
        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            var options=new JigPromptPointOptions("\nNuevo extremo de la linea: "){BasePoint=start,UseBasePoint=true};var point=prompts.AcquirePoint(options);if(point.Status!=PromptStatus.OK)return SamplerStatus.Cancel;
            double x=point.Value.TransformBy(transform.Inverse()).X;double width=Math.Max(12,right?x:-x);if(Math.Abs(width-Width)<1e-5)return SamplerStatus.NoChange;Width=width;return SamplerStatus.OK;
        }
        protected override bool WorldDraw(WorldDraw draw)
        {draw.SubEntityTraits.Color=4;var points=new Point3dCollection(new[]{Point3d.Origin.TransformBy(transform),new Point3d(right?Width:-Width,0,0).TransformBy(transform)});draw.Geometry.Polyline(points,Vector3d.ZAxis,IntPtr.Zero);return true;}
    }
}

