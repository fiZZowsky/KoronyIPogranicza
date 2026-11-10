namespace Crown.Core;

internal static class TreatySaveRules
{
    internal static void Validate(GameState s)
    {
        bool Amount(double x)=>double.IsFinite(x)&&x>=0&&x<=10000000;
        if(s.Treaties==null||s.Treaties.Count>10000||s.Treaties.Any(t=>t==null)||s.NextTreatyId<1||s.Treaties.Any(t=>t.Id<1||t.Id>=s.NextTreatyId)||s.Treaties.Select(t=>t.Id).Distinct().Count()!=s.Treaties.Count||
            (s.Version<26||!s.EpochCampaign)&&(s.Treaties.Count>0||s.NextTreatyId!=1))throw new InvalidDataException("Uszkodzony rejestr traktatów.");
        foreach(var t in s.Treaties)
        {
            if(t.From is <0 or >3||t.To is <0 or >3||t.From==t.To||t.From!=0&&t.To!=0||!Enum.IsDefined(t.Kind)||!Enum.IsDefined(t.Status)||t.Duration is not (12 or 24)||t.SentTurn<1||t.SentTurn>s.Turn||t.DueTurn!=t.SentTurn+2||t.DeadlineTurn!=t.DueTurn+4||t.ReplyTurn is int reply&&(reply<t.DueTurn||reply>s.Turn)||t.SignedTurn is int signed&&(signed<t.DueTurn||signed>=t.DeadlineTurn||signed>s.Turn)||t.ExpiresTurn!=(t.SignedTurn is int start?start+t.Duration:null)||t.ClosedTurn is int closed&&(closed<t.SentTurn||closed>s.Turn)||t.Report==null||t.Report.Length>1000)
                throw new InvalidDataException("Nieprawidłowe strony, warunki lub daty traktatu.");
            bool active=t.Status==TreatyStatus.Active,open=Treaties.Open(t),hasSignature=t.SignedTurn!=null;int bond=Treaties.Bond(t.Kind);
            if(hasSignature&&(t.ReplyTurn==null||t.ReplyTurn>t.SignedTurn)||active&&(s.Relation(t.From,t.To).War||!s.Cities.Any(c=>c.Owner==t.From)||!s.Cities.Any(c=>c.Owner==t.To)))throw new InvalidDataException("Traktat nie ma ważnej ratyfikacji lub pokoju.");
            if(active?(t.ClosedTurn!=null||!hasSignature||t.ReplyTurn==null||t.ReplyTurn>t.SignedTurn||t.ExpiresTurn<=s.Turn||t.EscrowFrom!=bond||t.EscrowTo!=bond||t.PayoutFrom!=0||t.PayoutTo!=0||t.BreachedBy!=null):
                t.EscrowFrom!=0||t.EscrowTo!=0||open&&(hasSignature||t.ClosedTurn!=null||t.PayoutFrom!=0||t.PayoutTo!=0)||!open&&t.ClosedTurn==null)
                throw new InvalidDataException("Niezgodny depozyt lub stan traktatu.");
            if(t.Status==TreatyStatus.Travelling&&t.ReplyTurn!=null||t.Status==TreatyStatus.AwaitingPlayer&&(t.ReplyTurn==null||s.Turn<t.DueTurn||s.Turn>=t.DeadlineTurn)||open&&s.Turn>=t.DeadlineTurn||t.Status is TreatyStatus.Completed or TreatyStatus.Broken&&!hasSignature||hasSignature&&t.Status is not (TreatyStatus.Active or TreatyStatus.Completed or TreatyStatus.Broken or TreatyStatus.Cancelled)||hasSignature&&t.ClosedTurn<t.SignedTurn)
                throw new InvalidDataException("Nieprawidłowy przebieg negocjacji.");
            if(!active)
            {
                if(!hasSignature?(t.PayoutFrom!=0||t.PayoutTo!=0||t.BreachedBy!=null):
                    t.Status==TreatyStatus.Broken?(t.BreachedBy!=t.From&&t.BreachedBy!=t.To||t.PayoutFrom!=(t.BreachedBy==t.To?2*bond:0)||t.PayoutTo!=(t.BreachedBy==t.From?2*bond:0)):
                    t.BreachedBy!=null||t.PayoutFrom!=bond||t.PayoutTo!=bond)
                    throw new InvalidDataException("Niezgodna wypłata zabezpieczenia traktatu.");
                if(t.Status==TreatyStatus.Completed&&t.ClosedTurn<t.ExpiresTurn)throw new InvalidDataException("Przedwczesne ukończenie traktatu.");
            }
            if(t.LastPhaseTurn<0||t.LastPhaseTurn>=s.Turn||t.SignedTurn==null&&t.LastPhaseTurn!=0||t.LastPhaseTurn>0&&(t.LastPhaseTurn<t.SignedTurn||t.LastPhaseTurn>=t.ExpiresTurn)||t.LastFee is not (0 or Treaties.ResearchFee)||!Amount(t.LastScience)||!Amount(t.LastIncomeFrom)||!Amount(t.LastIncomeTo)||!Amount(t.TotalScience)||t.FundedPhases<0||t.FundedPhases>t.Duration||t.TotalScience>t.FundedPhases*22+.00001||t.TotalScience+.00001<t.LastScience||t.MissedFrom is <0 or >3||t.MissedTo is <0 or >3)
                throw new InvalidDataException("Uszkodzone rozliczenie współpracy badawczej.");
            if(t.LastPhaseTurn==0?(t.LastFee!=0||t.LastScience!=0||t.LastIncomeFrom!=0||t.LastIncomeTo!=0||t.LastBonusCap!=0||t.FundedPhases!=0||t.TotalScience!=0||t.MissedFrom!=0||t.MissedTo!=0):
                t.Kind!=TreatyKind.Research||t.LastBonusCap is <5 or >22||Math.Abs(t.LastScience-(t.LastFee==0?0:Math.Min(t.LastBonusCap,Math.Min(t.LastIncomeFrom,t.LastIncomeTo)*.15)))>.00001||t.LastFee>0&&(t.FundedPhases<1||t.MissedFrom!=0||t.MissedTo!=0)||t.FundedPhases>t.LastPhaseTurn-t.SignedTurn+1)
                throw new InvalidDataException("Niezgodny dowód finansowania i nauki traktatu.");
        }
        foreach(var f in s.Factions)
            if(s.Treaties.Count(t=>(Treaties.Active(s,t)||Treaties.Open(t))&&(t.From==f.Id||t.To==f.Id))>2)throw new InvalidDataException("Przekroczony limit zobowiązań traktatowych.");
        if(s.Treaties.Where(Treaties.Open).GroupBy(t=>(A:Math.Min(t.From,t.To),B:Math.Max(t.From,t.To))).Any(g=>g.Count()>1))throw new InvalidDataException("Powielone negocjacje traktatu.");
        foreach(var group in s.Treaties.Where(t=>Treaties.Active(s,t)).GroupBy(t=>(A:Math.Min(t.From,t.To),B:Math.Max(t.From,t.To),t.Kind)))
            if(group.Count()>1)throw new InvalidDataException("Powielony aktywny traktat.");
    }
}
