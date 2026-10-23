using Crown.Core;
using System.Diagnostics;
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
    private async Task PhotographicSmoke(string folder)
    {
        Directory.CreateDirectory(folder);var checks=new List<string>();var timer=Stopwatch.StartNew();
        void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);checks.Add("PASS "+message);}
        byte[] Pixels(){var image=map.Photographic!.Image!;var data=new byte[image.PixelWidth*image.PixelHeight*4];image.CopyPixels(data,image.PixelWidth*4,0);return data;}
        string Hash()=>Convert.ToHexString(SHA256.HashData(Pixels()));
        async Task Capture(string name)
        {
            root.UpdateLayout();await Task.Delay(120);map?.RenderGpuFrame();
            var image=new RenderTargetBitmap((int)root.ActualWidth,(int)root.ActualHeight,96,96,PixelFormats.Pbgra32);image.Render(root);
            var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(image));string path=Path.Combine(folder,name+".png");
            using(var output=File.Create(path+".tmp"))encoder.Save(output);File.Move(path+".tmp",path,true);
        }
        GameState Valley()
        {
            var s=World.Create();Narrative.Resolve(s,0);var home=s.Cities.First(c=>c.Owner==0);var center=s.Tiles[home.Tile];
            s.Player.Technologies.UnionWith(EpochCatalog.All.Take(6).SelectMany(e=>e.Technologies));for(int i=0;i<6;i++)s.Player.EpochChoices[i]=1;
            home.Population=18;home.Buildings.UnionWith(["walls","market","granary"]);
            foreach(var city in s.Cities)city.ProductionPaused=true;
            foreach(var tile in s.Tiles.Where(t=>World.Distance(t,center)<=12))
            {
                tile.Explored=true;
                double dx=(tile.X-center.X-4)/3.2,dz=(tile.Y-center.Y+2)/3.4;
                if(dx*dx+dz*dz<1 && !s.Cities.Any(c=>c.Tile==tile.Id) && !s.Units.Any(u=>u.Tile==tile.Id)){tile.Terrain=Terrain.Coast;tile.Resource=Resource.None;tile.Improvement=Improvement.None;tile.River=false;tile.Ruin=false;}
                if(tile.Y<center.Y-5&&tile.Y>center.Y-10&&tile.X>center.X&&tile.X<center.X+9){tile.Terrain=Terrain3D.Noise(tile.X*.7,tile.Y*.5,s.Seed)>.5?Terrain.Mountain:Terrain.Hills;tile.Resource=Resource.None;}
            }
            s.InvalidateLogistics();return s;
        }
        try
        {
            if(arguments.Contains("--smoke-force-failure"))throw new InvalidOperationException("Intentional photographic diagnostic failure.");
            var s=Valley();string original=SaveGame.Serialize(s);Start(s);root.UpdateLayout();map.ShowResources=false;map.LandscapeCamera();await Task.Delay(160);map.RenderGpuFrame();
            Check(map.Photographic!=null,"Default game map initializes the hardware renderer: "+(map.GpuFailure??"Direct3D 11"));
            var renderer=map.Photographic!;Check(renderer.FeatureLevel>=0xb000&&renderer.Frames>0,"Hardware feature level 11_0 renders an actual frame");
            Check(renderer.ShadowSize==2048&&renderer.ResourceMeshes>0,"World meshes and a 2048px shadow map are resident on the GPU");
            var pixels=Pixels();Check(pixels.Where((_,i)=>i%4!=3).Max()-pixels.Where((_,i)=>i%4!=3).Min()>100,"GPU image contains a nonempty lit scene with a broad tonal range");
            foreach(string asset in new[]{"oak-foliage-0.21.png","spruce-foliage-0.21.png"})
            {
                var image=BitmapFrame.Create(new Uri("pack://application:,,,/Assets/"+asset));var rgba=new FormatConvertedBitmap(image,PixelFormats.Bgra32,null,0);var bytes=new byte[rgba.PixelWidth*rgba.PixelHeight*4];rgba.CopyPixels(bytes,rgba.PixelWidth*4,0);
                Check(bytes.Where((_,i)=>i%4==3).Any(a=>a<10)&&bytes.Where((_,i)=>i%4==3).Any(a=>a>245),asset+" has real transparent gaps and opaque leaves");
            }
            map.RenderGpuFrame();string baseline=Hash();map.RenderGpuFrame();Check(Hash()==baseline,"Fixed visual time renders deterministically without random simulation changes");
            map.RenderGpuFrame(12);Check(Hash()!=baseline,"Visual time animates water and leaves without advancing a turn");map.RenderGpuFrame();
            renderer.Shadows=false;map.RenderGpuFrame();Check(Hash()!=baseline,"Disabling shadow mapping changes actual pixels");renderer.Shadows=true;
            renderer.MaterialDetail=false;map.RenderGpuFrame();Check(Hash()!=baseline,"Photographic terrain detail changes actual surface pixels");renderer.MaterialDetail=true;
            renderer.Reflections=false;map.RenderGpuFrame();Check(Hash()!=baseline,"The visible lake changes when planar scene reflections are disabled");renderer.Reflections=true;
            renderer.PostProcessing=false;map.RenderGpuFrame();Check(Hash()!=baseline,"Ambient occlusion and highlight processing affect the rendered image");renderer.PostProcessing=true;
            renderer.Exposure=.55f;map.RenderGpuFrame();Check(Hash()!=baseline,"Exposure controls operate on the HDR scene");renderer.Exposure=.87f;
            renderer.Sun=System.Numerics.Vector3.Normalize(new(.8f,.7f,.25f));map.RenderGpuFrame();Check(Hash()!=baseline,"Changing sunlight repositions real world shading");renderer.Sun=System.Numerics.Vector3.Normalize(new(-.65f,.82f,-.35f));
            map.RenderGpuFrame();await Capture("photographic-valley");
            var center=Terrain3D.Center(s.Tiles[s.Cities[0].Tile]);var point=map.Project(Terrain3D.Ground(s,center.X+.16,center.Y,.5));
            int hit=point==null?-1:map.Pick(point.Value);
            Check(point!=null&&map.SelectScreen(point.Value)&&selectedTile==s.Cities[0].Tile,$"GPU presentation retains real geometry picking and city selection (hit {hit}, city {s.Cities[0].Tile})");
            Check(SaveGame.Serialize(state)==original,"Rendering, lighting and visual settings leave the campaign and RNG untouched");
            bool clear=true;
            foreach(var tile in s.Tiles.Where(t=>t.Explored&&t.Terrain is Terrain.Hills or Terrain.Mountain).Take(6))
            {
                map.Center(tile.Id);map.Zoom(.55);map.Orbit(15,-100);var eye=map.Camera.Position;
                clear&=eye.Y>=Math.Max(Terrain3D.SeaLevel,Terrain3D.Height(s,eye.X,eye.Z))+.299;
            }
            Check(clear,"Low camera remains above hills and mountains instead of entering the terrain");map.Center(s.Cities[0].Tile);
            map.ResetCamera();map.RenderGpuFrame();await Capture("photographic-strategy");
            var samples=new List<double>();for(int i=0;i<10;i++){map.Orbit(4,0);map.RenderGpuFrame();samples.Add(renderer.LastFrameMilliseconds);}
            checks.Add($"FRAME SAMPLE: median {samples.Order().ElementAt(5):0.0}ms, maximum {samples.Max():0.0}ms; {renderer.Image!.PixelWidth}x{renderer.Image.PixelHeight}; this machine only, not a frame-rate guarantee.");
            Width=1200;Height=800;await Task.Delay(140);root.UpdateLayout();map.RenderGpuFrame();
            Check(renderer.Image.PixelWidth<1100&&renderer.Image.PixelHeight>350,"Resize recreates GPU targets at the compact client's size");await Capture("photographic-compact");
            Width=1600;Height=980;await Task.Delay(140);root.UpdateLayout();renderer.MaximumWidth=900;map.RenderGpuFrame();
            Check(renderer.Image.PixelWidth<=900,"Balanced rendering bounds the GPU output resolution");renderer.MaximumWidth=1600;
            string path=Path.Combine(folder,"photographic-save.crown");SaveGame.Write(state,path);string before=SaveGame.Serialize(state);LoadPath(path);CloseModal();root.UpdateLayout();await Task.Delay(140);map.RenderGpuFrame();
            Check(SaveGame.Serialize(state)==before&&map.Photographic?.Frames>0,"Existing v19 save reloads into the new GPU renderer without migration");
            Check(Descendants<Button>(map).Any(b=>b.Content?.ToString()=="Grafika")&&Descendants<Button>(map).Any(b=>b.Content?.ToString()=="Krajobraz"),"Graphics and landscape camera controls remain available");
            map.ShowResources=false;map.LandscapeCamera();await Capture("photographic-final");
            checks.Add($"{checks.Count(c=>c.StartsWith("PASS "))} photographic desktop integration checks passed.");checks.Add($"Elapsed {timer.Elapsed.TotalSeconds:0.0}s; Direct3D feature level 0x{map.Photographic!.FeatureLevel:X}; fixed-time diagnostic.");
            File.WriteAllLines(Path.Combine(folder,"ui-smoke.txt"),checks);Close();
        }
        catch(Exception ex){checks.Add(ex.ToString());if(map?.GpuFailure!=null)checks.Add(map.GpuFailure);File.WriteAllLines(Path.Combine(folder,"ui-smoke.txt"),checks);if(!arguments.Contains("--smoke-force-failure"))await Capture("photographic-failure");Application.Current.Shutdown(3);}
    }
}
