namespace Crown.Core;

public static class DebtCouncil
{
    public static readonly NarrativeEvent Event = new("sovereign_debt", "Tera Voss", "Posłanka kupców i skarbu", "Pieczęć ma swoją cenę",
        "Dwanaście tur temu otrzymaliście nasze złoto. Dziś przywieźliśmy podpisaną umowę. Wasze warsztaty pracują, armia pobiera żołd, ale nasi wspólnicy też złożyli obietnice. Czy dotrzymacie własnej?",
        [new("Spłacimy całe zobowiązanie.", "Zaufanie wierzycieli +5."),
         new("Zapłacimy za jednorazowe odroczenie.", "Sześć dodatkowych tur, większy dług i opłata teraz."),
         new("Nie uznamy tego długu.", "Zaufanie -30, krzywdy +35, koniec traktatu handlowego; nowy kredyt zamknięty na 24 tury.", Stability: -12, Commons: -4, Nobles: -8, Prestige: -10)]);
    public static string? ChoiceError(GameState s, int choice)
    {
        var loan = s.Loans.FirstOrDefault(l => l.Id == s.PendingLoanId && l.Status == LoanStatus.AwaitingCouncil);
        if (s.Ending != null || s.PendingEvent != Event.Id || choice is < 0 or > 2 || loan == null) return "Nie ma prawidłowego wezwania wierzycieli.";
        return choice == 0 ? TreasuryLoans.RepayError(s, loan) : choice == 1 ? TreasuryLoans.ExtendError(s, loan) : null;
    }
    public static NarrativeEvent Current(GameState s)
    {
        var loan = s.Loans.FirstOrDefault(l => l.Id == s.PendingLoanId); if (loan == null) return Event;
        var choices = Event.Choices.ToArray();
        choices[0] = choices[0] with { Gold = -TreasuryLoans.Amount(loan) };
        choices[1] = choices[1] with { Gold = -TreasuryLoans.ExtensionFee, Consequence = $"Dług wzrośnie o {loan.Principal / 10} do {loan.Principal + loan.Principal / 5 + loan.Principal / 10} złota. Nowy termin: T{s.Turn + TreasuryLoans.Extension}. Zaufanie -5. Nie będzie drugiego odroczenia." };
        return Event with { Title = Event.Title + $" — pożyczka #{loan.Id}", Choices = choices,
            Body = $"Delegacja kupiecka ({s.Factions[loan.Creditor].Name}) przedstawia umowę #{loan.Id}: otrzymano {loan.Principal} złota w T{loan.StartedTurn}, termin T{loan.DueTurn}, do spłaty {TreasuryLoans.Amount(loan)}. Dotąd wpłacono wierzycielom {loan.Paid} złota. " +
                (loan.ExtendedTurn == null ? "Możemy zapłacić pełną sumę, wykupić sześć tur odroczenia albo zerwać zobowiązanie. " : "Termin był już raz przesuwany. Kupcy nie zgodzą się na kolejne odroczenie. ") +
                "Zerwanie ugodzi w zaufanie oraz handel, a pozostałe domy kupieckie zamkną kredyt na 24 tury. Wojna i upadek państwa wierzycieli nie umarzają długu. Jaka odpowiedź znajdzie się pod waszą pieczęcią?" };
    }
    public static bool Schedule(GameState s)
    {
        if (s.PendingEvent != null) return false;
        var loan = s.Loans.Where(l => l.Status == LoanStatus.AwaitingCouncil).OrderBy(l => l.DueTurn).ThenBy(l => l.Id).FirstOrDefault();
        if (loan == null) return false;
        s.PendingEvent = Event.Id; s.PendingLoanId = loan.Id; s.PendingCityId = s.PendingAuditId = s.PendingTradeCaseId = null; return true;
    }
    public static Result Present(GameState s, int loanId)
    {
        if (s.Ending != null || s.PendingEvent != null && (s.PendingEvent != Event.Id || s.PendingLoanId != loanId)) return Result.Fail("Najpierw rozstrzygnij bieżącą sprawę rady.");
        if (!s.Loans.Any(l => l.Id == loanId && l.Status == LoanStatus.AwaitingCouncil)) return Result.Fail("Nie ma takiego wezwania wierzycieli.");
        s.PendingEvent = Event.Id; s.PendingLoanId = loanId; s.PendingCityId = s.PendingAuditId = s.PendingTradeCaseId = null; return Result.Ok("Wierzyciele czekają w sali rady.");
    }
    internal static void Apply(GameState s, int choice)
    {
        var loan = s.Loans.First(l => l.Id == s.PendingLoanId); var relation = s.Relation(0, loan.Creditor);
        // Narrative applies the player's quoted gold and political effects; do not charge those twice.
        if (choice == 0)
        {
            s.Factions[loan.Creditor].Gold += TreasuryLoans.Amount(loan); loan.Paid += TreasuryLoans.Amount(loan);
            loan.Status = LoanStatus.Repaid; loan.ResolvedTurn = s.Turn; relation.Trust = Math.Min(100, relation.Trust + 5);
        }
        else if (choice == 1)
        {
            s.Factions[loan.Creditor].Gold += TreasuryLoans.ExtensionFee; loan.Paid += TreasuryLoans.ExtensionFee;
            loan.ExtendedTurn = s.Turn; loan.DueTurn = s.Turn + TreasuryLoans.Extension; loan.Status = LoanStatus.Active;
            relation.Trust = Math.Max(-100, relation.Trust - 5);
        }
        else
        {
            loan.Status = LoanStatus.Defaulted; loan.ResolvedTurn = s.Turn; relation.Trust = Math.Max(-100, relation.Trust - 30);
            relation.Trade = false; DiplomaticCouncil.AddGrievance(s, loan.Creditor, 0, 35); s.InvalidateLogistics();
        }
    }
}
