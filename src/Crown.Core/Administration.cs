namespace Crown.Core;

public static class Administration
{
    public const int AuditPrice=35,AuditTurns=3,AuditInterval=12,ReformPrice=60,ProtectionTurns=12;
    public static int Target(GameState s,City city)
    {
        var f=s.Factions[city.Owner];var capital=s.Cities.Where(c=>c.Owner==city.Owner).MinBy(c=>c.Id)!;
        return Math.Clamp(6+World.Distance(s.Tiles[capital.Tile],s.Tiles[city.Tile])*2+city.Unrest/5+(100-f.Stability)/10+
            (city.Governance==Governance.Integrated?0:8)-(city.Buildings.Contains("court")?12:0)-
            (f.Technologies.Contains("civilservice")?8:0)-(f.Technologies.Contains("constitution")?6:0)-
            (city.AuditProtectionUntil>s.Turn?20:0),0,65);
    }
    public static double Loss(GameState s,City city)=>Engine.BeforeCorruption(s,city).Gold*city.Corruption/100;
    public static bool Open(AuditCase audit)=>audit.Status is AuditStatus.Investigating or AuditStatus.AwaitingCouncil;
    public static string StatusName(AuditStatus status)=>status switch
    {AuditStatus.Investigating=>"Kontrola w toku",AuditStatus.AwaitingCouncil=>"Czeka na decyzję",AuditStatus.Prosecuted=>"Jawny proces",
        AuditStatus.Reformed=>"Niezależny urząd",AuditStatus.Settled=>"Ugoda z rodami",_=>"Przerwano po utracie miasta"};
    public static string? AuditError(GameState s,int cityId)
    {
        var city=s.Cities.FirstOrDefault(c=>c.Id==cityId&&c.Owner==0);
        if(s.Ending!=null||city==null)return "Wskaż własną prowincję w trwającej kampanii.";
        if(!s.Player.Technologies.Contains("writing"))return "Kontrola rachunków wymaga Pisma.";
        if(s.Audits.Any(a=>a.CityId==cityId&&Open(a)))return "Ta prowincja ma już otwartą kontrolę.";
        if(s.Turn-city.LastAuditTurn<AuditInterval)return $"Kolejna kontrola najwcześniej w T{city.LastAuditTurn+AuditInterval}.";
        if(city.Corruption<10&&city.HiddenGold<10)return "Brak podstaw do kontroli: potrzeba korupcji co najmniej 10% lub 10 złota nieujętych dochodów.";
        if(s.Player.Gold<AuditPrice)return "Wysłanie kontrolerów kosztuje 35 złota.";
        return null;
    }
    // Called just after income, before technology and growth alter the basis of that income.
    internal static void Collect(GameState s,int owner,double incomeMultiplier)
    {
        foreach(var city in s.Cities.Where(c=>c.Owner==owner&&c.LastAdministrationTurn<s.Turn))
        {
            city.LastRevenueLoss=Loss(s,city)*incomeMultiplier;
            city.HiddenGold+=city.LastRevenueLoss;city.LastAdministrationTurn=s.Turn;
            city.Corruption+=Math.Sign(Target(s,city)-city.Corruption);
            if(owner>0&&city.Corruption>=30&&s.Factions[owner].Gold>=180&&s.Turn-city.LastAuditTurn>=AuditInterval)
            {
                s.Factions[owner].Gold-=ReformPrice;city.Corruption=Math.Max(0,city.Corruption-25);
                city.HiddenGold=0;city.LastAuditTurn=s.Turn;city.AuditProtectionUntil=s.Turn+8;
            }
        }
    }
}

public static partial class Engine
{
    public static Result StartAudit(GameState s,int cityId)
    {
        if(Administration.AuditError(s,cityId) is string error)return Result.Fail(error);
        var city=s.Cities.First(c=>c.Id==cityId);
        s.Player.Gold-=Administration.AuditPrice;city.LastAuditTurn=s.Turn;
        s.Audits.Add(new(){Id=s.NextAuditId++,CityId=cityId,StartedTurn=s.Turn,DueTurn=s.Turn+Administration.AuditTurns});
        s.Log($"{city.Name}: wysłano kontrolerów za 35 złota. Raport w T{s.Turn+Administration.AuditTurns}. Ubytek dochodów trwa podczas kontroli.","administracja");
        return Result.Ok("Kontrolerzy zbadają rachunki w ciągu trzech tur. Wynik trafi do rady.");
    }
}
