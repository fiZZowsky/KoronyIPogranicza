namespace Crown.Core;

public static class MarketCouncil
{
    public static readonly NarrativeEvent Event=new("market_charter","Toma Rell","Przedstawicielka cechów","Komu służy otwarty targ?",
        "Kupcy chcą sprzedawać tam, gdzie płaci się najwięcej. Mieszkańcy pytają, dlaczego wozy z solą mijają ich domy, kiedy ceny rosną. Możemy chronić zapas dla rodzin, uwolnić eksport albo zamknąć handel zagraniczny. Każda droga ma cenę.",
        [new("Najpierw zaspokoimy potrzeby naszych mieszkańców.","Rezerwa krajowa przed eksportem.",Gold:-30,Stability:2,Commons:7,Nobles:-5),
         new("Nie zatrzymujmy kupców na granicach.","Eksport bez rezerwy dla mieszkańców.",Gold:40,Commons:-6,Nobles:8,Prestige:3),
         new("Wstrzymamy handel zagraniczny i uporządkujemy rynek.","Embargo blokuje również import.",Stability:3,Commons:-3,Nobles:-6)]);
}
