using Crown.Core;
using System.Text.Json.Nodes;

internal static class StageSevenSpecs
{
    private static void Assert(bool value,string message){if(!value)throw new Exception(message);}
    private static int At(GameState s,int x)=>15*s.Width+x;
    private static (GameState S,City Buyer,City Seller) Fixture()
    {
        var s=World.Create();Narrative.Resolve(s,0);s.Units.Clear();
        foreach(var t in s.Tiles){t.Terrain=Terrain.Mountain;t.Owner=-1;t.Resource=Resource.None;t.Improvement=Improvement.None;t.Road=false;t.RoadOwner=-1;t.Explored=true;}
        var buyer=s.Cities.First(c=>c.Owner==0);var seller=s.Cities.First(c=>c.Owner==1);seller.Tile=At(s,21);
        foreach(var c in s.Cities){s.Tiles[c.Tile].Terrain=Terrain.Grassland;s.Tiles[c.Tile].Owner=c.Owner;}
        for(int x=9;x<=21;x++)s.Tiles[At(s,x)].Terrain=Terrain.Grassland;
        buyer.Buildings.Add("market");seller.Buildings.Add("market");seller.MarketStocks[TradeGood.Salt]=60;
        foreach(var f in s.Factions){f.Gold=2000;f.Technologies.UnionWith(["pottery","trade","currency"]);}
        s.Relation(0,1).Trade=true;s.InvalidateLogistics();return(s,buyer,seller);
    }
    private static void Step(GameState s){s.Turn++;s.InvalidateLogistics();Markets.Tick(s);}
    private static void Sign(GameState s,City buyer,City seller)=>Assert(Engine.SignTradeContract(s,buyer.Id,seller.Id,TradeGood.Salt,4).Success,"Fixture contract rejected.");
    private static void Reject(GameState s)
    {bool failed=false;try{SaveGame.Deserialize(SaveGame.Serialize(s));}catch(InvalidDataException){failed=true;}Assert(failed,"Invalid caravan save accepted.");}
    public static void Run(Action<string,Action> test)
    {
        test("Caravan dispatch pays once and arrival is delayed by actual terrain distance",()=>
        {
            var (s,buyer,seller)=Fixture();Sign(s,buyer,seller);double gold=s.Player.Gold;Markets.Tick(s);var cargo=s.Caravans.Single();double paid=cargo.Payment;
            Assert(cargo.Tile==seller.Tile&&buyer.LastMarketLines.Single(l=>l.Good==TradeGood.Salt).ForeignIn==0&&s.Player.Gold==gold-paid,"Instant delivery or incorrect dispatch payment.");
            Engine.CancelTradeContract(s,1);
            for(int n=0;n<3;n++){Step(s);Assert(Caravans.Active(s.Caravans.Single())&&buyer.LastMarketLines.Single(l=>l.Good==TradeGood.Salt).ForeignIn==0,"Arrived before walking the route.");}
            Step(s);Assert(s.Caravans.Single().Status==CaravanStatus.Delivered&&s.Caravans.Single().Tile==buyer.Tile&&buyer.LastMarketLines.Single(l=>l.Good==TradeGood.Salt).ForeignIn==4&&s.Player.Gold==gold-paid,"Arrival lost cargo or charged twice.");
            Step(s);Assert(buyer.LastMarketLines.Single(l=>l.Good==TradeGood.Salt).ForeignIn==0,"Arrival replayed.");SaveGame.Deserialize(SaveGame.Serialize(s));
        });
        test("Road speed and terrain-aware ETA match movement phase boundaries",()=>
        {
            var (s,buyer,seller)=Fixture();var draft=new TradeContract{Buyer=0,Seller=1,BuyerCity=buyer.Id,SellerCity=seller.Id};
            Assert(Caravans.TravelTurns(s,Caravans.Route(s,draft))==4,"Flat ETA wrong.");
            foreach(var t in s.Tiles.Where(t=>t.Passable&&t.Y==15)){t.Road=true;t.RoadOwner=0;}
            Assert(Caravans.TravelTurns(s,Caravans.Route(s,draft))==2,"Roads did not reduce travel.");Sign(s,buyer,seller);Markets.Tick(s);Engine.CancelTradeContract(s,1);Step(s);Assert(Caravans.Active(s.Caravans.Single()),"Road cargo arrived in one turn.");Step(s);Assert(s.Caravans.Single().Status==CaravanStatus.Delivered,"Road ETA did not match arrival.");
            var route=Enumerable.Range(9,5).Select(x=>At(s,x)).ToArray();foreach(int id in route){s.Tiles[id].Road=false;s.Tiles[id].RoadOwner=-1;s.Tiles[id].Terrain=Terrain.Forest;}
            Assert(Caravans.TravelTurns(s,route)==4,"ETA incorrectly carried unused movement across turns.");
        });
        test("One caravan per contract prevents repeated purchases while cargo is travelling",()=>
        {
            var (s,buyer,seller)=Fixture();Sign(s,buyer,seller);Markets.Tick(s);double gold=s.Player.Gold;
            Step(s);Step(s);Assert(s.Caravans.Count==1&&s.TradeContracts.Single().LastPayment==0&&s.Player.Gold==gold,"A travelling contract charged again.");
            Step(s);Step(s);Assert(s.Caravans.Count==2&&s.Caravans.Count(Caravans.Active)==1&&s.TradeContracts.Single().LastPayment>0,"Delivery did not release the next departure.");
        });
        test("Physical cargo is conserved across warehouses, transit, consumption and overflow",()=>
        {
            var (s,buyer,seller)=Fixture();Sign(s,buyer,seller);double initial=s.Cities.Sum(c=>c.MarketStocks[TradeGood.Salt]),used=0,lost=0;
            for(int i=0;i<10;i++)
            {
                if(i>0)s.Turn++;s.InvalidateLogistics();string before=SaveGame.Serialize(s);var plan=Markets.Plan(s);
                Assert(SaveGame.Serialize(s)==before,"Forecast moved cargo or spent gold.");Markets.Tick(s);
                used+=s.Cities.Sum(c=>c.LastMarketLines.Single(l=>l.Good==TradeGood.Salt).Consumed);lost+=s.Cities.Sum(c=>c.LastMarketLines.Single(l=>l.Good==TradeGood.Salt).Lost);
                double stored=s.Cities.Sum(c=>c.MarketStocks[TradeGood.Salt]),transit=s.Caravans.Where(Caravans.Active).Sum(c=>c.Quantity),raided=s.Caravans.Where(c=>c.Status==CaravanStatus.Lost).Sum(c=>c.Quantity);
                Assert(Math.Abs(initial-stored-transit-used-lost-raided)<.00001,"Cargo appeared or vanished outside explicit losses.");
                string once=SaveGame.Serialize(s);Markets.Tick(s);Assert(once==SaveGame.Serialize(s),"Tick replayed movement.");
            }
        });
        test("Transit requires both parties' treaties and avoids embargoed third countries",()=>
        {
            var (s,buyer,seller)=Fixture();s.Tiles[At(s,15)].Owner=2;s.InvalidateLogistics();string before=SaveGame.Serialize(s);
            Assert(!Engine.SignTradeContract(s,buyer.Id,seller.Id,TradeGood.Salt,4).Success&&SaveGame.Serialize(s)==before,"Unapproved transit charged the player.");
            s.Relation(0,2).Trade=true;Assert(!Engine.SignTradeContract(s,buyer.Id,seller.Id,TradeGood.Salt,4).Success,"One-sided transit accepted.");
            s.Relation(1,2).Trade=true;Sign(s,buyer,seller);s.Factions[2].TradePolicy=TradePolicy.Embargo;Markets.Tick(s);Assert(s.Caravans.Count==0,"Crossed third-party embargo.");
        });
        test("Temporary blockades pause movement then resume without buying a second manifest",()=>
        {
            var (s,buyer,seller)=Fixture();Sign(s,buyer,seller);Markets.Tick(s);double gold=s.Player.Gold;Step(s);int tile=s.Caravans.Single().Tile;
            s.Player.TradePolicy=TradePolicy.Embargo;Step(s);Assert(s.Caravans.Single().Status==CaravanStatus.Waiting&&s.Caravans.Single().Tile==tile,"Embargo failed to stop cargo.");
            s.Player.TradePolicy=TradePolicy.DomesticFirst;Step(s);Assert(s.Caravans.Single().Status==CaravanStatus.Travelling&&s.Caravans.Single().BlockedTurns==0&&s.Player.Gold==gold,"Resumption paid again or failed to clear delay.");
        });
        test("Three consecutive blocked phases lose prepaid cargo without refund",()=>
        {
            var (s,buyer,seller)=Fixture();Sign(s,buyer,seller);Markets.Tick(s);double gold=s.Player.Gold;s.Player.TradePolicy=TradePolicy.Embargo;
            Step(s);Step(s);Assert(s.Caravans.Single().Status==CaravanStatus.Waiting,"Cargo expired too soon.");Step(s);
            Assert(s.Caravans.Single().Status==CaravanStatus.Lost&&s.Player.Gold==gold&&buyer.MarketStocks[TradeGood.Salt]==0,"Loss refunded money or delivered cargo.");SaveGame.Deserialize(SaveGame.Serialize(s));
        });
        test("A hostile zone captures an exposed caravan but a nearby military guard protects it",()=>
        {
            foreach(bool guarded in new[]{false,true})
            {
                var (s,buyer,seller)=Fixture();Sign(s,buyer,seller);Markets.Tick(s);Step(s);var cargo=s.Caravans.Single();
                var enemyTile=World.Neighbors(s,cargo.Tile).First(t=>t.Y!=15);enemyTile.Terrain=Terrain.Grassland;Engine.AddUnit(s,2,enemyTile.Id,UnitKind.Spearmen);s.Relation(0,2).War=true;
                if(guarded)Engine.AddUnit(s,0,cargo.Tile,UnitKind.Spearmen);
                Step(s);Assert(s.Caravans.Single().Status==(guarded?CaravanStatus.Travelling:CaravanStatus.Lost),"Escort did not alter the outcome.");
            }
        });
        test("An enemy occupying the caravan tile overrides adjacent escort protection",()=>
        {
            var (s,buyer,seller)=Fixture();Sign(s,buyer,seller);Markets.Tick(s);Step(s);var cargo=s.Caravans.Single();
            Engine.AddUnit(s,2,cargo.Tile,UnitKind.Cavalry);Engine.AddUnit(s,0,At(s,19),UnitKind.Scout);s.Relation(0,2).War=true;Step(s);
            Assert(s.Caravans.Single().Status==CaravanStatus.Lost,"Occupied caravan was not intercepted.");
        });
        test("Civilian settlers cannot intercept cargo or substitute for a military escort",()=>
        {
            var (s,buyer,seller)=Fixture();Sign(s,buyer,seller);Markets.Tick(s);Step(s);int at=s.Caravans.Single().Tile;
            Engine.AddUnit(s,2,at,UnitKind.Settler);s.Relation(0,2).War=true;
            Assert(!Caravans.AtRisk(s,s.TradeContracts.Single(),at),"Enemy settlers raided cargo.");
            Engine.AddUnit(s,0,at,UnitKind.Settler);Assert(!Caravans.Guarded(s,s.TradeContracts.Single(),at),"Own settlers became escorts.");
            Step(s);Assert(s.Caravans.Single().Status==CaravanStatus.Travelling,"Civilian occupation blocked transport.");
        });
        test("Closed paths reroute through safe terrain without teleporting",()=>
        {
            var (s,buyer,seller)=Fixture();Sign(s,buyer,seller);Markets.Tick(s);Step(s);int current=s.Caravans.Single().Tile;
            foreach(var t in s.Tiles.Where(t=>t.Y==14&&t.X>=9&&t.X<=21))t.Terrain=Terrain.Grassland;
            s.Tiles[At(s,16)].Owner=2;s.InvalidateLogistics();var contract=s.TradeContracts.Single();var route=Caravans.Route(s,contract,current);
            Assert(route.Count>0&&!route.Contains(At(s,16)),"Did not reroute around closed territory.");Step(s);Assert(s.Caravans.Single().Tile!=buyer.Tile&&s.Caravans.Single().Status==CaravanStatus.Travelling,"Rerouting teleported or stopped a safe convoy.");
        });
        test("Contract expiry leaves prepaid cargo travelling until arrival",()=>
        {
            var (s,buyer,seller)=Fixture();Sign(s,buyer,seller);s.Turn=12;Markets.Tick(s);double gold=s.Player.Gold;Step(s);
            Assert(s.TradeContracts.Single().Status==ContractStatus.Expired&&Caravans.Active(s.Caravans.Single()),"Expiry deleted cargo.");Step(s);Step(s);Step(s);
            Assert(s.Caravans.Single().Status==CaravanStatus.Delivered&&s.Player.Gold==gold&&s.Caravans.Count==1,"Expired contract failed to finish or spawned another cargo.");
        });
        test("A captured endpoint terminates cargo while preserving its historical payment",()=>
        {
            var (s,buyer,seller)=Fixture();Sign(s,buyer,seller);Markets.Tick(s);double gold=s.Player.Gold;ProvincialPolitics.Transfer(s,buyer,2);Step(s);
            Assert(s.Caravans.Single().Status==CaravanStatus.Lost&&s.TradeContracts.Single().Status==ContractStatus.Cancelled&&s.Player.Gold==gold,"Endpoint capture delivered or refunded cargo.");SaveGame.Deserialize(SaveGame.Serialize(s));
        });
        test("Reload during travel preserves positions, manifests and deterministic arrival",()=>
        {
            var (a,buyer,seller)=Fixture();Sign(a,buyer,seller);Markets.Tick(a);Step(a);var b=SaveGame.Deserialize(SaveGame.Serialize(a));
            for(int i=0;i<15;i++){Step(a);Step(b);Assert(SaveGame.Serialize(a)==SaveGame.Serialize(b),"Travel diverged after reload.");SaveGame.Deserialize(SaveGame.Serialize(a));}
        });
        test("V6 migration keeps paid trade history and never invents retroactive caravans",()=>
        {
            var (s,buyer,seller)=Fixture();Sign(s,buyer,seller);Markets.Tick(s);var node=JsonNode.Parse(SaveGame.Serialize(s))!;
            node["Version"]=6;node.AsObject().Remove("Caravans");node.AsObject().Remove("NextCaravanId");string legacy=node.ToJsonString();var loaded=SaveGame.Deserialize(legacy);
            Assert(loaded.Version==9&&loaded.Caravans.Count==0&&loaded.TradeContracts.Single().LastPayment==s.TradeContracts.Single().LastPayment&&loaded.Player.Gold==s.Player.Gold,"Migration duplicated or erased payment.");
            string path=Path.Combine(Environment.CurrentDirectory,"artifacts","migration-v6-test.crown");
            try{File.WriteAllText(path,legacy);SaveGame.Write(loaded,path);Assert(File.ReadAllText(path+".v6.bak")==legacy&&SaveGame.Read(path).Version==9,"V6 backup missing.");}
            finally{File.Delete(path);File.Delete(path+".v6.bak");}
        });
        test("Save validation rejects duplicate, unbacked and impossible caravan manifests",()=>
        {
            var (s,buyer,seller)=Fixture();Sign(s,buyer,seller);Markets.Tick(s);string valid=SaveGame.Serialize(s);
            s.Caravans[0].Quantity=7;Reject(s);s=SaveGame.Deserialize(valid);s.Caravans[0].Payment++;Reject(s);
            s=SaveGame.Deserialize(valid);s.Caravans[0].Tile=-1;Reject(s);s=SaveGame.Deserialize(valid);s.Caravans[0].ContractId=999;Reject(s);
            s=SaveGame.Deserialize(valid);s.Caravans[0].LastMovedTurn=s.Turn+1;Reject(s);s=SaveGame.Deserialize(valid);s.Caravans[0].Status=CaravanStatus.Delivered;Reject(s);
            s=SaveGame.Deserialize(valid);s.Caravans.Add(new(){Id=s.NextCaravanId++,ContractId=1,Tile=seller.Tile,DepartedTurn=1,LastMovedTurn=1,Quantity=4,Price=s.Caravans[0].Price,Payment=s.Caravans[0].Payment});Reject(s);
        });
    }
}
