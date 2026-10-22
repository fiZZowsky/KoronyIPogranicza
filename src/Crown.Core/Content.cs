namespace Crown.Core;

public static class Content
{
    public const int CultureVictory=30000;
    public const int PrestigeVictory=800;
    public static readonly Technology[] Technologies =
    [
        new("agriculture", "Rolnictwo", 32, [], "Farmy, spichlerze i stabilne plony. +1 żywności z każdej farmy."),
        new("pottery", "Garncarstwo", 40, [], "Magazynowanie i wymiana. Odblokowuje targowisko i budowę dróg."),
        new("mining", "Górnictwo", 44, [], "Kopalnie, kamieniołomy i dostęp do rudy."),
        new("writing", "Pismo", 65, ["pottery"], "Biblioteki. +15% nauki w całym państwie."),
        new("bronze", "Metalurgia brązu", 68, ["mining"], "Koszary. +3 siły wojsk lądowych."),
        new("irrigation", "Irygacja", 85, ["agriculture"], "+2 żywności w miastach nad rzeką."),
        new("masonry", "Kamieniarstwo", 80, ["mining"], "Mury i akwedukty."),
        new("riding", "Jeździectwo", 100, ["bronze","agriculture"], "Kawaleria; wymaga koni w granicach państwa."),
        new("trade", "Szlaki handlowe", 115, ["writing"], "Placówki handlowe. +25% dochodu i +2 do zasięgu dostaw."),
        new("philosophy", "Filozofia", 135, ["writing"], "Świątynie i instytucje. +20% kultury."),
        new("iron", "Żelazo", 155, ["bronze","masonry"], "+5 siły; dodatkowe +3 z własnym złożem żelaza."),
        new("engineering", "Inżynieria", 180, ["masonry","writing"], "Katapulty, warsztaty i +2 do zasięgu dostaw."),
        new("currency", "Moneta", 200, ["trade"], "Banki i wspólny rynek. +2 złota z miasta."),
        new("civilservice", "Służba publiczna", 220, ["philosophy","trade"], "Sądy; -25% kosztów utrzymania miast i +2 miejsca mieszkalne w każdym mieście."),
        new("education", "Uniwersytety", 260, ["philosophy","engineering"], "Uniwersytety; +20% nauki."),
        new("steel", "Stal", 290, ["iron","engineering"], "+7 siły wszystkim wojskom."),
        new("printing", "Druk", 320, ["education"], "+30% kultury; narodowa biblioteka."),
        new("navigation", "Nawigacja", 310, ["currency","engineering"], "Szlaki zamorskie: +8 złota za każdy traktat handlowy."),
        new("medicine", "Medycyna", 380, ["education","irrigation"], "+25% żywności, szybsze leczenie i +3 do pojemności opieki w każdym mieście."),
        new("industry", "Przemysł", 450, ["steel","currency"], "Fabryki i +25% produkcji."),
        new("constitution", "Konstytucja", 460, ["civilservice","printing"], "+10 do docelowej stabilności; parlament."),
        new("electricity", "Elektryczność", 560, ["industry","education"], "+30% nauki i produkcji."),
        new("ecology", "Ekologia", 620, ["medicine","industry"], "Przywraca równowagę: +10 stabilności, +15% żywności."),
        new("observatory", "Wielkie obserwatorium", 800, ["electricity","navigation","printing"], "Odblokowuje projekt zwycięstwa naukowego: Atlas świata.")
    ];
    public static readonly Project[] Projects =
    [
        new("granary", "Spichlerz", 45, "agriculture", "+4 żywności. Zapas chroni ludność w zimie."),
        new("market", "Targowisko", 60, "pottery", "+5 złota, +1 kultury."),
        new("library", "Biblioteka", 70, "writing", "+5 nauki."),
        new("barracks", "Koszary", 65, "bronze", "Nowi żołnierze zyskują 2 doświadczenia."),
        new("walls", "Mury miejskie", 80, "masonry", "+10 obrony miasta."),
        new("aqueduct", "Akwedukt", 95, "masonry", "+3 żywności, +1 nauki, +2 miejsca mieszkalne i +4 opieki."),
        new("temple", "Dom zgromadzeń", 85, "philosophy", "+5 kultury. +2 do docelowej stabilności."),
        new("workshop", "Warsztat", 100, "engineering", "+6 produkcji."),
        new("bank", "Dom bankierski", 150, "currency", "+9 złota."),
        new("court", "Sąd miejski", 140, "civilservice", "+4 do docelowej stabilności. Zmniejsza niepokoje."),
        new("university", "Uniwersytet", 190, "education", "+12 nauki."),
        new("archive", "Biblioteka narodowa", 220, "printing", "+10 kultury, +3 nauki."),
        new("factory", "Manufaktura", 260, "industry", "+14 produkcji, -3 do docelowej stabilności."),
        new("parliament", "Parlament", 280, "constitution", "+12 kultury, +6 do docelowej stabilności i +3 miejsca mieszkalne."),
        new("atlas", "Atlas świata", 3500, "observatory", "Ukończenie daje zwycięstwo naukowe."),
        new("housing", "Dzielnica mieszkalna", 85, "pottery", "+5 miejsc mieszkalnych. Ogranicza przeludnienie i presję na wyjazdy."),
        new("infirmary", "Lecznica", 135, "philosophy", "+6 do pojemności opieki i higieny miejskiej."),
        new("warehouse", "Skład towarowy", 150, "currency", "+100 pojemności dla każdego rodzaju towaru. Chroni nadwyżki przed utratą."),
        new("treasury", "Roboty publiczne", 80, null, "Projekt powtarzalny: +60 złota do skarbu."),
        new("festival", "Święto wspólnoty", 90, "philosophy", "Projekt powtarzalny: +30 kultury i +4 prestiżu."),
        new("scout", "Zwiadowca", 28, null, "3 punkty ruchu, duży zasięg widzenia.", UnitKind.Scout),
        new("spearmen", "Włócznicy", 45, null, "Trzon armii. Mogą zdobywać miasta.", UnitKind.Spearmen),
        new("archer", "Łucznicy", 50, "bronze", "Atak na odległość 2 pól. Nie zdobywają miast.", UnitKind.Archer),
        new("cavalry", "Kawaleria", 85, "riding", "3 punkty ruchu. Wymaga koni.", UnitKind.Cavalry),
        new("catapult", "Katapulta", 100, "engineering", "Atak na 2 pola, premia przeciw miastom.", UnitKind.Catapult),
        new("settler", "Osadnicy", 100, "agriculture", "Zakładają miasto. Rekrutacja kosztuje 1 mieszkańca.", UnitKind.Settler)
    ];
    static Content()
    {
        for(int i=0;i<Technologies.Length;i++)Technologies[i]=Technologies[i] with {Cost=Technologies[i].Cost*(i<7?1:i<14?3:i<20?6:10)};
    }
    public static Technology Tech(string id) => Technologies.First(t => t.Id == id);
    public static Project Project(string id) => Projects.First(p => p.Id == id);
    public static string TerrainName(Terrain t) => t switch { Terrain.Ocean => "Ocean", Terrain.Coast => "Wybrzeże", Terrain.Grassland => "Łąki", Terrain.Forest => "Las", Terrain.Hills => "Wzgórza", Terrain.Mountain => "Góry", Terrain.Desert => "Pustynia", Terrain.Marsh => "Mokradła", _ => "Tundra" };
    public static string ResourceName(Resource r) => r switch { Resource.Grain => "Zboże", Resource.Iron => "Żelazo", Resource.Horses => "Konie", Resource.Stone => "Kamień", Resource.Salt => "Sól", Resource.Dyes => "Barwniki", _ => "Brak" };
    public static string UnitName(UnitKind k) => k switch { UnitKind.Scout => "Zwiadowca", UnitKind.Spearmen => "Włócznicy", UnitKind.Archer => "Łucznicy", UnitKind.Cavalry => "Kawaleria", UnitKind.Catapult => "Katapulta", _ => "Osadnicy" };
    public static string Era(int era) => new[] { "Era osad", "Era miast", "Era królestw", "Era odkryć", "Era przemian" }[Math.Clamp(era,0,4)];
}
