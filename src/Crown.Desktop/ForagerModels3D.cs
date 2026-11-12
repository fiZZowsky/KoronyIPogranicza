using System.Windows;
using System.Windows.Media.Media3D;

namespace Crown.Desktop;

internal static partial class WorldScene3D
{
    private static void Forager(SceneBatch batch,double x,double y,double z)
    {
        const double h=.43;
        ActorAssets.Draw(batch,7,x,y,z,h);
        batch.Organic(()=>
        {
            var garment=batch.Mesh("camp-hide-garment","#77624F");
            garment.Hull(x,y,z-.025*h,[
                (.34*h,.133*h,.085*h,0),(.41*h,.123*h,.085*h,0),(.52*h,.115*h,.085*h,0),
                (.63*h,.105*h,.085*h,0),(.72*h,.122*h,.079*h,0),(.78*h,.116*h,.067*h,0),(.815*h,.040*h,.044*h,0)],garment.Simplified?12:24);
            // Two hide shoulder strips carry the tunic; the torso covering is not a strapless cylinder.
            for(int side=-1;side<=1;side+=2)
            {
                var strap=batch.Mesh("camp-hide-shoulder","#77624F");
                Point3D P(double v,int edge)=>new(x+(side*.071+edge*.018)*h,y+(.756+.068*Math.Sin(v*Math.PI))*h,z+(-.106+.16*v)*h);
                for(int i=0;i<8;i++)strap.Quad(P(i/8d,-1),P((i+1)/8d,-1),P((i+1)/8d,1),P(i/8d,1),new(0,1,0));
            }
            var belt=batch.Mesh("camp-seams-leather","#453A2E");
            for(int i=0;i<20;i++)
            {
                Point3D P(int j)=>new(x+Math.Cos(j*Math.PI/10)*.118*h,y+.53*h,z-.025*h+Math.Sin(j*Math.PI/10)*.087*h);
                belt.Tube(P(i),P(i+1),.004*h,5);
            }
            // Hair follows the anatomical scalp, avoiding a separate spherical helmet.
            var model=ActorAssets.Load(7)[garment.Simplified?1:0];var hair=batch.Mesh("forager-hair","#30291F");
            for(int i=0;i<model.Indices.Length;i+=3)
            {
                int a=model.Indices[i],b=model.Indices[i+1],c=model.Indices[i+2];
                bool Scalp(int v){var p=model.Positions[v];return p.Y>.967||p.Y>.91&&p.Z>-.025;}
                if(!Scalp(a)||!Scalp(b)||!Scalp(c))continue;
                Point3D P(int v){var p=model.Positions[v];return new Point3D(x+p.X*h,y+p.Y*h,z+p.Z*h)+model.Normals[v]*(.002*h);}
                hair.Triangle(P(a),P(b),P(c),model.Normals[a],model.Normals[b],model.Normals[c]);
            }
        });
    }
}
