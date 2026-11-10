using Crown.Core;

namespace Crown.Desktop;

public sealed partial class MainWindow
{
    private static string TreatyStatusName(TreatyStatus status)=>status switch
    {
        TreatyStatus.Travelling=>"Posłowie w drodze",TreatyStatus.AwaitingPlayer=>"Oczekuje na ratyfikację",TreatyStatus.Active=>"Obowiązuje",TreatyStatus.Completed=>"Dotrzymany",TreatyStatus.Broken=>"Zerwany",TreatyStatus.Rejected=>"Odmowa",TreatyStatus.Expired=>"Minął termin podpisu",_=>"Anulowany"
    };
    private void ShowTreaties(string? notice=null)
    {
        var p=Ui.Stack(Ui.Eyebrow("UMOWY, ZABEZPIECZENIA I ODPOWIEDZIALNOŚĆ"),Ui.Label("Traktaty i gwarancje",30,Ui.Text,true),
            Ui.Label("Propozycja kosztuje 10 złota. Odpowiedź po dwóch turach nie jest jeszcze podpisem: obie strony wpłacają depozyty dopiero przy ratyfikacji. Jedno państwo utrzymuje najwyżej dwa aktywne lub negocjowane traktaty. Kolejna propozycja do tej samej strony po ośmiu turach.",14,Ui.Muted));
        if(notice!=null)p.Children.Add(Ui.Label(notice,14,Ui.Gold));
        foreach(var f in state.Factions.Skip(1).Where(f=>state.Cities.Any(c=>c.Owner==f.Id)))p.Children.Add(Ui.Button("Zaproponuj traktat: "+f.Name,()=>ShowTreatyDraft(f.Id)));
        foreach(var t in state.Treaties.OrderByDescending(t=>Treaties.Open(t)).ThenByDescending(t=>t.Id))
        {
            int other=t.From==0?t.To:t.From;var c=Ui.Stack(Ui.Label($"#{t.Id} · {Treaties.Name(t.Kind)} · {state.Factions[other].Name}",21,Ui.Text,true),
                Ui.Label(TreatyStatusName(t.Status)+$" · propozycja T{t.SentTurn} · {t.Duration} tur",14,Ui.Gold),Ui.Label(t.Report,13,Ui.Muted));
            if(Treaties.Open(t))c.Children.Add(Ui.Label($"Odpowiedź T{t.DueTurn}; ratyfikacja przed T{t.DeadlineTurn}. Depozyt każdej strony: {Treaties.Bond(t.Kind)} złota.",13,Ui.Muted));
            if(t.Status==TreatyStatus.AwaitingPlayer)c.Children.Add(Ui.Button("Przeczytaj warunki #"+t.Id,()=>ShowTreatyReply(t.Id),true));
            if(t.SignedTurn!=null)c.Children.Add(Ui.Label($"Podpis T{t.SignedTurn} · wygaśnięcie przed gospodarką T{t.ExpiresTurn}\nDepozyty: {t.EscrowFrom} / {t.EscrowTo} · wypłaty dla stron: {t.PayoutFrom} / {t.PayoutTo}",13,Ui.Muted));
            if(t.Kind==TreatyKind.Research&&t.SignedTurn!=null)c.Children.Add(Ui.Label($"Sfinansowane fazy: {t.FundedPhases} · opłaty każdej strony łącznie {t.FundedPhases*Treaties.ResearchFee} złota · nauka każdej strony łącznie {t.TotalScience:0.##}\nOstatnia faza T{t.LastPhaseTurn}: rzeczywista nauka stron {t.LastIncomeFrom:0.##} / {t.LastIncomeTo:0.##}, premia {t.LastScience:0.##}, opłata {t.LastFee} złota/osobę.",13,Ui.Green));
            if(Treaties.Active(state,t))c.Children.Add(Ui.Button("Rozważ zerwanie #"+t.Id,()=>ConfirmTreatyBreak(t.Id)));
            p.Children.Add(Ui.Card(c));
        }
        if(state.Treaties.Count==0)p.Children.Add(Ui.Label("Brak nowych traktatów. Pierwsza propozycja otwiera kanał, którym AI może później składać własne oferty. Żadna oferta AI nie pobierze twojego depozytu bez podpisu.",14,Ui.Muted));
        p.Children.Add(Ui.Label("Badania: obie strony płacą po 2 złota/fazę; każda otrzymuje 15% mniejszego rzeczywistego dochodu naukowego, najwyżej 5 + niższy indeks epoki. Zapas dawnej nauki nie daje premii. Niedobór środków zatrzymuje opłatę i premię obu stron; trzy jednostronne niedobory zrywają umowę. Po wspólnym niedoborze zabezpieczenia wracają. Premia jest przyznawana po badaniach bieżącej tury, do wykorzystania w kolejnej.",13,Ui.Muted));
        p.Children.Add(Ui.Button("Wróć do mapy",CloseModal));Modal(p,960);
    }
    private void ShowTreatyDraft(int other)
    {
        var f=state.Factions[other];var p=Ui.Stack(Ui.Eyebrow("ROZMOWA O WSPÓLNYCH ZOBOWIĄZANIACH"),Ui.Label(f.Name,28,Ui.Text,true),
            Ui.Label("„Umowa jest warta tyle, ile gotowi jesteśmy w niej zabezpieczyć. Nasi uczeni mogą pracować wspólnie, a pokoju możemy strzec własnym depozytem. Wyślijcie warunki; żadna odpowiedź nie zastąpi waszego podpisu.”",15,Ui.Text));
        foreach(var kind in Enum.GetValues<TreatyKind>())
        {
            p.Children.Add(Ui.Label(Treaties.Name(kind)+$" · depozyt po {Treaties.Bond(kind)} złota",20,Ui.Gold,true));
            p.Children.Add(Ui.Label(kind==TreatyKind.Research?"Współpraca płatna po 2 złota na stronę i fazę, oparta na rzeczywistych dochodach naukowych. Wojna i embargo ją zrywają.":"Gwarancja nie jest przymusowym rozejmem. Wojnę nadal można wypowiedzieć, ale napastnik odda swój depozyt partnerowi i poniesie koszt reputacji. AI ostrożniej rozważa atak.",14,Ui.Muted));
            foreach(int duration in new[]{12,24})
            {
                int term=duration;var type=kind;var error=Treaties.OfferError(state,0,other,type,term);
                p.Children.Add(Ui.Label($"{term} tur · obecna ocena partnera {Treaties.AcceptanceScore(state,other,type,term)} / wymagane 30. Ocena i środki będą sprawdzone ponownie.",12,Ui.Muted));
                var b=Ui.Button($"Wyślij: {Treaties.Name(type)} · {term} tur · 10 złota",()=>{var r=Treaties.Propose(state,other,type,term);Do(r);if(r.Success)TryAutoSave(false);ShowTreaties(r.Message);},true);b.IsEnabled=error==null;p.Children.Add(b);if(error!=null)p.Children.Add(Ui.Label(error,12,Ui.Red));
            }
        }
        p.Children.Add(Ui.Button("Wróć bez wysyłania",()=>ShowTreaties()));Modal(p,900);
    }
    private void ShowTreatyReply(int id)
    {
        var t=state.Treaties.Single(t=>t.Id==id);int other=t.From==0?t.To:t.From;var error=Treaties.RatifyError(state,t);
        var p=Ui.Stack(Ui.Eyebrow("RATYFIKACJA · OSTATNIA DECYZJA NALEŻY DO CIEBIE"),Ui.Label(Treaties.Name(t.Kind),29,Ui.Text,true),
            Ui.Label($"Partner: {state.Factions[other].Name}. Czas: {t.Duration} tur od podpisu. Depozyt: po {Treaties.Bond(t.Kind)} złota, pobrany z obu skarbców dopiero teraz. Podpis przed T{t.DeadlineTurn}.",15,Ui.Gold),
            Ui.Label("„Potwierdzamy otrzymane warunki. Wasza zgoda zabezpieczy wspólną przyszłość; odmowę również zapiszemy w historii rozmów.”",15,Ui.Text),
            Ui.Label(t.Kind==TreatyKind.Research?"Koszt kolejnej fazy: 2 złota dla każdego. Premia zależy od rzeczywistych dochodów naukowych obu państw; przy niedoborze jednej strony nie powstaje. Trzy jednostronne niedobory, wojna lub embargo oznaczają utratę depozytu sprawcy.":"Pokój jest zobowiązaniem zabezpieczonym majątkiem. Zwykły pakt lub rozejm nadal ma własne zasady. Ta gwarancja nie blokuje przycisku wojny, lecz rozlicza jej sprawcę.",14,Ui.Muted),
            Ui.Label("Przy dotrzymaniu terminu każda strona odzyskuje własny depozyt. Przy zerwaniu poszkodowany otrzymuje oba depozyty; sprawca traci 3 stabilności za badania lub 6 za gwarancję, do 8 prestiżu, 25 relacji i daje 30 krzywd. Pozostałe państwa tracą do sprawcy 8 zaufania na umowę. Opłaty za posłów i badania nie są zwracane.",13,Ui.Muted));
        var sign=Ui.Button("Ratyfikuj traktat #"+id,()=>{var r=Treaties.Ratify(state,id);Do(r);if(r.Success)TryAutoSave(false);ShowTreaties(r.Message);},true);sign.IsEnabled=error==null;p.Children.Add(sign);if(error!=null)p.Children.Add(Ui.Label(error,13,Ui.Red));
        p.Children.Add(Ui.Button("Odmów ratyfikacji #"+id,()=>{var r=Treaties.Reject(state,id);Do(r);if(r.Success)TryAutoSave(false);ShowTreaties(r.Message);}));p.Children.Add(Ui.Button("Zdecyduję później",()=>ShowTreaties()));Modal(p,880);
    }
    private void ConfirmTreatyBreak(int id)
    {
        var t=state.Treaties.Single(t=>t.Id==id);var p=Ui.Stack(Ui.Eyebrow("KONSEKWENCJE ZERWANIA"),Ui.Label("Czy wypowiadamy umowę?",29,Ui.Text,true),
            Ui.Label($"{Treaties.Name(t.Kind)} #{id}: oddasz {Treaties.Bond(t.Kind)} złota własnego depozytu partnerowi. Stabilność -{(t.Kind==TreatyKind.Research?3:6)}, prestiż do -8, relacje -25, krzywdy partnera +30, zaufanie pozostałych państw -8. Opłaty nie wrócą.",15,Ui.Gold));
        p.Children.Add(Ui.Button("Potwierdź zerwanie #"+id,()=>{var r=Treaties.Break(state,id);Do(r);if(r.Success)TryAutoSave(false);ShowTreaties(r.Message);},true));p.Children.Add(Ui.Button("Dotrzymamy umowy",()=>ShowTreaties()));Modal(p,800);
    }
}
