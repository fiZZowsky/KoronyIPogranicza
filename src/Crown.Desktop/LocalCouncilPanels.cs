using Crown.Core;

namespace Crown.Desktop;

public sealed partial class MainWindow
{
    private void ShowLocalCouncil(City city,string? notice=null)
    {
        var r=city.Council;
        var p=Ui.Stack(Ui.Eyebrow("POLITYKA PROWINCJI · "+CampaignCalendar.At(state,state.Turn)),Ui.Label("Rada: "+city.Name,30,Ui.Text,true),
            Ui.Label("Rodziny, cechy i miejscowe rody pamiętają wykonanie obietnic. Umowa trwa sześć rozliczeń gospodarki; co najmniej cztery udane ustanawiają trwałą kartę. Zwołanie rady kosztuje 30 złota. Każdy udany etap wymaga dodatkowej opłaty; brak środków lub warunków oznacza niewykonanie. Kolejna umowa po dziesięciu rozliczeniach od podpisu. Podbój rozwiązuje lokalną koalicję i jej karty.",14,Ui.Muted));
        Ui.Bar(p,"Rodziny · "+r.Families+"%",r.Families,100,Ui.Green);
        Ui.Bar(p,"Cechy · "+r.Guilds+"%",r.Guilds,100,Ui.Gold);
        Ui.Bar(p,"Miejscowe rody · "+r.Landholders+"%",r.Landholders,100,Ui.Gold);
        p.Children.Add(Ui.Label($"Wpływ stronnictw na cel lojalności: {LocalCouncils.Loyalty(city):+0;-0;0}. Sukces: poparcie grupy +12, lojalność +5. Porażka: poparcie -18, niepokoje +8, lojalność -8. Podpis: poparcie zainteresowanej grupy +8, konkurentów -5.",13,Ui.Muted));
        if(notice!=null)p.Children.Add(Ui.Label(notice,14,Ui.Gold));
        p.Children.Add(Ui.Label(r.Report,14,Ui.Text));
        if(r.Mandate!=LocalMandate.None)
        {
            p.Children.Add(Ui.Label(LocalCouncils.Name(r.Mandate),22,Ui.Gold,true));
            Ui.Bar(p,$"Wykonanie {r.Delivered}/4 · minione rozliczenia {r.Phases}/6",r.Delivered,4,Ui.Green);
            p.Children.Add(Ui.Label($"Podpis: {CampaignCalendar.At(state,r.StartedTurn)} · koniec po gospodarce {CampaignCalendar.At(state,r.StartedTurn+5)}. Opłata za udane rozliczenie {r.PhaseFee}; wydano {r.Paid+30} złota.\n"+LocalCouncils.Terms(r.Mandate),14,Ui.Muted));
        }
        foreach(var m in Enum.GetValues<LocalMandate>().Where(m=>m!=LocalMandate.None))
        {
            var mandate=m;var error=LocalCouncils.Error(state,city,m);
            p.Children.Add(Ui.Label(LocalCouncils.Name(m)+(r.Charters.Contains(m)?" · USTANOWIONA":""),20,Ui.Gold,true));
            p.Children.Add(Ui.Label(LocalCouncils.Terms(m),14,Ui.Text));
            var b=Ui.Button("Uzgodnij: "+LocalCouncils.Name(m),()=>ConfirmLocalMandate(city,mandate));b.IsEnabled=error==null;p.Children.Add(b);
            p.Children.Add(Ui.Label(error??$"30 złota teraz + do {LocalCouncils.Fee(state,city)*6} złota za wykonanie; stawka zostanie utrwalona przy podpisie.",12,error==null?Ui.Muted:Ui.Gold));
        }
        p.Children.Add(Ui.Label("Daty z oznaczeniem „ok.” są szacunkami: dotyczą przyszłych terminów lub historii sprzed wprowadzenia kalendarza. Czas wykonywania umów jest liczony w rozliczeniach, ponieważ tempo upływu lat zmienia się w toku historii.",12,Ui.Muted));
        p.Children.Add(Ui.Button("Wróć do prowincji",()=>ShowProvince(city)));Modal(p,920);
    }
    private void ConfirmLocalMandate(City city,LocalMandate mandate)
    {
        var p=Ui.Stack(Ui.Eyebrow("ROZMOWA RADY · "+city.Name),Ui.Label(LocalCouncils.Name(mandate),28,Ui.Text,true),
            Ui.Label(LocalCouncils.Terms(mandate),16,Ui.Text),
            Ui.Label($"Zwołanie: 30 złota. Stawka: {LocalCouncils.Fee(state,city)} złota za każde udane rozliczenie, przez sześć rozliczeń. Potrzebne cztery udane. Podpis zmieni poparcie grup; niedotrzymanie obietnicy pogorszy lojalność i zwiększy niepokoje.",15,Ui.Gold));
        p.Children.Add(Ui.Button("Podpisz lokalną umowę",()=>{var result=LocalCouncils.Begin(state,city.Id,mandate);Do(result);if(result.Success)TryAutoSave(false);ShowLocalCouncil(city,result.Message);},true));
        p.Children.Add(Ui.Button("Wróć do rozmów",()=>ShowLocalCouncil(city)));Modal(p,780);
    }
}
