using Crown.Core;
using System.Windows.Controls;

namespace Crown.Desktop;

public sealed partial class MainWindow
{
    private void DiplomacyPanel()
    {
        TitlePanel("SŁOWO I ODPOWIEDZIALNOŚĆ", "Poselstwa i układy", "Jedna rozmowa z państwem na turę. Poselstwo kosztuje 20 złota; odpowiedź wraca po dwóch turach. Krzywdy wygasają o 1 co osiem tur.");
        foreach (var f in state.Factions.Skip(1))
        {
            var r = state.Relation(0, f.Id);
            var p = Ui.Stack(Ui.Eyebrow(f.Leader), Ui.Label(f.Name, 23, Ui.Brush(f.Color), true), Ui.Label(f.Trait, 11, Ui.Muted));
            Ui.Gap(p, 12);
            p.Children.Add(Ui.Label($"{(r.War ? "WOJNA" : r.Trade ? "TRAKTAT HANDLOWY" : "POKÓJ")} · relacje {r.Trust:+0;-0;0}", 12, r.War ? Ui.Red : Ui.Green));
            p.Children.Add(Ui.Label($"Nasze krzywdy: {DiplomaticCouncil.Grievances(state, 0, f.Id)} · ich krzywdy: {DiplomaticCouncil.Grievances(state, f.Id, 0)}", 12, Ui.Gold));
            var pending = state.Demands.LastOrDefault(d => d.To == f.Id && d.Status == DemandStatus.Pending);
            var reply = state.Demands.LastOrDefault(d => d.To == f.Id && d.Status != DemandStatus.Pending);
            Ui.Gap(p, 8);
            if (pending != null) p.Children.Add(Ui.Label($"W DRODZE · {DiplomaticCouncil.DemandName(pending.Kind)}\nOdpowiedź w turze {pending.DueTurn}.", 12, Ui.Gold));
            if (reply != null) p.Children.Add(Ui.Label($"T{reply.ResolvedTurn} · {reply.Reply}", 12, Ui.Muted, true));
            else p.Children.Add(Ui.Label(r.War ? "„Słowa mają wagę, ale teraz przemawiają armie.”" : r.Trust >= 20 ? "„Dotrzymane słowo otwiera więcej bram niż miecz.”" : "„Rada wysłucha waszych posłów. Nie obiecujemy zgody.”", 12, Ui.Muted, true));
            Ui.Gap(p, 12);
            if (!state.Cities.Any(c => c.Owner == f.Id)) p.Children.Add(Ui.Label("Państwo utraciło swoje miasta.", 12, Ui.Red));
            else if (!r.War)
            {
                p.Children.Add(Ui.Button("Wyślij dar · 40 złota · relacje +12", () => Do(Engine.Diplomacy(state, f.Id, "gift"))));
                if (!r.Trade) p.Children.Add(Ui.Button("Traktat handlowy · 30 złota", () => Do(Engine.Diplomacy(state, f.Id, "trade"))));
                p.Children.Add(Ui.Button("Negocjuj warunki: " + f.Name, () => ShowNegotiation(f)));
                p.Children.Add(Ui.Button("Rozważ wypowiedzenie wojny…", () => ConfirmWar(f)));
            }
            else p.Children.Add(Ui.Button($"Pokój · {Math.Max(40, 100 - (int)state.Player.WarWeariness)} złota reparacji", () => Do(Engine.Diplomacy(state, f.Id, "peace"))));
            if (r.TruceUntil > state.Turn) p.Children.Add(Ui.Label($"Pakt / rozejm do tury {r.TruceUntil}.", 11, Ui.Gold));
            side.Children.Add(Ui.Card(p));
        }
    }

    private void ShowNegotiation(Faction f)
    {
        var r = state.Relation(0, f.Id);
        var p = Ui.Stack(Ui.Eyebrow("INSTRUKCJA DLA POSŁÓW"), Ui.Label(f.Name, 30, Ui.Text, true),
            Ui.Label("Posłowie powrócą po dwóch turach. Rada sąsiada oceni sytuację w dniu odpowiedzi. Dar lub zmiana siły armii przed ich powrotem mogą wpłynąć na wynik.", 14, Ui.Muted));
        Ui.Gap(p, 18);
        foreach (var kind in Enum.GetValues<DemandKind>())
        {
            p.Children.Add(Ui.Label(DiplomaticCouncil.DemandName(kind), 20, Ui.Gold, true));
            p.Children.Add(Ui.Label(kind == DemandKind.Compensation ? "Wymaga 30 krzywd. Przyjęcie: +80 złota i -30 krzywd. Odmowa: relacje -5. Krzywdy pozostają." : "Przyjęcie blokuje wypowiedzenie wojny przez obie strony przez 15 tur. Odmowa: relacje -5.", 13, Ui.Muted));
            p.Children.Add(Ui.Label($"Ocena obecnej sytuacji: {DiplomaticCouncil.AcceptanceScore(state, f.Id, kind)} / wymagane 15.\n{DiplomaticCouncil.Forecast(state, f.Id, kind)}", 12, Ui.Muted));
            var button = Ui.Button("Wyślij: " + DiplomaticCouncil.DemandName(kind) + " · koszt 20 złota", () =>
            {
                var result = Engine.SendDemand(state, f.Id, kind);
                if (result.Success) CloseModal(); Do(result);
            });
            button.IsEnabled = !r.War && r.LastActionTurn != state.Turn && state.Player.Gold >= 20 && state.Turn-r.LastDemandTurn >= 10 &&
                !state.Demands.Any(d => d.To == f.Id && d.Status == DemandStatus.Pending) &&
                (kind == DemandKind.Compensation ? DiplomaticCouncil.Grievances(state, 0, f.Id) >= 30 : r.TruceUntil <= state.Turn);
            p.Children.Add(button); Ui.Gap(p, 16);
        }
        p.Children.Add(Ui.Label("Po poselstwie kolejne warunki można wysłać temu państwu po dziesięciu turach. Przyjęte warunki poprawiają relacje o 5. Wypowiedzenie wojny anuluje oczekujące negocjacje.", 12, Ui.Muted));
        p.Children.Add(Ui.Button("Wróć do mapy", CloseModal)); Modal(p, 780);
    }

    private void ConfirmWar(Faction f)
    {
        var p = Ui.Stack(Ui.Eyebrow("RADA WOJENNA"), Ui.Label("Wojna z " + f.Name, 30, Ui.Text, true),
            Ui.Label("Wojna zrywa handel i obniża wzajemne relacje o 35. Powód określa koszt społeczny i reakcję pozostałych państw. Z czasem rośnie wyczerpanie wojną.", 14, Ui.Muted));
        Ui.Gap(p, 16);
        foreach (var reason in Enum.GetValues<WarReason>())
        {
            var preview = DiplomaticCouncil.PreviewWar(state, f.Id, reason);
            p.Children.Add(Ui.Label(DiplomaticCouncil.WarName(reason), 20, Ui.Gold, true));
            p.Children.Add(Ui.Label(preview.Reason, 13, preview.Allowed ? Ui.Muted : Ui.Red));
            if (preview.Allowed) p.Children.Add(Ui.Label($"Lud -{preview.CommonsCost} · stabilność -{preview.StabilityCost} · zaufanie innych państw -{preview.ForeignTrustCost}", 12, Ui.Muted));
            var button = Ui.Button("Wypowiedz wojnę: " + DiplomaticCouncil.WarName(reason), () =>
            { var result = Engine.DeclareWar(state, f.Id, reason); if (result.Success) CloseModal(); Do(result); });
            button.IsEnabled = preview.Allowed; p.Children.Add(button); Ui.Gap(p, 12);
        }
        p.Children.Add(Ui.Button("Wycofaj posłów", CloseModal)); Modal(p, 780);
    }
}
