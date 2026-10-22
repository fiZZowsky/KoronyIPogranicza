using Crown.Core;
using System.Text.Json.Nodes;

internal static class StageTwoSpecs
{
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static GameState Corridor()
    {
        var s = World.Create();
        Narrative.Resolve(s, 0);
        foreach (var tile in s.Tiles)
        {
            tile.Terrain = Terrain.Mountain; tile.Owner = -1; tile.Explored = true;
            tile.Ruin = false; tile.River = false; tile.Resource = Resource.None;
            tile.Improvement = Improvement.None; tile.Road = false; tile.RoadOwner = -1;
        }
        foreach (var c in s.Cities) { s.Tiles[c.Tile].Terrain = Terrain.Grassland; s.Tiles[c.Tile].Owner = c.Owner; }
        foreach (var u in s.Units) s.Tiles[u.Tile].Terrain = Terrain.Grassland;
        for (int x = 9; x <= 23; x++) s.Tiles[15 * s.Width + x].Terrain = Terrain.Grassland;
        s.Player.Gold = 2000;
        s.Player.Technologies.Add("pottery");
        s.InvalidateLogistics();
        return s;
    }

    private static int At(GameState s, int x) => 15 * s.Width + x;
    private static Unit Army(GameState s) => s.Units.First(u => u.Owner == 0 && u.Kind == UnitKind.Spearmen);
    private static void Roads(GameState s, int end)
    {
        for (int x = 10; x <= end; x++)
            if (!Navigation.RoadNode(s, At(s, x))) Check(Engine.BuildRoad(s, At(s, x)).Success, "Road construction failed.");
    }

    public static void Run(Action<string, Action> test)
    {
        test("Road movement, full reach and compatible improvements", () =>
        {
            var s = Corridor(); var u = Army(s);
            s.Tiles[At(s, 11)].Terrain = Terrain.Forest;
            s.Tiles[At(s, 10)].Improvement = Improvement.Farm;
            Check(!Engine.Move(s, u.Id, At(s, 13)).Success, "Walked four tiles without roads.");
            Roads(s, 15);
            Check(s.Tiles[At(s, 10)].Improvement == Improvement.Farm, "Road erased a farm.");
            var reach = Navigation.ForUnit(s, u, u.Actions).Costs;
            Check(reach[At(s, 13)] == 2 && !reach.ContainsKey(At(s, 14)), "Road range preview is wrong.");
            Check(Engine.Move(s, u.Id, At(s, 13)).Success && u.Actions == 0, "Road movement did not cost half-points.");
        });
        test("Road quotes, atomic funding and economy reconciliation", () =>
        {
            var s = Corridor(); var city = Engine.AddCity(s, 0, At(s, 18), "Drugi Brzeg");
            var before = Engine.CityYields(s, city);
            var plan = Logistics.PlanRoad(s, city.Id);
            Check(plan.NewTiles == 8 && plan.Cost == 200, "Incorrect road quote.");
            s.Player.Gold = 199; string snapshot = SaveGame.Serialize(s);
            Check(!Engine.ConnectCity(s, city.Id).Success && SaveGame.Serialize(s) == snapshot, "Insufficient funds built a partial route.");
            s.Player.Gold = 500;
            Check(Engine.ConnectCity(s, city.Id).Success && s.Player.Gold == 300, "Wrong construction charge.");
            Check(Logistics.For(s, 0).ConnectedCities.Contains(city.Id), "Road did not connect cities.");
            var after = Engine.CityYields(s, city);
            Check(after.Gold > before.Gold && after.Science > before.Science, "No city connection bonus.");
            var economy = Engine.Economy(s, 0);
            Check(Math.Abs(economy.RoadCost - 1.2) < .00001, "Wrong upkeep.");
            Check(Math.Abs(economy.Net - Engine.Income(s, 0).Gold) < .00001, "Budget does not match turn income.");
            snapshot = SaveGame.Serialize(s);
            Check(!Engine.ConnectCity(s, city.Id).Success && SaveGame.Serialize(s) == snapshot, "Charged twice for existing roads.");
        });
        test("Supplies follow passable routes and roads extend reach", () =>
        {
            var s = Corridor(); var u = Army(s); u.Tile = At(s, 14);
            s.Tiles[At(s, 12)].Terrain = Terrain.Ocean; s.InvalidateLogistics();
            Check(!Engine.Supplied(s, u), "Supplies crossed water based on straight distance.");
            s.Tiles[At(s, 12)].Terrain = Terrain.Grassland; u.Tile = At(s, 20); s.InvalidateLogistics();
            Check(!Engine.Supplied(s, u), "Supplied a distant army without a road.");
            Roads(s, 20);
            Check(Engine.Supplied(s, u) && Logistics.For(s, 0).Costs[u.Tile] == 5.5, "Road did not extend supplies.");
        });
        test("Winter increases off-road logistics cost", () =>
        {
            var s = Corridor(); var u = Army(s); u.Tile = At(s, 15); s.Turn = 3;
            Check(Engine.Supplied(s, u), "Summer six-point route failed.");
            s.Turn = 4;
            Check(!Engine.Supplied(s, u), "Winter did not reduce off-road reach.");
            Roads(s, 15);
            Check(Engine.Supplied(s, u), "Roads failed to protect winter transport.");
        });
        test("Enemy interdiction cuts trade connections and moving removes blockade", () =>
        {
            var s = Corridor(); var city = Engine.AddCity(s, 0, At(s, 18), "Drugi Brzeg");
            Engine.ConnectCity(s, city.Id);
            Check(Logistics.For(s, 0).ConnectedCities.Contains(city.Id), "Initial network missing.");
            s.Relation(0, 1).War = true;
            var enemy = Engine.AddUnit(s, 1, At(s, 14), UnitKind.Spearmen);
            Check(!Logistics.For(s, 0).ConnectedCities.Contains(city.Id), "Enemy did not interdict the route.");
            Check(Logistics.For(s, 0).Interdicted.Contains(At(s, 13)), "No adjacent zone of control.");
            // Movement of a blocker invalidates the cached supply map.
            Check(Engine.Move(s, enemy.Id, At(s, 15)).Success, "Enemy could not move on road.");
            Check(Logistics.For(s, 0).Interdicted.Contains(At(s, 16)), "Blockade cache remained at the old tile.");
            enemy.Tile = s.Cities.First(c => c.Owner == 1).Tile; s.InvalidateLogistics();
            Check(Logistics.For(s, 0).ConnectedCities.Contains(city.Id), "Connection did not recover.");
        });
        test("Road pillaging spends movement and breaks the segment", () =>
        {
            var s = Corridor(); var u = Army(s); Roads(s, 15); u.Tile = At(s, 13);
            s.Tiles[u.Tile].RoadOwner = 1; s.Relation(0, 1).War = true; s.InvalidateLogistics();
            double gold = s.Player.Gold;
            Check(Engine.PillageRoad(s, u.Id).Success, "Could not pillage enemy road.");
            Check(!s.Tiles[u.Tile].Road && s.Player.Gold == gold + 20 && u.Actions == 1, "Incorrect pillage effects.");
            Check(!Engine.PillageRoad(s, u.Id).Success, "Pillaged the same road twice.");
        });
        test("Multi-turn orders survive saving and arrive without free healing", () =>
        {
            var s = Corridor(); var u = Army(s); u.Health = 80;
            Check(Engine.OrderMove(s, u.Id, At(s, 16)).Success && u.Tile == At(s, 11) && u.Destination == At(s, 16), "Order did not execute its first leg.");
            var loaded = SaveGame.Deserialize(SaveGame.Serialize(s));
            for (int i = 0; i < 3; i++) { Engine.EndTurn(s); Engine.EndTurn(loaded); }
            Check(u.Tile == At(s, 16) && u.Destination == null, "Order did not finish.");
            Check(u.Health == 80, "Marching granted idle healing.");
            Check(SaveGame.Serialize(s) == SaveGame.Serialize(loaded), "Saved march diverged after loading.");
        });
        test("Blocked march stops without attacking and cancellation preserves movement", () =>
        {
            var s = Corridor(); var u = Army(s);
            Engine.OrderMove(s, u.Id, At(s, 18)); s.Relation(0, 1).War = true;
            var enemy = Engine.AddUnit(s, 1, At(s, 14), UnitKind.Spearmen);
            Engine.EndTurn(s);
            Check(u.Destination == null && u.Health == 100 && enemy.Health == 95, "March attacked automatically through a blocker (beyond normal supply attrition).");
            s = Corridor(); u = Army(s); Engine.OrderMove(s, u.Id, At(s, 18)); double actions = u.Actions;
            Check(Engine.CancelOrder(s, u.Id).Success && u.Destination == null && u.Actions == actions, "Cancellation refunded movement.");
            s.Tiles[At(s, 20)].Explored = false;
            Check(!Engine.OrderMove(s, u.Id, At(s, 20)).Success, "Queued a march into unexplored terrain.");
        });
        test("Movement discovers ruins on intermediate tiles", () =>
        {
            var s = Corridor(); var u = Army(s); Roads(s, 15);
            s.Tiles[At(s, 10)].Ruin = true; s.Tiles[At(s, 12)].Ruin = true;
            double before = s.Player.Science;
            Engine.Move(s, u.Id, At(s, 13));
            Check(!s.Tiles[At(s, 10)].Ruin && !s.Tiles[At(s, 12)].Ruin && s.Player.Science == before + 24, "Movement skipped intermediate discoveries.");
        });
        test("Combat preview is pure and exactly matches damage", () =>
        {
            var s = Corridor(); var u = Army(s);
            s.Units.RemoveAll(e => e.Owner == 0 && e.Id != u.Id);
            s.Relation(0, 1).War = true;
            var enemy = Engine.AddUnit(s, 1, At(s, 10), UnitKind.Spearmen);
            s.Tiles[enemy.Tile].Terrain = Terrain.Forest; u.Health = 70; enemy.Health = 65;
            string before = SaveGame.Serialize(s);
            var preview = Engine.PreviewAttack(s, u.Id, enemy.Tile);
            Check(preview.Valid && SaveGame.Serialize(s) == before, "Preview changed gameplay state.");
            double ownHealth = u.Health, enemyHealth = enemy.Health;
            Check(Engine.Move(s, u.Id, enemy.Tile).Success, "Attack failed after preview.");
            Check(Math.Abs(u.Health - (ownHealth - preview.DamageToAttacker)) < .00001 &&
                Math.Abs(enemy.Health - (enemyHealth - preview.DamageToTarget)) < .00001, "Preview and combat disagree.");
        });
        test("Ranged preview respects mountains and movement requirements", () =>
        {
            var s = Corridor(); var u = Army(s); u.Kind = UnitKind.Archer;
            s.Relation(0, 1).War = true; Engine.AddUnit(s, 1, At(s, 11), UnitKind.Spearmen);
            s.Tiles[At(s, 10)].Terrain = Terrain.Mountain; s.InvalidateLogistics();
            Check(!Engine.PreviewAttack(s, u.Id, At(s, 11)).Valid, "Ranged attack ignored mountains.");
            s.Tiles[At(s, 10)].Terrain = Terrain.Grassland; s.InvalidateLogistics();
            var preview = Engine.PreviewAttack(s, u.Id, At(s, 11));
            Check(preview.Valid && preview.Ranged && preview.DamageToAttacker == 0, "Ranged counterattack was predicted.");
            u.Actions = .5; Check(!Engine.PreviewAttack(s, u.Id, At(s, 11)).Valid, "Half a movement point allowed attacking.");
        });
        test("Infrastructure dialogue changes permanent logistics policy", () =>
        {
            var s = Corridor();var city=Engine.AddCity(s,0,At(s,18),"Drugi Brzeg");Engine.ConnectCity(s,city.Id);s.Turn=10;s.PendingEvent=null;
            Narrative.Schedule(s);Check(s.PendingEvent=="caravan_compact","The road network did not trigger its council debate.");
            double before=Engine.CityYields(s,city).Science;Narrative.Resolve(s,0);
            Check(s.Player.Flags.Contains("open_roads")&&Engine.CityYields(s,city).Science>before,"Open-road policy did not increase science.");
            s=Corridor();s.PendingEvent="caravan_compact";double range=Logistics.Range(s,0);Narrative.Resolve(s,2);
            Check(Logistics.Range(s,0)==range+1,"Patrol policy did not extend logistics.");
        });
        test("Legacy v1 migration, backup and v2 order validation", () =>
        {
            var s = Corridor();
            var json = JsonNode.Parse(SaveGame.Serialize(s))!; json["Version"] = 1;
            foreach (var tile in json["Tiles"]!.AsArray()) { tile!.AsObject().Remove("Road"); tile.AsObject().Remove("RoadOwner"); }
            foreach (var unit in json["Units"]!.AsArray()) unit!.AsObject().Remove("Destination");
            string legacy = json.ToJsonString(); var migrated = SaveGame.Deserialize(legacy);
            Check(migrated.Version == 12 && migrated.Tiles.All(t => !t.Road) && migrated.Units.All(u => u.Destination == null), "Legacy migration failed.");
            var folder = Path.Combine(AppContext.BaseDirectory, "CrownSpecs-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder); string path = Path.Combine(folder, "legacy.crown");
            try
            {
                File.WriteAllText(path, legacy); SaveGame.Write(migrated, path);
                Check(File.ReadAllText(path + ".v1.bak") == legacy && SaveGame.Read(path).Version == 12, "Original v1 save was not backed up.");
            }
            finally { foreach (string file in new[] { path, path + ".v1.bak", path + ".tmp" }) if (File.Exists(file)) File.Delete(file); Directory.Delete(folder); }
            migrated.Units[0].Destination = -1; bool rejected = false;
            try { SaveGame.Deserialize(SaveGame.Serialize(migrated)); } catch (InvalidDataException) { rejected = true; }
            Check(rejected, "Accepted an invalid destination.");
            migrated.Units[0].Destination = null; migrated.Units[0].Actions = .25; rejected = false;
            try { SaveGame.Deserialize(SaveGame.Serialize(migrated)); } catch (InvalidDataException) { rejected = true; }
            Check(rejected, "Accepted invalid movement precision.");
        });
    }
}
