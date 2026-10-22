namespace Crown.Core;

public static partial class Engine
{
    public static CombatPreview PreviewAttack(GameState s, int unitId, int target)
    {
        var u = s.Units.FirstOrDefault(u => u.Id == unitId);
        CombatPreview Invalid(string text) => new(false, text, u == null ? "" : Content.UnitName(u.Kind), "");
        if (s.Ending != null || u == null || target < 0 || target >= s.Tiles.Count) return Invalid("Nieprawidłowy cel.");
        if(u.EscortCaravanId!=null)return Invalid("Najpierw odwołaj eskortę, aby zaatakować samodzielnie.");
        if (u.Actions < 1) return Invalid("Atak wymaga co najmniej 1 punktu ruchu.");
        if (u.Owner == 0 && !s.Tiles[target].Explored) return Invalid("Cel nie został odkryty.");
        var defender = s.Units.FirstOrDefault(e => e.Tile == target && e.Owner != u.Owner);
        var city = s.Cities.FirstOrDefault(c => c.Tile == target && c.Owner != u.Owner);
        if (defender == null && city == null) return Invalid("Na polu nie ma przeciwnika.");
        int owner = defender?.Owner ?? city!.Owner;
        if (!AtWar(s, u.Owner, owner)) return Invalid("Najpierw wypowiedz wojnę w panelu dyplomacji.");
        if (u.Kind == UnitKind.Settler) return Invalid("Osadnicy nie mogą walczyć.");
        bool ranged = u.Kind is UnitKind.Archer or UnitKind.Catapult;
        int distance = World.Distance(s.Tiles[u.Tile], s.Tiles[target]);
        if (distance > (ranged ? 2 : 1)) return Invalid("Wróg jest poza zasięgiem ataku.");
        if (ranged && distance == 2 && !World.Neighbors(s, u.Tile).Any(t => World.Distance(t, s.Tiles[target]) == 1 && t.Terrain != Terrain.Mountain))
            return Invalid("Góry zasłaniają cel.");

        var modifiers = new List<string>();
        double attack = Strength(s, u), defense;
        if (!Supplied(s, u)) modifiers.Add("Atakujący bez dostaw: siła ×0,72.");
        if (defender != null)
        {
            bool terrain = s.Tiles[target].Terrain is Terrain.Forest or Terrain.Hills;
            defense = Strength(s, defender) * MilitaryOrders.DefenseMultiplier(defender) + (terrain ? 5 : 0);
            if (defender.Order == MilitaryOrder.Fortify) modifiers.Add($"Umocnienia przeciwnika {defender.FortificationTurns}/2: siła obronna +{defender.FortificationTurns * 20}% (przed premią terenu).");
            if (terrain) modifiers.Add("Las lub wzgórza: +5 obrony przeciwnika.");
            if (!Supplied(s, defender)) modifiers.Add("Przeciwnik bez dostaw: siła ×0,72.");
            if (city != null) modifiers.Add("Najpierw walczysz z garnizonem. Ten atak nie zdobywa miasta.");
        }
        else
        {
            defense = 20 + city!.Population * 1.5 + (city.Buildings.Contains("walls") ? 10 : 0) + s.Factions[owner].Era * 3;
            if (city.Buildings.Contains("walls")) modifiers.Add("Mury miejskie: +10 obrony.");
            if (u.Kind == UnitKind.Catapult) { attack *= 2; modifiers.Add("Katapulta przeciw miastu: siła ×2."); }
            if (ranged) modifiers.Add("Ostrzał nie zdobywa miasta. Potrzebna piechota lub kawaleria.");
        }
        double health = defender?.Health ?? city!.Health;
        double damage = Math.Min(health, Math.Clamp(28 * attack / Math.Max(2, defense), 8, 65));
        double counter = ranged ? 0 : Math.Min(u.Health, defender != null ?
            Math.Clamp(21 * defense / Math.Max(2, attack), 5, 55) : Math.Clamp(15 * defense / Math.Max(2, attack), 4, 45));
        if (ranged) modifiers.Add("Atak dystansowy: bez kontrataku.");
        if (u.Order != MilitaryOrder.None) modifiers.Add("Atak porzuci stały rozkaz i własne umocnienia; nie wzmacniają one ataku.");
        if (defender?.Order == MilitaryOrder.Recover) modifiers.Add("Trafienie przerwie rekonwalescencję przeciwnika.");
        modifiers.Add("Siła uwzględnia zdrowie, doświadczenie, technologie i cechę państwa.");
        bool capture = defender == null && city != null && !ranged && damage >= health && counter < u.Health;
        return new(true, "", Content.UnitName(u.Kind), defender == null ? city!.Name : Content.UnitName(defender.Kind),
            attack, defense, health, damage, counter, capture, ranged, modifiers);
    }

    private static Result Attack(GameState s, Unit u, int target, Unit? defender, City? city)
    {
        var preview = PreviewAttack(s, u.Id, target);
        if (!preview.Valid) return Result.Fail(preview.Reason);
        MilitaryOrders.Clear(u);
        int owner = defender?.Owner ?? city!.Owner;
        WarCouncil.RecordBattle(s,u.Owner,owner,preview.DamageToAttacker,preview.DamageToTarget);
        u.Actions = 0;
        u.Destination = null;
        u.Health -= preview.DamageToAttacker;
        if (defender != null)
        {
            if (defender.Order == MilitaryOrder.Recover)
            {
                MilitaryOrders.Clear(defender);
                if (defender.Owner == 0) s.Log($"{Content.UnitName(defender.Kind)}: atak przerwał rekonwalescencję.", "rozkazy");
            }
            defender.Health -= preview.DamageToTarget;
            if (defender.Health <= 0) { s.Units.Remove(defender); u.Experience += 2; s.Factions[u.Owner].Prestige += 3; }
        }
        else
        {
            city!.Health = Math.Max(0, city.Health - preview.DamageToTarget);
            if (preview.CanCapture)
            {
                int old = city.Owner;
                ProvincialPolitics.Transfer(s, city, u.Owner);
                DiplomaticCouncil.AddGrievance(s, old, u.Owner, 30);
                u.Tile = city.Tile;
                s.Factions[u.Owner].Prestige += 25;
                s.Log($"{s.Factions[u.Owner].Name} zdobywa {city.Name}. Nowa administracja mierzy się z oporem mieszkańców.", "wojna");
            }
        }
        if (u.Health <= 0) s.Units.Remove(u); else u.Experience++;
        s.InvalidateLogistics();
        if (u.Owner == 0 || owner == 0)
            s.Log($"{Content.UnitName(u.Kind)} atakują pod ({s.Tiles[target].X}, {s.Tiles[target].Y}): {preview.DamageToTarget:0} obrażeń.", "wojna");
        Explore(s); CheckEnding(s);
        return Result.Ok($"Atak zadał {preview.DamageToTarget:0} obrażeń. Straty własne: {preview.DamageToAttacker:0}.");
    }
}
