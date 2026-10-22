namespace Crown.Core;

public static class PeaceCouncil
{
    public const int EmbassyPrice=20;
    public const int AcceptanceThreshold=50;
    public static readonly int[] Payments=[-200,-100,0,100,200];
    public static bool Active(GameState s,int a,int b)=>WarCouncil.Current(s,a,b) is { EndedTurn:null };

    public static string Terms(GameState s,int from,int to,int gold,int? cityId)
    {
        string payment=gold==0?"Bez reparacji":$"{s.Factions[gold>0?from:to].Name} płaci {Math.Abs(gold)} złota państwu {s.Factions[gold>0?to:from].Name}";
        var city=s.Cities.FirstOrDefault(c=>c.Id==cityId);
        return payment+(city==null?" · bez zwrotu miast":$" · {city.Name} wraca do {s.Factions[city.OriginalOwner].Name}")+" · rozejm 12 tur";
    }

    public static IEnumerable<City> ReturnableCities(GameState s,int a,int b)=>s.Cities.Where(c=>
        (c.Owner==a&&c.OriginalOwner==b||c.Owner==b&&c.OriginalOwner==a)&&s.Cities.Count(home=>home.Owner==c.Owner)>1).OrderBy(c=>c.Id);

    public static PeacePreview Preview(GameState s,int from,int to,int gold,int? cityId)
    {
        PeacePreview Fail(string message)=>new(false,message,0,[]);
        if(s.Ending!=null||from<0||from>3||to<0||to>3||from==to||!Payments.Contains(gold))return Fail("Nieprawidłowe warunki pokoju.");
        if(!s.Relation(from,to).War||!s.Cities.Any(c=>c.Owner==from)||!s.Cities.Any(c=>c.Owner==to))return Fail("Negocjacje wymagają wojny i miast po obu stronach.");
        if(cityId!=null&&!ReturnableCities(s,from,to).Any(c=>c.Id==cityId))return Fail("Miasto nie jest już zajęte przez drugą stronę lub jest jej ostatnim miastem.");
        if(s.Factions[gold>=0?from:to].Gold<Math.Abs(gold))return Fail("Płatnik nie ma środków na reparacje. Warunki nie zostaną wykonane częściowo.");
        var war=WarCouncil.Current(s,from,to);
        int score=war==null?0:WarCouncil.Score(s,war,to).Score;
        int duration=war==null?0:Math.Min(20,(s.Turn-war.StartedTurn)*2);
        int exhaustion=(int)(s.Factions[to].WarWeariness*.6);
        int battlefield=(int)(-score*.8);
        int relationship=s.Relation(from,to).Trust/10;
        int payment=gold/5;
        var city=s.Cities.FirstOrDefault(c=>c.Id==cityId);
        int territory=city==null?0:city.OriginalOwner==to?30:-30;
        int acceptance=35+duration+exhaustion+battlefield+relationship+payment+territory;
        return new(true,"",acceptance,[$"Gotowość bazowa 35",$"Czas trwania wojny +{duration}",$"Wyczerpanie rozmówcy +{exhaustion}",
            $"Sytuacja wojskowa {battlefield:+0;-0;0}",$"Relacje {relationship:+0;-0;0}",$"Reparacje {payment:+0;-0;0}",$"Zwrot miasta {territory:+0;-0;0}"]);
    }

    internal static PeaceOffer Create(GameState s,WarCampaign war,int from,int to,int gold,int? cityId,int? inReplyTo=null)
    {
        var offer=new PeaceOffer {Id=s.NextPeaceId++,WarId=war.Id,From=from,To=to,Gold=gold,ReturnCityId=cityId,InReplyTo=inReplyTo,
            SentTurn=s.Turn,DueTurn=s.Turn+2,DeadlineTurn=s.Turn+6,Status=PeaceStatus.Travelling};
        s.PeaceOffers.Add(offer);s.Relation(from,to).LastPeaceTurn=s.Turn;
        s.Log($"{s.Factions[from].Leader} wysyła warunki pokoju do {s.Factions[to].Name}: {Terms(s,from,to,gold,cityId)}. Posłowie dotrą w turze {offer.DueTurn}.","negocjacje");
        return offer;
    }

    internal static void Settle(GameState s,WarCampaign war,int from,int to,int gold,int? cityId)
    {
        // Callers validate the entire treaty immediately before settlement.
        s.Factions[from].Gold-=gold;s.Factions[to].Gold+=gold;
        Withdraw(s,from,to);Withdraw(s,to,from);
        if(cityId is int id)
        {var city=s.Cities.First(c=>c.Id==id);ProvincialPolitics.Transfer(s,city,city.OriginalOwner,peaceful:true);}
        var r=s.Relation(from,to);r.War=false;r.Trade=false;r.TruceUntil=s.Turn+12;r.Trust=Math.Max(-30,r.Trust);
        foreach(int owner in new[]{from,to})
        {s.Factions[owner].WarWeariness=Math.Max(0,s.Factions[owner].WarWeariness-15);DiplomaticCouncil.AddGrievance(s,owner,owner==from?to:from,-20);}
        Withdraw(s,from,to);Withdraw(s,to,from);
        int goldToAggressor=war.Aggressor==to?gold:-gold;
        EscortOrders.Normalize(s);
        WarCouncil.Finish(s,war,$"Traktat {s.Factions[from].Name} – {s.Factions[to].Name}: {Terms(s,from,to,gold,cityId)}.",goldToAggressor);
        if(from==0||to==0)s.Player.Flags.Add("negotiated_peace");
        s.InvalidateLogistics();Engine.Explore(s);Engine.CheckEnding(s);
    }

    private static void Withdraw(GameState s,int owner,int other)
    {
        foreach(var unit in s.Units.Where(u=>u.Owner==owner).ToArray())
        {
            if(unit.Destination is int destination&&s.Tiles[destination].Owner==other)unit.Destination=null;
            if(s.Tiles[unit.Tile].Owner!=other)continue;
            // The other signatory will withdraw too; its troops must not block this evacuation.
            var refuge=s.Cities.Where(c=>c.Owner==owner&&!s.Units.Any(u=>u.Owner!=owner&&u.Owner!=other&&u.Tile==c.Tile))
                .OrderBy(c=>World.Distance(s.Tiles[unit.Tile],s.Tiles[c.Tile])).ThenBy(c=>c.Id).FirstOrDefault();
            if(refuge==null)s.Units.Remove(unit);
            else{unit.Tile=refuge.Tile;unit.Destination=null;unit.Actions=0;}
        }
    }

    public static void Tick(GameState s)
    {
        WarCouncil.FinishEliminated(s);
        foreach(var offer in s.PeaceOffers.Where(WarCouncil.IsOpen).OrderBy(o=>o.Id).ToArray())
        {
            var war=s.Wars.First(w=>w.Id==offer.WarId);
            if(war.EndedTurn!=null||!s.Relation(offer.From,offer.To).War)
            {Close(s,offer,PeaceStatus.Cancelled,"Wojna zakończyła się przed ratyfikacją.");continue;}
            if(s.Turn>=offer.DeadlineTurn){Close(s,offer,PeaceStatus.Expired,"Termin odpowiedzi upłynął. Wojna trwa dalej.");continue;}
            if(offer.Status!=PeaceStatus.Travelling||offer.DueTurn>s.Turn)continue;
            var preview=Preview(s,offer.From,offer.To,offer.Gold,offer.ReturnCityId);
            if(!preview.Valid){Close(s,offer,PeaceStatus.Cancelled,"Warunki utraciły ważność: "+preview.Error);continue;}
            if(offer.To==0)
            {offer.Status=PeaceStatus.AwaitingPlayer;offer.Reply="„Nasza rada proponuje zakończenie wojny. Czekamy na waszą odpowiedź.”";s.Log($"{s.Factions[offer.From].Leader}: {offer.Reply} Termin: tura {offer.DeadlineTurn}.","negocjacje");continue;}
            if(preview.Acceptance>=AcceptanceThreshold)
            {
                Close(s,offer,PeaceStatus.Accepted,"„Przyjmujemy warunki. Niech posłowie opieczętują traktat.”");
                Settle(s,war,offer.From,offer.To,offer.Gold,offer.ReturnCityId);
            }
            else
            {
                Close(s,offer,PeaceStatus.Rejected,"„Nie przyjmiemy tych warunków. Nasza rada oczekuje korzystniejszego porozumienia.”");
                MaybeOffer(s,offer.To,offer.From,offer.Id);
            }
        }
    }

    private static void Close(GameState s,PeaceOffer offer,PeaceStatus status,string text)
    {
        offer.Status=status;offer.ResolvedTurn=s.Turn;offer.Reply=text;
        s.Log($"Rozmowy {s.Factions[offer.From].Name} – {s.Factions[offer.To].Name}: {text}","negocjacje");
    }

    internal static void MaybeOffer(GameState s,int from,int to,int? replyTo=null)
    {
        var war=WarCouncil.Current(s,from,to);if(war==null||to!=0||from==0||s.Ending!=null)return;
        var r=s.Relation(from,to);
        if(s.PeaceOffers.Any(o=>o.WarId==war.Id&&WarCouncil.IsOpen(o))||replyTo==null&&s.Turn-r.LastPeaceTurn<8)return;
        int advantage=WarCouncil.Score(s,war,from).Score;
        if(replyTo==null&&(s.Turn-war.StartedTurn<8||s.Factions[from].WarWeariness<35&&advantage> -20))return;
        int gold=advantage>=20?-100:advantage<=-20&&s.Factions[from].Gold>=100?100:0;
        if(!Preview(s,from,to,gold,null).Valid)gold=0;
        if(Preview(s,from,to,gold,null).Valid&&s.Factions[from].Gold>=EmbassyPrice+Math.Max(0,gold))
        {s.Factions[from].Gold-=EmbassyPrice;Create(s,war,from,to,gold,null,replyTo);}
    }

    internal static void AiArmistice(GameState s,Relation r)
    {
        var war=WarCouncil.Current(s,r.A,r.B);if(war!=null)Settle(s,war,r.A,r.B,0,null);
    }
}

public static partial class Engine
{
    public static Result ProposePeace(GameState s,int other,int gold,int? returnCityId=null)
    {
        var preview=PeaceCouncil.Preview(s,0,other,gold,returnCityId);
        if(!preview.Valid)return Result.Fail(preview.Error);
        var r=s.Relation(0,other);
        if(r.LastActionTurn==s.Turn||s.Turn-r.LastPeaceTurn<4)return Result.Fail("Rozmowa odbyła się w tej turze lub od ostatniej propozycji nie minęły cztery tury.");
        var existing=WarCouncil.Current(s,0,other);
        if(existing!=null&&s.PeaceOffers.Any(o=>o.WarId==existing.Id&&WarCouncil.IsOpen(o)))return Result.Fail("Najpierw rozstrzygnij trwające negocjacje.");
        if(s.Player.Gold<PeaceCouncil.EmbassyPrice+Math.Max(0,gold))return Result.Fail("Potrzeba 20 złota na poselstwo oraz środków na oferowane reparacje.");
        var war=existing??WarCouncil.Start(s,0,other,WarReason.Conquest,null,false);
        s.Player.Gold-=PeaceCouncil.EmbassyPrice;r.LastActionTurn=s.Turn;
        PeaceCouncil.Create(s,war,0,other,gold,returnCityId);
        return Result.Ok("Posłowie wyruszyli. Wojna trwa do przyjęcia warunków; odpowiedź za dwie tury.");
    }

    public static Result AnswerPeace(GameState s,int offerId,bool accept)
    {
        var offer=s.PeaceOffers.FirstOrDefault(o=>o.Id==offerId&&o.To==0&&o.Status==PeaceStatus.AwaitingPlayer);
        if(s.Ending!=null||offer==null||s.Turn>=offer.DeadlineTurn)return Result.Fail("Te warunki nie oczekują już na odpowiedź.");
        if(accept)
        {
            var preview=PeaceCouncil.Preview(s,offer.From,0,offer.Gold,offer.ReturnCityId);
            if(!preview.Valid)return Result.Fail(preview.Error);
            offer.Status=PeaceStatus.Accepted;offer.ResolvedTurn=s.Turn;offer.Reply="Marchie przyjęły warunki. Podpisano traktat pokojowy.";
            PeaceCouncil.Settle(s,s.Wars.First(w=>w.Id==offer.WarId),offer.From,0,offer.Gold,offer.ReturnCityId);
        }
        else
        {
            offer.Status=PeaceStatus.Rejected;offer.ResolvedTurn=s.Turn;offer.Reply="Marchie odrzuciły warunki. Wojna trwa dalej.";
        }
        s.Relation(offer.From,0).LastActionTurn=s.Turn;s.Log(offer.Reply,"decyzja");return Result.Ok(offer.Reply);
    }
}
