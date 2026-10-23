using Crown.Core;
using System.Text.Json.Nodes;

internal static class StageSixSpecs
{
    private static void Assert(bool value,string message){if(!value)throw new Exception(message);}
    private static int At(GameState s,int x)=>15*s.Width+x;
    private static (GameState S,City A,City B,City Foreign) Fixture()
    {
        var s=World.CreateLegacy();Narrative.Resolve(s,0);
        foreach(var tile in s.Tiles){tile.Terrain=Terrain.Mountain;tile.Resource=Resource.None;tile.Owner=-1;tile.Improvement=Improvement.None;tile.Road=false;tile.RoadOwner=-1;tile.River=tile.Ruin=false;tile.Explored=true;}
        foreach(var city in s.Cities){s.Tiles[city.Tile].Terrain=Terrain.Grassland;s.Tiles[city.Tile].Owner=city.Owner;}
        foreach(var u in s.Units)s.Tiles[u.Tile].Terrain=Terrain.Grassland;
        for(int x=9;x<=18;x++)s.Tiles[At(s,x)].Terrain=Terrain.Grassland;
        foreach(var from in s.Cities)foreach(var to in s.Cities)
        {
            var cursor=s.Tiles[from.Tile];
            while(cursor.Id!=to.Tile){cursor=World.Neighbors(s,cursor.Id).OrderBy(t=>World.Distance(t,s.Tiles[to.Tile])).ThenBy(t=>t.Id).First();cursor.Terrain=Terrain.Grassland;}
        }
        var a=s.Cities.First(c=>c.Owner==0);var b=Engine.AddCity(s,0,At(s,15),"Kamieniec");var foreign=s.Cities.First(c=>c.Owner==1);
        foreach(var city in new[]{a,b,foreign})city.Buildings.Add("market");
        foreach(var f in s.Factions){f.Gold=2000;f.Technologies.UnionWith(["pottery","trade","currency"]);}
        s.Relation(0,1).Trade=true;foreign.MarketStocks[TradeGood.Salt]=20;s.InvalidateLogistics();return(s,a,b,foreign);
    }
    private static void Connect(GameState s){for(int x=10;x<15;x++)Assert(Engine.BuildRoad(s,At(s,x)).Success,"Road fixture failed.");}
    private static CommodityBalance Line(MarketPlan plan,City city,TradeGood good)=>plan.Cities[city.Id].First(l=>l.Good==good);
    private static void Reject(GameState s){bool rejected=false;try{SaveGame.Deserialize(SaveGame.Serialize(s));}catch(InvalidDataException){rejected=true;}Assert(rejected,"Corrupt market state accepted.");}
    public static void Run(Action<string,Action> test)
    {
        test("Extraction assigns a worked resource once even when city radii overlap",()=>
        {
            var (s,a,_,_)=Fixture();var closer=Engine.AddCity(s,0,At(s,12),"Bliskie Miasto");var tile=s.Tiles[At(s,11)];tile.Owner=0;tile.Resource=Resource.Iron;tile.Improvement=Improvement.Mine;
            var production=Markets.Production(s);Assert(production.Values.Sum(v=>v[TradeGood.Iron])==4&&production[closer.Id][TradeGood.Iron]==4&&production[a.Id][TradeGood.Iron]==0,"Overlapping cities duplicated extraction.");
            tile.Owner=-1;Assert(Markets.Production(s).Values.Sum(v=>v[TradeGood.Iron])==0,"Extracted neutral ore.");
        });
        test("Market forecasts are pure and conserve every commodity globally",()=>
        {
            var (s,a,_,_)=Fixture();a.MarketStocks[TradeGood.Iron]=15;Connect(s);string before=SaveGame.Serialize(s);var plan=Markets.Plan(s);
            Assert(SaveGame.Serialize(s)==before,"Forecast changed game state.");
            foreach(var good in Markets.Goods)
            {
                var rows=plan.Cities.Values.SelectMany(x=>x).Where(l=>l.Good==good).ToArray();
                Assert(Math.Abs(rows.Sum(l=>l.Opening+l.Produced-l.Consumed-l.Lost-l.Closing))<.00001,"Goods created or lost outside the ledger.");
                Assert(Math.Abs(rows.Sum(l=>l.DomesticIn-l.DomesticOut))<.00001&&Math.Abs(rows.Sum(l=>l.ForeignIn-l.ForeignOut))<.00001,"Shipments not conserved.");
            }
        });
        test("Regional prices respond to stocks and demand within explicit limits",()=>
        {
            var (s,a,b,_)=Fixture();a.MarketStocks[TradeGood.Salt]=10;double supplied=Line(Markets.Plan(s),a,TradeGood.Salt).Price;
            a.Population=10;double demand=Line(Markets.Plan(s),a,TradeGood.Salt).Price;Assert(demand>supplied,"Population did not affect prices.");
            a.MarketStocks[TradeGood.Salt]=0;double scarce=Line(Markets.Plan(s),a,TradeGood.Salt).Price;Assert(scarce>=demand&&scarce<=15,"Scarcity price escaped its cap.");
            Connect(s);var plan=Markets.Plan(s);Assert(Line(plan,a,TradeGood.Salt).Price==Line(plan,b,TradeGood.Salt).Price,"One regional market has inconsistent prices.");
        });
        test("Road markets deliver real domestic surplus without minting foreign payments",()=>
        {
            var (s,a,b,_)=Fixture();a.MarketStocks[TradeGood.Salt]=20;b.Population=8;
            Assert(Line(Markets.Plan(s),b,TradeGood.Salt).Consumed==0,"Goods crossed a missing road.");Connect(s);var plan=Markets.Plan(s);
            var recipient=Line(plan,b,TradeGood.Salt);Assert(recipient.DomesticIn>0&&recipient.Coverage==1&&plan.Shipments.Any(p=>p.From==a.Id&&p.To==b.Id),"Connected demand not supplied.");
            Assert(plan.Cash.Values.All(v=>v==0),"Domestic sharing created treasury transfers.");
        });
        test("A military blockade separates goods markets and cuts regional consumption",()=>
        {
            var (s,a,b,_)=Fixture();a.MarketStocks[TradeGood.Salt]=20;Connect(s);Assert(Line(Markets.Plan(s),b,TradeGood.Salt).Consumed>0,"Fixture not connected.");
            s.Relation(0,1).War=true;Engine.AddUnit(s,1,At(s,12),UnitKind.Spearmen);
            Assert(Line(Markets.Plan(s),b,TradeGood.Salt).Consumed==0,"Goods crossed a blockade.");
        });
        test("Market processing consumes stocks once and stores a valid detailed ledger",()=>
        {
            var (s,a,_,_)=Fixture();a.MarketStocks[TradeGood.Salt]=20;Markets.Tick(s);
            Assert(a.MarketStocks[TradeGood.Salt]<20&&a.LastMarketTax>0&&a.LastMarketLines.Count==3,"Consumption or tax missing.");
            string once=SaveGame.Serialize(s);Markets.Tick(s);Assert(SaveGame.Serialize(s)==once,"Market processed twice in one turn.");SaveGame.Deserialize(once);
        });
        test("Warehouse capacity preserves otherwise lost surplus",()=>
        {
            var (s,a,_,_)=Fixture();a.MarketStocks[TradeGood.Salt]=Markets.Capacity(a);var tile=s.Tiles[At(s,10)];tile.Owner=0;tile.Resource=Resource.Salt;tile.Improvement=Improvement.TradingPost;
            var small=Line(Markets.Plan(s),a,TradeGood.Salt);Assert(small.Lost>0&&small.Closing==75,"Overflow not bounded by storage.");
            a.Buildings.Add("warehouse");var large=Line(Markets.Plan(s),a,TradeGood.Salt);Assert(large.Lost==0&&large.Closing>small.Closing&&Markets.Capacity(a)==175,"Warehouse did not preserve goods.");
        });
        test("Foreign contracts transfer matching goods and gold without double taxation",()=>
        {
            var (s,a,_,seller)=Fixture();Assert(Engine.SignTradeContract(s,a.Id,seller.Id,TradeGood.Salt,2).Success,"Contract rejected.");
            double buyerGold=s.Player.Gold,sellerGold=s.Factions[1].Gold,total=s.Factions.Sum(f=>f.Gold);var plan=Markets.Plan(s);var trade=plan.Trades.Single();
            Assert(trade.Quantity==2&&Math.Abs(plan.Cash.Values.Sum())<.00001,"Unbalanced contract quote.");Markets.Tick(s);
            Assert(Math.Abs(s.Player.Gold-(buyerGold-trade.Payment))<.00001&&Math.Abs(s.Factions[1].Gold-(sellerGold+trade.Payment))<.00001&&Math.Abs(s.Factions.Sum(f=>f.Gold)-total)<.00001,"Gold was minted or charged twice.");
            Assert(Line(plan,a,TradeGood.Salt).ForeignIn==0&&s.Caravans.Single().Quantity==2&&Markets.LastTradeBalance(s,0)== -trade.Payment,"Dispatch was delivered instantly or trade history lost.");SaveGame.Deserialize(SaveGame.Serialize(s));
        });
        test("Domestic reserve protects inhabitants while free exports can cause shortages",()=>
        {
            var (s,a,_,seller)=Fixture();seller.MarketStocks[TradeGood.Salt]=2;Engine.SignTradeContract(s,a.Id,seller.Id,TradeGood.Salt,2);
            var protectedPlan=Markets.Plan(s);Assert(Line(protectedPlan,seller,TradeGood.Salt).Coverage>.999&&protectedPlan.Trades.Single().Quantity<2,"Domestic reserve failed.");
            s.Factions[1].TradePolicy=TradePolicy.OpenExports;var open=Markets.Plan(s);
            Assert(open.Trades.Single().Quantity==2&&Line(open,seller,TradeGood.Salt).Consumed==0,"Free export did not spend the local reserve.");
        });
        test("Embargo and war pause deliveries and restored treaties resume them",()=>
        {
            var (s,a,_,seller)=Fixture();Engine.SignTradeContract(s,a.Id,seller.Id,TradeGood.Salt,2);Engine.SetTradePolicy(s,TradePolicy.Embargo);Markets.Tick(s);
            Assert(s.TradeContracts.Single().LastDelivered==0&&s.TradeContracts.Single().Status==ContractStatus.Active,"Embargo cancelled rather than paused the contract.");
            s.Player.TradePolicy=TradePolicy.DomesticFirst;s.Relation(0,1).War=true;s.Relation(0,1).Trade=false;s.Turn++;s.InvalidateLogistics();Markets.Tick(s);
            Assert(s.TradeContracts.Single().LastDelivered==0,"Traded during war.");s.Relation(0,1).War=false;s.Relation(0,1).Trade=true;s.Turn++;s.InvalidateLogistics();Markets.Tick(s);
            Assert(s.TradeContracts.Single().LastDelivered>0,"Trade did not resume.");
        });
        test("Poor buyers receive affordable partial shipments and never overdraft",()=>
        {
            var (s,a,_,seller)=Fixture();Engine.SignTradeContract(s,a.Id,seller.Id,TradeGood.Salt,6);s.Player.Gold=1.239;Markets.Tick(s);var contract=s.TradeContracts.Single();
            Assert(contract.LastDelivered>0&&contract.LastDelivered<6&&contract.LastPayment<=1.239&&s.Player.Gold>=0,"Partial delivery overdrew the buyer.");
            s.Player.Gold=0;s.Turn++;Markets.Tick(s);Assert(contract.LastDelivered==0&&contract.LastPayment==0,"Free cargo supplied to an insolvent buyer.");
        });
        test("Contracts reject duplicate, unrelated, oversized and unfunded agreements atomically",()=>
        {
            var (s,a,_,seller)=Fixture();string before=SaveGame.Serialize(s);
            Assert(!Engine.SignTradeContract(s,a.Id,seller.Id,TradeGood.Salt,7).Success&&SaveGame.Serialize(s)==before,"Invalid quantity mutated state.");
            s.Player.Gold=14;before=SaveGame.Serialize(s);Assert(!Engine.SignTradeContract(s,a.Id,seller.Id,TradeGood.Salt,2).Success&&SaveGame.Serialize(s)==before,"Unfunded contract mutated state.");
            s.Player.Gold=500;Assert(Engine.SignTradeContract(s,a.Id,seller.Id,TradeGood.Salt,2).Success,"Valid agreement failed.");before=SaveGame.Serialize(s);
            Assert(!Engine.SignTradeContract(s,a.Id,seller.Id,TradeGood.Salt,2).Success&&SaveGame.Serialize(s)==before,"Duplicate charged a fee.");
            Assert(!Engine.SignTradeContract(s,a.Id,a.Id,TradeGood.Iron,1).Success,"Domestic treasury contract allowed.");
        });
        test("Contracts expire, cancel and terminate after city capture without replaying payments",()=>
        {
            var (s,a,_,seller)=Fixture();Engine.SignTradeContract(s,a.Id,seller.Id,TradeGood.Salt,2);s.Turn=13;Markets.Tick(s);
            Assert(s.TradeContracts.Single().Status==ContractStatus.Expired&&s.TradeContracts.Single().LastDelivered==0,"Expired agreement delivered cargo.");SaveGame.Deserialize(SaveGame.Serialize(s));
            (s,a,_,seller)=Fixture();Engine.SignTradeContract(s,a.Id,seller.Id,TradeGood.Salt,2);Markets.Tick(s);double gold=s.Player.Gold;
            Assert(Engine.CancelTradeContract(s,1).Success&&!Engine.CancelTradeContract(s,1).Success&&s.Player.Gold==gold,"Cancellation replayed or refunded a payment.");
            (s,a,_,seller)=Fixture();Engine.SignTradeContract(s,a.Id,seller.Id,TradeGood.Salt,2);ProvincialPolitics.Transfer(s,seller,0);Markets.Tick(s);
            Assert(s.TradeContracts.Single().Status==ContractStatus.Cancelled&&s.TradeContracts.Single().LastPayment==0,"Captured port retained a foreign agreement.");
        });
        test("Market policy has real costs and a cooldown and the council records its tradeoff",()=>
        {
            var (s,_,_,_)=Fixture();double gold=s.Player.Gold;Assert(Engine.SetTradePolicy(s,TradePolicy.OpenExports).Success&&s.Player.Gold==gold-30,"Policy charge missing.");
            string before=SaveGame.Serialize(s);Assert(!Engine.SetTradePolicy(s,TradePolicy.Embargo).Success&&SaveGame.Serialize(s)==before,"Policy cooldown mutated state.");
            s.Turn=10;Narrative.Schedule(s);Assert(s.PendingEvent=="market_charter","No market council.");
            Assert(!Engine.SetTradePolicy(s,TradePolicy.Embargo).Success&&Narrative.Resolve(s,2).Success&&s.Player.TradePolicy==TradePolicy.Embargo,"Council did not determine policy.");
        });
        test("Commodity consumption affects production, prosperity and the treasury",()=>
        {
            var (s,a,_,_)=Fixture();Markets.Tick(s);double scarce=Engine.CityYields(s,a).Production;int target=Markets.ProsperityEffect(a);
            s.Turn++;foreach(var good in Markets.Goods)a.MarketStocks[good]=20;Markets.Tick(s);
            Assert(Engine.CityYields(s,a).Production>scarce&&Markets.ProsperityEffect(a)>target&&a.LastMarketTax>0,"Consumption has no economic effect.");
            var report=Engine.Economy(s,0);Assert(report.MarketIncome==s.Cities.Where(c=>c.Owner==0).Sum(c=>c.LastMarketTax)&&Math.Abs(report.Net-Engine.Income(s,0).Gold)<.00001,"Treasury report does not reconcile.");
            double before=s.Player.Gold,expected=Engine.Income(s,0).Gold;Assert(Engine.EndTurn(s).Success&&Math.Abs(s.Player.Gold-before-expected)<.00001,"Market taxes were omitted or collected twice.");
        });
        test("Contract limits require player consent and release slots after cancellation",()=>
        {
            var (s,a,_,seller)=Fixture();string before=SaveGame.Serialize(s);
            Assert(!Engine.SignTradeContract(s,seller.Id,a.Id,TradeGood.Salt,1,1).Success&&SaveGame.Serialize(s)==before,"AI committed the player's goods without consent.");
            foreach(var good in Markets.Goods)Assert(Engine.SignTradeContract(s,a.Id,seller.Id,good,1).Success,"Could not fill three valid slots.");
            before=SaveGame.Serialize(s);Assert(!Engine.SignTradeContract(s,seller.Id,a.Id,TradeGood.Salt,1).Success&&SaveGame.Serialize(s)==before,"Fourth contract bypassed a state's limit.");
            Engine.CancelTradeContract(s,1);Assert(Engine.SignTradeContract(s,seller.Id,a.Id,TradeGood.Salt,1).Success,"Cancellation did not release a slot.");
        });
        test("AI imports from another AI market to address a real commodity shortage",()=>
        {
            var (s,_,_,buyer)=Fixture();var supplier=s.Cities.First(c=>c.Owner==2);supplier.Buildings.Add("market");supplier.MarketStocks[TradeGood.Iron]=20;s.Relation(1,2).Trade=true;s.Turn=10;
            Markets.Tick(s);Assert(Markets.Coverage(buyer,TradeGood.Iron)==0,"Fixture lacks a shortage.");Markets.ManageAi(s,1);
            Assert(s.TradeContracts.Any(c=>c.Buyer==1&&c.Seller==2&&c.Good==TradeGood.Iron)&&s.TradeContracts.All(c=>c.Buyer!=0&&c.Seller!=0),"AI did not address its shortage or committed player trade.");
            SaveGame.Deserialize(SaveGame.Serialize(s));
        });
        test("V5 migration preserves war and peace history and starts with empty warehouses",()=>
        {
            var (s,_,_,_)=Fixture();Engine.DeclareWar(s,1,WarReason.Conquest);s.Turn=2;Engine.ProposePeace(s,1,100);
            var node=JsonNode.Parse(SaveGame.Serialize(s))!;node["Version"]=5;foreach(string key in new[]{"TradeContracts","NextContractId","LastMarketTurn"})node.AsObject().Remove(key);
            foreach(var city in node["Cities"]!.AsArray())foreach(string key in new[]{"MarketStocks","LastMarketLines","LastMarketTurn","LastMarketTax"})city!.AsObject().Remove(key);
            string legacy=node.ToJsonString();var loaded=SaveGame.Deserialize(legacy);
            Assert(loaded.Version == 19&&loaded.Wars.Count==1&&loaded.PeaceOffers.Count==1&&loaded.Cities.All(c=>c.MarketStocks.Values.All(v=>v==0)&&c.LastMarketTurn==0),"Migration lost diplomacy or invented stock.");
            string path=Path.Combine(Environment.CurrentDirectory,"artifacts","migration-v5-test.crown");
            try{File.WriteAllText(path,legacy);SaveGame.Write(loaded,path);Assert(File.ReadAllText(path+".v5.bak")==legacy&&SaveGame.Read(path).Version == 19,"V5 backup failed.");}
            finally{File.Delete(path);File.Delete(path+".v5.bak");}
        });
        test("V6 rejects corrupt warehouses, physical ledgers and contract payments",()=>
        {
            var (s,a,_,seller)=Fixture();a.MarketStocks[TradeGood.Salt]=-1;Reject(s);
            (s,a,_,seller)=Fixture();a.MarketStocks.Remove(TradeGood.Iron);Reject(s);
            (s,a,_,seller)=Fixture();Markets.Tick(s);a.LastMarketLines[0]=a.LastMarketLines[0] with{Closing=a.LastMarketLines[0].Closing+1};Reject(s);
            (s,a,_,seller)=Fixture();Engine.SignTradeContract(s,a.Id,seller.Id,TradeGood.Salt,2);s.TradeContracts[0].Quantity=7;Reject(s);
            (s,a,_,seller)=Fixture();Engine.SignTradeContract(s,a.Id,seller.Id,TradeGood.Salt,2);s.TradeContracts[0].LastPayment=100;Reject(s);
            (s,a,_,seller)=Fixture();Engine.SignTradeContract(s,a.Id,seller.Id,TradeGood.Salt,2);s.TradeContracts[0].ExpiresTurn++;Reject(s);
        });
        test("Markets and active contracts continue deterministically after reload",()=>
        {
            var (a,city,_,seller)=Fixture();Engine.SignTradeContract(a,city.Id,seller.Id,TradeGood.Salt,2);var b=SaveGame.Deserialize(SaveGame.Serialize(a));
            for(int i=0;i<18;i++)foreach(var s in new[]{a,b})
            {
                if(s.PendingEvent!=null){var e=Narrative.Get(s.PendingEvent);Narrative.Resolve(s,Array.FindIndex(e.Choices,c=>s.Player.Gold+c.Gold>=0));}
                Assert(Engine.EndTurn(s).Success,"Market continuation failed.");
            }
            Assert(SaveGame.Serialize(a)==SaveGame.Serialize(b),"Markets diverged after reload.");SaveGame.Deserialize(SaveGame.Serialize(a));
        });
    }
}
