namespace Crown.Core;

public enum Terrain { Ocean, Coast, Grassland, Forest, Hills, Mountain, Desert, Marsh, Tundra }
public enum Resource { None, Grain, Iron, Horses, Stone, Salt, Dyes }
public enum Improvement { None, Farm, Mine, TradingPost }
public enum UnitKind { Scout, Spearmen, Archer, Cavalry, Settler, Catapult }
public enum CityFocus { Balanced, Agriculture, Industry, Scholarship }
public enum Governance { Integrated, Autonomous, Military }
public enum WarReason { Conquest, Redress, Liberation }
public enum DemandKind { Compensation, NonAggression }
public enum DemandStatus { Pending, Accepted, Rejected, Cancelled }
public enum PeaceStatus { Travelling, AwaitingPlayer, Accepted, Rejected, Cancelled, Expired }
public sealed class Tile
{
    public int Id { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public Terrain Terrain { get; set; }
    public Resource Resource { get; set; }
    public int Owner { get; set; } = -1;
    public Improvement Improvement { get; set; }
    public bool River { get; set; }
    public bool Explored { get; set; }
    public bool Ruin { get; set; }
    public bool Road { get; set; }
    public int RoadOwner { get; set; } = -1;
    public bool Passable => Terrain is not (Terrain.Ocean or Terrain.Coast or Terrain.Mountain);
}
public sealed class City
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Owner { get; set; }
    public int Tile { get; set; }
    public int Population { get; set; } = 3;
    public double Food { get; set; } = 12;
    public double Production { get; set; }
    public string Project { get; set; } = "granary";
    public HashSet<string> Buildings { get; set; } = [];
    public double Health { get; set; } = 100;
    public int Unrest { get; set; }
    public int OriginalOwner { get; set; }
    public Governance Governance { get; set; }
    public int Loyalty { get; set; } = 75;
    public int OccupiedUntil { get; set; }
    public int RevoltTurns { get; set; }
    public int LastGovernanceTurn { get; set; } = -10;
    public CityFocus Focus { get; set; }
    public int Prosperity { get; set; } = 50;
    public int HungerTurns { get; set; }
    public int LastFocusTurn { get; set; } = -1;
    public int LastAidTurn { get; set; } = -10;
    public int LastMigrationTurn { get; set; } = -20;
    public int LastCouncilTurn { get; set; } = -20;
    public int MigrantsReceived { get; set; }
    public int MigrantsDeparted { get; set; }
    public double LastFoodImported { get; set; }
    public double LastFoodExported { get; set; }
    public HashSet<string> Charters { get; set; } = [];
    public HashSet<string> ResolvedIssues { get; set; } = [];
}
public sealed class Unit
{
    public int Id { get; set; }
    public int Owner { get; set; }
    public int Tile { get; set; }
    public UnitKind Kind { get; set; }
    public double Health { get; set; } = 100;
    public double Actions { get; set; } = 2;
    public int? Destination { get; set; }
    public int Experience { get; set; }
    public int MaxActions => Kind is UnitKind.Scout or UnitKind.Cavalry ? 3 : 2;
}
public sealed class Faction
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Leader { get; set; } = "";
    public string Color { get; set; } = "#C4A261";
    public string Trait { get; set; } = "";
    public double Gold { get; set; } = 130;
    public double Science { get; set; }
    public double Culture { get; set; }
    public double Prestige { get; set; }
    public int Stability { get; set; } = 65;
    public int Commons { get; set; } = 58;
    public int Nobles { get; set; } = 58;
    public int Scholars { get; set; } = 58;
    public int Tax { get; set; } = 1;
    public int Government { get; set; }
    public int Focus { get; set; }
    public int LastPolicyTurn { get; set; } = -1;
    public double WarWeariness { get; set; }
    public string Research { get; set; } = "agriculture";
    public HashSet<string> Technologies { get; set; } = [];
    public HashSet<string> Flags { get; set; } = [];
    public int Era => Technologies.Count / 5;
}
public sealed class Relation
{
    public int A { get; set; }
    public int B { get; set; }
    public int Trust { get; set; }
    public bool War { get; set; }
    public bool Trade { get; set; }
    public int TruceUntil { get; set; }
    public int LastActionTurn { get; set; } = -1;
    public int GrievanceA { get; set; }
    public int GrievanceB { get; set; }
    public int LastDemandTurn { get; set; } = -10;
    public int LastPeaceTurn { get; set; } = -10;
}
public sealed class WarCampaign
{
    public int Id { get; set; }
    public int A { get; set; }
    public int B { get; set; }
    public int Aggressor { get; set; }
    public WarReason Reason { get; set; }
    public int? TargetCityId { get; set; }
    public bool GoalDeclared { get; set; }
    public int StartedTurn { get; set; }
    public int? EndedTurn { get; set; }
    public List<int> StartingCitiesA { get; set; } = [];
    public List<int> StartingCitiesB { get; set; } = [];
    public double LossesA { get; set; }
    public double LossesB { get; set; }
    public string Outcome { get; set; } = "";
}
public sealed class PeaceOffer
{
    public int Id { get; set; }
    public int WarId { get; set; }
    public int From { get; set; }
    public int To { get; set; }
    // Positive gold is paid by From, negative by To. A city returns to its founder.
    public int Gold { get; set; }
    public int? ReturnCityId { get; set; }
    public int? InReplyTo { get; set; }
    public int SentTurn { get; set; }
    public int DueTurn { get; set; }
    public int DeadlineTurn { get; set; }
    public int? ResolvedTurn { get; set; }
    public PeaceStatus Status { get; set; }
    public string Reply { get; set; } = "";
}
public sealed class DiplomaticDemand
{
    public int Id { get; set; }
    public int From { get; set; }
    public int To { get; set; }
    public DemandKind Kind { get; set; }
    public DemandStatus Status { get; set; }
    public int SentTurn { get; set; }
    public int DueTurn { get; set; }
    public int? ResolvedTurn { get; set; }
    public string Reply { get; set; } = "";
}
public sealed class ChronicleEntry
{
    public int Turn { get; set; }
    public string Text { get; set; } = "";
    public string Category { get; set; } = "świat";
}
public sealed class DeferredEffect
{
    public int Due { get; set; }
    public string Text { get; set; } = "";
    public int Faction { get; set; }
    public double Gold { get; set; }
    public int Stability { get; set; }
    public int Commons { get; set; }
    public int Nobles { get; set; }
    public int Scholars { get; set; }
    public int Prestige { get; set; }
}
public sealed class GameState
{
    public int Version { get; set; } = 5;
    public int Seed { get; set; }
    public uint RandomState { get; set; }
    public int Width { get; set; } = 42;
    public int Height { get; set; } = 28;
    public int Turn { get; set; } = 1;
    public int MaxTurns { get; set; } = 320;
    public string Difficulty { get; set; } = "Standard";
    public List<Tile> Tiles { get; set; } = [];
    public List<City> Cities { get; set; } = [];
    public List<Unit> Units { get; set; } = [];
    public List<Faction> Factions { get; set; } = [];
    public List<Relation> Relations { get; set; } = [];
    public List<ChronicleEntry> Chronicle { get; set; } = [];
    public List<DeferredEffect> Deferred { get; set; } = [];
    public HashSet<string> SeenEvents { get; set; } = [];
    public string? PendingEvent { get; set; } = "founding";
    public int? PendingCityId { get; set; }
    public List<MigrationRecord> Migrations { get; set; } = [];
    public List<DiplomaticDemand> Demands { get; set; } = [];
    public int NextDemandId { get; set; } = 1;
    public List<WarCampaign> Wars { get; set; } = [];
    public List<PeaceOffer> PeaceOffers { get; set; } = [];
    public int NextWarId { get; set; } = 1;
    public int NextPeaceId { get; set; } = 1;
    public string? Ending { get; set; }
    public int NextId { get; set; } = 1;
    internal int LogisticsRevision;
    internal Dictionary<int, SupplyMap> LogisticsCache = [];
    public void InvalidateLogistics() { LogisticsRevision++; LogisticsCache.Clear(); }
    public Faction Player => Factions[0];
    public string Season => (Turn % 4) switch { 1 => "Wiosna", 2 => "Lato", 3 => "Jesień", _ => "Zima" };
    public int Year => -2400 + (Turn - 1) * 15;
    public int Next(int max)
    {
        RandomState ^= RandomState << 13; RandomState ^= RandomState >> 17; RandomState ^= RandomState << 5;
        return (int)(RandomState % (uint)max);
    }
    public Relation Relation(int a, int b) => Relations.First(r => r.A == Math.Min(a,b) && r.B == Math.Max(a,b));
    public void Log(string text, string category = "świat") => Chronicle.Add(new() { Turn = Turn, Text = text, Category = category });
}
public readonly record struct Yields(double Food, double Production, double Gold, double Science, double Culture);
public readonly record struct Result(bool Success, string Message)
{
    public static Result Ok(string message) => new(true, message);
    public static Result Fail(string message) => new(false, message);
}
public sealed record Technology(string Id, string Name, int Cost, string[] Requires, string Description);
public sealed record Project(string Id, string Name, int Cost, string? Technology, string Description, UnitKind? Unit = null);
public sealed record Choice(string Text, string Consequence, int Gold = 0, int Stability = 0, int Commons = 0, int Nobles = 0, int Scholars = 0, int Prestige = 0, string? Flag = null, int Delay = 0, string? FutureText = null, int FutureGold = 0, int FutureStability = 0, int FutureCommons = 0, int FutureNobles = 0, int FutureScholars = 0, int FuturePrestige = 0);
public sealed record NarrativeEvent(string Id, string Speaker, string Role, string Title, string Body, Choice[] Choices);
public sealed record RoadPlan(IReadOnlyList<int> Path, int NewTiles, int Cost, string? Error = null);
public sealed record EconomyReport(double CityIncome, double TradeIncome, double ArmyCost, double AdministrationCost, double RoadCost)
{
    public double Net => CityIncome + TradeIncome - ArmyCost - AdministrationCost - RoadCost;
}
public sealed record CombatPreview(bool Valid, string Reason, string Attacker, string Target,
    double AttackStrength = 0, double DefenseStrength = 0, double TargetHealth = 0,
    double DamageToTarget = 0, double DamageToAttacker = 0, bool CanCapture = false,
    bool Ranged = false, IReadOnlyList<string>? Modifiers = null);
public sealed record MigrationRecord(int Turn, int Owner, int Source, int Destination, int People);
public sealed record FoodShipment(int Source, int Destination, double Amount);
public sealed record FoodPlan(Dictionary<int, double> Raw, Dictionary<int, double> Imports,
    Dictionary<int, double> Exports, IReadOnlyList<FoodShipment> Shipments)
{
    public double Balance(int cityId) => Raw[cityId] + Imports[cityId] - Exports[cityId];
}
public sealed record ProvinceStatus(int Housing, int Sanitation, IReadOnlyList<string> Goods,
    double FoodBalance, double Imports, double Exports, int ProsperityTarget, int Pressure);
public sealed record LoyaltyReport(int Target, int Change, bool Garrison, IReadOnlyList<string> Factors);
public sealed record WarDeclaration(bool Allowed, string Reason, int CommonsCost, int StabilityCost, int ForeignTrustCost);
public sealed record WarScoreReport(int Score, IReadOnlyList<string> Factors);
public sealed record PeacePreview(bool Valid, string Error, int Acceptance, IReadOnlyList<string> Factors);
