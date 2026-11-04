using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using CivilSpellAI.Geometry;

namespace CivilSpellAI.Autodesk
{
    internal static class PairedProfile3DAnalyzer
    {
        public static ReferenceAssembly3D Analyze(List<MaterialReferenceProfile> top,List<MaterialReferenceProfile> front,
            double topDatum,double frontDatum,double topY,double frontZ)
        {
            var topContours=Contours(top);var frontContours=Contours(front);
            if(topContours.Count==0 || frontContours.Count==0)
                throw new ArgumentException("Ambas selecciones necesitan contornos cerrados del material de un mismo componente (polilíneas, círculos o hatches con bordes poligonales). Los contornos abiertos no se cierran automáticamente.");
            double topWidth=topContours.Max(c=>c.X.Max())-topContours.Min(c=>c.X.Min());
            double frontWidth=frontContours.Max(c=>c.X.Max())-frontContours.Min(c=>c.X.Min());
            if(Math.Abs(topWidth-frontWidth)>Math.Max(topWidth,frontWidth)*1e-4)
                throw new ArgumentException("Los anchos de TOP y FRONT no coinciden. Revise la escala y seleccione el mismo componente en ambas vistas.");
            double bottom=frontContours.Min(c=>c.Y.Min())-frontZ;
            double height=frontContours.Max(c=>c.Y.Max())-frontContours.Min(c=>c.Y.Min());
            var part=new ReferencePart3D {Name="Componente: TOP + FRONT",Bottom=bottom,Height=height};
            part.Top.AddRange(topContours.Select(c=>ReferenceProfileReader.Map(c,topDatum,topY)));
            part.Front.AddRange(frontContours.Select(c=>ReferenceProfileReader.Map(c,frontDatum,frontZ)));
            var model=new ReferenceAssembly3D {Parts={part}};
            model.Notes.Add("Perfiles de material emparejados: la proyección TOP limita X/Y y FRONT limita X/Z. Se conservan arcos y vacíos cerrados.");
            model.Notes.Add("Si FRONT es un corte de una estructura con varias piezas, seleccione los perfiles de cada componente por separado y repita el comando. Un corte interior no representa las paredes que oculta.");
            model.Validate();return model;
        }
        private static List<ReferenceContour3D> Contours(List<MaterialReferenceProfile> profiles)
        {
            // A hatch already declares its interior loops. Without hatches, determine
            // containment using native regions, including curved boundaries.
            var hatches=profiles.Where(p=>p.Pattern!=null).ToArray();
            if(hatches.Length>0) return hatches.Select(p=>p.Contour).ToList();
            var result=new List<ReferenceContour3D>();
            var regions=new List<Region>();
            try
            {
                foreach(var p in profiles)
                {
                    var region=ReferenceAssemblyDrawingService.CreateRegion(new[]{p.Contour});
                    bool duplicate=false;
                    for(int i=0;i<regions.Count;i++)
                        using(var overlap=(Region)region.Clone())
                        using(var other=(Region)regions[i].Clone())
                        {
                            overlap.BooleanOperation(BooleanOperationType.BoolIntersect,other);
                            if(Math.Abs(region.Area-regions[i].Area)<1e-8 && Math.Abs(overlap.Area-region.Area)<1e-8) {duplicate=true;break;}
                        }
                    if(duplicate){region.Dispose();continue;}
                    regions.Add(region);result.Add(p.Contour);
                }
                for(int i=0;i<regions.Count;i++)
                {
                    int containing=0;
                    for(int j=0;j<regions.Count;j++)
                    {
                        if(i==j || regions[j].Area<=regions[i].Area+1e-8) continue;
                        using(var overlap=(Region)regions[i].Clone())
                        using(var other=(Region)regions[j].Clone())
                        {
                            overlap.BooleanOperation(BooleanOperationType.BoolIntersect,other);
                            if(Math.Abs(overlap.Area-regions[i].Area)<1e-8) containing++;
                            else if(overlap.Area>1e-8) throw new ArgumentException("Los contornos se cruzan parcialmente. Separe los componentes y sus vacíos.");
                        }
                    }
                    if(containing>1) throw new ArgumentException("Hay islas dentro de vacíos. Seleccione cada isla como un componente separado.");
                    result[i].Hole=containing==1;
                }
                return result;
            }
            finally {foreach(var region in regions) region.Dispose();}
        }
    }
}
