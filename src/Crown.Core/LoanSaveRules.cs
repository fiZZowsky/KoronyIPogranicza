namespace Crown.Core;

internal static class LoanSaveRules
{
    public static void Validate(GameState s)
    {
        if (s.Loans == null || s.Loans.Count > 2000 || s.Loans.Any(l => l == null) || s.NextLoanId < 1 || s.Loans.Select(l => l.Id).Distinct().Count() != s.Loans.Count)
            throw new InvalidDataException("Uszkodzony rejestr pożyczek.");
        foreach (var loan in s.Loans)
        {
            bool extended = loan.ExtendedTurn != null;
            int fees = extended ? TreasuryLoans.ExtensionFee : 0;
            if (loan.Id < 1 || loan.Id >= s.NextLoanId || loan.Creditor is < 1 or > 3 || !TreasuryLoans.Principals.Contains(loan.Principal) ||
                !Enum.IsDefined(loan.Status) || loan.StartedTurn < 1 || loan.StartedTurn > s.Turn ||
                (extended ? loan.ExtendedTurn < loan.StartedTurn + TreasuryLoans.Term || loan.ExtendedTurn > s.Turn || loan.DueTurn != loan.ExtendedTurn + TreasuryLoans.Extension : loan.DueTurn != loan.StartedTurn + TreasuryLoans.Term))
                throw new InvalidDataException("Nieprawidłowe warunki pożyczki.");
            if (TreasuryLoans.Open(loan))
            {
                if (loan.ResolvedTurn != null || loan.Paid != fees || (loan.Status == LoanStatus.Active ? loan.DueTurn <= s.Turn : loan.DueTurn > s.Turn))
                    throw new InvalidDataException("Nieprawidłowy stan otwartego długu.");
            }
            else if (loan.ResolvedTurn == null || loan.ResolvedTurn < (loan.ExtendedTurn ?? loan.StartedTurn) || loan.ResolvedTurn > s.Turn ||
                loan.Status == LoanStatus.Defaulted && loan.ResolvedTurn < loan.DueTurn || loan.Paid != fees + (loan.Status == LoanStatus.Repaid ? TreasuryLoans.Amount(loan) : 0))
                throw new InvalidDataException("Nieprawidłowe rozliczenie pożyczki.");
        }
        if (s.Loans.Count(TreasuryLoans.Open) > 2 || s.Loans.Where(TreasuryLoans.Open).GroupBy(l => l.Creditor).Any(g => g.Count() > 1))
            throw new InvalidDataException("Powielone aktywne zobowiązania.");
        foreach (var group in s.Loans.GroupBy(l => l.Creditor))
        {
            var history = group.OrderBy(l => l.StartedTurn).ToArray();
            if (history.Zip(history.Skip(1)).Any(p => p.Second.StartedTurn - p.First.StartedTurn < TreasuryLoans.Interval))
                throw new InvalidDataException("Pożyczki omijają odstęp finansowania.");
        }
        // A loan can be signed before a default later in that same turn; save data records turns, not action order.
        if (s.Loans.Any(l => s.Loans.Any(d => d.Status == LoanStatus.Defaulted && d.ResolvedTurn < l.StartedTurn && l.StartedTurn < d.ResolvedTurn + TreasuryLoans.CreditBan)))
            throw new InvalidDataException("Pożyczka omija utratę wiarygodności.");
        if (s.PendingEvent == DebtCouncil.Event.Id ? s.PendingCityId != null || s.PendingAuditId != null || s.PendingTradeCaseId != null ||
            !s.Loans.Any(l => l.Id == s.PendingLoanId && l.Status == LoanStatus.AwaitingCouncil) : s.PendingLoanId != null)
            throw new InvalidDataException("Uszkodzony kontekst rozmowy o długu.");
    }
}
