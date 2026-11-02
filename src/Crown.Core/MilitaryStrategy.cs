namespace Crown.Core;

public enum ArmyTask { Garrison, Defend, Assemble, Advance }
public sealed record CityThreat(int CityId, int Enemies, double EnemyPower, int Defenders, double DefensePower);
public sealed record ArmyAssignment(int UnitId, ArmyTask Task, int CityId);
public sealed record MilitaryPlan(int Owner, int? ObjectiveCityId, bool Ready, IReadOnlyList<CityThreat> Threats, IReadOnlyList<ArmyAssignment> Assignments);

/// <summary>Deterministic, disposable plans. Orders still use the ordinary movement and combat rules.</summary>
public static class MilitaryStrategy
{
    public static bool Combatant(Unit u) => u.Kind is not (UnitKind.Scout or UnitKind.Settler);
    public static bool Ranged(Unit u) => u.Kind is UnitKind.Archer or UnitKind.Catapult;
    public static HashSet<int> Sight(GameState s, int owner)
    {
        var centers = s.Cities.Where(c => c.Owner == owner).Select(c => (c.Tile, Range: 3))
            .Concat(s.Units.Where(u => u.Owner == owner).Select(u => (u.Tile, Range: u.Kind == UnitKind.Scout ? 4 : 2))).ToArray();
        return s.Tiles.Where(t => centers.Any(c => World.Distance(t, s.Tiles[c.Tile]) <= c.Range)).Select(t => t.Id).ToHashSet();
    }
    public static IReadOnlyList<CityThreat> Threats(GameState s, int owner)
    {
        var sight = Sight(s, owner);
        var enemies = s.Units.Where(u => u.Kind != UnitKind.Settler && Engine.AtWar(s, owner, u.Owner) && sight.Contains(u.Tile)).ToArray();
        return s.Cities.Where(c => c.Owner == owner).OrderBy(c => c.Id).Select(c =>
        {
            var hostile = enemies.Where(u => World.Distance(s.Tiles[u.Tile], s.Tiles[c.Tile]) <= 5).ToArray();
            var defenders = s.Units.Where(u => u.Owner == owner && Combatant(u) && u.EscortCaravanId == null && World.Distance(s.Tiles[u.Tile], s.Tiles[c.Tile]) <= 3).ToArray();
            return new CityThreat(c.Id, hostile.Length, hostile.Sum(u => Engine.Strength(s, u)), defenders.Length, defenders.Sum(u => Engine.Strength(s, u)));
        }).ToArray();
    }
    public static MilitaryPlan Plan(GameState s, int owner)
    {
        var cities = s.Cities.Where(c => c.Owner == owner).OrderBy(c => c.Id).ToArray();
        var army = s.Units.Where(u => u.Owner == owner && Combatant(u) && u.EscortCaravanId == null).OrderBy(u => u.Id).ToArray();
        var threats = Threats(s, owner); var assignments = new List<ArmyAssignment>();
        if (cities.Length == 0) return new(owner, null, false, threats, assignments);
        var reserve = army.ToList(); bool war = s.Relations.Any(r => (r.A == owner || r.B == owner) && r.War);
        double typicalPower = Math.Max(10, army.Length == 0 ? 30 : army.Average(u => Engine.Strength(s, u)));
        foreach (var threat in threats.OrderByDescending(t => t.EnemyPower - t.DefensePower).ThenBy(t => t.CityId))
        {
            var city = cities.Single(c => c.Id == threat.CityId);
            int needed = threat.Enemies > 0 ? Math.Clamp((int)Math.Ceiling(threat.EnemyPower / typicalPower * 1.15), 2, 4) : war ? 1 : 0;
            for (int i = 0; i < needed && reserve.Count > 0; i++)
            {
                var unit = reserve.OrderBy(u => i == 0 && Ranged(u) ? 1 : 0)
                    .ThenBy(u => World.Distance(s.Tiles[u.Tile], s.Tiles[city.Tile]) + (100 - u.Health) / 25).ThenBy(u => u.Id).First();
                reserve.Remove(unit); assignments.Add(new(unit.Id, threat.Enemies > 0 ? ArmyTask.Defend : ArmyTask.Garrison, city.Id));
            }
        }
        // City locations and geography are strategic knowledge for AI, as in previous releases.
        // The player's assessment filters city objectives by explored terrain. Hostile troop counts always use current sight.
        var anchor = cities[0]; var probe = army.FirstOrDefault() ?? new Unit { Owner = owner, Tile = anchor.Tile, Kind = UnitKind.Spearmen };
        var route = Navigation.ForUnit(s, new Unit { Owner = owner, Tile = anchor.Tile, Kind = probe.Kind });
        var objective = s.Cities.Where(c => Engine.AtWar(s, owner, c.Owner) && (owner != 0 || s.Tiles[c.Tile].Explored))
            .Select(c => (City: c, Cost: World.Neighbors(s, c.Tile).Select(t => route.Costs.GetValueOrDefault(t.Id, double.PositiveInfinity)).DefaultIfEmpty(double.PositiveInfinity).Min()))
            .Where(x => double.IsFinite(x.Cost))
            .OrderBy(x => x.Cost + x.City.Health * .03 - (x.City.OriginalOwner == owner ? 12 : 0) - (s.Wars.Any(w => w.EndedTurn == null && w.Aggressor == owner && w.TargetCityId == x.City.Id) ? 8 : 0))
            .ThenBy(x => x.City.Id).Select(x => x.City).FirstOrDefault();
        var sight = Sight(s, owner);
        var rally = objective == null ? anchor : cities.OrderBy(c => World.Distance(s.Tiles[c.Tile], s.Tiles[objective.Tile])).ThenBy(c => c.Id).First();
        double opposition = objective == null ? 0 : s.Units.Where(u => Engine.AtWar(s, owner, u.Owner) && u.Kind != UnitKind.Settler && sight.Contains(u.Tile) && World.Distance(s.Tiles[u.Tile], s.Tiles[objective.Tile]) <= 3).Sum(u => Engine.Strength(s, u));
        var field = reserve.Where(u => u.Health >= 65).ToArray();
        var staged = field.Where(u => objective != null && (World.Distance(s.Tiles[u.Tile], s.Tiles[rally.Tile]) <= 4 || World.Distance(s.Tiles[u.Tile], s.Tiles[objective.Tile]) <= World.Distance(s.Tiles[rally.Tile], s.Tiles[objective.Tile]))).ToArray();
        double power = staged.Sum(u => Engine.Strength(s, u));
        double cityDefense = objective == null ? 0 : 20 + objective.Population * 1.5 + (objective.Buildings.Contains("walls") ? 10 : 0) + s.Factions[objective.Owner].Era * 3 + Epochs.Defense(s,objective);
        bool ready = objective != null && staged.Any(u => !Ranged(u)) && power >= cityDefense * .9 + opposition * .65 &&
            (objective.Health <= 30 || staged.Any(Ranged) || power >= cityDefense * 2.5);
        foreach (var u in reserve) assignments.Add(new(u.Id, ready ? ArmyTask.Advance : ArmyTask.Assemble, ready ? objective!.Id : rally.Id));
        return new(owner, objective?.Id, ready, threats, assignments);
    }
    public static Project? Recruitment(GameState s, City city)
    {
        var ownCities = s.Cities.Where(c => c.Owner == city.Owner).ToArray();
        var force = s.Units.Where(u => u.Owner == city.Owner && Combatant(u)).Select(u => u.Kind).ToList();
        force.AddRange(ownCities.Where(c => c.Id != city.Id).Select(c => Content.Project(c.Project).Unit).Where(k => k != null && k is not (UnitKind.Scout or UnitKind.Settler)).Select(k => k!.Value));
        bool war = s.Relations.Any(r => (r.A == city.Owner || r.B == city.Owner) && r.War);
        var threat = Threats(s, city.Owner).Single(c => c.CityId == city.Id);
        int limit = war ? ownCities.Length * 3 + 2 : ownCities.Length + 2;
        if (force.Count >= limit && threat.EnemyPower <= threat.DefensePower * 1.2) return null;
        if (s.Factions[city.Owner].Gold < 80 && Engine.Income(s, city.Owner).Gold < 0 && threat.Enemies == 0) return null;
        UnitKind desired = force.Count(k => k is UnitKind.Spearmen or UnitKind.Cavalry) < ownCities.Length + (war ? 1 : 0) ? UnitKind.Spearmen :
            war && !force.Contains(UnitKind.Catapult) ? UnitKind.Catapult : force.Count(k => k is UnitKind.Archer or UnitKind.Catapult) < Math.Max(1, force.Count / 3) ? UnitKind.Archer : UnitKind.Cavalry;
        var available = Engine.AvailableProjects(s, city).Where(p => p.Unit != null && p.Unit is not (UnitKind.Scout or UnitKind.Settler)).ToArray();
        return available.OrderBy(p => p.Unit == desired || desired == UnitKind.Archer && p.Unit == UnitKind.Catapult ? 0 : p.Unit == UnitKind.Spearmen ? 1 : 2)
            .ThenByDescending(p => UnitModels.All.FirstOrDefault(m => m.Id == p.Id)?.Strength ?? UnitModels.BaseStrength(new Unit { Kind = p.Unit!.Value }))
            .ThenBy(p => p.Cost).ThenBy(p => p.Id, StringComparer.Ordinal).FirstOrDefault();
    }
}

public static partial class Engine
{
    public static void RunMilitaryAi(GameState s, int owner)
    {
        if (owner is < 1 or > 3 || !s.EpochCampaign || s.Ending != null) return;
        var plan = MilitaryStrategy.Plan(s, owner);
        foreach (var assignment in plan.Assignments.OrderBy(a => MilitaryStrategy.Ranged(s.Units.Single(u => u.Id == a.UnitId)) ? 0 : 1).ThenBy(a => a.UnitId))
        {
            var u = s.Units.FirstOrDefault(u => u.Id == assignment.UnitId);
            if (s.Ending != null) break;
            if (u == null || u.Actions < .5 || u.EscortCaravanId != null) continue;
            var sight = MilitaryStrategy.Sight(s, owner);
            var enemies = s.Units.Where(e => AtWar(s, owner, e.Owner) && sight.Contains(e.Tile) && e.Kind != UnitKind.Settler).ToArray();
            double Exposure(int tile) => enemies.Where(e => World.Distance(s.Tiles[e.Tile], s.Tiles[tile]) <= UnitModels.Range(e) + 1).Sum(e => Strength(s, e));
            if (u.Health < 45 || !Supplied(s, u) && u.Health < 70 || u.Order == MilitaryOrder.Recover && u.Health < 90)
            {
                if (Supplied(s, u) && Exposure(u.Tile) == 0)
                { if (u.Order != MilitaryOrder.Recover) SetMilitaryOrder(s, u.Id, MilitaryOrder.Recover, owner); continue; }
                var supply = Logistics.For(s, owner); var retreat = Navigation.ForUnit(s, u);
                int safe = retreat.Costs.Keys.Where(t => supply.Costs.TryGetValue(t, out double cost) && cost <= Logistics.Range(s, owner))
                    .OrderBy(t => Exposure(t)).ThenBy(t => retreat.Costs[t]).ThenBy(t => t).DefaultIfEmpty(u.Tile).First();
                March(u, retreat.PathTo(safe)); continue;
            }
            if (TryAttack(u, sight)) continue;
            if (Districts.Pillage(s, u.Id, owner).Success) continue;
            var city = s.Cities.FirstOrDefault(c => c.Id == assignment.CityId);
            if (city == null) continue;
            bool advance = assignment.Task == ArmyTask.Advance && AtWar(s, owner, city.Owner);
            var path = Navigation.ForUnit(s, u);
            int radius = advance ? Math.Min(UnitModels.Range(u), sight.Contains(city.Tile) ? UnitModels.Range(u) : 2) : assignment.Task is ArmyTask.Garrison or ArmyTask.Defend ? 1 : 2;
            var positions = path.Costs.Keys.Where(t => World.Distance(s.Tiles[t], s.Tiles[city.Tile]) <= radius)
                .Where(t => !advance || t != city.Tile).ToArray();
            if (positions.Length > 0)
            {
                int goal = positions.OrderBy(t => path.Costs[t] + Exposure(t) * (MilitaryStrategy.Ranged(u) ? .10 : .035) +
                    s.Units.Count(e => e.Owner == owner && e.Id != u.Id && MilitaryStrategy.Combatant(e) && e.Tile == t) * 3 +
                    (advance ? World.Distance(s.Tiles[t], s.Tiles[city.Tile]) * (MilitaryStrategy.Ranged(u) ? -.3 : .3) : World.Distance(s.Tiles[t], s.Tiles[city.Tile]) * 2))
                    .ThenBy(t => t).First();
                March(u, path.PathTo(goal));
            }
            if (TryAttack(u, MilitaryStrategy.Sight(s, owner))) continue;
            if (u.Actions == u.MaxActions && u.Order != MilitaryOrder.Fortify) SetMilitaryOrder(s, u.Id, MilitaryOrder.Fortify, owner);
        }
        void March(Unit unit, IReadOnlyList<int> path)
        {
            foreach (int next in path.Skip(1))
            { if (Navigation.MovementCost(s, unit.Tile, next) > unit.Actions) break; EnterTile(s, unit, next); }
        }
        bool TryAttack(Unit unit, HashSet<int> visible)
        {
            if (unit.Actions < 1) return false;
            var target = s.Units.Where(e => AtWar(s, owner, e.Owner) && visible.Contains(e.Tile)).Select(e => e.Tile)
                .Concat(s.Cities.Where(c => AtWar(s, owner, c.Owner) && visible.Contains(c.Tile)).Select(c => c.Tile)).Distinct()
                .Select(t => (Tile: t, Preview: PreviewAttack(s, unit.Id, t)))
                .Where(x => x.Preview.Valid && unit.Health - x.Preview.DamageToAttacker > (x.Preview.CanCapture ? 5 : 20) &&
                    (x.Preview.Ranged || x.Preview.CanCapture || x.Preview.DamageToTarget >= x.Preview.DamageToAttacker * .8))
                .OrderByDescending(x => x.Preview.DamageToTarget - x.Preview.DamageToAttacker * 1.4 + (x.Preview.CanCapture ? 120 : 0) + (x.Preview.DamageToTarget >= x.Preview.TargetHealth ? 35 : 0))
                .ThenBy(x => x.Tile).FirstOrDefault();
            return target.Preview != null && Move(s, unit.Id, target.Tile).Success;
        }
    }
}
