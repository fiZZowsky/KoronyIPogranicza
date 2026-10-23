namespace Crown.Core;

public static class SpyCouncil
{
    public const int Ransom = 100, ApologyCost = 40;
    public static readonly NarrativeEvent Event = new("captured_spy", "Mira Weles", "Kanclerka Marchii", "Czyj człowiek, czyja odpowiedzialność?",
        "Obca straż schwytała naszego agenta. Możemy zapłacić okup, publicznie przyjąć odpowiedzialność albo pozostawić go bez ochrony państwa.",
        [new("Zapłacimy okup i sprowadzimy agenta.", "100 złota trafia do państwa przetrzymującego agenta. Wykrycie nadal obciąża relacje.", Gold: -Ransom),
         new("Przeprosimy publicznie i wynegocjujemy powrót.", "40 złota dla obcego skarbu, zaufanie +8; wymaga pokoju.", Gold: -ApologyCost, Commons: -3, Prestige: -8),
         new("Wyprzemy się powiązań z tym człowiekiem.", "Agent zostaje bezpowrotnie utracony; miejsce w służbie zostaje zwolnione.", Nobles: 3, Prestige: -4),
         new("Wymienimy jeńców i odzyskamy agenta.", "Wymaga nierozstrzygniętej sprawy agenta tego państwa w naszej niewoli. Obaj wracają bez opłaty, także podczas wojny.")]);
    public static string? ChoiceError(GameState s, int choice)
    {
        var operation = s.SpyOperations.FirstOrDefault(o => o.Id == s.PendingSpyOperationId && o.Owner == 0 && o.Outcome == SpyOutcome.Captured && o.CaptureChoice == null);
        if (s.Ending != null || s.PendingEvent != Event.Id || operation == null || choice is < 0 or > 3) return "Nie ma takiej nierozstrzygniętej sprawy agenta.";
        if (choice == 3 && CustodyCouncil.ForeignPartner(s, operation) == null) return "Nie mamy agenta tego państwa gotowego do wymiany.";
        if (choice == 1 && Engine.AtWar(s, 0, operation.TargetOwner)) return "Publiczne porozumienie wymaga pokoju.";
        int cost = choice == 0 ? Ransom : choice == 1 ? ApologyCost : 0;
        return s.Player.Gold < cost ? $"Ta decyzja wymaga {cost} złota." : null;
    }
    public static NarrativeEvent Current(GameState s)
    {
        var operation = s.SpyOperations.FirstOrDefault(o => o.Id == s.PendingSpyOperationId); if (operation == null) return Event;
        var agent = s.Spies.First(a => a.Id == operation.AgentId); var city = s.Cities.First(c => c.Id == operation.CityId);
        return Event with { Title = Event.Title + " — " + agent.Name,
            Body = $"Agenta ({agent.Name}) schwytano podczas operacji „{Espionage.Name(operation.Mission)}” w mieście {city.Name}, w T{operation.ResolvedTurn}. " +
                $"Agenta przetrzymuje państwo {s.Factions[operation.TargetOwner].Name}. Cena powrotu to 100 złota albo publiczne przeprosiny z opłatą 40 złota w czasie pokoju. " +
                "Wyparcie się agenta zwolni miejsce w służbie, lecz ten człowiek już do nas nie wróci. Jeśli trzymamy ich agenta z nierozstrzygniętą sprawą, możemy wymienić jeńców bez opłaty. Samo wykrycie przyniosło już utratę zaufania i nowe krzywdy. Jaką odpowiedź wyślemy?" };
    }
    public static bool Schedule(GameState s)
    {
        if (s.PendingEvent != null) return false;
        var operation = s.SpyOperations.Where(o => o.Owner == 0 && o.Outcome == SpyOutcome.Captured && o.CaptureChoice == null).OrderBy(o => o.ResolvedTurn).ThenBy(o => o.Id).FirstOrDefault();
        if (operation == null) return false;
        SetContext(s, operation.Id); return true;
    }
    private static void SetContext(GameState s, int operationId)
    { s.PendingEvent = Event.Id; s.PendingSpyOperationId = operationId; s.PendingCityId = s.PendingAuditId = s.PendingTradeCaseId = s.PendingLoanId = null; }
    public static Result Present(GameState s, int operationId)
    {
        if (s.Ending != null || s.PendingEvent != null && (s.PendingEvent != Event.Id || s.PendingSpyOperationId != operationId)) return Result.Fail("Najpierw rozstrzygnij aktualną sprawę rady.");
        if (!s.SpyOperations.Any(o => o.Id == operationId && o.Owner == 0 && o.Outcome == SpyOutcome.Captured && o.CaptureChoice == null)) return Result.Fail("Nie ma takiej sprawy schwytanego agenta.");
        SetContext(s, operationId); return Result.Ok("Rada rozpatruje los agenta.");
    }
    internal static void Apply(GameState s, int choice)
    {
        var operation = s.SpyOperations.First(o => o.Id == s.PendingSpyOperationId);
        if (choice == 3) CustodyCouncil.Exchange(s, CustodyCouncil.ForeignPartner(s, operation)!, operation);
        else ResolveCapture(s, operation, choice, false);
    }
    internal static void ResolveCapture(GameState s, SpyOperation operation, int choice, bool chargeSender)
    {
        var sender = s.Factions[operation.Owner]; var agent = s.Spies.First(a => a.Id == operation.AgentId); int cost = choice == 0 ? Ransom : choice == 1 ? ApologyCost : 0;
        if (chargeSender)
        {
            sender.Gold -= cost; sender.Prestige -= choice == 1 ? 8 : choice == 2 ? 4 : 0;
            sender.Commons -= choice == 1 ? 3 : 0; sender.Nobles += choice == 2 ? 3 : 0; Engine.Clamp(sender);
        }
        s.Factions[operation.TargetOwner].Gold += cost;
        if (choice == 1) { var r = s.Relation(operation.Owner, operation.TargetOwner); r.Trust = Math.Min(100, r.Trust + 8); }
        agent.Status = choice == 2 ? SpyStatus.Lost : SpyStatus.Available; operation.CaptureChoice = choice; operation.CaptureDecisionTurn = s.Turn;
        operation.Report += choice == 2 ? $" W T{s.Turn} państwo wyparło się agenta." : $" W T{s.Turn} wpłacono {cost} złota; agent wrócił do służby.";
    }
}
