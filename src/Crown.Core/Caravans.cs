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
    public static IReadOnlyList<int> Route(GameState s,TradeContract contract,int? start=null,Unit? escort=null)
        =>FindRoute(s,contract,start,escort,false);
    internal static IReadOnlyList<int> PreviewTransitRoute(GameState s,TradeContract contract,int? start=null,Unit? escort=null)
        =>FindRoute(s,contract,start,escort,true);
    private static IReadOnlyList<int> FindRoute(GameState s,TradeContract contract,int? start,Unit? escort,bool previewOpenBorders)
    {
        var seller=s.Cities.FirstOrDefault(c=>c.Id==contract.SellerCity);var buyer=s.Cities.FirstOrDefault(c=>c.Id==contract.BuyerCity);
        if(seller==null||buyer==null)return [];
        bool movingGuard=escort!=null&&escort.Tile==(start??seller.Tile)&&escort.Kind!=UnitKind.Settler&&
            (escort.Owner==contract.Buyer||escort.Owner==contract.Seller)&&s.Caravans.Any(c=>c.Id==escort.EscortCaravanId&&c.ContractId==contract.Id&&Caravans.Active(c));
        var danger=s.Units.Where(u=>u.Owner!=contract.Buyer&&u.Owner!=contract.Seller&&u.Kind!=UnitKind.Settler&&
            (Engine.AtWar(s,u.Owner,contract.Buyer)||Engine.AtWar(s,u.Owner,contract.Seller)))
            .SelectMany(u=>World.Neighbors(s,u.Tile).Select(t=>t.Id).Append(u.Tile)).ToHashSet();
        bool Safe(Tile t)=>TransitCouncil.CanCross(s,contract,t,previewOpenBorders)&&(!danger.Contains(t.Id)||Guarded(s,contract,t.Id)||movingGuard)&&
            !s.Units.Any(u=>u.Tile==t.Id&&u.Kind!=UnitKind.Settler&&u.Owner!=contract.Buyer&&u.Owner!=contract.Seller&&
                (Engine.AtWar(s,u.Owner,contract.Buyer)||Engine.AtWar(s,u.Owner,contract.Seller)));
        if(!Safe(s.Tiles[start??seller.Tile])||!Safe(s.Tiles[buyer.Tile]))return [];
        return Navigation.SearchFrom(s,start??seller.Tile,Safe,(a,b)=>Navigation.MovementCost(s,a,b),target:buyer.Tile).PathTo(buyer.Tile);
    }
    public static int TravelTurns(GameState s,IReadOnlyList<int> route,Unit? escort=null)
    {
        if(route.Count==0)return -1;
        if(route.Count==1)return 0;
        int turns=1;double budget=Speed,escortBudget=escort?.Actions??double.PositiveInfinity;
        for(int i=1;i<route.Count;i++)
        {
            if(escort!=null&&!Navigation.CanEnter(s,escort,s.Tiles[route[i]])){escort=null;escortBudget=double.PositiveInfinity;}
            double cost=Navigation.MovementCost(s,route[i-1],route[i]);
            if(cost>budget||cost>escortBudget){turns++;budget=Speed;escortBudget=escort?.MaxActions??double.PositiveInfinity;}
            budget-=cost;escortBudget-=cost;
        }
        return turns;
    }
    public static string? Suspension(GameState s,TradeContract contract)
    {
        var r=s.Relation(contract.Buyer,contract.Seller);
        if(r.War||!r.Trade)return "Granica zamknięta: brak pokojowego traktatu handlowego.";
        if(s.Factions[contract.Buyer].TradePolicy==TradePolicy.Embargo||s.Factions[contract.Seller].TradePolicy==TradePolicy.Embargo)return "Embargo zatrzymało karawanę.";
        return null;
    }
    public static CaravanProjection Project(GameState s)
    {
        var result=new List<Caravan>();
        var moves=new List<EscortMovement>();
        // Only cloned units move in the forecast. Terrain, politics and inventories remain read-only.
        var world=new GameState{Width=s.Width,Height=s.Height,Turn=s.Turn,Tiles=s.Tiles,Cities=s.Cities,Factions=s.Factions,
            Relations=s.Relations,Demands=s.Demands,Caravans=s.Caravans,TradeContracts=s.TradeContracts,
            Units=s.Units.Select(u=>new Unit{Id=u.Id,Owner=u.Owner,Tile=u.Tile,Kind=u.Kind,Health=u.Health,Actions=u.Actions,
                Destination=u.Destination,EscortCaravanId=u.EscortCaravanId,Order=u.Order,FortificationTurns=u.FortificationTurns,Experience=u.Experience}).ToList()};
        foreach(var original in s.Caravans.OrderBy(c=>c.Id))
        {
            var c=new Caravan{Id=original.Id,ContractId=original.ContractId,Tile=original.Tile,DepartedTurn=original.DepartedTurn,
                LastMovedTurn=original.LastMovedTurn,BlockedTurns=original.BlockedTurns,ResolvedTurn=original.ResolvedTurn,
                Quantity=original.Quantity,Price=original.Price,Payment=original.Payment,Status=original.Status,Message=original.Message};
            result.Add(c);
            if(!Active(c)||c.LastMovedTurn>=s.Turn)continue;
            var contract=s.TradeContracts.First(t=>t.Id==c.ContractId);c.LastMovedTurn=s.Turn;
            var escort=EscortOrders.Attached(world,c);var boundEscort=escort;var escortPath=new List<int>();
            if(escort!=null)escortPath.Add(escort.Tile);
            string? release=null;
            void RecordEscort()
            {
                if(boundEscort==null)return;
                if(!Active(c)){release??="Transport zakończył podróż.";boundEscort.EscortCaravanId=null;}
                moves.Add(new(boundEscort.Id,c.Id,escortPath.ToArray(),release));
            }
            void Lose(string reason){c.Status=CaravanStatus.Lost;c.ResolvedTurn=s.Turn;c.Message=reason+" Ładunek utracony; płatność nie podlega zwrotowi.";}
            if(!s.Cities.Any(city=>city.Id==contract.BuyerCity&&city.Owner==contract.Buyer)||!s.Cities.Any(city=>city.Id==contract.SellerCity&&city.Owner==contract.Seller))
            {Lose("Miasto kontraktu zmieniło właściciela.");RecordEscort();continue;}
            if(AtRisk(world,contract,c.Tile))
            {Lose("Wrogie oddziały przechwyciły karawanę.");RecordEscort();continue;}
            string? suspended=Suspension(s,contract);var route=suspended==null?Route(world,contract,c.Tile,escort):[];
            if(route.Count==0)
            {
                c.BlockedTurns++;c.Status=CaravanStatus.Waiting;c.Message=suspended??TransitCouncil.Blockage(world,contract,c.Tile,escort);
                if(c.BlockedTurns>=MaxBlockedTurns)Lose("Trzy kolejne tury postoju zmusiły kupców do porzucenia transportu.");
                RecordEscort();
                continue;
            }
            double budget=Speed;
            int initial=c.Tile;
            for(int i=1;i<route.Count;i++)
            {
                if(escort!=null&&!Navigation.CanEnter(world,escort,world.Tiles[route[i]]))
                {
                    release="Wojsko nie ma prawa wejścia na następne pole; pozostaje przy granicy lub obcym oddziale.";
                    escort.EscortCaravanId=null;escort=null;route=Route(world,contract,c.Tile);i=0;continue;
                }
                double cost=Navigation.MovementCost(s,c.Tile,route[i]);if(cost>budget||escort!=null&&cost>escort.Actions)break;
                c.Tile=route[i];budget-=cost;
                if(escort!=null){escort.Actions-=cost;escort.Tile=c.Tile;escortPath.Add(c.Tile);}
            }
            if(c.Tile==initial&&route.Count!=1)
            {
                c.BlockedTurns++;c.Status=CaravanStatus.Waiting;c.Message=escort!=null?"Eskorta nie ma punktów ruchu na następny heks.":"Dalsza bezpieczna trasa jest niedostępna.";
                if(c.BlockedTurns>=MaxBlockedTurns)Lose("Trzy kolejne tury postoju zmusiły kupców do porzucenia transportu.");RecordEscort();continue;
            }
            c.BlockedTurns=0;c.Status=CaravanStatus.Travelling;c.Message="Karawana posuwa się ku odbiorcy.";
            if(c.Tile==s.Cities.First(city=>city.Id==contract.BuyerCity).Tile)
            {c.Status=CaravanStatus.Delivered;c.ResolvedTurn=s.Turn;c.Message="Ładunek przekazano do magazynu odbiorcy.";}
            RecordEscort();
        }
        return new(result,moves);
    }
}
