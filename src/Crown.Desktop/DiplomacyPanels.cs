using Crown.Core;
using System.Windows.Controls;

namespace Crown.Desktop;

public sealed partial class MainWindow
{
    private void DiplomacyPanel()
    {
        TitlePanel("SŁOWO I ODPOWIEDZIALNOŚĆ", "Poselstwa i układy", "Jedna rozmowa z państwem na turę. Poselstwo kosztuje 20 złota; odpowiedź wraca po dwóch turach. Krzywdy wygasają o 1 co osiem tur.");
        side.Children.Add(Ui.Button("Pożyczki i zobowiązania", () => ShowLoans()));
        side.Children.Add(Ui.Button("Wywiad i kontrwywiad", ShowEspionage));
        foreach (var f in state.Factions.Skip(1))
        {
            var r = state.Relation(0, f.Id);
            var p = Ui.Stack(Ui.Eyebrow(f.Leader), Ui.Label(f.Name, 23, Ui.Brush(f.Color), true), Ui.Label(f.Trait, 11, Ui.Muted));
            Ui.Gap(p, 12);
            p.Children.Add(Ui.Label($"{(r.War ? "WOJNA" : r.Trade ? "TRAKTAT HANDLOWY" : "POKÓJ")} · relacje {r.Trust:+0;-0;0}", 12, r.War ? Ui.Red : Ui.Green));
            p.Children.Add(Ui.Label($"Nasze krzywdy: {DiplomaticCouncil.Grievances(state, 0, f.Id)} · ich krzywdy: {DiplomaticCouncil.Grievances(state, f.Id, 0)}", 12, Ui.Gold));
            var war=WarCouncil.Current(state,0,f.Id);
            if(war!=null)
            {
                var score=WarCouncil.Score(state,war,0);
                p.Children.Add(Ui.Label($"WOJNA OD TURY {war.StartedTurn} · bilans {score.Score:+0;-0;0}",12,score.Score>=0?Ui.Green:Ui.Red));
                p.Children.Add(Ui.Label($"Cel {state.Factions[war.Aggressor].Name}: {WarCouncil.Goal(state,war)}",12,Ui.Muted));
                p.Children.Add(Ui.Button("Przebieg wojny: "+f.Name,()=>ShowWarReport(war)));
            }
            else if(state.Wars.LastOrDefault(w=>w.A==0&&w.B==f.Id&&w.EndedTurn!=null) is WarCampaign ended)
            {
                p.Children.Add(Ui.Label($"OSTATNIA WOJNA · tury {ended.StartedTurn}–{ended.EndedTurn}",11,Ui.Gold));
                p.Children.Add(Ui.Label(ended.Outcome,12,Ui.Muted));
            }
            var peace=state.PeaceOffers.LastOrDefault(o=>o.From==f.Id||o.To==f.Id);
            if(peace!=null)
            {
                p.Children.Add(Ui.Label(PeaceStatusName(peace),12,WarCouncil.IsOpen(peace)?Ui.Gold:Ui.Muted));
                if(peace.Status==PeaceStatus.AwaitingPlayer)p.Children.Add(Ui.Button("Przeczytaj warunki: "+f.Name,()=>ShowPeaceReply(peace)));
                else if(peace.Reply.Length>0)p.Children.Add(Ui.Label(peace.Reply,12,Ui.Muted,true));
            }
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
                p.Children.Add(Ui.Button("Przejazd kupców: " + f.Name, () => ShowTransitNegotiation(f)));
                p.Children.Add(Ui.Button("Rozważ wypowiedzenie wojny…", () => ConfirmWar(f)));
            }
            else p.Children.Add(Ui.Button("Negocjuj pokój: "+f.Name,()=>ShowPeaceConference(f)));
            if (r.TruceUntil > state.Turn) p.Children.Add(Ui.Label($"Pakt / rozejm do tury {r.TruceUntil}.", 11, Ui.Gold));
            int until=TransitCouncil.Until(state,f.Id);
            if(until>0)p.Children.Add(Ui.Label(until>state.Turn?$"Zgoda na przejazd ładunków Marchii: do początku T{until}. Wojna i embargo mają pierwszeństwo.":$"Zgoda na przejazd wygasła w T{until}.",12,until>state.Turn?Ui.Green:Ui.Muted));
            side.Children.Add(Ui.Card(p));
        }
    }

    private void ShowNegotiation(Faction f)
    {
        var r = state.Relation(0, f.Id);
        var p = Ui.Stack(Ui.Eyebrow("INSTRUKCJA DLA POSŁÓW"), Ui.Label(f.Name, 30, Ui.Text, true),
            Ui.Label("Posłowie powrócą po dwóch turach. Rada sąsiada oceni sytuację w dniu odpowiedzi. Dar lub zmiana siły armii przed ich powrotem mogą wpłynąć na wynik.", 14, Ui.Muted));
        Ui.Gap(p, 18);
        foreach (var kind in new[]{DemandKind.Compensation,DemandKind.NonAggression})
        {
            p.Children.Add(Ui.Label(DiplomaticCouncil.DemandName(kind), 20, Ui.Gold, true));
            p.Children.Add(Ui.Label(kind == DemandKind.Compensation ? "Wymaga 30 krzywd. Przyjęcie: +80 złota i -30 krzywd. Odmowa: relacje -5. Krzywdy pozostają." : "Przyjęcie blokuje wypowiedzenie wojny przez obie strony przez 15 tur. Odmowa: relacje -5.", 13, Ui.Muted));
            p.Children.Add(Ui.Label($"Ocena obecnej sytuacji: {DiplomaticCouncil.AcceptanceScore(state, f.Id, kind)} / wymagane 15.\n{DiplomaticCouncil.Forecast(state, f.Id, kind)}", 12, Ui.Muted));
            var button = Ui.Button("Wyślij: " + DiplomaticCouncil.DemandName(kind) + " · koszt 20 złota", () =>
            {
                var result = Engine.SendDemand(state, f.Id, kind);
                if (result.Success) CloseModal(); Do(result);
            });
            button.IsEnabled = DiplomaticCouncil.DemandError(state,f.Id,kind)==null;
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
            var targets=state.Cities.Where(c=>c.Owner==f.Id&&(reason!=WarReason.Liberation||c.OriginalOwner==0)).OrderBy(c=>c.Id).ToArray();
            ComboBox? target=null;
            if(reason!=WarReason.Redress&&targets.Length>0)
            {
                p.Children.Add(Ui.Label("Ogłoszony cel: zdobyć / wyzwolić i utrzymać wybrane miasto do pokoju.",12,Ui.Muted));
                target=Ui.Combo(targets.Select(c=>c.Name).ToArray(),0);p.Children.Add(target);
            }
            else if(reason==WarReason.Redress)p.Children.Add(Ui.Label("Ogłoszony cel: otrzymać co najmniej 80 złota reparacji w traktacie.",12,Ui.Muted));
            var button = Ui.Button("Wypowiedz wojnę: " + DiplomaticCouncil.WarName(reason), () =>
            { var result = Engine.DeclareWar(state, f.Id, reason,target==null?null:targets[target.SelectedIndex].Id); if (result.Success) CloseModal(); Do(result); });
            button.IsEnabled = preview.Allowed; p.Children.Add(button); Ui.Gap(p, 12);
        }
        p.Children.Add(Ui.Label("Cel spełniony przy pokoju: prestiż +12, lud +3. Niespełniony: prestiż -5, lud -3. Bilans wojny pomaga negocjować, ale nie wymusza kapitulacji.",12,Ui.Gold));
        p.Children.Add(Ui.Button("Wycofaj posłów", CloseModal)); Modal(p, 780);
    }
}
