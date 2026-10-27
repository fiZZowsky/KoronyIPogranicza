using Crown.Core;
using System.IO;
using System.Numerics;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace Crown.Desktop;

public sealed partial class MainWindow
{
    private async Task RenderingSmoke(string folder)
    {
        Directory.CreateDirectory(folder);var checks=new List<string>();
        void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);checks.Add("PASS "+message);}
        IEnumerable<GeometryModel3D> Models(Model3D model)
        {
            if(model is GeometryModel3D geometry)yield return geometry;
            if(model is Model3DGroup group)foreach(var child in group.Children)foreach(var item in Models(child))yield return item;
        }
        int Triangles(Model3D model)=>Models(model).Sum(m=>((MeshGeometry3D)m.Geometry).TriangleIndices.Count/3);
        string Hash(){var image=map.Photographic!.Image!;var bytes=new byte[image.PixelWidth*image.PixelHeight*4];image.CopyPixels(bytes,image.PixelWidth*4,0);return Convert.ToHexString(SHA256.HashData(bytes));}
        async Task Capture(string name)
        {
            root.UpdateLayout();await Task.Delay(100);map?.RenderGpuFrame();var image=new RenderTargetBitmap((int)root.ActualWidth,(int)root.ActualHeight,96,96,PixelFormats.Pbgra32);image.Render(root);
            var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(image));using var file=File.Create(Path.Combine(folder,name+".png"));encoder.Save(file);
        }
        void Menu(string label)
        {
            Descendants<Button>(map).Single(b=>b.Content?.ToString()=="Grafika").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            var menu=map.ActiveGraphicsMenu!;menu.Items.OfType<MenuItem>().Single(i=>i.Header.ToString()!.StartsWith(label)).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));menu.IsOpen=false;map.RenderGpuFrame();
        }
        try
        {
            if(arguments.Contains("--smoke-force-failure"))throw new InvalidOperationException("Intentional rendering diagnostic failure.");
            var s=World.Create();Narrative.Resolve(s,0);var home=s.Cities.First(c=>c.Owner==0);var capital=s.Tiles[home.Tile];home.Population=18;home.Buildings.UnionWith(["walls","market"]);
            s.Player.Technologies.UnionWith(EpochCatalog.All.Take(6).SelectMany(e=>e.Technologies));for(int i=0;i<6;i++)s.Player.EpochChoices[i]=1;
            foreach(var city in s.Cities)city.ProductionPaused=true;
            foreach(var tile in s.Tiles.Where(t=>World.Distance(t,capital)<=10))
            {
                tile.Explored=true;
                if(World.Distance(tile,capital) is >=3 and <=7&&tile.Id%3==0&&!s.Cities.Any(c=>c.Tile==tile.Id)&&!s.Units.Any(u=>u.Tile==tile.Id))
                {tile.Terrain=Terrain.Forest;tile.River=false;tile.Ruin=false;tile.Improvement=Improvement.None;tile.Resource=Resource.None;}
            }
            var iron=World.Neighbors(s,capital.Id).First(t=>t.Passable&&!s.Cities.Any(c=>c.Tile==t.Id));iron.Resource=Resource.Iron;iron.Terrain=Terrain.Grassland;iron.Ruin=false;iron.Improvement=Improvement.None;
            s.InvalidateLogistics();string original=SaveGame.Serialize(s);Start(s);root.UpdateLayout();await Task.Delay(140);map.RenderGpuFrame();
            Check(map.Photographic!=null,"Spatial rendering initializes on the actual Direct3D device: "+map.GpuFailure);var renderer=map.Photographic!;renderer.Weather=VisualWeather.Clear;
            var frustum=new RenderFrustum(Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI/2,1,.1f,100));
            Check(frustum.Intersects(new(-1,-1,-4),new(1,1,-2)),"A box in front of the camera remains visible");
            Check(!frustum.Intersects(new(-1,-1,2),new(1,1,4)),"A box behind the camera is rejected");
            Check(!frustum.Intersects(new(20,-1,-4),new(22,1,-2)),"A box beyond a side plane is rejected");
            Check(frustum.Intersects(new(2,-1,-3),new(6,1,-2)),"A box crossing the edge stays visible even with its center outside");
            Check(frustum.Intersects(new(-.2f,-.2f,-.3f),new(.2f,.2f,.2f)),"A box crossing the near plane is kept conservatively");
            Check(!frustum.Intersects(new(-1,-1,-120),new(1,1,-110)),"A box beyond the far plane is rejected");
            int fullTrees=Models(map.Scene!.Objects).Where(m=>SceneSurface.Key(m).StartsWith("bark")||SceneSurface.Key(m).StartsWith("foliage")).Sum(m=>((MeshGeometry3D)m.Geometry).TriangleIndices.Count/3);
            Check(Triangles(map.Scene.CoarseTrees)>0&&Triangles(map.Scene.CoarseTrees)<fullTrees/4,"Coarse trees retain geometry with fewer than one quarter of the detailed triangles");
            Check(renderer.SpatialBatches>renderer.ResourceMeshes,"Material meshes are partitioned into cached spatial batches");
            map.Zoom(.5);renderer.AutomaticDetail=false;renderer.VisibilityCulling=false;map.RenderGpuFrame();string reference=Hash();var referenceStats=renderer.Statistics;
            renderer.VisibilityCulling=true;map.RenderGpuFrame();var near=renderer.Statistics;
            Check(Hash()==reference,"Main, reflected and shadow frusta preserve every rendered pixel at fixed time");
            Check(near.MainTriangles<referenceStats.MainTriangles*.5&&near.CulledBatches>0,"A close camera submits fewer than half the uncropped world triangles");
            Check(near.ShadowTriangles<referenceStats.ShadowTriangles&&near.ReflectionTriangles<referenceStats.ReflectionTriangles,"Shadows and reflections cull using their own view volumes");
            Check(near.CoarseTreeDraws==0,"Disabling automatic detail selects only detailed tree geometry");
            map.LandscapeCamera();renderer.VisibilityCulling=false;map.RenderGpuFrame();string landscapeReference=Hash();renderer.VisibilityCulling=true;map.RenderGpuFrame();
            Check(Hash()==landscapeReference,"The low landscape camera preserves the exact reference image including reflections");map.ResetCamera();map.Zoom(.5);
            Menu("Statystyki renderowania");Check(map.ShowRenderStatistics&&map.RenderStatisticsText.Visibility==Visibility.Visible&&map.RenderStatisticsText.Text.Contains("trójkątów"),"The real graphics menu opens live rendering statistics");await Capture("rendering-near");
            int builds=map.SceneBuildCount,uploads=renderer.SceneUploads,resources=renderer.LiveResources;var terrain=map.Scene.Terrain;int batches=renderer.SpatialBatches;
            for(int i=0;i<24;i++){map.Orbit(7,0);map.RenderGpuFrame(i*.1);}
            Check(map.SceneBuildCount==builds&&ReferenceEquals(terrain,map.Scene.Terrain),"Orbiting changes visibility without rebuilding simulation geometry");
            Check(renderer.SceneUploads==uploads&&renderer.SpatialBatches==batches&&renderer.LiveResources==resources,"Camera motion reuses buffers and does not accumulate native resources or scene uploads");
            map.ResetCamera();map.Zoom(4);renderer.AutomaticDetail=false;map.RenderGpuFrame();int farFull=renderer.Statistics.MainTriangles;
            Menu("Automatyczne szczegóły");var far=renderer.Statistics;
            Check(renderer.AutomaticDetail&&far.CoarseTreeDraws>0,"The real detail control selects coarse trees in the distant overview");
            Check(far.MainTriangles<farFull,"Distant tree and small-plant detail reduces submitted geometry");await Capture("rendering-overview");
            map.ResetCamera();map.Zoom(.3);map.RenderGpuFrame();Check(renderer.Statistics.CoarseTreeDraws<far.CoarseTreeDraws,"Approaching the map restores nearby tree detail");
            var cp=Terrain3D.Center(capital);var point=map.Project(Terrain3D.Ground(s,cp.X,cp.Y,.1));Check(point!=null&&map.Pick(point.Value)==capital.Id,"Visibility and tree detail do not change logical city picking");
            Select(iron.Id);map.Center(iron.Id);map.RenderGpuFrame();Check(Descendants<TextBlock>(map).Any(t=>t.Text=="Żelazo"),"Resource names remain readable with rendering optimizations enabled");
            Menu("Pomijanie geometrii");Check(!renderer.VisibilityCulling&&renderer.Statistics.CulledBatches==0,"The real culling control restores the reference path");Menu("Pomijanie geometrii");
            Check(SaveGame.Serialize(state)==original,"Rendering optimizations, statistics and camera changes preserve campaign state and RNG");
            var hidden=s.Tiles.First(t=>!t.Explored);var oldTerrain=hidden.Terrain;int coarseBefore=Triangles(map.Scene.CoarseTrees);hidden.Terrain=Terrain.Forest;Refresh();
            Check(Triangles(map.Scene.CoarseTrees)==coarseBefore,"Unknown forest geography creates no coarse tree models");hidden.Terrain=oldTerrain;Refresh();
            Width=1200;Height=800;await Task.Delay(140);root.UpdateLayout();map.RenderGpuFrame();Check(map.RenderStatisticsText.ActualWidth<map.ActualWidth&&map.RenderStatisticsText.ActualHeight<map.ActualHeight,"Statistics fit the compact map without blocking its controls");
            renderer.VisibilityCulling=false;map.RenderGpuFrame();string compactReference=Hash();renderer.VisibilityCulling=true;map.RenderGpuFrame();Check(Hash()==compactReference,"Resizing preserves every reference pixel with the new camera aspect ratio");await Capture("rendering-compact");
            Width=1600;Height=980;await Task.Delay(120);root.UpdateLayout();map.ResetCamera();renderer.AutomaticDetail=false;
            double Sample(bool culling){renderer.VisibilityCulling=culling;map.RenderGpuFrame();var times=new List<double>();for(int i=0;i<10;i++){map.RenderGpuFrame();times.Add(renderer.LastFrameMilliseconds);}return times.Order().ElementAt(5);}
            double baseline=Sample(false),optimized=Sample(true);renderer.AutomaticDetail=true;map.RenderGpuFrame();
            checks.Add($"SAME-BUILD SAMPLE: reference {baseline:0.0}ms; spatial culling {optimized:0.0}ms; ten fixed-time frames each, median; not an FPS guarantee.");
            checks.Add($"NEAR TRIANGLES: main {near.MainTriangles}/{referenceStats.MainTriangles}; shadows {near.ShadowTriangles}/{referenceStats.ShadowTriangles}; reflections {near.ReflectionTriangles}/{referenceStats.ReflectionTriangles}.");
            checks.Add($"FAR TRIANGLES: detail {far.MainTriangles}/{farFull}; coarse tree batches {far.CoarseTreeDraws}; cached spatial batches {renderer.SpatialBatches}.");
            await Capture("rendering-final");checks.Add($"{checks.Count(c=>c.StartsWith("PASS "))} rendering desktop integration checks passed.");File.WriteAllLines(Path.Combine(folder,"ui-smoke.txt"),checks);Close();
        }
        catch(Exception ex){checks.Add(ex.ToString());if(map?.GpuFailure!=null)checks.Add(map.GpuFailure);File.WriteAllLines(Path.Combine(folder,"ui-smoke.txt"),checks);if(!arguments.Contains("--smoke-force-failure"))await Capture("rendering-failure");Application.Current.Shutdown(3);}
    }
}
