namespace Crown.Core;

internal static class MarketSaveRules
{
    public static void Validate(GameState s)
    {
        bool Amount(double value)=>double.IsFinite(value)&&value>=0&&value<=100000;
        if(s.LastMarketTurn<0||s.LastMarketTurn>s.Turn||s.NextContractId<1||s.Factions.Any(f=>!Enum.IsDefined(f.TradePolicy)||f.LastTradePolicyTurn< -10||f.LastTradePolicyTurn>s.Turn))
            throw new InvalidDataException("Uszkodzona polityka handlowa.");
        foreach(var c in s.Cities)
        {
            if(c.MarketStocks==null||c.MarketStocks.Count!=3||Markets.Goods.Any(g=>!c.MarketStocks.TryGetValue(g,out double stock)||!Amount(stock)||stock>Markets.Capacity(c)+.00001)||
                c.LastMarketTurn<0||c.LastMarketTurn>s.LastMarketTurn||!Amount(c.LastMarketTax)||c.LastMarketLines==null||
                (c.LastMarketTurn==0?c.LastMarketLines.Count!=0||c.LastMarketTax!=0:c.LastMarketLines.Count!=3||c.LastMarketLines.Select(l=>l.Good).Distinct().Count()!=3))
                throw new InvalidDataException("Uszkodzone magazyny prowincji.");
            foreach(var line in c.LastMarketLines)
            {
                double[] amounts=[line.Opening,line.Produced,line.DomesticIn,line.DomesticOut,line.ForeignIn,line.ForeignOut,line.Consumed,line.Demand,line.Lost,line.Closing];
                if(!Enum.IsDefined(line.Good)||amounts.Any(v=>!Amount(v))||!double.IsFinite(line.Price)||line.Price<=0||line.Price>100||line.Consumed>line.Demand+.00001||
                    Math.Abs(line.Opening+line.Produced+line.DomesticIn+line.ForeignIn-line.DomesticOut-line.ForeignOut-line.Consumed-line.Lost-line.Closing)>.00001||
                    Math.Abs(line.Closing-c.MarketStocks[line.Good])>.00001)
                    throw new InvalidDataException("Niezgodny bilans towarów.");
            }
        }
        if(s.TradeContracts==null||s.TradeContracts.Count>4000||s.TradeContracts.Select(c=>c.Id).Distinct().Count()!=s.TradeContracts.Count)
            throw new InvalidDataException("Uszkodzona lista kontraktów.");
        foreach(var c in s.TradeContracts)
        {
            if(c.Id<1||c.Id>=s.NextContractId||c.Buyer is <0 or >3||c.Seller is <0 or >3||c.Buyer==c.Seller||
                c.BuyerCity==c.SellerCity||!s.Cities.Any(city=>city.Id==c.BuyerCity)||!s.Cities.Any(city=>city.Id==c.SellerCity)||!Enum.IsDefined(c.Good)||!Enum.IsDefined(c.Status)||
                c.Quantity is <1 or >6||c.StartedTurn<1||c.StartedTurn>s.Turn||c.ExpiresTurn!=c.StartedTurn+12||c.Status==ContractStatus.Active&&c.ExpiresTurn<s.Turn||
                c.LastExecutedTurn<0||c.LastExecutedTurn>s.LastMarketTurn||c.LastExecutedTurn>0&&c.LastExecutedTurn<c.StartedTurn||
                !Amount(c.LastDelivered)||c.LastDelivered>c.Quantity||!Amount(c.LastPayment)||!Amount(c.LastPrice)||c.LastPrice>100||
                c.LastExecutedTurn==0&&(c.LastDelivered!=0||c.LastPayment!=0||c.LastPrice!=0)||
                Math.Abs(c.LastPayment-Math.Round(c.LastDelivered*c.LastPrice,2))>.00001||c.Message==null||c.Message.Length>1000)
                throw new InvalidDataException("Uszkodzone warunki kontraktu.");
        }
        if(s.TradeContracts.Where(c=>c.Status==ContractStatus.Active).GroupBy(c=>(c.BuyerCity,c.SellerCity,c.Good)).Any(g=>g.Count()>1)||
            s.Factions.Any(f=>s.TradeContracts.Count(c=>c.Status==ContractStatus.Active&&(c.Buyer==f.Id||c.Seller==f.Id))>3))
            throw new InvalidDataException("Sprzeczne lub nadmiarowe umowy.");
    }
}
