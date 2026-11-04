using Crown.Core;
using System.Windows.Controls;

namespace Crown.Desktop;

public sealed partial class MainWindow
{
    private int? landingPassenger;
    private void ShowTransport(Unit ship,string? notice=null)
    {
        var cargo=SeaTransport.Passengers(state,ship).ToArray();
        var p=Ui.Stack(Ui.Eyebrow("TRANSPORT MORSKI · MANIFEST OKRĘTU"),Ui.Label(UnitModels.Name(ship),30,Ui.Text,true),
            Ui.Label($"Ładunek {SeaTransport.Used(state,ship)}/{SeaTransport.Capacity(ship)} · ruch okrętu {ship.Actions:0.#}/{ship.MaxActions}",16,Ui.Gold),
            Ui.Label("Załadunek wymaga oddziału w czynnym własnym porcie i okrętu obok. Zwykły załadunek lub wyładunek kosztuje 1 ruchu okrętu; szturm zużywa cały pozostały ruch. Piechota, zwiad i osadnicy zajmują jedno miejsce; jazda i artyleria dwa. Zatopienie okrętu oznacza utratę wszystkich przewożonych oddziałów.",13,Ui.Muted));
        if(notice!=null)p.Children.Add(Ui.Label(notice,13,Ui.Gold));
        foreach(var passenger in cargo)
        {
            var card=Ui.Stack(Ui.Label($"{UnitModels.Name(passenger)} · #{passenger.Id}",21,Ui.Text,true),Ui.Label($"Zdrowie {passenger.Health:0}% · miejsca {SeaTransport.Weight(passenger)}. Na pokładzie nie walczy, nie zwiaduje i nie odzyskuje własnego ruchu.",12,Ui.Muted));
            int legal=state.Tiles.Count(t=>SeaTransport.LandError(state,passenger,t.Id)==null||AmphibiousAssault.Preview(state,passenger.Id,t.Id).Valid);var button=Ui.Button("Wybierz brzeg dla #"+passenger.Id,()=>BeginLanding(passenger),true);button.IsEnabled=legal>0;card.Children.Add(button);
            card.Children.Add(Ui.Label(legal>0?$"Dostępne pola lądowania: {legal}. Po wyładunku oddział działa od następnej tury.":"Brak wolnego legalnego brzegu lub ruchu okrętu. Podejdź do lądu i sprawdź granice oraz obrońców.",12,Ui.Muted));p.Children.Add(Ui.Card(card));
        }
        p.Children.Add(Ui.Eyebrow("ODDZIAŁY W SĄSIEDNICH WŁASNYCH PORTACH"));
        var units=state.Units.Where(u=>u.Owner==0&&SeaTransport.Active(u)&&!Navy.IsShip(u)&&state.Cities.Any(c=>c.Owner==0&&c.Tile==u.Tile&&c.Buildings.Contains(Navy.Port)&&World.Distance(state.Tiles[c.Tile],state.Tiles[ship.Tile])==1)).OrderBy(u=>u.Id).ToArray();
        if(units.Length==0)p.Children.Add(Ui.Label("Brak oddziałów przy tym okręcie. Sprowadź je do portu.",13,Ui.Muted));
        foreach(var unit in units)
        {
            string? error=SeaTransport.BoardError(state,unit,ship);var button=Ui.Button($"Zaokrętuj #{unit.Id} · {UnitModels.Name(unit)} · {SeaTransport.Weight(unit)} miejsc",()=>{var result=SeaTransport.Embark(state,unit.Id,ship.Id);Do(result);if(result.Success)TryAutoSave(false);ShowTransport(ship,result.Message);});button.IsEnabled=error==null;p.Children.Add(button);if(error!=null)p.Children.Add(Ui.Label(error,12,Ui.Muted));
        }
        p.Children.Add(Ui.Label("Piechota i jazda mogą szturmować widocznych obrońców oraz miasta na sąsiednim brzegu, z siłą ×0,65 i kontratakiem. Najpierw osłab obronę ostrzałem floty. Brzeg zajmiesz po pokonaniu wszystkich obrońców; ocalały oddział po nieudanym szturmie pozostaje na pokładzie. Osadnicy, zwiad i artyleria wymagają wolnego brzegu. Po lądowaniu działania od następnej tury.",13,Ui.Muted));
        p.Children.Add(Ui.Button("Pokaż okręt",()=>{CloseModal();Select(ship.Tile);selectedUnit=ship.Id;map.Center(ship.Tile);Refresh();}));
        p.Children.Add(Ui.Button("Flota i porty",()=>ShowFleet()));p.Children.Add(Ui.Button("Wróć do mapy",CloseModal));Modal(p,880);
    }
    private void BeginLanding(Unit unit)
    {
        CloseModal();landingPassenger=unit.Id;map.LandingSites=state.Tiles.Where(t=>SeaTransport.LandError(state,unit,t.Id)==null||AmphibiousAssault.Preview(state,unit.Id,t.Id).Valid).Select(t=>t.Id).ToArray();
        map.LandingPlanningText=$"Wyładunek: {UnitModels.Name(unit)}\nWskaż zielony brzeg. Esc anuluje wybór.";map.Center(unit.Tile);map.InvalidateVisual();
    }
    private void CancelLanding()
    {landingPassenger=null;if(map!=null){map.LandingSites=[];map.LandingPlanningText="";map.InvalidateVisual();}Toast("Anulowano wybór brzegu.");}
    private bool HandleLandingSelection(int tile)
    {
        if(landingPassenger is not int id)return false;var unit=state.Units.FirstOrDefault(u=>u.Id==id);
        var assault=AmphibiousAssault.Preview(state,id,tile);
        if(assault.Valid)
        {
            var panel=Ui.Stack(Ui.Eyebrow("SZTURM Z MORZA"),Ui.Label(assault.Attacker+" → "+assault.Target,26,Ui.Text,true),
                Ui.Label($"Przewidywane straty: obrońcy −{assault.DamageToTarget:0.#}, atakujący −{assault.DamageToAttacker:0.#}. Przewoźnik i oddział kończą działania w tej turze.",15,Ui.Gold),
                Ui.Label(assault.CanCapture?"Ten szturm może zdobyć miasto.":"Lądowanie nastąpi tylko po oczyszczeniu pola ze wszystkich obrońców. Miasto z garnizonem wymaga kolejnego ataku.",14,Ui.Muted));
            foreach(var modifier in assault.Modifiers??[])panel.Children.Add(Ui.Label(modifier,12,Ui.Muted));
            panel.Children.Add(Ui.Button("Rozpocznij szturm z morza",()=>{var result=AmphibiousAssault.Execute(state,id,tile);if(result.Success){CloseModal();CancelLanding();selectedTile=tile;selectedUnit=state.Units.Any(u=>u.Id==id&&SeaTransport.Active(u))?id:SeaTransport.Carrier(state,unit!)?.Id??-1;Do(result);TryAutoSave(false);}else Toast(result.Message,false);},true));
            panel.Children.Add(Ui.Button("Wybierz inny brzeg",CloseModal));panel.Children.Add(Ui.Button("Anuluj wyładunek",()=>{CloseModal();CancelLanding();}));Modal(panel,760);return true;
        }
        if(SeaTransport.LandError(state,unit,tile) is{} error){Toast(error,false);return true;}
        var t=state.Tiles[tile];var p=Ui.Stack(Ui.Eyebrow("WYŁADUNEK I DESANT"),Ui.Label(UnitModels.Name(unit!),28,Ui.Text,true),
            Ui.Label($"Brzeg ({t.X}, {t.Y}). Okręt zużyje 1 ruchu. Oddział rozpocznie działania w następnej turze.",14,Ui.Gold),
            Ui.Button("Wysadź oddział na brzeg",()=>{var result=SeaTransport.Disembark(state,id,tile);if(result.Success){CloseModal();CancelLanding();selectedTile=tile;selectedUnit=id;Do(result);TryAutoSave(false);}else Toast(result.Message,false);},true),
            Ui.Button("Wybierz inny brzeg",CloseModal),Ui.Button("Anuluj wyładunek",()=>{CloseModal();CancelLanding();}));Modal(p,650);return true;
    }
}
