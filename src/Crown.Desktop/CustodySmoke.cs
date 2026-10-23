using Crown.Core;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Crown.Desktop;

public sealed partial class MainWindow
{
    private async Task CustodySmoke(string folder)
    {
        Directory.CreateDirectory(folder); var checks = new List<string>();
        void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); checks.Add("PASS " + message); }
        void Click(string text)
        {
            root.UpdateLayout(); var button = Descendants<Button>(root).First(b => b.IsEnabled && (b.Content?.ToString()?.StartsWith(text) == true || b.Content is StackPanel p && p.Children.OfType<TextBlock>().Any(t => t.Text.Contains(text))));
            button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        }
        Button[] Choices() { root.UpdateLayout(); return Descendants<Button>(overlay!).Where(b => b.Content is StackPanel).ToArray(); }
        async Task Capture(string name)
        {
            root.UpdateLayout(); await Task.Delay(100); var bmp = new RenderTargetBitmap((int)root.ActualWidth, (int)root.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32); bmp.Render(root);
            var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bmp)); string path = Path.Combine(folder, name + ".png"), temp = path + ".tmp";
            using (var stream = File.Create(temp)) encoder.Save(stream); File.Move(temp, path, true);
        }
        GameState Fixture()
        {
            var s = World.CreateLegacy(); Narrative.Resolve(s, 0); s.Units.Clear();
            foreach (var f in s.Factions) { f.Gold = 3000; f.Stability = 65; f.Technologies.UnionWith(["pottery", "writing"]); }
            foreach (var c in s.Cities) { c.Project = "treasury"; c.Production = 90; c.ProductionPaused = true; s.Tiles[c.Tile].Explored = true; }
            foreach (var r in s.Relations) { r.Trust = 30; r.Trade = false; } return s;
        }
        void Launch(GameState s, int owner, int target)
        {
            if (!Engine.RecruitSpy(s, owner).Success || !Engine.StartSpyMission(s, s.Spies.Last().Id, s.Cities.First(c => c.Owner == target).Id, SpyMission.Sabotage, owner).Success)
                throw new InvalidOperationException("Could not prepare intelligence operation.");
        }
        void SelectCaptureSeed()
        {
            // Select fixture randomness using the actual end-turn pipeline; ordinary gameplay has no such control.
            string saved = SaveGame.Serialize(state);
            for (uint seed = 1; seed < 10000; seed++)
            {
                var trial = SaveGame.Deserialize(saved); trial.RandomState = seed; if (!Engine.EndTurn(trial).Success) throw new InvalidOperationException("Trial turn failed.");
                if (trial.SpyOperations.All(o => o.Outcome == SpyOutcome.Captured && o.CaptureChoice == null)) { state.RandomState = seed; return; }
            }
            throw new InvalidOperationException("No deterministic capture fixture found.");
        }
        void Turn() { CloseModal(); Advance(); }
        try
        {
            if (arguments.Contains("--smoke-force-failure")) throw new InvalidOperationException("Intentional custody diagnostic failure.");
            var s = Fixture(); Launch(s, 1, 0); Start(s);
            Turn(); Turn(); Turn(); SelectCaptureSeed(); Turn();
            Check(state.PendingEvent == CustodyCouncil.Event.Id && state.Spies[0].Status == SpyStatus.Captured && state.SpyOperations[0].CaptureChoice == null, "A real fourth turn opens the player's foreign-agent decision instead of auto-paying ransom");
            ShowCouncil(); Check(Choices().Length == 4 && Descendants<TextBlock>(overlay!).Any(t => t.Text.Contains(state.Spies[0].Name) && t.Text.Contains(state.Factions[1].Name)), "Foreign-agent council shows the actual captive, country and four decisions"); await Capture("custody-council");
            string path = Path.Combine(folder, "custody-open-save.crown"); SaveGame.Write(state, path); string before = SaveGame.Serialize(state); LoadPath(path);
            Check(SaveGame.Serialize(state) == before && overlay != null, "Desktop reload reopens the same unanswered foreign-agent case");
            state.Factions[1].Gold = 0; ShowCouncil(); var choices = Choices();
            Check(!choices[0].IsEnabled && !choices[1].IsEnabled && choices[2].IsEnabled && choices[3].IsEnabled, "Insolvent sender and missing exchange partner disable only the unavailable choices");
            double releaseGold = state.Player.Gold, prestige = state.Player.Prestige; int trust = state.Relation(0, 1).Trust, nobles = state.Player.Nobles; Click("03   Zwolnimy");
            Check(state.Player.Gold == releaseGold && state.Player.Prestige == prestige + 4 && state.Player.Nobles == nobles - 3 && state.Relation(0, 1).Trust == trust + 12 && state.Spies[0].Status == SpyStatus.Available, "Free release returns the agent and applies political and diplomatic consequences");
            LoadPath(path); double gold = state.Player.Gold, foreign = state.Factions[1].Gold; Click("01   Zażądamy");
            Check(state.Player.Gold == gold + 100 && state.Factions[1].Gold == foreign - 100 && state.SpyOperations[0].CustodyChoice == CustodyDecision.Ransom && state.Spies[0].Status == SpyStatus.Available, "Ransom button transfers the actual foreign gold and releases the same person");
            LoadPath(path); int internTrust = state.Relation(0, 1).Trust, internTurn = state.Turn; Click("04   Internujemy");
            Check(state.SpyOperations[0].InternedUntil == internTurn + 8 && state.Spies[0].Status == SpyStatus.Captured && state.Relation(0, 1).Trust == internTrust - 8, "Internment button binds the agent until the exact deadline and reduces trust");
            ShowEspionage(); root.UpdateLayout();
            Check(Descendants<TextBlock>(overlay!).Any(t => t.Text.Contains("internowany do początku T")) && !Descendants<Button>(overlay!).Any(b => b.Content?.ToString()?.StartsWith("Rozpatrz obcego jeńca") == true), "Agency lists the interned captive and prevents reversing the decision"); await Capture("custody-internment");
            string internPath = Path.Combine(folder, "custody-interned-save.crown"); SaveGame.Write(state, internPath); before = SaveGame.Serialize(state); LoadPath(internPath);
            Check(SaveGame.Serialize(state) == before, "Desktop reload retains the internment deadline and custody history");
            CloseModal(); state.Turn = state.SpyOperations[0].InternedUntil!.Value - 1; Turn();
            Check(state.Spies[0].Status == SpyStatus.Available && state.SpyOperations[0].CaptureChoice == CustodyCouncil.InternmentEnded && state.SpyOperations[0].CaptureDecisionTurn == state.Turn, "A real end turn automatically returns the interned agent at the deadline");
            CloseModal(); ShowSpyReport(state.SpyOperations[0]); root.UpdateLayout(); Check(Descendants<TextBlock>(overlay!).Any(t => t.Text.Contains("koniec internowania")), "Report identifies the finished internment instead of a lost agent"); await Capture("custody-release-report");
            s = Fixture(); Launch(s, 0, 1); Launch(s, 1, 0); Start(s); state.Turn = 4; SelectCaptureSeed(); Turn();
            Check(state.PendingEvent == CustodyCouncil.Event.Id && state.Spies.All(a => a.Status == SpyStatus.Captured), "When both sides capture agents the foreign-captive council preserves the exchange opportunity");
            string pairPath = Path.Combine(folder, "custody-pair-save.crown"); SaveGame.Write(state, pairPath); var funds = state.Factions.Select(f => f.Gold).ToArray(); ShowCouncil();
            Check(Choices()[1].IsEnabled, "Foreign-captive dialogue enables exchange when that country holds our agent"); await Capture("custody-exchange"); Click("02   Wymienimy");
            Check(state.Spies.All(a => a.Status == SpyStatus.Available) && state.SpyOperations.All(o => o.CaptureChoice == 3 && o.ExchangeOperationId != null) && funds.SequenceEqual(state.Factions.Select(f => f.Gold)), "Exchange button returns exactly the two linked people with no gold transfer");
            string exchangedPath = Path.Combine(folder, "custody-exchanged-save.crown"); SaveGame.Write(state, exchangedPath); before = SaveGame.Serialize(state); LoadPath(exchangedPath);
            Check(SaveGame.Serialize(state) == before, "Desktop reload retains reciprocal prisoner exchange links");
            s = SaveGame.Read(pairPath); s.PendingEvent = null; s.PendingSpyOperationId = null; SpyCouncil.Present(s, s.SpyOperations.First(o => o.Owner == 0).Id);
            s.Player.Gold = s.Factions[1].Gold = 0; s.Relation(0, 1).War = true; WarCouncil.EnsureActive(s); Start(s); ShowCouncil(); choices = Choices();
            Check(choices.Length == 4 && choices[3].IsEnabled && !choices[1].IsEnabled, "Own-captive dialogue allows wartime exchange even when both treasuries are empty"); Click("04   Wymienimy");
            Check(state.Spies.All(a => a.Status == SpyStatus.Available) && state.Factions.Take(2).All(f => f.Gold == 0), "Own-captive exchange uses the same free atomic release");
            LoadPath(pairPath); Width = 1200; Height = 800; ShowCouncil(); root.UpdateLayout(); await Capture("custody-compact");
            Check(Descendants<ScrollViewer>(overlay!).Any(v => v.ScrollableHeight > 0) || Choices().All(b => b.TransformToAncestor(root).TransformBounds(new Rect(b.RenderSize)).Bottom <= root.ActualHeight), "Compact council keeps every choice reachable through scrolling");
            Width = 1600; Height = 980; CloseModal(); ShowEspionage(); await Capture("custody-final");
            checks.Add($"{checks.Count} custody desktop integration checks passed."); File.WriteAllLines(Path.Combine(folder, "ui-smoke.txt"), checks); Close();
        }
        catch (Exception exception) { checks.Add(exception.ToString()); File.WriteAllLines(Path.Combine(folder, "ui-smoke.txt"), checks); Application.Current.Shutdown(3); }
    }
}
