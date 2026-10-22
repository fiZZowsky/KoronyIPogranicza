namespace Crown.Core;

public static class ProvincialPolitics
{
    public const int GovernancePrice = 40;
    public static string Name(Governance mode) => mode switch
    { Governance.Autonomous => "Autonomia", Governance.Military => "Zarząd wojskowy", _ => "Wspólna administracja" };
    public static string Description(Governance mode) => mode switch
    {
        Governance.Autonomous => "Cel lojalności +18; złoto ×0,70, kultura ×1,20.",
        Governance.Military => "Cel lojalności +10 z garnizonem, -12 bez niego; produkcja ×0,90, nauka ×0,80.",
        _ => "Pełne wpływy i produkcja; bez dodatkowego modyfikatora lojalności."
    };

    public static LoyaltyReport Report(GameState s, City c)
    {
        var f = s.Factions[c.Owner];
        bool garrison = s.Units.Any(u => u.Owner == c.Owner && u.Tile == c.Tile && u.Kind is not (UnitKind.Scout or UnitKind.Settler));
        bool connected = Logistics.For(s, c.Owner).ConnectedCities.Contains(c.Id);
        var factors = new List<string>(); int target = 55;
        void Add(string label, int value) { target += value; if (value != 0) factors.Add($"{label} {value:+0;-0}"); }
        Add("Stabilność państwa", (f.Stability - 50) / 3);
        Add("Dobrobyt", (c.Prosperity - 50) / 4);
        Add("Niepokoje", -c.Unrest / 4);
        Add("Podatki", f.Tax == 0 ? 8 : f.Tax == 2 ? -12 : 0);
        Add("Połączenie ze stolicą", connected ? 5 : -8);
        Add("Garnizon", garrison ? 6 : 0);
        Add("Zgromadzenie wspólnot", c.Charters.Contains("local_assembly") ? 6 : 0);
        Add("Wspólne szkoły", c.Charters.Contains("integration") ? 4 : 0);
        Add("Praca za dług", c.Charters.Contains("bonded_labor") ? -8 : 0);
        Add("Obca administracja", c.Owner != c.OriginalOwner ? -10 : 8);
        Add("Niedawny podbój", c.OccupiedUntil > s.Turn ? -20 : 0);
        Add("Ustrój prowincji", c.Governance == Governance.Autonomous ? 18 : c.Governance == Governance.Military ? garrison ? 10 : -12 : 0);
        target = Math.Clamp(target, 5, 95);
        int change = Math.Clamp(target - c.Loyalty, -3, 3);
        return new(target, change, garrison, factors);
    }

    public static void Tick(GameState s)
    {
        // Reports are computed before any transfer so city order cannot change the political result.
        var reports = s.Cities.ToDictionary(c => c.Id, c => Report(s, c));
        foreach (var c in s.Cities.OrderBy(c => c.Id).ToArray())
        {
            c.Loyalty = Math.Clamp(c.Loyalty + reports[c.Id].Change, 0, 100);
            if (c.Owner != c.OriginalOwner && c.Loyalty < 20)
            {
                c.RevoltTurns++;
                s.Log($"{c.Name}: bunt narasta ({c.RevoltTurns}/3). Lojalność {c.Loyalty}%. Zmień ustrój, podatki lub warunki życia.", "prowincja");
                if (c.RevoltTurns >= 3)
                {
                    int old = c.Owner; Transfer(s, c, c.OriginalOwner);
                    DiplomaticCouncil.AddGrievance(s, old, c.Owner, 30);
                    s.Log($"{c.Name} odrzuca administrację {s.Factions[old].Name} i wraca do {s.Factions[c.Owner].Name}. Wojsko wycofuje się do własnych miast.", "prowincja");
                }
            }
            else c.RevoltTurns = 0;
            if (c.Owner > 0 && c.Loyalty < 40 && c.Governance != Governance.Autonomous && s.Factions[c.Owner].Gold >= GovernancePrice && s.Turn - c.LastGovernanceTurn >= 8)
            { s.Factions[c.Owner].Gold -= GovernancePrice; c.Governance = Governance.Autonomous; c.LastGovernanceTurn = s.Turn; }
        }
    }

    // All changes of sovereignty share border, occupation and displaced-unit handling.
    public static void Transfer(GameState s, City city, int owner, bool peaceful=false)
    {
        int old = city.Owner;
        city.Owner = owner; if(!peaceful)city.Health = 50; city.Unrest = owner == city.OriginalOwner ? 20 : 55;
        city.Loyalty = owner == city.OriginalOwner ? 65 : 30;
        city.OccupiedUntil = owner == city.OriginalOwner ? 0 : s.Turn + 12;
        city.Governance = Governance.Integrated; city.RevoltTurns = 0; city.LastGovernanceTurn = -10;
        if(!peaceful){city.Production = 0; city.Project = "spearmen";} city.LastFoodImported = city.LastFoodExported = 0;
        foreach (var tile in s.Tiles.Where(t => t.Owner == old && World.Distance(t, s.Tiles[city.Tile]) <= 2 &&
            !s.Cities.Any(c => c.Owner == old && World.Distance(t, s.Tiles[c.Tile]) <= 2)))
        { tile.Owner = owner; if (tile.Road) tile.RoadOwner = owner; }
        s.Tiles[city.Tile].Owner = owner;
        if (s.Tiles[city.Tile].Road) s.Tiles[city.Tile].RoadOwner = owner;
        foreach (var unit in s.Units.Where(u => u.Owner == old && u.Tile == city.Tile).ToArray())
        {
            var refuge = s.Cities.Where(c => c.Owner == old && !s.Units.Any(u => u.Owner != old && u.Tile == c.Tile))
                .OrderBy(c => World.Distance(s.Tiles[unit.Tile], s.Tiles[c.Tile])).ThenBy(c => c.Id).FirstOrDefault();
            if (refuge == null) s.Units.Remove(unit);
            else { unit.Tile = refuge.Tile; unit.Destination = null; unit.Actions = 0; }
        }
        if (s.PendingCityId == city.Id && owner != 0) { s.PendingCityId = null; s.PendingEvent = null; }
        s.InvalidateLogistics();
    }
}

public static partial class Engine
{
    public static Result SetGovernance(GameState s, int cityId, Governance mode)
    {
        var c = s.Cities.FirstOrDefault(c => c.Id == cityId && c.Owner == 0);
        if (s.Ending != null || c == null || !Enum.IsDefined(mode)) return Result.Fail("Nieprawidłowa prowincja lub ustrój.");
        if (c.Governance == mode) return Result.Fail("Ten ustrój już obowiązuje.");
        if (s.Turn - c.LastGovernanceTurn < 8) return Result.Fail("Kolejna zmiana ustroju będzie możliwa osiem tur po poprzedniej.");
        if (s.Player.Gold < ProvincialPolitics.GovernancePrice) return Result.Fail("Zmiana ustroju kosztuje 40 złota.");
        s.Player.Gold -= ProvincialPolitics.GovernancePrice; c.Governance = mode; c.LastGovernanceTurn = s.Turn;
        s.Log($"{c.Name}: {ProvincialPolitics.Name(mode)}. {ProvincialPolitics.Description(mode)}", "decyzja");
        return Result.Ok("Zmiana ustroju wpływa na cel lojalności; lojalność zmienia się o maksymalnie 3 punkty na turę.");
    }
}
