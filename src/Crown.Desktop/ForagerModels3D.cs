using System.Windows;
using System.Windows.Media.Media3D;

namespace Crown.Desktop;

internal static partial class WorldScene3D
{
    private static void Forager(SceneBatch batch,double x,double y,double z,int identity)
    {
        int variant=CharacterAppearance.Variant(identity);double h=CharacterAppearance.Height(variant);
        batch.Character(new(x,y,z),h,()=>
        {
        ActorAssets.Draw(batch,7,x,y,z,h,identity:identity);
        batch.Organic(()=>
        {
            var garment=batch.Mesh("camp-hide-garment-"+variant,CharacterAppearance.Hide(variant));
            Tunic(garment,x,y,z,h,variant);
            // Two hide shoulder strips carry the tunic; the torso covering is not a strapless cylinder.
            for(int side=-1;side<=1;side+=2)
            {
                var strap=batch.Mesh("camp-hide-shoulder-"+variant,CharacterAppearance.Hide(variant));
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
                bool Scalp(int v){var p=model.Positions[v];return p.Y>(variant%2==0?.967:.952)||p.Y>(variant%2==0?.91:.89)&&p.Z>-.025;}
                if(!Scalp(a)||!Scalp(b)||!Scalp(c))continue;
                Point3D P(int v){var p=model.Positions[v];return new Point3D(x+p.X*h,y+p.Y*h,z+p.Z*h)+model.Normals[v]*(.002*h);}
                hair.Triangle(P(a),P(b),P(c),model.Normals[a],model.Normals[b],model.Normals[c]);
            }
        });
        },identity);
    }
    private static void Tunic(SceneMesh mesh,double x,double y,double z,double h,int variant)
    {
        (double Y,double X,double Z)[] sections=[(.34,.133,.085),(.41,.123,.085),(.52,.115,.085),(.63,.105,.085),(.72,.122,.079),(.78,.116,.067),(.815,.040,.044)];
        int sides=mesh.Simplified?12:32;
        Point3D P(int row,double a)
        {
            var ring=sections[row];double folds=1+.022*Math.Sin(a*9+variant)*Math.Sin(row*.9+.4);
            double hem=row==0?.012*Math.Sin(a*5+variant)+.004*Math.Sin(a*11):0;
            return new(x+Math.Cos(a)*ring.X*h*folds,y+(ring.Y+hem)*h,z-.025*h+Math.Sin(a)*ring.Z*h*folds);
        }
        Vector3D N(int row,double a)
        {
            var along=P(Math.Min(sections.Length-1,row+1),a)-P(Math.Max(0,row-1),a);
            var around=P(row,a+.001)-P(row,a-.001);var n=Vector3D.CrossProduct(along,around);n.Normalize();return n;
        }
        void Tri(int row,double a,double b,bool second)
        {
            var p=P(row,a);var q=second?P(row+1,b):P(row+1,a);var r=second?P(row,b):P(row+1,b);
            var np=N(row,a);var nq=second?N(row+1,b):N(row+1,a);var nr=second?N(row,b):N(row+1,b);
            var uv=new Point(a/(2*Math.PI),row/6d);var uq=new Point((second?b:a)/(2*Math.PI),(row+1)/6d);var ur=new Point(b/(2*Math.PI),(second?row:row+1)/6d);
            if(Vector3D.DotProduct(Vector3D.CrossProduct(q-p,r-p),np+nq+nr)<0){(q,r)=(r,q);(nq,nr)=(nr,nq);(uq,ur)=(ur,uq);}
            mesh.Triangle(p,q,r,np,nq,nr,uv,uq,ur);
        }
        for(int row=0;row<sections.Length-1;row++)for(int i=0;i<sides;i++){double a=i*Math.PI*2/sides,b=(i+1)*Math.PI*2/sides;Tri(row,a,b,false);Tri(row,a,b,true);}
    }
}
