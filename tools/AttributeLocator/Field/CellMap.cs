using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using LocalizarCasillas;
using ObjectId=Autodesk.AutoCAD.DatabaseServices.ObjectId;

namespace CasillasField
{
    public static class CellStatus
    {
        public static bool WithoutData(Slot s)=>String.IsNullOrWhiteSpace(s.Value) || s.Value.Trim()==".";
        public static bool HasField(Slot s)=>s.HasFields || !String.IsNullOrWhiteSpace(s.Code);
        public static string State(Slot s)=>s.PendingEdit?"Pendiente · texto local":WithoutData(s)?(HasField(s)?"Sin dato · con Field":"Sin dato · sin Field"):
            (HasField(s)?"Con dato · con Field":"Con dato · texto fijo");
        public static Color Color(Slot s)=>WithoutData(s)?(HasField(s)?System.Drawing.Color.DarkOrange:System.Drawing.Color.ForestGreen):System.Drawing.Color.DimGray;
        public static string Row(Slot s)
        {
            var reference=Regex.Match(s.Tag,@"^04_R(?<n>\d+)_");
            if(s.Block=="MEMB_WWL_P_A1_" && reference.Success)return "Planos ref. R"+reference.Groups["n"].Value;
            var m=Regex.Match(s.Tag,@"^R(?<n>\d+)_");
            if(s.Block!="MEMB_WWL_P_A1_")return "Otros atributos";
            if(m.Success)return "Fila R"+m.Groups["n"].Value;
            if(s.Tag=="REV" || s.Tag=="REVISION")return "Revisión del plano";
            if(s.Tag.StartsWith("02_") && s.Tag!="02_DATUM" && s.Tag!="02_PROJECTION" && s.Tag!="02_SCALE" && s.Tag!="02_SHEETSIZE")return "Firmas y responsables";
            return "Datos generales";
        }
        public static string Column(Slot s)
        {
            if(s.Block!="MEMB_WWL_P_A1_")return "";
            switch(s.Tag)
            {
                case "REV":return "Revisión del plano";case "REVISION":return "Descripción de revisión";
                case "DRAWING_NO":return "Número del plano";case "CLIENT":return "Cliente";case "PROJECT":return "Código de proyecto";
                case "TITTLE_1":return "Nombre del proyecto · 1";case "TITTLE_2":return "Nombre del proyecto · 2";
                case "TITTLE_DRAW1":return "Título del plano · 1";case "TITTLE_DRAW2":return "Título del plano · 2";case "DISCIPLINE":return "Disciplina";
                case "02_DATUM":return "Datum";case "02_PROJECTION":return "Proyección";case "02_SCALE":return "Escala";case "02_SHEETSIZE":return "Tamaño de hoja";
                case "02_DRAWN":return "Dibujante";case "02_DESIGN":return "Diseño";case "02_CHECKED":return "Revisor";case "02_APPROVED":return "Responsable / aprobación";
                case "02_DE_DATE":return "Fecha · dibujante";case "02_DR_DATE":return "Fecha · diseño";case "02_CH_DATE":return "Fecha · revisión";case "02_AP_DATE":return "Fecha · aprobación";
            }
            var reference=Regex.Match(s.Tag,@"^04_R\d+_(NO|DESCRIP)$");
            if(reference.Success)return reference.Groups[1].Value=="NO"?"PLANO":"TÍTULO";
            var m=Regex.Match(s.Tag,@"^R(\d+)_(BY|DS|CH|AP|NO|DATE|DESCRIPTION)$");if(!m.Success)return "";
            switch(m.Groups[2].Value)
            {
                case "BY":return "1 · DIB";case "DS":return "2 · DIS";
                case "CH":return "3 · REV";case "AP":return "4 · APR";
                case "NO":return "No.";case "DATE":return "FECHA";case "DESCRIPTION":return "DESCRIPCIÓN";
                default:return "";
            }
        }
    }

    public sealed class CellMap : UserControl
    {
        readonly DataGridView grid=new DataGridView();
        readonly ComboBox filter=new ComboBox();
        readonly Label details=new Label(),count=new Label();
        readonly CellPreview preview=new CellPreview();
        readonly CheckBox follow=new CheckBox {Text="Seguir Property",Checked=true,AutoSize=true,Padding=new Padding(3,5,0,0)};
        FieldIdentity selectedProperty;
        List<Slot> slots=new List<Slot>();
        Dictionary<ObjectId,Scene> scenes=new Dictionary<ObjectId,Scene>();
        bool binding;
        internal bool FollowsProperty=>follow.Checked;
        internal Slot CurrentSlot=>grid.CurrentRow?.Tag as Slot;
        public event Action<Slot> EditRequested;
        readonly Button edit=new Button {Text="Editar casilla...",AutoSize=true,Enabled=false};
        public CellMap()
        {
            var root=new TableLayoutPanel {Dock=DockStyle.Fill,ColumnCount=1,RowCount=3};
            root.RowStyles.Add(new RowStyle(SizeType.Absolute,32));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.Absolute,95));Controls.Add(root);
            var controls=new FlowLayoutPanel {Dock=DockStyle.Fill,WrapContents=false};
            controls.Controls.Add(new Label {Text="MAPA DE CASILLAS",AutoSize=true,Padding=new Padding(0,7,5,0)});
            filter.Name="CellMapFilter";filter.DropDownStyle=ComboBoxStyle.DropDownList;filter.Width=185;
            filter.Items.AddRange(new object[]{"Sin dato visible (. o vacío)","Sin dato y sin Field","Todas las casillas"});filter.SelectedIndex=0;
            filter.SelectedIndexChanged+=(s,e)=>Bind(null);controls.Controls.Add(filter);
            follow.CheckedChanged+=(s,e)=>{if(follow.Checked)FollowProperty(selectedProperty,true);};controls.Controls.Add(follow);
            count.AutoSize=true;count.Padding=new Padding(5,7,0,0);controls.Controls.Add(count);root.Controls.Add(controls,0,0);
            var content=new TableLayoutPanel {Dock=DockStyle.Fill,ColumnCount=2,RowCount=1};
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,68));content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,32));root.Controls.Add(content,0,1);
            grid.Name="CellMapGrid";grid.Text="CellMapGrid";grid.Dock=DockStyle.Fill;grid.ReadOnly=true;grid.AllowUserToAddRows=false;grid.AllowUserToDeleteRows=false;
            grid.RowHeadersVisible=false;grid.MultiSelect=false;grid.SelectionMode=DataGridViewSelectionMode.FullRowSelect;
            grid.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill;grid.BackgroundColor=System.Drawing.Color.White;
            foreach(var item in new[]{new {Name="Celda",Weight=65f},new{Name="Fila / columna",Weight=70f},new{Name="Estado",Weight=105f},new{Name="Field actual",Weight=100f}})
                grid.Columns.Add(new DataGridViewTextBoxColumn {HeaderText=item.Name,FillWeight=item.Weight,SortMode=DataGridViewColumnSortMode.NotSortable});
            grid.SelectionChanged+=(s,e)=>{if(!binding)Inspect();};content.Controls.Add(grid,0,0);
            preview.Name=preview.Text="MapPreview";preview.Dock=DockStyle.Fill;content.Controls.Add(preview,1,0);
            var footer=new Panel {Dock=DockStyle.Fill};details.Dock=DockStyle.Fill;edit.Dock=DockStyle.Bottom;
            footer.Controls.Add(details);footer.Controls.Add(edit);root.Controls.Add(footer,0,2);
            edit.Click+=(s,e)=>{var slot=grid.CurrentRow?.Tag as Slot;if(slot!=null)EditRequested?.Invoke(slot);};
            grid.CellDoubleClick+=(s,e)=>{if(e.RowIndex>=0)edit.PerformClick();};
        }
        internal static Slot[] Candidates(IEnumerable<Slot> slots,FieldIdentity selected,out bool nameOnly)
        {
            nameOnly=false;if(selected==null)return new Slot[0];
            var exact=FieldIdentity.Uses(slots,selected);
            if(exact.Length>0)return exact;
            var names=slots.Where(s=>CellStatus.HasField(s) && String.Equals(s.Property,selected.Property,StringComparison.OrdinalIgnoreCase)).ToArray();
            nameOnly=names.Length>0;return names;
        }
        public void FollowProperty(FieldIdentity selected,bool force=false)
        {
            bool changed=selectedProperty==null?selected!=null:!selectedProperty.SameProperty(selected);
            selectedProperty=selected;if(!follow.Checked || (!changed && !force))return;
            bool nameOnly;var candidates=Candidates(slots,selected,out nameOnly);
            if(candidates.Length==0)
            {
                binding=true;grid.ClearSelection();grid.CurrentCell=null;binding=false;
                edit.Enabled=false;
                preview.Update(null,null,System.Drawing.Color.Gray);
                details.Text=selected==null?"Selecciona una propiedad en Field o una casilla del mapa.":
                    "No hay una casilla asociada a esta propiedad en este bloque. El mapa permite consultar los espacios sin dato; no se asignan por el punto.";
                return;
            }
            var rows=grid.Rows.Cast<DataGridViewRow>().Where(r=>r.Tag is Slot && candidates.Any(s=>s.AttributeId==((Slot)r.Tag).AttributeId)).ToArray();
            if(rows.Length==0)
            {
                filter.SelectedIndex=2;
                rows=grid.Rows.Cast<DataGridViewRow>().Where(r=>r.Tag is Slot && candidates.Any(s=>s.AttributeId==((Slot)r.Tag).AttributeId)).ToArray();
            }
            if(rows.Length>0)
            {
                grid.CurrentCell=rows[0].Cells[0];grid.FirstDisplayedScrollingRowIndex=rows[0].Index;Inspect();
            }
        }
        public void FocusSlot(Slot slot)
        {
            if(!slots.Any(s=>s.AttributeId==slot.AttributeId))
            {
                Scene other;
                if(scenes.TryGetValue(slot.BlockId,out other))
                    SetCatalog(other.Texts.Where(t=>t.Slot!=null).Select(t=>t.Slot).Distinct(),scenes);
            }
            var row=grid.Rows.Cast<DataGridViewRow>().FirstOrDefault(r=>((Slot)r.Tag).AttributeId==slot.AttributeId);
            if(row==null){filter.SelectedIndex=2;row=grid.Rows.Cast<DataGridViewRow>().FirstOrDefault(r=>((Slot)r.Tag).AttributeId==slot.AttributeId);}
            if(row!=null){grid.CurrentCell=row.Cells[0];grid.FirstDisplayedScrollingRowIndex=row.Index;Inspect();}
        }
        public void SetCatalog(IEnumerable<Slot> value,Dictionary<ObjectId,Scene> sceneMap)
        {
            var previous=grid.CurrentRow?.Tag as Slot;
            slots=value.ToList();scenes=sceneMap;
            string selection=Convert.ToString(filter.SelectedItem);
            binding=true;
            while(filter.Items.Count>3)filter.Items.RemoveAt(filter.Items.Count-1);
            foreach(var row in slots.Select(CellStatus.Row).Where(r=>r!="Otros atributos").Distinct().OrderBy(r=>r))filter.Items.Add(row);
            filter.SelectedItem=filter.Items.Contains(selection)?selection:filter.Items[0];binding=false;
            Bind(previous);
        }
        void Bind(Slot previous)
        {
            if(binding)return;binding=true;
            string choice=Convert.ToString(filter.SelectedItem);
            IEnumerable<Slot> filtered=slots;
            if(choice=="Sin dato visible (. o vacío)")filtered=filtered.Where(CellStatus.WithoutData);
            else if(choice=="Sin dato y sin Field")filtered=filtered.Where(s=>CellStatus.WithoutData(s)&&!CellStatus.HasField(s));
            else if(filter.SelectedIndex>=3)filtered=filtered.Where(s=>CellStatus.Row(s)==choice);
            var displayed=filtered.OrderBy(CellStatus.Row).ThenByDescending(s=>Math.Round(s.Position.Y,3)).ThenBy(s=>s.Position.X).ToList();
            grid.Rows.Clear();
            foreach(var s in displayed)
            {
                string linked=CellStatus.HasField(s)?(s.Property.Length>0?s.Property:"Otro Field"):"Sin Field";
                int index=grid.Rows.Add(s.Tag,CellStatus.Row(s).Replace("Fila ","")+" / "+CellStatus.Column(s),CellStatus.State(s),linked);
                grid.Rows[index].Tag=s;grid.Rows[index].DefaultCellStyle.ForeColor=CellStatus.Color(s);
            }
            int linkedCount=displayed.Count(CellStatus.HasField);
            count.Text=displayed.Count+" · "+linkedCount+" con Field · "+(displayed.Count-linkedCount)+" sin Field";binding=false;
            if(displayed.Count>0)
            {
                int index=previous==null?-1:displayed.FindIndex(s=>s.AttributeId==previous.AttributeId);
                grid.CurrentCell=grid.Rows[Math.Max(0,index)].Cells[0];
            }
            Inspect();
        }
        void Inspect()
        {
            var s=grid.CurrentRow?.Tag as Slot;Scene scene=null;
            edit.Enabled=s!=null;
            if(s!=null)scenes.TryGetValue(s.BlockId,out scene);
            preview.Update(scene,s,s==null?System.Drawing.Color.Gray:CellStatus.Color(s));
            if(s==null){details.Text="No hay casillas con ese filtro. Sin dato y sin Field significa disponible para asignar en este bloque.";return;}
            var identity=FieldIdentity.Parse(s.Code);
            bool nameOnly=selectedProperty!=null && String.Equals(selectedProperty.Property,s.Property,StringComparison.OrdinalIgnoreCase) && !selectedProperty.SameProperty(identity);
            details.Text="CONSULTANDO: "+s.Tag+" ["+s.Layout+"] · "+CellStatus.Row(s)+" / "+CellStatus.Column(s)+" · Valor: "+(String.IsNullOrEmpty(s.Value)?"(vacío)":s.Value)+Environment.NewLine+
                (CellStatus.HasField(s)?"Field actual: "+s.Property+(identity==null?"":" · DST: "+System.IO.Path.GetFileName(identity.DatabasePath)):"No tiene Field.")+Environment.NewLine+
                (s.PendingEdit?"Edición pendiente: se aplica al cerrar Field y el editor de atributos.":nameOnly?"Solo coincide el nombre: el DST o ámbito es diferente. No es un uso de la propiedad elegida.":"Consultar esta casilla no cambia el destino del Field abierto.");
        }
    }
}
