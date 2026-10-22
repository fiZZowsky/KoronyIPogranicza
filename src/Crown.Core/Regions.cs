namespace Crown.Core;

/// <summary>Local needs and exchange use the same safe road components as military logistics.</summary>
public static class Regions
{
    public static readonly string[] CharterIds = ["housing_rights", "guild_housing", "integration", "bonded_labor", "local_assembly"];
    public const int FocusPrice = 15;
    public const int AidPrice = 50;

    public static string FocusName(CityFocus focus) => focus switch
    {
        CityFocus.Agriculture => "Rolnictwo", CityFocus.Industry => "Rzemiosło",
        CityFocus.Scholarship => "Nauka", _ => "Równowaga"
    };
    public static int Housing(GameState s, City c) => 6 + (c.Buildings.Contains("housing") ? 5 : 0) +
        (c.Buildings.Contains("aqueduct") ? 2 : 0) + (c.Buildings.Contains("parliament") ? 3 : 0) +
        (s.Factions[c.Owner].Technologies.Contains("civilservice") ? 2 : 0) +
        (c.Charters.Contains("housing_rights") || c.Charters.Contains("guild_housing") ? 2 : 0);

    public static int Sanitation(GameState s, City c) => 6 + (s.Tiles[c.Tile].River ? 1 : 0) +
        (c.Buildings.Contains("aqueduct") ? 4 : 0) + (c.Buildings.Contains("infirmary") ? 6 : 0) +
        (s.Factions[c.Owner].Technologies.Contains("medicine") ? 3 : 0) - (c.Focus == CityFocus.Industry ? 1 : 0);

    public static IReadOnlyList<string> Goods(GameState s, City city)
    {
        var network = Logistics.For(s, city.Owner);
        int group = network.MarketGroups[city.Id];
        var members = s.Cities.Where(c => c.Owner == city.Owner && network.MarketGroups[c.Id] == group).ToArray();
        var resources = s.Tiles.Where(t => t.Owner == city.Owner && t.Resource is Resource.Salt or Resource.Dyes &&
            members.Any(c => World.Distance(t, s.Tiles[c.Tile]) <= 2)).Select(t => t.Resource).Distinct().OrderBy(r => r);
        var goods = resources.Select(Content.ResourceName).ToList();
        if (city.Buildings.Contains("market") && network.ConnectedCities.Contains(city.Id) &&
            s.Relations.Any(r => (r.A == city.Owner || r.B == city.Owner) && r.Trade && !r.War)) goods.Add("Towary zagraniczne");
        return goods;
    }

    public static FoodPlan PlanFood(GameState s, int owner)
    {
        var cities = s.Cities.Where(c => c.Owner == owner).OrderBy(c => c.Id).ToArray();
        var raw = cities.ToDictionary(c => c.Id, c => Engine.CityYields(s, c).Food);
        var imports = cities.ToDictionary(c => c.Id, _ => 0d);
        var exports = cities.ToDictionary(c => c.Id, _ => 0d);
        var shipments = new List<FoodShipment>();
        var groups = Logistics.For(s, owner).MarketGroups;
        foreach (var group in cities.GroupBy(c => groups[c.Id]))
        {
            foreach (var recipient in group.Where(c => raw[c.Id] < 0).OrderBy(c => c.Food + raw[c.Id]).ThenBy(c => c.Id))
            {
                double need = Math.Min(6, -raw[recipient.Id]);
                foreach (var donor in group.Where(c => raw[c.Id] > 0).OrderBy(c => c.Id))
                {
                    double available = Math.Max(0, Math.Min(6, raw[donor.Id]) - exports[donor.Id]);
                    double amount = Math.Min(need, available);
                    if (amount <= 0) continue;
                    exports[donor.Id] += amount; imports[recipient.Id] += amount;
                    shipments.Add(new(donor.Id, recipient.Id, amount));
                    need -= amount;
                    if (need < .00001) break;
                }
            }
        }
        return new(raw, imports, exports, shipments);
    }

    public static ProvinceStatus Status(GameState s, City c, FoodPlan? food = null)
    {
        food ??= PlanFood(s, c.Owner);
        int housing = Housing(s, c), sanitation = Sanitation(s, c);
        var goods = Goods(s, c);
        int pressure = Math.Max(0, c.Population - housing) * 3 + Math.Max(0, c.Population - sanitation) * 2;
        if (food.Balance(c.Id) < 0 && c.Food < -food.Balance(c.Id) + 8) pressure += 5;
        int target = 64 + Math.Min(3, goods.Count) * 6 - pressure * 3 - c.Unrest / 4 +
            (s.Factions[c.Owner].Tax == 0 ? 5 : s.Factions[c.Owner].Tax == 2 ? -7 : 0) -
            (c.Charters.Contains("bonded_labor") ? 8 : 0);
        return new(housing, sanitation, goods, food.Balance(c.Id), food.Imports[c.Id], food.Exports[c.Id],
            Math.Clamp(target, 5, 95), pressure);
    }

    public static void Tick(GameState s, int owner)
    {
        var food = PlanFood(s, owner);
        foreach (var c in s.Cities.Where(c => c.Owner == owner))
        {
            var status = Status(s, c, food);
            c.Prosperity += Math.Sign(status.ProsperityTarget - c.Prosperity) * Math.Min(2, Math.Abs(status.ProsperityTarget - c.Prosperity));
            int calm = 2 + (c.Buildings.Contains("court") ? 3 : 0) + (c.Charters.Contains("local_assembly") ? 2 : 0);
            c.Unrest = Math.Clamp(c.Unrest + status.Pressure - calm, 0, 100);
        }
        Migrate(s, owner);
    }

    public static void Migrate(GameState s, int owner)
    {
        if (s.Turn % 4 != 0) return;
        var cities = s.Cities.Where(c => c.Owner == owner).OrderBy(c => c.Id).ToArray();
        var food = PlanFood(s, owner);
        var network = Logistics.For(s, owner).MarketGroups;
        var statuses = cities.ToDictionary(c => c.Id, c => Status(s, c, food));
        foreach (var source in cities.Where(c => c.Population >= 3 && s.Turn - c.LastMigrationTurn >= 8 &&
            (statuses[c.Id].Pressure > 0 || c.Unrest >= 35 || c.Prosperity < 45)).OrderByDescending(c => statuses[c.Id].Pressure + c.Unrest))
        {
            var target = cities.Where(c => c.Id != source.Id && network[c.Id] == network[source.Id] &&
                s.Turn - c.LastMigrationTurn >= 8 && c.Population < statuses[c.Id].Housing && c.Population < statuses[c.Id].Sanitation &&
                statuses[c.Id].FoodBalance >= 2 && c.Prosperity >= source.Prosperity + 10)
                .OrderByDescending(c => c.Prosperity).ThenBy(c => c.Id).FirstOrDefault();
            if (target == null) continue;
            source.Population--; target.Population++;
            source.LastMigrationTurn = target.LastMigrationTurn = s.Turn;
            source.MigrantsDeparted++; target.MigrantsReceived++;
            source.Unrest = Math.Max(0, source.Unrest - 3);
            if (!target.Charters.Contains("integration")) target.Unrest = Math.Min(100, target.Unrest + 4);
            s.Migrations.Add(new(s.Turn, owner, source.Id, target.Id, 1));
            if (owner == 0) s.Log($"Rodziny przeniosły się z {source.Name} do {target.Name}: 1 mieszkaniec. Szukają żywności, dachu i bezpiecznej przyszłości.", "migracja");
            break; // At most one movement per faction per season cycle; population is conserved.
        }
    }

    public static void ManageAi(GameState s, City c)
    {
        var status = Status(s, c); var f = s.Factions[c.Owner];
        if (status.FoodBalance < 0 && c.Focus != CityFocus.Agriculture && f.Gold > 100)
            Engine.SetCityFocus(s, c.Id, CityFocus.Agriculture, c.Owner);
        if (status.FoodBalance < 0 && c.Food < 8 && f.Gold > AidPrice + 90) Engine.GrantCityAid(s, c.Id, c.Owner);
        string? project = c.Population >= status.Housing ? "housing" : c.Population >= status.Sanitation ?
            c.Buildings.Contains("aqueduct") ? "infirmary" : "aqueduct" : null;
        if (project != null && c.Project != "settler" && c.Project != project && Engine.AvailableProjects(s, c).Any(p => p.Id == project))
        { c.Project = project; c.Production *= .8; }
    }
}

public static partial class Engine
{
    public static Result SetCityFocus(GameState s, int cityId, CityFocus focus, int owner = 0)
    {
        var c = s.Cities.FirstOrDefault(c => c.Id == cityId && c.Owner == owner);
        if (s.Ending != null || c == null || !Enum.IsDefined(focus)) return Result.Fail("Nieprawidłowa prowincja lub kierunek rozwoju.");
        if (c.Focus == focus) return Result.Fail("Miasto realizuje już ten kierunek rozwoju.");
        if (c.LastFocusTurn == s.Turn) return Result.Fail("Miasto zmieniło już priorytet w tej turze.");
        if (s.Factions[owner].Gold < Regions.FocusPrice) return Result.Fail("Zmiana kierunku kosztuje 15 złota.");
        s.Factions[owner].Gold -= Regions.FocusPrice; c.Focus = focus; c.LastFocusTurn = s.Turn;
        if (owner == 0) s.Log($"{c.Name}: nowy kierunek rozwoju — {Regions.FocusName(focus)}.", "prowincje");
        return Result.Ok("Nowy kierunek rozwoju obowiązuje od razu.");
    }

    public static Result GrantCityAid(GameState s, int cityId, int owner = 0)
    {
        var c = s.Cities.FirstOrDefault(c => c.Id == cityId && c.Owner == owner);
        if (s.Ending != null || c == null) return Result.Fail("Nieprawidłowa prowincja.");
        if (s.Turn - c.LastAidTurn < 5) return Result.Fail("Kolejna pomoc będzie dostępna pięć tur po poprzedniej.");
        if (s.Factions[owner].Gold < Regions.AidPrice) return Result.Fail("Pomoc kosztuje 50 złota.");
        s.Factions[owner].Gold -= Regions.AidPrice; c.LastAidTurn = s.Turn; c.Food += 25;
        c.Unrest = Math.Max(0, c.Unrest - 8); c.Prosperity = Math.Min(100, c.Prosperity + 4);
        if (owner == 0) s.Log($"{c.Name}: sfinansowano pomoc — +25 zapasów żywności, -8 niepokojów i +4 dobrobytu.", "prowincje");
        return Result.Ok("Dostarczono pomoc do prowincji. Skarb wydał 50 złota.");
    }
}
