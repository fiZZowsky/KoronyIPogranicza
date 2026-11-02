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
    private async Task DistrictSmoke(string folder)
    {
        Directory.CreateDirectory(folder);var checks=new List<string>();
        void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);checks.Add("PASS "+message);}
        void Click(string title){root.UpdateLayout();var button=Descendants<Button>(overlay??root).First(b=>b.Content?.ToString()==title);CheckEnabled(button);button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));root.UpdateLayout();}
        void CheckEnabled(Button button){if(!button.IsEnabled)throw new InvalidOperationException("Disabled action: "+button.Content);}
        IEnumerable<GeometryModel3D> Models(Model3D model){if(model is GeometryModel3D g)yield return g;if(model is Model3DGroup group)foreach(var child in group.Children)foreach(var item in Models(child))yield return item;}
        async Task Capture(string name){root.UpdateLayout();await Task.Delay(120);map?.RenderGpuFrame();var bitmap=new RenderTargetBitmap((int)root.ActualWidth,(int)root.ActualHeight,96,96,PixelFormats.Pbgra32);bitmap.Render(root);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using var output=File.Create(Path.Combine(folder,name+".png"));encoder.Save(output);}
        try
        {
            if(arguments.Contains("--smoke-force-failure"))throw new InvalidOperationException("Intentional district diagnostic failure.");
            var s=World.Create();Narrative.Resolve(s,0);s.Player.Gold=10000;s.Player.Technologies.UnionWith(EpochCatalog.All.Take(6).SelectMany(e=>e.Technologies));for(int i=0;i<6;i++)s.Player.EpochChoices[i]=1;
            var city=s.Cities.First(c=>c.Owner==0);city.Population=20;foreach(var c in s.Cities)c.ProductionPaused=true;
            foreach(var t in s.Tiles.Where(t=>World.Distance(t,s.Tiles[city.Tile])<=4)){t.Explored=true;t.Terrain=Terrain.Grassland;t.Resource=Resource.None;t.Improvement=Improvement.None;t.Ruin=false;t.River=false;}
            Start(s);root.UpdateLayout();await Task.Delay(160);map.RenderGpuFrame();Check(map.Photographic!=null,"District scene uses the actual Direct3D device");
            string before=SaveGame.Serialize(s);Click("Dzielnice i plan miasta");
            Check(Districts.Types.All(type=>Descendants<Button>(overlay!).Any(b=>b.Content?.ToString()=="Wybierz miejsce: "+type.Name)),"The real city panel exposes all five district roles");await Capture("district-plan");
            Click("Wybierz miejsce: Okręg uczonych");Check(districtPlacement!=null&&map.DistrictSites.Count>0&&map.OverlayTriangles>0,"Choosing a role starts actual map placement with eligible field outlines");
            Check(SaveGame.Serialize(state)==before,"Browsing district plans and legal sites preserves campaign and RNG");Select(city.Tile);Check(overlay==null&&districtPlacement!=null&&state.Districts.Count==0,"Selecting an illegal city-center site does not reserve land or spend funds");
            int placementTurn=state.Turn;Advance();Check(state.Turn==placementTurn&&districtPlacement!=null,"An unresolved map placement cannot accidentally advance the turn");
            injectingSettingsKey=true;try{RaiseEvent(new System.Windows.Input.KeyEventArgs(System.Windows.Input.Keyboard.PrimaryDevice,PresentationSource.FromVisual(this)!,0,System.Windows.Input.Key.Escape){RoutedEvent=System.Windows.Input.Keyboard.PreviewKeyDownEvent});}finally{injectingSettingsKey=false;}
            Check(districtPlacement==null&&map.DistrictSites.Count==0&&SaveGame.Serialize(state)==before,"Esc cancels planning and removes its outlines without spending funds");Select(city.Tile);Click("Dzielnice i plan miasta");Click("Wybierz miejsce: Okręg uczonych");
            int tile=map.DistrictSites.First();map.Center(tile);map.ResetCamera();map.Zoom(.6);var center=Terrain3D.Center(s.Tiles[tile]);var point=map.Project(Terrain3D.Ground(s,center.X,center.Y,.08));
            Check(point!=null&&map.SelectScreen(point.Value)&&overlay!=null,"Clicking an actual 3D field opens the financing decision");
            root.UpdateLayout();
            Check(Descendants<Button>(overlay!).Any(b=>b.Content?.ToString()=="Rozpocznij: Fundusze publiczne · 30 złota")&&Descendants<Button>(overlay!).Any(b=>b.Content?.ToString()=="Rozpocznij: Przywilej cechowy · 10 złota"),"The council offers public and guild charters with real costs");await Capture("district-charter");
            double gold=s.Player.Gold;Click("Rozpocznij: Przywilej cechowy · 10 złota");var d=state.Districts.Single();Check(d.Tile==tile&&d.Funding==DistrictFunding.Guild&&state.Player.Gold==gold-10&&districtPlacement==null,"Accepting a charter creates the actual paid construction on the selected field");
            Check(!Engine.WorkedTiles(state,city).Any(t=>t.Id==tile),"The district construction field is removed from actual worked farmland");
            Check(Models(map.Scene!.Objects).Any(m=>SceneSurface.Key(m).StartsWith("district-scaffold")),"Unfinished districts render spatial scaffolding");
            Select(city.Tile);Click("Dzielnice i plan miasta");Check(Descendants<Button>(overlay!).Where(b=>b.Content?.ToString()?.StartsWith("Wybierz miejsce:")==true).All(b=>!b.IsEnabled),"A city with an unfinished site cannot start a second simultaneous district");
            Descendants<ComboBox>(overlay!).Single(c=>c.Tag?.ToString()=="district-share").SelectedIndex=2;Click("Zastosuj podział pracy");Check(city.DistrictShare==50,"The city planning control changes real production allocation");CloseModal();city.ProductionPaused=false;Advance();CloseModal();
            Check(d.Work>0&&d.Status==DistrictStatus.Building,"An actual economic turn advances district construction");
            string path=Path.Combine(folder,"district-construction.crown");SaveGame.Write(state,path);string saved=SaveGame.Serialize(state);LoadPath(path);CloseModal();city=state.Cities.Single(c=>c.Owner==0);d=state.Districts.Single();
            Check(SaveGame.Serialize(state)==saved&&d.Work>0&&city.DistrictShare==50,"Saving and loading preserves field work charter and production allocation");
            if(state.PendingEvent!=null)Narrative.Resolve(state,0);d.Work=Districts.Cost(d)-.1;Advance();CloseModal();Check(d.Status==DistrictStatus.Active&&Districts.Yields(state,city).Science>0,"Completing construction activates actual science income");
            Select(d.Tile);map.Center(d.Tile);map.ResetCamera();map.Zoom(.4);map.RenderGpuFrame();Check(Descendants<TextBlock>(side).Any(t=>t.Text.Contains("Okręg uczonych")),"Selecting the district opens its real management card");
            var anchor=WorldScene3D.DistrictAnchor(state,d);point=map.Project(Terrain3D.Ground(state,anchor.X,anchor.Y,.2));Check(point!=null&&map.Pick(point.Value)==d.Tile,"Picking district building geometry returns its logical field");await Capture("district-scholars");
            foreach(var type in Districts.Types.Where(t=>t.Kind!=DistrictKind.Scholars))
            {int site=state.Tiles.First(t=>Districts.SiteError(state,city,t.Id,type.Kind)==null).Id;Check(Districts.Begin(state,city.Id,site,type.Kind,DistrictFunding.Public).Success,"The fifth-role fixture uses valid paid placement: "+type.Name);state.Districts.Last().Status=DistrictStatus.Active;}
            Refresh();map.ResetCamera();map.Center(city.Tile);map.Zoom(.8);map.RenderGpuFrame();
            Check(new[]{"district-garden-beds","district-forge-metal","district-scholar-dome","district-awning","district-monument"}.All(key=>Models(map.Scene!.Objects).Any(m=>SceneSurface.Key(m).StartsWith(key))),"All five roles have recognizable distinct spatial model components");
            Check(Models(map.Scene.Objects).Select(m=>(MeshGeometry3D)m.Geometry).All(m=>m.Normals.All(n=>double.IsFinite(n.X)&&Math.Abs(n.Length-1)<.001)),"District scene geometry keeps finite unit normals");await Capture("district-city");
            var victim=state.Cities.First(c=>c.Owner==1);state.Factions[1].Gold=1000;state.Factions[1].Technologies.Add("agriculture");var foreign=World.Neighbors(state,victim.Tile).First(t=>t.Owner==1&&!state.Cities.Any(c=>c.Tile==t.Id));foreign.Terrain=Terrain.Grassland;foreign.Resource=Resource.None;foreign.Improvement=Improvement.None;foreign.Ruin=false;foreign.Explored=true;
            Check(Districts.Begin(state,victim.Id,foreign.Id,DistrictKind.Gardens,DistrictFunding.Public,1).Success,"Foreign district fixture uses the same placement rules");var enemyDistrict=state.Districts.Last();enemyDistrict.Status=DistrictStatus.Active;Refresh();
            Select(foreign.Id);Check(!Models(map.Scene!.Objects).Any(m=>SceneSurface.Key(m).StartsWith("district-")&&((int[]?)m.GetValue(SceneSurface.TileIdsProperty))?.Contains(foreign.Id)==true)&&!Descendants<TextBlock>(side).Any(t=>t.Text.Contains("Teren nieuprawiany:")||t.Text.Contains("Ogrody miejskie")),"An explored but currently unseen enemy district reveals neither construction geometry nor a sidebar land-use hint");
            var raider=Engine.AddUnit(state,0,foreign.Id,UnitKind.Scout);state.Relation(0,1).War=true;Refresh();Select(foreign.Id);map.Center(foreign.Id);map.ResetCamera();map.Zoom(.4);double loot=state.Player.Gold;Click("Splądruj dzielnicę #"+raider.Id);
            Check(enemyDistrict.Status==DistrictStatus.Pillaged&&state.Player.Gold>loot,"The actual field pillage button disables an enemy district and grants loot");
            Check(Models(map.Scene!.Objects).Any(m=>SceneSurface.Key(m).StartsWith("district-ruins")),"Looted districts render visible ruins");await Capture("district-pillage");
            d=state.Districts.First(v=>v.CityId==city.Id);d.Status=DistrictStatus.Pillaged;d.Work=0;Refresh();Select(city.Tile);Click("Dzielnice i plan miasta");Click("Odbuduj dzielnicę #"+d.Id);Check(d.Status==DistrictStatus.Repairing&&Districts.Cost(d)==Districts.Type(d.Kind).Cost*.5,"The actual management button starts half-cost reconstruction");CloseModal();
            var canceled=state.Districts.Last(v=>v.CityId==city.Id);d.Status=DistrictStatus.Active;canceled.Status=DistrictStatus.Building;canceled.Work=7;Refresh();Select(city.Tile);Click("Dzielnice i plan miasta");Click("Porzuć budowę #"+canceled.Id);gold=state.Player.Gold;Click("Porzuć bez zwrotu kosztów");Check(!state.Districts.Any(v=>v.Id==canceled.Id)&&state.Player.Gold==gold,"Confirmed cancellation releases the site without a treasury refund");
            Width=1200;Height=800;await Task.Delay(140);root.UpdateLayout();Check(overlay!=null&&Descendants<ScrollViewer>(overlay).Any()&&Descendants<Button>(overlay).Any(b=>b.Content?.ToString()=="Wróć do mapy"),"The extensive district plan remains scrollable with a return action in a compact window");await Capture("district-compact");
            CloseModal();Width=1600;Height=980;await Task.Delay(120);Select(city.Tile);map.ResetCamera();map.Zoom(.6);await Capture("district-final");
            checks.Add($"{checks.Count} district desktop integration checks passed.");File.WriteAllLines(Path.Combine(folder,"ui-smoke.txt"),checks);Close();
        }
        catch(Exception ex){checks.Add(ex.ToString());if(map?.GpuFailure!=null)checks.Add(map.GpuFailure);File.WriteAllLines(Path.Combine(folder,"ui-smoke.txt"),checks);if(!arguments.Contains("--smoke-force-failure"))await Capture("district-failure");Application.Current.Shutdown(3);}
    }
}
