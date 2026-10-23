using Crown.Core;
using System.Windows.Controls;
using Catalog = Crown.Core.Content;

namespace Crown.Desktop;

public sealed partial class MainWindow
{
    private void ShowEnergyCases(int? cityId = null)
    {
        var p = Ui.Stack(Ui.Eyebrow("SŁOWO RADY I DOSTAWY ENERGII"), Ui.Label("Kryzysy i obietnice", 31, Ui.Text, true),
            Ui.Label("Trzy kolejne fazy z pokryciem poniżej 75% rodzą kryzys. Od trzeciej fazy: niepokoje miasta +2 i dobrobyt -1; lud państwa -1 na fazę niezależnie od liczby dotkniętych miast. Zasada dotyczy też AI. Delegacje gracza mogą otrzymać pomoc, obietnicę albo odmowę. Kolejna sprawa tego miasta najwcześniej 16 tur po zamknięciu poprzedniej.", 13, Ui.Muted),
            Ui.Label("Obietnica wymaga trzech kolejnych przyszłych faz pełnego zasilania całego aktywnego miasta i działania wszystkich zakładów wskazanych w protokole. Masz sześć faz gospodarczych. Wyłączenie obiecanej usługi nie jest naprawą. Budowa elektrowni zasila od kolejnej fazy, a podgląd nie jest dowodem dostawy.", 13, Ui.Muted));
        if (state.PendingEvent != null) p.Children.Add(Ui.Label("Aktualna sprawa rady: " + ProvincialEvents.Current(state).Title, 12, Ui.Gold));
        var cases = state.EnergyCases.Where(c => cityId == null || c.CityId == cityId)
            .OrderByDescending(c => c.Status is EnergyCaseStatus.Pending or EnergyCaseStatus.Pledged).ThenByDescending(c => c.OpenedTurn).ThenBy(c => c.Id).Take(30).ToArray();
        if (cases.Length == 0) p.Children.Add(Ui.Label("Brak zgłoszonych kryzysów. Rejestr obejmuje przyszłe fazy gospodarki; starsze zapisy nie otrzymują wymyślonej historii niedoborów.", 15, Ui.Gold));
        foreach (var c in cases)
        {
            var city = EnergyCouncil.City(state, c); var last = c.Shortages[^1];
            var card = Ui.Stack(Ui.Eyebrow($"SPRAWA #{c.Id} · OD T{c.OpenedTurn}"), Ui.Label(city.Name, 24, Ui.Text, true),
                Ui.Label(EnergyCouncil.StatusName(c.Status), 17, c.Status == EnergyCaseStatus.Fulfilled ? Ui.Green : c.Status == EnergyCaseStatus.Broken ? Ui.Red : Ui.Gold),
                Ui.Label($"Ostatnia faza zgłoszenia T{last.PhaseTurn}: {last.Allocated:0.##}/{last.Demand} mocy · {last.Coverage:P0}\nPomoc: {c.ReliefCost} złota. Protokół: " + string.Join(", ", last.Services.Select(id => Catalog.Project(id).Name)), 13, Ui.Muted));
            if (c.Decision == 1)
            {
                Ui.Bar(card, $"Kolejne pełne fazy: {EnergyCouncil.Progress(c)}/{EnergyCouncil.RecoveryPhases} · termin: początek T{c.DeadlineTurn}", EnergyCouncil.Progress(c), EnergyCouncil.RecoveryPhases, c.Status == EnergyCaseStatus.Broken ? Ui.Red : Ui.Green);
                card.Children.Add(Ui.Label($"Dozwolone rozliczenia: T{c.DecidedTurn}–T{c.DeadlineTurn - 1}. Ostatnia dozwolona faza najpierw sprawdza wykonanie.\n{EnergyCouncil.SuccessEffects}\n{EnergyCouncil.FailureEffects}", 12, Ui.Muted));
                foreach (var r in c.Deliveries)
                    card.Children.Add(Ui.Label($"T{r.PhaseTurn}: {r.Allocated:0.##}/{r.Demand} mocy · " + (EnergyCouncil.Satisfies(c, r) ? "pełna praca wskazanych usług" : "warunki niewypełnione — seria przerwana"), 12, EnergyCouncil.Satisfies(c, r) ? Ui.Green : Ui.Gold));
            }
            if (c.Failure == EnergyFailure.CityLost) card.Children.Add(Ui.Label("Obietnicę przerwała utrata miasta. Kary miejscowe nie przechodzą na nowego właściciela.", 12, Ui.Red));
            if (c.ResolvedTurn != null) card.Children.Add(Ui.Label($"Zamknięto w T{c.ResolvedTurn}. Wynik pozostaje w kronice.", 12, Ui.Muted));
            if (c.Status == EnergyCaseStatus.Pending)
            {
                var button = Ui.Button("Rozpatrz kryzys #" + c.Id, () =>
                { var r = EnergyCouncil.Present(state, c.Id); if (r.Success) { CloseModal(); Refresh(); TryAutoSave(false); ShowCouncil(); } else Toast(r.Message, false); }, true);
                button.IsEnabled = state.Ending == null && (state.PendingEvent == null || state.PendingEvent == EnergyCouncil.Event.Id && state.PendingEnergyCaseId == c.Id); card.Children.Add(button);
            }
            if (city.Owner == 0) card.Children.Add(Ui.Button("Napraw zasilanie: " + city.Name, () => ShowPowerManagement(city.Id)));
            p.Children.Add(Ui.Card(card));
        }
        if (cityId != null) p.Children.Add(Ui.Button("Wszystkie kryzysy energetyczne", () => ShowEnergyCases()));
        p.Children.Add(Ui.Button("Energia, klimat i kosmos", ShowWorldDevelopment)); p.Children.Add(Ui.Button("Wróć do mapy", CloseModal)); Modal(p, 930);
    }
    private void AddEnergyHistory(StackPanel panel, City city)
    {
        panel.Children.Add(Ui.Eyebrow("ODCZYTY RZECZYWISTYCH FAZ GOSPODARKI"));
        if (city.EnergyHistory.Count == 0) panel.Children.Add(Ui.Label("Brak zarejestrowanych faz. Bieżący podgląd zasilania jest prognozą, nie historią.", 12, Ui.Muted));
        else foreach (var r in city.EnergyHistory.TakeLast(8))
            panel.Children.Add(Ui.Label($"T{r.PhaseTurn}: {r.Allocated:0.##}/{r.Demand} mocy · " + (r.Demand == 0 ? "brak aktywnych potrzeb" : r.Coverage.ToString("P0")), 12, r.Shortage ? Ui.Red : r.Coverage < 1 ? Ui.Gold : Ui.Green));
        var active = state.EnergyCases.FirstOrDefault(c => c.CityId == city.Id && c.Status is EnergyCaseStatus.Pending or EnergyCaseStatus.Pledged);
        if (active != null) panel.Children.Add(Ui.Label($"{EnergyCouncil.StatusName(active.Status)} · sprawa #{active.Id}" + (active.DeadlineTurn != null ? $" · termin T{active.DeadlineTurn} · seria {EnergyCouncil.Progress(active)}/3" : ""), 13, Ui.Gold));
        panel.Children.Add(Ui.Button("Kryzysy i obietnice: " + city.Name, () => ShowEnergyCases(city.Id)));
    }
}
