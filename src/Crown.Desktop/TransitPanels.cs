using Crown.Core;

namespace Crown.Desktop;

public sealed partial class MainWindow
{
    private void ShowTransitNegotiation(Faction host)
    {
        var p=Ui.Stack(Ui.Eyebrow("ROZMOWA O ZAMKNIĘTYCH BRAMACH"),Ui.Label(host.Name,30,Ui.Text,true),
            Ui.Label(host.Leader+": „Przejazd kupców to sprawa zaufania i dochodów naszej rady. Wasze sztandary pozostaną przed bramą.”",14,Ui.Gold,true),
            Ui.Label("Zgoda obejmuje przez 12 tur wszystkie importy i eksporty Marchii przez ziemie tego państwa, również już wysłane ładunki i rozwiązane kontrakty. Zastępuje wymóg jego traktatów z obiema stronami transportu. Nie zastępuje traktatu kupującego ze sprzedawcą, nie otwiera drogi armii, nie omija wojny z żadną ze stron ani embarga. Każde kolejne państwo na trasie wymaga osobnej zgody.",13,Ui.Muted));
        int until=TransitCouncil.Until(state,host.Id);
        if(until>0)p.Children.Add(Ui.Label(until>state.Turn?$"ZGODA OBOWIĄZUJE DO POCZĄTKU T{until} · pozostało {until-state.Turn} tur":$"POPRZEDNIA ZGODA WYGASŁA W T{until}",14,Ui.Gold,true));
        var pending=state.Demands.LastOrDefault(d=>d.To==host.Id&&d.Status==DemandStatus.Pending);
        if(pending!=null)p.Children.Add(Ui.Label($"Posłowie już w drodze: {DiplomaticCouncil.DemandName(pending.Kind)}. Odpowiedź T{pending.DueTurn}.",13,Ui.Gold));
        var reply=state.Demands.LastOrDefault(d=>d.To==host.Id&&TransitCouncil.IsRequest(d.Kind)&&d.Status!=DemandStatus.Pending);
        if(reply!=null)p.Children.Add(Ui.Label($"T{reply.ResolvedTurn} · {reply.Reply}",13,Ui.Muted,true));
        var affected=state.Caravans.Where(Caravans.Active).Where(c=>
            TransitCouncil.ClosedBorders(state,state.TradeContracts.First(t=>t.Id==c.ContractId),c.Tile,EscortOrders.Attached(state,c)).Contains(host.Id)).ToArray();
        if(affected.Length>0)p.Children.Add(Ui.Label("Na potencjalnym korytarzu czekają: "+string.Join(", ",affected.Select(c=>$"karawana #{c.Id} (postój {c.BlockedTurns}/3)"))+". Poselstwo nie zatrzymuje licznika postoju.",13,Ui.Red));
        Ui.Gap(p,14);
        foreach(var kind in new[]{DemandKind.Transit,DemandKind.PaidTransit})
        {
            var q=Ui.Stack(Ui.Label(DiplomaticCouncil.DemandName(kind),21,Ui.Gold,true),
                Ui.Label(kind==DemandKind.Transit?"Poproś o przejazd w imię dobrych relacji. Przyjęcie bez dodatkowej opłaty.":
                    "Zaoferuj jednorazowe 60 złota za wszystkie przejazdy przez 12 tur. Oferta dodaje 35 do oceny. Płatność trafi do gospodarza wyłącznie przy przyjęciu; brak środków w dniu odpowiedzi oznacza odmowę.",13,Ui.Muted),
                Ui.Label($"Ocena: {DiplomaticCouncil.AcceptanceScore(state,host.Id,kind)} / wymagane 15.\n{DiplomaticCouncil.Forecast(state,host.Id,kind)}",12,Ui.Muted));
            string? error=DiplomaticCouncil.DemandError(state,host.Id,kind);
            var send=Ui.Button("Wyślij: "+DiplomaticCouncil.DemandName(kind)+" · posłowie 20 złota",()=>
            {
                var result=Engine.SendDemand(state,host.Id,kind);if(result.Success)CloseModal();Do(result);TryAutoSave(false);
            },kind==DemandKind.PaidTransit);
            send.IsEnabled=error==null;q.Children.Add(send);
            if(error!=null)q.Children.Add(Ui.Label(error,12,Ui.Red));
            p.Children.Add(Ui.Card(q));
        }
        p.Children.Add(Ui.Label("Odpowiedź po dwóch turach; warunki są oceniane ponownie przy jej nadejściu. Przyjęcie: relacje +5. Odmowa: relacje -5. Wysłanie zużywa rozmowę w tej turze i wspólny odstęp 10 tur między poselstwami. Podróż posłów kosztuje 20 złota także przy odmowie lub przerwaniu przez wojnę. Zgoda wygasa przed rynkiem tury końcowej; nie odnawia się automatycznie.",12,Ui.Muted));
        p.Children.Add(Ui.Button("Wróć do mapy",CloseModal));Modal(p,830);
    }

    private void ShowTransitRoute(TradeContract contract,int? start=null,Unit? escort=null)
    {
        var route=Caravans.Route(state,contract,start,escort);var closed=TransitCouncil.ClosedBorders(state,contract,start,escort);
        var p=Ui.Stack(Ui.Eyebrow("GRANICE NA SZLAKU"),Ui.Label("Prawo przejazdu",30,Ui.Text,true),
            Ui.Label(Caravans.Suspension(state,contract)??(route.Count>0?"Obecnie istnieje dostępna trasa handlowa.":TransitCouncil.Blockage(state,contract,start,escort)),14,route.Count>0?Ui.Green:Ui.Gold));
        foreach(int host in closed)
        {
            p.Children.Add(Ui.Label(state.Factions[host].Name+": brak zgody na potencjalnym korytarzu.",16,Ui.Gold,true));
            p.Children.Add(Ui.Button("Rozmowa o przejeździe: "+state.Factions[host].Name,()=>ShowTransitNegotiation(state.Factions[host])));
        }
        foreach(int host in route.Select(id=>state.Tiles[id].Owner).Where(owner=>owner>=0&&owner!=contract.Buyer&&owner!=contract.Seller).Distinct())
        {
            int until=TransitCouncil.Until(state,host);
            bool treaties=state.Relation(host,contract.Buyer).Trade&&state.Relation(host,contract.Seller).Trade;
            p.Children.Add(Ui.Label(state.Factions[host].Name+(treaties?": przejazd dzięki traktatom z obiema stronami.":$": zgoda do początku T{until}; pozostało {until-state.Turn} tur."),14,Ui.Gold));
        }
        p.Children.Add(Ui.Label("Raport zamkniętych granic wskazuje państwa na jednym możliwym bezpiecznym korytarzu. Może być potrzebnych kilka zgód. Nie obiecuje dostawy po podpisaniu: terminy, embargo, wojska i granice są sprawdzane przy każdej fazie. Oczekujące karawany przepadają przy trzecim kolejnym postoju; dwie tury podróży posłów mogą minąć zbyt późno. Zgoda na tranzyt nie zezwala na przemarsz eskorty.",13,Ui.Muted));
        p.Children.Add(Ui.Button("Wróć do mapy",CloseModal));Modal(p,810);
    }
}
