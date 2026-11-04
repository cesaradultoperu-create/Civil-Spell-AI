using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace LocalizarCasillas
{
    public sealed class Slot
    {
        public ObjectId AttributeId, BlockId;
        public string Layout, Block, Tag, Value, Code, Property, Handle, Path;
        public Point3d Position;
        public Extents3d Bounds, BlockBounds;
        public Extents3d? CellBounds;
        public double Height;
        public Matrix3d ParentTransform=Matrix3d.Identity;
        public bool Nested, Invisible, HasFields, PendingEdit;
        public string SearchText => Layout+" "+Block+" "+Tag+" "+Value+" "+Property+" "+Code;
    }

    public static class Catalog
    {
        public static List<Slot> Read(Database db, ObjectId onlyBlock = default(ObjectId))
        {
            var result = new List<Slot>();
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var layouts = (DBDictionary)tr.GetObject(db.LayoutDictionaryId, OpenMode.ForRead);
                foreach (DBDictionaryEntry entry in layouts)
                {
                    var layout = (Layout)tr.GetObject(entry.Value, OpenMode.ForRead);
                    var space = (BlockTableRecord)tr.GetObject(layout.BlockTableRecordId, OpenMode.ForRead);
                    foreach (ObjectId id in space)
                    {
                        var block = tr.GetObject(id, OpenMode.ForRead) as BlockReference;
                        if (block == null || (!onlyBlock.IsNull && id != onlyBlock)) continue;
                        int first=result.Count;
                        Visit(block, block.ObjectId, Matrix3d.Identity, layout.LayoutName, "", 0,
                            new HashSet<ObjectId>(), result, tr);
                        if(result.Count>first)
                        {
                            var edges=new List<Segment>();
                            Edges(block,Matrix3d.Identity,0,new HashSet<ObjectId>(),edges,tr);
                            for(int i=first;i<result.Count;i++) result[i].CellBounds=FindCell(result[i],edges);
                        }
                    }
                }
                tr.Commit();
            }
            return result.OrderBy(s=>s.Layout == "Model" ? 1 : 0).ThenBy(s=>s.Layout)
                .ThenBy(s=>s.BlockId.Handle.Value).ThenBy(s=>s.Tag).ToList();
        }

        static void Visit(BlockReference block, ObjectId root, Matrix3d parent, string layout,
            string path, int depth, HashSet<ObjectId> ancestors, List<Slot> result, Transaction tr)
        {
            if (depth > 12 || ancestors.Contains(block.BlockTableRecord)) return;
            var definition = (BlockTableRecord)tr.GetObject(block.BlockTableRecord, OpenMode.ForRead);
            if (definition.IsFromExternalReference || definition.IsFromOverlayReference) return;
            string name = definition.Name;
            if (block.IsDynamicBlock)
                name = ((BlockTableRecord)tr.GetObject(block.DynamicBlockTableRecord, OpenMode.ForRead)).Name;
            path = path.Length == 0 ? name : path+" / "+name;
            var rootBlock = (BlockReference)tr.GetObject(root, OpenMode.ForRead);
            var blockBounds = SafeBounds(rootBlock, Matrix3d.Identity, rootBlock.Position, 5);
            foreach (ObjectId id in block.AttributeCollection)
            {
                var a = tr.GetObject(id, OpenMode.ForRead) as AttributeReference;
                if (a == null || a.IsErased) continue;
                string code = FieldCode(a, tr);
                double height = Math.Max(0.1, a.Height * parent.CoordinateSystem3d.Xaxis.Length);
                var position = a.Position.TransformBy(parent);
                result.Add(new Slot { AttributeId=id, BlockId=root, Layout=layout, Block=name, Tag=a.Tag,
                    Value=DisplayValue(a,tr), Code=code, Property=PropertyName(code), Handle=a.Handle.ToString(),
                    Position=position, Height=height, ParentTransform=parent, Bounds=SafeBounds(a,parent,position,height),
                    BlockBounds=blockBounds, Nested=depth>0, Invisible=a.Invisible, HasFields=a.HasFields, Path=path });
            }
            var next = new HashSet<ObjectId>(ancestors) { block.BlockTableRecord };
            foreach (ObjectId id in definition)
            {
                var child = tr.GetObject(id, OpenMode.ForRead) as BlockReference;
                if (child != null) Visit(child,root,parent*block.BlockTransform,layout,path,depth+1,next,result,tr);
            }
        }

        static string DisplayValue(AttributeReference attribute,Transaction tr)
        {
#if CASILLAS_FIELD
            // TextString can retain the previous display cache in an inactive layout.
            // Read the evaluated Field value without changing its expression or destination.
            if(attribute.HasFields)
            {
                try
                {
                    var field=tr.GetObject(attribute.GetField(),OpenMode.ForRead) as Field;
                    if(field!=null)return field.GetStringValue();
                }
                catch(Autodesk.AutoCAD.Runtime.Exception){}
            }
#endif
            return attribute.TextString;
        }

        public static string FieldCode(AttributeReference a, Transaction tr)
        {
            if (!a.HasFields) return "";
            var id = a.GetField();
            if (id.IsNull) return "";
            var field = (Field)tr.GetObject(id,OpenMode.ForRead);
            return field.GetFieldCode(FieldCodeFlags.AddMarkers);
        }

        public static string PropertyName(string code)
        {
            if (String.IsNullOrEmpty(code)) return "";
            if(code.IndexOf("\\AcSm",StringComparison.OrdinalIgnoreCase)>=0)
            {
                // Real AcSm codes end with .PropertyName, optionally followed by a format.
                var tail=Regex.Match(code,@"\)\.(?<p>[^.\\%<>]+?)(?:\s+\\f\s+""[^""]*"")?\s*>%$",RegexOptions.Singleline);
                if(tail.Success)return tail.Groups["p"].Value.Trim();
            }
            // Keep the original code available: an opaque field ID must never be guessed from its value.
            var custom = Regex.Match(code, @"Custom(?:SheetSet|Sheet)?Property\s*\(\s*""(?<p>[^""]+)""", RegexOptions.IgnoreCase);
            if (custom.Success) return custom.Groups["p"].Value;
            var quoted = Regex.Match(code, @"\\AcSm[^%]*?\.[A-Za-z]*Custom[^%]*?""(?<p>[^""]+)""", RegexOptions.IgnoreCase);
            if (quoted.Success) return quoted.Groups["p"].Value;
            return code.IndexOf("AcSm",StringComparison.OrdinalIgnoreCase)>=0 ? "Sheet Set (ver codigo)" : "Otro campo (ver codigo)";
        }

        static Extents3d SafeBounds(Entity entity, Matrix3d transform, Point3d fallback, double height)
        {
            try
            {
                var e=entity.GeometricExtents; e.TransformBy(transform);
                if (Finite(e.MinPoint.X) && Finite(e.MaxPoint.X) && Finite(e.MinPoint.Y) && Finite(e.MaxPoint.Y)) return e;
            }
            catch (Autodesk.AutoCAD.Runtime.Exception) { }
            return new Extents3d(fallback-new Vector3d(height/2,height/2,0),fallback+new Vector3d(height/2,height/2,0));
        }
        static bool Finite(double n) => !Double.IsNaN(n) && !Double.IsInfinity(n);

        public static Extents3d MarkerBounds(Slot s)
        {
            if(s.CellBounds.HasValue)return s.CellBounds.Value;
            double h=Math.Max(s.Height,0.1), pad=h*0.65;
            double x0=s.Bounds.MinPoint.X, x1=s.Bounds.MaxPoint.X;
            double y0=s.Bounds.MinPoint.Y, y1=s.Bounds.MaxPoint.Y;
            double cx=(x0+x1)/2, cy=(y0+y1)/2;
            double width=Math.Max(x1-x0+pad*2,h*3), height=Math.Max(y1-y0+pad*2,h*2.2);
            return new Extents3d(new Point3d(cx-width/2,cy-height/2,s.Position.Z),new Point3d(cx+width/2,cy+height/2,s.Position.Z));
        }

        struct Segment { public Point3d A,B; public Segment(Point3d a,Point3d b){A=a;B=b;} }
        static void Edges(BlockReference block,Matrix3d parent,int depth,HashSet<ObjectId> ancestors,List<Segment> edges,Transaction tr)
        {
            if(depth>12 || ancestors.Contains(block.BlockTableRecord))return;
            var definition=(BlockTableRecord)tr.GetObject(block.BlockTableRecord,OpenMode.ForRead);
            if(definition.IsFromExternalReference || definition.IsFromOverlayReference)return;
            var transform=parent*block.BlockTransform;
            var next=new HashSet<ObjectId>(ancestors){block.BlockTableRecord};
            foreach(ObjectId id in definition)
            {
                var entity=tr.GetObject(id,OpenMode.ForRead);
                var line=entity as Line;
                if(line!=null)edges.Add(new Segment(line.StartPoint.TransformBy(transform),line.EndPoint.TransformBy(transform)));
                var polyline=entity as Polyline;
                if(polyline!=null)
                    for(int i=0;i<polyline.NumberOfVertices-(polyline.Closed?0:1);i++)
                        if(polyline.GetBulgeAt(i)==0)edges.Add(new Segment(polyline.GetPoint3dAt(i).TransformBy(transform),polyline.GetPoint3dAt((i+1)%polyline.NumberOfVertices).TransformBy(transform)));
                var child=entity as BlockReference;
                if(child!=null)Edges(child,transform,depth+1,next,edges,tr);
            }
        }
        static Extents3d? FindCell(Slot s,List<Segment> edges)
        {
            double x=(s.Bounds.MinPoint.X+s.Bounds.MaxPoint.X)/2,y=(s.Bounds.MinPoint.Y+s.Bounds.MaxPoint.Y)/2;
            double left=Double.NegativeInfinity,right=Double.PositiveInfinity,bottom=Double.NegativeInfinity,top=Double.PositiveInfinity;
            double epsilon=Math.Max(1e-6,s.Height*0.005);
            foreach(var edge in edges)
            {
                double x0=Math.Min(edge.A.X,edge.B.X),x1=Math.Max(edge.A.X,edge.B.X);
                double y0=Math.Min(edge.A.Y,edge.B.Y),y1=Math.Max(edge.A.Y,edge.B.Y);
                if(x1-x0<epsilon && y>y0-epsilon && y<y1+epsilon)
                {if(x0<x-epsilon)left=Math.Max(left,x0);if(x0>x+epsilon)right=Math.Min(right,x0);}
                if(y1-y0<epsilon && x>x0-epsilon && x<x1+epsilon)
                {if(y0<y-epsilon)bottom=Math.Max(bottom,y0);if(y0>y+epsilon)top=Math.Min(top,y0);}
            }
            if(!Finite(left)||!Finite(right)||!Finite(bottom)||!Finite(top))return null;
            double width=right-left,height=top-bottom;
            if(width<s.Height || height<s.Height || width>s.Height*180 || height>s.Height*18)return null;
            if(s.Bounds.MinPoint.X<left-epsilon || s.Bounds.MaxPoint.X>right+epsilon || s.Bounds.MinPoint.Y<bottom-epsilon || s.Bounds.MaxPoint.Y>top+epsilon)return null;
            // Require each side to reach both corners; a partial stroke is not a cell border.
            bool l=false,r=false,b=false,t=false;
            foreach(var edge in edges)
            {
                double x0=Math.Min(edge.A.X,edge.B.X),x1=Math.Max(edge.A.X,edge.B.X),y0=Math.Min(edge.A.Y,edge.B.Y),y1=Math.Max(edge.A.Y,edge.B.Y);
                if(x1-x0<epsilon && y0<=bottom+epsilon && y1>=top-epsilon){l|=Math.Abs(x0-left)<epsilon;r|=Math.Abs(x0-right)<epsilon;}
                if(y1-y0<epsilon && x0<=left+epsilon && x1>=right-epsilon){b|=Math.Abs(y0-bottom)<epsilon;t|=Math.Abs(y0-top)<epsilon;}
            }
            return l&&r&&b&&t ? new Extents3d(new Point3d(left,bottom,s.Position.Z),new Point3d(right,top,s.Position.Z)) : (Extents3d?)null;
        }

        public static void Export(IEnumerable<Slot> slots,string file)
        {
            var lines=new List<string>{"Layout;Bloque;Atributo;Valor;Propiedad;Handle;Anidado;Invisible;X;Y;Altura;CodigoCampo"};
            foreach(var s in slots)
                lines.Add(String.Join(";",new[]{s.Layout,s.Block,s.Tag,s.Value,s.Property,s.Handle,s.Nested.ToString(),s.Invisible.ToString(),
                    s.Position.X.ToString("R",CultureInfo.InvariantCulture),s.Position.Y.ToString("R",CultureInfo.InvariantCulture),
                    s.Height.ToString("R",CultureInfo.InvariantCulture),s.Code}.Select(v=>"\""+(v??"").Replace("\"","\"\"")+"\"")));
            File.WriteAllLines(file,lines,new UTF8Encoding(true));
        }
    }
}
