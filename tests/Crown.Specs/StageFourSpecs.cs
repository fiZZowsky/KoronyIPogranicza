using Crown.Core;
using System.Text.Json.Nodes;

internal static class StageFourSpecs
{
    private static void Assert(bool value, string message) { if (!value) throw new Exception(message); }
    private static GameState Fresh() { var s=World.Create();Narrative.Resolve(s,0);s.Player.Gold=2000;return s; }
    private static void Reject(GameState s)
    {
        bool rejected=false;try{SaveGame.Deserialize(SaveGame.Serialize(s));}catch(InvalidDataException){rejected=true;}
        Assert(rejected,"Invalid political save accepted.");
    }
    public static void Run(Action<string,Action> test)
    {
        test("War justification previews are pure and illegal declarations spend nothing",()=>
        {
            var s=Fresh();string before=SaveGame.Serialize(s);
            Assert(!DiplomaticCouncil.PreviewWar(s,1,WarReason.Redress).Allowed&&!Engine.DeclareWar(s,1,WarReason.Redress).Success,"Invented a grievance.");
            Assert(!Engine.DeclareWar(s,1,WarReason.Liberation).Success&&!Engine.DeclareWar(s,1,(WarReason)99).Success,"Invalid liberation or reason.");
            Assert(SaveGame.Serialize(s)==before,"Preview or rejection mutated state.");
        });
        test("Conquest costs domestic support and third-party trust",()=>
        {
            var s=Fresh();int commons=s.Player.Commons,stability=s.Player.Stability,trust=s.Relation(0,2).Trust;
            Assert(Engine.DeclareWar(s,1,WarReason.Conquest).Success,"War failed.");
            Assert(s.Player.Commons==commons-8&&s.Player.Stability==stability-6&&s.Relation(0,2).Trust==trust-12,"Wrong declared costs.");
            Assert(DiplomaticCouncil.Grievances(s,1,0)==40&&DiplomaticCouncil.Grievances(s,0,1)==0,"Grievance credited to aggressor.");
        });
        test("Redress spends a directed grievance and liberation recognizes founders",()=>
        {
            var s=Fresh();DiplomaticCouncil.AddGrievance(s,0,1,40);int trust=s.Relation(0,2).Trust,commons=s.Player.Commons;
            Assert(Engine.DeclareWar(s,1,WarReason.Redress).Success&&DiplomaticCouncil.Grievances(s,0,1)==10&&s.Player.Commons==commons-2&&s.Relation(0,2).Trust==trust,"Justified war costs wrong.");
            s=Fresh();var city=s.Cities.First(c=>c.Owner==1);city.OriginalOwner=0;
            Assert(Engine.DeclareWar(s,1,WarReason.Liberation).Success,"Founder has no liberation claim.");
        });
        test("Diplomatic missions resolve once after two turns and survive reload",()=>
        {
            var s=Fresh();s.Relation(0,1).Trust=80;DiplomaticCouncil.AddGrievance(s,0,1,50);s.Factions[1].Gold=200;
            Assert(Engine.SendDemand(s,1,DemandKind.Compensation).Success&&s.Player.Gold==1980,"Mission charge wrong.");
            Assert(!Engine.Diplomacy(s,1,"gift").Success,"Mission bypassed conversation limit.");
            s.Turn=2;DiplomaticCouncil.Tick(s);Assert(s.Demands.Single().Status==DemandStatus.Pending,"Reply arrived early.");
            s=SaveGame.Deserialize(SaveGame.Serialize(s));s.Turn=3;DiplomaticCouncil.Tick(s);
            Assert(s.Demands.Single().Status==DemandStatus.Accepted&&s.Player.Gold==2060&&s.Factions[1].Gold==120&&DiplomaticCouncil.Grievances(s,0,1)==20,"Compensation settlement failed.");
            string once=SaveGame.Serialize(s);DiplomaticCouncil.Tick(s);Assert(SaveGame.Serialize(s)==once,"Reply applied twice.");
        });
        test("Mission replies use the situation on arrival and refusals do not invent claims",()=>
        {
            var s=Fresh();s.Relation(0,1).Trust=80;DiplomaticCouncil.AddGrievance(s,0,1,40);
            Assert(Engine.SendDemand(s,1,DemandKind.Compensation).Success,"Could not send.");
            s.Relation(0,1).Trust=-90;s.Turn=3;DiplomaticCouncil.Tick(s);
            Assert(s.Demands.Single().Status==DemandStatus.Rejected&&s.Player.Gold==1980&&DiplomaticCouncil.Grievances(s,0,1)==40,"Refusal created money or extra grievance.");
            s=Fresh();s.Relation(0,1).Trust=100;s.Factions[1].Gold=79;DiplomaticCouncil.AddGrievance(s,0,1,40);Engine.SendDemand(s,1,DemandKind.Compensation);s.Turn=3;DiplomaticCouncil.Tick(s);
            Assert(s.Demands.Single().Status==DemandStatus.Rejected,"An insolvent state paid compensation.");
        });
        test("Non-aggression pacts enforce the same truce for player and AI",()=>
        {
            var s=Fresh();s.Relation(0,1).Trust=100;Assert(Engine.SendDemand(s,1,DemandKind.NonAggression).Success,"Pact mission failed.");
            s.Turn=3;DiplomaticCouncil.Tick(s);var r=s.Relation(0,1);
            Assert(r.TruceUntil==18&&r.Trust==100&&!Engine.DeclareWar(s,1,WarReason.Conquest).Success,"Pact does not block war or trust escaped bounds.");
            SaveGame.Deserialize(SaveGame.Serialize(s));s.Turn=18;Assert(Engine.DeclareWar(s,1,WarReason.Conquest).Success,"Expired pact still blocks war.");
        });
        test("Demand cooldowns, treasury limits and war cancellation are atomic",()=>
        {
            var s=Fresh();s.Player.Gold=19;string before=SaveGame.Serialize(s);Assert(!Engine.SendDemand(s,1,DemandKind.NonAggression).Success&&SaveGame.Serialize(s)==before,"Unfunded mission mutated state.");
            s.Player.Gold=100;Assert(Engine.SendDemand(s,1,DemandKind.NonAggression).Success,"Funded mission failed.");s.Turn=2;
            Assert(!Engine.SendDemand(s,1,DemandKind.NonAggression).Success,"Pending mission duplicated.");
            Assert(Engine.DeclareWar(s,1,WarReason.Conquest).Success&&s.Demands.Single().Status==DemandStatus.Cancelled,"War left the embassy pending.");
            SaveGame.Deserialize(SaveGame.Serialize(s));
        });
        test("Governance changes trade income for loyalty and honor their cooldown",()=>
        {
            var s=Fresh();var c=s.Cities.First(c=>c.Owner==0);var before=Engine.CityYields(s,c);int target=ProvincialPolitics.Report(s,c).Target;
            Assert(Engine.SetGovernance(s,c.Id,Governance.Autonomous).Success,"Autonomy failed.");var after=Engine.CityYields(s,c);
            Assert(Math.Abs(after.Gold-before.Gold*.7)<.00001&&Math.Abs(after.Culture-before.Culture*1.2)<.00001&&ProvincialPolitics.Report(s,c).Target==Math.Min(95,target+18),"Autonomy has incorrect costs.");
            string unchanged=SaveGame.Serialize(s);Assert(!Engine.SetGovernance(s,c.Id,Governance.Integrated).Success&&SaveGame.Serialize(s)==unchanged,"Cooldown mutated state.");
            s.Turn+=8;Assert(Engine.SetGovernance(s,c.Id,Governance.Military).Success,"Could not change after cooldown.");
        });
        test("Military administration depends on a real military garrison",()=>
        {
            var s=Fresh();var c=s.Cities[0];c.Governance=Governance.Military;s.Units.RemoveAll(u=>u.Owner==0);s.InvalidateLogistics();
            int empty=ProvincialPolitics.Report(s,c).Target;Engine.AddUnit(s,0,c.Tile,UnitKind.Scout);
            Assert(ProvincialPolitics.Report(s,c).Target==empty,"Scout counts as occupation garrison.");
            Engine.AddUnit(s,0,c.Tile,UnitKind.Spearmen);Assert(ProvincialPolitics.Report(s,c).Target==Math.Min(95,empty+28),"Missing military and garrison effects.");
        });
        test("Capture establishes occupation and recapture restores original sovereignty",()=>
        {
            var s=Fresh();var c=s.Cities.First(c=>c.Owner==1);int tile=c.Tile;
            ProvincialPolitics.Transfer(s,c,0);Assert(c.OriginalOwner==1&&c.Loyalty==30&&c.OccupiedUntil==13&&c.Owner==0,"Capture lost provenance.");
            Assert(s.Tiles[tile].Owner==0,"City border did not transfer.");
            int occupied=ProvincialPolitics.Report(s,c).Target;s.Turn=13;Assert(ProvincialPolitics.Report(s,c).Target==Math.Min(95,occupied+20),"Occupation did not expire.");
            ProvincialPolitics.Transfer(s,c,1);Assert(c.Owner==1&&c.Loyalty==65&&c.OccupiedUntil==0&&c.RevoltTurns==0,"Recapture not recognized.");
        });
        test("Revolt gives two warnings then transfers borders and evacuates the garrison",()=>
        {
            var s=Fresh();var home=s.Cities[0];var c=s.Cities.First(c=>c.Owner==1);ProvincialPolitics.Transfer(s,c,0);
            c.Loyalty=15;c.Unrest=100;c.Prosperity=0;s.Player.Stability=0;s.Player.Tax=2;
            var guard=Engine.AddUnit(s,0,c.Tile,UnitKind.Spearmen);guard.Destination=home.Tile;
            s.PendingEvent="local_autonomy";s.PendingCityId=c.Id;
            ProvincialPolitics.Tick(s);Assert(c.Owner==0&&c.RevoltTurns==1,"No first warning.");
            ProvincialPolitics.Tick(s);Assert(c.Owner==0&&c.RevoltTurns==2,"No second warning.");
            s=SaveGame.Deserialize(SaveGame.Serialize(s));c=s.Cities.First(x=>x.Id==c.Id);guard=s.Units.First(x=>x.Id==guard.Id);
            ProvincialPolitics.Tick(s);Assert(c.Owner==1&&s.Tiles[c.Tile].Owner==1&&guard.Tile==home.Tile&&guard.Actions==0&&guard.Destination==null,"Revolt stranded the garrison or left borders behind.");
            Assert(s.PendingEvent==null&&s.PendingCityId==null&&DiplomaticCouncil.Grievances(s,0,1)==30,"Stale council or missing claim after revolt.");
        });
        test("Recovering loyalty resets rebellion and native cities cannot defect",()=>
        {
            var s=Fresh();var c=s.Cities.First(c=>c.Owner==1);ProvincialPolitics.Transfer(s,c,0);c.Loyalty=19;c.RevoltTurns=2;
            c.Governance=Governance.Autonomous;c.Unrest=0;c.Prosperity=100;s.Player.Stability=100;s.Player.Tax=0;
            ProvincialPolitics.Tick(s);Assert(c.Owner==0&&c.Loyalty>=20&&c.RevoltTurns==0,"Recovery did not cancel rebellion.");
            var native=s.Cities.First(c=>c.OriginalOwner==0);native.Loyalty=0;native.Unrest=100;s.Player.Stability=0;
            for(int i=0;i<5;i++)ProvincialPolitics.Tick(s);Assert(native.Owner==0&&native.RevoltTurns==0,"Native city defected to arbitrary faction.");
        });
        test("Autonomy dialogue affects one province and leaves a political legacy",()=>
        {
            var s=Fresh();var c=s.Cities.First(c=>c.Owner==1);ProvincialPolitics.Transfer(s,c,0);s.Turn=5;Narrative.Schedule(s);
            Assert(s.PendingEvent=="local_autonomy"&&s.PendingCityId==c.Id,"No local autonomy council.");
            Assert(Narrative.Resolve(s,0).Success&&c.Governance==Governance.Autonomous&&c.Loyalty==40&&s.Cities[0].Governance==Governance.Integrated,"Choice leaked or lost loyalty effect.");
            SaveGame.Deserialize(SaveGame.Serialize(s));
        });
        test("V3 migration preserves provincial history and creates an original backup",()=>
        {
            var s=Fresh();s.Cities[0].Charters.Add("integration");s.Cities[0].Prosperity=63;
            var node=JsonNode.Parse(SaveGame.Serialize(s))!;node["Version"]=3;node.AsObject().Remove("Demands");node.AsObject().Remove("NextDemandId");
            foreach(var city in node["Cities"]!.AsArray())foreach(string key in new[]{"OriginalOwner","Governance","Loyalty","OccupiedUntil","RevoltTurns","LastGovernanceTurn"})city!.AsObject().Remove(key);
            string legacy=node.ToJsonString();var loaded=SaveGame.Deserialize(legacy);
            Assert(loaded.Version==6&&loaded.Cities.All(c=>c.OriginalOwner==c.Owner&&c.Loyalty==75)&&loaded.Cities[0].Prosperity==63&&loaded.Cities[0].Charters.Contains("integration"),"Migration damaged provincial data.");
            string path=Path.Combine(Environment.CurrentDirectory,"artifacts","migration-v3-test.crown");
            try{File.WriteAllText(path,legacy);SaveGame.Write(loaded,path);Assert(File.ReadAllText(path+".v3.bak")==legacy&&SaveGame.Read(path).Version==6,"V3 backup failed.");}
            finally{File.Delete(path);File.Delete(path+".v3.bak");}
        });
        test("Corrupt political state and forged diplomatic missions are rejected",()=>
        {
            var s=Fresh();s.Cities[0].OriginalOwner=7;Reject(s);s=Fresh();s.Cities[0].Loyalty=101;Reject(s);
            s=Fresh();s.Relation(0,1).GrievanceA=-1;Reject(s);s=Fresh();s.Cities[0].RevoltTurns=2;Reject(s);
            s=Fresh();Engine.SendDemand(s,1,DemandKind.NonAggression);s.Demands[0].DueTurn=8;Reject(s);
            s=Fresh();Engine.SendDemand(s,1,DemandKind.NonAggression);s.Demands[0].From=2;Reject(s);
            s=Fresh();Engine.SendDemand(s,1,DemandKind.NonAggression);s.Demands[0].Status=DemandStatus.Accepted;Reject(s);
        });
        test("Combat capture and road pillaging create grievances for the actual victim",()=>
        {
            var s=Fresh();var c=s.Cities.First(c=>c.Owner==1);s.Relation(0,1).War=true;c.Health=1;
            var refuge=s.Tiles.First(t=>t.Passable&&t.Owner<0&&s.Cities.All(city=>World.Distance(t,s.Tiles[city.Tile])>=4));Engine.AddCity(s,1,refuge.Id,"Schronienie Ligi");
            s.Units.RemoveAll(u=>u.Owner==1&&u.Tile==c.Tile);s.Tiles[c.Tile].Explored=true;
            var near=World.Neighbors(s,c.Tile).First(t=>t.Passable&&!s.Units.Any(u=>u.Owner!=0&&u.Tile==t.Id));
            var attacker=Engine.AddUnit(s,0,near.Id,UnitKind.Spearmen);
            Assert(Engine.Move(s,attacker.Id,c.Tile).Success&&c.Owner==0&&c.OriginalOwner==1&&c.Loyalty==30&&DiplomaticCouncil.Grievances(s,1,0)==30,"Combat failed to register occupation and a claim.");
            near.Road=true;near.RoadOwner=1;attacker.Tile=near.Id;attacker.Actions=2;
            Assert(Engine.PillageRoad(s,attacker.Id).Success&&DiplomaticCouncil.Grievances(s,1,0)==42,"Pillaging failed to record the road owner's grievance.");
        });
        test("AI honors an active pact even with a hostile and stronger army",()=>
        {
            var s=Fresh();s.Turn=30;var r=s.Relation(0,3);r.Trust=-100;r.TruceUntil=45;
            var city=s.Cities.First(c=>c.Owner==3);
            for(int i=0;i<15;i++)Engine.AddUnit(s,3,city.Tile,UnitKind.Cavalry);
            for(int i=0;i<12;i++)
            {
                if(s.PendingEvent!=null){var e=Narrative.Get(s.PendingEvent);Narrative.Resolve(s,Array.FindIndex(e.Choices,c=>s.Player.Gold+c.Gold>=0));}
                Assert(Engine.EndTurn(s).Success&&!r.War,"AI broke the pact.");
            }
        });
        test("Pending embassies and political consequences resume deterministically",()=>
        {
            var a=Fresh();a.Relation(0,1).Trust=80;Engine.SendDemand(a,1,DemandKind.NonAggression);var b=SaveGame.Deserialize(SaveGame.Serialize(a));
            for(int i=0;i<18;i++)
            {
                foreach(var s in new[]{a,b})
                {if(s.PendingEvent!=null){var e=Narrative.Get(s.PendingEvent);Narrative.Resolve(s,Array.FindIndex(e.Choices,c=>s.Player.Gold+c.Gold>=0));}Assert(Engine.EndTurn(s).Success,"Continuation turn failed.");}
            }
            Assert(SaveGame.Serialize(a)==SaveGame.Serialize(b),"Political continuation diverged.");
        });
    }
}
