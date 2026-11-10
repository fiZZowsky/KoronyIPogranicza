namespace Crown.Core;

public enum InvestmentFunding { None, Public, Concession }
public sealed class FactoryInvestment
{
    public int Level {get;set;}
    public int PeakLevel {get;set;}
    public InvestmentFunding Funding {get;set;}
    public CraftRecipe Recipe {get;set;}
    public int StartedTurn {get;set;}
    public int LastDecisionTurn {get;set;}=-12;
    public int LastPhaseTurn {get;set;}
    public int Phases {get;set;}
    public int ProductivePhases {get;set;}
    public int Batches {get;set;}
    public int Spent {get;set;}
    public int PhaseFee {get;set;}
    public int RoyaltyRemaining {get;set;}
    public int RoyaltyFee {get;set;}
    public int RoyaltyPaid {get;set;}
    public int MissedRoyalties {get;set;}
    public int LastRoyaltyTurn {get;set;}
    public int CapacityLostTurn {get;set;}
    public string Report {get;set;}="Brak inwestycji przemysłowej.";
}

public static class IndustrialInvestment
{
    public const int Duration=8,RequiredBatches=6,RequiredPhases=4,RoyaltyTerm=24;
    public static string Name(InvestmentFunding funding)=>funding==InvestmentFunding.Public?"Inwestycja państwowa":"Koncesja kupiecka";
    public static int Signature(InvestmentFunding funding)=>funding==InvestmentFunding.Public?120:30;
    public static int Fee(GameState s,City c,InvestmentFunding funding)=>funding==InvestmentFunding.Public?5+s.Factions[c.Owner].Era/2:2;
    public static string? Error(GameState s,City? c,CraftRecipe recipe,InvestmentFunding funding,int owner=0)
    {
        if(s.Ending!=null||s.PendingEvent!=null||c==null||c.Owner!=owner||!s.EpochCampaign||!Enum.IsDefined(recipe)||!Enum.IsDefined(funding)||funding==InvestmentFunding.None)return "Nie można teraz zawrzeć umowy inwestycyjnej.";
        if(!c.Buildings.Contains(Manufacturing.Type(recipe).Facility))return "Najpierw zbuduj warsztat wybranego wyrobu.";
        var plan=c.Factory;
        if(plan.Level>=2)return "Zakłady osiągnęły dwa poziomy rozbudowy.";
        if(plan.Funding!=InvestmentFunding.None||plan.RoyaltyRemaining>0)return "Najpierw rozlicz obecną inwestycję i zobowiązania koncesji.";
        if(s.Turn-plan.LastDecisionTurn<12)return "Kolejna umowa jest dostępna dwanaście rozliczeń po poprzednim podpisie.";
        if(s.Factions[owner].Gold<Signature(funding))return "Brak środków na podpisanie umowy.";
        return null;
    }
    public static Result Begin(GameState s,int cityId,CraftRecipe recipe,InvestmentFunding funding,int owner=0)
    {
        var c=s.Cities.FirstOrDefault(c=>c.Id==cityId);if(Error(s,c,recipe,funding,owner) is{} error)return Result.Fail(error);
        var p=c!.Factory;s.Factions[owner].Gold-=Signature(funding);p.Funding=funding;p.Recipe=recipe;p.StartedTurn=p.LastDecisionTurn=s.Turn;
        p.Phases=p.ProductivePhases=p.Batches=p.LastPhaseTurn=0;p.PhaseFee=Fee(s,c,funding);p.Spent=Signature(funding);
        p.RoyaltyFee=funding==InvestmentFunding.Concession?6+s.Factions[owner].Era:0;p.RoyaltyPaid=p.MissedRoyalties=p.LastRoyaltyTurn=p.CapacityLostTurn=0;
        p.Report=$"{CampaignCalendar.At(s,s.Turn)}: {Name(funding)}, wyrób: {Manufacturing.Type(recipe).Name}. Podpis {Signature(funding)} złota; nadzór {p.PhaseFee}/rozliczenie. Cel: 6 partii w co najmniej 4 finansowanych fazach, przez 8 rozliczeń.";
        s.Log(c.Name+": "+p.Report,"decyzja");return Result.Ok("Podpisano umowę. Kupione towary nie zastąpią produkcji własnych warsztatów.");
    }
    public static double PlannedFees(GameState s,int owner)=>s.Cities.Where(c=>c.Owner==owner).Sum(c=>(c.Factory.Funding!=InvestmentFunding.None?c.Factory.PhaseFee:0)+(c.Factory.RoyaltyRemaining>0?c.Factory.RoyaltyFee:0));
    public static Result Cancel(GameState s,int cityId)
    {
        var c=s.Cities.FirstOrDefault(c=>c.Id==cityId&&c.Owner==0);
        if(s.Ending!=null||s.PendingEvent!=null||c==null||c.Factory.Funding==InvestmentFunding.None||c.Factory.LastDecisionTurn==s.Turn)return Result.Fail("Nie można teraz odwołać tej umowy.");
        var p=c.Factory;p.Funding=InvestmentFunding.None;p.LastDecisionTurn=s.Turn;c.Unrest=Math.Min(100,c.Unrest+4);
        p.Report="Inwestycję odwołano bez zwrotu wpłat. Niepokoje +4; urządzenia nie zostały rozbudowane.";s.Log(c.Name+": "+p.Report,"decyzja");return Result.Ok(p.Report);
    }
    public static void Tick(GameState s)
    {
        if(s.Cities.Any(c=>c.Owner==0&&(c.Factory.Funding!=InvestmentFunding.None||c.Factory.Level>0)))
            foreach(var c in s.Cities.Where(c=>c.Owner>0&&s.Turn%20==c.Id%20&&s.Factions[c.Owner].Gold>400&&c.IndustryShare>0).OrderBy(c=>c.Id))
                if(Error(s,c,c.IndustryRecipe,InvestmentFunding.Public,c.Owner)==null)Begin(s,c.Id,c.IndustryRecipe,InvestmentFunding.Public,c.Owner);
        foreach(var c in s.Cities.OrderBy(c=>c.Id))
        {
            var p=c.Factory;var f=s.Factions[c.Owner];
            // Royalties begin only in the phase after completion, never retroactively.
            if(p.RoyaltyRemaining>0&&p.LastRoyaltyTurn<s.Turn)
            {
                p.LastRoyaltyTurn=s.Turn;
                if(f.Gold>=p.RoyaltyFee){f.Gold-=p.RoyaltyFee;p.RoyaltyPaid+=p.RoyaltyFee;p.RoyaltyRemaining--;p.MissedRoyalties=0;}
                else p.MissedRoyalties++;
                if(p.MissedRoyalties>=3)
                {
                    p.RoyaltyRemaining=0;p.Level=Math.Max(0,p.Level-1);p.CapacityLostTurn=s.Turn;c.Unrest=Math.Min(100,c.Unrest+10);c.Loyalty=Math.Max(0,c.Loyalty-6);f.Prestige=Math.Max(0,f.Prestige-5);
                    p.Report=$"{CampaignCalendar.At(s,s.Turn)}: trzy niedobory opłaty koncesyjnej. Spółka odzyskała jeden poziom urządzeń; niepokoje +10, lojalność -6, prestiż -5.";s.Log(c.Name+": "+p.Report,"przemysł");
                }
                else if(p.RoyaltyRemaining==0){p.Report="Koncesja rozliczona: urządzenia pozostają w mieście bez dalszych opłat.";s.Log(c.Name+": "+p.Report,"przemysł");}
            }
            if(p.Funding==InvestmentFunding.None||p.LastPhaseTurn>=s.Turn)continue;
            p.LastPhaseTurn=s.Turn;p.Phases++;bool funded=f.Gold>=p.PhaseFee;
            if(funded){f.Gold-=p.PhaseFee;p.Spent+=p.PhaseFee;}
            int batches=funded&&c.LastIndustryTurn==s.Turn&&c.LastIndustryRecipe==p.Recipe?c.LastIndustryBatches:0;
            if(batches>0){p.ProductivePhases++;p.Batches+=batches;}
            p.Report=$"{CampaignCalendar.At(s,s.Turn)}: "+(funded?$"nadzór opłacony {p.PhaseFee} złota; zaliczono {batches} partii.":"brak środków na nadzór; produkcja nie zalicza się do umowy.");
            if(p.Phases<Duration)continue;
            var funding=p.Funding;p.Funding=InvestmentFunding.None;
            bool success=p.Batches>=RequiredBatches&&p.ProductivePhases>=RequiredPhases;
            if(success)
            {
                p.Level++;p.PeakLevel=Math.Max(p.PeakLevel,p.Level);c.Prosperity=Math.Min(100,c.Prosperity+5);
                if(funding==InvestmentFunding.Public){f.Commons=Math.Min(100,f.Commons+4);f.Scholars=Math.Min(100,f.Scholars+3);}
                else {f.Nobles=Math.Min(100,f.Nobles+6);f.Commons=Math.Max(0,f.Commons-4);p.RoyaltyRemaining=RoyaltyTerm;p.LastRoyaltyTurn=s.Turn;}
                p.Report+=$" Rozbudowa zakończona: poziom {p.Level}, limit przetwórstwa +1 partia/fazę; dobrobyt +5. "+(funding==InvestmentFunding.Public?"Mieszkańcy +4, uczeni +3.":$"Rody +6, mieszkańcy -4; koncesja wymaga następnych 24 wpłat po {p.RoyaltyFee} złota.");
            }
            else {c.Unrest=Math.Min(100,c.Unrest+6);c.Loyalty=Math.Max(0,c.Loyalty-4);p.Report+=" Umowa niewykonana: bez rozbudowy i zwrotu wpłat; niepokoje +6, lojalność -4.";}
            s.Log(c.Name+": "+p.Report,"przemysł");
        }
    }
    public static void OwnershipChanged(City c)=>c.Factory=new(){Level=c.Factory.Level,PeakLevel=c.Factory.Level,Report="Zmieniła się administracja. Urządzenia zachowano; dawne umowy i koncesje nie obciążają zdobywcy."};
    public static void Validate(GameState s)
    {
        foreach(var c in s.Cities)
        {
            if(s.Version<28){c.Factory=new();continue;}
            var p=c.Factory;
            if(p!=null&&(p.PeakLevel<p.Level||p.PeakLevel>2))throw new InvalidDataException("Nieprawidłowa historia zdolności zakładu.");
            if(p==null||p.Level is <0 or >2||!Enum.IsDefined(p.Funding)||!Enum.IsDefined(p.Recipe)||p.StartedTurn<0||p.StartedTurn>s.Turn||p.LastDecisionTurn< -12||p.LastDecisionTurn>s.Turn||p.LastPhaseTurn<0||p.LastPhaseTurn>s.Turn||p.Phases is <0 or >8||p.ProductivePhases<0||p.ProductivePhases>p.Phases||p.Batches<0||p.Batches>80||p.Spent<0||p.Spent>1000||p.PhaseFee is <0 or >13||p.RoyaltyRemaining is <0 or >24||p.RoyaltyFee is <0 or >23||p.RoyaltyPaid<0||p.RoyaltyPaid>24*p.RoyaltyFee||p.MissedRoyalties is <0 or >3||p.LastRoyaltyTurn<0||p.LastRoyaltyTurn>s.Turn||p.Report==null||p.Report.Length>2500)
                throw new InvalidDataException("Nieprawidłowy rejestr inwestycji przemysłowej.");
            if(p.Level>0&&!Manufacturing.Facility(c)||p.Funding!=InvestmentFunding.None&&(p.Level>=2||p.StartedTurn<1||p.Phases>=8||p.RoyaltyRemaining>0||!c.Buildings.Contains(Manufacturing.Type(p.Recipe).Facility)||p.Spent<Signature(p.Funding)||p.Spent>Signature(p.Funding)+p.Phases*p.PhaseFee)||p.RoyaltyRemaining>0&&(p.Level<1||p.RoyaltyFee<6||p.Funding!=InvestmentFunding.None)||p.ProductivePhases>0&&p.Batches<p.ProductivePhases||p.Level>0&&!s.EpochCampaign)
                throw new InvalidDataException("Niespójna umowa przemysłowa.");
            if(p.CapacityLostTurn<0||p.CapacityLostTurn>s.Turn||p.CapacityLostTurn>0&&(p.MissedRoyalties!=3||p.RoyaltyRemaining!=0||p.LastRoyaltyTurn!=p.CapacityLostTurn||p.RoyaltyFee<6))throw new InvalidDataException("Nieprawidłowe rozliczenie odebranych urządzeń.");
            if(p.StartedTurn>0)
            {
                int signature=p.RoyaltyFee>0?30:120;
                if(p.PhaseFee<1||p.Spent<signature||(p.Spent-signature)%p.PhaseFee!=0||(p.Spent-signature)/p.PhaseFee>p.Phases||(p.Spent-signature)/p.PhaseFee<p.ProductivePhases||p.Batches>p.ProductivePhases*(3+s.Factions[c.Owner].Era/4)||p.RoyaltyFee>0&&p.PhaseFee!=2||p.RoyaltyFee==0&&p.PhaseFee is <5 or >13)
                    throw new InvalidDataException("Nieprawidłowe dowody finansowania inwestycji.");
            }
            if(p.RoyaltyRemaining>0&&(p.Phases!=8||p.Batches<6||p.ProductivePhases<4||p.LastRoyaltyTurn<p.StartedTurn+7)||p.RoyaltyFee>0&&p.RoyaltyPaid%p.RoyaltyFee!=0||p.RoyaltyFee==0&&p.RoyaltyPaid!=0)
                throw new InvalidDataException("Nieprawidłowe dowody koncesji.");
            if(p.RoyaltyRemaining>0&&p.RoyaltyPaid!=(24-p.RoyaltyRemaining)*p.RoyaltyFee)throw new InvalidDataException("Niespójne wpłaty koncesji.");
        }
    }
}
