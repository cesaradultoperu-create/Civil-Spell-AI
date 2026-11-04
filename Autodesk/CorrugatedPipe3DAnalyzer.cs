using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using CivilSpellAI.Geometry;

namespace CivilSpellAI.Autodesk
{
    // Read repeated transverse corrugation strokes as axis samples, rather than
    // interpreting the drafting circles and mask hatches as material sections.
    internal static class CorrugatedPipe3DAnalyzer
    {
        private sealed class Stroke { public Point2d Center; public Vector2d Axis; public double Length; }
        private sealed class View
        {
            public readonly List<Stroke> Strokes = new List<Stroke>();
            public readonly List<double> Diameters = new List<double>();
            public readonly List<Tuple<Point2d, Vector2d, double, double, ReferenceContour3D>> Caps = new List<Tuple<Point2d, Vector2d, double, double, ReferenceContour3D>>();
            public readonly List<double> BendRadii = new List<double>();
            public ReferenceContour3D CorrugationProfile;
        }
        private sealed class Run
        {
            public Vector2d Axis;
            public readonly List<Stroke> Samples = new List<Stroke>();
            public Point2d First, Last;
            public double Length { get { return First.GetDistanceTo(Last); } }
        }
        public static bool TryAnalyze(SelectedFigureSnapshot top, SelectedFigureSnapshot front, Transaction tr, out ReferenceAssembly3D model)
        {
            model = null;
            var plan = Read(top, tr);
            // Opt in only to the repeated corrugated pipe convention. Other figure
            // types continue through the existing closed-material profile reader.
            if (plan.Diameters.Count == 0 || plan.Strokes.Count < 20) return false;
            var elevation = Read(front, tr);
            if (elevation.Strokes.Count < 20)
                throw new ArgumentException("TOP contiene una tubería corrugada. Seleccione también su FRONT completo, con la pendiente y el codo.");
            double diameter = Median(plan.Diameters);
            if (elevation.Diameters.Count>0 && Math.Abs(Median(elevation.Diameters)-diameter)>diameter*.005)
                throw new ArgumentException("Los diámetros de TOP y FRONT no coinciden. Seleccione las dos vistas de la misma tubería.");
            var t = Runs(plan, diameter); var f = Runs(elevation, diameter);
            if (t.Count != 2 || f.Count != 2) throw new ArgumentException("La tubería requiere dos ejes en TOP (T) y dos tramos en FRONT (pendiente y salida). Seleccione ambas vistas completas, por separado.");
            var main = t.OrderByDescending(r => r.Length).First(); var branch = t.Single(r => r != main);
            if (Math.Abs(main.Axis.DotProduct(branch.Axis)) > .02) throw new ArgumentException("Los ramales TOP no forman una T perpendicular.");
            var tee = Intersection(main, branch);
            var start = FarEnd(main, tee);
            var xAxis = (tee-start).GetNormal(); var yAxis = new Vector2d(-xAxis.Y, xAxis.X);
            double topLength = start.GetDistanceTo(tee);
            var bend = Intersection(f[0], f[1]);
            var ends = f.Select(r => FarEnd(r, bend)).OrderBy(p => p.X).ToArray();
            var a = ends[0]; var c = ends[1];
            if (bend.X <= a.X || bend.X >= c.X) throw new ArgumentException("FRONT no define un recorrido monotónico entre los extremos de la tubería.");
            double length = c.X-a.X; double scale = length/topLength;
            if (scale < .5 || scale > 2) throw new ArgumentException("TOP y FRONT tienen escalas incompatibles. Revise que sean la misma tubería.");
            double baseZ = Math.Min(a.Y, Math.Min(bend.Y,c.Y));
            var mainRoute = new ReferencePipeRoute3D { Name = "Tubería: pendiente y codo (FRONT)",
                X = new[]{0.0,bend.X-a.X,length}, Y = new[]{0.0,0.0,0.0},
                Z = new[]{a.Y-baseZ+diameter/2,bend.Y-baseZ+diameter/2,c.Y-baseZ+diameter/2},
                Diameter = diameter, Wall = diameter/12,
                BendRadius = elevation.BendRadii.Count >= 2 ? elevation.BendRadii.OrderByDescending(v=>v).Take(2).Average() : diameter };
            // The two visible concentric bore strokes in the T indicate 250 mm
            // against its 300 mm exterior. The editable default is stated below.
            mainRoute.BendRadius = Math.Max(mainRoute.BendRadius,diameter*.55);
            ApplyProfile(mainRoute,elevation.CorrugationProfile??plan.CorrugationProfile);
            double by1 = (branch.First-tee).DotProduct(yAxis), by2 = (branch.Last-tee).DotProduct(yAxis);
            var branchRoute = new ReferencePipeRoute3D { Name = "Tubería: ramal T (TOP)",
                X = new[]{length,length}, Y = new[]{Math.Min(by1,by2),Math.Max(by1,by2)},
                Z = new[]{mainRoute.Z[2],mainRoute.Z[2]}, Diameter = diameter, Wall = mainRoute.Wall,
                CorrugationProfile=mainRoute.CorrugationProfile,CorrugationPitch=mainRoute.CorrugationPitch };
            model = new ReferenceAssembly3D { PipeRoutes = { mainRoute, branchRoute },
                TopReference = string.Join(",",top.SourceIds.Select(id=>id.Handle.ToString())),
                FrontReference = string.Join(",",front.SourceIds.Select(id=>id.Handle.ToString())) };
            foreach (var cap in plan.Caps)
            {
                var delta = cap.Item1-start;
                double x = delta.DotProduct(xAxis)*scale, y = delta.DotProduct(yAxis);
                var dir = cap.Item2;
                double dx = dir.DotProduct(xAxis), dy = dir.DotProduct(yAxis);
                // A cap on the main follows its FRONT slope. T caps follow Y.
                double dz = Math.Abs(dx)>.8 ? (a.Y-bend.Y)/(bend.X-a.X)*-dx : 0;
                var direction = new Vector3d(dx,dy,dz).GetNormal()*cap.Item3;
                double z = Math.Abs(dx)>.8 ? mainRoute.Z[0] : mainRoute.Z[2];
                model.PipeRoutes.Add(new ReferencePipeRoute3D { Name="Tapa HDPE "+(model.PipeRoutes.Count-1),
                    X=new[]{x,x+direction.X},Y=new[]{y,y+direction.Y},Z=new[]{z,z+direction.Z},Diameter=cap.Item4,Cap=true,CapProfile=cap.Item5 });
            }
            model.Notes.Add("FRONT manda: longitud horizontal del eje "+length.ToString("0.###")+"; TOP original "+topLength.ToString("0.###")+". Se ajustó TOP longitudinalmente por "+scale.ToString("0.######")+"; los anchos y ramales conservan sus medidas.");
            model.Notes.Add("Diámetro exterior detectado: "+diameter.ToString("0.###")+" unidades del dibujo (0.300 m = 300 mm). Paso del corrugado: "+mainRoute.CorrugationPitch.ToString("0.######")+". Perfil redondeado tomado del símbolo 2D y convertido en anillos sólidos.");
            model.Notes.Add("Corrugado con crestas y valles reales; las conexiones T y el codo permanecen lisos como en el detalle. Tapas con sus bandas y remate de revolución. Espesor editable medido en el valle (inicial ~3.8 mm, supuesto); confirme el espesor real. Cotas relativas a inserción.");
            model.Validate(); return true;
        }
        private static View Read(SelectedFigureSnapshot snapshot, Transaction tr)
        {
            var view = new View();
            var pose = Matrix3d.Displacement(Point3d.Origin-snapshot.Origin);
            foreach(var id in snapshot.SourceIds) Collect((Entity)tr.GetObject(id,OpenMode.ForRead),tr,pose,view,0);
            return view;
        }
        private static void Collect(Entity entity, Transaction tr, Matrix3d pose, View view, int depth)
        {
            if(depth>12) throw new ArgumentException("Demasiados bloques anidados en la tubería.");
            var block = entity as BlockReference;
            if(block!=null)
            {
                var def=(BlockTableRecord)tr.GetObject(block.BlockTableRecord,OpenMode.ForRead);
                var transform=pose*block.BlockTransform;
                if(def.Name.IndexOf("CORRUGADO",StringComparison.OrdinalIgnoreCase)>=0)
                {
                    var children=def.Cast<ObjectId>().Select(id=>tr.GetObject(id,OpenMode.ForRead) as Entity).Where(e=>e!=null).ToArray();
                    if(children.Length>0)
                    {
                        double low=children.Min(e=>e.GeometricExtents.MinPoint.Y),high=children.Max(e=>e.GeometricExtents.MaxPoint.Y);
                        view.Diameters.Add((new Point3d(0,high,0).TransformBy(transform)-new Point3d(0,low,0).TransformBy(transform)).Length);
                        if(view.CorrugationProfile==null)
                        {
                            double sx=Vector3d.XAxis.TransformBy(transform).Length,sy=Vector3d.YAxis.TransformBy(transform).Length;
                            var outline=children.OfType<Polyline>().FirstOrDefault(p=>p.NumberOfVertices>=8 &&
                                Enumerable.Range(0,p.NumberOfVertices).All(i=>p.GetPoint2dAt(i).Y>(low+high)/2));
                            if(outline!=null && Math.Abs(sx/sy-1)<.001)
                            {
                                int count=outline.NumberOfVertices;
                                while(count>8 && outline.GetPoint2dAt(count-1).GetDistanceTo(outline.GetPoint2dAt(count-2))<1e-5)count--;
                                var first=outline.GetPoint2dAt(0);
                                view.CorrugationProfile=new ReferenceContour3D {
                                    X=Enumerable.Range(0,count).Select(i=>(outline.GetPoint2dAt(i).X-first.X)*sx).ToArray(),
                                    Y=Enumerable.Range(0,count).Select(i=>(outline.GetPoint2dAt(i).Y-(low+high)/2)*sy).ToArray(),
                                    Bulges=Enumerable.Range(0,count).Select(i=>outline.GetBulgeAt(i)).ToArray() };
                            }
                        }
                    }
                }
                if(def.Name.IndexOf("TAPA",StringComparison.OrdinalIgnoreCase)>=0)
                {
                    // Symbol local +X points outwards from its pipe connection.
                    var origin=Point3d.Origin.TransformBy(transform);
                    var x=new Point3d(1,0,0).TransformBy(transform)-origin;
                    var bounds=def.Cast<ObjectId>().Select(id=>((Entity)tr.GetObject(id,OpenMode.ForRead)).GeometricExtents).ToArray();
                    double length=bounds.Max(b=>b.MaxPoint.X)-bounds.Min(b=>b.MinPoint.X);
                    double diameter=bounds.Max(b=>b.MaxPoint.Y)-bounds.Min(b=>b.MinPoint.Y);
                    var y=new Point3d(0,1,0).TransformBy(transform)-origin;
                    view.Caps.Add(Tuple.Create(new Point2d(origin.X,origin.Y),new Vector2d(x.X,x.Y).GetNormal(),length*x.Length,diameter*y.Length,CapEnvelope(def,tr)));
                }
                foreach(ObjectId id in def) Collect((Entity)tr.GetObject(id,OpenMode.ForRead),tr,transform,view,depth+1);
                return;
            }
            var line=entity as Line;
            if(line!=null && entity.Layer.IndexOf("CORRUGADO",StringComparison.OrdinalIgnoreCase)>=0)
            {
                var a=line.StartPoint.TransformBy(pose);var b=line.EndPoint.TransformBy(pose);
                var delta=new Vector2d(b.X-a.X,b.Y-a.Y);if(delta.Length<1e-8)return;
                var axis=new Vector2d(delta.Y,-delta.X).GetNormal();
                if(axis.X<-.01 || Math.Abs(axis.X)<.01 && axis.Y<0)axis=-axis;
                view.Strokes.Add(new Stroke {Center=new Point2d((a.X+b.X)/2,(a.Y+b.Y)/2),Axis=axis,Length=delta.Length});
            }
            var polyline=entity as Polyline;
            if(polyline!=null && entity.Layer.IndexOf("TUB_CONTORNO",StringComparison.OrdinalIgnoreCase)>=0)
                for(int i=0;i<polyline.NumberOfVertices-1;i++)
                    if(Math.Abs(polyline.GetBulgeAt(i))>.05 && Math.Abs(polyline.GetBulgeAt(i))<.3)
                    {
                        var arc=polyline.GetArcSegmentAt(i);
                        view.BendRadii.Add(arc.Radius);
                    }
        }
        private static List<Run> Runs(View view,double diameter)
        {
            var runs=new List<Run>();
            foreach(var sample in view.Strokes.Where(s=>s.Length>diameter*.75 && s.Length<diameter*1.05))
            {
                var run=runs.FirstOrDefault(r=>Math.Abs(Cross(r.Axis,sample.Axis))<.01 && Math.Abs(Cross(r.Axis,sample.Center-r.Samples[0].Center))<diameter*.075);
                if(run==null){run=new Run{Axis=sample.Axis};runs.Add(run);}run.Samples.Add(sample);
            }
            foreach(var run in runs)
            {
                var normal=new Vector2d(-run.Axis.Y,run.Axis.X);
                double offset=Median(run.Samples.Select(s=>(s.Center-Point2d.Origin).DotProduct(normal)));
                var distances=run.Samples.Select(s=>(s.Center-Point2d.Origin).DotProduct(run.Axis)).ToArray();
                run.First=Point2d.Origin+run.Axis*distances.Min()+normal*offset;
                run.Last=Point2d.Origin+run.Axis*distances.Max()+normal*offset;
            }
            return runs.Where(r=>r.Samples.Count>=10 && r.Length>diameter).ToList();
        }
        private static double Median(IEnumerable<double> values){var a=values.OrderBy(v=>v).ToArray();return (a[(a.Length-1)/2]+a[a.Length/2])/2;}
        private static void ApplyProfile(ReferencePipeRoute3D route,ReferenceContour3D profile)
        {
            if(profile==null)throw new ArgumentException("No se pudo leer la sección redondeada del corrugado 2D.");
            route.CorrugationProfile=profile;
            route.CorrugationPitch=(profile.X.Last()-profile.X[0])/2;
            double depth=route.Diameter/2*(1-profile.Y.Min()/profile.Y.Max());
            route.Wall=Math.Max(route.Wall-depth,route.Diameter*.005);
        }
        internal static bool UpgradeCorrugation(ReferenceAssembly3D model,Database db,Transaction tr)
        {
            if(model.PipeRoutes.Count==0 || model.PipeRoutes.Any(r=>r.CorrugationProfile!=null) || string.IsNullOrEmpty(model.TopReference))return false;
            var ids=model.TopReference.Split(',').Select(h=>db.GetObjectId(false,new Handle(Convert.ToInt64(h,16)),0)).ToArray();
            SelectedFigureSnapshot snapshot;string error;
            if(!new SelectedFigureDrawingService().TryRead(ids,tr,out snapshot,out error))return false;
            var view=Read(snapshot,tr);if(view.CorrugationProfile==null)return false;
            foreach(var route in model.PipeRoutes.Where(r=>!r.Cap))ApplyProfile(route,view.CorrugationProfile);
            int index=0;
            foreach(var route in model.PipeRoutes.Where(r=>r.Cap))
                if(index<view.Caps.Count)route.CapProfile=view.Caps[index++].Item5;
            model.Notes.RemoveAll(n=>n.Contains("envolvente") || n.Contains("D/12"));
            model.Notes.Add("Corrugado actualizado desde el perfil 2D: anillos en relieve, paso "+model.PipeRoutes[0].CorrugationPitch.ToString("0.######")+"; espesor medido en el valle. Confirme el espesor real.");
            return true;
        }
        private static ReferenceContour3D CapEnvelope(BlockTableRecord def,Transaction tr)
        {
            var segments=new List<Tuple<Point2d,Point2d>>();
            foreach(ObjectId id in def)
            {
                var entity=tr.GetObject(id,OpenMode.ForRead) as Entity;
                var line=entity as Line;var polyline=entity as Polyline;
                var points=new List<Point2d>();
                if(line!=null){points.Add(new Point2d(line.StartPoint.X,line.StartPoint.Y));points.Add(new Point2d(line.EndPoint.X,line.EndPoint.Y));}
                if(polyline!=null)
                {
                    int end=polyline.Closed?polyline.NumberOfVertices:polyline.NumberOfVertices-1;
                    for(int i=0;i<end;i++)
                    {
                        points.Add(polyline.GetPoint2dAt(i));
                        if(Math.Abs(polyline.GetBulgeAt(i))>1e-6)
                            for(int j=1;j<9;j++){var p=polyline.GetPointAtParameter(i+j/9.0);points.Add(new Point2d(p.X,p.Y));}
                    }
                    points.Add(polyline.GetPoint2dAt(polyline.Closed?0:polyline.NumberOfVertices-1));
                }
                for(int i=0;i<points.Count-1;i++)
                {
                    var a=points[i];var b=points[i+1];
                    if(a.Y<0 || b.Y<0 || Math.Abs(a.X-b.X)<1e-8)continue;
                    segments.Add(a.X<b.X?Tuple.Create(a,b):Tuple.Create(b,a));
                }
            }
            if(segments.Count==0)return null;
            var xs=segments.SelectMany(s=>new[]{s.Item1.X,s.Item2.X}).OrderBy(v=>v).Aggregate(new List<double>(),(a,v)=>{if(a.Count==0 || v-a.Last()>1e-7)a.Add(v);return a;});
            var outline=new List<Point2d>();
            for(int i=0;i<xs.Count-1;i++)
            {
                double left=xs[i],right=xs[i+1],mid=(left+right)/2;
                var piece=segments.Where(s=>s.Item1.X<=mid && s.Item2.X>=mid).OrderByDescending(s=>AtX(s,mid)).FirstOrDefault();
                if(piece==null)continue;
                foreach(var p in new[]{new Point2d(left,AtX(piece,left)),new Point2d(right,AtX(piece,right))})
                    if(outline.Count==0 || outline.Last().GetDistanceTo(p)>1e-7)outline.Add(p);
            }
            return outline.Count<3?null:new ReferenceContour3D{X=outline.Select(p=>p.X).ToArray(),Y=outline.Select(p=>p.Y).ToArray()};
        }
        private static double AtX(Tuple<Point2d,Point2d> s,double x){return s.Item1.Y+(s.Item2.Y-s.Item1.Y)*(x-s.Item1.X)/(s.Item2.X-s.Item1.X);}
        private static double Cross(Vector2d a,Vector2d b){return a.X*b.Y-a.Y*b.X;}
        private static Point2d Intersection(Run a,Run b)
        {
            double cross=Cross(a.Axis,b.Axis);
            if(Math.Abs(cross)<1e-4)throw new ArgumentException("Los tramos no definen una unión de tubería.");
            return a.First+a.Axis*(Cross(b.First-a.First,b.Axis)/cross);
        }
        private static Point2d FarEnd(Run run,Point2d junction){return run.First.GetDistanceTo(junction)>run.Last.GetDistanceTo(junction)?run.First:run.Last;}
    }
}
