namespace Crown.Core;

public static class Narrative
{
    public static readonly NarrativeEvent[] Events =
    [
        new("founding","Mira Weles","Kanclerka Marchii","Na czym zbudujemy państwo?","Przybyli tu rolnicy bez ziemi, wojownicy bez pana i rodziny szukające domu. Każdy widzi w Tobie inną obietnicę. Nim postawimy pierwszy kamień rady, powiedz: komu ma służyć Wysogród?",
        [new("Ziemia należy do tych, którzy ją uprawiają.","Lud +16, szlachta -10, skarb -30. Za 8 tur: +70 złota z pierwszych zbiorów.",-30,3,16,-10,0,4,"land_charter",8,"Pierwsi wolni gospodarze spłacają dług wdzięczności. Karta ziemska stała się fundamentem wspólnoty.",70,2,5),
         new("Oddajmy straż nad doliną rodom wojowników.","Szlachta +16, lud -8, +45 złota. Za 10 tur rody zażądają przywilejów.",45,2,-8,16,0,3,"noble_charter",10,"Rody zbrojne upominają się o obiecany udział we władzy. Ich przywileje niepokoją mieszkańców.",0,-5,-8,5),
         new("Niech radę wybierają ludzie wiedzy.","Uczeni +18, skarb -45. Za 10 tur: +12 prestiżu i +5 stabilności.",-45,0,-3,-5,18,0,"learned_charter",10,"Szkoła rady wykształciła pierwszych urzędników. Spory o granice pól rozstrzyga teraz zapisane prawo.",0,5,0,0,5,12)]),
        new("grain","Olen Rusk","Przedstawiciel gospodarzy","Ziarno na jutro","W dolnych spichlerzach pojawiła się pleśń. Kupcy oferują zdrowe ziarno, ale cena jest wysoka. Jeśli rozdamy rezerwy, ludzie przetrwają zimę. Jeśli zachowamy je na siew, wiosna będzie obfitsza — dla tych, którzy dotrwają.",
        [new("Otwórzcie magazyny. Sprowadzimy nowe ziarno.","-55 złota, lud +12, stabilność +5.",-55,5,12,-2),new("Zachowajcie ziarno na siew.","Lud -12, stabilność -6. Za 6 tur: +95 złota.",0,-6,-12,0,0,0,"hard_winter",6,"Ocalone ziarno dało obfity zbiór. Rodziny, które straciły bliskich w zimie, pamiętają jednak cenę tej decyzji.",95,0,-3),new("Rody oddadzą część swoich zapasów.","Szlachta -14, lud +8, stabilność +2.",0,2,8,-14)]),
        new("refugees","Ada z Brzegów","Przywódczyni uchodźców","Ludzie zza rzeki","Nie chcemy waszych pałaców. Dajcie nam grunt na skraju lasu. Potrafimy stawiać groble, kuć narzędzia, piec chleb. Za nami przyjdą inni — a z nimi ci, którzy wypędzili nas z domów.",
        [new("Przyjmujemy was jako wolnych mieszkańców.","-65 złota, lud +8, uczeni +4. Za 7 tur: +100 złota i +6 prestiżu.",-65,0,8,-6,4,0,"shelter",7,"Nowe warsztaty nad rzeką tętnią życiem. Przybysze wnieśli do Marchii kunszt i nowe opowieści.",100,3,4,0,0,6),new("Przyjmą was rody. Odpracujecie ochronę.","+35 złota, szlachta +10, lud -10. Za 9 tur: niepokoje.",35,0,-10,10,0,0,"bonded",9,"Związani długiem przybysze opuścili warsztaty. Wieść o pracy przymusowej obniża zaufanie do rady.",0,-8,-8),new("Zamknijcie przeprawę.","Stabilność +3, lud -4, prestiż -6.",0,3,-4,2,0,-6)]),
        new("river","Mira Weles","Kanclerka Marchii","Rzeka nie zna granic","Rolnicy proszą o kanał. Kupcy o port rzeczny. Obie strony obiecują, że inwestycja zwróci się w ciągu pokolenia. Skarb wystarczy na jedno przedsięwzięcie.",
        [new("Najpierw nawodnimy pola.","-70 złota, lud +10. Za 8 tur: +130 złota.",-70,2,10,0,0,0,"canals",8,"Kanały ożywiły suche pola. Nadwyżka plonów trafia na miejskie targi.",130,3),new("Zbudujemy przystań kupiecką.","-50 złota, szlachta +8. Za 6 tur: +100 złota, +5 prestiżu.",-50,0,-2,8,0,0,"river_port",6,"Pierwsza flotylla kupiecka wpłynęła do nowej przystani. Wysogród zyskał miejsce na mapach handlowych.",100,0,0,0,0,5),new("Zostawmy skarb na trudniejsze czasy.","+20 złota, lud -5, uczeni -3.",20,0,-5,0,-3)]),
        new("oath","Dagan z Kharu","Wódz klanów","Cena słowa","Nasi kupcy wrócili bez wozów. Wasi poborcy twierdzą, że nie zapłacili cła. Moi ludzie twierdzą, że wasze cło nie ma końca. Oddajcie towary, a zapomnę o sprawie. Albo pokażcie, jak bardzo wierzycie swoim strażnikom.",
        [new("Oddamy towary i ukarzemy poborców.","-60 złota, lud +4, szlachta -8. Trwała pamięć ugody: relacje z Kharem +20.",-60,2,4,-8,0,2,"khar_reconciled"),new("Nie ustąpimy pod groźbą.","Szlachta +10, prestiż +5. Relacje z Kharem -25.",0,0,-3,10,0,5,"khar_insulted"),new("Niech sprawę zbada wspólna rada.","-25 złota, uczeni +10. Relacje z Kharem +8.",-25,1,0,-2,10,2,"khar_council")]),
        new("school","Seren Ata","Pierwsza archiwistka","Kto może czytać?","Uczniowie przepisują prawa w salach świątynnych. Córki rzemieślników stoją pod drzwiami. Kapłani mówią, że wiedza wymaga porządku. Ja pytam: czyj porządek utrwalamy?",
        [new("Szkoła będzie otwarta dla każdego.","-80 złota, uczeni +16, szlachta -8. Za 12 tur: +20 prestiżu.",-80,0,6,-8,16,0,"open_schools",12,"Dzieci rzemieślników objęły urzędy. Ich wiedza zmienia sposób zarządzania państwem.",40,4,0,0,6,20),new("Rody sfinansują własne szkoły.","+30 złota, szlachta +12, uczeni -8.",30,1,-4,12,-8,1),new("Najpierw nauczmy pisać urzędników.","-30 złota, uczeni +6, stabilność +4.",-30,4,0,0,6)]),
        new("mines","Baro Kell","Mistrz kopalni","Głosy spod ziemi","Strop osunął się po nocnej zmianie. Pod kamieniem zostało siedmiu ludzi. Możemy zamknąć szyb i wypłacić rodzinom zapomogi. Możemy też kopać dalej, ryzykując następnych górników.",
        [new("Zatrzymajcie wydobycie. Ratujemy wszystkich.","-70 złota, lud +15, szlachta -5, stabilność +3.",-70,3,15,-5,0,3,"mine_safety"),new("Odszkodowania i nowy szyb.","-35 złota, lud -4, uczeni +3. Za 6 tur: +80 złota.",-35,0,-4,0,3,0,null,6,"Nowy szyb ruszył. W kamieniu przy wejściu wyryto siedem imion.",80),new("Nie możemy wstrzymać dostaw dla armii.","+50 złota, lud -16, stabilność -7.",50,-7,-16,8)]),
        new("succession","Mira Weles","Kanclerka Marchii","Prawo po Tobie","Rody chcą wiedzieć, kto będzie rządził po Twojej śmierci. Zgromadzenie domaga się wyboru. Straż obawia się chaosu. Prawo sukcesji przetrwa dłużej niż ktokolwiek w tej sali.",
        [new("Następcę wybierze zgromadzenie miast.","Lud +12, uczeni +8, szlachta -14. Za 15 tur: +8 stabilności.",0,0,12,-14,8,5,"elective",15,"Pierwszy wybór zarządcy prowincji odbył się bez rozlewu krwi. Zgromadzenie uznało wspólne prawo.",0,8),new("Władza pozostanie w jednym rodzie.","Szlachta +15, lud -7, stabilność +6.",0,6,-7,15,-5,3,"dynastic"),new("Każdy następca musi zdać próbę urzędu.","-45 złota, uczeni +16, szlachta -7, prestiż +8.",-45,3,0,-7,16,8,"merit")]),
        new("plague","Nela Orr","Medyczka rady","Gorączka na trakcie","W gospodach przy szlaku chorują całe rodziny. Zamknięcie targu uratuje część miasta, lecz kupcy stracą zarobek. Potrzebuję ludzi, czystej wody i zgody na wejście do domów możnych.",
        [new("Zamknąć targ. Lekarze otrzymają środki.","-100 złota, lud +12, uczeni +8, szlachta -8.",-100,4,12,-8,8,4,"quarantine"),new("Utrzymajcie handel, lecz leczcie chorych.","-45 złota, stabilność -6, lud -6, uczeni +4.",-45,-6,-6,5,4),new("Każda rodzina odpowiada za siebie.","Stabilność -14, lud -20, szlachta +6.",0,-14,-20,6)]),
        new("strike","Olen Rusk","Przedstawiciel gospodarzy","Puste place, pełne gniewu","Zebraliśmy się bez broni. Daniny rosną, a dzieci jedzą mniej. Nie wystarczy kolejna przemowa. Potrzebujemy znaku, że rada pamięta, kto utrzymuje jej mury.",
        [new("Zapłacimy za żywność i umorzymy długi.","-90 złota, lud +20, stabilność +10, szlachta -8.",-90,10,20,-8),new("Zwołamy rokowania z przedstawicielami.","-30 złota, lud +10, szlachta -4. Za 5 tur: +5 stabilności.",-30,2,10,-4,3,0,null,5,"Przedstawiciele miast podpisali porozumienie. Spór stał się precedensem dla przyszłych rad.",0,5),new("Straż przywróci porządek.","Lud -18, szlachta +12, stabilność +4. Za 8 tur: -12 stabilności.",0,4,-18,12,0,-5,"repressed",8,"Rodziny zatrzymanych tworzą tajne zgromadzenia. Milczenie placów okazało się krótkie.",0,-12)]),
        new("frontier","Kapitan Eren","Dowódca straży","Dokąd sięga nasze prawo?","Nowe osady żyją daleko od rady. Poborcy wracają z pustymi wozami. Mogę postawić garnizony, ale mój zastępca mówi, że lepiej pozwolić im samym wybierać zarządców.",
        [new("Dajmy prowincjom własne zgromadzenia.","Lud +12, szlachta -8, stabilność +6, -50 złota.",-50,6,12,-8,3,4,"autonomy"),new("Wyślijcie garnizony i urzędników.","-70 złota, szlachta +10, lud -6, stabilność +3.",-70,3,-6,10,0,3,"centralized"),new("Niech lokalne rody zbierają daniny.","+85 złota, szlachta +12, lud -12. Za 10 tur: -7 stabilności.",85,0,-12,12,0,0,null,10,"Lokalni zarządcy ukrywają część danin. Państwo zyskało pieniądze, ale straciło kontrolę.",0,-7)]),
        new("war","Kapitan Eren","Dowódca straży","Listy z frontu","Ludzie walczą dzielnie. Ale każda długa zima w obozie zabiera więcej niż bitwa. Rody proponują nowy pobór. Ja proszę o zapasy i możliwość odesłania rannych do domów.",
        [new("Sfinansujemy zaopatrzenie i opiekę.","-90 złota, lud +10, stabilność +8.",-90,8,10,0,2,4,"veterans"),new("Ogłoście powszechny pobór.","+50 złota, szlachta +12, lud -16, stabilność -5.",50,-5,-16,12,0,2,"conscription"),new("Nagrodzimy rodziny poległych ziemią.","-40 złota, lud +12, szlachta -12. Za 10 tur: +50 złota.",-40,3,12,-12,0,3,null,10,"Weterani osiedlili się na granicy. Ich gospodarstwa utrzymują dawnych towarzyszy broni.",50)]),
        new("land_echo","Ada z Brzegów","Starsza zgromadzenia","Słowo wyryte w ziemi","Nasi ojcowie otrzymali wolną ziemię na mocy Twojej karty. Teraz rody kupują długi i przejmują gospodarstwa. Litera prawa nadal istnieje. Czy jego obietnica też?",
        [new("Karta ma pierwszeństwo przed długiem.","-75 złota, lud +18, szlachta -16, prestiż +12.",-75,4,18,-16,3,12,"charter_upheld"),new("Własność musi podlegać umowom.","+80 złota, szlachta +14, lud -20, stabilność -8.",80,-8,-20,14,0,-8,"charter_broken"),new("Państwo wykupi ziemię i odda ją dzierżawcom.","-120 złota, lud +10, szlachta +3, stabilność +5.",-120,5,10,3,0,6)]),
        new("noble_echo","Radan Veyr","Marszałek rodów","Dług wobec miecza","Nasi przodkowie osłonili Waszą radę, gdy była tylko drewnianą ławą. Obiecano nam głos. Dziś proponujemy stałe miejsca w zgromadzeniu i prawo weta wobec podatków.",
        [new("Otrzymacie miejsca, ale bez prawa weta.","Szlachta +8, lud -5, stabilność +4.",0,4,-5,8,0,3),new("Obietnica jest ważniejsza niż wygoda rady.","Szlachta +20, lud -15, uczeni -8. Za 12 tur: -100 złota.",0,3,-15,20,-8,6,"noble_veto",12,"Weto rodów zatrzymało reformę podatkową. Skarb pokrywa zobowiązania ze starych rezerw.",-100),new("Państwo nie należy do tych, którzy noszą miecz.","Szlachta -22, lud +12, stabilność -8, prestiż +8.",0,-8,12,-22,4,8,"nobles_denied")]),
        new("press","Seren Ata","Archiwistka rady","Prawo do niewygodnych słów","Drukarze sprzedają pamflet oskarżający poborców o kradzież. Są tam kłamstwa, ale są też nazwiska, które znam. Zakaz zatrzyma druk. Nie wiem, czy zatrzyma gniew.",
        [new("Niech drukują. Zbadamy oskarżenia.","-60 złota, uczeni +18, lud +10, szlachta -10.",-60,3,10,-10,18,10,"free_press"),new("Wymagajmy podpisu pod każdą publikacją.","Uczeni -4, lud +3, stabilność +4.",0,4,3,2,-4),new("Zamknijcie drukarnie.","Uczeni -22, szlachta +10, stabilność +3. Za 8 tur: -8 stabilności.",0,3,-5,10,-22,-8,"censorship",8,"Zakazane pisma krążą w odpisach. Rada straciła możliwość publicznej odpowiedzi.",0,-8)]),
        new("smoke","Nela Orr","Medyczka rady","Cena nowych pieców","Miasto nigdy nie produkowało tyle żelaza. Nigdy też tyle dzieci nie kaszlało zimą. Właściciele pieców grożą zamknięciem warsztatów, jeśli ograniczymy dym.",
        [new("Czysta woda i filtry są obowiązkiem warsztatów.","-110 złota, lud +16, szlachta -10, uczeni +8, stabilność +5.",-110,5,16,-10,8,10,"clean_industry"),new("Państwo współfinansuje przebudowę.","-160 złota, lud +10, szlachta +6, uczeni +10.",-160,4,10,6,10,8),new("Produkcja jest warunkiem naszej niezależności.","+140 złota, lud -18, uczeni -10, stabilność -8.",140,-8,-18,10,-10,3,"smoke")]),
        new("legacy","Mira Weles","Kanclerka Marchii","Co zostanie po nas?","Na murach nowego zgromadzenia jest miejsce na jedno zdanie. Rzeźbiarze proszą o Twoje imię. Ja myślę o wszystkich, których głosów nie usłyszy następne pokolenie. Jaką historię chcemy pozostawić?",
        [new("Zapiszcie również nasze błędy.","Uczeni +14, lud +10, szlachta -6, prestiż +30.",0,5,10,-6,14,30,"honest_history"),new("Zapiszcie imiona mieszkańców, nie władców.","-70 złota, lud +18, prestiż +25.",-70,5,18,-4,5,25,"people_history"),new("Zapiszcie zwycięstwa, które nas ocaliły.","Szlachta +16, prestiż +20, uczeni -6.",0,4,0,16,-6,20,"victor_history")])
    ];
    public static readonly NarrativeEvent[] Recurring =
    [
        new("petition_water","Olen Rusk","Przedstawiciel gospodarzy","Kiedy nie pada","W studniach widać dno. Jeśli zatrzymamy wodę w mieście, stracimy część ogrodów poza murami. Jeśli rozdzielimy ją równo, wszyscy dostaną mniej. Ludzie przypomną sobie tę decyzję przy następnym poborze danin.",
        [new("Sprowadzimy wodę na koszt rady.","-85 złota, lud +10, stabilność +4.",-85,4,10,0,2),new("Równy przydział dla miasta i prowincji.","Lud +4, szlachta -6, stabilność +1.",0,1,4,-6),new("Chrońmy warsztaty i centrum miasta.","+45 złota, lud -9, szlachta +5, stabilność -3.",45,-3,-9,5)]),
        new("petition_accounts","Mira Weles","Kanclerka Marchii","Nie zgadzają się rachunki","W spisie podatkowym zniknęło kilka wsi. Zarządca twierdzi, że spłonęły. Posłowie wrócili z wieścią o nowych spichlerzach i ucztach na jego dworze. To stary sojusznik rady. Dochodzenie ujawni więcej niż jeden rachunek.",
        [new("Przeprowadzimy jawne dochodzenie.","-45 złota, uczeni +8, lud +6, szlachta -9.",-45,3,6,-9,8,3),new("Odda podatek i zachowa urząd.","+70 złota, szlachta +4, lud -7, uczeni -6.",70,-2,-7,4,-6),new("Odwołamy zarządcę. Nie będzie procesu.","Stabilność +2, szlachta -5, uczeni -3.",0,2,0,-5,-3)]),
        new("petition_fair","Irena z Talassu","Posłanka Ligi","Targ na wspólnej ziemi","Kupcy obu państw chcą urządzić jarmark przy przeprawie. Potrzebują wspólnej miary i gwarancji, że nie zmienicie cła, kiedy przyjadą wozy. Część rodów widzi w umowie utratę dochodów.",
        [new("Zagwarantujemy wolny handel.","-60 złota, lud +5, szlachta -5. Za 5 tur: +130 złota i +5 prestiżu.",-60,1,5,-5,0,0,null,5,"Jarmark zakończył się sukcesem. Ludzie wrócili z towarami i znajomościami po drugiej stronie granicy.",130,0,0,0,0,5),new("Rody opłacą straż i zachowają cło.","+30 złota, szlachta +7, lud -4.",30,0,-4,7),new("Przeprawa pozostanie pod kontrolą rady.","Uczeni +4, szlachta -3, prestiż +2.",0,1,0,-3,4,2)]),
        new("petition_service","Kapitan Eren","Dowódca straży","Po latach służby","Do rady przyszli ludzie, którzy budowali drogi i strzegli granic. Nie proszą o pomniki. Chcą opieki nad rannymi i pewności, że ich dzieci nie odziedziczą długu wobec własnego państwa.",
        [new("Ustanowimy fundusz opieki.","-95 złota, lud +12, stabilność +5, prestiż +4.",-95,5,12,-3,0,4),new("Rody obejmą rodziny patronatem.","Szlachta +8, lud -4. Za 8 tur: -4 stabilności.",0,0,-4,8,0,0,null,8,"Patroni zażądali od rodzin służby w zamian za opiekę. Dawni strażnicy czują, że ich poświęcenie stało się długiem.",0,-4),new("Przyznamy zwolnienie z daniny.","-35 złota, lud +5, stabilność +2.",-35,2,5)])
    ];
    public static readonly NarrativeEvent InfrastructureEvent = new("caravan_compact", "Mira Weles", "Kanclerka Marchii",
        "Komu służy wspólny trakt?", "Wozy jadą już między naszymi miastami. Kupcy proszą o wolny przejazd, rody chcą pobierać myto w zamian za straż, a dowódcy proponują stałe patrole rady. Droga połączyła ziemie. Teraz musimy zdecydować, jakie prawo będzie nią podróżować.",
        [new("Wolny przejazd i wymiana wiedzy.", "Połączone miasta otrzymają dodatkowy punkt nauki.", -70, 1, 8, -6, 3, 5, "open_roads"),
         new("Rody utrzymają straż w zamian za myto.", "Połączone miasta otrzymają dodatkowe dwa punkty bazowego złota.", 45, 2, -6, 12, 0, 3, "road_tolls"),
         new("Niech szlaki zabezpieczą patrole państwa.", "Zasięg dostaw zwiększy się o jeden punkt.", -100, 5, 6, -2, 0, 5, "road_patrols")]);
    public static readonly NarrativeEvent PeaceEvent=new("peace_legacy","Mira Weles","Kanclerka Marchii","Kiedy milkną miecze",
        "Posłowie złożyli pieczęcie, ale rodziny wciąż czekają na powrót bliskich. Kupcy pytają o drogi, weterani o opiekę, a rody chcą pomników zwycięstwa. Pokój daje nam czas. Komu go poświęcimy?",
        [new("Odbudujemy domy i wesprzemy rodziny po obu stronach.","Odbudowa i pojednanie.",Gold:-70,Stability:4,Commons:10,Nobles:-4,Prestige:6,Delay:8,FutureText:"Fundusz odbudowy przywraca warsztaty i zaufanie rodzin do rady.",FutureGold:100,FutureCommons:4),
         new("Utrwalimy pamięć wszystkich ofiar tej wojny.","Wspólna pamięć kosztem dumy rodów.",Gold:-35,Stability:2,Commons:4,Nobles:-5,Scholars:8,Prestige:12),
         new("Ogłosimy chwałę armii i zwycięskie czyny rodów.","Prestiż i poparcie dowódców kosztem społecznym.",Commons:-6,Nobles:10,Scholars:-4,Prestige:8)]);
    public static IEnumerable<NarrativeEvent> All => Events.Concat(Recurring).Append(InfrastructureEvent).Append(PeaceEvent).Concat(ProvincialEvents.Events);
    public static NarrativeEvent Get(string id) => All.First(e=>e.Id==id);
    public static string Effects(Choice c,string eventId,int choice)
    {
        static string Deltas(int gold,int stability,int commons,int nobles,int scholars,int prestige)
        {
            (string Label,int Value)[] values=[("Złoto",gold),("Stabilność",stability),("Lud",commons),("Rody",nobles),("Uczeni",scholars),("Prestiż",prestige)];
            return string.Join(" · ",values.Where(v=>v.Value!=0).Select(v=>$"{v.Label} {v.Value:+0;-0}"));
        }
        string result=Deltas(c.Gold,c.Stability,c.Commons,c.Nobles,c.Scholars,c.Prestige);
        if(eventId=="oath")result+=$" · Relacje z Kharem {(choice==0?20:choice==1?-25:8):+0;-0}";
        if(c.Delay>0)result+=$"\nZa {c.Delay} tur: "+Deltas(c.FutureGold,c.FutureStability,c.FutureCommons,c.FutureNobles,c.FutureScholars,c.FuturePrestige);
        if(eventId=="caravan_compact")result+="\nTrwale: "+(choice==0?"połączone miasta +1 nauki":choice==1?"połączone miasta +2 bazowego złota":"zasięg dostaw +1");
        if(ProvincialEvents.IsLocal(eventId))result+="\n"+ProvincialEvents.Describe(eventId,choice);
        return result;
    }
    public static void Schedule(GameState s)
    {
        if(s.PendingEvent!=null || s.Turn%5!=0) return;
        s.PendingCityId=null;
        if(s.Player.Flags.Contains("negotiated_peace")&&!s.SeenEvents.Contains("peace_legacy")){s.PendingEvent="peace_legacy";return;}
        if(s.Turn>=5&&ProvincialEvents.Schedule(s))return;
        string? id = s.Player.Commons<33 ? "strike" : s.Relations.Any(r=>(r.A==0||r.B==0)&&r.War) ? "war" :
            s.Player.Flags.Contains("land_charter") && s.Turn>=35 ? "land_echo" :
            s.Player.Flags.Contains("noble_charter") && s.Turn>=35 ? "noble_echo" : null;
        if(id!=null && !s.SeenEvents.Contains(id)) { s.PendingEvent=id; return; }
        if(s.Turn>=10&&!s.SeenEvents.Contains("caravan_compact")&&s.Tiles.Count(t=>t.Road&&t.RoadOwner==0)>=4&&Logistics.For(s,0).ConnectedCities.Count>=2)
        {s.PendingEvent="caravan_compact";return;}
        (int,string)[] timeline=[(5,"grain"),(10,"refugees"),(15,"river"),(20,"oath"),(25,"school"),(30,"mines"),(40,"succession"),(50,"plague"),(60,"frontier"),(85,"press"),(110,"smoke"),(145,"legacy")];
        s.PendingEvent=timeline.FirstOrDefault(e=>e.Item1<=s.Turn&&!s.SeenEvents.Contains(e.Item2)&&
            (e.Item2!="press"||s.Player.Technologies.Contains("printing"))&&(e.Item2!="smoke"||s.Player.Technologies.Contains("industry"))).Item2;
        if(s.PendingEvent==null&&s.Turn%10==0)s.PendingEvent=Recurring[s.Next(Recurring.Length)].Id;
    }
    public static Result Resolve(GameState s,int choice)
    {
        if(s.PendingEvent==null) return Result.Fail("Rada nie ma nowej sprawy.");
        var e=ProvincialEvents.Current(s); if(choice<0||choice>=e.Choices.Length) return Result.Fail("Nieznana odpowiedź.");
        City? context=null;
        if(ProvincialEvents.IsLocal(e.Id))
        {
            context=s.Cities.FirstOrDefault(city=>city.Id==s.PendingCityId&&city.Owner==0);
            if(context==null||context.ResolvedIssues.Contains(e.Id))return Result.Fail("Ta sprawa nie ma już prawidłowego kontekstu prowincji.");
        }
        var c=e.Choices[choice]; var f=s.Player;
        if(f.Gold+c.Gold<0) return Result.Fail("Skarb nie wystarczy na tę obietnicę.");
        f.Gold+=c.Gold; f.Stability+=c.Stability; f.Commons+=c.Commons; f.Nobles+=c.Nobles; f.Scholars+=c.Scholars; f.Prestige+=c.Prestige;
        if(c.Flag!=null) f.Flags.Add(c.Flag);
        if(context!=null)ProvincialEvents.Apply(s,context,e.Id,choice);
        if(e.Id=="oath") s.Relation(0,3).Trust=Math.Clamp(s.Relation(0,3).Trust+(choice==0?20:choice==1?-25:8),-100,100);
        if(c.Delay>0) s.Deferred.Add(new(){Due=s.Turn+c.Delay,Faction=0,Text=c.FutureText??"Obietnica przynosi skutki.",Gold=c.FutureGold,Stability=c.FutureStability,Commons=c.FutureCommons,Nobles=c.FutureNobles,Scholars=c.FutureScholars,Prestige=c.FuturePrestige});
        Engine.Clamp(f); s.SeenEvents.Add(e.Id); s.PendingEvent=null;s.PendingCityId=null;
        s.Log($"{e.Title} — „{c.Text}” {Effects(c,e.Id,choice)}","decyzja"); return Result.Ok("Decyzja została zapisana w kronice.");
    }
}
