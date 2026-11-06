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
    private async Task ManufacturingSmoke(string folder)
    {
        Directory.CreateDirectory(folder);var checks=new List<string>();
        void Check(bool value,string text){if(!value)throw new InvalidOperationException(text);checks.Add("PASS "+text);}
        void Click(string text,bool prefix=false){root.UpdateLayout();var b=Descendants<Button>(overlay??root).First(b=>prefix?b.Content?.ToString()?.StartsWith(text)==true:b.Content?.ToString()==text);if(!b.IsEnabled)throw new InvalidOperationException("Disabled: "+text);b.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));root.UpdateLayout();}
        bool Text(string value){root.UpdateLayout();return Descendants<TextBlock>(overlay!).Any(t=>t.Text.Contains(value,StringComparison.OrdinalIgnoreCase));}
        IEnumerable<GeometryModel3D> Models(Model3D model){if(model is GeometryModel3D g)yield return g;if(model is Model3DGroup group)foreach(var child in group.Children)foreach(var item in Models(child))yield return item;}
        async Task Capture(string name){root.UpdateLayout();await Task.Delay(120);map.RenderGpuFrame();var image=new RenderTargetBitmap((int)root.ActualWidth,(int)root.ActualHeight,96,96,PixelFormats.Pbgra32);image.Render(root);var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(image));using var file=File.Create(Path.Combine(folder,name+".png"));png.Save(file);}
        void Step(){CloseModal();if(state.PendingEvent!=null)Narrative.Resolve(state,0);Advance();if(state.PendingEvent!=null)Narrative.Resolve(state,0);CloseModal();root.UpdateLayout();}
        try
        {
            if(arguments.Contains("--smoke-force-failure"))throw new InvalidOperationException("Intentional manufacturing diagnostic failure.");
            var s=World.Create();Narrative.Resolve(s,0);s.Units.Clear();foreach(var t in s.Tiles){t.Terrain=Terrain.Grassland;t.Owner=-1;t.Resource=Resource.None;t.Improvement=Improvement.None;t.Ruin=t.River=t.Road=false;t.RoadOwner=-1;t.Explored=true;}
            (int X,int Y)[] centers=[(12,12),(20,12),(30,20),(6,5)];foreach(var c in s.Cities){c.Tile=centers[c.Owner].Y*s.Width+centers[c.Owner].X;c.ProductionPaused=true;foreach(var t in s.Tiles.Where(t=>World.Distance(t,s.Tiles[c.Tile])<=2))t.Owner=c.Owner;}
            foreach(var f in s.Factions)f.Gold=10000;s.Player.Technologies.UnionWith(EpochCatalog.All.Take(5).SelectMany(e=>e.Technologies));for(int i=0;i<5;i++)s.Player.EpochChoices[i]=0;
            var city=s.Cities[0];city.Buildings.UnionWith(["market","workshop","factory"]);city.ProductionPaused=false;city.Food=0;city.MarketStocks[TradeGood.Dyes]=40;
            var donor=Engine.AddCity(s,0,12*s.Width+16,"Skład nad Rzeką");donor.Buildings.Add("warehouse");donor.MarketStocks[TradeGood.Iron]=100;donor.ProductionPaused=true;
            var foreign=s.Cities[1];foreign.Buildings.UnionWith(["market",Manufacturing.Guildhall]);s.Factions[1].Technologies.Add("trade");s.Relation(0,1).Trade=true;
            for(int x=12;x<=20;x++){var t=s.Tiles[12*s.Width+x];t.Road=true;t.RoadOwner=t.Owner=x<=16?0:1;}s.InvalidateLogistics();Start(s);root.UpdateLayout();await Task.Delay(160);tab="markets";RefreshSide();Click("Warsztaty i łańcuchy produkcji");
            Check(Text("RZEMIOSŁO I SIEĆ WYMIANY")&&Text("Narzędzia")&&Text("Tkaniny"),"The real market entry opens the crafting and regional input panel");
            string before=SaveGame.Serialize(state);ShowManufacturing();Check(SaveGame.Serialize(state)==before,"Browsing the complete processing forecast preserves stocks and RNG");Click("Buduj: Warsztat narzędziowy · "+city.Name);
            Check(city.Project==Manufacturing.Toolmakers&&!city.Buildings.Contains(Manufacturing.Toolmakers),"The actual workshop button selects ordinary construction");city.Production=159.9;Step();ShowManufacturing();
            Check(city.Buildings.Contains(Manufacturing.Toolmakers)&&city.MarketStocks[TradeGood.Tools]==0&&city.IndustryShare==0,"A normal economic turn completes the prepared workshop without free goods");Click("Plan warsztatów: "+city.Name);
            Check(Text("ROZMOWA Z MISTRZYNIĄ CECHÓW")&&Text("budowę i rekrutację"),"The guild conversation explains the processing tradeoff");before=SaveGame.Serialize(state);Click("Wróć do warsztatów");Check(SaveGame.Serialize(state)==before,"Leaving the guild conversation changes no materials or work");Click("Plan warsztatów: "+city.Name);Click("Narzędzia · nakład 50%");
            Check(city.IndustryShare==50&&city.IndustryRecipe==CraftRecipe.Tools&&city.MarketStocks[TradeGood.Tools]==0,"The actual tool plan commits a policy without instant output");Click("Plan warsztatów: "+city.Name);
            Check(Descendants<Button>(overlay!).Where(b=>b.Content?.ToString()?.Contains("nakład ")==true).All(b=>!b.IsEnabled),"The production dialogue enforces the once-per-turn plan change");Step();ShowManufacturing();
            Check(city.LastIndustryBatches>0&&city.LastIndustryWork>0&&city.MarketStocks[TradeGood.Tools]==city.LastIndustryBatches,"An ordinary city turn converts actual work into tools");
            Check(city.LastIndustryInputs.All(i=>i.CityId==donor.Id)&&donor.LastMarketLines.Single(l=>l.Good==TradeGood.Iron).IndustrialUsed==city.LastIndustryBatches*2&&Text("Zużyto z "+donor.Name),"Processing draws real iron from the connected warehouse and names its source");
            Check(Models(map.Scene!.Objects).Any(m=>SceneSurface.Key(m).StartsWith("crafting-anvil-metal"))&&Models(map.Scene.Objects).Any(m=>SceneSurface.Key(m).StartsWith("crafting-products-wood")),"Actual anvils and produced-goods stacks appear in the 3D scene");await Capture("manufacturing-tools");Click("Buduj: Tkalnia · "+city.Name);city.Production=129.9;Step();ShowManufacturing();
            Check(city.Buildings.Contains(Manufacturing.Weavers),"The second actual construction path completes a weaving facility");Click("Plan warsztatów: "+city.Name);Click("Tkaniny · nakład 50%");Step();ShowManufacturing();
            Check(city.LastIndustryRecipe==CraftRecipe.Textiles&&city.LastIndustryBatches>0&&city.MarketStocks[TradeGood.Textiles]==city.LastIndustryBatches*2,"The real plan switch produces two textiles per consumed dye unit");
            Check(Models(map.Scene!.Objects).Any(m=>SceneSurface.Key(m).StartsWith("crafting-loom-cloth")),"The actual weaving facility has visible cloth and loom geometry");Click("Buduj: Dom cechów · "+city.Name);city.Production=179.9;Step();
            Check(city.Buildings.Contains(Manufacturing.Guildhall)&&Manufacturing.ProductionBonus(city)==0,"A newly completed guildhall cannot claim unearned zero-demand benefits");Step();ShowManufacturing();
            Check(Manufacturing.ProductionBonus(city)>0&&Manufacturing.CultureBonus(city)>0&&Text("Dom cechów — ostatnie dostawy"),"Actual subsequent product consumption activates the guildhall's economic benefits");
            Click("Plan warsztatów: "+city.Name);Click("Narzędzia · nakład 50%");int preparation=0;while(city.MarketStocks[TradeGood.Tools]<Markets.Demand(city,TradeGood.Tools)+2&&preparation++<6)Step();ShowManufacturing();Click("Podpisz kontrakt na wyroby");
            var goods=Descendants<ComboBox>(overlay!).Single(c=>c.Items.Count==5);Check(goods.Items.Cast<object>().Any(x=>x.ToString()=="Narzędzia")&&goods.Items.Cast<object>().Any(x=>x.ToString()=="Tkaniny"),"The real contract selector exposes both manufactured products alongside raw materials");
            var direction=Descendants<ComboBox>(overlay!).Single(c=>c.Items.Count==2&&c.Items.Cast<object>().Any(x=>x.ToString()?.Contains("Eksport")==true));direction.SelectedIndex=1;goods.SelectedIndex=Array.IndexOf(Markets.Goods,TradeGood.Tools);Descendants<ComboBox>(overlay!).Single(c=>c.Items.Count==6).SelectedIndex=1;root.UpdateLayout();
            Check(Text("Cena orientacyjna")&&Text(city.Name+" → "+foreign.Name),"The export form shows the chosen product route and current price quote");Click("Podpisz umowę · 15 złota");
            var contract=state.TradeContracts.Single();Check(contract.Good==TradeGood.Tools&&contract.SellerCity==city.Id&&contract.BuyerCity==foreign.Id,"The actual signature creates the intended processed-goods export");Step();
            Check(contract.LastDelivered>0&&state.Caravans.Count==1&&foreign.LastMarketLines.Single(l=>l.Good==TradeGood.Tools).ForeignIn==0,"Paid manufactured exports enter an actual caravan rather than arriving instantly");int cargo=state.Caravans[0].Id;
            for(int i=0;i<6&&Caravans.Active(state.Caravans.Single(c=>c.Id==cargo));i++)Step();Check(state.Caravans.Single(c=>c.Id==cargo).Status==CaravanStatus.Delivered&&foreign.LastMarketLines.Single(l=>l.Good==TradeGood.Tools).ForeignIn>0,"The actual caravan delivers manufactured tools to the foreign guildhall");
            string path=Path.Combine(folder,"manufacturing-campaign.crown");SaveGame.Write(state,path);before=SaveGame.Serialize(state);LoadPath(path);CloseModal();city=state.Cities.Single(c=>c.Id==city.Id);
            Check(state.Version==24&&SaveGame.Serialize(state)==before&&city.LastIndustryBatches>0,"Desktop V24 reload preserves manufacturing receipts stocks and shipment history");
            var cut=state.Tiles[12*state.Width+14];cut.Road=false;cut.RoadOwner=-1;city.MarketStocks[TradeGood.Iron]=0;state.InvalidateLogistics();Refresh();ShowManufacturing();
            Check(Text("Brak składników")&&Manufacturing.Quote(state,city,100).Batches==0,"The actual processing panel explains loss of the disconnected supplier");CloseModal();Step();ShowManufacturing();Check(city.LastIndustryBatches==0&&city.LastIndustryWork==0,"A real subsequent turn cannot manufacture through a missing road");
            CloseModal();map.Center(city.Tile);await Capture("manufacturing-workshops");ShowManufacturing();root.UpdateLayout();
            Check(arguments.Contains("--no-gpu")?map.Photographic==null:map.Photographic!=null,"The requested rendering backend displays workshop geometry: "+(map.GpuFailure??"no GPU error"));Width=1200;Height=800;await Task.Delay(120);root.UpdateLayout();var scroll=Descendants<ScrollViewer>(overlay!).First(v=>v.ExtentHeight>v.ViewportHeight);scroll.ScrollToEnd();root.UpdateLayout();
            Check(scroll.VerticalOffset>0&&Descendants<Button>(overlay!).Any(b=>b.Content?.ToString()=="Wróć do mapy"),"A complete crafting and receipt panel remains scrollable at compact window size");await Capture("manufacturing-compact");SaveGame.Deserialize(SaveGame.Serialize(state));
            checks.Add($"{checks.Count} manufacturing desktop integration checks passed.");File.WriteAllLines(Path.Combine(folder,"ui-smoke.txt"),checks);Close();
        }
        catch(Exception ex){checks.Add(ex.ToString());File.WriteAllLines(Path.Combine(folder,"ui-smoke.txt"),checks);if(!arguments.Contains("--smoke-force-failure"))await Capture("manufacturing-failure");Application.Current.Shutdown(3);}
    }
}
