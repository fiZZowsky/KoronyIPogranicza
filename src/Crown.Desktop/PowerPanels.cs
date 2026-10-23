using Crown.Core;
using System.Windows.Controls;
using Catalog = Crown.Core.Content;

namespace Crown.Desktop;

public sealed partial class MainWindow
{
    private void ShowPowerManagement(int cityId)
    {
        var city = state.Cities.FirstOrDefault(c => c.Id == cityId && c.Owner == 0);
        if (city == null) { ShowWorldDevelopment(); return; }
        var grid = PowerGrid.Plan(state, city);
        var local = grid.Cities.First(c => c.CityId == cityId);
        var p = Ui.Stack(Ui.Eyebrow("DYSPOZYCJE ENERGETYCZNE"), Ui.Label(city.Name, 30, Ui.Text, true),
            Ui.Label($"Sieć: {grid.Supply} mocy / {grid.Demand} potrzeb · rezerwa {grid.Unused:0.##}\nMiasto: {local.Allocated:0.##} / {local.Demand} · pokrycie {local.Coverage:P0}", 17, local.Coverage < 1 ? Ui.Gold : Ui.Green, true),
            Ui.Label("Pierwszeństwo → zwykły → ograniczony. Każda grupa dzieli dostępną moc proporcjonalnie do potrzeb. Zmiana przesuwa energię także w innych miastach tej sieci. Priorytet miasta i stan każdego zakładu zmienisz raz na turę.", 13, Ui.Muted));
        if (!state.EpochCampaign) p.Children.Add(Ui.Label("Zarządzanie energią wymaga rozszerzonej kampanii epok.", 14, Ui.Gold));
        if (local.Isolated) p.Children.Add(Ui.Label("Wroga blokada odcina miasto. Jego elektrownie nie zasilają sieci; priorytet nie usuwa blokady.", 14, Ui.Red));
        p.Children.Add(Ui.Eyebrow("PRIORYTET MIASTA"));
        var priorities = new StackPanel { Orientation = Orientation.Horizontal };
        foreach (var priority in new[] { PowerPriority.Critical, PowerPriority.Normal, PowerPriority.Reduced })
        {
            string? error = PowerGrid.PriorityError(state, cityId, priority);
            var b = Ui.Button(PowerGrid.PriorityName(priority) + (city.PowerPriority == priority ? " · przyjęty" : ""), () =>
            { var r = Engine.SetPowerPriority(state, cityId, priority); Do(r); if (r.Success) { TryAutoSave(false); ShowPowerManagement(cityId); } });
            b.IsEnabled = error == null; b.ToolTip = error; priorities.Children.Add(b);
        }
        p.Children.Add(priorities);
        p.Children.Add(Ui.Eyebrow("PODZIAŁ W TEJ SIECI"));
        foreach (var row in grid.Cities)
        {
            var member = state.Cities.First(c => c.Id == row.CityId);
            p.Children.Add(Ui.Label($"{member.Name} · {PowerGrid.PriorityName(member.PowerPriority)} · {row.Allocated:0.##}/{row.Demand} mocy · {row.Coverage:P0}", 14, row.Coverage < 1 ? Ui.Gold : Ui.Green));
            if (member.Id != cityId) p.Children.Add(Ui.Button("Dyspozycje: " + member.Name, () => ShowPowerManagement(member.Id)));
        }
        p.Children.Add(Ui.Eyebrow("ZAKŁADY TEGO MIASTA"));
        p.Children.Add(Ui.Label("Wyłączony zakład nie daje premii ani energii, nie potrzebuje zasilania i nie emituje. Pozostają miejsca mieszkalne i 25% jego utrzymania. Rozruch kosztuje 5% kosztu budowy w złocie, zaokrąglone w górę. Nie zmienia to planu produkcji. Bez energii działający zakład daje 25–100% premii; jego utrzymanie pozostaje pełne.", 13, Ui.Muted));
        foreach (var building in EpochCatalog.Buildings.Where(b => city.Buildings.Contains(b.Id) && PowerGrid.Controllable(b.Id)).OrderByDescending(b => b.Energy).ThenBy(b => b.Era))
        {
            bool suspended = city.SuspendedBuildings.Contains(building.Id);
            var card = Ui.Stack(Ui.Label(building.Name + (suspended ? " · wyłączony" : " · działa"), 21, suspended ? Ui.Muted : Ui.Text, true),
                Ui.Label($"Moc teraz {PowerGrid.Generation(state, city, building)} · popyt {(suspended ? 0 : building.Demand)} · utrzymanie {building.Upkeep * (suspended ? .25 : 1):0.##} złota/turę\n{EpochCatalog.Describe(building)}", 12, Ui.Muted));
            string? error = PowerGrid.OperationError(state, cityId, building.Id, !suspended);
            var button = Ui.Button((suspended ? $"Uruchom: {building.Name} · {PowerGrid.RestartCost(building.Id)} złota" : "Wyłącz: " + building.Name), () =>
            { var r = Engine.SetBuildingOperation(state, cityId, building.Id, !suspended); Do(r); if (r.Success) { TryAutoSave(false); ShowPowerManagement(cityId); } });
            button.IsEnabled = error == null; card.Children.Add(button);
            if (error != null) card.Children.Add(Ui.Label(error, 11, Ui.Gold));
            p.Children.Add(Ui.Card(card));
        }
        if (!city.Buildings.Any(PowerGrid.Controllable)) p.Children.Add(Ui.Label("Brak zakładów. Nowe źródła i przemysł budujesz w planie produkcji miasta.", 14, Ui.Muted));
        p.Children.Add(Ui.Button("Buduj infrastrukturę: " + city.Name, () => ShowProjects(city)));
        p.Children.Add(Ui.Button("Energia, klimat i kosmos", ShowWorldDevelopment)); p.Children.Add(Ui.Button("Wróć do mapy", CloseModal)); Modal(p, 950);
    }
}
