using Crown.Core;
using System.Windows.Controls;

namespace Crown.Desktop;

public sealed partial class MainWindow
{
    private void MarketsPanel()
    {
        TitlePanel("ZAPASY I WYMIANA","Rynki prowincji","Żelazo wspiera warsztaty, sól i barwniki dobrobyt. Ceny zależą od regionalnych zapasów i popytu. Prognoza rozliczenia nie rezerwuje towarów.");
        var plan=Markets.Plan(state);
        side.Children.Add(Ui.Label(Markets.PolicyName(state.Player.TradePolicy),20,Ui.Gold,true));
        side.Children.Add(Ui.Label(Markets.PolicyDescription(state.Player.TradePolicy),12,Ui.Muted));
        side.Children.Add(Ui.Button("Zmień politykę handlową",ShowTradePolicy));
        side.Children.Add(Ui.Button("Podpisz kontrakt handlowy",ShowTradeContract,true));
        var cargoes=state.Caravans.Where(c=>{var t=state.TradeContracts.First(t=>t.Id==c.ContractId);return t.Buyer==0||t.Seller==0;}).ToArray();
        side.Children.Add(Ui.Label($"Karawany w drodze: {cargoes.Count(Caravans.Active)} · dostarczone: {cargoes.Count(c=>c.Status==CaravanStatus.Delivered)} · utracone: {cargoes.Count(c=>c.Status==CaravanStatus.Lost)}",12,Ui.Gold));
        side.Children.Add(Ui.Button("Karawany i trasy",ShowCaravans));
        if(map.TrackedCaravanId>=0)side.Children.Add(Ui.Button("Zakończ śledzenie trasy",()=>{map.TrackedCaravanId=-1;map.InvalidateVisual();RefreshSide();}));
        side.Children.Add(Ui.Label($"Ostatni bilans kontraktów: {Ui.Signed(Markets.LastTradeBalance(state,0))} złota\nPodatki rynkowe: {state.Cities.Where(c=>c.Owner==0).Sum(c=>c.LastMarketTax):0.##} złota",12,Ui.Muted));
        side.Children.Add(Ui.Rule());
        foreach(var c in state.Cities.Where(c=>c.Owner==0).OrderBy(c=>c.Id))
        {
            var p=Ui.Stack(Ui.Label(c.Name,23,Ui.Text,true),Ui.Label($"Pojemność: {Markets.Capacity(c):0} na rodzaj towaru",12,Ui.Muted));
            foreach(var line in plan.Cities[c.Id])
                p.Children.Add(Ui.Label($"{Markets.Name(line.Good)} · cena {line.Price:0.00}\nZapas {line.Opening:0.##} · produkcja {line.Produced:0.##} · popyt {line.Demand:0.##}",12,Ui.Muted));
            Ui.Gap(p,10);p.Children.Add(Ui.Button("Bilans towarów: "+c.Name,()=>{map.Center(c.Tile);ShowCityMarket(c);}));side.Children.Add(Ui.Card(p));
        }
        side.Children.Add(Ui.Eyebrow("KONTRAKTY ZAGRANICZNE"));
        var contracts=state.TradeContracts.Where(c=>c.Buyer==0||c.Seller==0).OrderByDescending(c=>c.Status==ContractStatus.Active).ThenByDescending(c=>c.Id).Take(12).ToArray();
        if(contracts.Length==0)side.Children.Add(Ui.Label("Kontrakty wymagają traktatu handlowego, targowisk, Szlaków handlowych i bezpiecznego połączenia obu miast ze stolicami. Limit: trzy aktywne umowy na państwo.",12,Ui.Muted));
        foreach(var contract in contracts)
        {
            var buyer=state.Cities.First(c=>c.Id==contract.BuyerCity);var seller=state.Cities.First(c=>c.Id==contract.SellerCity);
            var p=Ui.Stack(Ui.Eyebrow($"UMOWA #{contract.Id} · {(contract.Status==ContractStatus.Active?"AKTYWNA":contract.Status==ContractStatus.Expired?"WYGASŁA":"ROZWIĄZANA")}"),
                Ui.Label($"{seller.Name} → {buyer.Name}",17,Ui.Text,true),Ui.Label($"{Markets.Name(contract.Good)} · do {contract.Quantity}/karawanę · koniec T{contract.ExpiresTurn}\n{contract.Message}",12,Ui.Muted),
                Ui.Label($"Ostatni załadunek: {contract.LastDelivered:0.##} × {contract.LastPrice:0.00} = {contract.LastPayment:0.00} złota",12,Ui.Gold));
            if(contract.Status==ContractStatus.Active)p.Children.Add(Ui.Button("Rozwiąż umowę #"+contract.Id,()=>{Do(Engine.CancelTradeContract(state,contract.Id));TryAutoSave(false);}));
            side.Children.Add(Ui.Card(p));
        }
    }

    private void ShowCityMarket(City city)
    {
        var forecast=Markets.Plan(state).Cities[city.Id];
        var p=Ui.Stack(Ui.Eyebrow("PROGNOZA RYNKU"),Ui.Label(city.Name,32,Ui.Text,true),
            Ui.Label("Rozliczenie następuje przed wzrostem i produkcją miejską. Powstają towary, przybywają karawany i ładujemy następne. Potem zaspokajamy potrzeby mieszkańców i wymianę krajową. Import w bilansie oznacza przybyły ładunek, eksport — wysłany. Nadmiar ponad pojemność przepada.",13,Ui.Muted));
        foreach(var line in forecast)
        {
            var q=Ui.Stack(Ui.Label($"{Markets.Name(line.Good)} · {line.Price:0.00} złota",23,Ui.Gold,true));
            q.Children.Add(Ui.Label($"Zapas {line.Opening:0.##} + produkcja {line.Produced:0.##}\nKraj: import {line.DomesticIn:0.##}, eksport {line.DomesticOut:0.##} · zagranica: import {line.ForeignIn:0.##}, eksport {line.ForeignOut:0.##}\nZużycie {line.Consumed:0.##}/{line.Demand:0.##} · utrata {line.Lost:0.##} · pozostanie {line.Closing:0.##}/{Markets.Capacity(city):0}",13,Ui.Muted));
            Ui.Bar(q,$"Zaspokojenie popytu {line.Coverage*100:0}%",line.Coverage,1,line.Coverage<.5?Ui.Red:Ui.Green);p.Children.Add(Ui.Card(q));
        }
        p.Children.Add(Ui.Label($"Ostatnie rozliczenie: {(city.LastMarketTurn==0?"brak":"tura "+city.LastMarketTurn)} · podatek {city.LastMarketTax:0.00}\nWpływ ostatnich dostaw: cel dobrobytu {Markets.ProsperityEffect(city):+0;-0;0}, produkcja ×{Markets.IndustryMultiplier(city):0.00}.",13,Ui.Gold));
        p.Children.Add(Ui.Label("Skład towarowy dodaje 100 miejsc na każdy towar. Targowisko dodaje 25 do początkowych 50. Żywność nadal ma własne spichlerze i system przesyłek.",12,Ui.Muted));
        p.Children.Add(Ui.Button("Wybierz budowę",()=>ShowProjects(city)));p.Children.Add(Ui.Button("Wróć do mapy",CloseModal));Modal(p,810);
    }

    private void ShowTradePolicy()
    {
        var p=Ui.Stack(Ui.Eyebrow("PRAWO HANDLOWE"),Ui.Label("Granice otwarte dla kogo?",32,Ui.Text,true),Ui.Label("Zmiana kosztuje 30 złota, z odstępem ośmiu tur. Embargo zatrzymuje również wysłane karawany. Po trzech kolejnych turach postoju ładunek przepada bez zwrotu płatności. Czas umów nadal upływa.",14,Ui.Muted));
        var feedback=Ui.Label("",12,Ui.Red);
        foreach(var policy in Enum.GetValues<TradePolicy>())
        {
            p.Children.Add(Ui.Label(Markets.PolicyName(policy),22,Ui.Gold,true));p.Children.Add(Ui.Label(Markets.PolicyDescription(policy),13,Ui.Muted));
            var button=Ui.Button("Przyjmij: "+Markets.PolicyName(policy),()=>{var result=Engine.SetTradePolicy(state,policy);if(result.Success){CloseModal();Do(result);TryAutoSave(false);}else feedback.Text=result.Message;});
            button.IsEnabled=policy!=state.Player.TradePolicy&&state.Turn-state.Player.LastTradePolicyTurn>=8&&state.Player.Gold>=30&&state.PendingEvent!="market_charter";p.Children.Add(button);
        }
        p.Children.Add(feedback);p.Children.Add(Ui.Button("Wróć do mapy",CloseModal));Modal(p,760);
    }

    private void ShowTradeContract()
    {
        var own=state.Cities.Where(c=>c.Owner==0&&c.Buildings.Contains("market")).OrderBy(c=>c.Id).ToArray();
        var foreign=state.Cities.Where(c=>c.Owner!=0&&c.Buildings.Contains("market")&&state.Relation(0,c.Owner).Trade&&!state.Relation(0,c.Owner).War).OrderBy(c=>c.Id).ToArray();
        var p=Ui.Stack(Ui.Eyebrow("UMOWA NA DWANAŚCIE TUR"),Ui.Label("Towary przez granicę",32,Ui.Text,true),Ui.Label("Przygotowanie: 15 złota. Jedna karawana w drodze na umowę; kolejny załadunek po zakończeniu podróży poprzedniej. Zakup płatny przy wysyłce, po 115% bieżącej ceny sprzedawcy. Nabywca ponosi ryzyko utraty; brak zwrotu. Rozwiązanie i wygaśnięcie umowy nie zawracają wysłanych transportów.",13,Ui.Muted));
        if(own.Length==0||foreign.Length==0)
        {
            p.Children.Add(Ui.Label("Potrzebne własne targowisko oraz targowisko państwa z pokojowym traktatem handlowym. Obie strony muszą znać Szlaki handlowe i mieć bezpieczne połączenia ze stolicami.",14,Ui.Gold));
            p.Children.Add(Ui.Button("Wróć do mapy",CloseModal));Modal(p,760);return;
        }
        var direction=Ui.Combo(["Import do Marchii","Eksport z Marchii"],0);p.Children.Add(direction);
        var home=Ui.Combo(own.Select(c=>c.Name).ToArray(),0);p.Children.Add(home);
        var partner=Ui.Combo(foreign.Select(c=>$"{c.Name} · {state.Factions[c.Owner].Name}").ToArray(),0);p.Children.Add(partner);
        var good=Ui.Combo(Markets.Goods.Select(Markets.Name).ToArray(),0);p.Children.Add(good);
        var quantity=Ui.Combo(Enumerable.Range(1,6).Select(n=>$"Do {n} jednostek na karawanę").ToArray(),1);p.Children.Add(quantity);
        var preview=Ui.Label("",13,Ui.Gold);p.Children.Add(preview);var feedback=Ui.Label("",12,Ui.Red);
        (City Buyer,City Seller) Parties()=>direction.SelectedIndex==0?(own[home.SelectedIndex],foreign[partner.SelectedIndex]):(foreign[partner.SelectedIndex],own[home.SelectedIndex]);
        void Update()
        {
            var (buyer,seller)=Parties();var commodity=Markets.Goods[good.SelectedIndex];var plan=Markets.Plan(state);
            double price=Math.Round(plan.Cities[seller.Id].First(l=>l.Good==commodity).Price*1.15,2);
            var draft=new TradeContract{Buyer=buyer.Owner,Seller=seller.Owner,BuyerCity=buyer.Id,SellerCity=seller.Id};
            int turns=Caravans.TravelTurns(state,Caravans.Route(state,draft));
            preview.Text=$"{seller.Name} → {buyer.Name}\nCena orientacyjna {price:0.00}; pełny ładunek {(quantity.SelectedIndex+1)*price:0.00} złota.\nPodróż od wysyłki: {(turns<0?"trasa niedostępna":$"około {turns} tur przy obecnej trasie")}.\n"+(Markets.RouteError(state,draft)??Markets.PolicyDescription(state.Factions[seller.Owner].TradePolicy));feedback.Text="";
        }
        foreach(var combo in new[]{direction,home,partner,good,quantity})combo.SelectionChanged+=(_,_)=>Update();Update();
        p.Children.Add(Ui.Button("Podpisz umowę · 15 złota",()=>
        {
            var (buyer,seller)=Parties();var result=Engine.SignTradeContract(state,buyer.Id,seller.Id,Markets.Goods[good.SelectedIndex],quantity.SelectedIndex+1);
            if(result.Success){CloseModal();Do(result);TryAutoSave(false);}else feedback.Text=result.Message;
        },true));p.Children.Add(feedback);p.Children.Add(Ui.Button("Wycofaj propozycję",CloseModal));Modal(p,800);
    }

    private void ShowCaravans()
    {
        var p=Ui.Stack(Ui.Eyebrow("ŁADUNKI I BEZPIECZEŃSTWO"),Ui.Label("Karawany Marchii",32,Ui.Text,true),
            Ui.Label("Karawana ma 3 punkty ruchu na turę: droga kosztuje 0,5, trudny teren 2, pozostały 1. Wyrusza turę po załadunku. Szuka bezpiecznej trasy; tranzyt wymaga traktatów trzeciego państwa z obiema stronami. Oddział każdej ze stron (również zwiadowcy, bez osadników) w promieniu jednego pola osłania przed sąsiednim wrogiem. Wrogi oddział na polu karawany przechwytuje ją zawsze. Trzy tury postoju lub zmiana właściciela miasta oznaczają utratę bez zwrotu płatności.",13,Ui.Muted));
        var cargoes=state.Caravans.Where(c=>{var t=state.TradeContracts.First(t=>t.Id==c.ContractId);return t.Buyer==0||t.Seller==0;})
            .OrderByDescending(Caravans.Active).ThenByDescending(c=>c.Id).Take(16).ToArray();
        if(cargoes.Length==0)p.Children.Add(Ui.Label("Nie wysłano jeszcze karawan. Podpisz kontrakt i zakończ turę, aby rozpocząć załadunek.",16,Ui.Gold));
        foreach(var cargo in cargoes)
        {
            var contract=state.TradeContracts.First(t=>t.Id==cargo.ContractId);var tile=state.Tiles[cargo.Tile];
            var route=Caravans.Route(state,contract,cargo.Tile);int eta=Caravans.TravelTurns(state,route);
            string arrival=Caravans.Suspension(state,contract)!=null||eta<0?"Trasa wstrzymana":$"Pozostało około {eta} tur (bez nowych blokad)";
            var q=Ui.Stack(Ui.Eyebrow($"KARAWANA #{cargo.Id} · {Caravans.StatusName(cargo.Status).ToUpperInvariant()}"),
                Ui.Label($"{state.Cities.First(c=>c.Id==contract.SellerCity).Name} → {state.Cities.First(c=>c.Id==contract.BuyerCity).Name}",22,Ui.Text,true),
                Ui.Label($"{Markets.Name(contract.Good)} {cargo.Quantity:0.##} · zapłacono {cargo.Payment:0.00} · wysłano T{cargo.DepartedTurn}\n{cargo.Message}",13,cargo.Status==CaravanStatus.Lost?Ui.Red:Ui.Muted));
            if(Caravans.Active(cargo))
            {
                if(Caravans.AtRisk(state,contract,cargo.Tile))q.Children.Add(Ui.Label("ZAGROŻENIE: przechwycenie przy następnym rozliczeniu. Odsuń przeciwnika lub sprowadź osłonę przed końcem tury.",13,Ui.Red));
                q.Children.Add(Ui.Label($"Pozycja ({tile.X}, {tile.Y}) · postój {cargo.BlockedTurns}/{Caravans.MaxBlockedTurns}\n{arrival} · {(Caravans.Guarded(state,contract,cargo.Tile)?"pod osłoną":"bez osłony")}",13,Ui.Gold));
                q.Children.Add(Ui.Button("Pokaż na mapie #"+cargo.Id,()=>{CloseModal();map.TrackedCaravanId=cargo.Id;map.Center(cargo.Tile);tab="markets";RefreshSide();}));
            }
            p.Children.Add(Ui.Card(q));
        }
        p.Children.Add(Ui.Button("Wróć do mapy",CloseModal));Modal(p,850);
    }
}
