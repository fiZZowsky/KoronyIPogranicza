namespace Crown.Core;

public sealed record UnitModel(string Id, string Name, int Era, string Technology, UnitKind Role, int Strength, int Moves, int Range, int Cost);
public static class UnitModels
{
    public static readonly UnitModel[] All =
    [
        new("hunters", "Drużyna łowców", 0, "hunting_bands", UnitKind.Spearmen, 12, 2, 1, 32),
        new("tribal_scout", "Tropiciele", 0, "stone_tools", UnitKind.Scout, 9, 3, 1, 28),
        new("village_guard", "Straż osady", 1, "animal_husbandry", UnitKind.Spearmen, 20, 2, 1, 55),
        new("bronze_guard", "Gwardia brązu", 2, "bronze", UnitKind.Spearmen, 27, 2, 1, 85),
        new("iron_infantry", "Piechota żelazna", 3, "iron", UnitKind.Spearmen, 36, 2, 1, 120),
        new("chariot_guard", "Jazda pogranicza", 3, "riding", UnitKind.Cavalry, 39, 3, 1, 145),
        new("legion", "Legion zawodowy", 4, "classical_institutions", UnitKind.Spearmen, 47, 2, 1, 180),
        new("siege_train", "Tabor oblężniczy", 4, "engineering", UnitKind.Catapult, 34, 2, 2, 200),
        new("men_at_arms", "Zbrojni", 5, "feudal_contracts", UnitKind.Spearmen, 58, 2, 1, 250),
        new("knights", "Rycerstwo", 5, "medieval_orders", UnitKind.Cavalry, 65, 3, 1, 300),
        new("musketeers", "Muszkieterzy", 6, "gunpowder", UnitKind.Spearmen, 75, 2, 1, 380),
        new("cannon", "Artyleria prochowa", 6, "gunpowder", UnitKind.Catapult, 62, 2, 2, 420),
        new("line_infantry", "Piechota liniowa", 7, "rights", UnitKind.Spearmen, 88, 2, 1, 500),
        new("field_guns", "Artyleria polowa", 7, "scientific_method", UnitKind.Catapult, 78, 2, 2, 550),
        new("riflemen", "Strzelcy", 8, "mass_production", UnitKind.Spearmen, 108, 2, 1, 680),
        new("howitzers", "Haubice", 8, "industrial_chemistry", UnitKind.Catapult, 96, 2, 3, 750),
        new("motor_infantry", "Piechota zmotoryzowana", 9, "combustion", UnitKind.Spearmen, 130, 3, 1, 900),
        new("tanks", "Batalion pancerny", 9, "mass_society", UnitKind.Cavalry, 150, 4, 1, 1100),
        new("mechanized_guard", "Piechota zmechanizowana", 10, "atomic_systems", UnitKind.Spearmen, 175, 3, 1, 1400),
        new("rocket_artillery", "Artyleria rakietowa", 10, "rocketry", UnitKind.Catapult, 150, 3, 3, 1600),
        new("network_infantry", "Piechota sieciocentryczna", 11, "internet", UnitKind.Spearmen, 205, 3, 1, 1900),
        new("recon_drones", "Drony rozpoznawcze", 11, "computing", UnitKind.Scout, 100, 4, 1, 1500),
        new("robot_guard", "Gwardia robotyczna", 12, "robotics", UnitKind.Spearmen, 240, 3, 1, 2400),
        new("autonomous_artillery", "Artyleria autonomiczna", 12, "machine_learning", UnitKind.Catapult, 220, 3, 3, 2700),
        new("planetary_guard", "Gwardia planetarna", 13, "planetary_cooperation", UnitKind.Spearmen, 280, 3, 1, 3300),
        new("electromagnetic_battery", "Bateria elektromagnetyczna", 13, "fusion", UnitKind.Catapult, 260, 3, 3, 3600),
        new("orbital_marines", "Piechota orbitalna", 14, "space_medicine", UnitKind.Spearmen, 330, 3, 1, 4200),
        new("orbital_rovers", "Łaziki wsparcia orbitalnego", 14, "orbital_assembly", UnitKind.Cavalry, 360, 4, 1, 4700),
        new("expedition_corps", "Korpus międzyplanetarny", 15, "mars_habitats", UnitKind.Spearmen, 390, 3, 1, 5400),
        new("fusion_armour", "Wozy napędu fuzyjnego", 15, "fusion_propulsion", UnitKind.Cavalry, 430, 4, 1, 6000),
        new("habitat_guard", "Straż habitatów Układu", 16, "synthetic_ecologies", UnitKind.Spearmen, 470, 3, 1, 7000),
        new("deep_space_battery", "Bateria dalekiego zasięgu", 16, "deep_space_relays", UnitKind.Catapult, 430, 3, 4, 7500),
        new("stellar_guard", "Straż cywilizacji rozproszonej", 17, "distributed_civilization", UnitKind.Spearmen, 550, 3, 1, 9000),
        new("stellar_rovers", "Autonomiczne wozy ekspedycyjne", 17, "interstellar_probes", UnitKind.Cavalry, 590, 5, 1, 10000)
    ];
    public static UnitModel? For(Unit u) => All.FirstOrDefault(m => m.Id == u.ModelId);
    public static string Name(Unit u) => For(u)?.Name ?? Content.UnitName(u.Kind);
    public static int Range(Unit u) => For(u)?.Range ?? (u.Kind is UnitKind.Archer or UnitKind.Catapult ? 2 : 1);
    public static double Upkeep(Unit u) => For(u) is UnitModel model ? 1.4 + model.Era * .6 : 1.4;
    public static bool RequiresHorses(string project) => project is "cavalry" or "chariot_guard" or "knights";
    public static double BaseStrength(Unit u) => For(u)?.Strength ?? u.Kind switch { UnitKind.Scout => 8, UnitKind.Spearmen => 19, UnitKind.Archer => 17, UnitKind.Cavalry => 25, UnitKind.Catapult => 14, _ => 2 };
    public static double EffectiveBaseStrength(GameState s,Unit u)
    {
        var f=s.Factions[u.Owner];
        return BaseStrength(u)+(u.ModelId==null?(f.Technologies.Contains("bronze")?3:0)+(f.Technologies.Contains("iron")?5+(Engine.HasResource(s,u.Owner,Resource.Iron)?3:0):0)+(f.Technologies.Contains("steel")?7:0):0);
    }
    public static IEnumerable<UnitModel> Upgrades(GameState s, Unit u) => All.Where(m => s.EpochCampaign && m.Role == u.Kind && m.Strength > EffectiveBaseStrength(s,u) && s.Factions[u.Owner].Technologies.Contains(m.Technology)).OrderBy(m => m.Era).ThenBy(m => m.Strength);
    public static int UpgradeCost(GameState s,Unit u, UnitModel m) => 60 + (int)(m.Strength - EffectiveBaseStrength(s,u)) * 3;
    public static string? UpgradeError(GameState s, Unit? u, string id, int owner = 0)
    {
        if (s.Ending != null || u == null || u.Owner != owner || !s.EpochCampaign) return "Wybierz własny oddział w kampanii epok.";
        var m = Upgrades(s, u).FirstOrDefault(m => m.Id == id); if (m == null) return "Brak dostępnej, silniejszej modernizacji tej roli.";
        if (!s.Cities.Any(c => c.Owner == owner && c.Tile == u.Tile) || !Engine.Supplied(s, u)) return "Modernizacja wymaga dostaw i pobytu we własnym mieście.";
        if (u.EscortCaravanId != null || u.Actions != u.MaxActions) return "Potrzebny pełny ruch i brak przydziału eskorty.";
        return s.Factions[owner].Gold < UpgradeCost(s,u, m) ? "Za mało złota na modernizację." : null;
    }
}
public static partial class Engine
{
    public static Result UpgradeUnit(GameState s, int id, string model, int owner = 0)
    {
        var u = s.Units.FirstOrDefault(u => u.Id == id); var error = UnitModels.UpgradeError(s, u, model, owner); if (error != null) return Result.Fail(error);
        var m = UnitModels.All.First(m => m.Id == model); int price = UnitModels.UpgradeCost(s,u!, m); s.Factions[owner].Gold -= price;
        MilitaryOrders.Clear(u!); u!.Destination = null; u.ModelId = model; u.Actions = 0; s.InvalidateLogistics();
        if (owner == 0) s.Log($"Modernizacja: {m.Name}, koszt {price} złota. Zdrowie i doświadczenie zachowane; poświęcono ruch.", "wojsko");
        return Result.Ok($"Oddział zmodernizowano: {m.Name}.");
    }
}
