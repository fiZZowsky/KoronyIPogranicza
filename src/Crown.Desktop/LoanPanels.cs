using Crown.Core;
using System.Windows.Controls;

namespace Crown.Desktop;

public sealed partial class MainWindow
{
    private void ShowLoans(int creditor = 1, int principal = 100, double scrollOffset = 0)
    {
        var open = state.Loans.Where(TreasuryLoans.Open).OrderBy(l => l.DueTurn).ThenBy(l => l.Id).ToArray();
        var p = Ui.Stack(Ui.Eyebrow("SKARB I WIARYGODNOŚĆ"), Ui.Label("Pożyczki i zobowiązania", 30, Ui.Text, true),
            Ui.Label($"Skarbiec: {state.Player.Gold:0.##} złota · otwarte umowy {open.Length}/2\nŁącznie do spłaty: {TreasuryLoans.Outstanding(state)} złota" + (open.Length == 0 ? "" : $" · najbliższy termin T{open[0].DueTurn}"), 17, Ui.Gold, true),
            Ui.Label("Pożyczka daje środki od razu i wymaga zwrotu 120% kapitału po 12 turach. Najwyżej dwie otwarte umowy, jedna na państwo wierzycieli. Kolejne finansowanie z tego samego państwa najwcześniej po ośmiu turach od poprzedniego. Wcześniejsza spłata nie obniża ceny umowy.", 13, Ui.Muted),
            Ui.Label("Spłaty i opłaty są jednorazowe; saldo dochodu na turę ich nie odejmuje. Punktacja majątku uwzględnia długi i należności, więc samo pożyczone złoto nie dodaje punktów. Termin nie zabiera złota automatycznie: rada wybiera odpowiedź. Wojna lub upadek państwa wierzycieli nie umarzają długu.", 13, Ui.Muted));
        if (TreasuryLoans.FrozenUntil(state) > state.Turn) p.Children.Add(Ui.Label($"Po utracie wiarygodności nowy kredyt zamknięty do T{TreasuryLoans.FrozenUntil(state)}.", 14, Ui.Red));
        ScrollViewer? Scroller() => overlay is System.Windows.Controls.Grid layer ? layer.Children.OfType<System.Windows.Controls.Border>().FirstOrDefault()?.Child as ScrollViewer : null;
        void Apply(Result result)
        { double offset = Scroller()?.VerticalOffset ?? 0; Do(result); if (result.Success) { TryAutoSave(false); ShowLoans(creditor, principal, offset); } }
        p.Children.Add(Ui.Rule()); p.Children.Add(Ui.Eyebrow("NOWE FINANSOWANIE"));
        var lenders = Ui.Combo(state.Factions.Skip(1).Select(f => f.Name).ToArray(), creditor - 1);
        var amounts = Ui.Combo(TreasuryLoans.Principals.Select(n => $"{n} złota teraz → {n + n / 5} do spłaty").ToArray(), Array.IndexOf(TreasuryLoans.Principals, principal));
        var quote = Ui.Label("", 13, Ui.Gold);
        var borrow = Ui.Button("Podpisz umowę pożyczki", () => Apply(Engine.BorrowGold(state, creditor, principal)), true);
        void Update()
        {
            creditor = lenders.SelectedIndex + 1; principal = TreasuryLoans.Principals[amounts.SelectedIndex];
            string? error = TreasuryLoans.BorrowError(state, creditor, principal); borrow.IsEnabled = error == null;
            quote.Text = $"Otrzymasz {principal}; zwrócisz {principal + principal / 5} w T{state.Turn + TreasuryLoans.Term}.\n" + (error ?? "Kupcy zgodzą się na te warunki. Podpisanie przeleje złoto i zapisze zobowiązanie.");
        }
        lenders.SelectionChanged += (_, _) => Update(); amounts.SelectionChanged += (_, _) => Update(); Update();
        p.Children.Add(lenders); p.Children.Add(amounts); p.Children.Add(quote); p.Children.Add(borrow);
        p.Children.Add(Ui.Label("Wymagania: Moneta, pokój, traktat handlowy i zaufanie +10. Wspierający kupców skarb musi mieć kapitał. W terminie można raz zapłacić 20 złota za sześć tur odroczenia, zwiększając dług o 10% kapitału; zaufanie -5. Drugiego odroczenia nie ma.", 12, Ui.Muted));
        p.Children.Add(Ui.Rule()); p.Children.Add(Ui.Eyebrow("UMOWY I HISTORIA · OSTATNIE 20"));
        foreach (var loan in state.Loans.OrderByDescending(TreasuryLoans.Open).ThenByDescending(l => l.Id).Take(20))
        {
            var q = Ui.Stack(Ui.Eyebrow($"POŻYCZKA #{loan.Id} · {TreasuryLoans.StatusName(loan.Status)}"),
                Ui.Label(state.Factions[loan.Creditor].Name, 22, Ui.Text, true),
                Ui.Label($"Kapitał {loan.Principal} · umowna spłata {TreasuryLoans.Amount(loan)} · wpłaty do wierzycieli {loan.Paid}\nPodpisano T{loan.StartedTurn} · termin T{loan.DueTurn}" + (loan.ExtendedTurn == null ? "" : $" · odroczono T{loan.ExtendedTurn}") + (loan.ResolvedTurn == null ? "" : $"\nZamknięto T{loan.ResolvedTurn}."), 13, Ui.Muted));
            if (TreasuryLoans.Open(loan))
            {
                var repay = Ui.Button($"Spłać pożyczkę #{loan.Id} · {TreasuryLoans.Amount(loan)} złota", () => Apply(Engine.RepayLoan(state, loan.Id)));
                string? error = TreasuryLoans.RepayError(state, loan); repay.IsEnabled = error == null; q.Children.Add(repay);
                if (error != null) q.Children.Add(Ui.Label(error, 12, Ui.Gold));
                if (loan.Status == LoanStatus.AwaitingCouncil)
                {
                    var reply = Ui.Button("Rozmowa z wierzycielami #" + loan.Id, () => { var result = DebtCouncil.Present(state, loan.Id); if (result.Success) ShowCouncil(); else Do(result); }, true);
                    reply.IsEnabled = state.Ending == null && (state.PendingEvent == null || state.PendingEvent == DebtCouncil.Event.Id && state.PendingLoanId == loan.Id); q.Children.Add(reply);
                    if (!reply.IsEnabled) q.Children.Add(Ui.Label("Najpierw rozstrzygnij aktualną sprawę rady.", 12, Ui.Gold));
                }
            }
            p.Children.Add(Ui.Card(q));
        }
        p.Children.Add(Ui.Label("Odmowa spłaty: stabilność -12, lud -4, rody -8, prestiż -10, zaufanie wierzycieli -30 i ich krzywdy +35. Traktat handlowy z nimi wygasa, co może zatrzymać karawany; nowy kredyt u wszystkich kupców zamyka się na 24 tury. Pozostałe pożyczki nadal trzeba rozliczyć.", 13, Ui.Red));
        p.Children.Add(Ui.Button("Wróć do mapy", CloseModal)); Modal(p, 830); root.UpdateLayout(); Scroller()?.ScrollToVerticalOffset(scrollOffset);
    }
}
