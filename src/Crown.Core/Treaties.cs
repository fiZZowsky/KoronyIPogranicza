namespace Crown.Core;

public enum TreatyKind { Research, PeaceGuarantee }
public enum TreatyStatus { Travelling, AwaitingPlayer, Active, Rejected, Expired, Completed, Broken, Cancelled }
public sealed class BilateralTreaty
{
    public int Id {get;set;}
    public int From {get;set;}
    public int To {get;set;}
    public TreatyKind Kind {get;set;}
    public int Duration {get;set;}=12;
    public int SentTurn {get;set;}
    public int DueTurn {get;set;}
    public int DeadlineTurn {get;set;}
    public int? ReplyTurn {get;set;}
    public int? SignedTurn {get;set;}
    public int? ExpiresTurn {get;set;}
    public int? ClosedTurn {get;set;}
    public TreatyStatus Status {get;set;}
    public int EscrowFrom {get;set;}
    public int EscrowTo {get;set;}
    public int PayoutFrom {get;set;}
    public int PayoutTo {get;set;}
    public int? BreachedBy {get;set;}
    public int LastPhaseTurn {get;set;}
    public int LastFee {get;set;}
    public double LastScience {get;set;}
    public double LastIncomeFrom {get;set;}
    public double LastIncomeTo {get;set;}
    public int LastBonusCap {get;set;}
    public int FundedPhases {get;set;}
    public double TotalScience {get;set;}
    public int MissedFrom {get;set;}
    public int MissedTo {get;set;}
    public string Report {get;set;}="";
}

public static class Treaties
{
    public const int EmbassyFee=10,ResearchFee=2;
    public static string Name(TreatyKind kind)=>kind==TreatyKind.Research?"Współpraca badawcza":"Gwarancja pokoju";
    public static int Bond(TreatyKind kind)=>kind==TreatyKind.Research?50:80;
    public static bool Open(BilateralTreaty t)=>t.Status is TreatyStatus.Travelling or TreatyStatus.AwaitingPlayer;
    public static bool Active(GameState s,BilateralTreaty t)=>t.Status==TreatyStatus.Active&&t.SignedTurn<=s.Turn&&t.ExpiresTurn>s.Turn;
    public static bool Pair(BilateralTreaty t,int a,int b)=>t.From==a&&t.To==b||t.From==b&&t.To==a;
    public static int PlannedFees(GameState s,int owner)=>s.Treaties.Count(t=>Active(s,t)&&t.Kind==TreatyKind.Research&&(t.From==owner||t.To==owner))*ResearchFee;
    public static bool HasGuarantee(GameState s,int a,int b)=>s.Treaties.Any(t=>Active(s,t)&&Pair(t,a,b)&&t.Kind==TreatyKind.PeaceGuarantee);
    private static string? Context(GameState s,int from,int to,TreatyKind kind)
    {
        if(!s.EpochCampaign||from is <0 or >3||to is <0 or >3||from==to||from!=0&&to!=0||!Enum.IsDefined(kind)||!s.Cities.Any(c=>c.Owner==from)||!s.Cities.Any(c=>c.Owner==to))return "Potrzebne są dwa istniejące państwa i pełna kampania epok.";
        if(s.Relation(from,to).War)return "Traktat wymaga pokoju.";
        if(!s.Factions[from].Technologies.Contains("writing")||!s.Factions[to].Technologies.Contains("writing"))return "Obie strony muszą znać Pismo.";
        if(kind==TreatyKind.Research&&(s.Factions[from].TradePolicy==TradePolicy.Embargo||s.Factions[to].TradePolicy==TradePolicy.Embargo))return "Najpierw znieście embargo; badania wymagają otwartej wymiany.";
        return null;
    }
    public static int AcceptanceScore(GameState s,int other,TreatyKind kind,int duration)
    {
        var r=s.Relation(0,other);return r.Trust+(r.Trade?10:0)+(kind==TreatyKind.PeaceGuarantee?15:5)-(duration==24?10:0)-DiplomaticCouncil.Grievances(s,other,0)/2-(other==3?10:0);
    }
    public static string? OfferError(GameState s,int from,int to,TreatyKind kind,int duration)
    {
        if(s.Ending!=null||duration is not (12 or 24))return "Wybierz czas 12 lub 24 tur.";
        if(Context(s,from,to,kind) is{} error)return error;
        if(from==0&&s.Relation(from,to).LastActionTurn==s.Turn)return "Wykorzystano już rozmowę z tym państwem w tej turze.";
        if(s.Treaties.Any(t=>Pair(t,from,to)&&(Open(t)||Active(s,t)&&t.Kind==kind)))return "Obowiązuje już taka umowa lub trwają negocjacje z tym państwem.";
        if(s.Treaties.Any(t=>Pair(t,from,to)&&s.Turn-t.SentTurn<8))return "Nową propozycję można wysłać po ośmiu turach od poprzedniej.";
        if(new[]{from,to}.Any(id=>s.Treaties.Count(t=>(Active(s,t)||Open(t))&&(t.From==id||t.To==id))>=2))return "Jedna ze stron ma już dwa aktywne lub negocjowane traktaty.";
        return s.Factions[from].Gold<EmbassyFee?"Poselstwo kosztuje 10 złota.":null;
    }
    public static Result Propose(GameState s,int other,TreatyKind kind,int duration,int owner=0)
    {
        if(OfferError(s,owner,other,kind,duration) is{} error)return Result.Fail(error);
        s.Factions[owner].Gold-=EmbassyFee;var t=new BilateralTreaty{Id=s.NextTreatyId++,From=owner,To=other,Kind=kind,Duration=duration,SentTurn=s.Turn,DueTurn=s.Turn+2,DeadlineTurn=s.Turn+6,Report="Posłowie w drodze. Depozyty nie zostały jeszcze pobrane."};s.Treaties.Add(t);
        if(owner==0)s.Relation(owner,other).LastActionTurn=s.Turn;
        s.Log($"Propozycja #{t.Id}: {Name(kind)}, {s.Factions[owner].Name} → {s.Factions[other].Name}, {duration} tur. Poselstwo {EmbassyFee} złota; odpowiedź T{t.DueTurn}, podpis najpóźniej przed T{t.DeadlineTurn}.","traktaty");
        return Result.Ok("Wysłano propozycję. Depozyty będą pobrane dopiero przy obustronnej ratyfikacji.");
    }
    public static string? RatifyError(GameState s,BilateralTreaty t)
    {
        if(s.Ending!=null||t.Status!=TreatyStatus.AwaitingPlayer||s.Turn>=t.DeadlineTurn)return "Propozycja nie oczekuje na ważną ratyfikację.";
        if(Context(s,t.From,t.To,t.Kind) is{} error)return error;
        int other=t.From==0?t.To:t.From;
        if(s.Relation(0,other).LastActionTurn==s.Turn)return "Rozmowa z tym państwem została już wykorzystana w tej turze.";
        if(AcceptanceScore(s,other,t.Kind,t.Duration)<30)return "Sytuacja uległa zmianie; partner nie potwierdzi obecnie warunków.";
        if(new[]{t.From,t.To}.Any(id=>s.Factions[id].Gold<Bond(t.Kind)))return "Skarbiec jednej ze stron nie pokrywa depozytu. Niczego nie pobrano.";
        if(s.Treaties.Any(x=>x.Id!=t.Id&&Active(s,x)&&Pair(x,t.From,t.To)&&x.Kind==t.Kind))return "Taka umowa już obowiązuje.";
        if(new[]{t.From,t.To}.Any(id=>s.Treaties.Count(x=>x.Id!=t.Id&&Active(s,x)&&(x.From==id||x.To==id))>=2))return "Jedna ze stron osiągnęła limit aktywnych traktatów.";
        return null;
    }
    public static Result Ratify(GameState s,int id)
    {
        var t=s.Treaties.FirstOrDefault(t=>t.Id==id);if(t==null)return Result.Fail("Brak propozycji.");if(RatifyError(s,t) is{} error)return Result.Fail(error);
        int bond=Bond(t.Kind);s.Factions[t.From].Gold-=bond;s.Factions[t.To].Gold-=bond;t.EscrowFrom=t.EscrowTo=bond;t.SignedTurn=s.Turn;t.ExpiresTurn=s.Turn+t.Duration;t.Status=TreatyStatus.Active;
        s.Relation(t.From,t.To).LastActionTurn=s.Turn;t.Report=$"Podpisano. Każda strona złożyła {bond} złota. Wygaśnięcie przed gospodarką T{t.ExpiresTurn}.";s.Log(Name(t.Kind)+" #"+t.Id+": "+t.Report,"traktaty");return Result.Ok(t.Report);
    }
    public static Result Reject(GameState s,int id)
    {
        var t=s.Treaties.FirstOrDefault(t=>t.Id==id);if(s.Ending!=null||t==null||t.Status!=TreatyStatus.AwaitingPlayer)return Result.Fail("Brak propozycji do odrzucenia.");
        Close(s,t,TreatyStatus.Rejected,"Gracz odmówił ratyfikacji.");return Result.Ok(t.Report);
    }
    private static void Close(GameState s,BilateralTreaty t,TreatyStatus status,string reason,int? offender=null)
    {
        if(!Open(t)&&t.Status!=TreatyStatus.Active)return;
        int pool=t.EscrowFrom+t.EscrowTo;
        if(offender!=null)
        {
            int victim=offender==t.From?t.To:t.From;t.PayoutFrom=victim==t.From?pool:0;t.PayoutTo=victim==t.To?pool:0;
            var f=s.Factions[offender.Value];f.Stability-=t.Kind==TreatyKind.PeaceGuarantee?6:3;f.Prestige=Math.Max(0,f.Prestige-8);Engine.Clamp(f);
            var relation=s.Relation(t.From,t.To);relation.Trust=Math.Max(-100,relation.Trust-25);DiplomaticCouncil.AddGrievance(s,victim,offender.Value,30);
            foreach(var third in s.Factions.Where(f=>f.Id!=offender&&f.Id!=victim)){var r=s.Relation(offender.Value,third.Id);r.Trust=Math.Max(-100,r.Trust-8);}
        }
        else{t.PayoutFrom=t.EscrowFrom;t.PayoutTo=t.EscrowTo;}
        s.Factions[t.From].Gold+=t.PayoutFrom;s.Factions[t.To].Gold+=t.PayoutTo;t.EscrowFrom=t.EscrowTo=0;t.BreachedBy=offender;t.Status=status;t.ClosedTurn=s.Turn;t.Report=reason+$" Wypłata: {s.Factions[t.From].Name} {t.PayoutFrom}, {s.Factions[t.To].Name} {t.PayoutTo} złota.";
        s.Log(Name(t.Kind)+" #"+t.Id+": "+t.Report,"traktaty");
    }
    public static string BreachWarning(GameState s,int owner,int? other=null,bool researchOnly=false)
    {
        var rows=s.Treaties.Where(t=>Active(s,t)&&(t.From==owner||t.To==owner)&&(other==null||Pair(t,owner,other.Value))&&(!researchOnly||t.Kind==TreatyKind.Research)).ToArray();
        return rows.Length==0?"":$"Zerwanie {rows.Length} traktatów: utrata {rows.Sum(t=>Bond(t.Kind))} złota depozytów na rzecz partnerów, stabilność -{rows.Sum(t=>t.Kind==TreatyKind.PeaceGuarantee?6:3)}, prestiż do -{rows.Length*8}, relacje -25 i krzywdy +30 na umowę oraz zaufanie pozostałych państw -8 na umowę.";
    }
    public static Result Break(GameState s,int id,int owner=0)
    {
        var t=s.Treaties.FirstOrDefault(t=>t.Id==id);if(s.Ending!=null||t==null||!Active(s,t)||owner!=t.From&&owner!=t.To)return Result.Fail("Brak własnego aktywnego traktatu.");
        if(s.Relation(t.From,t.To).LastActionTurn==s.Turn)return Result.Fail("Rozmowę wykorzystano już w tej turze.");
        s.Relation(t.From,t.To).LastActionTurn=s.Turn;Close(s,t,TreatyStatus.Broken,"Jednostronne wypowiedzenie umowy.",owner);return Result.Ok(t.Report);
    }
    internal static void OnWar(GameState s,int aggressor,int other)
    {
        foreach(var t in s.Treaties.Where(t=>Pair(t,aggressor,other)&&(Active(s,t)||Open(t))).ToArray())
            Close(s,t,t.Status==TreatyStatus.Active?TreatyStatus.Broken:TreatyStatus.Cancelled,"Wojna zakończyła traktat lub negocjacje.",t.Status==TreatyStatus.Active?aggressor:null);
    }
    internal static void OnEmbargo(GameState s,int owner)
    {
        foreach(var t in s.Treaties.Where(t=>t.Kind==TreatyKind.Research&&(t.From==owner||t.To==owner)&&(Active(s,t)||Open(t))).ToArray())
            Close(s,t,t.Status==TreatyStatus.Active?TreatyStatus.Broken:TreatyStatus.Cancelled,"Embargo zakończyło współpracę badawczą.",t.Status==TreatyStatus.Active?owner:null);
    }
    internal static void TickDiplomacy(GameState s)
    {
        foreach(var t in s.Treaties.Where(t=>Open(t)||t.Status==TreatyStatus.Active).OrderBy(t=>t.Id).ToArray())
        {
            if(t.Status==TreatyStatus.Active&&s.Turn>=t.ExpiresTurn){Close(s,t,TreatyStatus.Completed,"Dotrzymano terminu umowy.");continue;}
            if(!s.Cities.Any(c=>c.Owner==t.From)||!s.Cities.Any(c=>c.Owner==t.To)){Close(s,t,TreatyStatus.Cancelled,"Jedna ze stron utraciła miasta; zwrot zabezpieczeń.");continue;}
            if(s.Relation(t.From,t.To).War){var war=WarCouncil.Current(s,t.From,t.To);Close(s,t,t.Status==TreatyStatus.Active&&war?.GoalDeclared==true?TreatyStatus.Broken:TreatyStatus.Cancelled,"Wojna przerwała porozumienie.",t.Status==TreatyStatus.Active&&war?.GoalDeclared==true?war.Aggressor:null);continue;}
            if(Open(t)&&s.Turn>=t.DeadlineTurn){Close(s,t,TreatyStatus.Expired,"Minął termin ratyfikacji; depozytów nie pobrano.");continue;}
            if(t.Status==TreatyStatus.Travelling&&s.Turn>=t.DueTurn)
            {
                t.ReplyTurn=s.Turn;int other=t.From==0?t.To:t.From;var error=Context(s,t.From,t.To,t.Kind);
                if(error!=null||AcceptanceScore(s,other,t.Kind,t.Duration)<30){Close(s,t,TreatyStatus.Rejected,error??"Partner odmówił po ocenie bieżącej sytuacji.");continue;}
                t.Status=TreatyStatus.AwaitingPlayer;t.Report="Partner potwierdza warunki. Czekamy na decyzję gracza; depozyty nadal w skarbcach.";
                s.Log(Name(t.Kind)+$" #{t.Id}: odpowiedź czeka do początku T{t.DeadlineTurn}.","traktaty");
            }
        }
    }
    internal static void EconomyPhase(GameState s,IReadOnlyDictionary<int,double> incomes)
    {
        foreach(var t in s.Treaties.Where(t=>Active(s,t)&&t.Kind==TreatyKind.Research&&t.LastPhaseTurn<s.Turn).OrderBy(t=>t.Id).ToArray())
        {
            if(s.Factions[t.From].TradePolicy==TradePolicy.Embargo||s.Factions[t.To].TradePolicy==TradePolicy.Embargo){OnEmbargo(s,s.Factions[t.From].TradePolicy==TradePolicy.Embargo?t.From:t.To);continue;}
            t.LastPhaseTurn=s.Turn;t.LastFee=0;t.LastScience=0;t.LastIncomeFrom=incomes.GetValueOrDefault(t.From);t.LastIncomeTo=incomes.GetValueOrDefault(t.To);t.LastBonusCap=5+Math.Min(s.Factions[t.From].Era,s.Factions[t.To].Era);
            bool a=s.Factions[t.From].Gold>=ResearchFee,b=s.Factions[t.To].Gold>=ResearchFee;t.MissedFrom=a?0:t.MissedFrom+1;t.MissedTo=b?0:t.MissedTo+1;
            if(!a||!b)
            {
                t.Report=$"Faza T{s.Turn}: brak finansowania; nie pobrano opłat i nie przyznano nauki. Niedobory stron: {t.MissedFrom}/3 i {t.MissedTo}/3.";
                if(t.MissedFrom>=3&&t.MissedTo>=3)Close(s,t,TreatyStatus.Cancelled,"Obie strony przez trzy fazy nie finansowały współpracy; zwrot depozytów.");
                else if(t.MissedFrom>=3||t.MissedTo>=3)Close(s,t,TreatyStatus.Broken,"Trzy kolejne fazy jednostronnego braku finansowania.",t.MissedFrom>=3?t.From:t.To);
                continue;
            }
            s.Factions[t.From].Gold-=ResearchFee;s.Factions[t.To].Gold-=ResearchFee;t.LastFee=ResearchFee;t.LastScience=Math.Min(t.LastBonusCap,Math.Max(0,Math.Min(t.LastIncomeFrom,t.LastIncomeTo))*.15);t.TotalScience+=t.LastScience;t.FundedPhases++;
            s.Factions[t.From].Science+=t.LastScience;s.Factions[t.To].Science+=t.LastScience;t.Report=$"Faza T{s.Turn}: każda strona opłaciła {ResearchFee} złota i uzyskała {t.LastScience:0.##} nauki z rzeczywistej pracy obu państw.";
        }
    }
    public static void ManageAi(GameState s,int owner)
    {
        // A first player contact opens this optional diplomatic channel; old campaigns remain passive until then.
        if(!s.EpochCampaign||!s.Treaties.Any()||s.Turn%20!=owner||s.Relation(0,owner).Trust<55)return;
        var kind=s.Treaties.Any(t=>Pair(t,0,owner)&&Active(s,t)&&t.Kind==TreatyKind.Research)?TreatyKind.PeaceGuarantee:TreatyKind.Research;
        if(s.Factions[owner].Gold>=Bond(kind)+EmbassyFee+40&&OfferError(s,owner,0,kind,12)==null)Propose(s,0,kind,12,owner);
    }
}
