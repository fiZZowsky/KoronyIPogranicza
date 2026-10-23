namespace Crown.Core;

public sealed record EnergyReport(int Supply, int Demand, double? CityCoverage = null)
{
    public double Coverage => CityCoverage ?? (Demand == 0 ? 1 : Math.Min(1, (double)Supply / Demand));
}
public sealed record SpaceProgram(string Id, string Name, int Era, string Technology, int Cost, string? Previous, string Facility, string Effect);
public static class Epochs
{
    public const int CampaignTurns = 1600;
    private static readonly int[] EraYears = [-40000,-10000,-3300,-1200,-500,500,1450,1650,1750,1900,1945,1980,2035,2080,2150,2250,2500,3000];
    public static int WorldYear(GameState s) => Math.Max(Year(s.Turn), s.Factions.Count == 0 ? -40000 : EraYears[s.Factions.Max(f => f.Era)]);
    public static readonly SpaceProgram[] Programs =
    [
        new("orbital_station", "Stała stacja orbitalna", 14, "orbital_industry", 6000, null, "orbital_shipyard", "Stolica: +40 nauki, +20 złota na turę; dostawy państwa +2."),
        new("lunar_base", "Samowystarczalna baza księżycowa", 15, "lunar_resources", 10000, "orbital_station", "lunar_exchange", "Stolica: +80 produkcji i +30 złota na turę."),
        new("mars_settlement", "Osadnictwo marsjańskie", 15, "interplanetary_logistics", 15000, "lunar_base", "exofarm", "Stolica: +60 żywności i +120 nauki na turę."),
        new("belt_settlement", "Wspólnota pasa asteroid", 16, "solar_commonwealth", 22000, "mars_settlement", "asteroid_forge", "Stolica: +180 produkcji i +100 złota na turę."),
        new("ark_launch", "Wysłanie statku pokoleniowego", 17, "interstellar_charter", 40000, "belt_settlement", "departure_yard", "Zwycięstwo naukowe; wymaga stabilności co najmniej 55 i pełnego pokrycia energii w mieście startu.")
    ];
    public static int Current(Faction f) => EpochCatalog.All.Where(e => e.Technologies.Any(f.Technologies.Contains)).Select(e => e.Index).DefaultIfEmpty(0).Max();
    public static string? ResearchError(GameState s, Faction f, Technology t)
    {
        if (f.Technologies.Contains(t.Id)) return "Technologia jest już odkryta.";
        if (!s.EpochCampaign && !Content.LegacyTechnologies.Contains(t.Id)) return "Rozszerz tę kampanię do pełnej historii epok.";
        if (!t.Requires.All(f.Technologies.Contains)) return "Wymagane: " + string.Join(", ", t.Requires.Where(id => !f.Technologies.Contains(id)).Select(id => Content.Tech(id).Name));
        int era = EpochCatalog.EraOf(t.Id);
        if (s.EpochCampaign && era > 0 && !f.Technologies.Contains(EpochCatalog.All[era - 1].Capstone)) return "Najpierw zamknij poprzednią epokę: " + Content.Tech(EpochCatalog.All[era - 1].Capstone).Name;
        return null;
    }
    public static IEnumerable<Technology> ResearchOptions(GameState s, Faction f) => Content.Technologies.Where(t => ResearchError(s, f, t) == null);
    public static string? ProjectError(GameState s, City c, Project p)
    {
        if (!s.EpochCampaign) return Content.LegacyProjects.Contains(p.Id) ? null : "Projekt wymaga rozszerzonej kampanii epok.";
        string? site = PowerGrid.SiteError(s, c, p.Id); if (site != null) return site;
        var program = Programs.FirstOrDefault(x => x.Id == p.Id);
        if (program == null) return null;
        var f = s.Factions[c.Owner];
        if (f.SpaceMilestones.Contains(p.Id)) return "Państwo ukończyło już ten program.";
        if (program.Previous != null && !f.SpaceMilestones.Contains(program.Previous)) return "Najpierw ukończ program: " + Programs.First(x => x.Id == program.Previous).Name;
        if (!c.Buildings.Contains(program.Facility)) return "Potrzebna infrastruktura w tym mieście: " + Content.Project(program.Facility).Name;
        if (!PowerGrid.Active(c, program.Facility)) return "Najpierw uruchom infrastrukturę programu: " + Content.Project(program.Facility).Name;
        if (Energy(s, c).Coverage < 1) return "Program wymaga pełnego pokrycia energii w sieci drogowej miasta.";
        if (p.Id == "ark_launch" && f.Stability < 55) return "Wysłanie statku pokoleniowego wymaga stabilności co najmniej 55.";
        return null;
    }
    public static EnergyReport Energy(GameState s, City c)
    {
        if (!s.EpochCampaign) return new(0, 0);
        var grid = PowerGrid.Plan(s, c); var allocation = grid.Cities.First(x => x.CityId == c.Id);
        return new(grid.Supply, grid.Demand, allocation.Coverage);
    }
    public static Yields Bonus(GameState s, City c)
    {
        if (!s.EpochCampaign) return new();
        var f = s.Factions[c.Owner]; double food = 0, prod = 0, gold = 0, science = 0, culture = 0;
        void Add(Yields y, double factor = 1) { food += y.Food * factor; prod += y.Production * factor; gold += y.Gold * factor; science += y.Science * factor; culture += y.Culture * factor; }
        foreach (var e in EpochCatalog.All) for (int n = 0; n < e.Technologies.Length; n++)
            if (!Content.LegacyTechnologies.Contains(e.Technologies[n]) && f.Technologies.Contains(e.Technologies[n])) Add(EpochCatalog.TechBonus(e.Index, n));
        double power = .25 + .75 * Energy(s, c).Coverage;
        if(c.Buildings.Contains("atlas"))Add(new(0,0,0,35,25));
        foreach (var b in EpochCatalog.Buildings.Where(b => PowerGrid.Active(c, b.Id))) Add(new(b.Food, b.Production, b.Gold, b.Science, b.Culture), b.Demand > 0 ? power : 1);
        if (c.Id == s.Cities.Where(x => x.Owner == c.Owner).Min(x => x.Id))
        {
            if (f.SpaceMilestones.Contains("orbital_station")) Add(new(0, 0, 20, 40, 0));
            if (f.SpaceMilestones.Contains("lunar_base")) Add(new(0, 80, 30, 0, 0));
            if (f.SpaceMilestones.Contains("mars_settlement")) Add(new(60, 0, 0, 120, 0));
            if (f.SpaceMilestones.Contains("belt_settlement")) Add(new(0, 180, 100, 0, 0));
        }
        return new(food, prod, gold, science, culture);
    }
    public static int Housing(GameState s, City c) => s.EpochCampaign ? EpochCatalog.Buildings.Where(b => c.Buildings.Contains(b.Id)).Sum(b => b.Housing) : 0;
    public static int Sanitation(GameState s, City c) => s.EpochCampaign ? EpochCatalog.Buildings.Where(b => PowerGrid.Active(c, b.Id)).Sum(b => b.Sanitation) : 0;
    public static int Defense(GameState s, City c) => s.EpochCampaign ? EpochCatalog.Buildings.Where(b => PowerGrid.Active(c, b.Id)).Sum(b => b.Defense) : 0;
    public static int Supply(GameState s, int owner) => !s.EpochCampaign ? 0 : s.Cities.Where(c => c.Owner == owner).Select(c => EpochCatalog.Buildings.Where(b => PowerGrid.Active(c, b.Id)).Sum(b => b.Supply)).DefaultIfEmpty(0).Max() + (s.Factions[owner].SpaceMilestones.Contains("orbital_station") ? 2 : 0);
    public static double Upkeep(GameState s, int owner) => !s.EpochCampaign ? 0 : s.Cities.Where(c => c.Owner == owner).Sum(c => EpochCatalog.Buildings.Where(b => c.Buildings.Contains(b.Id)).Sum(b => b.Upkeep * (c.SuspendedBuildings.Contains(b.Id) ? .25 : 1)));
    public static double Emissions(GameState s, int owner) => !s.EpochCampaign ? 0 : s.Cities.Where(c => c.Owner == owner).Sum(c => EpochCatalog.Buildings.Where(b => PowerGrid.Active(c, b.Id)).Sum(b => b.Emissions * (b.Demand > 0 ? .25 + .75 * Energy(s,c).Coverage : 1)) + EpochCouncils.Policy(s.Factions[owner]).Emissions);
    public static void Tick(GameState s, double? phaseEmissions = null)
    {
        if (!s.EpochCampaign) return;
        double before = s.ClimatePressure; s.ClimatePressure = Math.Clamp(before + (phaseEmissions ?? s.Factions.Sum(f => Emissions(s, f.Id))) / 200, 0, 100);
        if ((int)(before / 20) != (int)(s.ClimatePressure / 20)) s.Log($"Presja klimatyczna: {s.ClimatePressure:0.0}/100. Emisje wszystkich państw wpływają na plony całej planety.", "klimat");
    }
    public static int Year(int turn)
    {
        (int Turn, int Year)[] anchors = [(1, -40000), (81, -10000), (161, -3000), (281, 500), (441, 1500), (601, 1900), (741, 2025), (901, 2200), (1201, 2800), (1601, 5000)];
        for (int i = 1; i < anchors.Length; i++) if (turn <= anchors[i].Turn) return anchors[i - 1].Year + (int)Math.Round((double)(turn - anchors[i - 1].Turn) * (anchors[i].Year - anchors[i - 1].Year) / (anchors[i].Turn - anchors[i - 1].Turn));
        return 5000;
    }
    internal static void Complete(GameState s, Faction f, Project p)
    {
        f.SpaceMilestones.Add(p.Id); f.Prestige += 50;
        s.Log($"{f.Name}: ukończono {p.Name}. {Programs.First(x => x.Id == p.Id).Effect}", "kosmos");
    }
    internal static void ManageAi(GameState s, Faction f, City c)
    {
        if (!s.EpochCampaign) return;
        PowerGrid.ManageAi(s, f, c);
        var available = Engine.AvailableProjects(s, c).ToArray();
        bool war = s.Relations.Any(r => (r.A == f.Id || r.B == f.Id) && r.War);
        int army = s.Units.Count(u => u.Owner == f.Id && u.Kind is not (UnitKind.Settler or UnitKind.Scout));
        var bestUnit = available.Where(p => UnitModels.All.Any(m => m.Id == p.Id)).OrderByDescending(p => UnitModels.All.First(m => m.Id == p.Id).Strength).FirstOrDefault();
        var power = available.Where(p => EpochCatalog.Buildings.Any(b => b.Id == p.Id && b.Energy > 0)).OrderBy(p => EpochCatalog.Buildings.First(b => b.Id == p.Id).Emissions).ThenBy(p => p.Cost).FirstOrDefault();
        var program = available.FirstOrDefault(p => Programs.Any(x => x.Id == p.Id));
        var building = available.Where(p => p.Unit == null && p.Id is not ("treasury" or "festival") && !Programs.Any(x => x.Id == p.Id))
            .OrderByDescending(p => EpochCatalog.Buildings.FirstOrDefault(b => b.Id == p.Id)?.Science ?? (p.Id == "library" ? 8 : 0)).ThenBy(p => p.Cost).FirstOrDefault();
        if (power != null && Energy(s, c).Supply <= Energy(s, c).Demand) c.Project = power.Id;
        else if (bestUnit != null && army < (war ? 3 : 1) * s.Cities.Count(x => x.Owner == f.Id) + 2) c.Project = bestUnit.Id;
        else if (program != null) c.Project = program.Id;
        else if (building != null && c.Project != "settler") c.Project = building.Id;
        PowerGrid.UpdateAiPriority(s, f, c);
        foreach (var u in s.Units.Where(u => u.Owner == f.Id && u.Tile == c.Tile).ToArray())
        {
            var model = UnitModels.Upgrades(s, u).LastOrDefault(); if (model != null && UnitModels.UpgradeError(s, u, model.Id, f.Id) == null && f.Gold > UnitModels.UpgradeCost(s,u, model) + 200) Engine.UpgradeUnit(s, u.Id, model.Id, f.Id);
        }
    }
}
public static partial class Engine
{
    public static Result ExtendCampaign(GameState s)
    {
        if (s.EpochCampaign || !s.Cities.Any(c => c.Owner == 0)) return Result.Fail("Nie można teraz rozszerzyć tej kampanii.");
        s.EpochCampaign = true; s.MaxTurns = Epochs.CampaignTurns; s.ClimatePressure = 0;
        foreach (var f in s.Factions) { int era = Epochs.Current(f); foreach (var e in EpochCatalog.All.Take(era)) f.Technologies.Add(e.Capstone); }
        if (s.Ending != null) { s.Log(s.Ending, "historia"); s.Ending = null; }
        s.InvalidateLogistics(); s.Log("Kampania rozszerzona: 18 epok, 1600 tur. Uznano wcześniejsze fundamenty instytucji; posiadane odkrycia i budynki pozostają.", "epoki");
        return Result.Ok("Otworzono dalszą historię i możliwą przyszłość świata.");
    }
    public static Result ContinueHistory(GameState s)
    {
        if (!s.EpochCampaign || s.Ending == null || s.Turn > s.MaxTurns || !s.Cities.Any(c => c.Owner == 0)) return Result.Fail("Nie można kontynuować tej historii.");
        s.Log(s.Ending, "historia"); s.ContinueAfterVictory = true; s.Ending = null; return Result.Ok("Kontynuujesz rozwój świata po rozstrzygnięciu zwycięstwa, do limitu kampanii.");
    }
}
