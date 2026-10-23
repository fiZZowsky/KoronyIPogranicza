using Crown.Core;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Crown.Desktop;

public sealed partial class MainWindow
{
    private async Task SpySmoke(string folder)
    {
        Directory.CreateDirectory(folder); var checks = new List<string>();
        void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); checks.Add("PASS " + message); }
        void Click(string text)
        {
            root.UpdateLayout(); var button = Descendants<Button>(root).First(b => b.IsEnabled && (b.Content?.ToString()?.StartsWith(text) == true || b.Content is StackPanel p && p.Children.OfType<TextBlock>().Any(t => t.Text.Contains(text))));
            button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        }
        async Task Capture(string name)
        {
            root.UpdateLayout(); await Task.Delay(100); var bmp = new RenderTargetBitmap((int)root.ActualWidth, (int)root.ActualHeight, 96, 96, PixelFormats.Pbgra32); bmp.Render(root);
            var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bmp)); string path = Path.Combine(folder, name + ".png"), temp = path + ".tmp";
            using (var stream = File.Create(temp)) encoder.Save(stream); File.Move(temp, path, true);
        }
        GameState Fixture()
        {
            var s = World.CreateLegacy(); Narrative.Resolve(s, 0); s.Units.Clear();
            foreach (var f in s.Factions) { f.Gold = 3000; f.Stability = 65; f.Research = "agriculture"; f.Technologies.UnionWith(["pottery", "writing", "agriculture"]); }
            s.Player.Technologies.Remove("agriculture"); s.Player.Research = "";
            foreach (var c in s.Cities) { c.Project = "treasury"; c.Production = 90; c.ProductionPaused = true; s.Tiles[c.Tile].Explored = true; }
            foreach (var relation in s.Relations) { relation.Trust = 30; relation.Trade = false; } return s;
        }
        void AnswerPending()
        {
            if (state.PendingEvent != null) { var e = ProvincialEvents.Current(state); int choice = Array.FindIndex(e.Choices, c => state.Player.Gold + c.Gold >= 0); if (!Narrative.Resolve(state, choice).Success) throw new InvalidOperationException("Prepared council could not resolve."); }
        }
        void Turn()
        {
            AnswerPending();
            CloseModal(); Advance(); CloseModal();
        }
        void SetNextOutcome(int operationId, bool success, bool detected)
        {
            // Pick a deterministic fixture seed through the actual end-turn pipeline, including other AI draws.
            AnswerPending();
            string saved = SaveGame.Serialize(state);
            for (uint seed = 1; seed < 10000; seed++)
            {
                var trial = SaveGame.Deserialize(saved); trial.RandomState = seed; var result = Engine.EndTurn(trial); if (!result.Success) throw new InvalidOperationException(result.Message);
                var operation = trial.SpyOperations.Single(o => o.Id == operationId);
                if ((operation.Outcome == SpyOutcome.Succeeded) == success && operation.Detected == detected && operation.Outcome != SpyOutcome.Active && operation.Outcome != SpyOutcome.Cancelled)
                { state.RandomState = seed; return; }
            }
            throw new InvalidOperationException("No prepared end-turn outcome found.");
        }
        void SelectMission(int targetIndex, SpyMission kind)
        { root.UpdateLayout(); var combos = Descendants<ComboBox>(overlay!).ToArray(); combos[0].SelectedIndex = targetIndex; combos[1].SelectedIndex = (int)kind; }
        try
        {
            if (arguments.Contains("--smoke-force-failure")) throw new InvalidOperationException("Intentional espionage diagnostic failure.");
            var world = Fixture(); world.Player.Technologies.Remove("writing"); Start(world); tab = "diplomacy"; RefreshSide(); Click("Wywiad i kontrwywiad"); root.UpdateLayout();
            Check(!Descendants<Button>(overlay!).First(b => b.Content?.ToString()?.StartsWith("Wyszkol agenta") == true).IsEnabled, "Intelligence training is disabled before Writing");
            state.Player.Technologies.Add("writing"); ShowEspionage(); double recruitGold = state.Player.Gold; Click("Wyszkol agenta");
            Check(state.Spies.Single().Status == SpyStatus.Available && state.Player.Gold == recruitGold - 60, "Recruitment button pays for a ready named agent"); await Capture("spy-agency");
            var agent = state.Spies[0]; Click("Zaplanuj operację agenta #" + agent.Id); SelectMission(0, SpyMission.Reconnaissance); string beforePreview = SaveGame.Serialize(state); await Capture("spy-planner");
            Check(SaveGame.Serialize(state) == beforePreview, "Mission form and preview do not change money or random state"); double launchGold = state.Player.Gold; Click("Rozpocznij wybraną operację");
            Check(state.SpyOperations.Single().Mission == SpyMission.Reconnaissance && agent.Status == SpyStatus.Deployed && state.Player.Gold == launchGold - 55 && state.SpyOperations[0].DueTurn == 4, "Recon launch button reserves the agent and charges its actual cost and deadline");
            Click("Operacja #1"); Click("Odwołaj operację #1"); Check(agent.Status == SpyStatus.Available && state.Player.Gold == launchGold - 55 && state.SpyOperations[0].Outcome == SpyOutcome.Cancelled, "Recall button returns an agent without refunding the paid mission");
            Start(Fixture()); tab = "diplomacy"; RefreshSide(); Click("Wywiad i kontrwywiad"); Click("Wyszkol agenta"); Click("Zaplanuj operację agenta #1"); SelectMission(0, SpyMission.Reconnaissance); Click("Rozpocznij wybraną operację");
            Turn(); Turn(); SetNextOutcome(1, true, false); Turn();
            Check(state.SpyOperations[0].Outcome == SpyOutcome.Succeeded && state.Spies[0].Experience == 2 && state.Tiles.Where(t => World.Distance(t, state.Tiles[state.Cities[1].Tile]) <= 3).All(t => t.Explored), "Third real turn delivers reconnaissance, explored terrain and agent experience");
            tab = "diplomacy"; RefreshSide(); Click("Wywiad i kontrwywiad"); Click("Raport #1"); root.UpdateLayout();
            Check(Descendants<TextBlock>(overlay!).Any(t => t.Text.Contains("Raport z T4") && t.Text.Contains("projekt")), "Report dialog displays a dated city snapshot"); await Capture("spy-recon-report");
            string reportPath = Path.Combine(folder, "spy-report-save.crown"); SaveGame.Write(state, reportPath); string reportBefore = SaveGame.Serialize(state); LoadPath(reportPath); CloseModal();
            Check(SaveGame.Serialize(state) == reportBefore, "Desktop reload retains the intelligence snapshot and experience"); ShowEspionage(); Click("Zaplanuj operację agenta #1"); SelectMission(1, SpyMission.StealResearch); Click("Rozpocznij wybraną operację");
            Turn(); Turn(); Turn(); double stolenBefore = state.Player.Science; SetNextOutcome(2, true, false); Turn();
            Check(state.SpyOperations[1].Outcome == SpyOutcome.Succeeded && state.Player.Science >= stolenBefore + 12 && !state.Player.Technologies.Contains("agriculture"), "Research theft grants science through turn processing without an automatic discovery");
            ShowEspionage(); Click("Zaplanuj operację agenta #1"); SelectMission(2, SpyMission.Sabotage); Click("Rozpocznij wybraną operację");
            string activePath = Path.Combine(folder, "spy-active-save.crown"); SaveGame.Write(state, activePath); string activeBefore = SaveGame.Serialize(state); LoadPath(activePath); CloseModal();
            Check(SaveGame.Serialize(state) == activeBefore, "Desktop reload preserves a paid in-flight operation and reserved agent");
            Turn(); Turn(); Turn(); double workBefore = state.Cities[3].Production; int trustBefore = state.Relation(0, 3).Trust; SetNextOutcome(3, true, true); Turn();
            Check(state.SpyOperations[2].Outcome == SpyOutcome.Succeeded && state.SpyOperations[2].Detected && state.Cities[3].Production == Math.Max(0, workBefore - 60) && state.Relation(0, 3).Trust == trustBefore - 15, "Successful detected sabotage removes actual work and damages foreign trust");
            ShowEspionage(); Click("Raport #3"); await Capture("spy-sabotage-report");
            Start(Fixture()); tab = "diplomacy"; RefreshSide(); Click("Wywiad i kontrwywiad"); Click("Wyszkol agenta"); Click("Zaplanuj operację agenta #1"); SelectMission(0, SpyMission.Sabotage); Click("Rozpocznij wybraną operację");
            Turn(); Turn(); Turn(); SetNextOutcome(1, false, true); Turn();
            Check(state.Spies[0].Status == SpyStatus.Captured && state.PendingEvent == SpyCouncil.Event.Id && state.PendingSpyOperationId == 1, "Failed detected operation opens a contextual captured-agent council");
            string capturedPath = Path.Combine(folder, "spy-captured-save.crown"); SaveGame.Write(state, capturedPath); string capturedBefore = SaveGame.Serialize(state); LoadPath(capturedPath);
            Check(SaveGame.Serialize(state) == capturedBefore && overlay != null, "Desktop reload reopens the unanswered captive decision"); await Capture("spy-council");
            double ransomGold = state.Player.Gold, ransomCaptor = state.Factions[1].Gold; Click("01   Zapłacimy okup");
            Check(state.Player.Gold == ransomGold - 100 && state.Factions[1].Gold == ransomCaptor + 100 && state.Spies[0].Status == SpyStatus.Available && state.SpyOperations[0].CaptureChoice == 0, "Ransom choice transfers money once and releases the same agent");
            LoadPath(capturedPath); double apologyGold = state.Player.Gold, apologyPrestige = state.Player.Prestige; int apologyTrust = state.Relation(0, 1).Trust; Click("02   Przeprosimy");
            Check(state.Player.Gold == apologyGold - 40 && state.Player.Prestige == apologyPrestige - 8 && state.Relation(0, 1).Trust == apologyTrust + 8 && state.Spies[0].Status == SpyStatus.Available, "Public apology exchanges gold and prestige for an agent and better relations");
            LoadPath(capturedPath); state.Relation(0, 1).War = true; WarCouncil.EnsureActive(state); ShowCouncil(); root.UpdateLayout();
            Check(!Descendants<Button>(overlay!).Where(b => b.Content is StackPanel).ElementAt(1).IsEnabled, "Wartime captive dialogue disables the public peace agreement");
            state.Relation(0, 1).War = false; state.Player.Gold = 0; ShowCouncil(); root.UpdateLayout(); var choices = Descendants<Button>(overlay!).Where(b => b.Content is StackPanel).ToArray();
            Check(!choices[0].IsEnabled && !choices[1].IsEnabled && choices[2].IsEnabled, "Empty treasury keeps a free denial choice available"); Click("03   Wyprzemy");
            Check(state.Spies[0].Status == SpyStatus.Lost && state.SpyOperations[0].CaptureChoice == 2 && state.PendingSpyOperationId == null, "Denial permanently removes the agent from the usable roster");
            LoadPath(reportPath); CloseModal(); ShowEspionage(); double protectionGold = state.Player.Gold; Click("Kontrwywiad: " + state.Cities[0].Name);
            Check(state.Player.Gold == protectionGold - 60 && state.Cities[0].CounterIntelligenceUntil == state.Turn + 8, "Protection button buys eight turns of counterintelligence");
            var hostileWorld = Fixture(); hostileWorld.Turn = 27; hostileWorld.Relation(0, 1).Trust = -20; Espionage.ManageAi(hostileWorld, 1); Start(hostileWorld); ShowEspionage(); root.UpdateLayout();
            Check(state.SpyOperations.Single().Owner == 1 && !Descendants<Button>(overlay!).Any(b => b.Content?.ToString()?.StartsWith("Raport #1") == true), "Hostile AI launches a paid operation which the target cannot inspect before detection");
            Click("Kontrwywiad: " + state.Cities[0].Name); Turn(); Turn(); Turn(); SetNextOutcome(1, false, true); Turn();
            ShowEspionage(); root.UpdateLayout(); Check(state.SpyOperations[0].Detected && state.SpyOperations[0].CaptureChoice == null && state.PendingEvent == CustodyCouncil.Event.Id && Descendants<Button>(overlay!).Any(b => b.Content?.ToString()?.StartsWith("Raport #1") == true), "Counterintelligence exposes the captured AI spy and leaves its fate to the defending player");
            string defensePath = Path.Combine(folder, "spy-defense-save.crown"); SaveGame.Write(state, defensePath); Width = 1200; Height = 800; ShowEspionage(); root.UpdateLayout(); await Capture("spy-compact");
            var back = Descendants<Button>(overlay!).First(b => b.Content?.ToString() == "Wróć do mapy"); var bounds = back.TransformToAncestor(root).TransformBounds(new Rect(back.RenderSize));
            Check(bounds.Top >= 0 && bounds.Bottom <= root.ActualHeight || Descendants<ScrollViewer>(overlay!).Any(v => v.ScrollableHeight > 0), "Compact intelligence panel keeps actions and return reachable through scrolling");
            Width = 1600; Height = 980; LoadPath(reportPath); CloseModal(); ShowEspionage(); await Capture("spy-final");
            checks.Add($"{checks.Count} espionage desktop integration checks passed."); File.WriteAllLines(Path.Combine(folder, "ui-smoke.txt"), checks); Close();
        }
        catch (Exception exception) { checks.Add(exception.ToString()); File.WriteAllLines(Path.Combine(folder, "ui-smoke.txt"), checks); Application.Current.Shutdown(3); }
    }
}
