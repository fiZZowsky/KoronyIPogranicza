using Crown.Core;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Crown.Desktop;

internal sealed record WorldScene(Model3DGroup Terrain, Model3DGroup Objects, int TerrainTriangles, int ObjectTriangles,
    IReadOnlyList<int> CityIds, IReadOnlyList<int> UnitIds, int TreeCount);

internal static partial class WorldScene3D
{
    public static WorldScene Build(GameState s, int selectedUnit = -1, WorldScene? reuseTerrain = null)
    {
        var ground = new SceneBatch(); var objects = new SceneBatch(); var visible = Engine.Visible(s); int trees = 0;
        foreach (var tile in s.Tiles)
        {
            var center = Terrain3D.Center(tile);
            var mesh = reuseTerrain == null ? ground.Landscape(s) : null;
            const int subdivisions = 3;
            for (int side = 0; mesh != null && side < 6; side++)
            {
                double angle = (30 + 60 * side) * Math.PI / 180, next = angle + Math.PI / 3;
                var a = new Vector(Math.Cos(angle), Math.Sin(angle)); var b = new Vector(Math.Cos(next), Math.Sin(next));
                Point3D P(int i, int j) => Terrain3D.Ground(s, center.X + (a.X * i + b.X * j) / subdivisions, center.Y + (a.Y * i + b.Y * j) / subdivisions);
                void Tri(Point3D p, Point3D q, Point3D r)
                {
                    var up = new Vector3D(0, 1, 0);
                    mesh.Triangle(p, r, q, tile.Explored ? Terrain3D.Normal(s, p.X, p.Z) : up,
                        tile.Explored ? Terrain3D.Normal(s, r.X, r.Z) : up, tile.Explored ? Terrain3D.Normal(s, q.X, q.Z) : up);
                }
                for (int i = 0; i < subdivisions; i++) for (int j = 0; j < subdivisions - i; j++)
                {
                    Tri(P(i, j), P(i + 1, j), P(i, j + 1));
                    if (i + j < subdivisions - 1) Tri(P(i + 1, j), P(i + 1, j + 1), P(i, j + 1));
                }
            }
            if (!tile.Explored) continue;
            if (tile.River && tile.Passable)
            {
                var neighbors = World.Neighbors(s, tile.Id).Where(n => n.Explored && n.River).ToArray();
                if (neighbors.Length == 0) Ribbon(objects.Mesh("rivers", "#538E99"), s, new(center.X - .12, center.Y - .8), new(center.X + .12, center.Y + .8), .09, .018);
                foreach (var n in neighbors.Where(n => n.Id > tile.Id)) Ribbon(objects.Mesh("rivers", "#538E99"), s, center, Terrain3D.Center(n), .09, .018);
            }
            if (Navigation.RoadNode(s, tile.Id)) foreach (var n in World.Neighbors(s, tile.Id).Where(n => n.Explored && n.Id > tile.Id && Navigation.RoadNode(s, n.Id)))
                Ribbon(objects.Mesh("roads", "#C6B794"), s, center, Terrain3D.Center(n), .055, .036);
            if (tile.Owner >= 0)
            {
                string color = s.Factions[tile.Owner].Color;
                foreach (var n in World.Neighbors(s, tile.Id).Where(n => n.Explored && n.Owner != tile.Owner))
                {
                    var np = Terrain3D.Center(n); var middle = new Point((np.X + center.X) / 2, (np.Y + center.Y) / 2);
                    var direction = np - center; direction.Normalize(); var tangent = new Vector(-direction.Y, direction.X) * .48;
                    Ribbon(objects.Mesh("border-" + tile.Owner, color), s, middle - tangent, middle + tangent, .022, .04);
                }
            }
            if (tile.Improvement != Improvement.None) BuildImprovement(objects, s, tile);
            else if (tile.Terrain == Terrain.Forest && !s.Cities.Any(c => c.Tile == tile.Id))
            {
                var rng = new Random(unchecked(tile.Id * 7907 + s.Seed));
                for (int i = 0; i < 10; i++)
                {
                    double x = center.X + (rng.NextDouble() - .5) * 1.25, z = center.Y + (rng.NextDouble() - .5) * 1.25;
                    Tree(objects, s, x, z, .45 + rng.NextDouble() * .35, i % 3 == 0); trees++;
                }
            }
            if (tile.Ruin) Ruin(objects, s, center);
            if (tile.Resource != Resource.None && tile.Passable && !s.Cities.Any(c => c.Tile == tile.Id)) ResourceModel(objects, s, tile);
        }
        // A single opaque sea surface is depth-tested against the actual land geometry.
        var water = reuseTerrain == null ? ground.Sea() : null;
        for(double x=-2;water != null && x<s.Width*Math.Sqrt(3)+3;x+=2)for(double z=-2;z<s.Height*1.5+3;z+=2)
        {
            Point3D Wave(double xx,double zz)=>new(xx,Terrain3D.SeaLevel+Math.Sin(xx*1.3+zz*.8)*.006,zz);
            water.Quad(Wave(x,z),Wave(x,z+2),Wave(x+2,z+2),Wave(x+2,z),new(0,1,0));
        }
        var cities = s.Cities.Where(c => s.Tiles[c.Tile].Explored).OrderBy(c => c.Id).ToArray(); foreach (var city in cities) City(objects, s, city);
        var units = s.Units.Where(u => u.Owner == 0 || visible.Contains(u.Tile)).Where(u => u.Owner == 0 || s.Tiles[u.Tile].Explored).OrderBy(u => u.Id).ToArray();
        var rendered = new List<int>();
        foreach (var group in units.GroupBy(u => u.Tile))
        {
            int i = 0; foreach (var u in group.OrderByDescending(u => u.Id == selectedUnit).ThenBy(u => u.Id).Take(3)) { Unit(objects, s, u, i); rendered.Add(u.Id); i++; }
        }
        foreach (var cargo in s.Caravans.Where(Caravans.Active).Where(c => s.Tiles[c.Tile].Explored))
        {
            var contract = s.TradeContracts.First(t => t.Id == cargo.ContractId);
            if (contract.Buyer == 0 || contract.Seller == 0) Wagon(objects, s, cargo.Tile);
        }
        int terrainTriangles = reuseTerrain?.TerrainTriangles ?? ground.Triangles, objectTriangles = objects.Triangles;
        return new(reuseTerrain?.Terrain ?? ground.Freeze(), objects.Freeze(), terrainTriangles, objectTriangles, cities.Select(c => c.Id).ToArray(), rendered, trees);
    }
    public static void Ribbon(SceneMesh mesh, GameState s, Point start, Point end, double width, double lift)
    {
        var delta = end - start; if (delta.Length < .001) return; var normal = new Vector(-delta.Y, delta.X); normal.Normalize(); normal *= width / 2;
        int steps = Math.Max(1, (int)Math.Ceiling(delta.Length / .17));
        for (int i = 0; i < steps; i++)
        {
            var a = start + delta * ((double)i / steps); var b = start + delta * ((double)(i + 1) / steps);
            mesh.Quad(Terrain3D.Ground(s, a.X + normal.X, a.Y + normal.Y, lift), Terrain3D.Ground(s, b.X + normal.X, b.Y + normal.Y, lift),
                Terrain3D.Ground(s, b.X - normal.X, b.Y - normal.Y, lift), Terrain3D.Ground(s, a.X - normal.X, a.Y - normal.Y, lift), new(0, 1, 0));
        }
    }
    public static void HexOutline(SceneMesh mesh, GameState s, Tile tile, double width, double lift, double radius = .97)
    {
        var center = Terrain3D.Center(tile);
        for (int i = 0; i < 6; i++)
        {
            double a = (30 + i * 60) * Math.PI / 180, b = a + Math.PI / 3;
            Ribbon(mesh, s, new(center.X + Math.Cos(a) * radius, center.Y + Math.Sin(a) * radius), new(center.X + Math.Cos(b) * radius, center.Y + Math.Sin(b) * radius), width, lift);
        }
    }
    public static void HexFill(SceneMesh mesh, GameState s, Tile tile, double lift)
    {
        var c = Terrain3D.Center(tile);
        for (int i = 0; i < 6; i++)
        {
            double a = (30 + i * 60) * Math.PI / 180, b = a + Math.PI / 3;
            mesh.Triangle(Terrain3D.Ground(s, c.X, c.Y, lift), Terrain3D.Ground(s, c.X + Math.Cos(b) * .96, c.Y + Math.Sin(b) * .96, lift), Terrain3D.Ground(s, c.X + Math.Cos(a) * .96, c.Y + Math.Sin(a) * .96, lift));
        }
    }
    private static void Tree(SceneBatch batch, GameState s, double x, double z, double height, bool broadleaf)
    {
        double y = Terrain3D.Height(s, x, z);
        var shadow=batch.Mesh("contact-shadows","#45101C15",true);
        for(int i=0;i<10;i++)
        {
            double a=i*Math.PI/5,b=(i+1)*Math.PI/5;
            shadow.Triangle(Terrain3D.Ground(s,x+.08,z+.05,.035),Terrain3D.Ground(s,x+.08+Math.Cos(b)*height*.31,z+.05+Math.Sin(b)*height*.23,.035),Terrain3D.Ground(s,x+.08+Math.Cos(a)*height*.31,z+.05+Math.Sin(a)*height*.23,.035));
        }
        batch.Mesh("bark", "#69533C").Cylinder(x, y, z, .027, height * .6, sides: 6);
        if (broadleaf)
        {
            batch.Mesh("leaves-olive", "#657947").Sphere(x, y + height * .7, z, height * .28, height * .35, height * .27, 12, 8);
            batch.Mesh("leaves-light", "#7C8C50").Sphere(x - .04, y + height * .85, z - .035, height * .20, height * .22, height * .20, 10, 7);
        }
        else
        {
            batch.Mesh("pine-dark", "#3C5E3C").Cylinder(x, y + height * .18, z, height * .29, height * .63, 0, 12);
            batch.Mesh("pine-light", "#577246").Cylinder(x, y + height * .45, z, height * .22, height * .55, 0, 12);
        }
    }
    private static void House(SceneBatch batch, double x, double y, double z, double width, double height, double depth, string wall = "#C6B797")
    {
        batch.Mesh("walls-" + wall, wall).Box(x, y, z, width, height, depth);
        var roof = batch.Mesh("roofs", "#9B6550"); double l = x - width * .57, r = x + width * .57, f = z - depth * .57, b = z + depth * .57, top = y + height, ridge = top + width * .42;
        roof.Quad(new(l, top, f), new(l, top, b), new(x, ridge, b), new(x, ridge, f), new(-1, 1, 0));
        roof.Quad(new(r, top, f), new(x, ridge, f), new(x, ridge, b), new(r, top, b), new(1, 1, 0));
        roof.Quad(new(l, top, f), new(x, ridge, f), new(r, top, f), new(r, top, f), new(0, 0, -1));
        roof.Quad(new(l, top, b), new(r, top, b), new(x, ridge, b), new(x, ridge, b), new(0, 0, 1));
        batch.Mesh("doors", "#514737").Box(x, y, f - .004, width * .15, height * .58, .012);
    }
    private static void BuildImprovement(SceneBatch batch, GameState s, Tile tile)
    {
        var p = Terrain3D.Center(tile);
        if (tile.Improvement == Improvement.Farm)
        {
            for (int i = 0; i < 7; i++) Ribbon(batch.Mesh(i % 2 == 0 ? "crop-ochre" : "crop-green", i % 2 == 0 ? "#B8A365" : "#87924C"), s, new(p.X - .6 + i * .17, p.Y - .52), new(p.X - .6 + i * .17, p.Y + .52), .13, .026);
            House(batch, p.X + .42, Terrain3D.Height(s, p.X + .42, p.Y + .35), p.Y + .35, .18, .12, .22);
        }
        else if (tile.Improvement == Improvement.Mine)
        {
            double y = Terrain3D.Height(s, p.X, p.Y); batch.Mesh("mine-hole", "#313B37").Box(p.X, y, p.Y, .36, .2, .29);
            batch.Mesh("mine-beams", "#9C8359").Box(p.X - .19, y, p.Y - .17, .055, .30, .06); batch.Mesh("mine-beams", "#9C8359").Box(p.X + .19, y, p.Y - .17, .055, .30, .06);
            batch.Mesh("mine-beams", "#9C8359").Box(p.X, y + .26, p.Y - .17, .45, .06, .08);
            batch.Mesh("ore", "#777C78").Sphere(p.X + .35, y + .10, p.Y + .15, .22, .16, .19, 8, 4);
        }
        else House(batch, p.X, Terrain3D.Height(s, p.X, p.Y), p.Y, .38, .26, .30);
    }
    private static void Ruin(SceneBatch batch, GameState s, Point p)
    {
        double y = Terrain3D.Height(s, p.X, p.Y); var stone = batch.Mesh("ruins", "#A29E87");
        stone.Cylinder(p.X - .16, y, p.Y, .055, .32); stone.Cylinder(p.X + .16, y, p.Y, .055, .22); stone.Box(p.X, y + .25, p.Y, .4, .07, .11);
    }
    private static void ResourceModel(SceneBatch batch, GameState s, Tile tile)
    {
        var p = Terrain3D.Center(tile); double x = p.X + .52, z = p.Y + .18, y = Terrain3D.Height(s, x, z);
        if (tile.Resource == Resource.Grain)
            for (int i = 0; i < 4; i++) { batch.Mesh("wheat", "#D5BC73").Cylinder(x + i * .04, y, z, .012, .18, sides: 5); batch.Mesh("wheat", "#D5BC73").Sphere(x + i * .04, y + .18, z, .022, .055, .022, 6, 4); }
        else if (tile.Resource == Resource.Horses) Horse(batch, x, y, z, .55);
        else batch.Mesh("resource-" + tile.Resource, tile.Resource == Resource.Iron ? "#747F82" : tile.Resource == Resource.Salt ? "#DDD7BB" : tile.Resource == Resource.Dyes ? "#92738C" : "#AAA997").Sphere(x, y + .055, z, .13, .12, .11, 8, 4);
    }
}
