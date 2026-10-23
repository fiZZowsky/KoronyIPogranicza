namespace Crown.Core;

public enum Terrain { Ocean, Coast, Grassland, Forest, Hills, Mountain, Desert, Marsh, Tundra }
public enum Resource { None, Grain, Iron, Horses, Stone, Salt, Dyes }
public enum Improvement { None, Farm, Mine, TradingPost }
public enum UnitKind { Scout, Spearmen, Archer, Cavalry, Settler, Catapult }
public enum MilitaryOrder { None, Fortify, Recover }
public enum LoanStatus { Active, AwaitingCouncil, Repaid, Defaulted }
public enum SpyStatus { Available, Deployed, Captured, Lost }
public enum SpyMission { Reconnaissance, StealResearch, Sabotage }
public enum SpyOutcome { Active, Succeeded, Failed, Captured, Cancelled }
public enum CustodyDecision { Ransom, Exchange, Release, Intern }
public enum CityFocus { Balanced, Agriculture, Industry, Scholarship }
public enum Governance { Integrated, Autonomous, Military }
public enum WarReason { Conquest, Redress, Liberation }
public enum DemandKind { Compensation, NonAggression, Transit, PaidTransit }
public enum DemandStatus { Pending, Accepted, Rejected, Cancelled }
public enum PeaceStatus { Travelling, AwaitingPlayer, Accepted, Rejected, Cancelled, Expired }
public enum TradeGood { Iron, Salt, Dyes }
public enum TradePolicy { DomesticFirst, OpenExports, Embargo }
public enum ContractStatus { Active, Cancelled, Expired }
public enum CaravanStatus { Travelling, Waiting, Delivered, Lost }
public enum TradeCaseStatus { Pending, Compensated, Promised, Fulfilled, Broken, Declined, Voided }
public enum AuditStatus { Investigating, AwaitingCouncil, Prosecuted, Reformed, Settled, Cancelled }
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
    public List<ProductionOrder> ProductionQueue { get; set; } = [];
    public bool RepeatProject { get; set; }
    public bool ProductionPaused { get; set; }
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
    public Dictionary<TradeGood,double> MarketStocks { get; set; } = new() { [TradeGood.Iron]=0,[TradeGood.Salt]=0,[TradeGood.Dyes]=0 };
    public List<CommodityBalance> LastMarketLines { get; set; } = [];
    public int LastMarketTurn { get; set; }
    public double LastMarketTax { get; set; }
    public int Corruption { get; set; }
    public double HiddenGold { get; set; }
    public double LastRevenueLoss { get; set; }
    public int LastAdministrationTurn { get; set; }
    public int LastAuditTurn { get; set; } = -12;
    public int AuditProtectionUntil { get; set; }
    public int CounterIntelligenceUntil { get; set; }
}
public sealed class Unit
{
    public int Id { get; set; }
    public int Owner { get; set; }
    public int Tile { get; set; }
    public UnitKind Kind { get; set; }
    public string? ModelId { get; set; }
    public double Health { get; set; } = 100;
    public double Actions { get; set; } = 2;
    public int? Destination { get; set; }
    public int? EscortCaravanId { get; set; }
    public MilitaryOrder Order { get; set; }
    public int FortificationTurns { get; set; }
    public int Experience { get; set; }
    public int MaxActions => UnitModels.For(this)?.Moves ?? (Kind is UnitKind.Scout or UnitKind.Cavalry ? 3 : 2);
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
    public TradePolicy TradePolicy { get; set; }
    public int LastTradePolicyTurn { get; set; } = -10;
    public Dictionary<int, int> EpochChoices { get; set; } = [];
    public HashSet<string> SpaceMilestones { get; set; } = [];
    public int Era => Epochs.Current(this);
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
    public int Version { get; set; } = 17;
    public bool EpochCampaign { get; set; }
    public bool ContinueAfterVictory { get; set; }
    public double ClimatePressure { get; set; }
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
    public List<TradeContract> TradeContracts { get; set; } = [];
    public int NextContractId { get; set; } = 1;
    public int LastMarketTurn { get; set; }
    public string? Ending { get; set; }
    public List<Caravan> Caravans { get; set; } = [];
    public int NextCaravanId { get; set; } = 1;
    public List<TradeCase> TradeCases { get; set; } = [];
    public int? PendingTradeCaseId { get; set; }
    public List<AuditCase> Audits { get; set; } = [];
    public int NextAuditId { get; set; } = 1;
    public int? PendingAuditId { get; set; }
    public List<SovereignLoan> Loans { get; set; } = [];
    public int NextLoanId { get; set; } = 1;
    public int? PendingLoanId { get; set; }
    public List<SpyAgent> Spies { get; set; } = [];
    public List<SpyOperation> SpyOperations { get; set; } = [];
    public int NextSpyId { get; set; } = 1;
    public int NextSpyOperationId { get; set; } = 1;
    public int? PendingSpyOperationId { get; set; }
    public int NextId { get; set; } = 1;
    internal int LogisticsRevision;
    internal Dictionary<int, SupplyMap> LogisticsCache = [];
    public void InvalidateLogistics() { LogisticsRevision++; LogisticsCache.Clear(); }
    public Faction Player => Factions[0];
    public string Season => (Turn % 4) switch { 1 => "Wiosna", 2 => "Lato", 3 => "Jesień", _ => "Zima" };
    public int Year => EpochCampaign ? Epochs.WorldYear(this) : -2400 + (Turn - 1) * 15;
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
public sealed record EconomyReport(double CityIncome, double TradeIncome, double ArmyCost, double AdministrationCost, double RoadCost,double MarketIncome=0,double CommodityTradeBalance=0)
{
    public double Net => CityIncome + TradeIncome + MarketIncome - ArmyCost - AdministrationCost - RoadCost;
    public double NetWithCommodityTrade => Net+CommodityTradeBalance;
}
public sealed class TradeContract
{
    public int Id { get; set; }
    public int Buyer { get; set; }
    public int Seller { get; set; }
    public int BuyerCity { get; set; }
    public int SellerCity { get; set; }
    public TradeGood Good { get; set; }
    public int Quantity { get; set; }
    public int StartedTurn { get; set; }
    public int ExpiresTurn { get; set; }
    public ContractStatus Status { get; set; }
    public int LastExecutedTurn { get; set; }
    // Legacy save field: purchased quantity dispatched in the last market phase, not arrivals.
    public double LastDelivered { get; set; }
    public double LastPrice { get; set; }
    public double LastPayment { get; set; }
    public string Message { get; set; } = "Oczekuje na pierwszą dostawę.";
}
public sealed record CommodityBalance(TradeGood Good,double Opening,double Produced,double DomesticIn,double DomesticOut,
    double ForeignIn,double ForeignOut,double Consumed,double Demand,double Lost,double Closing,double Price)
{
    public double Coverage => Demand<=0?1:Math.Clamp(Consumed/Demand,0,1);
}
public sealed record CommodityShipment(int From,int To,TradeGood Good,double Quantity);
public sealed record TradeExecution(int ContractId,double Quantity,double Price,double Payment,string Message);
public sealed record MarketPlan(Dictionary<int,List<CommodityBalance>> Cities,IReadOnlyList<CommodityShipment> Shipments,
    IReadOnlyList<TradeExecution> Trades,Dictionary<int,double> Cash,IReadOnlyList<Caravan> Caravans,IReadOnlyList<EscortMovement> EscortMoves);
public sealed record EscortMovement(int UnitId,int CaravanId,IReadOnlyList<int> Path,string? ReleaseReason);
public sealed record CaravanProjection(List<Caravan> Caravans,IReadOnlyList<EscortMovement> EscortMoves);
public sealed class Caravan
{
    public int Id { get; set; }
    public int ContractId { get; set; }
    public int Tile { get; set; }
    public int DepartedTurn { get; set; }
    public int LastMovedTurn { get; set; }
    public int BlockedTurns { get; set; }
    public int? ResolvedTurn { get; set; }
    public double Quantity { get; set; }
    public double Price { get; set; }
    public double Payment { get; set; }
    public CaravanStatus Status { get; set; }
    public string Message { get; set; } = "Załadowano towar. Wyruszy w następnej turze.";
}
public sealed class TradeCase
{
    // The lost caravan is also the unique identifier of its petition.
    public int CaravanId { get; set; }
    public int OpenedTurn { get; set; }
    public TradeCaseStatus Status { get; set; }
    public int? Decision { get; set; }
    public int? DecidedTurn { get; set; }
    public int? DeadlineTurn { get; set; }
    public int? AfterMarketTurn { get; set; }
    public int? ResolvedTurn { get; set; }
    public List<int> CreditedCaravans { get; set; } = [];
}
public sealed class AuditCase
{
    public int Id { get; set; }
    public int CityId { get; set; }
    public int StartedTurn { get; set; }
    public int DueTurn { get; set; }
    public int? CompletedTurn { get; set; }
    public int? ResolvedTurn { get; set; }
    public AuditStatus Status { get; set; }
    public double AssessedGold { get; set; }
    public int RecoveredGold { get; set; }
}
public sealed class ProductionOrder
{
    public string Project { get; set; } = "treasury";
    public bool Repeat { get; set; }
}
public sealed class SovereignLoan
{
    public int Id { get; set; }
    public int Creditor { get; set; }
    public int Principal { get; set; }
    public int StartedTurn { get; set; }
    public int DueTurn { get; set; }
    public int? ExtendedTurn { get; set; }
    public int? ResolvedTurn { get; set; }
    public LoanStatus Status { get; set; }
    public int Paid { get; set; }
}
public sealed class SpyAgent
{
    public int Id { get; set; }
    public int Owner { get; set; }
    public string Name { get; set; } = "";
    public int Experience { get; set; }
    public SpyStatus Status { get; set; }
}
public sealed class SpyOperation
{
    public int Id { get; set; }
    public int AgentId { get; set; }
    public int Owner { get; set; }
    public int CityId { get; set; }
    public int TargetOwner { get; set; }
    public SpyMission Mission { get; set; }
    public int StartedTurn { get; set; }
    public int DueTurn { get; set; }
    public int? ResolvedTurn { get; set; }
    public SpyOutcome Outcome { get; set; }
    public int SuccessChance { get; set; }
    public int DetectionChance { get; set; }
    public int? SuccessRoll { get; set; }
    public int? DetectionRoll { get; set; }
    public bool Detected { get; set; }
    public string? ResearchId { get; set; }
    public double ResultValue { get; set; }
    public string Report { get; set; } = "";
    public int? CaptureChoice { get; set; }
    public int? CaptureDecisionTurn { get; set; }
    public CustodyDecision? CustodyChoice { get; set; }
    public int? CustodyTurn { get; set; }
    public int? InternedUntil { get; set; }
    public int? ExchangeOperationId { get; set; }
}
public sealed record SpyForecast(int SuccessChance, int DetectionChance, int Defense, string? ResearchId);
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
