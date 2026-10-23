using Crown.Core;
using System.Text.Json.Nodes;

internal static class StageEightSpecs
{
    private static void Assert(bool value,string message){if(!value)throw new Exception(message);}
    private static void Step(GameState s){s.Turn++;s.InvalidateLogistics();Markets.Tick(s);}
    internal static (GameState S,City Buyer,City Seller) Loss()
    {
        var s=World.CreateLegacy();Narrative.Resolve(s,0);s.Units.Clear();
        foreach(var t in s.Tiles){t.Terrain=Terrain.Grassland;t.Resource=Resource.None;t.Owner=-1;t.Road=false;t.RoadOwner=-1;t.Improvement=Improvement.None;t.Explored=true;}
        var buyer=s.Cities.First(c=>c.Owner==0);var seller=s.Cities.First(c=>c.Owner==1);seller.Tile=15*s.Width+21;
        foreach(var c in s.Cities)s.Tiles[c.Tile].Owner=c.Owner;
        buyer.Buildings.Add("market");seller.Buildings.Add("market");seller.MarketStocks[TradeGood.Salt]=60;seller.MarketStocks[TradeGood.Dyes]=40;
        foreach(var f in s.Factions){f.Gold=2000;f.Technologies.UnionWith(["pottery","trade","currency"]);}
        s.Relation(0,1).Trade=true;s.InvalidateLogistics();Assert(Engine.SignTradeContract(s,buyer.Id,seller.Id,TradeGood.Salt,4).Success,"Loss fixture contract failed.");
        Markets.Tick(s);Engine.CancelTradeContract(s,1);s.Player.TradePolicy=TradePolicy.Embargo;Step(s);Step(s);Step(s);s.Player.TradePolicy=TradePolicy.DomesticFirst;
        Assert(s.Caravans.Single().Status==CaravanStatus.Lost&&s.TradeCases.Count==1,"Real loss did not produce a petition.");return(s,buyer,seller);
    }
    private static void Decide(GameState s,int choice)
    {Assert(TradeCouncil.Present(s,s.TradeCases.Last().CaravanId).Success,"Petition was not presentable.");Assert(Narrative.Resolve(s,choice).Success,"Petition choice failed.");}
    private static void Roads(GameState s)
    {foreach(var t in s.Tiles){t.Road=true;t.RoadOwner=0;}s.InvalidateLogistics();}
    private static void Reject(GameState s)
    {bool failed=false;try{SaveGame.Deserialize(SaveGame.Serialize(s));}catch(InvalidDataException){failed=true;}Assert(failed,"Corrupt trade case accepted.");}
    public static void Run(Action<string,Action> test)
    {
        test("A real import loss opens one contextual petition with local political cost",()=>
        {
            var (s,buyer,_)=Loss();Assert(buyer.Unrest==4,"Local loss impact missing.");Narrative.Schedule(s);
            Assert(s.PendingEvent==TradeCouncil.Event.Id&&s.PendingTradeCaseId==1&&s.Turn%5!=0,"Trade petition waited for an unrelated timeline event.");
            string before=SaveGame.Serialize(s);var e=ProvincialEvents.Current(s);Assert(e.Title.Contains(buyer.Name)&&e.Body.Contains("Karawana #1")&&e.Choices[0].Gold== -(int)Math.Ceiling(s.Caravans[0].Payment),"Dialogue has unrelated context or price.");
            Assert(SaveGame.Serialize(s)==before&&!Engine.EndTurn(s).Success,"Dialogue preview mutated state or failed to block the turn.");
            Markets.Tick(s);TradeCouncil.Tick(s);Assert(s.TradeCases.Count==1&&buyer.Unrest==4,"Petition or local impact replayed.");SaveGame.Deserialize(SaveGame.Serialize(s));
        });
        test("Compensation spends the exact rounded loss once without inventing goods",()=>
        {
            var (s,buyer,_)=Loss();double gold=s.Player.Gold,stock=buyer.MarketStocks[TradeGood.Salt];int cost=(int)Math.Ceiling(s.Caravans[0].Payment);Decide(s,0);
            Assert(s.Player.Gold==gold-cost&&buyer.MarketStocks[TradeGood.Salt]==stock&&buyer.Prosperity==58&&buyer.Unrest==0&&s.TradeCases[0].Status==TradeCaseStatus.Compensated,"Compensation mismatched its description.");
            string after=SaveGame.Serialize(s);Assert(!Narrative.Resolve(s,0).Success&&!TradeCouncil.Present(s,1).Success&&SaveGame.Serialize(s)==after,"Compensation replayed.");SaveGame.Deserialize(after);
        });
        test("Unfunded compensation is atomic and a free refusal remains available",()=>
        {
            var (s,buyer,_)=Loss();s.Player.Gold=0;TradeCouncil.Present(s,1);string before=SaveGame.Serialize(s);
            Assert(!Narrative.Resolve(s,0).Success&&SaveGame.Serialize(s)==before,"Unaffordable compensation mutated state.");
            Assert(Narrative.Resolve(s,2).Success&&s.TradeCases[0].Status==TradeCaseStatus.Declined&&buyer.Unrest==12,"Free refusal missing or wrong local consequence.");SaveGame.Deserialize(SaveGame.Serialize(s));
        });
        test("A replacement promise requires actual arrivals and settles its reward once",()=>
        {
            var (s,buyer,seller)=Loss();Decide(s,1);Roads(s);var c=s.TradeCases[0];double gold=s.Player.Gold;
            Assert(Engine.SignTradeContract(s,buyer.Id,seller.Id,TradeGood.Salt,4).Success,"Replacement contract rejected.");Step(s);
            Assert(c.Status==TradeCaseStatus.Promised&&TradeCouncil.Progress(s,c)==0&&s.Player.Gold<gold,"Purchasing cargo fulfilled the promise prematurely.");
            Step(s);int commons=s.Player.Commons;double prestige=s.Player.Prestige;Step(s);
            Assert(c.Status==TradeCaseStatus.Fulfilled&&TradeCouncil.Progress(s,c)==4&&c.CreditedCaravans.SequenceEqual(new[]{2})&&s.Player.Commons==commons+6&&s.Player.Prestige==prestige+5&&buyer.Prosperity==58,"Delivered replacement did not fulfill the pledge.");
            string once=SaveGame.Serialize(s);TradeCouncil.Tick(s);Assert(SaveGame.Serialize(s)==once,"Pledge reward replayed.");SaveGame.Deserialize(once);
        });
        test("Partial arrival manifests accumulate across shipments before the deadline",()=>
        {
            var (s,buyer,seller)=Loss();Decide(s,1);Roads(s);Engine.SignTradeContract(s,buyer.Id,seller.Id,TradeGood.Salt,2);
            Step(s);Step(s);Step(s);var c=s.TradeCases[0];Assert(c.Status==TradeCaseStatus.Promised&&TradeCouncil.Progress(s,c)==2,"First partial cargo falsely completed the promise.");
            Step(s);Step(s);Assert(c.Status==TradeCaseStatus.Fulfilled&&c.CreditedCaravans.Count==2&&TradeCouncil.Progress(s,c)==4,"Partial deliveries failed to accumulate.");SaveGame.Deserialize(SaveGame.Serialize(s));
        });
        test("Wrong commodities and pre-existing warehouse stocks do not satisfy a promise",()=>
        {
            var (s,buyer,seller)=Loss();Decide(s,1);Roads(s);buyer.MarketStocks[TradeGood.Salt]=60;Engine.SignTradeContract(s,buyer.Id,seller.Id,TradeGood.Dyes,4);
            Step(s);Step(s);Step(s);Assert(s.Caravans.Any(v=>v.Status==CaravanStatus.Delivered)&&TradeCouncil.Progress(s,s.TradeCases[0])==0,"Wrong good or stored stock counted as replacement imports.");SaveGame.Deserialize(SaveGame.Serialize(s));
        });
        test("Earlier arrivals cannot be reused for a promise made after their market phase",()=>
        {
            var (s,buyer,seller)=Loss();Roads(s);Engine.SignTradeContract(s,buyer.Id,seller.Id,TradeGood.Salt,4);Step(s);Step(s);Step(s);Engine.CancelTradeContract(s,2);
            Assert(s.Caravans[1].Status==CaravanStatus.Delivered,"Early arrival fixture failed.");Decide(s,1);TradeCouncil.Tick(s);
            Assert(TradeCouncil.Progress(s,s.TradeCases[0])==0&&s.TradeCases[0].Status==TradeCaseStatus.Promised,"Earlier arrival was reused after accepting a promise.");SaveGame.Deserialize(SaveGame.Serialize(s));
        });
        test("Deliveries to another own city do not satisfy the named province's promise",()=>
        {
            var (s,_,seller)=Loss();Decide(s,1);Roads(s);var other=Engine.AddCity(s,0,20*s.Width+12,"Inny Brzeg");other.Buildings.Add("market");
            Assert(Engine.SignTradeContract(s,other.Id,seller.Id,TradeGood.Salt,4).Success,"Wrong-city fixture contract failed.");
            for(int i=0;i<5;i++)Step(s);
            Assert(s.Caravans.Any(v=>v.Status==CaravanStatus.Delivered)&&TradeCouncil.Progress(s,s.TradeCases[0])==0,"Another province received credit for the promised city.");SaveGame.Deserialize(SaveGame.Serialize(s));
        });
        test("An already travelling caravan can fulfill a later promise upon its arrival",()=>
        {
            var (s,buyer,seller)=Loss();Roads(s);Engine.SignTradeContract(s,buyer.Id,seller.Id,TradeGood.Salt,4);Step(s);Decide(s,1);Step(s);Step(s);
            Assert(s.TradeCases[0].Status==TradeCaseStatus.Fulfilled&&s.Caravans[1].DepartedTurn==s.TradeCases[0].DecidedTurn,"Prepaid travelling replacement was ignored.");
        });
        test("Deadline failure applies political and local penalties exactly once",()=>
        {
            var (s,buyer,_)=Loss();Decide(s,1);var c=s.TradeCases[0];int commons=s.Player.Commons,stability=s.Player.Stability;double prestige=s.Player.Prestige;
            while(s.Turn<c.DeadlineTurn)Step(s);
            Assert(c.Status==TradeCaseStatus.Broken&&c.ResolvedTurn==c.DeadlineTurn&&s.Player.Commons==Math.Max(0,commons-10)&&s.Player.Stability==stability-4&&s.Player.Prestige==Math.Max(0,prestige-5)&&buyer.Unrest==14&&buyer.Prosperity==44,"Broken promise lacks its advertised consequences.");
            string once=SaveGame.Serialize(s);TradeCouncil.Tick(s);Assert(SaveGame.Serialize(s)==once,"Promise failure repeated.");SaveGame.Deserialize(once);
        });
        test("Arrival during the final permitted market phase fulfills rather than breaks the pledge",()=>
        {
            var (s,buyer,seller)=Loss();Decide(s,1);var c=s.TradeCases[0];s.Turn=c.DeadlineTurn!.Value-5;Markets.Tick(s);Roads(s);Engine.SignTradeContract(s,buyer.Id,seller.Id,TradeGood.Salt,4);
            s.Turn=c.DeadlineTurn.Value-2;Markets.Tick(s);Step(s);Step(s);
            Assert(c.Status==TradeCaseStatus.Fulfilled&&c.ResolvedTurn==c.DeadlineTurn,"Deadline arrival was processed after promise failure.");SaveGame.Deserialize(SaveGame.Serialize(s));
        });
        test("Arrivals after the deadline never repair a broken promise retroactively",()=>
        {
            var (s,buyer,seller)=Loss();Decide(s,1);Roads(s);var c=s.TradeCases[0];s.Turn=c.DeadlineTurn!.Value-1;Markets.Tick(s);Engine.SignTradeContract(s,buyer.Id,seller.Id,TradeGood.Salt,4);
            Step(s);Assert(c.Status==TradeCaseStatus.Broken,"Missing arrival did not break the pledge.");Step(s);Step(s);
            Assert(s.Caravans.Any(v=>v.Status==CaravanStatus.Delivered)&&c.Status==TradeCaseStatus.Broken&&c.CreditedCaravans.Count==0,"Late delivery rewrote a settled promise.");
        });
        test("Losing the petition city closes an undecided case without blocking the council",()=>
        {
            var (s,buyer,_)=Loss();TradeCouncil.Present(s,1);ProvincialPolitics.Transfer(s,buyer,2);
            Assert(s.TradeCases[0].Status==TradeCaseStatus.Voided&&s.PendingEvent==null&&s.PendingTradeCaseId==null,"Lost city left a deadlocked delegation.");SaveGame.Deserialize(SaveGame.Serialize(s));
        });
        test("Losing a promised city breaks the word without changing the new owner's province",()=>
        {
            var (s,buyer,_)=Loss();Decide(s,1);int commons=s.Player.Commons;ProvincialPolitics.Transfer(s,buyer,2);
            Assert(s.TradeCases[0].Status==TradeCaseStatus.Broken&&s.Player.Commons==commons-10&&buyer.Unrest==55&&buyer.Prosperity==50,"City loss left an active pledge or penalized the foreign province.");SaveGame.Deserialize(SaveGame.Serialize(s));
        });
        test("Repeated losses from one province respect the ten-turn delegation interval",()=>
        {
            var (s,buyer,seller)=Loss();Decide(s,2);Engine.SignTradeContract(s,buyer.Id,seller.Id,TradeGood.Salt,4);Step(s);Engine.CancelTradeContract(s,2);s.Player.TradePolicy=TradePolicy.Embargo;Step(s);Step(s);Step(s);
            Assert(s.Caravans.Count(v=>v.Status==CaravanStatus.Lost)==2&&s.TradeCases.Count==1,"Repeat loss spammed delegations.");
        });
        test("Export losses do not create a domestic buyer's petition",()=>
        {
            var (s,home,foreign)=Loss();Decide(s,2);home.MarketStocks[TradeGood.Salt]=20;
            Assert(Engine.SignTradeContract(s,foreign.Id,home.Id,TradeGood.Salt,4).Success,"Export fixture failed.");Step(s);Engine.CancelTradeContract(s,2);s.Factions[1].TradePolicy=TradePolicy.Embargo;Step(s);Step(s);Step(s);
            Assert(s.Caravans[1].Status==CaravanStatus.Lost&&s.TradeCases.Count==1,"Foreign buyer's loss created a domestic compensation case.");SaveGame.Deserialize(SaveGame.Serialize(s));
        });
        test("Overlapping promises for one city and commodity are rejected before applying effects",()=>
        {
            var (s,buyer,seller)=Loss();s.Turn=12;Markets.Tick(s);Decide(s,1);Engine.SignTradeContract(s,buyer.Id,seller.Id,TradeGood.Salt,4);Step(s);Engine.CancelTradeContract(s,2);s.Player.TradePolicy=TradePolicy.Embargo;Step(s);Step(s);Step(s);
            Assert(s.TradeCases.Count==2,"Separated losses did not create a second case.");TradeCouncil.Present(s,s.TradeCases[1].CaravanId);string before=SaveGame.Serialize(s);
            Assert(!Narrative.Resolve(s,1).Success&&SaveGame.Serialize(s)==before&&Narrative.Resolve(s,2).Success,"Duplicated promise changed support or blocked every choice.");SaveGame.Deserialize(SaveGame.Serialize(s));
        });
        test("Presenting a trade petition cannot overwrite another pending council event",()=>
        {
            var (s,_,_)=Loss();s.PendingEvent="grain";string before=SaveGame.Serialize(s);Assert(!TradeCouncil.Present(s,1).Success&&SaveGame.Serialize(s)==before,"Delegation overwrote another council.");Narrative.Schedule(s);Assert(s.PendingEvent=="grain","Scheduling overwrote a pending council.");
        });
        test("Trade petitions and partially fulfilled promises continue deterministically after reload",()=>
        {
            var (a,buyer,seller)=Loss();TradeCouncil.Present(a,1);var pending=SaveGame.Deserialize(SaveGame.Serialize(a));Assert(ProvincialEvents.Current(pending).Title==ProvincialEvents.Current(a).Title,"Pending context lost.");
            Narrative.Resolve(a,1);Roads(a);Engine.SignTradeContract(a,buyer.Id,seller.Id,TradeGood.Salt,2);Step(a);Step(a);Step(a);var b=SaveGame.Deserialize(SaveGame.Serialize(a));
            for(int i=0;i<12;i++){Step(a);Step(b);Assert(SaveGame.Serialize(a)==SaveGame.Serialize(b),"Promise continuation diverged.");SaveGame.Deserialize(SaveGame.Serialize(a));}
        });
        test("V7 migration preserves caravan losses without inventing retroactive petitions",()=>
        {
            var (s,_,_)=Loss();var node=JsonNode.Parse(SaveGame.Serialize(s))!;node["Version"]=7;node.AsObject().Remove("TradeCases");node.AsObject().Remove("PendingTradeCaseId");string legacy=node.ToJsonString();var loaded=SaveGame.Deserialize(legacy);
            Assert(loaded.Version == 18&&loaded.Caravans.Count==s.Caravans.Count&&loaded.Caravans[0].Payment==s.Caravans[0].Payment&&loaded.TradeCases.Count==0,"Migration erased cargo or invented a petition.");Step(loaded);Assert(loaded.TradeCases.Count==0,"Old losses became retroactive petitions.");
            string path=Path.Combine(Environment.CurrentDirectory,"artifacts","migration-v7-test.crown");
            try{File.WriteAllText(path,legacy);SaveGame.Write(loaded,path);Assert(File.ReadAllText(path+".v7.bak")==legacy&&SaveGame.Read(path).Version == 18,"V7 backup missing.");}
            finally{File.Delete(path);File.Delete(path+".v7.bak");}
        });
        test("Save validation rejects forged losses, deadlines, progress and council context",()=>
        {
            var (s,_,_)=Loss();string valid=SaveGame.Serialize(s);s.TradeCases[0].CaravanId=99;Reject(s);s=SaveGame.Deserialize(valid);s.TradeCases[0].OpenedTurn--;Reject(s);
            s=SaveGame.Deserialize(valid);s.PendingTradeCaseId=1;Reject(s);s=SaveGame.Deserialize(valid);s.PendingEvent=TradeCouncil.Event.Id;Reject(s);
            s=SaveGame.Deserialize(valid);Decide(s,1);valid=SaveGame.Serialize(s);s.TradeCases[0].DeadlineTurn++;Reject(s);s=SaveGame.Deserialize(valid);s.TradeCases[0].CreditedCaravans.Add(1);Reject(s);
            s=SaveGame.Deserialize(valid);s.TradeCases[0].Status=TradeCaseStatus.Fulfilled;s.TradeCases[0].ResolvedTurn=s.Turn;Reject(s);
        });
    }
}
