namespace Crown.Core;

internal static class TradeCaseSaveRules
{
    public static void Validate(GameState s)
    {
        if(s.TradeCases==null||s.TradeCases.Count>8000||s.TradeCases.Any(c=>c==null)||s.TradeCases.Select(c=>c.CaravanId).Distinct().Count()!=s.TradeCases.Count)
            throw new InvalidDataException("Uszkodzony rejestr spraw kupieckich.");
        foreach(var c in s.TradeCases)
        {
            var source=s.Caravans.FirstOrDefault(v=>v.Id==c.CaravanId);
            if(source==null||source.Status!=CaravanStatus.Lost||c.OpenedTurn!=source.ResolvedTurn||c.OpenedTurn>s.LastMarketTurn||
                !Enum.IsDefined(c.Status)||c.CreditedCaravans==null||c.CreditedCaravans.Count>8000||c.CreditedCaravans.Distinct().Count()!=c.CreditedCaravans.Count||
                s.TradeContracts.First(t=>t.Id==source.ContractId).Buyer!=0)
                throw new InvalidDataException("Nieprawidłowa strata będąca podstawą delegacji.");
            if((c.Status is TradeCaseStatus.Pending or TradeCaseStatus.Promised)&&TradeCouncil.City(s,c).Owner!=0)
                throw new InvalidDataException("Aktywna delegacja dotyczy utraconego miasta.");
            bool undecided=c.Status is TradeCaseStatus.Pending or TradeCaseStatus.Voided;
            if(undecided)
            {
                if(c.Decision!=null||c.DecidedTurn!=null||c.DeadlineTurn!=null||c.AfterMarketTurn!=null||c.CreditedCaravans.Count!=0||
                    (c.Status==TradeCaseStatus.Pending?c.ResolvedTurn!=null:c.ResolvedTurn==null||c.ResolvedTurn<c.OpenedTurn||c.ResolvedTurn>s.Turn))
                    throw new InvalidDataException("Nieprawidłowy stan oczekującej delegacji.");
                continue;
            }
            if(c.Decision is null or <0 or >2||c.DecidedTurn==null||c.DecidedTurn<c.OpenedTurn||c.DecidedTurn>s.Turn||
                c.Decision==0&&c.Status!=TradeCaseStatus.Compensated||c.Decision==2&&c.Status!=TradeCaseStatus.Declined||
                c.Decision==1&&c.Status is not (TradeCaseStatus.Promised or TradeCaseStatus.Fulfilled or TradeCaseStatus.Broken))
                throw new InvalidDataException("Nieprawidłowa decyzja rady kupieckiej.");
            if(c.Decision!=1)
            {
                if(c.ResolvedTurn!=c.DecidedTurn||c.DeadlineTurn!=null||c.AfterMarketTurn!=null||c.CreditedCaravans.Count!=0)
                    throw new InvalidDataException("Nieprawidłowe zamknięcie odszkodowania lub odmowy.");
                continue;
            }
            if(c.DeadlineTurn!=c.DecidedTurn+TradeCouncil.PromiseTurns||c.AfterMarketTurn==null||c.AfterMarketTurn<c.OpenedTurn||c.AfterMarketTurn>c.DecidedTurn||c.AfterMarketTurn>s.LastMarketTurn||
                (c.Status==TradeCaseStatus.Promised?c.ResolvedTurn!=null||s.LastMarketTurn>=c.DeadlineTurn:
                    c.ResolvedTurn==null||c.ResolvedTurn<c.DecidedTurn||c.ResolvedTurn>s.Turn))
                throw new InvalidDataException("Nieprawidłowy termin obietnicy handlowej.");
            var contract=TradeCouncil.Contract(s,c);double amount=0;
            foreach(int id in c.CreditedCaravans)
            {
                var cargo=s.Caravans.FirstOrDefault(v=>v.Id==id);
                if(cargo==null||cargo.Status!=CaravanStatus.Delivered||cargo.ResolvedTurn<c.DecidedTurn||cargo.ResolvedTurn<=c.AfterMarketTurn||
                    cargo.ResolvedTurn>c.DeadlineTurn||cargo.ResolvedTurn>c.ResolvedTurn)
                    throw new InvalidDataException("Obietnicę rozliczono nieprzybyłym ładunkiem.");
                var t=s.TradeContracts.First(t=>t.Id==cargo.ContractId);
                if(t.Buyer!=0||t.BuyerCity!=contract.BuyerCity||t.Good!=contract.Good)throw new InvalidDataException("Obietnicę rozliczono obcym towarem.");
                amount+=cargo.Quantity;
            }
            if((c.Status==TradeCaseStatus.Fulfilled)!=(amount+.00001>=source.Quantity)||c.Status==TradeCaseStatus.Fulfilled&&c.ResolvedTurn>c.DeadlineTurn)
                throw new InvalidDataException("Wynik obietnicy nie zgadza się z dostawami.");
        }
        if(s.TradeCases.Where(c=>c.Status==TradeCaseStatus.Promised).GroupBy(c=>(TradeCouncil.City(s,c).Id,TradeCouncil.Contract(s,c).Good)).Any(g=>g.Count()>1)||
            s.TradeCases.SelectMany(c=>c.CreditedCaravans).GroupBy(id=>id).Any(g=>g.Count()>1))
            throw new InvalidDataException("Te same dostawy obiecano lub rozliczono wielokrotnie.");
        foreach(var group in s.TradeCases.GroupBy(c=>TradeCouncil.City(s,c).Id))
        {
            var ordered=group.OrderBy(c=>c.OpenedTurn).ToArray();
            if(ordered.Zip(ordered.Skip(1)).Any(pair=>pair.Second.OpenedTurn-pair.First.OpenedTurn<TradeCouncil.PetitionInterval))
                throw new InvalidDataException("Powielone delegacje tej samej prowincji.");
        }
        if(s.PendingEvent==TradeCouncil.Event.Id?
            s.PendingCityId!=null||!s.TradeCases.Any(c=>c.CaravanId==s.PendingTradeCaseId&&c.Status==TradeCaseStatus.Pending&&TradeCouncil.City(s,c).Owner==0):s.PendingTradeCaseId!=null)
            throw new InvalidDataException("Uszkodzony kontekst delegacji kupców.");
    }
}
