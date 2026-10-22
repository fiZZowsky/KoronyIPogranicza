using Crown.Core;
using System.Text.Json.Nodes;

internal static class StageFiveSpecs
{
    private static void Assert(bool value,string message){if(!value)throw new Exception(message);}
    private static GameState Fresh(){var s=World.Create();Narrative.Resolve(s,0);s.Player.Gold=3000;s.Factions[1].Gold=1000;return s;}
    private static GameState War(WarReason reason=WarReason.Conquest)
    {var s=Fresh();if(reason==WarReason.Redress)DiplomaticCouncil.AddGrievance(s,0,1,40);Assert(Engine.DeclareWar(s,1,reason).Success,"War fixture failed.");s.Turn=2;return s;}
    private static City ExtraCity(GameState s,int owner,string name)
    {
        var tile=s.Tiles.First(t=>t.Passable&&t.Owner<0&&s.Cities.All(c=>World.Distance(t,s.Tiles[c.Tile])>=4));
        return Engine.AddCity(s,owner,tile.Id,name);
    }
    private static void Reject(GameState s)
    {bool rejected=false;try{SaveGame.Deserialize(SaveGame.Serialize(s));}catch(InvalidDataException){rejected=true;}Assert(rejected,"Corrupt campaign accepted.");}
    public static void Run(Action<string,Action> test)
    {
        test("War declarations record an explicit target and reject illegal targets atomically",()=>
        {
            var s=Fresh();var home=s.Cities[0];string before=SaveGame.Serialize(s);
            Assert(!Engine.DeclareWar(s,1,WarReason.Conquest,home.Id).Success&&SaveGame.Serialize(s)==before,"Own city became an enemy goal.");
            var target=ExtraCity(s,1,"Cel wyprawy");Assert(Engine.DeclareWar(s,1,WarReason.Conquest,target.Id).Success,"Selected target rejected.");
            var war=s.Wars.Single();Assert(war.TargetCityId==target.Id&&war.Aggressor==0&&war.GoalDeclared&&war.StartingCitiesB.Contains(target.Id),"Goal or opening borders not recorded.");
            SaveGame.Deserialize(SaveGame.Serialize(s));
        });
        test("War reports are pure and recapture reverses territorial score",()=>
        {
            var s=War();var war=s.Wars.Single();var target=s.Cities.First(c=>c.Owner==1);int baseline=WarCouncil.Score(s,war,0).Score;
            string before=SaveGame.Serialize(s);WarCouncil.Score(s,war,0);WarCouncil.Goal(s,war);Assert(SaveGame.Serialize(s)==before,"War report changed history.");
            ProvincialPolitics.Transfer(s,target,0);int capture=WarCouncil.Score(s,war,0).Score;
            Assert(capture>=baseline+19,"Captured city has no territorial weight.");
            ProvincialPolitics.Transfer(s,target,1);Assert(WarCouncil.Score(s,war,0).Factors.Contains("Zmiany kontroli miast 0"),"Recapture farmed permanent territorial score.");
            Assert(WarCouncil.Score(s,war,0).Score== -WarCouncil.Score(s,war,1).Score,"Scores are not symmetric.");
        });
        test("Real combat records exact losses and bounds its war-score contribution",()=>
        {
            var s=War();var target=s.Cities.First(c=>c.Owner==1);var near=World.Neighbors(s,target.Tile).First(t=>t.Passable&&!s.Units.Any(u=>u.Tile==t.Id));
            var attacker=Engine.AddUnit(s,0,near.Id,UnitKind.Archer);s.Tiles[target.Tile].Explored=true;
            var preview=Engine.PreviewAttack(s,attacker.Id,target.Tile);Assert(preview.Valid,"Invalid combat fixture.");
            Assert(Engine.Move(s,attacker.Id,target.Tile).Success,"Battle failed.");var war=s.Wars.Single();
            Assert(Math.Abs(war.LossesB-preview.DamageToTarget)<.0001&&war.LossesA==preview.DamageToAttacker,"Loss ledger disagrees with combat.");
            war.LossesB=100000;var report=WarCouncil.Score(s,war,0);Assert(report.Factors.Any(f=>f=="Bilans strat w walkach +25")&&report.Score<=100,"Battle score escaped its cap.");
        });
        test("Peace proposal checks are atomic and charge a single embassy fee",()=>
        {
            var s=War();s.Player.Gold=119;string before=SaveGame.Serialize(s);
            Assert(!Engine.ProposePeace(s,1,100).Success&&SaveGame.Serialize(s)==before,"Underfunded proposal mutated state.");
            Assert(!Engine.ProposePeace(s,1,123).Success&&SaveGame.Serialize(s)==before,"Unsupported payment accepted.");
            s.Player.Gold=500;Assert(Engine.ProposePeace(s,1,100).Success&&s.Player.Gold==480&&s.Relation(0,1).War,"Sending prematurely paid reparations or ended war.");
            before=SaveGame.Serialize(s);Assert(!Engine.ProposePeace(s,1,0).Success&&SaveGame.Serialize(s)==before,"Duplicate mission charged again.");
        });
        test("Peace is ratified after two turns, pays once and enforces the truce",()=>
        {
            var s=War();s.Factions[1].WarWeariness=60;double gold=s.Player.Gold,enemyGold=s.Factions[1].Gold;
            Assert(Engine.ProposePeace(s,1,100).Success,"Proposal failed.");s.Turn=3;PeaceCouncil.Tick(s);Assert(s.Relation(0,1).War,"Peace arrived early.");
            s=SaveGame.Deserialize(SaveGame.Serialize(s));s.Turn=4;PeaceCouncil.Tick(s);
            Assert(!s.Relation(0,1).War&&s.Relation(0,1).TruceUntil==16&&s.PeaceOffers.Single().Status==PeaceStatus.Accepted,"Peace not ratified.");
            Assert(s.Player.Gold==gold-120&&s.Factions[1].Gold==enemyGold+100&&s.Wars.Single().EndedTurn==4,"Treaty was only partly applied.");
            string once=SaveGame.Serialize(s);PeaceCouncil.Tick(s);Assert(SaveGame.Serialize(s)==once&&!Engine.DeclareWar(s,1,WarReason.Conquest).Success,"Settlement replayed or truce ignored.");
        });
        test("Reparation goals reward fulfillment once and record failed promises",()=>
        {
            var s=War(WarReason.Redress);s.Factions[1].WarWeariness=100;s.Units.RemoveAll(u=>u.Owner==1);s.InvalidateLogistics();
            int commons=s.Player.Commons;double prestige=s.Player.Prestige;Assert(Engine.ProposePeace(s,1,-100).Success,"Could not request reparations.");s.Turn=4;PeaceCouncil.Tick(s);
            Assert(s.PeaceOffers.Single().Status==PeaceStatus.Accepted&&s.Player.Commons==commons+3&&s.Player.Prestige==prestige+12&&s.Wars.Single().Outcome.Contains("osiągnięty"),"War goal not fulfilled by payment.");
            s=War(WarReason.Redress);s.Factions[1].WarWeariness=100;commons=s.Player.Commons;prestige=s.Player.Prestige;
            Engine.ProposePeace(s,1,0);s.Turn=4;PeaceCouncil.Tick(s);
            Assert(s.Player.Commons==commons-3&&s.Player.Prestige==Math.Max(0,prestige-5)&&s.Wars.Single().Outcome.Contains("nieosiągnięty"),"Abandoned goal has no consequence.");
        });
        test("Returning an occupied city preserves work and evacuates foreign troops",()=>
        {
            var s=Fresh();var lost=ExtraCity(s,0,"Odzyskana prowincja");ProvincialPolitics.Transfer(s,lost,1);lost.Project="granary";lost.Production=23;lost.Health=82;
            Assert(Engine.DeclareWar(s,1,WarReason.Liberation,lost.Id).Success,"Liberation declaration failed.");s.Turn=2;s.Factions[1].WarWeariness=100;
            var guard=Engine.AddUnit(s,1,lost.Tile,UnitKind.Spearmen);var outsider=Engine.AddUnit(s,0,s.Cities.First(c=>c.Owner==1).Tile,UnitKind.Scout);outsider.Destination=lost.Tile;
            Assert(Engine.ProposePeace(s,1,200,lost.Id).Success,"Return terms failed.");s.Turn=4;PeaceCouncil.Tick(s);
            Assert(lost.Owner==0&&lost.Loyalty==65&&lost.Production==23&&lost.Project=="granary"&&lost.Health==82,"Peaceful return destroyed work or lost sovereignty.");
            Assert(s.Tiles[guard.Tile].Owner==1&&s.Tiles[outsider.Tile].Owner==0&&guard.Actions==0&&outsider.Actions==0&&outsider.Destination==null,"Armies stranded across peaceful borders.");
            Assert(s.Wars.Single().Outcome.Contains("Cel wojenny osiągnięty"),"Return did not fulfill liberation.");
        });
        test("Last cities and unrelated territory cannot be ceded",()=>
        {
            var s=War();var target=s.Cities.First(c=>c.Owner==1);target.OriginalOwner=0;string before=SaveGame.Serialize(s);
            Assert(!Engine.ProposePeace(s,1,0,target.Id).Success&&SaveGame.Serialize(s)==before,"Treaty ceded the last city.");
            var unrelated=s.Cities.First(c=>c.Owner==2);Assert(!Engine.ProposePeace(s,1,0,unrelated.Id).Success,"Third party city ceded.");
        });
        test("Changed ownership and insolvency cancel the whole travelling treaty",()=>
        {
            var s=Fresh();var city=ExtraCity(s,0,"Sporny Brzeg");ProvincialPolitics.Transfer(s,city,1);Engine.DeclareWar(s,1,WarReason.Liberation,city.Id);s.Turn=2;
            Engine.ProposePeace(s,1,100,city.Id);ProvincialPolitics.Transfer(s,city,0);double gold=s.Player.Gold;s.Turn=4;PeaceCouncil.Tick(s);
            Assert(s.PeaceOffers.Single().Status==PeaceStatus.Cancelled&&s.Player.Gold==gold&&s.Relation(0,1).War,"Stale city condition partly settled.");
            s=War();Engine.ProposePeace(s,1,100);s.Player.Gold=0;s.Turn=4;PeaceCouncil.Tick(s);
            Assert(s.PeaceOffers.Single().Status==PeaceStatus.Cancelled&&s.Relation(0,1).War&&s.Factions[1].Gold==1000,"Insolvent treaty partly settled.");
        });
        test("Rejected proposals produce a delayed counteroffer requiring player consent",()=>
        {
            var s=War();Engine.ProposePeace(s,1,-200);s.Turn=4;PeaceCouncil.Tick(s);
            Assert(s.PeaceOffers[0].Status==PeaceStatus.Rejected&&s.PeaceOffers.Count==2&&s.PeaceOffers[1].InReplyTo==s.PeaceOffers[0].Id,"No linked counteroffer.");
            var counter=s.PeaceOffers[1];Assert(counter.Status==PeaceStatus.Travelling&&!Engine.AnswerPeace(s,counter.Id,true).Success&&s.Relation(0,1).War,"Counteroffer accepted before arrival.");
            s=SaveGame.Deserialize(SaveGame.Serialize(s));s.Turn=6;PeaceCouncil.Tick(s);counter=s.PeaceOffers[1];
            Assert(counter.Status==PeaceStatus.AwaitingPlayer&&s.Relation(0,1).War,"AI forced peace without consent.");
            Assert(Engine.AnswerPeace(s,counter.Id,true).Success&&!s.Relation(0,1).War&&!Engine.AnswerPeace(s,counter.Id,true).Success,"Player ratification failed or replayed.");
            SaveGame.Deserialize(SaveGame.Serialize(s));
        });
        test("Ignoring or rejecting counteroffers preserves the war",()=>
        {
            foreach(bool ignore in new[]{false,true})
            {
                var s=War();Engine.ProposePeace(s,1,-200);s.Turn=4;PeaceCouncil.Tick(s);s.Turn=6;PeaceCouncil.Tick(s);var counter=s.PeaceOffers[1];
                if(ignore){s.Turn=counter.DeadlineTurn;PeaceCouncil.Tick(s);Assert(counter.Status==PeaceStatus.Expired,"Ignored offer did not expire.");}
                else Assert(Engine.AnswerPeace(s,counter.Id,false).Success&&counter.Status==PeaceStatus.Rejected,"Rejection failed.");
                Assert(s.Relation(0,1).War&&!Engine.AnswerPeace(s,counter.Id,true).Success,"Rejected or expired offer ended war.");SaveGame.Deserialize(SaveGame.Serialize(s));
            }
        });
        test("AI initiates a peace proposal when exhausted instead of forcing an armistice",()=>
        {
            var s=War();s.Turn=40;s.Factions[1].WarWeariness=80;
            Assert(Engine.EndTurn(s).Success&&s.Relation(0,1).War&&s.PeaceOffers.Any(o=>o.From==1&&o.To==0),"AI did not initiate negotiations or forced peace.");
        });
        test("Defeating a state closes its war and cancels outstanding negotiations",()=>
        {
            var s=War();Engine.ProposePeace(s,1,0);var c=s.Cities.First(c=>c.Owner==1);ProvincialPolitics.Transfer(s,c,0);Engine.CheckEnding(s);
            Assert(!s.Relation(0,1).War&&s.Wars.Single().EndedTurn==s.Turn&&s.PeaceOffers.Single().Status==PeaceStatus.Cancelled,"Eliminated state kept an active negotiation.");SaveGame.Deserialize(SaveGame.Serialize(s));
        });
        test("The first negotiated peace generates a consequential reconstruction council",()=>
        {
            var s=War();s.Factions[1].WarWeariness=100;Engine.ProposePeace(s,1,100);s.Turn=4;PeaceCouncil.Tick(s);s.Turn=5;Narrative.Schedule(s);
            Assert(s.PendingEvent=="peace_legacy","Peace aftermath not scheduled.");Assert(Narrative.Resolve(s,0).Success&&s.Deferred.Any(e=>e.Text.Contains("Fundusz odbudowy")),"Reconstruction choice lost its future effect.");
        });
        test("V4 migration preserves grievances, occupation and embassies without invented goals",()=>
        {
            var s=Fresh();Engine.SendDemand(s,2,DemandKind.NonAggression);s.Relation(0,1).War=true;s.Cities[0].Governance=Governance.Autonomous;s.Cities[0].Loyalty=61;DiplomaticCouncil.AddGrievance(s,0,1,42);
            var node=JsonNode.Parse(SaveGame.Serialize(s))!;node["Version"]=4;
            foreach(string key in new[]{"Wars","PeaceOffers","NextWarId","NextPeaceId"})node.AsObject().Remove(key);
            string legacy=node.ToJsonString();var loaded=SaveGame.Deserialize(legacy);
            Assert(loaded.Version==5&&loaded.Wars.Count==1&&!loaded.Wars[0].GoalDeclared&&loaded.Demands.Count==1&&loaded.Cities[0].Loyalty==61&&loaded.Cities[0].Governance==Governance.Autonomous&&DiplomaticCouncil.Grievances(loaded,0,1)==42,"V4 migration lost existing diplomacy or invented a goal.");
            string path=Path.Combine(Environment.CurrentDirectory,"artifacts","migration-v4-test.crown");
            try{File.WriteAllText(path,legacy);SaveGame.Write(loaded,path);Assert(File.ReadAllText(path+".v4.bak")==legacy&&SaveGame.Read(path).Version==5,"V4 backup failed.");}
            finally{File.Delete(path);File.Delete(path+".v4.bak");}
        });
        test("V5 rejects corrupt war ledgers, deadlines, parties and duplicate treaties",()=>
        {
            var s=War();s.Wars[0].LossesA=double.PositiveInfinity;bool rejected=false;try{SaveGame.Serialize(s);}catch(ArgumentException){rejected=true;}Assert(rejected,"Infinite losses serialized.");
            s=War();s.Wars[0].TargetCityId=999999;Reject(s);s=War();s.Wars[0].StartingCitiesB.Add(s.Wars[0].StartingCitiesA[0]);Reject(s);
            s=War();Engine.ProposePeace(s,1,0);s.PeaceOffers[0].DeadlineTurn++;Reject(s);
            s=War();Engine.ProposePeace(s,1,0);s.PeaceOffers[0].From=2;Reject(s);
            s=War();Engine.ProposePeace(s,1,0);s.PeaceOffers[0].Status=PeaceStatus.Accepted;Reject(s);
            s=War();Engine.ProposePeace(s,1,0);s.PeaceOffers.Add(s.PeaceOffers[0]);Reject(s);
        });
        test("War history and delayed peace replies continue deterministically after reload",()=>
        {
            var a=War();Engine.ProposePeace(a,1,-200);var b=SaveGame.Deserialize(SaveGame.Serialize(a));
            for(int i=0;i<12;i++)foreach(var s in new[]{a,b})
            {
                if(s.PendingEvent!=null){var e=Narrative.Get(s.PendingEvent);Narrative.Resolve(s,Array.FindIndex(e.Choices,c=>s.Player.Gold+c.Gold>=0));}
                var incoming=s.PeaceOffers.FirstOrDefault(o=>o.Status==PeaceStatus.AwaitingPlayer);if(incoming!=null)Engine.AnswerPeace(s,incoming.Id,true);
                Assert(Engine.EndTurn(s).Success,"Continuation failed.");
            }
            Assert(SaveGame.Serialize(a)==SaveGame.Serialize(b),"War or negotiations diverged after reload.");
        });
    }
}
