namespace Crown.Core;

/// <summary>Safe conduct for consignments involving the player; never military access.</summary>
public static class TransitCouncil
{
    public const int Duration=12;
    public const int PassagePrice=60;
    public static bool IsRequest(DemandKind kind)=>kind is DemandKind.Transit or DemandKind.PaidTransit;
    // Accepted embassies are the treaty ledger. No duplicate permit state can drift from the reply.
    public static int Until(GameState s,int host)=>s.Demands
        .Where(d=>d.From==0&&d.To==host&&IsRequest(d.Kind)&&d.Status==DemandStatus.Accepted)
        .Select(d=>d.ResolvedTurn!.Value+Duration).DefaultIfEmpty(0).Max();

    public static string? RequestError(GameState s,int host)
    {
        if(s.Factions[host].TradePolicy==TradePolicy.Embargo||s.Player.TradePolicy==TradePolicy.Embargo)
            return "Najpierw trzeba znieść embargo. Zezwolenie go nie omija.";
        if(Until(s,host)>s.Turn)return $"Zgoda już obowiązuje; wygasa na początku tury {Until(s,host)}.";
        return null;
    }

    public static IReadOnlyList<string> Notices(GameState s)=>s.Factions.Skip(1).Select(f=>(Faction:f,Until:Until(s,f.Id)))
        .Where(p=>p.Until>0&&(p.Until==s.Turn||p.Until==s.Turn+2))
        .Select(p=>p.Until==s.Turn?$"{p.Faction.Name}: zgoda na przejazd wygasła. Sprawdź granice przed kolejnym rozliczeniem rynku.":
            $"{p.Faction.Name}: zgoda na przejazd wygaśnie za dwie tury, przed rynkiem T{p.Until}. Zaplanuj objazd lub nowe poselstwo po wygaśnięciu.").ToArray();

    internal static bool CanCross(GameState s,TradeContract contract,Tile tile,bool previewOpenBorders=false)
    {
        if(!tile.Passable)return false;
        int host=tile.Owner;
        if(host<0||host==contract.Buyer||host==contract.Seller)return true;
        if(s.Factions[host].TradePolicy==TradePolicy.Embargo||s.Relation(host,contract.Buyer).War||s.Relation(host,contract.Seller).War)return false;
        if(s.Relation(host,contract.Buyer).Trade&&s.Relation(host,contract.Seller).Trade)return true;
        return (contract.Buyer==0||contract.Seller==0)&&(previewOpenBorders||Until(s,host)>s.Turn);
    }

    /// <summary>Closed gates on one safe potential corridor. It may require several separate permissions.</summary>
    public static IReadOnlyList<int> ClosedBorders(GameState s,TradeContract contract,int? start=null,Unit? escort=null)
    {
        if(Caravans.Suspension(s,contract)!=null||Caravans.Route(s,contract,start,escort).Count>0)return [];
        return Caravans.PreviewTransitRoute(s,contract,start,escort)
            .Select(id=>s.Tiles[id]).Where(t=>!CanCross(s,contract,t)).Select(t=>t.Owner).Distinct().ToArray();
    }

    public static string Blockage(GameState s,TradeContract contract,int? start=null,Unit? escort=null)
    {
        var hosts=ClosedBorders(s,contract,start,escort);
        return hosts.Count==0?"Brak bezpiecznej trasy. Sprawdź wojnę, embargo, wojska i teren.":
            "Brak zgody na przejazd: "+string.Join(", ",hosts.Select(id=>s.Factions[id].Name))+". Potencjalny korytarz wymaga zgody każdego z tych państw.";
    }
}
