namespace Crown.Core;

public static class Markets
{
    public static readonly TradeGood[] Goods=Enum.GetValues<TradeGood>();
    public const int PolicyPrice=30,ContractPrice=15;
    public static string Name(TradeGood good)=>good switch{TradeGood.Iron=>"Żelazo",TradeGood.Salt=>"Sól",_=>"Barwniki"};
    public static double BasePrice(TradeGood good)=>good switch{TradeGood.Iron=>8,TradeGood.Salt=>5,_=>9};
    public static string PolicyName(TradePolicy policy)=>policy switch{TradePolicy.OpenExports=>"Swobodny eksport",TradePolicy.Embargo=>"Embargo",_=>"Najpierw mieszkańcy"};
    public static string PolicyDescription(TradePolicy policy)=>policy switch
    {
        TradePolicy.OpenExports=>"Eksport może wykorzystać całe zapasy przed zaspokojeniem potrzeb mieszkańców.",
        TradePolicy.Embargo=>"Wstrzymuje import i eksport; wymiana krajowa działa dalej. Kontrakty nadal wygasają.",
        _=>"Eksport pozostawia w regionie zapas odpowiadający jednej turze miejscowego popytu."
    };
    public static double Capacity(City city)=>50+(city.Buildings.Contains("market")?25:0)+(city.Buildings.Contains("warehouse")?100:0);
    public static double Demand(City city,TradeGood good)=>good switch
    {
        TradeGood.Iron=>city.Population*.15+(city.Buildings.Contains("workshop")?1:0)+(city.Buildings.Contains("factory")?2:0),
        TradeGood.Salt=>city.Population*.35,
        _=>city.Population*.2+(city.Buildings.Contains("bank")?1:0)
    };
    public static double TaxRate(Faction f)=>f.Tax==0?.08:f.Tax==2?.18:.12;
    public static double Coverage(City c,TradeGood good)=>c.LastMarketLines.FirstOrDefault(l=>l.Good==good)?.Coverage??1;
    public static double IndustryMultiplier(City c)=>c.LastMarketTurn==0?1:.9+.1*Coverage(c,TradeGood.Iron);
    public static int ProsperityEffect(City c)=>c.LastMarketTurn==0?0:(int)Math.Round((Coverage(c,TradeGood.Salt)+Coverage(c,TradeGood.Dyes))*6)-4;
    public static double LastTradeBalance(GameState s,int owner)=>s.LastMarketTurn==0?0:s.TradeContracts.Where(c=>c.LastExecutedTurn==s.LastMarketTurn)
        .Sum(c=>c.Seller==owner?c.LastPayment:c.Buyer==owner?-c.LastPayment:0);

    public static Dictionary<int,Dictionary<TradeGood,double>> Production(GameState s)
    {
        var output=s.Cities.ToDictionary(c=>c.Id,_=>Goods.ToDictionary(g=>g,_=>0d));
        var worked=s.Cities.ToDictionary(c=>c.Id,c=>Engine.WorkedTiles(s,c).Select(t=>t.Id).ToHashSet());
        foreach(var tile in s.Tiles.Where(t=>t.Owner>=0&&t.Resource is Resource.Iron or Resource.Salt or Resource.Dyes))
        {
            if(Logistics.For(s,tile.Owner).Interdicted.Contains(tile.Id))continue;
            var city=s.Cities.Where(c=>c.Owner==tile.Owner&&worked[c.Id].Contains(tile.Id))
                .OrderBy(c=>World.Distance(tile,s.Tiles[c.Tile])).ThenBy(c=>c.Id).FirstOrDefault();
            if(city==null)continue; // A resource tile can contribute to at most one city.
            var good=tile.Resource==Resource.Iron?TradeGood.Iron:tile.Resource==Resource.Salt?TradeGood.Salt:TradeGood.Dyes;
            double amount=2+(good==TradeGood.Iron&&tile.Improvement==Improvement.Mine?2:tile.Improvement==Improvement.TradingPost?1:0);
            output[city.Id][good]+=amount;
        }
        return output;
    }

    private sealed class Line(TradeGood good,double opening,double produced,double demand)
    {
        public TradeGood Good=good;
        public double Opening=opening,Produced=produced,Demand=demand,Stock=opening+produced;
        public double DomesticIn,DomesticOut,ForeignIn,ForeignOut,Consumed,Lost,Price;
        public CommodityBalance Freeze()=>new(Good,Opening,Produced,DomesticIn,DomesticOut,ForeignIn,ForeignOut,Consumed,Demand,Lost,Stock,Price);
    }

    public static string? RouteError(GameState s,TradeContract contract)
    {
        var buyer=s.Cities.FirstOrDefault(c=>c.Id==contract.BuyerCity&&c.Owner==contract.Buyer);
        var seller=s.Cities.FirstOrDefault(c=>c.Id==contract.SellerCity&&c.Owner==contract.Seller);
        if(buyer==null||seller==null)return "Miasto zmieniło właściciela; umowa zostaje rozwiązana.";
        var relation=s.Relation(contract.Buyer,contract.Seller);
        if(relation.War||!relation.Trade)return "Brak pokojowego traktatu handlowego.";
        if(s.Factions[contract.Buyer].TradePolicy==TradePolicy.Embargo||s.Factions[contract.Seller].TradePolicy==TradePolicy.Embargo)return "Embargo wstrzymuje dostawę.";
        foreach(var city in new[]{buyer,seller})
        {
            var network=Logistics.For(s,city.Owner);
            if(!city.Buildings.Contains("market")||!s.Factions[city.Owner].Technologies.Contains("trade"))return "Potrzebne targowiska i Szlaki handlowe po obu stronach.";
            if(!network.ConnectedCities.Contains(city.Id)||network.Interdicted.Contains(city.Tile))return "Miasto utraciło bezpieczne połączenie ze stolicą.";
        }
        return null;
    }

    public static MarketPlan Plan(GameState s)
    {
        var production=Production(s);
        var lines=s.Cities.ToDictionary(c=>c.Id,c=>Goods.ToDictionary(g=>g,g=>new Line(g,c.MarketStocks[g],production[c.Id][g],Demand(c,g))));
        var groups=s.Cities.ToDictionary(c=>c.Id,c=>(c.Owner,Group:Logistics.For(s,c.Owner).MarketGroups[c.Id]));
        var regions=s.Cities.OrderBy(c=>c.Id).GroupBy(c=>groups[c.Id]).ToDictionary(g=>g.Key,g=>g.ToArray());
        foreach(var region in regions.Values)foreach(var good in Goods)
        {
            double stock=region.Sum(c=>lines[c.Id][good].Stock),demand=region.Sum(c=>lines[c.Id][good].Demand);
            double price=Math.Round(Math.Clamp(BasePrice(good)*(.75+demand/(stock+1)),BasePrice(good)*.5,BasePrice(good)*3),2);
            foreach(var c in region)lines[c.Id][good].Price=price;
        }
        var budget=s.Factions.ToDictionary(f=>f.Id,f=>f.Gold);
        var cash=s.Factions.ToDictionary(f=>f.Id,_=>0d);
        var trades=new List<TradeExecution>();var shipments=new List<CommodityShipment>();
        foreach(var contract in s.TradeContracts.Where(c=>c.Status==ContractStatus.Active).OrderBy(c=>c.Id))
        {
            string? error=s.Turn>=contract.ExpiresTurn?"Umowa wygasła.":RouteError(s,contract);
            if(error!=null){trades.Add(new(contract.Id,0,0,0,error));continue;}
            var sellers=regions[groups[contract.SellerCity]];
            var good=contract.Good;
            double stock=sellers.Sum(c=>lines[c.Id][good].Stock);
            double reserve=s.Factions[contract.Seller].TradePolicy==TradePolicy.DomesticFirst?sellers.Sum(c=>lines[c.Id][good].Demand):0;
            double price=Math.Round(lines[contract.SellerCity][good].Price*1.15,2);
            double funds=Math.Floor(Math.Max(0,budget[contract.Buyer])*100)/100;
            double quantity=Math.Min(contract.Quantity,Math.Min(Math.Max(0,stock-reserve),Math.Floor(funds/price*100)/100));
            quantity=Math.Round(quantity,2,MidpointRounding.ToZero);
            if(quantity<=0){trades.Add(new(contract.Id,0,price,0,stock<=reserve?"Brak nadwyżki po rezerwie dla mieszkańców.":"Nabywcy brakuje złota."));continue;}
            double payment=Math.Round(quantity*price,2);
            double left=quantity;
            foreach(var city in sellers)
            {
                var line=lines[city.Id][good];double take=Math.Min(left,line.Stock);
                line.Stock-=take;line.ForeignOut+=take;left-=take;if(left<.000001)break;
            }
            var recipient=lines[contract.BuyerCity][good];recipient.Stock+=quantity;recipient.ForeignIn+=quantity;
            budget[contract.Buyer]-=payment;budget[contract.Seller]+=payment;cash[contract.Buyer]-=payment;cash[contract.Seller]+=payment;
            trades.Add(new(contract.Id,quantity,price,payment,quantity+.00001<contract.Quantity?"Dostawa częściowa.":"Dostawa zrealizowana."));
        }
        foreach(var region in regions.Values)foreach(var good in Goods)
        {
            foreach(var c in region)
            {var line=lines[c.Id][good];double used=Math.Min(line.Stock,line.Demand);line.Stock-=used;line.Consumed=used;}
            foreach(var receiver in region.OrderBy(c=>lines[c.Id][good].Consumed/Math.Max(.01,lines[c.Id][good].Demand)).ThenBy(c=>c.Id))
            {
                var recipient=lines[receiver.Id][good];double need=recipient.Demand-recipient.Consumed;
                foreach(var donor in region.Where(c=>c.Id!=receiver.Id))
                {
                    var source=lines[donor.Id][good];double amount=Math.Min(need,source.Stock);if(amount<=.000001)continue;
                    source.Stock-=amount;source.DomesticOut+=amount;recipient.DomesticIn+=amount;recipient.Consumed+=amount;need-=amount;
                    shipments.Add(new(donor.Id,receiver.Id,good,amount));if(need<.000001)break;
                }
            }
        }
        foreach(var c in s.Cities)foreach(var good in Goods)
        {var line=lines[c.Id][good];line.Lost=Math.Max(0,line.Stock-Capacity(c));line.Stock=Math.Clamp(line.Stock,0,Capacity(c));}
        return new(lines.ToDictionary(p=>p.Key,p=>Goods.Select(g=>p.Value[g].Freeze()).ToList()),shipments,trades,cash);
    }

    public static void Tick(GameState s)
    {
        if(s.LastMarketTurn==s.Turn)return;
        var plan=Plan(s);
        foreach(var c in s.Cities)
        {
            c.LastMarketLines=plan.Cities[c.Id];c.LastMarketTurn=s.Turn;
            c.MarketStocks=c.LastMarketLines.ToDictionary(l=>l.Good,l=>l.Closing);
            c.LastMarketTax=Math.Round(c.LastMarketLines.Sum(l=>l.Consumed*l.Price)*TaxRate(s.Factions[c.Owner]),2);
        }
        foreach(var f in s.Factions)f.Gold=Math.Max(0,f.Gold+plan.Cash[f.Id]);
        foreach(var trade in plan.Trades)
        {
            var contract=s.TradeContracts.First(c=>c.Id==trade.ContractId);
            bool changed=contract.Message!=trade.Message;
            contract.LastExecutedTurn=s.Turn;contract.LastDelivered=trade.Quantity;contract.LastPrice=trade.Price;contract.LastPayment=trade.Payment;contract.Message=trade.Message;
            if(s.Turn>=contract.ExpiresTurn)contract.Status=ContractStatus.Expired;
            else if(!s.Cities.Any(c=>c.Id==contract.BuyerCity&&c.Owner==contract.Buyer)||!s.Cities.Any(c=>c.Id==contract.SellerCity&&c.Owner==contract.Seller))contract.Status=ContractStatus.Cancelled;
            if(changed&&(contract.Buyer==0||contract.Seller==0))s.Log($"Umowa #{contract.Id}, {Name(contract.Good)}: {trade.Message} Ilość {trade.Quantity:0.##}, płatność {trade.Payment:0.##}.","handel");
        }
        s.LastMarketTurn=s.Turn;
    }

    public static void ManageAi(GameState s,int owner)
    {
        foreach(var c in s.Cities.Where(c=>c.Owner==owner&&Content.Project(c.Project).Unit==null&&c.MarketStocks.Values.Any(v=>v>=Capacity(c)*.8)))
            if(Engine.AvailableProjects(s,c).Any(p=>p.Id=="warehouse")&&c.Project!="warehouse"&&c.Population<Regions.Housing(s,c)&&c.Population<Regions.Sanitation(s,c)&&c.Food>8)
            {c.Project="warehouse";c.Production*=.8;}
        if(s.Turn%10!=0||s.Factions[owner].Gold<250||s.TradeContracts.Count(c=>c.Status==ContractStatus.Active&&(c.Buyer==owner||c.Seller==owner))>=3)return;
        var city=s.Cities.Where(c=>c.Owner==owner&&c.Buildings.Contains("market")).OrderBy(c=>c.Id).FirstOrDefault();if(city==null)return;
        foreach(var good in Goods.OrderBy(g=>Coverage(city,g)))
        {
            if(Coverage(city,good)>=.8)continue;
            foreach(var seller in s.Cities.Where(c=>c.Owner>0&&c.Owner!=owner&&c.Buildings.Contains("market")&&c.MarketStocks[good]>=6).OrderBy(c=>c.Id))
                if(Engine.SignTradeContract(s,city.Id,seller.Id,good,2,owner).Success)return;
        }
    }
}
