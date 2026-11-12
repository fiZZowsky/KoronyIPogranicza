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
    private async Task EnvironmentDetailSmoke(string folder)
    {
        Directory.CreateDirectory(folder);var checks=new List<string>();
        void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);checks.Add("PASS "+message);}
        IEnumerable<GeometryModel3D> Models(Model3D m){if(m is GeometryModel3D g)yield return g;if(m is Model3DGroup group)foreach(var child in group.Children)foreach(var model in Models(child))yield return model;}
        string Hash(){var image=map.Photographic!.Image!;var bytes=new byte[image.PixelWidth*image.PixelHeight*4];image.CopyPixels(bytes,image.PixelWidth*4,0);return Convert.ToHexString(SHA256.HashData(bytes));}
        async Task Capture(string name)
        {
            root.UpdateLayout();await Task.Delay(140);map.RenderGpuFrame();var image=new RenderTargetBitmap((int)root.ActualWidth,(int)root.ActualHeight,96,96,PixelFormats.Pbgra32);image.Render(root);
            var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(image));using var output=File.Create(Path.Combine(folder,name+".png"));encoder.Save(output);
        }
        try
        {
            if(arguments.Contains("--smoke-force-failure"))throw new InvalidOperationException("Intentional environment detail diagnostic failure.");
            var atlas=UnderstoryMaterials.Image;var rgba=new FormatConvertedBitmap(atlas,PixelFormats.Bgra32,null,0);var pixels=new byte[rgba.PixelWidth*rgba.PixelHeight*4];rgba.CopyPixels(pixels,rgba.PixelWidth*4,0);
            Check(atlas.IsFrozen&&atlas.PixelWidth>=1024&&atlas.PixelWidth==atlas.PixelHeight,"The shared vegetation atlas is frozen, detailed and square");
            for(int variant=0;variant<4;variant++)
            {
                int clear=0,solid=0,w=rgba.PixelWidth/2,h=rgba.PixelHeight/2;
                for(int y=0;y<h;y+=3)for(int x=0;x<w;x+=3){byte alpha=pixels[((variant/2*h+y)*rgba.PixelWidth+variant%2*w+x)*4+3];if(alpha<16)clear++;if(alpha>220)solid++;}
                Check(clear>500&&solid>500,$"Vegetation quadrant {variant} contains real alpha gaps and solid leaf detail");
                Check(UnderstoryMaterials.Brush(variant).IsFrozen&&ReferenceEquals(UnderstoryMaterials.Brush(variant),UnderstoryMaterials.Brush(variant)),"Vegetation variant "+variant+" reuses a frozen material");
            }
            var s=World.Create();Narrative.Resolve(s,0);var home=s.Cities.First(c=>c.Owner==0);var capital=s.Tiles[home.Tile];
            foreach(var tile in s.Tiles.Where(t=>World.Distance(t,capital)<=5))
            {
                tile.Explored=true;tile.Resource=Resource.None;tile.Ruin=false;tile.Improvement=Improvement.None;
                if(World.Distance(tile,capital)>=2&&tile.Id%3==0){tile.Terrain=Terrain.Forest;tile.River=false;}
            }
            s.InvalidateLogistics();string original=SaveGame.Serialize(s);Start(s);root.UpdateLayout();await Task.Delay(150);map.RenderGpuFrame();
            Check(arguments.Contains("--no-gpu")?map.Photographic==null:map.Photographic!=null,"The requested backend renders the vegetation scene: "+map.GpuFailure);
            var models=Models(map.Scene!.Objects).ToArray();var plants=models.Where(m=>UnderstoryMaterials.Index(SceneSurface.Key(m))>=0).ToArray();
            Check(Enumerable.Range(0,4).All(i=>plants.Any(m=>UnderstoryMaterials.Index(SceneSurface.Key(m))==i)),"Meadow grass, woodland ferns, bank sedges and herbs all have actual scene geometry");
            Check(plants.All(m=>m.Material is DiffuseMaterial{Brush:ImageBrush}&&m.BackMaterial!=null),"Every vegetation card has its matching texture and a visible back face");
            Check(plants.All(m=>((MeshGeometry3D)m.Geometry).Normals.All(n=>double.IsFinite(n.Length)&&Math.Abs(n.Length-1)<.001)),"Vegetation geometry has finite unit normals");
            Check(plants.All(m=>m.GetValue(SceneSurface.TileIdsProperty) is int[] ids&&ids.All(id=>id>=0&&s.Tiles[id].Explored)),"Vegetation batches are attached only to explored logical tiles");
            Check(models.Any(m=>SceneSurface.Key(m).StartsWith("detail-camp-basket-wood"))&&models.Any(m=>SceneSurface.Key(m).StartsWith("detail-camp-stacked-wood")),"The initial camp contains real storage baskets and stacked wood");
            Select(home.Tile);map.ResetCamera();map.Zoom(.22);map.Orbit(12,-18);await Capture("environment-detail-camp");
            var forest=s.Tiles.Where(t=>t.Explored&&t.Terrain==Terrain.Forest).MinBy(t=>World.Distance(t,capital))!;Select(forest.Id);map.ResetCamera();map.Zoom(.20);map.Orbit(30,-18);await Capture("environment-detail-forest");
            Check(SaveGame.Serialize(state)==original,"New plants, props, forest selection and close inspection preserve campaign state and RNG");
            if(map.Photographic is{} renderer)
            {
                renderer.Weather=VisualWeather.Clear;map.RenderGpuFrame();string still=Hash();int resources=renderer.LiveResources,uploads=renderer.SceneUploads;
                map.RenderGpuFrame(4);Check(Hash()!=still,"Visual time changes the actual foliage and water pixels");map.RenderGpuFrame();Check(Hash()==still,"A fixed visual time reproduces the exact vegetation scene");
                for(int i=0;i<16;i++){map.Orbit(1,0);map.RenderGpuFrame(i*.1);}
                Check(renderer.LiveResources==resources&&renderer.SceneUploads==uploads,"Animating plants and orbiting the camera neither leak resources nor rebuild meshes");
                map.ResetCamera();map.Zoom(4);renderer.AutomaticDetail=false;map.RenderGpuFrame();int full=renderer.Statistics.MainTriangles;renderer.AutomaticDetail=true;map.RenderGpuFrame();
                Check(renderer.Statistics.MainTriangles<full,"Automatic distance detail reduces actual submitted vegetation and prop geometry");
            }
            var hidden=s.Tiles.First(t=>!t.Explored);var was=hidden.Terrain;hidden.Terrain=Terrain.Forest;Refresh();
            Check(!Models(map.Scene!.Objects).Where(m=>UnderstoryMaterials.Index(SceneSurface.Key(m))>=0).Any(m=>((int[])m.GetValue(SceneSurface.TileIdsProperty)).Contains(hidden.Id)),"Unknown woodland creates no revealing vegetation");hidden.Terrain=was;Refresh();
            Width=1200;Height=800;await Task.Delay(180);root.UpdateLayout();map.ResetCamera();map.Center(home.Tile);map.Zoom(.25);await Capture("environment-detail-compact");
            Check(map.ActualWidth>650&&map.ActualHeight>450,"The detailed environment retains a usable compact viewport");
            Check(SaveGame.Serialize(state)==original,"The complete detail diagnostic leaves the exact original campaign unchanged");
            checks.Add($"{checks.Count} environment detail desktop integration checks passed. Inspect PNGs separately for visual quality.");File.WriteAllLines(Path.Combine(folder,"ui-smoke.txt"),checks);Close();
        }
        catch(Exception ex){checks.Add(ex.ToString());File.WriteAllLines(Path.Combine(folder,"ui-smoke.txt"),checks);if(!arguments.Contains("--smoke-force-failure")&&map!=null)await Capture("environment-detail-failure");Application.Current.Shutdown(3);}
    }
}
