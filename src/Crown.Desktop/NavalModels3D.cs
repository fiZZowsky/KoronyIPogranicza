using Crown.Core;
using System.Windows.Media.Media3D;

namespace Crown.Desktop;

internal static partial class WorldScene3D
{
    private static void ShipModel(SceneBatch batch,GameState s,Unit unit,int slot)
    {
        var p=UnitAnchor(s,unit,slot);double x=p.X,z=p.Y,y=Terrain3D.SeaLevel;
        int era=UnitModels.For(unit)?.Era??2;string color=s.Factions[unit.Owner].Color;
        var hull=batch.Mesh(era<8?"naval-hull-wood":"naval-hull-metal",era<8?"#816D50":"#7C8B8E");
        (double Z,double W)[] sections=[(-.55,.02),(-.32,.14),(.03,.18),(.31,.14),(.46,.07)];
        Point3D P(int i,int j){var v=sections[i];(double X,double Y)[] ring=[(-v.W,.12),(v.W,.12),(v.W*.85,-.02),(0,-.10),(-v.W*.85,-.02)];return new(x+ring[j].X,y+ring[j].Y,z+v.Z);}
        for(int i=0;i<sections.Length-1;i++)for(int j=0;j<5;j++)
        {var a=P(i,j);var b=P(i,(j+1)%5);hull.Quad(a,b,P(i+1,(j+1)%5),P(i+1,j),new((a.X+b.X)/2-x,(a.Y+b.Y)/2-y,0));}
        batch.Mesh("naval-deck-wood","#B4A283").Box(x,y+.115,z,.26,.018,.70);
        if(SeaTransport.Passengers(s,unit).Any())batch.Mesh("naval-cargo-wood","#B99159").Box(x+.055,y+.14,z+.14,.10,.09,.12);
        batch.Mesh("naval-bulwark-"+color,color).Box(x-.16,y+.11,z,.018,.055,.60);
        batch.Mesh("naval-bulwark-"+color,color).Box(x+.16,y+.11,z,.018,.055,.60);
        if(era<8)
        {
            var wood=batch.Mesh("naval-masts-wood","#9C8460");int masts=era<6?1:3;
            for(int i=0;i<masts;i++)
            {
                double zz=z+(i-(masts-1)*.5)*.24,h=i==1||masts==1?.72:.52;wood.Cylinder(x,y+.13,zz,.013,h,sides:7);
                wood.Tube(new(x-.24,y+h*.8,zz),new(x+.24,y+h*.8,zz),.009,5);
                var sail=batch.Mesh("naval-sail-cloth","#D9D2B4");sail.Quad(new(x-.22,y+h*.8,zz),new(x+.22,y+h*.8,zz),new(x+.18,y+.28,zz+.045),new(x-.18,y+.28,zz+.045),new(0,0,-1));
            }
            if(era<6)for(int side=-1;side<=1;side+=2)for(int i=0;i<6;i++)
                wood.Tube(new(x+side*.12,y+.11,z-.28+i*.095),new(x+side*.31,y+.005,z-.21+i*.095),.008,5);
            else{batch.Mesh("naval-stern-wood","#8B795E").Box(x,y+.13,z+.28,.23,.16,.17);batch.Mesh("naval-cannon-metal","#475353").Tube(new(x,y+.18,z-.27),new(x,y+.18,z-.47),.022,7);}
        }
        else
        {
            var metal=batch.Mesh("naval-superstructure-metal","#A4B1B1");metal.Box(x,y+.14,z+.10,.19,.17,.24);
            batch.Mesh("naval-bridge-glass","#52777E").Box(x,y+.24,z-.025,.16,.05,.014);
            metal.Cylinder(x,y+.15,z-.26,.075,.065,sides:10);metal.Tube(new(x,y+.22,z-.26),new(x,y+.25,z-.47),.018,7);
            if(era<10)batch.Mesh("naval-funnel-metal","#6C7471").Cylinder(x,y+.25,z+.16,.042,.22,sides:9);
            else{metal.Tube(new(x,y+.30,z+.10),new(x,y+.58,z+.10),.009,6);metal.Box(x,y+.53,z+.10,.20,.015,.03);}
            if(era>=12)batch.Mesh("naval-sensor-metal","#B6C7C8").Sphere(x,y+.38,z+.23,.055,.065,.055,10,6);
            if(era>=13)batch.Mesh("naval-reactor-glass","#6DABB6").Box(x,y+.15,z+.32,.16,.07,.10);
        }
        Flag(batch,x-.07,y+.15,z+.34,color,.29);
    }
    private static void PortModel(SceneBatch batch,GameState s,City city)
    {
        var water=World.Neighbors(s,city.Tile).Where(t=>t.Explored&&Navy.Water(t)).OrderBy(t=>t.Id).FirstOrDefault();if(water==null)return;
        var c=Terrain3D.Center(s.Tiles[city.Tile]);var w=Terrain3D.Center(water);double x=w.X+(c.X-w.X)*.45,z=w.Y+(c.Y-w.Y)*.45,y=Terrain3D.SeaLevel+.075;
        var origin=new Point3D(x,y,z);double yaw=Math.Atan2(c.X-w.X,c.Y-w.Y);
        batch.Rotated(origin,yaw,()=>
        {
            var wood=batch.Mesh("naval-port-wood","#A48D69");wood.Box(x,y,z,.34,.035,1.02);
            for(int a=-1;a<=1;a+=2)for(int i=-1;i<=1;i++)wood.Cylinder(x+a*.14,y-.45,z+i*.40,.025,.51,sides:6);
            wood.Tube(new(x+.21,y,z-.18),new(x+.21,y+.47,z-.18),.016,6);wood.Tube(new(x+.21,y+.47,z-.18),new(x-.16,y+.44,z-.18),.016,6);
            batch.Mesh("naval-port-crates-wood","#B69C71").Box(x-.04,y+.035,z+.25,.15,.13,.14);
            batch.Mesh("naval-port-crates-wood","#B69C71").Box(x+.08,y+.035,z+.39,.10,.09,.10);
        });
    }
}
