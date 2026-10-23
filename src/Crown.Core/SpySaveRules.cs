namespace Crown.Core;

internal static class SpySaveRules
{
    public static void Validate(GameState s)
    {
        if (s.Spies == null || s.SpyOperations == null || s.Spies.Count > 4000 || s.SpyOperations.Count > 10000 || s.Spies.Any(a => a == null) || s.SpyOperations.Any(o => o == null) ||
            s.NextSpyId < 1 || s.NextSpyOperationId < 1 || s.Spies.Select(a => a.Id).Distinct().Count() != s.Spies.Count || s.SpyOperations.Select(o => o.Id).Distinct().Count() != s.SpyOperations.Count ||
            s.Cities.Any(c => c.CounterIntelligenceUntil < 0 || c.CounterIntelligenceUntil > s.Turn + Espionage.ProtectionTurns))
            throw new InvalidDataException("Uszkodzony rejestr wywiadu lub ochrony miast.");
        foreach (var agent in s.Spies)
        {
            if (agent.Id < 1 || agent.Id >= s.NextSpyId || agent.Owner is < 0 or > 3 || agent.Name == null || agent.Name.Length is < 1 or > 100 || agent.Experience is < 0 or > 30 || !Enum.IsDefined(agent.Status))
                throw new InvalidDataException("Nieprawidłowe dane agenta.");
            int active = s.SpyOperations.Count(o => o.AgentId == agent.Id && o.Outcome == SpyOutcome.Active);
            int captured = s.SpyOperations.Count(o => o.AgentId == agent.Id && o.Outcome == SpyOutcome.Captured && o.CaptureChoice == null);
            var last = s.SpyOperations.Where(o => o.AgentId == agent.Id).OrderByDescending(o => o.Id).FirstOrDefault();
            var expected = last?.Outcome == SpyOutcome.Active ? SpyStatus.Deployed : last?.Outcome == SpyOutcome.Captured && last.CaptureChoice == null ? SpyStatus.Captured : last?.CaptureChoice == 2 ? SpyStatus.Lost : SpyStatus.Available;
            if (active != (agent.Status == SpyStatus.Deployed ? 1 : 0) || captured != (agent.Status == SpyStatus.Captured ? 1 : 0) ||
                agent.Status != expected)
                throw new InvalidDataException("Status agenta nie odpowiada jego operacjom.");
        }
        if (s.Spies.Where(a => a.Status != SpyStatus.Lost).GroupBy(a => a.Owner).Any(g => g.Count() > 3)) throw new InvalidDataException("Zbyt liczna służba wywiadowcza.");
        foreach (var operation in s.SpyOperations)
        {
            var agent = s.Spies.FirstOrDefault(a => a.Id == operation.AgentId); var city = s.Cities.FirstOrDefault(c => c.Id == operation.CityId);
            if (operation.Id < 1 || operation.Id >= s.NextSpyOperationId || agent == null || agent.Owner != operation.Owner || city == null || operation.TargetOwner is < 0 or > 3 || operation.TargetOwner == operation.Owner ||
                !Enum.IsDefined(operation.Mission) || !Enum.IsDefined(operation.Outcome) || operation.StartedTurn < 1 || operation.StartedTurn > s.Turn || operation.DueTurn != operation.StartedTurn + Espionage.Duration(operation.Mission) ||
                operation.SuccessChance is < 20 or > 95 || operation.DetectionChance is < 5 or > 80 || operation.Report == null || operation.Report.Length > 2500 ||
                !double.IsFinite(operation.ResultValue) || operation.ResultValue is < 0 or > 120 ||
                (operation.Mission == SpyMission.StealResearch ? !Content.Technologies.Any(t => t.Id == operation.ResearchId) : operation.ResearchId != null))
                throw new InvalidDataException("Nieprawidłowe warunki operacji wywiadu.");
            if (operation.Outcome == SpyOutcome.Active)
            {
                if (operation.ResolvedTurn != null || operation.DueTurn <= s.Turn || city.Owner != operation.TargetOwner || operation.SuccessRoll != null || operation.DetectionRoll != null || operation.Detected || operation.ResultValue != 0 || operation.Report.Length != 0)
                    throw new InvalidDataException("Nieprawidłowy stan trwającej operacji.");
            }
            else
            {
                if (operation.ResolvedTurn == null || operation.ResolvedTurn < operation.StartedTurn || operation.ResolvedTurn > s.Turn || operation.Report.Length == 0)
                    throw new InvalidDataException("Nieprawidłowe zakończenie operacji.");
                if (operation.Outcome == SpyOutcome.Cancelled)
                {
                    if (operation.SuccessRoll != null || operation.DetectionRoll != null || operation.Detected || operation.ResultValue != 0) throw new InvalidDataException("Odwołana operacja ma fikcyjny wynik.");
                }
                else
                {
                    if (operation.ResolvedTurn < operation.DueTurn || operation.SuccessRoll == null || operation.SuccessRoll is < 0 or > 99 || operation.DetectionRoll == null || operation.DetectionRoll is < 0 or > 99 ||
                        operation.Detected != (operation.DetectionRoll < operation.DetectionChance) || operation.Outcome != (operation.SuccessRoll < operation.SuccessChance ? SpyOutcome.Succeeded : operation.Detected ? SpyOutcome.Captured : SpyOutcome.Failed))
                        throw new InvalidDataException("Niespójny wynik losowania operacji.");
                    double maximum = operation.Mission == SpyMission.StealResearch ? Math.Min(120, Math.Ceiling(Content.Tech(operation.ResearchId!).Cost * .35)) : operation.Mission == SpyMission.Sabotage ? 60 : 0;
                    if (operation.Outcome != SpyOutcome.Succeeded ? operation.ResultValue != 0 : operation.Mission == SpyMission.Sabotage ? operation.ResultValue > maximum : operation.ResultValue != maximum)
                        throw new InvalidDataException("Nieprawidłowy efekt operacji.");
                }
            }
            if (operation.Outcome == SpyOutcome.Captured)
            {
                if (operation.CaptureChoice == null ? operation.CaptureDecisionTurn != null || operation.Owner != 0 && operation.TargetOwner != 0 : operation.CaptureChoice is < 0 or > 5 || operation.CaptureDecisionTurn == null || operation.CaptureDecisionTurn < operation.ResolvedTurn || operation.CaptureDecisionTurn > s.Turn)
                    throw new InvalidDataException("Nieprawidłowe rozstrzygnięcie sprawy schwytanego agenta.");
            }
            else if (operation.CaptureChoice != null || operation.CaptureDecisionTurn != null) throw new InvalidDataException("Decyzja o niewoli bez schwytanego agenta.");
        }
        foreach (var group in s.SpyOperations.GroupBy(o => (o.Owner, o.CityId)))
        {
            var history = group.OrderBy(o => o.StartedTurn).ToArray();
            if (history.Zip(history.Skip(1)).Any(p => p.Second.StartedTurn - p.First.StartedTurn < Espionage.CityInterval)) throw new InvalidDataException("Pominięto odstęp pracy siatki.");
        }
        foreach (var group in s.SpyOperations.GroupBy(o => o.AgentId))
        {
            var history = group.OrderBy(o => o.Id).ToArray();
            if (history.Zip(history.Skip(1)).Any(p => p.First.ResolvedTurn == null || (p.First.Outcome == SpyOutcome.Captured ? p.First.CaptureDecisionTurn == null || p.First.CaptureChoice == 2 || p.First.CaptureDecisionTurn > p.Second.StartedTurn : p.First.ResolvedTurn > p.Second.StartedTurn)))
                throw new InvalidDataException("Agent prowadzi sprzeczne operacje.");
        }
        bool ownCouncil = s.PendingEvent == SpyCouncil.Event.Id, foreignCouncil = s.PendingEvent == CustodyCouncil.Event.Id;
        if (ownCouncil || foreignCouncil ? s.PendingCityId != null || s.PendingTradeCaseId != null || s.PendingAuditId != null || s.PendingLoanId != null ||
            !s.SpyOperations.Any(o => o.Id == s.PendingSpyOperationId && (ownCouncil ? o.Owner == 0 && o.Outcome == SpyOutcome.Captured && o.CaptureChoice == null : CustodyCouncil.Open(o))) : s.PendingSpyOperationId != null)
            throw new InvalidDataException("Uszkodzony kontekst rady wywiadu.");
        CustodySaveRules.Validate(s);
    }
}
