using Crown.Core;
using System.Text.Json.Nodes;

internal static class StageFifteenSpecs
{
    private static void Assert(bool value, string message) { if (!value) throw new Exception(message); }
    public static GameState Fresh()
    {
        var s = World.Create(); Narrative.Resolve(s, 0); s.Units.Clear();
        foreach (var f in s.Factions) { f.Gold = 3000; f.Stability = 65; f.Technologies.UnionWith(["pottery", "writing"]); }
        foreach (var c in s.Cities) { c.ProductionPaused = true; c.Project = "treasury"; c.Production = 90; }
        foreach (var r in s.Relations) { r.Trust = 30; r.Trade = false; }
        s.Factions[1].Technologies.Add("agriculture"); s.Tiles[s.Cities[1].Tile].Explored = true; return s;
    }
    private static SpyAgent Recruit(GameState s, int owner = 0)
    { Assert(Engine.RecruitSpy(s, owner).Success, "Recruitment failed."); return s.Spies.Last(); }
    private static SpyOperation Launch(GameState s, SpyMission mission = SpyMission.Sabotage, int owner = 0, int? cityId = null, SpyAgent? agent = null)
    {
        agent ??= Recruit(s, owner); Assert(Engine.StartSpyMission(s, agent.Id, cityId ?? s.Cities[1].Id, mission, owner).Success, "Launch failed."); return s.SpyOperations.Last();
    }
    public static void SetRolls(GameState s, SpyOperation operation, bool success, bool detected)
    {
        var forecast = Espionage.Forecast(s, s.Spies.First(a => a.Id == operation.AgentId), s.Cities.First(c => c.Id == operation.CityId), operation.Mission);
        for (uint seed = 1; seed < 100000; seed++)
        {
            var trial = new GameState { RandomState = seed }; bool a = trial.Next(100) < forecast.SuccessChance, b = trial.Next(100) < forecast.DetectionChance;
            if (a == success && b == detected) { s.RandomState = seed; return; }
        }
        throw new Exception("No deterministic outcome seed.");
    }
    private static void Finish(GameState s, SpyOperation operation, bool success = true, bool detected = false)
    { s.Turn = operation.DueTurn; SetRolls(s, operation, success, detected); Espionage.Tick(s); }
    public static GameState Ready()
    { var s = Fresh(); var operation = Launch(s); Finish(s, operation, false, true); Assert(SpyCouncil.Schedule(s), "No captured-agent council."); return s; }
    private static void Atomic(GameState s, Func<Result> action)
    { string before = SaveGame.Serialize(s); Assert(!action().Success && SaveGame.Serialize(s) == before, "Rejected spy action changed the campaign."); }
    private static void Reject(GameState s)
    { bool rejected = false; try { SaveGame.Deserialize(SaveGame.Serialize(s)); } catch (InvalidDataException) { rejected = true; } Assert(rejected, "Invalid intelligence data accepted."); }
    private static void Step(GameState s)
    {
        if (s.PendingEvent != null) { var e = ProvincialEvents.Current(s); Assert(Narrative.Resolve(s, Array.FindIndex(e.Choices, c => s.Player.Gold + c.Gold >= 0)).Success, "Council failed."); }
        Assert(Engine.EndTurn(s).Success, "Turn failed.");
    }
    public static void Run(Action<string, Action> test)
    {
        test("Intelligence recruitment charges sixty, gives unique agents and enforces the roster limit", () =>
        {
            var s = Fresh(); double gold = s.Player.Gold; var agent = Recruit(s); Recruit(s); Recruit(s);
            Assert(s.Player.Gold == gold - 180 && agent.Status == SpyStatus.Available && agent.Experience == 0 && s.Spies.Select(a => a.Id).Distinct().Count() == 3, "Recruitment price, identity or status wrong.");
            Atomic(s, () => Engine.RecruitSpy(s)); SaveGame.Deserialize(SaveGame.Serialize(s));
        });
        test("Recruitment rejects unavailable technology, money, state and owner without mutations", () =>
        {
            var s = Fresh(); s.Player.Technologies.Remove("writing"); Atomic(s, () => Engine.RecruitSpy(s)); s.Player.Technologies.Add("writing");
            s.Player.Gold = 59.5; Atomic(s, () => Engine.RecruitSpy(s)); s.Player.Gold = 60; Assert(Engine.RecruitSpy(s).Success && s.Player.Gold == 0, "Exact recruitment funds refused.");
            Atomic(s, () => Engine.RecruitSpy(s, -1)); Atomic(s, () => Engine.RecruitSpy(s, 4)); s.Ending = "Koniec"; Atomic(s, () => Engine.RecruitSpy(s));
        });
        test("Each mission charges its own cost and deadline once without rolling or immediate effects", () =>
        {
            foreach (var kind in Enum.GetValues<SpyMission>())
            {
                var s = Fresh(); var agent = Recruit(s); double gold = s.Player.Gold, work = s.Cities[1].Production, science = s.Player.Science; uint random = s.RandomState;
                var operation = Launch(s, kind, agent: agent);
                Assert(s.Player.Gold == gold - Espionage.Cost(kind) && operation.DueTurn == 1 + Espionage.Duration(kind) && operation.SuccessRoll == null && operation.DetectionRoll == null && operation.ResultValue == 0 && s.RandomState == random && s.Cities[1].Production == work && s.Player.Science == science && agent.Status == SpyStatus.Deployed, "Launch rolled, awarded or charged incorrectly.");
                Atomic(s, () => Engine.StartSpyMission(s, agent.Id, s.Cities[2].Id, kind)); SaveGame.Deserialize(SaveGame.Serialize(s));
            }
        });
        test("Mission previews are pure and agent rank improves both chances with a cap", () =>
        {
            var s = Fresh(); var agent = Recruit(s); string before = SaveGame.Serialize(s); var novice = Espionage.Forecast(s, agent, s.Cities[1], SpyMission.Sabotage);
            Espionage.Forecast(s, agent, s.Cities[1], SpyMission.Reconnaissance); Assert(SaveGame.Serialize(s) == before, "Forecast mutated data or random state.");
            agent.Experience = 15; var veteran = Espionage.Forecast(s, agent, s.Cities[1], SpyMission.Sabotage); agent.Experience = 30; var maximum = Espionage.Forecast(s, agent, s.Cities[1], SpyMission.Sabotage);
            Assert(veteran.SuccessChance == novice.SuccessChance + 25 && veteran.DetectionChance == novice.DetectionChance - 15 && veteran == maximum, "Rank modifiers or cap wrong.");
        });
        test("Invalid mission, ownership, fog, target, knowledge and funds fail atomically", () =>
        {
            var s = Fresh(); var agent = Recruit(s); var target = s.Cities[1];
            Atomic(s, () => Engine.StartSpyMission(s, -1, target.Id, SpyMission.Sabotage)); Atomic(s, () => Engine.StartSpyMission(s, agent.Id, -1, SpyMission.Sabotage));
            Atomic(s, () => Engine.StartSpyMission(s, agent.Id, s.Cities[0].Id, SpyMission.Sabotage)); Atomic(s, () => Engine.StartSpyMission(s, agent.Id, target.Id, (SpyMission)99)); Atomic(s, () => Engine.StartSpyMission(s, agent.Id, target.Id, SpyMission.Sabotage, 1));
            s.Tiles[target.Tile].Explored = false; Atomic(s, () => Engine.StartSpyMission(s, agent.Id, target.Id, SpyMission.Sabotage)); s.Tiles[target.Tile].Explored = true;
            s.Factions[1].Technologies.Remove("agriculture"); Atomic(s, () => Engine.StartSpyMission(s, agent.Id, target.Id, SpyMission.StealResearch));
            s.Player.Gold = 94.5; Atomic(s, () => Engine.StartSpyMission(s, agent.Id, target.Id, SpyMission.Sabotage));
        });
        test("Recall returns the agent without refund or bypassing the eight-turn city interval", () =>
        {
            var s = Fresh(); var operation = Launch(s); double gold = s.Player.Gold; uint random = s.RandomState;
            Assert(Engine.RecallSpy(s, operation.Id).Success && s.Player.Gold == gold && s.RandomState == random && s.Spies[0].Status == SpyStatus.Available && operation.Outcome == SpyOutcome.Cancelled, "Recall refunded or rolled.");
            Atomic(s, () => Engine.RecallSpy(s, operation.Id)); s.Turn = 8; Atomic(s, () => Engine.StartSpyMission(s, s.Spies[0].Id, operation.CityId, SpyMission.Sabotage)); s.Turn = 9;
            Assert(Engine.StartSpyMission(s, s.Spies[0].Id, operation.CityId, SpyMission.Sabotage).Success, "Interval boundary failed."); SaveGame.Deserialize(SaveGame.Serialize(s));
        });
        test("Reconnaissance reveals three hexes and retains a dated snapshot without live visibility", () =>
        {
            var s = Fresh(); var target = s.Cities[1]; var near = s.Tiles.First(t => World.Distance(t, s.Tiles[target.Tile]) == 3); var far = s.Tiles.First(t => World.Distance(t, s.Tiles[target.Tile]) == 4); near.Explored = far.Explored = false;
            var operation = Launch(s, SpyMission.Reconnaissance); Finish(s, operation);
            Assert(near.Explored && !far.Explored && !Engine.Visible(s).Contains(target.Tile) && operation.Report.Contains("Raport z T4") && operation.Report.Contains("praca 90") && s.Spies[0].Experience == 2, "Recon radius, snapshot or experience wrong.");
            string report = operation.Report; target.Production = 5; target.Population++; s.Turn++; Espionage.Tick(s); Assert(operation.Report == report, "Intelligence snapshot updated live."); SaveGame.Deserialize(SaveGame.Serialize(s));
        });
        test("Research theft copies actual available research into science without an automatic discovery", () =>
        {
            var s = Fresh(); double science = s.Player.Science, victim = s.Factions[1].Science; var operation = Launch(s, SpyMission.StealResearch); Finish(s, operation);
            Assert(operation.ResearchId == "agriculture" && operation.ResultValue == 12 && s.Player.Science == science + 12 && s.Factions[1].Science == victim && !s.Player.Technologies.Contains("agriculture"), "Theft invented a technology or removed the victim's science."); SaveGame.Deserialize(SaveGame.Serialize(s));
        });
        test("Research theft cannot exceed one hundred twenty science for a costly technology", () =>
        {
            var s = Fresh(); s.Player.Technologies.UnionWith(Content.Technologies.Where(t => t.Id != "observatory").Select(t => t.Id)); s.Factions[1].Technologies.Add("observatory");
            var operation = Launch(s, SpyMission.StealResearch); Finish(s, operation); Assert(operation.ResultValue == 120, "Research theft exceeded its cap."); SaveGame.Deserialize(SaveGame.Serialize(s));
        });
        test("Previously learned target research cancels without reward, refund or a random draw", () =>
        {
            var s = Fresh(); var operation = Launch(s, SpyMission.StealResearch); s.Player.Technologies.Add("agriculture"); double gold = s.Player.Gold, science = s.Player.Science; uint random = s.RandomState; s.Turn = operation.DueTurn; Espionage.Tick(s);
            Assert(operation.Outcome == SpyOutcome.Cancelled && s.Player.Gold == gold && s.Player.Science == science && s.RandomState == random && s.Spies[0].Status == SpyStatus.Available, "Obsolete research granted something or trapped the agent."); SaveGame.Deserialize(SaveGame.Serialize(s));
        });
        test("Sabotage removes only accumulated work and preserves buildings, queue and pause", () =>
        {
            foreach (double production in new[] { 90d, 12.5, 0 })
            {
                var s = Fresh(); var target = s.Cities[1]; target.Production = production; target.Buildings.Add("library"); target.ProductionQueue.Add(new() { Project = "scout", Repeat = true }); var operation = Launch(s); Finish(s, operation);
                Assert(target.Production == Math.Max(0, production - 60) && operation.ResultValue == Math.Min(60, production) && target.Buildings.Contains("library") && target.ProductionPaused && target.ProductionQueue.Single().Repeat, "Sabotage destroyed unrelated city state or made negative work."); SaveGame.Deserialize(SaveGame.Serialize(s));
            }
        });
        test("Real end-turn processing resolves reconnaissance on its third turn exactly once", () =>
        {
            var s = Fresh(); var operation = Launch(s, SpyMission.Reconnaissance); Step(s); Step(s); Assert(operation.Outcome == SpyOutcome.Active && s.Turn == 3, "Recon resolved before its deadline."); SetRolls(s, operation, true, false); Step(s);
            Assert(operation.Outcome == SpyOutcome.Succeeded && operation.ResolvedTurn == 4, "Turn pipeline missed reconnaissance."); string before = SaveGame.Serialize(s); Espionage.Tick(s); Assert(SaveGame.Serialize(s) == before, "Resolution paid or rolled twice.");
        });
        test("Successful detected missions keep their effect and cause diplomatic consequences", () =>
        {
            var s = Fresh(); var operation = Launch(s); int trust = s.Relation(0, 1).Trust, grievance = DiplomaticCouncil.Grievances(s, 1, 0); double prestige = s.Player.Prestige; Finish(s, operation, true, true);
            Assert(operation.Outcome == SpyOutcome.Succeeded && operation.Detected && s.Cities[1].Production == 30 && s.Spies[0].Status == SpyStatus.Available && s.Relation(0, 1).Trust == trust - 15 && DiplomaticCouncil.Grievances(s, 1, 0) == grievance + 25 && s.Player.Prestige == prestige - 2 && !SpyCouncil.Schedule(s), "Detected success lost its effect or diplomacy."); SaveGame.Deserialize(SaveGame.Serialize(s));
        });
        test("Undetected failure returns the agent with experience but without an effect or incident", () =>
        {
            var s = Fresh(); var operation = Launch(s); int trust = s.Relation(0, 1).Trust; double prestige = s.Player.Prestige; Finish(s, operation, false, false);
            Assert(operation.Outcome == SpyOutcome.Failed && !operation.Detected && operation.ResultValue == 0 && s.Cities[1].Production == 90 && s.Spies[0].Experience == 1 && s.Spies[0].Status == SpyStatus.Available && s.Relation(0, 1).Trust == trust && s.Player.Prestige == prestige, "Quiet failure caused an incident or a reward."); SaveGame.Deserialize(SaveGame.Serialize(s));
        });
        test("Capture creates a named council and still occupies one of the three roster slots", () =>
        {
            var s = Ready(); Recruit(s); Recruit(s); Atomic(s, () => Engine.RecruitSpy(s)); var current = ProvincialEvents.Current(s);
            Assert(s.Spies[0].Status == SpyStatus.Captured && current.Title.Contains(s.Spies[0].Name) && current.Body.Contains(s.Cities[1].Name) && s.PendingSpyOperationId == 1, "Captured context was unrelated."); Atomic(s, () => Engine.EndTurn(s)); SaveGame.Deserialize(SaveGame.Serialize(s));
        });
        test("Ransom transfers one hundred exactly once and returns the same agent", () =>
        {
            var s = Ready(); double player = s.Player.Gold, captor = s.Factions[1].Gold; int trust = s.Relation(0, 1).Trust;
            Assert(Narrative.Resolve(s, 0).Success && s.Player.Gold == player - 100 && s.Factions[1].Gold == captor + 100 && s.Spies[0].Status == SpyStatus.Available && s.Relation(0, 1).Trust == trust && s.SpyOperations[0].CaptureChoice == 0 && s.PendingSpyOperationId == null, "Ransom duplicated money or did not return the agent.");
            Atomic(s, () => Narrative.Resolve(s, 0)); SaveGame.Deserialize(SaveGame.Serialize(s));
        });
        test("Public apology trades money and prestige for recovery and improved relations", () =>
        {
            var s = Ready(); double gold = s.Player.Gold, captor = s.Factions[1].Gold, prestige = s.Player.Prestige; int commons = s.Player.Commons, trust = s.Relation(0, 1).Trust;
            Assert(Narrative.Resolve(s, 1).Success && s.Player.Gold == gold - 40 && s.Factions[1].Gold == captor + 40 && s.Player.Prestige == prestige - 8 && s.Player.Commons == commons - 3 && s.Relation(0, 1).Trust == trust + 8 && s.Spies[0].Status == SpyStatus.Available, "Apology price or political effect wrong."); SaveGame.Deserialize(SaveGame.Serialize(s));
        });
        test("Wartime and empty treasuries keep a free denial choice without enabling an apology", () =>
        {
            var s = Ready(); s.Relation(0, 1).War = true; WarCouncil.EnsureActive(s); Atomic(s, () => Narrative.Resolve(s, 1)); s.Player.Gold = 0; Atomic(s, () => Narrative.Resolve(s, 0));
            int nobles = s.Player.Nobles; double prestige = s.Player.Prestige; Assert(Narrative.Resolve(s, 2).Success && s.Spies[0].Status == SpyStatus.Lost && s.Player.Prestige == prestige - 4 && s.Player.Nobles == nobles + 3, "Denial did not retire the agent or cost politics.");
            s.Player.Gold = 60; Recruit(s); Assert(s.Spies.Count(a => a.Status != SpyStatus.Lost) == 1, "Lost agent still occupied roster capacity."); SaveGame.Deserialize(SaveGame.Serialize(s));
        });
        test("Existing councils and completed campaigns cannot be overwritten by spy requests", () =>
        {
            var s = Ready(); s.PendingEvent = "grain"; s.PendingSpyOperationId = null; string before = SaveGame.Serialize(s);
            Assert(!SpyCouncil.Schedule(s) && !SpyCouncil.Present(s, 1).Success && SaveGame.Serialize(s) == before, "Spy incident overwrote a council.");
            Narrative.Resolve(s, 2); Assert(SpyCouncil.Present(s, 1).Success, "Deferred incident disappeared."); s.Ending = "Koniec"; Atomic(s, () => Narrative.Resolve(s, 2)); Atomic(s, () => SpyCouncil.Present(s, 1));
        });
        test("Counterintelligence spends sixty, adds twenty-five defense and expires exclusively", () =>
        {
            var s = Fresh(); var city = s.Cities[0]; int defense = Espionage.Defense(s, city); double gold = s.Player.Gold;
            Assert(Engine.ProtectCity(s, city.Id).Success && city.CounterIntelligenceUntil == 9 && s.Player.Gold == gold - 60 && Espionage.Defense(s, city) == defense + 25, "Protection price, duration or defense wrong.");
            Atomic(s, () => Engine.ProtectCity(s, city.Id)); s.Turn = 8; Assert(Espionage.Defense(s, city) == defense + 25, "Protection expired early."); s.Turn = 9; Assert(Espionage.Defense(s, city) == defense && Engine.ProtectCity(s, city.Id).Success, "Protection boundary or renewal wrong."); SaveGame.Deserialize(SaveGame.Serialize(s));
        });
        test("Protection, courts and stability change risks for an operation already underway", () =>
        {
            var s = Fresh(); var target = s.Cities[1]; var operation = Launch(s); int initial = operation.SuccessChance; target.Buildings.Add("court"); s.Factions[1].Stability = 70; Engine.ProtectCity(s, target.Id, 1);
            var forecast = Espionage.Forecast(s, s.Spies[0], target, operation.Mission); Assert(forecast.Defense == 50 && forecast.SuccessChance == Math.Max(20, initial - 45), "Defense components or forecast wrong.");
            Finish(s, operation, false, true); Assert(operation.SuccessChance == forecast.SuccessChance && operation.DetectionChance == forecast.DetectionChance, "Resolution used stale dispatch defense."); SaveGame.Deserialize(SaveGame.Serialize(s));
        });
        test("Counterintelligence rejects foreign cities, missing technology and insufficient money", () =>
        {
            var s = Fresh(); Atomic(s, () => Engine.ProtectCity(s, s.Cities[1].Id)); Atomic(s, () => Engine.ProtectCity(s, -1)); s.Player.Technologies.Remove("writing"); Atomic(s, () => Engine.ProtectCity(s, s.Cities[0].Id));
            s.Player.Technologies.Add("writing"); s.Player.Gold = 59; Atomic(s, () => Engine.ProtectCity(s, s.Cities[0].Id));
        });
        test("Sovereignty changes cancel active missions and clear the former owner's protection", () =>
        {
            var s = Fresh(); var operation = Launch(s); var target = s.Cities[1]; Engine.ProtectCity(s, target.Id, 1); double gold = s.Player.Gold; uint random = s.RandomState;
            ProvincialPolitics.Transfer(s, target, 2); Assert(operation.Outcome == SpyOutcome.Cancelled && target.CounterIntelligenceUntil == 0 && s.Spies[0].Status == SpyStatus.Available && s.Player.Gold == gold && s.RandomState == random, "Conquest kept a stale operation or refunded it."); SaveGame.Deserialize(SaveGame.Serialize(s));
        });
        test("Captives remain held by the original state when their former target city is conquered", () =>
        {
            var s = Ready(); ProvincialPolitics.Transfer(s, s.Cities[1], 2); double captor = s.Factions[1].Gold; Assert(Narrative.Resolve(s, 0).Success && s.Factions[1].Gold == captor + 100 && s.Spies[0].Status == SpyStatus.Available, "Conquest freed a captive or paid the wrong state."); SaveGame.Deserialize(SaveGame.Serialize(s));
        });
        test("Hostile AI pays for its own operations while peaceful states and player control stay untouched", () =>
        {
            var s = Fresh(); s.Turn = 27; s.Player.Technologies.Add("agriculture"); s.Cities[0].Production = 80; string before = SaveGame.Serialize(s); Espionage.ManageAi(s, 1); Assert(SaveGame.Serialize(s) == before, "Peaceful AI launched an operation.");
            s.Relation(0, 1).Trust = -20; double player = s.Player.Gold, foreign = s.Factions[1].Gold; Espionage.ManageAi(s, 1);
            Assert(s.SpyOperations.Single().Owner == 1 && s.SpyOperations[0].TargetOwner == 0 && s.Factions[1].Gold < foreign - 60 && s.Player.Gold == player, "Hostile AI did not finance its own operation.");
            before = SaveGame.Serialize(s); Espionage.ManageAi(s, 0); Espionage.ManageAi(s, -1); Assert(SaveGame.Serialize(s) == before, "AI controlled player finance."); SaveGame.Deserialize(SaveGame.Serialize(s));
        });
        test("AI spies can sabotage the player without revealing attribution when undetected", () =>
        {
            var s = Fresh(); var operation = Launch(s, SpyMission.Sabotage, 1, s.Cities[0].Id); int logs = s.Chronicle.Count; Finish(s, operation, true, false);
            Assert(s.Cities[0].Production == 30 && s.Chronicle.Count == logs && !operation.Detected && s.PendingSpyOperationId == null, "Undetected AI operation revealed itself or had no effect."); SaveGame.Deserialize(SaveGame.Serialize(s));
        });
        test("Captured AI agents in another AI state pay ransom or are abandoned without funds", () =>
        {
            foreach (bool funded in new[] { true, false })
            {
                var s = Fresh(); var operation = Launch(s, SpyMission.Sabotage, 1, s.Cities[2].Id); if (!funded) s.Factions[1].Gold = 0; double gold = s.Factions[2].Gold, foreign = s.Factions[1].Gold; Finish(s, operation, false, true);
                Assert(operation.CaptureChoice == (funded ? 0 : 2) && s.Factions[2].Gold == gold + (funded ? 100 : 0) && s.Factions[1].Gold == foreign - (funded ? 100 : 0) && s.Spies[0].Status == (funded ? SpyStatus.Available : SpyStatus.Lost) && !SpyCouncil.Schedule(s), "AI captivity created money or a player-owned council."); SaveGame.Deserialize(SaveGame.Serialize(s));
            }
        });
        test("AI strengthens defense only after detected intrusion and pays its own protection cost", () =>
        {
            var s = Fresh(); var operation = Launch(s); Finish(s, operation, true, true); double foreign = s.Factions[1].Gold; Espionage.ManageAi(s, 1);
            Assert(s.Cities[1].CounterIntelligenceUntil == s.Turn + 8 && s.Factions[1].Gold == foreign - 60, "AI did not react to a detected spy with funded protection."); SaveGame.Deserialize(SaveGame.Serialize(s));
        });
        test("In-flight outcomes and captured decisions continue identically after reload", () =>
        {
            var s = Fresh(); var operation = Launch(s); s.Turn = operation.DueTurn; SetRolls(s, operation, false, true); s.Turn--; var loaded = SaveGame.Deserialize(SaveGame.Serialize(s));
            s.Turn++; loaded.Turn++; Espionage.Tick(s); Espionage.Tick(loaded); SpyCouncil.Schedule(s); SpyCouncil.Schedule(loaded); Assert(SaveGame.Serialize(s) == SaveGame.Serialize(loaded), "Reload changed outcome rolls.");
            loaded = SaveGame.Deserialize(SaveGame.Serialize(s)); Narrative.Resolve(s, 1); Narrative.Resolve(loaded, 1); Assert(SaveGame.Serialize(s) == SaveGame.Serialize(loaded), "Reload changed captive payments or decisions.");
        });
        test("V14 migration preserves debt, army orders and production without inventing spies", () =>
        {
            var s = StageFourteenSpecs.Ready(); s.Version = 14; var city = s.Cities[0]; city.ProductionQueue.Add(new() { Project = "scout", Repeat = true }); var unit = s.Units.First(); unit.Order = MilitaryOrder.Fortify; unit.FortificationTurns = 1;
            string legacy = SaveGame.Serialize(s); var loaded = SaveGame.Deserialize(legacy);
            Assert(loaded.Version == 16 && loaded.PendingLoanId == 1 && loaded.Loans[0].Principal == 200 && loaded.Spies.Count == 0 && loaded.SpyOperations.Count == 0 && loaded.Cities.All(c => c.CounterIntelligenceUntil == 0) && loaded.Units[0].FortificationTurns == 1 && loaded.Cities[0].ProductionQueue.Single().Repeat, "Migration lost existing state or invented intelligence history.");
            string path = Path.Combine(Environment.CurrentDirectory, "artifacts", "migration-v14-test.crown");
            try { File.WriteAllText(path, legacy); SaveGame.Write(loaded, path); Assert(File.ReadAllText(path + ".v14.bak") == legacy && SaveGame.Read(path).Version == 16, "V14 backup failed."); }
            finally { File.Delete(path); File.Delete(path + ".v14.bak"); }
        });
        test("Spy validation rejects invalid identities, ownership, status, chance, dates and rolls", () =>
        {
            var s = Fresh(); var operation = Launch(s); var agent = s.Spies[0]; agent.Status = SpyStatus.Available; Reject(s); agent.Status = SpyStatus.Deployed;
            agent.Owner = 2; Reject(s); agent.Owner = 0; operation.SuccessChance = 100; Reject(s); operation.SuccessChance = 60; operation.DueTurn++; Reject(s); operation.DueTurn--;
            operation.SuccessRoll = 3; Reject(s); operation.SuccessRoll = null; operation.ResultValue = double.NaN; bool rejected = false; try { SaveGame.Serialize(s); } catch (ArgumentException) { rejected = true; } Assert(rejected, "Nonfinite data serialized.");
            operation.ResultValue = 0; operation.TargetOwner = 0; Reject(s); operation.TargetOwner = 1; s.PendingSpyOperationId = operation.Id; Reject(s);
        });
        test("Spy validation rejects invented effects, reversed capture decisions and resurrected agents", () =>
        {
            var s = Fresh(); var operation = Launch(s); Finish(s, operation); operation.ResultValue = 61; Reject(s); operation.ResultValue = 60; operation.Detected = true; Reject(s);
            s = Ready(); s.SpyOperations[0].CaptureDecisionTurn = 1; Reject(s); s.SpyOperations[0].CaptureDecisionTurn = null; Narrative.Resolve(s, 2); s.Spies[0].Status = SpyStatus.Available; Reject(s);
            var json = JsonNode.Parse(SaveGame.Serialize(Fresh()))!; json["Spies"] = null; bool invalid = false; try { SaveGame.Deserialize(json.ToJsonString()); } catch (InvalidDataException) { invalid = true; } Assert(invalid, "Missing spy registry accepted.");
        });
    }
}
