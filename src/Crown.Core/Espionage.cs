namespace Crown.Core;

public static class Espionage
{
    public const int RecruitCost = 60, ProtectionCost = 60, ProtectionTurns = 8, CityInterval = 8;
    private static readonly string[] Names = ["Lena Wrona", "Marek Nurt", "Sura Kell", "Iwo Lis", "Ada Venn", "Nela Tarn"];
    public static int Rank(SpyAgent agent) => Math.Min(5, agent.Experience / 3);
    public static int Cost(SpyMission mission) => mission switch { SpyMission.Reconnaissance => 55, SpyMission.StealResearch => 110, _ => 95 };
    public static int Duration(SpyMission mission) => mission == SpyMission.Reconnaissance ? 3 : 4;
    public static string Name(SpyMission mission) => mission switch { SpyMission.Reconnaissance => "Rozpoznanie miasta", SpyMission.StealResearch => "Kradzież badań", _ => "Sabotaż produkcji" };
    public static string StatusName(SpyOutcome outcome) => outcome switch
    { SpyOutcome.Active => "Operacja trwa", SpyOutcome.Succeeded => "Sukces", SpyOutcome.Failed => "Niepowodzenie", SpyOutcome.Captured => "Agent schwytany", _ => "Operacja odwołana" };
    public static string? RecruitError(GameState s, int owner = 0)
    {
        if (s.Ending != null || owner is < 0 or > 3 || !s.Cities.Any(c => c.Owner == owner)) return "Państwo nie może teraz rekrutować agentów.";
        if (!s.Factions[owner].Technologies.Contains("writing")) return "Wywiad wymaga Pisma.";
        if (s.Spies.Count(a => a.Owner == owner && a.Status != SpyStatus.Lost) >= 3) return "Służba może utrzymywać trzech agentów, wliczając schwytanych.";
        return s.Factions[owner].Gold < RecruitCost ? "Szkolenie agenta kosztuje 60 złota." : null;
    }
    public static string? ResearchTarget(GameState s, int owner, int target) => Content.Technologies
        .Where(t => s.Factions[target].Technologies.Contains(t.Id) && !s.Factions[owner].Technologies.Contains(t.Id) && t.Requires.All(s.Factions[owner].Technologies.Contains))
        .OrderBy(t => t.Cost).ThenBy(t => t.Id).Select(t => t.Id).FirstOrDefault();
    public static int Defense(GameState s, City city) => (city.Buildings.Contains("court") ? 15 : 0) +
        (s.Factions[city.Owner].Technologies.Contains("writing") ? 5 : 0) + (s.Factions[city.Owner].Stability >= 70 ? 5 : 0) + (city.CounterIntelligenceUntil > s.Turn ? 25 : 0);
    public static SpyForecast Forecast(GameState s, SpyAgent agent, City city, SpyMission mission)
    {
        int defense = Defense(s, city), rank = Rank(agent);
        int success = Math.Clamp((mission == SpyMission.Reconnaissance ? 85 : mission == SpyMission.StealResearch ? 60 : 65) + rank * 5 - defense, 20, 95);
        int detection = Math.Clamp((mission == SpyMission.Reconnaissance ? 20 : mission == SpyMission.StealResearch ? 35 : 45) + defense / 2 - rank * 3, 5, 80);
        return new(success, detection, defense, mission == SpyMission.StealResearch ? ResearchTarget(s, agent.Owner, city.Owner) : null);
    }
    public static string? MissionError(GameState s, SpyAgent? agent, City? city, SpyMission mission, int owner = 0)
    {
        if (s.Ending != null || owner is < 0 or > 3 || agent == null || agent.Owner != owner || city == null || city.Owner == owner || !Enum.IsDefined(mission)) return "Wybierz własnego agenta, obce miasto i operację.";
        if (!s.Cities.Any(c => c.Owner == owner) || !s.Factions[owner].Technologies.Contains("writing")) return "Wywiad wymaga własnego miasta i Pisma.";
        if (agent.Status != SpyStatus.Available) return "Agent nie jest dostępny.";
        if (owner == 0 && !s.Tiles[city.Tile].Explored) return "Najpierw odkryj pole miasta.";
        if (s.SpyOperations.Any(o => o.Owner == owner && o.CityId == city.Id && (o.Outcome == SpyOutcome.Active || s.Turn - o.StartedTurn < CityInterval))) return "Siatka w tym mieście wymaga ośmiu tur odstępu od rozpoczęcia poprzedniej operacji.";
        if (mission == SpyMission.StealResearch && ResearchTarget(s, owner, city.Owner) == null) return "Cel nie zna dostępnej dla nas, nieodkrytej technologii.";
        return s.Factions[owner].Gold < Cost(mission) ? $"Operacja kosztuje {Cost(mission)} złota z góry." : null;
    }
    public static string? ProtectionError(GameState s, City? city, int owner = 0)
    {
        if (s.Ending != null || owner is < 0 or > 3 || city == null || city.Owner != owner) return "Wybierz własne miasto.";
        if (!s.Factions[owner].Technologies.Contains("writing")) return "Kontrwywiad wymaga Pisma.";
        if (city.CounterIntelligenceUntil > s.Turn) return $"Wzmocniona ochrona działa do początku T{city.CounterIntelligenceUntil}.";
        return s.Factions[owner].Gold < ProtectionCost ? "Wzmocnienie kontrwywiadu kosztuje 60 złota." : null;
    }
    public static void Cancel(GameState s, SpyOperation operation, string reason)
    {
        operation.Outcome = SpyOutcome.Cancelled; operation.ResolvedTurn = s.Turn; operation.Report = reason + " Koszt operacji nie podlega zwrotowi.";
        s.Spies.First(a => a.Id == operation.AgentId).Status = SpyStatus.Available;
        if (operation.Owner == 0) s.Log($"Operacja #{operation.Id}: {operation.Report}", "wywiad");
    }
    public static void OwnershipChanged(GameState s, City city)
    {
        city.CounterIntelligenceUntil = 0;
        foreach (var operation in s.SpyOperations.Where(o => o.CityId == city.Id && o.Outcome == SpyOutcome.Active && o.TargetOwner != city.Owner))
            Cancel(s, operation, "Miasto zmieniło właściciela; siatka została wycofana.");
    }
    public static void Tick(GameState s)
    {
        foreach (var operation in s.SpyOperations.Where(o => o.Outcome == SpyOutcome.Active && o.DueTurn <= s.Turn).OrderBy(o => o.Id))
        {
            var city = s.Cities.First(c => c.Id == operation.CityId); var agent = s.Spies.First(a => a.Id == operation.AgentId);
            if (city.Owner != operation.TargetOwner || !s.Cities.Any(c => c.Owner == operation.Owner)) { Cancel(s, operation, "Cel lub państwo wysyłające nie ma już właściwego kontekstu."); continue; }
            if (operation.Mission == SpyMission.StealResearch && (s.Factions[operation.Owner].Technologies.Contains(operation.ResearchId!) || !s.Factions[city.Owner].Technologies.Contains(operation.ResearchId!)))
            { Cancel(s, operation, "Wskazane badania nie stanowią już dostępnej, nowej wiedzy."); continue; }
            var forecast = Forecast(s, agent, city, operation.Mission);
            operation.SuccessChance = forecast.SuccessChance; operation.DetectionChance = forecast.DetectionChance;
            operation.SuccessRoll = s.Next(100); operation.DetectionRoll = s.Next(100);
            bool success = operation.SuccessRoll < operation.SuccessChance; operation.Detected = operation.DetectionRoll < operation.DetectionChance;
            operation.ResolvedTurn = s.Turn; operation.Outcome = success ? SpyOutcome.Succeeded : operation.Detected ? SpyOutcome.Captured : SpyOutcome.Failed;
            agent.Status = operation.Outcome == SpyOutcome.Captured ? SpyStatus.Captured : SpyStatus.Available;
            agent.Experience = Math.Min(30, agent.Experience + (success ? 2 : operation.Detected ? 0 : 1));
            operation.Report = success ? ApplySuccess(s, operation, city) : "Nie uzyskano efektu operacji.";
            if (operation.Detected)
            {
                var relation = s.Relation(operation.Owner, operation.TargetOwner); relation.Trust = Math.Max(-100, relation.Trust - 15);
                DiplomaticCouncil.AddGrievance(s, operation.TargetOwner, operation.Owner, 25); s.Factions[operation.Owner].Prestige -= 2;
                operation.Report += " Wykrycie: zaufanie -15, krzywdy poszkodowanych +25, prestiż wysyłającego -2.";
            }
            if (operation.Outcome == SpyOutcome.Captured)
            {
                operation.Report += " Agent pozostaje w niewoli do rozstrzygnięcia jego losu.";
                if (operation.Owner != 0) SpyCouncil.ResolveCapture(s, operation, s.Factions[operation.Owner].Gold >= SpyCouncil.Ransom ? 0 : 2, true);
            }
            if (operation.Owner == 0 || operation.TargetOwner == 0 && operation.Detected)
                s.Log($"{Name(operation.Mission)} #{operation.Id} — {city.Name}: {StatusName(operation.Outcome)}. {operation.Report}", "wywiad");
        }
    }
    private static string ApplySuccess(GameState s, SpyOperation operation, City city)
    {
        if (operation.Mission == SpyMission.StealResearch)
        {
            operation.ResultValue = Math.Min(120, Math.Ceiling(Content.Tech(operation.ResearchId!).Cost * .35));
            s.Factions[operation.Owner].Science += operation.ResultValue;
            return $"Kopie badań: {Content.Tech(operation.ResearchId!).Name}. +{operation.ResultValue:0} nauki do wspólnej puli; bez automatycznego odkrycia technologii.";
        }
        if (operation.Mission == SpyMission.Sabotage)
        {
            operation.ResultValue = Math.Min(60, Math.Max(0, city.Production)); city.Production -= operation.ResultValue;
            return $"Usunięto {operation.ResultValue:0.##} punktów zgromadzonej produkcji. Budynki i kolejka pozostają; efekt zależy od pracy obecnej w chwili akcji.";
        }
        if (operation.Owner == 0) foreach (var tile in s.Tiles.Where(t => World.Distance(t, s.Tiles[city.Tile]) <= 3)) tile.Explored = true;
        return $"Raport z T{s.Turn}: {city.Name}, ludność {city.Population}, zdrowie {city.Health:0}%, projekt {Content.Project(city.Project).Name}, praca {city.Production:0.##}. " +
            $"Skarb państwa {s.Factions[city.Owner].Name}: {s.Factions[city.Owner].Gold:0.##}. Garnizon: {s.Units.Count(u => u.Owner == city.Owner && u.Tile == city.Tile)} oddziałów. " +
            "To stan z chwili rozpoznania, bez stałego podglądu miasta.";
    }
    public static void ManageAi(GameState s, int owner)
    {
        if (owner is < 1 or > 3) return;
        foreach (var city in s.Cities.Where(c => c.Owner == owner && s.SpyOperations.Any(o => o.TargetOwner == owner && o.CityId == c.Id && o.Detected && o.ResolvedTurn == s.Turn)))
            if (ProtectionError(s, city, owner) == null) Engine.ProtectCity(s, city.Id, owner);
        if (s.Turn < 20 || s.Turn % 12 != owner * 3 || s.Factions[owner].Gold < 250 || !s.Factions[owner].Technologies.Contains("writing")) return;
        var target = s.Cities.Where(c => c.Owner != owner && (Engine.AtWar(s, owner, c.Owner) || s.Relation(owner, c.Owner).Trust < -10))
            .OrderBy(c => c.Owner == 0 ? 0 : 1).ThenByDescending(c => c.Production).ThenBy(c => c.Id)
            .FirstOrDefault(c => !s.SpyOperations.Any(o => o.Owner == owner && o.CityId == c.Id && (o.Outcome == SpyOutcome.Active || s.Turn - o.StartedTurn < CityInterval)) &&
                (ResearchTarget(s, owner, c.Owner) != null || c.Production >= 20));
        if (target == null) return;
        var agent = s.Spies.FirstOrDefault(a => a.Owner == owner && a.Status == SpyStatus.Available);
        if (agent == null && RecruitError(s, owner) == null) { Engine.RecruitSpy(s, owner); agent = s.Spies.Last(a => a.Owner == owner); }
        if (agent != null) Engine.StartSpyMission(s, agent.Id, target.Id, ResearchTarget(s, owner, target.Owner) == null ? SpyMission.Sabotage : SpyMission.StealResearch, owner);
    }
    internal static SpyAgent CreateAgent(GameState s, int owner) => new() { Id = s.NextSpyId++, Owner = owner, Name = Names[(s.NextSpyId - 2) % Names.Length] };
}

public static partial class Engine
{
    public static Result RecruitSpy(GameState s, int owner = 0)
    {
        string? error = Espionage.RecruitError(s, owner); if (error != null) return Result.Fail(error);
        s.Factions[owner].Gold -= Espionage.RecruitCost; var agent = Espionage.CreateAgent(s, owner); s.Spies.Add(agent);
        if (owner == 0) s.Log($"Wywiad: przeszkolono agenta {agent.Name} za 60 złota.", "wywiad"); return Result.Ok("Agent jest gotowy do operacji.");
    }
    public static Result StartSpyMission(GameState s, int agentId, int cityId, SpyMission mission, int owner = 0)
    {
        var agent = s.Spies.FirstOrDefault(a => a.Id == agentId); var city = s.Cities.FirstOrDefault(c => c.Id == cityId);
        string? error = Espionage.MissionError(s, agent, city, mission, owner); if (error != null) return Result.Fail(error);
        var forecast = Espionage.Forecast(s, agent!, city!, mission); s.Factions[owner].Gold -= Espionage.Cost(mission); agent!.Status = SpyStatus.Deployed;
        var operation = new SpyOperation { Id = s.NextSpyOperationId++, AgentId = agentId, Owner = owner, CityId = cityId, TargetOwner = city!.Owner, Mission = mission,
            StartedTurn = s.Turn, DueTurn = s.Turn + Espionage.Duration(mission), SuccessChance = forecast.SuccessChance, DetectionChance = forecast.DetectionChance, ResearchId = forecast.ResearchId };
        s.SpyOperations.Add(operation);
        if (owner == 0) s.Log($"{Espionage.Name(mission)} #{operation.Id}: {agent.Name} → {city.Name}, koszt {Espionage.Cost(mission)}, termin T{operation.DueTurn}.", "wywiad");
        return Result.Ok("Operacja rozpoczęta. Ryzyko zależy od obrony miasta w chwili rozstrzygnięcia.");
    }
    public static Result RecallSpy(GameState s, int operationId)
    {
        var operation = s.SpyOperations.FirstOrDefault(o => o.Id == operationId && o.Owner == 0 && o.Outcome == SpyOutcome.Active);
        if (s.Ending != null || operation == null) return Result.Fail("Nie ma takiej trwającej operacji Marchii.");
        Espionage.Cancel(s, operation, "Rada odwołała operację przed rozstrzygnięciem."); return Result.Ok("Operacja odwołana. Agent jest dostępny; koszt i odstęp siatki pozostają.");
    }
    public static Result ProtectCity(GameState s, int cityId, int owner = 0)
    {
        var city = s.Cities.FirstOrDefault(c => c.Id == cityId); string? error = Espionage.ProtectionError(s, city, owner); if (error != null) return Result.Fail(error);
        s.Factions[owner].Gold -= Espionage.ProtectionCost; city!.CounterIntelligenceUntil = s.Turn + Espionage.ProtectionTurns;
        if (owner == 0) s.Log($"{city.Name}: wzmocniony kontrwywiad do początku T{city.CounterIntelligenceUntil}, koszt 60 złota.", "wywiad");
        return Result.Ok("Obrona przed wywiadem +25 przez osiem tur. Działa także na rozpoczęte operacje.");
    }
}
