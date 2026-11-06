namespace Crown.Core;

public sealed record SupplyRaid(int CityId,double Destroyed,double Loot,string? Error=null);

public static class FieldLogistics
{
    public const string Depot="military_depot";
    public const int ReserveLimit=3;
    public static bool Eligible(Unit u)=>u.Kind is UnitKind.Spearmen or UnitKind.Archer or UnitKind.Cavalry or UnitKind.Catapult;
    public static int Weight(Unit u)=>u.Kind is UnitKind.Cavalry or UnitKind.Catapult?2:1;
    public static double Capacity(GameState s,City c)=>80+s.Factions[c.Owner].Era*8;
    public static bool HasReserve(GameState s,Unit u)=>s.EpochCampaign&&Eligible(u)&&SeaTransport.Active(u)&&u.SupplyReserve>0;
    public static bool Active(GameState s,City c)=>s.EpochCampaign&&c.Buildings.Contains(Depot);
    public static string? ProjectError(GameState s,string project)=>project==Depot&&!s.EpochCampaign?"Magazyn wojskowy wymaga pełnej kampanii epok.":null;
    public static double Upkeep(GameState s,int owner)=>s.Cities.Count(c=>c.Owner==owner&&Active(s,c))*2;
    public static double Allocation(GameState s,City c,double available)
    {
        if(!Active(s,c)||c.ProductionPaused||c.SupplyAllocation==0||SiegeWarfare.Report(s,c).Encircled)return 0;
        return Math.Min(Math.Max(0,available)*c.SupplyAllocation/100.0,Math.Max(0,Capacity(s,c)-c.MilitarySupplies)*2);
    }
    internal static double BuildPhase(GameState s,City c,double available)
    {
        double work=Allocation(s,c,available);
        c.MilitarySupplies=Math.Min(Capacity(s,c),c.MilitarySupplies+work/2);return work;
    }
    public static string? PolicyError(GameState s,City? c,int allocation,int owner=0)
    {
        if(s.Ending!=null||owner is <0 or >3||c==null||c.Owner!=owner||!Active(s,c)||allocation is not (0 or 25 or 50))return "Wybierz własny magazyn i nakład 0%, 25% lub 50%.";
        if(c.SupplyAllocation==allocation)return "Ten nakład już obowiązuje.";
        return c.LastSupplyPolicyTurn==s.Turn?"Zmieniono już nakład zaopatrzenia w tej turze.":null;
    }
    public static Result SetAllocation(GameState s,int cityId,int allocation,int owner=0)
    {
        var c=s.Cities.FirstOrDefault(c=>c.Id==cityId);if(PolicyError(s,c,allocation,owner) is{} error)return Result.Fail(error);
        c!.SupplyAllocation=allocation;c.LastSupplyPolicyTurn=s.Turn;
        if(owner==0)s.Log($"{c.Name}: {allocation}% pozostałej po dzielnicach produkcji przeznaczono na zaopatrzenie wojskowe. 2 pracy tworzy 1 pakiet.","zaopatrzenie");
        return Result.Ok("Nakład zmieniony. Zapasy powstaną dopiero w zwykłej fazie gospodarki.");
    }
    public static IEnumerable<City> NearbyDepots(GameState s,Unit u)
    {
        if(!s.EpochCampaign||!Eligible(u)||!SeaTransport.Active(u))return [];
        var candidates=s.Cities.Where(c=>c.Owner==u.Owner&&Active(s,c)&&World.Distance(s.Tiles[c.Tile],s.Tiles[u.Tile])<=2).OrderBy(c=>World.Distance(s.Tiles[c.Tile],s.Tiles[u.Tile])).ThenBy(c=>c.Id).ToArray();
        if(candidates.Length==0)return [];
        var path=Navigation.SearchFrom(s,u.Tile,t=>t.Passable&&(t.Owner<0||t.Owner==u.Owner||Engine.AtWar(s,u.Owner,t.Owner))&&
            !s.Units.Any(e=>SeaTransport.Active(e)&&e.Owner!=u.Owner&&e.Tile==t.Id)&&!s.Cities.Any(c=>c.Owner!=u.Owner&&c.Tile==t.Id),(_,_)=>1,2);
        return candidates.Where(c=>path.Costs.ContainsKey(c.Tile)).ToArray();
    }
    public static string? RefillError(GameState s,Unit? u,int owner=0)
    {
        if(s.Ending!=null||owner is <0 or >3||u==null||u.Owner!=owner||!s.EpochCampaign||!Eligible(u)||!SeaTransport.Active(u))return "Wybierz własny lądowy oddział bojowy poza pokładem.";
        if(u.EscortCaravanId!=null)return "Najpierw odwołaj eskortę.";
        if(u.Actions<1)return "Załadunek zapasów wymaga 1 punktu ruchu.";
        if(u.SupplyReserve>=ReserveLimit)return "Oddział ma pełne zapasy na trzy odcięte tury.";
        return NearbyDepots(s,u).Any(c=>c.MilitarySupplies+1e-8>=Weight(u))?null:"Potrzebny własny magazyn z zapasami do dwóch przechodnich kroków lądem.";
    }
    private static int Draw(GameState s,Unit u)
    {
        int loaded=0,weight=Weight(u);
        foreach(var c in NearbyDepots(s,u))
        {
            int amount=Math.Min(ReserveLimit-u.SupplyReserve,(int)Math.Floor((c.MilitarySupplies+1e-8)/weight));
            if(amount<=0)continue;c.MilitarySupplies=Math.Max(0,c.MilitarySupplies-amount*weight);u.SupplyReserve+=amount;loaded+=amount;
            if(u.SupplyReserve==ReserveLimit)break;
        }
        return loaded;
    }
    public static Result Refill(GameState s,int unitId,int owner=0)
    {
        var u=s.Units.FirstOrDefault(u=>u.Id==unitId);if(RefillError(s,u,owner) is{} error)return Result.Fail(error);
        int amount=Draw(s,u!);u!.Actions-=1;u.Destination=null;MilitaryOrders.Clear(u);s.InvalidateLogistics();
        if(owner==0)s.Log($"{UnitModels.Name(u)}: załadowano {amount} tur zapasów, rezerwa {u.SupplyReserve}/3. Zużyto {amount*Weight(u)} pakietów z magazynów.","zaopatrzenie");
        return Result.Ok("Zapasy załadowane. Zużyto 1 punkt ruchu; wcześniejszy marsz i umocnienia odwołano.");
    }
    internal static void Tick(GameState s)
    {
        if(!s.EpochCampaign)return;
        foreach(var u in s.Units.Where(u=>Eligible(u)&&SeaTransport.Active(u)).OrderBy(u=>u.Owner).ThenBy(u=>u.Id))
        {
            if(Logistics.HasSupplyLine(s,u)){if(u.EscortCaravanId==null)Draw(s,u);}
            else if(u.SupplyReserve>0){u.SupplyReserve--;if(u.Owner==0&&u.SupplyReserve==0)s.Log($"{UnitModels.Name(u)}: skończyły się zapasy marszowe. Przywróć dostawy; brak zapasów osłabia siłę i powoduje straty.","zaopatrzenie");}
        }
    }
    public static SupplyRaid PreviewRaid(GameState s,Unit? u,int owner=0)
    {
        SupplyRaid Invalid(string error)=>new(-1,0,0,error);
        if(s.Ending!=null||owner is <0 or >3||u==null||u.Owner!=owner||!s.EpochCampaign||!Eligible(u)||!SeaTransport.Active(u)||u.EscortCaravanId!=null||u.Actions<1)return Invalid("Potrzebny własny gotowy oddział bojowy bez eskorty.");
        var t=s.Tiles[u.Tile];int victim=t.RoadOwner;
        if(!t.Road||victim<0||!Engine.AtWar(s,owner,victim))return Invalid("Napad wymaga wrogiej drogi podczas wojny.");
        if(s.Units.Any(e=>SeaTransport.Active(e)&&e.Owner!=owner&&e.Tile==u.Tile))return Invalid("Najpierw usuń obce wojska z pola napadu.");
        var nearby=s.Cities.Where(c=>c.Owner==victim&&World.Distance(t,s.Tiles[c.Tile])<=3).ToArray();
        if(nearby.Length==0)return Invalid("Brak widocznego magazynu z dostępną drogą.");
        var sight=MilitaryStrategy.Sight(s,owner);
        var candidates=nearby.Where(c=>sight.Contains(c.Tile)&&Active(s,c)&&c.MilitarySupplies>=1).OrderBy(c=>World.Distance(t,s.Tiles[c.Tile])).ThenBy(c=>c.Id).ToArray();
        if(candidates.Length==0)return Invalid("Brak magazynu z zapasami w zasięgu trzech połączonych odcinków drogi.");
        var path=Navigation.SearchFrom(s,u.Tile,x=>x.Passable&&(x.Owner<0||x.Owner==victim)&&
            (x.Road&&x.RoadOwner==victim||s.Cities.Any(c=>c.Owner==victim&&c.Tile==x.Id))&&
            !s.Units.Any(e=>SeaTransport.Active(e)&&e.Id!=u.Id&&e.Owner!=owner&&e.Tile==x.Id),(_,_)=>1,3);
        var city=candidates.FirstOrDefault(c=>path.Costs.ContainsKey(c.Tile));
        if(city==null)return Invalid("Droga do magazynu jest przerwana lub osłaniana przez obrońców.");
        double destroyed=Math.Min(20,city.MilitarySupplies);return new(city.Id,destroyed,Math.Floor(destroyed/2));
    }
    public static Result Raid(GameState s,int unitId,int owner=0)
    {
        var u=s.Units.FirstOrDefault(u=>u.Id==unitId);var raid=PreviewRaid(s,u,owner);if(raid.Error is{} error)return Result.Fail(error);
        var c=s.Cities.Single(c=>c.Id==raid.CityId);c.MilitarySupplies=Math.Max(0,c.MilitarySupplies-raid.Destroyed);s.Factions[owner].Gold+=raid.Loot;
        MilitaryOrders.Clear(u!);u!.Actions=0;u.Destination=null;DiplomaticCouncil.AddGrievance(s,c.Owner,owner,8);s.InvalidateLogistics();
        if(owner==0||c.Owner==0)s.Log($"Napad na dostawy {c.Name}: zniszczono {raid.Destroyed:0.#} pakietów, łup {raid.Loot:0} złota. Droga pozostaje; napastnik zakończył działania.","zaopatrzenie");
        return Result.Ok($"Zniszczono {raid.Destroyed:0.#} pakietów, zdobyto {raid.Loot:0} złota. Oddział zakończył działania.");
    }
    internal static void ManageAi(GameState s,int owner)
    {
        if(!s.EpochCampaign||owner is <1 or >3)return;
        bool war=s.Relations.Any(r=>(r.A==owner||r.B==owner)&&r.War);
        foreach(var c in s.Cities.Where(c=>c.Owner==owner&&Active(s,c)))
        {int allocation=c.MilitarySupplies>=Capacity(s,c)*.85?0:war?50:25;if(PolicyError(s,c,allocation,owner)==null)SetAllocation(s,c.Id,allocation,owner);}
    }
    internal static void Validate(GameState s)
    {
        if(s.Version<23&&(s.Units.Any(u=>u.SupplyReserve!=0)||s.Cities.Any(c=>c.Buildings.Contains(Depot)||c.Project==Depot||c.ProductionQueue.Any(o=>o.Project==Depot))))throw new InvalidDataException("Starszy format nie zawiera wojskowych magazynów ani zapasów marszowych.");
        if(s.Units.Any(u=>u.SupplyReserve is <0 or >ReserveLimit||u.SupplyReserve>0&&(!s.EpochCampaign||!Eligible(u))))throw new InvalidDataException("Uszkodzone zapasy marszowe oddziału.");
        if(s.Cities.Any(c=>!double.IsFinite(c.MilitarySupplies)||c.MilitarySupplies<0||c.MilitarySupplies>Capacity(s,c)||c.SupplyAllocation is not (0 or 25 or 50)||c.LastSupplyPolicyTurn< -1||c.LastSupplyPolicyTurn>s.Turn||
            (!c.Buildings.Contains(Depot)&&(c.MilitarySupplies!=0||c.SupplyAllocation!=0||c.LastSupplyPolicyTurn!=-1))||c.Buildings.Contains(Depot)&&!s.EpochCampaign))throw new InvalidDataException("Uszkodzony magazyn lub nakład zaopatrzenia.");
    }
}
