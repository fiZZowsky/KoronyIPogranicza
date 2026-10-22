namespace Crown.Core;

public static class DiplomaticCouncil
{
    public static int Grievances(GameState s, int owner, int other)
    {
        var r = s.Relation(owner, other);
        return owner == r.A ? r.GrievanceA : r.GrievanceB;
    }

    public static void AddGrievance(GameState s, int victim, int offender, int amount)
    {
        var r = s.Relation(victim, offender);
        if (victim == r.A) r.GrievanceA = Math.Clamp(r.GrievanceA + amount, 0, 100);
        else r.GrievanceB = Math.Clamp(r.GrievanceB + amount, 0, 100);
    }

    public static string WarName(WarReason reason) => reason switch
    { WarReason.Redress => "Zadośćuczynienie", WarReason.Liberation => "Wyzwolenie własnych miast", _ => "Wojna podboju" };
    public static string DemandName(DemandKind kind) => kind == DemandKind.Compensation ? "Odszkodowanie · 80 złota" : "Pakt o nieagresji · 15 tur";

    public static WarDeclaration PreviewWar(GameState s, int other, WarReason reason)
    {
        WarDeclaration Fail(string text) => new(false, text, 0, 0, 0);
        if (s.Ending != null || other <= 0 || other >= s.Factions.Count || !Enum.IsDefined(reason) || !s.Cities.Any(c => c.Owner == other))
            return Fail("Nieprawidłowy adresat wypowiedzenia wojny.");
        var r = s.Relation(0, other);
        if (r.War) return Fail("Wojna już trwa.");
        if (r.TruceUntil > s.Turn) return Fail($"Rozejm obowiązuje do tury {r.TruceUntil}.");
        if (r.LastActionTurn == s.Turn) return Fail("Posłowie odbyli już rozmowę w tej turze.");
        if (reason == WarReason.Redress && Grievances(s, 0, other) < 30) return Fail("Potrzeba co najmniej 30 punktów krzywd.");
        if (reason == WarReason.Liberation && !s.Cities.Any(c => c.Owner == other && c.OriginalOwner == 0)) return Fail("Rozmówca nie zajmuje żadnego miasta założonego przez Marchie.");
        return reason == WarReason.Conquest ? new(true, "Podbój nie ma uzasadnienia. Inne państwa stracą zaufanie.", 8, 6, 12) :
            new(true, reason == WarReason.Redress ? "Wykorzystasz 30 punktów krzywd." : "Prawo powrotu do zajętych miast uzasadnia wojnę.", 2, 1, 0);
    }

    // A public forecast uses the same calculation as the eventual reply. No hidden random roll.
    public static int AcceptanceScore(GameState s, int other, DemandKind kind)
    {
        double own = Engine.Military(s, 0), foreign = Engine.Military(s, other);
        int leverage = (int)Math.Clamp((own - foreign) / Math.Max(20, foreign) * 35, -60, 60);
        return s.Relation(0, other).Trust + leverage + (int)s.Factions[other].WarWeariness / 2 +
            (kind == DemandKind.Compensation ? Grievances(s, 0, other) / 2 - 20 : 15) - (other == 3 ? 15 : 0);
    }

    public static string Forecast(GameState s, int other, DemandKind kind) =>
        kind == DemandKind.Compensation && s.Factions[other].Gold < 80 ? "Obecnie odmówią: skarbiec nie pokrywa 80 złota." :
        AcceptanceScore(s, other, kind) >= 15 ? "Obecnie przyjęliby propozycję. Stan przy powrocie posłów rozstrzygnie odpowiedź." :
        "Obecnie odmówiliby. Wpływ mają relacje, siła armii, krzywdy i wyczerpanie wojną.";

    internal static void Cancel(GameState s, Relation r, string explanation)
    {
        foreach (var d in s.Demands.Where(d => d.Status == DemandStatus.Pending && d.From == r.A && d.To == r.B))
        {
            d.Status = DemandStatus.Cancelled; d.ResolvedTurn = s.Turn; d.Reply = explanation;
            s.Log($"{s.Factions[d.To].Name}: {explanation}", "poselstwo");
        }
    }

    public static void Tick(GameState s)
    {
        foreach (var d in s.Demands.Where(d => d.Status == DemandStatus.Pending && d.DueTurn <= s.Turn).OrderBy(d => d.Id))
        {
            var r = s.Relation(d.From, d.To);
            if (r.War || !s.Cities.Any(c => c.Owner == d.To) || !s.Cities.Any(c => c.Owner == d.From))
            { Cancel(s, r, "Poselstwo przerwano z powodu wojny lub upadku państwa."); continue; }
            bool accept = AcceptanceScore(s, d.To, d.Kind) >= 15 && (d.Kind != DemandKind.Compensation || s.Factions[d.To].Gold >= 80);
            d.Status = accept ? DemandStatus.Accepted : DemandStatus.Rejected; d.ResolvedTurn = s.Turn;
            if (accept)
            {
                if (d.Kind == DemandKind.Compensation)
                {
                    s.Factions[d.To].Gold -= 80; s.Player.Gold += 80; AddGrievance(s, 0, d.To, -30);
                    d.Reply = "„Uznajemy wyrządzoną krzywdę. Przekazujemy 80 złota odszkodowania.”";
                }
                else
                { r.TruceUntil = Math.Max(r.TruceUntil, s.Turn + 15); d.Reply = $"„Przyjmujemy pakt. Nasze miecze pozostaną w pochwach do tury {r.TruceUntil}.”"; }
                r.Trust = Math.Min(100, r.Trust + 5);
            }
            else
            {
                r.Trust = Math.Max(-100, r.Trust - 5);
                d.Reply = "„Nie przyjmiemy tych warunków. Nasza rada uznaje żądanie za zbyt daleko idące.”";
            }
            s.Log($"{s.Factions[d.To].Leader}: {d.Reply} ({DemandName(d.Kind)})", "poselstwo");
        }
        if (s.Turn % 8 == 0) foreach (var r in s.Relations)
        { r.GrievanceA = Math.Max(0, r.GrievanceA - 1); r.GrievanceB = Math.Max(0, r.GrievanceB - 1); }
    }
}

public static partial class Engine
{
    public static Result SendDemand(GameState s, int other, DemandKind kind)
    {
        if (s.Ending != null || other <= 0 || other >= s.Factions.Count || !Enum.IsDefined(kind) || !s.Cities.Any(c => c.Owner == other)) return Result.Fail("Nieprawidłowy rozmówca.");
        var r = s.Relation(0, other);
        if (r.War || r.LastActionTurn == s.Turn) return Result.Fail("Poselstwo wymaga pokoju i wolnej rozmowy w tej turze.");
        if (s.Turn - r.LastDemandTurn < 10 || s.Demands.Any(d => d.To == other && d.Status == DemandStatus.Pending)) return Result.Fail("Kolejne poselstwo można wysłać po dziesięciu turach i powrocie poprzedniego.");
        if (kind == DemandKind.Compensation && DiplomaticCouncil.Grievances(s, 0, other) < 30) return Result.Fail("Żądanie odszkodowania wymaga 30 punktów krzywd.");
        if (kind == DemandKind.NonAggression && r.TruceUntil > s.Turn) return Result.Fail("Obowiązuje już pakt lub rozejm.");
        if (s.Player.Gold < 20) return Result.Fail("Podróż posłów kosztuje 20 złota.");
        s.Player.Gold -= 20; r.LastActionTurn = r.LastDemandTurn = s.Turn;
        s.Demands.Add(new() { Id = s.NextDemandId++, From = 0, To = other, Kind = kind, SentTurn = s.Turn, DueTurn = s.Turn + 2 });
        s.Log($"Posłowie do {s.Factions[other].Name}: {DiplomaticCouncil.DemandName(kind)}. Odpowiedź w turze {s.Turn + 2}.", "poselstwo");
        return Result.Ok("Wysłano posłów. Odpowiedź nadejdzie za dwie tury.");
    }

    public static Result DeclareWar(GameState s, int other, WarReason reason)
    {
        var preview = DiplomaticCouncil.PreviewWar(s, other, reason);
        if (!preview.Allowed) return Result.Fail(preview.Reason);
        var r = s.Relation(0, other);
        r.War = true; r.Trade = false; r.Trust = Math.Max(-100, r.Trust - 35); r.LastActionTurn = s.Turn;
        s.Player.Commons -= preview.CommonsCost; s.Player.Stability -= preview.StabilityCost; Clamp(s.Player);
        if (reason == WarReason.Redress) DiplomaticCouncil.AddGrievance(s, 0, other, -30);
        DiplomaticCouncil.AddGrievance(s, other, 0, reason == WarReason.Conquest ? 40 : 15);
        if (preview.ForeignTrustCost > 0) foreach (var neighbor in s.Factions.Skip(1).Where(f => f.Id != other))
        { var third = s.Relation(0, neighbor.Id); third.Trust = Math.Max(-100, third.Trust - preview.ForeignTrustCost); }
        DiplomaticCouncil.Cancel(s, r, "Wypowiedzenie wojny zakończyło negocjacje.");
        s.InvalidateLogistics();
        s.Log($"{DiplomaticCouncil.WarName(reason)} przeciw {s.Factions[other].Name}. Lud -{preview.CommonsCost}, stabilność -{preview.StabilityCost}, zaufanie pozostałych państw -{preview.ForeignTrustCost}.", "dyplomacja");
        return Result.Ok("Przekazano wypowiedzenie wojny.");
    }
}
