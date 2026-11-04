using System;
using System.Drawing;
using System.Windows.Forms;
using LocalizarCasillas;

namespace CasillasField
{
    public sealed class CellValueEditor : Form
    {
        readonly TextBox value=new TextBox {Name="CellValue",Multiline=true,Dock=DockStyle.Fill,ScrollBars=ScrollBars.Vertical};
        public string Value=>value.Text;
        public CellValueEditor(Slot slot,Scene scene,string initial,string impact,bool linked)
        {
            Text="Editar casilla · "+slot.Tag;Size=new Size(650,560);MinimumSize=Size;
            StartPosition=FormStartPosition.CenterParent;Font=new Font("Segoe UI",9);ShowInTaskbar=false;
            var layout=new TableLayoutPanel {Dock=DockStyle.Fill,Padding=new Padding(12),ColumnCount=1,RowCount=5};Controls.Add(layout);
            foreach(int h in new[]{55,170,120,110,40})layout.RowStyles.Add(new RowStyle(SizeType.Absolute,h));
            var caption=new Label {Dock=DockStyle.Fill};layout.Controls.Add(caption);
            string heading=slot.Tag+" ["+slot.Layout+"] · "+CellStatus.Row(slot)+" / "+CellStatus.Column(slot)+"\n"+(linked?"Se conserva el Field: "+slot.Property:"Casilla sin Field · se edita su texto local");
            caption.Text=heading+"\nVista previa: "+initial;
            var preview=new CellPreview {Dock=DockStyle.Fill};
            var draft=new Slot {AttributeId=slot.AttributeId,BlockId=slot.BlockId,Tag=slot.Tag,Value=initial,Height=slot.Height,Position=slot.Position,Bounds=slot.Bounds,CellBounds=slot.CellBounds};
            var draftScene=new Scene();if(scene!=null)
            {
                draftScene.Edges.AddRange(scene.Edges);
                foreach(var text in scene.Texts)draftScene.Texts.Add(text.WithSlot(text.Slot==slot?draft:text.Slot));
            }
            preview.Update(draftScene,draft,Color.DarkOrange);layout.Controls.Add(preview);
            value.TextChanged+=(s,e)=>{draft.Value=value.Text;caption.Text=heading+"\nVista previa: "+value.Text;preview.Invalidate();};
            layout.Controls.Add(new Label {Dock=DockStyle.Fill,Text=impact});value.Text=initial;layout.Controls.Add(value);
            var buttons=new FlowLayoutPanel {Dock=DockStyle.Fill,FlowDirection=FlowDirection.RightToLeft};layout.Controls.Add(buttons);
            var cancel=new Button {Text="Cancelar",DialogResult=DialogResult.Cancel,AutoSize=true};
            var save=new Button {Text=linked?"Guardar en Sheet Set":"Guardar al cerrar editor",DialogResult=DialogResult.OK,AutoSize=true};
            var point=new Button {Text="Dejar punto",AutoSize=true};point.Click+=(s,e)=>value.Text=".";
            buttons.Controls.Add(cancel);buttons.Controls.Add(save);buttons.Controls.Add(point);AcceptButton=save;CancelButton=cancel;
        }
    }
}
