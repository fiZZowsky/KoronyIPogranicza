using System.Text.Json;
using System.Text.Json.Serialization;

namespace Crown.Core;

public static class SaveGame
{
    private static readonly JsonSerializerOptions Options=new(){WriteIndented=true,IgnoreReadOnlyProperties=true,Converters={new JsonStringEnumConverter()}};
    public static string Serialize(GameState state)=>JsonSerializer.Serialize(state,Options);
    public static GameState Deserialize(string json)
    {
        var s=JsonSerializer.Deserialize<GameState>(json,Options)??throw new InvalidDataException("Pusty zapis.");
        if(s.Version is <1 or >5)throw new InvalidDataException("Nieobsługiwana wersja zapisu.");
        if(s.Version<4)
        {
            s.Demands=[];s.NextDemandId=1;
            foreach(var city in s.Cities)
            {
                // Old saves did not record founders. Recognize their current sovereignty on migration.
                city.OriginalOwner=city.Owner;city.Governance=Governance.Integrated;city.Loyalty=75;
                city.OccupiedUntil=city.RevoltTurns=0;city.LastGovernanceTurn=-10;
            }
            foreach(var r in s.Relations){r.GrievanceA=r.GrievanceB=0;r.LastDemandTurn=-10;r.Trust=Math.Clamp(r.Trust,-100,100);}
        }
        if(s.Version<3)
        {
            s.PendingCityId=null;s.Migrations=[];
            foreach(var city in s.Cities)
            {
                city.Focus=CityFocus.Balanced;city.Prosperity=50;city.HungerTurns=0;city.LastFocusTurn=-1;city.Unrest=Math.Clamp(city.Unrest,0,100);
                city.LastAidTurn=-10;city.LastMigrationTurn=city.LastCouncilTurn=-20;
                city.MigrantsReceived=city.MigrantsDeparted=0;city.LastFoodImported=city.LastFoodExported=0;
                city.Charters=[];city.ResolvedIssues=[];
            }
        }
        if(s.Width!=42||s.Height!=28||s.Tiles.Count!=s.Width*s.Height||s.Factions.Count!=4||s.RandomState==0||s.Turn<1||s.Turn>321||s.MaxTurns!=320)throw new InvalidDataException("Uszkodzone dane świata.");
        if(!s.Tiles.Select((t,i)=>t.Id==i&&t.X==i%s.Width&&t.Y==i/s.Width&&t.Owner>=-1&&t.Owner<4&&Enum.IsDefined(t.Terrain)&&Enum.IsDefined(t.Resource)&&Enum.IsDefined(t.Improvement)&&
            (t.Road?t.Passable&&t.RoadOwner>=0&&t.RoadOwner<4:t.RoadOwner==-1)).All(x=>x))throw new InvalidDataException("Uszkodzona mapa.");
        bool ValidOwner(int owner)=>owner>=0&&owner<4;
        bool ValidTile(int tile)=>tile>=0&&tile<s.Tiles.Count;
        if(s.Factions.Where((f,i)=>f.Id!=i||!double.IsFinite(f.Gold)||!double.IsFinite(f.Science)||!double.IsFinite(f.Culture)||!double.IsFinite(f.Prestige)||f.Tax is <0 or >2||f.Government is <0 or >2||f.Focus is <0 or >2||f.Technologies.Any(id=>!Content.Technologies.Any(t=>t.Id==id))||f.Research!=""&&!Content.Technologies.Any(t=>t.Id==f.Research)).Any())throw new InvalidDataException("Uszkodzone dane państw.");
        if(s.Cities.Any(c=>!ValidOwner(c.Owner)||!ValidTile(c.Tile)||c.Population<1||c.Population>1000||!double.IsFinite(c.Food)||!double.IsFinite(c.Production)||!double.IsFinite(c.Health)||!Content.Projects.Any(p=>p.Id==c.Project)||c.Buildings.Any(id=>!Content.Projects.Any(p=>p.Id==id&&p.Unit==null))))throw new InvalidDataException("Uszkodzone miasta.");
        if(s.Cities.Any(c=>!Enum.IsDefined(c.Focus)||c.Prosperity is <0 or >100||c.Unrest is <0 or >100||c.Health is <0 or >100||c.Food<0||c.HungerTurns is <0 or >321||
            c.MigrantsReceived<0||c.MigrantsDeparted<0||!double.IsFinite(c.LastFoodImported)||!double.IsFinite(c.LastFoodExported)||c.LastFoodImported is <0 or >6||c.LastFoodExported is <0 or >6||
            c.Charters==null||c.Charters.Any(id=>!Regions.CharterIds.Contains(id))||c.ResolvedIssues==null||c.ResolvedIssues.Any(id=>!ProvincialEvents.IsLocal(id))))throw new InvalidDataException("Uszkodzone dane prowincji.");
        if(s.Units.Any(u=>!ValidOwner(u.Owner)||!ValidTile(u.Tile)||u.Health<=0||u.Health>100||!double.IsFinite(u.Health)||!Enum.IsDefined(u.Kind)||
            !double.IsFinite(u.Actions)||u.Actions<0||u.Actions>u.MaxActions||u.Actions*2!=Math.Floor(u.Actions*2)||
            u.Destination is int target&&(!ValidTile(target)||!s.Tiles[target].Passable||u.Owner==0&&!s.Tiles[target].Explored)))throw new InvalidDataException("Uszkodzone jednostki lub rozkazy.");
        var ids=s.Cities.Select(c=>c.Id).Concat(s.Units.Select(u=>u.Id)).ToArray();if(ids.Distinct().Count()!=ids.Length||ids.Any(i=>i<=0||i>=s.NextId))throw new InvalidDataException("Uszkodzone identyfikatory.");
        if(s.Relations.Count!=6||s.Relations.Any(r=>!ValidOwner(r.A)||!ValidOwner(r.B)||r.A>=r.B)||s.Relations.Select(r=>(r.A,r.B)).Distinct().Count()!=6)throw new InvalidDataException("Uszkodzone relacje.");
        if(s.PendingEvent!=null&&!Narrative.All.Any(e=>e.Id==s.PendingEvent)||s.SeenEvents.Any(id=>!Narrative.All.Any(e=>e.Id==id))||s.Deferred.Any(e=>!ValidOwner(e.Faction)||!double.IsFinite(e.Gold)))throw new InvalidDataException("Uszkodzona historia.");
        if(s.PendingEvent!=null&&ProvincialEvents.IsLocal(s.PendingEvent)?!s.Cities.Any(c=>c.Id==s.PendingCityId&&c.Owner==0&&!c.ResolvedIssues.Contains(s.PendingEvent)):s.PendingCityId!=null)
            throw new InvalidDataException("Uszkodzony kontekst sprawy prowincji.");
        var cityIds=s.Cities.Select(c=>c.Id).ToHashSet();
        if(s.Cities.Any(c=>!ValidOwner(c.OriginalOwner)||!Enum.IsDefined(c.Governance)||c.Loyalty is <0 or >100||c.RevoltTurns is <0 or >2||
            c.OccupiedUntil<0||c.OccupiedUntil>s.Turn+12||c.LastGovernanceTurn< -10||c.LastGovernanceTurn>s.Turn||
            c.RevoltTurns>0&&(c.Owner==c.OriginalOwner||c.Loyalty>=20)))throw new InvalidDataException("Uszkodzone dane lojalności.");
        if(s.Relations.Any(r=>r.Trust is < -100 or >100||r.GrievanceA is <0 or >100||r.GrievanceB is <0 or >100||r.TruceUntil<0||r.TruceUntil>s.Turn+15||
            r.LastDemandTurn< -10||r.LastDemandTurn>s.Turn))throw new InvalidDataException("Uszkodzone zobowiązania dyplomatyczne.");
        if(s.Demands==null||s.Demands.Count>2000||s.NextDemandId<1||s.Demands.Select(d=>d.Id).Distinct().Count()!=s.Demands.Count||s.Demands.Any(d=>
            d.Id<1||d.Id>=s.NextDemandId||d.From!=0||d.To<1||d.To>3||!Enum.IsDefined(d.Kind)||!Enum.IsDefined(d.Status)||d.SentTurn<1||d.SentTurn>s.Turn||
            d.DueTurn!=d.SentTurn+2||d.Reply==null||d.Reply.Length>2000||
            (d.Status==DemandStatus.Pending ? d.ResolvedTurn!=null||d.DueTurn<=s.Turn||d.Reply.Length!=0||s.Relation(d.From,d.To).War :
                d.ResolvedTurn==null||d.ResolvedTurn<d.SentTurn||d.ResolvedTurn>s.Turn||d.Reply.Length==0||d.Status!=DemandStatus.Cancelled&&d.ResolvedTurn<d.DueTurn))||
            s.Demands.Where(d=>d.Status==DemandStatus.Pending).GroupBy(d=>d.To).Any(g=>g.Count()>1))throw new InvalidDataException("Uszkodzone poselstwa.");
        if(s.Migrations==null||s.Migrations.Count>2000||s.Migrations.Any(m=>!ValidOwner(m.Owner)||m.Turn<1||m.Turn>s.Turn||m.People!=1||m.Source==m.Destination||!cityIds.Contains(m.Source)||!cityIds.Contains(m.Destination)))
            throw new InvalidDataException("Uszkodzona historia migracji.");
        if(s.Version<5)
        {
            s.Wars=[];s.PeaceOffers=[];s.NextWarId=s.NextPeaceId=1;
            foreach(var r in s.Relations)r.LastPeaceTurn=-10;
            WarCouncil.EnsureActive(s);
        }
        WarSaveRules.Validate(s);
        s.Version=5;
        return s;
    }
    public static void Write(GameState state,string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        string temp=path+".tmp";File.WriteAllText(temp,Serialize(state));
        if(File.Exists(path))
        {
            try{using var previous=JsonDocument.Parse(File.ReadAllText(path));if(previous.RootElement.TryGetProperty("Version",out var version)&&version.TryGetInt32(out int number)&&number is 1 or 2 or 3 or 4&&!File.Exists(path+$".v{number}.bak"))File.Copy(path,path+$".v{number}.bak",false);}
            catch(JsonException){ /* An invalid old file may still be replaced by an explicit save. */ }
        }
        File.Move(temp,path,true);
    }
    public static GameState Read(string path)
    {
        if(new FileInfo(path).Length>12_000_000)throw new InvalidDataException("Zapis jest zbyt duży.");return Deserialize(File.ReadAllText(path));
    }
}
