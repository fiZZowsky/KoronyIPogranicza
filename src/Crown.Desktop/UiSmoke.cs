using Crown.Core;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Crown.Desktop;

public sealed partial class MainWindow
{
    private static IEnumerable<T> Descendants<T>(DependencyObject parent) where T:DependencyObject
    {
        for(int i=0;i<VisualTreeHelper.GetChildrenCount(parent);i++){var child=VisualTreeHelper.GetChild(parent,i);if(child is T t)yield return t;foreach(var d in Descendants<T>(child))yield return d;}
    }
    private async Task Smoke(string folder)
    {
        var checks=new List<string>();Directory.CreateDirectory(folder);
        void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);checks.Add("PASS "+message);}
        async Task Capture(string name)
        {
            root.UpdateLayout();await Task.Delay(100);var bmp=new RenderTargetBitmap((int)root.ActualWidth,(int)root.ActualHeight,96,96,PixelFormats.Pbgra32);bmp.Render(root);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bmp));using var output=File.Create(Path.Combine(folder,name+".png"));encoder.Save(output);
        }
        void Click(string text)
        {
            root.UpdateLayout();var button=Descendants<Button>(root).First(b=>b.IsEnabled&&(b.Content?.ToString()?.StartsWith(text)==true||b.Content is StackPanel p&&p.Children.OfType<TextBlock>().Any(t=>t.Text.Contains(text))));button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        }
        try
        {
            if(arguments.Contains("--smoke-force-failure"))throw new InvalidOperationException("Intentional diagnostic failure to verify process exit propagation.");
            Landing();await Capture("menu");Click("ROZPOCZNIJ");Check(state.PendingEvent=="founding"&&overlay!=null,"New game opens the council");await Capture("council");
            Click("01   Ziemia");Check(state.Player.Flags.Contains("land_charter")&&overlay==null,"Council button applies the founding charter");await Capture("gameplay");
            var scout=state.Units.First(u=>u.Owner==0&&u.Kind==UnitKind.Scout);Select(scout.Tile);selectedUnit=scout.Id;var target=World.Neighbors(state,scout.Tile).First(t=>t.Passable&&t.Owner==0&&!state.Units.Any(u=>u.Owner!=0&&u.Tile==t.Id));Order(target.Id);Check(scout.Tile==target.Id,"Map order moves a selected unit");
            tab="research";RefreshSide();Click("Rozpocznij badania");Check(state.Player.Research=="pottery","Research button changes the technology");await Capture("research");
            tab="policy";RefreshSide();root.UpdateLayout();var combos=Descendants<ComboBox>(side).ToArray();combos[0].SelectedIndex=2;Click("Przyjmij reformę");Check(state.Player.Tax==2,"Policy form changes taxation");await Capture("policy");
            var city=state.Cities.First(c=>c.Owner==0);ShowProjects(city);Click("Roboty publiczne");Check(city.Project=="treasury"&&overlay==null,"Production dialog changes the project");
            // A treaty is tested through the same WPF button handler used by a human player.
            state.Player.Gold=150;tab="diplomacy";RefreshSide();Click("Traktat handlowy");Check(state.Relation(0,1).Trade,"Diplomacy button signs a trade treaty");await Capture("diplomacy");
            for(int i=0;i<12;i++)
            {
                if(state.PendingEvent!=null)
                {
                    ShowCouncil();var e=Narrative.Get(state.PendingEvent);int choice=Array.FindIndex(e.Choices,c=>state.Player.Gold+c.Gold>=0);Click($"{choice+1:00}   ");
                }
                Advance();
            }
            CloseModal();Check(state.Turn==13,"Turn button advances twelve turns with council decisions");
            string path=Path.Combine(folder,"smoke-save.crown");SaveGame.Write(state,path);string before=SaveGame.Serialize(state);LoadPath(path);Check(SaveGame.Serialize(state)==before,"Desktop reload restores the complete game");CloseModal();tab="realm";Select(city.Tile);await Capture("turn-13");
            var scenario=World.Create();Narrative.Resolve(scenario,0);scenario.Player.Technologies.UnionWith(["pottery","agriculture","mining","bronze"]);scenario.Player.Gold=3000;
            var startCity=scenario.Cities.First(c=>c.Owner==0);int endTile=20*scenario.Width+13;
            var cursor=scenario.Tiles[startCity.Tile];var goal=scenario.Tiles[endTile];goal.Terrain=Terrain.Grassland;
            while(cursor.Id!=goal.Id){cursor=World.Neighbors(scenario,cursor.Id).OrderBy(t=>World.Distance(t,goal)).First();if(!cursor.Passable)cursor.Terrain=Terrain.Grassland;cursor.Explored=true;}
            var newCity=Engine.AddCity(scenario,0,endTile,"Brzegowo");Engine.Explore(scenario);Start(scenario);
            tab="logistics";RefreshSide();Click("Połącz ze stolicą");
            Check(Logistics.For(state,0).ConnectedCities.Contains(newCity.Id),"Infrastructure button builds a complete capital connection");
            map.ShowSupply=true;RefreshSide();map.Center(startCity.Tile);await Capture("logistics");
            var marching=state.Units.First(u=>u.Owner==0&&u.Kind==UnitKind.Spearmen);Select(marching.Tile);selectedUnit=marching.Id;Refresh();await Capture("movement-range");
            Order(newCity.Tile);Check(marching.Destination==newCity.Tile,"Distant map order queues a multi-turn march");await Capture("march-order");
            int pauseTile=marching.Tile;Click("Anuluj rozkaz marszu");Check(marching.Destination==null&&marching.Tile==pauseTile,"Cancel march button preserves the current tile");
            Order(newCity.Tile);for(int i=0;i<4&&marching.Destination!=null;i++)Advance();CloseModal();
            Check(marching.Tile==newCity.Tile&&marching.Destination==null,"Turn handler continues and completes the saved march");
            var battleTile=World.Neighbors(state,marching.Tile).First(t=>t.Passable&&!state.Cities.Any(c=>c.Tile==t.Id)&&!state.Units.Any(u=>u.Tile==t.Id));
            state.Relation(0,1).War=true;var opponent=Engine.AddUnit(state,1,battleTile.Id,UnitKind.Spearmen);marching.Actions=marching.MaxActions;
            Select(marching.Tile);selectedUnit=marching.Id;Refresh();var prediction=Engine.PreviewAttack(state,marching.Id,opponent.Tile);
            Order(opponent.Tile);Check(overlay!=null&&opponent.Health==100,"Enemy map order opens a pure combat preview");await Capture("combat-preview");
            Click("Wycofaj rozkaz");Check(overlay==null&&opponent.Health==100&&marching.Actions==marching.MaxActions,"Cancelling combat preview spends no movement");
            Order(opponent.Tile);Click("Wykonaj atak");
            Check(Math.Abs(opponent.Health-(100-prediction.DamageToTarget))<.00001&&marching.Actions==0,"Combat confirmation matches the preview");
            Width=1200;Height=800;tab="logistics";RefreshSide();await Capture("compact-layout");
            Check(root.ActualWidth>=1100&&endButton.IsVisible,"Compact desktop layout keeps the turn button visible");
            Width=1600;Height=980;
            var society=World.Create();Narrative.Resolve(society,0);society.Player.Gold=3000;society.Player.Technologies.UnionWith(["pottery","agriculture","masonry"]);
            var oldTown=society.Cities.First(c=>c.Owner==0);int destination=20*society.Width+13;
            society.Tiles[destination].Terrain=Terrain.Grassland;
            foreach(var tile in society.Tiles.Where(t=>World.Distance(t,society.Tiles[oldTown.Tile])<=2))
            {tile.Terrain=Terrain.Desert;tile.Resource=Resource.None;tile.River=false;tile.Ruin=false;}
            var routeTile=society.Tiles[oldTown.Tile];
            while(routeTile.Id!=destination){routeTile=World.Neighbors(society,routeTile.Id).OrderBy(t=>World.Distance(t,society.Tiles[destination])).First();if(!routeTile.Passable)routeTile.Terrain=Terrain.Grassland;routeTile.Explored=true;}
            var haven=Engine.AddCity(society,0,destination,"Brzegowo");oldTown.Population=10;oldTown.Prosperity=20;oldTown.Unrest=50;oldTown.Food=0;haven.Prosperity=85;
            Engine.Explore(society);Check(Engine.ConnectCity(society,haven.Id).Success,"Provincial scenario has a funded road network");society.Turn=8;
            int residents=society.Cities.Where(c=>c.Owner==0).Sum(c=>c.Population);Regions.Migrate(society,0);
            Check(haven.MigrantsReceived==1&&society.Cities.Where(c=>c.Owner==0).Sum(c=>c.Population)==residents,"Society scenario records a population-conserving migration");
            Start(society);tab="provinces";RefreshSide();map.ShowPopulation=true;map.ShowSupply=false;RefreshSide();map.Center(oldTown.Tile);await Capture("provinces");
            Check(Regions.PlanFood(state,0).Imports[oldTown.Id]>0,"Province panel exposes road-based food imports");
            Click("Zarządzaj: "+oldTown.Name);root.UpdateLayout();var cityFocus=Descendants<ComboBox>(overlay!).Single();cityFocus.SelectedIndex=2;
            Click("Zmień kierunek");Check(oldTown.Focus==CityFocus.Industry,"Provincial form changes the local development focus");
            double reserve=oldTown.Food;Click("Pomoc żywnościowa");Check(oldTown.Food==reserve+25&&oldTown.LastAidTurn==state.Turn,"Provincial aid button funds food reserves and sets a cooldown");await Capture("province-management");CloseModal();
            state.Turn=10;Narrative.Schedule(state);Check(state.PendingEvent=="local_housing"&&state.PendingCityId==oldTown.Id,"Council selects a concrete overcrowded province");
            ShowCouncil();await Capture("province-council");Click("01   Ustanowimy");
            Check(oldTown.Charters.Contains("housing_rights")&&!haven.Charters.Contains("housing_rights"),"Local council choice grants housing rights only to the affected city");
            Narrative.Schedule(state);Check(state.PendingEvent=="local_settlement"&&state.PendingCityId==haven.Id,"Migration generates a settlement dialogue in the receiving city");
            ShowCouncil();Click("01   Otwórzmy");Check(haven.Charters.Contains("integration"),"Settlement dialogue installs the integration charter");
            string societyPath=Path.Combine(folder,"society-save.crown");SaveGame.Write(state,societyPath);string societyBefore=SaveGame.Serialize(state);LoadPath(societyPath);
            Check(SaveGame.Serialize(state)==societyBefore,"Desktop reload preserves provincial rights and migration history");
            CloseModal();tab="provinces";map.ShowPopulation=true;map.ShowSupply=false;RefreshSide();map.Center(oldTown.Tile);await Capture("provinces-final");
            checks.Add($"{checks.Count} desktop integration checks passed.");File.WriteAllLines(Path.Combine(folder,"ui-smoke.txt"),checks);Close();
        }
        catch(Exception e){checks.Add(e.ToString());File.WriteAllLines(Path.Combine(folder,"ui-smoke.txt"),checks);Application.Current.Shutdown(3);}
    }
}
