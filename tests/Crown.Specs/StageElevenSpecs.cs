using Crown.Core;
using System.Text.Json.Nodes;

internal static class StageElevenSpecs
{
    private static void Assert(bool value,string message){if(!value)throw new Exception(message);}
    private static GameState Fresh()
    {
        var s=World.CreateLegacy();Narrative.Resolve(s,0);s.Player.Gold=3000;s.Player.Technologies.Add("writing");
        foreach(var city in s.Cities){city.Project="treasury";city.Production=0;}
        return s;
    }
    private static void Step(GameState s)
    {
        if(s.PendingEvent!=null){var e=ProvincialEvents.Current(s);int choice=Array.FindIndex(e.Choices,c=>s.Player.Gold+c.Gold>=0);Assert(Narrative.Resolve(s,choice).Success,"Council resolution failed.");}
        Assert(Engine.EndTurn(s).Success,"Administration turn failed.");
    }
    internal static GameState Ready()
    {
        var s=Fresh();var city=s.Cities.First(c=>c.Owner==0);city.Corruption=55;
        Assert(Engine.StartAudit(s,city.Id).Success,"Audit fixture did not start.");
        for(int i=0;i<3;i++)Step(s);
        Assert(s.PendingEvent==AuditCouncil.Event.Id&&s.Audits[0].Status==AuditStatus.AwaitingCouncil,"Audit fixture did not reach council.");return s;
    }
    private static void Reject(GameState s)
    {bool rejected=false;try{SaveGame.Deserialize(SaveGame.Serialize(s));}catch(InvalidDataException){rejected=true;}Assert(rejected,"Invalid administration save accepted.");}
    public static void Run(Action<string,Action> test)
    {
        test("Corruption removes only the exact share of city gold and forecasts remain pure",()=>
        {
            var s=Fresh();var city=s.Cities[0];var gross=Engine.CityYields(s,city);city.Corruption=40;string before=SaveGame.Serialize(s);var net=Engine.CityYields(s,city);
            Assert(Math.Abs(net.Gold-gross.Gold*.6)<1e-9&&Math.Abs(Administration.Loss(s,city)-gross.Gold*.4)<1e-9,"Revenue loss differs from corruption.");
            Assert(net.Food==gross.Food&&net.Production==gross.Production&&net.Science==gross.Science&&net.Culture==gross.Culture,"Corruption changed unrelated yields.");
            Markets.Plan(s);Administration.Target(s,city);Assert(SaveGame.Serialize(s)==before,"Preview created a hidden balance.");
        });
        test("A completely corrupt office remains finite and economy totals do not subtract losses twice",()=>
        {
            var s=Fresh();var city=s.Cities[0];city.Corruption=100;Assert(Engine.CityYields(s,city).Gold==0&&double.IsFinite(Administration.Loss(s,city)),"Full corruption produced invalid revenue.");
            var report=Engine.Economy(s,0);Assert(report.CityIncome==0&&Math.Abs(report.Net-Engine.Income(s,0).Gold)<1e-9,"Economy double-counted the missing money.");
        });
        test("Actual revenue loss enters the local ledger once per collected turn",()=>
        {
            var s=Fresh();var city=s.Cities[0];city.Corruption=40;Markets.Tick(s);double loss=Administration.Loss(s,city),net=Engine.Income(s,0).Gold,gold=s.Player.Gold;
            Step(s);Assert(Math.Abs(city.HiddenGold-loss)<1e-9&&city.LastRevenueLoss==loss&&city.LastAdministrationTurn==1,"Hidden balance did not match the collected phase.");
            Assert(Math.Abs(s.Player.Gold-(gold+net))<1e-9,"Treasury differs from the net forecast.");
            double next=Administration.Loss(s,city);Markets.Plan(s);Step(s);Assert(Math.Abs(city.HiddenGold-loss-next)<1e-9,"Subsequent ledger duplicated a previous phase or forecast.");
        });
        test("Distance, unrest and governance increase corruption while courts and institutions reduce it",()=>
        {
            var s=Fresh();var home=s.Cities[0];int tile=s.Tiles.First(t=>t.Passable&&t.Owner<0&&World.Distance(t,s.Tiles[home.Tile])>=9).Id;
            var remote=Engine.AddCity(s,0,tile,"Daleka rada");Assert(Administration.Target(s,remote)>Administration.Target(s,home),"Distance has no administrative cost.");
            int before=Administration.Target(s,remote);remote.Unrest=40;remote.Governance=Governance.Autonomous;Assert(Administration.Target(s,remote)>before,"Unrest and autonomy did not change target.");
            int exposed=Administration.Target(s,remote);remote.Buildings.Add("court");s.Player.Technologies.UnionWith(["civilservice","constitution"]);
            Assert(Administration.Target(s,remote)<exposed,"Institutions did not improve integrity.");
        });
        test("Independent supervision lowers the target only before its exclusive end turn",()=>
        {
            var s=Fresh();var city=s.Cities[0];city.Unrest=80;int normal=Administration.Target(s,city);city.AuditProtectionUntil=13;
            Assert(Administration.Target(s,city)==Math.Max(0,normal-20),"Supervision changed the wrong target.");s.Turn=12;Assert(Administration.Target(s,city)==Math.Max(0,normal-20),"Protection expired early.");
            s.Turn=13;Assert(Administration.Target(s,city)==normal,"Expired protection survived.");
        });
        test("Audit takes three turns, charges once and freezes only actual accumulated shortages",()=>
        {
            var s=Fresh();var city=s.Cities[0];city.Corruption=40;double gold=s.Player.Gold;Assert(Engine.StartAudit(s,city.Id).Success,"Audit failed.");
            Assert(s.Player.Gold==gold-35&&s.Audits[0].AssessedGold==0,"Audit fabricated or prepaid recovery.");
            for(int i=0;i<2;i++){Step(s);Assert(s.Audits[0].Status==AuditStatus.Investigating,"Audit completed early.");}
            double expected=city.HiddenGold+Administration.Loss(s,city);Step(s);
            Assert(s.Audits[0].CompletedTurn==4&&Math.Abs(s.Audits[0].AssessedGold-expected)<1e-9&&city.HiddenGold==0&&s.PendingAuditId==1,"Report did not freeze the collected balance.");
            string once=SaveGame.Serialize(s);AuditCouncil.Tick(s);Assert(SaveGame.Serialize(s)==once,"Repeated completion froze money again.");SaveGame.Deserialize(once);
        });
        test("Invalid, poor, duplicate and unsupported audits are rejected atomically",()=>
        {
            var s=Fresh();var city=s.Cities[0];city.Corruption=30;
            void Fail(int id){string before=SaveGame.Serialize(s);Assert(!Engine.StartAudit(s,id).Success&&SaveGame.Serialize(s)==before,"Rejected audit changed state.");}
            Fail(-1);Fail(s.Cities.First(c=>c.Owner==1).Id);s.Player.Technologies.Remove("writing");Fail(city.Id);s.Player.Technologies.Add("writing");s.Player.Gold=34;Fail(city.Id);
            s.Player.Gold=3000;city.Corruption=0;Fail(city.Id);city.Corruption=30;Assert(Engine.StartAudit(s,city.Id).Success,"Funded audit failed.");Fail(city.Id);
        });
        test("Twelve-turn audit interval survives resolution and cannot be bypassed with low corruption",()=>
        {
            var s=Ready();var city=s.Cities[0];Narrative.Resolve(s,2);s.Turn=12;city.Corruption=40;string before=SaveGame.Serialize(s);
            Assert(!Engine.StartAudit(s,city.Id).Success&&SaveGame.Serialize(s)==before,"Audit ignored cooldown.");s.Turn=13;Assert(Engine.StartAudit(s,city.Id).Success,"Audit did not become available after twelve turns.");
            s.Turn+=3;AuditCouncil.Tick(s);AuditCouncil.Present(s,2);Narrative.Resolve(s,0);Assert(s.Audits[1].AssessedGold==0&&s.Audits[1].RecoveredGold==0,"Repeating an audit recovered the old frozen sum again.");SaveGame.Deserialize(SaveGame.Serialize(s));
        });
        test("Delaying a ruling never adds subsequent losses to the frozen report",()=>
        {
            var s=Ready();var city=s.Cities[0];double amount=s.Audits[0].AssessedGold;s.PendingEvent=null;s.PendingAuditId=null;
            Step(s);double newLoss=city.HiddenGold;Assert(newLoss>0&&s.Audits[0].AssessedGold==amount,"Delayed report absorbed new losses.");
            Assert(AuditCouncil.Present(s,1).Success,"Could not reopen a valid report.");double gold=s.Player.Gold;int expected=AuditCouncil.Recovery(s.Audits[0],0);Narrative.Resolve(s,0);
            Assert(s.Player.Gold==gold+expected&&city.HiddenGold==newLoss&&s.Audits[0].RecoveredGold==expected,"Recovery used new losses or changed the unexamined ledger.");
        });
        test("Public prosecution restores three quarters and carries the authored political costs",()=>
        {
            var s=Ready();var city=s.Cities[0];int corruption=city.Corruption,unrest=city.Unrest,loyalty=city.Loyalty,nobles=s.Player.Nobles,commons=s.Player.Commons;
            var choice=ProvincialEvents.Current(s).Choices[0];double gold=s.Player.Gold;Assert(Narrative.Resolve(s,0).Success,"Prosecution failed.");
            Assert(s.Player.Gold==gold+choice.Gold&&s.Audits[0].Status==AuditStatus.Prosecuted&&s.Audits[0].RecoveredGold==choice.Gold,"Ruling differs from its quoted recovery.");
            Assert(city.Corruption==Math.Max(0,corruption-25)&&city.Unrest==unrest+6&&city.Loyalty==loyalty-4&&s.Player.Nobles==nobles-10&&s.Player.Commons==commons+6,"Prosecution ignored its tradeoff.");
        });
        test("Office reform costs additional money, forgoes recovery and grants local twelve-turn supervision",()=>
        {
            var s=Ready();var city=s.Cities[0];var other=s.Cities.First(c=>c.Owner==1);int corruption=city.Corruption,loyalty=city.Loyalty,scholars=s.Player.Scholars,otherCorruption=other.Corruption;double gold=s.Player.Gold;
            Assert(Narrative.Resolve(s,1).Success&&s.Player.Gold==gold-60&&s.Audits[0].RecoveredGold==0,"Reform recovered old money or omitted its cost.");
            Assert(city.Corruption==Math.Max(0,corruption-35)&&city.Loyalty==loyalty+4&&s.Player.Scholars==scholars+8&&city.AuditProtectionUntil==s.Turn+12&&other.Corruption==otherCorruption,"Local reform leaked into another province.");
        });
        test("Settlement restores a quarter with smaller corruption relief and favors the nobles",()=>
        {
            var s=Ready();var city=s.Cities[0];int corruption=city.Corruption,nobles=s.Player.Nobles,commons=s.Player.Commons;double gold=s.Player.Gold;int expected=AuditCouncil.Recovery(s.Audits[0],2);
            Assert(Narrative.Resolve(s,2).Success&&s.Player.Gold==gold+expected&&s.Audits[0].Status==AuditStatus.Settled,"Settlement payment differs.");
            Assert(city.Corruption==corruption-8&&s.Player.Nobles==nobles+6&&s.Player.Commons==commons-5&&city.AuditProtectionUntil==0,"Settlement omitted its political consequence.");
        });
        test("An empty treasury can still choose prosecution or settlement but cannot finance reform",()=>
        {
            var s=Ready();s.Player.Gold=0;string before=SaveGame.Serialize(s);
            Assert(!Narrative.Resolve(s,1).Success&&SaveGame.Serialize(s)==before,"Unfunded reform changed the council.");Assert(Narrative.Resolve(s,2).Success&&s.Player.Gold>=0,"Council softlocked an empty treasury.");
        });
        test("Closed audit rulings cannot be replayed or re-presented for another payment",()=>
        {
            var s=Ready();Narrative.Resolve(s,0);string once=SaveGame.Serialize(s);Assert(!Narrative.Resolve(s,0).Success&&!AuditCouncil.Present(s,1).Success&&SaveGame.Serialize(s)==once,"Closed ruling paid twice.");
        });
        test("Audit reports queue behind an existing council instead of replacing it",()=>
        {
            var s=Ready();s.PendingEvent="grain";s.PendingAuditId=null;string before=SaveGame.Serialize(s);
            Assert(!AuditCouncil.Present(s,1).Success&&!AuditCouncil.Schedule(s)&&SaveGame.Serialize(s)==before,"Report overwrote another speaker.");
            Narrative.Resolve(s,2);Narrative.Schedule(s);Assert(s.PendingEvent==AuditCouncil.Event.Id&&s.PendingAuditId==1,"Queued report did not return after the other case.");
        });
        test("Different provinces keep separate reports in deterministic completion order",()=>
        {
            var s=Fresh();var city=s.Cities[0];city.Corruption=40;var second=Engine.AddCity(s,0,s.Tiles.First(t=>t.Passable&&t.Owner<0&&World.Distance(t,s.Tiles[city.Tile])>=6).Id,"Drugie księgi");second.Corruption=35;second.HiddenGold=17;
            Engine.StartAudit(s,second.Id);Engine.StartAudit(s,city.Id);s.Turn+=3;AuditCouncil.Tick(s);Narrative.Schedule(s);
            Assert(s.PendingAuditId==1&&s.Audits[0].CityId==second.Id&&s.Audits[0].AssessedGold==17&&s.Audits[1].AssessedGold==0,"Reports combined provinces or lost their order.");
            Narrative.Resolve(s,2);Narrative.Schedule(s);Assert(s.PendingAuditId==2,"Second report was discarded.");SaveGame.Deserialize(SaveGame.Serialize(s));
        });
        test("Capturing an investigated city cancels its audit without transferring money or applying a ruling",()=>
        {
            var s=Fresh();var city=s.Cities[0];city.Corruption=40;city.HiddenGold=50;Engine.StartAudit(s,city.Id);double player=s.Player.Gold,enemy=s.Factions[1].Gold;
            ProvincialPolitics.Transfer(s,city,1);AuditCouncil.Tick(s);
            Assert(s.Audits[0].Status==AuditStatus.Cancelled&&s.Audits[0].CompletedTurn==null&&s.Player.Gold==player&&s.Factions[1].Gold==enemy&&city.HiddenGold==0,"Capture paid an audit or moved hidden money into a foreign treasury.");SaveGame.Deserialize(SaveGame.Serialize(s));
        });
        test("Capture closes a ready council and ends supervision, while recapture cannot revive its report",()=>
        {
            var s=Ready();var city=s.Cities[0];city.AuditProtectionUntil=s.Turn+12;double gold=s.Player.Gold;ProvincialPolitics.Transfer(s,city,1);
            Assert(s.PendingEvent==null&&s.PendingAuditId==null&&s.Audits[0].Status==AuditStatus.Cancelled&&city.AuditProtectionUntil==0&&s.Player.Gold==gold,"Capture left a stale council or supervision.");
            ProvincialPolitics.Transfer(s,city,0);Assert(!AuditCouncil.Present(s,1).Success,"Recapture revived the old report.");SaveGame.Deserialize(SaveGame.Serialize(s));
        });
        test("AI finances local integrity measures but never takes a player's administrative decision",()=>
        {
            var s=Fresh();s.Cities[0].Corruption=31;var foreign=s.Cities.First(c=>c.Owner==1);foreign.Corruption=31;s.Factions[1].Gold=1000;
            Step(s);Assert(foreign.Corruption<=6&&foreign.HiddenGold==0&&foreign.AuditProtectionUntil==9&&foreign.LastAuditTurn==1,"AI neglected a funded integrity measure.");
            Assert(s.Cities[0].Corruption==30&&s.Audits.Count==0&&s.Cities[0].AuditProtectionUntil==0,"AI spent money or chose a reform for the player.");
        });
        test("Audit investigation and unresolved findings resume identically after save and reload",()=>
        {
            var a=Fresh();a.Cities[0].Corruption=45;Engine.StartAudit(a,a.Cities[0].Id);Step(a);var b=SaveGame.Deserialize(SaveGame.Serialize(a));
            Step(a);Step(b);Step(a);Step(b);Assert(SaveGame.Serialize(a)==SaveGame.Serialize(b),"Investigation diverged after reload.");b=SaveGame.Deserialize(SaveGame.Serialize(a));
            Narrative.Resolve(a,1);Narrative.Resolve(b,1);for(int i=0;i<15;i++){Step(a);Step(b);Assert(SaveGame.Serialize(a)==SaveGame.Serialize(b),"Reform continuation diverged.");SaveGame.Deserialize(SaveGame.Serialize(a));}
        });
        test("V10 migration preserves accepted transit and merchant promises without invented shortages",()=>
        {
            var s=Fresh();s.Relation(0,2).Trust=40;Engine.SendDemand(s,2,DemandKind.Transit);Step(s);Step(s);var node=JsonNode.Parse(SaveGame.Serialize(s))!;node["Version"]=10;string legacy=node.ToJsonString();var loaded=SaveGame.Deserialize(legacy);
            Assert(loaded.Version == 18&&TransitCouncil.Until(loaded,2)==TransitCouncil.Until(s,2)&&loaded.Cities.All(c=>c.Corruption==0&&c.HiddenGold==0)&&loaded.Audits.Count==0,"Migration invented losses or erased an accepted passage.");
            var (promises,_,_)=StageEightSpecs.Loss();TradeCouncil.Present(promises,1);Narrative.Resolve(promises,1);node=JsonNode.Parse(SaveGame.Serialize(promises))!;node["Version"]=10;
            Assert(SaveGame.Deserialize(node.ToJsonString()).TradeCases[0].Status==TradeCaseStatus.Promised,"Migration erased a merchant promise.");
            string path=Path.Combine(Environment.CurrentDirectory,"artifacts","migration-v10-test.crown");
            try{File.WriteAllText(path,legacy);SaveGame.Write(loaded,path);Assert(File.ReadAllText(path+".v10.bak")==legacy&&SaveGame.Read(path).Version == 18,"V10 backup missing.");}
            finally{File.Delete(path);File.Delete(path+".v10.bak");}
        });
        test("Save validation rejects corrupt ledgers, forged recovery, unknown cities and stale council context",()=>
        {
            string valid=SaveGame.Serialize(Ready());var s=SaveGame.Deserialize(valid);s.Cities[0].Corruption=101;Reject(s);s=SaveGame.Deserialize(valid);s.Cities[0].HiddenGold=-1;Reject(s);
            s=SaveGame.Deserialize(valid);s.Cities[0].LastRevenueLoss=double.PositiveInfinity;bool finiteRejected=false;try{SaveGame.Serialize(s);}catch(ArgumentException){finiteRejected=true;}Assert(finiteRejected,"Infinite loss serialized.");
            s=SaveGame.Deserialize(valid);s.Audits[0].CityId=-1;Reject(s);s=SaveGame.Deserialize(valid);s.Audits[0].DueTurn++;Reject(s);
            s=SaveGame.Deserialize(valid);s.Audits[0].RecoveredGold=100;Reject(s);s=SaveGame.Deserialize(valid);s.PendingAuditId=999;Reject(s);
            s=SaveGame.Deserialize(valid);s.Audits[0].CompletedTurn=s.Turn+1;Reject(s);s=SaveGame.Deserialize(valid);Narrative.Resolve(s,0);s.Audits[0].RecoveredGold++;Reject(s);
            s=SaveGame.Deserialize(valid);s.Cities[0].Owner=1;Reject(s);
        });
    }
}
