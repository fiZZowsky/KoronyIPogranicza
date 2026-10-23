using Crown.Core;
using System.Windows.Controls;

namespace Crown.Desktop;

public sealed partial class MainWindow
{
    private void ShowEspionage()
    {
        var agents = state.Spies.Where(a => a.Owner == 0 && a.Status != SpyStatus.Lost).OrderBy(a => a.Id).ToArray();
        var p = Ui.Stack(Ui.Eyebrow("SŁUŻBA CIENI"), Ui.Label("Wywiad i kontrwywiad", 30, Ui.Text, true),
            Ui.Label($"Agenci {agents.Length}/3 · utraceni {state.Spies.Count(a => a.Owner == 0 && a.Status == SpyStatus.Lost)} · skarb {state.Player.Gold:0.##}", 17, Ui.Gold, true),
            Ui.Label("Rozpoznanie daje raport z chwili akcji i odkrywa okolice miasta. Kradzież badań dodaje naukę do wspólnej puli. Sabotaż odbiera do 60 aktualnie zgromadzonych punktów produkcji. Koszt płacisz z góry; wynik i wykrycie rozstrzygamy osobno po 3–4 turach.", 13, Ui.Muted));
        var train = Ui.Button("Wyszkol agenta · 60 złota", () => { var result = Engine.RecruitSpy(state); Do(result); if (result.Success) { TryAutoSave(false); ShowEspionage(); } }, true);
        string? trainError = Espionage.RecruitError(state); train.IsEnabled = trainError == null; p.Children.Add(train);
        if (trainError != null) p.Children.Add(Ui.Label(trainError, 12, Ui.Gold));
        foreach (var agent in agents)
        {
            string status = agent.Status == SpyStatus.Available ? "Gotowy" : agent.Status == SpyStatus.Deployed ? "Na operacji" : "W niewoli";
            var q = Ui.Stack(Ui.Eyebrow($"AGENT #{agent.Id} · {status}"), Ui.Label(agent.Name, 22, Ui.Text, true), Ui.Label($"Doświadczenie {agent.Experience} · stopień {Espionage.Rank(agent)}/5", 13, Ui.Muted));
            if (agent.Status == SpyStatus.Available) q.Children.Add(Ui.Button("Zaplanuj operację agenta #" + agent.Id, () => ShowSpyPlanner(agent)));
            if (agent.Status == SpyStatus.Deployed)
            {
                var operation = state.SpyOperations.Single(o => o.AgentId == agent.Id && o.Outcome == SpyOutcome.Active);
                q.Children.Add(Ui.Button($"Operacja #{operation.Id} · termin T{operation.DueTurn}", () => ShowSpyReport(operation)));
            }
            if (agent.Status == SpyStatus.Captured)
            {
                var operation = state.SpyOperations.Single(o => o.AgentId == agent.Id && o.Outcome == SpyOutcome.Captured && o.CaptureChoice == null);
                q.Children.Add(SpyReplyButton(operation));
            }
            p.Children.Add(Ui.Card(q));
        }
        p.Children.Add(Ui.Rule()); p.Children.Add(Ui.Eyebrow("OBCY AGENCI W NASZEJ NIEWOLI"));
        var captives = state.SpyOperations.Where(o => o.TargetOwner == 0 && o.Owner != 0 && o.Outcome == SpyOutcome.Captured && o.CaptureChoice == null).OrderBy(o => o.Id).ToArray();
        p.Children.Add(Ui.Label("Zażądaj 100 złota z obcego skarbu, wymień jeńców, zwolnij agenta dla poprawy relacji lub internuj na osiem tur. Wymiana jest dostępna także z rady o naszym jeńcu. Internowanie jest nieodwracalne i do końca zajmuje miejsce w obcej służbie.", 13, Ui.Muted));
        if (captives.Length == 0) p.Children.Add(Ui.Label("Nie przetrzymujemy obcych agentów.", 13, Ui.Muted));
        foreach (var captive in captives)
        {
            var agent = state.Spies.First(a => a.Id == captive.AgentId);
            p.Children.Add(Ui.Label($"{agent.Name} · {state.Factions[captive.Owner].Name}" + (captive.InternedUntil == null ? " · czeka na decyzję" : $" · internowany do początku T{captive.InternedUntil}"), 16, Ui.Gold, true));
            if (CustodyCouncil.Open(captive)) p.Children.Add(CustodyReplyButton(captive));
            p.Children.Add(Ui.Button("Raport o jeńcu #" + captive.Id, () => ShowSpyReport(captive)));
        }
        p.Children.Add(Ui.Rule()); p.Children.Add(Ui.Eyebrow("OCHRONA WŁASNYCH MIAST"));
        p.Children.Add(Ui.Label("Sąd daje +15 obrony wywiadowczej, Pismo +5, stabilność co najmniej 70 kolejne +5. Wzmocniony kontrwywiad dodaje +25 przez osiem tur, także wobec operacji już w drodze. Zmiana właściciela kończy ochronę. Wrogie państwa od T20 mogą prowadzić kradzież badań i sabotaż.", 13, Ui.Muted));
        foreach (var city in state.Cities.Where(c => c.Owner == 0).OrderBy(c => c.Id))
        {
            var protect = Ui.Button("Kontrwywiad: " + city.Name + " · 60 złota", () => { var result = Engine.ProtectCity(state, city.Id); Do(result); if (result.Success) { TryAutoSave(false); ShowEspionage(); } });
            string? error = Espionage.ProtectionError(state, city); protect.IsEnabled = error == null;
            p.Children.Add(Ui.Label($"{city.Name} · obrona {Espionage.Defense(state, city)}" + (city.CounterIntelligenceUntil > state.Turn ? $" · ochrona do początku T{city.CounterIntelligenceUntil}" : ""), 14, Ui.Gold));
            p.Children.Add(protect); if (error != null) p.Children.Add(Ui.Label(error, 12, Ui.Muted));
        }
        p.Children.Add(Ui.Rule()); p.Children.Add(Ui.Eyebrow("RAPORTY I WYKRYTE INGERENCJE · OSTATNIE 20"));
        foreach (var operation in state.SpyOperations.Where(o => o.Owner == 0 || o.TargetOwner == 0 && o.Detected).OrderByDescending(o => o.Id).Take(20))
            p.Children.Add(Ui.Button($"Raport #{operation.Id} · {Espionage.Name(operation.Mission)} · {Espionage.StatusName(operation.Outcome)}", () => ShowSpyReport(operation)));
        p.Children.Add(Ui.Label("Wykrycie: zaufanie -15, krzywdy poszkodowanego +25, prestiż wysyłającego -2. Niepowodzenie z wykryciem oznacza schwytanie. Sukces z wykryciem zachowuje efekt, lecz wywołuje te same konsekwencje dyplomatyczne. Niewykryte obce operacje nie pojawiają się w tej historii.", 12, Ui.Muted));
        p.Children.Add(Ui.Button("Wróć do mapy", CloseModal)); Modal(p, 830);
    }
    private Button SpyReplyButton(SpyOperation operation)
    {
        var button = Ui.Button("Rozpatrz los agenta #" + operation.AgentId, () => { var result = SpyCouncil.Present(state, operation.Id); if (result.Success) ShowCouncil(); else Do(result); }, true);
        button.IsEnabled = state.Ending == null && (state.PendingEvent == null || state.PendingEvent == SpyCouncil.Event.Id && state.PendingSpyOperationId == operation.Id); return button;
    }
    private Button CustodyReplyButton(SpyOperation operation)
    {
        var button = Ui.Button("Rozpatrz obcego jeńca #" + operation.Id, () => { var result = CustodyCouncil.Present(state, operation.Id); if (result.Success) ShowCouncil(); else Do(result); }, true);
        button.IsEnabled = state.Ending == null && (state.PendingEvent == null || state.PendingEvent == CustodyCouncil.Event.Id && state.PendingSpyOperationId == operation.Id); return button;
    }
    private void ShowSpyPlanner(SpyAgent agent)
    {
        var p = Ui.Stack(Ui.Eyebrow("PRZYGOTOWANIE OPERACJI"), Ui.Label(agent.Name, 30, Ui.Text, true),
            Ui.Label("Wybierz odkryte obce miasto i zadanie. Na operację w tym samym mieście przypada osiem tur odstępu od poprzedniego wysłania siatki. Poziom agenta zwiększa sukces i zmniejsza wykrycie. Podgląd nie zużywa złota ani nie losuje wyniku.", 13, Ui.Muted));
        var cities = state.Cities.Where(c => c.Owner != 0 && state.Tiles[c.Tile].Explored).OrderBy(c => c.Owner).ThenBy(c => c.Id).ToArray();
        if (cities.Length == 0) p.Children.Add(Ui.Label("Najpierw odkryj obce miasto na mapie.", 16, Ui.Gold));
        else
        {
            var target = Ui.Combo(cities.Select(c => c.Name + " · " + state.Factions[c.Owner].Name).ToArray(), 0);
            var missions = Enum.GetValues<SpyMission>(); var mission = Ui.Combo(missions.Select(m => $"{Espionage.Name(m)} · {Espionage.Cost(m)} złota · {Espionage.Duration(m)} tury").ToArray(), 0);
            var quote = Ui.Label("", 14, Ui.Gold); var detail = Ui.Label("", 13, Ui.Muted);
            var launch = Ui.Button("Rozpocznij wybraną operację", () =>
            {
                var result = Engine.StartSpyMission(state, agent.Id, cities[target.SelectedIndex].Id, missions[mission.SelectedIndex]); Do(result);
                if (result.Success) { TryAutoSave(false); ShowEspionage(); }
            }, true);
            void Update()
            {
                var city = cities[target.SelectedIndex]; var kind = missions[mission.SelectedIndex]; var forecast = Espionage.Forecast(state, agent, city, kind);
                string? error = Espionage.MissionError(state, agent, city, kind); launch.IsEnabled = error == null;
                quote.Text = $"Sukces obecnie {forecast.SuccessChance}% · wykrycie {forecast.DetectionChance}% · obrona {forecast.Defense}\nKoszt {Espionage.Cost(kind)} · rozstrzygnięcie T{state.Turn + Espionage.Duration(kind)}\n" + (error ?? "Można rozpocząć operację.");
                detail.Text = (kind == SpyMission.Reconnaissance ? "Odkrycie pól do trzech heksów od miasta i raport o projekcie, pracy, ludności, garnizonie oraz skarbie. Raport nie aktualizuje się po zakończeniu misji." :
                    kind == SpyMission.StealResearch ? forecast.ResearchId == null ? "Brak nowych badań do skopiowania." : $"Cel badań: {Crown.Core.Content.Tech(forecast.ResearchId).Name}. Nagroda: {Math.Min(120, Math.Ceiling(Crown.Core.Content.Tech(forecast.ResearchId).Cost * .35)):0} nauki, bez natychmiastowego odkrycia. Jeśli poznasz tę technologię wcześniej, operacja zostanie odwołana bez zwrotu." :
                    "Sabotaż odbierze do 60 punktów pracy obecnych przy rozstrzygnięciu. Ukończony wcześniej projekt może ograniczyć stratę nawet do zera. Nie usuwa budynku ani planu produkcji.") +
                    "\nRyzyko sprawdzamy ponownie na końcu operacji. Nowy sąd, stabilność i kontrwywiad mogą zmienić wynik. Zmiana właściciela celu odwoła misję bez zwrotu.";
            }
            target.SelectionChanged += (_, _) => Update(); mission.SelectionChanged += (_, _) => Update(); Update();
            p.Children.Add(target); p.Children.Add(mission); p.Children.Add(quote); p.Children.Add(detail); p.Children.Add(launch);
        }
        p.Children.Add(Ui.Button("Wróć do wywiadu", ShowEspionage)); p.Children.Add(Ui.Button("Wróć do mapy", CloseModal)); Modal(p, 810);
    }
    private void ShowSpyReport(SpyOperation operation)
    {
        var agent = state.Spies.First(a => a.Id == operation.AgentId); var city = state.Cities.First(c => c.Id == operation.CityId);
        var p = Ui.Stack(Ui.Eyebrow($"OPERACJA #{operation.Id} · {Espionage.StatusName(operation.Outcome)}"), Ui.Label(Espionage.Name(operation.Mission), 30, Ui.Text, true),
            Ui.Label($"Agent {agent.Name} · cel {city.Name}\nWysłano T{operation.StartedTurn} · termin T{operation.DueTurn}" + (operation.ResolvedTurn == null ? "" : $" · rozstrzygnięto T{operation.ResolvedTurn}"), 15, Ui.Gold, true));
        if (operation.Outcome == SpyOutcome.Active)
        {
            var forecast = Espionage.Forecast(state, agent, city, operation.Mission);
            p.Children.Add(Ui.Label($"Aktualne szanse: sukces {forecast.SuccessChance}%, wykrycie {forecast.DetectionChance}%. Nie są gwarancją; warunki mogą się zmienić.", 14, Ui.Muted));
            if (operation.Owner == 0) p.Children.Add(Ui.Button("Odwołaj operację #" + operation.Id, () => { var result = Engine.RecallSpy(state, operation.Id); Do(result); if (result.Success) { TryAutoSave(false); ShowEspionage(); } }));
        }
        else p.Children.Add(Ui.Label(operation.Report, 15, Ui.Text));
        if (operation.Outcome == SpyOutcome.Captured && operation.Owner == 0 && operation.CaptureChoice == null) p.Children.Add(SpyReplyButton(operation));
        if (CustodyCouncil.Open(operation)) p.Children.Add(CustodyReplyButton(operation));
        if (operation.InternedUntil != null) p.Children.Add(Ui.Label($"Internowanie do początku T{operation.InternedUntil}" + (operation.CaptureChoice == null ? " · agent nadal w niewoli" : " · zakończone"), 14, Ui.Gold));
        if (operation.CaptureChoice is int choice) p.Children.Add(Ui.Label($"Powrót lub utrata w T{operation.CaptureDecisionTurn}: " + CustodyCouncil.DecisionName(choice), 14, Ui.Gold));
        p.Children.Add(Ui.Button("Wróć do wywiadu", ShowEspionage)); p.Children.Add(Ui.Button("Wróć do mapy", CloseModal)); Modal(p, 810);
    }
}
