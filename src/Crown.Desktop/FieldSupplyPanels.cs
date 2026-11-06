using Crown.Core;

namespace Crown.Desktop;

public sealed partial class MainWindow
{
    private void ShowFieldSupply(string? notice=null)
    {
        var units=state.Units.Where(u=>u.Owner==0&&FieldLogistics.Eligible(u)).OrderBy(u=>u.Id).ToArray();
        var p=Ui.Stack(Ui.Eyebrow("KWATERMISTRZOSTWO MARCHII"),Ui.Label("Magazyny i zapasy marszowe",30,Ui.Text,true),
            Ui.Label($"Pakiety w magazynach: {state.Cities.Where(c=>c.Owner==0).Sum(c=>c.MilitarySupplies):0.#} · oddziały z rezerwą: {units.Count(u=>u.SupplyReserve>0)}/{units.Length}",15,Ui.Gold),
            Ui.Label("Magazyn wojskowy kosztuje 140 produkcji i wymaga Inżynierii. Utrzymanie: 2 złota plus administracja. Przeznacz 0%, 25% lub 50% produkcji pozostałej po dzielnicach na pakiety: 2 pracy tworzy 1 pakiet. Pełne oblężenie i wstrzymanie produkcji zatrzymują wytwarzanie.",14,Ui.Muted));
        if(notice!=null)p.Children.Add(Ui.Label(notice,14,Ui.Gold));
        if(!state.EpochCampaign)p.Children.Add(Ui.Label("Rozszerz dawną kampanię w panelu epok, aby korzystać z magazynów wojskowych.",14,Ui.Gold));
        foreach(var c in state.Cities.Where(c=>c.Owner==0).OrderBy(c=>c.Id))
        {
            bool built=FieldLogistics.Active(state,c);
            var card=Ui.Stack(Ui.Label(c.Name,23,Ui.Text,true),Ui.Label(built?$"Zapasy {c.MilitarySupplies:0.#}/{FieldLogistics.Capacity(state,c):0} pakietów · nakład {c.SupplyAllocation}%":"Magazyn nie jest zbudowany",14,Ui.Gold));
            if(!built)
            {
                var error=ProductionPlanning.ProjectError(state,c,FieldLogistics.Depot);var build=Ui.Button("Buduj magazyn: "+c.Name,()=>{var result=Engine.SetProject(state,c.Id,FieldLogistics.Depot);Do(result);if(result.Success)TryAutoSave(false);ShowFieldSupply(result.Message);},true);build.IsEnabled=error==null;card.Children.Add(build);if(error!=null)card.Children.Add(Ui.Label(error,12,Ui.Muted));
            }
            else
            {
                double production=Engine.CityYields(state,c).Production,districtWork=Districts.Allocation(state,c,production),supplyWork=FieldLogistics.Allocation(state,c,Math.Max(0,production-districtWork));
                card.Children.Add(Ui.Label($"Bieżąca prognoza: {supplyWork/2:0.##} pakietów/turę, koszt {supplyWork:0.##} pracy. Dzielnice otrzymują pracę wcześniej.",12,Ui.Muted));
                card.Children.Add(Ui.Button("Ustal nakład: "+c.Name,()=>ShowSupplyPolicy(c),true));
                if(c.ProductionPaused||SiegeWarfare.Report(state,c).Encircled)card.Children.Add(Ui.Label("Wytwarzanie zatrzymane: produkcja wstrzymana lub pełne oblężenie.",12,Ui.Red));
                else if(c.SupplyAllocation==0)card.Children.Add(Ui.Label("Wytwarzanie wyłączone. Istniejące zapasy pozostają dostępne.",12,Ui.Muted));
            }
            card.Children.Add(Ui.Button("Plan produkcji: "+c.Name,()=>ShowProductionQueue(c)));
            card.Children.Add(Ui.Button("Pokaż magazyn: "+c.Name,()=>{CloseModal();Select(c.Tile);map.Center(c.Tile);}));p.Children.Add(Ui.Card(card));
        }
        p.Children.Add(Ui.Eyebrow("ZAPASY ODDZIAŁÓW"));
        p.Children.Add(Ui.Label("Zabierz do 3 tur zapasów z własnego magazynu do dwóch przechodnich kroków lądem. Piechota i łucznicy zużywają 1 pakiet za turę rezerwy, jazda i artyleria 2. Ręczny załadunek kosztuje 1 ruchu i odwołuje marsz oraz umocnienia. Koniec tury przy dostawach i magazynie automatycznie uzupełnia zapasy według numerów oddziałów, z rzeczywistego stanu magazynu. Odcięty oddział zużywa jedną turę rezerwy na koniec tury; po jej wyczerpaniu obowiązują osłabienie i straty. Na pokładzie zapasów nie uzupełnia się ani nie zużywa. Flota, zwiad i osadnicy nie korzystają z rezerwy marszowej.",14,Ui.Muted));
        foreach(var u in units)
        {
            bool line=Logistics.HasSupplyLine(state,u);var card=Ui.Stack(Ui.Label($"#{u.Id} · {UnitModels.Name(u)}",20,Ui.Text,true),Ui.Label($"Zapasy marszowe {u.SupplyReserve}/3 · "+(!SeaTransport.Active(u)?"na pokładzie":line?"dostawy połączone":u.SupplyReserve>0?"korzysta z rezerwy":"BRAK DOSTAW I ZAPASÓW"),13,Ui.Gold));
            string? error=FieldLogistics.RefillError(state,u);var button=Ui.Button("Załaduj zapasy #"+u.Id,()=>{var result=FieldLogistics.Refill(state,u.Id);Do(result);if(result.Success)TryAutoSave(false);ShowFieldSupply(result.Message);});button.IsEnabled=error==null;card.Children.Add(button);if(error!=null)card.Children.Add(Ui.Label(error,12,Ui.Muted));
            card.Children.Add(Ui.Button("Pokaż oddział #"+u.Id,()=>{CloseModal();Select(u.Tile);selectedUnit=SeaTransport.Active(u)?u.Id:SeaTransport.Carrier(state,u)?.Id??-1;map.Center(u.Tile);Refresh();}));p.Children.Add(Ui.Card(card));
        }
        p.Children.Add(Ui.Label("Napad na dostawy: wybierz gotowy oddział na wrogiej drodze podczas wojny i otwórz Rozkazy wojskowe. W zasięgu trzech połączonych odcinków musi być widoczny magazyn z zapasami; obrońcy odcinają dojście. Napad niszczy do 20 pakietów, daje połowę wartości w złocie, zwiększa krzywdy o 8 i kończy działania. Droga pozostaje. Zdobycie miasta zachowuje połowę zapasów i wyłącza nakład nowej administracji.",14,Ui.Muted));
        p.Children.Add(Ui.Button("Wróć do mapy",CloseModal));Modal(p,900);
    }
    private void ShowSupplyPolicy(City c)
    {
        var p=Ui.Stack(Ui.Eyebrow("ROZMOWA Z KWATERMISTRZEM"),Ui.Label(c.Name+" · przygotowanie wyprawy",28,Ui.Text,true),
            Ui.Label("Kwatermistrz: „Każdy pakiet wymaga pracy rzemieślników. Jeśli zwiększymy zapasy, oddziały przetrwają odcięcie, ale budowa i rekrutacja będą wolniejsze. Jak dzielimy pracę?”",15,Ui.Text),
            Ui.Label($"Obecnie {c.SupplyAllocation}% · {c.MilitarySupplies:0.#} pakietów. Zmienisz nakład raz w turze; wybór nie tworzy zapasów natychmiast.",14,Ui.Gold));
        foreach(int share in new[]{0,25,50})
        {
            int value=share;string label=share==0?"0% · cała pozostała praca na rozwój i rekrutację":share==25?"25% · przygotuj zapasy bez zatrzymywania rozwoju":"50% · priorytet wyprawy kosztem budowy i rekrutacji";
            string? error=FieldLogistics.PolicyError(state,c,value);var b=Ui.Button(label,()=>{var result=FieldLogistics.SetAllocation(state,c.Id,value);Do(result);if(result.Success)TryAutoSave(false);ShowFieldSupply(result.Message);},true);b.IsEnabled=error==null;p.Children.Add(b);if(error!=null)p.Children.Add(Ui.Label(error,12,Ui.Muted));
        }
        p.Children.Add(Ui.Button("Wróć do magazynów",()=>ShowFieldSupply()));Modal(p,760);
    }
    private void ShowSupplyRaid(Unit u)
    {
        var raid=FieldLogistics.PreviewRaid(state,u);if(raid.Error is{} error){Toast(error,false);return;}
        var c=state.Cities.Single(c=>c.Id==raid.CityId);var p=Ui.Stack(Ui.Eyebrow("NAPAD NA DOSTAWY"),Ui.Label(c.Name,29,Ui.Text,true),
            Ui.Label($"Zniszczysz {raid.Destroyed:0.#} pakietów, zdobędziesz {raid.Loot:0} złota. Krzywdy +8. Oddział zużyje cały pozostały ruch; droga pozostanie.",15,Ui.Gold),
            Ui.Button("Wykonaj napad na dostawy",()=>{var result=FieldLogistics.Raid(state,u.Id);if(result.Success){CloseModal();Do(result);TryAutoSave(false);}else Toast(result.Message,false);},true),Ui.Button("Wycofaj rozkaz",CloseModal));Modal(p,700);
    }
}
