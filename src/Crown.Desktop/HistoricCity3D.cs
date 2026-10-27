using Crown.Core;
using System.Windows;
using System.Windows.Media.Media3D;

namespace Crown.Desktop;

internal sealed record CityLot(double X,double Z,double Width,double Depth,double Height,double Yaw);

internal static partial class WorldScene3D
{
    internal static IReadOnlyList<CityLot> HistoricLots(int cityTile,int population)
    {
        var random=new Random(unchecked(cityTile*571+19));var lots=new List<CityLot>();
        for(int row=-3;row<=3;row++)for(int col=-3;col<=3;col++)
        {
            if(row==0||col==0||(Math.Abs(row)==1&&Math.Abs(col)==1))continue;
            double x=col*.22,z=row*.22;if(x*x+z*z>.78*.78)continue;
            double yaw=Math.Abs(col)>Math.Abs(row)?Math.PI/2:0;
            lots.Add(new(x,z,.13+random.NextDouble()*.035,.14+random.NextDouble()*.035,.14+random.NextDouble()*.12,yaw));
        }
        return lots.OrderBy(l=>l.X*l.X+l.Z*l.Z).ThenBy(l=>l.Z).ThenBy(l=>l.X).Take(Math.Clamp(6+population,8,lots.Count)).ToArray();
    }
    private static void HistoricCity(SceneBatch batch,GameState s,City city,int era)
    {
        var center=Terrain3D.Center(s.Tiles[city.Tile]);double x=center.X,z=center.Y,y=Terrain3D.Height(s,x,z);
        var streets=batch.Mesh("streets-cobbles","#9D9682");
        Ribbon(streets,s,new(x-.82,z),new(x+.82,z),.115,.027);Ribbon(streets,s,new(x,z-.82),new(x,z+.90),.115,.027);
        Ribbon(streets,s,new(x-.17,z+.22),new(x+.17,z+.22),.26,.029);
        foreach(var lot in HistoricLots(city.Tile,city.Population))
        {
            double hx=x+lot.X,hz=z+lot.Z,hy=Terrain3D.Height(s,hx,hz);
            batch.Rotated(new(hx,hy,hz),lot.Yaw,()=>
            {
                House(batch,hx,hy,hz,lot.Width,lot.Height,lot.Depth,era<4?"#B1A58B":"#D0C5AC");
                if(era>=4)
                {
                    var wood=batch.Mesh("timber-beams","#74634C");double f=hz-lot.Depth*.515;
                    wood.Tube(new(hx-lot.Width*.47,hy+.02,f),new(hx+lot.Width*.47,hy+lot.Height*.85,f),.006,4);
                    wood.Tube(new(hx+lot.Width*.47,hy+.02,f),new(hx-lot.Width*.47,hy+lot.Height*.85,f),.006,4);
                }
            });
        }
        House(batch,x,y,z-.055,.28,.28,.34,"#C4BBA4");
        if(era>=4)
        {
            batch.Mesh("hall-tower","#C4BBA4").Box(x+.16,y,z-.02,.14,.57,.14);
            batch.Mesh("roofs","#9B6550").Cylinder(x+.16,y+.57,z-.02,.12,.18,0,4);
            batch.Mesh("window-glass","#455B5B").Box(x+.16,y+.39,z-.096,.045,.095,.008);
        }
        // Market stalls are decorative city geometry, not additional simulated trade contracts.
        for(int i=-1;i<=1;i+=2)
        {
            double mx=x+i*.23,mz=z+.20,my=Terrain3D.Height(s,mx,mz);
            batch.Mesh("market-wood","#95816A").Box(mx,my+.035,mz,.12,.045,.11);
            var canvas=batch.Mesh(i<0?"market-canvas-ochre":"market-canvas-linen",i<0?"#B39964":"#CFC8B5");canvas.Box(mx,my+.17,mz,.16,.012,.14);
            foreach(double side in new[]{-.06,.06})batch.Mesh("market-wood","#95816A").Cylinder(mx+side,my,mz,.007,.17,sides:5);
        }
        if(city.Buildings.Contains("walls"))CityFortifications(batch,s,center);
        CityIndustry(batch,s,city);
        Flag(batch,x-.12,y+.35,z-.02,s.Factions[city.Owner].Color,.35);
    }
    private static void CityFortifications(SceneBatch batch,GameState s,Point center)
    {
        var stone=batch.Mesh("fortifications","#B6B0A0");
        int tile=Terrain3D.TileAt(s,center.X,center.Y);
        bool eastGate=tile>=0&&World.Neighbors(s,tile).Any(n=>n.Explored&&n.Road&&Terrain3D.Center(n).X>center.X+1&&Math.Abs(Terrain3D.Center(n).Y-center.Y)<.1);
        const int sides=16;
        for(int i=0;i<sides;i++)
        {
            double a=i*Math.PI*2/sides,b=(i+1)*Math.PI*2/sides;
            var p=new Point(center.X+Math.Cos(a)*.91,center.Y+Math.Sin(a)*.91);var q=new Point(center.X+Math.Cos(b)*.91,center.Y+Math.Sin(b)*.91);
            // Leave a real opening in the south curtain wall for the gateway.
            if(i!=3&&i!=4&&(!eastGate||i!=0&&i!=15))
            {
                double dx=q.X-p.X,dz=q.Y-p.Y,len=Math.Sqrt(dx*dx+dz*dz),mx=(p.X+q.X)/2,mz=(p.Y+q.Y)/2;
                double baseY=Math.Min(Terrain3D.Height(s,p.X,p.Y),Terrain3D.Height(s,q.X,q.Y));
                batch.Rotated(new(mx,baseY,mz),Math.Atan2(dz,dx),()=>
                {
                    // Rotation is about Y with +Z toward the right of local +X.
                    batch.Mesh("fortifications","#B6B0A0").Box(mx,baseY,mz,len+.025,.24,.065);
                    for(int m=0;m<4;m++)batch.Mesh("fortifications","#B6B0A0").Box(mx-len*.4+m*len*.27,baseY+.24,mz,.045,.045,.065);
                });
            }
            if(i%2==0&&(!eastGate||i!=0))
            {
                double ty=Terrain3D.Height(s,p.X,p.Y);stone=batch.Mesh("fortifications","#B6B0A0");stone.Cylinder(p.X,ty,p.Y,.068,.31,sides:12);
                for(int m=0;m<6;m++){double t=m*Math.PI/3;stone.Box(p.X+Math.Cos(t)*.052,ty+.31,p.Y+Math.Sin(t)*.052,.032,.038,.032);}
            }
        }
        double gx=center.X,gz=center.Y+.87,gy=Terrain3D.Height(s,gx,gz);
        Gateway(batch,gx,gy,gz,.29,.32,.13);
        // Short wall shoulders connect the gateway to the adjacent curtain corners.
        batch.Mesh("fortifications","#B6B0A0").Box(gx-.24,gy,gz-.035,.19,.24,.065);
        batch.Mesh("fortifications","#B6B0A0").Box(gx+.24,gy,gz-.035,.19,.24,.065);
        if(eastGate)
        {
            double ex=center.X+.87,ez=center.Y,ey=Terrain3D.Height(s,ex,ez);
            batch.Rotated(new(ex,ey,ez),-Math.PI/2,()=>Gateway(batch,ex,ey,ez,.29,.32,.13));
            batch.Mesh("fortifications","#B6B0A0").Box(ex-.035,ey,ez-.24,.065,.24,.19);
            batch.Mesh("fortifications","#B6B0A0").Box(ex-.035,ey,ez+.24,.065,.24,.19);
        }
    }
    private static void Gateway(SceneBatch batch,double x,double y,double z,double width,double height,double depth)
    {
        var mesh=batch.Mesh("fortifications-gateway","#B6B0A0");double radius=width*.37,outer=width*.53,spring=y+height-radius;
        mesh.Box(x-width*.45,y,z,width*.16,spring-y,depth);mesh.Box(x+width*.45,y,z,width*.16,spring-y,depth);
        for(int i=0;i<12;i++)
        {
            double a=i*Math.PI/12,b=(i+1)*Math.PI/12;
            Point3D P(double angle,double r,double zz)=>new(x+Math.Cos(angle)*r,spring+Math.Sin(angle)*r,zz);
            double f=z-depth/2,back=z+depth/2;
            mesh.Quad(P(a,radius,f),P(b,radius,f),P(b,outer,f),P(a,outer,f),new(0,0,-1));
            mesh.Quad(P(a,radius,back),P(a,outer,back),P(b,outer,back),P(b,radius,back),new(0,0,1));
            mesh.Quad(P(a,outer,f),P(b,outer,f),P(b,outer,back),P(a,outer,back),new(Math.Cos((a+b)/2),Math.Sin((a+b)/2),0));
            mesh.Quad(P(a,radius,f),P(a,radius,back),P(b,radius,back),P(b,radius,f),new(-Math.Cos((a+b)/2),-Math.Sin((a+b)/2),0));
        }
    }
}
