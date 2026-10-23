using System.Diagnostics.CodeAnalysis;

namespace Crown.Core;

internal static class EnergySaveRules
{
    [DoesNotReturn]
    private static void Invalid() => throw new InvalidDataException("Nieprawidłowa historia kryzysów lub zobowiązań energetycznych.");
    private static void Reading(GameState s, EnergyReading r)
    {
        if (r == null || r.PhaseTurn < 1 || r.PhaseTurn >= s.Turn || r.PhaseTurn > s.LastEnergyPhase || r.Demand < 0 ||
            !double.IsFinite(r.Allocated) || r.Allocated < 0 || r.Allocated > r.Demand + 1e-8 || r.Services == null ||
            r.Services.Count > EpochCatalog.Buildings.Length || r.Services.Distinct().Count() != r.Services.Count) Invalid();
        int demand = 0;
        foreach (string id in r.Services)
        {
            var b = EpochCatalog.Buildings.FirstOrDefault(b => b.Id == id && b.Demand > 0); if (b == null) Invalid();
            demand += b!.Demand;
        }
        if (demand != r.Demand) Invalid();
    }
    public static void Validate(GameState s)
    {
        if (s.EnergyCases == null || s.EnergyCases.Count > 4000 || s.EnergyCases.Any(c => c == null) ||
            s.NextEnergyCaseId is < 1 or > 4001 || s.EnergyCases.Select(c => c.Id).Distinct().Count() != s.EnergyCases.Count ||
            s.LastEnergyPhase < 0 || s.LastEnergyPhase >= s.Turn) Invalid();
        foreach (var city in s.Cities)
        {
            if (city.EnergyHistory == null || city.EnergyHistory.Count > EnergyCouncil.HistoryLimit) Invalid();
            foreach (var r in city.EnergyHistory) Reading(s, r);
            if (city.EnergyHistory.Zip(city.EnergyHistory.Skip(1)).Any(pair => pair.Second.PhaseTurn != pair.First.PhaseTurn + 1)) Invalid();
            if (city.EnergyHistory.Count > 0 && city.EnergyHistory[^1].PhaseTurn != s.LastEnergyPhase) Invalid();
        }
        if (!s.EpochCampaign && (s.LastEnergyPhase != 0 || s.NextEnergyCaseId != 1 || s.EnergyCases.Count != 0 || s.Cities.Any(c => c.EnergyHistory.Count != 0))) Invalid();
        foreach (var c in s.EnergyCases)
        {
            var city = s.Cities.FirstOrDefault(city => city.Id == c.CityId);
            if (city == null || c.Id < 1 || c.Id >= s.NextEnergyCaseId || c.OpenedTurn < 4 || c.OpenedTurn > s.Turn ||
                c.ReliefCost is < 65 or > 5060 || c.ReliefCost % 5 != 0 || !Enum.IsDefined(c.Status) ||
                c.Shortages == null || c.Shortages.Count != EnergyCouncil.ShortagePhases || c.Deliveries == null || c.Deliveries.Count > EnergyCouncil.PromiseTurns) Invalid();
            foreach (var r in c.Shortages) Reading(s, r);
            if (c.Shortages.Any(r => !r.Shortage) || c.Shortages.Select((r, i) => r.PhaseTurn == c.OpenedTurn - EnergyCouncil.ShortagePhases + i).Any(valid => !valid)) Invalid();
            if (c.Status is EnergyCaseStatus.Pending or EnergyCaseStatus.Pledged && city!.Owner != 0) Invalid();
            if (c.Status is EnergyCaseStatus.Pending or EnergyCaseStatus.Voided)
            {
                if (c.Decision != null || c.DecidedTurn != null || c.DeadlineTurn != null || c.Deliveries.Count != 0 || c.Failure != null ||
                    (c.Status == EnergyCaseStatus.Pending ? c.ResolvedTurn != null : c.ResolvedTurn == null || c.ResolvedTurn < c.OpenedTurn || c.ResolvedTurn > s.Turn)) Invalid();
                continue;
            }
            if (c.Decision is null or < 0 or > 2 || c.DecidedTurn == null || c.DecidedTurn < c.OpenedTurn || c.DecidedTurn > s.Turn) Invalid();
            if (c.Decision != 1)
            {
                if (c.Status != (c.Decision == 0 ? EnergyCaseStatus.Relief : EnergyCaseStatus.Declined) || c.ResolvedTurn != c.DecidedTurn ||
                    c.DeadlineTurn != null || c.Failure != null || c.Deliveries.Count != 0) Invalid();
                continue;
            }
            if (c.Status is not (EnergyCaseStatus.Pledged or EnergyCaseStatus.Fulfilled or EnergyCaseStatus.Broken) || c.DeadlineTurn != c.DecidedTurn + EnergyCouncil.PromiseTurns) Invalid();
            int streak = 0;
            for (int i = 0; i < c.Deliveries.Count; i++)
            {
                var r = c.Deliveries[i]; Reading(s, r);
                if (r.PhaseTurn != c.DecidedTurn + i || r.PhaseTurn >= c.DeadlineTurn) Invalid();
                streak = EnergyCouncil.Satisfies(c, r) ? streak + 1 : 0;
                if (streak >= EnergyCouncil.RecoveryPhases && i != c.Deliveries.Count - 1) Invalid();
            }
            if (c.Status == EnergyCaseStatus.Pledged)
            {
                if (c.ResolvedTurn != null || c.Failure != null || streak >= EnergyCouncil.RecoveryPhases || s.LastEnergyPhase >= c.DeadlineTurn - 1 ||
                    c.Deliveries.Count != Math.Max(0, s.LastEnergyPhase - c.DecidedTurn.Value + 1)) Invalid();
            }
            else
            {
                if (c.ResolvedTurn == null || c.ResolvedTurn < c.DecidedTurn || c.ResolvedTurn > s.Turn || c.ResolvedTurn > c.DeadlineTurn) Invalid();
                if (c.Status == EnergyCaseStatus.Fulfilled)
                {
                    if (streak != EnergyCouncil.RecoveryPhases || c.Failure != null || c.ResolvedTurn != c.Deliveries[^1].PhaseTurn + 1) Invalid();
                }
                else if (c.Failure == EnergyFailure.Deadline)
                {
                    if (c.ResolvedTurn != c.DeadlineTurn || c.Deliveries.Count != EnergyCouncil.PromiseTurns || streak >= EnergyCouncil.RecoveryPhases) Invalid();
                }
                else if (c.Failure != EnergyFailure.CityLost || c.Deliveries.Any(r => r.PhaseTurn >= c.ResolvedTurn)) Invalid();
            }
        }
        foreach (var group in s.EnergyCases.GroupBy(c => c.CityId))
        {
            if (group.Count(c => c.Status is EnergyCaseStatus.Pending or EnergyCaseStatus.Pledged) > 1) Invalid();
            var ordered = group.OrderBy(c => c.OpenedTurn).ThenBy(c => c.Id).ToArray();
            if (ordered.Zip(ordered.Skip(1)).Any(pair => pair.First.ResolvedTurn == null || pair.Second.OpenedTurn - pair.First.ResolvedTurn < EnergyCouncil.Cooldown)) Invalid();
        }
        if (s.PendingEvent == EnergyCouncil.Event.Id ? s.PendingCityId != null || !s.EnergyCases.Any(c => c.Id == s.PendingEnergyCaseId && c.Status == EnergyCaseStatus.Pending && EnergyCouncil.City(s, c).Owner == 0) : s.PendingEnergyCaseId != null) Invalid();
    }
}
