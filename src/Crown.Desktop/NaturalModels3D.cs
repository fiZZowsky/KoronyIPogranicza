using Crown.Core;
using System.Windows.Media.Media3D;

namespace Crown.Desktop;

internal static partial class WorldScene3D
{
    private static void DistantLandscape(SceneBatch batch,GameState s)
    {
        // A continuous radial surface outside the playable world, with smooth normals.
        double cx=s.Width*Math.Sqrt(3)/2,cz=s.Height*.75,radius=Math.Sqrt(cx*cx+cz*cz)+12;
        double H(double x,double z)
        {
            double r=Math.Sqrt((x-cx)*(x-cx)+(z-cz)*(z-cz));double t=Math.Clamp((r-radius)/12,0,1);t=t*t*(3-2*t);
            double ridge=1-Math.Abs(Terrain3D.Noise(x*.075,z*.075,s.Seed+932)*2-1);ridge*=ridge;
            return -.8+t*(2.8+ridge*11+Terrain3D.Noise(x*.22,z*.22,s.Seed+1932)*2);
        }
        Point3D P(int i,int ring){double a=i*Math.PI*2/192,r=radius+ring*3.2;double x=cx+Math.Cos(a)*r,z=cz+Math.Sin(a)*r;return new(x,H(x,z),z);}
        Vector3D N(Point3D p){var n=new Vector3D(H(p.X-.2,p.Z)-H(p.X+.2,p.Z),.4,H(p.X,p.Z-.2)-H(p.X,p.Z+.2));n.Normalize();return n;}
        for(int ring=0;ring<16;ring++)for(int i=0;i<192;i++)
        {
            var mesh=batch.Mesh("distant-mountains","#8E9895");var a=P(i,ring);var b=P(i,ring+1);var c=P((i+1)%192,ring+1);var d=P((i+1)%192,ring);
            mesh.Triangle(a,c,b,N(a),N(c),N(b));mesh.Triangle(a,d,c,N(a),N(d),N(c));
        }
        batch.Sea().Quad(new(-110,Terrain3D.SeaLevel-.014,-110),new(-110,Terrain3D.SeaLevel-.014,160),new(190,Terrain3D.SeaLevel-.014,160),new(190,Terrain3D.SeaLevel-.014,-110),new(0,1,0));
    }
    private static void NaturalTree(SceneBatch batch, GameState s, double x, double z, double height, bool broadleaf)
    {
        double y=Terrain3D.Height(s,x,z);var random=new Random(unchecked((int)(x*17291+z*31337)+s.Seed));
        bool birch=broadleaf&&Math.Abs((int)(x*17+z*23))%4==0;
        string barkKey=birch?"bark-birch":broadleaf?"bark-oak":"bark-spruce";var bark=batch.Mesh(barkKey,birch?"#C7C6AD":"#89785E");
        bark.Hull(x,y,z,[(0,height*.038,height*.034,0),(height*.13,height*.025,height*.024,0),(height*.52,height*.016,height*.015,height*.018),(height*.92,height*.005,height*.005,height*.025)],9);
        for(int root=0;root<(broadleaf?5:3);root++)
        {
            double angle=root*Math.PI*2/(broadleaf?5:3);double rx=x+Math.Cos(angle)*height*.13,rz=z+Math.Sin(angle)*height*.13;
            bark.Tube(new(rx,Terrain3D.Height(s,rx,rz)+.005,rz),new(x,y+height*.10,z),height*.012,5);
        }
        if(birch)for(int mark=1;mark<5;mark++)batch.Mesh("bark-birch-marks","#615D4F").Tube(new(x-height*.024,y+mark*height*.13,z-.014),new(x+height*.014,y+mark*height*.13+.008,z-.014),height*.003,4);
        // Individually oriented branch sprays retain volume from every camera direction.
        for(int branch=0;branch<9;branch++)
        {
            double angle=branch*2.399963+random.NextDouble()*.7, level=(branch+1)/10d;
            double radius=broadleaf ? height*(birch?.15+Math.Sin(level*Math.PI)*.16:.17+Math.Sin(level*Math.PI)*.26) : height*(1-level)*.35;
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
    private static void CoarseTree(SceneBatch batch,GameState s,double x,double z,double height,bool broadleaf)
    {
        double y=Terrain3D.Height(s,x,z);bool birch=broadleaf&&Math.Abs((int)(x*17+z*23))%4==0;
        batch.Mesh(birch?"lod-bark-birch":broadleaf?"lod-bark-oak":"lod-bark-spruce",birch?"#C7C6AD":"#89785E").Cylinder(x,y,z,height*.026,height*.84,height*.006,6);
        for(int card=0;card<3;card++)
        {
            double angle=card*Math.PI/3,spread=height*(broadleaf?.39:.31);
            batch.Foliage(!broadleaf,"lod-").FoliageCard(new(x,y+height*(broadleaf?.67:.59),z),new(Math.Cos(angle)*spread,0,Math.Sin(angle)*spread),new(0,height*(broadleaf?.37:.49),0));
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
