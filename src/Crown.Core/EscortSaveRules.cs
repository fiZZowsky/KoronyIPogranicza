namespace Crown.Core;

internal static class EscortSaveRules
{
    public static void Validate(GameState s)
    {
        foreach(var unit in s.Units.Where(u=>u.EscortCaravanId!=null))
        {
            var cargo=s.Caravans.FirstOrDefault(c=>c.Id==unit.EscortCaravanId&&Caravans.Active(c));
            if(cargo==null||unit.Kind==UnitKind.Settler||unit.Destination!=null||unit.Tile!=cargo.Tile)
                throw new InvalidDataException("Uszkodzony rozkaz eskorty.");
            var contract=s.TradeContracts.First(t=>t.Id==cargo.ContractId);
            if(unit.Owner!=contract.Buyer&&unit.Owner!=contract.Seller)throw new InvalidDataException("Eskorta należy do państwa spoza umowy.");
        }
        if(s.Units.Where(u=>u.EscortCaravanId!=null).GroupBy(u=>u.EscortCaravanId).Any(g=>g.Count()>1))
            throw new InvalidDataException("Karawana ma więcej niż jedną wyznaczoną eskortę.");
    }
}
