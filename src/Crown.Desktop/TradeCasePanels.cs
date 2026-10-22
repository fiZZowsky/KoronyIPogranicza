using Crown.Core;
using System.Windows.Controls;

namespace Crown.Desktop;

public sealed partial class MainWindow
{
    private void ShowTradeCases()
    {
        var p=Ui.Stack(Ui.Eyebrow("SŁOWO RADY"),Ui.Label("Kupcy pamiętają",32,Ui.Text,true),
            Ui.Label("Utrata importowej karawany może sprowadzić delegację z miasta: lud -2, miejscowe niepokoje +4. Najwyżej jedna nowa delegacja z prowincji na dziesięć tur. Odszkodowanie kosztuje zaokrągloną w górę wartość utraconego zakupu i nie odtwarza towaru. Zastępcza dostawa wymaga rzeczywistego przybycia właściwego towaru do wskazanego miasta; kupienie lub wyprodukowanie zapasu nie wystarczy.",13,Ui.Muted));
        if(state.PendingEvent!=null)p.Children.Add(Ui.Label("Aktualna sprawa rady: "+ProvincialEvents.Current(state).Title+". Odpowiedz jej przed inną delegacją.",12,Ui.Gold));
        var cases=state.TradeCases.OrderByDescending(c=>c.Status is TradeCaseStatus.Pending or TradeCaseStatus.Promised).ThenByDescending(c=>c.OpenedTurn).ThenBy(c=>c.CaravanId).Take(20).ToArray();
        if(cases.Length==0)p.Children.Add(Ui.Label("Nie ma jeszcze delegacji kupieckich ani obietnic dostaw. Nowe sprawy wynikają z przyszłych strat importów do własnych miast.",16,Ui.Gold));
        foreach(var c in cases)
        {
            var city=TradeCouncil.City(state,c);var source=TradeCouncil.Source(state,c);var t=TradeCouncil.Contract(state,c);
            var q=Ui.Stack(Ui.Eyebrow($"KARAWANA #{c.CaravanId} · SPRAWA OD T{c.OpenedTurn}"),Ui.Label(city.Name,23,Ui.Text,true),
                Ui.Label(TradeCouncil.StatusName(c.Status),16,c.Status==TradeCaseStatus.Broken?Ui.Red:Ui.Gold),
                Ui.Label($"Utracono {source.Quantity:0.##}: {Markets.Name(t.Good)} · zapłacono {source.Payment:0.00} złota",13,Ui.Muted));
            if(c.Decision==1)
            {
                double progress=TradeCouncil.Progress(state,c);Ui.Bar(q,$"Dostarczono {progress:0.##}/{source.Quantity:0.##} · termin: faza rynku T{c.DeadlineTurn}",progress,source.Quantity,c.Status==TradeCaseStatus.Broken?Ui.Red:Ui.Green);
                q.Children.Add(Ui.Label("Liczą się zagraniczne przyjazdy po decyzji, także wcześniej wysłanych karawan.\n"+TradeCouncil.SuccessEffects+"\n"+TradeCouncil.FailureEffects,12,Ui.Muted));
                if(c.CreditedCaravans.Count>0)q.Children.Add(Ui.Label("Rozliczone manifesty: "+string.Join(", ",c.CreditedCaravans.Select(id=>"#"+id)),12,Ui.Gold));
            }
            if(c.Status==TradeCaseStatus.Pending)
            {
                var button=Ui.Button("Rozpatrz sprawę #"+c.CaravanId,()=>
                {
                    var result=TradeCouncil.Present(state,c.CaravanId);
                    if(result.Success){CloseModal();Refresh();TryAutoSave(false);ShowCouncil();}else Toast(result.Message,false);
                },true);
                button.IsEnabled=state.PendingEvent==null||state.PendingEvent==TradeCouncil.Event.Id&&state.PendingTradeCaseId==c.CaravanId;q.Children.Add(button);
            }
            if(city.Owner==0)q.Children.Add(Ui.Button("Zobacz rynek: "+city.Name,()=>{map.Center(city.Tile);ShowCityMarket(city);}));
            p.Children.Add(Ui.Card(q));
        }
        p.Children.Add(Ui.Button("Wróć do mapy",CloseModal));Modal(p,860);
    }
}
