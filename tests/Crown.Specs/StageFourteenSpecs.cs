using Crown.Core;
using System.Text.Json.Nodes;

internal static class StageFourteenSpecs
{
    private static void Assert(bool value, string message) { if (!value) throw new Exception(message); }
    public static GameState Fresh()
    {
        var s = World.Create(); Narrative.Resolve(s, 0); s.Player.Technologies.Add("currency");
        foreach (var f in s.Factions) f.Gold = 3000;
        foreach (var c in s.Cities) { c.ProductionPaused = true; c.Project = "treasury"; }
        s.Units.RemoveAll(u => u.Owner != 0);
        foreach (var r in s.Relations) { r.Trade = true; r.Trust = 40; }
        return s;
    }
    private static SovereignLoan Borrow(GameState s, int creditor = 1, int principal = 200)
    { Assert(Engine.BorrowGold(s, creditor, principal).Success, "Borrowing failed."); return s.Loans.Last(); }
    public static GameState Ready()
    {
        var s = Fresh(); var loan = Borrow(s); s.Turn = loan.DueTurn; TreasuryLoans.Tick(s); Assert(DebtCouncil.Schedule(s), "Debt council missing."); return s;
    }
    private static void Atomic(GameState s, Func<Result> command)
    { string before = SaveGame.Serialize(s); Assert(!command().Success && SaveGame.Serialize(s) == before, "Rejected financial action changed the state."); }
    private static void Reject(GameState s)
    { bool rejected = false; try { SaveGame.Deserialize(SaveGame.Serialize(s)); } catch (InvalidDataException) { rejected = true; } Assert(rejected, "Invalid debt accepted."); }
    private static void Step(GameState s)
    {
        if (s.PendingEvent != null) { var e = ProvincialEvents.Current(s); Assert(Narrative.Resolve(s, Array.FindIndex(e.Choices, c => s.Player.Gold + c.Gold >= 0)).Success, "Council failed."); }
        Assert(Engine.EndTurn(s).Success, "Turn failed.");
    }
    public static void Run(Action<string, Action> test)
    {
        test("Loans transfer real principal and preserve combined treasury balances", () =>
        {
            foreach (int principal in TreasuryLoans.Principals)
            {
                var s = Fresh(); double combined = s.Player.Gold + s.Factions[1].Gold; var loan = Borrow(s, 1, principal);
                Assert(s.Player.Gold == 3000 + principal && s.Factions[1].Gold == 3000 - principal && s.Player.Gold + s.Factions[1].Gold == combined && loan.DueTurn == 13 && TreasuryLoans.Amount(loan) == principal * 1.2 && TreasuryLoans.Outstanding(s) == TreasuryLoans.Amount(loan), "Loan created money or incorrect terms.");
                SaveGame.Deserialize(SaveGame.Serialize(s));
            }
        });
        test("Credit requirements reject missing technology, war, treaty, trust and lender funds atomically", () =>
        {
            var s = Fresh(); s.Player.Technologies.Remove("currency"); Atomic(s, () => Engine.BorrowGold(s, 1, 100)); s.Player.Technologies.Add("currency");
            s.Relation(0, 1).War = true; Atomic(s, () => Engine.BorrowGold(s, 1, 100)); s.Relation(0, 1).War = false;
            s.Relation(0, 1).Trade = false; Atomic(s, () => Engine.BorrowGold(s, 1, 100)); s.Relation(0, 1).Trade = true;
            s.Relation(0, 1).Trust = 9; Atomic(s, () => Engine.BorrowGold(s, 1, 100)); s.Relation(0, 1).Trust = 10;
            s.Factions[1].Gold = 99; Atomic(s, () => Engine.BorrowGold(s, 1, 100)); s.Factions[1].Gold = 100; Assert(Engine.BorrowGold(s, 1, 100).Success && s.Factions[1].Gold == 0, "Exact funds were refused.");
        });
        test("Invalid creditors, unsupported principals, fallen states and completed campaigns cannot borrow", () =>
        {
            var s = Fresh(); foreach (int creditor in new[] { -1, 0, 4 }) Atomic(s, () => Engine.BorrowGold(s, creditor, 100));
            foreach (int principal in new[] { 0, -100, 101, int.MaxValue }) Atomic(s, () => Engine.BorrowGold(s, 1, principal));
            s.Cities.RemoveAll(c => c.Owner == 1); Atomic(s, () => Engine.BorrowGold(s, 1, 100)); s.Ending = "Koniec"; Atomic(s, () => Engine.BorrowGold(s, 2, 100));
        });
        test("Only two obligations and one per creditor can remain open", () =>
        {
            var s = Fresh(); Borrow(s); Atomic(s, () => Engine.BorrowGold(s, 1, 100)); Borrow(s, 2); Atomic(s, () => Engine.BorrowGold(s, 3, 100));
            Assert(TreasuryLoans.Outstanding(s) == 480, "Debt total missed an obligation.");
        });
        test("Early repayment charges full agreed interest exactly once and transfers it to the creditor", () =>
        {
            var s = Fresh(); var loan = Borrow(s); double player = s.Player.Gold, lender = s.Factions[1].Gold, total = player + lender; int trust = s.Relation(0, 1).Trust;
            Assert(Engine.RepayLoan(s, loan.Id).Success && s.Player.Gold == player - 240 && s.Factions[1].Gold == lender + 240 && s.Player.Gold + s.Factions[1].Gold == total && loan.Paid == 240 && loan.Status == LoanStatus.Repaid && loan.ResolvedTurn == s.Turn && s.Relation(0, 1).Trust == trust + 5 && TreasuryLoans.Outstanding(s) == 0, "Repayment charged twice or lost cash.");
            Atomic(s, () => Engine.RepayLoan(s, loan.Id)); SaveGame.Deserialize(SaveGame.Serialize(s));
        });
        test("Repayment refuses insufficient gold, unknown ids and an ended campaign atomically", () =>
        {
            var s = Fresh(); var loan = Borrow(s); s.Player.Gold = 239.5; Atomic(s, () => Engine.RepayLoan(s, loan.Id)); Atomic(s, () => Engine.RepayLoan(s, -1));
            s.Player.Gold = 240; Assert(Engine.RepayLoan(s, loan.Id).Success && s.Player.Gold == 0, "Exact repayment funds refused.");
            s = Fresh(); loan = Borrow(s); s.Ending = "Koniec"; Atomic(s, () => Engine.RepayLoan(s, loan.Id));
        });
        test("Early settlement does not bypass the eight-turn funding interval", () =>
        {
            var s = Fresh(); var loan = Borrow(s); Engine.RepayLoan(s, loan.Id); s.Turn = 8; Atomic(s, () => Engine.BorrowGold(s, 1, 200));
            s.Turn = 9; Borrow(s); SaveGame.Deserialize(SaveGame.Serialize(s));
        });
        test("Maturity warnings and due transitions are idempotent without seizing treasury", () =>
        {
            var s = Fresh(); var loan = Borrow(s); s.Turn = 11; double gold = s.Player.Gold; TreasuryLoans.Tick(s); TreasuryLoans.Tick(s);
            Assert(s.Chronicle.Count(e => e.Text.StartsWith("Za dwie tury zapada pożyczka")) == 1 && loan.Status == LoanStatus.Active && s.Player.Gold == gold, "Warning repeated or charged repayment.");
            s.Turn = 12; TreasuryLoans.Tick(s); Assert(loan.Status == LoanStatus.Active, "Loan matured early."); s.Turn = 13; TreasuryLoans.Tick(s); TreasuryLoans.Tick(s);
            Assert(loan.Status == LoanStatus.AwaitingCouncil && s.Player.Gold == gold && s.Chronicle.Count(e => e.Text.Contains("nadszedł termin spłaty")) == 1, "Maturity duplicated or seized gold.");
        });
        test("Real end-turn processing schedules the due debt and the council blocks further time", () =>
        {
            var s = Fresh(); Borrow(s); for (int i = 0; i < 12; i++) Step(s);
            Assert(s.Turn == 13 && s.PendingEvent == DebtCouncil.Event.Id && s.PendingLoanId == 1, "Turn processing missed maturity."); Atomic(s, () => Engine.EndTurn(s)); SaveGame.Deserialize(SaveGame.Serialize(s));
        });
        test("An existing council keeps precedence while overdue obligations wait without invented interest", () =>
        {
            var s = Fresh(); var loan = Borrow(s); s.PendingEvent = "grain"; s.Turn = 13; TreasuryLoans.Tick(s); string before = SaveGame.Serialize(s);
            Assert(!DebtCouncil.Schedule(s) && !DebtCouncil.Present(s, loan.Id).Success && SaveGame.Serialize(s) == before, "Debt overwrote another council.");
            Narrative.Resolve(s, 2); Narrative.Schedule(s); Assert(s.PendingLoanId == loan.Id && TreasuryLoans.Amount(loan) == 240, "Deferred hearing invented extra charges.");
        });
        test("Due hearing uses the named creditor and actual terms without mutating the state", () =>
        {
            var s = Ready(); string before = SaveGame.Serialize(s); var current = ProvincialEvents.Current(s);
            Assert(current.Body.Contains(s.Factions[1].Name) && current.Body.Contains("240") && current.Choices[0].Gold == -240 && current.Choices[1].Gold == -20 && SaveGame.Serialize(s) == before, "Debt dialogue showed stale terms or altered the loan.");
        });
        test("Council repayment transfers the quoted amount once and increases trust", () =>
        {
            var s = Ready(); double player = s.Player.Gold, lender = s.Factions[1].Gold; int trust = s.Relation(0, 1).Trust;
            Assert(Narrative.Resolve(s, 0).Success && s.Player.Gold == player - 240 && s.Factions[1].Gold == lender + 240 && s.Relation(0, 1).Trust == trust + 5 && s.Loans[0].Paid == 240 && s.PendingLoanId == null && s.PendingEvent == null, "Council repayment duplicated or lost the transfer.");
            Atomic(s, () => Narrative.Resolve(s, 0)); SaveGame.Deserialize(SaveGame.Serialize(s));
        });
        test("One extension pays a real fee and adds fixed interest with a new six-turn term", () =>
        {
            var s = Ready(); double player = s.Player.Gold, lender = s.Factions[1].Gold; int trust = s.Relation(0, 1).Trust;
            Assert(Narrative.Resolve(s, 1).Success, "Extension refused."); var loan = s.Loans[0];
            Assert(loan.ExtendedTurn == 13 && loan.DueTurn == 19 && loan.Status == LoanStatus.Active && loan.Paid == 20 && s.Player.Gold == player - 20 && s.Factions[1].Gold == lender + 20 && TreasuryLoans.Amount(loan) == 260 && s.Relation(0, 1).Trust == trust - 5, "Extension invented money or used incorrect terms.");
            SaveGame.Deserialize(SaveGame.Serialize(s));
        });
        test("A second extension is refused atomically and the remaining debt still has two outcomes", () =>
        {
            var s = Ready(); Narrative.Resolve(s, 1); s.Turn = 19; TreasuryLoans.Tick(s); DebtCouncil.Schedule(s);
            Atomic(s, () => Narrative.Resolve(s, 1)); Assert(DebtCouncil.ChoiceError(s, 0) == null && DebtCouncil.ChoiceError(s, 2) == null, "Second maturity has no valid decision.");
            double lender = s.Factions[1].Gold; Narrative.Resolve(s, 0); Assert(s.Loans[0].Paid == 280 && s.Factions[1].Gold == lender + 260, "Extended loan repaid the fee twice."); SaveGame.Deserialize(SaveGame.Serialize(s));
        });
        test("War and a fallen creditor stop extensions but never erase principal or block repayment", () =>
        {
            var s = Ready(); s.Relation(0, 1).War = true; WarCouncil.EnsureActive(s); Atomic(s, () => Narrative.Resolve(s, 1)); Assert(TreasuryLoans.Outstanding(s) == 240 && Narrative.Resolve(s, 0).Success, "War erased debt or prevented payment.");
            s = Ready(); s.Cities.RemoveAll(c => c.Owner == 1); Atomic(s, () => Narrative.Resolve(s, 1)); Assert(Engine.RepayLoan(s, 1).Success && s.PendingLoanId == null && s.PendingEvent == null, "Fallen creditor invalidated its existing claim.");
        });
        test("Empty treasury has a default option with political, diplomatic and credit consequences", () =>
        {
            var s = Ready(); Borrow(s, 2, 100); s.Player.Gold = 0; var f = s.Player; int stability = f.Stability, commons = f.Commons, nobles = f.Nobles; double prestige = f.Prestige; int trust = s.Relation(0, 1).Trust, grievance = DiplomaticCouncil.Grievances(s, 1, 0); double lender = s.Factions[1].Gold;
            Atomic(s, () => Narrative.Resolve(s, 0)); Atomic(s, () => Narrative.Resolve(s, 1)); Assert(Narrative.Resolve(s, 2).Success, "Empty treasury softlocked default.");
            Assert(f.Gold == 0 && s.Factions[1].Gold == lender && f.Stability == stability - 12 && f.Commons == commons - 4 && f.Nobles == nobles - 8 && f.Prestige == prestige - 10 && s.Relation(0, 1).Trust == trust - 30 && !s.Relation(0, 1).Trade && DiplomaticCouncil.Grievances(s, 1, 0) == grievance + 35 && TreasuryLoans.FrozenUntil(s) == 37 && s.Loans[0].Status == LoanStatus.Defaulted && TreasuryLoans.Outstanding(s) == 120, "Default lost its consequences or erased another loan.");
            SaveGame.Deserialize(SaveGame.Serialize(s));
        });
        test("Credit freeze applies to every lender and expires at its exclusive boundary", () =>
        {
            var s = Ready(); Narrative.Resolve(s, 2); s.Turn = 36; Atomic(s, () => Engine.BorrowGold(s, 2, 100)); s.Turn = 37;
            Borrow(s, 2, 100); SaveGame.Deserialize(SaveGame.Serialize(s));
        });
        test("Debt defaults stop treaty-dependent paid cargo without refunding its purchase", () =>
        {
            var s = Fresh(); s.Units.Clear(); foreach (var t in s.Tiles) { t.Terrain = Terrain.Grassland; t.Owner = -1; t.Explored = true; }
            var buyer = s.Cities[0]; var seller = s.Cities[1]; seller.Tile = 15 * s.Width + 21;
            foreach (var c in s.Cities) s.Tiles[c.Tile].Owner = c.Owner;
            foreach (var f in s.Factions) f.Technologies.UnionWith(["pottery", "trade", "currency"]);
            buyer.Buildings.Add("market"); seller.Buildings.Add("market"); seller.MarketStocks[TradeGood.Salt] = 50; s.InvalidateLogistics();
            Assert(Engine.SignTradeContract(s, buyer.Id, seller.Id, TradeGood.Salt, 4).Success, "Cargo contract failed."); Markets.Tick(s); Engine.CancelTradeContract(s, 1);
            var cargo = s.Caravans.Single(); double payment = cargo.Payment; Assert(payment > 0 && Caravans.Active(cargo), "Cargo was not really purchased.");
            var loan = Borrow(s); s.Turn = loan.DueTurn; TreasuryLoans.Tick(s); DebtCouncil.Schedule(s); double gold = s.Player.Gold;
            Assert(Narrative.Resolve(s, 2).Success && !s.Relation(0, 1).Trade && s.Player.Gold == gold, "Default refunded trade or kept the treaty.");
            s.Turn++; Markets.Tick(s); Assert(cargo.Payment == payment && s.Caravans[0].Status == CaravanStatus.Waiting && s.Caravans[0].BlockedTurns == 1 && s.Player.Gold == gold, "Paid cargo ignored a terminated treaty or received a refund.");
            s.Turn++; Markets.Tick(s); s.Turn++; Markets.Tick(s); Assert(s.Caravans[0].Status == CaravanStatus.Lost && s.Player.Gold == gold, "Third blockade failed to lose cargo without repayment."); SaveGame.Deserialize(SaveGame.Serialize(s));
        });
        test("Outstanding debts stay distinct from recurring income and pure economic forecasts", () =>
        {
            var s = Fresh(); var beforeIncome = Engine.Income(s, 0); Borrow(s); var afterIncome = Engine.Income(s, 0); string before = SaveGame.Serialize(s);
            Markets.Plan(s); Engine.Economy(s, 0); TreasuryLoans.Outstanding(s);
            Assert(beforeIncome == afterIncome && SaveGame.Serialize(s) == before, "Debt was charged as recurring interest or a forecast changed it.");
        });
        test("Two matured loans are presented separately and cannot overwrite each other", () =>
        {
            var s = Fresh(); Borrow(s); Borrow(s, 2); s.Turn = 13; TreasuryLoans.Tick(s); DebtCouncil.Schedule(s);
            Atomic(s, () => DebtCouncil.Present(s, 2)); Assert(s.PendingLoanId == 1, "Second loan replaced the first hearing.");
            Narrative.Resolve(s, 0); DebtCouncil.Schedule(s); Assert(s.PendingLoanId == 2 && Narrative.Resolve(s, 0).Success && s.Loans.All(l => l.Status == LoanStatus.Repaid), "One hearing settled multiple loans.");
        });
        test("Borrowed gold creates no free victory points and repayment preserves net wealth", () =>
        {
            var s = Fresh(); double borrowerScore = Engine.Score(s, 0), lenderScore = Engine.Score(s, 1), total = s.Factions.Sum(f => TreasuryLoans.NetWealth(s, f.Id));
            var loan = Borrow(s);
            Assert(Math.Abs(Engine.Score(s, 0) - (borrowerScore - 1.2)) < 1e-8 && Math.Abs(Engine.Score(s, 1) - (lenderScore + 1.2)) < 1e-8 && s.Factions.Sum(f => TreasuryLoans.NetWealth(s, f.Id)) == total, "Principal created points or a receivable was lost.");
            double borrowerWealth = TreasuryLoans.NetWealth(s, 0), lenderWealth = TreasuryLoans.NetWealth(s, 1); Engine.RepayLoan(s, loan.Id);
            Assert(TreasuryLoans.NetWealth(s, 0) == borrowerWealth && TreasuryLoans.NetWealth(s, 1) == lenderWealth, "Repayment charged wealth twice.");
        });
        test("Malformed and finished council requests cannot mutate finance", () =>
        {
            var s = Ready(); Atomic(s, () => DebtCouncil.Present(s, 99)); Atomic(s, () => Narrative.Resolve(s, -1)); Atomic(s, () => Narrative.Resolve(s, 3));
            s.Ending = "Koniec"; Atomic(s, () => DebtCouncil.Present(s, 1)); Atomic(s, () => Narrative.Resolve(s, 2));
        });
        test("Ongoing, extended and due loans continue identically across save and reload", () =>
        {
            foreach (int stage in new[] { 0, 1, 2 })
            {
                var s = stage == 0 ? Fresh() : Ready(); if (stage == 0) Borrow(s); if (stage == 1) Narrative.Resolve(s, 1);
                var loaded = SaveGame.Deserialize(SaveGame.Serialize(s)); Assert(SaveGame.Serialize(s) == SaveGame.Serialize(loaded), "Debt roundtrip changed state.");
                if (stage == 2) { Narrative.Resolve(s, 2); Narrative.Resolve(loaded, 2); } else { Step(s); Step(loaded); }
                Assert(SaveGame.Serialize(s) == SaveGame.Serialize(loaded), "Reloaded finances diverged.");
            }
        });
        test("V13 migration keeps military preparation, production and audits without inventing debt", () =>
        {
            var s = Fresh(); s.Version = 13; var u = s.Units.First(); u.Order = MilitaryOrder.Fortify; u.FortificationTurns = 1; var c = s.Cities[0]; c.ProductionQueue.Add(new() { Project = "scout", Repeat = true }); c.HiddenGold = 18;
            string legacy = SaveGame.Serialize(s); var loaded = SaveGame.Deserialize(legacy);
            Assert(loaded.Version == 14 && loaded.Loans.Count == 0 && loaded.NextLoanId == 1 && loaded.PendingLoanId == null && loaded.Units[0].FortificationTurns == 1 && loaded.Cities[0].HiddenGold == 18 && loaded.Cities[0].ProductionQueue[0].Repeat, "Migration invented debt or erased ongoing state.");
            string path = Path.Combine(Environment.CurrentDirectory, "artifacts", "migration-v13-test.crown");
            try { File.WriteAllText(path, legacy); SaveGame.Write(loaded, path); Assert(File.ReadAllText(path + ".v13.bak") == legacy && SaveGame.Read(path).Version == 14, "V13 backup missing."); }
            finally { File.Delete(path); File.Delete(path + ".v13.bak"); }
        });
        test("Loan validation rejects unknown terms, impossible dates, fake payments and bad contexts", () =>
        {
            var s = Ready(); var loan = s.Loans[0]; loan.Principal = 101; Reject(s); loan.Principal = 200; loan.Creditor = 0; Reject(s); loan.Creditor = 1;
            loan.DueTurn++; Reject(s); loan.DueTurn--; loan.Paid = 1; Reject(s); loan.Paid = 0; loan.Status = LoanStatus.Active; Reject(s); loan.Status = LoanStatus.AwaitingCouncil;
            loan.Status = (LoanStatus)99; Reject(s); loan.Status = LoanStatus.AwaitingCouncil; loan.ResolvedTurn = s.Turn; Reject(s); loan.ResolvedTurn = null;
            s.PendingLoanId = 99; Reject(s); s.PendingLoanId = 1; s.NextLoanId = 1; Reject(s); s.NextLoanId = 2;
            s.PendingEvent = null; Reject(s); s.PendingLoanId = null; loan.ExtendedTurn = 2; Reject(s);
        });
        test("Loan validation rejects duplicate records, null registries and unsettled terminal amounts", () =>
        {
            var s = Fresh(); Borrow(s); var json = JsonNode.Parse(SaveGame.Serialize(s))!; json["Loans"]!.AsArray().Add(json["Loans"]![0]!.DeepClone());
            bool failed = false; try { SaveGame.Deserialize(json.ToJsonString()); } catch (InvalidDataException) { failed = true; } Assert(failed, "Duplicate loan id accepted.");
            s.Loans[0].Status = LoanStatus.Repaid; s.Loans[0].ResolvedTurn = 1; Reject(s); s.Loans = null!; Reject(s);
        });
    }
}
