using System;
using System.IO;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using App=Autodesk.AutoCAD.ApplicationServices.Core.Application;
public partial class Builder {
 void RLong(){const double z=-20;Func<double,double> Y=value=>z+value;
 // Geomembrane system, following the reference including the anchor trench.
 double[] floor={-4.25,Y(2.0205),-3.65,Y(2.0205),-3.65,Y(1.5205),-3.05,Y(1.5205),-3.05,Y(2.0205),-2.996,Y(2.0205),0,Y(0),2,Y(0),2.75,Y(.5),6.4,Y(.5)};
 RHatch(new[]{-4.25,Y(2.0005),-3.67,Y(2.0005),-3.67,Y(1.5005),-3.03,Y(1.5005),-3.03,Y(2.0005),-2.996,Y(2.0005),0,Y(-.02),2,Y(-.02),2.75,Y(.48),6.4,Y(.48),6.4,Y(.18),2.75,Y(.18),2,Y(-.32),0,Y(-.32),-2.996,Y(1.7005),-4.25,Y(1.7005)},"DF_TERRENO","EARTH",.027);
 // Drainage aggregate around the original 300 mm assembly.
 RHatch(new[]{-.742,Y(.5),0,Y(.0),2,Y(0),2.75,Y(.5)},"DF_GRAVA","GRAVEL",.0038);
 RMesh(floor,.018,.07);var below=(double[])floor.Clone();for(int i=1;i<below.Length;i+=2)below[i]-=.025;Poly(below,"DF_GCL");
 RMesh(new[]{-3.04,Y(2.57),0,Y(.52),6.4,Y(.52)},.035,.085);
 // Concrete support and 200 mm apron, with true construction hatch.
 RMask(new[]{-3.88,Y(.52),-3.72,Y(.52),-3.72,Y(2.13),-3.20,Y(2.48),-3.29,Y(2.61),-3.88,Y(2.22)});
 RHatch(new[]{-3.88,Y(.52),-3.72,Y(.52),-3.72,Y(2.13),-3.20,Y(2.48),-3.29,Y(2.61),-3.88,Y(2.22)},"DF_CONCRETO","AR-CONC",.0015);
 Poly(new[]{-3.88,Y(.52),-3.72,Y(.52),-3.72,Y(2.13),-3.20,Y(2.48),-3.29,Y(2.61),-3.88,Y(2.22)},"DF_CONCRETO",true);
 RHatch(new[]{-4.25,Y(1.82),-3.65,Y(1.82),-3.65,Y(2.02),-4.25,Y(2.02)},"DF_CONCRETO","AR-CONC",.009);Line(-4.25,Y(2.02),-3.65,Y(2.02),"DF_GCL");
 // White interior mask is drawn before the original block, so gravel never crosses the pipe.
 var a=P(-3.43,Y(2.58));var b=P(.1,Y(.23418));var u=(b-a).GetNormal();var n=new Vector3d(-u.Y,u.X,0);var p1=a+n*.15;var p2=b+n*.15;var p3=b-n*.15;var p4=a-n*.15;RMask(new[]{p1.X,p1.Y,p2.X,p2.Y,p3.X,p3.Y,p4.X,p4.Y});RMask(new[]{-.05,Y(.08418),1.70,Y(.08418),1.70,Y(.38418),-.05,Y(.38418)});
 Add(new BlockReference(P(.1003894457,Y(.384179005)),rCodo){ScaleFactors=new Scale3d(-1,1,1)},"DF_TUBERIA");RPipe(-3.13,Y(2.38732),-3.927,Y(2.9245));RCap(-3.927,Y(2.9245),Math.PI-.593);
 // Black saddle clamp around the original pipe, with plate edges and fixing bolts.
 var clamp=P(-3.17,Y(2.43));var cu=new Vector3d(.829,-.559,0);var cn=new Vector3d(.559,.829,0);Func<double,double,Point3d> cp=(s,t)=>clamp+cu*s+cn*t;var ca=cp(-.025,-.185);var cb=cp(.025,-.185);var cc=cp(.025,.185);var cd=cp(-.025,.185);RMask(new[]{ca.X,ca.Y,cb.X,cb.Y,cc.X,cc.Y,cd.X,cd.Y});Poly(new[]{ca.X,ca.Y,cb.X,cb.Y,cc.X,cc.Y,cd.X,cd.Y},"DF_CONTORNO",true);foreach(double t in new[]{-.16,.16}){var c=cp(0,t);Circle(c.X,c.Y,.012,"DF_CONTORNO");}
 // Geomembrane boot with folded, welded edges at the penetration.
 Poly(new[]{-3.46,Y(2.05),-3.20,Y(2.32),-3.04,Y(2.57)},"DF_MEMBRANA");Poly(new[]{-3.43,Y(2.04),-3.17,Y(2.30),-3.01,Y(2.55)},"DF_MEMBRANA");Circle(-3.05,Y(2.55),.025,"DF_MEMBRANA");RDash(-3.40,Y(2.12),-3.10,Y(2.48),"DF_AUXILIAR",.045,.03);
 // Reference interruption runs through pipe and lining together.
 RBreak(-1.72,Y(1.39),-.593,.83);
 // Pond slope above the primary liner, shown as a continuous dashed indication.
 RDash(-2.9,Y(2.93),-.38,Y(1.22),"DF_AUXILIAR",.12,.07);
 // Three protection sheets beneath the reinforced-concrete slab.
 for(int i=0;i<3;i++)Line(3.70,Y(.56+i*.014),5.80,Y(.56+i*.014),"DF_MEMBRANA");
 RMask(new[]{3.75,Y(.60),5.75,Y(.60),5.75,Y(.80),3.75,Y(.80)});RHatch(new[]{3.75,Y(.60),5.75,Y(.60),5.75,Y(.80),3.75,Y(.80)},"DF_CONCRETO","AR-CONC",.012);Rect(3.75,Y(.60),2,.20,"DF_CONTORNO");Rect(3.69,Y(.815),2.12,.025,"DF_CONTORNO");
 Line(3.79,Y(.635),5.71,Y(.635),"DF_TUBERIA");for(int i=0;i<10;i++)Circle(3.84+i*.2,Y(.645),.014,"DF_TUBERIA");foreach(double xx in new[]{4.05,4.75,5.45}){Rect(xx-.012,Y(.74),.024,.10,"DF_CONCRETO");Line(xx-.025,Y(.84),xx+.025,Y(.84),"DF_CONTORNO");}
 // Pump slide by mine operation: ghosted outline and dashed vertical ribs.
 for(int i=0;i<7;i++){double xx=3.85+i*.3;RDash(xx,Y(.86),xx,Y(2.45),"DF_AUXILIAR",.075,.06);}RDash(3.85,Y(2.45),5.65,Y(2.45),"DF_AUXILIAR",.10,.06);for(int i=0;i<32;i+=2){double t=i*Math.PI/16;Line(4.75+.95*Math.Cos(t),Y(2.14+.065*Math.Sin(t)),4.75+.95*Math.Cos(t+Math.PI/32),Y(2.14+.065*Math.Sin(t+Math.PI/32)),"DF_AUXILIAR");}
 // All construction dimensions are native AutoCAD dimensions.
 RDim(0,Y(0),2,Y(0),1,Y(-.40),.02);RDim(2,Y(0),2,Y(.5),2.16,Y(.25),.02,Math.PI/2);RDim(2.75,Y(.5),3.75,Y(.5),3.25,Y(.20),.02);RDim(3.75,Y(.5),5.75,Y(.5),4.75,Y(.20),.02);RDim(5.75,Y(.6),5.75,Y(.8),5.96,Y(.68),.02,Math.PI/2);RDim(-3.65,Y(1.52),-3.05,Y(1.52),-3.35,Y(1.31),.02);RDim(-3.05,Y(1.52),-3.05,Y(2.02),-2.88,Y(1.77),.02,Math.PI/2);RDim(-4.25,Y(1.82),-4.25,Y(2.02),-4.46,Y(1.92),.02,Math.PI/2);RDim(-4.25,Y(1.02),-4.25,Y(2.02),-4.46,Y(1.52),.02,Math.PI/2);RDim(-4.25,Y(.52),-4.25,Y(1.02),-4.46,Y(.77),.02,Math.PI/2);
 RText("(MÍN.)",-4.26,Y(1.56),.05,.38);RText("(MÍN.)",-4.26,Y(.81),.05,.38);Slope(-2.53,Y(1.63),1.5,.11,.048);Slope(-.87,Y(.50),1.5,.11,.048);Slope(2.36,Y(.18),1.5,.10,.048);
 Line(2.35,Y(.75),3.20,Y(.767),"DF_COTAS");RArrow(2.35,Y(.75),-1,-.02,.08,"DF_COTAS");RText("2% (MÍN.)",2.45,Y(.92),.058,.85);
 // Closely follow the reference's separated rows of callouts.
 RCall("TAPA DE HDPE",-4.04,Y(3.00),-3.85,Y(3.60),.05,1.15);RCall("SOPORTE DEL ELEVADOR",-3.17,Y(2.43),-2.59,Y(3.32),.05,1.50);RCall("BOTA DE GEOMEMBRANA",-3.16,Y(2.34),-1.47,Y(3.18),.05,1.50);RCall("TALUD DE LA POZA",-1.08,Y(1.70),-.52,Y(2.88),.05,1.35);
 RCall("GEOMEMBRANA PRIMARIA LISA\\PDE HDPE DE 1.5 mm",-.80,Y(1.07),.15,Y(2.60),.05,1.90);RCall("GEONET",-.40,Y(.81),1.34,Y(2.18),.05,.85);RCall("TUBERÍA DE HDPE PERFORADA O\\PNO PERFORADA PD Ø300 mm",-.45,Y(.58),2.00,Y(1.89),.05,2.20);RCall("ENVOLTURA DE GEOTEXTIL\\PNO TEJIDO DE 270 g/m²",.66,Y(.43),2.68,Y(1.49),.05,1.90);RCall("GRAVA PARA DRENAJE",2.18,Y(.28),3.18,Y(1.13),.05,1.45);
 RCall("TRINCHERA DE ANCLAJE\\P(VER NOTA 4)",-3.06,Y(1.52),-2.69,Y(1.12),.05,1.55);RCall("ANCLAJE DE SOPORTE PARA ELEVADOR\\P(CONCRETO f'c = 210 kg/cm²)",-3.77,Y(.90),-3.45,Y(.10),.05,2.70);RCall("GCL",-2.25,Y(1.47),-2.66,Y(.69),.05,.55);RCall("FONDO DE TRINCHERA\\PPARA TUBERÍA DEL ELEVADOR",-1.12,Y(.75),-1.80,Y(.40),.05,2.10);RCall("GEOMEMBRANA SECUNDARIA LISA\\PDE HDPE DE 1.5 mm",-.52,Y(.35),-2.70,Y(-.40),.05,2.50);RCall("CODO DE 30° DE HDPE PD Ø300 mm",.10,Y(.10),-.88,Y(-.68),.05,2.40);RCall("TUBERÍA DE HDPE\\PPERFORADA PD Ø300 mm",1.70,Y(.20),2.34,Y(-.44),.05,1.70);
 RCall("TOBOGÁN PARA PROTECCIÓN DE BOMBAS\\P(POR OPERACIÓN MINA)",3.85,Y(2.15),2.12,Y(2.88),.05,2.50);RCall("TRES LÁMINAS DE PROTECCIÓN DE GEOMEMBRANA\\PDE HDPE DE 1.5 mm BAJO LOSA DE CONCRETO",3.72,Y(.57),4.00,Y(2.69),.05,3.20);RCall("PLANCHA DE FIERRO PARA SOPORTE\\PDE TOBOGÁN SOLDADO (VER NOTA 5)",4.10,Y(.83),5.00,Y(2.17),.05,2.40);RCall("LOSA DE CONCRETO ARMADO CON MALLA SIMPLE\\PDE 3/8\" @ 200 mm, f'c = 280 kg/cm²",5.20,Y(.72),5.73,Y(1.57),.05,2.35);
 }
}
