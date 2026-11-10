using System.Windows;
using System.Windows.Media.Media3D;

namespace Crown.Desktop;

internal sealed partial class SceneMesh
{
    // Catmull-Rom cross sections produce a continuous silhouette with smooth normals.
    internal void SoftHull(double x,double y,double z,(double Height,double RadiusX,double RadiusZ,double OffsetZ)[] sections,int sides=18,bool horizontal=false)
    {
        var samples=new List<(double H,double X,double Z,double O)>();
        int subdivisions=Simplified?1:3;sides=Simplified?8:sides;
        for(int i=0;i<sections.Length-1;i++)for(int j=0;j<subdivisions;j++)
        {
            var a=sections[Math.Max(0,i-1)];var b=sections[i];var c=sections[i+1];var d=sections[Math.Min(sections.Length-1,i+2)];double t=j/(double)subdivisions;
            double C(double aa,double bb,double cc,double dd)=>.5*((2*bb)+(-aa+cc)*t+(2*aa-5*bb+4*cc-dd)*t*t+(-aa+3*bb-3*cc+dd)*t*t*t);
            samples.Add((b.Height+(c.Height-b.Height)*t,Math.Max(.001,C(a.RadiusX,b.RadiusX,c.RadiusX,d.RadiusX)),Math.Max(.001,C(a.RadiusZ,b.RadiusZ,c.RadiusZ,d.RadiusZ)),C(a.OffsetZ,b.OffsetZ,c.OffsetZ,d.OffsetZ)));
        }
        var end=sections[^1];samples.Add((end.Height,end.RadiusX,end.RadiusZ,end.OffsetZ));
        Point3D P(int row,int side)
        {
            var r=samples[row];double a=side*Math.PI*2/sides;
            return horizontal?new(x+Math.Cos(a)*r.X,y+r.O+Math.Sin(a)*r.Z,z+r.H):new(x+Math.Cos(a)*r.X,y+r.H,z+r.O+Math.Sin(a)*r.Z);
        }
        Vector3D N(int row,int side)
        {
            var tangent=P(Math.Min(samples.Count-1,row+1),side)-P(Math.Max(0,row-1),side);
            var around=P(row,side+1)-P(row,side-1);var n=Vector3D.CrossProduct(tangent,around);if(horizontal)n=-n;n.Normalize();return n;
        }
        for(int row=0;row<samples.Count-1;row++)for(int side=0;side<sides;side++)
        {
            var a=P(row,side);var b=P(row+1,side);var c=P(row+1,side+1);var d=P(row,side+1);
            var na=N(row,side);var nb=N(row+1,side);var nc=N(row+1,side+1);var nd=N(row,side+1);
            Point UV(int r,int i)=>new(i/(double)sides,r/(double)(samples.Count-1));
            void Tri(Point3D aa,Point3D bb,Point3D cc,Vector3D an,Vector3D bn,Vector3D cn,Point at,Point bt,Point ct)
            {if(Vector3D.DotProduct(Vector3D.CrossProduct(bb-aa,cc-aa),an+bn+cn)<0){(bb,cc)=(cc,bb);(bn,cn)=(cn,bn);(bt,ct)=(ct,bt);}Triangle(aa,bb,cc,an,bn,cn,at,bt,ct);}
            Tri(a,b,c,na,nb,nc,UV(row,side),UV(row+1,side),UV(row+1,side+1));Tri(a,c,d,na,nc,nd,UV(row,side),UV(row+1,side+1),UV(row,side+1));
        }
    }
}
