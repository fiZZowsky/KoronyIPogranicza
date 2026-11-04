using Crown.Core;

namespace Crown.Desktop;

public sealed partial class MainWindow
{
    private void ShowSieges()
    {
        var sight=Engine.Visible(state);
        var p=Ui.Stack(Ui.Eyebrow("SZTAB OPERACJI WYBRZEŻNYCH"),Ui.Label("Oblężenia i przyczółki",30,Ui.Text,true),
            Ui.Label("Dwa zajęte kierunki lądowe ograniczają naprawy miasta do +2 zdrowia na turę. Trzy kierunki wstrzymują naprawy; nad wodą wymagają też okrętu blokującego wybrzeże. Kilka oddziałów na jednym polu to jeden kierunek. Oddział musi mieć przynajmniej 25% zdrowia. Uszkodzenia miasta osłabiają premię murów.",14,Ui.Muted));
        foreach(var city in state.Cities.Where(c=>c.Owner==0||sight.Contains(c.Tile)).OrderBy(c=>c.Id))
        {
            var siege=SiegeWarfare.Report(state,city);
            var card=Ui.Stack(Ui.Label(city.Name,23,Ui.Text,true),Ui.Label($"Zdrowie {city.Health:0}% · kierunki lądowe {siege.Fronts} · naprawy +{siege.Repair}/turę",14,Ui.Gold),
                Ui.Label(siege.Encircled?"PEŁNE OBLĘŻENIE · naprawy zatrzymane":siege.SeaClosed?"Wybrzeże blokowane":siege.Fronts>=2?"Presja oblężnicza":"Brak pełnego oblężenia",13,siege.Encircled?Ui.Red:Ui.Muted));
            card.Children.Add(Ui.Button("Pokaż miasto: "+city.Name,()=>{CloseModal();Select(city.Tile);map.Center(city.Tile);}));p.Children.Add(Ui.Card(card));
        }
        p.Children.Add(Ui.Eyebrow("PRZYCZÓŁKI ZAOPATRYWANE Z MORZA"));
        p.Children.Add(Ui.Label("Okop piechotę na brzegu i utrzymaj ją przez co najmniej jeden koniec tury. Piechota i sąsiedni własny okręt muszą mieć minimum 40% zdrowia. Okręt potrzebuje dostaw z portu i wolnych sąsiednich wód. Przyczółek dostarcza zapasy oddziałom do dwóch kroków lądem; góry, woda, obce wojska i miasta odcinają trasę. Nie zastępuje portu ani sieci handlowej.",14,Ui.Muted));
        var guards=SiegeWarfare.Beachheads(state,0).OrderBy(u=>u.Id).ToArray();
        if(guards.Length==0)p.Children.Add(Ui.Label("Brak czynnych przyczółków. Wysadź piechotę, zabezpiecz brzeg i osłoń przewoźnika.",14,Ui.Gold));
        foreach(var guard in guards)p.Children.Add(Ui.Button($"Przyczółek #{guard.Id} · ({state.Tiles[guard.Tile].X}, {state.Tiles[guard.Tile].Y})",()=>{CloseModal();Select(guard.Tile);selectedUnit=guard.Id;map.Center(guard.Tile);Refresh();}));
        p.Children.Add(Ui.Button("Flota i porty",()=>ShowFleet()));p.Children.Add(Ui.Button("Wróć do mapy",CloseModal));Modal(p,860);
    }
}
