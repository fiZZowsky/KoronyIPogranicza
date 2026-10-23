namespace Crown.Core;

public sealed record EpochDefinition(int Index, string Name, string Period, string Description, string[] Technologies)
{
    public string Capstone => Technologies[^1];
    public bool Speculative => Index >= 12;
}
public sealed record Development(string Id, string Name, int Era, string Technology, int Cost,
    double Food = 0, double Production = 0, double Gold = 0, double Science = 0, double Culture = 0,
    int Energy = 0, int Demand = 0, int Emissions = 0, int Housing = 0, int Sanitation = 0, int Defense = 0, int Supply = 0, int Upkeep = 0);

public static class EpochCatalog
{
    private static readonly (string Name, string Period, string Description, string Techs)[] Rows =
    [
        ("Paleolit", "Prehistoria · łowcy i zbieracze", "Ogień, narzędzia, wspólna pamięć i podział żywności tworzą pierwsze wspólnoty.", "stone_tools|Narzędzia kamienne;controlled_fire|Kontrola ognia;hunting_bands|Wspólne łowy;oral_tradition|Tradycja ustna;tribal_knowledge|Wiedza wspólnoty"),
        ("Neolit", "Prehistoria · osiadłe społeczności", "Rolnictwo i hodowla pozwalają budować osady. Własność zapasów staje się sprawą polityczną.", "agriculture|Rolnictwo;pottery|Garncarstwo;animal_husbandry|Udomowienie zwierząt;weaving|Tkactwo;settled_societies|Społeczeństwa osiadłe"),
        ("Epoka brązu", "Pierwsze miasta i pisma", "Metalurgia, rachunki i monumentalna budowa wymagają pracy oraz odpowiedzialnej administracji.", "mining|Górnictwo;bronze|Metalurgia brązu;writing|Pismo;masonry|Kamieniarstwo;urban_records|Archiwa miejskie"),
        ("Epoka żelaza", "Państwa terytorialne", "Żelazo, jeździectwo i drogi zmieniają wojnę, produkcję i kontrolę nad odległymi ziemiami.", "iron|Żelazo;riding|Jeździectwo;irrigation|Irygacja;roadbuilding|Budowa traktów;regional_states|Państwa terytorialne"),
        ("Starożytność klasyczna", "Miasta, obywatelstwo, filozofia", "Moneta i wiedza łączą rynki, lecz pytanie o prawa obywateli dzieli państwo.", "trade|Szlaki handlowe;philosophy|Filozofia;currency|Moneta;engineering|Inżynieria;classical_institutions|Instytucje klasyczne"),
        ("Średniowiecze", "Wspólnoty, domeny i cechy", "Młyny, uniwersytety i umowy lenne tworzą odmienne drogi do zamożności i władzy.", "civilservice|Służba publiczna;education|Uniwersytety;threefield|Trójpolówka;feudal_contracts|Umowy lenne;medieval_orders|Ład średniowieczny"),
        ("Renesans", "Wczesna nowożytność", "Druk, nawigacja i broń prochowa rozszerzają horyzonty oraz skutki rywalizacji.", "steel|Stal;printing|Druk;navigation|Nawigacja;gunpowder|Proch i balistyka;renaissance_methods|Metody renesansu"),
        ("Oświecenie", "Nauka, prawa i reformy", "Medycyna, doświadczenie i konstytucja każą pytać, komu służy postęp.", "medicine|Medycyna;constitution|Konstytucja;scientific_method|Metoda naukowa;rights|Prawa obywatelskie;public_reason|Rozum publiczny"),
        ("Rewolucja przemysłowa", "Para, kolej i masowa produkcja", "Energia napędza fabryki; emisje obciążają wspólny klimat. Wzrost ma społeczną cenę.", "industry|Przemysł;steam_power|Maszyna parowa;railways|Kolej;industrial_chemistry|Chemia przemysłowa;mass_production|Produkcja masowa"),
        ("Nowoczesność", "Elektryfikacja i społeczeństwo masowe", "Sieci energetyczne, silniki i radio zwiększają możliwości państwa oraz skalę wojny.", "electricity|Elektryczność;combustion|Silnik spalinowy;radio|Radio;aviation|Lotnictwo i aeronautyka;mass_society|Społeczeństwo masowe"),
        ("Epoka atomowa", "Atom, elektronika i pierwsze rakiety", "Reaktory i medycyna ratują życie, a technologie strategiczne stawiają nowe pytania o bezpieczeństwo.", "nuclear_physics|Fizyka jądrowa;rocketry|Technika rakietowa;antibiotics|Antybiotyki;semiconductors|Półprzewodniki;atomic_systems|Systemy epoki atomowej"),
        ("Epoka informacyjna", "Sieci cyfrowe i wiedza globalna", "Komputery, ekologia i łączność pozwalają koordynować państwo. Prywatność staje się dobrem wspólnym.", "ecology|Ekologia;computing|Informatyka;internet|Internet;observatory|Wielkie obserwatorium;digital_society|Społeczeństwo informacyjne"),
        ("Epoka automatyzacji", "Możliwa bliska przyszłość", "Uczenie maszynowe, roboty i genomika mogą zmienić pracę. Ich korzyści można dzielić na różne sposoby.", "machine_learning|Uczenie maszynowe;robotics|Robotyka;genomics|Genomika;smart_materials|Inteligentne materiały;automation_charter|Karta automatyzacji"),
        ("Epoka planetarna", "Możliwa przyszłość · wspólna planeta", "Synteza jądrowa i obiegi zamknięte otwierają drogę odbudowie klimatu lub dalszej koncentracji przemysłu.", "fusion|Energetyka fuzyjna;closed_cycles|Obiegi zamknięte;climate_engineering|Inżynieria klimatu;planetary_network|Sieć planetarna;planetary_cooperation|Współpraca planetarna"),
        ("Epoka orbitalna", "Możliwa przyszłość · infrastruktura orbitalna", "Wielokrotne starty, medycyna kosmiczna i montaż orbitalny pozwalają utrzymywać pierwsze stałe habitaty.", "reusable_launch|Wielokrotne systemy startowe;orbital_assembly|Montaż orbitalny;space_habitats|Habitaty kosmiczne;space_medicine|Medycyna kosmiczna;orbital_industry|Przemysł orbitalny"),
        ("Epoka międzyplanetarna", "Możliwa przyszłość · Księżyc i Mars", "Samowystarczalne placówki wymagają rolnictwa, napędu i prawa do decydowania o życiu daleko od ojczyzny.", "lunar_resources|Zasoby księżycowe;mars_habitats|Habitaty marsjańskie;planetary_agriculture|Rolnictwo pozaziemskie;fusion_propulsion|Napęd fuzyjny;interplanetary_logistics|Logistyka międzyplanetarna"),
        ("Epoka Układu Słonecznego", "Odległa możliwa przyszłość", "Asteroidy i dalekie przekaźniki poszerzają gospodarkę. Ograniczone zasoby, energia i autonomia nadal mają znaczenie.", "asteroid_mining|Górnictwo asteroid;outer_system|Eksploracja zewnętrznego układu;synthetic_ecologies|Syntetyczne ekosystemy;deep_space_relays|Przekaźniki dalekiego kosmosu;solar_commonwealth|Wspólnota Układu"),
        ("Epoka międzygwiezdna", "Spekulatywna daleka przyszłość", "Sondy i statki pokoleniowe są przedsięwzięciami wielu generacji. Nie zakładamy podróży szybszej od światła.", "generation_ships|Statki pokoleniowe;stellar_energy|Energetyka gwiazdowa;interstellar_probes|Sondy międzygwiezdne;distributed_civilization|Cywilizacja rozproszona;interstellar_charter|Karta międzygwiezdna")
    ];
    public static readonly EpochDefinition[] All = Rows.Select((r, i) => new EpochDefinition(i, r.Name, r.Period, r.Description, r.Techs.Split(';').Select(t => t.Split('|')[0]).ToArray())).ToArray();
    private static readonly int[] Costs = [24, 44, 75, 130, 220, 360, 600, 900, 1500, 2300, 3500, 5000, 7200, 10000, 14000, 19000, 26000, 35000];
    public static Technology[] Technologies(Technology[] legacy) => Rows.SelectMany((row, era) => row.Techs.Split(';').Select((text, n) =>
    {
        var parts = text.Split('|'); var old = legacy.FirstOrDefault(t => t.Id == parts[0]);
        string[] requires = n == 0 ? era == 0 ? [] : [All[era - 1].Capstone] : n == 4 ? All[era].Technologies[..4] : [All[era].Technologies[0]];
        return old ?? new(parts[0], parts[1], (int)(Costs[era] * (1 + n * .15)), requires, n == 4 ? $"Zamyka dorobek epoki i otwiera badania: {(era == 17 ? "dalsze projekty międzygwiezdne" : All[era + 1].Name)}. " + BonusDescription(era, n) : row.Description + " " + BonusDescription(era, n));
    })).ToArray();
    public static int EraOf(string technology) => Array.FindIndex(All, e => e.Technologies.Contains(technology));
    public static Yields TechBonus(int era, int index)
    {
        double amount = 1 + era / 3;
        return index switch { 0 => new(amount, 0, 0, 0, 0), 1 => new(0, amount, 0, 0, 0), 2 => new(0, 0, 0, amount, 0), 3 => new(0, 0, amount, 0, amount), _ => new(0, 0, 0, amount, amount) };
    }
    private static string BonusDescription(int era, int n) => "Premia w każdym mieście: " + Describe(TechBonus(era, n)) + ".";
    public static string Describe(Yields y) => string.Join(" · ", new[] { ("żywność", y.Food), ("produkcja", y.Production), ("złoto", y.Gold), ("nauka", y.Science), ("kultura", y.Culture) }.Where(x => x.Item2 != 0).Select(x => $"{x.Item1} {x.Item2:+0.##;-0.##}"));
    public static readonly Development[] Buildings =
    [
        new("communal_camp", "Wspólne obozowisko", 0, "controlled_fire", 30, Food: 2, Housing: 2),
        new("flint_workshop", "Pracownia krzemienia", 0, "stone_tools", 35, Production: 3),
        new("memory_cave", "Grota pamięci", 0, "oral_tradition", 40, Culture: 3, Science: 1),
        new("village_well", "Studnia wspólnoty", 1, "agriculture", 55, Food: 3, Sanitation: 3),
        new("weavers_house", "Dom tkaczy", 1, "weaving", 65, Gold: 5, Production: 2),
        new("village_school", "Dom przekazywania wiedzy", 1, "settled_societies", 75, Science: 4, Culture: 2),
        new("bronze_foundry", "Odlewnia brązu", 2, "bronze", 100, Production: 7),
        new("scribal_school", "Szkoła skrybów", 2, "writing", 100, Science: 7, Gold: 3),
        new("water_cistern", "Cysterna miejska", 2, "masonry", 110, Food: 4, Housing: 3, Sanitation: 3),
        new("bloomery", "Dymarki", 3, "iron", 135, Production: 10, Emissions: 1),
        new("paved_bazaar", "Targ przy trakcie", 3, "roadbuilding", 140, Gold: 12),
        new("border_post", "Strażnica graniczna", 3, "regional_states", 160, Defense: 8, Supply: 1),
        new("classical_academy", "Akademia filozofów", 4, "philosophy", 180, Science: 12, Culture: 4),
        new("civic_forum", "Forum obywatelskie", 4, "classical_institutions", 190, Culture: 12, Gold: 4),
        new("engineers_yard", "Plac inżynierów", 4, "engineering", 180, Production: 12, Housing: 4),
        new("watermill", "Młyn wodny", 5, "threefield", 230, Food: 10, Production: 7),
        new("guild_hall", "Dom cechowy", 5, "feudal_contracts", 240, Gold: 18, Production: 7),
        new("hospital_order", "Szpital wspólnoty", 5, "medieval_orders", 260, Sanitation: 9, Housing: 5, Science: 5),
        new("printing_house", "Oficyna drukarska", 6, "printing", 310, Culture: 20, Science: 8),
        new("powder_arsenal", "Arsenał prochowy", 6, "gunpowder", 340, Production: 20, Defense: 12),
        new("exchange_house", "Giełda kupiecka", 6, "navigation", 320, Gold: 30),
        new("public_lyceum", "Liceum publiczne", 7, "scientific_method", 400, Science: 30, Culture: 15),
        new("civic_housing", "Dzielnica obywatelska", 7, "rights", 420, Housing: 12, Gold: 10),
        new("medical_college", "Kolegium medyczne", 7, "medicine", 440, Science: 20, Sanitation: 12),
        new("coal_station", "Elektrownia cieplna", 8, "steam_power", 520, Energy: 24, Emissions: 6, Upkeep: 4),
        new("steel_mill", "Kombinat stalowy", 8, "industry", 560, Production: 50, Demand: 12, Emissions: 4, Upkeep: 3),
        new("rail_terminal", "Terminal kolejowy", 8, "railways", 580, Gold: 40, Supply: 3, Demand: 4),
        new("electric_grid", "Sieć elektroenergetyczna", 9, "electricity", 650, Energy: 40, Emissions: 3, Upkeep: 5),
        new("motor_works", "Zakłady silnikowe", 9, "combustion", 700, Production: 75, Demand: 22, Emissions: 3),
        new("modern_hospital", "Szpital powszechny", 9, "mass_society", 700, Sanitation: 18, Housing: 8, Demand: 6),
        new("atomic_reactor", "Elektrownia jądrowa", 10, "nuclear_physics", 900, Energy: 85, Emissions: 1, Upkeep: 10),
        new("rocket_centre", "Ośrodek rakietowy", 10, "rocketry", 950, Science: 65, Production: 20, Demand: 20),
        new("public_broadcast", "Rozgłośnia publiczna", 10, "atomic_systems", 850, Culture: 65, Demand: 10),
        new("data_centre", "Centrum danych", 11, "computing", 1200, Science: 110, Demand: 25, Upkeep: 6),
        new("chip_fab", "Fabryka półprzewodników", 11, "internet", 1300, Production: 100, Gold: 25, Demand: 28),
        new("recycling_district", "Dzielnica recyklingu", 11, "ecology", 1100, Food: 20, Gold: 25, Emissions: -5, Demand: 8),
        new("smart_grid", "Inteligentna sieć odnawialna", 12, "smart_materials", 1600, Energy: 110, Emissions: -4, Upkeep: 8),
        new("robot_factory", "Fabryka robotyczna", 12, "robotics", 1700, Production: 170, Demand: 45),
        new("genomic_clinic", "Klinika genomowa", 12, "genomics", 1700, Food: 45, Sanitation: 25, Demand: 20),
        new("fusion_plant", "Elektrownia fuzyjna", 13, "fusion", 2300, Energy: 240, Emissions: -8, Upkeep: 18),
        new("climate_restoration", "Ośrodek odbudowy klimatu", 13, "climate_engineering", 2400, Culture: 35, Emissions: -25, Demand: 45),
        new("planetary_institute", "Instytut planetarny", 13, "planetary_network", 2200, Science: 170, Housing: 15, Demand: 35),
        new("orbital_shipyard", "Stocznia orbitalna", 14, "orbital_assembly", 3000, Production: 240, Demand: 65),
        new("orbital_habitat", "Administracja habitatów orbitalnych", 14, "space_habitats", 3100, Gold: 110, Housing: 35, Demand: 50),
        new("zero_g_lab", "Laboratorium mikrograwitacji", 14, "space_medicine", 3200, Science: 260, Demand: 55),
        new("lunar_exchange", "Terminal księżycowy", 15, "lunar_resources", 4000, Production: 300, Demand: 85),
        new("exofarm", "Zespół farm pozaziemskich", 15, "planetary_agriculture", 4100, Food: 110, Sanitation: 30, Demand: 75),
        new("mars_institute", "Instytut badań marsjańskich", 15, "mars_habitats", 4200, Science: 360, Demand: 90),
        new("asteroid_forge", "Odlewnia asteroid", 16, "asteroid_mining", 5200, Production: 450, Demand: 140),
        new("deep_space_array", "Sieć dalekiego kosmosu", 16, "deep_space_relays", 5300, Science: 500, Demand: 120),
        new("biosphere_habitat", "Habitat biosferyczny", 16, "synthetic_ecologies", 5400, Food: 90, Housing: 70, Sanitation: 40, Demand: 110),
        new("stellar_collector", "Kolektor energii gwiazdowej", 17, "stellar_energy", 7000, Energy: 700, Emissions: -30, Upkeep: 40),
        new("civilization_archive", "Archiwum cywilizacji", 17, "distributed_civilization", 7200, Culture: 550, Science: 330, Demand: 180),
        new("departure_yard", "Stocznia statków pokoleniowych", 17, "generation_ships", 7500, Production: 700, Science: 160, Demand: 240),
        new("hydro_station", "Elektrownia wodna", 9, "electricity", 900, Energy: 45, Upkeep: 3),
        new("wind_farm", "Farma wiatrowa", 11, "ecology", 1250, Energy: 35, Upkeep: 2),
        new("solar_farm", "Farma słoneczna", 11, "ecology", 1300, Energy: 42, Upkeep: 2),
        new("geothermal_station", "Elektrownia geotermalna", 12, "smart_materials", 1800, Energy: 100, Upkeep: 8)
    ];
    public static string Describe(Development b)
    {
        var parts = new List<string> { Describe(new Yields(b.Food, b.Production, b.Gold, b.Science, b.Culture)) };
        foreach (var x in new[] { ("Energia", b.Energy), ("Popyt energii", b.Demand), ("Emisje", b.Emissions), ("Mieszkania", b.Housing), ("Opieka", b.Sanitation), ("Obrona", b.Defense), ("Zasięg dostaw", b.Supply), ("Utrzymanie złota/turę", b.Upkeep) })
            if (x.Item2 != 0) parts.Add($"{x.Item1} {x.Item2:+0;-0}");
        if (b.Demand > 0) parts.Add("Produkcja, wiedza i dochód tego obiektu zależą od miejskiego pokrycia energii: 25–100%.");
        parts.Add(PowerGrid.SiteDescription(b.Id));
        return string.Join(" · ", parts.Where(p => p.Length > 0));
    }
}
