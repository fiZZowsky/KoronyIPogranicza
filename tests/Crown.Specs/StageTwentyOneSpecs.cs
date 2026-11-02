using Crown.Core;
using System.Text.Json;

internal static class StageTwentyOneSpecs
{
    private static void Assert(bool condition, string message) { if (!condition) throw new Exception(message); }
    internal static GameState Fixture()
    {
        var s = World.Create(); s.PendingEvent = null; s.Units.Clear();
        foreach (var t in s.Tiles) { t.Terrain = Terrain.Grassland; t.Owner = -1; t.Resource = Resource.None; t.Improvement = Improvement.None; t.Ruin = t.Road = t.River = false; t.RoadOwner = -1; t.Explored = true; }
        (int X, int Y)[] positions = [(12, 12), (20, 12), (30, 20), (6, 5)];
        foreach (var c in s.Cities)
        {
            c.Tile = positions[c.Owner].Y * s.Width + positions[c.Owner].X; c.ProductionPaused = true;
            foreach (var t in s.Tiles.Where(t => World.Distance(t, s.Tiles[c.Tile]) <= 2)) t.Owner = c.Owner;
        }
        foreach (var f in s.Factions) f.Gold = 10000;
        s.InvalidateLogistics(); return s;
    }
    private static Unit Unit(GameState s, int owner, int x, int y, UnitKind kind = UnitKind.Spearmen) => Engine.AddUnit(s, owner, y * s.Width + x, kind);
    private static void War(GameState s, int other = 0) { s.Relation(1, other).War = true; s.InvalidateLogistics(); }
    private static void Tech(GameState s, int owner = 1)
    { s.Factions[owner].Technologies.UnionWith(EpochCatalog.All.Take(5).SelectMany(e => e.Technologies)); for (int i = 0; i < 5; i++) s.Factions[owner].EpochChoices[i] = 0; }
    public static void Run(Action<string, Action> test)
    {
        test("Military sight matches the player's existing map visibility", () => { var s = World.Create(); Assert(MilitaryStrategy.Sight(s, 0).SetEquals(Engine.Visible(s)), "Sight rules diverged"); });
        test("Threat assessment counts only currently observed wartime combat troops", () =>
        { var s = Fixture(); War(s); Unit(s, 0, 22, 12); Unit(s, 0, 25, 12); Unit(s, 2, 21, 12); Unit(s, 0, 21, 13, UnitKind.Settler); Assert(MilitaryStrategy.Threats(s, 1).Single().Enemies == 1, "Hidden, peaceful or civilian troops leaked"); });
        test("AI observation does not depend on the player's explored flags", () =>
        { var s = Fixture(); War(s); Unit(s, 0, 22, 12); var before = MilitaryStrategy.Threats(s, 1).Single(); foreach (var t in s.Tiles) t.Explored = false; Assert(MilitaryStrategy.Threats(s, 1).Single() == before, "AI borrowed player exploration"); });
        test("An observed hostile scout still counts as a fighting threat to a city", () =>
        { var s = Fixture(); War(s); Unit(s, 0, 22, 12, UnitKind.Scout); Assert(MilitaryStrategy.Threats(s, 1).Single().Enemies == 1, "A scout able to raid or capture was ignored"); });
        test("Military plans are read only deterministic and consume no campaign RNG", () =>
        { var s = Fixture(); War(s); Unit(s, 1, 20, 12); Unit(s, 1, 19, 12, UnitKind.Catapult); string before = SaveGame.Serialize(s); string a = JsonSerializer.Serialize(MilitaryStrategy.Plan(s, 1)); string b = JsonSerializer.Serialize(MilitaryStrategy.Plan(s, 1)); Assert(a == b && SaveGame.Serialize(s) == before, "Planning changed state or RNG"); });
        test("Settlers scouts and convoy escorts never receive field army assignments", () =>
        { var s = Fixture(); Unit(s, 1, 20, 12, UnitKind.Settler); Unit(s, 1, 20, 13, UnitKind.Scout); Unit(s, 1, 20, 14).EscortCaravanId = 99; Assert(MilitaryStrategy.Plan(s, 1).Assignments.Count == 0, "Civilian or escort commandeered"); });
        test("Wartime city security reserves infantry before ranged field troops", () =>
        { var s = Fixture(); War(s); var infantry = Unit(s, 1, 20, 12); Unit(s, 1, 20, 12, UnitKind.Catapult); var plan = MilitaryStrategy.Plan(s, 1); Assert(plan.Assignments.Single(a => a.UnitId == infantry.Id).Task == ArmyTask.Garrison, "City lost its melee garrison"); });
        test("A threatened city receives defenders before an offensive army is released", () =>
        { var s = Fixture(); War(s); Unit(s, 0, 22, 12); for (int i = 0; i < 3; i++) Unit(s, 1, 20, 12); Assert(MilitaryStrategy.Plan(s, 1).Assignments.Count(a => a.Task == ArmyTask.Defend) >= 2, "Threat ignored"); });
        test("Future-era defense requirements scale with contemporary unit strength", () =>
        { var s = Fixture(); War(s); Unit(s, 0, 22, 12).ModelId = "stellar_guard"; for (int i = 0; i < 4; i++) Unit(s, 1, 20, 12).ModelId = "stellar_guard"; Assert(MilitaryStrategy.Plan(s, 1).Assignments.Count(a => a.Task == ArmyTask.Defend) == 2, "Future defense used ancient fixed strength thresholds"); });
        test("An isolated weak force assembles instead of declaring an unsupported assault", () =>
        { var s = Fixture(); War(s); Unit(s, 1, 20, 12); Unit(s, 1, 20, 13); var p = MilitaryStrategy.Plan(s, 1); Assert(!p.Ready && p.Assignments.Any(a => a.Task == ArmyTask.Assemble), "Tiny army released"); });
        test("A combined healthy field force can advance while preserving a garrison", () =>
        { var s = Fixture(); War(s); for (int i = 0; i < 3; i++) Unit(s, 1, 20, 12); Unit(s, 1, 20, 13, UnitKind.Catapult); var p = MilitaryStrategy.Plan(s, 1); Assert(p.Ready && p.ObjectiveCityId == s.Cities[0].Id && p.Assignments.Any(a => a.Task == ArmyTask.Garrison) && p.Assignments.Any(a => a.Task == ArmyTask.Advance), "Combined army not released"); });
        test("An all-ranged army does not plan to capture a city by itself", () =>
        { var s = Fixture(); War(s); for (int i = 0; i < 5; i++) Unit(s, 1, 20, 12, UnitKind.Catapult); Assert(!MilitaryStrategy.Plan(s, 1).Ready, "Capture role ignored"); });
        test("Strategic objectives discard cities whose approaches cannot be reached", () =>
        { var s = Fixture(); War(s); War(s, 2); foreach (var t in World.Neighbors(s, s.Cities[0].Tile)) t.Terrain = Terrain.Mountain; Unit(s, 1, 20, 12); Assert(MilitaryStrategy.Plan(s, 1).ObjectiveCityId == s.Cities[2].Id, "Inaccessible target chosen"); });
        test("Player assessment never names an unexplored hostile city objective", () =>
        { var s = Fixture(); War(s); foreach (var c in s.Cities.Where(c => c.Owner != 0)) s.Tiles[c.Tile].Explored = false; Assert(MilitaryStrategy.Plan(s, 0).ObjectiveCityId == null, "Hidden city exposed"); });
        test("Recruitment fills a missing capture role instead of always buying strongest cavalry", () =>
        { var s = Fixture(); Tech(s); War(s); var c = s.Cities[1]; Unit(s, 1, 20, 12, UnitKind.Catapult); Assert(MilitaryStrategy.Recruitment(s, c)?.Unit == UnitKind.Spearmen, "Infantry deficit ignored"); });
        test("Wartime recruitment includes siege support once infantry is available", () =>
        { var s = Fixture(); Tech(s); War(s); Unit(s, 1, 20, 12); Unit(s, 1, 20, 13); Assert(MilitaryStrategy.Recruitment(s, s.Cities[1])?.Unit == UnitKind.Catapult, "Siege role missing"); });
        test("Recruitment accounts for units already being produced by another city", () =>
        { var s = Fixture(); Tech(s); War(s); var second = Engine.AddCity(s, 1, 7 * s.Width + 20, "Posterunek"); second.Project = "siege_train"; for (int i = 0; i < 3; i++) Unit(s, 1, 20, 12); var next = MilitaryStrategy.Recruitment(s, s.Cities[1]); Assert(next != null && next.Unit != UnitKind.Catapult, "Duplicate siege pipeline ignored"); });
        test("Recruitment stops an unaffordable peaceful arms buildup", () =>
        { var s = Fixture(); Tech(s); s.Factions[1].Gold = 0; s.Factions[1].Tax = 0; for (int i = 0; i < 20; i++) Unit(s, 1, 20, 12); Assert(MilitaryStrategy.Recruitment(s, s.Cities[1]) == null, "Insolvent recruitment continued"); });
        test("Recruitment chooses a legal contemporary model for the missing role", () =>
        { var s = Fixture(); Tech(s); var project = MilitaryStrategy.Recruitment(s, s.Cities[1]); Assert(project?.Id == "legion" && ProductionPlanning.ProjectError(s, s.Cities[1], project.Id) == null, "Outdated or illegal infantry selected"); });
        test("AI preserves invested production on a legal project without an emergency", () =>
        { var s = Fixture(); s.Factions[1].Technologies.Add("stone_tools"); var c = s.Cities[1]; c.Project = "flint_workshop"; c.Production = 8; Assert(Engine.EndTurn(s).Success && c.Project == "flint_workshop" && c.Production == 8, "Daily replan erased or transferred work"); });
        test("A completed civilian project permits real AI recruitment without locking the next automatic project", () =>
        { var s = Fixture(); s.Factions[1].Technologies.UnionWith(["stone_tools","hunting_bands"]); var c = s.Cities[1]; c.ProductionPaused = false; c.Project = "flint_workshop"; c.Production = Content.Project(c.Project).Cost - .1; Assert(Engine.EndTurn(s).Success && c.Buildings.Contains("flint_workshop") && Content.Project(c.Project).Unit == UnitKind.Spearmen, "Automatic next project prevented recruitment"); });
        test("A free production boundary permits expansion without abandoning an invested project", () =>
        { var s = Fixture(); s.Turn = 20; s.Factions[1].Technologies.Add("stone_tools"); var c = s.Cities[1]; c.Population = 8; c.ProductionPaused = false; c.Project = "flint_workshop"; c.Production = Content.Project(c.Project).Cost - .1; Assert(Engine.EndTurn(s).Success && c.Project == "settler", "AI stopped recruiting settlers"); });
        test("Emergency military replanning pays the existing twenty percent work loss", () =>
        { var s = Fixture(); War(s); s.Factions[1].Technologies.UnionWith(["stone_tools","hunting_bands"]); var c = s.Cities[1]; c.Project = "flint_workshop"; c.Production = 8; Unit(s, 0, 22, 12); Assert(Engine.EndTurn(s).Success && Content.Project(c.Project).Unit == UnitKind.Spearmen && Math.Abs(c.Production - 6.4) < 1e-8, "Emergency changed work for free"); });
        test("Tactical ranged fire prioritizes a kill rather than list insertion order", () =>
        { var s = Fixture(); War(s); var strong = Unit(s, 0, 22, 12); var weak = Unit(s, 0, 21, 12); weak.Health = 1; Unit(s, 1, 20, 12, UnitKind.Archer); Engine.RunMilitaryAi(s, 1); Assert(!s.Units.Contains(weak) && strong.Health == 100, "Weak target not finished"); });
        test("Tactical melee refuses a preview that would kill its attacker", () =>
        { var s = Fixture(); War(s); var enemy = Unit(s, 0, 21, 12); enemy.ModelId = "stellar_guard"; var own = Unit(s, 1, 20, 12); own.Health = 46; Engine.RunMilitaryAi(s, 1); Assert(s.Units.Contains(own) && own.Health == 46 && enemy.Health == 100, "Suicidal attack executed"); });
        test("Ranged fire followed by infantry captures a weakened city through real combat", () =>
        { var s = Fixture(); War(s); s.Cities[0].Tile = 12 * s.Width + 22; s.Tiles[s.Cities[0].Tile].Owner = 0; s.Cities[0].Health = 10; Unit(s, 1, 20, 12); Unit(s, 1, 21, 12); Unit(s, 1, 21, 13, UnitKind.Catapult); s.InvalidateLogistics(); Engine.RunMilitaryAi(s, 1); Assert(s.Cities[0].Owner == 1, "Infantry did not finish the bombardment"); });
        test("An injured supplied army uses the existing recovery order without free healing", () =>
        { var s = Fixture(); var own = Unit(s, 1, 20, 12); own.Health = 30; Engine.RunMilitaryAi(s, 1); Assert(own.Order == MilitaryOrder.Recover && own.Actions == 0 && own.Health == 30, "Recovery bypassed turn rules"); });
        test("A damaged unsupplied army spends ordinary movement retreating toward supplies", () =>
        { var s = Fixture(); War(s); var own = Unit(s, 1, 9, 12); own.Health = 50; int before = World.Distance(s.Tiles[own.Tile], s.Tiles[s.Cities[1].Tile]); Engine.RunMilitaryAi(s, 1); Assert(World.Distance(s.Tiles[own.Tile], s.Tiles[s.Cities[1].Tile]) < before && own.Actions < own.MaxActions && own.Actions >= 0 && own.Health == 50, "Retreat teleported, healed or stalled"); });
        test("The AI executor cannot issue orders to the player's army", () =>
        { var s = Fixture(); Unit(s, 0, 12, 12); string before = SaveGame.Serialize(s); Engine.RunMilitaryAi(s, 0); Assert(before == SaveGame.Serialize(s), "Player orders overwritten"); });
        test("Existing v20 saves reproduce the same plans and future military turns", () =>
        { var s = Fixture(); War(s); for (int i = 0; i < 3; i++) Unit(s, 1, 20, 12); Unit(s, 1, 20, 13, UnitKind.Catapult); var copy = SaveGame.Deserialize(SaveGame.Serialize(s)); Assert(JsonSerializer.Serialize(MilitaryStrategy.Plan(s, 1)) == JsonSerializer.Serialize(MilitaryStrategy.Plan(copy, 1)), "Reload changed strategy"); for (int i = 0; i < 3 && s.Ending == null; i++) { s.PendingEvent = copy.PendingEvent = null; Assert(Engine.EndTurn(s).Success && Engine.EndTurn(copy).Success, "Continuation failed"); Assert(SaveGame.Serialize(s) == SaveGame.Serialize(copy), "Future army or RNG diverged"); } });
        test("A prepared combined army can complete an actual multi-turn offensive while retaining city security", () =>
        { var s = Fixture(); War(s); for (int i = 0; i < 3; i++) Unit(s, 1, 20, 12); Unit(s, 1, 20, 13, UnitKind.Catapult); int turns = 0; while (s.Cities[0].Owner == 0 && s.Ending == null && turns++ < 18) { s.PendingEvent = null; Assert(Engine.EndTurn(s).Success, "Offensive turn failed"); } Assert(s.Cities[0].Owner == 1 && s.Units.Any(u => u.Owner == 1 && World.Distance(s.Tiles[u.Tile], s.Tiles[s.Cities[1].Tile]) <= 1), "Offensive stalled or emptied its home city"); SaveGame.Deserialize(SaveGame.Serialize(s)); });
    }
}
