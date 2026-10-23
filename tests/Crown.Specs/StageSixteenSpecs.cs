using Crown.Core;

internal static class StageSixteenSpecs
{
    private static void Assert(bool value, string message) { if (!value) throw new Exception(message); }
    public static SpyOperation Capture(GameState s, int owner, int target)
    {
        s.Tiles[s.Cities.First(c => c.Owner == target).Tile].Explored = true;
        Assert(Engine.RecruitSpy(s, owner).Success, "Recruit failed.");
        Assert(Engine.StartSpyMission(s, s.Spies.Last().Id, s.Cities.First(c => c.Owner == target).Id, SpyMission.Sabotage, owner).Success, "Launch failed.");
        var o = s.SpyOperations.Last(); s.Turn = o.DueTurn; StageFifteenSpecs.SetRolls(s, o, false, true); Espionage.Tick(s); return o;
    }
    public static GameState Ready()
    { var s = StageFifteenSpecs.Fresh(); Capture(s, 1, 0); Assert(CustodyCouncil.Schedule(s), "No custody council."); return s; }
    public static GameState Pair(bool ownCouncil = false)
    {
        var s = StageFifteenSpecs.Fresh(); Capture(s, 0, 1); Capture(s, 1, 0);
        Assert(ownCouncil ? SpyCouncil.Schedule(s) : CustodyCouncil.Schedule(s), "No exchange council."); return s;
    }
    private static void Atomic(GameState s, Func<Result> action)
    { string before = SaveGame.Serialize(s); Assert(!action().Success && SaveGame.Serialize(s) == before, "Rejected command mutated the world."); }
    private static void Reject(GameState s)
    { bool bad = false; try { SaveGame.Deserialize(SaveGame.Serialize(s)); } catch (InvalidDataException) { bad = true; } Assert(bad, "Invalid custody accepted."); }
    private static void Valid(GameState s) => SaveGame.Deserialize(SaveGame.Serialize(s));
    public static void Run(Action<string, Action> test)
    {
        test("Foreign capture leaves funds and the prisoner's fate under player control", () =>
        {
            var s = StageFifteenSpecs.Fresh(); double gold = s.Player.Gold; var o = Capture(s, 1, 0);
            Assert(o.CaptureChoice == null && o.CustodyChoice == null && s.Spies[0].Status == SpyStatus.Captured && s.Player.Gold == gold, "AI resolved the player's captive.");
            Assert(CustodyCouncil.Schedule(s) && s.PendingSpyOperationId == o.Id && !Engine.EndTurn(s).Success, "No contextual blocking council."); Valid(s);
        });
        test("Custody council describes the actual agent, state, city and exchange candidate", () =>
        {
            var s = Pair(); var e = ProvincialEvents.Current(s);
            Assert(e.Body.Contains(s.Spies[1].Name) && e.Body.Contains(s.Spies[0].Name) && e.Body.Contains(s.Factions[1].Name) && e.Body.Contains(s.Cities[0].Name) && e.Choices.Length == 4, "Missing specific context."); Valid(s);
        });
        test("Ransom demand transfers exactly a hundred from the sender once", () =>
        {
            var s = Ready(); double gold = s.Player.Gold, other = s.Factions[1].Gold; int trust = s.Relation(0, 1).Trust;
            Assert(Narrative.Resolve(s, 0).Success && s.Player.Gold == gold + 100 && s.Factions[1].Gold == other - 100 && s.Spies[0].Status == SpyStatus.Available && s.SpyOperations[0].CustodyChoice == CustodyDecision.Ransom && s.Relation(0, 1).Trust == trust, "Wrong ransom transfer.");
            Atomic(s, () => Narrative.Resolve(s, 0)); Atomic(s, () => CustodyCouncil.Present(s, 1)); Valid(s);
        });
        test("An insolvent sender blocks ransom without blocking the free decisions", () =>
        {
            var s = Ready(); s.Factions[1].Gold = 99.5; Atomic(s, () => Narrative.Resolve(s, 0));
            Assert(CustodyCouncil.ChoiceError(s, 2) == null && CustodyCouncil.ChoiceError(s, 3) == null, "Free decisions unavailable."); s.Factions[1].Gold = 100; Assert(Narrative.Resolve(s, 0).Success && s.Factions[1].Gold == 0, "Exact ransom refused."); Valid(s);
        });
        test("Foreign captive exchange returns the same two agents without gold or erased grievances", () =>
        {
            var s = Pair(); var gold = s.Factions.Select(f => f.Gold).ToArray(); int grievance = DiplomaticCouncil.Grievances(s, 0, 1), trust = s.Relation(0, 1).Trust;
            Assert(Narrative.Resolve(s, 1).Success && s.Spies.All(a => a.Status == SpyStatus.Available) && gold.SequenceEqual(s.Factions.Select(f => f.Gold)) && s.SpyOperations.All(o => o.CaptureChoice == 3 && o.ExchangeOperationId != null) && DiplomaticCouncil.Grievances(s, 0, 1) == grievance && s.Relation(0, 1).Trust == trust, "Exchange changed money or returned wrong people."); Valid(s);
        });
        test("Own captive dialogue offers the same atomic exchange during war and with an empty treasury", () =>
        {
            var s = Pair(true); s.Player.Gold = s.Factions[1].Gold = 0; s.Relation(0, 1).War = true; WarCouncil.EnsureActive(s);
            Assert(SpyCouncil.ChoiceError(s, 3) == null && Narrative.Resolve(s, 3).Success && s.Spies.All(a => a.Status == SpyStatus.Available) && s.Factions.Take(2).All(f => f.Gold == 0), "Wartime exchange failed."); Valid(s);
        });
        test("Exchange rejects missing, resolved, interned or wrong-country partners atomically", () =>
        {
            var s = Ready(); Atomic(s, () => Narrative.Resolve(s, 1)); s = StageFifteenSpecs.Ready(); Atomic(s, () => Narrative.Resolve(s, 3));
            s = Pair(); Assert(Narrative.Resolve(s, 3).Success && SpyCouncil.Schedule(s), "Setup failed."); Atomic(s, () => Narrative.Resolve(s, 3)); Valid(s);
            s = StageFifteenSpecs.Fresh(); Capture(s, 0, 2); Capture(s, 1, 0); CustodyCouncil.Schedule(s); Atomic(s, () => Narrative.Resolve(s, 1)); Valid(s);
        });
        test("An exchange consumes one prisoner pair and leaves other people in custody", () =>
        {
            var s = StageFifteenSpecs.Fresh(); var first = Capture(s, 0, 1); s.Turn = 9; var second = Capture(s, 0, 1); var foreign = Capture(s, 1, 0);
            CustodyCouncil.Schedule(s); Assert(Narrative.Resolve(s, 1).Success && foreign.ExchangeOperationId == first.Id && second.CaptureChoice == null && s.Spies.First(a => a.Id == second.AgentId).Status == SpyStatus.Captured, "Exchanged more than one pair or wrong priority."); Valid(s);
        });
        test("Release improves trust, costs noble support and rewards prestige without transferring gold", () =>
        {
            var s = Ready(); var gold = s.Factions.Select(f => f.Gold).ToArray(); int trust = s.Relation(0, 1).Trust, nobles = s.Player.Nobles; double prestige = s.Player.Prestige;
            Assert(Narrative.Resolve(s, 2).Success && s.Spies[0].Status == SpyStatus.Available && s.SpyOperations[0].CaptureChoice == 4 && s.Relation(0, 1).Trust == trust + 12 && s.Player.Nobles == nobles - 3 && s.Player.Prestige == prestige + 4 && gold.SequenceEqual(s.Factions.Select(f => f.Gold)), "Release effects incorrect."); Valid(s);
        });
        test("Release trust is clamped and is available during war", () =>
        {
            var s = Ready(); s.Relation(0, 1).Trust = 98; s.Relation(0, 1).War = true; WarCouncil.EnsureActive(s);
            Assert(Narrative.Resolve(s, 2).Success && s.Relation(0, 1).Trust == 100 && s.Relation(0, 1).War, "Release ended a war or exceeded trust."); Valid(s);
        });
        test("Internment keeps the person and roster slot for eight turns with a diplomatic cost", () =>
        {
            var s = Ready(); int turn = s.Turn, trust = s.Relation(0, 1).Trust, commons = s.Player.Commons;
            Assert(Narrative.Resolve(s, 3).Success && s.Spies[0].Status == SpyStatus.Captured && s.SpyOperations[0].InternedUntil == turn + 8 && s.Relation(0, 1).Trust == trust - 8 && s.Player.Commons == commons + 3, "Internment not enacted.");
            Engine.RecruitSpy(s, 1); Engine.RecruitSpy(s, 1); Assert(!Engine.RecruitSpy(s, 1).Success, "Interned person did not count toward the limit."); Atomic(s, () => CustodyCouncil.Present(s, 1)); Valid(s);
        });
        test("Internment expires once at the start of its final turn and frees the same agent", () =>
        {
            var s = Ready(); Narrative.Resolve(s, 3); var o = s.SpyOperations[0]; int due = o.InternedUntil!.Value; double gold = s.Player.Gold;
            s.Turn = due - 1; Espionage.Tick(s); Assert(s.Spies[0].Status == SpyStatus.Captured, "Released early.");
            s.Turn++; Espionage.Tick(s); Assert(s.Spies[0].Status == SpyStatus.Available && o.CaptureChoice == 5 && o.CaptureDecisionTurn == due && s.Player.Gold == gold, "Expiry failed or invented payment.");
            string before = SaveGame.Serialize(s); Espionage.Tick(s); Assert(SaveGame.Serialize(s) == before, "Repeated release."); Valid(s);
        });
        test("A real end turn releases an interned agent through the normal intelligence phase", () =>
        {
            var s = Ready(); Narrative.Resolve(s, 3); s.Turn = s.SpyOperations[0].InternedUntil!.Value - 1;
            Assert(Engine.EndTurn(s).Success && s.SpyOperations[0].CaptureChoice == 5 && s.Spies[0].Status == SpyStatus.Available, "End-turn did not release internment."); Valid(s);
        });
        test("Conquest changes neither the captor nor an internment deadline", () =>
        {
            var s = Ready(); Narrative.Resolve(s, 3); int? due = s.SpyOperations[0].InternedUntil; ProvincialPolitics.Transfer(s, s.Cities[0], 2);
            Assert(s.SpyOperations[0].TargetOwner == 0 && s.SpyOperations[0].InternedUntil == due && s.Spies[0].Status == SpyStatus.Captured, "Conquest transferred custody."); s.Turn = due!.Value; Espionage.Tick(s); Valid(s);
        });
        test("Custody waits behind an existing council and precedes own ransom when both are waiting", () =>
        {
            var s = StageFifteenSpecs.Fresh(); Capture(s, 0, 1); Capture(s, 1, 0); s.PendingEvent = "grain"; string before = SaveGame.Serialize(s);
            Assert(!CustodyCouncil.Schedule(s) && SaveGame.Serialize(s) == before, "Overwrote existing council."); Narrative.Resolve(s, 2); Narrative.Schedule(s);
            Assert(s.PendingEvent == CustodyCouncil.Event.Id, "Own ransom hid the opportunity to exchange."); Valid(s);
        });
        test("Present rejects wrong ids, already-decided cases, other councils and ended campaigns", () =>
        {
            var s = Ready(); Atomic(s, () => CustodyCouncil.Present(s, -1)); s.PendingEvent = "grain"; s.PendingSpyOperationId = null; Atomic(s, () => CustodyCouncil.Present(s, 1));
            s.PendingEvent = null; Assert(CustodyCouncil.Present(s, 1).Success, "Cannot reopen own case."); s.Ending = "Koniec"; Atomic(s, () => CustodyCouncil.Present(s, 1)); Atomic(s, () => Narrative.Resolve(s, 2));
        });
        test("No captor decision is offered for undetected failures or detected successes", () =>
        {
            foreach (var result in new[] { (false, false), (true, true) })
            {
                var s = StageFifteenSpecs.Fresh(); Engine.RecruitSpy(s, 1); Engine.StartSpyMission(s, s.Spies[0].Id, s.Cities[0].Id, SpyMission.Sabotage, 1); var o = s.SpyOperations[0];
                s.Turn = o.DueTurn; StageFifteenSpecs.SetRolls(s, o, result.Item1, result.Item2); Espionage.Tick(s); Assert(!CustodyCouncil.Schedule(s), "Noncaptive received a custody council."); Valid(s);
            }
        });
        test("Save and reload preserve open custody, exchange links and delayed release identically", () =>
        {
            var s = Ready(); var loaded = SaveGame.Deserialize(SaveGame.Serialize(s)); Assert(SaveGame.Serialize(s) == SaveGame.Serialize(loaded), "Open case changed.");
            Narrative.Resolve(s, 3); Narrative.Resolve(loaded, 3); s.Turn += 8; loaded.Turn += 8; Espionage.Tick(s); Espionage.Tick(loaded); Assert(SaveGame.Serialize(s) == SaveGame.Serialize(loaded), "Reload changed internment.");
            s = Pair(); Narrative.Resolve(s, 1); loaded = SaveGame.Deserialize(SaveGame.Serialize(s)); Assert(SaveGame.Serialize(s) == SaveGame.Serialize(loaded), "Exchange links lost.");
        });
        test("V15 migration preserves captured people and old ransom decisions without invented custody", () =>
        {
            var s = StageFifteenSpecs.Ready(); s.Version = 15; var loaded = SaveGame.Deserialize(SaveGame.Serialize(s));
            Assert(loaded.Version == 18 && loaded.Spies[0].Status == SpyStatus.Captured && loaded.PendingSpyOperationId == 1 && loaded.SpyOperations.All(o => o.CustodyChoice == null && o.InternedUntil == null), "Migration lost a captive.");
            Narrative.Resolve(s, 0); string legacy = SaveGame.Serialize(s); loaded = SaveGame.Deserialize(legacy); Assert(loaded.SpyOperations[0].CaptureChoice == 0 && loaded.Spies[0].Status == SpyStatus.Available, "Migration reopened a paid ransom.");
            string path = Path.Combine(Environment.CurrentDirectory, "artifacts", "migration-v15-test.crown");
            try { File.WriteAllText(path, legacy); SaveGame.Write(loaded, path); Assert(File.ReadAllText(path + ".v15.bak") == legacy && SaveGame.Read(path).Version == 18, "V15 backup failed."); }
            finally { File.Delete(path); File.Delete(path + ".v15.bak"); }
        });
        test("Custody validation rejects false context, missing decisions and invalid imprisonment dates", () =>
        {
            var s = Ready(); s.PendingEvent = SpyCouncil.Event.Id; Reject(s); s.PendingEvent = CustodyCouncil.Event.Id; s.PendingSpyOperationId = null; Reject(s);
            s = Ready(); Narrative.Resolve(s, 3); var o = s.SpyOperations[0]; o.InternedUntil++; Reject(s); o.InternedUntil--; o.CustodyTurn = 1; Reject(s);
            s = Ready(); Narrative.Resolve(s, 2); o = s.SpyOperations[0]; o.CustodyChoice = null; Reject(s); o.CustodyChoice = CustodyDecision.Release; o.CaptureChoice = 0; Reject(s);
        });
        test("V15 migration keeps previously resolved foreign captives closed", () =>
        {
            foreach (int decision in new[] { 0, 2 })
            {
                var s = StageFifteenSpecs.Fresh(); var o = Capture(s, 1, 0); o.CaptureChoice = decision; o.CaptureDecisionTurn = s.Turn;
                s.Spies[0].Status = decision == 2 ? SpyStatus.Lost : SpyStatus.Available; s.Version = 15; var funds = s.Factions.Select(f => f.Gold).ToArray();
                var loaded = SaveGame.Deserialize(SaveGame.Serialize(s));
                Assert(!CustodyCouncil.Schedule(loaded) && loaded.SpyOperations[0].CaptureChoice == decision && loaded.SpyOperations[0].CustodyChoice == null && loaded.Spies[0].Status == s.Spies[0].Status && funds.SequenceEqual(loaded.Factions.Select(f => f.Gold)), "Migration reopened or charged a historical foreign capture.");
            }
        });
        test("V15 migration rejects a missing spy registry with a controlled invalid-data error", () =>
        {
            var s = StageFifteenSpecs.Fresh(); s.Version = 15; s.SpyOperations = null!; Reject(s);
        });
        test("Custody validation rejects early freedom, stale captivity and forged prisoner exchanges", () =>
        {
            var s = Ready(); Narrative.Resolve(s, 3); s.Spies[0].Status = SpyStatus.Available; Reject(s); s.Spies[0].Status = SpyStatus.Captured; s.Turn = s.SpyOperations[0].InternedUntil!.Value; Reject(s);
            s = Pair(); Narrative.Resolve(s, 1); s.SpyOperations[0].ExchangeOperationId = null; Reject(s); s.SpyOperations[0].ExchangeOperationId = 2; s.SpyOperations[1].ExchangeOperationId = 2; Reject(s);
            s = Pair(); Narrative.Resolve(s, 1); s.SpyOperations[0].CaptureDecisionTurn--; Reject(s);
        });
        test("Returned captives can deploy again while both sides of an exchange remain valid history", () =>
        {
            var s = Pair(); Narrative.Resolve(s, 1); s.Turn = 20; var agent = s.Spies.First(a => a.Owner == 0); s.Tiles[s.Cities[2].Tile].Explored = true;
            Assert(Engine.StartSpyMission(s, agent.Id, s.Cities[2].Id, SpyMission.Reconnaissance).Success && agent.Status == SpyStatus.Deployed && s.SpyOperations[0].CaptureChoice == 3, "Exchanged person cannot be redeployed."); Valid(s);
        });
    }
}
