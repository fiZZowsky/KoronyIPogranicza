using Crown.Core;
using System.Diagnostics;

int passed=0;var timer=Stopwatch.StartNew();
void Assert(bool value,string message){if(!value)throw new Exception(message);}
void Test(string name,Action action){try{action();passed++;Console.WriteLine($"PASS {name}");}catch(Exception e){Console.Error.WriteLine($"FAIL {name}: {e}");Environment.ExitCode=1;}}
GameState Fresh(){var s=World.Create();Narrative.Resolve(s,0);return s;}
void Decide(GameState s)
{
    if(s.PendingEvent==null)return;var e=Narrative.Get(s.PendingEvent);int c=Array.FindIndex(e.Choices,x=>s.Player.Gold+x.Gold>=0);Assert(c>=0,"Every event needs an affordable option.");Assert(Narrative.Resolve(s,c).Success,"Resolution must succeed.");
}

Test("Seed determinism and distinct worlds",()=>
{
    Assert(SaveGame.Serialize(World.Create(44))==SaveGame.Serialize(World.Create(44)),"Same seed diverged.");
    Assert(!World.Create(44).Tiles.Select(t=>t.Terrain).SequenceEqual(World.Create(45).Tiles.Select(t=>t.Terrain)),"Seeds made identical maps.");
    foreach(int seed in new[]{0,-1,int.MinValue,int.MaxValue,17421,2026}){var s=World.Create(seed);Assert(s.Cities.All(c=>s.Tiles[c.Tile].Passable),"Invalid start.");SaveGame.Deserialize(SaveGame.Serialize(s));}
});
Test("Hex topology, symmetry, distance",()=>
{
    var s=Fresh();foreach(var t in s.Tiles)foreach(var n in World.Neighbors(s,t.Id)){Assert(World.Distance(t,n)==1,"Adjacent tile distance != 1");Assert(World.Neighbors(s,n.Id).Any(x=>x.Id==t.Id),"Asymmetric neighbor.");}Assert(World.Neighbors(s,15*s.Width+15).Count()==6,"Interior must have six neighbors.");
});
Test("Capital land connections across seeds",()=>
{
    foreach(int seed in new[]{0,-1,17421,2026,8,99,771})
    {
        var s=World.Create(seed);var reached=new HashSet<int>{s.Cities[0].Tile};var queue=new Queue<int>();queue.Enqueue(s.Cities[0].Tile);
        while(queue.TryDequeue(out int tile))foreach(var n in World.Neighbors(s,tile).Where(t=>t.Passable))if(reached.Add(n.Id))queue.Enqueue(n.Id);
        Assert(s.Cities.All(c=>reached.Contains(c.Tile)),"A capital is inaccessible without fleets.");
    }
});
Test("All events retain an affordable option and known effects",()=>
{
    foreach(var e in Narrative.All)
    {
        Assert(e.Choices.Any(c=>c.Gold>=0),e.Id+" can softlock an empty treasury.");
        for(int i=0;i<e.Choices.Length;i++){var s=Fresh();s.PendingEvent=e.Id;if(ProvincialEvents.IsLocal(e.Id))s.PendingCityId=s.Cities.First(c=>c.Owner==0).Id;s.Player.Gold=10000;Assert(!string.IsNullOrEmpty(Narrative.Effects(e.Choices[i],e.Id,i)),"Invisible consequence.");Assert(Narrative.Resolve(s,i).Success,"Invalid narrative branch.");SaveGame.Deserialize(SaveGame.Serialize(s));}
    }
});
Test("Council blocks turn and choices are irreversible",()=>
{
    var s=World.Create();Assert(!Engine.EndTurn(s).Success&&s.Turn==1,"Unresolved council advanced time.");Assert(!Narrative.Resolve(s,99).Success,"Accepted bad choice.");Assert(Narrative.Resolve(s,0).Success,"Founding failed.");Assert(!Narrative.Resolve(s,0).Success,"Repeated choice.");Assert(s.Player.Flags.Contains("land_charter")&&s.Deferred.Count==1,"No lasting effect.");
});
Test("Promises mature once and survive save/load",()=>
{
    var s=Fresh();s=SaveGame.Deserialize(SaveGame.Serialize(s));for(int i=0;i<10;i++){Decide(s);Engine.EndTurn(s);}Assert(s.Chronicle.Count(e=>e.Category=="konsekwencja"&&e.Text.Contains("Karta ziemska"))==1,"Promise lost or repeated.");
    Assert(s.Player.Flags.Contains("land_charter"),"Flag disappeared.");s.Turn=35;s.PendingEvent=null;Narrative.Schedule(s);Assert(s.PendingEvent=="land_echo","Founding did not change future dialogue.");
});
Test("Unaffordable choices make no mutation",()=>
{
    var s=World.Create();s.Player.Gold=0;var json=SaveGame.Serialize(s);Assert(!Narrative.Resolve(s,0).Success,"Accepted debt.");Assert(SaveGame.Serialize(s)==json,"Rejected action changed state.");Assert(Narrative.Resolve(s,1).Success,"No affordable fallback.");
});
Test("Technology prerequisites and retained science",()=>
{
    var s=Fresh();Assert(!Engine.SetResearch(s,"industry").Success,"Skipped prerequisites.");s.Player.Science=30;Assert(Engine.SetResearch(s,"mining").Success&&s.Player.Science==30,"Science reset.");s.Player.Science=200;Engine.EndTurn(s);Assert(s.Player.Technologies.Contains("mining"),"Research did not unlock.");
});
Test("Production, population cost, improvements",()=>
{
    var s=Fresh();var c=s.Cities.First(c=>c.Owner==0);s.Player.Technologies.Add("agriculture");Assert(Engine.SetProject(s,c.Id,"settler").Success,"Cannot recruit settler.");c.Production=100;c.Food=0;int pop=c.Population;Engine.EndTurn(s);Assert(c.Population==pop-1&&s.Units.Any(u=>u.Owner==0&&u.Kind==UnitKind.Settler),"Settler did not consume population.");
    var tile=Engine.WorkedTiles(s,c).First(t=>t.Improvement==Improvement.None);var before=Engine.CityYields(s,c);s.Player.Gold=200;Assert(Engine.Improve(s,tile.Id,Improvement.Farm).Success,"Farm failed.");Assert(Engine.CityYields(s,c).Food>before.Food,"Farm has no benefit.");Assert(!Engine.Improve(s,tile.Id,Improvement.Mine).Success,"Double improvement.");
});
Test("Winter, taxation and institutions affect the economy",()=>
{
    var s=Fresh();var c=s.Cities[0];s.Turn=2;double summer=Engine.CityYields(s,c).Food;s.Turn=4;Assert(Engine.CityYields(s,c).Food<summer,"Winter has no cost.");s.Player.Tax=0;double low=Engine.Income(s,0).Gold;s.Player.Tax=2;Assert(Engine.Income(s,0).Gold>low,"Taxes have no effect.");s.Player.Tax=1;double science=Engine.Income(s,0).Science;c.Buildings.Add("library");Assert(Engine.Income(s,0).Science>science,"Library has no effect.");
});
Test("Policy cooldown and social cost",()=>
{
    var s=Fresh();int commons=s.Player.Commons;Assert(Engine.SetPolicy(s,2,1,1).Success,"Reform failed.");Assert(s.Player.Commons<commons,"No social cost.");Assert(!Engine.SetPolicy(s,0,0,0).Success,"Cooldown missing.");s.Turn++;int scholars=s.Player.Scholars;Assert(Engine.SetPolicy(s,0,1,1).Success&&s.Player.Scholars==scholars,"Unchanged academic policy granted repeatable support.");
});
Test("Movement spends terrain cost and respects water",()=>
{
    var s=Fresh();var u=s.Units.First(u=>u.Owner==0);var n=World.Neighbors(s,u.Tile).First(t=>t.Owner==0);n.Terrain=Terrain.Forest;n.Explored=true;u.Actions=1;Assert(!Engine.Move(s,u.Id,n.Id).Success,"Forest costs too little.");u.Actions=2;Assert(Engine.Move(s,u.Id,n.Id).Success&&u.Actions==0,"Incorrect movement cost.");
    var water=World.Neighbors(s,u.Tile).First(t=>!s.Cities.Any(c=>c.Tile==t.Id));water.Terrain=Terrain.Ocean;water.Explored=true;u.Actions=2;Assert(!Engine.Move(s,u.Id,water.Id).Success,"Crossed ocean.");
});
Test("Combat requires war; ranged units cannot capture",()=>
{
    var s=Fresh();var c=s.Cities.First(c=>c.Owner==1);var near=World.Neighbors(s,c.Tile).First(t=>t.Passable);var u=Engine.AddUnit(s,0,near.Id,UnitKind.Archer);s.Tiles[c.Tile].Explored=true;
    Assert(!Engine.Move(s,u.Id,c.Tile).Success,"Attack during peace.");s.Units.RemoveAll(x=>x.Owner==1&&x.Tile==c.Tile);s.Relation(0,1).War=true;c.Health=1;Assert(Engine.Move(s,u.Id,c.Tile).Success&&c.Owner==1&&c.Health==0,"Ranged attack must reduce city to zero without capturing.");
    s.Units.RemoveAll(x=>x.Owner==1&&x.Tile==c.Tile);u.Kind=UnitKind.Spearmen;u.Health=100;u.Actions=2;Assert(Engine.Move(s,u.Id,c.Tile).Success&&c.Owner==0,"Melee cannot capture defeated city.");
});
Test("Supply limits and healing",()=>
{
    var s=Fresh();var u=s.Units.First(u=>u.Owner==0);u.Tile=s.Tiles.First(t=>t.Passable&&s.Cities.Where(c=>c.Owner==0).All(c=>World.Distance(t,s.Tiles[c.Tile])>10)).Id;u.Health=80;u.Actions=0;Assert(!Engine.Supplied(s,u),"Remote army supplied.");double strength=Engine.Strength(s,u);Engine.EndTurn(s);Assert(u.Health<80,"No attrition.");u.Tile=s.Cities[0].Tile;Assert(Engine.Supplied(s,u)&&Engine.Strength(s,u)>strength,"Supply does not affect strength.");u.Health=60;u.Actions=u.MaxActions;Decide(s);Engine.EndTurn(s);Assert(u.Health>60,"Resting does not heal.");
});
Test("Diplomacy, trade income, cooldown and truce",()=>
{
    var s=Fresh();double before=Engine.Income(s,0).Gold;Assert(Engine.Diplomacy(s,1,"trade").Success&&Engine.Income(s,0).Gold>before,"Trade ineffective.");Assert(!Engine.Diplomacy(s,1,"war").Success,"Diplomacy cooldown missing.");s.Turn++;Assert(Engine.Diplomacy(s,1,"war").Success&&!s.Relation(0,1).Trade,"War preserves trade.");s.Turn++;s.Player.Gold=500;s.Factions[1].WarWeariness=100;Assert(Engine.Diplomacy(s,1,"peace").Success&&s.Relation(0,1).War,"Peace proposal must travel.");s.Turn+=2;PeaceCouncil.Tick(s);Assert(!s.Relation(0,1).War&&!Engine.Diplomacy(s,1,"war").Success,"Treaty or truce missing.");
});
Test("Settling validates distance and creates territory",()=>
{
    var s=Fresh();var c=s.Cities[0];var u=Engine.AddUnit(s,0,c.Tile,UnitKind.Settler);Assert(!Engine.FoundCity(s,u.Id).Success,"Settled on city.");var target=s.Tiles.First(t=>t.Passable&&t.Owner<0&&s.Cities.All(c=>World.Distance(t,s.Tiles[c.Tile])>=4));u.Tile=target.Id;Assert(Engine.FoundCity(s,u.Id).Success&&target.Owner==0&&!s.Units.Contains(u),"Settlement did not consume settler.");
});
Test("Save round trip and deterministic continuation",()=>
{
    var s=Fresh();for(int i=0;i<22;i++){Decide(s);Engine.EndTurn(s);}string json=SaveGame.Serialize(s);var loaded=SaveGame.Deserialize(json);Assert(SaveGame.Serialize(loaded)==json,"Save changed state.");for(int i=0;i<15;i++){Decide(s);Decide(loaded);Engine.EndTurn(s);Engine.EndTurn(loaded);}Assert(SaveGame.Serialize(s)==SaveGame.Serialize(loaded),"Continuation diverged.");
});
Test("Malformed save is rejected",()=>
{
    var s=Fresh();s.Tiles[0].Owner=100;bool rejected=false;try{SaveGame.Deserialize(SaveGame.Serialize(s));}catch(InvalidDataException){rejected=true;}Assert(rejected,"Accepted invalid owner.");s=Fresh();s.Relations.Clear();rejected=false;try{SaveGame.Deserialize(SaveGame.Serialize(s));}catch(InvalidDataException){rejected=true;}Assert(rejected,"Accepted missing diplomacy.");
});
Test("Scientific, cultural and turn-limit endings",()=>
{
    var s=Fresh();s.Cities[0].Buildings.Add("atlas");Engine.CheckEnding(s);Assert(s.Ending?.Contains("naukowe")==true,"No science victory.");Assert(!Engine.EndTurn(s).Success,"Ending not final.");s=Fresh();s.Player.Culture=Content.CultureVictory;s.Player.Prestige=Content.PrestigeVictory;Engine.CheckEnding(s);Assert(s.Ending?.Contains("kulturowe")==true,"No cultural victory.");s=Fresh();s.Turn=321;Engine.CheckEnding(s);Assert(s.Ending?.Contains("Koniec epoki")==true,"No score ending.");
});
Test("Campaign soak: multiple seeds, complete campaigns and AI expansion",()=>
{
    foreach(int seed in new[]{17421,2026,8,99})
    {
        var s=World.Create(seed);int turns=0,maxCities=4;
        while(s.Ending==null&&turns<321)
        {
            Decide(s);Assert(Engine.EndTurn(s).Success,"Turn failed.");turns++;maxCities=Math.Max(maxCities,s.Cities.Count);
            Assert(s.Factions.All(f=>double.IsFinite(f.Gold)&&f.Gold>=0&&f.Stability>=0&&f.Stability<=100),"Invalid economy.");
            Assert(s.Units.All(u=>u.Health>0&&u.Health<=100&&s.Tiles[u.Tile].Passable),"Invalid army.");
            if(turns%50==0)s=SaveGame.Deserialize(SaveGame.Serialize(s));
        }
        Console.WriteLine($"  seed {seed}: {turns} turns, {maxCities} cities, {s.Units.Count} units; {s.Ending}");Assert(s.Ending!=null,"Campaign did not end.");Assert(maxCities>4,"AI never founded a city.");
    }
});
StageTwoSpecs.Run(Test);
StageThreeSpecs.Run(Test);
StageFourSpecs.Run(Test);
StageFiveSpecs.Run(Test);
StageSixSpecs.Run(Test);
StageSevenSpecs.Run(Test);
Console.WriteLine($"{passed} specifications passed in {timer.Elapsed.TotalSeconds:0.0}s.");
