namespace Crown.Core;

public static class AuditCouncil
{
    public static readonly NarrativeEvent Event=new("provincial_audit","Seren Ata","Archiwistka komisji rachunkowej","Czyje są dochody prowincji?",
        "Kontrolerzy wrócili z księgami. Część danin zatrzymali urzędnicy związani z miejscowymi rodami. Rada musi zdecydować, czy odzyskać pieniądze, przebudować urząd, czy zawrzeć ugodę.",
        [new("Przeprowadzimy jawny proces i odzyskamy część danin.","",Stability:-2,Commons:6,Nobles:-10),
         new("Odpiszemy braki i ustanowimy niezależny urząd.","",Gold:-Administration.ReformPrice,Commons:3,Nobles:-6,Scholars:8),
         new("Rody zwrócą część dochodów w zamian za ugodę.","",Commons:-5,Nobles:6)]);
    public static City City(GameState s,AuditCase audit)=>s.Cities.First(c=>c.Id==audit.CityId);
    public static int Recovery(AuditCase audit,int choice)=>(int)Math.Floor(audit.AssessedGold*(choice==0?.75:choice==2?.25:0));
    public static string? ChoiceError(GameState s,int choice)
    {
        var audit=s.Audits.FirstOrDefault(a=>a.Id==s.PendingAuditId&&a.Status==AuditStatus.AwaitingCouncil);
        if(choice is <0 or >2||audit==null||City(s,audit).Owner!=0)return "Ta kontrola nie ma prawidłowej sprawy Marchii.";
        return choice==1&&s.Player.Gold<Administration.ReformPrice?"Niezależny urząd kosztuje dodatkowe 60 złota.":null;
    }
    public static NarrativeEvent Current(GameState s)
    {
        var audit=s.Audits.FirstOrDefault(a=>a.Id==s.PendingAuditId);if(audit==null)return Event;
        var city=City(s,audit);var choices=Event.Choices.ToArray();
        choices[0]=choices[0] with {Gold=Recovery(audit,0),Consequence="Odzyskujemy 75% ustalonych braków (w dół do pełnego złota); reszta jest nieściągalna. W mieście: korupcja -25 punktów, niepokoje +6, lojalność -4."};
        choices[1]=choices[1] with {Consequence=$"Bez odzyskania dawnych braków. W mieście: korupcja -35 punktów, niepokoje -5, lojalność +4. Nadzór obniża cel korupcji o 20 do początku T{s.Turn+Administration.ProtectionTurns}."};
        choices[2]=choices[2] with {Gold=Recovery(audit,2),Consequence="Odzyskujemy 25% ustalonych braków (w dół do pełnego złota); reszta jest umorzona. W mieście: korupcja -8 punktów, niepokoje -3, lojalność +3."};
        return Event with {Title=Event.Title+" — "+city.Name,Choices=choices,
            Body=$"W mieście {city.Name} ustalono {audit.AssessedGold:0.00} złota rzeczywistych braków z poprzednich rozliczeń. Środki objęte raportem odłączono od dalszych rachunków; nowe straty będą przedmiotem przyszłej kontroli. Korupcja wynosi teraz {city.Corruption}%. Jawny proces uderzy w miejscowe rody. Niezależny urząd wymaga nowych środków. Ugoda ocali część dochodów i wpływy dawnych zarządców. Jaką instrukcję wpiszemy do protokołu?"};
    }
    public static bool Schedule(GameState s)
    {
        if(s.PendingEvent!=null)return false;
        var audit=s.Audits.Where(a=>a.Status==AuditStatus.AwaitingCouncil).OrderBy(a=>a.CompletedTurn).ThenBy(a=>a.Id).FirstOrDefault();
        if(audit==null)return false;
        s.PendingEvent=Event.Id;s.PendingCityId=null;s.PendingTradeCaseId=null;s.PendingAuditId=audit.Id;return true;
    }
    public static Result Present(GameState s,int id)
    {
        if(s.Ending!=null||s.PendingEvent!=null&&(s.PendingEvent!=Event.Id||s.PendingAuditId!=id))return Result.Fail("Najpierw rozstrzygnij bieżącą sprawę rady.");
        var audit=s.Audits.FirstOrDefault(a=>a.Id==id&&a.Status==AuditStatus.AwaitingCouncil);
        if(audit==null||City(s,audit).Owner!=0)return Result.Fail("Nie ma takiego gotowego raportu Marchii.");
        s.PendingEvent=Event.Id;s.PendingCityId=null;s.PendingTradeCaseId=null;s.PendingAuditId=id;return Result.Ok("Komisja czeka na decyzję rady.");
    }
    internal static void Apply(GameState s,int choice)
    {
        var audit=s.Audits.First(a=>a.Id==s.PendingAuditId);var city=City(s,audit);
        audit.Status=choice==0?AuditStatus.Prosecuted:choice==1?AuditStatus.Reformed:AuditStatus.Settled;
        audit.ResolvedTurn=s.Turn;audit.RecoveredGold=Recovery(audit,choice);
        city.Corruption=Math.Max(0,city.Corruption-(choice==0?25:choice==1?35:8));
        city.Unrest=Math.Clamp(city.Unrest+(choice==0?6:choice==1?-5:-3),0,100);
        city.Loyalty=Math.Clamp(city.Loyalty+(choice==0?-4:choice==1?4:3),0,100);
        if(choice==1)city.AuditProtectionUntil=Math.Max(city.AuditProtectionUntil,s.Turn+Administration.ProtectionTurns);
    }
    public static void OwnershipChanged(GameState s,City city)
    {
        city.AuditProtectionUntil=0;city.HiddenGold=0;city.LastRevenueLoss=0;
        if(city.Owner==0)return;
        foreach(var audit in s.Audits.Where(a=>a.CityId==city.Id&&Administration.Open(a)))
        {
            audit.Status=AuditStatus.Cancelled;audit.ResolvedTurn=s.Turn;
            s.Log($"{city.Name}: utrata prowincji przerwała kontrolę #{audit.Id}. Opłata za kontrolerów nie podlega zwrotowi.","administracja");
            if(s.PendingAuditId==audit.Id){s.PendingAuditId=null;if(s.PendingEvent==Event.Id)s.PendingEvent=null;}
        }
    }
    public static void Tick(GameState s)
    {
        foreach(var audit in s.Audits.Where(a=>a.Status==AuditStatus.Investigating&&a.DueTurn<=s.Turn).OrderBy(a=>a.Id))
        {
            var city=City(s,audit);if(city.Owner!=0){OwnershipChanged(s,city);continue;}
            audit.AssessedGold=city.HiddenGold;city.HiddenGold=0;audit.CompletedTurn=s.Turn;audit.Status=AuditStatus.AwaitingCouncil;
            s.Log($"{city.Name}: komisja ustaliła {audit.AssessedGold:0.00} złota braków. Raport #{audit.Id} czeka na radę; nowe straty są księgowane osobno.","administracja");
        }
    }
}
