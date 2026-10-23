namespace Crown.Core;

public static class World
{
    public static GameState CreateLegacy(int seed = 17421, string difficulty = "Standard") => Create(seed, difficulty, false);
    public static GameState Create(int seed = 17421, string difficulty = "Standard", bool extended = true)
    {
        var s = new GameState { Seed = seed, RandomState = unchecked((uint)seed) | 1u, Difficulty = difficulty, EpochCampaign = extended, MaxTurns = extended ? Epochs.CampaignTurns : 320 };
        for (int y=0; y<s.Height; y++) for(int x=0;x<s.Width;x++)
        {
            double edge = Math.Min(Math.Min(x / 5.0, (s.Width-1-x)/5.0), Math.Min(y/3.0,(s.Height-1-y)/3.0));
            double h = Noise(x/5.0, y/5.0,seed) * .65 + Noise(x/2.1,y/2.1,seed+7)*.25 + Math.Min(edge,1)*.45;
            double moisture = Noise(x/6.0+50,y/6.0,seed+31);
            Terrain t = h<.36 ? Terrain.Ocean : h<.46 ? Terrain.Coast : h>1.08 ? Terrain.Mountain : h>.94 ? Terrain.Hills : y<3 || y>25 ? Terrain.Tundra : moisture>.58 ? Terrain.Forest : moisture<.26 ? Terrain.Desert : Terrain.Grassland;
            bool river = t is not (Terrain.Ocean or Terrain.Coast or Terrain.Mountain) && x==(int)Math.Round(12+Math.Sin(y*.28)*3);
            if(river && t == Terrain.Desert) t = Terrain.Grassland;
            var tile = new Tile { Id=s.Tiles.Count,X=x,Y=y,Terrain=t,River=river };
            if(tile.Passable && s.Next(100)<18) tile.Resource=(Resource)(1+s.Next(6));
            if(tile.Passable && s.Next(100)<3) tile.Ruin=true;
            s.Tiles.Add(tile);
        }
        s.Factions =
        [
            new(){Id=0,Name="Marchie Veyru",Leader="Ty — Strażnik Marchii",Color="#D1B273",Trait="Pogranicznicy · +1 produkcji w miastach"},
            new(){Id=1,Name="Liga Talassu",Leader="Irena z Talassu",Color="#65B4C2",Trait="Kupcy mórz · +3 złota w miastach"},
            new(){Id=2,Name="Arweńska Unia",Leader="Konsul Seran",Color="#B49DD4",Trait="Uczeni · +2 nauki w miastach"},
            new(){Id=3,Name="Klanowe Ziemie Kharu",Leader="Wódz Dagan",Color="#D17862",Trait="Ludzie żelaza · +3 siły wojsk"}
        ];
        (int X,int Y,string Name)[] starts=[(9,15,"Wysogród"),(30,8,"Talassar"),(31,21,"Arven"),(10,5,"Kar-Dun")];
        for(int f=0;f<4;f++)
        {
            var p=starts[f]; var center=s.Tiles[p.Y*s.Width+p.X];
            foreach(var tile in s.Tiles.Where(t=>Distance(t,center)<=3))
                if(!tile.Passable || tile.Terrain==Terrain.Desert) tile.Terrain= s.Next(5)==0 ? Terrain.Hills : Terrain.Grassland;
            center.Terrain=Terrain.Grassland; center.River=true; center.Ruin=false;
            s.Tiles[center.Id+1].Resource=Resource.Grain; s.Tiles[center.Id-1].Resource=Resource.Horses;
            Engine.AddCity(s,f,center.Id,p.Name);
            Engine.AddUnit(s,f,center.Id,UnitKind.Spearmen);
            Engine.AddUnit(s,f,center.Id+1,UnitKind.Scout);
        }
        for(int a=0;a<4;a++) for(int b=a+1;b<4;b++) s.Relations.Add(new(){A=a,B=b,Trust=b==3?-12:12});
        // Every capital belongs to the same land network, even when noise creates an inland sea.
        for(int f=1;f<starts.Length;f++)
        {
            var cursor=s.Tiles[starts[f].Y*s.Width+starts[f].X];var goal=s.Tiles[starts[0].Y*s.Width+starts[0].X];
            while(cursor.Id!=goal.Id){cursor=Neighbors(s,cursor.Id).OrderBy(t=>Distance(t,goal)).ThenBy(t=>t.Passable?0:1).First();if(!cursor.Passable)cursor.Terrain=Terrain.Hills;}
        }
        s.Tiles[16*s.Width+13].Terrain=Terrain.Forest; s.Tiles[16*s.Width+13].Ruin=true;
        if (extended)
        {
            foreach (var f in s.Factions) { f.Research = "stone_tools"; }
            foreach (var u in s.Units) { u.ModelId = u.Kind == UnitKind.Scout ? "tribal_scout" : "hunters"; u.Actions = u.MaxActions; }
            foreach (var c in s.Cities) c.Project = "flint_workshop";
        }
        s.Log("Cztery ludy osiadają w dolinie. Rada Wysogrodu oczekuje pierwszej decyzji.","kronika");
        Engine.Explore(s); return s;
    }
    private static double Hash(int x,int y,int seed)
    {
        uint a=unchecked((uint)(x*374761393+y*668265263+seed*1447));
        a=(a^(a>>13))*1274126177; return (a^(a>>16))/(double)uint.MaxValue;
    }
    private static double Noise(double x,double y,int seed)
    {
        int ix=(int)Math.Floor(x),iy=(int)Math.Floor(y); double fx=x-ix,fy=y-iy;
        fx=fx*fx*(3-2*fx); fy=fy*fy*(3-2*fy);
        double a=Hash(ix,iy,seed)*(1-fx)+Hash(ix+1,iy,seed)*fx;
        double b=Hash(ix,iy+1,seed)*(1-fx)+Hash(ix+1,iy+1,seed)*fx; return a*(1-fy)+b*fy;
    }
    public static int Distance(Tile a,Tile b)
    {
        int aq=a.X-(a.Y-(a.Y&1))/2,bq=b.X-(b.Y-(b.Y&1))/2;
        int dq=aq-bq,dr=a.Y-b.Y; return (Math.Abs(dq)+Math.Abs(dr)+Math.Abs(dq+dr))/2;
    }
    public static IEnumerable<Tile> Neighbors(GameState s,int id)
    {
        var t=s.Tiles[id]; int d=(t.Y&1)==0?-1:1;
        (int,int)[] dirs=[(-1,0),(1,0),(0,-1),(d,-1),(0,1),(d,1)];
        foreach(var (dx,dy) in dirs) { int x=t.X+dx,y=t.Y+dy; if(x>=0&&x<s.Width&&y>=0&&y<s.Height) yield return s.Tiles[y*s.Width+x]; }
    }
}
