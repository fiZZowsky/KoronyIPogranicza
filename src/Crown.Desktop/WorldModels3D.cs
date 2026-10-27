using Crown.Core;
using System.Windows.Media.Media3D;

namespace Crown.Desktop;

internal static partial class WorldScene3D
{
    private static void Flag(SceneBatch batch, double x, double y, double z, string color, double height = .5)
    {
        batch.Mesh("flag-poles", "#736A53").Cylinder(x, y, z, .012, height, sides: 6);
        batch.Mesh("flag-" + color, color).Box(x + .08, y + height * .67, z, .16, height * .25, .012);
    }
    private static void City(SceneBatch batch, GameState s, City city)
    {
        var p = Terrain3D.Center(s.Tiles[city.Tile]); int era = s.EpochCampaign ? s.Factions[city.Owner].Era : 4;
        if(era>=2&&era<9){HistoricCity(batch,s,city,era);return;}
        string color = s.Factions[city.Owner].Color; var random = new Random(city.Tile * 571 + 19);
        double y = Terrain3D.Height(s, p.X, p.Y);
        batch.Mesh("town-square", "#A79D7F").Cylinder(p.X, y - .035, p.Y, .23, .04, sides: 16);
        int count = era<2?Math.Clamp(4+city.Population,6,14):Math.Clamp(10+city.Population*2,12,46);
        for (int i = 0; i < count; i++)
        {
            double angle = i * 2.399963, radius = .24+Math.Sqrt((i+.5)/count)*.56;
            double x = p.X + Math.Cos(angle) * radius, z = p.Y + Math.Sin(angle) * radius, baseY = Terrain3D.Height(s, x, z);
            if (era == 0)
            {
                batch.Mesh("tents", "#B1A080").Cylinder(x, baseY, z, .12, .20 + random.NextDouble() * .08, .012, 8);
            }
            else if (era < 9) House(batch, x, baseY, z, .12 + random.NextDouble() * .065, .13 + random.NextDouble() * .13, .13 + random.NextDouble() * .085, era < 3 ? "#B0A07F" : "#C6B797");
            else
            {
                double height = .22 + random.NextDouble() * (era >= 12 ? .55 : .35);
                string walls = era >= 12 ? "#A9C1C1" : "#ADAFA8";
                batch.Mesh("urban-" + walls, walls).Box(x, baseY, z, .16, height, .17);
                batch.Mesh("glass", "#5E8C9A").Box(x, baseY + height * .28, z - .09, .115, height * .57, .008);
                batch.Mesh("flat-roofs", "#778781").Box(x, baseY + height, z, .18, .025, .19);
            }
        }
        if (era < 1) batch.Mesh("camp-fire", "#D0A86D").Cylinder(p.X, y, p.Y, .10, .07, .03, 8);
        else if (era < 8)
        {
            House(batch, p.X, y, p.Y, .30, .30, .37);
            if (era >= 4) { batch.Mesh("hall-tower", "#B9B49F").Box(p.X + .16, y, p.Y, .15, .48, .16); batch.Mesh("roofs", "#9B6550").Cylinder(p.X + .16, y + .48, p.Y, .12, .17, 0, 4); }
        }
        else if (era < 12)
        {
            House(batch, p.X, y, p.Y, .32, .30, .38, "#B3ADA0");
            batch.Mesh("civic-tower", "#CBC7B7").Box(p.X, y, p.Y, .18, .70, .18);
            batch.Mesh("civic-glass", "#72929B").Box(p.X, y + .4, p.Y - .097, .13, .20, .009);
        }
        else
        {
            batch.Mesh("future-towers", "#ADBFBC").Cylinder(p.X, y, p.Y, .15, .85, .11, 12);
            batch.Mesh("future-glass", "#5C99A3").Cylinder(p.X, y + .22, p.Y, .155, .44, .13, 12);
            batch.Mesh("future-caps", "#C9D3CC").Cylinder(p.X, y + .85, p.Y, .13, .075, .04, 12);
            if (era >= 14)
            {
                batch.Mesh("habitat-domes", "#83ACAB").Sphere(p.X + .28, y + .14, p.Y + .16, .25, .22, .22, 14, 7);
                batch.Mesh("antenna", "#C4C8BE").Tube(new(p.X - .26, y, p.Y - .20), new(p.X - .26, y + .68, p.Y - .20), .016);
            }
        }
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
        Flag(batch, p.X - .1, y + (era >= 9 ? .60 : .32), p.Y, color, .36);
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
            Horse(batch, x, y, z,1,tack:true); Person(batch, x, y + .24, z, color, false); Flag(batch, x + .15, y, z, color, .68); return;
        }
        if (unit.Kind == UnitKind.Catapult)
        {
            var wood = batch.Mesh("siege-frame", "#857251"); wood.Box(x, y + .06, z, .3, .06, .35);
            for (int i = -1; i <= 1; i += 2) batch.Mesh("wheels", "#484B42").Tube(new(x + i * .18, y + .08, z - .1), new(x + i * .18, y + .08, z + .10), .07);
            if (era < 6) { wood.Tube(new(x, y + .1, z + .1), new(x, y + .44, z - .15), .03); wood.Tube(new(x - .1, y + .1, z), new(x, y + .3, z), .025); }
            else batch.Mesh("gun-metal", "#657471").Tube(new(x, y + .20, z + .05), new(x, y + .29, z - .39), .052);
            Person(batch, x + .28, y, z + .05, color, era >= 8, era >= 12); return;
        }
        if (unit.Kind == UnitKind.Settler)
        {
            Person(batch, x, y, z, "#BEAA78", false); Person(batch, x + .18, y, z + .08, color, false);
            batch.Mesh("settler-packs", "#867251").Box(x, y + .16, z + .09, .13, .14, .10); return;
        }
        for (int i = 0; i < (unit.Kind == UnitKind.Scout ? 1 : 3); i++)
        {
            double xx = x + (unit.Kind==UnitKind.Scout?0:(i - 1) * .15), zz = z + (i % 2) * .13, yy = Terrain3D.Height(s, xx, zz);
            Person(batch, xx, yy, zz, color, era >= 8, era >= 12);
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
                batch.Mesh("spear-heads", "#B0B6AE").Cylinder(xx + .11, yy + .6, zz, .021, .07, 0, 6);
                batch.Mesh("shields-wood","#8B7756").Sphere(xx-.10,yy+.205,zz-.05,.060,.078,.012,12,6);
                batch.Mesh("shield-boss","#A9A891").Sphere(xx-.10,yy+.205,zz-.065,.016,.017,.010,8,4);
            }
        }
        if (unit.Kind != UnitKind.Scout) Flag(batch, x - .20, y, z + .14, color, .56);
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
