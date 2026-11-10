using Crown.Core;

namespace Crown.Desktop;

public sealed partial class MainWindow
{
    private void ShowIndustrialInvestments(string? notice=null)
    {
        var p=Ui.Stack(Ui.Eyebrow("ROZWÓJ ZAKŁADÓW I ODPOWIEDZIALNOŚĆ ZA UMOWY"),Ui.Label("Inwestycje przemysłowe",30,Ui.Text,true),
            Ui.Label("Rozbudowa wymaga 6 rzeczywiście wyprodukowanych partii wybranego wyrobu, w co najmniej 4 opłaconych fazach, przez 8 rozliczeń gospodarki. Kupione zapasy i sam podpis nie spełniają celu. Ukończenie dodaje jeden poziom urządzeń i +1 do limitu partii warsztatów; nadal potrzebujesz pracy i składników. Miasto może uzyskać dwa poziomy.",14,Ui.Muted));
        if(notice!=null)p.Children.Add(Ui.Label(notice,14,Ui.Gold));
        foreach(var c in state.Cities.Where(c=>c.Owner==0).OrderBy(c=>c.Id))
        {
            var plan=c.Factory;var card=Ui.Stack(Ui.Label(c.Name+$" · urządzenia {plan.Level}/2",24,Ui.Text,true),Ui.Label(plan.Report,14,Ui.Muted));
            if(plan.StartedTurn>0)card.Children.Add(Ui.Label("Podpis: "+CampaignCalendar.At(state,plan.StartedTurn),12,Ui.Muted));
            if(plan.Funding!=InvestmentFunding.None)
            {
                card.Children.Add(Ui.Label(IndustrialInvestment.Name(plan.Funding)+" · "+Manufacturing.Type(plan.Recipe).Name,18,Ui.Gold,true));
                Ui.Bar(card,$"Produkcja {plan.Batches}/6 partii · udane fazy {plan.ProductivePhases}/4 · minione {plan.Phases}/8",plan.ProductivePhases,4,Ui.Green);
                card.Children.Add(Ui.Label($"Podpis {CampaignCalendar.At(state,plan.StartedTurn)}; koniec po gospodarce {CampaignCalendar.At(state,plan.StartedTurn+7)}. Wydano {plan.Spent} złota; nadzór {plan.PhaseFee}/fazę.",13,Ui.Muted));
                card.Children.Add(Ui.Button("Rozważ odwołanie: "+c.Name,()=>ConfirmInvestmentCancel(c)));
            }
            else if(plan.RoyaltyRemaining>0)card.Children.Add(Ui.Label($"Koncesja: {plan.RoyaltyFee} złota/fazę, pozostało {plan.RoyaltyRemaining}/24 wpłat; wpłacono {plan.RoyaltyPaid}. Kolejne niedobory {plan.MissedRoyalties}/3. Trzeci oznacza utratę jednego poziomu urządzeń, niepokoje +10, lojalność -6 i prestiż -5.",14,Ui.Gold));
            foreach(var recipe in Manufacturing.Recipes.Where(r=>c.Buildings.Contains(r.Facility)))
                foreach(var funding in new[]{InvestmentFunding.Public,InvestmentFunding.Concession})
                {
                    var type=recipe.Recipe;var mode=funding;string? error=IndustrialInvestment.Error(state,c,type,mode);
                    var b=Ui.Button($"Rozmowa: {c.Name} · {recipe.Name} · {IndustrialInvestment.Name(mode)}",()=>ShowInvestmentTerms(c,type,mode));b.IsEnabled=error==null;card.Children.Add(b);if(error!=null)card.Children.Add(Ui.Label(error,12,Ui.Muted));
                }
            if(!Manufacturing.Facility(c))card.Children.Add(Ui.Label("Najpierw zbuduj warsztat narzędzi albo tkalnię.",13,Ui.Muted));
            card.Children.Add(Ui.Button("Otwórz warsztaty: "+c.Name,()=>ShowManufacturing()));p.Children.Add(Ui.Card(card));
        }
        p.Children.Add(Ui.Label("Nadzór jest płatny także w fazie bez produkcji. Brak środków zatrzymuje opłatę i zaliczanie partii. Nieudana umowa: bez zwrotu, niepokoje +6, lojalność -4. Odwołanie: bez zwrotu, niepokoje +4. Kolejna umowa po 12 rozliczeniach od poprzedniej decyzji. Zmiana właściciela zachowuje urządzenia, kończy dawne zobowiązania.",13,Ui.Muted));
        p.Children.Add(Ui.Button("Wróć do mapy",CloseModal));Modal(p,960);
    }
    private void ShowInvestmentTerms(City c,CraftRecipe recipe,InvestmentFunding funding)
    {
        int fee=IndustrialInvestment.Fee(state,c,funding),royalty=6+state.Player.Era;
        var p=Ui.Stack(Ui.Eyebrow("ROZMOWA Z RADĄ ZAKŁADÓW · "+c.Name),Ui.Label(IndustrialInvestment.Name(funding),28,Ui.Text,true),
            Ui.Label(funding==InvestmentFunding.Public?"„Państwo zapłaci za urządzenia, ale cechy muszą dowieść, że potrafią je wykorzystać. Bez żelaza, barwników i pracy nie ma rozbudowy.”":"„Wyłożymy własny kapitał. Zapłacicie mniej na początku, ale po uruchomieniu urządzeń będziecie przez pewien czas finansować naszą koncesję. Zobowiązanie pozostanie także po podpisie.”",16,Ui.Text),
            Ui.Label($"Wyrób: {Manufacturing.Type(recipe).Name}. Podpis {IndustrialInvestment.Signature(funding)} złota; nadzór {fee} za każde z 8 rozliczeń. Cel: 6 partii w co najmniej 4 finansowanych fazach. Nowy limit +1 partia po zakończeniu; bez darmowych składników ani pracy.",15,Ui.Gold),
            Ui.Label(funding==InvestmentFunding.Public?"Sukces: dobrobyt +5, mieszkańcy +4, uczeni +3; brak dalszych opłat.":$"Sukces: dobrobyt +5, rody +6, mieszkańcy -4. Następnie {royalty} złota/fazę do dokonania 24 wpłat. Brak opłaty nie skraca umowy. Trzy kolejne niedobory: spółka odbierze jeden poziom urządzeń; niepokoje +10, lojalność -6, prestiż -5.",14,Ui.Muted),
            Ui.Label("Nieudana umowa: wpłaty przepadają, niepokoje +6, lojalność -4. Zmieniony plan warsztatów lub brak surowca może uniemożliwić wykonanie celu.",14,Ui.Muted));
        var b=Ui.Button("Podpisz inwestycję",()=>{var r=IndustrialInvestment.Begin(state,c.Id,recipe,funding);Do(r);if(r.Success)TryAutoSave(false);ShowIndustrialInvestments(r.Message);},true);b.IsEnabled=IndustrialInvestment.Error(state,c,recipe,funding)==null;p.Children.Add(b);
        p.Children.Add(Ui.Button("Wróć bez podpisu",()=>ShowIndustrialInvestments()));Modal(p,850);
    }
    private void ConfirmInvestmentCancel(City c)
    {
        var p=Ui.Stack(Ui.Label("Odwołanie inwestycji",27,Ui.Text,true),Ui.Label($"{c.Name}: wydane {c.Factory.Spent} złota nie wróci do skarbu. Nie otrzymasz nowych urządzeń, a niepokoje wzrosną o 4.",15,Ui.Gold));
        p.Children.Add(Ui.Button("Odwołaj umowę inwestycyjną",()=>{var r=IndustrialInvestment.Cancel(state,c.Id);Do(r);if(r.Success)TryAutoSave(false);ShowIndustrialInvestments(r.Message);}));p.Children.Add(Ui.Button("Kontynuuj inwestycję",()=>ShowIndustrialInvestments()));Modal(p,720);
    }
}
