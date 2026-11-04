using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using AcApp=Autodesk.AutoCAD.ApplicationServices.Core.Application;
using Font=System.Drawing.Font;

namespace LocalizarCasillas
{
    public sealed class LocatorPanel : Form
    {
        readonly Document document;
        readonly Marker marker=new Marker();
        readonly DataGridView grid=new DataGridView();
        readonly TextBox filter=new TextBox();
        readonly TextBox details=new TextBox();
        readonly TextBox code=new TextBox();
        readonly Label status=new Label();
        readonly CheckBox zoom=new CheckBox();
        readonly Timer delay=new Timer {Interval=180};
        List<Slot> all=new List<Slot>();
        List<Slot> shown=new List<Slot>();
        bool binding;
        ObjectId blockFilter=ObjectId.Null;
        public Document Document => document;
        public Slot Selected => grid.CurrentRow!=null && grid.CurrentRow.Index<shown.Count ? shown[grid.CurrentRow.Index] : null;

        public LocatorPanel(Document doc)
        {
            document=doc;
            Text="Localizar casillas · prueba 2024"; StartPosition=FormStartPosition.Manual;
            Size=new Size(660,790); MinimumSize=new Size(570,640);
            var area=Screen.PrimaryScreen.WorkingArea;
            Location=new Point(Math.Max(area.Left,area.Right-Width-16),area.Top+55);
            Font=new Font("Segoe UI",9); BackColor=Color.FromArgb(245,247,250);
            var root=new TableLayoutPanel {Dock=DockStyle.Fill,ColumnCount=1,RowCount=11,Padding=new Padding(14)};
            root.RowStyles.Add(new RowStyle(SizeType.Absolute,57));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute,33));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute,29));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute,31));
            root.RowStyles.Add(new RowStyle(SizeType.Percent,100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute,40));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute,64));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute,22));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute,74));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute,40));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute,49));
            Controls.Add(root);
            root.Controls.Add(new Label {Text="Encuentra la casilla antes de editar\nSelecciona una fila: la marca roja señala su posición en el plano.",AutoSize=false,Dock=DockStyle.Fill,Font=new Font(Font.FontFamily,10,FontStyle.Bold)},0,0);
            var tools=new FlowLayoutPanel {Dock=DockStyle.Fill,WrapContents=false};
            tools.Controls.Add(Button("Elegir bloque",()=>Queue("CASILLASBLOQUE")));
            tools.Controls.Add(Button("Todo el plano",()=>{blockFilter=ObjectId.Null;RefreshCatalog();}));
            tools.Controls.Add(Button("Actualizar",RefreshCatalog)); root.Controls.Add(tools,0,1);
            root.Controls.Add(new Label {Text="Buscar atributo, propiedad, valor, bloque o presentación:",Dock=DockStyle.Fill,TextAlign=ContentAlignment.BottomLeft},0,2);
            filter.Dock=DockStyle.Fill;filter.TextChanged+=(s,e)=>Bind();root.Controls.Add(filter,0,3);
            grid.Dock=DockStyle.Fill; grid.ReadOnly=true; grid.AllowUserToAddRows=false;grid.AllowUserToDeleteRows=false;
            grid.RowHeadersVisible=false;grid.MultiSelect=false;grid.SelectionMode=DataGridViewSelectionMode.FullRowSelect;
            grid.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill;grid.BackgroundColor=Color.White;
            grid.AutoGenerateColumns=false;
            grid.Columns.Add(new DataGridViewTextBoxColumn {Name="layout",HeaderText="Layout",FillWeight=55});
            grid.Columns.Add(new DataGridViewTextBoxColumn {Name="tag",HeaderText="Atributo",FillWeight=85});
            grid.Columns.Add(new DataGridViewTextBoxColumn {Name="property",HeaderText="Propiedad vinculada",FillWeight=140});
            grid.Columns.Add(new DataGridViewTextBoxColumn {Name="value",HeaderText="Valor",FillWeight=75});
            grid.SelectionChanged+=(s,e)=>{if(!binding){delay.Stop();delay.Start();}};
            grid.CellDoubleClick+=(s,e)=>Locate();root.Controls.Add(grid,0,4);
            delay.Tick+=(s,e)=>{delay.Stop();Locate();};
            var nav=new FlowLayoutPanel {Dock=DockStyle.Fill,WrapContents=false};
            nav.Controls.Add(Button("◀ Anterior",()=>Step(-1)));nav.Controls.Add(Button("Siguiente ▶",()=>Step(1)));
            nav.Controls.Add(Button("Ubicar",Locate));nav.Controls.Add(Button("Ver bloque",Overview));
            zoom.Text="Acercar";zoom.Checked=true;zoom.AutoSize=true;zoom.Padding=new Padding(0,6,0,0);nav.Controls.Add(zoom);
            root.Controls.Add(nav,0,5);
            details.Dock=DockStyle.Fill;details.Multiline=true;details.ReadOnly=true;details.BorderStyle=BorderStyle.FixedSingle;root.Controls.Add(details,0,6);
            root.Controls.Add(new Label {Text="Vínculo guardado en el DWG (solo lectura):",Dock=DockStyle.Fill,TextAlign=ContentAlignment.BottomLeft},0,7);
            code.Dock=DockStyle.Fill;code.Multiline=true;code.ReadOnly=true;code.ScrollBars=ScrollBars.Vertical;root.Controls.Add(code,0,8);
            var edits=new FlowLayoutPanel {Dock=DockStyle.Fill,WrapContents=false};
            edits.Controls.Add(Button("Editar atributo / vínculo…",Edit));
            edits.Controls.Add(Button("Quitar marca",()=>marker.Clear()));
            edits.Controls.Add(Button("Cerrar",Close));root.Controls.Add(edits,0,9);
            status.Dock=DockStyle.Fill;status.ForeColor=Color.FromArgb(45,60,85);root.Controls.Add(status,0,10);
            AcApp.DocumentManager.DocumentToBeDeactivated+=Deactivating;
            AcApp.DocumentManager.DocumentToBeDestroyed+=Destroying;
            AcApp.DocumentManager.DocumentActivated+=DocumentActivated;
            FormClosed+=(s,e)=>{
                delay.Dispose();marker.Dispose();
                AcApp.DocumentManager.DocumentToBeDeactivated-=Deactivating;
                AcApp.DocumentManager.DocumentToBeDestroyed-=Destroying;
                AcApp.DocumentManager.DocumentActivated-=DocumentActivated;
            };
        }
        Button Button(string text,Action action)
        {
            var button=new Button {Text=text,AutoSize=true,Height=29,Margin=new Padding(0,3,6,0)};
            button.Click+=(s,e)=>Run(action);return button;
        }
        public void Run(Action action)
        {
            try
            {
                if(AcApp.DocumentManager.MdiActiveDocument!=document) {status.Text="Activa el plano: "+System.IO.Path.GetFileName(document.Name);return;}
                if(Convert.ToInt32(AcApp.GetSystemVariable("CMDACTIVE"))!=0) {status.Text="Termina el comando activo y vuelve a pulsar Ubicar.";return;}
                action();
            }
            catch(System.Exception ex) {status.Text="No se pudo completar: "+ex.Message;document.Editor.WriteMessage("\nCASILLAS: "+ex.Message);}
        }
        public void RefreshCatalog()
        {
            using(document.LockDocument()) all=Catalog.Read(document.Database,blockFilter);
            Bind();
        }
        public void Restrict(ObjectId block) {blockFilter=block;filter.Clear();RefreshCatalog();}
        public void Search(string text){filter.Text=text;}
        void Bind()
        {
            delay.Stop();marker.Clear();binding=true;
            var tokens=filter.Text.Split(new[]{' '},StringSplitOptions.RemoveEmptyEntries);
            shown=all.Where(s=>tokens.All(t=>s.SearchText.IndexOf(t,StringComparison.OrdinalIgnoreCase)>=0)).ToList();
            grid.Rows.Clear();
            foreach(var s in shown)grid.Rows.Add(s.Layout,s.Tag,s.Property.Length==0?"Sin campo":s.Property,s.Value);
            binding=false;
            status.Text=shown.Count+" casillas · "+System.IO.Path.GetFileName(document.Name)+"\nLas marcas son temporales; no se guardan ni se imprimen.";
            details.Text="";code.Text="";
            if(shown.Count>0) {grid.CurrentCell=grid.Rows[0].Cells[1];delay.Start();}
        }
        public void Locate() => Run(()=>{
            var s=Selected;if(s==null)return;
            marker.Show(document,s,zoom.Checked);
            details.Text="CASILLA: "+s.Tag+"     ·     Layout: "+s.Layout+Environment.NewLine+
                "Bloque: "+s.Path+"     ·     ID: "+s.Handle+Environment.NewLine+
                (s.Invisible?"Atributo invisible: se indica su posición guardada.":s.Nested?"Atributo anidado: localización disponible; edición deshabilitada.":s.CellBounds.HasValue?"Se marca la celda delimitada por las líneas del bloque.":"Se marca la posición del atributo; no se detectaron cuatro bordes de celda.");
            code.Text=String.IsNullOrEmpty(s.Code)?"Este atributo no contiene un campo vinculado.":s.Code;
            status.Text="Mostrando "+(grid.CurrentRow.Index+1)+" de "+shown.Count+" · "+s.Tag+"\n"+
                (s.Property.Length>0?"Propiedad: "+s.Property:"Texto sin vínculo");
        });
        void Step(int delta)
        {
            if(shown.Count==0)return;
            int index=(Math.Max(0,grid.CurrentRow?.Index??0)+delta+shown.Count)%shown.Count;
            grid.CurrentCell=grid.Rows[index].Cells[1];Locate();
        }
        void Overview()
        {
            var s=Selected;if(s==null)return;
            marker.Show(document,s,false);
            using(document.LockDocument())Marker.Zoom(document.Editor,s.BlockBounds,s.Height*30);
            document.Editor.UpdateScreen();
        }
        void Edit()
        {
            var s=Selected;if(s==null)return;
            if(s.Nested){status.Text="La casilla pertenece a un bloque anidado. Esta prueba permite localizarla, pero no editar una definición compartida.";return;}
            Locate();
            Commands.Pending=s;
            Queue("CASILLASEDITAR");
        }
        void Queue(string command) {delay.Stop();document.SendStringToExecute(command+" ",true,false,false);}
        public void BeforeNativeEdit(){delay.Stop();Hide();}
        public void AfterNativeEdit(){Show();RefreshCatalog();}
        void Deactivating(object sender,DocumentCollectionEventArgs e){if(e.Document==document){delay.Stop();marker.Clear();}}
        void Destroying(object sender,DocumentCollectionEventArgs e){if(e.Document==document)Close();}
        void DocumentActivated(object sender,DocumentCollectionEventArgs e){if(e.Document==document)Run(RefreshCatalog);}
    }
}
