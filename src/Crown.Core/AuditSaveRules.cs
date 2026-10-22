namespace Crown.Core;

internal static class AuditSaveRules
{
    public static void Validate(GameState s)
    {
        if(s.Cities.Any(c=>c.Corruption is <0 or >100||!double.IsFinite(c.HiddenGold)||c.HiddenGold is <0 or >1_000_000_000||
            !double.IsFinite(c.LastRevenueLoss)||c.LastRevenueLoss is <0 or >1_000_000_000||c.LastAdministrationTurn<0||c.LastAdministrationTurn>s.Turn||
            c.LastAuditTurn< -12||c.LastAuditTurn>s.Turn||c.AuditProtectionUntil<0||c.AuditProtectionUntil>s.Turn+Administration.ProtectionTurns))
            throw new InvalidDataException("Uszkodzone rachunki administracji prowincji.");
        if(s.Audits==null||s.Audits.Count>10000||s.Audits.Any(a=>a==null)||s.NextAuditId<1||s.Audits.Select(a=>a.Id).Distinct().Count()!=s.Audits.Count)
            throw new InvalidDataException("Uszkodzony rejestr kontroli.");
        foreach(var audit in s.Audits)
        {
            var city=s.Cities.FirstOrDefault(c=>c.Id==audit.CityId);
            if(audit.Id<1||audit.Id>=s.NextAuditId||city==null||audit.StartedTurn<1||audit.StartedTurn>s.Turn||
                audit.DueTurn!=audit.StartedTurn+Administration.AuditTurns||!Enum.IsDefined(audit.Status)||
                !double.IsFinite(audit.AssessedGold)||audit.AssessedGold is <0 or >1_000_000_000||audit.RecoveredGold<0||
                audit.CompletedTurn is int completed&&(completed<audit.DueTurn||completed>s.Turn)||
                audit.CompletedTurn==null&&audit.AssessedGold!=0)
                throw new InvalidDataException("Nieprawidłowe ustalenia kontroli.");
            if(Administration.Open(audit)&&(city.Owner!=0||city.LastAuditTurn<audit.StartedTurn))
                throw new InvalidDataException("Aktywna kontrola dotyczy obcej lub nieprzebadanej prowincji.");
            if(audit.Status==AuditStatus.Investigating)
            {
                if(audit.DueTurn<=s.Turn||audit.CompletedTurn!=null||audit.ResolvedTurn!=null||audit.RecoveredGold!=0)
                    throw new InvalidDataException("Nieprawidłowy termin trwającej kontroli.");
            }
            else if(audit.Status==AuditStatus.AwaitingCouncil)
            {
                if(audit.CompletedTurn==null||audit.ResolvedTurn!=null||audit.RecoveredGold!=0)
                    throw new InvalidDataException("Nieprawidłowy raport oczekujący na radę.");
            }
            else
            {
                if(audit.ResolvedTurn==null||audit.ResolvedTurn<audit.StartedTurn||audit.ResolvedTurn>s.Turn||
                    audit.CompletedTurn is int done&&audit.ResolvedTurn<done||
                    (audit.Status==AuditStatus.Cancelled?audit.RecoveredGold!=0:
                        audit.CompletedTurn==null||audit.RecoveredGold!=AuditCouncil.Recovery(audit,audit.Status==AuditStatus.Prosecuted?0:audit.Status==AuditStatus.Reformed?1:2)))
                    throw new InvalidDataException("Nieprawidłowe rozliczenie kontroli.");
            }
        }
        if(s.Audits.Where(Administration.Open).GroupBy(a=>a.CityId).Any(g=>g.Count()>1))throw new InvalidDataException("Powielona kontrola tej samej prowincji.");
        foreach(var group in s.Audits.GroupBy(a=>a.CityId))
        {
            var history=group.OrderBy(a=>a.StartedTurn).ToArray();
            if(history.Zip(history.Skip(1)).Any(p=>p.Second.StartedTurn-p.First.StartedTurn<Administration.AuditInterval))
                throw new InvalidDataException("Kontrole omijają odstęp między dochodzeniami.");
        }
        if(s.PendingEvent==AuditCouncil.Event.Id?
            s.PendingCityId!=null||s.PendingTradeCaseId!=null||!s.Audits.Any(a=>a.Id==s.PendingAuditId&&a.Status==AuditStatus.AwaitingCouncil):s.PendingAuditId!=null)
            throw new InvalidDataException("Uszkodzony kontekst rady rachunkowej.");
    }
}
