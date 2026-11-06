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
    private async Task FieldSupplySmoke(string folder)
    {
        Directory.CreateDirectory(folder);var checks=new List<string>();
        void Check(bool value,string text){if(!value)throw new InvalidOperationException(text);checks.Add("PASS "+text);}
        void Click(string text,bool prefix=false){root.UpdateLayout();var b=Descendants<Button>(overlay??root).First(b=>prefix?b.Content?.ToString()?.StartsWith(text)==true:b.Content?.ToString()==text);if(!b.IsEnabled)throw new InvalidOperationException("Disabled: "+text);b.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));root.UpdateLayout();}
        bool Text(string text){root.UpdateLayout();return Descendants<TextBlock>(overlay!).Any(t=>t.Text.Contains(text));}
        IEnumerable<GeometryModel3D> Models(Model3D model){if(model is GeometryModel3D g)yield return g;if(model is Model3DGroup group)foreach(var child in group.Children)foreach(var item in Models(child))yield return item;}
        async Task Capture(string name){root.UpdateLayout();await Task.Delay(120);map.RenderGpuFrame();var image=new RenderTargetBitmap((int)root.ActualWidth,(int)root.ActualHeight,96,96,PixelFormats.Pbgra32);image.Render(root);var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(image));using var file=File.Create(Path.Combine(folder,name+".png"));png.Save(file);}
        void Step(){CloseModal();if(state.PendingEvent!=null)Narrative.Resolve(state,0);Advance();if(state.PendingEvent!=null)Narrative.Resolve(state,0);CloseModal();root.UpdateLayout();}
        GameState Fixture()
        {
            var s=World.Create();Narrative.Resolve(s,0);s.Units.Clear();
            foreach(var t in s.Tiles){t.Terrain=Terrain.Grassland;t.Owner=-1;t.Resource=Resource.None;t.Improvement=Improvement.None;t.Ruin=t.River=t.Road=false;t.RoadOwner=-1;t.Explored=true;}
            (int X,int Y)[] centers=[(12,12),(20,12),(30,20),(6,5)];foreach(var c in s.Cities){c.Tile=centers[c.Owner].Y*s.Width+centers[c.Owner].X;c.ProductionPaused=true;foreach(var t in s.Tiles.Where(t=>World.Distance(t,s.Tiles[c.Tile])<=2))t.Owner=c.Owner;}
            foreach(var f in s.Factions)f.Gold=10000;s.Player.Technologies.UnionWith(EpochCatalog.All.Take(5).SelectMany(e=>e.Technologies));for(int i=0;i<5;i++)s.Player.EpochChoices[i]=0;s.InvalidateLogistics();return s;
        }
        try
        {
            if(arguments.Contains("--smoke-force-failure"))throw new InvalidOperationException("Intentional field supply diagnostic failure.");
            var s=Fixture();var city=s.Cities[0];city.Buildings.Add("workshop");city.Food=0;city.ProductionPaused=false;Start(s);root.UpdateLayout();await Task.Delay(160);
            Click("Wojsko i stałe rozkazy");Click("Magazyny i zapasy marszowe");
            Check(Text("KWATERMISTRZOSTWO MARCHII")&&Text("Magazyn nie jest zbudowany"),"The real army entry opens the stockpile and reserve panel");
            string before=SaveGame.Serialize(state);ShowFieldSupply();Check(SaveGame.Serialize(state)==before,"Browsing logistics leaves campaign and RNG unchanged");Click("Buduj magazyn: "+city.Name);
            Check(city.Project==FieldLogistics.Depot&&!city.Buildings.Contains(FieldLogistics.Depot),"The actual depot button selects an ordinary construction project");city.Production=139.9;Step();ShowFieldSupply();
            Check(city.Buildings.Contains(FieldLogistics.Depot)&&city.MilitarySupplies==0&&city.SupplyAllocation==0,"An ordinary production turn completes the prepared depot without free stocks");
            Check(Models(map.Scene!.Objects).Any(m=>SceneSurface.Key(m).StartsWith("logistics-depot-doors-wood")),"A completed depot has real visible warehouse geometry");
            Click("Ustal nakład: "+city.Name);Check(Text("ROZMOWA Z KWATERMISTRZEM")&&Text("rekrutacja będą wolniejsze"),"The quartermaster dialogue explains the production tradeoff");before=SaveGame.Serialize(state);Click("Wróć do magazynów");Check(SaveGame.Serialize(state)==before,"Leaving the quartermaster conversation changes no policy or resources");
            Click("Ustal nakład: "+city.Name);Click("50% · priorytet wyprawy",true);Check(city.SupplyAllocation==50&&city.MilitarySupplies==0,"The actual priority decision commits work without instant supplies");
            Click("Ustal nakład: "+city.Name);Check(Descendants<Button>(overlay!).Where(b=>b.Content?.ToString()?.Contains("% ·")==true).All(b=>!b.IsEnabled),"The dialogue enforces the once-per-turn policy change");Step();Step();ShowFieldSupply();
            Check(city.MilitarySupplies>=3&&Text("nakład 50%"),"Actual economic turns manufacture paid stock packages");
            var troop=Engine.AddUnit(state,0,city.Tile,UnitKind.Spearmen,"iron_infantry");var heavy=Engine.AddUnit(state,0,city.Tile,UnitKind.Catapult);Refresh();ShowFieldSupply();double stock=city.MilitarySupplies,moves=troop.Actions;Click("Załaduj zapasy #"+troop.Id);
            Check(troop.SupplyReserve==3&&city.MilitarySupplies==stock-3&&troop.Actions==moves-1,"The actual loading button spends earned packages and one movement point");
            Check(!Descendants<Button>(overlay!).Single(b=>b.Content?.ToString()=="Załaduj zapasy #"+troop.Id).IsEnabled,"A fully supplied troop cannot load beyond three reserve turns");
            stock=city.MilitarySupplies;Click("Załaduj zapasy #"+heavy.Id);Check(heavy.SupplyReserve>0&&Math.Abs(city.MilitarySupplies-(stock-heavy.SupplyReserve*2))<.00001,"Heavy artillery draws two real packs per loaded reserve turn");await Capture("field-supply-stockpile");
            troop.Tile=7*state.Width+29;troop.Actions=0;state.InvalidateLogistics();Refresh();Select(troop.Tile);ShowMilitaryOrders(troop);Check(Text("Zapasy marszowe 3/3")&&Text("korzysta z zapasów"),"Orders identify an isolated army living on its carried reserve");CloseModal();
            string path=Path.Combine(folder,"field-supply-campaign.crown");SaveGame.Write(state,path);before=SaveGame.Serialize(state);LoadPath(path);CloseModal();troop=state.Units.Single(u=>u.Id==troop.Id);city=state.Cities.Single(c=>c.Id==city.Id);
            Check(state.Version==23&&SaveGame.Serialize(state)==before&&troop.SupplyReserve==3&&city.SupplyAllocation==50,"Desktop V23 reload restores carried supplies stock and allocation exactly");
            double health=troop.Health;for(int i=0;i<3;i++){troop.Actions=0;Step();}Check(troop.SupplyReserve==0&&troop.Health==health&&!Engine.Supplied(state,troop),"Three ordinary turns consume reserve before attrition begins");troop.Actions=0;Step();ShowMilitaryOrders(troop);
            Check(troop.Health==health-5&&Text("Zapasy marszowe 0/3")&&Text("Brak dostaw"),"The UI reports exhaustion and actual attrition after the last ration");CloseModal();
            var raidState=Fixture();var target=raidState.Cities[1];target.Buildings.Add(FieldLogistics.Depot);target.MilitarySupplies=40;raidState.Relation(0,1).War=true;
            for(int x=18;x<=20;x++){var t=raidState.Tiles[12*raidState.Width+x];t.Owner=t.RoadOwner=1;t.Road=true;}
            var raider=Engine.AddUnit(raidState,0,12*raidState.Width+18,UnitKind.Spearmen);raidState.InvalidateLogistics();Start(raidState);root.UpdateLayout();Select(raider.Tile);ShowMilitaryOrders(raider);Click("Napad na dostawy #"+raider.Id);
            Check(Text("NAPAD NA DOSTAWY")&&Text("20 pakietów")&&Text("10 złota"),"The real military order opens an exact supply raid quote");before=SaveGame.Serialize(state);Click("Wycofaj rozkaz");Check(SaveGame.Serialize(state)==before,"Cancelling a raid preserves stocks cash movement and RNG");
            ShowMilitaryOrders(raider);Click("Napad na dostawy #"+raider.Id);double gold=state.Player.Gold;int triangles=map.Scene!.ObjectTriangles;await Capture("field-supply-raid-preview");Click("Wykonaj napad na dostawy");
            Check(target.MilitarySupplies==20&&state.Player.Gold==gold+10&&raider.Actions==0&&state.Tiles[raider.Tile].Road,"Confirmed raid destroys finite supplies pays exact loot and preserves the road");
            Check(map.Scene!.ObjectTriangles<triangles,"Stock loss rebuilds the actual supply-crate geometry within the same turn");ShowMilitaryOrders(raider);root.UpdateLayout();Check(!Descendants<Button>(overlay!).Single(b=>b.Content?.ToString()=="Napad na dostawy #"+raider.Id).IsEnabled,"The real order prevents a repeated exhausted raid");CloseModal();map.Center(raider.Tile);await Capture("field-supply-raid-result");
            raider.Tile=7*state.Width+29;state.InvalidateLogistics();Refresh();Check(!Models(map.Scene!.Objects).Any(m=>SceneSurface.Key(m).StartsWith("logistics-depot")),"A currently unseen foreign depot does not expose its warehouse or stock geometry");
            var own=state.Cities[0];own.Buildings.Add(FieldLogistics.Depot);own.MilitarySupplies=60;state.Player.Technologies.UnionWith(EpochCatalog.All.Take(9).SelectMany(e=>e.Technologies));for(int i=0;i<9;i++)state.Player.EpochChoices[i]=0;Refresh();
            Check(Models(map.Scene!.Objects).Any(m=>SceneSurface.Key(m).StartsWith("logistics-depot-metal:")),"Industrial-era warehouses use the actual metal roof model");
            Check(arguments.Contains("--no-gpu")?map.Photographic==null:map.Photographic!=null,"The requested renderer displays the new logistics geometry: "+(map.GpuFailure??"no GPU error"));
            for(int i=0;i<12;i++)Engine.AddUnit(state,0,own.Tile,UnitKind.Spearmen);Refresh();ShowFieldSupply();Width=1200;Height=800;await Task.Delay(120);root.UpdateLayout();var scroll=Descendants<ScrollViewer>(overlay!).First(v=>v.ExtentHeight>v.ViewportHeight);scroll.ScrollToEnd();root.UpdateLayout();
            Check(Descendants<Button>(overlay!).Any(b=>b.Content?.ToString()=="Wróć do mapy")&&scroll.VerticalOffset>0,"A large reserve roster remains scrollable in a compact native window");await Capture("field-supply-compact");SaveGame.Deserialize(SaveGame.Serialize(state));
            checks.Add($"{checks.Count} field supply desktop integration checks passed.");File.WriteAllLines(Path.Combine(folder,"ui-smoke.txt"),checks);Close();
        }
        catch(Exception ex){checks.Add(ex.ToString());File.WriteAllLines(Path.Combine(folder,"ui-smoke.txt"),checks);if(!arguments.Contains("--smoke-force-failure"))await Capture("field-supply-failure");Application.Current.Shutdown(3);}
    }
}
