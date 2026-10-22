using Crown.Core;
using System.Text.Json.Nodes;

internal static class StageTwelveSpecs
{
    private static void Assert(bool value,string message){if(!value)throw new Exception(message);}
    private static GameState Fresh()
    {
        var s=World.Create();Narrative.Resolve(s,0);s.Player.Gold=3000;s.Player.Technologies.UnionWith(["pottery","writing","agriculture","mining","bronze","masonry"]);
        var city=s.Cities[0];city.Project="treasury";city.Production=0;Markets.Tick(s);return s;
    }
    private static void Step(GameState s)
    {
        if(s.PendingEvent!=null){var e=ProvincialEvents.Current(s);int index=Array.FindIndex(e.Choices,c=>s.Player.Gold+c.Gold>=0);Assert(Narrative.Resolve(s,index).Success,"Council resolution failed.");}
        Assert(Engine.EndTurn(s).Success,"Production turn failed.");
    }
    private static void Add(GameState s,string id,bool repeat=false)=>Assert(Engine.EnqueueProject(s,s.Cities[0].Id,id,repeat).Success,"Could not enqueue "+id);
    private static void AtomicFailure(GameState s,Func<Result> action){string before=SaveGame.Serialize(s);Assert(!action().Success&&SaveGame.Serialize(s)==before,"Rejected planning command changed state.");}
    private static void Reject(GameState s)
    {bool rejected=false;try{SaveGame.Deserialize(SaveGame.Serialize(s));}catch(InvalidDataException){rejected=true;}Assert(rejected,"Invalid production plan accepted.");}
    public static void Run(Action<string,Action> test)
    {
        test("Future orders leave the current project, treasury and accumulated work intact",()=>
        {
            var s=Fresh();var c=s.Cities[0];c.Production=31;double gold=s.Player.Gold;Add(s,"granary");Add(s,"scout",true);
            Assert(c.Project=="treasury"&&c.Production==31&&s.Player.Gold==gold&&c.ProductionQueue.Select(o=>o.Project).SequenceEqual(new[]{"granary","scout"})&&c.ProductionQueue[1].Repeat,"Planning changed current work or lost its sequence.");
        });
        test("Automatic transitions preserve all overflow and finish only one project per city and turn",()=>
        {
            var s=Fresh();var c=s.Cities[0];c.Production=1000;Add(s,"granary");Add(s,"library");double output=Engine.CityYields(s,c).Production;
            Step(s);Assert(c.Project=="granary"&&c.ProductionQueue.Single().Project=="library"&&Math.Abs(c.Production-(1000+output-80))<1e-8&&!c.Buildings.Contains("granary"),"First completion discarded overflow or built twice.");
            Step(s);Assert(c.Buildings.Contains("granary")&&c.Project=="library"&&!c.Buildings.Contains("library")&&c.ProductionQueue.Count==0,"Next queued building did not retain its turn boundary.");
        });
        test("A planned unknown technology waits without producing and resumes when discovered",()=>
        {
            var s=Fresh();var c=s.Cities[0];Add(s,"university");c.Production=300;Engine.StartQueuedProject(s,c.Id);double held=c.Production;Step(s);
            Assert(c.Project=="university"&&c.Production==held&&!c.Buildings.Contains("university")&&ProductionPlanning.CurrentBlock(s,c)!.Contains("technologia"),"Unresearched project progressed or disappeared.");
            s.Player.Technologies.Add("education");Step(s);Assert(c.Buildings.Contains("university"),"Discovery did not unblock the planned project.");
        });
        test("Settlers wait for inhabitants rather than skipping later orders or borrowing population",()=>
        {
            var s=Fresh();var c=s.Cities[0];c.Population=2;c.Food=0;Add(s,"settler");Add(s,"library");c.Production=300;Engine.StartQueuedProject(s,c.Id);double held=c.Production;
            int settlers=s.Units.Count(u=>u.Owner==0&&u.Kind==UnitKind.Settler);Step(s);Assert(c.Project=="settler"&&c.Production==held&&c.ProductionQueue[0].Project=="library"&&s.Units.Count(u=>u.Owner==0&&u.Kind==UnitKind.Settler)==settlers,"Blocked settler borrowed inhabitants or skipped the queue.");
            c.Population=3;c.Food=0;Step(s);Assert(c.Population==2&&c.Project=="library"&&s.Units.Count(u=>u.Owner==0&&u.Kind==UnitKind.Settler)==settlers+1,"Settler did not consume exactly one inhabitant.");
        });
        test("A planned cavalry order remains blocked until the faction controls horses",()=>
        {
            var s=Fresh();var c=s.Cities[0];foreach(var t in s.Tiles)t.Resource=Resource.None;s.Player.Technologies.Add("riding");Add(s,"cavalry");c.Production=250;Engine.StartQueuedProject(s,c.Id);double held=c.Production;
            Step(s);Assert(c.Project=="cavalry"&&c.Production==held&&!s.Units.Any(u=>u.Owner==0&&u.Kind==UnitKind.Cavalry),"Cavalry ignored the resource requirement.");
            s.Tiles[c.Tile].Resource=Resource.Horses;Step(s);Assert(s.Units.Count(u=>u.Owner==0&&u.Kind==UnitKind.Cavalry)==1,"Horse control did not unblock recruitment.");
        });
        test("Repeating recruitment rotates behind other orders and retains barracks training",()=>
        {
            var s=Fresh();var c=s.Cities[0];c.Project="scout";c.Production=1000;c.Buildings.Add("barracks");Engine.SetProjectRepeat(s,c.Id,true);Add(s,"archer");
            int scouts=s.Units.Count(u=>u.Owner==0&&u.Kind==UnitKind.Scout);Step(s);
            Assert(c.Project=="archer"&&!c.RepeatProject&&c.ProductionQueue.Single().Project=="scout"&&c.ProductionQueue[0].Repeat,"Repeated recruitment jumped the waiting order.");
            Step(s);Assert(c.Project=="scout"&&c.RepeatProject&&s.Units.Any(u=>u.Owner==0&&u.Kind==UnitKind.Archer&&u.Experience==2),"Queue lost a repeating order or training.");
            Step(s);Assert(s.Units.Count(u=>u.Owner==0&&u.Kind==UnitKind.Scout)==scouts+2&&c.Project=="scout"&&c.RepeatProject&&c.ProductionQueue.Count==0,"A standalone repeating order stopped or duplicated itself.");
        });
        test("A repeating current order respects eight waiting slots through save and completion",()=>
        {
            var s=Fresh();var c=s.Cities[0];c.Project="scout";c.Production=1000;Engine.SetProjectRepeat(s,c.Id,true);for(int i=0;i<8;i++)Add(s,"spearmen");
            AtomicFailure(s,()=>Engine.EnqueueProject(s,c.Id,"archer"));Step(s);
            Assert(c.ProductionQueue.Count==8&&c.ProductionQueue[^1].Project=="scout"&&c.ProductionQueue[^1].Repeat,"Repeat overflowed or vanished from a full queue.");SaveGame.Deserialize(SaveGame.Serialize(s));
        });
        test("Repeating public works pay exactly once and disarming repetition restores normal fallback",()=>
        {
            var s=Fresh();var c=s.Cities[0];c.Production=1000;Engine.SetProjectRepeat(s,c.Id,true);double gold=s.Player.Gold,net=Engine.Income(s,0).Gold;
            Step(s);Assert(Math.Abs(s.Player.Gold-(gold+net+60))<1e-8&&c.Project=="treasury"&&c.RepeatProject,"Repeating public works duplicated a payment.");
            Engine.SetProjectRepeat(s,c.Id,false);Step(s);Assert(!c.RepeatProject&&c.Project!="treasury","Disarmed repeat did not use the normal next-building selection.");
        });
        test("Repeating settlers stop after reducing the city below the recruitment threshold",()=>
        {
            var s=Fresh();var c=s.Cities[0];c.Project="settler";c.Production=1000;c.Food=0;Engine.SetProjectRepeat(s,c.Id,true);Step(s);int count=s.Units.Count(u=>u.Owner==0&&u.Kind==UnitKind.Settler);double held=c.Production;c.Food=0;
            Step(s);Assert(c.Population==2&&c.Project=="settler"&&c.RepeatProject&&c.Production==held&&s.Units.Count(u=>u.Owner==0&&u.Kind==UnitKind.Settler)==count,"Repeating settlers consumed nonexistent population.");
        });
        test("Pausing preserves work and plans while food, inhabitants and treasury continue normally",()=>
        {
            var s=Fresh();var c=s.Cities[0];c.Production=1000;Add(s,"library");double food=c.Food,gold=s.Player.Gold;Engine.SetProductionPaused(s,c.Id,true);Step(s);
            Assert(c.Production==1000&&c.Project=="treasury"&&c.ProductionQueue.Single().Project=="library"&&c.Food>food&&s.Player.Gold!=gold,"Pause stopped the whole city or completed a project.");
            Engine.SetProductionPaused(s,c.Id,false);Step(s);Assert(c.Project=="library","Resumed production did not complete its held project.");
        });
        test("Resuming production cannot bypass an unmet technology requirement",()=>
        {
            var s=Fresh();var c=s.Cities[0];Add(s,"university");Engine.StartQueuedProject(s,c.Id);Engine.SetProductionPaused(s,c.Id,true);c.Production=1000;Engine.SetProductionPaused(s,c.Id,false);Step(s);
            Assert(!c.Buildings.Contains("university")&&c.Production==1000&&ProductionPlanning.CurrentBlock(s,c)!=null,"Resume bypassed the project requirements.");
        });
        test("Manual project replacement retains the historical loss but preserves the future plan and pause",()=>
        {
            var s=Fresh();var c=s.Cities[0];c.Production=100;Add(s,"library");Engine.SetProjectRepeat(s,c.Id,true);Engine.SetProductionPaused(s,c.Id,true);
            Assert(Engine.SetProject(s,c.Id,"scout").Success&&c.Production==80&&!c.RepeatProject&&c.ProductionPaused&&c.ProductionQueue.Single().Project=="library","Manual replacement silently changed the plan or avoided its cost.");
        });
        test("Starting the queue applies loss only when replacing a different current project",()=>
        {
            var s=Fresh();var c=s.Cities[0];c.Production=100;Add(s,"scout",true);Engine.SetProductionPaused(s,c.Id,true);Assert(Engine.StartQueuedProject(s,c.Id).Success&&c.Production==80&&c.RepeatProject&&c.ProductionPaused,"Immediate queued start has incorrect cost or mode.");
            Add(s,"scout");Assert(Engine.StartQueuedProject(s,c.Id).Success&&c.Production==80&&!c.RepeatProject&&c.ProductionQueue.Count==0,"Choosing the same project lost work or kept an obsolete repeat mode.");
        });
        test("Reordering and removing pending work do not alter production, cash or the active project",()=>
        {
            var s=Fresh();var c=s.Cities[0];c.Production=31;Add(s,"granary");Add(s,"library");Add(s,"scout");double gold=s.Player.Gold;
            Engine.MoveProductionOrder(s,c.Id,2,-1);Assert(c.ProductionQueue.Select(o=>o.Project).SequenceEqual(new[]{"granary","scout","library"}),"Move did not preserve other orders.");
            Engine.RemoveProductionOrder(s,c.Id,0);Assert(c.ProductionQueue.Select(o=>o.Project).SequenceEqual(new[]{"scout","library"})&&c.Production==31&&c.Project=="treasury"&&s.Player.Gold==gold,"Pending edits changed current work.");
        });
        test("Clearing pending orders leaves current production and its repetition intact",()=>
        {
            var s=Fresh();var c=s.Cities[0];c.Production=35;Add(s,"granary");Engine.SetProjectRepeat(s,c.Id,true);Engine.ClearProductionQueue(s,c.Id);
            Assert(c.ProductionQueue.Count==0&&c.Project=="treasury"&&c.RepeatProject&&c.Production==35,"Clearing pending orders cancelled active work.");AtomicFailure(s,()=>Engine.ClearProductionQueue(s,c.Id));
        });
        test("Permanent projects cannot repeat or be enqueued twice or after completion",()=>
        {
            var s=Fresh();var c=s.Cities[0];Add(s,"granary");AtomicFailure(s,()=>Engine.EnqueueProject(s,c.Id,"granary"));AtomicFailure(s,()=>Engine.EnqueueProject(s,c.Id,"library",true));
            c.Buildings.Add("library");AtomicFailure(s,()=>Engine.EnqueueProject(s,c.Id,"library"));c.Project="aqueduct";AtomicFailure(s,()=>Engine.EnqueueProject(s,c.Id,"aqueduct"));AtomicFailure(s,()=>Engine.SetProjectRepeat(s,c.Id,true));
        });
        test("Completed buildings left in a plan are skipped once rather than rebuilt or softlocking it",()=>
        {
            var s=Fresh();var c=s.Cities[0];Add(s,"granary");Add(s,"library");c.Buildings.Add("granary");c.Production=1000;
            SaveGame.Deserialize(SaveGame.Serialize(s));Step(s);Assert(c.Project=="library"&&c.ProductionQueue.Count==0&&s.Chronicle.Count(e=>e.Text.Contains("pominięto ukończony"))==1,"Stale building blocked the plan or was rebuilt.");
        });
        test("An already completed first queued building cannot be started manually for a second reward",()=>
        {
            var s=Fresh();var c=s.Cities[0];Add(s,"granary");c.Buildings.Add("granary");AtomicFailure(s,()=>Engine.StartQueuedProject(s,c.Id));
        });
        test("Invalid indexes, foreign cities, unknown projects and finished campaigns reject changes atomically",()=>
        {
            var s=Fresh();var c=s.Cities[0];Add(s,"scout");int foreign=s.Cities.First(c=>c.Owner==1).Id;
            AtomicFailure(s,()=>Engine.EnqueueProject(s,foreign,"scout"));AtomicFailure(s,()=>Engine.EnqueueProject(s,c.Id,"missing"));AtomicFailure(s,()=>Engine.MoveProductionOrder(s,c.Id,0,-1));
            AtomicFailure(s,()=>Engine.MoveProductionOrder(s,c.Id,0,2));AtomicFailure(s,()=>Engine.RemoveProductionOrder(s,c.Id,-1));AtomicFailure(s,()=>Engine.SetProjectRepeat(s,c.Id,true,5));
            AtomicFailure(s,()=>Engine.SetProductionPaused(s,c.Id,false));s.Ending="Koniec";AtomicFailure(s,()=>Engine.StartQueuedProject(s,c.Id));AtomicFailure(s,()=>Engine.ClearProductionQueue(s,c.Id));AtomicFailure(s,()=>Engine.SetProjectRepeat(s,c.Id,true));
        });
        test("Changing sovereignty clears inherited queue, repetition and pause during capture and peaceful return",()=>
        {
            foreach(bool peaceful in new[]{false,true})
            {
                var s=Fresh();var c=s.Cities[0];Add(s,"scout",true);Engine.SetProjectRepeat(s,c.Id,true);Engine.SetProductionPaused(s,c.Id,true);ProvincialPolitics.Transfer(s,c,1,peaceful);
                Assert(c.ProductionQueue.Count==0&&!c.RepeatProject&&!c.ProductionPaused,"New owner inherited the previous player's orders.");SaveGame.Deserialize(SaveGame.Serialize(s));
            }
        });
        test("Planning forecasts and repeated presentation never change accumulated work or order state",()=>
        {
            var s=Fresh();var c=s.Cities[0];Add(s,"university");Add(s,"settler",true);string before=SaveGame.Serialize(s);
            foreach(var order in c.ProductionQueue){ProductionPlanning.ProjectError(s,c,order.Project);ProductionPlanning.EnqueueError(s,c.Id,order.Project,order.Repeat);}ProductionPlanning.CurrentBlock(s,c);Markets.Plan(s);
            Assert(SaveGame.Serialize(s)==before,"Read-only planning changed the campaign.");
        });
        test("Paused, repeated and blocked queues continue deterministically after reload",()=>
        {
            var a=Fresh();var city=a.Cities[0];city.Production=1000;Add(a,"university");Add(a,"scout",true);Engine.SetProductionPaused(a,city.Id,true);var b=SaveGame.Deserialize(SaveGame.Serialize(a));
            for(int i=0;i<12;i++)
            {
                if(i==1){Engine.SetProductionPaused(a,city.Id,false);Engine.SetProductionPaused(b,city.Id,false);}
                if(i==5){a.Player.Technologies.Add("education");b.Player.Technologies.Add("education");}
                Step(a);Step(b);Assert(SaveGame.Serialize(a)==SaveGame.Serialize(b),"Production plan diverged after reload.");SaveGame.Deserialize(SaveGame.Serialize(a));
            }
        });
        test("V11 migration preserves an unanswered audit and active project without inventing queue orders",()=>
        {
            var s=StageElevenSpecs.Ready();var c=s.Cities[0];c.Production=37;var node=JsonNode.Parse(SaveGame.Serialize(s))!;node["Version"]=11;
            foreach(var city in node["Cities"]!.AsArray())foreach(string key in new[]{"ProductionQueue","RepeatProject","ProductionPaused"})city!.AsObject().Remove(key);
            string legacy=node.ToJsonString();var loaded=SaveGame.Deserialize(legacy);
            Assert(loaded.Version==12&&loaded.PendingAuditId==1&&loaded.Audits[0].AssessedGold==s.Audits[0].AssessedGold&&loaded.Cities[0].Production==37&&loaded.Cities[0].Project==c.Project&&loaded.Cities.All(v=>v.ProductionQueue.Count==0&&!v.RepeatProject&&!v.ProductionPaused),"Migration erased audit evidence, work or invented orders.");
            string path=Path.Combine(Environment.CurrentDirectory,"artifacts","migration-v11-test.crown");
            try{File.WriteAllText(path,legacy);SaveGame.Write(loaded,path);Assert(File.ReadAllText(path+".v11.bak")==legacy&&SaveGame.Read(path).Version==12,"V11 backup missing.");}
            finally{File.Delete(path);File.Delete(path+".v11.bak");}
        });
        test("Save validation rejects oversized, unknown, null and impossible repeating production plans",()=>
        {
            var s=Fresh();s.Cities[0].ProductionQueue=null!;Reject(s);s=Fresh();s.Cities[0].ProductionQueue.Add(null!);Reject(s);
            s=Fresh();s.Cities[0].ProductionQueue.Add(new(){Project="missing"});Reject(s);s=Fresh();for(int i=0;i<9;i++)s.Cities[0].ProductionQueue.Add(new(){Project="scout"});Reject(s);
            s=Fresh();s.Cities[0].ProductionQueue.Add(new(){Project="granary",Repeat=true});Reject(s);s=Fresh();s.Cities[0].ProductionQueue.AddRange([new(){Project="granary"},new(){Project="granary"}]);Reject(s);
            s=Fresh();s.Cities[0].Project="granary";s.Cities[0].RepeatProject=true;Reject(s);
        });
    }
}
