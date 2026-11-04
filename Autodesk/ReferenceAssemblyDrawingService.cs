using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using CivilSpellAI.Geometry;
using AcadSurface = Autodesk.AutoCAD.DatabaseServices.Surface;

namespace CivilSpellAI.Autodesk
{
    internal sealed class ReferenceAssemblyDrawingService
    {
        private const string Record = "CIVILSPELLAI_REFERENCIAS_3D";

        // The preview and committed drawing share these same native entities.
        public List<Entity> Build(ReferenceAssembly3D model)
        {
            model.Validate();
            var entities = new List<Entity>();
            try
            {
                foreach (var part in model.Parts)
                {
                    var body = BuildPart(part);
                    entities.Add(body);
                    if (part.Name == "Concreto: caja")
                    {
                        foreach (var cut in model.Cuts)
                            using (var cutter = BuildPart(cut)) body.BooleanOperation(BooleanOperationType.BoolSubtract, cutter);
                        if (model.CutPipeOpening && model.Pipe != null)
                            using (var cutter = Cylinder(model.Pipe, model.Pipe.Diameter / 2, true))
                                body.BooleanOperation(BooleanOperationType.BoolSubtract, cutter);
                    }
                    body.ColorIndex = part.Color;
                }
                if (model.Pipe != null)
                {
                    var p = model.Pipe;
                    Entity pipe;
                    if (p.Wall > 0)
                    {
                        var outer = Cylinder(p, p.Diameter / 2, false);
                        try
                        {
                            using (var inner = Cylinder(p, p.Diameter / 2 - p.Wall, true))
                                outer.BooleanOperation(BooleanOperationType.BoolSubtract, inner);
                            pipe = outer;
                        }
                        catch { outer.Dispose(); throw; }
                    }
                    else
                    {
                        Vector3d direction = End(p) - Start(p);
                        using (var circle = new Circle(Start(p), direction.GetNormal(), p.Diameter / 2))
                        using (var profile = new Profile3d(circle))
                            pipe = AcadSurface.CreateExtrudedSurface(profile, direction, new SweepOptions());
                    }
                    pipe.ColorIndex = 4; entities.Add(pipe);
                }
                foreach (var route in model.PipeRoutes)
                {
                    var body = BuildRoute(route);
                    try
                    {
                        // At a T, each pipe's bore cuts the other pipe's wall.
                        // Otherwise two independently hollow tubes still obstruct
                        // one another where their annular walls cross.
                        if (!route.Cap)
                            foreach (var other in model.PipeRoutes.Where(r => !r.Cap && r != route))
                                using (var bore = BuildRoute(new ReferencePipeRoute3D { X=other.X, Y=other.Y, Z=other.Z,
                                    Diameter=2*BoreRadius(other), BendRadius=other.BendRadius, Cap=true }))
                                    body.BooleanOperation(BooleanOperationType.BoolSubtract,bore);
                        body.ColorIndex = route.Cap ? 2 : 4;
                        entities.Add(body); body=null;
                    }
                    finally { if(body!=null)body.Dispose(); }
                }
                return entities;
            }
            catch { foreach (var entity in entities) entity.Dispose(); throw; }
        }

        internal static Solid3d BuildRoute(ReferencePipeRoute3D route)
        {
            if (route.CorrugationProfile != null && !route.Cap) return CorrugatedPipeSolidBuilder.Build(route);
            if (route.CapProfile != null && route.Cap) return CorrugatedPipeSolidBuilder.BuildCap(route);
            using (var path = RouteCurve(route))
            {
                var start = path.StartPoint;
                var normal = path.GetFirstDerivative(path.StartParam).GetNormal();
                using (var outerCircle = new Circle(start, normal, route.Diameter / 2))
                using (var curves = new DBObjectCollection { outerCircle })
                using (var regions = Region.CreateFromCurves(curves))
                {
                    var solid = new Solid3d();
                    try
                    {
                        var builder = new SweepOptionsBuilder { Align = SweepOptionsAlignOption.AlignSweepEntityToPath, BasePoint = start };
                        solid.CreateSweptSolid((Region)regions[0], path, builder.ToSweepOptions());
                        if (!route.Cap)
                        {
                            using (var innerCircle = new Circle(start, normal, BoreRadius(route)))
                            using (var innerCurves = new DBObjectCollection { innerCircle })
                            using (var innerRegions = Region.CreateFromCurves(innerCurves))
                            using (var inner = new Solid3d())
                            {
                                try
                                {
                                    inner.CreateSweptSolid((Region)innerRegions[0], path, builder.ToSweepOptions());
                                    solid.BooleanOperation(BooleanOperationType.BoolSubtract, inner);
                                }
                                finally { foreach (DBObject r in innerRegions) r.Dispose(); }
                            }
                        }
                        return solid;
                    }
                    catch { solid.Dispose(); throw; }
                    finally { foreach (DBObject r in regions) r.Dispose(); }
                }
            }
        }

        internal static double BoreRadius(ReferencePipeRoute3D route)
        {
            return route.CorrugationProfile == null ? route.Diameter/2-route.Wall :
                route.Diameter/2*route.CorrugationProfile.Y.Min()/route.CorrugationProfile.Y.Max()-route.Wall;
        }

        internal static Solid3d SweptRoute(ReferencePipeRoute3D route, double radius)
        {
            return BuildRoute(new ReferencePipeRoute3D { X=route.X,Y=route.Y,Z=route.Z,Diameter=radius*2,BendRadius=route.BendRadius,Cap=true });
        }

        internal static Curve RouteCurve(ReferencePipeRoute3D route)
        {
            var points = Enumerable.Range(0, route.X.Length).Select(i => new Point3d(route.X[i], route.Y[i], route.Z[i])).ToArray();
            if (points.Length == 2) return new Line(points[0], points[1]);
            var a = points[1]-points[0]; var b = points[2]-points[1];
            var x = a.GetNormal(); var normal = x.CrossProduct(b.GetNormal());
            if (normal.Length < 1e-7) return new Line(points[0], points[2]);
            normal = normal.GetNormal(); var y = normal.CrossProduct(x);
            double angle = x.GetAngleTo(b.GetNormal());
            if (route.BendRadius <= route.Diameter / 2)
                throw new ArgumentException("El radio del codo debe superar la mitad del diámetro exterior.");
            double trim = route.BendRadius * Math.Tan(angle / 2);
            if (trim >= Math.Min(a.Length, b.Length)) throw new ArgumentException("El radio del codo no cabe entre los tramos.");
            var before = points[1] - x * trim; var after = points[1] + b.GetNormal() * trim;
            var polyline = new Polyline();
            try
            {
                var vertices = new[] { points[0], before, after, points[2] };
                for (int i = 0; i < vertices.Length; i++)
                {
                    var delta = vertices[i] - points[0];
                    polyline.AddVertexAt(i, new Point2d(delta.DotProduct(x), delta.DotProduct(y)), i == 1 ? Math.Tan(angle/4) : 0, 0, 0);
                }
                polyline.TransformBy(Matrix3d.AlignCoordinateSystem(Point3d.Origin, Vector3d.XAxis, Vector3d.YAxis, Vector3d.ZAxis, points[0], x, y, normal));
                return polyline;
            }
            catch { polyline.Dispose(); throw; }
        }

        internal static Solid3d BuildPart(ReferencePart3D part)
        {
            var solid = ExtrudeContours(part.Top, Matrix3d.Displacement(new Vector3d(0, 0, part.Bottom)), new Vector3d(0, 0, part.Height));
            try
            {
                if (part.Front.Count > 0)
                {
                    var bounds = solid.GeometricExtents;
                    using (var limit = ExtrudeContours(part.Front,Matrix3d.Displacement(new Vector3d(0, bounds.MinPoint.Y - .01, 0)) *
                        Matrix3d.Rotation(Math.PI / 2, Vector3d.XAxis, Point3d.Origin),new Vector3d(0, bounds.MaxPoint.Y - bounds.MinPoint.Y + .02, 0)))
                        solid.BooleanOperation(BooleanOperationType.BoolIntersect, limit);
                    if (solid.MassProperties.Volume < 1e-10) throw new ArgumentException("TOP y FRONT no coinciden: " + part.Name);
                }
                return solid;
            }
            catch { solid.Dispose(); throw; }
        }
        private static Solid3d ExtrudeContours(List<ReferenceContour3D> contours, Matrix3d pose, Vector3d direction)
        {
            Solid3d result=null;
            try
            {
                foreach(var outer in contours.Where(c=>!c.Hole))
                {
                    using(var region=CreateRegion(new[]{outer}.Concat(contours.Where(c=>c.Hole))))
                    {
                        region.TransformBy(pose);
                        var next=new Solid3d();
                        try
                        {
                            next.CreateExtrudedSolid(region,direction,new SweepOptions());
                            if(result==null){result=next;next=null;}
                            else result.BooleanOperation(BooleanOperationType.BoolUnite,next);
                        }
                        finally {if(next!=null) next.Dispose();}
                    }
                }
                if(result==null)throw new ArgumentException("Falta el contorno exterior del componente.");
                return result;
            }
            catch {if(result!=null)result.Dispose();throw;}
        }
        internal static Region CreateRegion(IEnumerable<ReferenceContour3D> contours)
        {
            Region result = null;
            try
            {
                // Outer components are united first; explicitly selected holes are then removed.
                foreach (var contour in contours.OrderBy(c => c.Hole))
                {
                    using (var polyline = new Polyline())
                    {
                        for (int i = 0; i < contour.X.Length; i++)
                            polyline.AddVertexAt(i, new Point2d(contour.X[i], contour.Y[i]), contour.Bulges == null ? 0 : contour.Bulges[i], 0, 0);
                        polyline.Closed = true;
                        double signedArea=0;
                        for(int i=0;i<contour.X.Length;i++)
                        {
                            int j=(i+1)%contour.X.Length;
                            signedArea+=(contour.X[i]*contour.Y[j]-contour.X[j]*contour.Y[i])/2;
                            double bulge=contour.Bulges==null?0:contour.Bulges[i];
                            if(Math.Abs(bulge)>1e-10)
                            {
                                double theta=4*Math.Atan(bulge),chord=Math.Sqrt(Math.Pow(contour.X[j]-contour.X[i],2)+Math.Pow(contour.Y[j]-contour.Y[i],2));
                                double radius=chord/(2*Math.Sin(theta/2));signedArea+=radius*radius*(theta-Math.Sin(theta))/2;
                            }
                        }
                        if(signedArea<0) polyline.ReverseCurve();
                        using (var curves = new DBObjectCollection { polyline })
                        using (var regions = Region.CreateFromCurves(curves))
                        {
                            try
                            {
                                if (regions.Count != 1) throw new ArgumentException("El perfil debe ser cerrado, plano y sin cruces.");
                                var next = (Region)regions[0];
                                if (result == null)
                                {
                                    if (contour.Hole) throw new ArgumentException("Falta el contorno exterior del componente.");
                                    result = (Region)next.Clone();
                                }
                                else result.BooleanOperation(contour.Hole ? BooleanOperationType.BoolSubtract : BooleanOperationType.BoolUnite, next);
                            }
                            finally { foreach (DBObject region in regions) region.Dispose(); }
                        }
                    }
                }
                if (result == null || result.Area <= 1e-10) throw new ArgumentException("El perfil no define material.");
                return result;
            }
            catch { if (result != null) result.Dispose(); throw; }
        }
        internal static Solid3d Cylinder(ReferencePipe3D p, double radius, bool extend)
        {
            Vector3d direction = End(p) - Start(p);
            Vector3d extra = direction.GetNormal() * (extend ? p.Diameter : 0);
            using (var circle = new Circle(Start(p) - extra, direction.GetNormal(), radius))
            using (var curves = new DBObjectCollection { circle })
            using (var regions = Region.CreateFromCurves(curves))
            {
                var solid = new Solid3d();
                try { solid.CreateExtrudedSolid((Region)regions[0], direction + extra * 2, new SweepOptions()); return solid; }
                catch { solid.Dispose(); throw; }
                finally { foreach (DBObject region in regions) region.Dispose(); }
            }
        }
        private static Point3d Start(ReferencePipe3D p) { return new Point3d(p.X1, p.Y1, p.Z1); }
        private static Point3d End(ReferencePipe3D p) { return new Point3d(p.X2, p.Y2, p.Z2); }

        public ObjectId Create(Database db, Transaction tr, Point3d insertion, ReferenceAssembly3D model)
        {
            ObjectId definitionId = CreateDefinition(db, tr, model);
            var space = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
            var reference = new BlockReference(insertion, definitionId);
            space.AppendEntity(reference); tr.AddNewlyCreatedDBObject(reference, true);
            Write(reference, tr, model);
            return reference.ObjectId;
        }
        private ObjectId CreateDefinition(Database db, Transaction tr, ReferenceAssembly3D model)
        {
            var entities = Build(model);
            try
            {
                var blocks = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForWrite);
                var definition = new BlockTableRecord { Name = "CIVILSPELLAI_REFERENCIAS_3D_" + Guid.NewGuid().ToString("N") };
                blocks.Add(definition); tr.AddNewlyCreatedDBObject(definition, true);
                foreach (var entity in entities) { definition.AppendEntity(entity); tr.AddNewlyCreatedDBObject(entity, true); }
                return definition.ObjectId;
            }
            finally { foreach (var entity in entities) entity.Dispose(); }
        }
        public bool TryRead(ObjectId id, Transaction tr, out ReferenceAssembly3D model)
        {
            model = null;
            if (id.IsNull || !id.IsValid || id.IsErased) return false;
            var reference = tr.GetObject(id, OpenMode.ForRead) as BlockReference;
            if (reference == null || reference.ExtensionDictionary.IsNull) return false;
            var dictionary = (DBDictionary)tr.GetObject(reference.ExtensionDictionary, OpenMode.ForRead);
            if (!dictionary.Contains(Record)) return false;
            var record = (Xrecord)tr.GetObject(dictionary.GetAt(Record), OpenMode.ForRead);
            if (record.Data == null) return false;
            var values = record.Data.AsArray();
            if (values.Length < 2 || Convert.ToInt16(values[0].Value) != 1) return false;
            model = ReferenceAssembly3D.Deserialize(string.Concat(values.Skip(1).Select(v => (string)v.Value)));
            model.Validate(); return true;
        }
        public void Update(Database db, Transaction tr, ObjectId id, ReferenceAssembly3D model)
        {
            var reference = (BlockReference)tr.GetObject(id, OpenMode.ForWrite);
            if (((LayerTableRecord)tr.GetObject(reference.LayerId, OpenMode.ForRead)).IsLocked)
                throw new InvalidOperationException("El modelo está en una capa bloqueada.");
            reference.BlockTableRecord = CreateDefinition(db, tr, model); Write(reference, tr, model);
        }
        private static void Write(BlockReference reference, Transaction tr, ReferenceAssembly3D model)
        {
            if(reference.ExtensionDictionary.IsNull) reference.CreateExtensionDictionary();
            var dictionary = (DBDictionary)tr.GetObject(reference.ExtensionDictionary, OpenMode.ForWrite);
            Xrecord record;
            if (dictionary.Contains(Record)) record = (Xrecord)tr.GetObject(dictionary.GetAt(Record), OpenMode.ForWrite);
            else { record = new Xrecord(); dictionary.SetAt(Record, record); tr.AddNewlyCreatedDBObject(record, true); }
            var values = new List<TypedValue> { new TypedValue((int)DxfCode.Int16, (short)1) };
            string json = model.Serialize();
            for (int i = 0; i < json.Length; i += 240)
                values.Add(new TypedValue((int)DxfCode.Text, json.Substring(i, Math.Min(240, json.Length-i))));
            using (var data = new ResultBuffer(values.ToArray())) record.Data = data;
        }
    }
}
