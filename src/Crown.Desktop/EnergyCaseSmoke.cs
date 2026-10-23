using Crown.Core;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Crown.Desktop;

public sealed partial class MainWindow
{
    private async Task EnergyCaseSmoke(string folder)
    {
        Directory.CreateDirectory(folder); var checks = new List<string>();
        void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); checks.Add("PASS " + message); }
        Button Find(string text)
        {
            root.UpdateLayout(); return Descendants<Button>(root).First(b => b.Content?.ToString()?.StartsWith(text) == true || b.Content is StackPanel p && p.Children.OfType<TextBlock>().Any(t => t.Text.StartsWith(text)));
        }
        void Click(string text)
        {
            var button = Find(text); if (!button.IsEnabled) throw new InvalidOperationException("Disabled: " + text);
            button.BringIntoView(); root.UpdateLayout(); button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); root.UpdateLayout();
        }
        async Task Capture(string name)
        {
            root.UpdateLayout(); await Task.Delay(100); var bitmap = new RenderTargetBitmap((int)root.ActualWidth, (int)root.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32); bitmap.Render(root);
            var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap)); string path = Path.Combine(folder, name + ".png"), temp = path + ".tmp";
            using (var stream = File.Create(temp)) encoder.Save(stream); File.Move(temp, path, true);
        }
        GameState Fixture(bool ready = false)
        {
            var s = World.Create(); Narrative.Resolve(s, 0); s.Units.Clear(); foreach (var f in s.Factions) { f.Gold = 100000; f.Research = ""; }
            foreach (var city in s.Cities) { city.ProductionPaused = true; city.Project = "treasury"; s.Tiles[city.Tile].Explored = true; }
            foreach (var r in s.Relations) r.Trust = 100;
            s.Player.Technologies.UnionWith(Crown.Core.Content.Technologies.Select(t => t.Id)); for (int i = 0; i < 18; i++) s.Player.EpochChoices[i] = 1;
            s.SeenEvents.UnionWith(Narrative.All.Select(e => e.Id)); s.Cities[0].Buildings.Add("data_centre");
            if (ready) for (int i = 0; i < 3; i++) Engine.EndTurn(s);
            return s;
        }
        void Turn() { CloseModal(); Advance(); root.UpdateLayout(); }
        try
        {
            if (arguments.Contains("--smoke-force-failure")) throw new InvalidOperationException("Intentional energy case diagnostic failure.");
            Start(Fixture()); ShowWorldDevelopment(); Click("Kryzysy i obietnice energetyczne");
            Check(Descendants<TextBlock>(overlay!).Any(t => t.Text.Contains("Brak zgłoszonych kryzysów")), "World overview exposes an initially empty energy petition register");
            Turn(); Turn(); Check(state.EnergyCases.Count == 0 && state.Cities[0].EnergyHistory.Count == 2, "Two actual desktop turns record history without opening an early petition");
            Turn(); var c = state.EnergyCases.Single(); var city = state.Cities[0];
            Check(state.PendingEvent == EnergyCouncil.Event.Id && state.PendingEnergyCaseId == c.Id && state.Turn == 4, "Third real shortage turn opens the contextual city energy council");
            Check(Descendants<TextBlock>(overlay!).Any(t => t.Text.Contains("Ira Senn")) && Descendants<TextBlock>(overlay!).Any(t => t.Text.Contains("Centrum danych")), "Energy dialogue identifies its speaker and the actual affected service");
            Check(Find("02   Zapewnimy trzy kolejne fazy").IsEnabled, "A free verifiable recovery pledge is selectable"); await Capture("energy-crisis-council");
            string path = Path.Combine(folder, "energy-pending.crown"); SaveGame.Write(state, path); LoadPath(path);
            Check(state.PendingEvent == EnergyCouncil.Event.Id && Find("01   Przeznaczymy").IsEnabled, "Reload reopens the correct pending delegation");
            double gold = state.Player.Gold; int cost = state.EnergyCases.Single().ReliefCost; Click("01   Przeznaczymy");
            Check(state.Player.Gold == gold - cost && state.EnergyCases.Single().Status == EnergyCaseStatus.Relief && Epochs.Energy(state, state.Cities[0]).Coverage == 0, "Relief option pays the quoted amount and leaves the power shortage real");
            ShowEnergyCases(); root.UpdateLayout(); Check(Descendants<TextBlock>(overlay!).Any(t => t.Text == "Wypłacono pomoc"), "Register retains the actual relief decision");
            Start(Fixture(true)); ShowCouncil(); Click("02   Zapewnimy trzy kolejne fazy"); c = state.EnergyCases.Single(); city = state.Cities[0];
            Check(c.Status == EnergyCaseStatus.Pledged && c.DeadlineTurn == 10 && c.Deliveries.Count == 0, "Promise decision records its deadline without crediting forecasts");
            ShowEnergyCases(); Click("Napraw zasilanie: " + city.Name);
            Check(Descendants<TextBlock>(overlay!).Any(t => t.Text.Contains("T3: 0/25")), "Operating panel displays real earlier economic readings");
            Click("Buduj infrastrukturę: " + city.Name); ShowProjects(city, 10); root.UpdateLayout(); Click("Elektrownia jądrowa"); city.Production = 900; city.ProductionPaused = false; Turn();
            Check(city.Buildings.Contains("atomic_reactor") && c.Deliveries.Count == 1 && EnergyCouncil.Progress(c) == 0 && Epochs.Energy(state, city).Coverage == 1, "Plant construction changes the forecast but does not rewrite that turn's unpowered delivery");
            Turn(); Turn(); Check(c.Status == EnergyCaseStatus.Pledged && EnergyCouncil.Progress(c) == 2, "Two later fully powered desktop phases build a genuine consecutive recovery streak");
            path = Path.Combine(folder, "energy-mid-pledge.crown"); SaveGame.Write(state, path); string json = SaveGame.Serialize(state); LoadPath(path); CloseModal();
            Check(SaveGame.Serialize(state) == json && EnergyCouncil.Progress(state.EnergyCases.Single()) == 2, "Mid-pledge desktop reload preserves the exact evidence and progress");
            Turn(); c = state.EnergyCases.Single(); Check(c.Status == EnergyCaseStatus.Fulfilled && c.ResolvedTurn == 8, "Third full phase completes the real political commitment");
            ShowEnergyCases(); root.UpdateLayout(); Check(Descendants<TextBlock>(overlay!).Any(t => t.Text == "Przywrócono niezawodne zasilanie"), "Register shows fulfilled recovery with its preserved outcome"); await Capture("energy-pledge-fulfilled");
            Start(Fixture(true)); state.Player.Gold = 0; ShowCouncil();
            Check(!Find("01   Przeznaczymy").IsEnabled && Find("02   Zapewnimy").IsEnabled && Find("03   Utrzymamy").IsEnabled, "Empty treasury disables relief while preserving both free political choices");
            Click("03   Utrzymamy"); Check(state.EnergyCases.Single().Status == EnergyCaseStatus.Declined && state.Player.Gold == 0, "Refusal records a real decision without fabricating a payment or promise");
            Start(Fixture(true)); ShowCouncil(); Click("02   Zapewnimy"); city = state.Cities[0]; ShowPowerManagement(city.Id); Click("Wyłącz: Centrum danych");
            for (int i = 0; i < 6; i++) Turn(); c = state.EnergyCases.Single();
            Check(c.Status == EnergyCaseStatus.Broken && c.Failure == EnergyFailure.Deadline && c.Deliveries.Count == 6 && EnergyCouncil.Progress(c) == 0, "Turning off the named service cannot satisfy the pledge at zero demand");
            ShowEnergyCases(); root.UpdateLayout(); Check(Descendants<TextBlock>(overlay!).Any(t => t.Text.Contains("warunki niewypełnione")), "Failed pledge register explains the unmet phases rather than awarding a timer reward"); await Capture("energy-pledge-broken");
            var failedState = state;
            Start(Fixture(true)); state.PendingEvent = "grain"; state.PendingEnergyCaseId = null; ShowEnergyCases();
            Check(!Find("Rozpatrz kryzys #").IsEnabled, "An older pending council visibly prevents replacing its context with an energy hearing");
            Start(failedState); WindowState = WindowState.Normal; Width = 1200; Height = 800; await Task.Delay(150); UpdateLayout(); ShowEnergyCases(); root.UpdateLayout();
            Check(root.ActualWidth < 1250 && root.ActualHeight < 850, $"Compact native layout: requested {Width}x{Height}, window {ActualWidth}x{ActualHeight}, root {root.ActualWidth}x{root.ActualHeight}, state {WindowState}");
            Check(Descendants<ScrollViewer>(overlay!).Any(v => v.ScrollableHeight > 0), "Energy case explanations and actions remain reachable in a compact scrolling window"); await Capture("energy-cases-compact");
            Width = 1600; Height = 980; await Task.Delay(150); ShowEnergyCases(); await Capture("energy-cases-final");
            checks.Add($"{checks.Count} energy case desktop integration checks passed."); File.WriteAllLines(Path.Combine(folder, "ui-smoke.txt"), checks); Close();
        }
        catch (Exception ex) { checks.Add(ex.ToString()); File.WriteAllLines(Path.Combine(folder, "ui-smoke.txt"), checks); if (!arguments.Contains("--smoke-force-failure")) await Capture("energy-cases-failure"); Application.Current.Shutdown(3); }
    }
}
