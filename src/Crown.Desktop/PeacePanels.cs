using Crown.Core;
using System.Windows.Controls;

namespace Crown.Desktop;

public sealed partial class MainWindow
{
    private static string PeaceStatusName(PeaceOffer offer)=>offer.Status switch
    {
        PeaceStatus.Travelling=>$"WARUNKI W DRODZE · przybycie w turze {offer.DueTurn}",
        PeaceStatus.AwaitingPlayer=>$"ODPOWIEDZ PRZED TURĄ {offer.DeadlineTurn}",
        PeaceStatus.Accepted=>"TRAKTAT PRZYJĘTY",PeaceStatus.Rejected=>"WARUNKI ODRZUCONE",
        PeaceStatus.Expired=>"PROPOZYCJA WYGASŁA",_=>"NEGOCJACJE ANULOWANE"
    };

    private void ShowWarReport(WarCampaign war)
    {
        int other=war.A==0?war.B:war.A;var score=WarCouncil.Score(state,war,0);
        var p=Ui.Stack(Ui.Eyebrow("DORADCY WOJENNI"),Ui.Label("Wojna z "+state.Factions[other].Name,32,Ui.Text,true),
            Ui.Label($"Od tury {war.StartedTurn} · bilans Marchii {score.Score:+0;-0;0} / -100…100",18,score.Score>=0?Ui.Green:Ui.Red));
        Ui.Gap(p,14);p.Children.Add(Ui.Label("Ogłoszony cel: "+WarCouncil.Goal(state,war),16,Ui.Gold,true));
        foreach(string factor in score.Factors)p.Children.Add(Ui.Label(factor,14,Ui.Muted));
        p.Children.Add(Ui.Rule());
        p.Children.Add(Ui.Label("Miasta liczą się według zmiany kontroli od początku tej wojny. Odzyskanie miasta odwraca ten składnik. Straty obejmują obrażenia wojsk i umocnień; wkład walk jest ograniczony do ±25. Siła armii do ±20, różnica wyczerpania do ±15.",13,Ui.Muted));
        p.Children.Add(Ui.Label($"Wyczerpanie Marchii {state.Player.WarWeariness:0}% · sąsiada {state.Factions[other].WarWeariness:0}%\nRozliczenie celu przy pokoju: prestiż +12 i lud +3 albo prestiż -5 i lud -3.",13,Ui.Gold));
        Ui.Gap(p,16);p.Children.Add(Ui.Button("Przejdź do rokowań",()=>ShowPeaceConference(state.Factions[other]),true));
        p.Children.Add(Ui.Button("Wróć do mapy",CloseModal));Modal(p,740);
    }

    private void ShowPeaceConference(Faction f)
    {
        var p=Ui.Stack(Ui.Eyebrow("ROKOWANIA POKOJOWE"),Ui.Label(f.Name,32,Ui.Text,true),
            Ui.Label("Wojna trwa podczas podróży posłów. Odpowiedź nadejdzie za dwie tury. Opłata poselstwa: 20 złota; reparacje są płacone dopiero przy ratyfikacji. Odrzucenie może przynieść kontrofertę.",14,Ui.Muted));
        Ui.Gap(p,14);p.Children.Add(Ui.Eyebrow("REPARACJE"));
        var payment=Ui.Combo(["Zażądaj 200 złota","Zażądaj 100 złota","Pokój bez reparacji","Zapłać 100 złota","Zapłać 200 złota"],2);p.Children.Add(payment);
        var cities=PeaceCouncil.ReturnableCities(state,0,f.Id).ToArray();
        p.Children.Add(Ui.Eyebrow("ZWROT JEDNEGO MIASTA"));
        var names=new[]{"Pozostaw obecną kontrolę miast"}.Concat(cities.Select(c=>$"{c.Name} → {state.Factions[c.OriginalOwner].Name}")).ToArray();
        var city=Ui.Combo(names,0);p.Children.Add(city);
        p.Children.Add(Ui.Label("Można zwracać miasta założycielom po obu stronach. Nie można oddać ostatniego miasta państwa. Pozostałe miasta pozostają pod obecną kontrolą.",12,Ui.Muted));
        var assessment=Ui.Stack();p.Children.Add(assessment);var feedback=Ui.Label("",12,Ui.Red);
        var submit=Ui.Button("Wyślij warunki pokoju · 20 złota",()=>
        {
            int gold=PeaceCouncil.Payments[payment.SelectedIndex];int? id=city.SelectedIndex==0?null:cities[city.SelectedIndex-1].Id;
            var result=Engine.ProposePeace(state,f.Id,gold,id);
            if(result.Success){CloseModal();TryAutoSave(false);Do(result);}else feedback.Text=result.Message;
        },true);
        void Update()
        {
            int gold=PeaceCouncil.Payments[payment.SelectedIndex];int? id=city.SelectedIndex==0?null:cities[city.SelectedIndex-1].Id;
            var preview=PeaceCouncil.Preview(state,0,f.Id,gold,id);assessment.Children.Clear();Ui.Gap(assessment,14);
            assessment.Children.Add(Ui.Label(preview.Valid?$"Ocena rozmówcy {preview.Acceptance} · próg przyjęcia {PeaceCouncil.AcceptanceThreshold}":preview.Error,17,preview.Valid&&preview.Acceptance>=50?Ui.Green:Ui.Gold,true));
            if(preview.Valid)assessment.Children.Add(Ui.Label(string.Join(" · ",preview.Factors),12,Ui.Muted));
            assessment.Children.Add(Ui.Label("Przyjęcie: zwrot wskazanego miasta, reparacje, rozejm 12 tur, wyczerpanie obu stron -15, krzywdy -20. Wojsko z terytorium drugiej strony wycofa się do własnych miast; bez schronienia zostanie rozwiązane.",12,Ui.Muted));
            submit.IsEnabled=preview.Valid;feedback.Text="";
        }
        payment.SelectionChanged+=(_,_)=>Update();city.SelectionChanged+=(_,_)=>Update();Update();
        Ui.Gap(p,14);p.Children.Add(submit);p.Children.Add(feedback);p.Children.Add(Ui.Button("Wycofaj posłów",CloseModal));Modal(p,810);
    }

    private void ShowPeaceReply(PeaceOffer offer)
    {
        var preview=PeaceCouncil.Preview(state,offer.From,0,offer.Gold,offer.ReturnCityId);
        var p=Ui.Stack(Ui.Eyebrow("POSŁOWIE PRZYBYLI"),Ui.Label(state.Factions[offer.From].Leader,30,Ui.Text,true),
            Ui.Label(offer.Reply,17,Ui.Text,true),Ui.Label(PeaceCouncil.Terms(state,offer.From,0,offer.Gold,offer.ReturnCityId),16,Ui.Gold,true));
        Ui.Gap(p,16);p.Children.Add(Ui.Label($"Odpowiedz przed turą {offer.DeadlineTurn}. Zignorowanie oznacza wygaśnięcie warunków i dalszą wojnę. Nie ma automatycznego pokoju bez Twojej decyzji.",13,Ui.Muted));
        p.Children.Add(Ui.Label("Traktat zmniejsza wyczerpanie obu stron o 15 i krzywdy o 20, wycofuje oddziały z terytorium drugiej strony oraz rozlicza ogłoszony cel wojenny. Handel wymaga nowej umowy.",13,Ui.Muted));
        if(!preview.Valid)p.Children.Add(Ui.Label(preview.Error,13,Ui.Red));
        var feedback=Ui.Label("",12,Ui.Red);
        void Answer(bool accept){var result=Engine.AnswerPeace(state,offer.Id,accept);if(result.Success){CloseModal();TryAutoSave(false);Do(result);}else feedback.Text=result.Message;}
        var agree=Ui.Button("Przyjmij warunki i podpisz traktat",()=>Answer(true),true);agree.IsEnabled=preview.Valid&&offer.Status==PeaceStatus.AwaitingPlayer;p.Children.Add(agree);
        p.Children.Add(Ui.Button("Odrzuć warunki · wojna trwa",()=>Answer(false)));p.Children.Add(feedback);
        p.Children.Add(Ui.Button("Odpowiem później",CloseModal));Modal(p,770);
    }
}
