using Crown.Core;
using System.Text.Json.Nodes;

internal static class StageThreeSpecs
{
    private static void Assert(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static int At(GameState s, int x) => 15 * s.Width + x;
    private static (GameState State, City A, City B, City C) Fixture()
    {
        var s = World.CreateLegacy(); Narrative.Resolve(s, 0);
        foreach (var t in s.Tiles)
        {
            t.Terrain = Terrain.Mountain; t.Owner = -1; t.Resource = Resource.None;
            t.River = t.Ruin = t.Road = false; t.RoadOwner = -1; t.Explored = true;
        }
        foreach (var c in s.Cities) { s.Tiles[c.Tile].Terrain = Terrain.Grassland; s.Tiles[c.Tile].Owner = c.Owner; }
        foreach (var u in s.Units) s.Tiles[u.Tile].Terrain = Terrain.Grassland;
        for (int x = 9; x <= 23; x++) s.Tiles[At(s, x)].Terrain = Terrain.Grassland;
        var a = s.Cities.First(c => c.Owner == 0); a.Population = 2;
        foreach(var tile in s.Tiles.Where(t=>World.Distance(t,s.Tiles[a.Tile])<=2))tile.Owner=0;
        var b = Engine.AddCity(s, 0, At(s, 15), "Kamieniec");
        var c2 = Engine.AddCity(s, 0, At(s, 21), "Brzegowo");
        s.Player.Technologies.UnionWith(["pottery", "agriculture", "masonry"]); s.Player.Gold = 2000;
        s.InvalidateLogistics();
        return (s, a, b, c2);
    }
    private static void Connect(GameState s, int from, int to)
    {
        for (int x = from; x <= to; x++)
            if (!Navigation.RoadNode(s, At(s, x))) Assert(Engine.BuildRoad(s, At(s, x)).Success, "Road failed.");
    }

    public static void Run(Action<string, Action> test)
    {
        test("Food exchange conserves output and respects road components", () =>
        {
            var (s, a, b, _) = Fixture(); b.Population = 10;
            Assert(Regions.PlanFood(s, 0).Imports[b.Id] == 0, "Imported without a road.");
            Connect(s, 10, 14); var food = Regions.PlanFood(s, 0);
            Assert(food.Imports[b.Id] > 0 && food.Exports[a.Id] > 0, "Connected surplus did not feed a deficit.");
            Assert(Math.Abs(food.Raw.Values.Sum() - s.Cities.Where(c => c.Owner == 0).Sum(c => food.Balance(c.Id))) < .00001, "Food was created by trade.");
            Assert(food.Exports.All(p => p.Value <= Math.Min(6, Math.Max(0, food.Raw[p.Key]))) && food.Imports.Values.All(v => v <= 6), "Transport limit exceeded.");
        });
        test("Provincial exchange works without capital access", () =>
        {
            var (s, a, b, c) = Fixture(); b.Population = 10; Connect(s, 16, 20);
            var network = Logistics.For(s, 0);
            Assert(!network.ConnectedCities.Contains(b.Id) && network.MarketGroups[b.Id] == network.MarketGroups[c.Id], "Outlying market group was lost.");
            var food = Regions.PlanFood(s, 0);
            Assert(food.Imports[b.Id] > 0 && food.Exports[c.Id] > 0 && food.Exports[a.Id] == 0, "Trade incorrectly requires the capital.");
        });
        test("Blockade interrupts provincial food and luxury access", () =>
        {
            var (s, a, b, _) = Fixture(); b.Population = 10;
            var salt = s.Tiles[At(s, 10)]; salt.Owner = 0; salt.Resource = Resource.Salt;
            Connect(s, 10, 14);
            Assert(Regions.Goods(s, b).Contains("Sól") && Regions.PlanFood(s, 0).Imports[b.Id] > 0, "Goods not shared through road.");
            s.Relation(0, 1).War = true; Engine.AddUnit(s, 1, At(s, 12), UnitKind.Spearmen);
            Assert(!Regions.Goods(s, b).Contains("Sól") && Regions.PlanFood(s, 0).Imports[b.Id] == 0, "Goods crossed a blockade.");
        });
        test("Food allocation prioritizes urgent shortages deterministically", () =>
        {
            var (s, a, b, c) = Fixture(); b.Population = 10; c.Population = 14; b.Food = 2; c.Food = 5;
            Connect(s, 10, 20); var food = Regions.PlanFood(s, 0);
            Assert(food.Imports[c.Id] == 6 && food.Imports[b.Id] == 0, "Urgent shortage did not receive limited supply first.");
            var clone = SaveGame.Deserialize(SaveGame.Serialize(s));
            Assert(Regions.PlanFood(clone, 0).Shipments.SequenceEqual(food.Shipments), "Food allocation diverged after loading.");
        });
        test("Turn processing uses food transfers before starvation", () =>
        {
            var (s, _, b, _) = Fixture(); b.Population = 10; b.Food = 0; Connect(s, 10, 14);
            int population = b.Population; Engine.EndTurn(s);
            Assert(b.LastFoodImported > 0 && b.HungerTurns == 0 && b.Population == population, "Imported food did not prevent hunger.");
            var (isolated, _, hungry, _) = Fixture(); hungry.Population = 10; hungry.Food = 0;
            Engine.EndTurn(isolated); Assert(hungry.Population == 10 && hungry.HungerTurns == 1, "Population fell on the first shortage turn.");
            Engine.EndTurn(isolated); Assert(hungry.Population == 9 && hungry.HungerTurns == 2, "Prolonged hunger did not reduce population.");
        });
        test("Housing and healthcare relieve pressure and prosperity affects yields", () =>
        {
            var (s, _, b, _) = Fixture(); b.Population = 10; b.Food = 100;
            var before = Regions.Status(s, b); Assert(before.Pressure > 0, "Overcrowding has no pressure.");
            Regions.Tick(s, 0); Assert(b.Unrest > 0 && b.Prosperity < 50, "Needs did not affect local wellbeing.");
            b.Buildings.UnionWith(["housing", "aqueduct", "infirmary"]);
            var after = Regions.Status(s, b); Assert(after.Housing > before.Housing && after.Sanitation > before.Sanitation && after.Pressure == 0, "Buildings did not meet needs.");
            b.Prosperity = 0; double low = Engine.CityYields(s, b).Production; b.Prosperity = 100;
            Assert(Engine.CityYields(s, b).Production > low, "Prosperity has no economic consequence.");
        });
        test("City focus has costs, tradeoffs and a per-turn cooldown", () =>
        {
            var (s, a, _, _) = Fixture(); var before = Engine.CityYields(s, a); double gold = s.Player.Gold;
            Assert(Engine.SetCityFocus(s, a.Id, CityFocus.Agriculture).Success, "Focus change failed.");
            var after = Engine.CityYields(s, a);
            Assert(s.Player.Gold == gold - 15 && after.Food > before.Food && after.Production < before.Production, "Agricultural tradeoff missing.");
            string snapshot = SaveGame.Serialize(s); Assert(!Engine.SetCityFocus(s, a.Id, CityFocus.Industry).Success && SaveGame.Serialize(s) == snapshot, "Focus changed twice in a turn.");
            s.Turn++; Assert(Engine.SetCityFocus(s, a.Id, CityFocus.Industry).Success && Regions.Sanitation(s, a) == 5, "Industrial priority has no sanitation cost.");
            s.Turn++;Assert(Engine.SetCityFocus(s,a.Id,CityFocus.Scholarship).Success&&Engine.CityYields(s,a).Science>before.Science&&Engine.CityYields(s,a).Gold<before.Gold,"Scholarship has no science/gold tradeoff.");
        });
        test("Emergency aid charges once and cannot bypass its cooldown", () =>
        {
            var (s, a, _, _) = Fixture(); a.Food = 0; a.Unrest = 20; double gold = s.Player.Gold;
            Assert(Engine.GrantCityAid(s, a.Id).Success && a.Food == 25 && a.Unrest == 12 && s.Player.Gold == gold - 50, "Aid effects wrong.");
            string before = SaveGame.Serialize(s); Assert(!Engine.GrantCityAid(s, a.Id).Success && SaveGame.Serialize(s) == before, "Aid cooldown mutated state.");
            s.Turn += 5; Assert(Engine.GrantCityAid(s, a.Id).Success, "Aid did not become available after five turns.");
        });
        test("Migration needs safe roads and conserves inhabitants", () =>
        {
            var (s, _, b, c) = Fixture(); b.Population = 8; b.Prosperity = 20; b.Unrest = 50; c.Prosperity = 85; s.Turn = 4;
            int population = s.Cities.Sum(city => city.Population); Regions.Migrate(s, 0);
            Assert(s.Migrations.Count == 0, "Families crossed a disconnected frontier.");
            Connect(s, 16, 20); Regions.Migrate(s, 0);
            Assert(b.Population == 7 && c.Population == 4 && s.Cities.Sum(city => city.Population) == population, "Migration created or lost inhabitants.");
            Assert(c.MigrantsReceived == 1 && b.MigrantsDeparted == 1 && s.Migrations.Count == 1, "Migration not recorded.");
            s.Turn = 8; Regions.Migrate(s, 0); Assert(s.Migrations.Count == 1, "Families ping-ponged before cooldown.");
            s.Turn = 12; Regions.Migrate(s, 0); Assert(s.Migrations.Count == 2, "Migration did not resume after cooldown.");
        });
        test("Migration refuses overcrowded and unhealthy destinations", () =>
        {
            var (s, _, b, c) = Fixture(); b.Population = 8; b.Prosperity = 20; c.Population = 6; c.Prosperity = 85; s.Turn = 4;
            Connect(s, 16, 20); Regions.Migrate(s, 0); Assert(s.Migrations.Count == 0, "Families moved to a full destination.");
            c.Buildings.Add("housing"); Regions.Migrate(s, 0); Assert(s.Migrations.Count == 0, "Housing alone bypassed healthcare capacity.");
        });
        test("Provincial dialogue targets the affected city and prevents replay", () =>
        {
            var (s, a, b, c) = Fixture(); b.Population = 10; b.Food = 0; s.Turn = 5;
            double unaffected = a.Food; Narrative.Schedule(s);
            Assert(s.PendingEvent == "local_grain" && s.PendingCityId == b.Id && ProvincialEvents.Current(s).Title.Contains(b.Name), "Council did not target the hungry province.");
            Assert(Narrative.Resolve(s, 0).Success && b.Food == 40 && a.Food == unaffected && b.ResolvedIssues.Contains("local_grain"), "Council aid affected the wrong city.");
            s.PendingEvent = "local_grain"; s.PendingCityId = b.Id; string before = SaveGame.Serialize(s);
            Assert(!Narrative.Resolve(s, 0).Success && SaveGame.Serialize(s) == before, "Repeated the same local award.");
            s.PendingEvent = null; s.PendingCityId = null; s.Turn = 10; c.Population = 14; c.Food = 0; Narrative.Schedule(s);
            Assert(s.PendingCityId == c.Id && s.PendingEvent == "local_grain", "A different province could not raise the same issue.");
        });
        test("Local charters persist and integration changes later arrivals", () =>
        {
            var (s, _, b, c) = Fixture(); int housing = Regions.Housing(s, b);
            s.PendingEvent = "local_housing"; s.PendingCityId = b.Id; Narrative.Resolve(s, 0);
            Assert(Regions.Housing(s, b) == housing + 2, "Housing charter had no lasting effect.");
            s.PendingEvent = "local_settlement"; s.PendingCityId = c.Id; Narrative.Resolve(s, 0);
            b.Population = 8; b.Prosperity = 20; c.Prosperity = 85; b.Unrest = 50; s.Turn = 4;
            Connect(s, 16, 20); int unrest = c.Unrest; Regions.Migrate(s, 0);
            Assert(c.MigrantsReceived == 1 && c.Unrest == unrest, "Integration failed to prevent arrival unrest.");
            var loaded = SaveGame.Deserialize(SaveGame.Serialize(s));
            Assert(loaded.Cities.First(city => city.Id == b.Id).Charters.Contains("housing_rights") && loaded.Migrations.SequenceEqual(s.Migrations), "Local history was lost in save.");
        });
        test("Local guild charters and bonded labor retain economic tradeoffs", () =>
        {
            var (s,a,b,_) = Fixture();double gold=Engine.CityYields(s,a).Gold;int housing=Regions.Housing(s,a);
            s.PendingEvent="local_housing";s.PendingCityId=a.Id;Narrative.Resolve(s,1);
            Assert(Regions.Housing(s,a)==housing+2&&Engine.CityYields(s,a).Gold<gold,"Guild housing did not charge its local fee.");
            var baseline=Engine.CityYields(s,b);int target=Regions.Status(s,b).ProsperityTarget;
            s.PendingEvent="local_settlement";s.PendingCityId=b.Id;Narrative.Resolve(s,1);
            Assert(Engine.CityYields(s,b).Production>baseline.Production&&Regions.Status(s,b).ProsperityTarget<target,"Bonded labor has no productivity/wellbeing tradeoff.");
            (s,a,b,_)=Fixture();a.Unrest=30;s.PendingEvent="local_settlement";s.PendingCityId=a.Id;Narrative.Resolve(s,2);int unrest=a.Unrest;
            Regions.Tick(s,0);Assert(a.Unrest==unrest-4,"Local assembly did not add its calming effect.");
        });
        test("AI prioritizes food relief and missing housing", () =>
        {
            var (s, _, b, _) = Fixture(); b.Population = 10; b.Food = 0; b.Project = "treasury";
            Regions.ManageAi(s, b);
            Assert(b.Focus == CityFocus.Agriculture && b.Food == 25 && b.Project == "housing", "AI ignored provincial needs.");
        });
        test("V2 migration preserves roads and orders and backs up the original", () =>
        {
            var (s, _, b, _) = Fixture(); Connect(s, 10, 14);
            var unit = s.Units.First(u => u.Owner == 0 && u.Kind == UnitKind.Spearmen); Engine.OrderMove(s, unit.Id, b.Tile);
            var json = JsonNode.Parse(SaveGame.Serialize(s))!; json["Version"] = 2; json.AsObject().Remove("Migrations"); json.AsObject().Remove("PendingCityId");
            string[] fields = ["Focus", "Prosperity", "HungerTurns", "LastFocusTurn", "LastAidTurn", "LastMigrationTurn", "LastCouncilTurn", "MigrantsReceived", "MigrantsDeparted", "LastFoodImported", "LastFoodExported", "Charters", "ResolvedIssues"];
            foreach (var city in json["Cities"]!.AsArray()) foreach (string field in fields) city!.AsObject().Remove(field);
            json["Cities"]![0]!["Unrest"]=145; // V2 did not cap this field; migrate it to the bounded V3 scale.
            string legacy = json.ToJsonString(); var loaded = SaveGame.Deserialize(legacy);
            Assert(loaded.Version == 18 && loaded.Cities.All(city => city.Prosperity == 50 && city.Focus == CityFocus.Balanced) && loaded.Cities[0].Unrest==100 && loaded.Tiles.Any(t => t.Road) && loaded.Units.Any(u => u.Destination != null), "V2 migration lost existing logistics or rejected its old unrest scale.");
            string folder = Path.Combine(AppContext.BaseDirectory, "V3Specs-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder); string path = Path.Combine(folder, "old.crown");
            try { File.WriteAllText(path, legacy); SaveGame.Write(loaded, path); Assert(File.ReadAllText(path + ".v2.bak") == legacy && SaveGame.Read(path).Version == 18, "V2 backup failed."); }
            finally { foreach (string file in new[] { path, path + ".v2.bak", path + ".tmp" }) if (File.Exists(file)) File.Delete(file); Directory.Delete(folder); }
        });
        test("V3 validation rejects corrupt needs, context and migration records", () =>
        {
            var (s, a, _, _) = Fixture(); a.Prosperity = 101; bool rejected = false;
            try { SaveGame.Deserialize(SaveGame.Serialize(s)); } catch (InvalidDataException) { rejected = true; }
            Assert(rejected, "Invalid prosperity accepted."); a.Prosperity = 50; s.PendingEvent = "local_grain"; s.PendingCityId = -1; rejected = false;
            try { SaveGame.Deserialize(SaveGame.Serialize(s)); } catch (InvalidDataException) { rejected = true; }
            Assert(rejected, "Missing local context accepted."); s.PendingEvent = null; s.PendingCityId = null; s.Migrations.Add(new(1, 0, a.Id, 999, 1)); rejected = false;
            try { SaveGame.Deserialize(SaveGame.Serialize(s)); } catch (InvalidDataException) { rejected = true; }
            Assert(rejected, "Invalid migration city accepted.");
        });
        test("V3 continuation deterministically preserves provincial decisions and trade", () =>
        {
            var (s, _, b, _) = Fixture(); Connect(s, 10, 20); b.Population = 10; b.Food = 0; s.Turn = 5; Narrative.Schedule(s);
            var copy = SaveGame.Deserialize(SaveGame.Serialize(s));
            for (int turn = 0; turn < 20; turn++) foreach (var game in new[] { s, copy })
            {
                if (game.PendingEvent != null) { var e = Narrative.Get(game.PendingEvent); Narrative.Resolve(game, Array.FindIndex(e.Choices, c => game.Player.Gold + c.Gold >= 0)); }
                Engine.EndTurn(game);
            }
            Assert(SaveGame.Serialize(s) == SaveGame.Serialize(copy), "Provincial simulation diverged after loading.");
        });
    }
}
