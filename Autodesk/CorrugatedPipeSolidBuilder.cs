using System;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using CivilSpellAI.Geometry;

namespace CivilSpellAI.Autodesk
{
    internal static class CorrugatedPipeSolidBuilder
    {
        // Revolve the actual rounded side profile. These are ACIS faces and
        // measurable valleys/crests, rather than displayed lines or a texture.
        public static Solid3d Build(ReferencePipeRoute3D route)
        {
            double root = route.Diameter/2*route.CorrugationProfile.Y.Min()/route.CorrugationProfile.Y.Max();
            var body = ReferenceAssemblyDrawingService.SweptRoute(route,root);
            try
            {
                var points=Enumerable.Range(0,route.X.Length).Select(i=>new Point3d(route.X[i],route.Y[i],route.Z[i])).ToArray();
                if(points.Length==3)
                {
                    var a=points[1]-points[0];var b=points[2]-points[1];
                    var da=a.GetNormal();var db=b.GetNormal();
                    double trim=route.BendRadius*Math.Tan(da.GetAngleTo(db)/2);
                    var before=points[1]-da*(trim+route.Diameter*.5);
                    var after=points[1]+db*(trim+route.Diameter*.5);
                    var tee=points[2]-db*(route.Diameter*7/6);
                    if((before-points[0]).DotProduct(da)<=0 || (tee-after).DotProduct(db)<=0)
                        throw new ArgumentException("No queda longitud para el corrugado entre el codo y las conexiones.");
                    AddCorrugated(body,points[0],before,route,root);
                    AddCorrugated(body,after,tee,route,root);
                    UniteSweep(body,new[]{before,points[1],after},route.Diameter/2,route.BendRadius);
                    UniteSweep(body,new[]{tee,points[2]},route.Diameter/2,0);
                }
                else
                {
                    var delta=points[1]-points[0];var dir=delta.GetNormal();var mid=points[0]+delta*.5;
                    double half=route.Diameter*16/15;
                    if(delta.Length<=2*half)throw new ArgumentException("El ramal es demasiado corto para sus conexiones corrugadas.");
                    var left=mid-dir*half;var right=mid+dir*half;
                    AddCorrugated(body,points[0],left,route,root);
                    AddCorrugated(body,right,points[1],route,root);
                    UniteSweep(body,new[]{left,right},route.Diameter/2,0);
                }
                using(var bore=ReferenceAssemblyDrawingService.SweptRoute(route,ReferenceAssemblyDrawingService.BoreRadius(route)))
                    body.BooleanOperation(BooleanOperationType.BoolSubtract,bore);
                return body;
            }
            catch { body.Dispose();throw; }
        }
        private static void UniteSweep(Solid3d body,Point3d[] points,double radius,double bend)
        {
            using(var next=ReferenceAssemblyDrawingService.SweptRoute(new ReferencePipeRoute3D {
                X=points.Select(p=>p.X).ToArray(),Y=points.Select(p=>p.Y).ToArray(),Z=points.Select(p=>p.Z).ToArray(),BendRadius=bend},radius))
                body.BooleanOperation(BooleanOperationType.BoolUnite,next);
        }
        private static void AddCorrugated(Solid3d body,Point3d start,Point3d end,ReferencePipeRoute3D route,double root)
        {
            var source=route.CorrugationProfile;
            double length=start.DistanceTo(end), sourceLength=source.X.Last()-source.X[0];
            double sx=route.CorrugationPitch*2/sourceLength, sy=route.Diameter/(2*source.Y.Max());
            double cycle=sourceLength*sx;
            using(var pattern=new Polyline())
            using(var section=new Polyline())
            {
                for(int i=0;i<source.X.Length;i++)
                    pattern.AddVertexAt(i,new Point2d((source.X[i]-source.X[0])*sx,source.Y[i]*sy),source.Bulges==null?0:source.Bulges[i],0,0);
                // Close the microscopic drawing seam without creating a sliver face.
                pattern.SetPointAt(pattern.NumberOfVertices-1,new Point2d(cycle,pattern.GetPoint2dAt(0).Y));
                for(double offset=0;offset<length-1e-8;offset+=cycle)
                {
                    for(int i=0;i<pattern.NumberOfVertices-1;i++)
                    {
                        var a=pattern.GetPoint2dAt(i);var b=pattern.GetPoint2dAt(i+1);
                        if(offset+a.X>=length-1e-8)break;
                        double fraction=1;
                        if(offset+b.X>length)
                        {
                            double low=0,high=1;
                            for(int n=0;n<40;n++)
                            {
                                double t=(low+high)/2;
                                if(pattern.GetPointAtParameter(i+t).X+offset<length)low=t;else high=t;
                            }
                            fraction=(low+high)/2;
                        }
                        double bulge=Math.Tan(Math.Atan(pattern.GetBulgeAt(i))*fraction);
                        Append(section,new Point2d(offset+a.X,a.Y),bulge);
                        if(fraction<1)
                        {
                            var p=pattern.GetPointAtParameter(i+fraction);
                            Append(section,new Point2d(length,p.Y),0);break;
                        }
                        if(i==pattern.NumberOfVertices-2 && offset+b.X>=length-1e-8)
                            Append(section,new Point2d(length,b.Y),0);
                    }
                }
                double bottom=root-Math.Min(.001,root*.01);
                Append(section,new Point2d(length,bottom),0);
                Append(section,new Point2d(0,bottom),0);section.Closed=true;
                using(var solid=Revolve(section))
                {
                    Place(solid,start,end-start);
                    body.BooleanOperation(BooleanOperationType.BoolUnite,solid);
                }
            }
        }
        private static void Append(Polyline line,Point2d p,double bulge)
        {
            int count=line.NumberOfVertices;
            if(count>0 && line.GetPoint2dAt(count-1).GetDistanceTo(p)<1e-7){line.SetBulgeAt(count-1,bulge);return;}
            line.AddVertexAt(count,p,bulge,0,0);
        }
        private static Solid3d Revolve(Polyline section)
        {
            using(var curves=new DBObjectCollection {section})
            using(var regions=Region.CreateFromCurves(curves))
            {
                try
                {
                    if(regions.Count!=1)throw new ArgumentException("El perfil de revolución del corrugado no es válido.");
                    var solid=new Solid3d();
                    try { solid.CreateRevolvedSolid((Region)regions[0],Point3d.Origin,Vector3d.XAxis,Math.PI*2,0,new RevolveOptions());return solid; }
                    catch {solid.Dispose();throw;}
                }
                finally {foreach(DBObject region in regions)region.Dispose();}
            }
        }
        private static void Place(Entity solid,Point3d start,Vector3d direction)
        {
            var x=direction.GetNormal();var seed=Math.Abs(x.DotProduct(Vector3d.ZAxis))>.95?Vector3d.YAxis:Vector3d.ZAxis;
            var y=(seed-x*seed.DotProduct(x)).GetNormal();var z=x.CrossProduct(y);
            solid.TransformBy(Matrix3d.AlignCoordinateSystem(Point3d.Origin,Vector3d.XAxis,Vector3d.YAxis,Vector3d.ZAxis,start,x,y,z));
        }
        public static Solid3d BuildCap(ReferencePipeRoute3D route)
        {
            var source=route.CapProfile;
            var start=new Point3d(route.X[0],route.Y[0],route.Z[0]);
            var direction=new Vector3d(route.X[1]-route.X[0],route.Y[1]-route.Y[0],route.Z[1]-route.Z[0]);
            double sx=direction.Length/(source.X.Last()-source.X[0]),sy=route.Diameter/(2*source.Y.Max());
            using(var section=new Polyline())
            {
                for(int i=0;i<source.X.Length;i++)Append(section,new Point2d((source.X[i]-source.X[0])*sx,source.Y[i]*sy),0);
                Append(section,new Point2d(direction.Length,0),0);Append(section,Point2d.Origin,0);section.Closed=true;
                var solid=Revolve(section);
                try {Place(solid,start,direction);return solid;}catch{solid.Dispose();throw;}
            }
        }
    }
}
