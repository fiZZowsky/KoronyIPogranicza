namespace Crown.Core;

public sealed record SiegeReport(int Fronts, bool SeaClosed, bool Encircled, int Repair);

/// <summary>Derived from actual positions, diplomacy and orders; no hidden siege timer.</summary>
public static class SiegeWarfare
{
    public static SiegeReport Report(GameState s, City city)
    {
        if (!s.EpochCampaign) return new(0, false, false, 7);
        int fronts = s.Units.Where(u => MilitaryStrategy.Combatant(u) && u.Health >= 25 &&
            u.EscortCaravanId == null && Engine.AtWar(s, city.Owner, u.Owner) &&
            World.Distance(s.Tiles[u.Tile], s.Tiles[city.Tile]) == 1).Select(u => u.Tile).Distinct().Count();
        bool seaClosed = Navy.Coastal(s, city) && s.Units.Any(u => Navy.IsShip(u) && u.Health >= 25 &&
            Engine.AtWar(s, city.Owner, u.Owner) && World.Distance(s.Tiles[u.Tile], s.Tiles[city.Tile]) == 1);
        bool encircled = fronts >= 3 && (!Navy.Coastal(s, city) || seaClosed);
        return new(fronts, seaClosed, encircled, encircled ? 0 : fronts >= 2 ? 2 : 7);
    }

    public static double WallBonus(GameState s, City city) => !city.Buildings.Contains("walls") ? 0 :
        s.EpochCampaign ? 10 * (.35 + .65 * city.Health / 100) : 10;

    public static bool BeachheadGuard(Unit u) => SeaTransport.Active(u) &&
        u.Kind == UnitKind.Spearmen && u.Health >= 40 && u.EscortCaravanId == null &&
        u.Order == MilitaryOrder.Fortify && u.FortificationTurns >= 1;

    public static IEnumerable<Unit> Beachheads(GameState s, int owner)
    {
        if (!s.EpochCampaign) return [];
        var guards=s.Units.Where(u=>u.Owner==owner&&BeachheadGuard(u)).ToArray();
        if(guards.Length==0)return [];
        // Naval supply never calls terrestrial supply, so a beachhead cannot supply its own ship.
        var ships = s.Units.Where(u => u.Owner == owner && Navy.IsShip(u) && u.Health >= 40)
            .Where(u => guards.Any(g => World.Distance(s.Tiles[g.Tile],s.Tiles[u.Tile]) == 1))
            .Where(u => !s.Units.Any(e => Navy.IsShip(e) && Engine.AtWar(s, owner, e.Owner) &&
                World.Distance(s.Tiles[u.Tile], s.Tiles[e.Tile]) <= 1))
            .Where(u => Navy.Supplied(s, u)).ToArray();
        return guards.Where(u =>
            s.Tiles[u.Tile].Passable && (s.Tiles[u.Tile].Owner < 0 || s.Tiles[u.Tile].Owner == owner || Engine.AtWar(s, owner, s.Tiles[u.Tile].Owner)) &&
            !s.Units.Any(e => SeaTransport.Active(e) && e.Owner != owner && e.Tile == u.Tile) &&
            !s.Cities.Any(c => c.Tile == u.Tile && c.Owner != owner) &&
            ships.Any(ship => World.Distance(s.Tiles[u.Tile], s.Tiles[ship.Tile]) == 1)).ToArray();
    }

    public static bool BeachheadSupplies(GameState s, Unit unit)
    {
        if (!s.EpochCampaign || !SeaTransport.Active(unit) || Navy.IsShip(unit)) return false;
        if(!s.Units.Any(g=>g.Owner==unit.Owner&&BeachheadGuard(g)&&World.Distance(s.Tiles[g.Tile],s.Tiles[unit.Tile])<=2))return false;
        foreach (var guard in Beachheads(s, unit.Owner))
        {
            if (World.Distance(s.Tiles[guard.Tile], s.Tiles[unit.Tile]) > 2) continue;
            var route = Navigation.SearchFrom(s, guard.Tile, t => t.Passable &&
                (t.Owner < 0 || t.Owner == unit.Owner || Engine.AtWar(s, unit.Owner, t.Owner)) &&
                !s.Units.Any(e => SeaTransport.Active(e) && e.Owner != unit.Owner && e.Tile == t.Id) &&
                !s.Cities.Any(c => c.Owner != unit.Owner && c.Tile == t.Id), (_, _) => 1, 2);
            if (route.Costs.ContainsKey(unit.Tile)) return true;
        }
        return false;
    }

    public static HashSet<int> BeachheadCoverage(GameState s,int owner)
    {
        var covered=new HashSet<int>();
        foreach(var guard in Beachheads(s,owner))
            covered.UnionWith(Navigation.SearchFrom(s,guard.Tile,t=>t.Passable&&
                (t.Owner<0||t.Owner==owner||Engine.AtWar(s,owner,t.Owner))&&
                !s.Units.Any(e=>SeaTransport.Active(e)&&e.Owner!=owner&&e.Tile==t.Id)&&
                !s.Cities.Any(c=>c.Owner!=owner&&c.Tile==t.Id),(_,_)=>1,2).Costs.Keys);
        return covered;
    }
}

public static class AmphibiousAssault
{
    public static CombatPreview Preview(GameState s, int unitId, int target, int owner = 0)
    {
        var unit = s.Units.FirstOrDefault(u => u.Id == unitId);
        CombatPreview Invalid(string reason) => new(false, reason, unit == null ? "" : UnitModels.Name(unit), "");
        if (s.Ending != null || !s.EpochCampaign || owner is < 0 or > 3 || unit == null || unit.Owner != owner ||
            unit.Kind is not (UnitKind.Spearmen or UnitKind.Cavalry) || unit.EmbarkedOnId == null)
            return Invalid("Szturm z morza wymaga piechoty lub jazdy na własnym okręcie.");
        var ship = SeaTransport.Carrier(s, unit);
        if (ship == null || ship.Actions < 1 || target < 0 || target >= s.Tiles.Count)
            return Invalid("Szturm wymaga 1 punktu ruchu przewoźnika.");
        var tile = s.Tiles[target];
        if (!tile.Passable || World.Distance(tile, s.Tiles[ship.Tile]) != 1)
            return Invalid("Szturm jest możliwy tylko na sąsiedni brzeg.");
        if (!MilitaryStrategy.Sight(s, owner).Contains(target) || owner == 0 && !tile.Explored)
            return Invalid("Obrońcy muszą być obecnie widoczni.");
        if (tile.Owner >= 0 && tile.Owner != owner && !Engine.AtWar(s, owner, tile.Owner))
            return Invalid("Nie można szturmować brzegu państwa, z którym trwa pokój.");
        return Engine.PreviewAttackCore(s, unitId, target, true);
    }

    public static Result Execute(GameState s, int unitId, int target, int owner = 0)
    {
        var preview = Preview(s, unitId, target, owner);
        if (!preview.Valid) return Result.Fail(preview.Reason);
        var unit = s.Units.Single(u => u.Id == unitId);
        var ship = SeaTransport.Carrier(s, unit)!;
        // The assault occupies the carrier for the rest of this turn. Cargo already has
        // zero independent actions, so this also prevents repeated attacks by the same passenger.
        ship.Actions = 0; ship.Destination = null; MilitaryOrders.Clear(ship);
        var defender = s.Units.FirstOrDefault(e => SeaTransport.Active(e) && e.Tile == target && e.Owner != owner);
        var city = s.Cities.FirstOrDefault(c => c.Tile == target && c.Owner != owner);
        var result = Engine.ResolveAttack(s, unit, target, defender, city, preview);
        if (s.Units.Contains(unit) && !s.Units.Any(e => SeaTransport.Active(e) && e.Owner != owner && e.Tile == target) &&
            !s.Cities.Any(c => c.Owner != owner && c.Tile == target))
        { unit.EmbarkedOnId = null; unit.Tile = target; }
        // A defeated assault leaves survivors aboard; a successful landing has no further actions.
        s.InvalidateLogistics(); Engine.Explore(s);
        return Result.Ok(result.Message + (unit.EmbarkedOnId == null && s.Units.Contains(unit) ? " Przyczółek zdobyty; oddział działa od następnej tury." : s.Units.Contains(unit) ? " Obrońcy utrzymali brzeg; ocalały oddział wrócił na pokład." : " Oddział szturmowy utracony."));
    }
}
