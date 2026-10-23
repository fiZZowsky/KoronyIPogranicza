using Crown.Core;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Crown.Desktop;

public sealed partial class MainWindow
{
    private async Task EpochSmoke(string folder)
    {
        Directory.CreateDirectory(folder); var checks = new List<string>();
        void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); checks.Add("PASS " + message); }
        void Click(string text)
        {
            root.UpdateLayout(); var b = Descendants<Button>(root).First(b => b.IsEnabled && (b.Content?.ToString()?.StartsWith(text) == true || b.Content is StackPanel p && p.Children.OfType<TextBlock>().Any(t => t.Text.Contains(text)))); b.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        }
        async Task Capture(string name)
        {
            root.UpdateLayout(); await Task.Delay(100); var bmp = new RenderTargetBitmap((int)root.ActualWidth, (int)root.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32); bmp.Render(root);
            var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bmp)); string path = Path.Combine(folder, name + ".png"), temp = path + ".tmp";
            using (var stream = File.Create(temp)) encoder.Save(stream); File.Move(temp, path, true);
        }
        GameState Fixture(int era)
        {
            var s = World.Create(); Narrative.Resolve(s, 0); s.Units.Clear();
            foreach (var f in s.Factions) { f.Gold = 100000; f.Research = ""; }
            foreach (var c in s.Cities) { c.ProductionPaused = true; c.Project = "treasury"; s.Tiles[c.Tile].Explored = true; }
            foreach (var r in s.Relations) r.Trust = 100;
            s.Player.Technologies.UnionWith(EpochCatalog.All.Take(era + 1).SelectMany(e => e.Technologies));
            for (int i = 0; i <= era; i++) s.Player.EpochChoices[i] = 2;
            return s;
        }
        void Turn() { CloseModal(); Advance(); }
        try
        {
            if (arguments.Contains("--smoke-force-failure")) throw new InvalidOperationException("Intentional epoch diagnostic failure.");
            Landing(); Click("ROZPOCZNIJ"); Check(state.EpochCampaign && state.MaxTurns == 1600 && state.Player.Era == 0, "New desktop campaign starts a full Paleolithic-to-future history");
            Click("01   Ziemia"); tab = "research"; RefreshSide(); Click("Epoki i rozwój świata"); root.UpdateLayout();
            var combo = Descendants<ComboBox>(overlay!).Single(); Check(combo.Items.Count == 18, "Era browser contains all eighteen ordered stages");
            for (int era = 0; era < 18; era++) { combo = Descendants<ComboBox>(overlay!).Single(); combo.SelectedIndex = era; root.UpdateLayout(); Check(Descendants<TextBlock>(overlay!).Any(t => t.Text == EpochCatalog.All[era].Name), "Era browser opens " + EpochCatalog.All[era].Name); }
            Check(!Descendants<Button>(overlay!).Where(b => b.Content?.ToString()?.StartsWith("Badaj:") == true).Any(b => b.IsEnabled), "Prehistoric state cannot start interstellar research from the browser"); await Capture("epochs-future-locked");
            var s = Fixture(6); s.Player.Technologies.Remove("gunpowder"); s.Player.Technologies.Remove("renaissance_methods"); s.Player.EpochChoices.Remove(6); s.Player.Science = 100000;
            Start(s); ShowEpochs(6); double science = state.Player.Science; Click("Badaj: Proch");
            Check(state.Player.Research == "gunpowder" && state.Player.Science == science, "Era research button selects a real technology without resetting the science pool"); Turn();
            Check(state.Player.Technologies.Contains("gunpowder") && state.PendingEvent == EpochCouncils.Id(6), "A real turn completes Renaissance research and opens the era's political debate");
            await Capture("epochs-renaissance-council"); Click("02   Pierwszeństwo dostaną arsenały");
            Check(state.Player.EpochChoices[6] == 1 && EpochCouncils.Policy(state.Player).Production >= 14, "Era debate stores the chosen permanent industrial and supply policy");
            ShowEpochs(6); root.UpdateLayout(); Check(Descendants<TextBlock>(overlay!).Any(t => t.Text.Contains("Przyjęto:") && t.Text.Contains("arsenały")), "Era browser shows the actually adopted historical institution"); await Capture("epochs-renaissance");
            s = Fixture(9); var u = Engine.AddUnit(s, 0, s.Cities[0].Tile, UnitKind.Spearmen, "men_at_arms"); u.Health = 65; u.Experience = 7; Start(s); selectedUnit = u.Id; selectedTile = u.Tile; tab = "realm"; Refresh();
            Click("Modernizuj oddział #" + u.Id); double gold = state.Player.Gold; Click("Modernizuj: Piechota zmotoryzowana");
            Check(u.ModelId == "motor_infantry" && u.MaxActions == 3 && u.Actions == 0 && u.Health == 65 && u.Experience == 7 && state.Player.Gold < gold, "Modernization UI pays gold, changes the physical model and retains veteran health and experience");
            CloseModal(); map.Center(u.Tile); root.UpdateLayout(); await Capture("epochs-modern-city");
            s=Fixture(8);var city=s.Cities[0];Start(s);ShowProjects(city);root.UpdateLayout();Descendants<ComboBox>(overlay!).Single().SelectedIndex=9;root.UpdateLayout();
            Check(Descendants<TextBlock>(overlay!).Any(t=>t.Text=="Elektrownia cieplna")&&!Descendants<TextBlock>(overlay!).Any(t=>t.Text=="Grota pamięci"),"Project era filter shows the requested industrial infrastructure");
            Click("Elektrownia cieplna");city.ProductionPaused=false;city.Production=550;Turn();Check(city.Buildings.Contains("coal_station")&&Epochs.Energy(state,city).Supply>=24,"An industrial building is ordered through its production button and really supplies energy after completion");
            s = Fixture(14); foreach(int era in s.Player.EpochChoices.Keys.ToArray())s.Player.EpochChoices[era]=1; city = s.Cities[0]; city.Buildings.Add("orbital_shipyard"); Start(s); ShowWorldDevelopment(); root.UpdateLayout();
            Check(!Descendants<Button>(overlay!).First(b => b.Content?.ToString()?.StartsWith("Zleć: Stała stacja") == true).IsEnabled, "Space program button is disabled when its industrial network lacks power");
            city.Buildings.Add("fusion_plant"); ShowWorldDevelopment(); root.UpdateLayout(); Check(Descendants<Button>(overlay!).First(b => b.Content?.ToString()?.StartsWith("Zleć: Stała stacja") == true).IsEnabled, "Sufficient fusion energy enables the orbital program"); await Capture("epochs-energy-and-orbit");
            Click("Zleć: Stała stacja"); Check(city.Project == "orbital_station", "Space overview starts actual city production"); city.ProductionPaused = false; city.Production = 7000; Turn();
            Check(state.Player.SpaceMilestones.Contains("orbital_station") && !city.Buildings.Contains("orbital_station"), "Real production completes a national orbital program rather than a decorative local building");
            string path = Path.Combine(folder, "epochs-orbital-save.crown"); SaveGame.Write(state, path); string before = SaveGame.Serialize(state); LoadPath(path); CloseModal();
            Check(SaveGame.Serialize(state) == before && state.Player.SpaceMilestones.Contains("orbital_station"), "Desktop reload preserves era institutions, national infrastructure and the extended campaign");
            s = Fixture(13); s.ClimatePressure = 60; s.Cities[0].Buildings.UnionWith(["fusion_plant", "climate_restoration"]); Start(s); ShowWorldDevelopment(); root.UpdateLayout();
            Check(Descendants<TextBlock>(overlay!).Any(t => t.Text.Contains("60") && t.Text.Contains("Presja klimatyczna")), "Planetary overview displays actual climate pressure and crop impact"); await Capture("epochs-climate");
            s = Fixture(17); s.Player.SpaceMilestones.UnionWith(Epochs.Programs[..4].Select(p => p.Id)); city = s.Cities[0]; city.Buildings.UnionWith(["departure_yard", "stellar_collector"]); s.Player.Stability = 90; Start(s); ShowWorldDevelopment();
            Click("Zleć: Wysłanie statku"); city.ProductionPaused = false; city.Production = 45000; Turn();
            Check(state.Ending?.Contains("naukowe") == true && state.Player.SpaceMilestones.Contains("ark_launch"), "Interstellar program completes through a real UI turn and triggers the new science victory"); await Capture("epochs-interstellar-victory");
            Click("Kontynuuj rozwój świata"); Check(state.Ending == null && state.ContinueAfterVictory, "Victory dialog allows further development of the future world");
            Start(World.CreateLegacy()); Narrative.Resolve(state, 0); ShowEpochs(); int cities = state.Cities.Count; gold = state.Player.Gold; Click("Rozszerz kampanię do 18 epok");
            Check(state.EpochCampaign && state.MaxTurns == 1600 && state.Cities.Count == cities && state.Player.Gold == gold, "Legacy campaign expansion preserves its cities and treasury");
            Start(Fixture(17)); Width = 1200; Height = 800; ShowEpochs(17); root.UpdateLayout(); await Capture("epochs-compact");
            Check(Descendants<ScrollViewer>(overlay!).Any(v => v.ScrollableHeight > 0), "Extensive future-era content remains reachable in a compact scrolling window");
            Width = 1600; Height = 980; ShowEpochs(17); await Capture("epochs-final");
            checks.Add($"{checks.Count} epoch desktop integration checks passed."); File.WriteAllLines(Path.Combine(folder, "ui-smoke.txt"), checks); Close();
        }
        catch (Exception ex) { checks.Add(ex.ToString()); File.WriteAllLines(Path.Combine(folder, "ui-smoke.txt"), checks); Application.Current.Shutdown(3); }
    }
}
