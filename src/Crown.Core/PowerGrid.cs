namespace Crown.Core;

public enum PowerPriority { Reduced, Normal, Critical }
public sealed record CityPower(int CityId, int Demand, double Allocated, bool Isolated)
{
    public double Coverage => Demand == 0 ? 1 : Math.Clamp(Allocated / Demand, 0, 1);
}
public sealed record PowerNetwork(int Supply, int Demand, IReadOnlyList<CityPower> Cities)
{
    public double Unused => Math.Max(0, Supply - Cities.Sum(c => c.Allocated));
}

public static class PowerGrid
{
    public static string PriorityName(PowerPriority priority) => priority switch
    { PowerPriority.Critical => "Pierwszeństwo", PowerPriority.Normal => "Zwykły", _ => "Ograniczony" };
    public static bool Controllable(string id) => EpochCatalog.Buildings.Any(b => b.Id == id && (b.Energy > 0 || b.Demand > 0));
    public static bool Active(City city, string id) => city.Buildings.Contains(id) && !city.SuspendedBuildings.Contains(id);
    public static int RestartCost(string id) => (int)Math.Ceiling(Content.Project(id).Cost * .05);
    public static string? SiteError(GameState s, City city, string id)
    {
        if (id is not ("hydro_station" or "geothermal_station")) return null;
        var local = World.Neighbors(s, city.Tile).Append(s.Tiles[city.Tile]).Where(t => t.Owner == city.Owner).ToArray();
        if (id == "hydro_station" && !local.Any(t => t.River)) return "Elektrownia wodna wymaga rzeki na polu miasta lub sąsiednim własnym polu.";
        if (id == "geothermal_station" && local.Count(t => t.Terrain is Terrain.Hills or Terrain.Mountain) < 2) return "Elektrownia geotermalna wymaga dwóch własnych pól wzgórz lub gór przy mieście (licząc pole miasta).";
        return null;
    }
    public static string SiteDescription(string id) => id switch
    {
        "hydro_station" => "Wymaga własnej rzeki przy mieście. Stała moc, bez emisji.",
        "wind_farm" => "Moc: latem ×0,8, zimą ×1,2; własne wzgórze przy mieście daje ×1,2. Pozostałe sezony ×1.",
        "solar_farm" => "Moc zimą ×0,65; pustynne pole miasta daje ×1,25. Pozostałe sezony ×1.",
        "geothermal_station" => "Dwa własne pola wzgórz lub gór przy mieście. Stała moc, bez emisji.",
        _ => ""
    };
    public static int Generation(GameState s, City city, Development building)
    {
        if (!Active(city, building.Id)) return 0;
        double factor = 1;
        if (building.Id == "wind_farm")
        {
            factor = (s.Turn % 4) switch { 2 => .8, 0 => 1.2, _ => 1 };
            if (World.Neighbors(s, city.Tile).Append(s.Tiles[city.Tile]).Any(t => t.Owner == city.Owner && t.Terrain == Terrain.Hills)) factor *= 1.2;
        }
        if (building.Id == "solar_farm") factor = (s.Turn % 4 == 0 ? .65 : 1) * (s.Tiles[city.Tile].Terrain == Terrain.Desert ? 1.25 : 1);
        return (int)Math.Floor(building.Energy * factor + 1e-9);
    }
    public static PowerNetwork Plan(GameState s, City city)
    {
        if (s.PowerPhase?.TryGetValue(city.Id, out var frozen) == true) return frozen;
        if (!s.EpochCampaign) return new(0, 0, [new(city.Id, 0, 0, false)]);
        var roads = Logistics.For(s, city.Owner);
        bool isolated = roads.Interdicted.Contains(city.Tile);
        int group = roads.MarketGroups.GetValueOrDefault(city.Id, city.Id);
        var members = isolated ? [city] : s.Cities.Where(c => c.Owner == city.Owner && !roads.Interdicted.Contains(c.Tile) && roads.MarketGroups.GetValueOrDefault(c.Id, c.Id) == group).OrderBy(c => c.Id).ToArray();
        int supply = isolated ? 0 : members.Sum(c => EpochCatalog.Buildings.Sum(b => Generation(s, c, b)) + EpochCouncils.Policy(s.Factions[c.Owner]).Energy);
        supply = Math.Max(0, supply);
        var needs = members.ToDictionary(c => c.Id, c => EpochCatalog.Buildings.Where(b => Active(c, b.Id)).Sum(b => b.Demand));
        double available = supply;
        var rows = new Dictionary<int, CityPower>();
        foreach (var tier in members.GroupBy(c => c.PowerPriority).OrderByDescending(g => g.Key))
        {
            int demand = tier.Sum(c => needs[c.Id]);
            double ratio = demand == 0 ? 1 : Math.Min(1, available / demand);
            foreach (var c in tier) rows[c.Id] = new(c.Id, needs[c.Id], needs[c.Id] * ratio, isolated);
            available = Math.Max(0, available - demand * ratio);
        }
        return new(supply, needs.Values.Sum(), members.Select(c => rows[c.Id]).ToArray());
    }
    internal sealed class EconomicPhase : IDisposable
    {
        private readonly GameState state;
        public double? Emissions { get; }
        public IReadOnlyList<EnergySample> Readings { get; } = [];
        public EconomicPhase(GameState s)
        {
            state = s;
            if (!s.EpochCampaign) return;
            s.PowerPhase = s.Cities.ToDictionary(c => c.Id, c => Plan(s, c));
            Readings = s.Cities.Select(c =>
            {
                var row = s.PowerPhase[c.Id].Cities.First(x => x.CityId == c.Id);
                return new EnergySample(c.Id, c.Owner, new() { PhaseTurn = s.Turn, Demand = row.Demand, Allocated = row.Allocated,
                    Services = EpochCatalog.Buildings.Where(b => b.Demand > 0 && Active(c, b.Id)).Select(b => b.Id).OrderBy(id => id, StringComparer.Ordinal).ToList() });
            }).ToArray();
            Emissions = s.Factions.Sum(f => Epochs.Emissions(s, f.Id));
        }
        public void Dispose() => state.PowerPhase = null;
    }
    public static string? PriorityError(GameState s, int cityId, PowerPriority priority, int owner = 0)
    {
        var city = s.Cities.FirstOrDefault(c => c.Id == cityId && c.Owner == owner);
        if (s.Ending != null || !s.EpochCampaign || city == null || !Enum.IsDefined(priority)) return "Nieprawidłowe miasto lub tryb kampanii.";
        if (city.PowerPriority == priority) return "Miasto już ma ten priorytet.";
        if (city.LastPowerPriorityTurn == s.Turn) return "Priorytet miasta można zmienić raz na turę.";
        return null;
    }
    public static string? OperationError(GameState s, int cityId, string id, bool suspend, int owner = 0)
    {
        var city = s.Cities.FirstOrDefault(c => c.Id == cityId && c.Owner == owner);
        if (s.Ending != null || !s.EpochCampaign || city == null || !city.Buildings.Contains(id) || !Controllable(id)) return "Ten zakład nie podlega zarządzaniu energetycznemu.";
        if (city.SuspendedBuildings.Contains(id) == suspend) return "Zakład już ma wybrany stan.";
        if (city.LastOperationTurns.GetValueOrDefault(id, -1) == s.Turn) return "Stan tego zakładu można zmienić raz na turę.";
        if (!suspend && s.Factions[owner].Gold < RestartCost(id)) return "Brak złota na ponowny rozruch zakładu.";
        return null;
    }
    internal static void ManageAi(GameState s, Faction faction, City city)
    {
        if (!s.EpochCampaign) return;
        // Restart inherited plants before considering new construction. Never retire the only source.
        foreach (string id in city.SuspendedBuildings.OrderBy(id => id).ToArray())
            if ((!EpochCatalog.Buildings.Any(b => b.Id == id && b.Energy > 0) || Plan(s, city).Supply < Plan(s, city).Demand) &&
                faction.Gold > RestartCost(id) + 200 && OperationError(s, city.Id, id, false, faction.Id) == null) Engine.SetBuildingOperation(s, city.Id, id, false, faction.Id);
        foreach (var b in EpochCatalog.Buildings.Where(b => b.Energy > 0 && b.Emissions > 0 && Active(city, b.Id)).OrderByDescending(b => b.Emissions))
        {
            var grid = Plan(s, city);
            if (grid.Supply - Generation(s, city, b) >= grid.Demand && OperationError(s, city.Id, b.Id, true, faction.Id) == null)
                Engine.SetBuildingOperation(s, city.Id, b.Id, true, faction.Id);
        }
    }
    internal static void UpdateAiPriority(GameState s, Faction faction, City city)
    {
        var priority = Epochs.Programs.Any(p => p.Id == city.Project) ? PowerPriority.Critical : PowerPriority.Normal;
        if (PriorityError(s, city.Id, priority, faction.Id) == null) Engine.SetPowerPriority(s, city.Id, priority, faction.Id);
    }
    internal static void Validate(GameState s)
    {
        foreach (var c in s.Cities)
            if (!Enum.IsDefined(c.PowerPriority) || c.LastPowerPriorityTurn < -1 || c.LastPowerPriorityTurn > s.Turn ||
                c.SuspendedBuildings == null || c.LastOperationTurns == null ||
                c.SuspendedBuildings.Any(id => !c.Buildings.Contains(id) || !Controllable(id)) ||
                c.LastOperationTurns.Any(x => !c.Buildings.Contains(x.Key) || !Controllable(x.Key) || x.Value < 1 || x.Value > s.Turn) ||
                !s.EpochCampaign && (c.PowerPriority != PowerPriority.Normal || c.LastPowerPriorityTurn != -1 || c.SuspendedBuildings.Count != 0 || c.LastOperationTurns.Count != 0))
                throw new InvalidDataException("Nieprawidłowe dyspozycje energetyczne miasta.");
    }
}
public static partial class Engine
{
    public static Result SetPowerPriority(GameState s, int cityId, PowerPriority priority, int owner = 0)
    {
        string? error = PowerGrid.PriorityError(s, cityId, priority, owner); if (error != null) return Result.Fail(error);
        var city = s.Cities.First(c => c.Id == cityId); city.PowerPriority = priority; city.LastPowerPriorityTurn = s.Turn;
        if (owner == 0) s.Log($"{city.Name}: priorytet zasilania — {PowerGrid.PriorityName(priority)}. Zmienia podział energii całej sieci.", "energia");
        return Result.Ok("Zmieniono priorytet zasilania miasta.");
    }
    public static Result SetBuildingOperation(GameState s, int cityId, string id, bool suspend, int owner = 0)
    {
        string? error = PowerGrid.OperationError(s, cityId, id, suspend, owner); if (error != null) return Result.Fail(error);
        var city = s.Cities.First(c => c.Id == cityId);
        if (suspend) city.SuspendedBuildings.Add(id);
        else { s.Factions[owner].Gold -= PowerGrid.RestartCost(id); city.SuspendedBuildings.Remove(id); }
        city.LastOperationTurns[id] = s.Turn; s.InvalidateLogistics();
        if (owner == 0) s.Log($"{city.Name}: {Content.Project(id).Name} — {(suspend ? "wstrzymano działanie; pozostaje 25% utrzymania" : "ponownie uruchomiono za " + PowerGrid.RestartCost(id) + " złota")}.", "energia");
        return Result.Ok(suspend ? "Wyłączono zakład. Zachowano budynek i miejsca mieszkalne; pozostałe efekty ustają." : "Opłacono rozruch i przywrócono działanie zakładu.");
    }
}
