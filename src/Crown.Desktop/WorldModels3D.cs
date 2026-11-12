using Crown.Core;
using System.Windows.Media.Media3D;

namespace Crown.Desktop;

internal static partial class WorldScene3D
{
    private static void Flag(SceneBatch batch, double x, double y, double z, string color, double height = .5)
    {
        batch.Mesh("flag-poles", "#655644").Cylinder(x,y,z,.006,height,sides:10);
        var cloth=batch.Mesh("flag-fabric-"+color,color);double w=height*.32,h=height*.24;
        Point3D P(int i,int j){double u=i/8d,v=j/3d;return new(x+u*w,y+height-v*h-.025*u,z+Math.Sin(u*Math.PI*2.4)*.012*u);}
        for(int i=0;i<8;i++)for(int j=0;j<3;j++)
        {
            var a=P(i,j);var b=P(i+1,j);var c=P(i+1,j+1);var d=P(i,j+1);
            cloth.Triangle(a,b,c,null,null,null,new(i/8d,j/3d),new((i+1)/8d,j/3d),new((i+1)/8d,(j+1)/3d));
            cloth.Triangle(a,c,d,null,null,null,new(i/8d,j/3d),new((i+1)/8d,(j+1)/3d),new(i/8d,(j+1)/3d));
        }
    }
    private static void City(SceneBatch batch, GameState s, City city)
    {
        var p = Terrain3D.Center(s.Tiles[city.Tile]); int era = s.EpochCampaign ? s.Factions[city.Owner].Era : 4;
        if(era>=2&&era<9){HistoricCity(batch,s,city,era);return;}
        string color = s.Factions[city.Owner].Color; var random = new Random(city.Tile * 571 + 19);
        double y = Terrain3D.Height(s, p.X, p.Y);
        if(era<2)CampGround(batch,s,p.X,p.Y);
        else batch.Mesh("town-square", "#A79D7F").Cylinder(p.X, y - .035, p.Y, .23, .04, sides: 16);
        int count = era<2?Math.Clamp(4+city.Population,6,14):Math.Clamp(10+city.Population*2,12,46);
        bool depotYard=ReserveDepotYard(s,city);
        bool craftYard=ReserveCraftYard(s,city);
        for (int i = 0; i < count; i++)
        {
            double angle = i * 2.399963, radius = .24+Math.Sqrt((i+.5)/count)*.56;
            double x = p.X + Math.Cos(angle) * radius, z = p.Y + Math.Sin(angle) * radius, baseY = Terrain3D.Height(s, x, z);
            if(depotYard&&InDepotYard(x-p.X,z-p.Y))continue;
            if(craftYard&&InCraftYard(x-p.X,z-p.Y))continue;
            if (era == 0)
            {
                if(!CampSiteDry(s,new(x,z)))continue;
                HideShelter(batch,s,x,z,angle+.4,i+city.Tile);
            }
            else if (era < 9) House(batch, x, baseY, z, .12 + random.NextDouble() * .065, .13 + random.NextDouble() * .13, .13 + random.NextDouble() * .085, era < 3 ? "#B0A07F" : "#C6B797");
            else
            {
                double height = .22 + random.NextDouble() * (era >= 12 ? .55 : .35);
                string walls = era >= 12 ? "#A9C1C1" : "#ADAFA8";
                batch.Mesh("era-facade-"+(era==9?2:3),"#FFFFFF").FacadeBox(x,baseY,z,.16,height,.17);
                batch.Mesh("flat-roofs", "#778781").Box(x, baseY + height, z, .18, .025, .19);
                EraFacade(batch,x,baseY,z,.16,height,.17,era);
            }
        }
        EraCivic(batch,s,city,era);
        if(era==0)CampActivity(batch,s,city);
        if (city.Buildings.Contains("walls"))
        {
            var stone = batch.Mesh("fortifications", "#A5A28C");
            for (int i = 0; i < 12; i++)
            {
                double a = i * Math.PI / 6, x = p.X + Math.Cos(a) * .94, z = p.Y + Math.Sin(a) * .94;
                stone.Cylinder(x, Terrain3D.Height(s, x, z), z, .075, .24, sides: 6);
                var b = a + Math.PI / 6; stone.Tube(new(x, Terrain3D.Height(s, x, z) + .12, z), new(p.X + Math.Cos(b) * .94, Terrain3D.Height(s, p.X + Math.Cos(b) * .94, p.Y + Math.Sin(b) * .94) + .12, p.Y + Math.Sin(b) * .94), .055, 6);
            }
        }
        CityIndustry(batch,s,city);
        Flag(batch,p.X-.1,y+(era<2?0:era>=9?.60:.32),p.Y,color,era<2?.24:.36);
    }
    private static void CityIndustry(SceneBatch batch,GameState s,City city)
    {
        var p=Terrain3D.Center(s.Tiles[city.Tile]);double y=Terrain3D.Height(s,p.X,p.Y);
        if (city.Buildings.Contains("coal_station") || city.Buildings.Contains("steel_mill"))
        {
            House(batch, p.X + .45, Terrain3D.Height(s, p.X + .45, p.Y - .48), p.Y - .48, .28, .19, .25, "#9C8270");
            batch.Mesh("chimneys", "#7E6C5D").Cylinder(p.X + .50, y, p.Y - .48, .045, .60, .032, 10);
        }
        if (city.Buildings.Contains("atomic_reactor") || city.Buildings.Contains("fusion_plant"))
            batch.Mesh("power-domes", "#C3C9BA").Sphere(p.X - .45, Terrain3D.Height(s, p.X - .45, p.Y - .38) + .12, p.Y - .38, .16, .22, .16, 12, 6);
        if (city.Buildings.Contains("solar_farm") || city.Buildings.Contains("smart_grid"))
            for (int i = 0; i < 3; i++) batch.Mesh("solar-panels", "#345766").Box(p.X - .45 + i * .15, y + .08, p.Y + .55, .12, .025, .14);
    }
    private static void Unit(SceneBatch batch, GameState s, Unit unit, int slot)
    {
        if(Navy.IsShip(unit))
        {
            var anchor=UnitAnchor(s,unit,slot);var seaOrigin=new Point3D(anchor.X,Terrain3D.SeaLevel,anchor.Y);double heading=0;
            if(unit.Destination is int dest){var goal=Terrain3D.Center(s.Tiles[dest]);heading=Math.Atan2(goal.X-anchor.X,anchor.Y-goal.Y);}
            batch.Rotated(seaOrigin,heading,()=>ShipModel(batch,s,unit,slot));return;
        }
        var p=UnitAnchor(s,unit,slot);double x=p.X,z=p.Y;
        var origin=new Point3D(x,Terrain3D.Height(s,x,z),z);double yaw=Math.PI;
        if(unit.Destination is int destination){var goal=Terrain3D.Center(s.Tiles[destination]);var start=Terrain3D.Center(s.Tiles[unit.Tile]);yaw=Math.Atan2(goal.X-start.X,start.Y-goal.Y);}
        batch.Scaled(origin,.52,()=>batch.Rotated(origin,yaw,()=>UnitModel(batch,s,unit,slot)));
    }
    private static void UnitModel(SceneBatch batch, GameState s, Unit unit, int slot)
    {
        var p = UnitAnchor(s,unit,slot); double x = p.X, z = p.Y, y = Terrain3D.Height(s, x, z);
        string color = s.Factions[unit.Owner].Color; int era = UnitModels.For(unit)?.Era ?? 4;
        if (unit.Kind == UnitKind.Cavalry && era >= 9)
        {
            Vehicle(batch, x, y, z, color, era >= 12); return;
        }
        if (unit.Kind == UnitKind.Cavalry)
        {
            Horse(batch, x, y, z,1,tack:true); Person(batch, x, y + .19, z, color, era,unit.Kind,true); Flag(batch, x + .15, y, z, color, .68); return;
        }
        if (unit.Kind == UnitKind.Catapult)
        {
            var wood = batch.Mesh("siege-frame", "#857251"); wood.Box(x, y + .06, z, .3, .06, .35);
            for (int i = -1; i <= 1; i += 2) batch.Mesh("wheels", "#484B42").Tube(new(x + i * .18, y + .08, z - .1), new(x + i * .18, y + .08, z + .10), .07);
            if (era < 6) { wood.Tube(new(x, y + .1, z + .1), new(x, y + .44, z - .15), .03); wood.Tube(new(x - .1, y + .1, z), new(x, y + .3, z), .025); }
            else batch.Mesh("gun-metal", "#657471").Tube(new(x, y + .20, z + .05), new(x, y + .29, z - .39), .052);
            Person(batch, x + .28, y, z + .05, color, era,unit.Kind); return;
        }
        if (unit.Kind == UnitKind.Settler)
        {
            Person(batch, x, y, z, "#BEAA78", era,unit.Kind); Person(batch, x + .18, y, z + .08, color, era,unit.Kind);
            return;
        }
        for (int i = 0; i < (unit.Kind == UnitKind.Scout ? 1 : 3); i++)
        {
            double xx = x + (unit.Kind==UnitKind.Scout?0:(i - 1) * .15), zz = z + (i % 2) * .13, yy = Terrain3D.Height(s, xx, zz);
            Person(batch, xx, yy, zz, color, era,unit.Kind);
            if (era >= 6) batch.Mesh("weapons", "#4C5755").Tube(new(xx + .09, yy + .20, zz), new(xx + .09, yy + .31, zz - .22), .014);
            else if (unit.Kind == UnitKind.Archer)
            {
                Point3D Bow(double t)=>new(xx+.105,yy+.22+Math.Sin(t)*.155,zz-.06-Math.Cos(t)*.06);
                var bow=batch.Mesh("bows-wood","#A48F69");for(int j=0;j<8;j++)bow.Tube(Bow(-1.25+j*2.5/8),Bow(-1.25+(j+1)*2.5/8),.006,5);
                batch.Mesh("bow-strings","#D7CAA3").Tube(Bow(-1.25),Bow(1.25),.0018,4);
                batch.Mesh("quiver-leather","#7C6B50").Cylinder(xx-.035,yy+.18,zz+.07,.019,.14,sides:8);
            }
            else if (unit.Kind == UnitKind.Spearmen)
            {
                batch.Mesh("spear-shafts-wood", "#9C855E").Tube(new(xx + .11, yy, zz), new(xx + .11, yy + .62, zz), .007);
                batch.Mesh(era<2?"flint-spear-heads":"spear-heads", era<2?"#777C73":"#B0B6AE").Cylinder(xx + .11, yy + .6, zz, .021, .07, 0, 6);
            }
        }
        if (era>=2&&unit.Kind != UnitKind.Scout) Flag(batch, x - .20, y, z + .14, color, .56);
    }
    private static void Vehicle(SceneBatch batch, double x, double y, double z, string color, bool future)
    {
        batch.Mesh("tracks", "#3C4845").Box(x - .15, y, z, .09, .13, .49); batch.Mesh("tracks", "#3C4845").Box(x + .15, y, z, .09, .13, .49);
        batch.Mesh("vehicle-" + color, color).Box(x, y + .08, z, .30, .14, .43);
        batch.Mesh("vehicle-" + color, color).Cylinder(x, y + .20, z, .12, .09, .08, 8);
        batch.Mesh(future ? "future-guns" : "tank-guns", future ? "#8EB9BD" : "#727E70").Tube(new(x, y + .25, z), new(x, y + .26, z - .43), .025);
    }
    private static void Wagon(SceneBatch batch, GameState s, int tile)
    {
        var p = Terrain3D.Center(s.Tiles[tile]); double x = p.X + .38, z = p.Y - .22, y = Terrain3D.Height(s, x, z);
        batch.Mesh("wagon-wood", "#9B8057").Box(x, y + .08, z, .22, .11, .34);
        batch.Mesh("wagon-cover", "#C9BD99").Sphere(x, y + .23, z, .115, .13, .17, 8, 5);
        foreach (double dx in new[] { -.13, .13 }) foreach (double dz in new[] { -.1, .1 }) batch.Mesh("wagon-wheels", "#554F3E").Sphere(x + dx, y + .07, z + dz, .025, .07, .07, 8, 4);
    }
}
