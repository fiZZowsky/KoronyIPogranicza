namespace Crown.Core;

public sealed record SupplyMap(int Revision, int Turn, int TechCount, Dictionary<int, double> Costs,
    Dictionary<int, int> SourceCities, HashSet<int> Interdicted, HashSet<int> ConnectedCities, Dictionary<int, int> MarketGroups);

public static class Logistics
{
    public const int RoadPrice = 25;
    public const double RoadUpkeep = .15;

    public static double Range(GameState s, int owner) => 6 +
        (s.Factions[owner].Technologies.Contains("trade") ? 2 : 0) +
        (s.Factions[owner].Technologies.Contains("engineering") ? 2 : 0) +
        (s.Factions[owner].Flags.Contains("road_patrols") ? 1 : 0);

    public static SupplyMap For(GameState s, int owner)
    {
        int techCount = s.Factions[owner].Technologies.Count;
        if (s.LogisticsCache.TryGetValue(owner, out var cached) && cached.Revision == s.LogisticsRevision &&
            cached.Turn == s.Turn && cached.TechCount == techCount) return cached;
        var map = Analyze(s, owner);
        s.LogisticsCache[owner] = map;
        return map;
    }

    public static SupplyMap Analyze(GameState s, int owner)
    {
        var cities = s.Cities.Where(c => c.Owner == owner).OrderBy(c => c.Id).ToArray();
        var cityTiles = cities.Select(c => c.Tile).ToHashSet();
        var escorts = s.Units.Where(u => u.Owner == owner && u.Kind is not (UnitKind.Scout or UnitKind.Settler))
            .Select(u => u.Tile).ToHashSet();
        var blocked = new HashSet<int>();
        foreach (var enemy in s.Units.Where(u => Engine.AtWar(s, owner, u.Owner) && u.Kind != UnitKind.Settler))
        {
            blocked.Add(enemy.Tile);
            foreach (var tile in World.Neighbors(s, enemy.Tile))
                if (!escorts.Contains(tile.Id) && !cityTiles.Contains(tile.Id)) blocked.Add(tile.Id);
        }
        bool Transit(Tile t) => t.Passable && !blocked.Contains(t.Id) &&
            (t.Owner < 0 || t.Owner == owner || Engine.AtWar(s, owner, t.Owner));
        var costs = new Dictionary<int, double>();
        var sources = new Dictionary<int, int>();
        var frontier = new PriorityQueue<int, double>();
        foreach (var city in cities.Where(c => !blocked.Contains(c.Tile)))
        {
            costs[city.Tile] = 0;
            sources[city.Tile] = city.Id;
            frontier.Enqueue(city.Tile, 0);
        }
        while (frontier.TryDequeue(out int at, out double priority))
        {
            if (priority > costs[at]) continue;
            foreach (var tile in World.Neighbors(s, at))
            {
                if (!Transit(tile)) continue;
                bool road = Navigation.RoadNode(s, at) && Navigation.RoadNode(s, tile.Id) &&
                    !Engine.AtWar(s, owner, tile.RoadOwner) && !Engine.AtWar(s, owner, s.Tiles[at].RoadOwner);
                double step = road ? .5 : tile.Terrain is Terrain.Forest or Terrain.Hills or Terrain.Marsh ? 2 : 1;
                if (!road && s.Turn % 4 == 0) step *= 1.25;
                if (tile.Owner >= 0 && tile.Owner != owner) step += .5;
                double next = priority + step;
                if (costs.GetValueOrDefault(tile.Id, double.PositiveInfinity) <= next) continue;
                costs[tile.Id] = next;
                sources[tile.Id] = sources[at];
                frontier.Enqueue(tile.Id, next);
            }
        }
        var tileGroups = new Dictionary<int, int>();
        var marketGroups = new Dictionary<int, int>();
        foreach (var city in cities)
        {
            if (tileGroups.TryGetValue(city.Tile, out int existing)) { marketGroups[city.Id] = existing; continue; }
            var queue = new Queue<int>();
            queue.Enqueue(city.Tile);
            tileGroups[city.Tile] = city.Id;
            marketGroups[city.Id] = city.Id;
            while (queue.TryDequeue(out int at))
                foreach (var next in World.Neighbors(s, at))
                    if (Transit(next) && (cityTiles.Contains(next.Id) || next.Road && next.RoadOwner == owner) &&
                        (next.Owner < 0 || next.Owner == owner) && !tileGroups.ContainsKey(next.Id))
                    { tileGroups[next.Id] = city.Id; queue.Enqueue(next.Id); }
        }
        return new(s.LogisticsRevision, s.Turn, s.Factions[owner].Technologies.Count, costs, sources, blocked,
            cities.Where(c => marketGroups[c.Id] == cities[0].Id).Select(c => c.Id).ToHashSet(), marketGroups);
    }

    public static bool Supplied(GameState s, Unit unit) =>
        For(s, unit.Owner).Costs.GetValueOrDefault(unit.Tile, double.PositiveInfinity) <= Range(s, unit.Owner);

    public static RoadPlan PlanRoad(GameState s, int cityId)
    {
        var city = s.Cities.FirstOrDefault(c => c.Id == cityId);
        if (city == null) return new([], 0, 0, "Nieznane miasto.");
        var capital = s.Cities.Where(c => c.Owner == city.Owner).MinBy(c => c.Id)!;
        if (capital.Id == city.Id) return new([city.Tile], 0, 0);
        var route = Navigation.SearchFrom(s, capital.Tile,
            t => t.Passable && (t.Owner < 0 || t.Owner == city.Owner) && (!t.Road || t.RoadOwner == city.Owner) && (city.Owner != 0 || t.Explored) &&
                 !s.Units.Any(u => u.Owner != city.Owner && u.Tile == t.Id),
            (from, to) => Navigation.RoadNode(s, to) && (s.Tiles[to].RoadOwner == city.Owner || s.Cities.Any(c => c.Tile == to && c.Owner == city.Owner)) ? .2 : 1,
            target: city.Tile).PathTo(city.Tile);
        if (route.Count == 0) return new([], 0, 0, "Brak dostępnej, odkrytej trasy lądowej do stolicy.");
        int missing = route.Count(id => !Navigation.RoadNode(s, id));
        return new(route, missing, missing * RoadPrice);
    }
}
