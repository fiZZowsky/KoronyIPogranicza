using Crown.Core;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace Crown.Desktop;

public sealed partial class MainWindow
{
    private async Task TransportSmoke(string folder)
    {
        Directory.CreateDirectory(folder);var checks=new List<string>();
        void Check(bool value,string text){if(!value)throw new InvalidOperationException(text);checks.Add("PASS "+text);}
        void Click(string text,bool prefix=false){root.UpdateLayout();var button=Descendants<Button>(overlay??root).First(b=>prefix?b.Content?.ToString()?.StartsWith(text)==true:b.Content?.ToString()==text);if(!button.IsEnabled)throw new InvalidOperationException("Disabled: "+text);button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));root.UpdateLayout();}
        bool Text(string text)=>Descendants<TextBlock>(overlay!).Any(t=>t.Text.Contains(text));
        IEnumerable<GeometryModel3D> Models(Model3D model){if(model is GeometryModel3D g)yield return g;if(model is Model3DGroup group)foreach(var child in group.Children)foreach(var item in Models(child))yield return item;}
        async Task Capture(string name){root.UpdateLayout();await Task.Delay(140);map.RenderGpuFrame();var bitmap=new RenderTargetBitmap((int)root.ActualWidth,(int)root.ActualHeight,96,96,PixelFormats.Pbgra32);bitmap.Render(root);var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(bitmap));using var output=File.Create(Path.Combine(folder,name+".png"));png.Save(output);}
        void Step(){if(state.PendingEvent!=null)Narrative.Resolve(state,0);Advance();if(state.PendingEvent!=null)Narrative.Resolve(state,0);CloseModal();root.UpdateLayout();}
        try
        {
            if(arguments.Contains("--smoke-force-failure"))throw new InvalidOperationException("Intentional sea transport diagnostic failure.");
            var s=World.Create();Narrative.Resolve(s,0);s.Units.Clear();s.Player.Gold=10000;s.Player.Technologies.UnionWith(EpochCatalog.All.Take(7).SelectMany(e=>e.Technologies));for(int i=0;i<7;i++)s.Player.EpochChoices[i]=0;
            foreach(var t in s.Tiles){t.Terrain=Terrain.Grassland;t.Resource=Resource.None;t.Improvement=Improvement.None;t.Owner=-1;t.Ruin=t.River=t.Road=false;t.RoadOwner=-1;t.Explored=true;}
            (int X,int Y)[] centers=[(12,12),(20,12),(30,20),(6,5)];foreach(var c in s.Cities){c.Tile=centers[c.Owner].Y*s.Width+centers[c.Owner].X;c.ProductionPaused=true;foreach(var t in s.Tiles.Where(t=>World.Distance(t,s.Tiles[c.Tile])<=2))t.Owner=c.Owner;}
            foreach(var t in s.Tiles.Where(t=>t.X>=10&&t.X<=32&&t.Y>=13&&t.Y<=15))t.Terrain=t.Y==14?Terrain.Ocean:Terrain.Coast;
            var city=s.Cities[0];city.Buildings.Add(Navy.Port);var ship=Engine.AddUnit(s,0,13*s.Width+12,UnitKind.Ship,"carrack");var settler=Engine.AddUnit(s,0,city.Tile,UnitKind.Settler);var guard=Engine.AddUnit(s,0,city.Tile,UnitKind.Spearmen);var cavalry=Engine.AddUnit(s,0,city.Tile,UnitKind.Cavalry);s.InvalidateLogistics();SaveGame.Deserialize(SaveGame.Serialize(s));Start(s);root.UpdateLayout();await Task.Delay(160);
            string before=SaveGame.Serialize(state);Click("Flota i porty");Click("Załadunek i desant #"+ship.Id,true);
            Check(Text("TRANSPORT MORSKI")&&Text("Ładunek 0/2"),"The real fleet entry opens the carrier manifest with model capacity");
            Check(before==SaveGame.Serialize(state),"Browsing the manifest preserves exact campaign and RNG");Click("Zaokrętuj #"+settler.Id,true);
            Check(settler.EmbarkedOnId==ship.Id&&settler.Actions==0&&ship.Actions==3,"The actual boarding button loads the settler and spends movement");
            Check(!Descendants<Button>(overlay!).Single(b=>b.Content?.ToString()?.StartsWith("Zaokrętuj #"+cavalry.Id)==true).IsEnabled,"The manifest disables a heavy unit that exceeds remaining capacity");Click("Zaokrętuj #"+guard.Id,true);
            Check(SeaTransport.Used(state,ship)==2&&Text("Ładunek 2/2"),"The second real boarding action fills the carrier");
            Check(!map.Scene!.UnitIds.Contains(settler.Id)&&!map.Scene.UnitIds.Contains(guard.Id)&&map.Scene.UnitIds.Contains(ship.Id),"Carried troops disappear from independent 3D formations");
            Check(Models(map.Scene.Objects).Any(m=>SceneSurface.Key(m).StartsWith("naval-cargo-wood")),"A loaded ship shows an actual deck cargo marker");await Capture("transport-manifest");
            before=SaveGame.Serialize(state);Click("Wybierz brzeg dla #"+settler.Id);Check(landingPassenger==settler.Id&&map.LandingSites.Count>0,"The manifest starts real map placement for disembarkation");int turn=state.Turn;Advance();Check(state.Turn==turn,"Choosing a landing shore blocks accidental turn advancement");
            injectingSettingsKey=true;try{RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice,PresentationSource.FromVisual(this)!,0,Key.Escape){RoutedEvent=Keyboard.PreviewKeyDownEvent});}finally{injectingSettingsKey=false;}
            Check(landingPassenger==null&&map.LandingSites.Count==0&&SaveGame.Serialize(state)==before,"Esc cancels landing selection without spending movement");ShowTransport(ship);root.UpdateLayout();Click("Pokaż okręt");
            Check(selectedUnit==ship.Id&&selectedTile==ship.Tile,"Selecting a loaded carrier never selects its hidden passengers");map.ResetCamera();map.Zoom(.7);int sea=15*state.Width+12;var center=Terrain3D.Center(state.Tiles[sea]);var point=map.Project(new(center.X,Terrain3D.SeaLevel+.02,center.Y));
            Check(point!=null&&map.OrderScreen(point.Value)&&ship.Tile==sea&&settler.Tile==sea&&guard.Tile==sea,"A real sea map order moves the complete manifest across the water");
            string path=Path.Combine(folder,"transport-campaign.crown");SaveGame.Write(state,path);before=SaveGame.Serialize(state);LoadPath(path);CloseModal();ship=state.Units.Single(u=>u.Id==ship.Id);settler=state.Units.Single(u=>u.Id==settler.Id);guard=state.Units.Single(u=>u.Id==guard.Id);
            Check(state.Version==SaveGame.CurrentVersion&&SaveGame.Serialize(state)==before&&SeaTransport.Passengers(state,ship).Count()==2,"Current-format desktop reload restores the actual manifest mid voyage");ShowTransport(ship);root.UpdateLayout();Check(!Descendants<Button>(overlay!).Single(b=>b.Content?.ToString()=="Wybierz brzeg dla #"+settler.Id).IsEnabled,"An exhausted carrier cannot unload from its actual manifest");CloseModal();Step();Select(ship.Tile);Click("Rozkazy wojskowe #"+ship.Id);Click("Załadunek i desant #"+ship.Id);Click("Wybierz brzeg dla #"+settler.Id);
            before=SaveGame.Serialize(state);Select(ship.Tile);Check(overlay==null&&landingPassenger!=null&&SaveGame.Serialize(state)==before,"An invalid water landing neither opens confirmation nor changes state");
            int shore=16*state.Width+12;map.ResetCamera();map.Center(ship.Tile);map.Zoom(.7);center=Terrain3D.Center(state.Tiles[shore]);point=map.Project(Terrain3D.Ground(state,center.X,center.Y,.10));
            bool shoreClick=point!=null&&map.SelectScreen(point.Value);root.UpdateLayout();Check(shoreClick&&overlay!=null&&Text("WYŁADUNEK I DESANT"),"Selecting the actual shore opens a landing confirmation");await Capture("transport-landing");Click("Wysadź oddział na brzeg");
            Check(settler.EmbarkedOnId==null&&settler.Tile==shore&&settler.Actions==0&&landingPassenger==null,"Confirmed landing exhausts the settler and removes the placement overlay");ShowTransport(ship);root.UpdateLayout();Click("Wybierz brzeg dla #"+guard.Id);Select(16*state.Width+13);Click("Wysadź oddział na brzeg");
            Check(guard.EmbarkedOnId==null&&guard.Actions==0&&SeaTransport.Used(state,ship)==0,"The second actual landing empties the carrier without granting attacks");Select(settler.Tile);int cities=state.Cities.Count;Click("Załóż miasto w tym miejscu");Check(state.Cities.Count==cities,"A freshly unloaded settler cannot found a city before the next turn");Step();Select(settler.Tile);Click("Załóż miasto w tym miejscu");
            Check(state.Cities.Count==cities+1&&state.Cities.Any(c=>c.Owner==0&&c.Tile==shore),"A real voyage and next-turn founding establish a coastal colony");await Capture("transport-colony");
            var visibleCarrier=Engine.AddUnit(state,1,13*state.Width+11,UnitKind.Ship,"carrack");var hiddenCrew=Engine.AddUnit(state,1,visibleCarrier.Tile,UnitKind.Spearmen);hiddenCrew.EmbarkedOnId=visibleCarrier.Id;hiddenCrew.Actions=0;Refresh();
            Check(map.Scene!.UnitIds.Contains(visibleCarrier.Id)&&!map.Scene.UnitIds.Contains(hiddenCrew.Id),"A visible foreign carrier does not expose independent passenger models");
            var modern=Engine.AddUnit(state,0,13*state.Width+12,UnitKind.Ship,"ocean_guardian");for(int i=0;i<4;i++){var troop=Engine.AddUnit(state,0,state.Cities.First(c=>c.Owner==0).Tile,UnitKind.Spearmen);if(!SeaTransport.Embark(state,troop.Id,modern.Id).Success)throw new InvalidOperationException("Compact manifest setup failed");}Refresh();ShowTransport(modern);Width=1200;Height=800;await Task.Delay(150);root.UpdateLayout();
            Check(Text("Ładunek 4/4")&&Descendants<ScrollViewer>(overlay!).Any(v=>v.ExtentHeight>v.ViewportHeight)&&Descendants<Button>(overlay!).Any(b=>b.Content?.ToString()=="Wróć do mapy"),"A full future carrier manifest remains scrollable in a compact window");await Capture("transport-compact");SaveGame.Deserialize(SaveGame.Serialize(state));
            checks.Add($"{checks.Count} transport desktop integration checks passed.");File.WriteAllLines(Path.Combine(folder,"ui-smoke.txt"),checks);Close();
        }
        catch(Exception ex){checks.Add(ex.ToString());File.WriteAllLines(Path.Combine(folder,"ui-smoke.txt"),checks);if(!arguments.Contains("--smoke-force-failure"))await Capture("transport-failure");Application.Current.Shutdown(3);}
    }
}
