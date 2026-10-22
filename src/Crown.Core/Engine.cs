namespace Crown.Core;

public static partial class Engine
{
    public static City AddCity(GameState s,int owner,int tile,string name)
    {
        var c=new City {Id=s.NextId++,Owner=owner,OriginalOwner=owner,Tile=tile,Name=name,Project="scout"}; s.Cities.Add(c);
        foreach(var t in s.Tiles.Where(t=>World.Distance(t,s.Tiles[tile])<=2 && t.Owner<0)) t.Owner=owner;
        s.Tiles[tile].Owner=owner; s.Tiles[tile].Ruin=false; s.InvalidateLogistics(); return c;
    }
    public static Unit AddUnit(GameState s,int owner,int tile,UnitKind kind)
    {
        var u=new Unit {Id=s.NextId++,Owner=owner,Tile=tile,Kind=kind}; u.Actions=u.MaxActions; s.Units.Add(u); s.InvalidateLogistics(); return u;
    }
    public static void Clamp(Faction f)
    {
        f.Stability=Math.Clamp(f.Stability,0,100); f.Commons=Math.Clamp(f.Commons,0,100); f.Nobles=Math.Clamp(f.Nobles,0,100); f.Scholars=Math.Clamp(f.Scholars,0,100);
    }
    public static bool HasResource(GameState s,int owner,Resource r) => s.Tiles.Any(t=>t.Owner==owner&&t.Resource==r);
    public static Yields TileYields(Tile t,Faction f)
    {
        (double food,double prod,double gold)=t.Terrain switch {Terrain.Grassland=>(3,1,0),Terrain.Forest=>(2,2,0),Terrain.Hills=>(1,3,0),Terrain.Desert=>(0,1,1),Terrain.Marsh=>(3,0,0),Terrain.Tundra=>(1,1,0),_=>(0,0,0)};
        if(t.River) {food++;gold++;}
        switch(t.Resource) {case Resource.Grain:food+=2;break;case Resource.Horses:food++;prod++;break;case Resource.Iron:case Resource.Stone:prod+=2;break;case Resource.Salt:case Resource.Dyes:gold+=3;break;}
        switch(t.Improvement) {case Improvement.Farm:food+=f.Technologies.Contains("agriculture")?3:2;break;case Improvement.Mine:prod+=3;break;case Improvement.TradingPost:gold+=4;break;}
        return new(food,prod,gold,0,0);
    }
    public static IEnumerable<Tile> WorkedTiles(GameState s,City c) => s.Tiles.Where(t=>t.Owner==c.Owner && t.Passable && World.Distance(t,s.Tiles[c.Tile])<=2 && t.Id!=c.Tile)
        .OrderByDescending(t=> {var y=TileYields(t,s.Factions[c.Owner]); return y.Food*1.1+y.Production+y.Gold*.7;}).Take(c.Population+1);
    public static Yields CityYields(GameState s,City c)
    {
        var y=BeforeCorruption(s,c);return y with {Gold=y.Gold*(1-c.Corruption/100d)};
    }
    internal static Yields BeforeCorruption(GameState s,City c)
    {
        var f=s.Factions[c.Owner]; double food=5,prod=4+(f.Id==0?1:0),gold=3+(f.Id==1?3:0),science=2+c.Population*.7+(f.Id==2?2:0),culture=1+c.Population*.3;
        foreach(var t in WorkedTiles(s,c)) {var y=TileYields(t,f);food+=y.Food;prod+=y.Production;gold+=y.Gold;}
        foreach(var b in c.Buildings) switch(b)
        {
            case "granary":food+=4;break;case "market":gold+=5;culture++;break;case "library":science+=5;break;case "aqueduct":food+=3;science++;break;case "temple":culture+=5;break;
            case "workshop":prod+=6;break;case "bank":gold+=9;break;case "university":science+=12;break;case "archive":culture+=10;science+=3;break;case "factory":prod+=14;break;case "parliament":culture+=12;break;
        }
        if(s.Tiles[c.Tile].River && f.Technologies.Contains("irrigation")) food+=2;
        if(f.Technologies.Contains("medicine")) food*=1.25;
        if(f.Technologies.Contains("ecology")) food*=1.15;
        if(f.Technologies.Contains("currency")) gold+=2;
        if(f.Technologies.Contains("trade")) gold*=1.25;
        if(f.Technologies.Contains("writing")) science*=1.15;
        if(f.Technologies.Contains("education")) science*=1.2;
        if(f.Technologies.Contains("industry")) prod*=1.25;
        if(f.Technologies.Contains("electricity")) {prod*=1.3;science*=1.3;}
        if(f.Technologies.Contains("philosophy")) culture*=1.2;
        if(f.Technologies.Contains("printing")) culture*=1.3;
        if(f.Government==1) {gold*=1.15;culture*=1.1;} else if(f.Government==2) {prod*=1.2;culture*=.85;}
        if(f.Focus==1) {science*=1.3;prod*=.85;} else if(f.Focus==2) {prod*=1.25;science*=.85;}
        if(c.Id!=s.Cities.Where(city=>city.Owner==c.Owner).Min(city=>city.Id)&&Logistics.For(s,c.Owner).ConnectedCities.Contains(c.Id))
        {gold+=3+(f.Flags.Contains("road_tolls")?2:0);science+=1+(f.Flags.Contains("open_roads")?1:0);}
        if(c.Focus==CityFocus.Agriculture){food*=1.2;prod*=.9;}
        else if(c.Focus==CityFocus.Industry){prod*=1.2;food*=.9;}
        else if(c.Focus==CityFocus.Scholarship){science*=1.25;gold*=.9;}
        if(c.Charters.Contains("bonded_labor"))prod*=1.1;
        if(c.Charters.Contains("integration"))culture+=1;
        if(c.Charters.Contains("guild_housing"))gold*=.95;
        if(c.Charters.Contains("local_assembly"))gold*=.95;
        if(c.Governance==Governance.Autonomous){gold*=.7;culture*=1.2;}
        else if(c.Governance==Governance.Military){prod*=.9;science*=.8;}
        food*=s.Turn%4==0?.8:s.Turn%4==3?1.1:1;
        double efficiency=Math.Clamp(.65+f.Stability*.005-c.Unrest*.003,.35,1.15)*(.9+c.Prosperity*.002)*(.85+c.Loyalty*.002);
        gold*=f.Tax==0?.65:f.Tax==2?1.5:1;
        return new(food-c.Population*2,prod*efficiency*Markets.IndustryMultiplier(c),gold*efficiency,science*efficiency,culture*efficiency);
    }
    public static Yields Income(GameState s,int owner)
    {
        double food=0,prod=0,gold=0,science=0,culture=0;
        foreach(var c in s.Cities.Where(c=>c.Owner==owner)) {var y=CityYields(s,c);food+=y.Food;prod+=y.Production;gold+=y.Gold;science+=y.Science;culture+=y.Culture;}
        return new(food,prod,Costs(s,owner,gold).Net,science,culture);
    }
    public static IEnumerable<Project> AvailableProjects(GameState s,City c) => Content.Projects.Where(p=>
        (p.Technology==null || s.Factions[c.Owner].Technologies.Contains(p.Technology)) && (p.Unit!=null||!c.Buildings.Contains(p.Id)) &&
        (p.Unit!=UnitKind.Settler||c.Population>=3) && (p.Unit!=UnitKind.Cavalry||HasResource(s,c.Owner,Resource.Horses)));
    public static Result SetProject(GameState s,int cityId,string project)
    {
        var c=s.Cities.FirstOrDefault(c=>c.Id==cityId&&c.Owner==0);
        if(s.Ending!=null||c==null||!AvailableProjects(s,c).Any(p=>p.Id==project)) return Result.Fail("Ten projekt jest niedostępny.");
        if(c.Project==project) return Result.Ok("Projekt już jest realizowany.");
        c.Project=project;c.RepeatProject=false; c.Production*=.8; return Result.Ok("Zmieniono projekt. Zachowano 80% zgromadzonej produkcji; oczekujące zlecenia pozostają w planie.");
    }
    public static Result SetResearch(GameState s,string id)
    {
        var t=Content.Technologies.FirstOrDefault(t=>t.Id==id); var f=s.Player;
        if(s.Ending!=null||t==null||f.Technologies.Contains(id)||!t.Requires.All(f.Technologies.Contains)) return Result.Fail("Nie spełniono wymagań technologii.");
        f.Research=id; return Result.Ok("Zmieniono kierunek badań. Punkty nauki pozostają w puli.");
    }
    public static Result Improve(GameState s,int id,Improvement improvement)
    {
        if(id<0||id>=s.Tiles.Count||s.Ending!=null) return Result.Fail("Nieprawidłowe pole."); var t=s.Tiles[id]; var f=s.Player;
        string tech=improvement==Improvement.Farm?"agriculture":improvement==Improvement.Mine?"mining":"trade";
        int cost=improvement==Improvement.Farm?35:improvement==Improvement.Mine?45:55;
        if(improvement==Improvement.None||t.Owner!=0||!t.Passable||t.Improvement!=Improvement.None||s.Cities.Any(c=>c.Tile==id)) return Result.Fail("Ulepszenie wymaga wolnego pola we własnych granicach.");
        if(!f.Technologies.Contains(tech)) return Result.Fail($"Wymagana technologia: {Content.Tech(tech).Name}.");
        if(f.Gold<cost) return Result.Fail("Za mało złota.");
        f.Gold-=cost; t.Improvement=improvement; return Result.Ok("Ulepszono pole. Korzysta z niego pobliskie miasto, jeśli mieszkańcy je uprawiają.");
    }
    public static Result SetPolicy(GameState s,int tax,int government,int focus)
    {
        var f=s.Player; if(s.Ending!=null||tax is <0 or >2||government is <0 or >2||focus is <0 or >2) return Result.Fail("Nieprawidłowa polityka.");
        if(f.Tax==tax&&f.Government==government&&f.Focus==focus) return Result.Fail("Te zasady już obowiązują.");
        if(f.LastPolicyTurn==s.Turn) return Result.Fail("Rada przyjęła już reformę w tej turze.");
        if(f.Gold<20) return Result.Fail("Reforma kosztuje 20 złota.");
        f.Gold-=20;f.Commons+=(f.Tax-tax)*5;
        static int NobleEffect(int g)=>g==2?4:g==1?-3:0;
        static int ScholarEffect(int p)=>p==1?5:p==2?-4:0;
        f.Nobles+=NobleEffect(government)-NobleEffect(f.Government);f.Scholars+=ScholarEffect(focus)-ScholarEffect(f.Focus);
        f.Tax=tax;f.Government=government;f.Focus=focus;f.LastPolicyTurn=s.Turn; Clamp(f);s.Log("Rada zmieniła podatki, ustrój lub priorytety państwa.","polityka");return Result.Ok("Reforma weszła w życie. Koszt: 20 złota.");
    }
    public static int StabilityTarget(GameState s,Faction f)
    {
        int buildings=s.Cities.Where(c=>c.Owner==f.Id).Sum(c=>(c.Buildings.Contains("temple")?2:0)+(c.Buildings.Contains("court")?4:0)+(c.Buildings.Contains("parliament")?6:0)-(c.Buildings.Contains("factory")?3:0));
        return Math.Clamp(22+(f.Commons+f.Nobles+f.Scholars)/5 + (f.Tax==0?6:f.Tax==2?-12:0)+(f.Government==1?5:0)+buildings-Math.Max(0,s.Cities.Count(c=>c.Owner==f.Id)-3)*4-(int)(f.WarWeariness*.35)+(f.Technologies.Contains("constitution")?10:0)+(f.Technologies.Contains("ecology")?10:0),5,95);
    }
    public static bool Supplied(GameState s,Unit u) => Logistics.Supplied(s,u);
    public static double Strength(GameState s,Unit u)
    {
        var f=s.Factions[u.Owner]; double p=u.Kind switch {UnitKind.Scout=>8,UnitKind.Spearmen=>19,UnitKind.Archer=>17,UnitKind.Cavalry=>25,UnitKind.Catapult=>14,_=>2};
        p+=(f.Id==3?3:0)+(f.Technologies.Contains("bronze")?3:0)+(f.Technologies.Contains("iron")?5+(HasResource(s,u.Owner,Resource.Iron)?3:0):0)+(f.Technologies.Contains("steel")?7:0)+Math.Min(u.Experience,10);
        return p*(.6+u.Health*.004)*(Supplied(s,u)?1:.72);
    }
    public static double Military(GameState s,int owner) => s.Units.Where(u=>u.Owner==owner&&u.Kind!=UnitKind.Settler).Sum(u=>Strength(s,u));
    public static bool AtWar(GameState s,int a,int b) => a>=0&&b>=0&&a!=b&&s.Relation(a,b).War;
    public static HashSet<int> Visible(GameState s)
    {
        var ids=new HashSet<int>(); foreach(var c in s.Cities.Where(c=>c.Owner==0)) foreach(var t in s.Tiles.Where(t=>World.Distance(t,s.Tiles[c.Tile])<=3)) ids.Add(t.Id);
        foreach(var u in s.Units.Where(u=>u.Owner==0)) foreach(var t in s.Tiles.Where(t=>World.Distance(t,s.Tiles[u.Tile])<=(u.Kind==UnitKind.Scout?4:2))) ids.Add(t.Id); return ids;
    }
    public static void Explore(GameState s) {foreach(int id in Visible(s)) s.Tiles[id].Explored=true;}
    public static Result FoundCity(GameState s,int unitId)
    {
        var u=s.Units.FirstOrDefault(u=>u.Id==unitId);if(s.Ending!=null||u==null||u.Kind!=UnitKind.Settler||u.Actions<1)return Result.Fail("Potrzebni są osadnicy z punktem ruchu.");
        var t=s.Tiles[u.Tile];if(!t.Passable||(t.Owner>=0&&t.Owner!=u.Owner)||s.Cities.Any(c=>World.Distance(t,s.Tiles[c.Tile])<4))return Result.Fail("Miasto musi leżeć przynajmniej 4 pola od innych miast, na wolnej ziemi lub we własnych granicach.");
        int n=s.Cities.Count(c=>c.Owner==u.Owner);string[] names=["Brzegowo","Kamieniec","Złote Łąki","Ostrowiec","Nowa Przystań","Srebrna Dolina"];
        string name=u.Owner==0?names[(n-1+names.Length)%names.Length]:$"{s.Factions[u.Owner].Name.Split(' ')[0]} — {n+1}";
        AddCity(s,u.Owner,t.Id,name);s.Units.Remove(u);s.Factions[u.Owner].Prestige+=8;s.Log($"Założono {name}. Nowe miasto zwiększa wpływy i koszty administracji.","miasto");Explore(s);return Result.Ok($"Założono {name}.");
    }
    public static Result Diplomacy(GameState s,int other,string action)
    {
        if(action=="war")return DeclareWar(s,other,WarReason.Conquest);
        if(action=="peace")return ProposePeace(s,other,100);
        if(s.Ending!=null||other<=0||other>=s.Factions.Count)return Result.Fail("Nieprawidłowy rozmówca.");
        var f=s.Player;var r=s.Relation(0,other);if(!s.Cities.Any(c=>c.Owner==other))return Result.Fail("To państwo utraciło wszystkie miasta.");
        if(r.LastActionTurn==s.Turn)return Result.Fail("Posłowie odbyli już rozmowę w tej turze.");
        switch(action)
        {
            case "gift":if(r.War||f.Gold<40)return Result.Fail("Dar wymaga pokoju i 40 złota.");f.Gold-=40;s.Factions[other].Gold+=40;r.Trust+=12;break;
            case "trade":if(r.War||r.Trade||r.Trust<10||f.Gold<30)return Result.Fail("Traktat wymaga pokoju, relacji +10 i 30 złota.");f.Gold-=30;r.Trade=true;r.Trust+=5;break;
            default:return Result.Fail("Nieznana propozycja.");
        }
        r.Trust=Math.Clamp(r.Trust,-100,100);r.LastActionTurn=s.Turn;Clamp(f);s.InvalidateLogistics();
        string text=action switch{"gift"=>"Wymieniono dary","trade"=>"Zawarto traktat handlowy","war"=>"Wypowiedziano wojnę",_=>"Podpisano pokój i dziesięcioturowy rozejm"};
        s.Log($"{text}: {s.Factions[other].Name}.","dyplomacja");return Result.Ok(text+".");
    }
    public static Result EndTurn(GameState s)
    {
        if(s.Ending!=null)return Result.Fail("Kampania dobiegła końca. Rozpocznij nową historię.");
        if(s.PendingEvent!=null)return Result.Fail("Najpierw odpowiedz na sprawę przedstawioną radzie.");
        WarCouncil.EnsureActive(s);
        s.InvalidateLogistics();
        Markets.Tick(s);
        foreach(var f in s.Factions)
        {
            if(!s.Cities.Any(c=>c.Owner==f.Id))continue;
            var income=Income(s,f.Id);double modifier=f.Id==0?1:s.Difficulty=="Wymagający"?1.3:s.Difficulty=="Spokojny"?.85:1;
            f.Gold+=income.Gold*modifier;f.Science+=income.Science*modifier;f.Culture+=income.Culture;f.Prestige+=income.Culture*.045;
            Administration.Collect(s,f.Id,modifier);
            if(f.Gold<0){f.Stability-=3;f.Commons-=2;f.Gold=0;if(s.Turn%3==0){var u=s.Units.LastOrDefault(u=>u.Owner==f.Id);if(u!=null)s.Units.Remove(u);}if(f.Id==0)s.Log("Niewypłacalność: niepokoje i dezercje. Zwiększ dochody lub zmniejsz armię.","gospodarka");}
            var tech=Content.Technologies.FirstOrDefault(t=>t.Id==f.Research);
            if(tech!=null&&!f.Technologies.Contains(tech.Id)&&tech.Requires.All(f.Technologies.Contains)&&f.Science>=tech.Cost)
            {
                f.Science-=tech.Cost;f.Technologies.Add(tech.Id);f.Prestige+=5;
                if(f.Id==0)s.Log($"Odkrycie: {tech.Name}. {tech.Description}","nauka");
                f.Research=Content.Technologies.FirstOrDefault(t=>!f.Technologies.Contains(t.Id)&&t.Requires.All(f.Technologies.Contains))?.Id??"";
            }
            var foodPlan=Regions.PlanFood(s,f.Id);
            foreach(var c in s.Cities.Where(c=>c.Owner==f.Id).ToArray())
            {
                var y=CityYields(s,c);c.Food+=foodPlan.Balance(c.Id);c.LastFoodImported=foodPlan.Imports[c.Id];c.LastFoodExported=foodPlan.Exports[c.Id];int needed=22+c.Population*12;
                if(c.Food>=needed){c.Population++;c.Food-=needed;}
                if(c.Food<0){c.HungerTurns++;if(c.HungerTurns%2==0&&c.Population>1){c.Population--;if(f.Id==0)s.Log($"{c.Name}: przedłużający się głód zmniejsza liczbę mieszkańców.","gospodarka");}c.Food=0;f.Commons-=3;c.Unrest=Math.Min(100,c.Unrest+5);}
                else c.HungerTurns=0;
                c.Health=Math.Min(100,c.Health+7);
                var p=ProductionPlanning.CurrentBlock(s,c)==null?AvailableProjects(s,c).FirstOrDefault(p=>p.Id==c.Project):null;
                if(p!=null)c.Production+=y.Production*modifier;
                if(p!=null&&c.Production>=p.Cost)
                {
                    c.Production-=p.Cost;
                    if(p.Unit!=null){var u=AddUnit(s,f.Id,c.Tile,p.Unit.Value);if(c.Buildings.Contains("barracks"))u.Experience=2;if(p.Unit==UnitKind.Settler)c.Population--;}
                    else if(p.Id=="treasury")f.Gold+=60;
                    else if(p.Id=="festival"){f.Culture+=30;f.Prestige+=4;}
                    else c.Buildings.Add(p.Id);
                    if(f.Id==0)s.Log($"{c.Name}: ukończono {p.Name}.","miasto");
                    ProductionPlanning.Next(s,c);
                }
            }
            Regions.Tick(s,f.Id);
            f.WarWeariness=Math.Clamp(f.WarWeariness+(s.Relations.Any(r=>(r.A==f.Id||r.B==f.Id)&&r.War)?1.8:-3),0,100);
            f.Stability+=Math.Sign(StabilityTarget(s,f)-f.Stability);
            // Society slowly reacts to taxation; low support has an ongoing economic consequence through stability.
            if(s.Turn%5==0){f.Commons+=f.Tax==0?2:f.Tax==2?-3:1;f.Scholars+=f.Focus==1?1:0;f.Nobles+=f.Government==2?1:0;}
            if(f.Stability<25){foreach(var c in s.Cities.Where(c=>c.Owner==f.Id))c.Unrest=Math.Min(90,c.Unrest+8);f.Gold=Math.Max(0,f.Gold-15);}
            Clamp(f);
        }
        foreach(var u in s.Units.ToArray())MilitaryOrders.FinishTurn(s,u);
        ProvincialPolitics.Tick(s);RunAi(s);s.Turn++;s.InvalidateLogistics();PeaceCouncil.Tick(s);DiplomaticCouncil.Tick(s);
        AuditCouncil.Tick(s);
        TreasuryLoans.Tick(s);
        foreach(string notice in TransitCouncil.Notices(s))s.Log(notice,"tranzyt");
        foreach(var unit in s.Units.Where(u=>u.Owner==0&&u.Destination!=null).OrderBy(u=>u.Id).ToArray())FollowOrder(s,unit);
        EscortOrders.Normalize(s);
        foreach(var effect in s.Deferred.Where(e=>e.Due<=s.Turn).ToArray())
        {
            var f=s.Factions[effect.Faction];f.Gold=Math.Max(0,f.Gold+effect.Gold);f.Stability+=effect.Stability;f.Commons+=effect.Commons;f.Nobles+=effect.Nobles;f.Scholars+=effect.Scholars;f.Prestige+=effect.Prestige;Clamp(f);s.Log(effect.Text,"konsekwencja");s.Deferred.Remove(effect);
        }
        Explore(s);CheckEnding(s);if(s.Ending==null)Narrative.Schedule(s);return Result.Ok($"Początek tury {s.Turn}. {s.Season}.");
    }
    private static void RunAi(GameState s)
    {
        foreach(var f in s.Factions.Skip(1))
        {
            var cities=s.Cities.Where(c=>c.Owner==f.Id).ToArray();if(cities.Length==0)continue;
            var choices=Content.Technologies.Where(t=>!f.Technologies.Contains(t.Id)&&t.Requires.All(f.Technologies.Contains)).ToArray();
            if(choices.Length>0&&(s.Turn%4==0||f.Research==""))f.Research=choices[s.Next(choices.Length)].Id;
            if(f.Gold>130&&f.Technologies.Contains("pottery"))
            {
                var route=cities.Skip(1).Select(c=>Logistics.PlanRoad(s,c.Id)).FirstOrDefault(p=>p.Error==null&&p.NewTiles>0);
                if(route!=null)foreach(int id in route.Path.Where(id=>!Navigation.RoadNode(s,id)).Take(2).ToArray())
                    if(f.Gold>100)BuildRoad(s,id,f.Id);
            }
            foreach(var city in cities)
            {
                int military=s.Units.Count(u=>u.Owner==f.Id&&u.Kind!=UnitKind.Scout&&u.Kind!=UnitKind.Settler);
                var possible=AvailableProjects(s,city).ToArray();bool war=s.Relations.Any(r=>(r.A==f.Id||r.B==f.Id)&&r.War);
                if(s.Turn>12&&cities.Length<4&&!s.Units.Any(u=>u.Owner==f.Id&&u.Kind==UnitKind.Settler)&&city.Population>=4&&s.Turn%7==0)city.Project="settler";
                else if(military<(war?cities.Length*3+2:cities.Length+2)&&city.Project!="settler")city.Project=possible.Any(p=>p.Id=="cavalry")?"cavalry":possible.Any(p=>p.Id=="archer")&&s.Next(2)==0?"archer":"spearmen";
                else if(city.Project!="settler"&&Content.Project(city.Project).Unit!=null&&military>=cities.Length*3+2)city.Project=possible.FirstOrDefault(p=>p.Unit==null)?.Id??"spearmen";
                if(f.Gold>120)
                {
                    var tile=WorkedTiles(s,city).FirstOrDefault(t=>t.Improvement==Improvement.None);
                    if(tile!=null&&f.Technologies.Contains("agriculture")){tile.Improvement=tile.Terrain==Terrain.Hills&&f.Technologies.Contains("mining")?Improvement.Mine:Improvement.Farm;f.Gold-=35;}
                }
                Regions.ManageAi(s,city);
            }
            Markets.ManageAi(s,f.Id);
            EscortOrders.ManageAi(s,f.Id);
            foreach(var r in s.Relations.Where(r=>r.A==f.Id||r.B==f.Id))
            {
                int other=r.A==f.Id?r.B:r.A;if(!s.Cities.Any(c=>c.Owner==other))continue;
                if(s.Turn%8==0)r.Trust=Math.Clamp(r.Trust+(r.Trade?2:r.War?-2:0),-100,100);
                if(s.Turn%16==0&&!r.War&&!r.Trade&&f.Id==3&&s.Turn>=r.TruceUntil)r.Trust=Math.Max(-100,r.Trust-3);
                if(other>0&&!r.Trade&&!r.War&&r.Trust>=10&&s.Turn%15==0){r.Trade=true;r.Trust=Math.Min(100,r.Trust+3);}
                if(!r.War&&s.Turn>25&&s.Turn>=r.TruceUntil&&r.Trust< -15&&Military(s,f.Id)>Military(s,other)*1.3+15&&s.Next(30)==0)
                {
                    r.War=true;r.Trade=false;r.Trust=Math.Max(-100,r.Trust-20);DiplomaticCouncil.AddGrievance(s,other,f.Id,40);
                    WarCouncil.Start(s,f.Id,other,WarReason.Conquest,WarCouncil.DefaultTarget(s,f.Id,other,WarReason.Conquest)?.Id);
                    foreach(var third in s.Factions.Where(n=>n.Id!=f.Id&&n.Id!=other)){var rr=s.Relation(f.Id,third.Id);rr.Trust=Math.Max(-100,rr.Trust-12);}
                    DiplomaticCouncil.Cancel(s,r,"Wojna przerwała poselstwo.");s.InvalidateLogistics();s.Log($"{f.Name} wypowiada wojnę państwu {s.Factions[other].Name}. Ofiara zyskuje 40 punktów krzywd, pozostałe państwa tracą 12 zaufania do napastnika.","dyplomacja");
                }
                if(r.War&&other==0&&s.Turn%5==0)PeaceCouncil.MaybeOffer(s,f.Id,other);
                else if(r.War&&other>0&&f.WarWeariness>65&&s.Turn%10==0)PeaceCouncil.AiArmistice(s,r);
            }
            foreach(var u in s.Units.Where(u=>u.Owner==f.Id).ToArray())
            {
                if(u.EscortCaravanId!=null)continue;
                if(u.Kind!=UnitKind.Settler&&Supplied(s,u)&&(u.Health<45||u.Order==MilitaryOrder.Recover))
                {if(u.Order!=MilitaryOrder.Recover)SetMilitaryOrder(s,u.Id,MilitaryOrder.Recover,f.Id);continue;}
                if(u.Health<50&&!Supplied(s,u)&&u.Kind is not (UnitKind.Settler or UnitKind.Scout))
                {AiStep(s,u,cities.OrderBy(c=>World.Distance(s.Tiles[u.Tile],s.Tiles[c.Tile])).First().Tile);continue;}
                if(u.Kind==UnitKind.Settler)
                {
                    var site=s.Tiles.Where(t=>t.Passable&&(t.Owner<0||t.Owner==f.Id)&&s.Cities.All(c=>World.Distance(t,s.Tiles[c.Tile])>=4)&&cities.Any(c=>World.Distance(t,s.Tiles[c.Tile])<9)).OrderByDescending(t=>TileYields(t,f).Food+TileYields(t,f).Production*.5-World.Distance(t,s.Tiles[u.Tile])*.8).FirstOrDefault();
                    if(site!=null){if(u.Tile==site.Id)FoundCity(s,u.Id);else AiStep(s,u,site.Id);}continue;
                }
                var enemy=s.Units.Where(e=>AtWar(s,u.Owner,e.Owner)&&World.Distance(s.Tiles[u.Tile],s.Tiles[e.Tile])<=(u.Kind is UnitKind.Archer or UnitKind.Catapult?2:1)).OrderBy(e=>e.Health).FirstOrDefault();
                if(enemy!=null){Move(s,u.Id,enemy.Tile);continue;}
                var targetCity=s.Cities.Where(c=>AtWar(s,f.Id,c.Owner)).OrderBy(c=>World.Distance(s.Tiles[u.Tile],s.Tiles[c.Tile])).FirstOrDefault();
                if(targetCity!=null&&u.Kind!=UnitKind.Scout){if(World.Distance(s.Tiles[u.Tile],s.Tiles[targetCity.Tile])<=(u.Kind is UnitKind.Archer or UnitKind.Catapult?2:1))Move(s,u.Id,targetCity.Tile);else AiStep(s,u,targetCity.Tile);}
                else if(u.Kind==UnitKind.Scout){var ns=World.Neighbors(s,u.Tile).Where(t=>CanEnter(s,u,t)).ToArray();if(ns.Length>0)Move(s,u.Id,ns[s.Next(ns.Length)].Id);}
                else if(!cities.Any(c=>World.Distance(s.Tiles[u.Tile],s.Tiles[c.Tile])<=2))AiStep(s,u,cities.OrderBy(c=>World.Distance(s.Tiles[u.Tile],s.Tiles[c.Tile])).First().Tile);
                else if(u.Order!=MilitaryOrder.Fortify)SetMilitaryOrder(s,u.Id,MilitaryOrder.Fortify,f.Id);
            }
        }
    }
    private static void AiStep(GameState s,Unit u,int target)
    {
        bool enterTarget=CanEnter(s,u,s.Tiles[target]);
        var search=enterTarget?Navigation.ForUnit(s,u,target:target):Navigation.SearchFrom(s,u.Tile,
            tile=>CanEnter(s,u,tile),(from,to)=>Navigation.MovementCost(s,from,to),
            reached:tile=>World.Distance(s.Tiles[tile],s.Tiles[target])==1);
        int goal=target;
        if(!enterTarget)
            goal=search.Costs.Where(pair=>World.Distance(s.Tiles[pair.Key],s.Tiles[target])==1)
                .OrderBy(pair=>pair.Value).Select(pair=>pair.Key).DefaultIfEmpty(-1).First();
        if(goal<0)return;
        foreach(int next in search.PathTo(goal).Skip(1))
        {
            if(Navigation.MovementCost(s,u.Tile,next)>u.Actions)break;
            EnterTile(s,u,next);
        }
    }
    public static double Score(GameState s,int owner) => s.Cities.Where(c=>c.Owner==owner).Sum(c=>c.Population*8+c.Buildings.Count*5)+s.Factions[owner].Technologies.Count*18+s.Factions[owner].Culture*.08+s.Factions[owner].Prestige*2+TreasuryLoans.NetWealth(s,owner)*.03;
    public static void CheckEnding(GameState s)
    {
        WarCouncil.FinishEliminated(s);
        if(s.Ending!=null)return;
        if(!s.Cities.Any(c=>c.Owner==0)){s.Ending="Twoje ostatnie miasto upadło. Marchie przetrwają w opowieściach rozproszonych mieszkańców.";return;}
        foreach(var f in s.Factions)
        {
            string? type=s.Cities.Any(c=>c.Owner==f.Id&&c.Buildings.Contains("atlas"))?"naukowe":s.Cities.Count>3&&s.Cities.All(c=>c.Owner==f.Id)?"przez zjednoczenie":f.Culture>=Content.CultureVictory&&f.Prestige>=Content.PrestigeVictory?"kulturowe":null;
            if(type!=null){s.Ending=$"{f.Name}: zwycięstwo {type}. Historia zapamięta podjęte decyzje.";return;}
        }
        if(s.Turn>s.MaxTurns){var winner=s.Factions.MaxBy(f=>Score(s,f.Id))!;s.Ending=$"Koniec epoki. {winner.Name} zwycięża dorobkiem: {Score(s,winner.Id):0} punktów.";}
    }
}
