namespace Crown.Core;

public static class WarCouncil
{
    public static WarCampaign? Current(GameState s, int a, int b) => s.Wars.LastOrDefault(w => w.A==Math.Min(a,b) && w.B==Math.Max(a,b) && w.EndedTurn==null);

    internal static WarCampaign Start(GameState s, int aggressor, int other, WarReason reason, int? target, bool declared=true)
    {
        var war=new WarCampaign { Id=s.NextWarId++, A=Math.Min(aggressor,other), B=Math.Max(aggressor,other), Aggressor=aggressor,
            Reason=reason, TargetCityId=target, GoalDeclared=declared, StartedTurn=s.Turn };
        war.StartingCitiesA=s.Cities.Where(c=>c.Owner==war.A).Select(c=>c.Id).ToList();
        war.StartingCitiesB=s.Cities.Where(c=>c.Owner==war.B).Select(c=>c.Id).ToList();
        s.Wars.Add(war);return war;
    }

    public static void EnsureActive(GameState s)
    {
        foreach(var r in s.Relations.Where(r=>r.War))
            if(Current(s,r.A,r.B)==null)Start(s,r.A,r.B,WarReason.Conquest,null,false);
    }

    public static City? DefaultTarget(GameState s,int aggressor,int other,WarReason reason) => reason==WarReason.Redress ? null :
        s.Cities.Where(c=>c.Owner==other&&(reason!=WarReason.Liberation||c.OriginalOwner==aggressor))
            .OrderBy(c=>s.Cities.Where(home=>home.Owner==aggressor).Select(home=>World.Distance(s.Tiles[c.Tile],s.Tiles[home.Tile])).DefaultIfEmpty(999).Min()).ThenBy(c=>c.Id).FirstOrDefault();

    public static string Goal(GameState s,WarCampaign war) => !war.GoalDeclared ? "Dawna wojna · cel nie został zapisany" : war.Reason==WarReason.Redress ?
        "Otrzymać co najmniej 80 złota reparacji w pokoju" : (war.Reason==WarReason.Liberation ? "Wyzwolić i utrzymać: " : "Zdobyć i utrzymać: ") + s.Cities.First(c=>c.Id==war.TargetCityId).Name;

    public static WarScoreReport Score(GameState s,WarCampaign war,int owner)
    {
        int other=owner==war.A?war.B:war.A;
        var ours=owner==war.A?war.StartingCitiesA:war.StartingCitiesB;
        var theirs=owner==war.A?war.StartingCitiesB:war.StartingCitiesA;
        int cities=20*(s.Cities.Count(c=>theirs.Contains(c.Id)&&c.Owner==owner)-s.Cities.Count(c=>ours.Contains(c.Id)&&c.Owner==other));
        double ownLoss=owner==war.A?war.LossesA:war.LossesB,otherLoss=owner==war.A?war.LossesB:war.LossesA;
        int battles=(int)Math.Clamp((otherLoss-ownLoss)/12,-25,25);
        double ownArmy=Engine.Military(s,owner),otherArmy=Engine.Military(s,other);
        int army=(int)Math.Clamp((ownArmy-otherArmy)/Math.Max(20,Math.Max(ownArmy,otherArmy))*20,-20,20);
        int exhaustion=(int)Math.Clamp((s.Factions[other].WarWeariness-s.Factions[owner].WarWeariness)/4,-15,15);
        return new(Math.Clamp(cities+battles+army+exhaustion,-100,100),
            [$"Zmiany kontroli miast {cities:+0;-0;0}",$"Bilans strat w walkach {battles:+0;-0;0}",$"Obecna siła armii {army:+0;-0;0}",$"Wyczerpanie obu państw {exhaustion:+0;-0;0}"]);
    }

    internal static void RecordBattle(GameState s,int attacker,int defender,double attackerLoss,double defenderLoss)
    {
        var war=Current(s,attacker,defender)??Start(s,attacker,defender,WarReason.Conquest,null,false);
        if(attacker==war.A){war.LossesA+=attackerLoss;war.LossesB+=defenderLoss;}
        else{war.LossesB+=attackerLoss;war.LossesA+=defenderLoss;}
    }

    internal static void Finish(GameState s,WarCampaign war,string text,int goldToAggressor=0)
    {
        if(war.EndedTurn!=null)return;
        war.EndedTurn=s.Turn;
        bool fulfilled=war.GoalDeclared&&(war.Reason==WarReason.Redress ? goldToAggressor>=80 : s.Cities.Any(c=>c.Id==war.TargetCityId&&c.Owner==war.Aggressor));
        if(war.GoalDeclared)
        {
            var f=s.Factions[war.Aggressor];f.Prestige=Math.Max(0,f.Prestige+(fulfilled?12:-5));f.Commons+=fulfilled?3:-3;Engine.Clamp(f);
        }
        war.Outcome=text+(war.GoalDeclared ? fulfilled?" Cel wojenny osiągnięty: prestiż +12, lud +3.":" Cel wojenny nieosiągnięty: prestiż -5, lud -3.":" Brak rozliczenia dawnego celu.");
        s.Log(war.Outcome,"pokój");
        foreach(var offer in s.PeaceOffers.Where(o=>o.WarId==war.Id&&IsOpen(o)))
        {offer.Status=PeaceStatus.Cancelled;offer.ResolvedTurn=s.Turn;offer.Reply="Negocjacje zamknięto wraz z zakończeniem wojny.";}
    }

    public static bool IsOpen(PeaceOffer offer)=>offer.Status is PeaceStatus.Travelling or PeaceStatus.AwaitingPlayer;

    internal static void FinishEliminated(GameState s)
    {
        foreach(var war in s.Wars.Where(w=>w.EndedTurn==null).ToArray())
            if(!s.Cities.Any(c=>c.Owner==war.A)||!s.Cities.Any(c=>c.Owner==war.B))
            {
                var r=s.Relation(war.A,war.B);r.War=false;r.Trade=false;
                Finish(s,war,$"Wojna {s.Factions[war.A].Name} – {s.Factions[war.B].Name} kończy się upadkiem państwa.");
            }
    }
}
