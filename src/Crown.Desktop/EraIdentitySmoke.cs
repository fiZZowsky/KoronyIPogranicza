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
    private async Task EraIdentitySmoke(string folder)
    {
        Directory.CreateDirectory(folder);var checks=new List<string>();
        void Check(bool value,string text){if(!value)throw new InvalidOperationException(text);checks.Add("PASS "+text);}
        void Click(string text){root.UpdateLayout();var b=Descendants<Button>(overlay??root).First(b=>b.Content?.ToString()==text);if(!b.IsEnabled)throw new InvalidOperationException("Disabled: "+text);b.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));root.UpdateLayout();}
        bool Text(string text){root.UpdateLayout();return Descendants<TextBlock>(overlay!).Any(t=>t.Text.Contains(text));}
        IEnumerable<GeometryModel3D> Models(Model3D m){if(m is GeometryModel3D g)yield return g;if(m is Model3DGroup group)foreach(var child in group.Children)foreach(var q in Models(child))yield return q;}
        string Pixels(){var bitmap=map.Photographic!.Image!;var pixels=new byte[bitmap.PixelWidth*bitmap.PixelHeight*4];bitmap.CopyPixels(pixels,bitmap.PixelWidth*4,0);return Convert.ToHexString(SHA256.HashData(pixels));}
        async Task Capture(string name){root.UpdateLayout();await Task.Delay(140);map.RenderGpuFrame();var bmp=new RenderTargetBitmap((int)root.ActualWidth,(int)root.ActualHeight,96,96,PixelFormats.Pbgra32);bmp.Render(root);var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(bmp));using var file=File.Create(Path.Combine(folder,name+".png"));png.Save(file);}
        void Step(){CloseModal();state.PendingEvent=null;Advance();state.PendingEvent=null;CloseModal();root.UpdateLayout();}
        try
        {
            if(arguments.Contains("--smoke-force-failure"))throw new InvalidOperationException("Intentional era identity diagnostic failure.");
            var s=World.Create();Narrative.Resolve(s,0);s.PendingEvent=null;s.Player.EpochChoices[0]=0;s.Player.Gold=100000;s.Units.Clear();foreach(var c in s.Cities){c.ProductionPaused=true;c.Food=500;}
            var city=s.Cities[0];World.Neighbors(s,city.Tile).First(t=>t.Passable&&!s.Cities.Any(c=>c.Tile==t.Id)).Terrain=Terrain.Forest;s.InvalidateLogistics();Start(s);await Task.Delay(180);map.RenderGpuFrame();Check(arguments.Contains("--no-gpu")?map.Photographic==null:map.Photographic!=null&&map.GpuFailure==null,"The new GPU material shader initializes successfully or the requested WPF backend is selected");ShowEpochs();Click("Przedsięwzięcia i dziedzictwo epok");
            Check(Text("Szlak wspólnych łowów")&&Text("Las przy własnym mieście"),"The real epoch entry opens era-specific conditions and legacy");string before=SaveGame.Serialize(state);ShowEraEndeavours();Check(SaveGame.Serialize(state)==before,"Reading the endeavour panel preserves campaign and RNG");
            Click("Porozmawiaj z radą o przedsięwzięciu");Check(Text("ROZMOWA O DZIEDZICTWIE")&&Text("3 kolejne fazy")&&Text("6 kolejnych faz"),"The council explains the two paid commitments");Click("Wróć bez decyzji");Check(SaveGame.Serialize(state)==before,"Leaving the council incurs no costs");
            Click("Porozmawiaj z radą o przedsięwzięciu");double gold=state.Player.Gold;Click("Przyjmij plan intensywny");Check(state.Player.ActiveEndeavour==0&&state.Player.Gold==gold-25&&state.Player.EraLegacies.Count==0,"A real council decision pays startup cost without an instant legacy");
            Check(!Descendants<Button>(overlay!).Single(b=>b.Content?.ToString()=="Odwołaj przedsięwzięcie").IsEnabled,"A second plan decision is blocked in the same turn");
            Step();Check(state.Player.EndeavourProgress==1&&!state.Player.EraLegacies.Contains(0),"The first actual economic phase records only one paid success");Step();Step();ShowEraEndeavours(0);
            Check(state.Player.EraLegacies.Contains(0)&&state.Player.ActiveEndeavour==-1&&Text("Dziedzictwo ustanowione"),"Three actual qualified phases establish the legacy through the UI");Check(EraEndeavours.Bonus(state,city).Food>0,"The earned hunter legacy changes the city's actual food yield");await Capture("era-legacy");
            string path=Path.Combine(folder,"era-save.crown");SaveGame.Write(state,path);before=SaveGame.Serialize(state);LoadPath(path);CloseModal();await Task.Delay(160);Check(state.Version==SaveGame.CurrentVersion&&SaveGame.Serialize(state)==before,"The actual desktop reload preserves paid plans and earned legacies");city=state.Cities.First(c=>c.Id==city.Id);
            state.Turn=5;state.PendingEvent=null;Refresh();
            var signatures=new HashSet<string>();long maxTriangles=0;var buildTimer=Stopwatch.StartNew();
            foreach(int era in Enumerable.Range(0,18))
            {
                state.Player.Technologies=EpochCatalog.All.Take(era+1).SelectMany(e=>e.Technologies).ToHashSet();state.Player.EpochChoices.Clear();for(int i=0;i<=era;i++)state.Player.EpochChoices[i]=0;
                string json=SaveGame.Serialize(state);map.SelectedTile=city.Tile;Refresh();map.InvalidateVisual();var models=Models(map.Scene!.Objects).ToArray();var civic=models.Where(m=>SceneSurface.Key(m).Contains("era-")).SelectMany(m=>((MeshGeometry3D)m.Geometry).Positions).ToArray();
                signatures.Add(string.Join("|",civic.Select(p=>$"{p.X:F4},{p.Y:F4},{p.Z:F4}")));maxTriangles=Math.Max(maxTriangles,civic.Length/3);
                Check(civic.Length>0&&civic.All(p=>double.IsFinite(p.X)&&double.IsFinite(p.Y)&&double.IsFinite(p.Z))&&SaveGame.Serialize(state)==json,"Distinct finite read-only architecture: "+EpochCatalog.All[era].Name);
                if(era is 4 or 8 or 13 or 17){map.ResetCamera();map.Zoom(.25);CloseModal();await Capture("architecture-"+era);}
            }
            Check(signatures.Count==18&&maxTriangles<30000,"All 18 civic silhouettes differ within a bounded geometry budget");
            Check(Models(map.Scene!.Objects).Any(m=>SceneSurface.Key(m).StartsWith("era-facade-")&&m.Material is DiffuseMaterial {Brush:ImageBrush}),"Photographic facade textures are shared by the GPU scene and WPF fallback");
            bool fallback=arguments.Contains("--no-gpu");Check(fallback?map.Photographic==null:map.Photographic!=null&&map.GpuFailure==null,"The requested rendering backend actually renders the new architecture");
            var budget=new FrameBudget();for(int i=0;i<128;i++)budget.Observe(150,30);Check(budget.Scale==.5,"Sustained slow frames lower detail resolution to its bounded floor");for(int i=0;i<400;i++)budget.Observe(1,30);Check(budget.Scale==1,"Sustained spare frame budget gradually restores native resolution");double scale=budget.Scale;budget.Observe(double.NaN,30);Check(budget.Scale==scale,"Invalid timing never corrupts the adaptive controller");
            if(!fallback)
            {
                var renderer=map.Photographic!;renderer.AdaptiveResolution=false;renderer.ContactShadows=true;map.RenderGpuFrame();string lit=Pixels();renderer.ContactShadows=false;map.RenderGpuFrame();Check(Pixels()!=lit,"World-space contact shading changes actual GPU pixels");renderer.ContactShadows=true;
                map.RenderGpuFrame(10);int shadow=renderer.Statistics.ShadowDraws;map.RenderGpuFrame(10.02);Check(shadow>0&&renderer.Statistics.ShadowDraws==0&&renderer.Statistics.ReflectionDraws==0,"A stationary animated frame reuses valid shadow and reflection passes");map.Orbit(3,0);map.RenderGpuFrame(10.04);Check(renderer.Statistics.ReflectionDraws>0&&renderer.Statistics.ShadowDraws==0,"Orbiting refreshes reflections while retaining valid world-space shadows");map.PanPixels(35,0);map.RenderGpuFrame(10.06);Check(renderer.Statistics.ShadowDraws>0,"Moving the camera focus invalidates cached shadow coverage");
                map.ResetCamera();map.Zoom(4);renderer.AutomaticDetail=false;map.RenderGpuFrame();int full=renderer.Statistics.MainTriangles;renderer.AutomaticDetail=true;map.RenderGpuFrame();Check(renderer.Statistics.MainTriangles<full,"Distant facade and foliage details reduce real submitted geometry");
                renderer.AdaptiveResolution=true;for(int i=0;i<64;i++)renderer.Budget.Observe(150,30);map.RenderGpuFrame();Check(renderer.RenderWidth<=Math.Max(640,renderer.MaximumWidth*.5)&&renderer.ResolutionScale<=.625,"The actual renderer applies the measured resolution budget");renderer.AdaptiveResolution=false;renderer.Budget.Reset();
                map.ResetCamera();map.Zoom(.25);var samples=new List<double>();var reference=new List<double>();
                for(int i=0;i<12;i++){map.RenderGpuFrame();if(i>=2)reference.Add(renderer.LastFrameMilliseconds);}
                int fullPasses=renderer.Statistics.ShadowDraws+renderer.Statistics.ReflectionDraws;
                for(int i=0;i<12;i++){map.RenderGpuFrame(20+i*.01);if(i>=2)samples.Add(renderer.LastFrameMilliseconds);}
                checks.Add($"MEASURED: full passes {reference.Order().ElementAt(5):F1}ms; cached {samples.Order().ElementAt(5):F1}ms; ten warm frames per path; {renderer.RenderWidth}x{renderer.RenderHeight}; {renderer.Statistics.MainTriangles} main triangles; {fullPasses} extra draws refreshed only when needed. CPU+GPU+readback; one machine, no temperature or FPS guarantee.");
            }
            ShowSettings(true);root.UpdateLayout();var limit=Descendants<ComboBox>(overlay!).Single(c=>c.Tag?.ToString()=="Limit klatek");limit.SelectedIndex=1;var adaptive=Descendants<CheckBox>(overlay!).Single(c=>c.Content?.ToString()=="Adaptacyjna rozdzielczość");adaptive.IsChecked=true;Click("Zastosuj");var loaded=PreferencesStore.Read(preferencesPath,out var warning);Check(loaded.FrameLimit==20&&loaded.AdaptiveResolution&&loaded.EcoRendering&&warning==null,"The real settings controls persist frame budget and idle energy saving");CloseModal();
            ShowEraEndeavours(17);Click("Porozmawiaj z radą o przedsięwzięciu");Click("Przyjmij plan stopniowy");Check(state.Player.ActiveEndeavour==17&&state.Player.EndeavourProgress==0&&!state.Player.EraLegacies.Contains(17),"An unfulfilled future commitment remains a paid plan rather than a free reward");
            Width=1200;Height=800;ShowEraEndeavours(17);root.UpdateLayout();foreach(var scroll in Descendants<ScrollViewer>(overlay!))scroll.ScrollToBottom();await Capture("era-compact");var back=Descendants<Button>(overlay!).Single(b=>b.Content?.ToString()=="Wróć do mapy");var bounds=back.TransformToAncestor(root).TransformBounds(new Rect(new Point(),back.RenderSize));Check(back.IsVisible&&bounds.Top>=0&&bounds.Bottom<=root.ActualHeight&&bounds.Left>=0&&bounds.Right<=root.ActualWidth,"Era requirements and decisions remain reachable in the compact window");
            checks.Add($"{checks.Count(x=>x.StartsWith("PASS "))} era identity desktop integration checks passed.");File.WriteAllLines(Path.Combine(folder,"ui-smoke.txt"),checks);Close();
        }
        catch(Exception e){if(state!=null)checks.Add($"STATE turn={state.Turn}; era={state.Player.Era}; active={state.Player.ActiveEndeavour}; progress={state.Player.EndeavourProgress}; report={state.Player.EndeavourReport}; GPU={map?.GpuFailure}");checks.Add(e.ToString());File.WriteAllLines(Path.Combine(folder,"ui-smoke.txt"),checks);Application.Current.Shutdown(3);}
    }
}
