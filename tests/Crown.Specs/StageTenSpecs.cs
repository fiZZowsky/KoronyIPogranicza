using Crown.Core;
using System.Text.Json.Nodes;

internal static class StageTenSpecs
{
    private static void Assert(bool value,string message){if(!value)throw new Exception(message);}
    private static int At(GameState s,int x)=>15*s.Width+x;
    private static (GameState S,TradeContract Contract,Caravan Cargo) Fixture()
    {
        var s=World.Create();Narrative.Resolve(s,0);s.Units.Clear();
        foreach(var t in s.Tiles){t.Terrain=Terrain.Mountain;t.Resource=Resource.None;t.Owner=-1;t.Road=false;t.RoadOwner=-1;t.Ruin=false;t.Explored=true;t.Improvement=Improvement.None;}
        var seller=s.Cities.First(c=>c.Owner==0);var buyer=s.Cities.First(c=>c.Owner==1);buyer.Tile=At(s,21);
        foreach(var c in s.Cities){s.Tiles[c.Tile].Terrain=Terrain.Grassland;s.Tiles[c.Tile].Owner=c.Owner;}
        for(int x=9;x<=21;x++)s.Tiles[At(s,x)].Terrain=Terrain.Grassland;
        foreach(var f in s.Factions){f.Gold=2000;f.Technologies.UnionWith(["pottery","trade","currency"]);}
        seller.Buildings.Add("market");buyer.Buildings.Add("market");seller.MarketStocks[TradeGood.Salt]=50;
        s.Relation(0,1).Trade=true;s.Relation(0,2).Trust=30;s.Relation(0,3).Trust=40;s.InvalidateLogistics();
        Assert(Engine.SignTradeContract(s,buyer.Id,seller.Id,TradeGood.Salt,4).Success,"Transit fixture contract failed.");
        Markets.Tick(s);Engine.CancelTradeContract(s,1);s.Tiles[At(s,14)].Owner=2;s.InvalidateLogistics();
        return(s,s.TradeContracts.Single(),s.Caravans.Single());
    }
    private static void Request(GameState s,int host=2,DemandKind kind=DemandKind.Transit)=>Assert(Engine.SendDemand(s,host,kind).Success,"Transit embassy failed.");
    private static void Reply(GameState s){s.Turn+=2;DiplomaticCouncil.Tick(s);s.InvalidateLogistics();}
    private static void Step(GameState s){s.Turn++;foreach(var u in s.Units)u.Actions=u.MaxActions;s.InvalidateLogistics();Markets.Tick(s);}
    private static void Reject(GameState s)
    {bool rejected=false;try{SaveGame.Deserialize(SaveGame.Serialize(s));}catch(InvalidDataException){rejected=true;}Assert(rejected,"Corrupt transit embassy accepted.");}
    public static void Run(Action<string,Action> test)
    {
        test("Transit embassy takes two turns and grants exactly twelve market phases",()=>
        {
            var (s,t,_)=Fixture();double gold=s.Player.Gold;Request(s);Assert(s.Player.Gold==gold-20&&TransitCouncil.Until(s,2)==0,"Embassy granted access early or charged passage.");
            s.Turn++;DiplomaticCouncil.Tick(s);Assert(s.Demands[0].Status==DemandStatus.Pending&&Caravans.Route(s,t).Count==0,"Reply arrived early.");
            s.Turn++;DiplomaticCouncil.Tick(s);Assert(s.Demands[0].Status==DemandStatus.Accepted&&TransitCouncil.Until(s,2)==15&&s.Relation(0,2).Trust==35,"Reply or expiry incorrect.");
            Assert(Caravans.Route(s,t).Count>0&&!s.Relation(0,2).Trade&&!s.Relation(1,2).Trade,"Safe conduct failed without two host treaties.");
            s.Turn=14;Assert(Caravans.Route(s,t).Count>0,"Permit expired early.");s.Turn=15;Assert(Caravans.Route(s,t).Count==0,"Permit survived its exclusive deadline.");
        });
        test("Paid transit changes acceptance and transfers its price exactly once on reply",()=>
        {
            var (s,_,_)=Fixture();s.Relation(0,2).Trust=-10;double home=s.Player.Gold,host=s.Factions[2].Gold;
            Assert(DiplomaticCouncil.AcceptanceScore(s,2,DemandKind.PaidTransit)-DiplomaticCouncil.AcceptanceScore(s,2,DemandKind.Transit)==35,"Paid forecast bonus differs.");
            Request(s,2,DemandKind.PaidTransit);Assert(s.Factions[2].Gold==host&&s.Player.Gold==home-20,"Passage price charged before consent.");Reply(s);
            Assert(s.Demands[0].Status==DemandStatus.Accepted&&s.Player.Gold==home-80&&s.Factions[2].Gold==host+60,"Accepted fee not conserved.");
            string once=SaveGame.Serialize(s);DiplomaticCouncil.Tick(s);Assert(SaveGame.Serialize(s)==once,"Repeated reply charged twice.");
        });
        test("Rejected transit charges only the embassy and leaves the border closed",()=>
        {
            var (s,t,_)=Fixture();s.Relation(0,2).Trust=-60;double home=s.Player.Gold,host=s.Factions[2].Gold;Request(s,2,DemandKind.PaidTransit);Reply(s);
            Assert(s.Demands[0].Status==DemandStatus.Rejected&&s.Player.Gold==home-20&&s.Factions[2].Gold==host&&Caravans.Route(s,t).Count==0&&s.Relation(0,2).Trust==-65,"Refusal granted passage or charged the offer.");
        });
        test("Spending the offered passage money before reply causes a recorded refusal",()=>
        {
            var (s,_,_)=Fixture();Request(s,2,DemandKind.PaidTransit);s.Player.Gold=59;double host=s.Factions[2].Gold;Reply(s);
            Assert(s.Demands[0].Status==DemandStatus.Rejected&&s.Demands[0].Reply.Contains("60 złota")&&s.Player.Gold==59&&s.Factions[2].Gold==host,"Unfunded offer created debt or access.");SaveGame.Deserialize(SaveGame.Serialize(s));
        });
        test("An embargo introduced while envoys travel prevents acceptance without taking passage money",()=>
        {
            var (s,_,_)=Fixture();Request(s,2,DemandKind.PaidTransit);double gold=s.Player.Gold;s.Factions[2].TradePolicy=TradePolicy.Embargo;Reply(s);
            Assert(s.Demands[0].Status==DemandStatus.Rejected&&s.Player.Gold==gold&&s.Demands[0].Reply.Contains("embargo"),"Embassy ignored changed policy.");
        });
        test("War cancels a travelling paid embassy before passage can be charged",()=>
        {
            var (s,_,_)=Fixture();Request(s,2,DemandKind.PaidTransit);double gold=s.Player.Gold;s.Turn++;
            Assert(Engine.DeclareWar(s,2,WarReason.Conquest).Success&&s.Demands[0].Status==DemandStatus.Cancelled,"War did not cancel the transit embassy.");Reply(s);
            Assert(TransitCouncil.Until(s,2)==0&&s.Player.Gold==gold,"Cancelled passage was charged or granted.");SaveGame.Deserialize(SaveGame.Serialize(s));
        });
        test("Transit validation rejects duplicate rights, poor offers, cooldowns and embargo atomically",()=>
        {
            var (s,_,_)=Fixture();s.Player.Gold=79;string before=SaveGame.Serialize(s);Assert(!Engine.SendDemand(s,2,DemandKind.PaidTransit).Success&&SaveGame.Serialize(s)==before,"Unaffordable proposal mutated state.");
            s.Player.Gold=2000;s.Player.TradePolicy=TradePolicy.Embargo;before=SaveGame.Serialize(s);Assert(!Engine.SendDemand(s,2,DemandKind.Transit).Success&&SaveGame.Serialize(s)==before,"Embargo request mutated state.");
            s.Player.TradePolicy=TradePolicy.DomesticFirst;Request(s);s.Turn++;before=SaveGame.Serialize(s);Assert(!Engine.SendDemand(s,2,DemandKind.NonAggression).Success&&SaveGame.Serialize(s)==before,"Transit bypassed common embassy cooldown.");
            s.Turn++;DiplomaticCouncil.Tick(s);s.Turn=12;before=SaveGame.Serialize(s);Assert(!Engine.SendDemand(s,2,DemandKind.Transit).Success&&SaveGame.Serialize(s)==before,"Existing permit renewed early.");
            s.Turn=15;Request(s);Assert(s.Demands.Count==2,"Expired safe conduct could not be renegotiated.");
        });
        test("A closed corridor reports every missing host and one permission cannot open two borders",()=>
        {
            var (s,t,c)=Fixture();s.Tiles[At(s,17)].Owner=3;string before=SaveGame.Serialize(s);
            Assert(TransitCouncil.ClosedBorders(s,t,c.Tile).SequenceEqual(new[]{2,3})&&SaveGame.Serialize(s)==before,"Report invented a host, missed a gate or mutated state.");
            Request(s);Reply(s);Assert(Caravans.Route(s,t).Count==0&&TransitCouncil.ClosedBorders(s,t,c.Tile).SequenceEqual(new[]{3}),"One host granted another country's rights.");
            Request(s,3);Reply(s);Assert(Caravans.Route(s,t).Count>0&&TransitCouncil.ClosedBorders(s,t,c.Tile).Count==0,"Both permissions failed to open the corridor.");
        });
        test("Available neutral detours avoid spurious border disputes",()=>
        {
            var (s,t,c)=Fixture();foreach(var tile in s.Tiles.Where(t=>t.Y==14&&t.X>=9&&t.X<=21))tile.Terrain=Terrain.Grassland;
            Assert(Caravans.Route(s,t).Count>0&&TransitCouncil.ClosedBorders(s,t,c.Tile).Count==0,"A usable neutral route was reported as blocked.");
        });
        test("Host treaties with both parties still allow transit after a temporary permit expires",()=>
        {
            var (s,t,_)=Fixture();Request(s);Reply(s);s.Turn=TransitCouncil.Until(s,2);s.Relation(0,2).Trade=s.Relation(1,2).Trade=true;
            Assert(Caravans.Route(s,t).Count>0&&TransitCouncil.ClosedBorders(s,t).Count==0,"Expiry removed independent treaty rights.");
        });
        test("Safe conduct covers exports and imports but cannot subsidize unrelated AI cargo",()=>
        {
            var (s,t,_)=Fixture();Request(s);Reply(s);Assert(Caravans.Route(s,t).Count>0,"Export permit failed.");
            (t.Buyer,t.Seller)=(t.Seller,t.Buyer);(t.BuyerCity,t.SellerCity)=(t.SellerCity,t.BuyerCity);Assert(Caravans.Route(s,t).Count>0,"Import permit failed.");
            s.Tiles[At(s,14)].Owner=3;Request(s,3);Reply(s);
            var city=s.Cities.First(c=>c.Id==t.BuyerCity);city.Owner=2;s.Tiles[city.Tile].Owner=2;t.Buyer=2;s.Relation(1,2).Trade=true;
            Assert(Caravans.Route(s,t).Count==0&&TransitCouncil.ClosedBorders(s,t).Count==0,"Player permission opened an unrelated AI shipment.");
        });
        test("A permit never overrides host embargo or host war against either trading party",()=>
        {
            var (s,t,_)=Fixture();Request(s);Reply(s);s.Factions[2].TradePolicy=TradePolicy.Embargo;
            Assert(Caravans.Route(s,t).Count==0&&TransitCouncil.ClosedBorders(s,t).Count==0,"Permit bypassed embargo or offered an impossible fix.");
            s.Factions[2].TradePolicy=TradePolicy.DomesticFirst;s.Relation(1,2).War=true;Assert(Caravans.Route(s,t).Count==0,"Permit overrode war with the partner.");
            s.Relation(1,2).War=false;s.Relation(0,2).War=true;Assert(Caravans.Route(s,t).Count==0,"Permit overrode war with the player.");
            s.Relation(0,2).War=false;Assert(Caravans.Route(s,t).Count>0,"Suspension consumed the remaining consent.");
        });
        test("Trade between buyer and seller remains mandatory even with every host's consent",()=>
        {
            var (s,t,c)=Fixture();Request(s);Reply(s);s.Relation(0,1).Trade=false;
            Assert(Caravans.Suspension(s,t)!=null&&Markets.RouteError(s,t)!=null&&TransitCouncil.ClosedBorders(s,t,c.Tile).Count==0,"Transit replaced the buyer-seller treaty.");
            var p=Caravans.Project(s).Caravans.Single();Assert(p.Status==CaravanStatus.Waiting&&p.Tile==c.Tile,"Cargo ignored suspension in forecast.");
        });
        test("Transit replies reopen stopped prepaid cargo before its third waiting phase",()=>
        {
            var (s,t,c)=Fixture();s.Turn=2;Markets.Tick(s);c=s.Caravans.Single();Assert(c.BlockedTurns==1&&c.Message.Contains(s.Factions[2].Name),"First closure did not identify the host.");
            Request(s);Step(s);Assert(s.Caravans[0].BlockedTurns==2,"Envoys stopped the waiting clock.");
            s.Turn++;DiplomaticCouncil.Tick(s);var before=SaveGame.Serialize(s);var plan=Markets.Plan(s);
            Assert(plan.Caravans[0].Tile!=c.Tile&&SaveGame.Serialize(s)==before,"Forecast lost the permit or changed live state.");Markets.Tick(s);
            Assert(Caravans.Active(s.Caravans[0])&&s.Caravans[0].BlockedTurns==0&&s.Caravans[0].Tile==plan.Caravans[0].Tile,"Accepted passage failed to resume the actual transport.");
            Assert(s.TradeContracts[0].Status==ContractStatus.Cancelled&&s.Caravans.Count==1,"Consent resurrected the cancelled contract or purchased cargo twice.");
        });
        test("A late transit reply cannot resurrect a caravan already abandoned",()=>
        {
            var (s,_,_)=Fixture();Step(s);Step(s);Assert(s.Caravans[0].BlockedTurns==2,"Late fixture did not wait twice.");Request(s);Step(s);Assert(s.Caravans[0].Status==CaravanStatus.Lost,"Cargo survived its third stop.");
            s.Turn++;DiplomaticCouncil.Tick(s);Markets.Tick(s);Assert(s.Demands[0].Status==DemandStatus.Accepted&&s.Caravans[0].Status==CaravanStatus.Lost,"Safe conduct resurrected a lost manifest.");
        });
        test("Expired permissions strand cargo inside the host until normal three-phase loss",()=>
        {
            var (s,_,c)=Fixture();Request(s);Reply(s);c.Tile=At(s,14);c.LastMovedTurn=s.Turn;s.LastMarketTurn=s.Turn;s.Turn=TransitCouncil.Until(s,2)-1;
            for(int i=0;i<3;i++)Step(s);Assert(s.Caravans[0].Status==CaravanStatus.Lost&&s.Caravans[0].Tile==At(s,14),"Expired permit allowed escape from a closed starting tile or skipped the waiting rule.");
        });
        test("Civilian passage ends escorts at the foreign gate without moving the army across",()=>
        {
            var (s,t,c)=Fixture();Request(s);Reply(s);c.Tile=At(s,13);c.LastMovedTurn=s.Turn-1;
            var u=Engine.AddUnit(s,0,c.Tile,UnitKind.Spearmen);Assert(Engine.AssignEscort(s,u.Id,c.Id).Success,"Gate escort fixture failed.");
            string before=SaveGame.Serialize(s);var plan=Caravans.Project(s);Assert(SaveGame.Serialize(s)==before&&plan.Caravans[0].Tile==At(s,16)&&plan.EscortMoves[0].ReleaseReason!=null,"Pure gate forecast failed.");
            Markets.Tick(s);Assert(u.Tile==At(s,13)&&u.EscortCaravanId==null&&s.Caravans[0].Tile==At(s,16)&&!Navigation.CanEnter(s,u,s.Tiles[At(s,14)]),"Permit granted military access.");
        });
        test("Transit permission never bypasses enemy occupation or grants phantom protection",()=>
        {
            var (s,t,c)=Fixture();Request(s);Reply(s);s.Relation(0,3).War=true;Engine.AddUnit(s,3,At(s,15),UnitKind.Spearmen);
            Assert(Caravans.Route(s,t,c.Tile).Count==0&&TransitCouncil.ClosedBorders(s,t,c.Tile).Count==0,"Permission bypassed an enemy or misdiagnosed combat as a closed gate.");
        });
        test("Pending offers and accepted rights continue identically through save and reload",()=>
        {
            var (a,_,_)=Fixture();Request(a,2,DemandKind.PaidTransit);var b=SaveGame.Deserialize(SaveGame.Serialize(a));Reply(a);Reply(b);
            Assert(SaveGame.Serialize(a)==SaveGame.Serialize(b),"Pending offer diverged after reload.");b=SaveGame.Deserialize(SaveGame.Serialize(a));
            for(int i=0;i<15;i++){Step(a);Step(b);Assert(SaveGame.Serialize(a)==SaveGame.Serialize(b),"Permit continuation diverged.");SaveGame.Deserialize(SaveGame.Serialize(a));}
        });
        test("V9 migration preserves escorts and promises without inventing transit consent",()=>
        {
            var (s,_,c)=Fixture();var u=Engine.AddUnit(s,0,c.Tile,UnitKind.Scout);Engine.AssignEscort(s,u.Id,c.Id);Engine.SendDemand(s,1,DemandKind.NonAggression);
            var node=JsonNode.Parse(SaveGame.Serialize(s))!;node["Version"]=9;string legacy=node.ToJsonString();var loaded=SaveGame.Deserialize(legacy);
            Assert(loaded.Version==14&&loaded.Units.Single().EscortCaravanId==c.Id&&loaded.Demands[0].Kind==DemandKind.NonAggression&&TransitCouncil.Until(loaded,2)==0,"Migration lost orders or invented a permission.");
            var (promises,_,_)=StageEightSpecs.Loss();TradeCouncil.Present(promises,1);Narrative.Resolve(promises,1);node=JsonNode.Parse(SaveGame.Serialize(promises))!;node["Version"]=9;
            var preserved=SaveGame.Deserialize(node.ToJsonString());Assert(preserved.TradeCases[0].Status==TradeCaseStatus.Promised&&preserved.TradeCases[0].DeadlineTurn==promises.TradeCases[0].DeadlineTurn,"V9 migration lost an active promise.");
            string path=Path.Combine(Environment.CurrentDirectory,"artifacts","migration-v9-test.crown");
            try{File.WriteAllText(path,legacy);SaveGame.Write(loaded,path);Assert(File.ReadAllText(path+".v9.bak")==legacy&&SaveGame.Read(path).Version==14,"V9 backup missing.");}
            finally{File.Delete(path);File.Delete(path+".v9.bak");}
        });
        test("Forged transit replies, future grants and unknown embassy variants are rejected",()=>
        {
            var (s,_,_)=Fixture();Request(s);string pending=SaveGame.Serialize(s);s.Demands[0].Status=DemandStatus.Accepted;Reject(s);
            s=SaveGame.Deserialize(pending);s.Demands[0].Kind=(DemandKind)99;Reject(s);s=SaveGame.Deserialize(pending);s.Demands[0].From=1;Reject(s);
            s=SaveGame.Deserialize(pending);Reply(s);s.Demands[0].ResolvedTurn=s.Turn+1;Reject(s);
            s=SaveGame.Deserialize(pending);s.Version=9;Reject(s);
        });
        test("Expiry notices are pure, warn two turns ahead and reach the turn chronicle",()=>
        {
            var (s,t,_)=Fixture();Request(s);Reply(s);s.Turn=13;string before=SaveGame.Serialize(s);
            Assert(TransitCouncil.Notices(s).Single().Contains("za dwie tury")&&SaveGame.Serialize(s)==before,"Warning changed state or missed its deadline.");
            s.Turn=14;Assert(TransitCouncil.Notices(s).Count==0,"Warning repeats every turn.");Assert(Engine.EndTurn(s).Success,"Expiry turn failed.");
            Assert(s.Turn==15&&Caravans.Route(s,t).Count==0&&s.Chronicle.Count(e=>e.Category=="tranzyt"&&e.Text.Contains("wygasła"))==1,"Expiry was not announced before the closed market phase.");
        });
    }
}
