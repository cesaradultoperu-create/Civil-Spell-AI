using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Autodesk.AutoCAD.Geometry;
using CivilSpellAI.Geometry;

namespace CivilSpellAI.Autodesk
{
    // Recognition of this detail family is deliberately explicit. Other shapes use
    // paired component profiles; they are never silently replaced by this chamber.
    internal sealed class DrainageReference3DAnalyzer
    {
        private sealed class Segment
        {
            public double X1,Y1,X2,Y2;
            public double Slope { get { return (Y2-Y1)/(X2-X1); } }
            public double At(double x) { return Y1+(x-X1)*Slope; }
        }
        public ReferenceAssembly3D Analyze(SelectedFigureSnapshot top, SelectedFigureSnapshot front,
            List<MaterialReferenceProfile> topProfiles, List<MaterialReferenceProfile> frontProfiles)
        {
            if (top.SourceIds.Intersect(front.SourceIds).Any()) throw new ArgumentException("TOP y FRONT deben ser referencias diferentes.");
            if (top.ApproximatePreview || front.ApproximatePreview || top.Measures == null || top.Measures.FrameBounds == null)
                throw new ArgumentException("No se reconoció este detalle de caja. Use el modo Componentes con perfiles cerrados de TOP y FRONT.");
            var b=top.Measures.FrameBounds;
            // FrameBounds is local; this analyzer requires orthogonal references in WCS.
            if (Math.Abs(top.AxisAngle)>1e-8 || Math.Abs(front.AxisAngle)>1e-8) throw new ArgumentException("Alinee TOP y FRONT con los ejes XY antes de reconocer el detalle.");
            double tx=top.Origin.X+b[0],ty=top.Origin.Y+b[2],w=b[1]-b[0],d=b[3]-b[2],tol=Math.Max(w,d)*1e-4;
            double fx=0,bottom=0,floor=0;
            bool found=false;
            foreach(var profile in frontProfiles.Where(p=>p.Pattern == "AR-CONC" && !p.Contour.Hole))
            {
                var c=profile.Contour; double low=c.Y.Min();
                for(int i=0;i<c.X.Length;i++)
                {
                    int j=(i+1)%c.X.Length;
                    if(Math.Abs(c.Y[i]-low)>tol || Math.Abs(c.Y[j]-low)>tol || Math.Abs(Math.Abs(c.X[i]-c.X[j])-w)>tol) continue;
                    double left=Math.Min(c.X[i],c.X[j]);
                    var floors=new List<double>();
                    for(int k=0;k<c.X.Length;k++)
                    {
                        int l=(k+1)%c.X.Length;
                        if(Math.Abs(c.Y[k]-c.Y[l])<tol && Math.Abs(Math.Min(c.X[k],c.X[l])-(left+b[4]-b[0]))<tol &&
                            Math.Abs(Math.Max(c.X[k],c.X[l])-(left+b[5]-b[0]))<tol && c.Y[k]>low+tol) floors.Add(c.Y[k]);
                    }
                    if(floors.Count==0) continue;
                    if(found && (Math.Abs(fx-left)>tol || Math.Abs(bottom-low)>tol)) throw new ArgumentException("FRONT incluye más de una caja compatible; seleccione solo una sección.");
                    fx=left;bottom=low;floor=floors.Min();found=true;
                }
            }
            if(!found) throw new ArgumentException("FRONT debe incluir el perfil de concreto de la sección correspondiente a TOP, con su hatch AR-CONC.");
            var rawSegments=Segments(front.Outlines).ToArray();
            var segments=MergeSegments(rawSegments,tol).ToArray();
            var caps=rawSegments.Where(s=>Math.Abs(s.Y2-s.Y1)<tol && s.X1>=fx-tol && s.X2<=fx+w+tol && s.Y1>floor+tol).ToArray();
            double cap=caps.Where(s=>Math.Abs(s.X1-fx)<tol || Math.Abs(s.X2-(fx+w))<tol).Select(s=>s.Y1).DefaultIfEmpty(double.NaN).Max();
            if(double.IsNaN(cap)) throw new ArgumentException("No se encontró la coronación en FRONT.");
            var model=new ReferenceAssembly3D { TopReference=Describe(top),FrontReference=Describe(front) };
            var chamber=new ReferencePart3D { Name="Concreto: caja",Bottom=0,Height=cap-bottom,Color=8 };
            chamber.Top.Add(ReferenceProfileReader.Rectangle(0,0,w,d));
            model.Parts.Add(chamber);
            // Cavity extends past the crown; inlet is independently cut from its paired section.
            var cavity=new ReferencePart3D { Name="Vacío interior",Bottom=floor-bottom,Height=cap-floor+w,
                Top={ReferenceProfileReader.Rectangle(b[4]-b[0],b[6]-b[2],b[5]-b[4],b[7]-b[6])} };
            model.Cuts.Add(cavity);
            var sand=frontProfiles.Where(p=>p.Pattern == "AR-SAND" && !p.Contour.Hole &&
                Math.Abs(p.Contour.X.Min()-fx)<tol && Math.Abs(p.Contour.X.Max()-(fx+w))<tol &&
                Math.Abs(p.Contour.Y.Max()-bottom)<tol && p.Contour.Y.Min()<bottom-tol).FirstOrDefault();
            if(sand!=null) model.Parts.Add(new ReferencePart3D { Name="Solado",Color=9,Bottom=sand.Contour.Y.Min()-bottom,
                Height=bottom-sand.Contour.Y.Min(),Top={ReferenceProfileReader.Rectangle(0,0,w,d)} });

            model.Pipe=ReadPipe(top,segments,tx,ty,fx,bottom,w,tol);
            var transverse=ReadTransversePipe(top,frontProfiles,tx,ty,fx,bottom,d,tol);
            if(model.Pipe!=null && transverse!=null) throw new ArgumentException("Se detectaron dos recorridos de tubería. Reconstruya cada componente con sus referencias correspondientes.");
            if(model.Pipe==null) model.Pipe=transverse;
            if(model.Pipe==null && HasPipePlan(top,tx,ty,w,d,tol))
                throw new ArgumentException("TOP contiene un recorrido de tubería sin una correspondencia única en FRONT. Incluya sus bordes y eje, o su sección circular con la misma posición y diámetro.");
            AddMasonry(model,topProfiles,frontProfiles,tx,ty,fx,bottom,cap,b,w,tol,false);
            AddMasonry(model,topProfiles,frontProfiles,tx,ty,fx,bottom,cap,b,w,tol,true);
            if(model.Parts.Count(p=>p.Name=="Mampostería: fondo")==2)
                foreach(var part in model.Parts.Where(p=>p.Name.StartsWith("Mampostería:")))
                    part.Name+=part.Top.Min(c=>c.X.Min())<0?" (izquierdo)":" (derecho)";
            AddInteriorParts(model,top,front,frontProfiles,tx,ty,fx,bottom,b,tol);
            model.CutPipeOpening=model.Pipe!=null;
            if(model.Pipe!=null && Math.Abs(model.Pipe.X2-model.Pipe.X1)>tol)
                AddSandBed(model,topProfiles,frontProfiles,tx,ty,fx,bottom,w,tol,model.Pipe.X2>w);
            model.Notes.Add("Caja abierta: altura de concreto " + F(cap-bottom) + "; base " + F(floor-bottom) + ". El solado es un componente separado.");
            model.Notes.Add("Mampostería reconstruida desde sus contornos GRAVEL. El saliente conserva la planta seleccionada.");
            if(model.Pipe!=null)
            {
                if(transverse!=null)
                    model.Notes.Add("Tubería perpendicular a FRONT: diámetro y cota proceden del círculo; recorrido y longitud, de TOP. Se representa a cota constante. Si FRONT es un corte y existe pendiente longitudinal, indique las cotas inicial/final o aporte ese corte antes de dar el modelo por definitivo.");
                else
                {
                    model.Notes.Add("Tubería: diámetro geométrico " + F(model.Pipe.Diameter) + "; pendiente leída " + F(100*(model.Pipe.Z2-model.Pipe.Z1)/(model.Pipe.X2-model.Pipe.X1)) + "%. Revise el diámetro nominal de la lámina.");
                    model.Notes.Add("El eje de la tubería se calcula entre sus bordes paralelos de FRONT y se comprueba el diámetro con TOP. Puede ajustar su inclinación en grados en Tubería.");
                }
            }
            model.Notes.Add("Las longitudes de canal y tubería son las dibujadas entre cortes. Revise su longitud real antes de construir.");
            if(model.Pipe!=null) model.Notes.Add("Espesor de tubería 0: superficie cilíndrica abierta. Ingrese un espesor conocido para generar un sólido hueco.");
            model.Notes.Add("La cama de arena se genera cuando sus perfiles AR-SAND aparecen en ambas vistas, limitada al tramo visible común. El terreno no se convierte en un sólido.");
            model.Notes.Add("Canal: se conservan las cotas geométricas de FRONT. Revise la pendiente mínima rotulada y las cotas de entrada antes de dar el diseño por definitivo.");
            model.Validate(); return model;
        }
        private static void AddMasonry(ReferenceAssembly3D model,List<MaterialReferenceProfile> topProfiles,
            List<MaterialReferenceProfile> frontProfiles,double tx,double ty,double fx,double bottom,double cap,double[] b,double width,double tol,bool onRight)
        {
            var plan=topProfiles.Where(p=>p.Pattern == "GRAVEL" && !p.Contour.Hole && (onRight?
                p.Contour.X.Max()>tx+width+tol && p.Contour.X.Min()>=tx+b[5]-b[0]-tol:
                p.Contour.X.Min()<tx-tol && p.Contour.X.Max()<=tx+b[4]-b[0]+tol)).ToArray();
            var elevation=frontProfiles.Where(p=>p.Pattern == "GRAVEL" && !p.Contour.Hole && (onRight?
                p.Contour.X.Max()>fx+width+tol && p.Contour.X.Min()>=fx+b[5]-b[0]-tol:
                p.Contour.X.Min()<fx-tol && p.Contour.X.Max()<=fx+b[4]-b[0]+tol)).ToArray();
            if(plan.Length==0 && elevation.Length==0) return;
            if(plan.Length<3 || elevation.Length<2) throw new ArgumentException("Seleccione el canal de mampostería completo en TOP y FRONT, incluidos sus hatches GRAVEL.");
            double low=elevation.Min(p=>p.Contour.Y.Min()),upper=elevation.Max(p=>p.Contour.Y.Max());
            // FRONT separates the invert band and side wall height.
            double invert=elevation.Where(p=>Math.Abs(p.Contour.Y.Min()-low)<tol).Max(p=>p.Contour.Y.Max());
            if(invert>=upper-tol) throw new ArgumentException("FRONT debe separar el fondo del canal de sus paredes laterales.");
            double middle=(b[2]+b[3])/2+ty-b[2];
            var center=plan.Where(p=>p.Contour.Y.Min()<middle-tol && p.Contour.Y.Max()>middle+tol).ToArray();
            if(center.Length!=1) throw new ArgumentException("No se pudo distinguir el fondo del canal en TOP.");
            var basePart=new ReferencePart3D {Name="Mampostería: fondo",Color=30,Bottom=low-bottom,Height=invert-low};
            foreach(var p in plan) basePart.Top.Add(ReferenceProfileReader.Map(p.Contour,tx,ty));
            model.Parts.Add(basePart);
            var walls=new ReferencePart3D {Name="Mampostería: laterales",Color=30,Bottom=invert-bottom,Height=upper-invert};
            foreach(var p in plan.Except(center)) walls.Top.Add(ReferenceProfileReader.Map(p.Contour,tx,ty));
            model.Parts.Add(walls);
            model.Cuts.Add(new ReferencePart3D {Name="Entrada del canal",Bottom=low-bottom,Height=upper-low,
                Top={ReferenceProfileReader.Rectangle(onRight?b[5]-b[0]-width*.01:-width*.01,plan.Min(p=>p.Contour.Y.Min())-ty,
                    (onRight?width-(b[5]-b[0]):b[4]-b[0])+width*.02,
                    plan.Max(p=>p.Contour.Y.Max())-plan.Min(p=>p.Contour.Y.Min()))} });
        }
        private static ReferencePipe3D ReadPipe(SelectedFigureSnapshot top,Segment[] front,double tx,double ty,double fx,double bottom,double width,double tol)
        {
            var axes=front.Where(s=>s.X2-s.X1>tol && Math.Abs(s.Slope)<5).ToArray();
            var lines=axes.Where(s=>CrossesWall(s,fx,width,tol)).ToArray();
            var rawPlan=Segments(top.Outlines).ToArray();
            // Retain original pipe strokes as well as reconstructed fragments.
            // A collinear outline inside the chamber may otherwise extend a
            // merged border past the wall-entry limit and hide the actual pipe.
            var plan=rawPlan.Concat(MergeSegments(rawPlan,tol))
                .Where(s=>Math.Abs(s.Y2-s.Y1)<tol && CrossesWall(s,tx,width,tol)).ToArray();
            var candidates=new List<ReferencePipe3D>();
            foreach(var a in lines)
                foreach(var c in lines.Where(s=>s.At(a.X1)>a.Y1+tol && Math.Abs(s.Slope-a.Slope)<tol &&
                    Math.Min(s.X2,a.X2)-Math.Max(s.X1,a.X1)>tol))
                {
                    // A rotated pipe's border endpoints have different X coordinates.
                    // Its two parallel borders determine the axis even if the separate
                    // drafting centerline was not moved with the block.
                    bool normalStart=NormalPipeEnd(a,c,true,tol),normalEnd=NormalPipeEnd(a,c,false,tol);
                    bool shiftedStart=Math.Abs(c.X1-a.X1)>tol,shiftedEnd=Math.Abs(c.X2-a.X2)>tol;
                    bool hasAxis=axes.Any(s=>Math.Min(s.X2,a.X2)-Math.Max(s.X1,a.X1)>tol &&
                        Math.Abs(s.At(a.X1)-(a.Y1+c.At(a.X1))/2)<tol && Math.Abs(s.Slope-a.Slope)<tol);
                    // Without an aligned axis, require a drawn inclined end whose
                    // border endpoints are normal to the tube. Horizontal channel
                    // layers and unrelated trim lines must not become extra pipes.
                    bool inclinedEnd=(shiftedStart && normalStart) || (shiftedEnd && normalEnd);
                    if(!inclinedEnd && !(hasAxis && !shiftedStart && !shiftedEnd)) continue;
                    double diameter=(c.At(a.X1)-a.Y1)/Math.Sqrt(1+a.Slope*a.Slope);
                    var pair=(from lower in plan from upper in plan where (lower.X1<tx)==(a.X1<fx) && upper.Y1>lower.Y1+tol && Math.Abs(upper.Y1-lower.Y1-diameter)<tol*4 &&
                        Math.Abs(upper.X1-lower.X1)<tol && Math.Abs(upper.X2-lower.X2)<tol select new[]{lower,upper}).ToArray();
                    if(pair.Length==0) continue;
                    // Repeated/fragmented depictions on the same TOP axis are
                    // one pipe. Distinct parallel axes remain ambiguous.
                    double planCenter=(pair[0][0].Y1+pair[0][1].Y1)/2;
                    if(pair.Any(p=>Math.Abs((p[0].Y1+p[1].Y1)/2-planCenter)>tol)) continue;
                    var footprint=pair.OrderByDescending(p=>p[0].X2-p[0].X1).First();
                    double end=Math.Min(PipeEndpointX(a,c,false,tol)-fx,footprint[0].X2-tx);
                    double start=Math.Max(PipeEndpointX(a,c,true,tol)-fx,footprint[0].X1-tx);
                    if(end-start<=tol) continue;
                    candidates.Add(new ReferencePipe3D { Name="Tubería: eje TOP + FRONT",X1=start,X2=end,
                        Y1=planCenter-ty,Y2=planCenter-ty,
                        Z1=(a.At(fx+start)+c.At(fx+start))/2-bottom,
                        Z2=(a.At(fx+end)+c.At(fx+end))/2-bottom,Diameter=diameter });
                }
            return candidates.Count==1?candidates[0]:null;
        }
        private static double PipeEndpointX(Segment lower,Segment upper,bool start,double tolerance)
        {
            double x1=start?lower.X1:lower.X2,x2=start?upper.X1:upper.X2;
            // At a square/rounded pipe end, the border endpoints are normal to the
            // axis. Their midpoint is the end-center, not the shorter border's X.
            if(NormalPipeEnd(lower,upper,start,tolerance)) return (x1+x2)/2;
            return start?Math.Max(x1,x2):Math.Min(x1,x2);
        }
        private static bool NormalPipeEnd(Segment lower,Segment upper,bool start,double tolerance)
        {
            double dx=start?upper.X1-lower.X1:upper.X2-lower.X2;
            double dy=start?upper.Y1-lower.Y1:upper.Y2-lower.Y2;
            return Math.Abs(dx+lower.Slope*dy)<=tolerance*Math.Sqrt(1+lower.Slope*lower.Slope);
        }
        private static Segment[] PlanLines(SelectedFigureSnapshot top,bool transverse,double tol)
        {
            var lines=Segments(top.Outlines);
            if(transverse) lines=lines.Select(s=>new Segment {X1=Math.Min(s.Y1,s.Y2),X2=Math.Max(s.Y1,s.Y2),
                Y1=s.Y1<=s.Y2?s.X1:s.X2,Y2=s.Y1<=s.Y2?s.X2:s.X1});
            return MergeSegments(lines,tol).Where(s=>Math.Abs(s.Y2-s.Y1)<tol && s.X2-s.X1>tol).ToArray();
        }
        private static bool HasPipePlan(SelectedFigureSnapshot top,double tx,double ty,double width,double depth,double tol)
        {
            for(int direction=0;direction<2;direction++)
            {
                double datum=direction==0?tx:ty,size=direction==0?width:depth;
                var all=PlanLines(top,direction==1,tol);
                var edges=all.Where(s=>CrossesWall(s,datum,size,tol)).ToArray();
                foreach(var a in edges)
                    foreach(var b in edges.Where(s=>s.Y1>a.Y1+tol && Math.Abs(s.X1-a.X1)<tol && Math.Abs(s.X2-a.X2)<tol))
                        if(all.Any(s=>Math.Abs(s.Y1-(a.Y1+b.Y1)/2)<tol && Math.Min(s.X2,a.X2)-Math.Max(s.X1,a.X1)>tol)) return true;
            }
            return false;
        }
        private static ReferencePipe3D ReadTransversePipe(SelectedFigureSnapshot top,List<MaterialReferenceProfile> front,
            double tx,double ty,double fx,double bottom,double depth,double tol)
        {
            var lines=PlanLines(top,true,tol).Where(s=>CrossesWall(s,ty,depth,tol)).ToArray();
            var candidates=new List<ReferencePipe3D>();
            foreach(var profile in front.Where(p=>p.Pattern==null && !p.Contour.Hole))
            {
                var circle=profile.Contour;
                // ReferenceProfileReader preserves a native circle as four exact
                // quarter arcs. A rectangular outline is not a pipe section.
                if(circle.X.Length!=4 || circle.Bulges==null || circle.Bulges.Any(v=>Math.Abs(v-Math.Tan(Math.PI/8))>1e-8)) continue;
                double x=(circle.X.Min()+circle.X.Max())/2,z=(circle.Y.Min()+circle.Y.Max())/2,r=(circle.X.Max()-circle.X.Min())/2;
                if(r<=tol || Math.Abs(circle.Y.Max()-circle.Y.Min()-2*r)>tol ||
                    Enumerable.Range(0,4).Any(i=>Math.Abs(Math.Sqrt(Math.Pow(circle.X[i]-x,2)+Math.Pow(circle.Y[i]-z,2))-r)>tol)) continue;
                foreach(var a in lines)
                    foreach(var b in lines.Where(s=>s.Y1>a.Y1+tol && Math.Abs(s.X1-a.X1)<tol && Math.Abs(s.X2-a.X2)<tol))
                    {
                        if(Math.Abs(b.Y1-a.Y1-2*r)>tol*4 || Math.Abs((a.Y1+b.Y1)/2-tx-(x-fx))>tol) continue;
                        candidates.Add(new ReferencePipe3D {Name="Tubería: TOP + sección circular FRONT",Diameter=2*r,
                            X1=x-fx,X2=x-fx,Y1=a.X1-ty,Y2=a.X2-ty,Z1=z-bottom,Z2=z-bottom});
                    }
            }
            return candidates.Count==1?candidates[0]:null;
        }
        private static void AddSandBed(ReferenceAssembly3D model,List<MaterialReferenceProfile> top,List<MaterialReferenceProfile> front,
            double tx,double ty,double fx,double bottom,double width,double tolerance,bool onRight)
        {
            var plan=top.Where(p=>p.Pattern=="AR-SAND" && !p.Contour.Hole && (onRight?
                p.Contour.X.Min()>=tx+width*.65 && p.Contour.X.Max()>tx+width+tolerance:
                p.Contour.X.Min()<tx-tolerance && p.Contour.X.Max()<=tx+width*.35)).ToArray();
            var section=front.Where(p=>p.Pattern=="AR-SAND" && !p.Contour.Hole && (onRight?
                p.Contour.X.Min()>=fx+width*.65 && p.Contour.X.Max()>fx+width+tolerance:
                p.Contour.X.Min()<fx-tolerance && p.Contour.X.Max()<=fx+width*.35)).ToArray();
            if(plan.Length<2 || section.Length!=1) return;
            var c=section[0].Contour;
            double start=onRight?width:Math.Max(model.Pipe.X1,Math.Max(plan.Max(p=>p.Contour.X.Min())-tx,c.X.Min()-fx));
            double end=onRight?Math.Min(model.Pipe.X2,Math.Min(plan.Min(p=>p.Contour.X.Max())-tx,c.X.Max()-fx)):0;
            if(end<=start+tolerance) return;
            model.Parts.Add(new ReferencePart3D {Name="Cama de arena: TOP + FRONT",Color=9,Bottom=c.Y.Min()-bottom,Height=c.Y.Max()-c.Y.Min(),
                Top={ReferenceProfileReader.Rectangle(start,plan.Min(p=>p.Contour.Y.Min())-ty,end-start,
                    plan.Max(p=>p.Contour.Y.Max())-plan.Min(p=>p.Contour.Y.Min()))},
                Front={ReferenceProfileReader.Map(c,fx,bottom)} });
        }
        private static bool CrossesWall(Segment s,double datum,double width,double tolerance)
        {
            return s.X2-s.X1>tolerance && ((s.X1<datum-tolerance && s.X2>datum+tolerance && s.X2<datum+width*.35) ||
                (s.X1>datum+width*.65 && s.X1<datum+width-tolerance && s.X2>datum+width+tolerance));
        }
        private static void AddInteriorParts(ReferenceAssembly3D model,SelectedFigureSnapshot top,SelectedFigureSnapshot front,
            List<MaterialReferenceProfile> profiles,double tx,double ty,double fx,double bottom,double[] bounds,double tolerance)
        {
            double il=bounds[4]-bounds[0],ir=bounds[5]-bounds[0],ib=bounds[6]-bounds[2],it=bounds[7]-bounds[2];
            var plan=Segments(top.Outlines).ToArray();var elevation=Segments(front.Outlines).ToArray();
            foreach(var profile in profiles.Where(p=>p.Pattern=="AR-CONC" && !p.Contour.Hole &&
                p.Contour.X.Min()>fx+il+tolerance && p.Contour.X.Max()<fx+ir-tolerance))
            {
                var c=profile.Contour;
                double left=c.X.Min()-fx,right=c.X.Max()-fx,low=c.Y.Min()-bottom,high=c.Y.Max()-bottom;
                if(!CoversVertical(plan,tx+left,ty+ib,ty+it,tolerance) || !CoversVertical(plan,tx+right,ty+ib,ty+it,tolerance))
                    throw new ArgumentException("FRONT contiene un componente interior de concreto sin un perfil TOP correspondiente. Incluya ambas referencias completas.");
                model.Parts.Add(new ReferencePart3D {Name="Concreto: soporte interior",Color=8,Bottom=low,Height=high-low,
                    Top={ReferenceProfileReader.Rectangle(left,ib,right-left,it-ib)},Front={ReferenceProfileReader.Map(c,fx,bottom)}});
                // The upper piece has three explicit edges and shares the support's
                // drawn top edge. It is not closed by an invented connection.
                var crowns=elevation.Where(s=>Math.Abs(s.X1-(fx+left))<tolerance && Math.Abs(s.X2-(fx+right))<tolerance &&
                    Math.Abs(s.Y2-s.Y1)<tolerance && s.Y1>bottom+high+tolerance).Select(s=>s.Y1).Distinct().ToArray();
                if(crowns.Length==0)continue;
                if(crowns.Length!=1 || !CoversVertical(elevation,fx+left,bottom+high,crowns[0],tolerance) ||
                    !CoversVertical(elevation,fx+right,bottom+high,crowns[0],tolerance))
                    throw new ArgumentException("El elemento interior de FRONT no tiene límites cerrados inequívocos. Revise sus contornos.");
                model.Parts.Add(new ReferencePart3D {Name="Elemento interior: material por confirmar",Color=2,Bottom=high,Height=crowns[0]-bottom-high,
                    Top={ReferenceProfileReader.Rectangle(left,ib,right-left,it-ib)},
                    Front={ReferenceProfileReader.Rectangle(left,high,right-left,crowns[0]-bottom-high)}});
                model.Notes.Add("Elemento interior: se conserva la geometría de TOP y FRONT sobre su soporte de concreto. Confirme si la pieza superior es tabique, rejilla o pantalla; las barras o perforaciones requieren su detalle.");
            }
        }
        private static bool CoversVertical(IEnumerable<Segment> segments,double x,double low,double high,double tolerance)
        {
            double covered=low;
            foreach(var s in segments.Where(s=>Math.Abs(s.X1-x)<tolerance && Math.Abs(s.X2-x)<tolerance).OrderBy(s=>Math.Min(s.Y1,s.Y2)))
            {
                double start=Math.Min(s.Y1,s.Y2),end=Math.Max(s.Y1,s.Y2);
                if(end<covered-tolerance)continue;
                if(start>covered+tolerance)return false;
                covered=Math.Max(covered,end);if(covered>=high-tolerance)return true;
            }
            return false;
        }
        private static IEnumerable<Segment> MergeSegments(IEnumerable<Segment> segments,double tolerance)
        {
            var result=new List<Segment>();
            foreach(var s in segments.OrderBy(s=>s.X1))
            {
                var current=result.FirstOrDefault(r=>r.X2-r.X1>tolerance && s.X2-s.X1>tolerance &&
                    s.X1<=r.X2+tolerance && Math.Abs(r.Slope-s.Slope)<tolerance && Math.Abs(r.At(s.X1)-s.Y1)<tolerance);
                if(current==null) result.Add(new Segment {X1=s.X1,Y1=s.Y1,X2=s.X2,Y2=s.Y2});
                else if(s.X2>current.X2){double y=current.At(s.X2);current.X2=s.X2;current.Y2=y;}
            }
            return result;
        }
        private static IEnumerable<Segment> Segments(IEnumerable<Point2d[]> outlines)
        {
            var seen=new HashSet<string>();
            foreach(var points in outlines)
                for(int i=1;i<points.Length;i++)
                {
                    var a=points[i-1];var b=points[i];if(a.GetDistanceTo(b)<1e-8) continue;
                    if(a.X>b.X){var swap=a;a=b;b=swap;}
                    string key=F(a.X)+","+F(a.Y)+":"+F(b.X)+","+F(b.Y);
                    if(seen.Add(key)) yield return new Segment{X1=a.X,Y1=a.Y,X2=b.X,Y2=b.Y};
                }
        }
        private static string Describe(SelectedFigureSnapshot view) { return string.Join(",",view.SourceIds.Select(id=>id.Handle.ToString())); }
        private static string F(double value) { return value.ToString("0.######",CultureInfo.InvariantCulture); }
    }
}
