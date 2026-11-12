using Crown.Core;
using System.Windows;
using System.Windows.Media.Media3D;

namespace Crown.Desktop;

internal static partial class WorldScene3D
{
    private static void CampActivity(SceneBatch batch,GameState s,City city)
    {
        var center=Terrain3D.Center(s.Tiles[city.Tile]);double x=center.X,z=center.Y;
        var random=new Random(unchecked(s.Seed+city.Id*9967));
        for(int i=0;i<3;i++)
        {
            double bx=x-.22+i*.09,bz=z-.25;
            if(!CampSiteDry(s,new(bx,bz)))continue;
            double y=Terrain3D.Height(s,bx,bz),r=.022+i%2*.005;
            var wicker=batch.Mesh("detail-camp-basket-wood","#8D7959");
            // An open rim and intersecting woven strips retain holes at close range.
            for(int ring=0;ring<6;ring++)for(int side=0;side<16;side++)
            {
                Point3D P(int j){double a=j*Math.PI/8,rr=r*(.78+.22*ring/5);return new(bx+Math.Cos(a)*rr,y+.004+ring*.006,bz+Math.Sin(a)*rr);}
                wicker.Tube(P(side),P(side+1),.0015,4);
            }
            for(int side=0;side<12;side++)
            {
                double a=side*Math.PI/6;
                wicker.Tube(new(bx+Math.Cos(a)*r*.78,y,bz+Math.Sin(a)*r*.78),new(bx+Math.Cos(a)*r,y+.036,bz+Math.Sin(a)*r),.0012,4);
            }
        }
        for(int i=0;i<6;i++)
        {
            double bx=x-.18+random.NextDouble()*.16,bz=z+.14+random.NextDouble()*.09;
            if(!CampSiteDry(s,new(bx,bz)))continue;
            Rock(batch,s,new(bx,bz),.008+random.NextDouble()*.008,.004+random.NextDouble()*.006,city.Id*13+i,"detail-stone-outcrop-flakes","#7D817B");
        }
        var wood=batch.Mesh("detail-camp-stacked-wood","#685541");
        for(int i=0;i<7;i++)
        {
            double bx=x+.18+(i%3)*.012,bz=z-.12+(i/3)*.012;
            if(!CampSiteDry(s,new(bx,bz)))continue;
            double y=Terrain3D.Height(s,bx,bz)+.008+(i/3)*.008;
            wood.Tube(new(bx-.045,y,bz),new(bx+.045,y+.003,bz+.012),.004,7);
        }
        // Nearby vegetation stays clear of the fire and the central working area.
        for(int i=0;i<12;i++)
        {
            double a=i*Math.PI/6,r=.80+random.NextDouble()*.08;var p=new Point(x+Math.Cos(a)*r,z+Math.Sin(a)*r);
            if(Terrain3D.TileAt(s,p.X,p.Y)!=city.Tile||!CampSiteDry(s,p))continue;
            UnderstoryClump(batch,s,p,.028+random.NextDouble()*.020,i%4==0?3:0,a);
        }
    }
}
