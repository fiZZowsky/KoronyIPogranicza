namespace Crown.Core;

public static partial class Engine
{
    public static EconomyReport Economy(GameState s, int owner)
    {
        double gross = s.Cities.Where(c => c.Owner == owner).Sum(c => CityYields(s, c).Gold);
        return Costs(s, owner, gross);
    }

    private static EconomyReport Costs(GameState s, int owner, double gross)
    {
        var f = s.Factions[owner];
        double trade = s.Relations.Count(r => (r.A == owner || r.B == owner) && r.Trade && !r.War) *
            (f.Technologies.Contains("navigation") ? 14 : 6);
        double army = s.Units.Count(u => u.Owner == owner) * 1.4;
        double administration = s.Cities.Where(c => c.Owner == owner).Sum(c => 2 + c.Buildings.Count * .35) *
            (f.Technologies.Contains("civilservice") ? .75 : 1);
        double roads = s.Tiles.Count(t => t.Road && t.RoadOwner == owner) * Logistics.RoadUpkeep;
        return new(gross, trade, army, administration, roads,s.Cities.Where(c=>c.Owner==owner).Sum(c=>c.LastMarketTax),Markets.LastTradeBalance(s,owner));
    }

    public static Result BuildRoad(GameState s, int tileId, int owner = 0)
    {
        if (s.Ending != null || tileId < 0 || tileId >= s.Tiles.Count || owner < 0 || owner >= s.Factions.Count)
            return Result.Fail("Nieprawidłowy rozkaz budowy.");
        var t = s.Tiles[tileId];
        var f = s.Factions[owner];
        if (!f.Technologies.Contains("pottery")) return Result.Fail("Budowa dróg wymaga Garncarstwa.");
        if (!t.Passable || owner == 0 && !t.Explored || t.Owner >= 0 && t.Owner != owner || Navigation.RoadNode(s, tileId))
            return Result.Fail("Droga wymaga odkrytego, wolnego pola na własnej lub neutralnej ziemi.");
        if (s.Units.Any(u => u.Owner != owner && u.Tile == tileId)) return Result.Fail("Obcy oddział blokuje budowę.");
        if (f.Gold < Logistics.RoadPrice) return Result.Fail("Droga kosztuje 25 złota.");
        f.Gold -= Logistics.RoadPrice;
        t.Road = true;
        t.RoadOwner = owner;
        s.InvalidateLogistics();
        return Result.Ok("Zbudowano drogę. Ruch po połączonych odcinkach kosztuje 0,5 punktu; utrzymanie: 0,15 złota/turę.");
    }

    public static Result ConnectCity(GameState s, int cityId)
    {
        var city = s.Cities.FirstOrDefault(c => c.Id == cityId && c.Owner == 0);
        if (s.Ending != null || city == null) return Result.Fail("Wybierz własne miasto.");
        if (!s.Player.Technologies.Contains("pottery")) return Result.Fail("Budowa dróg wymaga Garncarstwa.");
        var plan = Logistics.PlanRoad(s, cityId);
        if (plan.Error != null) return Result.Fail(plan.Error);
        if (plan.NewTiles == 0) return Result.Fail("Trasa ma już drogi. Sprawdź, czy nie blokują jej obce oddziały lub obce odcinki.");
        if (s.Player.Gold < plan.Cost) return Result.Fail($"Cała trasa kosztuje {plan.Cost} złota.");
        // Validate the whole quote before mutating so insufficient funds never produce a partial route.
        foreach (int id in plan.Path.Where(id => !Navigation.RoadNode(s, id)))
        {
            s.Tiles[id].Road = true;
            s.Tiles[id].RoadOwner = 0;
        }
        s.Player.Gold -= plan.Cost;
        s.InvalidateLogistics();
        s.Log($"Droga do {city.Name}: {plan.NewTiles} odcinków za {plan.Cost} złota. Handel zależy teraz od bezpieczeństwa trasy.", "infrastruktura");
        return Result.Ok($"Zbudowano {plan.NewTiles} odcinków drogi do {city.Name}.");
    }

    public static Result PillageRoad(GameState s, int unitId)
    {
        var u = s.Units.FirstOrDefault(u => u.Id == unitId && u.Owner == 0);
        if (s.Ending != null || u == null || u.Actions < 1 || u.Kind == UnitKind.Settler)
            return Result.Fail("Potrzebny oddział z co najmniej 1 punktem ruchu.");
        if(u.EscortCaravanId!=null)return Result.Fail("Najpierw odwołaj eskortę, aby niszczyć drogi.");
        var tile = s.Tiles[u.Tile];
        if (!tile.Road || !AtWar(s, u.Owner, tile.RoadOwner)) return Result.Fail("Możesz zniszczyć drogę państwa, z którym toczysz wojnę.");
          DiplomaticCouncil.AddGrievance(s, tile.RoadOwner, u.Owner, 12);
          tile.Road = false;
        tile.RoadOwner = -1;
        u.Actions -= 1;
        u.Destination = null;
        s.Player.Gold += 20;
        s.InvalidateLogistics();
        s.Log("Oddział zniszczył wrogi odcinek drogi. Zdobyto 20 złota; łańcuch dostaw został przerwany.", "wojna");
        return Result.Ok("Droga zniszczona: +20 złota, koszt 1 punkt ruchu.");
    }
}
