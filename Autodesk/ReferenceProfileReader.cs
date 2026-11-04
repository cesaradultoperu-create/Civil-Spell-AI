using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using CivilSpellAI.Geometry;

namespace CivilSpellAI.Autodesk
{
    internal sealed class MaterialReferenceProfile
    {
        public string Pattern;
        public ReferenceContour3D Contour;
    }
    internal static class ReferenceProfileReader
    {
        public static List<MaterialReferenceProfile> Read(ObjectId[] ids, Transaction tr)
        {
            var result = new List<MaterialReferenceProfile>();
            foreach (var id in ids) ReadEntity((Entity)tr.GetObject(id, OpenMode.ForRead), tr, result, Matrix3d.Identity, 0);
            return result;
        }
        private static void ReadEntity(Entity entity, Transaction tr, List<MaterialReferenceProfile> result, Matrix3d pose, int depth)
        {
            if (depth > 12) throw new ArgumentException("Referencia con demasiados bloques anidados.");
            var block = entity as BlockReference;
            if (block != null)
            {
                var definition = (BlockTableRecord)tr.GetObject(block.BlockTableRecord, OpenMode.ForRead);
                foreach (ObjectId id in definition) ReadEntity((Entity)tr.GetObject(id, OpenMode.ForRead), tr, result, pose * block.BlockTransform, depth + 1);
                return;
            }
            var hatch = entity as Hatch;
            if (hatch != null)
            {
                for (int loopIndex = 0; loopIndex < hatch.NumberOfLoops; loopIndex++)
                {
                    var loop = hatch.GetLoopAt(loopIndex);
                    using (var polyline = new Polyline())
                    {
                        int i = 0;
                        if (loop.IsPolyline)
                        {
                            foreach (BulgeVertex vertex in loop.Polyline)
                                polyline.AddVertexAt(i++, vertex.Vertex, vertex.Bulge, 0, 0);
                        }
                        else
                        {
                            foreach (Curve2d curve in loop.Curves)
                            {
                                var line = curve as LineSegment2d;
                                var arc = curve as CircularArc2d;
                                if (line == null && arc == null)
                                    throw new ArgumentException("El hatch contiene elipses o splines. Seleccione su contorno cerrado como polilínea para reconstruirlo.");
                                double bulge = 0;
                                if (arc != null)
                                {
                                    double sweep = arc.EndAngle - arc.StartAngle;
                                    if (sweep < 0) sweep += Math.PI * 2;
                                    bulge = Math.Tan(sweep / 4) * (arc.IsClockWise ? -1 : 1);
                                }
                                polyline.AddVertexAt(i++, curve.StartPoint, bulge, 0, 0);
                            }
                        }
                        if (i < 3) continue;
                        polyline.Closed = true;
                        polyline.TransformBy(pose);
                        result.Add(new MaterialReferenceProfile { Pattern = hatch.PatternName,
                            Contour = FromPolyline(polyline, (loop.LoopType & HatchLoopTypes.External) == 0 && loopIndex > 0) });
                    }
                }
                return;
            }
            var source = entity as Polyline;
            if (source != null && source.Closed)
            {
                using (var clone = (Polyline)source.Clone())
                {
                    clone.TransformBy(pose);
                    result.Add(new MaterialReferenceProfile { Contour = FromPolyline(clone, false) });
                }
            }
            var circle = entity as Circle;
            if (circle != null)
            {
                using (var polygon = new Polyline())
                {
                    for (int i=0; i<4; i++) polygon.AddVertexAt(i,
                        new Point2d(circle.Center.X + circle.Radius * Math.Cos(i*Math.PI/2), circle.Center.Y + circle.Radius * Math.Sin(i*Math.PI/2)),
                        Math.Tan(Math.PI/8), 0, 0);
                    polygon.Closed = true; polygon.TransformBy(pose);
                    result.Add(new MaterialReferenceProfile { Contour = FromPolyline(polygon, false) });
                }
            }
        }
        private static ReferenceContour3D FromPolyline(Polyline polygon, bool hole)
        {
            int count = polygon.NumberOfVertices;
            if (count > 1 && polygon.GetPoint2dAt(0).GetDistanceTo(polygon.GetPoint2dAt(count-1)) < 1e-8) count--;
            var result = new ReferenceContour3D { X = new double[count], Y = new double[count], Bulges = new double[count], Hole = hole };
            for (int i=0; i<count; i++) { var p=polygon.GetPoint2dAt(i); result.X[i]=p.X;result.Y[i]=p.Y;result.Bulges[i]=polygon.GetBulgeAt(i); }
            return result;
        }
        public static ReferenceContour3D Map(ReferenceContour3D source, double x, double y)
        {
            return new ReferenceContour3D { X=source.X.Select(v=>v-x).ToArray(),Y=source.Y.Select(v=>v-y).ToArray(),
                Bulges=source.Bulges == null ? null : (double[])source.Bulges.Clone(),Hole=source.Hole };
        }
        public static ReferenceContour3D Rectangle(double x, double y, double width, double height, bool hole = false)
        {
            return new ReferenceContour3D { X=new[]{x,x+width,x+width,x},Y=new[]{y,y,y+height,y+height},Hole=hole };
        }
    }
}
