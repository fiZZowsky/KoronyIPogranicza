using Crown.Core;

internal static class StageNineteenSpecs
{
    private static void Assert(bool value, string message) { if (!value) throw new Exception(message); }
    public static GameState Fresh()
    {
        var s = StageEighteenSpecs.Fresh(); s.Cities[0].Buildings.Add("data_centre");
        for (int i = 0; i < 18; i++) s.Player.EpochChoices[i] = 1;
        return s;
    }
    private static void DecideOther(GameState s)
    {
        if (s.PendingEvent == null) return; var e = ProvincialEvents.Current(s);
        Assert(Narrative.Resolve(s, Enumerable.Range(0, e.Choices.Length).First(i => s.Player.Gold + e.Choices[i].Gold >= 0)).Success, "Council failed.");
    }
    private static void Step(GameState s) { DecideOther(s); Assert(Engine.EndTurn(s).Success, "Turn failed."); }
    public static GameState Ready()
    {
        var s = Fresh(); for (int i = 0; i < 3; i++) Step(s);
        Assert(s.EnergyCases.Count == 1 && s.PendingEvent == EnergyCouncil.Event.Id, "No real energy petition."); return s;
    }
    private static void Valid(GameState s) => SaveGame.Deserialize(SaveGame.Serialize(s));
    private static void Reject(GameState s)
    { bool failed = false; try { Valid(s); } catch (InvalidDataException) { failed = true; } Assert(failed, "Damaged energy promise accepted."); }
    private static void Atomic(GameState s, Func<Result> action)
    { string before = SaveGame.Serialize(s); Assert(!action().Success && before == SaveGame.Serialize(s), "Rejected energy command changed state."); }
    public static void Run(Action<string, Action> test)
    {
        test("An energy petition requires three real consecutive phases below seventy-five percent", () =>
        {
            var s = Fresh(); Step(s); Step(s); Assert(s.EnergyCases.Count == 0 && s.Cities[0].EnergyHistory.Count == 2, "Petition opened too soon."); Step(s);
            var c = s.EnergyCases.Single(); Assert(c.OpenedTurn == 4 && c.Shortages.Select(r => r.PhaseTurn).SequenceEqual([1, 2, 3]) && c.Shortages.All(r => r.Demand == 25 && r.Allocated == 0) && s.PendingEnergyCaseId == c.Id, "Petition did not use economic evidence."); Valid(s);
        });
        test("Shortages apply pressure to all countries while only the player receives a council", () =>
        {
            var s = Fresh(); s.Cities[0].Buildings.Add("atomic_reactor"); s.Cities[1].Buildings.Add("data_centre"); Step(s); Step(s); int commons = s.Factions[1].Commons; Step(s);
            Assert(s.EnergyCases.Count == 0 && s.Cities[1].EnergyHistory.Count == 3 && s.Factions[1].Commons == commons - 1, "Foreign shortages were free or created player petitions."); Valid(s);
        });
        test("Exactly seventy-five percent coverage does not trigger a crisis", () =>
        {
            var s = Fresh(); s.Cities[0].Buildings.Clear(); s.Cities[0].Buildings.UnionWith(["coal_station", "steel_mill", "rocket_centre"]);
            for (int i = 0; i < 4; i++) Step(s);
            Assert(s.EnergyCases.Count == 0 && s.Cities[0].EnergyHistory.All(r => r.Coverage == .75), "Threshold used an inclusive comparison.");
        });
        test("A powered phase resets the consecutive-shortage requirement", () =>
        {
            var s = Fresh(); Step(s); Step(s); s.Cities[0].Buildings.Add("atomic_reactor"); Step(s); s.Cities[0].Buildings.Remove("atomic_reactor"); Step(s); Step(s);
            Assert(s.EnergyCases.Count == 0, "Separated shortages accumulated as a streak."); Step(s); Assert(s.EnergyCases.Count == 1, "New three-phase streak ignored.");
        });
        test("Historical coverage uses the frozen economic phase rather than a newly completed plant", () =>
        {
            var s = Fresh(); Step(s); Step(s); var city = s.Cities[0]; city.Project = "atomic_reactor"; city.ProductionPaused = false; city.Production = 900; Step(s);
            Assert(city.Buildings.Contains("atomic_reactor") && Epochs.Energy(s, city).Coverage == 1 && s.EnergyCases.Single().Shortages[^1].Allocated == 0, "Construction rewrote the previous shortage.");
        });
        test("Power forecasts never create observations or petitions", () =>
        {
            var s = Fresh(); string before = SaveGame.Serialize(s); for (int i = 0; i < 20; i++) { PowerGrid.Plan(s, s.Cities[0]); Engine.CityYields(s, s.Cities[0]); }
            Assert(SaveGame.Serialize(s) == before && s.LastEnergyPhase == 0 && s.EnergyCases.Count == 0, "Preview generated historical evidence.");
        });
        test("Energy observations are bounded, consecutive and preserved by save reload", () =>
        {
            var s = Fresh(); s.Cities[0].Buildings.Add("atomic_reactor"); for (int i = 0; i < 12; i++) Step(s);
            var history = s.Cities[0].EnergyHistory; Assert(history.Count == 8 && history[0].PhaseTurn == 5 && history[^1].PhaseTurn == 12 && s.LastEnergyPhase == 12, "History did not bound itself.");
            Assert(SaveGame.Serialize(SaveGame.Deserialize(SaveGame.Serialize(s))) == SaveGame.Serialize(s), "History did not round-trip.");
        });
        test("Relief pays the displayed opening quote once and does not invent electricity", () =>
        {
            var s = Ready(); var c = s.EnergyCases.Single(); int quote = c.ReliefCost; s.Cities[0].Population++; double gold = s.Player.Gold;
            Assert(ProvincialEvents.Current(s).Choices[0].Gold == -quote && Narrative.Resolve(s, 0).Success && s.Player.Gold == gold - quote && c.Status == EnergyCaseStatus.Relief && Epochs.Energy(s, s.Cities[0]).Coverage == 0, "Relief payment or effect is wrong.");
            Atomic(s, () => Narrative.Resolve(s, 0)); Valid(s);
        });
        test("An empty treasury still has both a promise and an explicit refusal", () =>
        {
            var s = Ready(); s.Player.Gold = 0; Atomic(s, () => Narrative.Resolve(s, 0)); Assert(Narrative.Resolve(s, 1).Success, "Free promise missing.");
            s = Ready(); s.Player.Gold = 0; Assert(Narrative.Resolve(s, 2).Success && s.EnergyCases.Single().Status == EnergyCaseStatus.Declined, "Free refusal missing."); Valid(s);
        });
        test("Promise decisions alone never count existing or forecast power as delivery", () =>
        {
            var s = Ready(); s.Cities[0].Buildings.Add("atomic_reactor"); Assert(Narrative.Resolve(s, 1).Success, "Pledge failed."); var c = s.EnergyCases.Single();
            Assert(c.Deliveries.Count == 0 && c.DecidedTurn == 4 && c.DeadlineTurn == 10 && c.Status == EnergyCaseStatus.Pledged, "Past observations were credited."); Valid(s);
        });
        test("Three fully powered future phases settle a pledge through actual turn execution", () =>
        {
            var s = Ready(); Narrative.Resolve(s, 1); var c = s.EnergyCases.Single(); s.Cities[0].Buildings.Add("atomic_reactor"); Step(s); Step(s);
            Assert(c.Status == EnergyCaseStatus.Pledged && EnergyCouncil.Progress(c) == 2, "Promise settled before three phases."); Step(s);
            Assert(c.Status == EnergyCaseStatus.Fulfilled && c.ResolvedTurn == 7 && c.Deliveries.Select(r => r.PhaseTurn).SequenceEqual([4, 5, 6]) && s.Chronicle.Any(e => e.Category == "konsekwencja" && e.Text.Contains("niezawodne")), "Real service recovery was not rewarded."); Valid(s);
        });
        test("One interruption resets promise progress and success wins on the final allowed phase", () =>
        {
            var s = Ready(); Narrative.Resolve(s, 1); var c = s.EnergyCases.Single(); var city = s.Cities[0]; city.Buildings.Add("atomic_reactor"); Step(s); Step(s);
            city.Buildings.Remove("atomic_reactor"); Step(s); Assert(EnergyCouncil.Progress(c) == 0, "Interrupted service kept its streak."); city.Buildings.Add("atomic_reactor"); Step(s); Step(s); Step(s);
            Assert(c.Status == EnergyCaseStatus.Fulfilled && c.ResolvedTurn == c.DeadlineTurn && c.Deliveries.Count == 6, "Deadline ran before its final delivery."); Valid(s);
        });
        test("Two full phases at the deadline break the promise and cannot later resurrect its reward", () =>
        {
            var s = Ready(); Narrative.Resolve(s, 1); var c = s.EnergyCases.Single(); for (int i = 0; i < 4; i++) Step(s); s.Cities[0].Buildings.Add("atomic_reactor"); Step(s); Step(s);
            Assert(c.Status == EnergyCaseStatus.Broken && c.Failure == EnergyFailure.Deadline && c.ResolvedTurn == 10 && EnergyCouncil.Progress(c) == 2, "Unmet deadline did not fail.");
            int logs = s.Chronicle.Count(e => e.Category == "konsekwencja" && e.Text.Contains("Złamana obietnica")); for (int i = 0; i < 3; i++) Step(s);
            Assert(c.Status == EnergyCaseStatus.Broken && c.Deliveries.Count == 6 && s.Chronicle.Count(e => e.Category == "konsekwencja" && e.Text.Contains("Złamana obietnica")) == logs, "Closed promise paid or punished twice."); Valid(s);
        });
        test("Turning off promised services cannot satisfy a restoration pledge with zero demand", () =>
        {
            var s = Ready(); Narrative.Resolve(s, 1); var city = s.Cities[0]; Engine.SetBuildingOperation(s, city.Id, "data_centre", true);
            for (int i = 0; i < 6; i++) Step(s); var c = s.EnergyCases.Single();
            Assert(c.Status == EnergyCaseStatus.Broken && c.Deliveries.All(r => r.Demand == 0) && EnergyCouncil.Progress(c) == 0, "Shutdown produced a false recovery."); Valid(s);
        });
        test("Powering a substitute does not erase the named services in the original promise", () =>
        {
            var s = Ready(); Narrative.Resolve(s, 1); var city = s.Cities[0]; Engine.SetBuildingOperation(s, city.Id, "data_centre", true); city.Buildings.UnionWith(["fusion_plant", "steel_mill"]);
            for (int i = 0; i < 6; i++) Step(s); var c = s.EnergyCases.Single(); Assert(c.Status == EnergyCaseStatus.Broken && c.Deliveries.All(r => r.Coverage == 1), "Unrelated industry satisfied the named pledge.");
        });
        test("A pledge checks the entire active city's power rather than only its old demand", () =>
        {
            var s = Ready(); Narrative.Resolve(s, 1); var city = s.Cities[0]; city.Buildings.UnionWith(["atomic_reactor", "data_centre", "asteroid_forge"]);
            for (int i = 0; i < 6; i++) Step(s); Assert(s.EnergyCases.Single().Status == EnergyCaseStatus.Broken, "Old 25-demand service ignored current municipal shortages.");
        });
        test("A real priority change can recover a pledged city by shifting power from its neighbour", () =>
        {
            var s = Fresh(); var city = s.Cities[0]; var other = StageEighteenSpecs.Neighbor(s, city); other.Buildings.UnionWith(["electric_grid", "data_centre", "steel_mill"]);
            for (int i = 0; i < 3; i++) Step(s); Assert(s.EnergyCases.Any(c => c.CityId == city.Id), "Connected shortage not detected.");
            var c = s.EnergyCases.First(c => c.CityId == city.Id); Assert(Narrative.Resolve(s, 1).Success && Engine.SetPowerPriority(s, city.Id, PowerPriority.Critical).Success, "Priority instruction failed.");
            for (int i = 0; i < 3; i++) Step(s); Assert(c.Status == EnergyCaseStatus.Fulfilled && Epochs.Energy(s, other).Coverage < 1, "Priority did not produce actual restoration with a real tradeoff."); Valid(s);
        });
        test("A case keeps its original protocol after the recent city history has rolled over", () =>
        {
            var s = Ready(); Narrative.Resolve(s, 0); var c = s.EnergyCases.Single(); var source = c.Shortages.Select(r => r.PhaseTurn).ToArray();
            for (int i = 0; i < 10; i++) Step(s); Assert(c.Shortages.Select(r => r.PhaseTurn).SequenceEqual(source) && s.Cities[0].EnergyHistory[0].PhaseTurn > source[^1], "Case source shared a mutable rolling history."); Valid(s);
        });
        test("Repeated shortages respect sixteen turns after resolution and one open case per city", () =>
        {
            var s = Ready(); Narrative.Resolve(s, 2); var first = s.EnergyCases.Single();
            while (s.Turn < first.ResolvedTurn + EnergyCouncil.Cooldown) Step(s);
            Assert(s.EnergyCases.Count == 2 && s.EnergyCases[1].OpenedTurn == first.ResolvedTurn + EnergyCouncil.Cooldown, "Cooldown reopened too early or never elapsed."); Valid(s);
        });
        test("An unresolved older council is never overwritten by a new energy delegation", () =>
        {
            var s = Fresh(); Step(s); Step(s); s.SeenEvents.Remove("founding"); s.PendingEvent = "founding";
            Atomic(s, () => EnergyCouncil.Present(s, 99)); Assert(s.PendingEvent == "founding" && s.EnergyCases.Count == 0, "Present overwrote an unrelated council.");
            DecideOther(s); Step(s); Assert(s.PendingEvent == EnergyCouncil.Event.Id, "Delegation did not become available after council.");
        });
        test("Losing a pledged city breaks the national word but does not punish its new local owner", () =>
        {
            var s = Ready(); Narrative.Resolve(s, 1); var c = s.EnergyCases.Single(); var city = s.Cities[0]; city.Buildings.Add("atomic_reactor"); Step(s);
            ProvincialPolitics.Transfer(s, city, 1, true); Assert(c.Status == EnergyCaseStatus.Broken && c.Failure == EnergyFailure.CityLost && city.Unrest == 55 && city.EnergyHistory.Count == 0, "City loss retained pledge or applied old ruler's local penalty."); Valid(s);
        });
        test("Losing a city closes its pending delegation and clears only that council context", () =>
        {
            var s = Ready(); var city = s.Cities[0]; ProvincialPolitics.Transfer(s, city, 1, true);
            Assert(s.EnergyCases.Single().Status == EnergyCaseStatus.Voided && s.PendingEvent == null && s.PendingEnergyCaseId == null && city.EnergyHistory.Count == 0, "Lost pending case stranded the council."); Valid(s);
        });
        test("A recovered city does not inherit its previous owner's shortage observations", () =>
        {
            var s = Ready(); Narrative.Resolve(s, 2); var city = s.Cities[0]; ProvincialPolitics.Transfer(s, city, 1, true); ProvincialPolitics.Transfer(s, city, 0, true); Step(s);
            Assert(city.EnergyHistory.Count == 1 && s.EnergyCases.Count == 1, "Occupation history caused an instant second petition."); Valid(s);
        });
        test("V18 migration preserves power directives without inventing old outages or promises", () =>
        {
            var s = Ready(); s.PendingEvent = null; s.PendingEnergyCaseId = null; s.Cities[0].Buildings.Add("atomic_reactor"); Engine.SetBuildingOperation(s, s.Cities[0].Id, "atomic_reactor", true); Engine.SetPowerPriority(s, s.Cities[0].Id, PowerPriority.Critical); s.Version = 18;
            var loaded = SaveGame.Deserialize(SaveGame.Serialize(s)); Assert(loaded.Version == 19 && loaded.EnergyCases.Count == 0 && loaded.LastEnergyPhase == 0 && loaded.Cities.All(c => c.EnergyHistory.Count == 0) && loaded.Cities[0].SuspendedBuildings.Contains("atomic_reactor") && loaded.Cities[0].PowerPriority == PowerPriority.Critical, "Migration erased directives or invented historical shortages."); Valid(loaded);
        });
        test("V18 overwrite retains an exact backup before introducing the new save format", () =>
        {
            var s = Fresh(); s.Version = 18; string old = SaveGame.Serialize(s); string path = Path.Combine(Environment.CurrentDirectory, "artifacts", "migration-v18-test.crown");
            try { File.WriteAllText(path, old); SaveGame.Write(SaveGame.Deserialize(old), path); Assert(File.ReadAllText(path + ".v18.bak") == old && SaveGame.Read(path).Version == 19, "V18 backup lost."); }
            finally { File.Delete(path); File.Delete(path + ".v18.bak"); }
        });
        test("A mid-pledge save retains evidence and follows the same future political outcome", () =>
        {
            var s = Ready(); Narrative.Resolve(s, 1); s.Cities[0].Buildings.Add("atomic_reactor"); Step(s); Step(s); string json = SaveGame.Serialize(s); var loaded = SaveGame.Deserialize(json);
            Assert(SaveGame.Serialize(loaded) == json, "Mid-pledge state changed on load."); for (int i = 0; i < 5; i++) { Step(s); Step(loaded); Assert(SaveGame.Serialize(s) == SaveGame.Serialize(loaded), "Promise replay diverged."); } Assert(s.EnergyCases[0].Status == EnergyCaseStatus.Fulfilled, "Replayed promise never fulfilled.");
        });
        test("Malformed observations, impossible success and missing council contexts are rejected", () =>
        {
            var s = Ready(); s.Cities[0].EnergyHistory[0].Allocated = double.NaN; bool failed = false; try { Valid(s); } catch (Exception e) when(e is InvalidDataException or ArgumentException) { failed = true; } Assert(failed, "NaN accepted.");
            s = Ready(); s.EnergyCases[0].Shortages[0].PhaseTurn++; Reject(s);
            s = Ready(); s.EnergyCases[0].Shortages[0].Services.Add("library"); Reject(s);
            s = Ready(); s.PendingEnergyCaseId = 999; Reject(s);
            s = Ready(); s.EnergyCases[0].Status = EnergyCaseStatus.Fulfilled; Reject(s);
            s = Ready(); s.EnergyCases[0].ReliefCost = -1; Reject(s);
            s = Ready(); s.Cities[0].EnergyHistory = null!; Reject(s);
            s = Ready(); s.EnergyCases = null!; Reject(s);
        });
        test("Legacy campaigns neither accumulate energy evidence nor permit fabricated petitions", () =>
        {
            var s = World.CreateLegacy(); for (int i = 0; i < 4; i++) Step(s);
            Assert(s.LastEnergyPhase == 0 && s.EnergyCases.Count == 0 && s.Cities.All(c => c.EnergyHistory.Count == 0), "Legacy rules gained power crises."); Atomic(s, () => EnergyCouncil.Present(s, 1)); Valid(s);
        });
        test("Live pledges cannot hide an observed phase or forge an earlier successful streak", () =>
        {
            var s = Ready(); Narrative.Resolve(s, 1); Step(s); s.EnergyCases[0].Deliveries.Clear(); Reject(s);
            s = Ready(); s.Cities[0].EnergyHistory.RemoveAt(s.Cities[0].EnergyHistory.Count - 1); Reject(s);
            s = Ready(); Narrative.Resolve(s, 1); Step(s); var c = s.EnergyCases[0]; c.Deliveries[0].Demand = 0; Reject(s);
        });
    }
}
