using Crown.Core;
using Catalog = Crown.Core.Content;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Crown.Desktop;

public sealed partial class MainWindow : Window
{
    private GameState state=null!;
    private readonly Grid root=new();
    private Grid shell=null!,header=null!;
    private StackPanel side=null!;
    private Border sideHost=null!;
    private MapView map=null!;
    private MiniMap mini=null!;
    private TextBlock status=null!;
    private Button endButton=null!;
    private string tab="realm";
    private int selectedTile=-1,selectedUnit=-1;
    private UIElement? overlay;
    private readonly string saveFolder=Path.Combine(AppContext.BaseDirectory,"saves");
    private string AutoPath=>Path.Combine(saveFolder,"autosave.crown");
    private readonly string[] arguments;
    private bool busy;
    public MainWindow(string[] args)
    {
        arguments=args;Title="Korony i Pogranicza";Width=1600;Height=980;MinWidth=1200;MinHeight=760;WindowStartupLocation=WindowStartupLocation.CenterScreen;Background=Ui.Ink;Foreground=Ui.Text;FontFamily=new FontFamily("Segoe UI");Content=root;
        TextOptions.SetTextFormattingMode(this,TextFormattingMode.Display);
        KeyDown+=OnKey;Closing+=(o,e)=>{if(state!=null)TryAutoSave(false);};
        Loaded+=(o,e)=>
        {
            if(args.Contains("--ui-smoke-photographic")){Dispatcher.BeginInvoke(async()=>await PhotographicSmoke(args.Last()),DispatcherPriority.ApplicationIdle);return;}
            if(args.Contains("--ui-smoke-3d")){Dispatcher.BeginInvoke(async()=>await ThreeDimensionalSmoke(args.Last()),DispatcherPriority.ApplicationIdle);return;}
            if(args.Contains("--ui-smoke-energy-cases")){Dispatcher.BeginInvoke(async()=>await EnergyCaseSmoke(args.Last()),DispatcherPriority.ApplicationIdle);return;}
            if(args.Contains("--ui-smoke-power")){Dispatcher.BeginInvoke(async()=>await PowerSmoke(args.Last()),DispatcherPriority.ApplicationIdle);return;}
            if(args.Contains("--ui-smoke-epochs")){Dispatcher.BeginInvoke(async()=>await EpochSmoke(args.Last()),DispatcherPriority.ApplicationIdle);return;}
            if(args.Contains("--ui-smoke-custody")){Dispatcher.BeginInvoke(async()=>await CustodySmoke(args.Last()),DispatcherPriority.ApplicationIdle);return;}
            if(args.Contains("--ui-smoke-espionage")){Dispatcher.BeginInvoke(async()=>await SpySmoke(args.Last()),DispatcherPriority.ApplicationIdle);return;}
            if(args.Contains("--ui-smoke")){Dispatcher.BeginInvoke(async()=>await Smoke(args.Last()),DispatcherPriority.ApplicationIdle);return;}
            if(args.Contains("--render"))
            {
                var s=World.Create();Narrative.Resolve(s,0);Start(s);if(args.Contains("--council")){state.PendingEvent="founding";ShowCouncil();}
                Dispatcher.BeginInvoke(async()=>{await Task.Delay(400);var png=new RenderTargetBitmap((int)root.ActualWidth,(int)root.ActualHeight,96,96,PixelFormats.Pbgra32);png.Render(root);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(png));string path=args.Last();using var output=File.Create(path);encoder.Save(output);Close();},DispatcherPriority.ApplicationIdle);
            }
            else if(args.Contains("--render-menu"))
            {
                Landing();Dispatcher.BeginInvoke(async()=>{await Task.Delay(400);var png=new RenderTargetBitmap((int)root.ActualWidth,(int)root.ActualHeight,96,96,PixelFormats.Pbgra32);png.Render(root);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(png));using var output=File.Create(args.Last());encoder.Save(output);Close();},DispatcherPriority.ApplicationIdle);
            }
            else Landing();
        };
    }
    private static ImageBrush Artwork()=>new(new BitmapImage(new Uri("pack://application:,,,/Assets/valley.png"))){Stretch=Stretch.UniformToFill,AlignmentX=AlignmentX.Center};
    private void Landing()
    {
        root.Children.Clear();overlay=null;var scene=new Grid{Background=Artwork()};root.Children.Add(scene);
        scene.Children.Add(new Border{Background=new LinearGradientBrush(new GradientStopCollection{new(Color.FromArgb(248,9,24,30),0),new(Color.FromArgb(210,9,24,30),.35),new(Color.FromArgb(15,9,24,30),.8)},0)});
        scene.Children.Add(new Border{Background=new LinearGradientBrush(Colors.Transparent,Color.FromArgb(225,7,19,25),90)});
        var p=new StackPanel{Width=500,HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(84,10,0,0)};scene.Children.Add(p);
        p.Children.Add(Ui.Eyebrow("AUTORSKA STRATEGIA TUROWA  /  0.21"));Ui.Gap(p,14);
        p.Children.Add(Ui.Label("Korony\ni Pogranicza",59,Ui.Text,true));Ui.Gap(p,18);
        p.Children.Add(Ui.Label("Każde państwo ma początek.\nKażda decyzja ma swoją przyszłość.",21,Ui.Gold,true));Ui.Gap(p,14);
        p.Children.Add(Ui.Label("Poprowadź Marchie Veyru przez stulecia. Buduj miasta, negocjuj z sąsiadami i słuchaj tych, którzy ponoszą ciężar Twoich wyborów.",14,Ui.Brush("#BFCDC9")));Ui.Gap(p,25);
        var row=new Grid();row.ColumnDefinitions.Add(new(){Width=new GridLength(155)});row.ColumnDefinitions.Add(new(){Width=new GridLength(20)});row.ColumnDefinitions.Add(new());
        var seed=new TextBox{Text="17421",Background=Ui.Panel,Foreground=Ui.Text,BorderBrush=Ui.Line,Padding=new Thickness(10),FontSize=13};
        var seedPanel=Ui.Stack(Ui.Label("Ziarno świata",11,Ui.Muted),new Border{Height=6},seed);row.Children.Add(seedPanel);
        var difficulty=Ui.Combo(["Spokojny","Standard","Wymagający"],1);difficulty.Margin=new Thickness(0);var dp=Ui.Stack(Ui.Label("Poziom wyzwania",11,Ui.Muted),new Border{Height=6},difficulty);Grid.SetColumn(dp,2);row.Children.Add(dp);p.Children.Add(row);Ui.Gap(p,16);
        p.Children.Add(Ui.Button("ROZPOCZNIJ NOWĄ HISTORIĘ   →",()=>{if(!int.TryParse(seed.Text,out int value)){MessageBox.Show("Ziarno musi być liczbą całkowitą.");return;}Start(World.Create(value,difficulty.SelectedItem?.ToString()??"Standard"));ShowCouncil();},true));
        if(File.Exists(AutoPath))p.Children.Add(Ui.Button("Kontynuuj ostatnią kampanię",()=>LoadPath(AutoPath)));
        p.Children.Add(Ui.Button("Wczytaj zapisaną historię",Load));Ui.Gap(p,12);
        p.Children.Add(Ui.Label("Kampania: do 1600 tur • 4 państwa • zapis lokalny\nNauka, kultura, zjednoczenie lub dorobek epoki",11,Ui.Muted));
        var note=Ui.Stack(Ui.Eyebrow("DOLINA VEYRU"),Ui.Label("Ziemia, której historia\nczeka na Twój głos.",25,Ui.Text,true));note.HorizontalAlignment=HorizontalAlignment.Right;note.VerticalAlignment=VerticalAlignment.Bottom;note.Margin=new Thickness(0,0,70,70);scene.Children.Add(note);
    }
    private void Start(GameState s)
    {
        state=s;tab="realm";selectedUnit=-1;selectedTile=s.Cities.FirstOrDefault(c=>c.Owner==0)?.Tile??s.Cities.FirstOrDefault()?.Tile??0;root.Children.Clear();overlay=null;
        shell=new Grid();shell.RowDefinitions.Add(new(){Height=new GridLength(82)});shell.RowDefinitions.Add(new());shell.RowDefinitions.Add(new(){Height=new GridLength(101)});shell.ColumnDefinitions.Add(new(){Width=new GridLength(83)});shell.ColumnDefinitions.Add(new());shell.ColumnDefinitions.Add(new(){Width=new GridLength(336)});root.Children.Add(shell);
        header=new Grid{Background=Ui.Ink};Grid.SetColumnSpan(header,3);shell.Children.Add(header);
        BuildNav();map=new MapView();map.Reset(s);map.SelectedTile=selectedTile;map.SelectTile=Select;map.OrderUnit=Order;map.CameraChanged=()=>mini?.InvalidateVisual();Grid.SetColumn(map,1);Grid.SetRow(map,1);shell.Children.Add(map);
        var mapInfo=new StackPanel{IsHitTestVisible=false,VerticalAlignment=VerticalAlignment.Top,HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(25,23,0,0)};
        mapInfo.Children.Add(Ui.Eyebrow("DOLINA VEYRU"));mapInfo.Children.Add(Ui.Label("Ziemia obiecana",27,Ui.Text,true));mapInfo.Children.Add(Ui.Label("TEREN 3D · LPM: przesuń · PPM: obróć · Kółko: zoom",11,Ui.Muted));Grid.SetColumn(mapInfo,1);Grid.SetRow(mapInfo,1);shell.Children.Add(mapInfo);
        var toggles=new StackPanel{Orientation=Orientation.Horizontal,VerticalAlignment=VerticalAlignment.Top,HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(0,20,20,0)};
        var grid=Ui.Button("Siatka",()=>{map.ShowGrid=!map.ShowGrid;map.InvalidateVisual();});grid.Margin=new Thickness(0,0,6,0);toggles.Children.Add(grid);toggles.Children.Add(Ui.Button("Zasoby",()=>{map.ShowResources=!map.ShowResources;map.InvalidateVisual();}));toggles.Children.Add(Ui.Button("Dostawy",()=>{map.ShowSupply=!map.ShowSupply;if(map.ShowSupply)map.ShowPopulation=false;map.InvalidateVisual();if(tab=="logistics")RefreshSide();}));toggles.Children.Add(Ui.Button("Ludność",()=>{map.ShowPopulation=!map.ShowPopulation;if(map.ShowPopulation)map.ShowSupply=false;map.InvalidateVisual();if(tab=="provinces")RefreshSide();}));Grid.SetColumn(toggles,1);Grid.SetRow(toggles,1);shell.Children.Add(toggles);
        sideHost=new Border{Background=Ui.Panel,BorderBrush=Ui.Line,BorderThickness=new Thickness(1,0,0,0)};Grid.SetColumn(sideHost,2);Grid.SetRow(sideHost,1);shell.Children.Add(sideHost);
        BuildFooter();Refresh();root.UpdateLayout();map.Center(selectedTile);
        if(s.PendingEvent!=null)Toast("Rada oczekuje Twojej odpowiedzi. Otwórz salę rady.");
        else if(s.PeaceOffers.Any(o=>o.Status==PeaceStatus.AwaitingPlayer)){tab="diplomacy";RefreshSide();Toast("Posłowie czekają na odpowiedź na warunki pokoju.");}
        if(s.Ending!=null)ShowEnding();
    }
    private void BuildNav()
    {
        var nav=new DockPanel{Background=Ui.Ink,LastChildFill=true};Grid.SetRow(nav,1);shell.Children.Add(nav);
        var bottom=Ui.Button("☰\nMenu",()=>{TryAutoSave(false);Landing();});bottom.FontSize=11;bottom.Margin=new Thickness(9,0,9,16);DockPanel.SetDock(bottom,Dock.Bottom);nav.Children.Add(bottom);
        var p=new StackPanel{Margin=new Thickness(9,18,9,0)};nav.Children.Add(new ScrollViewer{Content=p,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled});
        (string Symbol,string Name,string Tab)[] entries=[("♜","Państwo","realm"),("♙","Ludność","provinces"),("¤","Rynek","markets"),("↝","Szlaki","logistics"),("⌘","Badania","research"),("⚑","Dyplom.","diplomacy"),("⚖","Polityka","policy"),("▤","Kronika","history"),("◇","Dorobek","victory"),("?","Poradnik","guide")];
        foreach(var e in entries)
        {
            var content=Ui.Stack(Ui.Label(e.Symbol,24,Ui.Gold),Ui.Label(e.Name,10,Ui.Muted));foreach(TextBlock child in content.Children){child.HorizontalAlignment=HorizontalAlignment.Center;child.TextWrapping=TextWrapping.NoWrap;}
            var b=Ui.Button("",()=>{tab=e.Tab;RefreshSide();});b.Content=content;b.Padding=new Thickness(2,6,2,6);b.Margin=new Thickness(0,0,0,9);b.ToolTip=e.Name;p.Children.Add(b);
        }
    }
    private void BuildFooter()
    {
        var footer=new Grid{Background=Ui.Ink};footer.ColumnDefinitions.Add(new(){Width=new GridLength(300)});footer.ColumnDefinitions.Add(new());footer.ColumnDefinitions.Add(new(){Width=new GridLength(220)});Grid.SetRow(footer,2);Grid.SetColumnSpan(footer,3);shell.Children.Add(footer);
        var left=Ui.Stack(Ui.Label("TWOJA HISTORIA",10,Ui.Gold),Ui.Label("Decyzje przetrwają pokolenia.",17,Ui.Text,true));left.Margin=new Thickness(24,22,10,0);footer.Children.Add(left);
        var mid=new StackPanel{Margin=new Thickness(0,12,15,0)};Grid.SetColumn(mid,1);footer.Children.Add(mid);
        var buttons=new StackPanel{Orientation=Orientation.Horizontal};mid.Children.Add(buttons);
        foreach(var pair in new (string,Action)[]{("Sala rady",ShowCouncil),("Zapisz  F5",Save),("Wczytaj  F9",Load),("Następny oddział  Tab",NextUnit)}){var b=Ui.Button(pair.Item1,pair.Item2);b.Margin=new Thickness(0,0,7,0);buttons.Children.Add(b);}
        status=Ui.Label("LPM: wybór / przesunięcie · PPM: rozkaz / przeciągnij, aby obrócić · Q/E: obrót · Kółko: zoom",11,Ui.Muted);status.Margin=new Thickness(0,8,0,0);mid.Children.Add(status);
        endButton=Ui.Button("ZAKOŃCZ TURĘ   →",Advance,true);endButton.Margin=new Thickness(10,23,24,23);endButton.HorizontalContentAlignment=HorizontalAlignment.Center;Grid.SetColumn(endButton,2);footer.Children.Add(endButton);
    }
    private void Refresh()
    {
        RefreshHeader();RefreshSide();map.SelectedTile=selectedTile;map.SelectedUnit=selectedUnit;map.InvalidateVisual();endButton.IsEnabled=!busy&&state.Ending==null;endButton.Content=state.PendingEvent!=null?"DECYZJA RADY   →":"ZAKOŃCZ TURĘ   →";
    }
    private void RefreshHeader()
    {
        header.Children.Clear();header.ColumnDefinitions.Clear();header.ColumnDefinitions.Add(new(){Width=new GridLength(255)});header.ColumnDefinitions.Add(new());header.ColumnDefinitions.Add(new(){Width=new GridLength(225)});
        var word=Ui.Stack(Ui.Label("KORONY",23,Ui.Text,true),Ui.Label("I   P O G R A N I C Z A",9,Ui.Gold));word.Margin=new Thickness(26,10,0,0);header.Children.Add(word);
        var inc=Engine.Income(state,0);var stats=new UniformGridEx{Columns=5,Margin=new Thickness(0,12,15,8)};Grid.SetColumn(stats,1);header.Children.Add(stats);
        void Stat(string title,string value,string suffix,Brush color){var p=Ui.Stack(Ui.Label(title.ToUpperInvariant(),9,Ui.Muted),Ui.Label(value,21,color),Ui.Label(suffix,10,Ui.Muted));p.Margin=new Thickness(15,0,0,0);stats.Children.Add(p);}
        Stat("Skarb",Ui.Number(state.Player.Gold),$"{Ui.Signed(inc.Gold+(state.TradeContracts.Any(c=>c.Status==ContractStatus.Active&&(c.Buyer==0||c.Seller==0))?Markets.Plan(state).Cash[0]:0))} / turę",Ui.Gold);Stat("Nauka",Ui.Number(state.Player.Science),$"{Ui.Signed(inc.Science)} / turę",Ui.Brush("#91BFCD"));Stat("Kultura",Ui.Number(state.Player.Culture),$"{Ui.Signed(inc.Culture)} / turę",Ui.Brush("#BCA6CE"));Stat("Stabilność",state.Player.Stability+"%",$"cel: {Engine.StabilityTarget(state,state.Player)}%",Ui.Green);Stat("Prestiż",Ui.Number(state.Player.Prestige),Catalog.Era(state.Player.Era),Ui.Gold);
        string year=state.Year<0?$"{-state.Year} p.n.e.":$"{state.Year} n.e.";var date=Ui.Stack(Ui.Label($"TURA {state.Turn:000}  /  {state.MaxTurns}",10,Ui.Gold),Ui.Label(year,22,Ui.Text,true),Ui.Label($"{state.Season} · {state.Difficulty}",10,Ui.Muted));date.Margin=new Thickness(12,10,20,0);Grid.SetColumn(date,2);header.Children.Add(date);
    }
    private void RefreshSide()
    {
        side=new StackPanel{Margin=new Thickness(22,22,22,24)};sideHost.Child=new ScrollViewer{Content=side,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled};
        switch(tab){case "realm":RealmPanel();break;case "provinces":ProvincesPanel();break;case "markets":MarketsPanel();break;case "logistics":LogisticsPanel();break;case "research":ResearchPanel();break;case "diplomacy":DiplomacyPanel();break;case "policy":PolicyPanel();break;case "history":HistoryPanel();break;case "victory":VictoryPanel();break;default:GuidePanel();break;}
    }
    private void TitlePanel(string eyebrow,string title,string? body=null)
    {
        side.Children.Add(Ui.Eyebrow(eyebrow));side.Children.Add(Ui.Label(title,28,Ui.Text,true));if(body!=null){Ui.Gap(side,8);side.Children.Add(Ui.Label(body,12,Ui.Muted));}side.Children.Add(Ui.Rule());
    }
    private void ResearchPanel()
    {
        TitlePanel("WIEDZA I ODKRYCIA","Horyzonty myśli","Punkty nauki są wspólną pulą. Zmiana badań nie usuwa postępu.");var f=state.Player;
        side.Children.Add(Ui.Button("Epoki i rozwój świata",()=>ShowEpochs(),true));side.Children.Add(Ui.Button("Energia, klimat i kosmos",ShowWorldDevelopment));
        if(f.Research!=""){var t=Catalog.Tech(f.Research);Ui.Bar(side,$"{t.Name}: {f.Science:0} / {t.Cost}",f.Science,t.Cost,Ui.Brush("#91BFCD"));}
        foreach(var t in Catalog.Technologies.Where(t=>!f.Technologies.Contains(t.Id)&&(state.EpochCampaign?EpochCatalog.EraOf(t.Id)<=f.Era+1:Catalog.LegacyTechnologies.Contains(t.Id))))
        {
            bool available=Epochs.ResearchError(state,f,t)==null;var p=Ui.Stack(Ui.Label(t.Name,18,available?Ui.Text:Ui.Muted,true),Ui.Label($"{t.Cost} nauki · {t.Description}",12,Ui.Muted));
            if(available){Ui.Gap(p,10);p.Children.Add(Ui.Button(f.Research==t.Id?"Aktualne badania":"Rozpocznij badania",()=>Do(Engine.SetResearch(state,t.Id)),f.Research==t.Id));}
            else{Ui.Gap(p,8);p.Children.Add(Ui.Label(Epochs.ResearchError(state,f,t)??"",10,Ui.Gold));}
            side.Children.Add(Ui.Card(p));
        }
        side.Children.Add(Ui.Label("Odkryte: "+(f.Technologies.Count==0?"jeszcze żadne":string.Join(", ",f.Technologies.Select(id=>Catalog.Tech(id).Name))),11,Ui.Green));
    }
    private void PolicyPanel()
    {
        var f=state.Player;TitlePanel("RADA MARCHII","Kształt państwa","Reforma kosztuje 20 złota. Jedna zmiana na turę; ustawy działają do odwołania.");
        Ui.Bar(side,$"Lud · {f.Commons}%",f.Commons,100,Ui.Green);Ui.Bar(side,$"Rody · {f.Nobles}%",f.Nobles,100,Ui.Gold);Ui.Bar(side,$"Uczeni · {f.Scholars}%",f.Scholars,100,Ui.Brush("#91BFCD"));
        side.Children.Add(Ui.Label("Podatki",13,Ui.Text));var tax=Ui.Combo(["Niskie · 65% dochodu, większe poparcie","Umiarkowane · pełny dochód","Wysokie · 150% dochodu, niepokoje"],f.Tax);side.Children.Add(tax);
        side.Children.Add(Ui.Label("Organizacja władzy",13));var gov=Ui.Combo(["Rada pogranicza · równowaga","Zgromadzenie · +15% złota, +10% kultury","Władza rodów · +20% produkcji, -15% kultury"],f.Government);side.Children.Add(gov);
        side.Children.Add(Ui.Label("Priorytet państwa",13));var focus=Ui.Combo(["Zrównoważony rozwój","Wiedza · +30% nauki, -15% produkcji","Rozbudowa · +25% produkcji, -15% nauki"],f.Focus);side.Children.Add(focus);
        side.Children.Add(Ui.Button("Przyjmij reformę · 20 złota",()=>Do(Engine.SetPolicy(state,tax.SelectedIndex,gov.SelectedIndex,focus.SelectedIndex)),true));
        side.Children.Add(Ui.Rule());side.Children.Add(Ui.Label($"Wyczerpanie wojną: {f.WarWeariness:0}%\nDocelowa stabilność: {Engine.StabilityTarget(state,f)}%\nStabilność zmierza do celu o 1 punkt na turę. Poniżej 25% rosną niepokoje i spadają wpływy.",12,Ui.Muted));
        if(f.Flags.Count>0){Ui.Gap(side,12);side.Children.Add(Ui.Eyebrow("DZIEDZICTWO WYBORÓW"));foreach(var entry in state.Chronicle.Where(e=>e.Category=="decyzja").TakeLast(4))side.Children.Add(Ui.Label($"T{entry.Turn} · {entry.Text.Split('—')[0]}",12,Ui.Gold));}
    }
    private void HistoryPanel()
    {
        TitlePanel("ZAPIS POKOLEŃ","Kronika Marchii",$"{state.Chronicle.Count} zapisanych wydarzeń. Oczekujące następstwa decyzji: {state.Deferred.Count}.");
        foreach(var e in state.Chronicle.AsEnumerable().Reverse().Take(150))side.Children.Add(Ui.Card(Ui.Stack(Ui.Eyebrow($"TURA {e.Turn} / {e.Category}"),Ui.Label(e.Text,12,Ui.Text))));
    }
    private void VictoryPanel()
    {
        TitlePanel("DZIEDZICTWO EPOKI","Drogi do zwycięstwa","Państwo może przetrwać dzięki wiedzy, kulturze, jedności lub zgromadzonemu dorobkowi.");
        var f=state.Player;if(state.EpochCampaign){side.Children.Add(Ui.Button("Programy kosmiczne i energia",ShowWorldDevelopment,true));side.Children.Add(Ui.Label("Naukowe: ukończ pięć programów aż po wysłanie statku pokoleniowego. Kulturowe: epoka międzyplanetarna, 300 000 kultury i 5000 prestiżu. Zjednoczenie: wszystkie miasta. Po zwycięstwie możesz kontynuować rozwój do limitu 1600 tur.",13,Ui.Muted));return;}Ui.Bar(side,$"Kultura: {f.Culture:0} / {Catalog.CultureVictory}",f.Culture,Catalog.CultureVictory,Ui.Brush("#BCA6CE"));Ui.Bar(side,$"Prestiż: {f.Prestige:0} / {Catalog.PrestigeVictory}",f.Prestige,Catalog.PrestigeVictory);side.Children.Add(Ui.Label("Zwycięstwo kulturowe wymaga obu wartości. Naukowe: odkryj Wielkie obserwatorium i zbuduj Atlas świata. Zjednoczenie: zdobądź wszystkie miasta.",12,Ui.Muted));side.Children.Add(Ui.Rule());
        side.Children.Add(Ui.Eyebrow("DOROBEK PAŃSTW"));foreach(var n in state.Factions.OrderByDescending(n=>Engine.Score(state,n.Id)))side.Children.Add(Ui.Card(Ui.Stack(Ui.Label(n.Name,18,Ui.Brush(n.Color),true),Ui.Label($"{Engine.Score(state,n.Id):0} punktów dorobku",13,Ui.Muted))));
        side.Children.Add(Ui.Label("Po 320 turach zwycięża najwyższy dorobek. Punkty: ludność ×8, budynki ×5, technologie ×18, kultura ×0,08, prestiż ×2 i złoto ×0,03.",11,Ui.Muted));
    }
    private void GuidePanel()
    {
        TitlePanel("PIERWSZE KROKI","Sztuka rządzenia","Twoje państwo zaczyna od jednego miasta i dwóch oddziałów.");
        (string,string)[] help=[("01 · Odpowiedz radzie","Sala rady prezentuje wydarzenia i koszty każdej odpowiedzi. Niewypełnione obietnice i wcześniejsze ustawy mogą powrócić po wielu turach."),("02 · Zaplanuj rozwój","Odkryj Rolnictwo, Pismo i Górnictwo. Buduj spichlerze oraz biblioteki. Żywność pokazuje nadwyżkę po utrzymaniu mieszkańców. Zima obniża plony. Plan produkcji miasta mieści osiem następnych zleceń. Możesz zmieniać ich kolejność, planować przyszłe technologie i powtarzać rekrutację. Wstrzymany lub zablokowany projekt zachowuje pracę, ale nowej nie odkłada. Automatyczne przejście nie traci nadwyżki; ręczna zmiana zachowuje 80%."),("03 · Odkrywaj i osiedlaj","Wybierz oddział lewym przyciskiem; prawym wskaż odkryte pole. Dalszy marsz jest kontynuowany w kolejnych turach. Osadników rekrutujesz w miastach liczących co najmniej 3 mieszkańców. Osada kosztuje jednego mieszkańca i powstaje minimum 4 pola od innych miast."),("04 · Słuchaj społeczeństwa","Podatki i dialogi zmieniają poparcie ludu, rodów i uczonych. Poparcie kształtuje stabilność, a ta — wydajność gospodarki. Utrzymuj rezerwę złota."),("05 · Walcz z planem","W dyplomacji wybierz powód wojny: podbój obniża zaufanie innych państw; zadośćuczynienie wymaga 30 krzywd, wyzwolenie — zajętego własnego miasta. Ogłoszony cel rozliczamy przy pokoju. Panel przebiegu wojny pokazuje bilans. W rokowaniach zaproponuj reparacje i zwrot miasta; AI może odpowiedzieć kontrofertą, na którą czeka cztery tury od jej przybycia. Pokojowe poselstwa pozwalają żądać odszkodowania lub zawrzeć pakt; odpowiedź po dwóch turach. PPM na przeciwniku otwiera podgląd walki; osobny przycisk potwierdza atak. Łucznicy i katapulty strzelają na 2 pola; miasta zdobywa piechota lub kawaleria. Gór nie można przekraczać. Oddziały poza zasięgiem dostaw słabną. Rozkazy wojskowe pozwalają okopać oddział (+20% obrony po turze, do +40%) albo leczyć go do pełnego zdrowia. Wymagają całego ruchu; marsz i atak porzucają pozycje. Po Piśmie otwórz w Dyplomacji Wywiad i kontrwywiad. Szkol agenta za 60 złota, wybierz odkryte obce miasto i operację: rozpoznanie, kradzież badań albo sabotaż. Wzmocniona ochrona miasta działa osiem tur i utrudnia także rozpoczęte misje. Panel Nauka → Epoki i rozwój świata pokazuje 18 epok, 90 technologii i ich infrastrukturę. Następny etap wymaga zamknięcia poprzedniego. W sekcji Energia, klimat i kosmos sprawdzisz zasilanie, emisje i pięć programów pozaziemskich. W panelu energii ustawisz priorytety zasilania miast i wyłączysz wybrane zakłady. Rozruch kosztuje 5% kosztu budowy w złocie; wyłączony zakład zachowuje 25% utrzymania i miejsca mieszkalne. Zmieniaj priorytet miasta i stan każdego zakładu raz na turę. Elektrownie wodne i geotermalne potrzebują odpowiedniego terenu, a moc farm słonecznych i wiatrowych zależy od sezonu. Trzy kolejne fazy pokrycia poniżej 75% wywołują kryzys. W rejestrze Kryzysy i obietnice energetyczne sprawdzisz zgłoszenia i zobowiązania rady. Pomoc kosztuje złoto, a obietnica wymaga trzech kolejnych przyszłych faz pełnego zasilania wskazanych zakładów w sześć tur. Wyłączenie obiecanych usług nie zalicza naprawy. Starszą kampanię rozszerzysz przyciskiem w przeglądzie epok. Schwytany agent wymaga okupu, przeprosin, porzucenia albo wymiany jeńców. Za obcych agentów możesz żądać 100 złota, możesz ich wymienić, zwolnić dla poprawy relacji albo internować na osiem tur; znajdziesz ich w panelu wywiadu. Panel Wojsko pokazuje wszystkie oddziały; Tab pomija stałe rozkazy i eskorty. Panel Szlaki pokazuje połączenia, budżet i blokady. Po Monecie otwórz Pożyczki i zobowiązania: finansowanie od zagranicznych kupców trzeba spłacić z odsetkami po 12 turach. Jedno odroczenie kupuje sześć tur za opłatę i większy dług. Odmowa szkodzi stabilności, relacjom i handlowi oraz zamyka kredyt na 24 tury. Drogi obniżają ruch do 0,5 punktu; koszt budowy: 25 złota, utrzymanie: 0,15/turę."),("06 · Dbaj o prowincje","Panel Ludność pokazuje zapasy, mieszkania, opiekę i dobrobyt. Drogi pozwalają wymieniać nadwyżki żywności i przenosić się rodzinom do lepszych warunków. Wybierz Zarządzaj, aby zmienić rozwój i ustrój miasta, sfinansować pomoc oraz sprawdzić lojalność. Lojalność obcego miasta poniżej 20 przez trzy tury oznacza jego powrót do założycieli. Panel Rynek pokazuje zapasy, ceny i zużycie żelaza, soli oraz barwników. Kontrakty wymagają targowisk, Szlaków handlowych, traktatu i połączeń ze stolicami. Import kosztuje złoto, a eksport może uszczuplić rezerwy mieszkańców. Karawany mają rzeczywisty czas podróży. Zakup jest płatny przy wysyłce; drogi skracają trasę. W panelu Rynek otwórz Karawany i trasy, aby śledzić transport. Pobliski oddział osłania przed sąsiednim wrogiem. Na polu wozu możesz wyznaczyć eskortę z panelu karawany lub jednostki. Wojsko podąża za pozostałe punkty ruchu i spowalnia wóz, nie odpoczywa oraz wymaga odwołania przed samodzielnym atakiem. Przed niedostępną granicą eskorta się kończy; handel nie daje prawa przejścia armii. Granice i tranzyt wskazują państwa zamykające potencjalny korytarz. W rozmowie o przejeździe poproś o zgodę na 12 tur lub zaoferuj 60 złota płatne dopiero po przyjęciu. Posłowie kosztują 20 złota i wracają po dwóch turach, a licznik postoju nadal biegnie. Zgoda wygasa przed rynkiem tury końcowej; wojna i embargo nadal blokują trasę. Embargo zatrzymuje również opłacone karawany; trzy tury postoju oznaczają utratę bez zwrotu. Po stracie importu kupcy mogą przyjść do rady. Odszkodowanie kosztuje skarb, odmowa obniża poparcie, a obietnicę zastępczej dostawy trzeba wypełnić rzeczywistym przyjazdem właściwego towaru do wskazanego miasta przed terminem. Panel Sprawy kupców i obietnice pokazuje postęp i wynik. W Ludności otwórz Rachunki i kontrole prowincji: korupcja odbiera część miejskiego złota. Kontrola wymaga Pisma, kosztuje 35 złota i trwa trzy tury. Raport obejmie rzeczywiste braki; wybierz jawny proces, kosztowną reformę urzędu albo ugodę z rodami."),("07 · Dbaj o historię","F5 otwiera zapis, F9 wczytanie, Tab wybiera kolejny gotowy oddział, Spacja kończy turę, Esc zamyka okno dialogu. Autozapis powstaje po każdej turze. W menu można zmienić ziarno świata."),("O tej wersji","To grywalny fundament 0.21: przestrzenna mapa 3D, cztery państwa i jedna kampania. Ilustracja doliny powstała z użyciem AI. Direct3D 11 renderuje teksturowane materiały, cienie, odbicia wody i animowane otoczenie. Przyciski Krajobraz i Grafika zmieniają kamerę oraz jakość. Fotorealizm ilustracji menu, szczegółowe modele, animacje postaci, dźwięk, floty, religie i multiplayer wymagają dalszej produkcji. Teren, miasta i oddziały są przestrzenne. LPM wybiera lub przesuwa mapę, krótki PPM wydaje rozkaz, przeciągany PPM lub środkowy przycisk obraca kamerę. Q/E obracają, W/A/S/D przesuwają, Page Up/Down zmieniają kąt, kółko przybliża, Home centruje stolicę.")];
        foreach(var (a,b) in help)side.Children.Add(Ui.Card(Ui.Stack(Ui.Eyebrow(a),Ui.Label(b,12,Ui.Muted))));
    }
    private void Select(int id)
    {
        selectedTile=id;map.PlannedRoad=[];var units=state.Units.Where(u=>u.Owner==0&&u.Tile==id).ToArray();selectedUnit=units.FirstOrDefault()?.Id??-1;tab="realm";Refresh();
    }
    private void Order(int id)
    {
        if(overlay!=null||busy)return;
        if(selectedUnit<0){Toast("Wybierz własny oddział lewym przyciskiem myszy.");return;}
        if(state.Units.Any(u=>u.Owner!=0&&u.Tile==id)||state.Cities.Any(c=>c.Owner!=0&&c.Tile==id)){ShowCombat(id);return;}
        ApplyOrder(Engine.OrderMove(state,selectedUnit,id));
    }
    private void NextUnit()
    {
        var units=state.Units.Where(u=>u.Owner==0&&u.Actions>0&&u.Order==MilitaryOrder.None&&u.EscortCaravanId==null).OrderBy(u=>u.Id).ToArray();if(units.Length==0){Toast("Brak oddziałów oczekujących na rozkazy. Stałe rozkazy znajdziesz w panelu Wojsko.");return;}
        var next=units.FirstOrDefault(u=>u.Id>selectedUnit)??units[0];selectedTile=next.Tile;selectedUnit=next.Id;tab="realm";map.Center(next.Tile);Refresh();
    }
    private void Do(Result result){Toast(result.Message,result.Success);Refresh();if(state.Ending!=null&&overlay==null)ShowEnding();}
    private void Toast(string text,bool success=true){if(status!=null){status.Text=text;status.Foreground=success?Ui.Muted:Ui.Red;}}
    private void Advance()
    {
        if(busy||overlay!=null)return;if(state.PendingEvent!=null){ShowCouncil();return;}busy=true;endButton.IsEnabled=false;
        try
        {
            var result=Engine.EndTurn(state);Toast(result.Message,result.Success);
            var replies=state.Demands.Where(d=>d.ResolvedTurn==state.Turn).ToArray();
            var peaceNews=state.PeaceOffers.Any(o=>o.ResolvedTurn==state.Turn||o.Status==PeaceStatus.AwaitingPlayer&&o.DueTurn==state.Turn);
            if(peaceNews){tab="diplomacy";Toast("Wieści z rokowań. Sprawdź odpowiedź lub propozycję pokoju w dyplomacji.");}
            else if(replies.Length>0){tab="diplomacy";Toast($"Powrót posłów. Otrzymane odpowiedzi: {replies.Length}. Warunki i słowa sąsiadów znajdziesz w dyplomacji.");}
            else if(state.Chronicle.Any(e=>e.Turn==state.Turn&&e.Category=="wywiad")){tab="diplomacy";Toast("Nowe raporty wywiadu lub wykryta ingerencja. Otwórz Wywiad i kontrwywiad w Dyplomacji.",false);}
            else if(state.Chronicle.LastOrDefault(e=>e.Turn==state.Turn&&e.Category=="skarb") is ChronicleEntry debtNews){tab="logistics";Toast(debtNews.Text,false);}
            else if(state.Audits.Any(a=>a.CompletedTurn==state.Turn&&a.Status==AuditStatus.AwaitingCouncil)){tab="provinces";Toast("Kontrolerzy wrócili z księgami. Raport czeka w rachunkach prowincji i sali rady.");}
            else if(state.Cities.Any(c=>c.Owner==0&&c.RevoltTurns>0)){tab="provinces";Toast("Prowincja grozi buntem. Sprawdź lojalność i warunki zarządzania.",false);}
            else if(TransitCouncil.Notices(state) is {Count:>0} transitNews){tab="diplomacy";Toast(string.Join(" ",transitNews),false);}
            else if(state.TradeCases.Any(c=>c.ResolvedTurn==state.LastMarketTurn&&c.Status is TradeCaseStatus.Fulfilled or TradeCaseStatus.Broken))
            {tab="markets";Toast("Rada rozliczyła obietnicę zastępczej dostawy. Wynik znajdziesz w sprawach kupców.");}
            else if(state.Caravans.Any(c=>c.LastMovedTurn==state.LastMarketTurn&&(c.Status==CaravanStatus.Waiting||c.Status==CaravanStatus.Lost&&c.ResolvedTurn==state.LastMarketTurn)&&
                state.TradeContracts.Any(t=>t.Id==c.ContractId&&(t.Buyer==0||t.Seller==0))))
            {tab="markets";Toast("Kłopoty na szlaku. Sprawdź zatrzymane lub utracone karawany w panelu rynku.",false);}
            if(selectedUnit>=0&&!state.Units.Any(u=>u.Id==selectedUnit))selectedUnit=-1;TryAutoSave(true);
        }
        finally{busy=false;Refresh();}
        if(state.Ending!=null)ShowEnding();else if(state.PendingEvent!=null)ShowCouncil();
    }
    private void Modal(UIElement content,double width=720)
    {
        CloseModal();var layer=new Grid{Background=Ui.Brush("#B30A161C")};var card=new Border{Width=width,MaxHeight=Math.Max(600,ActualHeight-110),Background=Ui.Panel,BorderBrush=Ui.Gold,BorderThickness=new Thickness(1),Padding=new Thickness(32),VerticalAlignment=VerticalAlignment.Center,HorizontalAlignment=HorizontalAlignment.Center,CornerRadius=new CornerRadius(4),Child=new ScrollViewer{Content=content,VerticalScrollBarVisibility=ScrollBarVisibility.Auto}};layer.Children.Add(card);overlay=layer;root.Children.Add(layer);
    }
    private void CloseModal(){if(overlay!=null){root.Children.Remove(overlay);overlay=null;}}
    private void ShowCouncil()
    {
        if(state.PendingEvent==null){Toast("Rada nie ma nowej sprawy. Poprzednie decyzje znajdziesz w kronice.");return;}
        var e=ProvincialEvents.Current(state);var p=new StackPanel();p.Children.Add(Ui.Eyebrow($"SALA RADY  /  TURA {state.Turn}"));p.Children.Add(Ui.Label(e.Title,36,Ui.Text,true));Ui.Gap(p,18);
        var speaker=Ui.Stack(Ui.Label(e.Speaker,17,Ui.Gold,true),Ui.Label(e.Role,11,Ui.Muted));p.Children.Add(speaker);Ui.Gap(p,18);p.Children.Add(Ui.Label("„"+e.Body+"”",17,Ui.Text,true));p.Children.Add(Ui.Rule());
        for(int i=0;i<e.Choices.Length;i++)
        {
            int index=i;var c=e.Choices[i];var text=Ui.Stack(Ui.Label($"{i+1:00}   {c.Text}",15,Ui.Text),new Border{Height=5},Ui.Label(Narrative.Effects(c,e.Id,index),12,Ui.Muted));var button=Ui.Button("",()=>{var result=Narrative.Resolve(state,index);if(result.Success){CloseModal();TryAutoSave(false);}Do(result);});button.Content=text;button.IsEnabled=state.Player.Gold+c.Gold>=0&&(e.Id!=TradeCouncil.Event.Id||TradeCouncil.ChoiceError(state,index)==null)&&(e.Id!=AuditCouncil.Event.Id||AuditCouncil.ChoiceError(state,index)==null)&&(e.Id!=DebtCouncil.Event.Id||DebtCouncil.ChoiceError(state,index)==null)&&(e.Id!=SpyCouncil.Event.Id||SpyCouncil.ChoiceError(state,index)==null)&&(e.Id!=CustodyCouncil.Event.Id||CustodyCouncil.ChoiceError(state,index)==null)&&(e.Id!=EnergyCouncil.Event.Id||EnergyCouncil.ChoiceError(state,index)==null)&&(EpochCouncils.Index(e.Id)<0||EpochCouncils.ChoiceError(state,index)==null);p.Children.Add(button);
        }
        p.Children.Add(Ui.Label("Rada zapamięta tę odpowiedź. Wybory i ich następstwa trafiają do kroniki.",11,Ui.Gold));Ui.Gap(p,14);p.Children.Add(Ui.Button("Wróć do mapy — odpowiedz później",CloseModal));Modal(p,800);
    }
    private void ShowProjects(City city,int era=-1)
    {
        var p=Ui.Stack(Ui.Eyebrow($"{city.Name} / PRODUKCJA"),Ui.Label("Co zbudujemy?",34,Ui.Text,true),Ui.Label("Zmiana projektu zachowuje 80% zgromadzonej produkcji. Oddziały wymagają utrzymania w złocie.",12,Ui.Muted));Ui.Gap(p,18);
        p.Children.Add(Ui.Button("Zaplanuj kolejkę produkcji",()=>ShowProductionQueue(city)));
        if(state.EpochCampaign)
        {
            var filter=Ui.Combo(new[]{"Wszystkie epoki"}.Concat(EpochCatalog.All.Select(e=>e.Name)).ToArray(),era+1);
            filter.SelectionChanged+=(_,_)=>ShowProjects(city,filter.SelectedIndex-1);p.Children.Add(filter);
        }
        var projects=new System.Windows.Controls.Primitives.UniformGrid{Columns=2};p.Children.Add(projects);
        foreach(var project in Engine.AvailableProjects(state,city).Where(project=>era<0||EpochCatalog.EraOf(project.Technology??"")==era))
        {
            var c=Ui.Stack(Ui.Label(project.Name,20,Ui.Text,true),Ui.Label($"{project.Cost} produkcji · {project.Description}",12,Ui.Muted));var b=Ui.Button("",()=>{CloseModal();Do(Engine.SetProject(state,city.Id,project.Id));});b.Content=c;b.Margin=new Thickness(0,0,10,10);projects.Children.Add(b);
        }
        p.Children.Add(Ui.Button("Zamknij",CloseModal));Modal(p,820);
    }
    private void ShowEnding()
    {
        var p=Ui.Stack(Ui.Eyebrow("KONIEC TEGO ROZDZIAŁU"),Ui.Label("Historia została zapisana",34,Ui.Text,true),Ui.Label(state.Ending??"",17,Ui.Gold,true));Ui.Gap(p,20);p.Children.Add(Ui.Label($"Twój dorobek: {Engine.Score(state,0):0} punktów. Podjęte sprawy rady: {state.SeenEvents.Count}. Możesz obejrzeć mapę i kronikę albo rozpocząć nową kampanię.",14,Ui.Muted));Ui.Gap(p,20);if(state.EpochCampaign&&state.Cities.Any(c=>c.Owner==0)&&state.Turn<=state.MaxTurns)p.Children.Add(Ui.Button("Kontynuuj rozwój świata",()=>{var r=Engine.ContinueHistory(state);if(r.Success){CloseModal();TryAutoSave(false);}Do(r);}));p.Children.Add(Ui.Button("Obejrzyj kronikę",()=>{CloseModal();tab="history";RefreshSide();},true));p.Children.Add(Ui.Button("Wróć do menu",()=>{TryAutoSave(false);Landing();}));Modal(p,720);
    }
    private void Save()
    {
        Directory.CreateDirectory(saveFolder);var dialog=new SaveFileDialog{Filter="Historia Marchii (*.crown)|*.crown",InitialDirectory=saveFolder,FileName=$"Marchie-tura-{state.Turn:000}.crown"};if(dialog.ShowDialog(this)!=true)return;
        try{SaveGame.Write(state,dialog.FileName);Toast("Zapisano historię: "+Path.GetFileName(dialog.FileName));}catch(Exception e){Toast("Zapis nie powiódł się: "+e.Message,false);}
    }
    private void Load()
    {
        Directory.CreateDirectory(saveFolder);var dialog=new OpenFileDialog{Filter="Historia Marchii (*.crown)|*.crown",InitialDirectory=saveFolder};if(dialog.ShowDialog(this)==true)LoadPath(dialog.FileName);
    }
    private void LoadPath(string path)
    {
        try{var loaded=SaveGame.Read(path);Start(loaded);if(loaded.PendingEvent!=null&&loaded.Ending==null)ShowCouncil();}
        catch(Exception e){MessageBox.Show("Nie udało się wczytać zapisu. Bieżąca kampania pozostaje otwarta.\n"+e.Message,"Wczytanie historii",MessageBoxButton.OK,MessageBoxImage.Warning);}
    }
    private void TryAutoSave(bool report)
    {
        if(arguments.Any(a=>a.StartsWith("--render"))||arguments.Any(a=>a.StartsWith("--ui-smoke")))return;
        try{SaveGame.Write(state,AutoPath);}catch(Exception e){if(report)Toast("Tura zakończona, ale autozapis nie powiódł się: "+e.Message,false);}
    }
    private void OnKey(object sender,KeyEventArgs e)
    {
        if(e.Key==Key.Escape){CloseModal();return;}if(state==null||!root.Children.Contains(shell)||overlay!=null)return;
        if(e.OriginalSource is TextBox or ComboBox)return;
        if(e.Key==Key.F5){Save();e.Handled=true;}else if(e.Key==Key.F9){Load();e.Handled=true;}else if(e.Key==Key.Space){Advance();e.Handled=true;}else if(e.Key==Key.Tab){NextUnit();e.Handled=true;}else if(e.Key==Key.Home)map.Center(state.Cities.FirstOrDefault(c=>c.Owner==0)?.Tile??selectedTile);
    }
}
internal sealed class UniformGridEx : System.Windows.Controls.Primitives.UniformGrid { }
