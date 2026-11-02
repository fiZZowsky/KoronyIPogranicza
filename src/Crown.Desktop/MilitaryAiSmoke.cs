using Crown.Core;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Crown.Desktop;

public sealed partial class MainWindow
{
    private async Task MilitaryAiSmoke(string folder)
    {
        Directory.CreateDirectory(folder);var checks=new List<string>();
        void Check(bool value,string text){if(!value)throw new InvalidOperationException(text);checks.Add("PASS "+text);}
        void Click(string text){root.UpdateLayout();var button=Descendants<Button>(overlay??root).First(b=>b.Content?.ToString()==text);if(!button.IsEnabled)throw new InvalidOperationException("Disabled: "+text);button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));root.UpdateLayout();}
        bool Text(string text)=>Descendants<TextBlock>(overlay!).Any(t=>t.Text.Contains(text));
        async Task Capture(string name){root.UpdateLayout();await Task.Delay(150);map?.RenderGpuFrame();var bitmap=new RenderTargetBitmap((int)root.ActualWidth,(int)root.ActualHeight,96,96,PixelFormats.Pbgra32);bitmap.Render(root);var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(bitmap));using var output=File.Create(Path.Combine(folder,name+".png"));png.Save(output);}
        try
        {
            if(arguments.Contains("--smoke-force-failure"))throw new InvalidOperationException("Intentional military AI diagnostic failure.");
            var s=World.Create();Narrative.Resolve(s,0);s.Units.Clear();var city=s.Cities.First(c=>c.Owner==0);
            foreach(var t in s.Tiles.Where(t=>World.Distance(t,s.Tiles[city.Tile])<=9)){t.Terrain=Terrain.Grassland;t.Ruin=false;}
            var guard=Engine.AddUnit(s,0,city.Tile,UnitKind.Spearmen);
            var contact=s.Tiles.First(t=>World.Distance(t,s.Tiles[city.Tile])==2&&!s.Cities.Any(c=>c.Tile==t.Id));
            var enemy=Engine.AddUnit(s,1,contact.Id,UnitKind.Cavalry);enemy.ModelId="stellar_rovers";
            var distant=s.Tiles.First(t=>World.Distance(t,s.Tiles[city.Tile])==8&&t.Passable&&!s.Cities.Any(c=>c.Tile==t.Id));
            var unseen=Engine.AddUnit(s,1,distant.Id,UnitKind.Catapult);unseen.ModelId="deep_space_battery";
            s.Relation(0,1).War=true;s.InvalidateLogistics();Start(s);root.UpdateLayout();await Task.Delay(180);
            string before=SaveGame.Serialize(s);SaveGame.Deserialize(before);Click("Ocena sytuacji wojskowej");
            Check(Text("SZTAB MARCHII"),"The actual city button opens the military assessment");
            Check(Text("Widoczni przeciwnicy: 1"),"The assessment reports the real nearby observed hostile force");
            Check(!Text("Widoczni przeciwnicy: 2")&&!Text("Bateria dalekiego zasięgu"),"An unseen distant enemy is absent from the assessment");
            Check(Text("Przeciwnik ma przewagę polową"),"The assessment explains an actual local strength deficit");
            Check(Text("Brak kontaktu nie oznacza bezpiecznej granicy"),"The report explains the limits of current reconnaissance");
            Check(Text("Z dostawami: 1/1"),"Supply counts use the real friendly army");
            Check(SaveGame.Serialize(s)==before,"Opening the assessment preserves campaign orders and RNG");await Capture("military-assessment");
            Click("Plan obrony i produkcji: "+city.Name);Check(Descendants<Button>(overlay!).Any(b=>b.Content?.ToString()=="Dzielnice i podział pracy"),"Defense planning opens the existing actual city production controls");CloseModal();
            ShowArmy();root.UpdateLayout();Click("Ocena sytuacji wojskowej");Check(Text("Ocena sytuacji wojskowej"),"The real army roster also opens the assessment");
            Click("Pokaż miasto: "+city.Name);Check(overlay==null&&selectedTile==city.Tile,"The assessment locates the selected city on the actual map");
            s.Units.Remove(enemy);s.Relation(0,1).War=false;s.InvalidateLogistics();Refresh();Click("Ocena sytuacji wojskowej");
            Check(Text("Brak zaobserwowanych wojsk przeciwnika"),"A peaceful fresh report does not retain stale war warnings");CloseModal();
            var isolated=Engine.AddUnit(s,0,distant.Id,UnitKind.Spearmen);Refresh();Click("Ocena sytuacji wojskowej");
            Check(Text("Z dostawami: 1/2"),"An isolated friendly army changes the real supply count");
            Click($"Bez dostaw: #{isolated.Id} · {Crown.Core.UnitModels.Name(isolated)} · zdrowie 100%");Check(selectedUnit==isolated.Id&&Text("Brak dostaw"),"An unsupplied unit entry opens its actual order panel");CloseModal();
            Select(city.Tile);Click("Ocena sytuacji wojskowej");before=SaveGame.Serialize(state);Width=1200;Height=800;await Task.Delay(180);root.UpdateLayout();
            Check(Descendants<ScrollViewer>(overlay!).Any()&&Descendants<Button>(overlay!).Any(b=>b.Content?.ToString()=="Wróć do mapy"),"The complete assessment is scrollable in a compact window");await Capture("military-assessment-compact");
            Check(before==SaveGame.Serialize(state),"Assessment resizing and camera navigation preserve exact state");
            string path=Path.Combine(folder,"military-assessment.crown");SaveGame.Write(state,path);LoadPath(path);CloseModal();Select(city.Tile);Click("Ocena sytuacji wojskowej");
            Check(Text("Z dostawami: 1/2")&&SaveGame.Serialize(state)==before,"The report is correctly rebuilt after an unchanged v20 save reload");CloseModal();
            checks.Add($"{checks.Count} military AI desktop integration checks passed.");File.WriteAllLines(Path.Combine(folder,"ui-smoke.txt"),checks);Close();
        }
        catch(Exception ex){checks.Add(ex.ToString());File.WriteAllLines(Path.Combine(folder,"ui-smoke.txt"),checks);if(!arguments.Contains("--smoke-force-failure"))await Capture("military-assessment-failure");Application.Current.Shutdown(3);}
    }
}
