using Crown.Core;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Crown.Desktop;

public sealed partial class MainWindow
{
    private async Task PowerSmoke(string folder)
    {
        Directory.CreateDirectory(folder); var checks = new List<string>();
        void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); checks.Add("PASS " + message); }
        Button Find(string text)
        {
            root.UpdateLayout(); return Descendants<Button>(root).First(b => b.Content?.ToString()?.StartsWith(text) == true || b.Content is StackPanel p && p.Children.OfType<TextBlock>().Any(t => t.Text == text));
        }
        void Click(string text)
        {
            root.UpdateLayout(); var button = Find(text); if (!button.IsEnabled) throw new InvalidOperationException("Disabled button: " + text);
            button.BringIntoView(); root.UpdateLayout(); button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); root.UpdateLayout();
        }
        async Task Capture(string name)
        {
            root.UpdateLayout(); await Task.Delay(100); var bitmap = new RenderTargetBitmap((int)root.ActualWidth, (int)root.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32); bitmap.Render(root);
            var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap)); string path = Path.Combine(folder, name + ".png"), temp = path + ".tmp";
            using (var stream = File.Create(temp)) encoder.Save(stream); File.Move(temp, path, true);
        }
        GameState Fixture()
        {
            var s = World.Create(); Narrative.Resolve(s, 0); s.Units.Clear(); foreach (var f in s.Factions) { f.Gold = 100000; f.Research = ""; }
            foreach (var c in s.Cities) { c.ProductionPaused = true; c.Project = "treasury"; s.Tiles[c.Tile].Explored = true; }
            foreach (var r in s.Relations) r.Trust = 100;
            s.Player.Technologies.UnionWith(Crown.Core.Content.Technologies.Select(t => t.Id));
            for (int i = 0; i < 18; i++) s.Player.EpochChoices[i] = 1; // No extra policy energy.
            return s;
        }
        void Turn() { CloseModal(); Advance(); }
        try
        {
            if (arguments.Contains("--smoke-force-failure")) throw new InvalidOperationException("Intentional power diagnostic failure.");
            var s = Fixture(); var a = s.Cities[0]; var tile = World.Neighbors(s, a.Tile).First(t => t.Passable); var b = Engine.AddCity(s, 0, tile.Id, "Zakole");
            b.ProductionPaused = true; b.Project = "treasury"; a.Buildings.UnionWith(["atomic_reactor", "orbital_shipyard"]); b.Buildings.Add("data_centre");
            Start(s); ShowWorldDevelopment(); root.UpdateLayout();
            Check(Find("Zarządzaj energią: " + a.Name).IsEnabled, "World development exposes actual city power management");
            Check(!Find("Zleć: Stała stacja orbitalna").IsEnabled, "Shared energy shortage initially blocks the national orbital program");
            Click("Zarządzaj energią: " + a.Name);
            Check(Descendants<TextBlock>(overlay!).Any(t => t.Text.Contains("Zakole") && t.Text.Contains("Zwykły")), "Power panel shows other consumers in the real road network");
            double science = Engine.CityYields(state, b).Science; Click("Pierwszeństwo");
            Check(a.PowerPriority == PowerPriority.Critical && a.LastPowerPriorityTurn == state.Turn, "Priority button stores the actual city directive and turn lock");
            Check(Epochs.Energy(state, a).Coverage == 1 && Epochs.Energy(state, b).Coverage < 1 && Engine.CityYields(state, b).Science < science, "Prioritising the spaceport reduces another city's real research output");
            Check(!Find("Zwykły").IsEnabled && !Find("Ograniczony").IsEnabled, "Same-turn priority alternatives are visibly unavailable");
            await Capture("power-priority-network"); Click("Energia, klimat i kosmos");
            Check(Find("Zleć: Stała stacja orbitalna").IsEnabled, "City priority enables the real program button without adding a power plant");
            Click("Zarządzaj energią: " + a.Name); double gold = state.Player.Gold; double work = a.Production;
            Click("Wyłącz: Stocznia orbitalna");
            Check(a.SuspendedBuildings.Contains("orbital_shipyard") && a.Buildings.Contains("orbital_shipyard") && state.Player.Gold == gold && a.Production == work, "Shutdown retains the physical building, treasury and accumulated production");
            Check(!Find("Uruchom: Stocznia orbitalna").IsEnabled, "Immediate restart is disabled until the next turn");
            Check(Epochs.Energy(state, b).Coverage == 1, "Shutdown reallocates the released energy to the remaining consumer");
            Click("Energia, klimat i kosmos"); Check(!Find("Zleć: Stała stacja orbitalna").IsEnabled, "Inactive required infrastructure blocks the orbital program despite ample power");
            Turn(); ShowPowerManagement(a.Id); gold = state.Player.Gold;
            Check(Find("Uruchom: Stocznia orbitalna").IsEnabled, "Next-turn control offers a paid restart");
            Click("Uruchom: Stocznia orbitalna");
            Check(!a.SuspendedBuildings.Contains("orbital_shipyard") && Math.Abs(gold - state.Player.Gold - PowerGrid.RestartCost("orbital_shipyard")) < .001, "Restart debits the displayed gold cost exactly once");
            Check(!Find("Wyłącz: Stocznia orbitalna").IsEnabled, "Restart also locks further switching during its turn");
            string path = Path.Combine(folder, "power-directives.crown"); SaveGame.Write(state, path); string json = SaveGame.Serialize(state); LoadPath(path); CloseModal();
            Check(SaveGame.Serialize(state) == json && state.Cities.First(c => c.Id == a.Id).PowerPriority == PowerPriority.Critical, "Desktop save and reload preserve power allocation, payment and locks exactly");
            ShowPowerManagement(a.Id); Check(!Find("Wyłącz: Stocznia orbitalna").IsEnabled, "Reload does not bypass the same-turn operating lock");
            s = Fixture(); a = s.Cities[0]; a.Buildings.UnionWith(["coal_station", "atomic_reactor", "data_centre"]); Start(s); ShowPowerManagement(a.Id); double emissions = Epochs.Emissions(state, 0);
            Click("Wyłącz: Elektrownia cieplna");
            Check(Epochs.Emissions(state, 0) == emissions - 6 && Epochs.Energy(state, a).Coverage == 1, "Retiring redundant coal reduces actual emissions while preserving full supply");
            Check(Math.Abs(Epochs.Upkeep(state, 0) - 17) < .001, "Economic upkeep retains the coal plant's one-gold standby cost");
            await Capture("power-retired-coal");
            s = Fixture(); a = s.Cities[0]; s.Tiles[a.Tile].River = true; Start(s); ShowProjects(a, 9); root.UpdateLayout();
            Check(Descendants<TextBlock>(overlay!).Any(t => t.Text == "Elektrownia wodna"), "Production browser includes the new geographical energy source");
            Click("Elektrownia wodna"); a.ProductionPaused = false; a.Production = 900; Turn();
            Check(a.Buildings.Contains("hydro_station") && Epochs.Energy(state, a).Supply == 45, "Renewable production button builds an actual generating facility");
            ShowPowerManagement(a.Id); Check(Find("Wyłącz: Elektrownia wodna").IsEnabled, "New renewable facilities share the same operating controls"); await Capture("power-hydropower");
            s = Fixture(); a = s.Cities[0]; a.Buildings.Add("solar_farm"); s.Tiles[a.Tile].Terrain = Terrain.Desert; s.Turn = 4; Start(s); ShowPowerManagement(a.Id); root.UpdateLayout();
            Check(Descendants<TextBlock>(overlay!).Any(t => t.Text.Contains("Moc teraz 34")), "Operating panel shows actual winter desert solar generation rather than nominal power");
            Width = 1200; Height = 800; a.Buildings.UnionWith(["orbital_shipyard", "fusion_plant", "data_centre", "robot_factory"]); ShowPowerManagement(a.Id); root.UpdateLayout();
            Check(Descendants<ScrollViewer>(overlay!).Any(v => v.ScrollableHeight > 0), "Long industrial controls remain reachable at 1200 by 800"); await Capture("power-compact");
            Width = 1600; Height = 980; ShowPowerManagement(a.Id); await Capture("power-final");
            checks.Add($"{checks.Count} power desktop integration checks passed."); File.WriteAllLines(Path.Combine(folder, "ui-smoke.txt"), checks); Close();
        }
        catch (Exception ex) { checks.Add(ex.ToString()); File.WriteAllLines(Path.Combine(folder, "ui-smoke.txt"), checks); if(!arguments.Contains("--smoke-force-failure"))await Capture("power-failure"); Application.Current.Shutdown(3); }
    }
}
