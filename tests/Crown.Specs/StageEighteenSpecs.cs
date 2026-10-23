using Crown.Core;

internal static class StageEighteenSpecs
{
    private static void Assert(bool value, string message) { if (!value) throw new Exception(message); }
    private static bool Near(double a, double b) => Math.Abs(a - b) < 1e-8;
    public static GameState Fresh()
    {
        var s = StageSeventeenSpecs.Fresh(); s.Player.Technologies.UnionWith(Content.Technologies.Select(t => t.Id)); return s;
    }
    public static City Neighbor(GameState s, City city, string name = "Zakole")
    {
        var tile = World.Neighbors(s, city.Tile).First(t => t.Passable && !s.Cities.Any(c => c.Tile == t.Id));
        tile.Owner = city.Owner; var c = Engine.AddCity(s, city.Owner, tile.Id, name); c.ProductionPaused = true; c.Project = "treasury"; return c;
    }
    private static void Step(GameState s)
    {
        if (s.PendingEvent != null) { var e = ProvincialEvents.Current(s); Assert(Narrative.Resolve(s, Enumerable.Range(0, e.Choices.Length).First(i => s.Player.Gold + e.Choices[i].Gold >= 0)).Success, "Council failed."); }
        Assert(Engine.EndTurn(s).Success, "Turn failed.");
    }
    private static void Valid(GameState s) => SaveGame.Deserialize(SaveGame.Serialize(s));
    private static void Reject(GameState s)
    { bool rejected = false; try { Valid(s); } catch (InvalidDataException) { rejected = true; } Assert(rejected, "Damaged power state accepted."); }
    private static void Atomic(GameState s, Func<Result> action)
    { string before = SaveGame.Serialize(s); Assert(!action().Success && SaveGame.Serialize(s) == before, "Rejected action changed the state."); }
    public static void Run(Action<string, Action> test)
    {
        test("Equal power priorities retain proportional sharing and conserve generation", () =>
        {
            var s = Fresh(); var a = s.Cities[0]; var b = Neighbor(s, a); a.Buildings.UnionWith(["coal_station", "steel_mill"]); b.Buildings.Add("data_centre");
            var grid = PowerGrid.Plan(s, b); Assert(grid.Supply == 24 && grid.Demand == 37 && Near(grid.Cities.Sum(c => c.Allocated), 24) && Near(Epochs.Energy(s, a).Coverage, Epochs.Energy(s, b).Coverage), "Unfair sharing or invented energy."); Valid(s);
        });
        test("A critical city receives power before ordinary and reduced cities", () =>
        {
            var s = Fresh(); var a = s.Cities[0]; var b = Neighbor(s, a); a.Buildings.UnionWith(["electric_grid", "data_centre"]); b.Buildings.Add("data_centre");
            Assert(Engine.SetPowerPriority(s, a.Id, PowerPriority.Critical).Success && Engine.SetPowerPriority(s, b.Id, PowerPriority.Reduced).Success, "Priority rejected.");
            Assert(Near(Epochs.Energy(s, a).Coverage, 1) && Near(Epochs.Energy(s, b).Coverage, .6), "Priority did not allocate 25 and 15.");
        });
        test("Priority decisions change real science in both cities without generating power", () =>
        {
            var s = Fresh(); var a = s.Cities[0]; var b = Neighbor(s, a); a.Buildings.UnionWith(["coal_station", "data_centre"]); b.Buildings.Add("data_centre");
            double beforeA = Engine.CityYields(s, a).Science, beforeB = Engine.CityYields(s, b).Science;
            Engine.SetPowerPriority(s, a.Id, PowerPriority.Critical);
            Assert(Engine.CityYields(s, a).Science > beforeA && Engine.CityYields(s, b).Science < beforeB && Near(PowerGrid.Plan(s, a).Cities.Sum(c => c.Allocated), 24), "Political choice has no economic consequence.");
        });
        test("A shortage within the highest priority is proportional rather than city-order dependent", () =>
        {
            var s = Fresh(); var a = s.Cities[0]; var b = Neighbor(s, a); a.Buildings.UnionWith(["coal_station", "steel_mill"]); b.Buildings.Add("data_centre");
            Engine.SetPowerPriority(s, a.Id, PowerPriority.Critical); Engine.SetPowerPriority(s, b.Id, PowerPriority.Critical);
            double before = Epochs.Energy(s, b).Coverage; s.Cities.Reverse();
            Assert(Near(before, 24d / 37) && Near(before, Epochs.Energy(s, a).Coverage), "City order changed allocation.");
        });
        test("Idle networks preserve unused capacity and zero-demand cities require no power", () =>
        {
            var s = Fresh(); var c = s.Cities[0]; c.Buildings.Add("atomic_reactor"); var grid = PowerGrid.Plan(s, c);
            Assert(grid.Supply == 85 && grid.Unused == 85 && grid.Demand == 0 && Epochs.Energy(s, c).Coverage == 1, "Idle grid consumed phantom power.");
        });
        test("Power priority cannot bypass a disconnected road network", () =>
        {
            var s = Fresh(); var a = s.Cities[0]; var b = s.Cities[1]; b.Owner = 0; s.Tiles[b.Tile].Owner = 0; a.Buildings.Add("atomic_reactor"); b.Buildings.Add("data_centre"); s.InvalidateLogistics();
            Engine.SetPowerPriority(s, b.Id, PowerPriority.Critical); Assert(Epochs.Energy(s, b).Coverage == 0, "Power teleported across the map.");
        });
        test("An occupied generator cannot connect two otherwise separated consumers", () =>
        {
            var s = Fresh(); var a = s.Cities[0]; var neighbors = World.Neighbors(s, a.Tile).Where(t => t.Passable).ToArray();
            var pair = (from x in neighbors from y in neighbors where World.Distance(x, y) == 2 select (x, y)).First();
            var b = Engine.AddCity(s, 0, pair.x.Id, "Zachód"); var c = Engine.AddCity(s, 0, pair.y.Id, "Wschód");
            a.Buildings.Add("atomic_reactor"); b.Buildings.UnionWith(["coal_station", "data_centre"]); c.Buildings.Add("data_centre");
            s.Relation(0, 1).War = true; WarCouncil.EnsureActive(s); Engine.AddUnit(s, 1, a.Tile, UnitKind.Spearmen); s.InvalidateLogistics();
            Assert(PowerGrid.Plan(s, a).Cities.Single().Isolated && Epochs.Energy(s, a).Supply == 0 && Epochs.Energy(s, c).Coverage == 0 && Epochs.Energy(s, b).Supply == 24, "Occupied capital relayed power.");
        });
        test("Power controls reject foreign cities, invalid priorities and repeat changes atomically", () =>
        {
            var s = Fresh(); var c = s.Cities[0]; Atomic(s, () => Engine.SetPowerPriority(s, s.Cities[1].Id, PowerPriority.Critical)); Atomic(s, () => Engine.SetPowerPriority(s, c.Id, (PowerPriority)99));
            Engine.SetPowerPriority(s, c.Id, PowerPriority.Critical); Atomic(s, () => Engine.SetPowerPriority(s, c.Id, PowerPriority.Reduced)); Step(s);
            Assert(Engine.SetPowerPriority(s, c.Id, PowerPriority.Reduced).Success, "Priority did not unlock next turn.");
        });
        test("Suspending a factory removes its output, energy demand and emissions", () =>
        {
            var s = Fresh(); var c = s.Cities[0]; c.Buildings.UnionWith(["coal_station", "steel_mill"]); double output = Engine.CityYields(s, c).Production; double emissions = Epochs.Emissions(s, 0);
            Assert(Engine.SetBuildingOperation(s, c.Id, "steel_mill", true).Success && Engine.CityYields(s, c).Production < output && Epochs.Energy(s, c).Demand == 0 && Epochs.Emissions(s, 0) < emissions && c.Buildings.Contains("steel_mill"), "Suspension only changed a label."); Valid(s);
        });
        test("A suspended generator loses supply and retains one quarter of its actual upkeep", () =>
        {
            var s = Fresh(); var c = s.Cities[0]; c.Buildings.UnionWith(["atomic_reactor", "data_centre"]); double oldUpkeep = Epochs.Upkeep(s, 0); Engine.SetBuildingOperation(s, c.Id, "atomic_reactor", true);
            Assert(Epochs.Energy(s, c).Supply == 0 && Epochs.Energy(s, c).Coverage == 0 && Near(oldUpkeep - Epochs.Upkeep(s, 0), 7.5), "Shutdown supplied free power or lost standby cost.");
        });
        test("Shutdown frees scarce power for another city's data centre", () =>
        {
            var s = Fresh(); var a = s.Cities[0]; var b = Neighbor(s, a); a.Buildings.UnionWith(["electric_grid", "steel_mill", "motor_works"]); b.Buildings.Add("data_centre");
            double science = Engine.CityYields(s, b).Science; Engine.SetBuildingOperation(s, a.Id, "motor_works", true);
            Assert(Epochs.Energy(s, b).Coverage == 1 && Engine.CityYields(s, b).Science > science && a.SuspendedBuildings.Contains("motor_works"), "Shutdown did not reallocate power.");
        });
        test("Restart costs gold once, requires the next turn and preserves production plans", () =>
        {
            var s = Fresh(); var c = s.Cities[0]; c.Buildings.Add("data_centre"); c.Production = 77; c.ProductionQueue.Add(new() { Project = "scout", Repeat = true });
            Engine.SetBuildingOperation(s, c.Id, "data_centre", true); Atomic(s, () => Engine.SetBuildingOperation(s, c.Id, "data_centre", false)); s.Turn++;
            double gold = s.Player.Gold; Assert(Engine.SetBuildingOperation(s, c.Id, "data_centre", false).Success && Near(gold - s.Player.Gold, 60) && !c.SuspendedBuildings.Contains("data_centre") && c.Production == 77 && c.ProductionQueue.Single().Repeat, "Restart duplicated payment or destroyed plans.");
            Atomic(s, () => Engine.SetBuildingOperation(s, c.Id, "data_centre", false)); Valid(s);
        });
        test("Insufficient restart funds and unsupported buildings are rejected without mutation", () =>
        {
            var s = Fresh(); var c = s.Cities[0]; c.Buildings.UnionWith(["library", "data_centre"]); Engine.SetBuildingOperation(s, c.Id, "data_centre", true); s.Turn++; s.Player.Gold = 59;
            Atomic(s, () => Engine.SetBuildingOperation(s, c.Id, "data_centre", false)); Atomic(s, () => Engine.SetBuildingOperation(s, c.Id, "library", true)); Atomic(s, () => Engine.SetBuildingOperation(s, c.Id, "missing", true));
        });
        test("Suspended service buildings keep physical housing but stop sanitation and supply", () =>
        {
            var s = Fresh(); var c = s.Cities[0]; c.Buildings.UnionWith(["modern_hospital", "rail_terminal"]); int housing = Epochs.Housing(s, c); double range = Logistics.Range(s, 0);
            Engine.SetBuildingOperation(s, c.Id, "modern_hospital", true); Engine.SetBuildingOperation(s, c.Id, "rail_terminal", true);
            Assert(Epochs.Housing(s, c) == housing && Epochs.Sanitation(s, c) == 0 && Logistics.Range(s, 0) == range - 3, "Services remained active or homes vanished.");
        });
        test("Suspending the required space facility blocks work without removing accumulated production", () =>
        {
            var s = Fresh(); var c = s.Cities[0]; c.Buildings.UnionWith(["orbital_shipyard", "fusion_plant"]); c.Project = "orbital_station"; c.ProductionPaused = false; c.Production = 8000;
            Engine.SetBuildingOperation(s, c.Id, "orbital_shipyard", true); Step(s);
            Assert(!s.Player.SpaceMilestones.Contains("orbital_station") && c.Production == 8000 && ProductionPlanning.CurrentBlock(s, c) != null, "Inactive shipyard completed a program.");
            Engine.SetBuildingOperation(s, c.Id, "orbital_shipyard", false); Step(s); Assert(s.Player.SpaceMilestones.Contains("orbital_station"), "Program did not resume after paid restart.");
        });
        test("Prioritising the spaceport enables its real program at the expense of another city", () =>
        {
            var s = Fresh(); var a = s.Cities[0]; var b = Neighbor(s, a); a.Buildings.UnionWith(["atomic_reactor", "orbital_shipyard"]); b.Buildings.Add("data_centre");
            Assert(Epochs.ProjectError(s, a, Content.Project("orbital_station")) != null, "Shared shortage did not block program.");
            Engine.SetPowerPriority(s, a.Id, PowerPriority.Critical); Assert(Epochs.Energy(s, a).Coverage == 1 && Epochs.Energy(s, b).Coverage < 1 && Engine.SetProject(s, a.Id, "orbital_station").Success, "Priority did not enable spaceport.");
            a.ProductionPaused = false; a.Production = 7000; Step(s); Assert(s.Player.SpaceMilestones.Contains("orbital_station"), "Powered program did not complete.");
        });
        test("A plant completed this phase powers other cities only from the next economic phase", () =>
        {
            var s = Fresh(); var a = s.Cities[0]; var b = Neighbor(s, a); a.Project = "fusion_plant"; a.Production = 2300; a.ProductionPaused = false;
            b.Buildings.Add("orbital_shipyard"); b.Project = "orbital_station"; b.Production = 7000; b.ProductionPaused = false;
            Step(s); Assert(a.Buildings.Contains("fusion_plant") && !s.Player.SpaceMilestones.Contains("orbital_station") && b.Production == 7000 && Epochs.Energy(s, b).Coverage == 1, "Construction order granted same-phase energy.");
            Step(s); Assert(s.Player.SpaceMilestones.Contains("orbital_station"), "New plant remained frozen on the next turn.");
        });
        test("Climate charges the same operating phase rather than plants finished or retired by AI later", () =>
        {
            var s = Fresh(); var c = s.Cities[0]; c.Project = "coal_station"; c.Production = 520; c.ProductionPaused = false; Step(s);
            Assert(s.ClimatePressure == 0, "Unfinished plant emitted before operating."); Step(s); Assert(Near(s.ClimatePressure, .03), "Operating plant did not emit six units.");
        });
        test("Four renewable sources have usable technology unlocks and geographical descriptions", () =>
        {
            string[] ids = ["hydro_station", "wind_farm", "solar_farm", "geothermal_station"];
            Assert(EpochCatalog.Buildings.Length == 58 && ids.All(id => Content.Projects.Any(p => p.Id == id) && PowerGrid.SiteDescription(id).Length > 0 && EpochCatalog.Buildings.Single(b => b.Id == id).Emissions == 0), "Renewable catalog is incomplete.");
        });
        test("Hydropower requires a river belonging to the city owner during actual production", () =>
        {
            var s = Fresh(); var c = s.Cities[0]; foreach (var t in World.Neighbors(s, c.Tile).Append(s.Tiles[c.Tile])) t.River = false;
            Atomic(s, () => Engine.SetProject(s, c.Id, "hydro_station")); var river = World.Neighbors(s, c.Tile).First(); river.River = true; river.Owner = 1; s.InvalidateLogistics(); Atomic(s, () => Engine.SetProject(s, c.Id, "hydro_station"));
            river.Owner = 0; s.InvalidateLogistics(); Assert(Engine.SetProject(s, c.Id, "hydro_station").Success, "Own river did not unlock hydropower.");
            c.ProductionPaused = false; c.Production = 900; Step(s); Assert(c.Buildings.Contains("hydro_station") && Epochs.Energy(s, c).Supply == 45, "Hydropower did not become real supply.");
        });
        test("A queued renewable project waits for its geographic requirements without losing work", () =>
        {
            var s = Fresh(); var c = s.Cities[0]; foreach (var t in World.Neighbors(s, c.Tile).Append(s.Tiles[c.Tile])) if (t.Terrain is Terrain.Hills or Terrain.Mountain) t.Terrain = Terrain.Grassland;
            c.Project = "geothermal_station"; c.ProductionPaused = false; c.Production = 400; Step(s); Assert(c.Production == 400 && !c.Buildings.Contains("geothermal_station"), "Invalid site consumed production.");
            foreach (var t in World.Neighbors(s, c.Tile).Where(t => t.Owner == 0).Take(2)) t.Terrain = Terrain.Hills;
            Assert(ProductionPlanning.CurrentBlock(s, c) == null, "Valid geothermal site still blocked.");
        });
        test("Solar generation follows winter and desert modifiers without consuming RNG", () =>
        {
            var s = Fresh(); var c = s.Cities[0]; c.Buildings.Add("solar_farm"); s.Tiles[c.Tile].Terrain = Terrain.Desert; uint rng = s.RandomState; s.Turn = 2;
            Assert(Epochs.Energy(s, c).Supply == 52, "Desert solar capacity wrong."); s.Turn = 4; Assert(Epochs.Energy(s, c).Supply == 34 && s.RandomState == rng, "Winter solar modifier or determinism broken.");
        });
        test("Wind uses winter, summer and local hills while repeated forecasts remain pure", () =>
        {
            var s = Fresh(); var c = s.Cities[0]; c.Buildings.Add("wind_farm"); s.Tiles[c.Tile].Terrain = Terrain.Hills; s.Turn = 2;
            Assert(Epochs.Energy(s, c).Supply == 33, "Summer wind wrong."); s.Turn = 4; string before = SaveGame.Serialize(s);
            for (int i = 0; i < 10; i++) Assert(Epochs.Energy(s, c).Supply == 50, "Winter wind wrong."); Assert(SaveGame.Serialize(s) == before, "Power preview mutated state.");
        });
        test("AI retires redundant emitting capacity without causing shortages or restarting it repeatedly", () =>
        {
            var s = Fresh(); var c = s.Cities[1]; c.Buildings.UnionWith(["coal_station", "fusion_plant", "steel_mill"]); s.Factions[1].Technologies.UnionWith(Content.Technologies.Select(t => t.Id));
            Step(s); Assert(c.SuspendedBuildings.Contains("coal_station") && Epochs.Energy(s, c).Coverage == 1, "AI kept redundant coal or cut essential capacity.");
            int changed = c.LastOperationTurns["coal_station"]; Step(s); Assert(c.SuspendedBuildings.Contains("coal_station") && c.LastOperationTurns["coal_station"] == changed, "AI repeatedly paid to restart and retire coal."); Valid(s);
        });
        test("AI marks its active space program as critical and can restart an inherited source", () =>
        {
            var s = Fresh(); var c = s.Cities[1]; var f = s.Factions[1]; f.Technologies.UnionWith(Content.Technologies.Select(t => t.Id)); c.Project = "orbital_station"; c.Buildings.UnionWith(["fusion_plant", "orbital_shipyard"]); c.SuspendedBuildings.Add("fusion_plant");
            for(int i=0;i<4;i++)Engine.AddUnit(s,1,c.Tile,UnitKind.Spearmen);
            Step(s); Assert(c.PowerPriority == PowerPriority.Critical && !c.SuspendedBuildings.Contains("fusion_plant"), "AI ignored power policy or inherited shutdown.");
        });
        test("Sovereignty resets allocation orders but retains physically suspended facilities", () =>
        {
            var s = Fresh(); var c = s.Cities[0]; c.Buildings.Add("atomic_reactor"); Engine.SetPowerPriority(s, c.Id, PowerPriority.Critical); Engine.SetBuildingOperation(s, c.Id, "atomic_reactor", true);
            ProvincialPolitics.Transfer(s, c, 1, true); Assert(c.PowerPriority == PowerPriority.Normal && c.LastPowerPriorityTurn == -1 && c.LastOperationTurns.Count == 0 && c.SuspendedBuildings.Contains("atomic_reactor"), "Conquest retained old allocation orders or silently restarted plant.");
            Assert(Engine.SetBuildingOperation(s, c.Id, "atomic_reactor", false, 1).Success, "New owner cannot pay for restart."); Valid(s);
        });
        test("V17 migration keeps the extended world and resets only newly introduced power directives", () =>
        {
            var s = Fresh(); var c = s.Cities[0]; c.Buildings.Add("atomic_reactor"); s.Player.EpochChoices[13] = 2; s.ClimatePressure = 33; s.Version = 17; c.SuspendedBuildings.Add("atomic_reactor"); c.PowerPriority = PowerPriority.Critical;
            var loaded = SaveGame.Deserialize(SaveGame.Serialize(s)); Assert(loaded.Version == 18 && loaded.EpochCampaign && loaded.ClimatePressure == 33 && loaded.Player.EpochChoices[13] == 2 && loaded.Cities[0].PowerPriority == PowerPriority.Normal && loaded.Cities[0].SuspendedBuildings.Count == 0, "Migration erased epoch history or invented directives.");
        });
        test("Power save reload retains operating costs, priorities and same-turn locks exactly", () =>
        {
            var s = Fresh(); var c = s.Cities[0]; c.Buildings.Add("data_centre"); Engine.SetPowerPriority(s, c.Id, PowerPriority.Critical); Engine.SetBuildingOperation(s, c.Id, "data_centre", true);
            string json = SaveGame.Serialize(s); var loaded = SaveGame.Deserialize(json); Assert(SaveGame.Serialize(loaded) == json, "Power state did not round-trip exactly."); Atomic(loaded, () => Engine.SetBuildingOperation(loaded, c.Id, "data_centre", false));
            for (int i = 0; i < 5; i++) { Step(s); Step(loaded); Assert(SaveGame.Serialize(s) == SaveGame.Serialize(loaded), "Save replay diverged."); }
        });
        test("V17 overwrite retains a faithful backup of the player's previous campaign", () =>
        {
            var s = Fresh(); s.Version = 17; string old = SaveGame.Serialize(s); string path = Path.Combine(Environment.CurrentDirectory, "artifacts", "migration-v17-test.crown");
            try { File.WriteAllText(path, old); SaveGame.Write(SaveGame.Deserialize(old), path); Assert(File.ReadAllText(path + ".v17.bak") == old && SaveGame.Read(path).Version == 18, "Old campaign backup missing."); }
            finally { File.Delete(path); File.Delete(path + ".v17.bak"); }
        });
        test("Validation rejects unsupported shutdowns, future dates, undefined priorities and null registers", () =>
        {
            var s = Fresh(); s.Cities[0].SuspendedBuildings.Add("library"); Reject(s);
            s = Fresh(); s.Cities[0].PowerPriority = (PowerPriority)99; Reject(s);
            s = Fresh(); s.Cities[0].LastPowerPriorityTurn = s.Turn + 1; Reject(s);
            s = Fresh(); s.Cities[0].Buildings.Add("data_centre"); s.Cities[0].LastOperationTurns["data_centre"] = s.Turn + 1; Reject(s);
            s = Fresh(); s.Cities[0].LastOperationTurns = null!; Reject(s);
            s = Fresh(); s.Cities[0].SuspendedBuildings = null!; Reject(s);
        });
        test("Legacy games and ended campaigns reject industrial commands atomically", () =>
        {
            var s = World.CreateLegacy(); Narrative.Resolve(s, 0); var c = s.Cities[0]; c.Buildings.Add("data_centre");
            Atomic(s, () => Engine.SetPowerPriority(s, c.Id, PowerPriority.Critical)); Atomic(s, () => Engine.SetBuildingOperation(s, c.Id, "data_centre", true));
            s = Fresh(); c = s.Cities[0]; c.Buildings.Add("data_centre"); s.Ending = "Koniec"; Atomic(s, () => Engine.SetBuildingOperation(s, c.Id, "data_centre", true));
            s = World.CreateLegacy(); s.Cities[0].PowerPriority = PowerPriority.Critical; Reject(s);
        });
    }
}
