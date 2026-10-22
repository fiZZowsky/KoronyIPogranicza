namespace Crown.Core;

public static class MilitaryOrders
{
    public static double DefenseMultiplier(Unit unit) => unit.Order == MilitaryOrder.Fortify ? 1 + .2 * unit.FortificationTurns : 1;
    public static string Name(Unit unit) => unit.Order switch
    {
        MilitaryOrder.Fortify => $"Okopywanie {unit.FortificationTurns}/2 · obrona +{unit.FortificationTurns * 20}%",
        MilitaryOrder.Recover => "Rekonwalescencja do pełnego zdrowia",
        _ => "Gotowość do działania"
    };
    public static string? OrderError(GameState s, Unit? unit, MilitaryOrder order, int owner = 0)
    {
        if (s.Ending != null || unit == null || owner is < 0 or > 3 || unit.Owner != owner || !Enum.IsDefined(order) || order == MilitaryOrder.None)
            return "Wybierz własny oddział i rozkaz.";
        if (unit.Kind == UnitKind.Settler) return "Osadnicy nie mogą przyjmować rozkazów wojskowych.";
        if (unit.EscortCaravanId != null) return "Najpierw odwołaj eskortę.";
        if (unit.Order == order) return "Ten rozkaz już obowiązuje.";
        if (unit.Actions != unit.MaxActions) return "Przygotowanie wymaga pełnego zapasu ruchu. Poczekaj do następnej tury.";
        if (order == MilitaryOrder.Recover && unit.Health >= 100) return "Oddział ma już pełne zdrowie.";
        return null;
    }
    public static void Clear(Unit unit) { unit.Order = MilitaryOrder.None; unit.FortificationTurns = 0; }
    public static double Healing(GameState s, Unit unit, bool recovering)
    {
        if (!Engine.Supplied(s, unit)) return 3;
        bool medicine = s.Factions[unit.Owner].Technologies.Contains("medicine");
        return recovering ? medicine ? 28 : 20 : medicine ? 20 : 12;
    }
    internal static void FinishTurn(GameState s, Unit unit)
    {
        if (unit.Order == MilitaryOrder.Fortify) unit.FortificationTurns = Math.Min(2, unit.FortificationTurns + 1);
        bool resting = unit.Order != MilitaryOrder.None || unit.Actions == unit.MaxActions && unit.Destination == null && unit.EscortCaravanId == null;
        if (resting) unit.Health = Math.Min(100, unit.Health + Healing(s, unit, unit.Order == MilitaryOrder.Recover));
        if (!Engine.Supplied(s, unit) && unit.Kind is not (UnitKind.Scout or UnitKind.Settler)) unit.Health = Math.Max(10, unit.Health - 5);
        if (unit.Order == MilitaryOrder.Recover && unit.Health >= 100)
        {
            Clear(unit);
            if (unit.Owner == 0) s.Log($"{Content.UnitName(unit.Kind)}: rekonwalescencja zakończona. Oddział wraca do gotowości.", "rozkazy");
        }
        unit.Actions = unit.MaxActions;
    }
    public static void Validate(GameState s)
    {
        if (s.Units.Any(u => !Enum.IsDefined(u.Order) || u.FortificationTurns is < 0 or > 2 ||
            u.Order != MilitaryOrder.Fortify && u.FortificationTurns != 0 ||
            u.Order != MilitaryOrder.None && (u.Kind == UnitKind.Settler || u.Destination != null || u.EscortCaravanId != null)))
            throw new InvalidDataException("Uszkodzone rozkazy wojskowe lub umocnienia.");
    }
}

public static partial class Engine
{
    public static Result SetMilitaryOrder(GameState s, int unitId, MilitaryOrder order, int owner = 0)
    {
        var unit = s.Units.FirstOrDefault(u => u.Id == unitId);
        string? error = MilitaryOrders.OrderError(s, unit, order, owner);
        if (error != null) return Result.Fail(error);
        MilitaryOrders.Clear(unit!);
        unit!.Destination = null; unit.Order = order; unit.Actions = 0;
        if (owner == 0) s.Log($"{Content.UnitName(unit.Kind)}: {MilitaryOrders.Name(unit)}. Poświęcono cały ruch tej tury.", "rozkazy");
        return Result.Ok(order == MilitaryOrder.Fortify ? "Przygotowanie pozycji rozpoczęte. +20% obrony po końcu tury, do +40% po dwóch turach." : "Oddział odpoczywa do pełnego zdrowia. Atak przeciwnika przerwie rekonwalescencję.");
    }
    public static Result WakeUnit(GameState s, int unitId)
    {
        var unit = s.Units.FirstOrDefault(u => u.Id == unitId && u.Owner == 0);
        if (s.Ending != null || unit == null || unit.Order == MilitaryOrder.None) return Result.Fail("Oddział nie ma stałego rozkazu wojskowego.");
        MilitaryOrders.Clear(unit);
        s.Log($"{Content.UnitName(unit.Kind)}: odwołano stały rozkaz i porzucono umocnienia.", "rozkazy");
        return Result.Ok("Przywrócono gotowość. Wydane punkty ruchu nie wracają.");
    }
}
