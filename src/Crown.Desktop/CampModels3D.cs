using Crown.Core;
using System.Windows;
using System.Windows.Media.Media3D;

namespace Crown.Desktop;

internal static partial class WorldScene3D
{
    // A curved, open shelter: sewn hide panels over bent timber, rather than a closed cone.
    private static void HideShelter(SceneBatch batch,GameState s,double x,double z,double yaw,int seed)
    {
        double y=Terrain3D.Height(s,x,z),width=.125+(seed%3)*.009,height=.145+(seed%4)*.009;
        var origin=new Point3D(x,y,z);
        batch.Rotated(origin,yaw,()=>batch.Organic(()=>
        {
            var hide=batch.Mesh("camp-hide","#99816A");int sides=hide.Simplified?12:28,rings=hide.Simplified?4:10;
            Point3D P(double a,double t)
            {
                double phi=t*Math.PI*.5,r=Math.Cos(phi),drape=1+.022*Math.Sin(a*9+seed)*Math.Pow(1-t,2);
                return new(x+Math.Cos(a)*width*r*drape,y+Math.Sin(phi)*height,z+Math.Sin(a)*width*1.35*r*drape);
            }
            Vector3D N(double a,double t)
            {
                // Ellipsoid normals remain smooth across panel boundaries and finite at the crown.
                double phi=t*Math.PI*.5;var n=new Vector3D(Math.Cos(a)*Math.Cos(phi)/width,Math.Sin(phi)/height,Math.Sin(a)*Math.Cos(phi)/(width*1.35));n.Normalize();return n;
            }
            void Tri(double a,double aa,double t,double tt,bool second)
            {
                var p=P(a,t);var q=second?P(aa,tt):P(a,tt);var r=second?P(aa,t):P(aa,tt);
                var np=N(a,t);var nq=second?N(aa,tt):N(a,tt);var nr=second?N(aa,t):N(aa,tt);
                var uv=new Point(a/(Math.PI*2),t);var vq=new Point((second?aa:a)/(Math.PI*2),tt);var vr=new Point(aa/(Math.PI*2),second?t:tt);
                if(Vector3D.DotProduct(Vector3D.CrossProduct(q-p,r-p),np+nq+nr)<0){(q,r)=(r,q);(nq,nr)=(nr,nq);(vq,vr)=(vr,vq);}
                hide.Triangle(p,q,r,np,nq,nr,uv,vq,vr);
            }
            for(int row=0;row<rings;row++)for(int side=0;side<sides;side++)
            {
                double a=side*Math.PI*2/sides,aa=(side+1d)*Math.PI*2/sides,t=(double)row/rings,tt=(row+1d)/rings;
                // The south-facing opening exposes the dark interior and actual ground.
                if(Math.Sin((a+aa)*.5)>.82&&t<.60)continue;
                Tri(a,aa,t,tt,false);Tri(a,aa,t,tt,true);
            }
            var frame=batch.Mesh("camp-frame-wood","#66513D");
            for(int rib=0;rib<(hide.Simplified?3:5);rib++)
            {
                double a=rib*Math.PI*2/5+.28;
                for(int j=0;j<6;j++)frame.Tube(P(a,j/6d),P(a,(j+1d)/6),.0025,5);
            }
            var seam=batch.Mesh("camp-seams-leather","#5B4938");
            for(int panel=0;panel<(hide.Simplified?0:7);panel++)
            {
                double a=panel*Math.PI*2/7;
                if(Math.Sin(a)>.82)continue;
                for(int j=0;j<6;j++)seam.Tube(P(a,j/7d),P(a,(j+1d)/7),.0009,4);
            }
            foreach(int side in new[]{-1,1})frame.Tube(new(x+side*width*.48,y,z+width*1.17),new(x+side*width*.30,y+height*.72,z+width*.98),.003,6);
        }));
    }
    private static void CampGround(SceneBatch batch,GameState s,double x,double z)
    {
        var mesh=batch.Mesh("camp-ground-silt","#6C6655");
        for(int ring=0;ring<4;ring++)for(int side=0;side<32;side++)
        {
            double u=ring/4d,v=(ring+1d)/4d,a=side*Math.PI/16,aa=(side+1)*Math.PI/16;
            Point3D P(double t,double angle)=>Terrain3D.Ground(s,x+Math.Cos(angle)*.24*t,z+Math.Sin(angle)*.24*t,.004);
            mesh.Triangle(P(u,a),P(v,aa),P(v,a),new(0,1,0),new(0,1,0),new(0,1,0),new(u,0),new(v,0),new(v,0));
            mesh.Triangle(P(u,a),P(u,aa),P(v,aa),new(0,1,0),new(0,1,0),new(0,1,0),new(u,0),new(u,0),new(v,0));
        }
    }
    internal static bool CampSiteDry(GameState s,Point point)=>TerrainField.Current(s).Rivers.Distance(point)>.48&&Terrain3D.Height(s,point.X,point.Y)>Terrain3D.SeaLevel+.035;
}
