namespace Crown.Core;

public static class ProvincialEvents
{
    public sealed record LocalEffect(int Food = 0, int Unrest = 0, int Prosperity = 0, string? Charter = null, int Loyalty = 0, Governance? Government = null);
    public static readonly NarrativeEvent[] Events =
    [
        new("local_autonomy", "Rada starszych", "Delegacja prowincji", "Czyje prawa, czyja ziemia?",
            "W mieście {city} mieszkańcy uznają waszą pieczęć, lecz nie czują się częścią wspólnego państwa. Proszą o prawo wyboru miejscowej rady. Dowódca chce powierzyć mu zarząd, a urzędnicy proponują wspólną administrację. Każda droga oznacza inny podział dochodów i władzy.",
            [new("Uznajemy samorząd i wasze miejscowe prawa.", "Autonomia prowincji.", -60, 2, 6, -6),
             new("Porządku dopilnuje zarząd wojskowy.", "Lojalność zależy od obecności garnizonu.", 0, -2, -5, 6),
             new("Sfinansujemy wspólną administrację.", "Pełne wpływy, dalsza integracja wymaga dobrych warunków.", -40, 2, 0, 0, 4)]),
        new("local_grain", "Olen Rusk", "Przedstawiciel prowincji", "Puste spichlerze",
            "Tutejsze rodziny sprzedają narzędzia, żeby kupić chleb. Targ działa, lecz żywności jest za mało. Mieszkańcy pytają, czy wspólne państwo potrafi pomóc im przed kolejną zimą.",
            [new("Kupimy żywność dla zagrożonych rodzin.", "Pomoc dla wskazanego miasta.", -60, 2, 4),
             new("Wprowadźcie przydziały i otwórzcie zapasy domowe.", "Racjonowanie obciąży mieszkańców.", 0, -2, -5),
             new("Rody przekażą zboże i zaległe daniny.", "Konflikt z miejscowymi rodami.", 20, 0, 2, -14)]),
        new("local_housing", "Ada z Brzegów", "Delegatka mieszkańców", "Miasto za ciasnymi murami",
            "Po kilka rodzin dzieli jedną izbę. Cechy chcą stawiać nowe domy w zamian za stałe opłaty. Mieszkańcy proszą o ziemię i prawo, którego nikt nie odbierze po wyjeździe posłów.",
            [new("Ustanowimy kartę mieszkaniową i wykupimy grunt.", "Trwałe miejsca mieszkalne.", -90, 2, 6, -3),
             new("Cechy zbudują domy i pobiorą opłatę.", "Mieszkania finansowane przez cechy.", -25, 1, 0, 5),
             new("Budownictwo musi poczekać na lepsze czasy.", "Koszt społeczny zaniechania.", 0, -2, -8, 6)]),
        new("local_settlement", "Mira Weles", "Kanclerka Marchii", "Nowi sąsiedzi",
            "Przybyły tu rodziny z innych prowincji. Przywiozły umiejętności i własne zwyczaje. Miejscowi obawiają się konkurencji. O tym, czy nowi mieszkańcy będą partnerami czy dłużnikami, zdecydujemy teraz.",
            [new("Otwórzmy wspólne szkoły i zgromadzenia.", "Integracja i kultura.", -75, 2, 8, 0, 5),
             new("Cechy przyjmą ich do pracy za odpracowanie długu.", "Produkcja kosztem dobrobytu.", 40, 0, -6, 8),
             new("Niech obie wspólnoty wybiorą lokalną radę.", "Samorząd uspokaja spory.", -30, 3, 5, -5)])
    ];
    private static readonly Dictionary<string, LocalEffect[]> Consequences = new()
    {
        ["local_autonomy"] = [new(0, -8, 0, null, 10, Governance.Autonomous), new(0, 5, 0, null, -4, Governance.Military), new(0, -4, 2, null, 4, Governance.Integrated)],
        ["local_grain"] = [new(40, -12, 4), new(18, 6, -3), new(25, 3, 0)],
        ["local_housing"] = [new(0, -8, 4, "housing_rights"), new(0, -4, 2, "guild_housing"), new(0, 12, -5)],
        ["local_settlement"] = [new(0, -8, 6, "integration"), new(0, 8, -8, "bonded_labor"), new(0, -10, 3, "local_assembly")]
    };
    public static bool IsLocal(string id) => Consequences.ContainsKey(id);

    public static string CharterName(string charter) => charter switch
    {
        "housing_rights" => "Karta mieszkaniowa · +2 miejsca mieszkalne",
        "guild_housing" => "Domy cechowe · +2 miejsca, -5% miejscowego złota",
        "integration" => "Wspólne szkoły · +1 kultury, przybysze nie zwiększają niepokojów",
        "bonded_labor" => "Praca za dług · +10% produkcji, cel dobrobytu -8",
        "local_assembly" => "Rada wspólnot · niepokoje -2/turę, -5% miejscowego złota",
        _ => charter
    };

    public static string Describe(string id, int index)
    {
        var e = Consequences[id][index]; var parts = new List<string>();
        if (e.Food != 0) parts.Add($"Zapasy {e.Food:+0;-0}");
        if (e.Unrest != 0) parts.Add($"Niepokoje {e.Unrest:+0;-0}");
        if (e.Prosperity != 0) parts.Add($"Dobrobyt {e.Prosperity:+0;-0}");
        if (e.Loyalty != 0) parts.Add($"Lojalność {e.Loyalty:+0;-0}");
        if (e.Government is Governance mode) parts.Add(ProvincialPolitics.Name(mode) + ": " + ProvincialPolitics.Description(mode) + " Zmiana przez 8 tur zablokowana.");
        if (e.Charter != null) parts.Add("Trwale: " + CharterName(e.Charter));
        return "W tej prowincji: " + string.Join(" · ", parts);
    }

    public static NarrativeEvent Current(GameState s)
    {
        if(s.PendingEvent==TradeCouncil.Event.Id)return TradeCouncil.Current(s);
        var e = Narrative.Get(s.PendingEvent!);
        if (!IsLocal(e.Id) || s.PendingCityId is not int id) return e;
        var city = s.Cities.FirstOrDefault(c => c.Id == id);
        return city == null ? e : e with { Title = e.Title + " — " + city.Name, Body = e.Body.Replace("{city}", city.Name) };
    }

    public static bool Schedule(GameState s)
    {
        var food = Regions.PlanFood(s, 0);
        foreach (var c in s.Cities.Where(c => c.Owner == 0 && s.Turn - c.LastCouncilTurn >= 15)
            .OrderByDescending(c => c.HungerTurns * 10 + c.Unrest).ThenBy(c => c.Id))
        {
            string? id = !c.ResolvedIssues.Contains("local_autonomy") && c.Loyalty < 50 && s.Turn-c.LastGovernanceTurn>=8 ? "local_autonomy" :
                !c.ResolvedIssues.Contains("local_grain") && (c.HungerTurns > 0 || food.Balance(c.Id) < 0 && c.Food < 8) ? "local_grain" :
                !c.ResolvedIssues.Contains("local_housing") && c.Population > Regions.Housing(s, c) ? "local_housing" :
                !c.ResolvedIssues.Contains("local_settlement") && c.MigrantsReceived > 0 ? "local_settlement" : null;
            if (id == null) continue;
            s.PendingEvent = id; s.PendingCityId = c.Id;
            return true;
        }
        return false;
    }

    public static void Apply(GameState s, City city, string id, int index)
    {
        var effect = Consequences[id][index];
        city.Food += effect.Food;
        if (effect.Food > 0) city.HungerTurns = 0;
        city.Unrest = Math.Clamp(city.Unrest + effect.Unrest, 0, 100);
        city.Prosperity = Math.Clamp(city.Prosperity + effect.Prosperity, 0, 100);
        city.Loyalty = Math.Clamp(city.Loyalty + effect.Loyalty, 0, 100);
        if(city.Loyalty>=20)city.RevoltTurns=0;
        if(effect.Government is Governance mode){city.Governance=mode;city.LastGovernanceTurn=s.Turn;}
        if (effect.Charter != null) city.Charters.Add(effect.Charter);
        city.ResolvedIssues.Add(id); city.LastCouncilTurn = s.Turn;
    }
}
