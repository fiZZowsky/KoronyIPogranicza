namespace Crown.Core;

public enum CraftRecipe { Tools,Textiles }
public sealed record CraftInput(int CityId,TradeGood Good,double Quantity);
public sealed record CraftType(CraftRecipe Recipe,string Name,string Facility,TradeGood Input,int InputAmount,TradeGood Output,int OutputAmount,int Work);
public sealed record CraftQuote(int Batches,double Work,IReadOnlyList<CraftInput> Inputs,string? Error=null);

public static class Manufacturing
{
    public const string Toolmakers="toolmakers",Weavers="weavers",Guildhall="guildhall";
    public static readonly CraftType[] Recipes=[
        new(CraftRecipe.Tools,"Narzędzia",Toolmakers,TradeGood.Iron,2,TradeGood.Tools,1,6),
        new(CraftRecipe.Textiles,"Tkaniny",Weavers,TradeGood.Dyes,1,TradeGood.Textiles,2,4)
    ];
    public static CraftType Type(CraftRecipe recipe)=>Recipes.Single(r=>r.Recipe==recipe);
    public static bool Facility(City c)=>c.Buildings.Contains(Toolmakers)||c.Buildings.Contains(Weavers);
    public static bool Project(string id)=>id is Toolmakers or Weavers or Guildhall;
    public static int Limit(GameState s,City c)=>1+s.Factions[c.Owner].Era/4+(c.Factory?.Level??0);
    public static double Upkeep(GameState s,int owner)=>s.Cities.Where(c=>c.Owner==owner).Sum(c=>c.Buildings.Count(Project))*1.5;
    public static string? ProjectError(GameState s,string id)=>Project(id)&&!s.EpochCampaign?"Warsztaty przetwórcze i Dom cechów wymagają pełnej kampanii epok.":null;
    private static double Coverage(City c,TradeGood good)=>c.LastMarketLines.FirstOrDefault(l=>l.Good==good&&l.Demand>0)?.Coverage??0;
    public static double ProductionBonus(City c)=>c.Buildings.Contains(Guildhall)&&c.LastMarketTurn>0?.15*Coverage(c,TradeGood.Tools):0;
    public static double CultureBonus(City c)=>c.Buildings.Contains(Guildhall)&&c.LastMarketTurn>0?3*Coverage(c,TradeGood.Textiles):0;
    public static int ProsperityBonus(City c)=>c.Buildings.Contains(Guildhall)&&c.LastMarketTurn>0?(int)Math.Round(3*Coverage(c,TradeGood.Textiles)):0;
    private static City[] Region(GameState s,City c)
    {
        var network=Logistics.For(s,c.Owner);if(network.Interdicted.Contains(c.Tile))return [];
        int group=network.MarketGroups[c.Id];return s.Cities.Where(x=>x.Owner==c.Owner&&!network.Interdicted.Contains(x.Tile)&&network.MarketGroups[x.Id]==group).OrderBy(x=>x.Id==c.Id?0:1).ThenBy(x=>x.Id).ToArray();
    }
    public static CraftQuote Quote(GameState s,City c,double available)
    {
        CraftQuote Block(string reason)=>new(0,0,[],reason);
        if(!s.EpochCampaign||!Enum.IsDefined(c.IndustryRecipe)||!c.Buildings.Contains(Type(c.IndustryRecipe).Facility))return Block("Brak warsztatu dla wybranego wyrobu.");
        if(c.ProductionPaused)return Block("Produkcja miasta jest wstrzymana.");
        if(c.IndustryShare==0)return Block("Nakład przetwórstwa jest wyłączony.");
        if(SiegeWarfare.Report(s,c).Encircled)return Block("Pełne oblężenie zatrzymuje przetwórstwo.");
        var type=Type(c.IndustryRecipe);double budget=Math.Max(0,available)*c.IndustryShare/100.0;
        int workBatches=(int)Math.Min(Limit(s,c),Math.Floor((budget+1e-8)/type.Work));
        if(workBatches<1)return Block("Za mało pozostałej pracy na pełną partię.");
        int room=(int)Math.Floor((Math.Max(0,Markets.Capacity(c)-c.MarketStocks[type.Output])+1e-8)/type.OutputAmount);
        if(room<1)return Block("Magazyn wyrobu jest pełny.");
        var region=Region(s,c);double stock=region.Sum(x=>x.MarketStocks[type.Input]);
        int batches=Math.Min(workBatches,Math.Min(room,(int)Math.Floor((stock+1e-8)/type.InputAmount)));
        if(batches<1)return Block("Brak składników w bezpiecznej sieci własnych miast. Wymiana z zagranicą wymaga przyjazdu karawany.");
        double need=batches*type.InputAmount;var inputs=new List<CraftInput>();
        foreach(var source in region){double quantity=Math.Min(need,source.MarketStocks[type.Input]);if(quantity>1e-8){inputs.Add(new(source.Id,type.Input,quantity));need-=quantity;}if(need<1e-8)break;}
        return new(batches,batches*type.Work,inputs);
    }
    public static double ForecastWork(GameState s,City c,double available)=>Quote(s,c,available).Work;
    public static string? PolicyError(GameState s,City? c,CraftRecipe recipe,int share,int owner=0)
    {
        if(s.Ending!=null||owner is <0 or >3||c==null||c.Owner!=owner||!s.EpochCampaign||!Enum.IsDefined(recipe)||share is not (0 or 25 or 50)||!c.Buildings.Contains(Type(recipe).Facility))return "Wybierz własne warsztaty, wyrób i nakład 0%, 25% lub 50%.";
        if(c.IndustryRecipe==recipe&&c.IndustryShare==share)return "Ten plan już obowiązuje.";
        return c.LastIndustryPolicyTurn==s.Turn?"Plan warsztatów zmieniono już w tej turze.":null;
    }
    public static Result SetPolicy(GameState s,int cityId,CraftRecipe recipe,int share,int owner=0)
    {
        var c=s.Cities.FirstOrDefault(c=>c.Id==cityId);if(PolicyError(s,c,recipe,share,owner) is{} error)return Result.Fail(error);
        c!.IndustryRecipe=recipe;c.IndustryShare=share;c.LastIndustryPolicyTurn=s.Turn;
        if(owner==0)s.Log($"{c.Name}: warsztaty wytwarzają {Type(recipe).Name.ToLowerInvariant()}, nakład {share}% pozostałej pracy. Surowce i praca zostaną rozliczone w zwykłej turze.","przetwórstwo");
        return Result.Ok("Plan zatwierdzony. Produkcja i zużycie składników nastąpią dopiero w fazie gospodarki.");
    }
    internal static double BuildPhase(GameState s,City c,double available)
    {
        if(!s.EpochCampaign||!Facility(c)||c.LastIndustryTurn==s.Turn)return 0;
        var quote=Quote(s,c,available);var type=Type(c.IndustryRecipe);
        c.LastIndustryTurn=s.Turn;c.LastIndustryRecipe=c.IndustryRecipe;c.LastIndustryBatches=quote.Batches;c.LastIndustryWork=quote.Work;c.LastIndustryInputs=quote.Inputs.ToList();c.LastIndustryMessage=quote.Error??"Wyroby trafiły do magazynu; będą dostępne dla rynku w następnej turze.";
        foreach(var input in quote.Inputs)
        {
            var source=s.Cities.Single(x=>x.Id==input.CityId);source.MarketStocks[input.Good]=Math.Max(0,source.MarketStocks[input.Good]-input.Quantity);
            int index=source.LastMarketLines.FindIndex(l=>l.Good==input.Good);var line=source.LastMarketLines[index];
            source.LastMarketLines[index]=line with {IndustrialUsed=line.IndustrialUsed+input.Quantity,Closing=source.MarketStocks[input.Good]};
        }
        if(quote.Batches>0)
        {
            double produced=quote.Batches*type.OutputAmount;c.MarketStocks[type.Output]=Math.Min(Markets.Capacity(c),c.MarketStocks[type.Output]+produced);
            int index=c.LastMarketLines.FindIndex(l=>l.Good==type.Output);var line=c.LastMarketLines[index];c.LastMarketLines[index]=line with {Produced=line.Produced+produced,Closing=c.MarketStocks[type.Output]};
            if(c.Owner==0)s.Log($"{c.Name}: {quote.Batches} partii — {produced:0} {type.Name.ToLowerInvariant()}, koszt {quote.Work:0} pracy i {quote.Batches*type.InputAmount:0} {Markets.Name(type.Input).ToLowerInvariant()}.","przetwórstwo");
        }
        return quote.Work;
    }
    internal static void Reset(City c)
    {c.IndustryRecipe=c.LastIndustryRecipe=c.Buildings.Contains(Weavers)&&!c.Buildings.Contains(Toolmakers)?CraftRecipe.Textiles:CraftRecipe.Tools;c.IndustryShare=0;c.LastIndustryPolicyTurn=-1;c.LastIndustryTurn=c.LastIndustryBatches=0;c.LastIndustryWork=0;c.LastIndustryInputs=[];c.LastIndustryMessage="";}
    internal static void Migrate(GameState s)
    {
        if(s.Version>=24)return;
        foreach(var c in s.Cities)
        {
            Reset(c);
            if(c.MarketStocks==null||c.LastMarketLines==null||c.LastMarketLines.Any(l=>l.IndustrialUsed!=0))throw new InvalidDataException("Uszkodzone stare dane rynku.");
            foreach(var good in new[]{TradeGood.Tools,TradeGood.Textiles})
            {
                if(c.MarketStocks.TryGetValue(good,out double value)&&value!=0)throw new InvalidDataException("Starszy format nie zawiera zapasów wyrobów.");
                c.MarketStocks.TryAdd(good,0);
                if(c.LastMarketTurn>0&&!c.LastMarketLines.Any(l=>l.Good==good))c.LastMarketLines.Add(new(good,0,0,0,0,0,0,0,0,0,0,Markets.BasePrice(good)));
            }
        }
    }
    internal static bool AiProject(GameState s,City c,string id)
    {
        if(!Project(id))return true;
        if(!s.EpochCampaign||s.Factions[c.Owner].Gold<250||Epochs.Programs.Any(p=>p.Id==c.Project))return false;
        if(s.Cities.Count(x=>x.Owner==c.Owner&&x.Id!=c.Id&&(x.Buildings.Contains(id)||x.Project==id))>=Math.Min(3,1+s.Cities.Count(x=>x.Owner==c.Owner)/4))return false;
        var region=Region(s,c);
        return id==Toolmakers?region.Sum(x=>x.MarketStocks[TradeGood.Iron])>=6:id==Weavers?region.Sum(x=>x.MarketStocks[TradeGood.Dyes])>=3:region.Sum(x=>x.MarketStocks[TradeGood.Tools])>=2&&region.Sum(x=>x.MarketStocks[TradeGood.Textiles])>=2;
    }
    internal static void ManageAi(GameState s,int owner)
    {
        if(!s.EpochCampaign||owner is <1 or >3)return;
        foreach(var c in s.Cities.Where(c=>c.Owner==owner&&Facility(c)))
        {
            bool program=Epochs.Programs.Any(p=>p.Id==c.Project);
            var candidates=Recipes.Where(r=>c.Buildings.Contains(r.Facility)).Select(r=>(Type:r,Stock:Region(s,c).Sum(x=>x.MarketStocks[r.Input]),Output:c.MarketStocks[r.Output])).ToArray();
            var usable=candidates.Where(x=>x.Stock>=x.Type.InputAmount&&x.Output+x.Type.OutputAmount<=Markets.Capacity(c)).ToArray();
            var best=(usable.Length>0?usable:candidates).OrderByDescending(x=>x.Stock/x.Type.InputAmount-x.Output).ThenBy(x=>x.Type.Recipe).First();
            double potential=Engine.CityYields(s,c).Production,district=Districts.Allocation(s,c,potential),supply=FieldLogistics.Allocation(s,c,Math.Max(0,potential-district));
            int share=program||usable.Length==0||best.Output>=Markets.Capacity(c)*.8?0:(potential-district-supply)*.25>=best.Type.Work?25:50;
            if(PolicyError(s,c,best.Type.Recipe,share,owner)==null)SetPolicy(s,c.Id,best.Type.Recipe,share,owner);
        }
    }
    internal static void Validate(GameState s)
    {
        bool ValidAmount(double x)=>double.IsFinite(x)&&x>=0&&x<=100000;
        if(!s.EpochCampaign&&(s.Cities.Any(c=>c.MarketStocks[TradeGood.Tools]!=0||c.MarketStocks[TradeGood.Textiles]!=0)||s.TradeContracts.Any(c=>c.Good is TradeGood.Tools or TradeGood.Textiles)))throw new InvalidDataException("Wyroby wymagają rozszerzonej kampanii epok.");
        if(s.Version<24&&(s.Cities.Any(c=>c.Buildings.Any(Project)||Project(c.Project)||c.ProductionQueue.Any(o=>Project(o.Project)))||s.TradeContracts.Any(c=>c.Good is TradeGood.Tools or TradeGood.Textiles)))throw new InvalidDataException("Starszy format nie zawiera przetwórstwa i umów na wyroby.");
        foreach(var c in s.Cities)
        {
            if(!Enum.IsDefined(c.IndustryRecipe)||!Enum.IsDefined(c.LastIndustryRecipe)||c.IndustryShare is not (0 or 25 or 50)||c.LastIndustryPolicyTurn< -1||c.LastIndustryPolicyTurn>s.Turn||c.LastIndustryTurn<0||c.LastIndustryTurn>=s.Turn||c.LastIndustryBatches<0||c.LastIndustryBatches>1+s.Factions[c.Owner].Era/4+(c.Factory?.PeakLevel??0)||!ValidAmount(c.LastIndustryWork)||c.LastIndustryInputs==null||c.LastIndustryMessage==null||c.LastIndustryMessage.Length>500||c.LastIndustryInputs.Select(i=>i.CityId).Distinct().Count()!=c.LastIndustryInputs.Count)
                throw new InvalidDataException("Uszkodzony plan lub raport warsztatów.");
            if(c.Buildings.Any(Project)&&!s.EpochCampaign||(c.IndustryShare>0||c.LastIndustryPolicyTurn>=0)&&!c.Buildings.Contains(Type(c.IndustryRecipe).Facility))throw new InvalidDataException("Brak warsztatu dla nakładu przetwórstwa.");
            var type=Type(c.LastIndustryRecipe);
            if(c.LastIndustryTurn==0?(c.LastIndustryBatches!=0||c.LastIndustryWork!=0||c.LastIndustryInputs.Count!=0||c.LastIndustryMessage!=""):
                !Facility(c)||Math.Abs(c.LastIndustryWork-c.LastIndustryBatches*type.Work)>.00001||c.LastIndustryInputs.Any(i=>i.Good!=type.Input||!s.Cities.Any(x=>x.Id==i.CityId)||!ValidAmount(i.Quantity)||i.Quantity<=0)||Math.Abs(c.LastIndustryInputs.Sum(i=>i.Quantity)-c.LastIndustryBatches*type.InputAmount)>.00001)
                throw new InvalidDataException("Niezgodny bilans przetwórstwa.");
        }
    }
}
