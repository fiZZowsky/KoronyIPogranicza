namespace Crown.Core;

public static class CustodyCouncil
{
    public const int InternmentTurns = 8, Exchanged = 3, Released = 4, InternmentEnded = 5;
    public static readonly NarrativeEvent Event = new("foreign_captive", "Kapitan Eren", "Dowódca kontrwywiadu", "Człowiek po drugiej stronie stołu",
        "Straż zatrzymała obcego agenta. Jego państwo chce odzyskać człowieka, a nasi ludzie czekają na decyzję rady.",
        [new("Zażądamy okupu za powrót agenta.", "100 złota z obcego skarbu do naszego. Wymaga środków wysyłającego; agent wraca do służby."),
         new("Wymienimy go na naszego człowieka.", "Obaj agenci wracają bez opłaty. Wymaga naszego jeńca w tym samym państwie; możliwe także podczas wojny."),
         new("Zwolnimy go z listem do jego władcy.", "Agent wraca od razu; zaufanie +12. Wcześniejsze krzywdy pozostają.", Nobles: -3, Prestige: 4),
         new("Internujemy go na osiem tur.", "Agent nadal zajmuje miejsce w obcej służbie, potem automatycznie wraca. Zaufanie -8. Decyzji nie można cofnąć.", Commons: 3)]);

    public static bool Open(SpyOperation o) => o.Outcome == SpyOutcome.Captured && o.TargetOwner == 0 && o.Owner != 0 && o.CaptureChoice == null && o.CustodyChoice == null;
    public static SpyOperation? ExchangePartner(GameState s, SpyOperation foreign) => s.SpyOperations
        .Where(o => o.Owner == 0 && o.TargetOwner == foreign.Owner && o.Outcome == SpyOutcome.Captured && o.CaptureChoice == null)
        .OrderBy(o => o.ResolvedTurn).ThenBy(o => o.Id).FirstOrDefault();
    public static SpyOperation? ForeignPartner(GameState s, SpyOperation own) => s.SpyOperations
        .Where(o => Open(o) && o.Owner == own.TargetOwner).OrderBy(o => o.ResolvedTurn).ThenBy(o => o.Id).FirstOrDefault();
    public static string? ChoiceError(GameState s, int choice)
    {
        var o = s.SpyOperations.FirstOrDefault(o => o.Id == s.PendingSpyOperationId && Open(o));
        if (s.Ending != null || s.PendingEvent != Event.Id || o == null || choice is < 0 or > 3) return "Nie ma takiej nierozstrzygniętej sprawy obcego agenta.";
        if (choice == 0 && s.Factions[o.Owner].Gold < SpyCouncil.Ransom) return "Obcy skarb nie ma 100 złota na okup. Wybierz inne rozstrzygnięcie.";
        if (choice == 1 && ExchangePartner(s, o) == null) return "To państwo nie przetrzymuje naszego agenta gotowego do wymiany.";
        return null;
    }
    public static NarrativeEvent Current(GameState s)
    {
        var o = s.SpyOperations.FirstOrDefault(o => o.Id == s.PendingSpyOperationId); if (o == null) return Event;
        var agent = s.Spies.First(a => a.Id == o.AgentId); var partner = ExchangePartner(s, o);
        return Event with { Title = Event.Title + " — " + agent.Name,
            Body = $"Zatrzymana osoba to {agent.Name} z państwa {s.Factions[o.Owner].Name}. Do schwytania doszło w T{o.ResolvedTurn} podczas operacji „{Espionage.Name(o.Mission)}” w mieście {s.Cities.First(c => c.Id == o.CityId).Name}. " +
                $"Obcy skarb ma teraz {s.Factions[o.Owner].Gold:0.##} złota. " + (partner == null ? "Nie mają naszego człowieka do wymiany. " : $"Możemy odzyskać agenta {s.Spies.First(a => a.Id == partner.AgentId).Name} (sprawa #{partner.Id}). ") +
                $"Okup daje środki, wymiana ratuje własnego człowieka, a gest dobrej woli pomaga rozmowom. Internowanie zwiąże miejsce w obcej służbie do początku T{s.Turn + InternmentTurns}, kosztem zaufania. Żadna decyzja nie cofnie skutków wykrycia." };
    }
    public static bool Schedule(GameState s)
    {
        if (s.PendingEvent != null) return false;
        var o = s.SpyOperations.Where(Open).OrderBy(o => o.ResolvedTurn).ThenBy(o => o.Id).FirstOrDefault();
        if (o == null) return false; SetContext(s, o.Id); return true;
    }
    private static void SetContext(GameState s, int id)
    { s.PendingEvent = Event.Id; s.PendingSpyOperationId = id; s.PendingCityId = s.PendingAuditId = s.PendingTradeCaseId = s.PendingLoanId = null; }
    public static Result Present(GameState s, int id)
    {
        if (s.Ending != null || s.PendingEvent != null && (s.PendingEvent != Event.Id || s.PendingSpyOperationId != id)) return Result.Fail("Najpierw rozstrzygnij aktualną sprawę rady.");
        if (!s.SpyOperations.Any(o => o.Id == id && Open(o))) return Result.Fail("Nie ma takiej otwartej sprawy obcego agenta.");
        SetContext(s, id); return Result.Ok("Rada rozpatruje los obcego agenta.");
    }
    internal static void Apply(GameState s, int choice)
    {
        var o = s.SpyOperations.First(o => o.Id == s.PendingSpyOperationId);
        if (choice == 1) { Exchange(s, o, ExchangePartner(s, o)!); return; }
        o.CustodyChoice = (CustodyDecision)choice; o.CustodyTurn = s.Turn;
        if (choice == 0)
        {
            SpyCouncil.ResolveCapture(s, o, 0, true);
        }
        else if (choice == 2)
        {
            Free(s, o, Released); ChangeTrust(s, o.Owner, 12);
            o.Report += $" W T{s.Turn} rada zwolniła agenta z listem pojednania; zaufanie +12.";
        }
        else
        {
            o.InternedUntil = s.Turn + InternmentTurns; ChangeTrust(s, o.Owner, -8);
            o.Report += $" W T{s.Turn} rada postanowiła internować agenta do początku T{o.InternedUntil}; zaufanie -8.";
        }
    }
    internal static void Exchange(GameState s, SpyOperation foreign, SpyOperation own)
    {
        foreign.CustodyChoice = CustodyDecision.Exchange; foreign.CustodyTurn = s.Turn;
        foreign.ExchangeOperationId = own.Id; own.ExchangeOperationId = foreign.Id;
        Free(s, foreign, Exchanged); Free(s, own, Exchanged);
        foreign.Report += $" W T{s.Turn} wymieniono agenta za jeńca ze sprawy #{own.Id}, bez opłaty.";
        own.Report += $" W T{s.Turn} agent wrócił w wymianie za jeńca ze sprawy #{foreign.Id}, bez opłaty.";
    }
    private static void Free(GameState s, SpyOperation o, int result)
    { o.CaptureChoice = result; o.CaptureDecisionTurn = s.Turn; s.Spies.First(a => a.Id == o.AgentId).Status = SpyStatus.Available; }
    private static void ChangeTrust(GameState s, int owner, int delta)
    { var r = s.Relation(0, owner); r.Trust = Math.Clamp(r.Trust + delta, -100, 100); }
    internal static void Tick(GameState s)
    {
        foreach (var o in s.SpyOperations.Where(o => o.CustodyChoice == CustodyDecision.Intern && o.CaptureChoice == null && o.InternedUntil <= s.Turn).OrderBy(o => o.Id))
        {
            Free(s, o, InternmentEnded); o.Report += $" W T{s.Turn} internowanie zakończono; agent wrócił do służby.";
            s.Log($"Internowanie: {s.Spies.First(a => a.Id == o.AgentId).Name} wrócił do państwa {s.Factions[o.Owner].Name}. Sprawa #{o.Id} zakończona.", "wywiad");
        }
    }
    public static string DecisionName(int choice) => choice switch
    { 0 => "okup i powrót", 1 => "przeprosiny i powrót", 2 => "agent utracony", Exchanged => "wymiana agentów", Released => "zwolnienie i list pojednania", _ => "koniec internowania" };
}
