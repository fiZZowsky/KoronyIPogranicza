using Crown.Core;
using System.Windows;
using System.Windows.Controls;

namespace Crown.Desktop;

public sealed partial class MainWindow
{
    private void ProvincesPanel()
    {
        TitlePanel("LUDZIE I MIEJSCA", "Życie prowincji", "Żywność, mieszkania i opieka wpływają na dobrobyt. Rodziny mogą przenieść się drogami do lepiej przygotowanego miasta.");
        var cities = state.Cities.Where(c => c.Owner == 0).OrderBy(c => c.Id).ToArray();
        var food = Regions.PlanFood(state, 0);
        side.Children.Add(Ui.Button(map.ShowPopulation ? "Ukryj dobrobyt i przepływy" : "Pokaż dobrobyt i przepływy", () =>
        { map.ShowPopulation = !map.ShowPopulation; if (map.ShowPopulation) map.ShowSupply = false; map.InvalidateVisual(); RefreshSide(); }));
        side.Children.Add(Ui.Label($"Mieszkańcy: {cities.Sum(c => c.Population)} · miasta: {cities.Length}\nŻywność przesyłana drogami: {food.Shipments.Sum(x => x.Amount):0.#} / turę", 12, Ui.Gold));
        side.Children.Add(Ui.Rule());
        foreach (var c in cities.OrderBy(c => c.Prosperity).ThenBy(c => c.Id))
        {
            var status = Regions.Status(state, c, food);
            var p = Ui.Stack(Ui.Label(c.Name, 23, Ui.Text, true), Ui.Label($"{c.Population} mieszkańców · {Regions.FocusName(c.Focus)}", 12, Ui.Muted));
            Ui.Gap(p, 12);
            Ui.Bar(p, $"Dobrobyt {c.Prosperity}% → cel {status.ProsperityTarget}%", c.Prosperity, 100, c.Prosperity < 40 ? Ui.Red : Ui.Green);
            p.Children.Add(Ui.Label($"Mieszkania {c.Population}/{status.Housing} · opieka {c.Population}/{status.Sanitation}\nBilans żywności {Ui.Signed(status.FoodBalance)} · niepokoje {c.Unrest}%", 12,
                status.Pressure > 0 ? Ui.Gold : Ui.Muted));
            if (status.Imports > 0 || status.Exports > 0) p.Children.Add(Ui.Label($"Import {status.Imports:0.#} · eksport {status.Exports:0.#}", 11, Ui.Green));
            Ui.Gap(p, 10);
            p.Children.Add(Ui.Button("Zarządzaj: " + c.Name, () => { map.Center(c.Tile); ShowProvince(c); }));
            side.Children.Add(Ui.Card(p));
        }
        side.Children.Add(Ui.Eyebrow("WYMIANA ŻYWNOŚCI"));
        if (food.Shipments.Count == 0) side.Children.Add(Ui.Label("Brak bieżących przesyłek. Import wymaga niedoboru oraz bezpiecznego połączenia z miastem mającym nadwyżkę.", 12, Ui.Muted));
        foreach (var shipment in food.Shipments)
        {
            var from = state.Cities.First(c => c.Id == shipment.Source); var to = state.Cities.First(c => c.Id == shipment.Destination);
            side.Children.Add(Ui.Label($"{from.Name} → {to.Name}: {shipment.Amount:0.#}", 12, Ui.Green));
        }
        side.Children.Add(Ui.Rule());
        side.Children.Add(Ui.Eyebrow("OSTATNIE MIGRACJE"));
        var movements = state.Migrations.Where(m => m.Owner == 0).Reverse().Take(8).ToArray();
        if (movements.Length == 0) side.Children.Add(Ui.Label("Nie zanotowano jeszcze migracji. Potrzebne są połączenie drogowe, wyraźnie lepsze warunki i wolne miejsca w mieście docelowym.", 12, Ui.Muted));
        foreach (var m in movements)
            side.Children.Add(Ui.Label($"T{m.Turn} · {state.Cities.First(c => c.Id == m.Source).Name} → {state.Cities.First(c => c.Id == m.Destination).Name}: {m.People} mieszkaniec", 12, Ui.Muted));
    }

    private void ShowProvince(City city)
    {
        var food = Regions.PlanFood(state, 0); var status = Regions.Status(state, city, food);
        var p = Ui.Stack(Ui.Eyebrow("SPRAWY MIESZKAŃCÓW"), Ui.Label(city.Name, 35, Ui.Text, true));
        Ui.Gap(p, 14);
        Ui.Bar(p, $"Dobrobyt {city.Prosperity}% · docelowo {status.ProsperityTarget}%", city.Prosperity, 100, city.Prosperity < 40 ? Ui.Red : Ui.Green);
        var needs = new System.Windows.Controls.Primitives.UniformGrid { Columns = 3, Margin = new Thickness(0, 0, 0, 16) };
        needs.Children.Add(Ui.Stack(Ui.Eyebrow("MIESZKANIA"), Ui.Label($"{city.Population} / {status.Housing}", 25, city.Population > status.Housing ? Ui.Red : Ui.Green, true)));
        needs.Children.Add(Ui.Stack(Ui.Eyebrow("OPIEKA I HIGIENA"), Ui.Label($"{city.Population} / {status.Sanitation}", 25, city.Population > status.Sanitation ? Ui.Red : Ui.Green, true)));
        needs.Children.Add(Ui.Stack(Ui.Eyebrow("NIEPOKOJE"), Ui.Label(city.Unrest + "%", 25, city.Unrest > 30 ? Ui.Red : Ui.Gold, true)));
        p.Children.Add(needs);
        p.Children.Add(Ui.Label($"Żywność miejscowa {Ui.Signed(food.Raw[city.Id])} · import +{status.Imports:0.#} · eksport -{status.Exports:0.#} · bilans {Ui.Signed(status.FoodBalance)}\nZapasy: {city.Food:0.#}; presja potrzeb: {status.Pressure}. Dobrobyt zbliża się do celu o maksymalnie 2 punkty na turę.", 13, Ui.Muted));
        Ui.Gap(p, 8);
        p.Children.Add(Ui.Label("Dostępne towary: " + (status.Goods.Count == 0 ? "brak" : string.Join(", ", status.Goods)), 13, Ui.Gold));
        p.Children.Add(Ui.Rule());
        p.Children.Add(Ui.Eyebrow("KIERUNEK ROZWOJU"));
        var focus = Ui.Combo(["Równowaga", "Rolnictwo · +20% żywności, -10% produkcji", "Rzemiosło · +20% produkcji, -10% żywności, opieka -1", "Nauka · +25% nauki, -10% złota"], (int)city.Focus);
        p.Children.Add(focus);
        var feedback = Ui.Label("", 12, Ui.Red);
        var apply = Ui.Button("Zmień kierunek · 15 złota", () =>
        {
            var result = Engine.SetCityFocus(state, city.Id, (CityFocus)focus.SelectedIndex);
            if (!result.Success) { feedback.Text = result.Message; return; }
            Do(result); ShowProvince(city);
        });
        apply.IsEnabled = city.LastFocusTurn != state.Turn && state.Player.Gold >= Regions.FocusPrice;
        p.Children.Add(apply); p.Children.Add(feedback);
        int remaining = Math.Max(0, 5 - (state.Turn - city.LastAidTurn));
        var aid = Ui.Button(remaining > 0 ? $"Pomoc dostępna za {remaining} tur" : "Pomoc żywnościowa · 50 złota", () =>
        {
            var result = Engine.GrantCityAid(state, city.Id);
            if (!result.Success) { feedback.Text = result.Message; return; }
            Do(result); ShowProvince(city);
        });
        aid.IsEnabled = remaining == 0 && state.Player.Gold >= Regions.AidPrice;
        p.Children.Add(aid);
        p.Children.Add(Ui.Label("Pomoc: +25 zapasów, -8 niepokojów, +4 dobrobytu; raz na pięć tur. Dzielnica mieszkalna dodaje 5 miejsc, akwedukt 2 miejsca i 4 opieki, lecznica 6 opieki.", 12, Ui.Muted));
        Ui.Gap(p, 12);
        p.Children.Add(Ui.Button("Wybierz budowę miejską", () => ShowProjects(city)));
        if (city.Charters.Count > 0)
        {
            p.Children.Add(Ui.Rule()); p.Children.Add(Ui.Eyebrow("PRAWA TEJ PROWINCJI"));
            foreach (string charter in city.Charters) p.Children.Add(Ui.Label(ProvincialEvents.CharterName(charter), 13, Ui.Gold));
        }
        Ui.Gap(p, 12); p.Children.Add(Ui.Button("Wróć do mapy", CloseModal));
        Modal(p, 800);
    }
}
