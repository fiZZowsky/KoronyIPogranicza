namespace Crown.Core;

/// <summary>Petitions refer to real losses; promises are settled by delivered manifests, never by a timer reward.</summary>
public static class TradeCouncil
{
    public const int PromiseTurns=8,PetitionInterval=10;
    public const string SuccessEffects="Sukces: lud +6, stabilność +2, prestiż +5; w mieście dobrobyt +8, niepokoje -6.";
    public const string FailureEffects="Porażka: lud -10, stabilność -4, prestiż -5 (minimum 0); we własnym mieście dobrobyt -6, niepokoje +10. Utrata miasta także łamie obietnicę.";
    public static readonly NarrativeEvent Event=new("trade_loss","Toma Rell","Przedstawicielka kupców","Wozy, które nie wróciły",
        "Kupcy stracili opłacony ładunek. Rodziny pytają, czy rada pokryje stratę, zorganizuje zastępczą dostawę, czy pozostawi ryzyko po stronie nabywców.",
        [new("Wypłacimy odszkodowanie z naszego skarbu.","W mieście: dobrobyt +8, niepokoje -6. Towar nie zostaje odtworzony.",Gold:-40,Commons:5,Nobles:2),
         new("Dostarczymy zastępczy ładunek w ciągu ośmiu tur.",SuccessEffects+" "+FailureEffects,Commons:2),
         new("Rada nie zagwarantuje prywatnych przedsięwzięć.","W mieście: niepokoje +8.",Commons:-5,Nobles:-4)]);
    public static Caravan Source(GameState s,TradeCase c)=>s.Caravans.First(v=>v.Id==c.CaravanId);
    public static TradeContract Contract(GameState s,TradeCase c)=>s.TradeContracts.First(t=>t.Id==Source(s,c).ContractId);
    public static City City(GameState s,TradeCase c)=>s.Cities.First(city=>city.Id==Contract(s,c).BuyerCity);
    public static double Progress(GameState s,TradeCase c)=>Math.Min(Source(s,c).Quantity,s.Caravans.Where(v=>c.CreditedCaravans.Contains(v.Id)).Sum(v=>v.Quantity));
    public static string StatusName(TradeCaseStatus status)=>status switch
    {TradeCaseStatus.Pending=>"Czeka na radę",TradeCaseStatus.Compensated=>"Wypłacono odszkodowanie",TradeCaseStatus.Promised=>"Obietnica w toku",
     TradeCaseStatus.Fulfilled=>"Słowo dotrzymane",TradeCaseStatus.Broken=>"Złamana obietnica",TradeCaseStatus.Declined=>"Odmówiono pomocy",_=>"Sprawa zamknięta po utracie miasta"};
    public static string? ChoiceError(GameState s,int choice)
    {
        var c=s.TradeCases.FirstOrDefault(c=>c.CaravanId==s.PendingTradeCaseId&&c.Status==TradeCaseStatus.Pending);
        if(c==null||City(s,c).Owner!=0)return "Ta delegacja nie ma już prawidłowej sprawy Marchii.";
        if(choice==1&&s.TradeCases.Any(other=>other.Status==TradeCaseStatus.Promised&&City(s,other).Id==City(s,c).Id&&Contract(s,other).Good==Contract(s,c).Good))
            return "Dla tego miasta i towaru trwa już obietnica. Jednej dostawy nie można obiecać dwukrotnie.";
        return null;
    }
    public static NarrativeEvent Current(GameState s)
    {
        var c=s.TradeCases.FirstOrDefault(c=>c.CaravanId==s.PendingTradeCaseId);if(c==null)return Event;
        var source=Source(s,c);var city=City(s,c);var contract=Contract(s,c);int payment=(int)Math.Ceiling(source.Payment);
        var choices=Event.Choices.ToArray();choices[0]=choices[0] with {Gold=-payment,Text=$"Wypłacimy {payment} złota odszkodowania."};
        choices[1]=choices[1] with {Text=$"Dostarczymy {source.Quantity:0.##} jednostek: {Markets.Name(contract.Good)}, do fazy rynku T{s.Turn+PromiseTurns}.",
            Consequence=$"Liczą się przyjazdy zagraniczne do miasta {city.Name} po decyzji, również z karawan już w drodze. Sam zakup, zapasy i dostawy krajowe nie wystarczą. "+SuccessEffects+" "+FailureEffects+
                (ChoiceError(s,1) is string error?" Niedostępne: "+error:"")};
        return Event with {Title=Event.Title+" — "+city.Name,
            Body=$"Karawana #{source.Id} nie dotarła do odbiorcy w mieście {city.Name}. Utracono {source.Quantity:0.##} jednostek towaru: {Markets.Name(contract.Good)}; nabywca zapłacił {source.Payment:0.00} złota. Kupcy pytają, czy państwo odpowiada za bezpieczeństwo szlaku. Rodziny kupieckie czekają na Twoje słowo. Niepokoje rosną, a mieszkańcy tracą wiarę w ochronę traktów.",Choices=choices};
    }
    public static bool Schedule(GameState s)
    {
        var c=s.TradeCases.Where(c=>c.Status==TradeCaseStatus.Pending&&City(s,c).Owner==0).OrderBy(c=>c.OpenedTurn).ThenBy(c=>c.CaravanId).FirstOrDefault();
        if(c==null)return false;s.PendingEvent=Event.Id;s.PendingCityId=null;s.PendingTradeCaseId=c.CaravanId;return true;
    }
    public static Result Present(GameState s,int caravanId)
    {
        if(s.Ending!=null||s.PendingEvent!=null&&(s.PendingEvent!=Event.Id||s.PendingTradeCaseId!=caravanId))return Result.Fail("Najpierw rozstrzygnij aktualną sprawę rady.");
        var c=s.TradeCases.FirstOrDefault(c=>c.CaravanId==caravanId&&c.Status==TradeCaseStatus.Pending);
        if(c==null||City(s,c).Owner!=0)return Result.Fail("Nie ma takiej oczekującej delegacji Marchii.");
        s.PendingEvent=Event.Id;s.PendingCityId=null;s.PendingTradeCaseId=caravanId;return Result.Ok("Delegacja kupców czeka na odpowiedź rady.");
    }
    public static void Apply(GameState s,int choice)
    {
        var c=s.TradeCases.First(c=>c.CaravanId==s.PendingTradeCaseId);var city=City(s,c);
        c.Decision=choice;c.DecidedTurn=s.Turn;
        if(choice==1){c.Status=TradeCaseStatus.Promised;c.DeadlineTurn=s.Turn+PromiseTurns;c.AfterMarketTurn=s.LastMarketTurn;}
        else
        {
            c.Status=choice==0?TradeCaseStatus.Compensated:TradeCaseStatus.Declined;c.ResolvedTurn=s.Turn;
            city.Unrest=Math.Clamp(city.Unrest+(choice==0?-6:8),0,100);
            if(choice==0)city.Prosperity=Math.Clamp(city.Prosperity+8,0,100);
        }
    }
    private static void Settle(GameState s,TradeCase c,bool success)
    {
        c.Status=success?TradeCaseStatus.Fulfilled:TradeCaseStatus.Broken;c.ResolvedTurn=s.Turn;var f=s.Player;
        f.Commons+=success?6:-10;f.Stability+=success?2:-4;f.Prestige=Math.Max(0,f.Prestige+(success?5:-5));Engine.Clamp(f);
        var city=City(s,c);
        if(city.Owner==0){city.Prosperity=Math.Clamp(city.Prosperity+(success?8:-6),0,100);city.Unrest=Math.Clamp(city.Unrest+(success?-6:10),0,100);}
        s.Log($"{city.Name}: {StatusName(c.Status).ToLowerInvariant()} po utracie karawany #{c.CaravanId}. Przybyło {Progress(s,c):0.##}/{Source(s,c).Quantity:0.##}: {Markets.Name(Contract(s,c).Good)}. "+(success?SuccessEffects:FailureEffects),"konsekwencja");
    }
    public static void OwnershipChanged(GameState s,City city)
    {
        if(city.Owner==0)return;
        foreach(var c in s.TradeCases.Where(c=>City(s,c).Id==city.Id&&(c.Status==TradeCaseStatus.Pending||c.Status==TradeCaseStatus.Promised)))
        {
            if(c.Status==TradeCaseStatus.Promised)Settle(s,c,false);
            else{c.Status=TradeCaseStatus.Voided;c.ResolvedTurn=s.Turn;s.Log($"{city.Name}: zamknięto delegację kupców po utracie miasta.","konsekwencja");}
            if(s.PendingTradeCaseId==c.CaravanId){s.PendingTradeCaseId=null;if(s.PendingEvent==Event.Id)s.PendingEvent=null;}
        }
    }
    public static void Tick(GameState s)
    {
        foreach(var c in s.TradeCases.Where(c=>c.Status==TradeCaseStatus.Promised).OrderBy(c=>c.CaravanId).ToArray())
        {
            var contract=Contract(s,c);
            if(City(s,c).Owner!=0){Settle(s,c,false);continue;}
            foreach(var cargo in s.Caravans.Where(v=>v.Status==CaravanStatus.Delivered&&v.ResolvedTurn>=c.DecidedTurn&&v.ResolvedTurn>c.AfterMarketTurn&&v.ResolvedTurn<=c.DeadlineTurn).OrderBy(v=>v.ResolvedTurn).ThenBy(v=>v.Id))
            {
                var t=s.TradeContracts.First(t=>t.Id==cargo.ContractId);
                if(t.Buyer!=0||t.BuyerCity!=contract.BuyerCity||t.Good!=contract.Good||c.CreditedCaravans.Contains(cargo.Id))continue;
                c.CreditedCaravans.Add(cargo.Id);if(Progress(s,c)+.00001>=Source(s,c).Quantity)break;
            }
            if(Progress(s,c)+.00001>=Source(s,c).Quantity)Settle(s,c,true);
            else if(s.Turn>=c.DeadlineTurn)Settle(s,c,false);
        }
        foreach(var source in s.Caravans.Where(c=>c.Status==CaravanStatus.Lost&&c.ResolvedTurn==s.Turn).OrderBy(c=>c.Id))
        {
            var t=s.TradeContracts.First(t=>t.Id==source.ContractId);var city=s.Cities.First(c=>c.Id==t.BuyerCity);
            if(t.Buyer!=0||city.Owner!=0||s.TradeCases.Any(c=>c.CaravanId==source.Id||City(s,c).Id==city.Id&&s.Turn-c.OpenedTurn<PetitionInterval))continue;
            s.TradeCases.Add(new(){CaravanId=source.Id,OpenedTurn=s.Turn});city.Unrest=Math.Clamp(city.Unrest+4,0,100);s.Player.Commons-=2;Engine.Clamp(s.Player);
            s.Log($"{city.Name}: kupcy proszą radę o odpowiedź po utracie karawany #{source.Id}. Lud -2, miejscowe niepokoje +4.","handel");
        }
    }
}
