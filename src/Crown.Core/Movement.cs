namespace Crown.Core;

public static partial class Engine
{
    private static bool CanEnter(GameState s, Unit u, Tile t) => Navigation.CanEnter(s, u, t);

    public static Result Move(GameState s, int unitId, int target)
    {
        var u = s.Units.FirstOrDefault(u => u.Id == unitId);
        if (s.Ending != null || u == null || u.Actions < .5 || target < 0 || target >= s.Tiles.Count)
            return Result.Fail("Brak ruchu lub jednostki.");
        if(u.EscortCaravanId!=null)return Result.Fail("Najpierw odwołaj eskortę, aby wydać samodzielny rozkaz.");
        if (u.Owner == 0 && !s.Tiles[target].Explored) return Result.Fail("Najpierw odkryj to pole.");
        if (u.Tile == target) return Result.Fail("Jednostka już tu jest.");
        var enemy = s.Units.FirstOrDefault(e => e.Tile == target && e.Owner != u.Owner);
        var city = s.Cities.FirstOrDefault(c => c.Tile == target && c.Owner != u.Owner);
        if (enemy != null || city != null) return Attack(s, u, target, enemy, city);
        var path = Navigation.ForUnit(s, u, u.Actions, target).PathTo(target);
        if (path.Count == 0) return Result.Fail("Cel jest niedostępny lub poza zasięgiem tej tury.");
        u.Destination = null;
        foreach (int next in path.Skip(1)) EnterTile(s, u, next);
        return Result.Ok($"{Content.UnitName(u.Kind)}: pozostało {u.Actions:0.#} punktów ruchu.");
    }

    public static Result OrderMove(GameState s, int unitId, int target)
    {
        var u = s.Units.FirstOrDefault(u => u.Id == unitId && u.Owner == 0);
        if (s.Ending != null || u == null || target < 0 || target >= s.Tiles.Count)
            return Result.Fail("Nieprawidłowy rozkaz marszu.");
        if(u.EscortCaravanId!=null)return Result.Fail("Najpierw odwołaj eskortę, aby wydać samodzielny rozkaz.");
        if (u.Tile == target) return CancelOrder(s, unitId);
        if (Navigation.Route(s, u, target).Count == 0)
            return Result.Fail("Brak dostępnej, odkrytej trasy. Marsz nie przekracza obcych granic w czasie pokoju.");
        MilitaryOrders.Clear(u);
        u.Destination = target;
        FollowOrder(s, u);
        return Result.Ok(u.Destination == null ? "Oddział dotarł do celu." : "Rozkaz przyjęty. Marsz będzie kontynuowany po kolejnych turach.");
    }

    public static Result CancelOrder(GameState s, int unitId)
    {
        var u = s.Units.FirstOrDefault(u => u.Id == unitId && u.Owner == 0);
        if (s.Ending != null || u == null) return Result.Fail("Nieprawidłowy oddział.");
        if(u.EscortCaravanId!=null)return Result.Fail("Ten oddział eskortuje karawanę. Użyj odwołania eskorty.");
        u.Destination = null;
        return Result.Ok("Anulowano dalszy marsz. Pozostałe punkty ruchu są dostępne.");
    }

    private static void EnterTile(GameState s, Unit u, int next)
    {
        MilitaryOrders.Clear(u);
        u.Actions -= Navigation.MovementCost(s, u.Tile, next);
        u.Tile = next;
        s.InvalidateLogistics();
        var tile = s.Tiles[next];
        if (tile.Ruin)
        {
            tile.Ruin = false;
            var f = s.Factions[u.Owner];
            int gold = 25 + s.Next(40);
            f.Gold += gold; f.Science += 12; f.Prestige += 4;
            if (u.Owner == 0) s.Log($"Zwiadowcy zbadali dawne ruiny: +{gold} złota, +12 nauki i +4 prestiżu.", "odkrycie");
        }
        if (u.Owner == 0) Explore(s);
    }

    private static void FollowOrder(GameState s, Unit u)
    {
        if (u.Destination is not int goal || s.Ending != null) return;
        var path = Navigation.Route(s, u, goal);
        if (path.Count == 0)
        {
            u.Destination = null;
            s.Log($"{Content.UnitName(u.Kind)}: marsz wstrzymany — trasa została zablokowana. Wydaj nowy rozkaz.", "rozkazy");
            return;
        }
        foreach (int next in path.Skip(1))
        {
            if (Navigation.MovementCost(s, u.Tile, next) > u.Actions) break;
            EnterTile(s, u, next);
        }
        if (u.Tile == goal)
        {
            u.Destination = null;
            s.Log($"{Content.UnitName(u.Kind)} dotarli do celu marszu ({s.Tiles[goal].X}, {s.Tiles[goal].Y}).", "rozkazy");
        }
    }
}
