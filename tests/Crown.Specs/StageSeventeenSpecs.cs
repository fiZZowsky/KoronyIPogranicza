using Crown.Core;

internal static class StageSeventeenSpecs
{
    private static void Assert(bool value, string message) { if (!value) throw new Exception(message); }
    public static GameState Fresh()
    {
        var s = World.Create(); Narrative.Resolve(s, 0); s.Units.Clear();
        foreach (var f in s.Factions) { f.Gold = 100000; f.Research = ""; }
        foreach (var r in s.Relations) r.Trust = 100;
        foreach (var c in s.Cities) { c.ProductionPaused = true; c.Food = 20; }
        return s;
    }
    public static GameState Council(int era)
    { var s = Fresh(); foreach (var e in EpochCatalog.All.Take(era + 1)) s.Player.Technologies.UnionWith(e.Technologies); s.PendingEvent = EpochCouncils.Id(era); return s; }
    private static void Resolve(GameState s)
    {
        if (s.PendingEvent == null) return; var e = ProvincialEvents.Current(s);
        int choice = Enumerable.Range(0, e.Choices.Length).First(i => s.Player.Gold + e.Choices[i].Gold >= 0 && (e.Id != CustodyCouncil.Event.Id || CustodyCouncil.ChoiceError(s, i) == null) && (e.Id != SpyCouncil.Event.Id || SpyCouncil.ChoiceError(s, i) == null));
        Assert(Narrative.Resolve(s, choice).Success, "Could not resolve " + e.Id);
    }
    private static void Step(GameState s) { Resolve(s); Assert(Engine.EndTurn(s).Success, "Turn failed."); }
    private static void Valid(GameState s) => SaveGame.Deserialize(SaveGame.Serialize(s));
    private static void Reject(GameState s)
    { bool failed = false; try { Valid(s); } catch (InvalidDataException) { failed = true; } Assert(failed, "Invalid epoch state accepted."); }
    private static void Atomic(GameState s, Func<Result> action)
    { string before = SaveGame.Serialize(s); Assert(!action().Success && before == SaveGame.Serialize(s), "Rejected command mutated the state."); }
    public static void Run(Action<string, Action> test)
    {
        test("New campaigns begin in the Paleolithic with 1600 turns and historical continuity", () =>
        {
            var s = World.Create(); Assert(s.EpochCampaign && s.MaxTurns == 1600 && s.Year == -40000 && s.Player.Era == 0 && s.Player.Research == "stone_tools" && s.Units.All(u => u.ModelId != null), "New campaign not extended."); Valid(s);
        });
        test("Eighteen eras contain ninety unique technologies and all original discoveries", () =>
        {
            Assert(EpochCatalog.All.Length == 18 && Content.Technologies.Length == 90 && Content.Technologies.Select(t => t.Id).Distinct().Count() == 90 && Content.LegacyTechnologies.Count == 24 && EpochCatalog.All.All(e => e.Technologies.Length == 5), "Catalog count or identity error.");
            Assert(Content.Technologies.All(t => t.Requires.All(id => Content.Technologies.Any(x => x.Id == id))), "Unknown technology prerequisite.");
        });
        test("The full technology graph is reachable in order without cyclic prerequisites", () =>
        {
            var s = Fresh(); var reached = new HashSet<int>();
            while (s.Player.Technologies.Count < 90)
            { var t = Epochs.ResearchOptions(s, s.Player).FirstOrDefault(); Assert(t != null, "Technology deadlock."); s.Player.Technologies.Add(t!.Id); reached.Add(s.Player.Era); }
            Assert(reached.Count == 18 && s.Player.Era == 17, "Skipped eras."); Valid(s);
        });
        test("A later era cannot be researched by accumulating unrelated technology counts", () =>
        {
            var s = Fresh(); s.Player.Technologies.UnionWith(EpochCatalog.All[0].Technologies[..4]); Atomic(s, () => Engine.SetResearch(s, "agriculture"));
            s.Player.Technologies.Add(EpochCatalog.All[0].Capstone); Assert(Engine.SetResearch(s, "agriculture").Success, "Closed era did not unlock its successor."); Atomic(s, () => Engine.SetResearch(s, "stellar_energy"));
        });
        test("Every new discovery has an observable city bonus or usable project unlock", () =>
        {
            var s = Fresh(); var c = s.Cities[0];
            foreach (var t in Content.Technologies.Where(t => !Content.LegacyTechnologies.Contains(t.Id)))
            {
                var before = Epochs.Bonus(s, c); s.Player.Technologies.Add(t.Id); var after = Epochs.Bonus(s, c);
                Assert(before != after, "Discovery has no effect: " + t.Id);
            }
        });
        test("All epoch buildings and 34 models have actual effects and valid prerequisites", () =>
        {
            Assert(EpochCatalog.Buildings.Length >= 54 && UnitModels.All.Length == 34, "Missing content.");
            foreach (var b in EpochCatalog.Buildings) Assert(Content.Projects.Any(p => p.Id == b.Id && p.Technology == b.Technology) && b.Cost > 0 && EpochCatalog.Describe(b).Length > 0 && EpochCatalog.EraOf(b.Technology) == b.Era, "Invalid building " + b.Id);
            foreach (var m in UnitModels.All) Assert(Content.Project(m.Id).Unit == m.Role && EpochCatalog.EraOf(m.Technology) == m.Era && m.Range >= 1 && m.Strength > 0, "Invalid model " + m.Id);
        });
        test("Energy shortage reduces the actual yield of powered infrastructure", () =>
        {
            var s = Fresh(); var c = s.Cities[0]; c.Buildings.Add("data_centre"); double noPower = Engine.CityYields(s, c).Science; Assert(Epochs.Energy(s, c).Coverage == 0, "Unpowered data centre supplied.");
            c.Buildings.Add("atomic_reactor"); Assert(Epochs.Energy(s, c).Coverage == 1 && Engine.CityYields(s, c).Science > noPower, "Energy did not affect science."); Valid(s);
        });
        test("Energy is shared by connected domestic cities and an interdiction can cut it", () =>
        {
            var s = Fresh(); var c = s.Cities[0]; var tile = World.Neighbors(s, c.Tile).First(t => t.Passable); var second = Engine.AddCity(s, 0, tile.Id, "Sieć");
            c.Buildings.Add("atomic_reactor"); second.Buildings.Add("data_centre"); s.InvalidateLogistics(); Assert(Epochs.Energy(s, second).Coverage == 1, "Adjacent domestic nodes do not share power.");
            s.Relation(0, 1).War = true; WarCouncil.EnsureActive(s); Engine.AddUnit(s, 1, c.Tile, UnitKind.Spearmen); s.InvalidateLogistics(); Assert(Epochs.Energy(s, second).Coverage == 0, "Enemy occupation kept power connected.");
        });
        test("Powered facilities retain full upkeep when energy is scarce", () =>
        {
            var s = Fresh(); var c = s.Cities[0]; double before = Engine.Economy(s, 0).AdministrationCost; c.Buildings.Add("data_centre");
            Assert(Engine.Economy(s, 0).AdministrationCost >= before + 6, "Unpowered facility did not cost upkeep.");
        });
        test("Emissions from foreign factories affect global climate and domestic crops", () =>
        {
            var s = Fresh(); s.Cities[1].Buildings.UnionWith(["coal_station", "steel_mill"]); var c = s.Cities[0]; double food = Engine.CityYields(s, c).Food;
            for (int i = 0; i < 100; i++) Epochs.Tick(s);
            Assert(s.ClimatePressure > 0 && Engine.CityYields(s, c).Food < food, "Foreign emissions did not affect the world."); Valid(s);
        });
        test("Climate restoration reverses pressure without going below zero", () =>
        {
            var s = Fresh(); s.ClimatePressure = 2; s.Cities[0].Buildings.UnionWith(["fusion_plant", "climate_restoration"]);
            for (int i = 0; i < 100; i++) Epochs.Tick(s); Assert(s.ClimatePressure == 0, "Restoration failed or produced negative pressure."); Valid(s);
        });
        test("Housing, sanitation, defense and supply bonuses reach their existing systems", () =>
        {
            var s = Fresh(); var c = s.Cities[0]; int h = Regions.Housing(s, c), health = Regions.Sanitation(s, c); double range = Logistics.Range(s, 0);
            c.Buildings.UnionWith(["biosphere_habitat", "border_post"]); s.InvalidateLogistics();
            Assert(Regions.Housing(s, c) == h + 70 && Regions.Sanitation(s, c) == health + 40 && Epochs.Defense(s, c) == 8 && Logistics.Range(s, 0) == range + 1, "Development bonus was decorative.");
        });
        test("Era choices have lasting different effects, survive reload and cannot be repeated", () =>
        {
            var a = Council(12); var b = SaveGame.Deserialize(SaveGame.Serialize(a)); Assert(Narrative.Resolve(a, 0).Success && Narrative.Resolve(b, 1).Success, "Era choice failed.");
            Assert(a.Player.EpochChoices[12] == 0 && b.Player.EpochChoices[12] == 1 && Engine.CityYields(a, a.Cities[0]).Production < Engine.CityYields(b, b.Cities[0]).Production, "Branches have same economy.");
            a.PendingEvent = EpochCouncils.Id(12); Atomic(a, () => Narrative.Resolve(a, 1)); a.PendingEvent = null; Valid(a); Valid(b);
        });
        test("Era councils reject future contexts and keep a free affordable branch", () =>
        {
            var s = Fresh(); s.PendingEvent = EpochCouncils.Id(17); Atomic(s, () => Narrative.Resolve(s, 2));
            foreach (int era in Enumerable.Range(0, 18)) { s = Council(era); s.Player.Gold = 0; int choice = Array.FindIndex(ProvincialEvents.Current(s).Choices, c => c.Gold >= 0); Assert(choice >= 0 && Narrative.Resolve(s, choice).Success, "Empty treasury blocked era " + era); Valid(s); }
        });
        test("Models are produced with their own combat strength, movement and name", () =>
        {
            var s = Fresh(); var c = s.Cities[0]; s.Player.Technologies.Add("mass_society"); c.ProductionPaused = false; c.Production = 2000; Assert(Engine.SetProject(s, c.Id, "tanks").Success, "Tank project unavailable."); Step(s);
            var u = s.Units.First(u => u.Owner == 0); Assert(u.ModelId == "tanks" && u.MaxActions == 4 && UnitModels.Name(u).Contains("pancerny") && Engine.Strength(s, u) >= 150, "Produced legacy cavalry instead of tank."); Valid(s);
        });
        test("Modernization transfers gold once and preserves health and experience", () =>
        {
            var s = Fresh(); var u = Engine.AddUnit(s, 0, s.Cities[0].Tile, UnitKind.Spearmen); u.Health = 55; u.Experience = 6; s.Player.Technologies.Add("robotics"); double gold = s.Player.Gold;
            int price = UnitModels.UpgradeCost(s,u, UnitModels.All.First(m => m.Id == "robot_guard")); Assert(Engine.UpgradeUnit(s, u.Id, "robot_guard").Success && u.ModelId == "robot_guard" && u.Health == 55 && u.Experience == 6 && u.Actions == 0 && s.Player.Gold == gold - price, "Wrong modernization effects.");
            Atomic(s, () => Engine.UpgradeUnit(s, u.Id, "robot_guard")); Valid(s);
        });
        test("Modernization rejects wrong role, insufficient funds, movement and foreign location", () =>
        {
            var s = Fresh(); var u = Engine.AddUnit(s, 0, s.Cities[0].Tile, UnitKind.Spearmen); s.Player.Technologies.UnionWith(["robotics", "combustion"]);
            Atomic(s, () => Engine.UpgradeUnit(s, u.Id, "tanks")); u.Actions = 1; Atomic(s, () => Engine.UpgradeUnit(s, u.Id, "robot_guard")); u.Actions = 2; s.Player.Gold = 0; Atomic(s, () => Engine.UpgradeUnit(s, u.Id, "robot_guard")); s.Player.Gold = 10000; u.Tile = s.Cities[1].Tile; Atomic(s, () => Engine.UpgradeUnit(s, u.Id, "robot_guard"));
        });
        test("Late artillery uses model range and cannot capture cities", () =>
        {
            var s = Fresh(); var city = s.Cities[1]; s.Relation(0, 1).War = true; WarCouncil.EnsureActive(s); s.Tiles[city.Tile].Explored = true;
            var tile = s.Tiles.First(t => t.Passable && World.Distance(t, s.Tiles[city.Tile]) == 4); var u = Engine.AddUnit(s, 0, tile.Id, UnitKind.Catapult, "deep_space_battery"); city.Health = 1;
            var preview = Engine.PreviewAttack(s, u.Id, city.Tile); Assert(preview.Valid && preview.Ranged && !preview.CanCapture && Engine.Move(s, u.Id, city.Tile).Success && city.Owner == 1, "Model range not used by actual combat."); Valid(s);
        });
        test("Programs require technology, the right facility and their predecessor", () =>
        {
            var s = Fresh(); var c = s.Cities[0]; Atomic(s, () => Engine.SetProject(s, c.Id, "orbital_station")); s.Player.Technologies.Add("orbital_industry"); Atomic(s, () => Engine.SetProject(s, c.Id, "orbital_station"));
            c.Buildings.UnionWith(["orbital_shipyard", "fusion_plant"]); Assert(Engine.SetProject(s, c.Id, "orbital_station").Success, "Eligible station blocked."); s.Player.Technologies.Add("lunar_resources"); c.Buildings.Add("lunar_exchange"); Atomic(s, () => Engine.SetProject(s, c.Id, "lunar_base"));
        });
        test("All five space programs finish through real production and persist as national milestones", () =>
        {
            var s = Fresh(); s.Player.Technologies.UnionWith(Content.Technologies.Select(t => t.Id)); var c = s.Cities[0]; c.Buildings.Add("stellar_collector"); c.ProductionPaused = false;
            foreach (var program in Epochs.Programs)
            {
                c.Buildings.Add(program.Facility); s.Player.Stability = 90; c.Production = program.Cost * 2; Resolve(s); Assert(Engine.SetProject(s, c.Id, program.Id).Success, "Cannot begin " + program.Id); Step(s);
                Assert(s.Player.SpaceMilestones.Contains(program.Id) && !c.Buildings.Contains(program.Id), "Program stored as a local decorative building.");
                if (program.Id != "ark_launch") { Atomic(s, () => Engine.SetProject(s, c.Id, program.Id)); Valid(s); }
            }
            Assert(s.Ending?.Contains("naukowe") == true, "Interstellar launch did not win."); Assert(Engine.ContinueHistory(s).Success, "Cannot continue world after victory."); Engine.CheckEnding(s); Assert(s.Ending == null, "Victory immediately retriggered."); Valid(s);
        });
        test("Atlas no longer ends an extended world before future eras", () =>
        {
            var s = Fresh(); s.Cities[0].Buildings.Add("atlas"); Engine.CheckEnding(s); Assert(s.Ending == null, "Historical atlas ended future campaign."); s.Player.Culture = 500000; s.Player.Prestige = 10000; Engine.CheckEnding(s); Assert(s.Ending == null, "Ancient culture ended future campaign.");
        });
        test("Final program pauses for inadequate stability or power without losing work", () =>
        {
            var s = Fresh(); var c = s.Cities[0]; s.Player.Technologies.UnionWith(Content.Technologies.Select(t => t.Id)); s.Player.SpaceMilestones.UnionWith(Epochs.Programs[..4].Select(p => p.Id)); c.Buildings.Add("departure_yard");
            Atomic(s, () => Engine.SetProject(s, c.Id, "ark_launch")); c.Buildings.Add("stellar_collector"); s.Player.Stability = 54; Atomic(s, () => Engine.SetProject(s, c.Id, "ark_launch")); s.Player.Stability = 80; Assert(Engine.SetProject(s, c.Id, "ark_launch").Success, "Ready launch blocked.");
            c.Production = 300; c.ProductionPaused = false; c.Buildings.Remove("stellar_collector"); Step(s); Assert(c.Production == 300 && !s.Player.SpaceMilestones.Contains("ark_launch"), "Blocked program consumed work or completed.");
        });
        test("The calendar is monotonic, reaches the future and old campaigns retain their dates", () =>
        {
            var s = Fresh(); int last = s.Year; for (int i = 2; i <= 1601; i++) { s.Turn = i; Assert(s.Year >= last, "Calendar ran backwards."); last = s.Year; } Assert(last == 5000, "No far future.");
            var old = World.CreateLegacy(); old.Turn = 10; Assert(old.Year == -2265, "Changed old calendar."); Valid(s);
        });
        test("Old campaigns explicitly extend without erasing cities, people, treasury or discovered knowledge", () =>
        {
            var s = World.CreateLegacy(); Narrative.Resolve(s, 0); s.Player.Technologies.Add("electricity"); int city = s.Cities[0].Id, units = s.Units.Count; double gold = s.Player.Gold;
            Assert(Engine.ExtendCampaign(s).Success && s.MaxTurns == 1600 && s.Cities[0].Id == city && s.Units.Count == units && s.Player.Gold == gold && s.Player.Technologies.Contains("electricity") && s.Player.Technologies.Contains(EpochCatalog.All[8].Capstone), "Extension erased campaign."); Atomic(s, () => Engine.ExtendCampaign(s)); Valid(s);
        });
        test("V16 migration preserves espionage and custody without inventing new era decisions", () =>
        {
            var s = StageSixteenSpecs.Ready(); Narrative.Resolve(s, 3); s.Version = 16; var loaded = SaveGame.Deserialize(SaveGame.Serialize(s));
            Assert(loaded.Version == 18 && !loaded.EpochCampaign && loaded.MaxTurns == 320 && loaded.SpyOperations[0].InternedUntil == s.SpyOperations[0].InternedUntil && loaded.Spies[0].Status == SpyStatus.Captured && loaded.Factions.All(f => f.EpochChoices.Count == 0 && f.SpaceMilestones.Count == 0), "Migration lost custody or invented history.");
        });
        test("Validation rejects impossible eras, milestones, climate values and mismatched models", () =>
        {
            var s = Fresh(); s.Player.EpochChoices[17] = 0; Reject(s); s.Player.EpochChoices.Clear(); s.Player.SpaceMilestones.Add("mars_settlement"); Reject(s); s.Player.SpaceMilestones.Clear(); s.ClimatePressure = 101; Reject(s); s.ClimatePressure = 0;
            var u = Engine.AddUnit(s, 0, s.Cities[0].Tile, UnitKind.Scout); u.ModelId = "tanks"; Reject(s); u.ModelId = "unknown"; Reject(s);
        });
        test("A real campaign researches every epoch, resolves its institutions and keeps deterministic saves", () =>
        {
            var s = Fresh(); s.Player.Science = 3000000; s.Player.Research = "stone_tools"; int turns = 0;
            while (s.Player.Technologies.Count < 90 && turns++ < 160) Step(s);
            Assert(s.Player.Technologies.Count == 90 && s.Player.Era == 17, "Full technology campaign stalled.");
            while (s.Player.EpochChoices.Count < 18 && turns++ < 210) Step(s);
            Assert(s.Player.EpochChoices.Count == 18 && s.Cities.Count == 4, "Era institutions failed or erased cities.");
            var loaded = SaveGame.Deserialize(SaveGame.Serialize(s)); for (int i = 0; i < 5; i++) { Step(s); Step(loaded); }
            Assert(SaveGame.Serialize(s) == SaveGame.Serialize(loaded), "Future continuation diverged.");
        });
        test("AI develops from prehistoric income into future eras without granted research or buildings", () =>
        {
            var s = World.Create(); Narrative.Resolve(s, 0); s.ContinueAfterVictory = true; s.Units.Clear(); s.Player.Research = ""; s.Cities[0].ProductionPaused = true;
            int turns = 0;
            while (s.Factions.Skip(1).Max(f => f.Era) < 17 && turns < 1500)
            {
                foreach (var r in s.Relations) r.Trust = 100; Step(s); turns++;
                if (turns % 200 == 0) Console.WriteLine($"  extended development T{turns}: AI eras {string.Join(",",s.Factions.Skip(1).Select(f=>f.Era+1))}; techs {string.Join(",",s.Factions.Skip(1).Select(f=>f.Technologies.Count))}");
                if (turns % 100 == 0) s = SaveGame.Deserialize(SaveGame.Serialize(s));
            }
            Console.WriteLine($"  future reached after {turns} turns; highest AI era {s.Factions.Skip(1).Max(f=>f.Era)+1}; cities {s.Cities.Count}");
            Assert(s.Factions.Skip(1).Any(f => f.Era == 17 && f.EpochChoices.Count >= 17) && s.Cities.Count > 4, "Natural development never reached the far future."); Valid(s);
        });
        test("Army model forecasts remain pure and preserve modern movement metadata", () =>
        {
            var s = Fresh(); var u = Engine.AddUnit(s, 0, s.Cities[0].Tile, UnitKind.Cavalry, "stellar_rovers"); string before = SaveGame.Serialize(s);
            Markets.Plan(s); Assert(SaveGame.Serialize(s) == before && u.MaxActions == 5, "Forecast changed model or game state."); Valid(s);
        });
        test("V16 save overwrite creates a faithful backup with the old custody context", () =>
        {
            var s = StageSixteenSpecs.Ready(); s.Version = 16; string old = SaveGame.Serialize(s); var loaded = SaveGame.Deserialize(old);
            string path = Path.Combine(Environment.CurrentDirectory, "artifacts", "migration-v16-test.crown");
            try { File.WriteAllText(path, old); SaveGame.Write(loaded, path); Assert(File.ReadAllText(path + ".v16.bak") == old && SaveGame.Read(path).PendingSpyOperationId == 1, "V16 backup or council was lost."); }
            finally { File.Delete(path); File.Delete(path + ".v16.bak"); }
        });
        test("Completed national programs cannot be queued and another city's redundant work moves on", () =>
        {
            var s = Fresh(); s.Player.Technologies.UnionWith(Content.Technologies.Select(t=>t.Id)); s.Player.SpaceMilestones.Add("orbital_station"); var c=s.Cities[0];
            Atomic(s,()=>Engine.EnqueueProject(s,c.Id,"orbital_station")); c.Project="orbital_station"; c.Production=200; c.ProductionQueue.Add(new(){Project="orbital_station"}); c.ProductionQueue.Add(new(){Project="scout"}); c.ProductionPaused=true;
            Step(s); Assert(c.Project=="scout"&&c.Production==200&&c.ProductionQueue.Count==0,"Redundant national program trapped a production line or destroyed its work."); Valid(s);
        });
        test("Future or repeated era councils are rejected during load rather than trapping the player",()=>
        {
            var s=Fresh();s.PendingEvent=EpochCouncils.Id(17);Reject(s);s=Council(3);s.Player.EpochChoices[3]=1;Reject(s);
        });
        test("Every new building can be ordered, completed once and used by a real city",()=>
        {
            var s=Fresh();s.Player.Technologies.UnionWith(Content.Technologies.Select(t=>t.Id));var c=s.Cities[0];c.ProductionPaused=false;
            s.Tiles[c.Tile].River=true;foreach(var t in World.Neighbors(s,c.Tile).Where(t=>t.Owner==0).Take(2))t.Terrain=Terrain.Hills;
            foreach(var b in EpochCatalog.Buildings)
            {
                Resolve(s);Assert(Engine.SetProject(s,c.Id,b.Id).Success,"Cannot order "+b.Id);c.Production=b.Cost;Step(s);
                Assert(c.Buildings.Contains(b.Id)&&!Engine.AvailableProjects(s,c).Any(p=>p.Id==b.Id),"Building missing or repeatable: "+b.Id);
            }
            Assert(c.Buildings.Count==EpochCatalog.Buildings.Length,"Not all epoch buildings survived production.");Valid(s);
        });
        test("Veteran material bonuses cannot turn a paid modernization into a downgrade",()=>
        {
            var s=Fresh();s.Player.Technologies.UnionWith(Content.Technologies.Select(t=>t.Id));var u=Engine.AddUnit(s,0,s.Cities[0].Tile,UnitKind.Spearmen);
            Assert(UnitModels.EffectiveBaseStrength(s,u)>=34&&!UnitModels.Upgrades(s,u).Any(m=>m.Id=="village_guard"),"Material bonuses ignored in upgrade eligibility.");
            Atomic(s,()=>Engine.UpgradeUnit(s,u.Id,"village_guard"));
            double before=Engine.Strength(s,u);var target=UnitModels.Upgrades(s,u).First();Assert(Engine.UpgradeUnit(s,u.Id,target.Id).Success&&Engine.Strength(s,u)>before,"Accepted modernization reduced actual combat strength.");Valid(s);
        });
        test("Modernization price uses the actual material-adjusted base strength",()=>
        {
            var s=Fresh();s.Player.Technologies.UnionWith(["bronze","iron","steel","classical_institutions"]);var u=Engine.AddUnit(s,0,s.Cities[0].Tile,UnitKind.Spearmen);
            var model=UnitModels.All.First(m=>m.Id=="legion");int expected=60+(int)(model.Strength-UnitModels.EffectiveBaseStrength(s,u))*3;double gold=s.Player.Gold;
            Assert(Engine.UpgradeUnit(s,u.Id,model.Id).Success&&s.Player.Gold==gold-expected,"Modernization charged for material strength already owned.");
        });
    }
}
