using Crown.Core;

internal static class StageThirteenSpecs
{
    private static void Assert(bool value, string text) { if (!value) throw new Exception(text); }
    private static (GameState S, Unit U) Fixture()
    {
        var s = World.Create(); Narrative.Resolve(s, 0); s.Units.Clear();
        foreach (var t in s.Tiles) { t.Terrain = Terrain.Grassland; t.Explored = true; t.Ruin = false; t.Owner = -1; t.Road = false; t.RoadOwner = -1; }
        foreach (var c in s.Cities) { s.Tiles[c.Tile].Owner = c.Owner; c.ProductionPaused = true; }
        foreach (var f in s.Factions) f.Gold = 4000;
        var u = Engine.AddUnit(s, 0, s.Cities[0].Tile, UnitKind.Spearmen); s.InvalidateLogistics(); return (s, u);
    }
    private static void Step(GameState s)
    {
        if (s.PendingEvent != null)
        { var e = ProvincialEvents.Current(s); Assert(Narrative.Resolve(s, Array.FindIndex(e.Choices, c => s.Player.Gold + c.Gold >= 0)).Success, "Council failed."); }
        Assert(Engine.EndTurn(s).Success, "Turn failed.");
    }
    private static void Order(GameState s, Unit u, MilitaryOrder order) => Assert(Engine.SetMilitaryOrder(s, u.Id, order, u.Owner).Success, "Order refused.");
    private static void Atomic(GameState s, Func<Result> action)
    { string before = SaveGame.Serialize(s); Assert(!action().Success && SaveGame.Serialize(s) == before, "Rejected action changed state."); }
    private static void Reject(GameState s)
    { bool failed = false; try { SaveGame.Deserialize(SaveGame.Serialize(s)); } catch (InvalidDataException) { failed = true; } Assert(failed, "Invalid military save accepted."); }
    private static Unit Attacker(GameState s, Unit defender, UnitKind kind = UnitKind.Spearmen)
    {
        s.Relation(0, 1).War = true; WarCouncil.EnsureActive(s);
        return Engine.AddUnit(s, 1, World.Neighbors(s, defender.Tile).First(t => t.Passable && !s.Cities.Any(c => c.Tile == t.Id)).Id, kind);
    }
    public static void Run(Action<string, Action> test)
    {
        test("Fortification spends all movement, replaces a march and grants no instant bonus", () =>
        {
            var (s, u) = Fixture(); u.Destination = u.Tile + 4; u.Health = 60; double gold = s.Player.Gold; int tile = u.Tile;
            Order(s, u, MilitaryOrder.Fortify);
            Assert(u.Actions == 0 && u.Destination == null && u.Tile == tile && u.Health == 60 && s.Player.Gold == gold && u.FortificationTurns == 0, "Preparation moved, healed, charged or gained an instant defense.");
        });
        test("Fortification grows once per completed turn and caps at forty percent", () =>
        {
            var (s, u) = Fixture(); Order(s, u, MilitaryOrder.Fortify); Step(s);
            Assert(u.FortificationTurns == 1 && MilitaryOrders.DefenseMultiplier(u) == 1.2 && u.Actions == u.MaxActions, "First phase wrong.");
            Step(s); Step(s); Assert(u.FortificationTurns == 2 && MilitaryOrders.DefenseMultiplier(u) == 1.4, "Readiness exceeded its cap or failed to mature.");
        });
        test("Wake and repeated preparation cannot refund movement or create readiness", () =>
        {
            var (s, u) = Fixture(); Order(s, u, MilitaryOrder.Fortify); Assert(Engine.WakeUnit(s, u.Id).Success && u.Actions == 0 && u.FortificationTurns == 0, "Wake refunded movement.");
            Atomic(s, () => Engine.SetMilitaryOrder(s, u.Id, MilitaryOrder.Fortify)); Atomic(s, () => Engine.WakeUnit(s, u.Id));
        });
        test("Orders reject exhausted, foreign, settler, unknown and ended inputs atomically", () =>
        {
            var (s, u) = Fixture(); u.Actions = 1.5; Atomic(s, () => Engine.SetMilitaryOrder(s, u.Id, MilitaryOrder.Fortify)); u.Actions = u.MaxActions;
            Atomic(s, () => Engine.SetMilitaryOrder(s, u.Id, (MilitaryOrder)99)); Atomic(s, () => Engine.SetMilitaryOrder(s, u.Id, MilitaryOrder.None));
            Atomic(s, () => Engine.SetMilitaryOrder(s, -1, MilitaryOrder.Fortify)); Atomic(s, () => Engine.SetMilitaryOrder(s, u.Id, MilitaryOrder.Fortify, 9));
            u.Owner = 1; Atomic(s, () => Engine.SetMilitaryOrder(s, u.Id, MilitaryOrder.Fortify)); Atomic(s, () => Engine.WakeUnit(s, u.Id)); u.Owner = 0;
            u.Kind = UnitKind.Settler; Atomic(s, () => Engine.SetMilitaryOrder(s, u.Id, MilitaryOrder.Fortify)); u.Kind = UnitKind.Spearmen;
            s.Ending = "Koniec"; Atomic(s, () => Engine.SetMilitaryOrder(s, u.Id, MilitaryOrder.Fortify));
        });
        test("A duplicate standing order neither consumes readiness nor starts it again", () =>
        {
            var (s, u) = Fixture(); Order(s, u, MilitaryOrder.Fortify); Step(s); Atomic(s, () => Engine.SetMilitaryOrder(s, u.Id, MilitaryOrder.Fortify));
        });
        test("Pure combat preview multiplies unit defense before adding terrain", () =>
        {
            var (s, u) = Fixture(); Order(s, u, MilitaryOrder.Fortify); Step(s); Step(s); s.Tiles[u.Tile].Terrain = Terrain.Hills; var attacker = Attacker(s, u);
            string before = SaveGame.Serialize(s); var p = Engine.PreviewAttack(s, attacker.Id, u.Tile);
            Assert(p.Valid && Math.Abs(p.DefenseStrength - (Engine.Strength(s, u) * 1.4 + 5)) < 1e-8 && p.Modifiers!.Any(m => m.Contains("40%")) && SaveGame.Serialize(s) == before, "Defense formula or preview purity wrong.");
        });
        test("Actual combat matches fortified preview and preserves the defender's position", () =>
        {
            var (s, u) = Fixture(); Order(s, u, MilitaryOrder.Fortify); Step(s); var attacker = Attacker(s, u); var p = Engine.PreviewAttack(s, attacker.Id, u.Tile);
            Assert(Engine.Move(s, attacker.Id, u.Tile).Success && Math.Abs(u.Health - (100 - p.DamageToTarget)) < 1e-8 && Math.Abs(attacker.Health - (100 - p.DamageToAttacker)) < 1e-8 && u.Order == MilitaryOrder.Fortify && u.FortificationTurns == 1, "Battle diverged or wiped defensive works.");
        });
        test("Ranged damage respects fortifications without a counterattack", () =>
        {
            var (s, u) = Fixture(); Order(s, u, MilitaryOrder.Fortify); Step(s); var attacker = Attacker(s, u, UnitKind.Archer); var p = Engine.PreviewAttack(s, attacker.Id, u.Tile);
            Assert(p.Valid && p.Ranged && p.DamageToAttacker == 0 && Engine.Move(s, attacker.Id, u.Tile).Success && attacker.Health == 100 && Math.Abs(u.Health - (100 - p.DamageToTarget)) < 1e-8, "Ranged fortification result wrong.");
        });
        test("Prepared nearby troops never multiply an ungarrisoned city's walls", () =>
        {
            var (s, u) = Fixture(); var city = s.Cities[0]; city.Buildings.Add("walls"); var attacker = Attacker(s, u); u.Tile += 2;
            Order(s, u, MilitaryOrder.Fortify); u.FortificationTurns = 2;
            var p = Engine.PreviewAttack(s, attacker.Id, city.Tile);
            Assert(p.Valid && p.DefenseStrength == 20 + city.Population * 1.5 + 10 + s.Player.Era * 3 && !p.Modifiers!.Any(m => m.Contains("Umocnienia przeciwnika")), "Unit preparation leaked into a city's own defense.");
        });
        test("Launching an attack abandons works and never boosts offensive strength", () =>
        {
            var (s, u) = Fixture(); Order(s, u, MilitaryOrder.Fortify); Step(s); Step(s); var enemy = Attacker(s, u); var p = Engine.PreviewAttack(s, u.Id, enemy.Tile);
            Assert(p.AttackStrength == Engine.Strength(s, u) && Engine.Move(s, u.Id, enemy.Tile).Success && u.Order == MilitaryOrder.None && u.FortificationTurns == 0, "Works boosted attack or survived an offensive.");
        });
        test("A successful march clears works while rejected movement preserves them", () =>
        {
            var (s, u) = Fixture(); Order(s, u, MilitaryOrder.Fortify); Step(s); Atomic(s, () => Engine.Move(s, u.Id, -1)); Atomic(s, () => Engine.OrderMove(s, u.Id, -1));
            Assert(Engine.Move(s, u.Id, u.Tile + 1).Success && u.Order == MilitaryOrder.None && u.FortificationTurns == 0, "March carried works to another tile.");
        });
        test("Queued movement breaks preparation even before its first step without healing", () =>
        {
            var (s, u) = Fixture(); u.Health = 50; Order(s, u, MilitaryOrder.Fortify); int start = u.Tile;
            Assert(Engine.OrderMove(s, u.Id, start + 6).Success && u.Tile == start && u.Order == MilitaryOrder.None && u.Destination != null, "Exhausted march kept a defensive order.");
            Step(s); Assert(u.Tile != start && u.Health == 50 && u.FortificationTurns == 0, "Moving order received preparation healing.");
        });
        test("Pillaging a road consumes movement and abandons fortifications", () =>
        {
            var (s, u) = Fixture(); Order(s, u, MilitaryOrder.Fortify); Step(s); s.Relation(0, 1).War = true; var tile = s.Tiles[u.Tile]; tile.Road = true; tile.RoadOwner = 1;
            Assert(Engine.PillageRoad(s, u.Id).Success && u.Order == MilitaryOrder.None && u.FortificationTurns == 0 && u.Actions == 1 && !tile.Road, "Pillage kept preparation.");
        });
        test("Escort assignment abandons works and prevents conflicting rest commands", () =>
        {
            var (s, u) = Fixture(); Order(s, u, MilitaryOrder.Fortify); Step(s);
            s.TradeContracts.Add(new() { Id = 1, Buyer = 0, Seller = 1 }); s.Caravans.Add(new() { Id = 1, ContractId = 1, Tile = u.Tile });
            Assert(Engine.AssignEscort(s, u.Id, 1).Success && u.Order == MilitaryOrder.None && u.FortificationTurns == 0, "Escort inherited works.");
            Atomic(s, () => Engine.SetMilitaryOrder(s, u.Id, MilitaryOrder.Recover)); Atomic(s, () => Engine.SetMilitaryOrder(s, u.Id, MilitaryOrder.Fortify));
        });
        test("Recovery heals faster, finishes at full health and wakes exactly once", () =>
        {
            var (s, u) = Fixture(); u.Health = 55; Order(s, u, MilitaryOrder.Recover); Step(s); Assert(u.Health == 75 && u.Order == MilitaryOrder.Recover, "Recovery did not heal twenty.");
            Step(s); Assert(u.Health == 95, "Second treatment wrong."); Step(s); Assert(u.Health == 100 && u.Order == MilitaryOrder.None && u.Actions == u.MaxActions, "Recovery did not finish.");
            Step(s); Assert(s.Chronicle.Count(c => c.Text.Contains("rekonwalescencja zakończona")) == 1, "Recovery wake repeated.");
        });
        test("Medicine improves supplied recovery and ordinary rest at their own rates", () =>
        {
            var (s, u) = Fixture(); s.Player.Technologies.Add("medicine"); u.Health = 40; var ordinary = Engine.AddUnit(s, 0, u.Tile, UnitKind.Archer); ordinary.Health = 40;
            Order(s, u, MilitaryOrder.Recover); Step(s); Assert(u.Health == 68 && ordinary.Health == 60, "Medicine recovery rates wrong.");
        });
        test("Ordinary fortification heals normally rather than receiving recovery twice", () =>
        {
            var (s, u) = Fixture(); u.Health = 40; Order(s, u, MilitaryOrder.Fortify); Step(s); Step(s); Assert(u.Health == 64 && u.FortificationTurns == 2, "Fortification double-healed.");
        });
        test("Unsupplied recovery applies weak healing then attrition and never falsely completes", () =>
        {
            var (s, u) = Fixture(); u.Tile = 15 * s.Width + 21; u.Health = 99; s.InvalidateLogistics(); Assert(!Engine.Supplied(s, u), "Unsupplied fixture supplied.");
            Order(s, u, MilitaryOrder.Recover); Step(s); Assert(u.Health == 95 && u.Order == MilitaryOrder.Recover, "Unsupplied recovery skipped attrition or woke too early.");
            Step(s); Assert(u.Health == 93, "Unsupplied recovery treated lost health as a full treatment.");
        });
        test("Unsupplied scout recovery retains its existing attrition exemption", () =>
        {
            var (s, u) = Fixture(); u.Kind = UnitKind.Scout; u.Actions = u.MaxActions; u.Tile = 15 * s.Width + 21; u.Health = 98; Order(s, u, MilitaryOrder.Recover); Step(s);
            Assert(u.Health == 100 && u.Order == MilitaryOrder.None, "Scout suffered military attrition.");
        });
        test("Enemy fire interrupts recovery without granting any defensive works", () =>
        {
            var (s, u) = Fixture(); u.Health = 60; Order(s, u, MilitaryOrder.Recover); var enemy = Attacker(s, u, UnitKind.Archer); var p = Engine.PreviewAttack(s, enemy.Id, u.Tile);
            Assert(p.DefenseStrength == Engine.Strength(s, u) && Engine.Move(s, enemy.Id, u.Tile).Success && u.Order == MilitaryOrder.None && u.FortificationTurns == 0 && u.Actions == 0, "Enemy attack did not cancel recovery or refunded movement.");
            double health = u.Health; s.Units.Remove(enemy); Step(s); Assert(u.Health == health, "Interrupted recovery healed in the same phase.");
        });
        test("Switching from entrenchment to recovery discards all old preparation", () =>
        {
            var (s, u) = Fixture(); Order(s, u, MilitaryOrder.Fortify); Step(s); Step(s); u.Health = 50; Order(s, u, MilitaryOrder.Recover);
            Assert(u.FortificationTurns == 0 && u.Actions == 0, "Switch retained works."); Atomic(s, () => Engine.SetMilitaryOrder(s, u.Id, MilitaryOrder.Fortify)); Step(s); Assert(u.Health == 70, "Switch did not use one recovery phase.");
        });
        test("Full-health units cannot buy unnecessary recovery", () =>
        {
            var (s, u) = Fixture(); Atomic(s, () => Engine.SetMilitaryOrder(s, u.Id, MilitaryOrder.Recover));
        });
        test("Evacuation after sovereignty transfer removes works from the displaced unit", () =>
        {
            var (s, u) = Fixture(); var refuge = Engine.AddCity(s, 0, 20 * s.Width + 10, "Schronienie"); Order(s, u, MilitaryOrder.Fortify); Step(s);
            ProvincialPolitics.Transfer(s, s.Cities[0], 1); Assert(u.Tile == refuge.Tile && u.Actions == 0 && u.Order == MilitaryOrder.None && u.FortificationTurns == 0, "Evacuation carried works into refuge."); SaveGame.Deserialize(SaveGame.Serialize(s));
        });
        test("AI prepares stationary garrisons and treats its supplied wounded", () =>
        {
            var (s, _) = Fixture(); var guard = Engine.AddUnit(s, 1, s.Cities.First(c => c.Owner == 1).Tile, UnitKind.Spearmen);
            var wounded = Engine.AddUnit(s, 2, s.Cities.First(c => c.Owner == 2).Tile, UnitKind.Archer); wounded.Health = 20; Step(s);
            Assert(guard.Order == MilitaryOrder.Fortify && guard.Actions == 0 && wounded.Order == MilitaryOrder.Recover, "AI did not assign stationary orders.");
            Step(s); Assert(guard.FortificationTurns == 1 && wounded.Health == 52 && wounded.Order == MilitaryOrder.Recover, "AI repeatedly restarted its preparation or recovery.");
        });
        test("Peace withdrawal removes prepared works instead of transporting them home", () =>
        {
            var (s, u) = Fixture(); s.Relation(0, 1).War = true; s.Relation(0, 1).Trust = 50; WarCouncil.EnsureActive(s);
            u.Tile = s.Cities.First(c => c.Owner == 1).Tile; Order(s, u, MilitaryOrder.Fortify); u.FortificationTurns = 1;
            Assert(Engine.ProposePeace(s, 1, 200).Success, "Peace proposal failed."); s.Turn = 3; PeaceCouncil.Tick(s);
            Assert(!s.Relation(0, 1).War && u.Tile == s.Cities[0].Tile && u.Actions == 0 && u.Order == MilitaryOrder.None && u.FortificationTurns == 0, "Peace moved works along with the withdrawing unit."); SaveGame.Deserialize(SaveGame.Serialize(s));
        });
        test("Market projections retain and never mutate standing military orders", () =>
        {
            var (s, u) = Fixture(); Order(s, u, MilitaryOrder.Fortify); Step(s); string before = SaveGame.Serialize(s);
            Markets.Plan(s); Markets.Plan(s); Assert(SaveGame.Serialize(s) == before && u.FortificationTurns == 1, "Trade forecast changed military preparations.");
        });
        test("Standing orders and partially completed works continue identically after reload", () =>
        {
            var (s, u) = Fixture(); Order(s, u, MilitaryOrder.Fortify); Step(s); var recovery = Engine.AddUnit(s, 0, u.Tile, UnitKind.Archer); recovery.Health = 40; Order(s, recovery, MilitaryOrder.Recover);
            var loaded = SaveGame.Deserialize(SaveGame.Serialize(s)); Assert(SaveGame.Serialize(s) == SaveGame.Serialize(loaded), "Roundtrip changed orders.");
            Step(s); Step(loaded); Assert(SaveGame.Serialize(s) == SaveGame.Serialize(loaded), "Resumed treatment or readiness diverged.");
        });
        test("V12 migration retains production and audit data without inventing prepared positions", () =>
        {
            var (s, u) = Fixture(); var c = s.Cities[0]; c.ProductionQueue.Add(new() { Project = "scout", Repeat = true }); c.HiddenGold = 18; c.Production = 37; s.Version = 12;
            u.Order = MilitaryOrder.Fortify; u.FortificationTurns = 2; string legacy = SaveGame.Serialize(s); var loaded = SaveGame.Deserialize(legacy);
            Assert(loaded.Version == 15 && loaded.Units.All(v => v.Order == MilitaryOrder.None && v.FortificationTurns == 0) && loaded.Cities[0].HiddenGold == 18 && loaded.Cities[0].Production == 37 && loaded.Cities[0].ProductionPaused && loaded.Cities[0].ProductionQueue.Single().Repeat, "Migration lost economic plans or invented works.");
            string path = Path.Combine(Environment.CurrentDirectory, "artifacts", "migration-v12-test.crown");
            try { File.WriteAllText(path, legacy); SaveGame.Write(loaded, path); Assert(File.ReadAllText(path + ".v12.bak") == legacy && SaveGame.Read(path).Version == 15, "V12 backup failed."); }
            finally { File.Delete(path); File.Delete(path + ".v12.bak"); }
        });
        test("Military save validation rejects impossible readiness and conflicting orders", () =>
        {
            var (s, u) = Fixture(); u.Order = (MilitaryOrder)99; Reject(s); u.Order = MilitaryOrder.None; u.FortificationTurns = 1; Reject(s);
            u.Order = MilitaryOrder.Fortify; u.FortificationTurns = 3; Reject(s); u.FortificationTurns = -1; Reject(s); u.FortificationTurns = 1;
            u.Destination = u.Tile + 2; Reject(s); u.Destination = null; u.EscortCaravanId = 99; Reject(s); u.EscortCaravanId = null;
            u.Kind = UnitKind.Settler; Reject(s); u.Kind = UnitKind.Spearmen; u.Order = MilitaryOrder.Recover; Reject(s);
        });
    }
}
