using Crown.Core;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Crown.Desktop;

public sealed partial class MainWindow
{
    private async Task IndustrialInvestmentSmoke(string folder)
    {
        Directory.CreateDirectory(folder);var checks=new List<string>();
        void Check(bool ok,string text){if(!ok)throw new InvalidOperationException(text);checks.Add("PASS "+text);}
        void Click(string text){root.UpdateLayout();var b=Descendants<Button>(overlay??root).First(b=>b.Content?.ToString()==text);if(!b.IsEnabled)throw new InvalidOperationException("Disabled: "+text);b.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));root.UpdateLayout();}
        bool Text(string text){root.UpdateLayout();return Descendants<TextBlock>(overlay??root).Any(t=>t.Text.Contains(text,StringComparison.OrdinalIgnoreCase));}
        void Resolve(){if(state.PendingEvent!=null){var e=ProvincialEvents.Current(state);int i=Enumerable.Range(0,e.Choices.Length).First(i=>state.Player.Gold+e.Choices[i].Gold>=0);if(!Narrative.Resolve(state,i).Success)throw new InvalidOperationException("Council failed");}}
        void Step(){CloseModal();Resolve();Advance();Resolve();CloseModal();root.UpdateLayout();}
        async Task Capture(string name){root.UpdateLayout();await Task.Delay(120);map.RenderGpuFrame();var bitmap=new RenderTargetBitmap((int)root.ActualWidth,(int)root.ActualHeight,96,96,PixelFormats.Pbgra32);bitmap.Render(root);var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(bitmap));using var file=File.Create(Path.Combine(folder,name+".png"));png.Save(file);}
        try
        {
            if(arguments.Contains("--smoke-force-failure"))throw new InvalidOperationException("Intentional industrial investment diagnostic failure.");
            var s=World.Create();Narrative.Resolve(s,0);s.Units.Clear();s.ContinueAfterVictory=true;
            foreach(var t in s.Tiles){t.Terrain=Terrain.Grassland;t.Owner=-1;t.Resource=Resource.None;t.Improvement=Improvement.None;t.Ruin=t.River=t.Road=false;t.RoadOwner=-1;t.Explored=true;}
            (int X,int Y)[] centers=[(12,12),(20,12),(30,20),(6,5)];foreach(var c in s.Cities){c.Tile=centers[c.Owner].Y*s.Width+centers[c.Owner].X;c.ProductionPaused=true;c.Food=2000;foreach(var t in s.Tiles.Where(t=>World.Distance(t,s.Tiles[c.Tile])<=2))t.Owner=c.Owner;}
            foreach(var f in s.Factions){f.Gold=10000;f.Research="";}foreach(var r in s.Relations)r.Trust=90;
            s.Player.Technologies.UnionWith(EpochCatalog.All.Take(5).SelectMany(e=>e.Technologies));for(int i=0;i<5;i++)s.Player.EpochChoices[i]=0;
            var city=s.Cities[0];city.Buildings.UnionWith(["market","warehouse","workshop","factory","housing","aqueduct","infirmary",Manufacturing.Toolmakers]);city.ProductionPaused=false;city.Project="treasury";city.RepeatProject=true;city.MarketStocks[TradeGood.Iron]=140;city.Focus=CityFocus.Industry;city.Prosperity=85;city.Loyalty=90;s.Player.Stability=95;s.Player.Focus=2;
            foreach(var t in World.Neighbors(s,city.Tile).Take(2)){t.Owner=0;t.Resource=Resource.Iron;t.Improvement=Improvement.Mine;}
            Manufacturing.SetPolicy(s,city.Id,CraftRecipe.Tools,50);s.InvalidateLogistics();Start(s);root.UpdateLayout();await Task.Delay(140);
            Check(arguments.Contains("--no-gpu")?map.Photographic==null:map.Photographic!=null,"The selected renderer runs the investment scenario");ShowManufacturing();Click("Inwestycje i koncesje przemysłowe");
            Check(Text("Inwestycje przemysłowe")&&Text("6 rzeczywiście")&&Text("dwa poziomy"),"The actual workshop entry exposes investment goals and equipment limits");
            string before=SaveGame.Serialize(state);string publicButton=$"Rozmowa: {city.Name} · Narzędzia · Inwestycja państwowa";Click(publicButton);
            Check(Text("120 złota")&&Text("co najmniej 4")&&Text("wpłaty przepadają"),"The public financing dialogue states upfront cost real production and failure liability");Click("Wróć bez podpisu");Check(SaveGame.Serialize(state)==before,"Leaving financing talks changes neither treasury nor machinery");
            Click(publicButton);double gold=state.Player.Gold;Click("Podpisz inwestycję");
            Check(state.Player.Gold==gold-120&&city.Factory.Level==0&&city.Factory.Funding==InvestmentFunding.Public,"The real signature pays once without granting machinery");
            Check(Text("Produkcja 0/6")&&Descendants<Button>(overlay!).Where(b=>b.Content?.ToString()?.StartsWith("Rozmowa:")==true).All(b=>!b.IsEnabled),"The ledger exposes zero progress and prevents concurrent investment signatures");
            Step();ShowIndustrialInvestments();Check(city.Factory.Phases==1&&city.Factory.Batches>0&&city.Factory.Spent==120+city.Factory.PhaseFee,"An ordinary economic phase pays supervision and credits actual manufactured batches");await Capture("industry-active");
            string path=Path.Combine(folder,"investment.crown");SaveGame.Write(state,path);before=SaveGame.Serialize(state);LoadPath(path);CloseModal();city=state.Cities[0];Check(SaveGame.Serialize(state)==before,"Desktop reload preserves funding dates batches receipts and RNG exactly");
            for(int i=0;i<7;i++)Step();ShowIndustrialInvestments();
            Check(city.Factory.Level==1&&city.Factory.Funding==InvestmentFunding.None&&city.Factory.RoyaltyRemaining==0&&Manufacturing.Limit(state,city)==3,"Eight actual economic phases establish a public upgrade without concession royalties");
            Check(Text("Rozbudowa zakończona")&&Text("urządzenia 1/2"),"The final public ledger reports the physical upgrade");await Capture("industry-public-completed");
            Check(Text(CampaignCalendar.At(state,city.Factory.StartedTurn)),"Investment history remains tied to the signed historical phase");
            while(state.Turn<13)Step();ShowIndustrialInvestments();string concession=$"Rozmowa: {city.Name} · Narzędzia · Koncesja kupiecka";Click(concession);
            Check(Text("30 złota")&&Text("24 wpłat")&&Text("Trzy kolejne niedobory"),"The concession dialogue exposes future obligations and repossession before signature");gold=state.Player.Gold;Click("Podpisz inwestycję");Check(state.Player.Gold==gold-30&&city.Factory.Funding==InvestmentFunding.Concession,"The concession signature pays its lower actual upfront cost");
            before=SaveGame.Serialize(state);Click("Rozważ odwołanie: "+city.Name);Click("Kontynuuj inwestycję");Check(SaveGame.Serialize(state)==before,"Declining cancellation preserves the signed contract");
            for(int i=0;i<8;i++)Step();ShowIndustrialInvestments();Check(city.Factory.Level==2&&city.Factory.RoyaltyRemaining==24&&city.Factory.RoyaltyPaid==0,"The second completed investment starts future royalties: "+$"level {city.Factory.Level}, phases {city.Factory.Phases}, productive {city.Factory.ProductivePhases}, batches {city.Factory.Batches}, royalty {city.Factory.RoyaltyRemaining}");await Capture("industry-concession");
            Step();ShowIndustrialInvestments();Check(city.Factory.RoyaltyRemaining==23&&city.Factory.RoyaltyPaid==city.Factory.RoyaltyFee&&Text("23/24 wpłat"),"The next real turn collects one royalty and updates the visible obligation");
            before=SaveGame.Serialize(state);SaveGame.Write(state,path);LoadPath(path);CloseModal();city=state.Cities[0];Check(SaveGame.Serialize(state)==before,"Desktop reload retains paid royalties and both equipment levels");
            Width=1200;Height=800;ShowIndustrialInvestments();root.UpdateLayout();foreach(var scroll in Descendants<ScrollViewer>(overlay!))scroll.ScrollToBottom();await Capture("industry-compact");var back=Descendants<Button>(overlay!).Single(b=>b.Content?.ToString()=="Wróć do mapy");var bounds=back.TransformToAncestor(root).TransformBounds(new Rect(new Point(),back.RenderSize));
            Check(back.IsVisible&&bounds.Top>=0&&bounds.Bottom<=root.ActualHeight,"The complete investment ledger and exit remain reachable in a compact window");Click("Wróć do mapy");Check(overlay==null,"The investment ledger returns control to the map");
            checks.Add($"{checks.Count} industrial investment desktop integration checks passed.");File.WriteAllLines(Path.Combine(folder,"ui-smoke.txt"),checks);Close();
        }
        catch(Exception e){checks.Add(e.ToString());if(map?.GpuFailure!=null)checks.Add(map.GpuFailure);File.WriteAllLines(Path.Combine(folder,"ui-smoke.txt"),checks);Application.Current.Shutdown(3);}
    }
}
