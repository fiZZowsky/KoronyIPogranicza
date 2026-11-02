using Crown.Core;
using System.Windows.Controls;

namespace Crown.Desktop;

public sealed partial class MainWindow
{
    private void ShowFleet(string? notice=null)
    {
        var ships=state.Units.Where(u=>u.Owner==0&&Navy.IsShip(u)).OrderBy(u=>u.Id).ToArray();
        var ports=state.Cities.Where(c=>c.Owner==0&&Navy.Coastal(state,c)).OrderBy(c=>c.Id).ToArray();
        var p=Ui.Stack(Ui.Eyebrow("ADMIRALICJA MARCHII"),Ui.Label("Flota i porty",31,Ui.Text,true),
            Ui.Label($"Okręty: {ships.Length} · z dostawami: {ships.Count(u=>Engine.Supplied(state,u))}\nPorty: {ports.Count(c=>c.Buildings.Contains(Navy.Port))} · zablokowane: {ports.Count(c=>Navy.Blockaded(state,c))}",15,Ui.Gold),
            Ui.Label("Galery pływają po polach wybrzeża. Od karaki okręty przekraczają ocean i ostrzeliwują ląd. Port zapewnia +2 żywności, +4 złota, wodowanie i dostawy floty; utrzymanie portu to 2 złota plus zwykła administracja. Budowa zużywa normalną produkcję miasta.",13,Ui.Muted));
        if(notice!=null)p.Children.Add(Ui.Label(notice,13,Ui.Gold));
        if(!state.EpochCampaign)p.Children.Add(Ui.Label("Rozszerz dawną kampanię w panelu epok, aby odblokować porty i flotę.",14,Ui.Gold));
        if(ports.Length==0)p.Children.Add(Ui.Label("Nie masz miasta bezpośrednio przy akwenie. Zwiad i osadnicy pozwolą wybrać wybrzeże pod przyszły port.",14,Ui.Gold));
        foreach(var city in ports)
        {
            bool built=city.Buildings.Contains(Navy.Port),blocked=Navy.Blockaded(state,city);
            var card=Ui.Stack(Ui.Label(city.Name,23,Ui.Text,true),Ui.Label(!built?"Brak portu":blocked?"BLOKADA MORSKA · port wyłączony":"Port czynny",14,blocked?Ui.Red:Ui.Gold));
            if(blocked)card.Children.Add(Ui.Label("Wrogi okręt przy mieście wyłącza premie portu, dostawy i wodowanie. Miasto traci dodatkowo 25% podstawowego dochodu złota; utrzymanie portu pozostaje. Usuń okręt lub zakończ wojnę.",13,Ui.Muted));
            void AddProject(string id,string text)
            {
                string? error=ProductionPlanning.ProjectError(state,city,id);var button=Ui.Button(text,()=>{var result=Engine.SetProject(state,city.Id,id);Do(result);if(result.Success)TryAutoSave(false);ShowFleet(result.Message);});button.IsEnabled=error==null;card.Children.Add(button);
                if(error!=null)card.Children.Add(Ui.Label(error,12,Ui.Muted));
            }
            if(!built)AddProject(Navy.Port,"Buduj port: "+city.Name);
            else foreach(var model in Navy.Models.Where(m=>state.Player.Technologies.Contains(m.Technology)))AddProject(model.Id,$"Buduj: {model.Name} · {city.Name} · {model.Cost} produkcji");
            card.Children.Add(Ui.Button("Plan produkcji portu: "+city.Name,()=>ShowProductionQueue(city)));
            card.Children.Add(Ui.Button("Pokaż port: "+city.Name,()=>{CloseModal();Select(city.Tile);map.Center(city.Tile);}));p.Children.Add(Ui.Card(card));
        }
        p.Children.Add(Ui.Label("Zmiana rozpoczętego projektu zachowuje 80% pracy. Powtarzanie rekrutacji ustawisz w planie produkcji. Wroga flota blokuje port także bez wykonywania osobnej akcji.",12,Ui.Muted));
        foreach(var ship in ships)
        {
            var t=state.Tiles[ship.Tile];bool supplied=Engine.Supplied(state,ship);
            p.Children.Add(Ui.Button($"#{ship.Id} · {UnitModels.Name(ship)} · zdrowie {ship.Health:0}%\n({t.X}, {t.Y}) · "+(supplied?"Dostawy morskie":"BRAK DOSTAW MORSKICH"),()=>{CloseModal();Select(ship.Tile);selectedUnit=ship.Id;map.Center(ship.Tile);Refresh();ShowMilitaryOrders(ship);}));
        }
        p.Children.Add(Ui.Label("Dostawy biegną po wodzie z własnego czynnego portu; zasięg rośnie wraz z epokami. Naprawa wymaga odpoczynku, a modernizacja pełnego ruchu i bezpiecznego portu obok. Okręty nie przewożą jeszcze wojsk ani nie zajmują miast.",13,Ui.Muted));
        p.Children.Add(Ui.Button("Wróć do mapy",CloseModal));Modal(p,900);
    }
}
