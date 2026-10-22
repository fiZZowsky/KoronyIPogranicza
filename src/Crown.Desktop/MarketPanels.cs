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
                Ui.Label($"{seller.Name} → {buyer.Name}",17,Ui.Text,true),Ui.Label($"{Markets.Name(contract.Good)} · do {contract.Quantity}/turę · koniec T{contract.ExpiresTurn}\n{contract.Message}",12,Ui.Muted),
                Ui.Label($"Ostatnio: {contract.LastDelivered:0.##} × {contract.LastPrice:0.00} = {contract.LastPayment:0.00} złota",12,Ui.Gold));
            if(contract.Status==ContractStatus.Active)p.Children.Add(Ui.Button("Rozwiąż umowę #"+contract.Id,()=>{Do(Engine.CancelTradeContract(state,contract.Id));TryAutoSave(false);}));
            side.Children.Add(Ui.Card(p));
        }
    }

    private void ShowCityMarket(City city)
    {
        var forecast=Markets.Plan(state).Cities[city.Id];
        var p=Ui.Stack(Ui.Eyebrow("PROGNOZA RYNKU"),Ui.Label(city.Name,32,Ui.Text,true),
            Ui.Label("Rozliczenie następuje przed wzrostem i produkcją miejską. Najpierw powstają towary, potem wykonujemy kontrakty, a następnie potrzeby mieszkańców i wymianę krajową. Nadmiar ponad pojemność przepada.",13,Ui.Muted));
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
        var p=Ui.Stack(Ui.Eyebrow("PRAWO HANDLOWE"),Ui.Label("Granice otwarte dla kogo?",32,Ui.Text,true),Ui.Label("Zmiana kosztuje 30 złota, z odstępem ośmiu tur. Już podpisane umowy nie znikają po embargu; ich czas nadal upływa.",14,Ui.Muted));
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
        var p=Ui.Stack(Ui.Eyebrow("UMOWA NA DWANAŚCIE TUR"),Ui.Label("Towary przez granicę",32,Ui.Text,true),Ui.Label("Koszt przygotowania: 15 złota. Ilość jest limitem, a cena zmienia się wraz z rynkiem sprzedawcy; cena zagraniczna wynosi 115% jego ceny lokalnej. Bez gwarancji dostawy i bez rezerwowania złota.",13,Ui.Muted));
        if(own.Length==0||foreign.Length==0)
        {
            p.Children.Add(Ui.Label("Potrzebne własne targowisko oraz targowisko państwa z pokojowym traktatem handlowym. Obie strony muszą znać Szlaki handlowe i mieć bezpieczne połączenia ze stolicami.",14,Ui.Gold));
            p.Children.Add(Ui.Button("Wróć do mapy",CloseModal));Modal(p,760);return;
        }
        var direction=Ui.Combo(["Import do Marchii","Eksport z Marchii"],0);p.Children.Add(direction);
        var home=Ui.Combo(own.Select(c=>c.Name).ToArray(),0);p.Children.Add(home);
        var partner=Ui.Combo(foreign.Select(c=>$"{c.Name} · {state.Factions[c.Owner].Name}").ToArray(),0);p.Children.Add(partner);
        var good=Ui.Combo(Markets.Goods.Select(Markets.Name).ToArray(),0);p.Children.Add(good);
        var quantity=Ui.Combo(Enumerable.Range(1,6).Select(n=>n==1?"Do 1 jednostki na turę":$"Do {n} jednostek na turę").ToArray(),1);p.Children.Add(quantity);
        var preview=Ui.Label("",13,Ui.Gold);p.Children.Add(preview);var feedback=Ui.Label("",12,Ui.Red);
        (City Buyer,City Seller) Parties()=>direction.SelectedIndex==0?(own[home.SelectedIndex],foreign[partner.SelectedIndex]):(foreign[partner.SelectedIndex],own[home.SelectedIndex]);
        void Update()
        {
            var (buyer,seller)=Parties();var commodity=Markets.Goods[good.SelectedIndex];var plan=Markets.Plan(state);
            double price=Math.Round(plan.Cities[seller.Id].First(l=>l.Good==commodity).Price*1.15,2);
            var draft=new TradeContract{Buyer=buyer.Owner,Seller=seller.Owner,BuyerCity=buyer.Id,SellerCity=seller.Id};
            preview.Text=$"{seller.Name} → {buyer.Name}\nCena orientacyjna {price:0.00}; pełna dostawa {(quantity.SelectedIndex+1)*price:0.00} złota.\n"+(Markets.RouteError(state,draft)??Markets.PolicyDescription(state.Factions[seller.Owner].TradePolicy));feedback.Text="";
        }
        foreach(var combo in new[]{direction,home,partner,good,quantity})combo.SelectionChanged+=(_,_)=>Update();Update();
        p.Children.Add(Ui.Button("Podpisz umowę · 15 złota",()=>
        {
            var (buyer,seller)=Parties();var result=Engine.SignTradeContract(state,buyer.Id,seller.Id,Markets.Goods[good.SelectedIndex],quantity.SelectedIndex+1);
            if(result.Success){CloseModal();Do(result);TryAutoSave(false);}else feedback.Text=result.Message;
        },true));p.Children.Add(feedback);p.Children.Add(Ui.Button("Wycofaj propozycję",CloseModal));Modal(p,800);
    }
}
