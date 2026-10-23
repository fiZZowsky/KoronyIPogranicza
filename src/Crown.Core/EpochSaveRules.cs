namespace Crown.Core;

internal static class EpochSaveRules
{
    public static void Validate(GameState s)
    {
        if (!double.IsFinite(s.ClimatePressure) || s.ClimatePressure is < 0 or > 100 ||
            !s.EpochCampaign && (s.ClimatePressure != 0 || s.ContinueAfterVictory)) throw new InvalidDataException("Nieprawidłowe parametry historii i klimatu.");
        int pendingEra=EpochCouncils.Index(s.PendingEvent);
        if(pendingEra>=0&&(!s.EpochCampaign||pendingEra>s.Player.Era||s.Player.EpochChoices?.ContainsKey(pendingEra)==true))throw new InvalidDataException("Nieprawidłowy kontekst rady epoki.");
        foreach (var f in s.Factions)
        {
            if (f.EpochChoices == null || f.SpaceMilestones == null || f.EpochChoices.Any(x => x.Key < 0 || x.Key >= EpochCatalog.All.Length || x.Key > f.Era || x.Value is < 0 or > 2) ||
                !s.EpochCampaign && (f.EpochChoices.Count != 0 || f.SpaceMilestones.Count != 0)) throw new InvalidDataException("Nieprawidłowa historia decyzji epok.");
            foreach (string id in f.SpaceMilestones)
            {
                var p = Epochs.Programs.FirstOrDefault(p => p.Id == id);
                if (p == null || !f.Technologies.Contains(p.Technology) || p.Previous != null && !f.SpaceMilestones.Contains(p.Previous)) throw new InvalidDataException("Nieprawidłowa kolejność programów kosmicznych.");
            }
        }
        if (s.Units.Any(u => u.ModelId != null && (!s.EpochCampaign || !UnitModels.All.Any(m => m.Id == u.ModelId && m.Role == u.Kind))) ||
            s.Cities.Any(c => c.Buildings.Any(id => Epochs.Programs.Any(p => p.Id == id)))) throw new InvalidDataException("Nieprawidłowy model wojsk lub miejsce programu kosmicznego.");
    }
}
