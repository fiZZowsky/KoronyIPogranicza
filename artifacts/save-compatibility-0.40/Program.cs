using Crown.Core;
using System.Security.Cryptography;
void Check(bool ok,string text){if(!ok)throw new InvalidOperationException(text);Console.WriteLine("PASS "+text);}
var original=File.ReadAllBytes(args[0]);var hash=SHA256.HashData(original);var a=SaveGame.Read(args[0]);var text=SaveGame.Serialize(a);var b=SaveGame.Deserialize(text);
Check(a.Version==28&&text==SaveGame.Serialize(b),"The actual v28 predecessor loads and round trips exactly");
Directory.CreateDirectory(args[1]);string target=Path.Combine(args[1],"copy.crown");using(var f=new FileStream(target,FileMode.CreateNew))f.Write(original);SaveGame.Write(a,target);
Check(SaveGame.Serialize(SaveGame.Read(target))==text,"Writing and reading the isolated copy preserves the entire current-format campaign");
foreach(var s in new[]{a,b}){if(s.PendingEvent!=null){var e=ProvincialEvents.Current(s);bool done=false;for(int i=0;i<e.Choices.Length&&!done;i++)if(s.Player.Gold+e.Choices[i].Gold>=0)done=Narrative.Resolve(s,i).Success;Check(done,"The actual pending council remains resolvable");}Check(Engine.EndTurn(s).Success,"The actual campaign can advance through its real engine");}
Check(SaveGame.Serialize(a)==SaveGame.Serialize(b),"Continuation retains identical state and RNG after reload");
Check(SHA256.HashData(File.ReadAllBytes(args[0])).SequenceEqual(hash),"The original user save remains byte-for-byte unchanged");
