namespace Crown.Core;

public enum EnergyCaseStatus { Pending, Relief, Pledged, Fulfilled, Broken, Declined, Voided }
public enum EnergyFailure { Deadline, CityLost }
public sealed class EnergyReading
{
    public int PhaseTurn { get; set; }
    public int Demand { get; set; }
    public double Allocated { get; set; }
    public List<string> Services { get; set; } = [];
    public double Coverage => Demand == 0 ? 1 : Allocated / Demand;
    public bool Shortage => Demand > 0 && Coverage < .75;
}
public sealed class EnergyCase
{
    public int Id { get; set; }
    public int CityId { get; set; }
    public int OpenedTurn { get; set; }
    public int ReliefCost { get; set; }
    public EnergyCaseStatus Status { get; set; }
    public List<EnergyReading> Shortages { get; set; } = [];
    public int? Decision { get; set; }
    public int? DecidedTurn { get; set; }
    public int? DeadlineTurn { get; set; }
    public int? ResolvedTurn { get; set; }
    public EnergyFailure? Failure { get; set; }
    public List<EnergyReading> Deliveries { get; set; } = [];
}
internal sealed record EnergySample(int CityId, int Owner, EnergyReading Reading);

public static class EnergyCouncil
{
    public const int HistoryLimit = 8, ShortagePhases = 3, RecoveryPhases = 3, PromiseTurns = 6, Cooldown = 16;
    public const string SuccessEffects = "Sukces: lud +6, stabilność +2, prestiż +8; miasto: dobrobyt +6, niepokoje -6.";
    public const string FailureEffects = "Porażka: lud -10, stabilność -4, prestiż -8 (minimum 0); własne miasto: dobrobyt -6, niepokoje +10. Utrata miasta łamie obietnicę.";
    public static readonly NarrativeEvent Event = new("energy_petition", "Ira Senn", "Delegatka pracowników i usług miejskich", "Gdy sieć zawodzi",
        "Warsztaty skracają zmiany, laboratoria przerywają badania, a usługi czekają na przydział mocy. Mieszkańcy pytają, kto poniesie koszt priorytetów rady.",
        [new("Sfinansujemy pomoc dla dotkniętych rodzin.", "Pomoc nie przywraca energii. Miasto: niepokoje -8, dobrobyt +4.", Gold: -75, Stability: 1, Commons: 4),
         new("Przywrócimy pełną pracę wskazanych zakładów.", SuccessEffects + " " + FailureEffects, Commons: 2),
         new("Utrzymamy obecny podział. Nie obiecamy interwencji.", "Miasto: niepokoje +4. Nie powstaje zobowiązanie do dostaw.", Commons: -4, Nobles: 4)]);
    public static City City(GameState s, EnergyCase c) => s.Cities.First(city => city.Id == c.CityId);
    public static string StatusName(EnergyCaseStatus status) => status switch
    {
        EnergyCaseStatus.Pending => "Delegacja czeka na radę", EnergyCaseStatus.Relief => "Wypłacono pomoc", EnergyCaseStatus.Pledged => "Obietnica odbudowy",
        EnergyCaseStatus.Fulfilled => "Przywrócono niezawodne zasilanie", EnergyCaseStatus.Broken => "Złamana obietnica", EnergyCaseStatus.Declined => "Odmówiono interwencji", _ => "Zamknięto po utracie miasta"
    };
    public static bool Satisfies(EnergyCase c, EnergyReading reading) => reading.Demand > 0 && reading.Allocated + 1e-8 >= reading.Demand &&
        c.Shortages[^1].Services.All(reading.Services.Contains);
    public static int Progress(EnergyCase c) => c.Deliveries.AsEnumerable().Reverse().TakeWhile(r => Satisfies(c, r)).Count();
    public static string? ChoiceError(GameState s, int choice)
    {
        var c = s.EnergyCases.FirstOrDefault(c => c.Id == s.PendingEnergyCaseId && c.Status == EnergyCaseStatus.Pending);
        if (choice is < 0 or > 2 || !s.EpochCampaign || c == null || City(s, c).Owner != 0) return "Ta delegacja nie ma aktualnej sprawy Marchii.";
        if (choice == 0 && s.Player.Gold < c.ReliefCost) return $"Pomoc wymaga {c.ReliefCost} złota.";
        return null;
    }
    public static NarrativeEvent Current(GameState s)
    {
        var c = s.EnergyCases.FirstOrDefault(c => c.Id == s.PendingEnergyCaseId); if (c == null) return Event;
        var city = City(s, c); var last = c.Shortages[^1]; var choices = Event.Choices.ToArray();
        choices[0] = choices[0] with { Text = $"Przeznaczymy {c.ReliefCost} złota na pomoc rodzinom.", Gold = -c.ReliefCost };
        choices[1] = choices[1] with { Text = $"Zapewnimy trzy kolejne fazy pełnego zasilania przed początkiem T{s.Turn + PromiseTurns}.",
            Consequence = $"Liczą się wyłącznie przyszłe rozliczenia gospodarki od T{s.Turn} do T{s.Turn + PromiseTurns - 1}. Muszą działać wszystkie zakłady wskazane w protokole, a całe aktywne miasto potrzebuje 100% pokrycia. Wyłączenie tych zakładów nie spełnia obietnicy. " + SuccessEffects + " " + FailureEffects };
        return Event with { Title = Event.Title + " — " + city.Name,
            Body = $"W mieście {city.Name} przez trzy kolejne rozliczenia gospodarki pokrycie potrzeb wynosiło mniej niż 75%. Ostatni odczyt: {last.Allocated:0.##}/{last.Demand} mocy ({last.Coverage:P0}). Przestoje obciążają pracowników, naukę i miejskie usługi. Pomoc kosztuje {c.ReliefCost} złota według liczby mieszkańców w chwili zgłoszenia. Możemy złagodzić skutki, złożyć sprawdzalną obietnicę albo utrzymać podział bez gwarancji. Protokół obejmuje: " + string.Join(", ", last.Services.Select(id => Content.Project(id).Name)) + ".", Choices = choices };
    }
    public static bool Schedule(GameState s)
    {
        if (!s.EpochCampaign || s.PendingEvent != null) return false;
        var c = s.EnergyCases.Where(c => c.Status == EnergyCaseStatus.Pending && City(s, c).Owner == 0).OrderBy(c => c.OpenedTurn).ThenBy(c => c.Id).FirstOrDefault();
        if (c == null) return false;
        s.PendingEvent = Event.Id; s.PendingCityId = null; s.PendingEnergyCaseId = c.Id; return true;
    }
    public static Result Present(GameState s, int id)
    {
        if (s.Ending != null || s.PendingEvent != null && (s.PendingEvent != Event.Id || s.PendingEnergyCaseId != id)) return Result.Fail("Najpierw rozstrzygnij bieżącą sprawę rady.");
        var c = s.EnergyCases.FirstOrDefault(c => c.Id == id && c.Status == EnergyCaseStatus.Pending);
        if (!s.EpochCampaign || c == null || City(s, c).Owner != 0) return Result.Fail("Nie ma takiej oczekującej delegacji energetycznej.");
        s.PendingEvent = Event.Id; s.PendingCityId = null; s.PendingEnergyCaseId = id; return Result.Ok("Delegacja czeka w sali rady.");
    }
    internal static void Apply(GameState s, int choice)
    {
        var c = s.EnergyCases.First(c => c.Id == s.PendingEnergyCaseId); var city = City(s, c);
        c.Decision = choice; c.DecidedTurn = s.Turn;
        if (choice == 1) { c.Status = EnergyCaseStatus.Pledged; c.DeadlineTurn = s.Turn + PromiseTurns; return; }
        c.Status = choice == 0 ? EnergyCaseStatus.Relief : EnergyCaseStatus.Declined; c.ResolvedTurn = s.Turn;
        city.Unrest = Math.Clamp(city.Unrest + (choice == 0 ? -8 : 4), 0, 100);
        if (choice == 0) city.Prosperity = Math.Clamp(city.Prosperity + 4, 0, 100);
    }
    private static void Settle(GameState s, EnergyCase c, bool success, EnergyFailure? failure = null)
    {
        c.Status = success ? EnergyCaseStatus.Fulfilled : EnergyCaseStatus.Broken; c.Failure = failure; c.ResolvedTurn = s.Turn;
        var f = s.Player; f.Commons += success ? 6 : -10; f.Stability += success ? 2 : -4; f.Prestige = Math.Max(0, f.Prestige + (success ? 8 : -8)); Engine.Clamp(f);
        var city = City(s, c);
        if (city.Owner == 0) { city.Unrest = Math.Clamp(city.Unrest + (success ? -6 : 10), 0, 100); city.Prosperity = Math.Clamp(city.Prosperity + (success ? 6 : -6), 0, 100); }
        s.Log($"{city.Name}: {StatusName(c.Status)}. Kolejne pełne fazy: {Progress(c)}/{RecoveryPhases}. " + (failure == EnergyFailure.CityLost ? "Utracono miasto. " : "") + (success ? SuccessEffects : FailureEffects), "konsekwencja");
    }
    internal static EnergyReading Copy(EnergyReading reading) => new() { PhaseTurn = reading.PhaseTurn, Demand = reading.Demand, Allocated = reading.Allocated, Services = [.. reading.Services] };
    internal static void Observe(GameState s, IReadOnlyList<EnergySample> samples)
    {
        if (!s.EpochCampaign || s.LastEnergyPhase >= s.Turn - 1) return;
        var affectedOwners = new HashSet<int>();
        foreach (var sample in samples.OrderBy(x => x.CityId))
        {
            var city = s.Cities.FirstOrDefault(c => c.Id == sample.CityId && c.Owner == sample.Owner); if (city == null) continue;
            var reading = sample.Reading;
            city.EnergyHistory.Add(Copy(reading)); if (city.EnergyHistory.Count > HistoryLimit) city.EnergyHistory.RemoveAt(0);
            foreach (var c in s.EnergyCases.Where(c => c.CityId == city.Id && c.Status == EnergyCaseStatus.Pledged))
            {
                if (reading.PhaseTurn < c.DecidedTurn || reading.PhaseTurn >= c.DeadlineTurn) continue;
                c.Deliveries.Add(Copy(reading));
                if (Progress(c) >= RecoveryPhases) Settle(s, c, true);
                else if (s.Turn >= c.DeadlineTurn) Settle(s, c, false, EnergyFailure.Deadline);
            }
            var recent = city.EnergyHistory.TakeLast(ShortagePhases).ToArray();
            if (recent.Length != ShortagePhases || recent.Any(r => !r.Shortage) || recent.Zip(recent.Skip(1)).Any(pair => pair.Second.PhaseTurn != pair.First.PhaseTurn + 1)) continue;
            city.Unrest = Math.Min(100, city.Unrest + 2); city.Prosperity = Math.Max(0, city.Prosperity - 1); affectedOwners.Add(city.Owner);
            if (city.Owner != 0 || s.EnergyCases.Any(c => c.CityId == city.Id && (c.Status is EnergyCaseStatus.Pending or EnergyCaseStatus.Pledged || s.Turn - c.ResolvedTurn < Cooldown))) continue;
            s.EnergyCases.Add(new() { Id = s.NextEnergyCaseId++, CityId = city.Id, OpenedTurn = s.Turn, ReliefCost = 60 + city.Population * 5, Shortages = recent.Select(Copy).ToList() });
            s.Log($"{city.Name}: trzy fazy niedoboru energii. Delegacja prosi o pomoc lub sprawdzalne zobowiązanie rady. Długie niedobory: niepokoje +2 i dobrobyt -1 na fazę; lud państwa -1 niezależnie od liczby dotkniętych miast.", "energia");
        }
        foreach (int owner in affectedOwners) { s.Factions[owner].Commons--; Engine.Clamp(s.Factions[owner]); }
        s.LastEnergyPhase = s.Turn - 1;
    }
    internal static void OwnershipChanged(GameState s, City city)
    {
        city.EnergyHistory.Clear();
        if (city.Owner == 0) return;
        foreach (var c in s.EnergyCases.Where(c => c.CityId == city.Id && c.Status is EnergyCaseStatus.Pending or EnergyCaseStatus.Pledged))
        {
            if (c.Status == EnergyCaseStatus.Pledged) Settle(s, c, false, EnergyFailure.CityLost);
            else { c.Status = EnergyCaseStatus.Voided; c.ResolvedTurn = s.Turn; s.Log($"{city.Name}: zamknięto delegację energetyczną po utracie miasta.", "energia"); }
            if (s.PendingEnergyCaseId == c.Id) { s.PendingEnergyCaseId = null; if (s.PendingEvent == Event.Id) s.PendingEvent = null; }
        }
    }
}
