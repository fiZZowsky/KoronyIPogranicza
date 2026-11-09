namespace Crown.Core;

public sealed record EraEndeavour(int Era,string Name,string Task,string Legacy);
public sealed record EraRequirement(string Text,bool Met);

// Explicitly commissioned institutions: no retroactive awards for an old save.
public static class EraEndeavours
{
    public static readonly EraEndeavour[] All=[
        new(0,"Szlak wspólnych łowów","Las przy osadzie oraz co najmniej 4 zapasy żywności.","Do +3 żywności w miastach przy lesie."),
        new(1,"Rezerwa pierwszych rolników","Dwie własne farmy oraz 15 żywności w jednym mieście.","+1 żywności za własną farmę, do +4 na miasto."),
        new(2,"Państwo pisma i brązu","Szkoła skrybów, odlewnia brązu i poparcie uczonych ≥45.","+2 złota i +2 nauki w miastach ze szkołą skrybów."),
        new(3,"Trakty żelaznych ziem","Dwa własne miasta w jednej sieci i co najmniej 2 żelaza.","+1 do zasięgu dostaw państwa."),
        new(4,"Życie obywatelskie","Forum, akademia filozofów oraz targowisko.","+4 kultury w miastach z forum."),
        new(5,"Karta cechów i młynów","Czynny młyn wodny, dom cechowy oraz dobrobyt miasta ≥60.","+4 produkcji w miastach z czynnym młynem."),
        new(6,"Republika drukarzy","Czynna oficyna drukarska i arsenał prochowy.","+5 nauki w miastach z czynną oficyną."),
        new(7,"Powszechna służba publiczna","Liceum publiczne, kolegium medyczne i stabilność ≥60.","+3 miejsc mieszkalnych w miastach z kolegium."),
        new(8,"Węzeł przemysłowy","Kombinat stalowy i terminal kolejowy przy pełnym zasilaniu.","+10 produkcji w miastach z czynnym kombinatem."),
        new(9,"Elektryfikacja usług","Sieć elektroenergetyczna i szpital powszechny przy pełnym zasilaniu.","+6 żywności w miastach z czynnym szpitalem."),
        new(10,"Cywilny program atomowy","Reaktor, ośrodek rakietowy, pełne zasilanie i presja klimatyczna ≤50.","+12 nauki w miastach z czynnym ośrodkiem rakietowym."),
        new(11,"Sieć wiedzy i odzysku","Centrum danych i dzielnica recyklingu przy pełnym zasilaniu.","+12 złota w miastach z czynnym recyklingiem."),
        new(12,"Umowa o pracy robotów","Fabryka robotyczna, klinika genomowa, pełna energia i poparcie ludu ≥55.","+18 produkcji w miastach z czynną fabryką robotyczną."),
        new(13,"Odbudowa wspólnej planety","Czynna fuzja i odbudowa klimatu, pełna energia oraz emisje państwa ≤0.","Redukcja emisji o 2 na własne miasto."),
        new(14,"Stała obecność orbitalna","Ukończona stacja orbitalna i zasilane laboratorium mikrograwitacji.","+25 nauki w stolicy przy zachowanej stacji orbitalnej."),
        new(15,"Most między planetami","Baza księżycowa, osada marsjańska i zasilana farma pozaziemska.","+20 żywności w stolicy przy zachowanej osadzie marsjańskiej."),
        new(16,"Samowystarczalny Układ","Osada pasa asteroid, habitat biosferyczny i sieć dalekiego kosmosu, pełna energia.","+35 produkcji w stolicy przy zachowanej osadzie pasa."),
        new(17,"Dziedzictwo dalekiej podróży","Statek pokoleniowy, zasilane archiwum cywilizacji i stabilność ≥65.","+50 kultury w stolicy przy zachowanym programie międzygwiezdnym.")
    ];
    public static int PlannedFee(GameState s,int owner)=>s.Factions[owner].ActiveEndeavour is int e&&e>=0?Fee(e,s.Factions[owner].EndeavourPace):0;
    public static int StartCost(int era)=>25+era*era*25;
    public static int Fee(int era,int pace)=>(2+era*era)*(pace==1?3:1);
    public static int Required(int pace)=>pace==1?3:6;
    public static string PaceName(int pace)=>pace==1?"Intensywny · 3 kolejne fazy":"Stopniowy · 6 kolejnych faz";
    public static EraRequirement[] Requirements(GameState s,int owner,int era)
        =>Evaluate(s,owner,era,null);
    private static EraRequirement[] Evaluate(GameState s,int owner,int era,IReadOnlyList<EnergySample>? phase)
    {
        var f=s.Factions[owner];var cities=s.Cities.Where(c=>c.Owner==owner).ToArray();
        bool Building(string id)=>cities.Any(c=>PowerGrid.Active(c,id));
        bool Powered(string id)=>cities.Any(c=>PowerGrid.Active(c,id)&&(phase==null?Epochs.Energy(s,c).Coverage>=1:phase.Any(x=>x.CityId==c.Id&&x.Owner==owner&&x.Reading.Demand>0&&x.Reading.Services.Contains(id)&&x.Reading.Allocated+1e-8>=x.Reading.Demand)));
        EraRequirement Need(string text,bool met)=>new(text,met);
        return era switch
        {
            0=>[Need("Las przy własnym mieście",cities.Any(c=>World.Neighbors(s,c.Tile).Any(t=>t.Terrain==Terrain.Forest))),Need("Miasto z zapasem ≥4 żywności",cities.Any(c=>c.Food>=4))],
            1=>[Need("Co najmniej dwie własne farmy",s.Tiles.Count(t=>t.Owner==owner&&t.Improvement==Improvement.Farm)>=2),Need("Miasto z zapasem ≥15 żywności",cities.Any(c=>c.Food>=15))],
            2=>[Need("Szkoła skrybów",Building("scribal_school")),Need("Odlewnia brązu",Building("bronze_foundry")),Need("Poparcie uczonych ≥45",f.Scholars>=45)],
            3=>[Need("Dwa własne miasta w jednej bezpiecznej sieci",cities.Where(c=>!Logistics.For(s,owner).Interdicted.Contains(c.Tile)).GroupBy(c=>Logistics.For(s,owner).MarketGroups[c.Id]).Any(g=>g.Count()>=2)),Need("Co najmniej 2 żelaza",cities.Sum(c=>c.MarketStocks[TradeGood.Iron])>=2)],
            4=>[Need("Forum obywatelskie",Building("civic_forum")),Need("Akademia filozofów",Building("classical_academy")),Need("Targowisko",Building("market"))],
            5=>[Need("Czynny młyn wodny",Building("watermill")),Need("Dom cechowy",Building("guild_hall")),Need("Dobrobyt własnego miasta ≥60",cities.Any(c=>c.Prosperity>=60))],
            6=>[Need("Oficyna drukarska",Building("printing_house")),Need("Arsenał prochowy",Building("powder_arsenal"))],
            7=>[Need("Liceum publiczne",Building("public_lyceum")),Need("Kolegium medyczne",Building("medical_college")),Need("Stabilność ≥60",f.Stability>=60)],
            8=>[Need("Zasilany kombinat stalowy",Powered("steel_mill")),Need("Zasilany terminal kolejowy",Powered("rail_terminal"))],
            9=>[Need("Czynna sieć elektroenergetyczna",Building("electric_grid")),Need("Zasilany szpital powszechny",Powered("modern_hospital"))],
            10=>[Need("Czynny reaktor",Building("atomic_reactor")),Need("Zasilany ośrodek rakietowy",Powered("rocket_centre")),Need("Presja klimatyczna ≤50",s.ClimatePressure<=50)],
            11=>[Need("Zasilane centrum danych",Powered("data_centre")),Need("Zasilany recykling",Powered("recycling_district"))],
            12=>[Need("Zasilana fabryka robotyczna",Powered("robot_factory")),Need("Zasilana klinika genomowa",Powered("genomic_clinic")),Need("Poparcie ludu ≥55",f.Commons>=55)],
            13=>[Need("Czynna elektrownia fuzyjna",Building("fusion_plant")),Need("Zasilana odbudowa klimatu",Powered("climate_restoration")),Need("Emisje państwa ≤0",Epochs.Emissions(s,owner)<=0)],
            14=>[Need("Ukończona stacja orbitalna",f.SpaceMilestones.Contains("orbital_station")),Need("Zasilane laboratorium mikrograwitacji",Powered("zero_g_lab"))],
            15=>[Need("Baza księżycowa",f.SpaceMilestones.Contains("lunar_base")),Need("Osada marsjańska",f.SpaceMilestones.Contains("mars_settlement")),Need("Zasilana farma pozaziemska",Powered("exofarm"))],
            16=>[Need("Osada pasa asteroid",f.SpaceMilestones.Contains("belt_settlement")),Need("Zasilany habitat biosferyczny",Powered("biosphere_habitat")),Need("Zasilana sieć dalekiego kosmosu",Powered("deep_space_array"))],
            17=>[Need("Ukończony statek pokoleniowy",f.SpaceMilestones.Contains("ark_launch")),Need("Zasilane archiwum cywilizacji",Powered("civilization_archive")),Need("Stabilność ≥65",f.Stability>=65)],
            _=>throw new ArgumentOutOfRangeException(nameof(era))
        };
    }
    public static string? StartError(GameState s,int era,int pace,int owner=0)
    {
        if(s.Ending!=null||!s.EpochCampaign||owner is <0 or >3||era is <0 or >17||pace is <0 or >1)return "Wybierz dostępne przedsięwzięcie i tempo.";
        var f=s.Factions[owner];if(!s.Cities.Any(c=>c.Owner==owner))return "Brak własnych miast.";if(era>f.Era)return "Ta epoka jest jeszcze nieosiągalna.";
        if(f.EraLegacies.Contains(era))return "To dziedzictwo zostało już ustanowione.";
        if(f.ActiveEndeavour>=0)return "Najpierw ukończ albo odwołaj bieżące przedsięwzięcie.";
        if(f.LastEndeavourDecisionTurn==s.Turn)return "Zmieniono już plan w tej turze.";
        return f.Gold<StartCost(era)?"Brak złota na rozpoczęcie.":null;
    }
    public static Result Start(GameState s,int era,int pace,int owner=0)
    {
        if(StartError(s,era,pace,owner) is{} error)return Result.Fail(error);
        var f=s.Factions[owner];f.Gold-=StartCost(era);f.ActiveEndeavour=era;f.EndeavourPace=pace;f.EndeavourProgress=0;f.LastEndeavourDecisionTurn=s.Turn;f.EndeavourStartedTurn=s.Turn;f.EndeavourReport="Rozpoczęto. Liczą się wyłącznie przyszłe fazy gospodarki.";
        if(owner==0)s.Log(All[era].Name+": przyjęto plan. "+PaceName(pace)+$", koszt {StartCost(era)} i {Fee(era,pace)} złota/fazę.","epoki");return Result.Ok("Plan przyjęty. Dawne osiągnięcia nie zaliczają faz wstecz.");
    }
    public static Result Cancel(GameState s,int owner=0)
    {
        if(s.Ending!=null||owner is <0 or >3)return Result.Fail("Nie można odwołać planu.");var f=s.Factions[owner];
        if(f.ActiveEndeavour<0||f.LastEndeavourDecisionTurn==s.Turn)return Result.Fail("Brak planu albo decyzję podjęto już w tej turze.");
        f.ActiveEndeavour=-1;f.EndeavourProgress=0;f.LastEndeavourDecisionTurn=s.Turn;f.EndeavourStartedTurn=0;f.EndeavourReport="Odwołano bez zwrotu poniesionych kosztów.";return Result.Ok(f.EndeavourReport);
    }
    internal static void Tick(GameState s,IReadOnlyList<EnergySample> phase)
    {
        if(!s.EpochCampaign)return;
        foreach(var f in s.Factions.Where(f=>f.ActiveEndeavour>=0&&f.LastEndeavourPhase<s.Turn))
        {
            int era=f.ActiveEndeavour,fee=Fee(era,f.EndeavourPace);var requirements=Evaluate(s,f.Id,era,phase);f.LastEndeavourPhase=s.Turn;
            if(f.Gold<fee){f.EndeavourProgress=0;f.EndeavourReport="Brak środków na fazę. Nie pobrano opłaty; ciągłość została przerwana.";continue;}
            f.Gold-=fee;bool met=requirements.All(r=>r.Met);f.EndeavourProgress=met?f.EndeavourProgress+1:0;
            f.EndeavourReport=met?$"Opłacono {fee} złota. Pełna faza {f.EndeavourProgress}/{Required(f.EndeavourPace)}.":$"Opłacono {fee} złota. Przerwana ciągłość: "+string.Join("; ",requirements.Where(r=>!r.Met).Select(r=>r.Text));
            if(f.EndeavourProgress<Required(f.EndeavourPace))continue;
            f.EraLegacies.Add(era);f.ActiveEndeavour=-1;f.EndeavourProgress=0;f.EndeavourStartedTurn=0;f.EndeavourReport="Ustanowiono dziedzictwo: "+All[era].Legacy;
            if(f.Id==0)s.Log(All[era].Name+" — "+f.EndeavourReport,"epoki");
        }
    }
    internal static void ManageAi(GameState s,Faction f)
    {
        if(!s.EpochCampaign||f.Id==0||f.ActiveEndeavour>=0)return;
        int era=Enumerable.Range(0,f.Era+1).Reverse().FirstOrDefault(e=>!f.EraLegacies.Contains(e)&&Requirements(s,f.Id,e).All(r=>r.Met),-1);
        if(era>=0&&f.Gold>=StartCost(era)+Fee(era,0)*8&&StartError(s,era,0,f.Id)==null)Start(s,era,0,f.Id);
    }
    public static Yields Bonus(GameState s,City c)
    {
        if(!s.EpochCampaign)return new();var f=s.Factions[c.Owner];var a=f.EraLegacies;if(a.Count==0)return new();
        bool Has(int era,string building)=>a.Contains(era)&&PowerGrid.Active(c,building);
        int forest=World.Neighbors(s,c.Tile).Count(t=>t.Terrain==Terrain.Forest),farms=a.Contains(1)?s.Tiles.Count(t=>t.Owner==c.Owner&&t.Improvement==Improvement.Farm&&World.Distance(s.Tiles[c.Tile],t)<=2):0;
        bool capital=c.Id==s.Cities.Where(x=>x.Owner==c.Owner).Min(x=>x.Id);
        double Power(string id)=>.25+.75*Epochs.Energy(s,c).Coverage;
        return new((a.Contains(0)?Math.Min(3,forest):0)+(a.Contains(1)?Math.Min(4,farms):0)+(Has(9,"modern_hospital")?6*Power("modern_hospital"):0)+(capital&&a.Contains(15)&&f.SpaceMilestones.Contains("mars_settlement")?20:0),
            (Has(5,"watermill")?4:0)+(Has(8,"steel_mill")?10*Power("steel_mill"):0)+(Has(12,"robot_factory")?18*Power("robot_factory"):0)+(capital&&a.Contains(16)&&f.SpaceMilestones.Contains("belt_settlement")?35:0),
            (Has(2,"scribal_school")?2:0)+(Has(11,"recycling_district")?12*Power("recycling_district"):0),
            (Has(2,"scribal_school")?2:0)+(Has(6,"printing_house")?5:0)+(Has(10,"rocket_centre")?12*Power("rocket_centre"):0)+(capital&&a.Contains(14)&&f.SpaceMilestones.Contains("orbital_station")?25:0),
            (Has(4,"civic_forum")?4:0)+(capital&&a.Contains(17)&&f.SpaceMilestones.Contains("ark_launch")?50:0));
    }
    internal static void Validate(GameState s)
    {
        foreach(var f in s.Factions)
        {
            if(f.EraLegacies==null||f.EraLegacies.Any(e=>e<0||e>f.Era)||f.ActiveEndeavour is < -1 or >17||f.ActiveEndeavour>f.Era||f.EndeavourPace is <0 or >1||f.EndeavourProgress<0||f.EndeavourProgress>=Required(f.EndeavourPace)||f.LastEndeavourDecisionTurn< -1||f.LastEndeavourDecisionTurn>s.Turn||f.LastEndeavourPhase<0||f.LastEndeavourPhase>=s.Turn||f.EndeavourStartedTurn<0||f.EndeavourStartedTurn>s.Turn||f.EndeavourReport==null||f.EndeavourReport.Length>1000)throw new InvalidDataException("Uszkodzone przedsięwzięcie epoki.");
            bool has=f.EraLegacies.Count>0||f.ActiveEndeavour>=0||f.LastEndeavourDecisionTurn>=0||f.LastEndeavourPhase>0||f.EndeavourReport.Length>0;
            if(has&&(!s.EpochCampaign||s.Version<25)||f.ActiveEndeavour>=0&&(f.EraLegacies.Contains(f.ActiveEndeavour)||f.EndeavourStartedTurn<1||f.LastEndeavourDecisionTurn<f.EndeavourStartedTurn||f.EndeavourProgress>0&&(f.LastEndeavourPhase<f.EndeavourStartedTurn||f.EndeavourProgress>f.LastEndeavourPhase-f.EndeavourStartedTurn+1))||f.ActiveEndeavour<0&&(f.EndeavourProgress!=0||f.EndeavourStartedTurn!=0))throw new InvalidDataException("Nieprawidłowa historia dziedzictwa epok.");
        }
    }
}
