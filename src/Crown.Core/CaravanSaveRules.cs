namespace Crown.Core;

internal static class CaravanSaveRules
{
    public static void Validate(GameState s)
    {
        if(s.Caravans==null||s.Caravans.Count>8000||s.NextCaravanId<1||s.Caravans.Any(c=>c==null)||s.Caravans.Select(c=>c.Id).Distinct().Count()!=s.Caravans.Count)
            throw new InvalidDataException("Uszkodzona lista karawan.");
        foreach(var c in s.Caravans)
        {
            var contract=s.TradeContracts.FirstOrDefault(t=>t.Id==c.ContractId);
            if(contract==null||c.Id<1||c.Id>=s.NextCaravanId||c.Tile<0||c.Tile>=s.Tiles.Count||!s.Tiles[c.Tile].Passable||!Enum.IsDefined(c.Status)||
                c.DepartedTurn<contract.StartedTurn||c.DepartedTurn>=contract.ExpiresTurn||c.DepartedTurn>s.LastMarketTurn||
                c.LastMovedTurn<c.DepartedTurn||c.LastMovedTurn>s.LastMarketTurn||c.BlockedTurns is <0 or >Caravans.MaxBlockedTurns||
                !double.IsFinite(c.Quantity)||c.Quantity<=0||c.Quantity>contract.Quantity||!double.IsFinite(c.Price)||c.Price<=0||c.Price>100||
                !double.IsFinite(c.Payment)||Math.Abs(c.Payment-Math.Round(c.Quantity*c.Price,2))>.00001||
                c.Message==null||c.Message.Length>1000||
                (Caravans.Active(c)?c.ResolvedTurn!=null||c.BlockedTurns>=Caravans.MaxBlockedTurns:
                    c.ResolvedTurn==null||c.ResolvedTurn<=c.DepartedTurn||c.ResolvedTurn!=c.LastMovedTurn)||
                c.Status==CaravanStatus.Travelling&&c.BlockedTurns!=0||c.Status==CaravanStatus.Waiting&&c.BlockedTurns==0||
                c.Status==CaravanStatus.Delivered&&(c.BlockedTurns!=0||c.Tile!=s.Cities.First(city=>city.Id==contract.BuyerCity).Tile))
                throw new InvalidDataException("Uszkodzony manifest karawany.");
        }
        if(s.Caravans.Where(Caravans.Active).GroupBy(c=>c.ContractId).Any(g=>g.Count()>1)||
            s.Caravans.GroupBy(c=>(c.ContractId,c.DepartedTurn)).Any(g=>g.Count()>1))
            throw new InvalidDataException("Powielony ładunek karawany.");
    }
}
