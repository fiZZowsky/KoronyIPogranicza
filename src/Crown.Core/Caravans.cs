namespace Crown.Core;

/// <summary>Deterministic civilian transport. Forecasts copy manifests and never spend or move live state.</summary>
public static class Caravans
{
    public const double Speed=3;
    public const int MaxBlockedTurns=3;
    public static bool Active(Caravan c)=>c.Status is CaravanStatus.Travelling or CaravanStatus.Waiting;
    public static string StatusName(CaravanStatus status)=>status switch
    {CaravanStatus.Travelling=>"W drodze",CaravanStatus.Waiting=>"Zatrzymana",CaravanStatus.Delivered=>"Dostarczona",_=>"Utracona"};
    public static bool Guarded(GameState s,TradeContract contract,int tile)=>
        s.Units.Any(u=>(u.Owner==contract.Buyer||u.Owner==contract.Seller)&&u.Kind!=UnitKind.Settler&&World.Distance(s.Tiles[u.Tile],s.Tiles[tile])<=1)||
        s.Cities.Any(c=>(c.Owner==contract.Buyer||c.Owner==contract.Seller)&&c.Tile==tile);
    public static bool Threatened(GameState s,TradeContract contract,int tile)=>
        s.Units.Any(u=>u.Owner!=contract.Buyer&&u.Owner!=contract.Seller&&u.Kind!=UnitKind.Settler&&
            (Engine.AtWar(s,u.Owner,contract.Buyer)||Engine.AtWar(s,u.Owner,contract.Seller))&&World.Distance(s.Tiles[u.Tile],s.Tiles[tile])<=1);
    public static bool AtRisk(GameState s,TradeContract contract,int tile)=>
        s.Units.Any(u=>u.Tile==tile&&u.Kind!=UnitKind.Settler&&u.Owner!=contract.Buyer&&u.Owner!=contract.Seller&&
            (Engine.AtWar(s,u.Owner,contract.Buyer)||Engine.AtWar(s,u.Owner,contract.Seller)))||
        Threatened(s,contract,tile)&&!Guarded(s,contract,tile);
    private static bool Transit(GameState s,TradeContract contract,Tile tile)=>tile.Passable&&
        (tile.Owner<0||tile.Owner==contract.Buyer||tile.Owner==contract.Seller||
            new[]{contract.Buyer,contract.Seller}.All(owner=>!s.Relation(owner,tile.Owner).War&&s.Relation(owner,tile.Owner).Trade&&s.Factions[tile.Owner].TradePolicy!=TradePolicy.Embargo));
    public static IReadOnlyList<int> Route(GameState s,TradeContract contract,int? start=null)
    {
        var seller=s.Cities.FirstOrDefault(c=>c.Id==contract.SellerCity);var buyer=s.Cities.FirstOrDefault(c=>c.Id==contract.BuyerCity);
        if(seller==null||buyer==null)return [];
        var danger=s.Units.Where(u=>u.Owner!=contract.Buyer&&u.Owner!=contract.Seller&&u.Kind!=UnitKind.Settler&&
            (Engine.AtWar(s,u.Owner,contract.Buyer)||Engine.AtWar(s,u.Owner,contract.Seller)))
            .SelectMany(u=>World.Neighbors(s,u.Tile).Select(t=>t.Id).Append(u.Tile)).ToHashSet();
        bool Safe(Tile t)=>Transit(s,contract,t)&&(!danger.Contains(t.Id)||Guarded(s,contract,t.Id))&&
            !s.Units.Any(u=>u.Tile==t.Id&&u.Kind!=UnitKind.Settler&&u.Owner!=contract.Buyer&&u.Owner!=contract.Seller&&
                (Engine.AtWar(s,u.Owner,contract.Buyer)||Engine.AtWar(s,u.Owner,contract.Seller)));
        if(!Safe(s.Tiles[start??seller.Tile])||!Safe(s.Tiles[buyer.Tile]))return [];
        return Navigation.SearchFrom(s,start??seller.Tile,Safe,(a,b)=>Navigation.MovementCost(s,a,b),target:buyer.Tile).PathTo(buyer.Tile);
    }
    public static int TravelTurns(GameState s,IReadOnlyList<int> route)
    {
        if(route.Count==0)return -1;
        int turns=0;double budget=0;
        for(int i=1;i<route.Count;i++)
        {double cost=Navigation.MovementCost(s,route[i-1],route[i]);if(cost>budget){turns++;budget=Speed;}budget-=cost;}
        return turns;
    }
    public static string? Suspension(GameState s,TradeContract contract)
    {
        var r=s.Relation(contract.Buyer,contract.Seller);
        if(r.War||!r.Trade)return "Granica zamknięta: brak pokojowego traktatu handlowego.";
        if(s.Factions[contract.Buyer].TradePolicy==TradePolicy.Embargo||s.Factions[contract.Seller].TradePolicy==TradePolicy.Embargo)return "Embargo zatrzymało karawanę.";
        return null;
    }
    public static List<Caravan> Project(GameState s)
    {
        var result=new List<Caravan>();
        foreach(var original in s.Caravans)
        {
            var c=new Caravan{Id=original.Id,ContractId=original.ContractId,Tile=original.Tile,DepartedTurn=original.DepartedTurn,
                LastMovedTurn=original.LastMovedTurn,BlockedTurns=original.BlockedTurns,ResolvedTurn=original.ResolvedTurn,
                Quantity=original.Quantity,Price=original.Price,Payment=original.Payment,Status=original.Status,Message=original.Message};
            result.Add(c);
            if(!Active(c)||c.LastMovedTurn>=s.Turn)continue;
            var contract=s.TradeContracts.First(t=>t.Id==c.ContractId);c.LastMovedTurn=s.Turn;
            void Lose(string reason){c.Status=CaravanStatus.Lost;c.ResolvedTurn=s.Turn;c.Message=reason+" Ładunek utracony; płatność nie podlega zwrotowi.";}
            if(!s.Cities.Any(city=>city.Id==contract.BuyerCity&&city.Owner==contract.Buyer)||!s.Cities.Any(city=>city.Id==contract.SellerCity&&city.Owner==contract.Seller))
            {Lose("Miasto kontraktu zmieniło właściciela.");continue;}
            if(AtRisk(s,contract,c.Tile))
            {Lose("Wrogie oddziały przechwyciły karawanę.");continue;}
            string? suspended=Suspension(s,contract);var route=suspended==null?Route(s,contract,c.Tile):[];
            if(route.Count==0)
            {
                c.BlockedTurns++;c.Status=CaravanStatus.Waiting;c.Message=suspended??"Brak bezpiecznej trasy lub zgody na tranzyt.";
                if(c.BlockedTurns>=MaxBlockedTurns)Lose("Trzy kolejne tury postoju zmusiły kupców do porzucenia transportu.");
                continue;
            }
            double budget=Speed;
            for(int i=1;i<route.Count;i++)
            {double cost=Navigation.MovementCost(s,route[i-1],route[i]);if(cost>budget)break;c.Tile=route[i];budget-=cost;}
            c.BlockedTurns=0;c.Status=CaravanStatus.Travelling;c.Message="Karawana posuwa się ku odbiorcy.";
            if(c.Tile==s.Cities.First(city=>city.Id==contract.BuyerCity).Tile)
            {c.Status=CaravanStatus.Delivered;c.ResolvedTurn=s.Turn;c.Message="Ładunek przekazano do magazynu odbiorcy.";}
        }
        return result;
    }
}
