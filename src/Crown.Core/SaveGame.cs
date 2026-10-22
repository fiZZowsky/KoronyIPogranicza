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
        if(s.Version is <1 or >2)throw new InvalidDataException("Nieobsługiwana wersja zapisu.");
        if(s.Width!=42||s.Height!=28||s.Tiles.Count!=s.Width*s.Height||s.Factions.Count!=4||s.RandomState==0||s.Turn<1||s.Turn>321||s.MaxTurns!=320)throw new InvalidDataException("Uszkodzone dane świata.");
        if(!s.Tiles.Select((t,i)=>t.Id==i&&t.X==i%s.Width&&t.Y==i/s.Width&&t.Owner>=-1&&t.Owner<4&&Enum.IsDefined(t.Terrain)&&Enum.IsDefined(t.Resource)&&Enum.IsDefined(t.Improvement)&&
            (t.Road?t.Passable&&t.RoadOwner>=0&&t.RoadOwner<4:t.RoadOwner==-1)).All(x=>x))throw new InvalidDataException("Uszkodzona mapa.");
        bool ValidOwner(int owner)=>owner>=0&&owner<4;
        bool ValidTile(int tile)=>tile>=0&&tile<s.Tiles.Count;
        if(s.Factions.Where((f,i)=>f.Id!=i||!double.IsFinite(f.Gold)||!double.IsFinite(f.Science)||!double.IsFinite(f.Culture)||!double.IsFinite(f.Prestige)||f.Tax is <0 or >2||f.Government is <0 or >2||f.Focus is <0 or >2||f.Technologies.Any(id=>!Content.Technologies.Any(t=>t.Id==id))||f.Research!=""&&!Content.Technologies.Any(t=>t.Id==f.Research)).Any())throw new InvalidDataException("Uszkodzone dane państw.");
        if(s.Cities.Any(c=>!ValidOwner(c.Owner)||!ValidTile(c.Tile)||c.Population<1||c.Population>1000||!double.IsFinite(c.Food)||!double.IsFinite(c.Production)||!double.IsFinite(c.Health)||!Content.Projects.Any(p=>p.Id==c.Project)||c.Buildings.Any(id=>!Content.Projects.Any(p=>p.Id==id&&p.Unit==null))))throw new InvalidDataException("Uszkodzone miasta.");
        if(s.Units.Any(u=>!ValidOwner(u.Owner)||!ValidTile(u.Tile)||u.Health<=0||u.Health>100||!double.IsFinite(u.Health)||!Enum.IsDefined(u.Kind)||
            !double.IsFinite(u.Actions)||u.Actions<0||u.Actions>u.MaxActions||u.Actions*2!=Math.Floor(u.Actions*2)||
            u.Destination is int target&&(!ValidTile(target)||!s.Tiles[target].Passable||u.Owner==0&&!s.Tiles[target].Explored)))throw new InvalidDataException("Uszkodzone jednostki lub rozkazy.");
        var ids=s.Cities.Select(c=>c.Id).Concat(s.Units.Select(u=>u.Id)).ToArray();if(ids.Distinct().Count()!=ids.Length||ids.Any(i=>i<=0||i>=s.NextId))throw new InvalidDataException("Uszkodzone identyfikatory.");
        if(s.Relations.Count!=6||s.Relations.Any(r=>!ValidOwner(r.A)||!ValidOwner(r.B)||r.A>=r.B)||s.Relations.Select(r=>(r.A,r.B)).Distinct().Count()!=6)throw new InvalidDataException("Uszkodzone relacje.");
        if(s.PendingEvent!=null&&!Narrative.All.Any(e=>e.Id==s.PendingEvent)||s.SeenEvents.Any(id=>!Narrative.All.Any(e=>e.Id==id))||s.Deferred.Any(e=>!ValidOwner(e.Faction)||!double.IsFinite(e.Gold)))throw new InvalidDataException("Uszkodzona historia.");
        s.Version=2;
        return s;
    }
    public static void Write(GameState state,string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        string temp=path+".tmp";File.WriteAllText(temp,Serialize(state));
        if(File.Exists(path)&&!File.Exists(path+".v1.bak"))
        {
            try{using var previous=JsonDocument.Parse(File.ReadAllText(path));if(previous.RootElement.TryGetProperty("Version",out var version)&&version.GetInt32()==1)File.Copy(path,path+".v1.bak",false);}
            catch(JsonException){ /* An invalid old file may still be replaced by an explicit save. */ }
        }
        File.Move(temp,path,true);
    }
    public static GameState Read(string path)
    {
        if(new FileInfo(path).Length>12_000_000)throw new InvalidDataException("Zapis jest zbyt duży.");return Deserialize(File.ReadAllText(path));
    }
}
