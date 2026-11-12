using Crown.Core;
using System.IO;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace Crown.Desktop;

public sealed partial class MainWindow
{
    private async Task CharacterMotionSmoke(string folder)
    {
        Directory.CreateDirectory(folder);var checks=new List<string>();
        void Check(bool ok,string text){if(!ok)throw new InvalidOperationException(text);checks.Add("PASS "+text);}
        IEnumerable<GeometryModel3D> Models(Model3D m){if(m is GeometryModel3D g)yield return g;if(m is Model3DGroup group)foreach(var child in group.Children)foreach(var item in Models(child))yield return item;}
        string Hash(){var image=map.Photographic!.Image!;var data=new byte[image.PixelWidth*image.PixelHeight*4];image.CopyPixels(data,image.PixelWidth*4,0);return Convert.ToHexString(SHA256.HashData(data));}
        async Task Capture(string name){root.UpdateLayout();await Task.Delay(140);map.RenderGpuFrame();var b=new RenderTargetBitmap((int)root.ActualWidth,(int)root.ActualHeight,96,96,PixelFormats.Pbgra32);b.Render(root);var e=new PngBitmapEncoder();e.Frames.Add(BitmapFrame.Create(b));using var f=File.Create(Path.Combine(folder,name+".png"));e.Save(f);}
        void Click(string title){root.UpdateLayout();Descendants<Button>(overlay??root).First(b=>b.Content?.ToString()==title).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));root.UpdateLayout();}
        try
        {
            if(arguments.Contains("--smoke-force-failure"))throw new InvalidOperationException("Intentional character motion diagnostic failure.");
            var s=World.Create();Narrative.Resolve(s,0);string original=SaveGame.Serialize(s);Start(s);root.UpdateLayout();await Task.Delay(140);map.RenderGpuFrame();
            Check(arguments.Contains("--no-gpu")?map.Photographic==null:map.Photographic!=null,"The requested backend renders character geometry: "+map.GpuFailure);
            var models=Models(map.Scene!.Objects).ToArray();var attached=models.Where(m=>m.GetValue(SceneSurface.CharactersProperty) is CharacterFrame[]).ToArray();
            Check(attached.Length>0&&attached.All(m=>((CharacterFrame[])m.GetValue(SceneSurface.CharactersProperty)).Length==((MeshGeometry3D)m.Geometry).TriangleIndices.Count/3),"Character metadata aligns exactly with its immutable triangles");
            var frames=attached.SelectMany(m=>(CharacterFrame[])m.GetValue(SceneSurface.CharactersProperty)).Where(f=>f.Active).ToArray();
            Check(frames.All(f=>double.IsFinite(f.Origin.X+f.Origin.Y+f.Origin.Z+f.Height)&&f.Height is >0 and <1),"Every character has a finite transformed pivot and bounded world height");
            var body=attached.Where(m=>ActorAssets.MaterialId(SceneSurface.Key(m))==7).SelectMany(m=>(CharacterFrame[])m.GetValue(SceneSurface.CharactersProperty)).ToHashSet();
            var clothing=attached.Where(m=>SceneSurface.Key(m).Contains("camp-hide-garment")).SelectMany(m=>(CharacterFrame[])m.GetValue(SceneSurface.CharactersProperty)).ToHashSet();
            var hair=attached.Where(m=>SceneSurface.Key(m).Contains("forager-hair")).SelectMany(m=>(CharacterFrame[])m.GetValue(SceneSurface.CharactersProperty)).ToHashSet();
            Check(body.Count>0&&body.IsSubsetOf(clothing)&&body.IsSubsetOf(hair),"Bodies, tunics and hair share identical motion frames");
            Check(body.Select(f=>Math.Round(f.Height,5)).Distinct().Count()>1,"Initial people have stable individual height variations");
            Check(models.Where(m=>SceneSurface.Key(m).Contains("camp-hide-garment")).Select(m=>m.GetValue(SceneSurface.TintProperty)).Distinct().Count()>1,"The scene contains multiple subdued clothing colors");
            Check(models.Where(m=>SceneSurface.Key(m).StartsWith("flag-")||SceneSurface.Key(m).StartsWith("landscape")).All(m=>m.GetValue(SceneSurface.CharactersProperty)==null),"Character motion never attaches to markers or terrain");
            var unit=s.Units.First(u=>u.Owner==0&&!Navy.IsShip(u));Select(unit.Tile);selectedUnit=unit.Id;Refresh();map.InspectUnit(unit);await Capture("characters-close");
            var stillPreferences=preferences.Copy();stillPreferences.AnimateEnvironment=false;stillPreferences.AnimateCharacters=false;map.Configure(stillPreferences);root.UpdateLayout();
            if(map.Photographic is{} renderer)
            {
                renderer.Weather=VisualWeather.Clear;map.RenderGpuFrame();string zero=Hash();map.RenderGpuFrame(4);
                Check(Hash()==zero,"Disabling both animation channels keeps every pixel fixed as visual time changes");
                renderer.AnimateCharacters=true;map.RenderGpuFrame();Check(Hash()==zero,"Character animation has an exact neutral pose at visual time zero");map.RenderGpuFrame(4);
                Check(Hash()!=zero,"Character motion changes actual GPU pixels while wind and weather remain frozen");string posed=Hash();map.RenderGpuFrame(4);Check(Hash()==posed,"The same visual time reproduces the exact character pose");
                renderer.VisibilityCulling=false;map.RenderGpuFrame(4);string uncropped=Hash();renderer.VisibilityCulling=true;map.RenderGpuFrame(4);Check(Hash()==uncropped,"Conservative motion bounds preserve all pixels when frustum culling is enabled");
                var scene=map.Scene;int resources=renderer.LiveResources,uploads=renderer.SceneUploads;
                for(int i=0;i<24;i++)map.RenderGpuFrame(i*.15);
                Check(renderer.LiveResources==resources&&renderer.SceneUploads==uploads&&ReferenceEquals(scene,map.Scene),"Character animation updates uniforms without reallocating or rebuilding geometry");
                var figure=body.MinBy(f=>(f.Origin-Terrain3D.Ground(s,WorldScene3D.UnitAnchor(s,unit,0).X,WorldScene3D.UnitAnchor(s,unit,0).Y)).Length)!;
                var top=map.Project(figure.Origin+new Vector3D(0,figure.Height,0))!.Value;var bottom=map.Project(figure.Origin)!.Value;double scale=renderer.Image!.PixelWidth/map.ActualWidth;
                int width=Math.Min(420,renderer.Image.PixelWidth),height=Math.Min(400,renderer.Image.PixelHeight);
                int left=Math.Clamp((int)((top.X+bottom.X)*.5*scale-width*.5),0,renderer.Image.PixelWidth-width),y=Math.Clamp((int)((top.Y+bottom.Y)*.5*scale-height*.5),0,renderer.Image.PixelHeight-height);
                var preview=new List<BitmapSource>();var hashes=new HashSet<string>();
                for(int i=0;i<40;i++){map.RenderGpuFrame(i*.15);hashes.Add(Hash());var crop=new CroppedBitmap(renderer.Image,new Int32Rect(left,y,width,height));var copy=new WriteableBitmap(crop);copy.Freeze();preview.Add(copy);}
                Check(hashes.Count>30,"The close preview contains distinct rendered poses across its sampled cycle");
                string gif=Path.Combine(folder,"character-idle.gif");MotionPreview.Save(gif,preview);var decoder=new GifBitmapDecoder(new Uri(gif),BitmapCreateOptions.PreservePixelFormat,BitmapCacheOption.OnLoad);
                Check(decoder.Frames.Count==40&&decoder.Frames.All(f=>f.Metadata is BitmapMetadata m&&Convert.ToUInt16(m.GetQuery("/grctlext/Delay"))==15),"The real-render GIF contains all forty frames with explicit playback timing");
            }
            Check(SaveGame.Serialize(state)==original,"Appearance, camera inspection and visual motion preserve exact campaign state and RNG");
            ShowPause();Click("Ustawienia");root.UpdateLayout();var toggle=Descendants<CheckBox>(overlay!).Single(c=>c.Content?.ToString()=="Animacje spoczynku postaci");toggle.IsChecked=false;Click("Zastosuj");
            Check(!preferences.AnimateCharacters&&!PreferencesStore.Read(preferencesPath,out _).AnimateCharacters,"The actual video setting persists the separate character animation preference");
            Check(map.Photographic==null||!map.Photographic.AnimateCharacters,"Applying the video setting reaches the active renderer");CloseModal();
            var enabled=preferences.Copy();enabled.AnimateEnvironment=false;enabled.AnimateCharacters=true;map.Configure(enabled);await Task.Delay(180);
            if(map.Photographic is{} live){int before=live.Frames;await Task.Delay(300);Check(live.Frames>before,"Character-only animation runs even when environment animation is disabled");ShowPause();await Task.Delay(160);before=live.Frames;await Task.Delay(250);Check(live.Frames==before,"Pause suspends character-only animation");CloseModal();}
            var rebuilt=WorldScene3D.Build(SaveGame.Deserialize(original));var again=Models(rebuilt.Objects).Where(m=>m.GetValue(SceneSurface.CharactersProperty) is CharacterFrame[]).SelectMany(m=>(CharacterFrame[])m.GetValue(SceneSurface.CharactersProperty)).ToArray();
            Check(frames.SequenceEqual(again.Where(f=>f.Active)),"Reloading a save reconstructs the same visual identities without storing animation state");
            var moved=SaveGame.Deserialize(original);var walker=moved.Units.First(u=>u.Owner==0&&!Navy.IsShip(u));int start=walker.Tile;
            var neighbor=World.Neighbors(moved,start).First(t=>t.Passable&&t.Owner is -1 or 0&&!moved.Units.Any(u=>u.Owner!=0&&u.Tile==t.Id));
            Check(Engine.OrderMove(moved,walker.Id,neighbor.Id).Success&&walker.Tile!=start,"The identity check uses a real movement action on an isolated campaign");
            var movedPeople=Models(WorldScene3D.Build(moved).Objects).Where(m=>ActorAssets.MaterialId(SceneSurface.Key(m))==7).SelectMany(m=>(CharacterFrame[])m.GetValue(SceneSurface.CharactersProperty)).Where(f=>f.Active).GroupBy(f=>f.Identity).ToDictionary(g=>g.Key,g=>g.First());
            Check(body.All(f=>movedPeople.TryGetValue(f.Identity,out var same)&&Math.Abs(f.Height-same.Height)<.000001),"Moving to another tile preserves each member's appearance and motion identity");
            string legacy=Path.Combine(folder,"legacy-settings.json");File.WriteAllText(legacy,"{\"Version\":1}");Check(PreferencesStore.Read(legacy,out var warning).AnimateCharacters&&warning==null,"Existing settings files default to enabled character motion");
            Check(SaveGame.Serialize(state)==original,"Settings and pauses leave the original campaign unchanged");await Capture("characters-final");checks.Add($"{checks.Count} character motion desktop integration checks passed.");File.WriteAllLines(Path.Combine(folder,"ui-smoke.txt"),checks);Close();
        }
        catch(Exception ex){checks.Add(ex.ToString());File.WriteAllLines(Path.Combine(folder,"ui-smoke.txt"),checks);if(!arguments.Contains("--smoke-force-failure")&&map!=null)await Capture("characters-failure");Application.Current.Shutdown(3);}
    }
}
