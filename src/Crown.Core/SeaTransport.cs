namespace Crown.Core;

public static class SeaTransport
{
    public static bool Active(Unit u) => u.EmbarkedOnId == null;
    public static IEnumerable<Unit> Passengers(GameState s,Unit ship) => s.Units.Where(u=>u.EmbarkedOnId==ship.Id).OrderBy(u=>u.Id);
    public static int Weight(Unit u) => u.Kind is UnitKind.Cavalry or UnitKind.Catapult ? 2 : 1;
    public static int Capacity(Unit ship){if(!Navy.IsShip(ship))return 0;int era=UnitModels.For(ship)?.Era??2;return era<6?1:era<10?2:4;}
    public static Unit? Carrier(GameState s,Unit unit)=>s.Units.FirstOrDefault(u=>u.Id==unit.EmbarkedOnId&&Navy.IsShip(u)&&u.Owner==unit.Owner);
    public static int Used(GameState s,Unit ship) => Passengers(s,ship).Sum(Weight);
    public static string? BoardError(GameState s,Unit? unit,Unit? ship,int owner=0)
    {
        if(s.Ending!=null||!s.EpochCampaign||owner is <0 or >3||unit==null||ship==null||unit.Owner!=owner||ship.Owner!=owner||Navy.IsShip(unit)||!Navy.IsShip(ship)||!Active(unit)||!Active(ship))return "Wybierz własny oddział lądowy i własny okręt.";
        if(unit.EscortCaravanId!=null)return "Najpierw odwołaj eskortę karawany.";
        if(unit.Actions<1||ship.Actions<1)return "Załadunek wymaga 1 punktu ruchu oddziału i okrętu.";
        if(!s.Cities.Any(c=>c.Owner==owner&&c.Tile==unit.Tile&&Navy.ActivePort(s,c)&&World.Distance(s.Tiles[c.Tile],s.Tiles[ship.Tile])==1))return "Oddział musi przebywać w czynnym własnym porcie, a okręt na sąsiedniej wodzie.";
        return Used(s,ship)+Weight(unit)>Capacity(ship)?"Brak miejsca na pokładzie. Jazda i artyleria zajmują dwa miejsca.":null;
    }
    public static Result Embark(GameState s,int unitId,int shipId,int owner=0)
    {
        var unit=s.Units.FirstOrDefault(u=>u.Id==unitId);var ship=s.Units.FirstOrDefault(u=>u.Id==shipId);
        if(BoardError(s,unit,ship,owner) is{} error)return Result.Fail(error);
        unit!.EmbarkedOnId=ship!.Id;unit.Tile=ship.Tile;unit.Actions=0;unit.Destination=null;MilitaryOrders.Clear(unit);
        ship.Actions-=1;ship.Destination=null;MilitaryOrders.Clear(ship);s.InvalidateLogistics();Engine.Explore(s);
        if(owner==0)s.Log($"{UnitModels.Name(unit)} zaokrętowano na {UnitModels.Name(ship)}. Ładunek {Used(s,ship)}/{Capacity(ship)}.","flota");
        return Result.Ok("Oddział jest na pokładzie. Załadunek zużył 1 punkt ruchu okrętu; oddział zakończył działania.");
    }
    public static string? LandError(GameState s,Unit? unit,int tile,int owner=0)
    {
        var ship=unit?.EmbarkedOnId is int id?s.Units.FirstOrDefault(u=>u.Id==id&&Navy.IsShip(u)):null;
        if(s.Ending!=null||owner is <0 or >3||unit==null||ship==null||unit.Owner!=owner||ship.Owner!=owner||tile<0||tile>=s.Tiles.Count)return "Wybierz własny przewożony oddział i pole lądowania.";
        if(ship.Actions<1)return "Wyładunek wymaga 1 punktu ruchu okrętu.";
        var t=s.Tiles[tile];if(owner==0&&!t.Explored)return "Najpierw odkryj brzeg.";
        if(!t.Passable||World.Distance(t,s.Tiles[ship.Tile])!=1)return "Oddział może zejść tylko na sąsiednie pole lądu.";
        if(t.Owner>=0&&t.Owner!=owner&&(!Engine.AtWar(s,owner,t.Owner)||unit.Kind==UnitKind.Settler))return "Ten brzeg jest za obcą granicą. Osadnicy lądują tylko na własnej lub neutralnej ziemi.";
        if(s.Units.Any(u=>Active(u)&&u.Owner!=owner&&u.Tile==tile)||s.Cities.Any(c=>c.Owner!=owner&&c.Tile==tile))return "Najpierw oczyść miejsce lądowania z obcych wojsk lub miasta.";
        return null;
    }
    public static Result Disembark(GameState s,int unitId,int tile,int owner=0)
    {
        var unit=s.Units.FirstOrDefault(u=>u.Id==unitId);if(LandError(s,unit,tile,owner) is{} error)return Result.Fail(error);
        var ship=s.Units.Single(u=>u.Id==unit!.EmbarkedOnId);ship.Actions-=1;ship.Destination=null;MilitaryOrders.Clear(ship);
        unit!.EmbarkedOnId=null;unit.Tile=tile;unit.Actions=0;unit.Destination=null;MilitaryOrders.Clear(unit);s.InvalidateLogistics();Engine.Explore(s);
        if(owner==0)s.Log($"{UnitModels.Name(unit)} wysadzono na brzegu ({s.Tiles[tile].X}, {s.Tiles[tile].Y}). Działania od następnej tury.","flota");
        return Result.Ok("Oddział wylądował. Zacznie działać w następnej turze; okręt zużył 1 punkt ruchu.");
    }
    internal static void Sync(GameState s,Unit ship){if(Navy.IsShip(ship))foreach(var unit in Passengers(s,ship))unit.Tile=ship.Tile;}
    internal static double Sink(GameState s,Unit ship,string cause="Zatopienie")
    {
        var cargo=Passengers(s,ship).ToArray();double loss=cargo.Sum(u=>u.Health);foreach(var unit in cargo)s.Units.Remove(unit);
        if(cargo.Length>0&&(ship.Owner==0||Engine.Visible(s).Contains(ship.Tile)))s.Log($"{cause} {UnitModels.Name(ship)}: utracono {cargo.Length} przewożonych oddziałów.","wojna");
        return loss;
    }
    internal static void Withdraw(GameState s,Unit ship,int other)
    {
        var search=Navigation.SearchFrom(s,ship.Tile,t=>Navy.TerrainAllowed(s,ship,t)&&!s.Units.Any(u=>Active(u)&&u.Tile==t.Id&&u.Owner!=ship.Owner&&u.Owner!=other),(_,_)=>1);
        int refuge=search.Costs.Keys.Where(t=>s.Tiles[t].Owner!=other&&(s.Tiles[t].Owner<0||s.Tiles[t].Owner==ship.Owner||Engine.AtWar(s,ship.Owner,s.Tiles[t].Owner))&&(ship.Owner!=0||s.Tiles[t].Explored))
            .OrderBy(t=>s.Cities.Any(c=>c.Owner==ship.Owner&&c.Buildings.Contains(Navy.Port)&&World.Distance(s.Tiles[c.Tile],s.Tiles[t])==1)?0:1).ThenBy(t=>search.Costs[t]).ThenBy(t=>t).DefaultIfEmpty(-1).First();
        if(refuge<0){Sink(s,ship,"Brak miejsca wycofania dla");s.Units.Remove(ship);return;}
        ship.Tile=refuge;ship.Destination=null;ship.Actions=0;MilitaryOrders.Clear(ship);Sync(s,ship);
    }
    internal static void Validate(GameState s)
    {
        if(s.Version<22&&s.Units.Any(u=>u.EmbarkedOnId!=null))throw new InvalidDataException("Starszy format nie zawiera transportowanych oddziałów.");
        foreach(var u in s.Units.Where(u=>u.EmbarkedOnId!=null))
        {
            var ship=s.Units.FirstOrDefault(v=>v.Id==u.EmbarkedOnId);
            if(ship==null||ship==u||!Navy.IsShip(ship)||!Active(ship)||Navy.IsShip(u)||u.Owner!=ship.Owner||u.Tile!=ship.Tile||u.Actions!=0||u.Destination!=null||u.Order!=MilitaryOrder.None||u.EscortCaravanId!=null)
                throw new InvalidDataException("Uszkodzony manifest transportu morskiego.");
        }
        if(s.Units.Any(u=>!Navy.IsShip(u)&&Navy.Water(s.Tiles[u.Tile])&&Active(u))||s.Units.Where(Navy.IsShip).Any(ship=>Used(s,ship)>Capacity(ship)))throw new InvalidDataException("Nieprawidłowa pojemność lub oddział na wodzie.");
    }
    internal static int? Target(GameState s,Unit ship,Unit cargo,Navigation.Search route)
    {
        var own=s.Cities.Where(c=>c.Owner==ship.Owner).ToArray();if(own.Length==0)return null;
        var hostile=s.Cities.Where(c=>Engine.AtWar(s,ship.Owner,c.Owner)).ToArray();
        return s.Tiles.Where(t=>t.Passable&&(t.Owner<0||t.Owner==ship.Owner||cargo.Kind!=UnitKind.Settler&&Engine.AtWar(s,ship.Owner,t.Owner)))
            .Where(t=>!s.Units.Any(u=>Active(u)&&u.Owner!=ship.Owner&&u.Tile==t.Id)&&!s.Cities.Any(c=>c.Owner!=ship.Owner&&c.Tile==t.Id))
            .Where(t=>cargo.Kind==UnitKind.Settler?s.Cities.All(c=>World.Distance(t,s.Tiles[c.Tile])>=5):hostile.Any(c=>World.Distance(t,s.Tiles[c.Tile])<=2))
            .Select(t=>(Tile:t,Cost:World.Neighbors(s,t.Id).Where(Navy.Water).Select(n=>route.Costs.GetValueOrDefault(n.Id,double.PositiveInfinity)).DefaultIfEmpty(double.PositiveInfinity).Min()))
            .Where(x=>double.IsFinite(x.Cost))
            .OrderBy(x=>x.Cost-(cargo.Kind==UnitKind.Settler?Engine.TileYields(x.Tile,s.Factions[ship.Owner]).Food*1.5+Engine.TileYields(x.Tile,s.Factions[ship.Owner]).Production:0))
            .ThenBy(x=>x.Tile.Id).Select(x=>(int?)x.Tile.Id).FirstOrDefault();
    }
}

public static partial class Engine
{
    public static void RunTransportAi(GameState s,int owner)
    {
        if(!s.EpochCampaign||s.Ending!=null||owner is <1 or >3)return;
        foreach(var ship in s.Units.Where(u=>u.Owner==owner&&Navy.IsShip(u)).OrderBy(u=>u.Id).ToArray())
        {
            if(ship.Actions<1)continue;var route=Navigation.ForUnit(s,ship);
            if(ship.Health>=70&&!SeaTransport.Passengers(s,ship).Any())
            {
                var port=s.Cities.FirstOrDefault(c=>c.Owner==owner&&Navy.ActivePort(s,c)&&World.Distance(s.Tiles[c.Tile],s.Tiles[ship.Tile])==1);
                if(port!=null)
                foreach(var candidate in s.Units.Where(u=>u.Owner==owner&&u.Tile==port.Tile&&SeaTransport.Active(u)&&!Navy.IsShip(u)&&u.Health>=65).OrderBy(u=>u.Kind==UnitKind.Settler?0:1).ThenBy(u=>u.Id).ToArray())
                {
                    if(candidate.Kind==UnitKind.Scout||candidate.Kind==UnitKind.Settler&&s.Cities.Count(c=>c.Owner==owner)>=4)continue;
                    if(candidate.Kind!=UnitKind.Settler&&s.Units.Count(u=>u.Owner==owner&&SeaTransport.Active(u)&&MilitaryStrategy.Combatant(u)&&World.Distance(s.Tiles[u.Tile],s.Tiles[port.Tile])<=2)<=1)continue;
                    if(SeaTransport.BoardError(s,candidate,ship,owner)==null&&SeaTransport.Target(s,ship,candidate,route)!=null)SeaTransport.Embark(s,candidate.Id,ship.Id,owner);
                }
            }
            var cargo=SeaTransport.Passengers(s,ship).FirstOrDefault();if(cargo==null||ship.Actions<1)continue;
            bool retreat=ship.Health<45||!Supplied(s,ship)&&ship.Health<70;
            int? target=retreat?s.Cities.Where(c=>c.Owner==owner&&Navy.ActivePort(s,c)).Select(c=>(City:c,Cost:World.Neighbors(s,c.Tile).Where(Navy.Water).Select(t=>route.Costs.GetValueOrDefault(t.Id,double.PositiveInfinity)).DefaultIfEmpty(double.PositiveInfinity).Min())).Where(x=>double.IsFinite(x.Cost)).OrderBy(x=>x.Cost).ThenBy(x=>x.City.Id).Select(x=>(int?)x.City.Tile).FirstOrDefault():SeaTransport.Target(s,ship,cargo,route);if(target==null)continue;
            int sea=World.Neighbors(s,target.Value).Where(Navy.Water).OrderBy(t=>route.Costs.GetValueOrDefault(t.Id,double.PositiveInfinity)).ThenBy(t=>t.Id).Select(t=>t.Id).First();
            foreach(int next in route.PathTo(sea).Skip(1)){if(Navigation.MovementCost(s,ship.Tile,next)>ship.Actions)break;EnterTile(s,ship,next);}
            if(World.Distance(s.Tiles[ship.Tile],s.Tiles[target.Value])==1)
                foreach(var passenger in SeaTransport.Passengers(s,ship).ToArray())if(SeaTransport.LandError(s,passenger,target.Value,owner)==null)SeaTransport.Disembark(s,passenger.Id,target.Value,owner);
        }
    }
}
