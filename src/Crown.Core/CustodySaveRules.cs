namespace Crown.Core;

internal static class CustodySaveRules
{
    public static void Validate(GameState s)
    {
        foreach (var o in s.SpyOperations)
        {
            bool foreign = o.Outcome == SpyOutcome.Captured && o.Owner != 0 && o.TargetOwner == 0;
            if (o.CustodyChoice is CustodyDecision decision)
            {
                if (!foreign || !Enum.IsDefined(decision) || o.CustodyTurn == null || o.CustodyTurn < o.ResolvedTurn || o.CustodyTurn > s.Turn)
                    throw new InvalidDataException("Nieprawidłowa decyzja o obcym jeńcu.");
                if (decision == CustodyDecision.Intern)
                {
                    if (o.InternedUntil != o.CustodyTurn + CustodyCouncil.InternmentTurns ||
                        (o.CaptureChoice == null ? o.InternedUntil <= s.Turn : o.CaptureChoice != CustodyCouncil.InternmentEnded || o.CaptureDecisionTurn < o.InternedUntil))
                        throw new InvalidDataException("Nieprawidłowy termin internowania.");
                }
                else
                {
                    int result = decision == CustodyDecision.Ransom ? 0 : decision == CustodyDecision.Exchange ? CustodyCouncil.Exchanged : CustodyCouncil.Released;
                    if (o.InternedUntil != null || o.CaptureChoice != result || o.CaptureDecisionTurn != o.CustodyTurn)
                        throw new InvalidDataException("Decyzja o jeńcu nie odpowiada rozstrzygnięciu.");
                }
            }
            else if (o.CustodyTurn != null || o.InternedUntil != null || o.CaptureChoice is CustodyCouncil.Released or CustodyCouncil.InternmentEnded)
                throw new InvalidDataException("Skutek niewoli bez decyzji rady.");

            if (o.CaptureChoice == CustodyCouncil.Exchanged)
            {
                var partner = s.SpyOperations.FirstOrDefault(p => p.Id == o.ExchangeOperationId);
                if (o.Outcome != SpyOutcome.Captured || partner == null || partner.Id == o.Id || partner.Outcome != SpyOutcome.Captured ||
                    partner.CaptureChoice != CustodyCouncil.Exchanged || partner.ExchangeOperationId != o.Id || partner.Owner != o.TargetOwner || partner.TargetOwner != o.Owner ||
                    partner.CaptureDecisionTurn != o.CaptureDecisionTurn ||
                    (foreign ? o.CustodyChoice != CustodyDecision.Exchange || partner.CustodyChoice != null : o.Owner != 0 || partner.CustodyChoice != CustodyDecision.Exchange))
                    throw new InvalidDataException("Niespójna para wymienionych agentów.");
            }
            else if (o.ExchangeOperationId != null) throw new InvalidDataException("Powiązanie wymiany bez wymiany agentów.");
        }
    }
}
