namespace Crown.Core;

public static class EscortOrders
{
    public static Unit? Attached(GameState s,Caravan cargo)=>s.Units.FirstOrDefault(u=>u.EscortCaravanId==cargo.Id&&u.Tile==cargo.Tile);
    public static string? AssignmentError(GameState s,Unit? unit,Caravan? cargo,int owner=0)
    {
        if(s.Ending!=null||unit==null||cargo==null||unit.Owner!=owner||owner is <0 or >3||!Caravans.Active(cargo))return "Wybierz własny oddział i aktywną karawanę.";
        var contract=s.TradeContracts.First(t=>t.Id==cargo.ContractId);
        if(unit.Owner!=contract.Buyer&&unit.Owner!=contract.Seller)return "Oddział musi należeć do strony umowy.";
        if(unit.Kind==UnitKind.Settler)return "Osadnicy nie mogą pełnić eskorty.";
        if(unit.EscortCaravanId!=null||s.Units.Any(u=>u.EscortCaravanId==cargo.Id))return "Oddział lub karawana ma już przydzieloną eskortę.";
        if(unit.Tile!=cargo.Tile)return "Przenieś oddział na pole karawany przed wydaniem rozkazu.";
        if(unit.Actions<.5)return "Rozkaz eskorty wymaga co najmniej 0,5 pozostałego punktu ruchu.";
        return null;
    }
    public static void Normalize(GameState s)
    {
        foreach(var unit in s.Units.Where(u=>u.EscortCaravanId!=null))
        {
            var cargo=s.Caravans.FirstOrDefault(c=>c.Id==unit.EscortCaravanId&&Caravans.Active(c));
            var contract=cargo==null?null:s.TradeContracts.First(t=>t.Id==cargo.ContractId);
            if(cargo!=null&&unit.Tile==cargo.Tile&&unit.Kind!=UnitKind.Settler&&(unit.Owner==contract!.Buyer||unit.Owner==contract.Seller))continue;
            if(unit.Owner==0)s.Log($"{Content.UnitName(unit.Kind)}: zakończono eskortę #{unit.EscortCaravanId} — transport zakończył podróż lub oddział został odłączony.","rozkazy");
            unit.EscortCaravanId=null;
        }
    }
    public static void ManageAi(GameState s,int owner)
    {
        if(owner is <1 or >3)return;
        foreach(var cargo in s.Caravans.Where(Caravans.Active).OrderBy(c=>c.Id))
        {
            var unit=s.Units.Where(u=>u.Owner==owner&&u.Kind==UnitKind.Scout&&u.Health>=60&&u.Tile==cargo.Tile&&u.EscortCaravanId==null)
                .OrderBy(u=>u.Id).FirstOrDefault();
            if(unit!=null&&AssignmentError(s,unit,cargo,owner)==null)Engine.AssignEscort(s,unit.Id,cargo.Id,owner);
        }
    }
}

public static partial class Engine
{
    public static Result AssignEscort(GameState s,int unitId,int caravanId,int owner=0)
    {
        var unit=s.Units.FirstOrDefault(u=>u.Id==unitId);var cargo=s.Caravans.FirstOrDefault(c=>c.Id==caravanId);
        string? error=EscortOrders.AssignmentError(s,unit,cargo,owner);if(error!=null)return Result.Fail(error);
        MilitaryOrders.Clear(unit!);unit!.Destination=null;unit.EscortCaravanId=caravanId;
        if(owner==0)s.Log($"{Content.UnitName(unit.Kind)} obejmuje eskortę karawany #{caravanId}. Marsz w fazie rynku, kosztem pozostałych punktów ruchu; bez samodzielnych ataków i odpoczynku.","rozkazy");
        return Result.Ok("Przydzielono eskortę. Ruch wspólny przy następnym rozliczeniu rynku; granice wojskowe nadal obowiązują.");
    }
    public static Result CancelEscort(GameState s,int unitId)
    {
        var unit=s.Units.FirstOrDefault(u=>u.Id==unitId&&u.Owner==0&&u.EscortCaravanId!=null);
        if(s.Ending!=null||unit==null)return Result.Fail("Oddział nie ma aktywnego rozkazu eskorty Marchii.");
        s.Log($"{Content.UnitName(unit.Kind)}: odwołano eskortę karawany #{unit.EscortCaravanId}.","rozkazy");unit.EscortCaravanId=null;
        return Result.Ok("Odwołano eskortę. Wydane punkty ruchu nie wracają; pozostałe są dostępne.");
    }
    internal static void ApplyEscortMovement(GameState s,EscortMovement move)
    {
        var unit=s.Units.First(u=>u.Id==move.UnitId);
        foreach(int next in move.Path.Skip(1))EnterTile(s,unit,next);
        if(move.ReleaseReason!=null)
        {
            unit.EscortCaravanId=null;
            if(unit.Owner==0)s.Log($"{Content.UnitName(unit.Kind)}: eskorta karawany #{move.CaravanId} zakończona. {move.ReleaseReason}","rozkazy");
        }
    }
}
