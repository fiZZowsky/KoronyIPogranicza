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
    private async Task NavalSmoke(string folder)
    {
        Directory.CreateDirectory(folder);var checks=new List<string>();
        void Check(bool value,string text){if(!value)throw new InvalidOperationException(text);checks.Add("PASS "+text);}
        void Click(string text,bool prefix=false){root.UpdateLayout();var b=Descendants<Button>(overlay??root).First(b=>prefix?b.Content?.ToString()?.StartsWith(text)==true:b.Content?.ToString()==text);if(!b.IsEnabled)throw new InvalidOperationException("Disabled: "+text);b.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));root.UpdateLayout();}
        bool Text(string text)=>Descendants<TextBlock>(overlay!).Any(t=>t.Text.Contains(text));
        IEnumerable<GeometryModel3D> Models(Model3D model){if(model is GeometryModel3D geometry)yield return geometry;if(model is Model3DGroup group)foreach(var child in group.Children)foreach(var item in Models(child))yield return item;}
        async Task Capture(string name){root.UpdateLayout();await Task.Delay(150);map?.RenderGpuFrame();var bitmap=new RenderTargetBitmap((int)root.ActualWidth,(int)root.ActualHeight,96,96,PixelFormats.Pbgra32);bitmap.Render(root);var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(bitmap));using var output=File.Create(Path.Combine(folder,name+".png"));png.Save(output);}
        void Step(){if(state.PendingEvent!=null)Narrative.Resolve(state,0);Advance();if(state.PendingEvent!=null)Narrative.Resolve(state,0);CloseModal();root.UpdateLayout();}
        void Grant(GameState s,int eras){s.Player.Technologies.UnionWith(EpochCatalog.All.Take(eras).SelectMany(e=>e.Technologies));for(int i=0;i<eras;i++)s.Player.EpochChoices[i]=0;}
        try
        {
            if(arguments.Contains("--smoke-force-failure"))throw new InvalidOperationException("Intentional naval diagnostic failure.");
            var s=World.Create();Narrative.Resolve(s,0);s.Units.Clear();s.Player.Gold=10000;Grant(s,7);
            foreach(var t in s.Tiles){t.Terrain=Terrain.Grassland;t.Owner=-1;t.Resource=Resource.None;t.Improvement=Improvement.None;t.Ruin=t.Road=t.River=false;t.RoadOwner=-1;t.Explored=true;}
            (int X,int Y)[] centers=[(12,12),(20,12),(31,24),(6,5)];
            foreach(var c in s.Cities){c.Tile=centers[c.Owner].Y*s.Width+centers[c.Owner].X;c.ProductionPaused=true;foreach(var t in s.Tiles.Where(t=>World.Distance(t,s.Tiles[c.Tile])<=2))t.Owner=c.Owner;}
            foreach(var t in s.Tiles.Where(t=>t.X>=8&&t.X<=32&&t.Y>=13&&t.Y<=21))t.Terrain=t.Y is 13 or 21||t.X is 8 or 32?Terrain.Coast:Terrain.Ocean;
            s.InvalidateLogistics();Start(s);root.UpdateLayout();await Task.Delay(180);var city=state.Cities.First(c=>c.Owner==0);string before=SaveGame.Serialize(state);
            Click("Flota i porty");Check(Text("ADMIRALICJA MARCHII")&&(arguments.Contains("--no-gpu")||map.Photographic!=null),"The actual city button opens the fleet panel using the requested renderer");
            Check(Descendants<Button>(overlay!).Any(b=>b.Content?.ToString()=="Buduj port: "+city.Name&&b.IsEnabled),"A coastal city offers legal port construction");
            Check(SaveGame.Serialize(state)==before,"Browsing the admiralty preserves state and RNG");await Capture("navy-port-plan");
            Click("Buduj port: "+city.Name);Check(city.Project==Navy.Port,"The real admiralty button selects an actual city port project");CloseModal();city.ProductionPaused=false;city.Production=119.9;Step();
            Check(city.Buildings.Contains(Navy.Port),"An actual economic turn completes the selected port");
            Check(Models(map.Scene!.Objects).Any(m=>SceneSurface.Key(m).StartsWith("naval-port-wood")),"The constructed port renders a spatial pier and supports");
            Select(city.Tile);Click("Flota i porty");Click("Buduj: Galera przybrzeżna",true);Check(city.Project=="coastal_galley","The actual fleet panel orders a technology legal ship");CloseModal();city.Production=99.9;Step();city.ProductionPaused=true;
            var ship=state.Units.Single(u=>u.Owner==0&&Navy.IsShip(u));Check(Navy.Water(state.Tiles[ship.Tile])&&ship.Tile!=city.Tile,"Actual ship production launches on a water field");
            Check(Models(map.Scene!.Objects).Any(m=>SceneSurface.Key(m).StartsWith("naval-hull-wood")),"The launched galley has an actual hull model");
            Select(ship.Tile);selectedUnit=ship.Id;Refresh();map.Center(ship.Tile);map.ResetCamera();map.Zoom(.45);map.RenderGpuFrame();var anchor=WorldScene3D.UnitAnchor(state,ship,0);var point=map.Project(new(anchor.X,Terrain3D.SeaLevel+.17,anchor.Y));
            Check(point!=null&&map.Pick(point.Value)==ship.Tile&&map.SelectScreen(point.Value),"Selecting the actual hull returns its logical water field");
            Check(Descendants<TextBlock>(side).Any(t=>t.Text.Contains("Dostawy morskie zapewnione")),"The ship sidebar uses real naval supply rather than land roads");
            var overlayBatch=new SceneBatch();WorldScene3D.HexOutline(overlayBatch.Mesh("naval-probe","#FFFFFF"),state,state.Tiles[ship.Tile],.02,.07);
            Check(Models(overlayBatch.Freeze()).SelectMany(m=>((MeshGeometry3D)m.Geometry).Positions).All(p=>p.Y>Terrain3D.SeaLevel),"Water selection and reachable outlines stay above the sea surface");await Capture("navy-galley");
            int target=World.Neighbors(state,ship.Tile).Where(t=>t.Terrain==Terrain.Coast&&Navigation.CanEnter(state,ship,t)).OrderBy(t=>World.Distance(t,state.Tiles[city.Tile])).First().Id;
            var center=Terrain3D.Center(state.Tiles[target]);point=map.Project(new(center.X,Terrain3D.SeaLevel+.01,center.Y));double actions=ship.Actions;
            Check(point!=null&&map.OrderScreen(point.Value)&&ship.Tile==target&&ship.Actions==actions-1,"A real map order sails to a neighboring coast and spends movement");
            int ocean=World.Neighbors(state,ship.Tile).First(t=>t.Terrain==Terrain.Ocean).Id;before=SaveGame.Serialize(state);Order(ocean);Check(SaveGame.Serialize(state)==before&&ship.Tile!=ocean,"The real galley order cannot cross deep ocean");
            string path=Path.Combine(folder,"naval-campaign.crown");SaveGame.Write(state,path);before=SaveGame.Serialize(state);LoadPath(path);CloseModal();city=state.Cities.First(c=>c.Owner==0);ship=state.Units.Single(u=>u.Owner==0&&Navy.IsShip(u));
            Check(SaveGame.Serialize(state)==before&&state.Version==SaveGame.CurrentVersion,"V21 desktop reload preserves the actual port and ship");Step();Select(city.Tile);Click("Flota i porty");Click("#"+ship.Id+" · ",true);
            Check(!Descendants<Button>(overlay!).Any(b=>b.Content?.ToString()=="Okop oddział"),"Naval orders do not offer land fortification");Click("Modernizuj oddział #"+ship.Id);double gold=state.Player.Gold;int price=UnitModels.UpgradeCost(state,ship,Navy.Models.Single(m=>m.Id=="carrack"));Click("Modernizuj: Karaka wojenna",true);
            Check(ship.ModelId=="carrack"&&ship.Actions==0&&state.Player.Gold==gold-price,"The actual modernization panel pays gold and consumes movement at a safe port");CloseModal();Step();Select(ship.Tile);selectedUnit=ship.Id;Refresh();Order(ocean);
            Check(ship.Tile==ocean&&Navy.OceanCapable(ship),"The upgraded ship can receive a real open ocean order");
            int berth=World.Neighbors(state,city.Tile).Where(Navy.Water).OrderBy(t=>World.Distance(t,state.Tiles[ship.Tile])).First().Id;var enemy=Engine.AddUnit(state,1,berth,UnitKind.Ship,"coastal_galley");enemy.Health=1;state.Relation(0,1).War=true;state.InvalidateLogistics();Refresh();Select(city.Tile);Click("Flota i porty");
            Check(Text("BLOKADA MORSKA")&&Navy.Blockaded(state,city),"The fleet panel explains an actual wartime port blockade");
            Check(Descendants<Button>(overlay!).Where(b=>b.Content?.ToString()?.StartsWith("Buduj:")==true).All(b=>!b.IsEnabled),"A blocked port disables the actual shipbuilding choices");await Capture("navy-blockade");CloseModal();Select(ship.Tile);selectedUnit=ship.Id;Refresh();Order(enemy.Tile);root.UpdateLayout();
            Check(overlay!=null&&Text("PODGLĄD STARCIA"),"A ship attack uses the existing real combat preview");Click("Wykonaj atak");
            Check(!state.Units.Any(u=>u.Id==enemy.Id)&&!Navy.Blockaded(state,city)&&ship.Actions==0,"Confirmed naval fire sinks the blocker and restores port activity");
            var unseen=Engine.AddUnit(state,1,30+21*state.Width,UnitKind.Ship,"carrack");Refresh();Check(!Models(map.Scene!.Objects).Where(m=>SceneSurface.Key(m).StartsWith("naval-hull")).Any(m=>((int[]?)m.GetValue(SceneSurface.TileIdsProperty))?.Contains(unseen.Tile)==true),"An explored but unseen hostile ship does not appear in the 3D scene");
            Grant(state,18);foreach(var model in Navy.Models)Engine.AddUnit(state,0,13*state.Width+10+Array.IndexOf(Navy.Models,model),UnitKind.Ship,model.Id);Refresh();
            Check(new[]{"naval-hull-wood","naval-hull-metal","naval-masts-wood","naval-funnel-metal","naval-sensor-metal","naval-reactor-glass"}.All(key=>Models(map.Scene!.Objects).Any(m=>SceneSurface.Key(m).StartsWith(key))),"Historical industrial and future ships render their actual visual families");
            Check(Models(map.Scene.Objects).Where(m=>SceneSurface.Key(m).StartsWith("naval-")).SelectMany(m=>((MeshGeometry3D)m.Geometry).Normals).All(n=>double.IsFinite(n.X)&&Math.Abs(n.Length-1)<.001),"Naval meshes have finite unit normals");map.ResetCamera();map.Center(13*state.Width+14);map.Zoom(1.1);await Capture("navy-catalog");Select(city.Tile);Click("Flota i porty");
            bool allModels=Navy.Models.All(m=>Descendants<Button>(overlay!).Any(b=>b.Content?.ToString()?.StartsWith("Buduj: "+m.Name)==true));CloseModal();ShowEpochs(6);root.UpdateLayout();
            Check(allModels&&Text("Karaka wojenna")&&Text("przy własnym czynnym porcie"),"The admiralty and epoch catalog expose naval models and their real modernization requirements");CloseModal();ShowFleet();Width=1200;Height=800;await Task.Delay(160);root.UpdateLayout();
            Check(Descendants<ScrollViewer>(overlay!).Any()&&Descendants<Button>(overlay!).Any(b=>b.Content?.ToString()=="Wróć do mapy"),"The complete fleet roster remains scrollable in a compact window");await Capture("navy-compact");CloseModal();
            SaveGame.Deserialize(SaveGame.Serialize(state));checks.Add($"{checks.Count} naval desktop integration checks passed.");File.WriteAllLines(Path.Combine(folder,"ui-smoke.txt"),checks);Close();
        }
        catch(Exception ex){checks.Add(ex.ToString());File.WriteAllLines(Path.Combine(folder,"ui-smoke.txt"),checks);if(!arguments.Contains("--smoke-force-failure"))await Capture("navy-failure");Application.Current.Shutdown(3);}
    }
}
