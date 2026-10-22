namespace Crown.Core;

internal static class WarSaveRules
{
    public static void Validate(GameState s)
    {
        bool Owner(int id)=>id>=0&&id<4;
        var cities=s.Cities.Select(c=>c.Id).ToHashSet();
        bool CityList(List<int>? list)=>list!=null&&list.Count<=cities.Count&&list.Distinct().Count()==list.Count&&list.All(cities.Contains);
        if(s.Wars==null||s.Wars.Count>2000||s.NextWarId<1||s.Wars.Select(w=>w.Id).Distinct().Count()!=s.Wars.Count||s.Wars.Any(w=>
            w.Id<1||w.Id>=s.NextWarId||!Owner(w.A)||!Owner(w.B)||w.A>=w.B||w.Aggressor!=w.A&&w.Aggressor!=w.B||!Enum.IsDefined(w.Reason)||
            w.StartedTurn<1||w.StartedTurn>s.Turn||w.EndedTurn is int ended&&(ended<w.StartedTurn||ended>s.Turn)||
            !double.IsFinite(w.LossesA)||!double.IsFinite(w.LossesB)||w.LossesA is <0 or >1_000_000_000||w.LossesB is <0 or >1_000_000_000||
            !CityList(w.StartingCitiesA)||!CityList(w.StartingCitiesB)||w.StartingCitiesA.Intersect(w.StartingCitiesB).Any()||
            w.TargetCityId is int target&&!cities.Contains(target)||w.GoalDeclared&&(w.Reason==WarReason.Redress?w.TargetCityId!=null:w.TargetCityId==null)||
            w.GoalDeclared&&w.TargetCityId is int goal&&!(w.Aggressor==w.A?w.StartingCitiesB:w.StartingCitiesA).Contains(goal)||
            w.GoalDeclared&&w.Reason==WarReason.Liberation&&!s.Cities.Any(c=>c.Id==w.TargetCityId&&c.OriginalOwner==w.Aggressor)||
            w.Outcome==null||w.Outcome.Length>4000||(w.EndedTurn==null?w.Outcome.Length!=0||!s.Relation(w.A,w.B).War:w.Outcome.Length==0))||
            s.Wars.Where(w=>w.EndedTurn==null).GroupBy(w=>(w.A,w.B)).Any(g=>g.Count()>1))throw new InvalidDataException("Uszkodzona historia wojen.");
        if(s.Relations.Any(r=>r.LastPeaceTurn< -10||r.LastPeaceTurn>s.Turn))throw new InvalidDataException("Uszkodzony termin negocjacji.");
        if(s.PeaceOffers==null||s.PeaceOffers.Count>4000||s.NextPeaceId<1||s.PeaceOffers.Select(o=>o.Id).Distinct().Count()!=s.PeaceOffers.Count)
            throw new InvalidDataException("Uszkodzona lista traktatów.");
        foreach(var o in s.PeaceOffers)
        {
            var war=s.Wars.FirstOrDefault(w=>w.Id==o.WarId);
            bool invalid=o.Id<1||o.Id>=s.NextPeaceId||!Owner(o.From)||!Owner(o.To)||o.From==o.To||o.From!=0&&o.To!=0||
                war==null||war.A!=Math.Min(o.From,o.To)||war.B!=Math.Max(o.From,o.To)||!PeaceCouncil.Payments.Contains(o.Gold)||!Enum.IsDefined(o.Status)||
                o.ReturnCityId is int id&&(!cities.Contains(id)||!s.Cities.Any(c=>c.Id==id&&(c.OriginalOwner==o.From||c.OriginalOwner==o.To)))||o.SentTurn<1||o.SentTurn>s.Turn||o.SentTurn<war.StartedTurn||
                o.DueTurn!=o.SentTurn+2||o.DeadlineTurn!=o.DueTurn+4||o.Reply==null||o.Reply.Length>4000;
            if(invalid||war==null||o.Reply==null)throw new InvalidDataException("Uszkodzone warunki traktatu.");
            if(o.InReplyTo is int parent&&!s.PeaceOffers.Any(p=>p.Id==parent&&p.Id<o.Id&&p.WarId==o.WarId&&p.From==o.To&&p.To==o.From&&p.Status==PeaceStatus.Rejected))
                throw new InvalidDataException("Uszkodzona kontroferta.");
            if(WarCouncil.IsOpen(o))
            {
                if(o.ResolvedTurn!=null||war!.EndedTurn!=null||!s.Relation(o.From,o.To).War||
                    (o.Status==PeaceStatus.Travelling?o.DueTurn<=s.Turn||o.Reply.Length!=0:o.To!=0||o.DueTurn>s.Turn||o.DeadlineTurn<=s.Turn||o.Reply.Length==0))
                    throw new InvalidDataException("Uszkodzony stan trwających rozmów.");
            }
            else if(o.ResolvedTurn is not int resolved||resolved<o.SentTurn||resolved>s.Turn||o.Reply.Length==0||
                o.Status!=PeaceStatus.Cancelled&&resolved<o.DueTurn||o.Status==PeaceStatus.Expired&&resolved<o.DeadlineTurn||
                o.Status==PeaceStatus.Accepted&&war!.EndedTurn!=resolved)
                throw new InvalidDataException("Uszkodzona odpowiedź na traktat.");
        }
        if(s.PeaceOffers.Where(WarCouncil.IsOpen).GroupBy(o=>o.WarId).Any(g=>g.Count()>1)||s.PeaceOffers.Where(o=>o.Status==PeaceStatus.Accepted).GroupBy(o=>o.WarId).Any(g=>g.Count()>1))
            throw new InvalidDataException("Sprzeczne traktaty tej samej wojny.");
    }
}
