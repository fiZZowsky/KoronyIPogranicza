using Crown.Core;
using System.Windows.Controls;

namespace Crown.Desktop;

public sealed partial class MainWindow
{
    private void ShowArmy()
    {
        var p = Ui.Stack(Ui.Eyebrow("DOWÓDZTWO MARCHII"), Ui.Label("Wojsko i stałe rozkazy", 30, Ui.Text, true),
            Ui.Label("Wybierz oddział, aby obejrzeć jego pozycję i wydać rozkaz. Tab pomija okopujące się oddziały, rekonwalescentów i eskorty. Nadal można je wybrać na mapie lub tutaj.", 13, Ui.Muted));
        foreach (var unit in state.Units.Where(u => u.Owner == 0).OrderBy(u => u.Id))
        {
            var tile = state.Tiles[unit.Tile];
            string order = unit.EscortCaravanId is int convoy ? $"Eskorta #{convoy}" : unit.Destination is int target ? $"Marsz do ({state.Tiles[target].X}, {state.Tiles[target].Y})" : MilitaryOrders.Name(unit);
            p.Children.Add(Ui.Button($"#{unit.Id} · {Crown.Core.UnitModels.Name(unit)} · ({tile.X}, {tile.Y}) · zdrowie {unit.Health:0}%\n{order}",
                () => { Select(unit.Tile); selectedUnit = unit.Id; map.Center(unit.Tile); Refresh(); ShowMilitaryOrders(unit); }));
        }
        p.Children.Add(Ui.Button("Wróć do mapy", CloseModal)); Modal(p, 770);
    }

    private void ShowMilitaryOrders(Unit unit)
    {
        bool supplied = Engine.Supplied(state, unit);
        int attrition = !supplied && unit.Kind is not (UnitKind.Scout or UnitKind.Settler) ? 5 : 0;
        var tile = state.Tiles[unit.Tile];
        var p = Ui.Stack(Ui.Eyebrow("ROZKAZY ODDZIAŁU"), Ui.Label($"{Crown.Core.UnitModels.Name(unit)} · #{unit.Id}", 30, Ui.Text, true),
            Ui.Label($"Pozycja ({tile.X}, {tile.Y}) · zdrowie {unit.Health:0}% · ruch {unit.Actions:0.#}/{unit.MaxActions}", 15, Ui.Muted),
            Ui.Label(MilitaryOrders.Name(unit), 21, Ui.Gold, true),
            Ui.Label(supplied ? "Dostawy zapewnione." : "Brak dostaw: siła ×0,72. Rekonwalescencja nie zastępuje zaopatrzenia.", 13, supplied ? Ui.Green : Ui.Red),
            Ui.Label("Wydanie lub zmiana stałego rozkazu wymaga pełnego ruchu i zużywa go do końca tej tury. Zastępuje wcześniejszy marsz. Odwołanie nie zwraca ruchu. W następnej turze możesz wydać marsz lub atak, porzucając przygotowane pozycje.", 13, Ui.Muted));
        void Apply(Result result)
        { Do(result); if (result.Success) { TryAutoSave(false); ShowMilitaryOrders(unit); } }
        void AddOrder(MilitaryOrder order, string text, string description)
        {
            p.Children.Add(Ui.Rule());p.Children.Add(Ui.Label(description, 14, Ui.Text));
            string? error = MilitaryOrders.OrderError(state, unit, order);
            var button = Ui.Button(text, () => Apply(Engine.SetMilitaryOrder(state, unit.Id, order)), true); button.IsEnabled = error == null;
            p.Children.Add(button);if (error != null) p.Children.Add(Ui.Label(error, 12, Ui.Muted));
        }
        AddOrder(MilitaryOrder.Fortify, "Okop oddział", "Przygotowanie pozycji: po końcu pierwszej tury +20%, po drugiej +40% siły obronnej. Premia dotyczy oddziału, nie samego miasta; dodatek lasu lub wzgórz liczymy osobno. Ruch, atak, niszczenie drogi i przydział eskorty usuwają całą premię.");
        AddOrder(MilitaryOrder.Recover, "Rozpocznij rekonwalescencję", $"Odpoczynek do pełnego zdrowia: +{MilitaryOrders.Healing(state, unit, true):0} zdrowia/turę, do 100; następnie straty za brak dostaw: {attrition}. Zwykły odpoczynek lub okopywanie leczy +{MilitaryOrders.Healing(state, unit, false):0}. Medycyna zwiększa leczenie z dostawami. Trafienie przerywa rekonwalescencję; umocnień ten rozkaz nie daje.");
        if (unit.Order != MilitaryOrder.None) p.Children.Add(Ui.Button("Przywróć gotowość oddziału", () => Apply(Engine.WakeUnit(state, unit.Id))));
        if(state.EpochCampaign)p.Children.Add(Ui.Button("Modernizuj oddział #"+unit.Id,()=>ShowUnitModernization(unit)));
        p.Children.Add(Ui.Button("Wszystkie oddziały", ShowArmy)); p.Children.Add(Ui.Button("Wróć do mapy", CloseModal)); Modal(p, 800);
    }
}
