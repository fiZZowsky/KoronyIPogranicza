using Crown.Core;
using System.Windows.Media.Media3D;

namespace Crown.Desktop;

internal static partial class WorldScene3D
{
    private static void DistantLandscape(SceneBatch batch,GameState s)
    {
        // Decorative horizon lies outside the playable grid and reveals no hidden tiles.
        double cx=s.Width*Math.Sqrt(3)/2,cz=s.Height*.75,radius=Math.Sqrt(cx*cx+cz*cz)+20;
        Point3D P(int i,int ring)
        {
            double a=i*Math.PI*2/144,r=radius+ring*17;
            double height=ring==0?-1:4+Terrain3D.Noise(i*.55,ring*17,s.Seed+932)*11+Math.Sin(a*13)*2;
            return new(cx+Math.Cos(a)*r,height,cz+Math.Sin(a)*r);
        }
        for(int ring=0;ring<2;ring++)for(int i=0;i<144;i++)
        {
            var mesh=batch.Mesh("distant-mountains","#8E9895");mesh.Quad(P(i,ring),P(i,ring+1),P((i+1)%144,ring+1),P((i+1)%144,ring),new(0,1,0));
        }
        batch.Sea().Quad(new(-110,Terrain3D.SeaLevel-.014,-110),new(-110,Terrain3D.SeaLevel-.014,160),new(190,Terrain3D.SeaLevel-.014,160),new(190,Terrain3D.SeaLevel-.014,-110),new(0,1,0));
    }
    private static System.Windows.Point RiverCenter(GameState s,Tile tile)
    {
        var p=Terrain3D.Center(tile);return s.Cities.Any(c=>c.Tile==tile.Id)?new(p.X+1.02,p.Y-.08):p;
    }
    private static void NaturalRiver(SceneBatch batch,GameState s,System.Windows.Point a,System.Windows.Point b)
    {
        var delta=b-a;if(delta.Length<.01)return;var side=new System.Windows.Vector(-delta.Y,delta.X);side.Normalize();
        int steps=Math.Max(8,(int)Math.Ceiling(delta.Length/.10));
        System.Windows.Point P(double t)=>a+delta*t+side*(Math.Sin(t*Math.PI)*.19+Math.Sin(t*Math.PI*3)*.035);
        for(int i=0;i<steps;i++)
        {
            var p=P((double)i/steps);var q=P((i+1d)/steps);double width=.12+.025*Math.Sin(i*.6);
            Ribbon(batch.Mesh("river-banks","#817C60"),s,p,q,width+.07,.022);
            Ribbon(batch.Mesh("rivers","#467D7D"),s,p,q,width,.034);
        }
    }
    private static void NaturalTree(SceneBatch batch, GameState s, double x, double z, double height, bool broadleaf)
    {
        double y=Terrain3D.Height(s,x,z);var random=new Random(unchecked((int)(x*17291+z*31337)+s.Seed));
        var bark=batch.Mesh("bark", "#A99B81");bark.Cylinder(x,y,z,height*.024,height*.75,height*.009,7);
        // Individually oriented branch sprays retain volume from every camera direction.
        for(int branch=0;branch<9;branch++)
        {
            double angle=branch*2.399963+random.NextDouble()*.7, level=(branch+1)/10d;
            double radius=broadleaf ? height*(.19+Math.Sin(level*Math.PI)*.21) : height*(1-level)*.35;
            double bx=x+Math.Cos(angle)*radius,bz=z+Math.Sin(angle)*radius,by=y+height*(.32+level*.62);
            bark.Tube(new(x,y+height*(.25+level*.5),z),new(bx,by,bz),height*(.012-level*.006),5);
            for(int leaf=0;leaf<4;leaf++)
            {
                double a=angle+leaf*Math.PI/2, tilt=-.38+random.NextDouble()*.8;
                double spread=height*(broadleaf?.24:.18)*(1-level*.35);
                var center=new Point3D(bx+(random.NextDouble()-.5)*spread,by+(random.NextDouble()-.5)*spread,bz+(random.NextDouble()-.5)*spread);
                var right=new Vector3D(Math.Cos(a)*spread,0,Math.Sin(a)*spread);
                var up=new Vector3D(-Math.Sin(a)*tilt*spread,spread*.9,Math.Cos(a)*tilt*spread);
                batch.Foliage(!broadleaf).FoliageCard(center,right,up);
            }
        }
    }
    private static void BuildingDetails(SceneBatch batch,double x,double y,double z,double width,double height,double depth)
    {
        var frames=batch.Mesh("window-frames","#A29175");var panes=batch.Mesh("window-glass","#455B5B");
        for(int floor=0;floor<(height>.24?2:1);floor++)for(int side=-1;side<=1;side+=2)
        {
            double wy=y+height*(floor==0?.46:.82),wz=z-depth*.505;
            frames.Box(x+side*width*.26,wy,wz,width*.16,height*.17,.008);
            panes.Box(x+side*width*.26,wy+height*.015,wz-.005,width*.11,height*.13,.006);
        }
        batch.Mesh("stone-foundations","#9D9C8C").Box(x,y-.018,z,width*1.035,.035,depth*1.035);
        batch.Mesh("chimney-bricks","#A39C8B").Box(x+width*.29,y+height,z+depth*.18,width*.11,width*.43,depth*.13);
        batch.Mesh("timber-beams","#7D705B").Box(x,y+height*.87,z-depth*.508,width,.015,.008);
    }
}
