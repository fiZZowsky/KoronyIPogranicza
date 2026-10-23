using Crown.Core;
using System.Text.Json.Nodes;

internal static class StageNineSpecs
{
    private static void Assert(bool value,string message){if(!value)throw new Exception(message);}
    private static int At(GameState s,int x)=>15*s.Width+x;
    private static (GameState S,Caravan Cargo,Unit Guard) Fixture(UnitKind kind=UnitKind.Spearmen,bool second=false)
    {
        var s=World.Create();Narrative.Resolve(s,0);s.Units.Clear();
        foreach(var t in s.Tiles){t.Terrain=Terrain.Mountain;t.Resource=Resource.None;t.Owner=-1;t.Road=false;t.RoadOwner=-1;t.Ruin=false;t.Explored=true;t.Improvement=Improvement.None;}
        var seller=s.Cities.First(c=>c.Owner==0);var buyer=s.Cities.First(c=>c.Owner==1);buyer.Tile=At(s,21);
        foreach(var c in s.Cities){s.Tiles[c.Tile].Terrain=Terrain.Grassland;s.Tiles[c.Tile].Owner=c.Owner;}
        for(int x=9;x<=21;x++)s.Tiles[At(s,x)].Terrain=Terrain.Grassland;
        seller.Buildings.Add("market");buyer.Buildings.Add("market");seller.MarketStocks[TradeGood.Salt]=50;seller.MarketStocks[TradeGood.Dyes]=20;
        foreach(var f in s.Factions){f.Gold=2000;f.Technologies.UnionWith(["pottery","trade","currency"]);}
        s.Relation(0,1).Trade=true;s.InvalidateLogistics();Assert(Engine.SignTradeContract(s,buyer.Id,seller.Id,TradeGood.Salt,4).Success,"Escort contract fixture failed.");
        if(second)Assert(Engine.SignTradeContract(s,buyer.Id,seller.Id,TradeGood.Dyes,2).Success,"Second convoy fixture failed.");
        Markets.Tick(s);foreach(var contract in s.TradeContracts)Engine.CancelTradeContract(s,contract.Id);
        var guard=Engine.AddUnit(s,0,seller.Tile,kind);guard.Health=60;return(s,s.Caravans[0],guard);
    }
    private static void Assign(GameState s,Unit guard,Caravan cargo)=>Assert(Engine.AssignEscort(s,guard.Id,cargo.Id).Success,"Escort assignment failed.");
    private static void Step(GameState s,bool replenish=true){s.Turn++;if(replenish)foreach(var u in s.Units)u.Actions=u.MaxActions;s.InvalidateLogistics();Markets.Tick(s);}
    private static void Reject(GameState s)
    {bool failed=false;try{SaveGame.Deserialize(SaveGame.Serialize(s));}catch(InvalidDataException){failed=true;}Assert(failed,"Invalid escort save accepted.");}
    public static void Run(Action<string,Action> test)
    {
        test("Escort assignment replaces a march without moving, spending or duplicating an order",()=>
        {
            var (s,cargo,u)=Fixture();u.Destination=At(s,12);double gold=s.Player.Gold,actions=u.Actions;Assign(s,u,cargo);
            Assert(u.EscortCaravanId==cargo.Id&&u.Destination==null&&u.Tile==cargo.Tile&&u.Actions==actions&&s.Player.Gold==gold,"Assignment moved, charged or retained a conflicting march.");
            string once=SaveGame.Serialize(s);Assert(!Engine.AssignEscort(s,u.Id,cargo.Id).Success&&SaveGame.Serialize(s)==once,"Duplicate assignment changed state.");SaveGame.Deserialize(once);
        });
        test("Escorts reject remote, civilian, unrelated, foreign-controlled and exhausted units atomically",()=>
        {
            var (s,cargo,u)=Fixture();u.Tile=At(s,10);string before=SaveGame.Serialize(s);Assert(!Engine.AssignEscort(s,u.Id,cargo.Id).Success&&SaveGame.Serialize(s)==before,"Remote assignment changed state.");
            u.Tile=cargo.Tile;u.Kind=UnitKind.Settler;Assert(!Engine.AssignEscort(s,u.Id,cargo.Id).Success,"Settler escorted cargo.");u.Kind=UnitKind.Scout;u.Actions=0;Assert(!Engine.AssignEscort(s,u.Id,cargo.Id).Success,"Exhausted unit accepted.");
            u.Actions=3;u.Owner=2;Assert(!Engine.AssignEscort(s,u.Id,cargo.Id,2).Success,"Unrelated state escorted cargo.");u.Owner=1;Assert(!Engine.AssignEscort(s,u.Id,cargo.Id).Success,"Player assigned an AI unit.");
        });
        test("Only one designated escort can serve a caravan and a unit cannot escort two",()=>
        {
            var (s,cargo,u)=Fixture(second:true);Assign(s,u,cargo);var other=Engine.AddUnit(s,0,cargo.Tile,UnitKind.Scout);string before=SaveGame.Serialize(s);
            Assert(!Engine.AssignEscort(s,other.Id,cargo.Id).Success&&!Engine.AssignEscort(s,u.Id,s.Caravans[1].Id).Success&&SaveGame.Serialize(s)==before,"Escort uniqueness was not atomic.");
        });
        test("Joint forecasts are pure and real caravan and escort movement match them",()=>
        {
            var (s,cargo,u)=Fixture();Assign(s,u,cargo);s.Turn++;s.InvalidateLogistics();string before=SaveGame.Serialize(s);var plan=Markets.Plan(s);var move=plan.EscortMoves.Single();
            Assert(SaveGame.Serialize(s)==before&&plan.Caravans[0].Tile==At(s,11)&&move.Path.SequenceEqual(new[]{At(s,9),At(s,10),At(s,11)}),"Forecast mutated state or used free movement.");
            Markets.Tick(s);Assert(u.Tile==s.Caravans[0].Tile&&u.Tile==move.Path[^1]&&u.Actions==0&&u.EscortCaravanId==cargo.Id,"Actual joint movement differed from forecast.");
            string once=SaveGame.Serialize(s);Markets.Tick(s);Assert(SaveGame.Serialize(s)==once,"Market phase replayed joint movement.");SaveGame.Deserialize(once);
        });
        test("Slow escorts limit speed while cavalry and connected roads preserve faster movement",()=>
        {
            var (s,cargo,u)=Fixture(UnitKind.Cavalry);Assign(s,u,cargo);Step(s);Assert(u.Tile==At(s,12)&&u.Actions==0,"Cavalry did not use three movement points.");
            (s,cargo,u)=Fixture();foreach(var t in s.Tiles.Where(t=>t.Y==15&&t.Passable)){t.Road=true;t.RoadOwner=0;}Assign(s,u,cargo);Step(s);
            Assert(u.Tile==At(s,13)&&s.Caravans[0].Tile==u.Tile&&u.Actions==0,"Road escort did not spend four half-point steps.");
        });
        test("Spent movement can delay an escort but no movement is borrowed from the next phase",()=>
        {
            var (s,cargo,u)=Fixture();u.Actions=.5;Assign(s,u,cargo);Step(s,false);
            Assert(s.Caravans[0].Tile==At(s,9)&&s.Caravans[0].Status==CaravanStatus.Waiting&&s.Caravans[0].BlockedTurns==1&&u.Actions==.5,"Escort borrowed a movement point.");
            Step(s);Assert(u.Tile==At(s,11)&&s.Caravans[0].BlockedTurns==0,"Restored movement failed to resume the convoy.");
        });
        test("Terrain-aware escort ETA matches arrival and the military stops before a peaceful foreign city",()=>
        {
            var (s,cargo,u)=Fixture();Assign(s,u,cargo);var route=Caravans.Route(s,s.TradeContracts[0],cargo.Tile,u);int eta=Caravans.TravelTurns(s,route,u),steps=0;
            while(Caravans.Active(s.Caravans[0])&&steps<12){Step(s);steps++;}
            Assert(steps==eta&&s.Caravans[0].Status==CaravanStatus.Delivered&&u.Tile==At(s,20)&&u.EscortCaravanId==null,"ETA, arrival or foreign border release was wrong.");SaveGame.Deserialize(SaveGame.Serialize(s));
        });
        test("A designated escort carries protection through a hostile zone without attacking",()=>
        {
            var (s,cargo,u)=Fixture();var enemyTile=World.Neighbors(s,At(s,12)).First(t=>t.Y!=15);enemyTile.Terrain=Terrain.Grassland;
            var enemy=Engine.AddUnit(s,2,enemyTile.Id,UnitKind.Spearmen);s.Relation(0,2).War=true;Assign(s,u,cargo);
            Assert(Caravans.Route(s,s.TradeContracts[0],cargo.Tile).Count==0&&Caravans.Route(s,s.TradeContracts[0],cargo.Tile,u).Count>0,"Joint guard did not alter route safety.");
            Step(s);Step(s);Assert(s.Caravans[0].Status==CaravanStatus.Travelling&&u.Tile==s.Caravans[0].Tile&&enemy.Health==100&&u.Health==60,"Escorted convoy stopped, attacked or teleported.");
        });
        test("Direct occupation still loses cargo and releases the designated escort",()=>
        {
            var (s,cargo,u)=Fixture();Assign(s,u,cargo);Engine.AddUnit(s,2,cargo.Tile,UnitKind.Cavalry);s.Relation(0,2).War=true;Step(s);
            Assert(s.Caravans[0].Status==CaravanStatus.Lost&&u.EscortCaravanId==null&&u.Tile==cargo.Tile,"Direct occupation failed to end cargo and its order.");SaveGame.Deserialize(SaveGame.Serialize(s));
        });
        test("Commercial transit rights never grant military entry and the convoy can continue alone",()=>
        {
            var (s,cargo,u)=Fixture();s.Tiles[At(s,13)].Owner=2;s.Relation(0,2).Trade=s.Relation(1,2).Trade=true;Assign(s,u,cargo);Step(s);Step(s);
            Assert(u.Tile==At(s,12)&&u.EscortCaravanId==null&&s.Caravans[0].Tile==At(s,14)&&u.Actions==1,"Escort crossed a peaceful third-party border or convoy teleported.");SaveGame.Deserialize(SaveGame.Serialize(s));
        });
        test("Detaching at a border recalculates safety rather than carrying phantom protection onward",()=>
        {
            var (s,cargo,u)=Fixture();s.Tiles[At(s,13)].Owner=2;s.Relation(0,2).Trade=s.Relation(1,2).Trade=true;
            var enemyTile=World.Neighbors(s,At(s,15)).First(t=>t.Y!=15);enemyTile.Terrain=Terrain.Grassland;Engine.AddUnit(s,3,enemyTile.Id,UnitKind.Spearmen);s.Relation(0,3).War=true;
            Assign(s,u,cargo);Step(s);Step(s);Assert(u.Tile==At(s,12)&&u.EscortCaravanId==null&&s.Caravans[0].Tile<=At(s,13),"Detached escort protected an unreachable foreign route.");
        });
        test("Earlier convoys move their guards before later convoys evaluate shared protection",()=>
        {
            var (s,cargo,u)=Fixture(second:true);foreach(var c in s.Caravans)c.Tile=At(s,12);u.Tile=At(s,12);
            var enemyTile=World.Neighbors(s,At(s,12)).First(t=>t.Y!=15);enemyTile.Terrain=Terrain.Grassland;Engine.AddUnit(s,2,enemyTile.Id,UnitKind.Spearmen);s.Relation(0,2).War=true;
            Assign(s,u,cargo);Step(s);Assert(s.Caravans[0].Status==CaravanStatus.Travelling&&s.Caravans[1].Status==CaravanStatus.Lost,"A moved guard remained as a ghost at the earlier position.");
        });
        test("Escort orders reserve marches, attacks and pillaging until explicitly cancelled",()=>
        {
            var (s,cargo,u)=Fixture();Assign(s,u,cargo);s.Relation(0,2).War=true;var target=s.Tiles[At(s,10)];Engine.AddUnit(s,2,target.Id,UnitKind.Spearmen);s.Tiles[u.Tile].Road=true;s.Tiles[u.Tile].RoadOwner=2;
            string before=SaveGame.Serialize(s);Assert(!Engine.Move(s,u.Id,target.Id).Success&&!Engine.OrderMove(s,u.Id,At(s,11)).Success&&!Engine.CancelOrder(s,u.Id).Success&&!Engine.PreviewAttack(s,u.Id,target.Id).Valid&&!Engine.PillageRoad(s,u.Id).Success&&SaveGame.Serialize(s)==before,"Reserved unit performed a conflicting action.");
            Assert(Engine.CancelEscort(s,u.Id).Success&&Engine.PreviewAttack(s,u.Id,target.Id).Valid&&u.Actions==2,"Cancellation failed to release remaining movement.");
        });
        test("Cancelling after joint movement never refunds spent points",()=>
        {
            var (s,cargo,u)=Fixture();Assign(s,u,cargo);Step(s);Assert(Engine.CancelEscort(s,u.Id).Success&&u.Actions==0&&u.Destination==null,"Escort cancellation refunded movement.");
            string before=SaveGame.Serialize(s);Assert(!Engine.CancelEscort(s,u.Id).Success&&SaveGame.Serialize(s)==before,"Repeated cancellation mutated state.");
        });
        test("An escort discovers real intermediate ruins once while the forecast grants no rewards",()=>
        {
            var (s,cargo,u)=Fixture();s.Tiles[At(s,10)].Ruin=true;s.Tiles[At(s,10)].Explored=false;Assign(s,u,cargo);s.Turn++;string before=SaveGame.Serialize(s);Markets.Plan(s);Assert(SaveGame.Serialize(s)==before,"Forecast explored or granted ruins.");
            double gold=s.Player.Gold;Markets.Tick(s);Assert(s.Player.Gold>gold&&!s.Tiles[At(s,10)].Ruin&&s.Tiles[At(s,10)].Explored&&s.Chronicle.Count(e=>e.Category=="odkrycie")==1,"Actual escort failed to explore and collect an intermediate ruin once.");
        });
        test("Embargo holds the escort in place and an active assignment prevents passive healing",()=>
        {
            var (s,cargo,u)=Fixture();Assign(s,u,cargo);s.Player.TradePolicy=TradePolicy.Embargo;s.Turn++;s.InvalidateLogistics();
            Assert(Engine.EndTurn(s).Success&&u.Tile==cargo.Tile&&u.Health==60&&u.EscortCaravanId==cargo.Id,"Waiting escort moved or healed while on duty.");
            SaveGame.Deserialize(SaveGame.Serialize(s));
        });
        test("Cargo loss and displacement release an escort without inventing a retreat move",()=>
        {
            var (s,cargo,u)=Fixture();Assign(s,u,cargo);u.Tile=At(s,10);double actions=u.Actions;EscortOrders.Normalize(s);
            Assert(u.EscortCaravanId==null&&u.Tile==At(s,10)&&u.Actions==actions,"Disconnected guard teleported or retained an invalid assignment.");
            u.Tile=cargo.Tile;Assign(s,u,cargo);s.Player.TradePolicy=TradePolicy.Embargo;Step(s);Step(s);Step(s);
            Assert(s.Caravans[0].Status==CaravanStatus.Lost&&u.EscortCaravanId==null,"Lost cargo left a stale escort.");
        });
        test("AI may assign its own colocated scout but cannot take control of a player's unit",()=>
        {
            var (s,cargo,u)=Fixture();u.Owner=1;u.Kind=UnitKind.Scout;u.Actions=3;var player=Engine.AddUnit(s,0,cargo.Tile,UnitKind.Scout);EscortOrders.ManageAi(s,1);
            Assert(u.EscortCaravanId==cargo.Id&&player.EscortCaravanId==null,"AI failed to assign its scout or took the player's unit.");
            EscortOrders.ManageAi(s,0);Assert(player.EscortCaravanId==null,"AI helper changed player orders.");
        });
        test("Capture evacuates a designated guard and immediately clears its invalid escort order",()=>
        {
            var (s,cargo,u)=Fixture();var refuge=Engine.AddCity(s,0,At(s,17),"Schronienie");Assign(s,u,cargo);
            var origin=s.Cities.First(c=>c.Id==s.TradeContracts[0].SellerCity);ProvincialPolitics.Transfer(s,origin,2);
            Assert(u.Tile==refuge.Tile&&u.Actions==0&&u.EscortCaravanId==null&&s.Caravans[0].Tile==origin.Tile,"Capture kept a disconnected escort or moved its cargo with the retreat.");SaveGame.Deserialize(SaveGame.Serialize(s));
        });
        test("Joint movement and persistent escort orders resume deterministically after reload",()=>
        {
            var (a,cargo,u)=Fixture();Assign(a,u,cargo);Step(a);var b=SaveGame.Deserialize(SaveGame.Serialize(a));
            for(int i=0;i<9;i++){Step(a);Step(b);Assert(SaveGame.Serialize(a)==SaveGame.Serialize(b),"Escort continuation diverged.");SaveGame.Deserialize(SaveGame.Serialize(a));}
        });
        test("V8 migration preserves promises and caravans without retroactive escort assignments",()=>
        {
            var (s,cargo,u)=Fixture();Assign(s,u,cargo);var node=JsonNode.Parse(SaveGame.Serialize(s))!;node["Version"]=8;foreach(var unit in node["Units"]!.AsArray())unit!.AsObject().Remove("EscortCaravanId");string legacy=node.ToJsonString();var loaded=SaveGame.Deserialize(legacy);
            Assert(loaded.Version==15&&loaded.Caravans[0].Payment==cargo.Payment&&loaded.Units.All(v=>v.EscortCaravanId==null),"Migration assigned a guard or lost cargo history.");
            var (promises,_,_)=StageEightSpecs.Loss();TradeCouncil.Present(promises,1);Narrative.Resolve(promises,1);var promiseNode=JsonNode.Parse(SaveGame.Serialize(promises))!;promiseNode["Version"]=8;
            var preserved=SaveGame.Deserialize(promiseNode.ToJsonString());Assert(preserved.TradeCases[0].Status==TradeCaseStatus.Promised&&preserved.TradeCases[0].DeadlineTurn==promises.TradeCases[0].DeadlineTurn,"V8 migration lost an active promise.");
            string path=Path.Combine(Environment.CurrentDirectory,"artifacts","migration-v8-test.crown");
            try{File.WriteAllText(path,legacy);SaveGame.Write(loaded,path);Assert(File.ReadAllText(path+".v8.bak")==legacy&&SaveGame.Read(path).Version==15,"V8 backup missing.");}
            finally{File.Delete(path);File.Delete(path+".v8.bak");}
        });
        test("Save validation rejects unknown, remote, civilian, duplicated and conflicting escort orders",()=>
        {
            var (s,cargo,u)=Fixture();Assign(s,u,cargo);string valid=SaveGame.Serialize(s);u.EscortCaravanId=999;Reject(s);
            s=SaveGame.Deserialize(valid);s.Units[0].Tile=At(s,10);Reject(s);s=SaveGame.Deserialize(valid);s.Units[0].Kind=UnitKind.Settler;Reject(s);
            s=SaveGame.Deserialize(valid);s.Units[0].Owner=2;Reject(s);s=SaveGame.Deserialize(valid);s.Units[0].Destination=At(s,12);Reject(s);
            s=SaveGame.Deserialize(valid);var duplicate=Engine.AddUnit(s,0,cargo.Tile,UnitKind.Scout);duplicate.EscortCaravanId=cargo.Id;Reject(s);
            s=SaveGame.Deserialize(valid);s.Caravans[0].Status=CaravanStatus.Lost;s.Caravans[0].ResolvedTurn=s.Turn+1;s.Caravans[0].LastMovedTurn=s.Turn+1;s.LastMarketTurn=++s.Turn;Reject(s);
        });
    }
}
