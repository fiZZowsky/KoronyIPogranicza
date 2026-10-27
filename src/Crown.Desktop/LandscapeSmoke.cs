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
    private async Task LandscapeSmoke(string folder)
    {
        Directory.CreateDirectory(folder);var checks=new List<string>();
        void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);checks.Add("PASS "+message);}
        async Task Capture(string name)
        {
            root.UpdateLayout();await Task.Delay(120);map?.RenderGpuFrame();
            var image=new RenderTargetBitmap((int)root.ActualWidth,(int)root.ActualHeight,96,96,PixelFormats.Pbgra32);image.Render(root);
            var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(image));string path=Path.Combine(folder,name+".png");
            using(var output=File.Create(path+".tmp"))encoder.Save(output);File.Move(path+".tmp",path,true);
        }
        string ImageHash()
        {
            var image=map.Photographic!.Image!;var pixels=new byte[image.PixelWidth*image.PixelHeight*4];image.CopyPixels(pixels,image.PixelWidth*4,0);return Convert.ToHexString(SHA256.HashData(pixels));
        }
        IEnumerable<GeometryModel3D> Models(Model3D model)
        {
            if(model is GeometryModel3D geometry)yield return geometry;
            if(model is Model3DGroup group)foreach(var child in group.Children)foreach(var item in Models(child))yield return item;
        }
        try
        {
            if(arguments.Contains("--smoke-force-failure"))throw new InvalidOperationException("Intentional landscape diagnostic failure.");
            var s=World.Create();Narrative.Resolve(s,0);var home=s.Cities.First(c=>c.Owner==0);var center=s.Tiles[home.Tile];
            s.Player.Technologies.UnionWith(EpochCatalog.All.Take(6).SelectMany(e=>e.Technologies));for(int i=0;i<6;i++)s.Player.EpochChoices[i]=1;
            home.Population=18;home.Buildings.UnionWith(["walls","market","granary"]);foreach(var city in s.Cities)city.ProductionPaused=true;
            foreach(var tile in s.Tiles.Where(t=>World.Distance(t,center)<=10))
            {
                tile.Explored=true;double dx=(tile.X-center.X-4)/2.8,dz=(tile.Y-center.Y+2)/2.7;
                if(dx*dx+dz*dz<1&&!s.Cities.Any(c=>c.Tile==tile.Id)&&!s.Units.Any(u=>u.Tile==tile.Id)){tile.Terrain=Terrain.Coast;tile.Resource=Resource.None;tile.Improvement=Improvement.None;tile.River=false;tile.Ruin=false;}
            }
            s.InvalidateLogistics();string original=SaveGame.Serialize(s);var field=TerrainField.Prepare(s);
            Check(ReferenceEquals(field,TerrainField.Prepare(s)),"Unchanged explored geography reuses one immutable height field");
            Check(field.Values.Span.ToArray().All(float.IsFinite),"The shared height texture contains only finite samples");
            var hidden=s.Tiles.First(t=>!t.Explored);var hiddenOriginal=hidden.Terrain;hidden.Terrain=hiddenOriginal==Terrain.Mountain?Terrain.Ocean:Terrain.Mountain;
            Check(ReferenceEquals(field,TerrainField.Prepare(s)),"Changing an unexplored terrain type does not affect the visual field");hidden.Terrain=hiddenOriginal;
            var shore=s.Tiles.First(t=>t.Explored&&t.Terrain==Terrain.Coast&&!s.Cities.Any(c=>c.Tile==t.Id));var sp=Terrain3D.Center(shore);
            Check(field.Sample(sp.X,sp.Y)<Terrain3D.SeaLevel,"The known coast center remains below the visible water plane");
            shore.Terrain=Terrain.Hills;var changed=TerrainField.Prepare(s);
            Check(!ReferenceEquals(field,changed)&&changed.Sample(sp.X,sp.Y)>field.Sample(sp.X,sp.Y)+.3,"Changing explored geography invalidates the field and updates elevation");
            shore.Terrain=Terrain.Coast;field=TerrainField.Prepare(s);Check(SaveGame.Serialize(s)==original,"Preparing and invalidating visual fields does not change gameplay or RNG");
            var lots=WorldScene3D.HistoricLots(home.Tile,home.Population);var smaller=WorldScene3D.HistoricLots(home.Tile,3);
            Check(lots.Count>smaller.Count&&smaller.All(l=>lots.Contains(l)),"Town growth adds stable residential lots without rearranging existing homes");
            bool separated=true;
            for(int i=0;i<lots.Count;i++)for(int j=i+1;j<lots.Count;j++)
            {
                var a=lots[i];var b=lots[j];double ax=a.Yaw==0?a.Width:a.Depth,az=a.Yaw==0?a.Depth:a.Width,bx=b.Yaw==0?b.Width:b.Depth,bz=b.Yaw==0?b.Depth:b.Width;
                if(Math.Abs(a.X-b.X)<(ax+bx)/2+.015&&Math.Abs(a.Z-b.Z)<(az+bz)/2+.015)separated=false;
            }
            Check(separated&&lots.All(l=>Math.Abs(l.X)>.17&&Math.Abs(l.Z)>.17),"Residential footprints are separated and leave the two streets unobstructed");
            Start(s);root.UpdateLayout();await Task.Delay(120);map.ShowResources=false;map.ResetCamera();map.Orbit(15,-8);map.Zoom(.65);map.RenderGpuFrame();
            Check(map.Photographic!=null,"The new terrain and city render on the actual Direct3D device: "+map.GpuFailure);
            Check(ReferenceEquals(map.Scene!.Field,field),"Geometry, camera and the GPU use the same prepared geography");
            Check(Models(map.Scene.Objects).Any(m=>SceneSurface.Key(m).StartsWith("streets"))&&Models(map.Scene.Objects).Any(m=>SceneSurface.Key(m).StartsWith("fortifications-gateway")),"The town contains street surfaces and the archway's real 3D geometry");
            Check(Models(map.Scene.Terrain).Where(m=>SceneSurface.Key(m).StartsWith("distant-mountains")).Sum(m=>((MeshGeometry3D)m.Geometry).TriangleIndices.Count/3)>5000,"The distant horizon has a continuous, detailed mountain surface");
            var renderer=map.Photographic!;string baseline=ImageHash();renderer.ShoreEffects=false;map.RenderGpuFrame();Check(ImageHash()!=baseline,"Depth-aware shores and shallow water change actual rendered pixels");renderer.ShoreEffects=true;
            renderer.EdgeSmoothing=false;map.RenderGpuFrame();Check(ImageHash()!=baseline,"Edge smoothing changes the rendered silhouette pixels");renderer.EdgeSmoothing=true;
            await Capture("landscape-town");
            var cp=Terrain3D.Center(center);var gateway=map.Project(Terrain3D.Ground(s,cp.X+.13,cp.Y+.87,.17));int pick=gateway==null?-1:map.Pick(gateway.Value);
            Check(pick==home.Tile,"The gateway points to its city's logical tile, including the wall edge");
            map.LandscapeCamera();await Capture("landscape-ridges");
            int meshes=renderer.ResourceMeshes;for(int i=0;i<8;i++){map.Orbit(6,0);map.RenderGpuFrame();}
            Check(renderer.ResourceMeshes==meshes,"Repeated camera movement does not accumulate additional GPU meshes");
            Check(SaveGame.Serialize(state)==original,"Camera movement, shore shading and city rendering leave the exact save unchanged");
            Width=1200;Height=800;await Task.Delay(140);root.UpdateLayout();map.RenderGpuFrame();Check(renderer.Image!.PixelWidth>500&&renderer.Image.PixelHeight>350,"The detailed environment remains available in the compact window");await Capture("landscape-compact");
            Width=1600;Height=980;await Task.Delay(120);root.UpdateLayout();map.ResetCamera();map.Orbit(15,-8);map.Zoom(.65);await Capture("landscape-final");
            checks.Add($"{checks.Count} landscape desktop integration checks passed.");File.WriteAllLines(Path.Combine(folder,"ui-smoke.txt"),checks);Close();
        }
        catch(Exception ex){checks.Add(ex.ToString());if(map?.GpuFailure!=null)checks.Add(map.GpuFailure);File.WriteAllLines(Path.Combine(folder,"ui-smoke.txt"),checks);if(!arguments.Contains("--smoke-force-failure"))await Capture("landscape-failure");Application.Current.Shutdown(3);}
    }
}
