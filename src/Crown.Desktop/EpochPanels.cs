using Crown.Core;
using Catalog = Crown.Core.Content;
using System.Windows.Controls;

namespace Crown.Desktop;

public sealed partial class MainWindow
{
    private void ShowEpochs(int? selected = null)
    {
        var f = state.Player; int index = selected ?? f.Era; var e = EpochCatalog.All[index];
        var p = Ui.Stack(Ui.Eyebrow("HISTORIA I MOŻLIWA PRZYSZŁOŚĆ"), Ui.Label("Epoki i rozwój świata", 30, Ui.Text, true),
            Ui.Label($"{Catalog.Era(f.Era)} · odkrycia {f.Technologies.Count}/90 · tura {state.Turn}/{state.MaxTurns}", 16, Ui.Gold, true));
        if (!state.EpochCampaign)
            p.Children.Add(Ui.Button("Rozszerz kampanię do 18 epok i 1600 tur", () => { var r = Engine.ExtendCampaign(state); Do(r); if (r.Success) { TryAutoSave(false); ShowEpochs(); } }, true));
        var select = Ui.Combo(EpochCatalog.All.Select(x => $"{x.Index + 1:00} · {x.Name}" + (x.Speculative ? " · możliwa przyszłość" : "")).ToArray(), index);
        select.SelectionChanged += (_, _) => ShowEpochs(select.SelectedIndex); p.Children.Add(select);
        p.Children.Add(Ui.Label(e.Name, 28, Ui.Text, true)); p.Children.Add(Ui.Label(e.Period + "\n" + e.Description, 14, Ui.Muted));
        p.Children.Add(Ui.Label("Postęp zależy od wiedzy. Kalendarz jest umowny i uwzględnia najbardziej zaawansowane państwo. Każda epoka ma cztery dziedziny i odkrycie zamykające dorobek. Następny etap wymaga zamknięcia poprzedniego. Technologie i instytucje zachowujesz.", 12, Ui.Muted));
        foreach (string id in e.Technologies)
        {
            var t = Catalog.Tech(id); bool known = f.Technologies.Contains(id); string? error = Epochs.ResearchError(state, f, t);
            var card = Ui.Stack(Ui.Label(t.Name + (known ? " · odkryte" : f.Research == id ? " · badane" : ""), 19, known ? Ui.Green : Ui.Text, true),
                Ui.Label($"{t.Cost} nauki · {t.Description}", 12, Ui.Muted));
            if (!known)
            {
                var button = Ui.Button("Badaj: " + t.Name, () => { var r = Engine.SetResearch(state, id); Do(r); if (r.Success) { TryAutoSave(false); ShowEpochs(index); } });
                button.IsEnabled = state.Ending == null && error == null; card.Children.Add(button); if (error != null) card.Children.Add(Ui.Label(error, 11, Ui.Gold));
            }
            p.Children.Add(Ui.Card(card));
        }
        p.Children.Add(Ui.Eyebrow("INFRASTRUKTURA TEJ EPOKI"));
        foreach (var b in EpochCatalog.Buildings.Where(b => b.Era == index))
            p.Children.Add(Ui.Label($"{b.Name} · {b.Cost} produkcji\n{EpochCatalog.Describe(b)}\nWymaga: {Catalog.Tech(b.Technology).Name}", 13, Ui.Muted));
        p.Children.Add(Ui.Eyebrow("WOJSKA I MODERNIZACJA"));
        foreach (var m in UnitModels.All.Where(m => m.Era == index))
            p.Children.Add(Ui.Label($"{m.Name} · siła {m.Strength} · ruch {m.Moves} · zasięg {m.Range} · koszt {m.Cost}\nWymaga: {Catalog.Tech(m.Technology).Name}. Nowe modele rekrutujesz w produkcji miasta; istniejące modernizujesz we własnym mieście.", 13, Ui.Muted));
        var debate = EpochCouncils.Debates[index]; p.Children.Add(Ui.Eyebrow("DECYZJA O KIERUNKU ROZWOJU")); p.Children.Add(Ui.Label(debate.Title + "\n" + debate.Body, 14, Ui.Text));
        if (f.EpochChoices.TryGetValue(index, out int choice)) p.Children.Add(Ui.Label("Przyjęto: " + debate.Policies[choice].Choice.Text + "\n" + EpochCouncils.Describe(debate.Policies[choice]), 13, Ui.Green));
        else foreach (var policy in debate.Policies) p.Children.Add(Ui.Label(policy.Choice.Text + "\n" + EpochCouncils.Describe(policy), 12, Ui.Muted));
        p.Children.Add(Ui.Button("Energia, klimat i programy kosmiczne", ShowWorldDevelopment)); p.Children.Add(Ui.Button("Wróć do mapy", CloseModal)); Modal(p, 900);
    }
    private void ShowWorldDevelopment()
    {
        var p = Ui.Stack(Ui.Eyebrow("PLANETA I PRZYSZŁOŚĆ"), Ui.Label("Energia, klimat i kosmos", 30, Ui.Text, true),
            Ui.Label($"Presja klimatyczna {state.ClimatePressure:0.0}/100 · wpływ na plony -{state.ClimatePressure * .35:0.0}%\nEmisje państwa {Epochs.Emissions(state, 0):+0.##;-0.##;0} na turę", 16, Ui.Gold, true),
            Ui.Label("Energia jest współdzielona przez miasta w bezpiecznej sieci drogowej. Obiekt wymagający energii otrzymuje 25–100% swoich premii żywności, pracy, złota, nauki i kultury. Utrzymanie płacisz w całości. Program kosmiczny wymaga 100% pokrycia. Emisje wszystkich państw zmieniają presję klimatyczną o ich sumę / 200 na turę; redukcje mogą odbudować klimat.", 13, Ui.Muted));
        foreach (var c in state.Cities.Where(c => c.Owner == 0))
        {
            var energy = Epochs.Energy(state, c);
            p.Children.Add(Ui.Label($"{c.Name} · sieć: {energy.Supply} energii / {energy.Demand} potrzeb · pokrycie {energy.Coverage:P0}", 15, energy.Coverage < 1 ? Ui.Red : Ui.Green));
            p.Children.Add(Ui.Button("Infrastruktura: " + c.Name, () => ShowProjects(c)));
        }
        p.Children.Add(Ui.Eyebrow("PROGRAMY POZAZIEMSKIE"));
        p.Children.Add(Ui.Label("Placówki są trwałymi programami państwa z dochodami w stolicy. Ta wersja nie ma osobnej mapy kosmosu ani sterowania flotą. Programy budujesz w wybranym mieście i możesz umieszczać w planie produkcji.", 12, Ui.Muted));
        foreach (var program in Epochs.Programs)
        {
            bool completed = state.Player.SpaceMilestones.Contains(program.Id);
            var q = Ui.Stack(Ui.Label(program.Name + (completed ? " · ukończono" : ""), 20, completed ? Ui.Green : Ui.Text, true), Ui.Label($"{program.Cost} produkcji · {Catalog.Tech(program.Technology).Name}\n{program.Effect}", 13, Ui.Muted));
            if (!completed) foreach (var c in state.Cities.Where(c => c.Owner == 0))
            {
                string? error = ProductionPlanning.ProjectError(state, c, program.Id); var button = Ui.Button("Zleć: " + program.Name + " — " + c.Name, () => { var r = Engine.SetProject(state, c.Id, program.Id); Do(r); if (r.Success) { TryAutoSave(false); ShowWorldDevelopment(); } });
                button.IsEnabled = state.Ending == null && error == null; q.Children.Add(button); if (error != null) q.Children.Add(Ui.Label(c.Name + ": " + error, 11, Ui.Gold));
            }
            p.Children.Add(Ui.Card(q));
        }
        p.Children.Add(Ui.Button("Epoki i technologie", () => ShowEpochs())); p.Children.Add(Ui.Button("Wróć do mapy", CloseModal)); Modal(p, 900);
    }
    private void ShowUnitModernization(Unit unit)
    {
        var p = Ui.Stack(Ui.Eyebrow("MODERNIZACJA ODDZIAŁU"), Ui.Label(UnitModels.Name(unit), 28, Ui.Text, true), Ui.Label("Potrzebny pełny ruch, dostawy i własne miasto. Zdrowie i doświadczenie pozostają; modernizacja pochłania cały ruch i porzuca umocnienia.", 13, Ui.Muted));
        foreach (var model in UnitModels.Upgrades(state, unit))
        {
            string? error = UnitModels.UpgradeError(state, unit, model.Id); var button = Ui.Button($"Modernizuj: {model.Name} · {UnitModels.UpgradeCost(state,unit, model)} złota · siła {model.Strength}", () => { var r = Engine.UpgradeUnit(state, unit.Id, model.Id); Do(r); if (r.Success) { TryAutoSave(false); ShowUnitModernization(unit); } });
            button.IsEnabled = error == null; p.Children.Add(button); if (error != null) p.Children.Add(Ui.Label(error, 12, Ui.Gold));
        }
        if (!UnitModels.Upgrades(state, unit).Any()) p.Children.Add(Ui.Label("Nie znamy silniejszego modelu tej roli. Odkryj kolejne technologie.", 14, Ui.Gold));
        p.Children.Add(Ui.Button("Wróć do mapy", CloseModal)); Modal(p, 850);
    }
}
