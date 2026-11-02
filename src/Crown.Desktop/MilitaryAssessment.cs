using Crown.Core;
using System.Windows.Controls;

namespace Crown.Desktop;

public sealed partial class MainWindow
{
    private void ShowMilitaryAssessment()
    {
        var units = state.Units.Where(u => u.Owner == 0 && MilitaryStrategy.Combatant(u)).ToArray();
        var threats = MilitaryStrategy.Threats(state, 0);
        int supplied = units.Count(u => Engine.Supplied(state, u));
        int ready = units.Count(u => u.Actions >= 1 && u.Order == MilitaryOrder.None && u.EscortCaravanId == null);
        var p = Ui.Stack(Ui.Eyebrow("SZTAB MARCHII · ROZPOZNANIE I OBRONA"), Ui.Label("Ocena sytuacji wojskowej", 30, Ui.Text, true),
            Ui.Label($"Oddziały bojowe: {units.Length} · gotowe do działania: {ready}\nZ dostawami: {supplied}/{units.Length} · rekonwalescencja: {units.Count(u => u.Order == MilitaryOrder.Recover)} · eskorty: {units.Count(u => u.EscortCaravanId != null)}", 15, Ui.Gold),
            Ui.Label("Raport obejmuje obecnie widoczne wrogie oddziały bojowe w promieniu pięciu pól miasta. Brak kontaktu nie oznacza bezpiecznej granicy. Wartości siły polowej są szacunkiem; teren, umocnienia i mury rozstrzyga podgląd konkretnej bitwy.", 13, Ui.Muted));
        foreach (var threat in threats.OrderByDescending(t => t.EnemyPower - t.DefensePower).ThenBy(t => t.CityId))
        {
            var city = state.Cities.Single(c => c.Id == threat.CityId); string advice = Advice(threat, city);
            var card = Ui.Stack(Ui.Label(city.Name, 23, Ui.Text, true),
                Ui.Label($"Widoczni przeciwnicy: {threat.Enemies} · siła polowa {threat.EnemyPower:0}\nWłasne oddziały w promieniu 3 pól: {threat.Defenders} · siła polowa {threat.DefensePower:0}", 13, threat.Enemies > 0 ? Ui.Gold : Ui.Muted),
                Ui.Label(advice, 14, threat.EnemyPower > threat.DefensePower ? Ui.Red : Ui.Text),
                Ui.Label($"Zdrowie miasta: {city.Health:0}% · "+(city.Buildings.Contains("walls") ? "mury istnieją" : "brak murów"), 12, Ui.Muted),
                Ui.Button("Pokaż miasto: " + city.Name, () => { CloseModal(); Select(city.Tile); map.Center(city.Tile); }),
                Ui.Button("Plan obrony i produkcji: " + city.Name, () => ShowProductionQueue(city)));
            p.Children.Add(Ui.Card(card));
        }
        foreach (var unit in units.Where(u => !Engine.Supplied(state, u)).OrderBy(u => u.Id))
        {
            p.Children.Add(Ui.Button($"Bez dostaw: #{unit.Id} · {Crown.Core.UnitModels.Name(unit)} · zdrowie {unit.Health:0}%",
                () => { CloseModal(); Select(unit.Tile); selectedUnit = unit.Id; map.Center(unit.Tile); Refresh(); ShowMilitaryOrders(unit); }));
        }
        p.Children.Add(Ui.Label("Sztab nie wydaje automatycznych rozkazów za gracza. Przy obronie porównuj lokalną siłę, zachowaj piechotę do zajmowania miast i osłaniaj artylerię. Odwrót oraz leczenie zużywają zwykłe tury i ruch.", 13, Ui.Muted));
        p.Children.Add(Ui.Button("Wojsko i stałe rozkazy", ShowArmy));p.Children.Add(Ui.Button("Wróć do mapy", CloseModal)); Modal(p, 850);
        static string Advice(CityThreat threat, City city) => threat.Enemies == 0 ? "Brak zaobserwowanych wojsk przeciwnika. Utrzymuj zwiad i rezerwę." :
            threat.Defenders == 0 ? "Miasto bez pobliskiego wsparcia polowego. Sprowadź garnizon i sprawdź rekrutację." :
            threat.EnemyPower > threat.DefensePower * 1.2 ? "Przeciwnik ma przewagę polową. Ściągnij posiłki, wykorzystaj mury i bezpieczne pozycje." :
            city.Health < 40 ? "Miasto jest mocno uszkodzone. Osłoń je przed piechotą, która może zakończyć oblężenie." :
            "Lokalna obrona ma wsparcie. Porównaj podgląd bitwy przed kontratakiem.";
    }
}
