namespace Crown.Core;

public enum LocalMandate { None, Granaries, Crafts, Schools }
public sealed class LocalCouncil
{
    public bool Established {get;set;}
    public int Families {get;set;}=50;
    public int Guilds {get;set;}=50;
    public int Landholders {get;set;}=50;
    public LocalMandate Mandate {get;set;}
    public int StartedTurn {get;set;}
    public int LastDecisionTurn {get;set;}=-10;
    public int LastPhaseTurn {get;set;}
    public int Phases {get;set;}
    public int Delivered {get;set;}
    public int Paid {get;set;}
    public int PhaseFee {get;set;}
    public HashSet<LocalMandate> Charters {get;set;}=[];
    public string Report {get;set;}="Rada nie została jeszcze zwołana.";
}

public static class LocalCouncils
{
    public const int Phases=6,Required=4;
    public static string Name(LocalMandate m)=>m switch {LocalMandate.Granaries=>"Karta bezpieczeństwa żywnościowego",LocalMandate.Crafts=>"Umowa cechów i warsztatów",LocalMandate.Schools=>"Fundacja szkół prowincji",_=>"Brak zobowiązania"};
    public static string Terms(LocalMandate m)=>m switch
    {
        LocalMandate.Granaries=>"Rodziny: „Nie wystarczy otworzyć spichlerza na dzień wizyty posłów”. Każde rozliczenie wymaga co najmniej 12 żywności w zapasie i braku głodu. Karta: +2 żywności w mieście.",
        LocalMandate.Crafts=>"Cechy: „Zapłaćcie za pracę, a utrzymamy warsztaty”. Potrzebny czynny warsztat, nieprzerwana produkcja i niepokoje poniżej 40. Karta: +2 produkcji w mieście.",
        _=>"Nauczyciele i rody: „Zagwarantujcie szkołom utrzymanie, nie samą pieczęć”. Potrzebna czynna biblioteka i niepokoje poniżej 40. Karta: +2 nauki w mieście."
    };
    public static int Fee(GameState s,City c)=>3+s.Factions[c.Owner].Era+c.Population/3;
    public static string? Error(GameState s,City? c,LocalMandate m,int owner=0)
    {
        if(s.Ending!=null||s.PendingEvent!=null||c==null||c.Owner!=owner||!Enum.IsDefined(m)||m==LocalMandate.None)return "Rada nie może teraz przyjąć tego zobowiązania.";
        var r=c.Council;
        if(r.Mandate!=LocalMandate.None)return "Najpierw rozlicz bieżące zobowiązanie.";
        if(r.Charters.Contains(m))return "Ta karta została już ustanowiona w prowincji.";
        if(s.Turn-r.LastDecisionTurn<10)return "Rada zbierze się ponownie po dziesięciu rozliczeniach od poprzedniej decyzji.";
        if(s.Factions[owner].Gold<30)return "Zwołanie rady i zawarcie umowy kosztuje 30 złota.";
        return null;
    }
    public static Result Begin(GameState s,int cityId,LocalMandate m,int owner=0)
    {
        var c=s.Cities.FirstOrDefault(c=>c.Id==cityId);if(Error(s,c,m,owner) is{} error)return Result.Fail(error);
        var r=c!.Council;s.Factions[owner].Gold-=30;r.Established=true;r.Mandate=m;r.StartedTurn=r.LastDecisionTurn=s.Turn;
        r.LastPhaseTurn=0;r.Phases=r.Delivered=r.Paid=0;r.PhaseFee=Fee(s,c);
        // A coalition pleases one constituency and costs support with another.
        if(m==LocalMandate.Granaries){r.Families=Math.Min(100,r.Families+8);r.Landholders=Math.Max(0,r.Landholders-5);}
        if(m==LocalMandate.Crafts){r.Guilds=Math.Min(100,r.Guilds+8);r.Families=Math.Max(0,r.Families-5);}
        if(m==LocalMandate.Schools){r.Landholders=Math.Min(100,r.Landholders+8);r.Guilds=Math.Max(0,r.Guilds-5);}
        r.Report=$"{CampaignCalendar.At(s,s.Turn)}: rada przyjęła zobowiązanie; opłata za każde udane rozliczenie {r.PhaseFee} złota. Cel: co najmniej cztery z sześciu rozliczeń. Koszt zwołania 30 złota nie podlega zwrotowi.";
        s.Log(c.Name+": "+Name(m)+". "+r.Report,"decyzja");return Result.Ok("Podpisano lokalną umowę. Liczy się rzeczywiste wykonanie.");
    }
    public static int Loyalty(City c)=>!c.Council.Established?0:(c.Council.Families+c.Council.Guilds+c.Council.Landholders-150)/10;
    public static Yields Bonus(City c)=>new(c.Council.Charters.Contains(LocalMandate.Granaries)?2:0,c.Council.Charters.Contains(LocalMandate.Crafts)?2:0,0,c.Council.Charters.Contains(LocalMandate.Schools)?2:0,0);
    public static void Tick(GameState s)
    {
        // Local diplomacy opens for the AI after the player introduces provincial councils.
        if(s.Cities.Any(c=>c.Owner==0&&c.Council.Established))
            foreach(var c in s.Cities.Where(c=>c.Owner>0&&s.Turn%16==c.Id%16&&s.Factions[c.Owner].Gold>200).OrderBy(c=>c.Id))
            {
                var options=new[]{LocalMandate.Schools,LocalMandate.Crafts,LocalMandate.Granaries};
                var m=options.FirstOrDefault(m=>Error(s,c,m,c.Owner)==null&&(m==LocalMandate.Schools?c.Buildings.Contains("library")&&PowerGrid.Active(c,"library"):m==LocalMandate.Crafts?c.Buildings.Contains("workshop")&&!c.ProductionPaused:c.Food>=12&&c.HungerTurns==0));
                if(m!=LocalMandate.None)Begin(s,c.Id,m,c.Owner);
            }
        foreach(var c in s.Cities.OrderBy(c=>c.Id))
        {
            var r=c.Council;var m=r.Mandate;if(m==LocalMandate.None||r.LastPhaseTurn>=s.Turn)continue;
            bool Ready(string building)=>c.Buildings.Contains(building)&&PowerGrid.Active(c,building);
            bool conditions=m switch {LocalMandate.Granaries=>c.Food>=12&&c.HungerTurns==0,LocalMandate.Crafts=>Ready("workshop")&&!c.ProductionPaused&&c.Unrest<40,_=>Ready("library")&&c.Unrest<40};
            bool funds=s.Factions[c.Owner].Gold>=r.PhaseFee;
            r.Phases++;r.LastPhaseTurn=s.Turn;
            bool delivered=conditions&&funds;
            if(delivered){s.Factions[c.Owner].Gold-=r.PhaseFee;r.Paid+=r.PhaseFee;r.Delivered++;}
            r.Report=$"{CampaignCalendar.At(s,s.Turn)}: "+(delivered?"warunki spełnione; zapłacono "+r.PhaseFee+" złota.":!conditions?"warunki niewypełnione; bez opłaty.":"brak środków; bez opłaty i wykonania.");
            if(r.Phases<Phases)continue;
            bool success=r.Delivered>=Required;int delta=success?12:-18;
            if(m==LocalMandate.Granaries)r.Families=Math.Clamp(r.Families+delta,0,100);
            if(m==LocalMandate.Crafts)r.Guilds=Math.Clamp(r.Guilds+delta,0,100);
            if(m==LocalMandate.Schools)r.Landholders=Math.Clamp(r.Landholders+delta,0,100);
            if(success){r.Charters.Add(m);c.Loyalty=Math.Min(100,c.Loyalty+5);}
            else {c.Unrest=Math.Min(100,c.Unrest+8);c.Loyalty=Math.Max(0,c.Loyalty-8);}
            r.Report+=$" Rozliczono {Name(m)}: {r.Delivered}/6, wydano łącznie {r.Paid+30} złota. "+(success?"Trwała karta ustanowiona; poparcie grupy +12, lojalność +5.":"Obietnica niedotrzymana; poparcie grupy -18, niepokoje +8, lojalność -8.");
            r.Mandate=LocalMandate.None;s.Log(c.Name+": "+r.Report,"prowincja");
        }
    }
    public static void Validate(GameState s)
    {
        foreach(var c in s.Cities)
        {
            if(s.Version<27){c.Council=new();continue;}
            var r=c.Council;
            if(r==null||!Enum.IsDefined(r.Mandate)||r.Charters==null||r.Charters.Any(x=>x==LocalMandate.None||!Enum.IsDefined(x))||r.Charters.Count>3||r.Report==null||r.Report.Length>3000||r.Families is <0 or >100||r.Guilds is <0 or >100||r.Landholders is <0 or >100||r.Phases is <0 or >6||r.Delivered<0||r.Delivered>r.Phases||r.Paid<0||r.PhaseFee is <0 or >353||r.Paid!=r.Delivered*r.PhaseFee||r.StartedTurn<0||r.StartedTurn>s.Turn||r.LastDecisionTurn< -10||r.LastDecisionTurn>s.Turn||r.LastPhaseTurn<0||r.LastPhaseTurn>s.Turn||r.Mandate!=LocalMandate.None&&(!r.Established||r.Phases>=6||r.StartedTurn==0||r.Charters.Contains(r.Mandate)))
                throw new InvalidDataException("Nieprawidłowe zobowiązanie rady prowincji.");
            if(!r.Established&&(r.Charters.Count>0||r.Mandate!=LocalMandate.None||r.Phases!=0||r.Families!=50||r.Guilds!=50||r.Landholders!=50)||r.Phases>0&&(r.LastPhaseTurn<r.StartedTurn||r.LastPhaseTurn-r.StartedTurn>6)||r.Mandate!=LocalMandate.None&&r.PhaseFee<3)
                throw new InvalidDataException("Niespójna historia rady prowincji.");
        }
    }
}
