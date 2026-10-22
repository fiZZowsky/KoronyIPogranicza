namespace Crown.Core;

/// <summary>Shared weighted pathfinding for movement, road planning and map previews.</summary>
public static class Navigation
{
    public sealed record Search(Dictionary<int, double> Costs, Dictionary<int, int> Previous)
    {
        public IReadOnlyList<int> PathTo(int destination)
        {
            if (!Costs.ContainsKey(destination)) return [];
            var path = new List<int> { destination };
            while (Previous.TryGetValue(path[^1], out int previous)) path.Add(previous);
            path.Reverse();
            return path;
        }
    }

    public static Search SearchFrom(GameState s, int start, Func<Tile, bool> canEnter,
        Func<int, int, double> cost, double budget = double.PositiveInfinity, int? target = null, Func<int, bool>? reached = null)
    {
        var costs = new Dictionary<int, double> { [start] = 0 };
        var previous = new Dictionary<int, int>();
        var queue = new PriorityQueue<int, double>();
        queue.Enqueue(start, 0);
        while (queue.TryDequeue(out int current, out double priority))
        {
            if (priority > costs[current]) continue;
            if (current == target || reached?.Invoke(current) == true) break;
            foreach (var next in World.Neighbors(s, current))
            {
                if (!canEnter(next)) continue;
                double distance = priority + cost(current, next.Id);
                if (distance > budget || costs.GetValueOrDefault(next.Id, double.PositiveInfinity) <= distance) continue;
                costs[next.Id] = distance;
                previous[next.Id] = current;
                queue.Enqueue(next.Id, distance);
            }
        }
        return new(costs, previous);
    }

    public static bool CanEnter(GameState s, Unit u, Tile t) => t.Passable &&
        (t.Owner < 0 || t.Owner == u.Owner || Engine.AtWar(s, u.Owner, t.Owner)) &&
        !s.Units.Any(e => e.Owner != u.Owner && e.Tile == t.Id) &&
        !s.Cities.Any(c => c.Tile == t.Id && c.Owner != u.Owner);

    public static bool RoadNode(GameState s, int tile) => s.Tiles[tile].Road || s.Cities.Any(c => c.Tile == tile);

    public static double MovementCost(GameState s, int from, int to)
    {
        if (RoadNode(s, from) && RoadNode(s, to)) return .5;
        return s.Tiles[to].Terrain is Terrain.Forest or Terrain.Hills or Terrain.Marsh ? 2 : 1;
    }

    public static Search ForUnit(GameState s, Unit u, double budget = double.PositiveInfinity, int? target = null) =>
        SearchFrom(s, u.Tile, t => CanEnter(s, u, t) && (u.Owner != 0 || t.Explored),
            (from, to) => MovementCost(s, from, to), budget, target);

    public static IReadOnlyList<int> Route(GameState s, Unit u, int target)
    {
        if (target < 0 || target >= s.Tiles.Count) return [];
        return ForUnit(s, u, target: target).PathTo(target);
    }
}
