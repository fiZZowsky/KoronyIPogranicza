using Crown.Core;
using Catalog = Crown.Core.Content;
using System.Windows;
using System.Windows.Controls;

namespace Crown.Desktop;

public sealed partial class MainWindow
{
    private void RealmPanel()
    {
        var city = state.Cities.FirstOrDefault(c => c.Tile == selectedTile);
        var tile = selectedTile >= 0 ? state.Tiles[selectedTile] : null;
        if (city?.Owner == 0)
        {
            TitlePanel("MIASTO MARCHII", city.Name,
                $"{city.Population} mieszkańców · {city.Health:0}% obrony · niepokoje {city.Unrest}%");
            var y = Engine.CityYields(state, city);
            var province = Regions.Status(state, city);
            side.Children.Add(Ui.Label($"Żywność {Ui.Signed(province.FoodBalance)}   Produkcja +{y.Production:0.0}", 13, Ui.Green));
            side.Children.Add(Ui.Label($"Złoto +{y.Gold:0.0}   Nauka +{y.Science:0.0}   Kultura +{y.Culture:0.0}", 11, Ui.Muted));
            bool connected = Logistics.For(state, 0).ConnectedCities.Contains(city.Id);
            Ui.Gap(side, 10);
            side.Children.Add(Ui.Label(connected ? "W sieci drogowej stolicy" : "Brak bezpiecznego połączenia ze stolicą", 11, connected ? Ui.Green : Ui.Gold));
            side.Children.Add(Ui.Button("Szlaki i bilans skarbu   →", () => { tab = "logistics"; RefreshSide(); }));
            side.Children.Add(Ui.Button($"Mieszkańcy · dobrobyt {city.Prosperity}%", () => ShowProvince(city)));
            Ui.Bar(side, $"Wzrost miasta: {city.Food:0} / {22 + city.Population * 12}", city.Food, 22 + city.Population * 12, Ui.Green);
            var project = Catalog.Project(city.Project);
            int turns = (int)Math.Ceiling(Math.Max(0, project.Cost - city.Production) / Math.Max(1, y.Production));
            var work = Ui.Stack(Ui.Eyebrow("W REALIZACJI"), Ui.Label(project.Name, 22, Ui.Text, true), Ui.Label(project.Description, 12, Ui.Muted));
            Ui.Gap(work, 10);
            Ui.Bar(work, $"{city.Production:0} / {project.Cost} produkcji · ok. {turns} tur", city.Production, project.Cost);
            work.Children.Add(Ui.Button("Wybierz projekt   →", () => ShowProjects(city)));
            side.Children.Add(Ui.Card(work));
            if (city.Buildings.Count > 0)
            {
                side.Children.Add(Ui.Eyebrow("INSTYTUCJE I BUDYNKI"));
                side.Children.Add(Ui.Label(string.Join(" · ", city.Buildings.Select(id => Catalog.Project(id).Name)), 12, Ui.Muted));
            }
        }
        else if (tile?.Explored == true)
        {
            TitlePanel("ZIEMIE DOLINY", city?.Name ?? Catalog.TerrainName(tile.Terrain),
                $"{(tile.Owner < 0 ? "Ziemia niczyja" : state.Factions[tile.Owner].Name)} · ({tile.X}, {tile.Y})");
            side.Children.Add(Ui.Label($"Zasób: {Catalog.ResourceName(tile.Resource)}{(tile.River ? " · rzeka" : "")}", 13, Ui.Gold));
            var y = Engine.TileYields(tile, state.Player);
            side.Children.Add(Ui.Label($"Potencjał: {y.Food:0} żywności · {y.Production:0} produkcji · {y.Gold:0} złota", 12, Ui.Muted));
            Ui.Gap(side);
            if (tile.Passable && city == null)
            {
                if (tile.Road) side.Children.Add(Ui.Label($"Droga · opiekun: {state.Factions[tile.RoadOwner].Name}", 12, Ui.Gold));
                else if (tile.Owner is -1 or 0)
                    side.Children.Add(Ui.Button("Zbuduj drogę · 25 złota", () => Do(Engine.BuildRoad(state, tile.Id))));
            }
            if (tile.Owner == 0 && city == null && tile.Passable)
            {
                if (tile.Improvement == Improvement.None)
                {
                    side.Children.Add(Ui.Eyebrow("ZAGOSPODAROWANIE"));
                    side.Children.Add(Ui.Button("Farma · 35 złota", () => Do(Engine.Improve(state, tile.Id, Improvement.Farm))));
                    side.Children.Add(Ui.Button("Kopalnia · 45 złota", () => Do(Engine.Improve(state, tile.Id, Improvement.Mine))));
                    side.Children.Add(Ui.Button("Placówka handlowa · 55 złota", () => Do(Engine.Improve(state, tile.Id, Improvement.TradingPost))));
                    side.Children.Add(Ui.Label("Miasto wykorzystuje najlepsze pola w promieniu 2. Droga może współistnieć z farmą lub kopalnią.", 11, Ui.Muted));
                }
                else side.Children.Add(Ui.Label("Pole zostało zagospodarowane.", 12, Ui.Green));
            }
        }
        else TitlePanel("MARCHIE VEYRU", "Nieznane ziemie", "Wyślij zwiadowców, aby odkryć zasoby, ruiny i przyszłe miejsca osadnictwa.");

        var units = state.Units.Where(u => u.Owner == 0 && u.Tile == selectedTile).ToArray();
        if (units.Length > 0)
        {
            side.Children.Add(Ui.Rule());
            side.Children.Add(Ui.Eyebrow("ODDZIAŁY NA POLU"));
            foreach (var u in units)
            {
                side.Children.Add(Ui.Button($"{(u.Id == selectedUnit ? "▸ " : "")}{Catalog.UnitName(u.Kind)} · {u.Actions:0.#}/{u.MaxActions} ruchu",
                    () => { selectedUnit = u.Id; Refresh(); }));
                if (u.Id != selectedUnit) continue;
                side.Children.Add(Ui.Label($"Zdrowie {u.Health:0}% · siła {Engine.Strength(state, u):0} · doświadczenie {u.Experience}", 11, Ui.Muted));
                UnitSupply(u);
                if (u.Destination is int goal)
                {
                    side.Children.Add(Ui.Label($"Marsz do ({state.Tiles[goal].X}, {state.Tiles[goal].Y}). Kontynuacja po zakończeniu tury.", 12, Ui.Gold));
                    side.Children.Add(Ui.Button("Anuluj rozkaz marszu", () => Do(Engine.CancelOrder(state, u.Id))));
                }
                if (tile?.Road == true && Engine.AtWar(state, 0, tile.RoadOwner))
                    side.Children.Add(Ui.Button("Zniszcz wrogą drogę · +20 złota", () => Do(Engine.PillageRoad(state, u.Id))));
                if (u.Kind == UnitKind.Settler)
                    side.Children.Add(Ui.Button("Załóż miasto w tym miejscu", () => Do(Engine.FoundCity(state, u.Id)), true));
                Ui.Gap(side, 8);
            }
            side.Children.Add(Ui.Label("PPM: marsz do celu lub podgląd ataku. Drogi: 0,5 punktu ruchu między połączonymi odcinkami. Dalszy marsz wykonuje się automatycznie.", 11, Ui.Muted));
        }
        side.Children.Add(Ui.Rule());
        side.Children.Add(Ui.Eyebrow("TWOJE MIASTA"));
        foreach (var c in state.Cities.Where(c => c.Owner == 0))
            side.Children.Add(Ui.Button($"{c.Name} · {c.Population} mieszkańców", () => { Select(c.Tile); map.Center(c.Tile); }));
        mini = new MiniMap { State = state, Map = map, Margin = new Thickness(0, 12, 0, 0) };
        side.Children.Add(mini);
    }

    private void UnitSupply(Unit u)
    {
        var supply = Logistics.For(state, u.Owner);
        bool supplied = Engine.Supplied(state, u);
        string source = supply.SourceCities.TryGetValue(u.Tile, out int id) ? state.Cities.First(c => c.Id == id).Name : "brak trasy";
        string cost = supply.Costs.TryGetValue(u.Tile, out double distance) ? $"{distance:0.#} / {Logistics.Range(state, u.Owner):0}" : "—";
        string effect = supplied ? "Zaopatrzenie zapewnione" : u.Kind is UnitKind.Scout or UnitKind.Settler ?
            "Brak dostaw: osłabienie siły; ten oddział nie ponosi strat marszowych" : "Brak dostaw: siła ×0,72; straty 5 zdrowia/turę";
        side.Children.Add(Ui.Label($"Dostawy: {source} · koszt trasy {cost}\n{effect}", 11, supplied ? Ui.Green : Ui.Red));
        Ui.Gap(side, 10);
    }

    private void LogisticsPanel()
    {
        TitlePanel("INFRASTRUKTURA I SKARB", "Szlaki i dostawy", "Bezpieczne drogi łączą miasta i wydłużają zasięg dostaw. Wróg może przeciąć trasę lub ją zniszczyć.");
        var economy = Engine.Economy(state, 0);
        var budget = Ui.Stack(Ui.Eyebrow("BILANS NA TURĘ"));
        foreach (var row in new (string Label, double Amount)[] { ("Miasta", economy.CityIncome), ("Traktaty", economy.TradeIncome),
            ("Armia", -economy.ArmyCost), ("Administracja", -economy.AdministrationCost), ("Drogi", -economy.RoadCost) })
            budget.Children.Add(Ui.Label($"{row.Label}  {Ui.Signed(row.Amount)}", 13, row.Amount < 0 ? Ui.Muted : Ui.Green));
        budget.Children.Add(Ui.Rule());
        budget.Children.Add(Ui.Label($"Saldo skarbu  {Ui.Signed(economy.Net)}", 20, Ui.Gold, true));
        side.Children.Add(Ui.Card(budget));
        side.Children.Add(Ui.Button(map.ShowSupply ? "Ukryj nakładkę dostaw" : "Pokaż nakładkę dostaw", () =>
        { map.ShowSupply = !map.ShowSupply; if(map.ShowSupply)map.ShowPopulation=false; map.InvalidateVisual(); RefreshSide(); }));
        side.Children.Add(Ui.Label($"Zielony: dostawy w zasięgu {Logistics.Range(state, 0):0}. Czerwony: poza zasięgiem. Pomarańczowy: blokada wojskowa. Zima podnosi koszt transportu poza drogami o 25%.", 11, Ui.Muted));
        side.Children.Add(Ui.Rule());
        side.Children.Add(Ui.Eyebrow("SIEĆ STOLICY"));
        var cities = state.Cities.Where(c => c.Owner == 0).OrderBy(c => c.Id).ToArray();
        var connected = Logistics.For(state, 0).ConnectedCities;
        foreach (var c in cities)
        {
            bool capital = c.Id == cities[0].Id;
            var p = Ui.Stack(Ui.Label(c.Name, 21, Ui.Text, true));
            p.Children.Add(Ui.Label(capital ? "Ośrodek administracji" : connected.Contains(c.Id) ? $"Połączenie bezpieczne · +{3+(state.Player.Flags.Contains("road_tolls")?2:0)} bazowego złota, +{1+(state.Player.Flags.Contains("open_roads")?1:0)} nauki" : "Połączenie nie działa", 12, connected.Contains(c.Id) ? Ui.Green : Ui.Gold));
            if (!capital)
            {
                var plan = Logistics.PlanRoad(state, c.Id);
                if (plan.Error != null) p.Children.Add(Ui.Label(plan.Error, 11, Ui.Muted));
                else
                {
                    Ui.Gap(p, 10);
                    p.Children.Add(Ui.Button("Pokaż przebieg trasy", () => { map.PlannedRoad = plan.Path; map.Center(c.Tile); map.InvalidateVisual(); }));
                    if (plan.NewTiles > 0)
                    {
                        var b = Ui.Button($"Połącz ze stolicą · {plan.Cost} złota", () => { map.PlannedRoad = []; Do(Engine.ConnectCity(state, c.Id)); });
                        b.IsEnabled = state.Player.Technologies.Contains("pottery") && state.Player.Gold >= plan.Cost;
                        p.Children.Add(b);
                        p.Children.Add(Ui.Label($"{plan.NewTiles} nowych odcinków · +{plan.NewTiles * Logistics.RoadUpkeep:0.00} utrzymania/turę", 11, Ui.Muted));
                    }
                }
            }
            side.Children.Add(Ui.Card(p));
        }
        side.Children.Add(Ui.Label("Budowa wymaga Garncarstwa. Droga kosztuje 25 złota za pole; istniejące odcinki nie są kupowane ponownie. Wrogie oddziały blokują swoje pole i sąsiedztwo. Własne wojsko może osłonić zajmowane pole.", 11, Ui.Muted));
        side.Children.Add(Ui.Rule());
        var isolated = state.Units.Where(u => u.Owner == 0 && !Engine.Supplied(state, u)).ToArray();
        side.Children.Add(Ui.Eyebrow($"ODDZIAŁY BEZ DOSTAW: {isolated.Length}"));
        foreach (var u in isolated)
            side.Children.Add(Ui.Button($"{Catalog.UnitName(u.Kind)} · ({state.Tiles[u.Tile].X}, {state.Tiles[u.Tile].Y})", () => { Select(u.Tile); selectedUnit = u.Id; map.Center(u.Tile); Refresh(); }));
    }

    private void ShowCombat(int target)
    {
        var u = state.Units.FirstOrDefault(u => u.Id == selectedUnit && u.Owner == 0);
        if (u == null) return;
        var preview = Engine.PreviewAttack(state, u.Id, target);
        if (!preview.Valid) { Toast(preview.Reason, false); return; }
        var p = Ui.Stack(Ui.Eyebrow("PODGLĄD STARCIA"), Ui.Label($"{preview.Attacker} → {preview.Target}", 30, Ui.Text, true));
        Ui.Gap(p, 18);
        var columns = new System.Windows.Controls.Primitives.UniformGrid { Columns = 2 };
        var own = Ui.Stack(Ui.Eyebrow("TWÓJ ODDZIAŁ"), Ui.Label($"Siła {preview.AttackStrength:0.0}", 22, Ui.Gold, true));
        Ui.Bar(own, $"Zdrowie po ataku: {u.Health - preview.DamageToAttacker:0.0} / 100", u.Health - preview.DamageToAttacker, 100, Ui.Green);
        own.Children.Add(Ui.Label($"Straty własne: {preview.DamageToAttacker:0.0}", 14, Ui.Red));
        var foe = Ui.Stack(Ui.Eyebrow("PRZECIWNIK"), Ui.Label($"Obrona {preview.DefenseStrength:0.0}", 22, Ui.Gold, true));
        Ui.Bar(foe, $"Zdrowie po ataku: {preview.TargetHealth - preview.DamageToTarget:0.0} / 100", preview.TargetHealth - preview.DamageToTarget, 100, Ui.Red);
        foe.Children.Add(Ui.Label($"Zadane obrażenia: {preview.DamageToTarget:0.0}", 14, Ui.Green));
        own.Margin = new Thickness(0, 0, 22, 0);
        columns.Children.Add(own); columns.Children.Add(foe); p.Children.Add(columns);
        p.Children.Add(Ui.Rule());
        foreach (string modifier in preview.Modifiers ?? []) p.Children.Add(Ui.Label("• " + modifier, 13, Ui.Muted));
        Ui.Gap(p, 18);
        if (preview.CanCapture) p.Children.Add(Ui.Label("Ten atak przejmie miasto.", 16, Ui.Green));
        if (preview.DamageToAttacker >= u.Health) p.Children.Add(Ui.Label("Twój oddział zostanie zniszczony.", 16, Ui.Red));
        p.Children.Add(Ui.Label("Wynik oblicza ten sam model co walka. Atak zużyje wszystkie pozostałe punkty ruchu.", 12, Ui.Muted));
        Ui.Gap(p, 16);
        p.Children.Add(Ui.Button("Wykonaj atak", () => { CloseModal(); ApplyOrder(Engine.Move(state, u.Id, target)); }, true));
        p.Children.Add(Ui.Button("Wycofaj rozkaz", CloseModal));
        Modal(p, 760);
    }

    private void ApplyOrder(Result result)
    {
        var unit = state.Units.FirstOrDefault(u => u.Id == selectedUnit);
        if (unit != null) selectedTile = unit.Tile; else selectedUnit = -1;
        Do(result);
    }
}
