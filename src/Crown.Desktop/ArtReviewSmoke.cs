using Crown.Core;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace Crown.Desktop;

public sealed partial class MainWindow
{
    private async Task ArtReviewSmoke(string folder)
    {
        Directory.CreateDirectory(folder);var checks=new List<string>();
        void Check(bool ok,string message){if(!ok)throw new InvalidOperationException(message);checks.Add("PASS "+message);}
        IEnumerable<GeometryModel3D> Models(Model3D m){if(m is GeometryModel3D g)yield return g;if(m is Model3DGroup group)foreach(var child in group.Children)foreach(var item in Models(child))yield return item;}
        async Task Capture(string name)
        {
            root.UpdateLayout();await Task.Delay(150);map.RenderGpuFrame();var bitmap=new RenderTargetBitmap((int)root.ActualWidth,(int)root.ActualHeight,96,96,PixelFormats.Pbgra32);bitmap.Render(root);
            var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using var output=File.Create(Path.Combine(folder,name+".png"));encoder.Save(output);
        }
        try
        {
            if(arguments.Contains("--smoke-force-failure"))throw new InvalidOperationException("Intentional art review diagnostic failure.");
            var s=World.Create();string source=Path.Combine(AppContext.BaseDirectory,"saves","autosave.crown");
            if(arguments.Contains("--review-save")){int index=Array.IndexOf(arguments,"--review-save");source=arguments[index+1];s=SaveGame.Read(source);}
            string original=SaveGame.Serialize(s);Start(s);root.UpdateLayout();await Task.Delay(140);map.RenderGpuFrame();
            Check(arguments.Contains("--no-gpu")?map.Photographic==null:map.Photographic!=null,"The scene uses the requested rendering backend: "+map.GpuFailure);
            var models=Models(map.Scene!.Objects).ToArray();
            Check(models.Any(m=>ActorAssets.MaterialId(SceneSurface.Key(m))==7),"The initial era uses the anatomical human mesh instead of its fantasy warrior");
            Check(!models.Any(m=>ActorAssets.MaterialId(SceneSurface.Key(m)) is 1 or 2),"No fantasy armor is rendered in the initial Paleolithic scene");
            Check(!models.Any(m=>SceneSurface.Key(m).StartsWith("tents:")),"Closed eight-sided cone placeholders are absent from the initial city");
            var shelters=models.Where(m=>SceneSurface.Key(m).Contains("-camp-hide:")).ToArray();
            Check(shelters.Length>0&&shelters.All(m=>m.Material is DiffuseMaterial{Brush:ImageBrush}&&m.BackMaterial!=null),"Open curved shelters have textured, double-sided hide surfaces");
            Check(models.Any(m=>SceneSurface.Key(m).Contains("camp-frame-wood"))&&models.Any(m=>SceneSurface.Key(m).Contains("camp-seams-leather")),"Shelters include distinct timber ribs and seams rather than a color-only silhouette");
            var capital=s.Cities.First(c=>c.Owner==0);var center=Terrain3D.Center(s.Tiles[capital.Tile]);var field=TerrainField.Current(s);
            Check(field.Rivers.Paths.SelectMany(p=>p.Samples).All(p=>!WorldScene3D.CampSiteDry(s,p.Position)),"Shelter placement rejects every sampled channel center");
            Check(WorldScene3D.CampSiteDry(s,center),"The capital center remains a usable dry camp site");
            Check(models.Any(m=>SceneSurface.Key(m).StartsWith("river-bank-silt")),"River banks use graduated silt geometry instead of the broad gravel carpet");
            Check(models.Any(m=>SceneSurface.Key(m).StartsWith("flag-fabric")),"Faction banners use curved cloth geometry rather than rectangular solid boxes");
            Check(models.Select(m=>(MeshGeometry3D)m.Geometry).All(m=>m.Positions.Count<=20001&&m.Normals.All(n=>double.IsFinite(n.Length)&&Math.Abs(n.Length-1)<.005)),"New camp, river and human geometry stays bounded with finite unit normals");
            Check(ActorAssets.Load(7)[0].Indices.Length/3==12000&&ActorAssets.Load(7)[1].Indices.Length/3<=1600,"Anatomical people use bounded 12000 and 1600 triangle detail levels");
            await Capture("camp-strategic");map.ResetCamera();map.Center(capital.Tile);map.Zoom(.20);map.Orbit(0,-20);await Capture("camp-close");
            var unit=s.Units.First(u=>u.Owner==0&&!Navy.IsShip(u));Select(unit.Tile);selectedUnit=unit.Id;Refresh();map.InspectUnit(unit);await Capture("forager-close");map.Orbit(130,0);await Capture("forager-reverse");
            Check(SaveGame.Serialize(state)==original,"Visual geometry, anatomical actors and inspection preserve exact campaign state and RNG");
            if(map.Photographic is{} renderer)
            {
                int uploads=renderer.SceneUploads;var times=new List<double>();
                for(int i=0;i<12;i++){map.Orbit(2,0);map.RenderGpuFrame();times.Add(renderer.LastFrameMilliseconds);}
                Check(renderer.SceneUploads==uploads,"Orbiting the rebuilt scene does not re-upload meshes");
                File.WriteAllText(Path.Combine(folder,"frame-times.txt"),$"median {times.Order().ElementAt(times.Count/2):F1} ms; max {times.Max():F1} ms; local device only; {renderer.Image!.PixelWidth}x{renderer.Image.PixelHeight}");
            }
            Width=1200;Height=800;await Task.Delay(180);root.UpdateLayout();map.ResetCamera();map.Center(capital.Tile);map.Zoom(.25);await Capture("camp-compact");
            Check(map.ActualWidth>650&&map.ActualHeight>450,"The scene remains usable in a compact game window");
            checks.Add($"{checks.Count} art review desktop integration checks passed. Visual quality requires reviewing the PNGs.");File.WriteAllLines(Path.Combine(folder,"ui-smoke.txt"),checks);Close();
        }
        catch(Exception ex){checks.Add(ex.ToString());File.WriteAllLines(Path.Combine(folder,"ui-smoke.txt"),checks);if(!arguments.Contains("--smoke-force-failure"))await Capture("art-review-failure");Application.Current.Shutdown(3);}
    }
}
