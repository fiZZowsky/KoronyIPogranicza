using Crown.Core;
using System.IO;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace Crown.Desktop;

public sealed partial class MainWindow
{
    private async Task RiverSmoke(string folder)
    {
        Directory.CreateDirectory(folder);var checks=new List<string>();
        void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);checks.Add("PASS "+message);}
        IEnumerable<GeometryModel3D> Models(Model3D model)
        {
            if(model is GeometryModel3D geometry)yield return geometry;
            if(model is Model3DGroup group)foreach(var child in group.Children)foreach(var item in Models(child))yield return item;
        }
        string Hash(){var image=map.Photographic!.Image!;var bytes=new byte[image.PixelWidth*image.PixelHeight*4];image.CopyPixels(bytes,image.PixelWidth*4,0);return Convert.ToHexString(SHA256.HashData(bytes));}
        async Task Capture(string name)
        {
            root.UpdateLayout();await Task.Delay(120);map?.RenderGpuFrame();var image=new RenderTargetBitmap((int)root.ActualWidth,(int)root.ActualHeight,96,96,PixelFormats.Pbgra32);image.Render(root);
            var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(image));string path=Path.Combine(folder,name+".png");using(var output=File.Create(path+".tmp"))encoder.Save(output);File.Move(path+".tmp",path,true);
        }
        try
        {
            if(arguments.Contains("--smoke-force-failure"))throw new InvalidOperationException("Intentional river diagnostic failure.");
            var s=World.Create();Narrative.Resolve(s,0);var home=s.Cities.First(c=>c.Owner==0);var capital=s.Tiles[home.Tile];
            s.Player.Technologies.UnionWith(EpochCatalog.All.Take(6).SelectMany(e=>e.Technologies));for(int i=0;i<6;i++)s.Player.EpochChoices[i]=1;
            home.Population=15;home.Buildings.UnionWith(["walls","market"]);foreach(var city in s.Cities)city.ProductionPaused=true;
            foreach(var tile in s.Tiles.Where(t=>World.Distance(t,capital)<=7))
            {
                tile.Explored=true;tile.River=false;
                if(World.Distance(tile,capital)<=4){tile.Terrain=Terrain.Grassland;tile.Resource=Resource.None;tile.Ruin=false;tile.Improvement=Improvement.None;}
            }
            capital.River=true;capital.Road=true;capital.RoadOwner=0;var east=s.Tiles[home.Tile+1];east.Road=true;east.RoadOwner=0;east.Owner=0;
            s.Tiles[(capital.Y-1)*s.Width+capital.X+1].River=true;s.Tiles[(capital.Y+1)*s.Width+capital.X+1].River=true;
            var reach=s.Tiles[(capital.Y+2)*s.Width+capital.X+2];reach.River=true;s.Tiles[reach.Id+s.Width].River=true;s.Tiles[reach.Id+s.Width*2].River=true;
            s.InvalidateLogistics();string original=SaveGame.Serialize(s);var field=TerrainField.Prepare(s);var network=field.Rivers;
            Check(network.Paths.Count>=3,"Known rivers produce sampled reaches and connected neighbors");
            Check(network.Paths.All(p=>s.Tiles[p.From].Explored&&s.Tiles[p.To].Explored),"River geometry uses only explored endpoints");
            Check(network.Paths.SelectMany(p=>p.Samples).All(p=>double.IsFinite(p.Water)&&Math.Abs(p.Side.Length-1)<.00001),"Water elevations and normalized cross-sections remain finite");
            var hidden=s.Tiles.First(t=>!t.Explored);hidden.River=!hidden.River;
            Check(ReferenceEquals(field,TerrainField.Prepare(s)),"Changing a hidden river flag neither rebuilds nor reveals a channel");hidden.River=!hidden.River;
            var a=Terrain3D.Center(capital);var b=Terrain3D.Center(east);var crossing=network.Crossing(a,b);
            Check(crossing!=null,"The real city road intersects its riverside channel");
            var water=network.Paths.First(p=>p.From==home.Tile).Samples[6];
            Check(field.Sample(water.Position.X,water.Position.Y)<water.Water-.025,"River water lies above a carved bed rather than a raised terrain ribbon");
            capital.River=false;var dry=TerrainField.Prepare(s);
            Check(!ReferenceEquals(field,dry)&&dry.Sample(water.Position.X,water.Position.Y)>field.Sample(water.Position.X,water.Position.Y)+.05,"Changing a known river invalidates terrain and removes its channel");capital.River=true;field=TerrainField.Prepare(s);
            var clone=SaveGame.Deserialize(original);var copied=TerrainField.Prepare(clone);
            Check(field.Values.Span.SequenceEqual(copied.Values.Span)&&field.Rivers.Paths.Zip(copied.Rivers.Paths).All(p=>p.First.Samples.SequenceEqual(p.Second.Samples)),"The same save produces identical river geometry and terrain");
            int junction=reach.Id+s.Width;var joined=network.Paths.Where(p=>p.From==junction||p.To==junction).SelectMany(p=>new[]{(Id:p.From,Sample:p.Samples[0]),(Id:p.To,Sample:p.Samples[^1])}).Where(p=>p.Id==junction).Select(p=>p.Sample).ToArray();
            Check(joined.Length>=2&&joined.All(p=>p.Position==joined[0].Position&&p.Water==joined[0].Water),"Reaches sharing a known node meet at identical positions and water levels");
            Check(SaveGame.Serialize(s)==original,"Channel preparation leaves campaign decisions, roads and RNG unchanged");
            Start(s);root.UpdateLayout();await Task.Delay(140);map.ShowResources=false;map.Center(east.Id);map.Orbit(150,-8);map.Zoom(.58);map.RenderGpuFrame();
            Check(map.Photographic!=null,"River scene renders on the real Direct3D device: "+map.GpuFailure);
            var models=Models(map.Scene!.Objects).ToArray();var decks=models.Where(m=>SceneSurface.Key(m).StartsWith("bridge-deck-wood")).ToArray();
            Check(decks.Length>0&&models.Any(m=>SceneSurface.Key(m).StartsWith("bridge-fortification")),"Existing roads generate a planked bridge with stone supports and parapets");
            Check(models.Any(m=>SceneSurface.Key(m).StartsWith("river-reeds"))&&models.Any(m=>SceneSurface.Key(m).StartsWith("river-bank-gravel")),"The river contains actual bank and reed geometry");
            double deck=decks.SelectMany(m=>((MeshGeometry3D)m.Geometry).Positions).Max(p=>p.Y);var screen=map.Project(new(crossing!.Position.X,deck-.008,crossing.Position.Y));int pick=screen==null?-1:map.Pick(screen.Value);
            Check(pick==Terrain3D.TileAt(s,crossing.Position.X,crossing.Position.Y),"Picking the visible bridge returns the underlying logical tile");
            map.RenderGpuFrame();string still=Hash();map.RenderGpuFrame(8);Check(Hash()!=still,"Visual time changes the actual water and environment pixels");map.RenderGpuFrame();Check(Hash()==still,"Fixed visual time keeps river rendering deterministic");
            await Capture("river-bridge");int meshes=map.Photographic!.ResourceMeshes;for(int i=0;i<6;i++){map.Orbit(4,0);map.RenderGpuFrame();}Check(map.Photographic.ResourceMeshes==meshes,"Camera movement does not accumulate river GPU buffers");
            Check(SaveGame.Serialize(state)==original,"Bridges, river animation and camera motion do not change the save");
            Width=1200;Height=800;await Task.Delay(140);root.UpdateLayout();map.RenderGpuFrame();Check(map.Photographic.Image!.PixelWidth>500,"Rivers and bridges remain available in the compact window");await Capture("river-compact");
            Width=1600;Height=980;await Task.Delay(120);root.UpdateLayout();map.Center(east.Id);map.ResetCamera();map.Orbit(150,-8);map.Zoom(.58);await Capture("river-final");
            checks.Add($"{checks.Count} river desktop integration checks passed.");File.WriteAllLines(Path.Combine(folder,"ui-smoke.txt"),checks);Close();
        }
        catch(Exception ex){checks.Add(ex.ToString());if(map?.GpuFailure!=null)checks.Add(map.GpuFailure);File.WriteAllLines(Path.Combine(folder,"ui-smoke.txt"),checks);if(!arguments.Contains("--smoke-force-failure"))await Capture("river-failure");Application.Current.Shutdown(3);}
    }
}
