using Crown.Core;
using System.Windows;
using System.Windows.Media.Media3D;

namespace Crown.Desktop;

internal static partial class WorldScene3D
{
    private static void Person(SceneBatch batch,double x,double y,double z,string color,bool modern,bool robot=false)=>batch.Organic(()=>PersonShape(batch,x,y,z,color,modern,robot));
    private static void Horse(SceneBatch batch,double x,double y,double z,double scale=1,string coat="#80654B",bool tack=false)=>batch.Organic(()=>HorseShape(batch,x,y,z,scale,coat,tack));
    private static void PersonShape(SceneBatch batch,double x,double y,double z,string color,bool modern,bool robot=false)
    {
        string skin=robot?"#9EAEB0":"#BFA180",cloth=robot?"#657E83":color;var uniform=batch.Mesh("uniform-"+cloth,cloth);
        uniform.SoftHull(x,y,z,[(.145,.039,.029,0),(.17,.043,.032,0),(.20,.038,.028,0),(.235,.045,.031,-.002),(.27,.058,.037,-.002),(.285,.067,.032,0),(.298,.041,.024,0),(.31,.020,.018,0)],20);
        var dark=batch.Mesh(robot?"robot-joints":"boots",robot?"#4D6065":"#494B40");
        foreach(int side in new[]{-1,1})
        {
            var hip=new Point3D(x+side*.031,y+.16,z);var knee=new Point3D(x+side*.040,y+.088,z+(side<0?.012:-.014));var ankle=new Point3D(x+side*.041,y+.027,z-.004);
            uniform.Tube(hip,knee,.024,12,endRadius:.018);uniform.Sphere(knee.X,knee.Y,knee.Z,.023,.025,.022,8,4);dark.Tube(knee,ankle,.017,8);dark.Sphere(ankle.X,y+.02,z-.018,.026,.022,.045,9,5);
            var shoulder=new Point3D(x+side*.070,y+.277,z);var elbow=new Point3D(x+side*.09,y+.223,z-.014);var hand=new Point3D(x+side*.088,y+.175,z-.037);
            uniform.Tube(shoulder,elbow,.022,12,endRadius:.017);uniform.Sphere(shoulder.X,shoulder.Y,shoulder.Z,.024,.027,.025,8,5);uniform.Tube(elbow,hand,.017,12,endRadius:.011);
            var hands=batch.Mesh(robot?"robot-hands":"skin",skin);hands.Sphere(hand.X,hand.Y,hand.Z,.012,.018,.010,12,7);
            if(!hands.Simplified&&!robot)for(int finger=0;finger<4;finger++)hands.Tube(new(hand.X-.008+finger*.005,hand.Y-.008,hand.Z-.004),new(hand.X-.008+finger*.005,hand.Y-.025+Math.Abs(finger-1.5)*.002,hand.Z-.009),.0025,6,endRadius:.0018);
        }
        batch.Mesh(robot?"robot-neck":"skin",skin).Cylinder(x,y+.305,z,.018,.028,sides:9);
        batch.Mesh(robot?"robot-heads":"skin",skin).SoftHull(x,y,z,[(.323,.012,.013,-.002),(.333,.023,.021,-.004),(.35,.030,.027,-.001),(.373,.032,.030,0),(.39,.028,.026,.002),(.403,.019,.017,.003),(.408,.003,.003,.003)],20);
        dark.Hull(x,y,z,[(.187,.047,.034,0),(.201,.049,.035,0)],12);batch.Mesh("belt-buckles","#B5A27A").Box(x,y+.189,z-.037,.015,.011,.006);
        if(robot)
        {
            batch.Mesh("robot-visor-glass","#7AA9AF").Box(x,y+.36,z-.043,.068,.022,.012);
            batch.Mesh("robot-chest","#8C9E9E").Hull(x,y,z,[(.222,.047,.044,-.002),(.273,.060,.047,-.002),(.284,.045,.042,-.002)],8);
        }
        else
        {
            batch.Mesh("hair","#51493C").Sphere(x,y+.392,z+.007,.033,.025,.030,10,5);
            foreach(int side in new[]{-1,1})
            {
                batch.Mesh("skin",skin).Sphere(x+side*.031,y+.365,z,.008,.012,.009,7,4);
                batch.Mesh("eyes","#333E3A").Sphere(x+side*.012,y+.374,z-.029,.005,.004,.004,6,4);
            }
            batch.Mesh("skin",skin).Sphere(x,y+.357,z-.031,.007,.010,.012,12,7);
            var lips=batch.Mesh("lips","#967565");if(!lips.Simplified){lips.Sphere(x,y+.343,z-.025,.010,.0025,.003,10,5);foreach(int side in new[]{-1,1})batch.Mesh("hair","#51493C").Tube(new(x+side*.006,y+.382,z-.029),new(x+side*.021,y+.38,z-.026),.002,6); }
            var helmet=batch.Mesh(modern?"helmets-modern":"helmets",modern?"#66705E":"#9B9D8D");helmet.Sphere(x,y+.406,z+.004,.036,.022,.035,12,6);
            if(modern)helmet.Box(x,y+.397,z-.034,.064,.009,.025);
            else helmet.Tube(new(x,y+.404,z-.034),new(x,y+.365,z-.036),.005,5);
            if(!modern)
            {
                var cloak=batch.Mesh("cloak-fabric","#857B62");
                Point3D Fabric(int row,int col){double t=row/2d,u=col/4d;return new(x+(u-.5)*(.11+t*.015),y+.285-t*.14,z+.039+t*.021+Math.Sin(u*Math.PI*4)*.007);}
                for(int row=0;row<2;row++)for(int col=0;col<4;col++)cloak.Quad(Fabric(row,col),Fabric(row+1,col),Fabric(row+1,col+1),Fabric(row,col+1),new(0,0,1));
            }
        }
    }
    private static void HorseShape(SceneBatch batch,double x,double y,double z,double scale=1,string coat="#80654B",bool tack=false)
    {
        Point3D P(double xx,double yy,double zz)=>new(x+xx*scale,y+yy*scale,z+zz*scale);
        var body=batch.Mesh("horse-coat-"+coat,coat);var hair=batch.Mesh("horse-mane-tail","#453C31");var hooves=batch.Mesh("horse-hooves","#3B4039");
        void Ellipsoid(SceneMesh mesh,double xx,double yy,double zz,double rx,double ry,double rz,int sides=18,int rings=10){var p=P(xx,yy,zz);mesh.Sphere(p.X,p.Y,p.Z,rx*scale,ry*scale,rz*scale,sides,rings);}
        body.SoftHull(x,y,z,[(-.245*scale,.015*scale,.025*scale,.29*scale),(-.19*scale,.080*scale,.10*scale,.29*scale),(-.105*scale,.102*scale,.119*scale,.285*scale),(.01*scale,.104*scale,.112*scale,.28*scale),(.12*scale,.111*scale,.127*scale,.29*scale),(.21*scale,.081*scale,.095*scale,.285*scale),(.25*scale,.012*scale,.025*scale,.28*scale)],20,true);
        body.SoftHull(x,y,z,[(.31*scale,.070*scale,.080*scale,-.14*scale),(.39*scale,.055*scale,.058*scale,-.20*scale),(.475*scale,.032*scale,.043*scale,-.275*scale)],12);
        body.SoftHull(x,y,z,[(-.445*scale,.012*scale,.018*scale,.442*scale),(-.419*scale,.035*scale,.026*scale,.446*scale),(-.365*scale,.036*scale,.038*scale,.469*scale),(-.305*scale,.043*scale,.054*scale,.495*scale),(-.268*scale,.025*scale,.035*scale,.498*scale),(-.25*scale,.006*scale,.012*scale,.486*scale)],20,true);
        foreach(int side in new[]{-1,1})
        {
            foreach(int leg in new[]{-1,1})
            {
                double zz=leg*.15,shift=(side==leg?.012:-.012);body.Tube(P(side*.072,.28,zz),P(side*.079,.14,zz+shift),.024*scale,8);
                Ellipsoid(body,side*.079,.14,zz+shift,.024,.026,.024,8,5);body.Tube(P(side*.079,.14,zz+shift),P(side*.081,.035,zz-.014),.015*scale,8);
                Ellipsoid(hooves,side*.081,.020,zz-.025,.026,.022,.039,9,5);
            }
            body.Tube(P(side*.027,.526,-.27),P(side*.033,.578,-.255),.012*scale,12,endRadius:.002*scale);
            Ellipsoid(batch.Mesh("horse-eyes","#202C29"),side*.043,.498,-.313,.0065,.007,.009,7,5);
            Ellipsoid(batch.Mesh("horse-nostrils","#4D473C"),side*.025,.45,-.416,.008,.006,.004,7,4);
        }
        hair.Tube(P(0,.39,-.12),P(0,.515,-.25),.020*scale,7);hair.Tube(P(0,.33,.235),P(0,.22,.29),.020*scale,7);hair.Tube(P(0,.22,.29),P(0,.10,.31),.014*scale,7);hair.Tube(P(0,.10,.31),P(0,.055,.285),.009*scale,7);
        if(tack)
        {
            var leather=batch.Mesh("saddle-leather","#5F5140");var p=P(0,.39,.015);leather.Box(p.X,p.Y,p.Z,.17*scale,.025*scale,.14*scale);
            foreach(int side in new[]{-1,1}){leather.Tube(P(side*.103,.32,0),P(side*.103,.23,0),.007*scale,5);leather.Tube(P(side*.045,.52,-.29),P(side*.05,.47,-.37),.005*scale,5);}
        }
    }
    internal static Point UnitAnchor(GameState s,Unit unit,int slot)
    {
        if(Navy.IsShip(unit)){var center=Terrain3D.Center(s.Tiles[unit.Tile]);return new(center.X-.26+slot*.26,center.Y+.05);}
        var p=Terrain3D.Center(s.Tiles[unit.Tile]);return s.Cities.Any(c=>c.Tile==unit.Tile)?new(p.X,p.Y+.30+slot*.20):new(p.X-.34+slot*.29,p.Y+.28);
    }
}
