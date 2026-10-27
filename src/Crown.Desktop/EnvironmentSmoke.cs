using Crown.Core;
using System.IO;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Crown.Desktop;

public sealed partial class MainWindow
{
    private async Task EnvironmentSmoke(string folder)
    {
        Directory.CreateDirectory(folder);var checks=new List<string>();
        void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);checks.Add("PASS "+message);}
        string Hash(){var image=map.Photographic!.Image!;var bytes=new byte[image.PixelWidth*image.PixelHeight*4];image.CopyPixels(bytes,image.PixelWidth*4,0);return Convert.ToHexString(SHA256.HashData(bytes));}
        async Task Capture(string name)
        {
            root.UpdateLayout();await Task.Delay(120);map?.RenderGpuFrame();var image=new RenderTargetBitmap((int)root.ActualWidth,(int)root.ActualHeight,96,96,PixelFormats.Pbgra32);image.Render(root);
            var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(image));string path=Path.Combine(folder,name+".png");using(var output=File.Create(path+".tmp"))encoder.Save(output);File.Move(path+".tmp",path,true);
        }
        try
        {
            if(arguments.Contains("--smoke-force-failure"))throw new InvalidOperationException("Intentional environment diagnostic failure.");
            var s=World.Create();Narrative.Resolve(s,0);var home=s.Cities.First(c=>c.Owner==0);var center=s.Tiles[home.Tile];
            s.Player.Technologies.UnionWith(EpochCatalog.All.Take(6).SelectMany(e=>e.Technologies));for(int i=0;i<6;i++)s.Player.EpochChoices[i]=1;
            home.Population=16;home.Buildings.UnionWith(["walls","market"]);foreach(var city in s.Cities)city.ProductionPaused=true;
            foreach(var tile in s.Tiles.Where(t=>World.Distance(t,center)<=9))tile.Explored=true;
            foreach(var tile in World.Neighbors(s,home.Tile).Where(t=>!s.Cities.Any(c=>c.Tile==t.Id)).Take(3)){tile.Terrain=Terrain.Forest;tile.Improvement=Improvement.None;}
            s.InvalidateLogistics();Start(s);root.UpdateLayout();await Task.Delay(140);map.ShowResources=false;map.Orbit(15,-8);map.Zoom(.66);map.RenderGpuFrame();
            Check(map.Photographic!=null,"Seasonal environment uses the real Direct3D renderer: "+map.GpuFailure);
            var renderer=map.Photographic!;var terrain=map.Scene!.Terrain;var field=map.Scene.Field;var seasonalImages=new HashSet<string>();
            for(int season=0;season<4;season++)
            {
                s.Turn=season+1;string before=SaveGame.Serialize(s);Refresh();root.UpdateLayout();renderer.Weather=VisualWeather.Clear;map.RenderGpuFrame();seasonalImages.Add(Hash());
                Check(renderer.SeasonIndex==season&&ReferenceEquals(terrain,map.Scene!.Terrain)&&ReferenceEquals(field,map.Scene.Field),"Season "+season+" updates shader data without rebuilding terrain or revealing geography");
                Check(SaveGame.Serialize(state)==before,"Season "+season+" renders without changing the campaign or RNG");
                await Capture("environment-season-"+season);
            }
            Check(seasonalImages.Count==4,"Spring, summer, autumn and winter have four distinct actual GPU images");
            string winter=Hash();renderer.SeasonalColors=false;map.RenderGpuFrame();Check(Hash()!=winter,"Disabling seasonal surfaces changes actual winter pixels");renderer.SeasonalColors=true;
            renderer.Weather=VisualWeather.Seasonal;map.RenderGpuFrame();Check(renderer.Environment.Snowfall>0&&renderer.WeatherVertices>0,"Automatic winter creates real snow particle triangles");
            string snow=Hash();map.RenderGpuFrame();Check(Hash()==snow,"A fixed visual time reproduces the exact snowy image");
            map.RenderGpuFrame(5);Check(Hash()!=snow,"Snowfall advances with visual time without advancing a turn");
            renderer.Weather=VisualWeather.Clear;map.RenderGpuFrame();Check(renderer.WeatherVertices==0&&Hash()!=snow,"Clear weather releases the particle buffer and changes actual pixels");
            renderer.Weather=VisualWeather.Rain;map.RenderGpuFrame();Check(renderer.Environment.Rain>0&&renderer.WeatherVertices>0&&Hash()!=snow,"Manual rain creates depth-tested streak geometry and a distinct rendered image");await Capture("environment-rain");
            int resources=renderer.LiveResources;for(int i=0;i<24;i++)map.RenderGpuFrame(i*.2);
            Check(renderer.LiveResources==resources,"Replacing precipitation buffers over 24 frames does not leak native GPU resources");
            renderer.Weather=VisualWeather.Clear;map.RenderGpuFrame();int clearResources=renderer.LiveResources;
            for(int i=0;i<8;i++){renderer.Weather=i%2==0?VisualWeather.Rain:VisualWeather.Clear;map.RenderGpuFrame();}
            Check(renderer.LiveResources==clearResources,"Repeatedly enabling and clearing precipitation returns to the same native resource count");
            renderer.Weather=VisualWeather.Snow;map.RenderGpuFrame();var cp=Terrain3D.Center(center);var point=map.Project(Terrain3D.Ground(s,cp.X+.16,cp.Y,.5));
            Check(point!=null&&map.Pick(point.Value)==home.Tile,"Weather particles do not intercept picking of the city's real geometry");
            string original=SaveGame.Serialize(s);map.Orbit(25,-3);map.RenderGpuFrame(7);Check(SaveGame.Serialize(state)==original,"Weather controls, animation and camera motion leave the exact save unchanged");
            string save=Path.Combine(folder,"environment-save.crown");SaveGame.Write(s,save);LoadPath(save);CloseModal();root.UpdateLayout();await Task.Delay(140);map.RenderGpuFrame();
            Check(SaveGame.Serialize(state)==original&&map.Photographic?.SeasonIndex==3,"A v19 winter save reloads with the correct automatic season");
            Width=1200;Height=800;await Task.Delay(140);root.UpdateLayout();map.RenderGpuFrame();Check(map.Photographic!.Image!.PixelWidth>500,"The seasonal scene remains available in the compact window");await Capture("environment-compact");
            Width=1600;Height=980;await Task.Delay(120);root.UpdateLayout();map.ShowResources=false;map.ResetCamera();map.Orbit(15,-8);map.Zoom(.66);await Capture("environment-final");
            checks.Add($"{checks.Count} environment desktop integration checks passed.");File.WriteAllLines(Path.Combine(folder,"ui-smoke.txt"),checks);Close();
        }
        catch(Exception ex){checks.Add(ex.ToString());if(map?.GpuFailure!=null)checks.Add(map.GpuFailure);File.WriteAllLines(Path.Combine(folder,"ui-smoke.txt"),checks);if(!arguments.Contains("--smoke-force-failure"))await Capture("environment-failure");Application.Current.Shutdown(3);}
    }
}
