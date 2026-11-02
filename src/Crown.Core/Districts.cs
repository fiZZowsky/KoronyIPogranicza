namespace Crown.Core;

public enum DistrictKind { Gardens,Workshops,Scholars,Exchange,Forum }
public enum DistrictStatus { Building,Active,Pillaged,Repairing }
public enum DistrictFunding { Public,Guild }
public sealed class District
{
    public int Id {get;set;}
    public int CityId {get;set;}
    public int Tile {get;set;}
    public DistrictKind Kind {get;set;}
    public DistrictStatus Status {get;set;}
    public DistrictFunding Funding {get;set;}
    public double Work {get;set;}
    public int FoundedTurn {get;set;}
}
public sealed record DistrictType(DistrictKind Kind,string Name,string Technology,int Cost,double Upkeep,string Description);
public static class Districts
{
    public static readonly DistrictType[] Types=
    [
        new(DistrictKind.Gardens,"Ogrody miejskie","agriculture",60,1,"Żywność i dwa miejsca mieszkalne; korzystają z rzeki i pobliskich farm."),
        new(DistrictKind.Workshops,"Dzielnica rzemiosła","mining",90,2,"Produkcja; premiują wzgórza oraz pobliskie żelazo i kamień."),
        new(DistrictKind.Scholars,"Okręg uczonych","writing",100,2,"Nauka; premiuje otoczenie gór i zachowanych lasów."),
        new(DistrictKind.Exchange,"Dzielnica kupiecka","currency",85,1,"Złoto; korzysta z rzeki i sąsiednich dróg."),
        new(DistrictKind.Forum,"Forum obywatelskie","philosophy",95,2,"Kultura; korzysta z sąsiedztwa innych czynnych dzielnic.")
    ];
    public static DistrictType Type(DistrictKind kind)=>Types.Single(t=>t.Kind==kind);
    public static int Limit(City city)=>Math.Min(5,1+city.Population/4);
    public static int SurveyCost(DistrictFunding funding)=>funding==DistrictFunding.Public?30:10;
    public static double Cost(District d)=>Type(d.Kind).Cost*(d.Status==DistrictStatus.Repairing?.5:1);
    public static District? At(GameState s,int tile)=>s.Districts.FirstOrDefault(d=>d.Tile==tile);
    public static District? Construction(GameState s,City city)=>s.Districts.FirstOrDefault(d=>d.CityId==city.Id&&d.Status is DistrictStatus.Building or DistrictStatus.Repairing);
    public static string? SiteError(GameState s,City? city,int tile,DistrictKind kind,int owner=0)
    {
        if(s.Ending!=null||city==null||city.Owner!=owner||!Enum.IsDefined(kind)||tile<0||tile>=s.Tiles.Count)return "Nieprawidłowe miasto, pole lub rodzaj dzielnicy.";
        var t=s.Tiles[tile];if(owner==0&&!t.Explored)return "Najpierw odkryj to pole.";
        if(t.Owner!=owner||!t.Passable||World.Distance(t,s.Tiles[city.Tile])>2||s.Cities.Any(c=>c.Tile==tile))return "Dzielnica wymaga własnego pola lądowego w promieniu dwóch pól miasta.";
        if(t.Resource!=Resource.None||t.Improvement!=Improvement.None||t.Ruin||At(s,tile)!=null)return "Zachowaj złoża, ruiny i zagospodarowane pola. Wybierz wolne miejsce.";
        if(s.Units.Any(u=>u.Owner!=owner&&u.Tile==tile))return "Obce wojsko blokuje miejsce budowy.";
        if(!s.Factions[owner].Technologies.Contains(Type(kind).Technology))return "Wymagana technologia: "+Content.Tech(Type(kind).Technology).Name+".";
        if(Construction(s,city)!=null)return "Miasto może prowadzić jedną budowę lub odbudowę dzielnicy jednocześnie.";
        if(s.Districts.Count(d=>d.CityId==city.Id)>=Limit(city))return "Brak miejsca w planie miasta. Kolejny limit rośnie wraz z populacją.";
        if(s.Districts.Any(d=>d.CityId==city.Id&&d.Kind==kind))return "Miasto ma już dzielnicę tego rodzaju.";
        return null;
    }
    public static string? Block(GameState s,District d)
    {
        var city=s.Cities.FirstOrDefault(c=>c.Id==d.CityId);if(city==null||s.Tiles[d.Tile].Owner!=city.Owner)return "Pole poza kontrolą miasta.";
        if(s.Units.Any(u=>u.Owner!=city.Owner&&u.Tile==d.Tile))return "Obcy oddział blokuje dzielnicę.";
        if(d.Status==DistrictStatus.Pillaged)return "Dzielnica splądrowana; wymaga odbudowy.";
        if(d.Status!=DistrictStatus.Active&&city.ProductionPaused)return "Produkcja miasta jest wstrzymana.";
        return null;
    }
    public static Yields Preview(GameState s,City city,int tile,DistrictKind kind,DistrictFunding funding)
    {
        var neighbors=World.Neighbors(s,tile).Where(t=>city.Owner!=0||t.Explored).ToArray();var t=s.Tiles[tile];
        double value=kind switch
        {
            DistrictKind.Gardens=>3+(t.River?1:0)+Math.Min(3,neighbors.Count(n=>n.Improvement==Improvement.Farm)*.5),
            DistrictKind.Workshops=>3+Math.Min(4,neighbors.Count(n=>n.Terrain==Terrain.Hills||n.Resource is Resource.Iron or Resource.Stone)),
            DistrictKind.Scholars=>3+Math.Min(4,neighbors.Count(n=>n.Terrain==Terrain.Mountain)+neighbors.Count(n=>n.Terrain==Terrain.Forest)*.5),
            DistrictKind.Exchange=>4+(t.River?2:0)+Math.Min(3,neighbors.Count(n=>Navigation.RoadNode(s,n.Id))*.5),
            _=>3+Math.Min(3,neighbors.Count(n=>s.Districts.Any(d=>d.CityId==city.Id&&d.Tile==n.Id&&d.Status==DistrictStatus.Active&&Block(s,d)==null))*.75)
        };
        value*=1+Math.Floor(s.Factions[city.Owner].Era/4d)*.5;value*=funding==DistrictFunding.Guild?.85:1;
        return kind switch{DistrictKind.Gardens=>new(value,0,0,0,0),DistrictKind.Workshops=>new(0,value,0,0,0),DistrictKind.Scholars=>new(0,0,0,value,0),DistrictKind.Exchange=>new(0,0,value,0,0),_=>new(0,0,0,0,value)};
    }
    public static Yields Yields(GameState s,City city)
    {
        double food=0,prod=0,gold=0,science=0,culture=0;
        foreach(var d in s.Districts.Where(d=>d.CityId==city.Id&&d.Status==DistrictStatus.Active&&Block(s,d)==null))
        {var y=Preview(s,city,d.Tile,d.Kind,d.Funding);food+=y.Food;prod+=y.Production;gold+=y.Gold;science+=y.Science;culture+=y.Culture;}
        return new(food,prod,gold,science,culture);
    }
    public static int Housing(GameState s,City city)=>s.Districts.Any(d=>d.CityId==city.Id&&d.Kind==DistrictKind.Gardens&&d.Status==DistrictStatus.Active&&Block(s,d)==null)?2:0;
    public static double Upkeep(District d)=>Type(d.Kind).Upkeep*(d.Funding==DistrictFunding.Guild?.5:1)*(d.Status is DistrictStatus.Pillaged or DistrictStatus.Repairing?.25:d.Status==DistrictStatus.Building?0:1);
    public static double Upkeep(GameState s,int owner)=>s.Districts.Where(d=>s.Cities.Any(c=>c.Id==d.CityId&&c.Owner==owner)).Sum(Upkeep);
    public static double Allocation(GameState s,City city,double production)
    {
        var d=Construction(s,city);return d==null||Block(s,d)!=null||city.ProductionPaused?0:Math.Min(Math.Max(0,Cost(d)-d.Work),Math.Max(0,production)*city.DistrictShare/100d);
    }
    internal static double BuildPhase(GameState s,City city,double production)
    {
        var d=Construction(s,city);double work=Allocation(s,city,production);if(d==null||work<=0)return 0;
        d.Work+=work;if(d.Work+1e-8>=Cost(d)){d.Status=DistrictStatus.Active;d.Work=0;if(city.Owner==0||Engine.Visible(s).Contains(d.Tile))s.Log($"{city.Name}: ukończono {Type(d.Kind).Name}. Premie działają od następnej fazy gospodarki.","dzielnice");}
        return work;
    }
    public static Result Begin(GameState s,int cityId,int tile,DistrictKind kind,DistrictFunding funding,int owner=0)
    {
        var city=s.Cities.FirstOrDefault(c=>c.Id==cityId);if(!Enum.IsDefined(funding))return Result.Fail("Nieznany sposób finansowania.");
        if(SiteError(s,city,tile,kind,owner) is{} error)return Result.Fail(error);var faction=s.Factions[owner];int price=SurveyCost(funding);
        if(faction.Gold<price)return Result.Fail($"Przygotowanie terenu kosztuje {price} złota.");
        faction.Gold-=price;faction.Commons+=funding==DistrictFunding.Public?2:-2;faction.Nobles+=funding==DistrictFunding.Public?-1:2;Engine.Clamp(faction);
        s.Districts.Add(new(){Id=s.NextDistrictId++,CityId=cityId,Tile=tile,Kind=kind,Funding=funding,FoundedTurn=s.Turn});
        if(owner==0||Engine.Visible(s).Contains(tile))s.Log($"{city!.Name}: rozpoczęto {Type(kind).Name}, pole {s.Tiles[tile].X}, {s.Tiles[tile].Y}; "+(funding==DistrictFunding.Public?"fundusze publiczne.":"przywilej cechowy."),"dzielnice");
        return Result.Ok("Rozpoczęto budowę dzielnicy. Pole przestaje być uprawiane; nakład pracy ustalisz w planie miasta.");
    }
    public static Result SetShare(GameState s,int cityId,int share)
    {
        var city=s.Cities.FirstOrDefault(c=>c.Id==cityId&&c.Owner==0);if(s.Ending!=null||city==null||share is not (0 or 25 or 50 or 100))return Result.Fail("Nieprawidłowy nakład pracy.");
        if(city.DistrictShare==share)return Result.Ok("Ten nakład już obowiązuje.");if(city.LastDistrictPolicyTurn==s.Turn)return Result.Fail("Nakład pracy można zmienić raz na turę.");
        city.DistrictShare=share;city.LastDistrictPolicyTurn=s.Turn;return Result.Ok("Zmieniono podział produkcji między dzielnicę a zwykły projekt.");
    }
    public static Result Cancel(GameState s,int id)
    {
        var d=s.Districts.FirstOrDefault(d=>d.Id==id);var city=s.Cities.FirstOrDefault(c=>c.Id==d?.CityId&&c.Owner==0);
        if(s.Ending!=null||d==null||city==null||d.Status!=DistrictStatus.Building||s.Tiles[d.Tile].Owner!=0)return Result.Fail("Anulować można własną rozpoczętą budowę.");
        s.Districts.Remove(d);s.Log($"{city.Name}: porzucono budowę {Type(d.Kind).Name}; przygotowanie i praca nie są zwracane.","dzielnice");return Result.Ok("Porzucono budowę. Pole jest znów dostępne; bez zwrotu kosztów i pracy.");
    }
    public static Result Repair(GameState s,int id)
    {
        var d=s.Districts.FirstOrDefault(d=>d.Id==id);var city=s.Cities.FirstOrDefault(c=>c.Id==d?.CityId&&c.Owner==0);
        if(s.Ending!=null||d==null||city==null||d.Status!=DistrictStatus.Pillaged||s.Tiles[d.Tile].Owner!=0||Construction(s,city)!=null||s.Units.Any(u=>u.Tile==d.Tile&&u.Owner!=0))return Result.Fail("Odbudowa wymaga własnego, bezpiecznego pola i wolnego planu budowy.");
        d.Status=DistrictStatus.Repairing;d.Work=0;return Result.Ok("Rozpoczęto odbudowę za połowę pierwotnej produkcji.");
    }
    public static Result Pillage(GameState s,int unitId,int owner=0)
    {
        var u=s.Units.FirstOrDefault(u=>u.Id==unitId&&u.Owner==owner);var d=u==null?null:At(s,u.Tile);var city=s.Cities.FirstOrDefault(c=>c.Id==d?.CityId);
        if(s.Ending!=null||u==null||d==null||city==null||city.Owner==owner||s.Tiles[d.Tile].Owner!=city.Owner||!s.Relation(city.Owner,owner).War||d.Status!=DistrictStatus.Active||u.Kind==UnitKind.Settler||u.Actions<1||u.EscortCaravanId!=null)return Result.Fail("Grabież wymaga gotowego oddziału na czynnej dzielnicy państwa, z którym trwa wojna.");
        u.Actions-=1;u.Destination=null;MilitaryOrders.Clear(u);d.Status=DistrictStatus.Pillaged;d.Work=0;double loot=Type(d.Kind).Cost*.2;s.Factions[owner].Gold+=loot;
        DiplomaticCouncil.AddGrievance(s,city.Owner,owner,8);if(owner==0||city.Owner==0||Engine.Visible(s).Contains(d.Tile))s.Log($"{s.Factions[owner].Name}: splądrowano {Type(d.Kind).Name} przy {city.Name}; łup {loot:0} złota.","wojna");return Result.Ok($"Dzielnica wyłączona. Zdobyto {loot:0} złota; zużyto 1 punkt ruchu.");
    }
    internal static void ManageAi(GameState s,Faction faction)
    {
        if(!s.EpochCampaign||s.Turn<20||s.Turn%5!=0||faction.Gold<110||s.Relations.Any(r=>(r.A==faction.Id||r.B==faction.Id)&&r.War))return;
        foreach(var city in s.Cities.Where(c=>c.Owner==faction.Id).OrderBy(c=>c.Id))
        {
            if(Construction(s,city)!=null)continue;
            var damaged=s.Districts.FirstOrDefault(d=>d.CityId==city.Id&&d.Status==DistrictStatus.Pillaged&&s.Tiles[d.Tile].Owner==faction.Id&&!s.Units.Any(u=>u.Tile==d.Tile&&u.Owner!=faction.Id));
            if(damaged!=null){damaged.Status=DistrictStatus.Repairing;damaged.Work=0;continue;}
            var choice=Types.SelectMany(type=>s.Tiles.Where(t=>World.Distance(t,s.Tiles[city.Tile])<=2).Select(t=>(Type:type,Tile:t)))
                .Where(x=>SiteError(s,city,x.Tile.Id,x.Type.Kind,faction.Id)==null)
                .Select(x=>{var y=Preview(s,city,x.Tile.Id,x.Type.Kind,DistrictFunding.Public);var lost=Engine.TileYields(x.Tile,faction);double score=y.Food*(Engine.CityYields(s,city).Food<2?3:1)+y.Production*1.8+y.Science*(faction.Id==2?2.4:1.8)+y.Gold*(faction.Id==1?1.5:1)+y.Culture-lost.Food*.7-lost.Production*.5;return(x.Type,x.Tile,Score:score);})
                .OrderByDescending(x=>x.Score).ThenBy(x=>x.Tile.Id).ThenBy(x=>x.Type.Kind).FirstOrDefault();
            if(choice.Type!=null)Begin(s,city.Id,choice.Tile.Id,choice.Type.Kind,DistrictFunding.Public,faction.Id);
        }
    }
    internal static void Validate(GameState s)
    {
        if(s.Districts==null||s.Districts.Count>s.Cities.Count*5||s.NextDistrictId<1||s.NextDistrictId>100000||s.Districts.Any(d=>d==null)||s.Districts.Select(d=>d.Id).Distinct().Count()!=s.Districts.Count||s.Districts.Select(d=>d.Tile).Distinct().Count()!=s.Districts.Count)throw new InvalidDataException("Uszkodzony plan dzielnic.");
        foreach(var c in s.Cities)if(c.DistrictShare is not (0 or 25 or 50 or 100)||c.LastDistrictPolicyTurn< -1||c.LastDistrictPolicyTurn>s.Turn)throw new InvalidDataException("Nieprawidłowy podział pracy dzielnic.");
        foreach(var d in s.Districts)
        {
            var city=s.Cities.FirstOrDefault(c=>c.Id==d.CityId);
            if(d.Id<1||d.Id>=s.NextDistrictId||city==null||d.Tile<0||d.Tile>=s.Tiles.Count||!Enum.IsDefined(d.Kind)||!Enum.IsDefined(d.Status)||!Enum.IsDefined(d.Funding)||d.FoundedTurn<1||d.FoundedTurn>s.Turn||!double.IsFinite(d.Work)||d.Work<0||
                d.Work>=Cost(d)||(d.Status is DistrictStatus.Active or DistrictStatus.Pillaged)&&d.Work!=0||!s.Tiles[d.Tile].Passable||s.Tiles[d.Tile].Resource!=Resource.None||s.Tiles[d.Tile].Improvement!=Improvement.None||s.Tiles[d.Tile].Ruin||s.Cities.Any(c=>c.Tile==d.Tile)||World.Distance(s.Tiles[d.Tile],s.Tiles[city.Tile])>2||city.Owner==0&&!s.Tiles[d.Tile].Explored)
                throw new InvalidDataException("Uszkodzona dzielnica lub miejsce budowy.");
        }
        if(s.Districts.GroupBy(d=>d.CityId).Any(g=>g.Count()>5||g.Select(d=>d.Kind).Distinct().Count()!=g.Count()||g.Count(d=>d.Status is DistrictStatus.Building or DistrictStatus.Repairing)>1))throw new InvalidDataException("Powielony plan dzielnic miasta.");
    }
}
