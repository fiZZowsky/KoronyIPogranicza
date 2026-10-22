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
            root.UpdateLayout();await Task.Delay(100);var bmp=new RenderTargetBitmap((int)root.ActualWidth,(int)root.ActualHeight,96,96,PixelFormats.Pbgra32);bmp.Render(root);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bmp));
            string path=Path.Combine(folder,name+".png"),temp=path+".tmp";
            using(var output=File.Create(temp))encoder.Save(output);
            // Replace the completed image atomically; a preview may still map the previous PNG.
            File.Move(temp,path,true);
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
            var politics=World.Create();Narrative.Resolve(politics,0);politics.Player.Gold=3000;politics.Relation(0,1).Trust=85;
            DiplomaticCouncil.AddGrievance(politics,0,1,40);Start(politics);tab="diplomacy";RefreshSide();
            Check(map.Viewport.Contains(MapView.CenterOf(state.Tiles[state.Cities[0].Tile])),"Starting another campaign centers the map after layout");
            Click("Negocjuj warunki: "+state.Factions[1].Name);await Capture("negotiation");
            Click("Wyślij: Odszkodowanie");Check(state.Demands.Single().Status==DemandStatus.Pending&&overlay==null,"Negotiation button sends a delayed compensation mission");
            string embassyPath=Path.Combine(folder,"embassy-save.crown");SaveGame.Write(state,embassyPath);LoadPath(embassyPath);
            Check(map.Viewport.Contains(MapView.CenterOf(state.Tiles[state.Cities[0].Tile])),"Loading a campaign keeps the capital inside the map viewport");
            Check(state.Demands.Single().Status==DemandStatus.Pending,"Desktop reload preserves an embassy in transit");
            Advance();Advance();CloseModal();
            Check(state.Turn==3&&state.Demands.Single().Status==DemandStatus.Accepted,"Two turn handlers deliver and apply the diplomatic reply");
            tab="diplomacy";RefreshSide();await Capture("diplomatic-reply");
            var capital=state.Cities.First(c=>c.Owner==0);ShowProvince(capital);Click("Ustanów: Autonomia");
            Check(capital.Governance==Governance.Autonomous&&capital.LastGovernanceTurn==state.Turn,"Governance button establishes local autonomy and locks its cooldown");CloseModal();
            DiplomaticCouncil.AddGrievance(state,0,1,30);RefreshSide();var foreign=state.Factions[1];ConfirmWar(foreign);await Capture("war-council");
            string warBefore=SaveGame.Serialize(state);Click("Wycofaj posłów");
            Check(!state.Relation(0,1).War&&SaveGame.Serialize(state)==warBefore,"Cancelling war council leaves the simulation unchanged");
            ConfirmWar(foreign);Click("Wypowiedz wojnę: Zadośćuczynienie");Check(state.Relation(0,1).War&&overlay==null,"War council button declares war using an available justification");
            var occupied=state.Cities.First(c=>c.Owner==1);ProvincialPolitics.Transfer(state,occupied,0);state.Turn=5;
            Engine.Explore(state);Narrative.Schedule(state);Refresh();map.Center(occupied.Tile);Check(state.PendingEvent=="local_autonomy"&&state.PendingCityId==occupied.Id,"Occupation generates a council request for provincial rights");
            ShowCouncil();await Capture("autonomy-council");Click("01   Uznajemy");
            Check(occupied.Governance==Governance.Autonomous&&occupied.Loyalty==40,"Autonomy dialogue applies governance and loyalty to the occupied city");
            tab="provinces";RefreshSide();map.Center(occupied.Tile);ShowProvince(occupied);await Capture("provincial-politics");CloseModal();
            string politicsPath=Path.Combine(folder,"politics-save.crown");SaveGame.Write(state,politicsPath);string politicsBefore=SaveGame.Serialize(state);LoadPath(politicsPath);
            Check(SaveGame.Serialize(state)==politicsBefore,"Desktop reload preserves diplomatic replies, occupation and local governance");
            CloseModal();
            var peaceWorld=World.Create();Narrative.Resolve(peaceWorld,0);peaceWorld.Player.Gold=4000;peaceWorld.Factions[1].Gold=2000;
            var site=peaceWorld.Tiles.First(t=>t.Passable&&t.Owner<0&&peaceWorld.Cities.All(c=>World.Distance(t,peaceWorld.Tiles[c.Tile])>=4));
            var disputed=Engine.AddCity(peaceWorld,0,site.Id,"Sporny Brzeg");ProvincialPolitics.Transfer(peaceWorld,disputed,1);
            Engine.DeclareWar(peaceWorld,1,WarReason.Liberation,disputed.Id);peaceWorld.Turn=2;peaceWorld.Factions[1].WarWeariness=80;Engine.Explore(peaceWorld);
            Start(peaceWorld);tab="diplomacy";RefreshSide();Click("Przebieg wojny: "+state.Factions[1].Name);
            Check(overlay!=null&&state.Wars.Single().TargetCityId==disputed.Id,"War report opens with the declared liberation target");await Capture("war-report");
            Click("Przejdź do rokowań");root.UpdateLayout();var treatyInputs=Descendants<ComboBox>(overlay!).ToArray();treatyInputs[0].SelectedIndex=4;treatyInputs[1].SelectedIndex=1;
            await Capture("peace-conference");Click("Wyślij warunki pokoju");
            Check(state.PeaceOffers.Single().ReturnCityId==disputed.Id&&state.PeaceOffers.Single().Gold==200&&state.Relation(0,1).War,"Peace form sends selected reparations and city return while war continues");
            string treatyPath=Path.Combine(folder,"treaty-save.crown");SaveGame.Write(state,treatyPath);LoadPath(treatyPath);
            Check(state.PeaceOffers.Single().Status==PeaceStatus.Travelling&&state.Wars.Single().GoalDeclared,"Desktop reload preserves travelling peace terms and the war goal");
            Advance();Advance();CloseModal();
            Check(!state.Relation(0,1).War&&state.Cities.First(c=>c.Id==disputed.Id).Owner==0&&state.PeaceOffers.Single().Status==PeaceStatus.Accepted,"Turn handlers ratify the treaty and return the occupied city");
            tab="diplomacy";RefreshSide();await Capture("peace-ratified");
            Check(state.Wars.Single().Outcome.Contains("Cel wojenny osiągnięty"),"Diplomatic city recovery fulfills the declared liberation goal");
            state.Turn=5;Narrative.Schedule(state);ShowCouncil();await Capture("peace-aftermath");Click("01   Odbudujemy");
            Check(state.SeenEvents.Contains("peace_legacy")&&state.Deferred.Any(e=>e.Text.Contains("Fundusz odbudowy")),"Peace aftermath dialogue funds reconstruction and stores its delayed consequence");
            var bargaining=World.Create();Narrative.Resolve(bargaining,0);bargaining.Player.Gold=3000;bargaining.Factions[1].Gold=1000;Engine.DeclareWar(bargaining,1,WarReason.Conquest);bargaining.Turn=2;
            Start(bargaining);tab="diplomacy";RefreshSide();Click("Negocjuj pokój: "+state.Factions[1].Name);
            root.UpdateLayout();Descendants<ComboBox>(overlay!).First().SelectedIndex=0;Click("Wyślij warunki pokoju");Advance();Advance();CloseModal();
            Check(state.PeaceOffers.Count==2&&state.PeaceOffers[0].Status==PeaceStatus.Rejected&&state.PeaceOffers[1].From==1,"An excessive peace demand receives an AI counteroffer");
            for(int i=0;i<2;i++)
            {
                if(state.PendingEvent!=null){ShowCouncil();var e=Narrative.Get(state.PendingEvent);int option=Array.FindIndex(e.Choices,c=>state.Player.Gold+c.Gold>=0);Click($"{option+1:00}   ");}
                Advance();
            }
            CloseModal();tab="diplomacy";RefreshSide();Click("Przeczytaj warunki: "+state.Factions[1].Name);await Capture("peace-counteroffer");
            Check(state.PeaceOffers[1].Status==PeaceStatus.AwaitingPlayer&&state.Relation(0,1).War,"AI counteroffer waits for player consent");
            Click("Odpowiem później");string counterPath=Path.Combine(folder,"counteroffer-save.crown");SaveGame.Write(state,counterPath);LoadPath(counterPath);
            tab="diplomacy";RefreshSide();Click("Przeczytaj warunki: "+state.Factions[1].Name);Click("Odrzuć warunki");
            Check(state.Relation(0,1).War&&state.PeaceOffers[1].Status==PeaceStatus.Rejected,"Rejecting the incoming counteroffer keeps the war active");
            LoadPath(counterPath);tab="diplomacy";RefreshSide();Click("Przeczytaj warunki: "+state.Factions[1].Name);Click("Przyjmij warunki");
            Check(!state.Relation(0,1).War&&state.PeaceOffers[1].Status==PeaceStatus.Accepted,"Accepting the saved counteroffer ratifies peace through the WPF handler");
            string finalPath=Path.Combine(folder,"negotiated-peace-save.crown");SaveGame.Write(state,finalPath);string finalBefore=SaveGame.Serialize(state);LoadPath(finalPath);
            Check(SaveGame.Serialize(state)==finalBefore,"Desktop reload restores ratified treaties and completed war history");tab="diplomacy";RefreshSide();await Capture("peace-final");
            var commerce=World.Create();Narrative.Resolve(commerce,0);commerce.Player.Gold=4000;commerce.Factions[1].Gold=3000;
            foreach(var tile in commerce.Tiles)tile.Resource=Resource.None;
            foreach(var f in commerce.Factions)f.Technologies.UnionWith(["pottery","trade","currency"]);
            var marketHome=commerce.Cities.First(c=>c.Owner==0);var marketForeign=commerce.Cities.First(c=>c.Owner==1);marketHome.Buildings.Add("market");marketForeign.Buildings.Add("market");
            int marketDestination=20*commerce.Width+13;commerce.Tiles[marketDestination].Terrain=Terrain.Grassland;
            var marketCursor=commerce.Tiles[marketHome.Tile];
            while(marketCursor.Id!=marketDestination){marketCursor=World.Neighbors(commerce,marketCursor.Id).OrderBy(t=>World.Distance(t,commerce.Tiles[marketDestination])).First();if(!marketCursor.Passable)marketCursor.Terrain=Terrain.Grassland;marketCursor.Explored=true;}
            var marketTown=Engine.AddCity(commerce,0,marketDestination,"Brzegowo");marketTown.Buildings.Add("market");Engine.Explore(commerce);Engine.ConnectCity(commerce,marketTown.Id);
            marketHome.MarketStocks[TradeGood.Iron]=20;marketForeign.MarketStocks[TradeGood.Salt]=25;commerce.Relation(0,1).Trade=true;
            var commerceRoute=Caravans.Route(commerce,new(){Buyer=0,Seller=1,BuyerCity=marketHome.Id,SellerCity=marketForeign.Id});
            foreach(int id in commerceRoute){commerce.Tiles[id].Road=true;commerce.Tiles[id].RoadOwner=0;commerce.Tiles[id].Explored=true;}
            commerce.InvalidateLogistics();Start(commerce);tab="markets";RefreshSide();map.Center(marketHome.Tile);
            Check(Markets.Plan(state).Shipments.Any(p=>p.Good==TradeGood.Iron&&p.To==marketTown.Id),"Market panel projects a real domestic iron shipment");await Capture("markets");
            Click("Podpisz kontrakt handlowy");root.UpdateLayout();var marketInputs=Descendants<ComboBox>(overlay!).ToArray();marketInputs[3].SelectedIndex=1;marketInputs[4].SelectedIndex=3;
            await Capture("trade-contract");Click("Podpisz umowę");
            Check(state.TradeContracts.Single().Good==TradeGood.Salt&&state.TradeContracts.Single().Quantity==4&&state.TradeContracts.Single().Buyer==0,"Trade form signs the selected foreign salt contract");
            Advance();CloseModal();Check(state.TradeContracts.Single().LastDelivered>0&&marketHome.LastMarketTax>0,"Turn handler dispatches paid cargo and records local market taxation");
            Check(state.Caravans.Count==1&&marketHome.LastMarketLines.First(l=>l.Good==TradeGood.Salt).ForeignIn==0,"Dispatch creates a travelling caravan without instant import");
            tab="markets";RefreshSide();Click("Karawany i trasy");await Capture("caravans");Click("Pokaż na mapie #1");
            Check(map.TrackedCaravanId==1&&overlay==null,"Caravan report tracks a selected manifest on the map");await Capture("caravan-route");
            Click("Zakończ śledzenie trasy");Check(map.TrackedCaravanId==-1,"Map route tracking can be cleared without changing transport orders");
            int departedTile=state.Caravans[0].Tile;Advance();CloseModal();Check(state.Caravans[0].Tile!=departedTile&&Caravans.Active(state.Caravans[0]),"Turn handler moves the caravan along the road without teleporting");
            string transitPath=Path.Combine(folder,"caravan-save.crown");SaveGame.Write(state,transitPath);string transitBefore=SaveGame.Serialize(state);LoadPath(transitPath);CloseModal();
            Check(SaveGame.Serialize(state)==transitBefore,"Desktop reload preserves a prepaid caravan during travel");marketHome=state.Cities.First(c=>c.Owner==0);marketForeign=state.Cities.First(c=>c.Owner==1);
            for(int i=0;i<9&&Caravans.Active(state.Caravans[0]);i++)
            {
                if(state.PendingEvent!=null){ShowCouncil();var e=Narrative.Get(state.PendingEvent);int option=Array.FindIndex(e.Choices,c=>state.Player.Gold+c.Gold>=0);Click($"{option+1:00}   ");}
                Advance();CloseModal();
            }
            Check(state.Caravans[0].Status==CaravanStatus.Delivered,"Turn handler completes the road journey");
            tab="markets";RefreshSide();Click("Bilans towarów: "+marketHome.Name);await Capture("commodity-ledger");
            Check(marketHome.LastMarketLines.First(l=>l.Good==TradeGood.Salt).ForeignIn>0,"Commodity ledger preserves the actual international cargo");
            Click("Wybierz budowę");Click("Skład towarowy");Check(marketHome.Project=="warehouse","Warehouse project can be selected from the market report");
            tab="markets";RefreshSide();Click("Zmień politykę handlową");await Capture("trade-policy");Click("Przyjmij: Embargo");
            Check(state.Player.TradePolicy==TradePolicy.Embargo&&state.Player.LastTradePolicyTurn==state.Turn,"Trade policy form applies and locks an embargo");
            Advance();CloseModal();Check(state.TradeContracts.Single().LastDelivered==0&&state.TradeContracts.Single().Status==ContractStatus.Active,"Embargo pauses the contract without deleting it");
            Check(state.Caravans.Any(c=>c.Status==CaravanStatus.Waiting&&c.BlockedTurns==1),"Embargo stops already dispatched cargo at its current position");Click("Karawany i trasy");await Capture("caravan-blockade");CloseModal();
            if(state.PendingEvent!=null){ShowCouncil();Click("01   ");}
            state.Turn=Math.Max(10,state.Player.LastTradePolicyTurn+8);Narrative.Schedule(state);Refresh();ShowCouncil();await Capture("market-council");Click("01   Najpierw");
            Check(state.Player.TradePolicy==TradePolicy.DomesticFirst&&state.SeenEvents.Contains("market_charter"),"Trade council installs the domestic reserve policy");
            Advance();CloseModal();Check(state.Caravans.Any(c=>Caravans.Active(c)&&c.BlockedTurns==0),"Restoring trade policy resumes a paused caravan");
            string marketPath=Path.Combine(folder,"market-save.crown");SaveGame.Write(state,marketPath);string marketBefore=SaveGame.Serialize(state);LoadPath(marketPath);
            Check(SaveGame.Serialize(state)==marketBefore,"Desktop reload preserves stocks, prices, trade history and policy");
            Check(state.TradeContracts.Single().Status==ContractStatus.Expired&&state.Caravans.Any(Caravans.Active),"Contract expiry preserves its prepaid cargo in transit");
            tab="markets";RefreshSide();Click("Podpisz kontrakt handlowy");Click("Podpisz umowę");
            Check(state.TradeContracts.Count==2&&state.TradeContracts[1].Status==ContractStatus.Active,"Trade form can open a new agreement after the old one expires");
            Click("Rozwiąż umowę #2");Check(state.TradeContracts[1].Status==ContractStatus.Cancelled&&state.Caravans.Any(Caravans.Active),"Contract cancellation button closes the new agreement without deleting earlier cargo");
            Width=1200;Height=800;Refresh();await Capture("market-compact");Check(endButton.IsVisible&&root.ActualWidth>=1100,"Compact layout keeps the market and turn controls usable");
            Width=1600;Height=980;Refresh();await Capture("markets-final");
            // Reuse the real travelling manifest, then process three blocked market phases to prepare a loss.
            var petitionWorld=SaveGame.Read(transitPath);petitionWorld.Player.TradePolicy=TradePolicy.Embargo;Engine.CancelTradeContract(petitionWorld,1);
            for(int i=0;i<3;i++){Markets.Tick(petitionWorld);petitionWorld.Turn++;petitionWorld.InvalidateLogistics();}
            petitionWorld.Player.TradePolicy=TradePolicy.DomesticFirst;Start(petitionWorld);tab="markets";RefreshSide();
            Click("Sprawy kupców i obietnice");Check(state.TradeCases.Count==1&&state.TradeCases[0].Status==TradeCaseStatus.Pending,"Market petition report exposes a real import loss");await Capture("trade-petitions");
            Click("Rozpatrz sprawę #1");var petitionCity=TradeCouncil.City(state,state.TradeCases[0]);
            Check(state.PendingTradeCaseId==1&&ProvincialEvents.Current(state).Title.Contains(petitionCity.Name),"Petition button opens the council for the affected city and manifest");await Capture("trade-loss-council");
            string petitionPath=Path.Combine(folder,"trade-petition-save.crown");SaveGame.Write(state,petitionPath);string petitionBefore=SaveGame.Serialize(state);LoadPath(petitionPath);
            Check(SaveGame.Serialize(state)==petitionBefore&&overlay!=null,"Desktop reload preserves an unanswered contextual petition");
            double compensationGold=state.Player.Gold,compensationStock=TradeCouncil.City(state,state.TradeCases[0]).MarketStocks[TradeGood.Salt];int compensation=(int)Math.Ceiling(state.Caravans[0].Payment);
            Click("01   Wypłacimy");Check(state.TradeCases[0].Status==TradeCaseStatus.Compensated&&state.Player.Gold==compensationGold-compensation&&TradeCouncil.City(state,state.TradeCases[0]).MarketStocks[TradeGood.Salt]==compensationStock,"Compensation button spends the actual loss without replacing goods");
            LoadPath(petitionPath);Click("03   Rada nie");Check(state.TradeCases[0].Status==TradeCaseStatus.Declined&&state.PendingTradeCaseId==null,"Refusal button resolves the local petition without creating a promise");
            LoadPath(petitionPath);Click("02   Dostarczymy");
            Check(state.TradeCases[0].Status==TradeCaseStatus.Promised&&state.TradeCases[0].DeadlineTurn==state.Turn+TradeCouncil.PromiseTurns,"Promise choice creates a dated replacement obligation");
            tab="markets";RefreshSide();Click("Sprawy kupców i obietnice");await Capture("trade-promise-active");CloseModal();
            string promisePath=Path.Combine(folder,"trade-promise-save.crown");SaveGame.Write(state,promisePath);
            Click("Podpisz kontrakt handlowy");root.UpdateLayout();var promiseInputs=Descendants<ComboBox>(overlay!).ToArray();promiseInputs[3].SelectedIndex=1;promiseInputs[4].SelectedIndex=3;Click("Podpisz umowę");Advance();CloseModal();
            Check(TradeCouncil.Progress(state,state.TradeCases[0])==0&&state.Caravans.Any(Caravans.Active),"Buying replacement cargo does not fulfill the promise before arrival");
            string movingPromisePath=Path.Combine(folder,"trade-promise-transit-save.crown");SaveGame.Write(state,movingPromisePath);string movingBefore=SaveGame.Serialize(state);LoadPath(movingPromisePath);CloseModal();
            Check(SaveGame.Serialize(state)==movingBefore,"Desktop reload preserves a promise and its replacement caravan in transit");
            void PromiseTurn()
            {
                if(state.PendingEvent!=null)
                {
                    ShowCouncil();var e=ProvincialEvents.Current(state);int option=Array.FindIndex(e.Choices,c=>state.Player.Gold+c.Gold>=0);Click($"{option+1:00}   ");
                }
                Advance();CloseModal();
            }
            for(int i=0;i<10&&state.TradeCases[0].Status==TradeCaseStatus.Promised;i++)PromiseTurn();
            Check(state.TradeCases[0].Status==TradeCaseStatus.Fulfilled&&TradeCouncil.Progress(state,state.TradeCases[0])==state.Caravans[0].Quantity,"Turn handlers fulfill the obligation only when replacement goods arrive");
            Check(state.TradeCases[0].CreditedCaravans.Count>0&&state.Chronicle.Any(c=>c.Category=="konsekwencja"&&c.Text.Contains("słowo dotrzymane")),"Fulfilled promise records the actual manifests and political consequence");
            string fulfilledPath=Path.Combine(folder,"trade-promise-fulfilled-save.crown");SaveGame.Write(state,fulfilledPath);
            tab="markets";RefreshSide();Click("Sprawy kupców i obietnice");await Capture("trade-promise-fulfilled");CloseModal();
            LoadPath(promisePath);CloseModal();for(int i=0;i<11&&state.TradeCases[0].Status==TradeCaseStatus.Promised;i++)PromiseTurn();
            Check(state.TradeCases[0].Status==TradeCaseStatus.Broken&&state.TradeCases[0].ResolvedTurn==state.TradeCases[0].DeadlineTurn,"Passing the deadline without replacement goods breaks the promise");
            tab="markets";RefreshSide();Click("Sprawy kupców i obietnice");await Capture("trade-promise-broken");
            Width=1200;Height=800;root.UpdateLayout();await Capture("trade-promise-compact");
            var returnControl=Descendants<Button>(overlay!).First(b=>b.Content?.ToString()=="Wróć do mapy");
            var returnBounds=returnControl.TransformToAncestor(root).TransformBounds(new Rect(returnControl.RenderSize));
            Check(returnBounds.Top>=0&&returnBounds.Bottom<=root.ActualHeight||Descendants<ScrollViewer>(overlay!).Any(v=>v.ScrollableHeight>0),"Compact promise report keeps its return control visible or reachable by scrolling");
            Width=1600;Height=980;LoadPath(fulfilledPath);CloseModal();tab="markets";RefreshSide();Click("Sprawy kupców i obietnice");await Capture("trade-promise-final");
            var escortWorld=World.Create();Narrative.Resolve(escortWorld,0);escortWorld.Units.Clear();
            foreach(var t in escortWorld.Tiles){t.Resource=Resource.None;t.Owner=-1;t.Ruin=false;t.Explored=true;}
            var escortSeller=escortWorld.Cities.First(c=>c.Owner==0);var escortBuyer=escortWorld.Cities.First(c=>c.Owner==1);escortBuyer.Tile=15*escortWorld.Width+21;
            foreach(var c in escortWorld.Cities){escortWorld.Tiles[c.Tile].Owner=c.Owner;escortWorld.Tiles[c.Tile].Terrain=Terrain.Grassland;}
            for(int x=9;x<=21;x++){var t=escortWorld.Tiles[15*escortWorld.Width+x];t.Terrain=Terrain.Grassland;t.Road=true;t.RoadOwner=0;}
            foreach(var f in escortWorld.Factions){f.Gold=3000;f.Technologies.UnionWith(["pottery","trade","currency"]);}
            escortSeller.Buildings.Add("market");escortBuyer.Buildings.Add("market");escortSeller.MarketStocks[TradeGood.Salt]=50;escortWorld.Relation(0,1).Trade=true;escortWorld.InvalidateLogistics();
            Engine.SignTradeContract(escortWorld,escortBuyer.Id,escortSeller.Id,TradeGood.Salt,4);Markets.Tick(escortWorld);Engine.CancelTradeContract(escortWorld,1);escortWorld.Turn++;
            var escortUnit=Engine.AddUnit(escortWorld,0,escortSeller.Tile,UnitKind.Spearmen);escortUnit.Health=60;int escortUnitId=escortUnit.Id;
            Start(escortWorld);tab="markets";RefreshSide();Click("Karawany i trasy");Click("Wyznacz eskortę #1");await Capture("escort-assignment");Click("Przydziel: ");
            Check(escortUnit.EscortCaravanId==1&&escortUnit.Destination==null&&escortUnit.Actions==2,"Escort assignment form binds an own colocated unit without spending movement");await Capture("escort-order");
            Click("Pokaż na mapie #1");Select(escortUnit.Tile);root.UpdateLayout();int escortStart=escortUnit.Tile;double escortActions=escortUnit.Actions;Order(escortStart+1);
            Check(escortUnit.Tile==escortStart&&escortUnit.Actions==escortActions&&escortUnit.EscortCaravanId==1,"A map march cannot interrupt a designated escort implicitly");await Capture("escort-map");
            Advance();CloseModal();Check(escortUnit.Tile==state.Caravans[0].Tile&&escortUnit.Tile==15*state.Width+13&&escortUnit.Health==60,"Turn handler moves slow escort and caravan together without passive healing");await Capture("escort-moving");
            string escortPath=Path.Combine(folder,"escort-save.crown");SaveGame.Write(state,escortPath);string escortBefore=SaveGame.Serialize(state);LoadPath(escortPath);CloseModal();escortUnit=state.Units.First(u=>u.Id==escortUnitId);
            Check(SaveGame.Serialize(state)==escortBefore&&escortUnit.EscortCaravanId==1,"Desktop reload preserves the joint position and persistent escort order");
            tab="markets";RefreshSide();Click("Karawany i trasy");double cancelActions=escortUnit.Actions;Click("Odwołaj eskortę #1");
            Check(escortUnit.EscortCaravanId==null&&escortUnit.Actions==cancelActions,"Caravan report cancels an escort without refunding movement");CloseModal();
            Select(escortUnit.Tile);Click("Eskortuj karawanę #1");Check(escortUnit.EscortCaravanId==1,"Unit panel can reassign a colocated caravan escort");
            Advance();CloseModal();Advance();CloseModal();
            Check(state.Caravans[0].Status==CaravanStatus.Delivered&&escortUnit.EscortCaravanId==null&&escortUnit.Tile==15*state.Width+20&&state.Tiles[escortUnit.Tile].Owner!=1,"Arrival releases the escort on the near side of the peaceful foreign city");
            tab="markets";RefreshSide();Click("Karawany i trasy");await Capture("escort-arrival");
            Width=1200;Height=800;root.UpdateLayout();await Capture("escort-compact");var escortReturn=Descendants<Button>(overlay!).First(b=>b.Content?.ToString()=="Wróć do mapy");
            var escortReturnBounds=escortReturn.TransformToAncestor(root).TransformBounds(new Rect(escortReturn.RenderSize));
            Check(escortReturnBounds.Top>=0&&escortReturnBounds.Bottom<=root.ActualHeight||Descendants<ScrollViewer>(overlay!).Any(v=>v.ScrollableHeight>0),"Compact caravan report keeps escort controls reachable");
            Width=1600;Height=980;LoadPath(escortPath);CloseModal();tab="markets";RefreshSide();Click("Karawany i trasy");await Capture("escort-final");
            // A single third-party gate closes after dispatch; use real embassy and turn handlers to reopen it.
            var borderWorld=SaveGame.Read(escortPath);
            foreach(var t in borderWorld.Tiles){t.Terrain=Terrain.Mountain;t.Owner=-1;t.Road=false;t.RoadOwner=-1;}
            foreach(var c in borderWorld.Cities){borderWorld.Tiles[c.Tile].Terrain=Terrain.Grassland;borderWorld.Tiles[c.Tile].Owner=c.Owner;}
            for(int x=9;x<=21;x++){var t=borderWorld.Tiles[15*borderWorld.Width+x];t.Terrain=Terrain.Grassland;t.Road=true;t.RoadOwner=0;}
            borderWorld.Tiles[15*borderWorld.Width+14].Owner=2;borderWorld.Relation(0,2).Trust=-10;borderWorld.InvalidateLogistics();
            string borderPath=Path.Combine(folder,"transit-closed-save.crown");SaveGame.Write(borderWorld,borderPath);
            Start(borderWorld);tab="markets";RefreshSide();Click("Karawany i trasy");
            Check(TransitCouncil.ClosedBorders(state,state.TradeContracts[0],state.Caravans[0].Tile).SequenceEqual(new[]{2}),"Closed caravan report identifies the specific third-party gate");await Capture("transit-blocked");
            Click("Granice i tranzyt #1");await Capture("transit-route");Click("Rozmowa o przejeździe: "+state.Factions[2].Name);
            root.UpdateLayout();
            Check(Descendants<Button>(overlay!).Count(b=>b.IsEnabled&&b.Content?.ToString()?.StartsWith("Wyślij: ")==true)==2&&DiplomaticCouncil.AcceptanceScore(state,2,DemandKind.PaidTransit)==25,"Gate dialogue offers distinct free and paid proposals with the actual acceptance forecast");await Capture("transit-negotiation");
            double envoyGold=state.Player.Gold,envoyHostGold=state.Factions[2].Gold;Click("Wyślij: "+DiplomaticCouncil.DemandName(DemandKind.PaidTransit));
            Check(state.Demands.Single().Status==DemandStatus.Pending&&state.Player.Gold==envoyGold-20&&state.Factions[2].Gold==envoyHostGold&&TransitCouncil.Until(state,2)==0,"Paid proposal spends only embassy costs before consent");
            string envoyPath=Path.Combine(folder,"transit-pending-save.crown");SaveGame.Write(state,envoyPath);string envoyBefore=SaveGame.Serialize(state);LoadPath(envoyPath);CloseModal();
            Check(SaveGame.Serialize(state)==envoyBefore,"Desktop reload preserves a travelling transit proposal without early access");
            PromiseTurn();Check(state.Caravans[0].BlockedTurns==1&&state.Demands[0].Status==DemandStatus.Pending,"The first blocked phase does not pause while envoys travel");
            PromiseTurn();Check(state.Caravans[0].BlockedTurns==2&&state.Demands[0].Status==DemandStatus.Accepted&&TransitCouncil.Until(state,2)==state.Turn+12,"Reply arrives after two turns and reopens the corridor before the third stop");
            tab="diplomacy";RefreshSide();Click("Przejazd kupców: "+state.Factions[2].Name);await Capture("transit-accepted");
            Check(Descendants<Button>(overlay!).Where(b=>b.Content?.ToString()?.StartsWith("Wyślij: ")==true).All(b=>!b.IsEnabled),"Active permission prevents duplicate negotiation through the dialogue");CloseModal();
            string consentPath=Path.Combine(folder,"transit-accepted-save.crown");SaveGame.Write(state,consentPath);string consentBefore=SaveGame.Serialize(state);LoadPath(consentPath);CloseModal();
            Check(SaveGame.Serialize(state)==consentBefore&&TransitCouncil.Until(state,2)>state.Turn,"Desktop reload retains the accepted right and exclusive expiry date");
            int gateGuardTile=state.Units.First(u=>u.Id==escortUnitId).Tile;PromiseTurn();var gateGuard=state.Units.First(u=>u.Id==escortUnitId);
            Check(state.Caravans[0].Tile==15*state.Width+19&&state.Caravans[0].BlockedTurns==0&&gateGuard.Tile==gateGuardTile&&gateGuard.EscortCaravanId==null,"Market resumes prepaid cargo through the permitted gate while the escort remains outside");
            tab="markets";RefreshSide();Click("Karawany i trasy");await Capture("transit-resumed");CloseModal();PromiseTurn();
            Check(state.Caravans[0].Status==CaravanStatus.Delivered&&state.Caravans.Count==1&&state.TradeContracts[0].Status==ContractStatus.Cancelled,"Transit completes a prepaid manifest without reviving its cancelled contract");
            LoadPath(borderPath);CloseModal();tab="diplomacy";RefreshSide();Click("Przejazd kupców: "+state.Factions[2].Name);Click("Wyślij: "+DiplomaticCouncil.DemandName(DemandKind.Transit));PromiseTurn();PromiseTurn();
            Check(state.Demands[0].Status==DemandStatus.Rejected&&TransitCouncil.Until(state,2)==0&&state.Caravans[0].BlockedTurns==2,"Unpaid proposal can be refused and leaves the caravan waiting");
            tab="diplomacy";RefreshSide();Click("Przejazd kupców: "+state.Factions[2].Name);await Capture("transit-refused");
            LoadPath(consentPath);CloseModal();state.Turn=TransitCouncil.Until(state,2);state.InvalidateLogistics();
            Check(Caravans.Route(state,state.TradeContracts[0],state.Caravans[0].Tile).Count==0,"Permit expiry closes the route before that turn's market phase");PromiseTurn();
            Check(state.Caravans[0].Status==CaravanStatus.Lost,"An expired right does not protect a manifest at its third consecutive stop");
            tab="diplomacy";RefreshSide();Click("Przejazd kupców: "+state.Factions[2].Name);await Capture("transit-expired");
            Width=1200;Height=800;root.UpdateLayout();await Capture("transit-compact");var transitReturn=Descendants<Button>(overlay!).First(b=>b.Content?.ToString()=="Wróć do mapy");
            var transitBounds=transitReturn.TransformToAncestor(root).TransformBounds(new Rect(transitReturn.RenderSize));
            Check(transitBounds.Top>=0&&transitBounds.Bottom<=root.ActualHeight||Descendants<ScrollViewer>(overlay!).Any(v=>v.ScrollableHeight>0),"Compact transit dialogue keeps its return control visible or reachable by scrolling");
            Width=1600;Height=980;LoadPath(consentPath);CloseModal();tab="diplomacy";RefreshSide();Click("Przejazd kupców: "+state.Factions[2].Name);await Capture("transit-final");
            checks.Add($"{checks.Count} desktop integration checks passed.");File.WriteAllLines(Path.Combine(folder,"ui-smoke.txt"),checks);Close();
        }
        catch(Exception e){checks.Add(e.ToString());File.WriteAllLines(Path.Combine(folder,"ui-smoke.txt"),checks);Application.Current.Shutdown(3);}
    }
}
