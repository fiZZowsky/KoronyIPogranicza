using Crown.Core;
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
    private async Task SiegeSmoke(string folder)
    {
        Directory.CreateDirectory(folder);var checks=new List<string>();
        void Check(bool value,string text){if(!value)throw new InvalidOperationException(text);checks.Add("PASS "+text);}
        void Click(string text,bool prefix=false){root.UpdateLayout();var button=Descendants<Button>(overlay??root).First(b=>prefix?b.Content?.ToString()?.StartsWith(text)==true:b.Content?.ToString()==text);if(!button.IsEnabled)throw new InvalidOperationException("Disabled: "+text);button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));root.UpdateLayout();}
        bool Text(string text)=>Descendants<TextBlock>(overlay!).Any(t=>t.Text.Contains(text));
        IEnumerable<GeometryModel3D> Models(Model3D model){if(model is GeometryModel3D g)yield return g;if(model is Model3DGroup group)foreach(var child in group.Children)foreach(var item in Models(child))yield return item;}
        async Task Capture(string name){root.UpdateLayout();await Task.Delay(120);map.RenderGpuFrame();var bitmap=new RenderTargetBitmap((int)root.ActualWidth,(int)root.ActualHeight,96,96,PixelFormats.Pbgra32);bitmap.Render(root);var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(bitmap));using var file=File.Create(Path.Combine(folder,name+".png"));png.Save(file);}
        void Step(){if(state.PendingEvent!=null)Narrative.Resolve(state,0);Advance();if(state.PendingEvent!=null)Narrative.Resolve(state,0);CloseModal();root.UpdateLayout();}
        string PixelHash(){var image=map.Photographic!.Image!;byte[] data=new byte[image.PixelWidth*image.PixelHeight*4];image.CopyPixels(data,image.PixelWidth*4,0);return Convert.ToHexString(SHA256.HashData(data));}
        try
        {
            if(arguments.Contains("--smoke-force-failure"))throw new InvalidOperationException("Intentional siege diagnostic failure.");
            var s=World.Create();Narrative.Resolve(s,0);s.Units.Clear();
            foreach(var t in s.Tiles){t.Terrain=Terrain.Grassland;t.Owner=-1;t.Resource=Resource.None;t.Improvement=Improvement.None;t.Ruin=t.Road=t.River=false;t.RoadOwner=-1;t.Explored=true;}
            (int X,int Y)[] centers=[(12,12),(20,12),(30,20),(6,5)];foreach(var c in s.Cities){c.Tile=centers[c.Owner].Y*s.Width+centers[c.Owner].X;c.ProductionPaused=true;foreach(var t in s.Tiles.Where(t=>World.Distance(t,s.Tiles[c.Tile])<=2))t.Owner=c.Owner;}
            foreach(var f in s.Factions)f.Gold=10000;
            s.Player.Technologies.UnionWith(EpochCatalog.All.Take(8).SelectMany(e=>e.Technologies));for(int i=0;i<8;i++)s.Player.EpochChoices[i]=0;
            foreach(var t in s.Tiles.Where(t=>t.X>=10&&t.X<=32&&t.Y>=13&&t.Y<=15))t.Terrain=t.Y==14?Terrain.Ocean:Terrain.Coast;
            s.Cities[0].Buildings.Add(Navy.Port);s.Cities[1].Buildings.Add(Navy.Port);s.Relation(0,1).War=true;
            var ship=Engine.AddUnit(s,0,13*s.Width+12,UnitKind.Ship,"carrack");var troop=Engine.AddUnit(s,0,s.Cities[0].Tile,UnitKind.Spearmen,"line_infantry");
            int shore=16*s.Width+12;var enemy=Engine.AddUnit(s,1,shore,UnitKind.Spearmen,"hunters");enemy.Health=1;s.Tiles[shore].Owner=1;
            s.InvalidateLogistics();Start(s);root.UpdateLayout();await Task.Delay(160);Click("Flota i porty");Click("Oblężenia i przyczółki");
            Check(Text("SZTAB OPERACJI WYBRZEŻNYCH")&&Text("naprawy +7/turę"),"The fleet opens the real siege and beachhead report");
            string before=SaveGame.Serialize(state);ShowSieges();Check(SaveGame.Serialize(state)==before,"Reading siege intelligence leaves state and RNG unchanged");
            Check(!Text(s.Cities[2].Name),"The report omits currently unseen foreign cities");
            ShowTransport(ship);Click("Zaokrętuj #"+troop.Id,true);Click("Pokaż okręt");int sea=15*state.Width+12;
            Check(Engine.OrderMove(state,ship.Id,sea).Success&&troop.Tile==sea,"A real sea order carries the assault infantry to the enemy shore");Refresh();ShowTransport(ship);Click("Wybierz brzeg dla #"+troop.Id);
            Check(map.LandingSites.Contains(shore),"The real landing planner includes a defended legal assault shore");before=SaveGame.Serialize(state);Select(shore);root.UpdateLayout();
            Check(Text("SZTURM Z MORZA")&&Text("Przewidywane straty")&&Text("0,65"),"The map opens the assault forecast and explicit landing penalty");
            Check(SaveGame.Serialize(state)==before,"Opening the assault confirmation spends no resources");await Capture("siege-assault-preview");Click("Anuluj wyładunek");
            Check(landingPassenger==null&&SaveGame.Serialize(state)==before,"Cancelling the assault preserves the entire campaign");ShowTransport(ship);Click("Wybierz brzeg dla #"+troop.Id);Select(shore);root.UpdateLayout();double moves=ship.Actions;Click("Rozpocznij szturm z morza");
            Check(!state.Units.Contains(enemy)&&troop.EmbarkedOnId==null&&troop.Tile==shore&&troop.Actions==0&&ship.Actions==0,"Confirmed assault defeats defenders and lands exhausted infantry at the actual cost");
            Check(landingPassenger==null&&map.LandingSites.Count==0,"Successful assault clears the map planning state");
            Step();Select(troop.Tile);ShowMilitaryOrders(troop);Click("Okop oddział");Check(troop.Order==MilitaryOrder.Fortify&&troop.FortificationTurns==0,"The existing military order begins genuine beachhead preparation");CloseModal();Step();
            Check(SiegeWarfare.Beachheads(state,0).Any(g=>g.Id==troop.Id)&&Engine.Supplied(state,troop),"Ordinary turns activate fleet-backed beachhead supply");
            Check(Models(map.Scene!.Objects).Any(m=>SceneSurface.Key(m).StartsWith("beachhead-crates-wood")),"An active beachhead has a real flag and supply-crate model");
            Check(Models(map.Scene.Objects).Any(m=>SceneSurface.Key(m).StartsWith("frontline-stakes-wood")),"Prepared field positions appear in actual scene geometry");ShowSieges();root.UpdateLayout();Check(Text("Przyczółek #"+troop.Id),"The report exposes the established beachhead and its map link");Click("Wróć do mapy");
            Select(ship.Tile);selectedUnit=ship.Id;map.Center(ship.Tile);map.Zoom(.65);Refresh();map.RenderGpuFrame();await Capture("siege-beachhead");
            Check(Models(map.Scene.Objects).Any(m=>SceneSurface.Key(m).StartsWith("naval-rope"))&&Models(map.Scene.Objects).Any(m=>SceneSurface.Key(m).StartsWith("naval-port-bollards-metal")),"Ships and ports include actual rigging and mooring fittings");
            bool fallback=arguments.Contains("--no-gpu");Check(fallback?map.Photographic==null:map.Photographic!=null,"The requested rendering backend initializes successfully: "+(map.GpuFailure??"no GPU error"));
            if(!fallback)
            {
                map.RenderGpuFrame();string first=PixelHash();map.Photographic!.ShoreEffects=false;map.RenderGpuFrame();Check(first!=PixelHash(),"Shore bathymetry and wave foam change rendered pixels");map.Photographic.ShoreEffects=true;
                map.RenderGpuFrame();first=PixelHash();map.Photographic.Shadows=false;map.RenderGpuFrame();Check(first!=PixelHash(),"Detailed coastal models receive real GPU shadows");map.Photographic.Shadows=true;
            }
            else{Check(map.Scene.ObjectTriangles>0,"The WPF fallback keeps detailed coastal geometry");Check(map.Viewport3D.Children.Count>0,"The WPF fallback maintains a visible native 3D scene");}
            string path=Path.Combine(folder,"siege-campaign.crown");SaveGame.Write(state,path);before=SaveGame.Serialize(state);LoadPath(path);CloseModal();
            Check(SaveGame.Serialize(state)==before&&SiegeWarfare.Beachheads(state,0).Any(),"Desktop save and reload preserve the derived beachhead from saved positions and orders");
            ShowSieges();Width=1200;Height=800;await Task.Delay(120);root.UpdateLayout();Check(Descendants<ScrollViewer>(overlay!).Any()&&Descendants<Button>(overlay!).Any(b=>b.Content?.ToString()=="Wróć do mapy"),"The siege report remains navigable in a compact window");await Capture("siege-compact");CloseModal();
            SaveGame.Deserialize(SaveGame.Serialize(state));checks.Add($"{checks.Count} siege desktop integration checks passed.");File.WriteAllLines(Path.Combine(folder,"ui-smoke.txt"),checks);Close();
        }
        catch(Exception ex){checks.Add(ex.ToString());File.WriteAllLines(Path.Combine(folder,"ui-smoke.txt"),checks);if(!arguments.Contains("--smoke-force-failure"))await Capture("siege-failure");Application.Current.Shutdown(3);}
    }
}
