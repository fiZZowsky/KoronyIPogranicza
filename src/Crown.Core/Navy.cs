namespace Crown.Core;

public static class Navy
{
    public const string Port = "seaport";
    public static readonly UnitModel[] Models =
    [
        new("coastal_galley", "Galera przybrzeżna", 2, "bronze", UnitKind.Ship, 24, 3, 1, 100),
        new("war_galley", "Galera wojenna", 4, "classical_institutions", UnitKind.Ship, 44, 3, 1, 210),
        new("carrack", "Karaka wojenna", 6, "navigation", UnitKind.Ship, 72, 4, 2, 420),
        new("frigate", "Fregata żaglowa", 7, "scientific_method", UnitKind.Ship, 95, 4, 2, 600),
        new("ironclad", "Okręt pancerny", 8, "steam_power", UnitKind.Ship, 130, 4, 2, 900),
        new("destroyer", "Niszczyciel rakietowy", 10, "rocketry", UnitKind.Ship, 220, 5, 3, 1900),
        new("autonomous_frigate", "Fregata autonomiczna", 12, "robotics", UnitKind.Ship, 290, 5, 3, 3000),
        new("fusion_cruiser", "Krążownik fuzyjny", 13, "fusion", UnitKind.Ship, 370, 6, 3, 4500),
        new("ocean_guardian", "Strażnik oceaniczny", 17, "distributed_civilization", UnitKind.Ship, 650, 6, 4, 10000)
    ];
    public static bool IsShip(Unit u) => u.Kind == UnitKind.Ship;
    public static bool Water(Tile t) => t.Terrain is Terrain.Coast or Terrain.Ocean;
    public static bool OceanCapable(Unit u) => (UnitModels.For(u)?.Era ?? 2) >= 6;
    public static bool TerrainAllowed(GameState s, Unit u, Tile t) => IsShip(u) ? Water(t) && (t.Terrain == Terrain.Coast || OceanCapable(u)) : t.Passable;
    public static bool Coastal(GameState s, City c) => World.Neighbors(s, c.Tile).Any(Water);
    public static bool Blockaded(GameState s, City c) => c.Buildings.Contains(Port) &&
        s.Units.Any(u => IsShip(u) && Engine.AtWar(s, c.Owner, u.Owner) && World.Distance(s.Tiles[u.Tile], s.Tiles[c.Tile]) == 1);
    public static bool ActivePort(GameState s, City c) => c.Buildings.Contains(Port) && Coastal(s, c) && !Blockaded(s, c);
    public static int? LaunchTile(GameState s, City c, UnitModel model) => World.Neighbors(s, c.Tile)
        .Where(t => TerrainAllowed(s, new Unit { Kind = UnitKind.Ship, ModelId = model.Id }, t) &&
            (c.Owner != 0 || t.Explored) && (t.Owner < 0 || t.Owner == c.Owner || Engine.AtWar(s, c.Owner, t.Owner)) && !s.Units.Any(u => u.Owner != c.Owner && u.Tile == t.Id))
        .OrderBy(t => s.Units.Count(u => u.Tile == t.Id)).ThenBy(t => t.Id).Select(t => (int?)t.Id).FirstOrDefault();
    public static string? ProjectError(GameState s, City c, string project)
    {
        var model = Models.FirstOrDefault(m => m.Id == project);
        if (project != Port && model == null) return null;
        if (!s.EpochCampaign) return "Porty i flota wymagają pełnej kampanii epok.";
        if (!Coastal(s, c)) return "Potrzebne miasto bezpośrednio nad morzem lub jeziorem.";
        if (model == null) return null;
        if (!c.Buildings.Contains(Port)) return "Najpierw zbuduj port morski w tym mieście.";
        if (Blockaded(s, c)) return "Blokada morska wstrzymuje wodowanie okrętów.";
        return LaunchTile(s, c, model) == null ? "Brak dostępnego pola wodowania dla tego okrętu." : null;
    }
    public static double Upkeep(GameState s, int owner) => s.Cities.Count(c => c.Owner == owner && c.Buildings.Contains(Port)) * 2;
    public static double SupplyRange(GameState s, int owner) => 8 + s.Factions[owner].Era / 2;
    public static Navigation.Search Supply(GameState s, Unit u)
    {
        var costs = new Dictionary<int, double>();var previous = new Dictionary<int, int>();
        var enemy = s.Units.Where(e => IsShip(e) && Engine.AtWar(s, u.Owner, e.Owner)).Select(e => e.Tile).ToHashSet();
        foreach (var port in s.Cities.Where(c => c.Owner == u.Owner && ActivePort(s, c)))
        foreach (var shore in World.Neighbors(s, port.Tile).Where(t => TerrainAllowed(s, u, t) && !enemy.Contains(t.Id)))
        {
            var search = Navigation.SearchFrom(s, shore.Id, t => TerrainAllowed(s, u, t) && !enemy.Contains(t.Id) &&
                (t.Owner < 0 || t.Owner == u.Owner || Engine.AtWar(s, u.Owner, t.Owner)), (_, _) => 1, SupplyRange(s, u.Owner));
            foreach (var pair in search.Costs) if (pair.Value < costs.GetValueOrDefault(pair.Key, double.PositiveInfinity)) costs[pair.Key] = pair.Value;
        }
        return new(costs, previous);
    }
    public static bool Supplied(GameState s, Unit u) => Supply(s, u).Costs.ContainsKey(u.Tile);
    public static bool UpgradePort(GameState s, Unit u) => s.Cities.Any(c => c.Owner == u.Owner && ActivePort(s, c) && World.Distance(s.Tiles[c.Tile], s.Tiles[u.Tile]) == 1);
    public static Project? AiProject(GameState s, City c)
    {
        if (!s.EpochCampaign || !Coastal(s, c) || s.Factions[c.Owner].Era < 2 || s.Factions[c.Owner].Gold < 150) return null;
        if (!c.Buildings.Contains(Port)) return Engine.AvailableProjects(s, c).FirstOrDefault(p => p.Id == Port);
        int ports = s.Cities.Count(p => p.Owner == c.Owner && p.Buildings.Contains(Port));
        bool war = s.Relations.Any(r => (r.A == c.Owner || r.B == c.Owner) && r.War);
        int pending = s.Cities.Count(p => p.Owner == c.Owner && p.Id != c.Id && Models.Any(m => m.Id == p.Project));
        if (s.Units.Count(u => u.Owner == c.Owner && IsShip(u)) + pending >= Math.Min(4, ports + (war ? 2 : 1))) return null;
        return Engine.AvailableProjects(s, c).Where(p => p.Unit == UnitKind.Ship).OrderByDescending(p => Models.First(m => m.Id == p.Id).Strength).FirstOrDefault();
    }
    internal static void Validate(GameState s)
    {
        bool NavalProject(string id) => id == Port || Models.Any(m => m.Id == id);
        if (s.Version < 21 && (s.Units.Any(IsShip) || s.Cities.Any(c => c.Buildings.Contains(Port) || NavalProject(c.Project) || c.ProductionQueue.Any(o => NavalProject(o.Project)))))
            throw new InvalidDataException("Stary format nie może zawierać floty lub portów.");
        if (s.Units.Any(u => IsShip(u) && (!s.EpochCampaign || !Models.Any(m => m.Id == u.ModelId) || !TerrainAllowed(s, u, s.Tiles[u.Tile]) || u.EscortCaravanId != null || u.Order == MilitaryOrder.Fortify)))
            throw new InvalidDataException("Nieprawidłowy okręt, akwen lub rozkaz morski.");
        if (s.Cities.Any(c => c.Buildings.Contains(Port) && (!s.EpochCampaign || !Coastal(s, c)))) throw new InvalidDataException("Port musi leżeć przy akwenie w pełnej kampanii.");
    }
}

public static partial class Engine
{
    public static void RunNavalAi(GameState s, int owner)
    {
        if (!s.EpochCampaign || owner is < 1 or > 3 || s.Ending != null) return;
        foreach (var u in s.Units.Where(u => u.Owner == owner && Navy.IsShip(u)).OrderBy(u => u.Id).ToArray())
        {
            if (s.Ending != null) break;
            if (u.Actions < .5) continue;
            var upgrade=UnitModels.Upgrades(s,u).LastOrDefault();
            if(upgrade!=null&&s.Factions[owner].Gold>UnitModels.UpgradeCost(s,u,upgrade)+200&&UnitModels.UpgradeError(s,u,upgrade.Id,owner)==null)
            {UpgradeUnit(s,u.Id,upgrade.Id,owner);continue;}
            var visible = MilitaryStrategy.Sight(s, owner);
            if (u.Health < 45 || !Supplied(s, u) && u.Health < 70)
            {
                if (Supplied(s, u) && !s.Units.Any(e => AtWar(s, owner, e.Owner) && visible.Contains(e.Tile) && World.Distance(s.Tiles[u.Tile], s.Tiles[e.Tile]) <= 3))
                { if (u.Order != MilitaryOrder.Recover) SetMilitaryOrder(s, u.Id, MilitaryOrder.Recover, owner); continue; }
                var supply = Navy.Supply(s, u);var route = Navigation.ForUnit(s, u);
                int safe = route.Costs.Keys.Where(supply.Costs.ContainsKey).OrderBy(t => route.Costs[t]).ThenBy(t => t).DefaultIfEmpty(u.Tile).First();
                Sail(route.PathTo(safe));continue;
            }
            if (Fire()) continue;
            var enemy = s.Units.Where(e => Navy.IsShip(e) && AtWar(s, owner, e.Owner) && visible.Contains(e.Tile)).OrderBy(e => World.Distance(s.Tiles[u.Tile], s.Tiles[e.Tile])).ThenBy(e => e.Id).FirstOrDefault();
            var port = s.Cities.Where(c => AtWar(s, owner, c.Owner) && c.Buildings.Contains(Navy.Port)).OrderBy(c => World.Distance(s.Tiles[u.Tile], s.Tiles[c.Tile])).ThenBy(c => c.Id).FirstOrDefault();
            int? target = enemy?.Tile ?? port?.Tile;
            var ownPort = s.Cities.Where(c => c.Owner == owner && c.Buildings.Contains(Navy.Port)).OrderBy(c => World.Distance(s.Tiles[u.Tile], s.Tiles[c.Tile])).ThenBy(c => c.Id).FirstOrDefault();
            target ??= ownPort?.Tile;if (target == null) continue;
            var search = Navigation.ForUnit(s, u);
            int radius = enemy != null ? Math.Min(2, UnitModels.Range(u)) : 1;
            int goal = search.Costs.Keys.Where(t => World.Distance(s.Tiles[t], s.Tiles[target.Value]) <= radius)
                .OrderBy(t => search.Costs[t] + s.Units.Count(e => e.Owner == owner && e.Id != u.Id && e.Tile == t) * 2).ThenBy(t => t).DefaultIfEmpty(u.Tile).First();
            Sail(search.PathTo(goal));Fire();
            void Sail(IReadOnlyList<int> path)
            { foreach (int next in path.Skip(1)) { if (Navigation.MovementCost(s, u.Tile, next) > u.Actions) break; EnterTile(s, u, next); } }
            bool Fire()
            {
                if (u.Actions < 1) return false;
                visible=MilitaryStrategy.Sight(s,owner);
                var shot = s.Units.Where(e => AtWar(s, owner, e.Owner) && visible.Contains(e.Tile)).Select(e => e.Tile)
                    .Concat(s.Cities.Where(c => AtWar(s, owner, c.Owner) && visible.Contains(c.Tile)).Select(c => c.Tile)).Distinct()
                    .Select(t => (Tile: t, Preview: PreviewAttack(s, u.Id, t))).Where(x => x.Preview.Valid && x.Preview.DamageToTarget > 0)
                    .OrderByDescending(x => x.Preview.DamageToTarget + (x.Preview.DamageToTarget >= x.Preview.TargetHealth ? 30 : 0)).ThenBy(x => x.Tile).FirstOrDefault();
                return shot.Preview != null && Move(s, u.Id, shot.Tile).Success;
            }
        }
    }
}
