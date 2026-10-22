namespace Crown.Core;

public static class TreasuryLoans
{
    public const int Term = 12, Extension = 6, ExtensionFee = 20, Interval = 8, CreditBan = 24;
    public static readonly int[] Principals = [100, 200, 300];
    public static bool Open(SovereignLoan loan) => loan.Status is LoanStatus.Active or LoanStatus.AwaitingCouncil;
    public static int Amount(SovereignLoan loan) => loan.Principal + loan.Principal / 5 + (loan.ExtendedTurn == null ? 0 : loan.Principal / 10);
    public static int Outstanding(GameState s) => s.Loans.Where(Open).Sum(Amount);
    public static double NetWealth(GameState s, int owner) => s.Factions[owner].Gold + s.Loans.Where(l => Open(l) && l.Creditor == owner).Sum(Amount) - (owner == 0 ? Outstanding(s) : 0);
    public static int FrozenUntil(GameState s) => s.Loans.Where(l => l.Status == LoanStatus.Defaulted).Select(l => l.ResolvedTurn!.Value + CreditBan).DefaultIfEmpty(0).Max();
    public static string StatusName(LoanStatus status) => status switch
    { LoanStatus.Active => "Spłata w przyszłości", LoanStatus.AwaitingCouncil => "Wierzyciele czekają na decyzję", LoanStatus.Repaid => "Spłacona", _ => "Zerwane zobowiązanie" };
    public static string? BorrowError(GameState s, int creditor, int principal)
    {
        if (s.Ending != null || creditor is < 1 or > 3 || !Principals.Contains(principal)) return "Nieprawidłowy kredytodawca lub kwota.";
        if (!s.Player.Technologies.Contains("currency")) return "Pożyczki państwowe wymagają Monety.";
        if (!s.Cities.Any(c => c.Owner == 0) || !s.Cities.Any(c => c.Owner == creditor)) return "Pożyczka wymaga istniejących państw po obu stronach.";
        if (s.Turn < FrozenUntil(s)) return $"Po zerwaniu zobowiązań nowy kredyt jest zamknięty do T{FrozenUntil(s)}.";
        var relation = s.Relation(0, creditor);
        if (relation.War || !relation.Trade || relation.Trust < 10) return "Kupcy wymagają pokoju, traktatu handlowego i zaufania co najmniej +10.";
        if (s.Loans.Count(Open) >= 2) return "Państwo może utrzymywać najwyżej dwie otwarte pożyczki.";
        if (s.Loans.Any(l => l.Creditor == creditor && Open(l))) return "Wobec tych kupców istnieje już otwarte zobowiązanie.";
        int last = s.Loans.Where(l => l.Creditor == creditor).Select(l => l.StartedTurn).DefaultIfEmpty(-Interval).Max();
        if (s.Turn - last < Interval) return $"Następne finansowanie z tego państwa najwcześniej w T{last + Interval}.";
        if (s.Factions[creditor].Gold < principal) return "Skarb wspierający kupców nie ma środków na tę kwotę.";
        return null;
    }
    public static string? RepayError(GameState s, SovereignLoan? loan)
    {
        if (s.Ending != null || loan == null || !Open(loan)) return "Nie ma takiego otwartego zobowiązania.";
        return s.Player.Gold < Amount(loan) ? $"Spłata wymaga {Amount(loan)} złota, także przed terminem." : null;
    }
    public static string? ExtendError(GameState s, SovereignLoan loan)
    {
        if (loan.ExtendedTurn != null) return "To zobowiązanie było już odraczane. Kolejnego odroczenia nie będzie.";
        if (Engine.AtWar(s, 0, loan.Creditor) || !s.Cities.Any(c => c.Owner == loan.Creditor)) return "Odroczenie wymaga pokoju i czynnego państwa wierzycieli.";
        return s.Player.Gold < ExtensionFee ? "Odroczenie wymaga natychmiastowej opłaty 20 złota." : null;
    }
    internal static void Settle(GameState s, SovereignLoan loan)
    {
        int amount = Amount(loan); s.Player.Gold -= amount; s.Factions[loan.Creditor].Gold += amount;
        loan.Paid += amount; loan.Status = LoanStatus.Repaid; loan.ResolvedTurn = s.Turn;
        var relation = s.Relation(0, loan.Creditor); relation.Trust = Math.Min(100, relation.Trust + 5);
        if (s.PendingLoanId == loan.Id) { s.PendingLoanId = null; if (s.PendingEvent == DebtCouncil.Event.Id) s.PendingEvent = null; }
        s.Log($"Spłacono pożyczkę #{loan.Id}: {amount} złota dla kupców ({s.Factions[loan.Creditor].Name}). Zaufanie +5.", "skarb");
    }
    public static void Tick(GameState s)
    {
        foreach (var loan in s.Loans.Where(l => l.Status == LoanStatus.Active).OrderBy(l => l.Id))
        {
            if (loan.DueTurn <= s.Turn)
            { loan.Status = LoanStatus.AwaitingCouncil; s.Log($"Pożyczka #{loan.Id}: nadszedł termin spłaty {Amount(loan)} złota. Kupcy ({s.Factions[loan.Creditor].Name}) czekają na decyzję rady.", "skarb"); }
            else if (loan.DueTurn - s.Turn == 2)
            {
                string notice = $"Za dwie tury zapada pożyczka #{loan.Id}: {Amount(loan)} złota w T{loan.DueTurn}.";
                if (!s.Chronicle.Any(e => e.Turn == s.Turn && e.Category == "skarb" && e.Text == notice)) s.Log(notice, "skarb");
            }
        }
    }
}

public static partial class Engine
{
    public static Result BorrowGold(GameState s, int creditor, int principal)
    {
        string? error = TreasuryLoans.BorrowError(s, creditor, principal); if (error != null) return Result.Fail(error);
        s.Factions[creditor].Gold -= principal; s.Player.Gold += principal;
        var loan = new SovereignLoan { Id = s.NextLoanId++, Creditor = creditor, Principal = principal, StartedTurn = s.Turn, DueTurn = s.Turn + TreasuryLoans.Term };
        s.Loans.Add(loan);
        s.Log($"Pożyczka #{loan.Id} od kupców ({s.Factions[creditor].Name}): +{principal} złota teraz, spłata {TreasuryLoans.Amount(loan)} w T{loan.DueTurn}.", "skarb");
        return Result.Ok($"Otrzymano {principal} złota. Zobowiązanie: {TreasuryLoans.Amount(loan)} złota w T{loan.DueTurn}.");
    }
    public static Result RepayLoan(GameState s, int loanId)
    {
        var loan = s.Loans.FirstOrDefault(l => l.Id == loanId); string? error = TreasuryLoans.RepayError(s, loan);
        if (error != null) return Result.Fail(error);
        TreasuryLoans.Settle(s, loan!); return Result.Ok("Pożyczka spłacona. Zaufanie wierzycieli +5.");
    }
}
