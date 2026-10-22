namespace Crown.Core;

public static partial class Engine
{
    public static Result SetTradePolicy(GameState s,TradePolicy policy)
    {
        var f=s.Player;
        if(s.Ending!=null||!Enum.IsDefined(policy)||f.TradePolicy==policy)return Result.Fail("Nieprawidłowa lub już obowiązująca polityka.");
        if(s.PendingEvent=="market_charter")return Result.Fail("Najpierw odpowiedz radzie w sprawie handlu.");
        if(s.Turn-f.LastTradePolicyTurn<8)return Result.Fail("Zmiana polityki jest możliwa raz na osiem tur.");
        if(f.Gold<Markets.PolicyPrice)return Result.Fail("Zmiana polityki kosztuje 30 złota.");
        f.Gold-=Markets.PolicyPrice;f.TradePolicy=policy;f.LastTradePolicyTurn=s.Turn;
        s.Log($"Polityka handlowa: {Markets.PolicyName(policy)}. {Markets.PolicyDescription(policy)}","decyzja");
        return Result.Ok("Przyjęto politykę handlową. Obowiązuje od następnego rozliczenia rynku.");
    }

    public static Result SignTradeContract(GameState s,int buyerCity,int sellerCity,TradeGood good,int quantity,int owner=0)
    {
        var buyer=s.Cities.FirstOrDefault(c=>c.Id==buyerCity);var seller=s.Cities.FirstOrDefault(c=>c.Id==sellerCity);
        if(s.Ending!=null||owner<0||owner>3||buyer==null||seller==null||buyer.Owner==seller.Owner||buyer.Owner!=owner&&seller.Owner!=owner||!Enum.IsDefined(good)||quantity is <1 or >6)
            return Result.Fail("Nieprawidłowe strony lub ilość towaru.");
        if(owner!=0&&(buyer.Owner==0||seller.Owner==0))return Result.Fail("Umowy Marchii wymagają zgody gracza.");
        var draft=new TradeContract{Buyer=buyer.Owner,Seller=seller.Owner,BuyerCity=buyer.Id,SellerCity=seller.Id,Good=good,Quantity=quantity};
        string? error=Markets.RouteError(s,draft);if(error!=null)return Result.Fail(error);
        if(s.TradeContracts.Any(c=>c.Status==ContractStatus.Active&&c.BuyerCity==buyerCity&&c.SellerCity==sellerCity&&c.Good==good))return Result.Fail("Taka umowa już obowiązuje.");
        if(new[]{buyer.Owner,seller.Owner}.Any(id=>s.TradeContracts.Count(c=>c.Status==ContractStatus.Active&&(c.Buyer==id||c.Seller==id))>=3))return Result.Fail("Jedna ze stron osiągnęła limit trzech aktywnych umów.");
        if(s.Factions[owner].Gold<Markets.ContractPrice)return Result.Fail("Przygotowanie umowy kosztuje 15 złota.");
        s.Factions[owner].Gold-=Markets.ContractPrice;draft.Id=s.NextContractId++;draft.StartedTurn=s.Turn;draft.ExpiresTurn=s.Turn+12;s.TradeContracts.Add(draft);
        s.Log($"Umowa #{draft.Id}: {seller.Name} → {buyer.Name}, {Markets.Name(good)} do {quantity}/turę, do tury {draft.ExpiresTurn}. Cena zależy od rynku sprzedawcy; dostawa nie jest gwarantowana.","handel");
        return Result.Ok("Podpisano umowę na 12 tur. Pierwsza dostawa przy kolejnym rozliczeniu rynku.");
    }

    public static Result CancelTradeContract(GameState s,int id)
    {
        var contract=s.TradeContracts.FirstOrDefault(c=>c.Id==id&&(c.Buyer==0||c.Seller==0)&&c.Status==ContractStatus.Active);
        if(s.Ending!=null||contract==null)return Result.Fail("Nie ma takiej aktywnej umowy Marchii.");
        contract.Status=ContractStatus.Cancelled;contract.Message="Umowę rozwiązano decyzją Marchii.";
        s.Log($"Rozwiązano umowę handlową #{id}.","decyzja");return Result.Ok("Rozwiązano umowę. Dawne płatności pozostają w historii.");
    }
}
