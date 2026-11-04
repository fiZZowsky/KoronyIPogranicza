using Crown.Core;
using System.Windows.Media.Media3D;

namespace Crown.Desktop;

internal static partial class WorldScene3D
{
    private static void FrontlineDetails(SceneBatch batch,GameState s,HashSet<int> visible)
    {
        foreach(var unit in s.Units.Where(u=>SeaTransport.Active(u)&&!Navy.IsShip(u)&&u.Order==MilitaryOrder.Fortify&&u.FortificationTurns>0&&(u.Owner==0||visible.Contains(u.Tile))).GroupBy(u=>u.Tile).Select(g=>g.OrderBy(u=>u.Id).First()))
        {
            batch.TileId=unit.Tile;var c=Terrain3D.Center(s.Tiles[unit.Tile]);int era=s.Factions[unit.Owner].Era;
            for(int i=0;i<9;i++)
            {
                double angle=(i*22+2)*Math.PI/180,x=c.X+Math.Cos(angle)*.44,z=c.Y+Math.Sin(angle)*.44,y=Terrain3D.Height(s,x,z);
                if(era>=8)
                {var bags=batch.Mesh("frontline-sandbags-cloth","#968D6C");bags.Sphere(x,y+.025,z,.073,.032,.043,8,4);if(unit.FortificationTurns>=2)bags.Sphere(x,y+.08,z,.065,.03,.04,8,4);}
                else batch.Mesh("frontline-stakes-wood","#7D6E50").Cylinder(x,y,z,.015,.13+.025*(i%2),.002,6);
            }
        }
        foreach(var guard in Enumerable.Range(0,s.Factions.Count).SelectMany(owner=>SiegeWarfare.Beachheads(s,owner)).Where(u=>u.Owner==0||visible.Contains(u.Tile)))
        {
            batch.TileId=guard.Tile;var c=Terrain3D.Center(s.Tiles[guard.Tile]);double x=c.X-.38,z=c.Y-.24,y=Terrain3D.Height(s,x,z);
            Flag(batch,x,y,z,s.Factions[guard.Owner].Color,.37);
            batch.Mesh("beachhead-crates-wood","#A48A61").Box(x+.08,y,z,.12,.085,.10);
            batch.Mesh("beachhead-crates-wood","#8D7959").Box(x+.19,y,z+.025,.08,.065,.075);
        }
        foreach(var city in s.Cities.Where(c=>c.Health<55&&(c.Owner==0||visible.Contains(c.Tile))))
        {
            batch.TileId=city.Tile;var c=Terrain3D.Center(s.Tiles[city.Tile]);var rubble=batch.Mesh("siege-ruins-stone","#7C776A");
            for(int i=0;i<(55-city.Health)/4;i++){double a=i*2.39996,x=c.X+Math.Cos(a)*.64,z=c.Y+Math.Sin(a)*.64;rubble.Box(x,Terrain3D.Height(s,x,z)+.01,z,.045+(i%3)*.013,.018+(i%2)*.018,.052);}
        }
    }
}
