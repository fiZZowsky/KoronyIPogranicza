using Crown.Core;

namespace Crown.Desktop;

public sealed partial class MainWindow
{
    private void ShowManufacturing(string? notice=null)
    {
        var p=Ui.Stack(Ui.Eyebrow("RZEMIOSŁO I SIEĆ WYMIANY"),Ui.Label("Warsztaty i łańcuchy produkcji",30,Ui.Text,true),
            Ui.Label("Żelazo → narzędzia. Barwniki → tkaniny. Warsztaty pobierają składniki z bezpiecznej sieci własnych miast i część pracy pozostałej po dzielnicach oraz zaopatrzeniu wojskowym. Przerwana droga lub blokada odcina wspólne magazyny. Wyroby powstają po rozliczeniu rynku; mieszkańcy i karawany skorzystają z nich w następnej turze.",14,Ui.Muted));
        if(notice!=null)p.Children.Add(Ui.Label(notice,14,Ui.Gold));
        foreach(var c in state.Cities.Where(c=>c.Owner==0).OrderBy(c=>c.Id))
        {
            var card=Ui.Stack(Ui.Label(c.Name,24,Ui.Text,true),Ui.Label($"Żelazo {c.MarketStocks[TradeGood.Iron]:0.##} · barwniki {c.MarketStocks[TradeGood.Dyes]:0.##}\nNarzędzia {c.MarketStocks[TradeGood.Tools]:0.##} · tkaniny {c.MarketStocks[TradeGood.Textiles]:0.##} · pojemność {Markets.Capacity(c):0} na towar",14,Ui.Gold));
            foreach(string id in new[]{Manufacturing.Toolmakers,Manufacturing.Weavers,Manufacturing.Guildhall})
            {
                var project=Crown.Core.Content.Project(id);
                if(c.Buildings.Contains(id)){card.Children.Add(Ui.Label(project.Name+" · działa",13,Ui.Green));continue;}
                string? error=ProductionPlanning.ProjectError(state,c,id);var b=Ui.Button("Buduj: "+project.Name+" · "+c.Name,()=>{var r=Engine.SetProject(state,c.Id,id);Do(r);if(r.Success)TryAutoSave(false);ShowManufacturing(r.Message);});b.IsEnabled=error==null;card.Children.Add(b);if(error!=null)card.Children.Add(Ui.Label(error,12,Ui.Muted));
            }
            if(Manufacturing.Facility(c))
            {
                double potential=Engine.CityYields(state,c).Production,district=Districts.Allocation(state,c,potential),supply=FieldLogistics.Allocation(state,c,Math.Max(0,potential-district));
                var quote=Manufacturing.Quote(state,c,Math.Max(0,potential-district-supply));var type=Manufacturing.Type(c.IndustryRecipe);
                card.Children.Add(Ui.Label($"Plan: {type.Name} · nakład {c.IndustryShare}% · limit {Manufacturing.Limit(state,c)} partii/turę",15,Ui.Gold));
                card.Children.Add(Ui.Label(quote.Error??$"Obecna prognoza: {quote.Batches} partii, {quote.Batches*type.OutputAmount} wyrobu, {quote.Work:0} pracy. Rynek przed produkcją może zmienić dostępne składniki.",12,Ui.Muted));
                foreach(var input in quote.Inputs)card.Children.Add(Ui.Label($"Składniki z {state.Cities.Single(x=>x.Id==input.CityId).Name}: {input.Quantity:0.##} {Markets.Name(input.Good).ToLowerInvariant()}",12,Ui.Muted));
                card.Children.Add(Ui.Button("Plan warsztatów: "+c.Name,()=>ShowCraftPolicy(c),true));
                if(c.LastIndustryTurn>0)
                {
                    var last=Manufacturing.Type(c.LastIndustryRecipe);card.Children.Add(Ui.Label($"Ostatnia produkcja T{c.LastIndustryTurn}: {c.LastIndustryBatches} partii · {c.LastIndustryBatches*last.OutputAmount} {last.Name.ToLowerInvariant()} · praca {c.LastIndustryWork:0}",13,Ui.Gold));card.Children.Add(Ui.Label(c.LastIndustryMessage,12,Ui.Muted));
                    foreach(var input in c.LastIndustryInputs)card.Children.Add(Ui.Label($"Zużyto z {state.Cities.Single(x=>x.Id==input.CityId).Name}: {input.Quantity:0.##} {Markets.Name(input.Good).ToLowerInvariant()}",12,Ui.Muted));
                }
            }
            if(c.Buildings.Contains(Manufacturing.Guildhall))card.Children.Add(Ui.Label($"Dom cechów — ostatnie dostawy: produkcja +{Manufacturing.ProductionBonus(c)*100:0.#}%, bazowa kultura +{Manufacturing.CultureBonus(c):0.#}, dobrobyt +{Manufacturing.ProsperityBonus(c)}. Premie wymagają rzeczywistego pokrycia popytu.",13,Ui.Green));
            card.Children.Add(Ui.Button("Plan produkcji: "+c.Name,()=>ShowProductionQueue(c)));card.Children.Add(Ui.Button("Pokaż warsztaty: "+c.Name,()=>{CloseModal();Select(c.Tile);map.Center(c.Tile);}));p.Children.Add(Ui.Card(card));
        }
        p.Children.Add(Ui.Label("Dom cechów zużywa narzędzia i tkaniny. Pokrycie popytu daje do +15% produkcji, +3 bazowej kultury i +3 dobrobytu. Warsztaty i Dom cechów kosztują po 1,5 złota utrzymania plus administrację. Nadwyżki można sprzedawać tymi samymi kontraktami co surowce. Cena narzędzi i tkanin uwzględnia średnią cenę obu stron oraz marżę 15%; zapłata przy wysyłce, dostawa po rzeczywistym przejazdzie karawany lądowej. Embargo, granice, eskorta i straty działają jak wcześniej.",14,Ui.Muted));
        p.Children.Add(Ui.Button("Podpisz kontrakt na wyroby",ShowTradeContract));p.Children.Add(Ui.Button("Wróć do mapy",CloseModal));Modal(p,920);
    }
    private void ShowCraftPolicy(City c)
    {
        var p=Ui.Stack(Ui.Eyebrow("ROZMOWA Z MISTRZYNIĄ CECHÓW"),Ui.Label(c.Name+" · czym zajmą się warsztaty?",27,Ui.Text,true),
            Ui.Label("„Surowce można sprzedać od razu albo przetworzyć. Wyroby wesprą miejscowe cechy lub trafią na eksport, ale praca rzemieślników spowolni budowę i rekrutację. Wybierzmy jeden wyrób.”",15,Ui.Text),
            Ui.Label("Plan można zmienić raz w turze. Nakład liczony jest z pracy pozostałej po dzielnicach i zaopatrzeniu. Zbyt mały budżet, brak składników lub pełny magazyn nie marnują pracy; trafia ona do głównego projektu.",13,Ui.Muted));
        foreach(var recipe in Manufacturing.Recipes.Where(r=>c.Buildings.Contains(r.Facility)))
        {
            var type=recipe;p.Children.Add(Ui.Label($"{type.Name}: {type.InputAmount} {Markets.Name(type.Input).ToLowerInvariant()} + {type.Work} pracy → {type.OutputAmount} wyrobu",16,Ui.Gold));
            foreach(int share in new[]{0,25,50})
            {
                int value=share;var error=Manufacturing.PolicyError(state,c,type.Recipe,value);var b=Ui.Button($"{type.Name} · nakład {value}%",()=>{var r=Manufacturing.SetPolicy(state,c.Id,type.Recipe,value);Do(r);if(r.Success)TryAutoSave(false);ShowManufacturing(r.Message);},true);b.IsEnabled=error==null;p.Children.Add(b);if(error!=null)p.Children.Add(Ui.Label(error,12,Ui.Muted));
            }
        }
        p.Children.Add(Ui.Button("Wróć do warsztatów",()=>ShowManufacturing()));Modal(p,800);
    }
}
