using Crown.Core;
using System.Security.Cryptography;
using System.Text.Json;

var original=File.ReadAllBytes(args[0]);string hash=Convert.ToHexString(SHA256.HashData(original));
using var json=JsonDocument.Parse(original);int version=json.RootElement.GetProperty("Version").GetInt32();
var s=SaveGame.Read(args[0]);string text=SaveGame.Serialize(s);var clone=SaveGame.Deserialize(text);
void Check(bool ok,string message){if(!ok)throw new InvalidOperationException(message);Console.WriteLine("PASS "+message);}
Check(s.Version==27&&text==SaveGame.Serialize(clone),"Actual previous-release autosave migrates to v27 and round trips exactly");
Check(s.Cities.All(c=>!c.Council.Established)&&s.CalendarYears.Count==1,"Migration invents neither local agreements nor historical dates");
Directory.CreateDirectory(args[1]);string copied=Path.Combine(args[1],"copied-autosave.crown");
using(var file=new FileStream(copied,FileMode.CreateNew))file.Write(original);
SaveGame.Write(s,copied);
Check(File.ReadAllBytes(copied+$".v{version}.bak").SequenceEqual(original),"Writing the isolated migrated copy keeps an exact predecessor backup");
int turn=s.Turn;
foreach(var state in new[]{s,clone})
{
    if(state.PendingEvent!=null)
    {
        var e=ProvincialEvents.Current(state);bool resolved=false;
        for(int i=0;i<e.Choices.Length&&!resolved;i++)if(state.Player.Gold+e.Choices[i].Gold>=0)resolved=Narrative.Resolve(state,i).Success;
        Check(resolved,"The migrated pending council remains resolvable");
    }
    Check(Engine.EndTurn(state).Success,"The migrated campaign advances through the real economic engine");
}
Check(SaveGame.Serialize(s)==SaveGame.Serialize(clone),"The migrated campaign continues with identical state and RNG after reload");
Check(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(args[0])))==hash,"The original user autosave remains byte-for-byte unchanged");
Console.WriteLine($"Source format v{version}; turn {turn} -> {s.Turn}; SHA256 {hash}");
