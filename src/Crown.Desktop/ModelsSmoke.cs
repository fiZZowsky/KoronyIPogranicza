using Crown.Core;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace Crown.Desktop;

public sealed partial class MainWindow
{
    private async Task ModelsSmoke(string folder)
    {
        Directory.CreateDirectory(folder);var checks=new List<string>();
        void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);checks.Add("PASS "+message);}
        IEnumerable<GeometryModel3D> Models(Model3D model)
        {
            if(model is GeometryModel3D geometry)yield return geometry;
            if(model is Model3DGroup group)foreach(var child in group.Children)foreach(var item in Models(child))yield return item;
        }
        async Task Capture(string name)
        {
            root.UpdateLayout();await Task.Delay(120);map?.RenderGpuFrame();var image=new RenderTargetBitmap((int)root.ActualWidth,(int)root.ActualHeight,96,96,PixelFormats.Pbgra32);image.Render(root);
            var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(image));string path=Path.Combine(folder,name+".png");using(var output=File.Create(path+".tmp"))encoder.Save(output);File.Move(path+".tmp",path,true);
        }
        try
        {
            if(arguments.Contains("--smoke-force-failure"))throw new InvalidOperationException("Intentional models diagnostic failure.");
            var s=World.Create();Narrative.Resolve(s,0);var home=s.Cities.First(c=>c.Owner==0);var capital=s.Tiles[home.Tile];
            s.Player.Technologies.UnionWith(EpochCatalog.All.Take(6).SelectMany(e=>e.Technologies));for(int i=0;i<6;i++)s.Player.EpochChoices[i]=1;
            home.Population=16;home.Buildings.UnionWith(["walls","market"]);foreach(var city in s.Cities)city.ProductionPaused=true;
            foreach(var tile in s.Tiles.Where(t=>World.Distance(t,capital)<=7)){tile.Explored=true;tile.Resource=Resource.None;tile.Ruin=false;tile.Improvement=Improvement.None;tile.River=false;if(World.Distance(tile,capital)<=5)tile.Terrain=Terrain.Grassland;}
            var resources=Enum.GetValues<Resource>().Where(r=>r!=Resource.None).ToArray();var deposits=new List<Tile>();
            for(int i=0;i<resources.Length;i++){var tile=s.Tiles[(capital.Y+1+(i/3)*2)*s.Width+capital.X+1+i%3];tile.Resource=resources[i];tile.Owner=0;deposits.Add(tile);}
            foreach(var tile in s.Tiles.Where(t=>World.Distance(t,capital)==4&&t.X<capital.X+1))tile.Terrain=Terrain.Forest;
            s.Units.Clear();foreach(var role in Enum.GetValues<UnitKind>())
            {
                var tile=s.Tiles[(capital.Y-1)*s.Width+capital.X+1+(int)role%3];var model=UnitModels.All.LastOrDefault(m=>m.Role==role&&m.Era<=5);Engine.AddUnit(s,0,tile.Id,role,model?.Id);
            }
            s.InvalidateLogistics();string original=SaveGame.Serialize(s);Start(s);root.UpdateLayout();await Task.Delay(140);map.ShowResources=true;map.Center(deposits[1].Id);map.Zoom(.8);map.RenderGpuFrame();
            Check(map.Photographic!=null,"The new living models render on the actual Direct3D device: "+map.GpuFailure);
            var models=Models(map.Scene!.Objects).ToArray();
            Check(models.Any(m=>SceneSurface.Key(m).StartsWith("skin"))&&models.Any(m=>SceneSurface.Key(m).StartsWith("eyes"))&&models.Any(m=>SceneSurface.Key(m).StartsWith("boots")),"Human formations contain shaped bodies, facial details, hands and boots");
            Check(models.Any(m=>SceneSurface.Key(m).StartsWith("horse-hooves"))&&models.Any(m=>SceneSurface.Key(m).StartsWith("horse-mane-tail"))&&models.Any(m=>SceneSurface.Key(m).StartsWith("horse-eyes")),"Horses have actual hoof, mane, tail and eye geometry");
            Check(models.Any(m=>SceneSurface.Key(m).StartsWith("ground-grass"))&&models.Any(m=>SceneSurface.Key(m).StartsWith("bark-"))&&models.Any(m=>SceneSurface.Key(m).StartsWith("plant-leaves")),"Forests, grass tufts and smaller leaves have distinct spatial geometry");
            Check(new[]{"wheat-grain","iron-ore","horse-coat-","stone-outcrop","salt-crystals","dye-petals"}.All(key=>models.Any(m=>SceneSurface.Key(m).StartsWith(key))),"All six resource types have their own recognizable model components");
            Check(models.Select(m=>(MeshGeometry3D)m.Geometry).All(m=>m.Positions.Count<=20001&&m.Normals.All(n=>double.IsFinite(n.X)&&Math.Abs(n.Length-1)<.001)),"New resource and living meshes remain bounded with finite unit normals");
            Check(!models.Any(m=>SceneSurface.Key(m).StartsWith("resource-Dyes")),"Dyes are represented by flowering plants rather than the former colored sphere");
            await Capture("models-resources");
            foreach(var tile in deposits)
            {
                Select(tile.Id);map.Center(tile.Id);map.ResetCamera();map.Zoom(.22);map.RenderGpuFrame();root.UpdateLayout();
                Check(Descendants<Border>(map).Any(b=>b.Tag is ResourceMarker marker&&marker.Tile==tile.Id&&Descendants<TextBlock>(b).Any(t=>t.Text==Crown.Core.Content.ResourceName(tile.Resource))),"Actual map badge shows the full resource name and icon: "+Crown.Core.Content.ResourceName(tile.Resource));
                Check(Descendants<TextBlock>(side).Any(t=>t.Text==Crown.Core.Content.ResourceName(tile.Resource))&&Descendants<TextBlock>(side).Any(t=>t.Text.Contains(ResourcePresentation.Bonus(tile.Resource))),"Selected resource explains its identity and actual field bonus: "+Crown.Core.Content.ResourceName(tile.Resource));
                await Capture("models-resource-"+tile.Resource);
            }
            Check(!Descendants<TextBlock>(map).Any(t=>t.Text=="Fe"),"The map never substitutes a chemical abbreviation for the iron resource name");
            map.ShowResources=false;map.InvalidateVisual();Check(!Descendants<Border>(map).Any(b=>b.Tag is ResourceMarker),"The resources toggle hides badges while keeping the real world models");map.ShowResources=true;map.InvalidateVisual();
            Descendants<Button>(root).Single(b=>b.Content?.ToString()=="Legenda").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));root.UpdateLayout();
            Check(overlay!=null&&resources.All(r=>Descendants<TextBlock>(overlay).Any(t=>t.Text==Crown.Core.Content.ResourceName(r))),"The real legend button opens names and pictograms for every resource");await Capture("models-legend");CloseModal();
            var horseTile=deposits.Single(t=>t.Resource==Resource.Horses);Select(horseTile.Id);map.Center(horseTile.Id);map.ResetCamera();map.Zoom(.22);var anchor=WorldScene3D.ResourceAnchor(s,horseTile);var hp=new Point(anchor.X-.10,anchor.Y-.075);var point=map.Project(Terrain3D.Ground(s,hp.X,hp.Y,.17));
            Check(point!=null&&map.Pick(point.Value)==horseTile.Id,"Picking a horse resource model resolves its actual logical tile");
            var soldier=s.Units.First(u=>u.Kind==UnitKind.Spearmen);Select(soldier.Tile);map.ResetCamera();map.Zoom(.22);map.RenderGpuFrame();await Capture("models-people");
            Check(map.CameraDistance<3,"The camera can approach closely enough to inspect the new models");
            Check(SaveGame.Serialize(state)==original,"Resource labels, legend, new models and camera inspection preserve gameplay and RNG");
            var hidden=s.Tiles.First(t=>!t.Explored);var hiddenResource=hidden.Resource;hidden.Resource=Resource.Iron;Refresh();Check(!Descendants<Border>(map).Any(b=>b.Tag is ResourceMarker marker&&marker.Tile==hidden.Id),"An undiscovered iron deposit never gains a revealing map badge");hidden.Resource=hiddenResource;
            Width=1200;Height=800;await Task.Delay(140);root.UpdateLayout();ShowResourceLegend();Check(overlay!=null&&Descendants<ScrollViewer>(overlay).Any(),"The resource legend remains scrollable in the compact window");await Capture("models-compact");CloseModal();
            Width=1600;Height=980;await Task.Delay(120);root.UpdateLayout();Select(deposits[1].Id);map.ResetCamera();map.Center(deposits[1].Id);map.Zoom(.55);map.RenderGpuFrame();await Capture("models-final");
            checks.Add($"{checks.Count} models desktop integration checks passed.");File.WriteAllLines(Path.Combine(folder,"ui-smoke.txt"),checks);Close();
        }
        catch(Exception ex){checks.Add(ex.ToString());if(map?.GpuFailure!=null)checks.Add(map.GpuFailure);File.WriteAllLines(Path.Combine(folder,"ui-smoke.txt"),checks);if(!arguments.Contains("--smoke-force-failure"))await Capture("models-failure");Application.Current.Shutdown(3);}
    }
}
