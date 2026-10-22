using Crown.Core;

namespace Crown.Desktop;

public sealed partial class MainWindow
{
    private void ShowAdministration(City city)
    {
        var p=Ui.Stack(Ui.Eyebrow("DANINY I ODPOWIEDZIALNOŚĆ"),Ui.Label(city.Name,30,Ui.Text,true),
            Ui.Label($"Korupcja {city.Corruption}% → cel {Administration.Target(state,city)}% · zmiana o 1 punkt na turę",18,Ui.Gold,true),
            Ui.Label($"Dochód miasta po korupcji: {Engine.CityYields(state,city).Gold:0.00} złota/turę\nBieżący ubytek: {Administration.Loss(state,city):0.00} złota/turę · ostatnio utracono: {city.LastRevenueLoss:0.00}\nNieujęte dochody od poprzedniego raportu: {city.HiddenGold:0.00} złota",14,Ui.Muted),
            Ui.Label("Korupcja odbiera wskazany procent dochodu miasta. Nie pomniejsza osobno podatków rynkowych, opłat kupieckich ani pieniędzy z traktatów. Oddalenie od stolicy, niepokoje, niska stabilność i ustrój autonomiczny lub wojskowy zwiększają jej cel. Sąd, Służba publiczna, Konstytucja i czasowy nadzór go obniżają. Pieniądze do odzyskania pochodzą wyłącznie z faktycznie minionych rozliczeń.",13,Ui.Muted));
        if(city.AuditProtectionUntil>state.Turn)p.Children.Add(Ui.Label($"Nadzór niezależnego urzędu: cel korupcji -20 do początku T{city.AuditProtectionUntil}.",14,Ui.Green));
        Ui.Gap(p,14);
        var active=state.Audits.FirstOrDefault(a=>a.CityId==city.Id&&Administration.Open(a));
        if(active!=null)
        {
            p.Children.Add(Ui.Label($"KONTROLA #{active.Id} · {Administration.StatusName(active.Status)}\nWysłana T{active.StartedTurn} · termin raportu T{active.DueTurn}",16,Ui.Gold,true));
            if(active.Status==AuditStatus.AwaitingCouncil)p.Children.Add(AuditReplyButton(active));
        }
        else
        {
            string? error=Administration.AuditError(state,city.Id);
            var send=Ui.Button("Zleć kontrolę: "+city.Name+" · 35 złota",()=>
            {var result=Engine.StartAudit(state,city.Id);Do(result);TryAutoSave(false);if(result.Success)ShowAdministration(city);},true);
            send.IsEnabled=error==null;p.Children.Add(send);if(error!=null)p.Children.Add(Ui.Label(error,13,Ui.Gold));
        }
        p.Children.Add(Ui.Label("Kontrola trwa trzy tury i wymaga Pisma. Koszt 35 złota nie podlega zwrotowi. Kolejna w tej prowincji: po 12 turach od wysłania poprzedniej i po zamknięciu jej sprawy. Przy sporządzeniu raportu zamykamy objętą nim sumę; nowe ubytki liczymy osobno. Rada wybierze jawny proces, niezależny urząd za dodatkowe 60 złota albo ugodę. Utrata miasta przerywa otwartą kontrolę i nadzór.",13,Ui.Muted));
        p.Children.Add(Ui.Button("Historia kontroli prowincji",ShowAudits));p.Children.Add(Ui.Button("Wróć do mapy",CloseModal));Modal(p,810);
    }
    private System.Windows.Controls.Button AuditReplyButton(AuditCase audit)
    {
        var button=Ui.Button("Rozpatrz raport #"+audit.Id,()=>{var result=AuditCouncil.Present(state,audit.Id);if(result.Success)ShowCouncil();else Do(result);},true);
        button.IsEnabled=state.Ending==null&&(state.PendingEvent==null||state.PendingEvent==AuditCouncil.Event.Id&&state.PendingAuditId==audit.Id);
        return button;
    }
    private void ShowAudits()
    {
        var p=Ui.Stack(Ui.Eyebrow("KSIĘGI RADY"),Ui.Label("Rachunki i kontrole",30,Ui.Text,true),
            Ui.Label("Bieżące ubytki, otwarte dochodzenia i rozliczone decyzje pozostają w historii kampanii. Kwota raportu nie rośnie po jego sporządzeniu. Odzyskane środki nie obejmują nowych strat.",13,Ui.Muted));
        foreach(var city in state.Cities.Where(c=>c.Owner==0).OrderByDescending(c=>c.Corruption).ThenBy(c=>c.Id))
            p.Children.Add(Ui.Button($"Rachunki: {city.Name} · korupcja {city.Corruption}% · ubytek {Administration.Loss(state,city):0.00}/turę",()=>ShowAdministration(city)));
        var audits=state.Audits.OrderByDescending(Administration.Open).ThenByDescending(a=>a.Id).Take(20).ToArray();
        if(audits.Length==0)p.Children.Add(Ui.Label("Nie zlecono jeszcze kontroli. Wybierz rachunki prowincji, aby sprawdzić warunki.",14,Ui.Gold));
        foreach(var audit in audits)
        {
            var q=Ui.Stack(Ui.Eyebrow($"KONTROLA #{audit.Id} · {Administration.StatusName(audit.Status)}"),
                Ui.Label(AuditCouncil.City(state,audit).Name,22,Ui.Text,true),
                Ui.Label($"Wysłano T{audit.StartedTurn} · raport {(audit.CompletedTurn==null?"najwcześniej T"+audit.DueTurn:"T"+audit.CompletedTurn)}\nUstalone braki: {audit.AssessedGold:0.00} złota · odzyskano {audit.RecoveredGold}\n"+(audit.ResolvedTurn==null?"Sprawa otwarta.":"Zamknięto T"+audit.ResolvedTurn+"."),13,Ui.Muted));
            if(audit.Status==AuditStatus.AwaitingCouncil){var reply=AuditReplyButton(audit);q.Children.Add(reply);if(!reply.IsEnabled)q.Children.Add(Ui.Label("Najpierw rozstrzygnij aktualną sprawę rady.",12,Ui.Gold));}
            p.Children.Add(Ui.Card(q));
        }
        p.Children.Add(Ui.Button("Wróć do mapy",CloseModal));Modal(p,830);
    }
}
