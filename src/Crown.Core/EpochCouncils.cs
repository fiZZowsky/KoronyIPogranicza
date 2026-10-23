namespace Crown.Core;

public sealed record EpochPolicy(Choice Choice, int Food = 0, int Production = 0, int Science = 0, int Gold = 0, int Energy = 0, int Emissions = 0, int Supply = 0);
public sealed record EpochDebate(string Title, string Body, EpochPolicy[] Policies);
public sealed record DevelopmentPolicy(int Food, int Production, int Science, int Gold, int Energy, int Emissions, int Supply);
public static class EpochCouncils
{
    public static readonly EpochDebate[] Debates =
    [
        new("Kto zasiądzie przy ogniu?", "Mamy dość umiejętności, by przeżyć kolejny sezon. Spór dotyczy podziału łowów, pracy i wiedzy. Zwyczaj przyjęty teraz może przetrwać tysiąclecia.",
            [new(new("Zapasy będą wspólne.", "Wspólny stół i ochrona najsłabszych.", Commons: 6, Nobles: -4), Food: 8), new(new("Pierwszeństwo otrzymają drużyny łowieckie.", "Narzędzia i dyscyplina kosztem równego podziału.", Commons: -3, Nobles: 5), Production: 8), new(new("Pamięć i nauka należą do wszystkich.", "Opowieści nie będą przywilejem jednego rodu.", Scholars: 5), Science: 8)]),
        new("Czyja ziemia, czyje ziarno?", "Osiadłe rodziny proszą o zasady dostępu do ziemi. Duże gospodarstwa chcą sprzedawać nadwyżki, a strażnicy spichlerzy proponują wspólną rezerwę.",
            [new(new("Utrzymamy wspólną rezerwę żywności.", "Rolnicy oddają część pracy za bezpieczeństwo zapasów.", Gold: -30, Commons: 5), Food: 10), new(new("Pozwolimy łączyć gospodarstwa.", "Większy obrót, silniejsze rody, zależni dzierżawcy.", Commons: -5, Nobles: 7), Gold: 10), new(new("Ziemię dostaną rodziny pracujące na niej.", "Samodzielność gospodarzy wspiera uprawy i pracę.", Nobles: -4), Food: 4, Production: 4)]),
        new("Pieczęć na rachunkach", "Pismo pozwala utrwalać zobowiązania. Skrybowie chcą publicznych zapisów, rody własnych archiwów, a budowniczowie priorytetu dostaw.",
            [new(new("Rachunki będą publiczne.", "Szkoły i jawność kosztem skarbu.", Gold: -60, Scholars: 6), Science: 10), new(new("Rody poręczą za obrót.", "Prywatne archiwa i prawo kupieckie.", Nobles: 6, Commons: -3), Gold: 10), new(new("Najpierw dostawy dla wspólnych budów.", "Sprawniejsza praca i dłuższe dostawy.", Commons: -2), Production: 5, Supply: 1)]),
        new("Prawo traktu i żelaza", "Granice państwa oddalają się od stolicy. Jedni chcą patroli, inni wolnych targów, a osady wodnych inwestycji.",
            [new(new("Utrzymamy patrole państwowe.", "Bezpieczniejszy zasięg dostaw za opłatę.", Gold: -90, Stability: 3), Supply: 2), new(new("Otworzymy trakty kupcom.", "Większy obrót, mniejszy udział rodów.", Nobles: -4), Gold: 12), new(new("Woda i pola mają pierwszeństwo.", "Lokalne wspólnoty utrzymają kanały.", Commons: 4), Food: 10)]),
        new("Kto jest obywatelem?", "Miasta potrzebują wspólnego prawa. Akademie chcą głosu w radzie, kupcy jasnych umów, a możni utrzymania kontroli nad warsztatami.",
            [new(new("Rozszerzymy obywatelstwo i szkoły.", "Wiedza kosztem wpływów rodowych.", Gold: -100, Nobles: -5, Scholars: 6), Science: 12), new(new("Zagwarantujemy swobodę umów.", "Kupcy zyskują pewność prawa.", Commons: -3), Gold: 12), new(new("Rozwój oprzemy na warsztatach miejskich.", "Państwo skoordynuje pracę rzemieślników.", Nobles: 3), Production: 10)]),
        new("Przysięga, cech czy wspólnota?", "Młyny i uczelnie zmieniają prowincje. Umowy lenne, cechy i wolne miasta proponują różne odpowiedzi na pytanie o podział pracy.",
            [new(new("Wesprzemy wolne miasta i uniwersytety.", "Wykształcenie poza kontrolą domen.", Gold: -150, Scholars: 7, Nobles: -5), Science: 14), new(new("Cechy otrzymają kartę pracy.", "Większa sprawność i większy wpływ zrzeszeń.", Commons: 4), Production: 12), new(new("Domeny utrzymają kanały i młyny.", "Rolnictwo pozostaje związane z rodami.", Nobles: 6, Commons: -4), Food: 12)]),
        new("Komu wolno drukować?", "Drukarze powielają książki, a arsenały broń. Możemy otworzyć obieg wiedzy, podporządkować go wojsku albo związać z wymianą kupiecką.",
            [new(new("Wiedza będzie krążyć bez przywileju.", "Otwarte oficyny, mniej kontroli rodów.", Gold: -220, Scholars: 8, Nobles: -6), Science: 15), new(new("Pierwszeństwo dostaną arsenały.", "Skupienie zasobów na sile państwa.", Commons: -4, Nobles: 5), Production: 14, Supply: 1), new(new("Zawrzemy kartę jarmarków i wydawców.", "Handel sfinansuje nowe przedsięwzięcia.", Scholars: 3), Gold: 14)]),
        new("Prawo do postępu", "Medycy i obywatele chcą udziału w korzyściach nowych badań. Skarb pyta, kto poniesie koszty reform.",
            [new(new("Sfinansujemy naukę publiczną.", "Długofalowy rozwój kosztem bieżącego skarbu.", Gold: -300, Scholars: 8), Science: 16), new(new("Otwarte warsztaty i wolne patenty.", "Wynalazki szybciej trafią do produkcji.", Nobles: -4), Production: 12, Gold: 4), new(new("Najpierw medycyna i bezpieczeństwo żywności.", "Postęp ma chronić ludność.", Commons: 7), Food: 12)]),
        new("Dym nad doliną", "Przemysł potrzebuje energii. Nieograniczone spalanie przyspieszy produkcję, ale emisje wszystkich państw obciążą wspólne plony.",
            [new(new("Przyjmiemy normy i inspekcje.", "Redukcja emisji za koszt inwestycji i wolniejszy obrót.", Gold: -450, Commons: 6), Emissions: -4, Gold: -4), new(new("Produkcja ma pierwszeństwo.", "Szybsza praca i dodatkowe emisje w każdym mieście.", Commons: -5, Nobles: 6), Production: 20, Emissions: 5), new(new("Zbudujemy sieć usług komunalnych.", "Publiczna energia wspiera mieszkańców.", Gold: -150), Energy: 12, Food: 6)]),
        new("Sieć dla wszystkich?", "Prąd i transport mogą połączyć społeczeństwo albo utrwalić zależność od kilku dostawców. Nowe fabryki bez energii tracą wydajność.",
            [new(new("Energia będzie usługą powszechną.", "Większe pokrycie potrzeb kosztem dochodów.", Gold: -600, Commons: 8), Energy: 25, Gold: -3), new(new("Koncesje sfinansują rozbudowę.", "Większe wpływy i emisje.", Commons: -5, Nobles: 7), Gold: 18, Emissions: 3), new(new("Przyjmiemy standardy oszczędności.", "Wydajniejsza gospodarka i mniejszy ślad.", Scholars: 5), Production: 8, Emissions: -3)]),
        new("Atom pod czyją kontrolą?", "Energia jądrowa i elektronika mogą wspierać dobrobyt. Zaufanie zależy od jawności i kontroli strategicznych instytucji.",
            [new(new("Jawny nadzór cywilny i badania.", "Nauka i niskoemisyjna energia.", Gold: -800, Scholars: 8), Energy: 30, Science: 10), new(new("Państwo skupi zasoby strategiczne.", "Sprawniejsza produkcja i dostawy, mniejszy udział obywateli.", Commons: -7, Nobles: 6), Production: 18, Supply: 2), new(new("Najpierw zdrowie i edukacja.", "Wyniki badań trafią do mieszkańców.", Commons: 8), Food: 10, Science: 8)]),
        new("Kto posiada dane?", "Sieci zbierają informacje o obywatelach. Możemy chronić prywatność, powierzyć dane rynkowi albo zwiększyć kontrolę państwa.",
            [new(new("Dane obywateli podlegają ochronie.", "Badania oparte na zgodzie i zaufaniu.", Gold: -1000, Commons: 8, Scholars: 5), Science: 18), new(new("Dane sfinansują rozwój usług.", "Zyski platform kosztem poparcia.", Commons: -8, Nobles: 6), Gold: 22), new(new("Ujednolicimy systemy państwowe.", "Sprawniejsza praca i energetyka.", Scholars: -4), Production: 12, Energy: 20)]),
        new("Praca po automatyzacji", "Roboty wykonują coraz więcej zadań. To od nas zależy, czy zysk zamieni się w czas dla ludzi, koncentrację dochodów czy ścisłą kontrolę pracy.",
            [new(new("Podzielimy korzyści i sfinansujemy przekwalifikowanie.", "Wiedza i dostęp do żywności kosztem skarbu.", Gold: -1400, Commons: 12, Nobles: -6), Food: 12, Science: 15), new(new("Przyspieszymy automatyzację zakładów.", "Silny wzrost pracy, większe zapotrzebowanie na infrastrukturę.", Commons: -10, Nobles: 8), Production: 25, Emissions: 3), new(new("Roboty będą infrastrukturą wspólnot.", "Mniejszy, szerzej dzielony wzrost.", Commons: 5), Production: 12, Science: 8)]),
        new("Jaką planetę pozostawimy?", "Dysponujemy narzędziami do odbudowy klimatu. Możemy ograniczać szkody, zwiększać produkcję lub skupić się na wspólnej sieci energetycznej.",
            [new(new("Przyjmiemy program odbudowy biosfery.", "Stała redukcja emisji w każdym mieście.", Gold: -2000, Commons: 10), Emissions: -15, Food: 10), new(new("Energia zasili kolejną falę przemysłu.", "Wysoka produkcja i dalsze obciążenie klimatu.", Commons: -7), Production: 25, Emissions: 8), new(new("Energia i wiedza połączą państwo.", "Wzmocnimy lokalne sieci bez nowego dymu.", Scholars: 8), Energy: 60, Science: 12)]),
        new("Orbita: dobro wspólne czy koncesja?", "Pierwsze habitaty będą zależeć od infrastruktury na planecie. Zasady dostępu zadecydują, kto skorzysta z nowej gospodarki.",
            [new(new("Przyjmiemy otwartą kartę badań orbitalnych.", "Wiedza z orbity wróci do społeczeństwa.", Gold: -2500, Scholars: 10), Science: 22), new(new("Koncesjonariusze sfinansują habitaty.", "Duże wpływy i silniejszy wpływ właścicieli.", Commons: -8, Nobles: 10), Gold: 25), new(new("Stacje będą siecią publiczną.", "Sprawniejsze dostawy i większa dostępność energii.", Commons: 6), Energy: 70, Supply: 2)]),
        new("Prawo ludzi poza planetą", "Ludzie przygotowujący placówki Księżyca i Marsa proszą o reprezentację. Odległość sprawia, że każda decyzja centrum ma inny ciężar niż na własnym kontynencie.",
            [new(new("Osady otrzymają samorząd i udział w badaniach.", "Wiedza kosztem wpływów centralnych.", Gold: -3000, Scholars: 10, Nobles: -6), Science: 25), new(new("Utrzymamy centralny plan dostaw.", "Produkcja i szlaki kosztem autonomii.", Commons: -8), Production: 20, Supply: 2), new(new("Zagwarantujemy wspólną rezerwę życia.", "Żywność i energia dla wszystkich osad.", Commons: 8), Food: 18, Energy: 90)]),
        new("Kto dziedziczy zasoby Układu?", "Górnictwo asteroid tworzy nowe bogactwo. Nie musimy powtarzać dawnych reguł koncentracji własności.",
            [new(new("Zasoby sfinansują wspólną naukę.", "Otwarte laboratoria i niższy ślad środowiskowy.", Gold: -4000, Scholars: 12), Science: 28, Emissions: -8), new(new("Przyznamy długie koncesje wydobywcze.", "Szybki wzrost przemysłu i wpływów właścicieli.", Nobles: 12, Commons: -10), Production: 28, Gold: 12), new(new("Powstanie federacja samowystarczalnych habitatów.", "Mniejsza zależność od centrum.", Commons: 8, Nobles: -5), Food: 18, Energy: 110)]),
        new("List do ludzi, których nie poznamy", "Statek pokoleniowy może wyruszyć poza Układ. Nie dożyjemy odpowiedzi. Jakie instytucje i zobowiązania powierzymy następnym pokoleniom?",
            [new(new("Otrzymają wolność i otwarte archiwum wiedzy.", "Samostanowienie i nauka zamiast odziedziczonej zależności.", Gold: -5000, Scholars: 15, Commons: 10), Science: 30), new(new("Zachowamy ciągłość planu państwowego.", "Silna baza przemysłowa i długie zobowiązania.", Nobles: 12, Commons: -10), Production: 30), new(new("Najpierw samowystarczalność i ochrona życia.", "Energia oraz żywność dla przyszłych pokoleń.", Commons: 12), Food: 22, Energy: 150)])
    ];
    public static string Id(int era) => "epoch_debate_" + era;
    public static int Index(string? id) => id != null && id.StartsWith("epoch_debate_") && int.TryParse(id[13..], out int n) && n >= 0 && n < Debates.Length ? n : -1;
    public static readonly NarrativeEvent[] Events = Debates.Select((d, i) => new NarrativeEvent(Id(i), "Mira Weles", "Głos rady · " + EpochCatalog.All[i].Name, d.Title, d.Body,
        d.Policies.Select(p => p.Choice with { Consequence = p.Choice.Consequence + " Trwale: " + Describe(p) }).ToArray())).ToArray();
    public static string Describe(EpochPolicy p) => string.Join(" · ", new[] { ("żywność %", p.Food), ("produkcja %", p.Production), ("nauka %", p.Science), ("złoto %", p.Gold), ("energia/miasto", p.Energy), ("emisje/miasto", p.Emissions), ("zasięg dostaw", p.Supply) }.Where(x => x.Item2 != 0).Select(x => $"{x.Item1} {x.Item2:+0;-0}"));
    public static DevelopmentPolicy Policy(Faction f)
    {
        var policies = f.EpochChoices.Select(x => Debates[x.Key].Policies[x.Value]).ToArray();
        return new(policies.Sum(p => p.Food), policies.Sum(p => p.Production), policies.Sum(p => p.Science), policies.Sum(p => p.Gold), policies.Sum(p => p.Energy), policies.Sum(p => p.Emissions), policies.Sum(p => p.Supply));
    }
    public static string? ChoiceError(GameState s, int choice)
    {
        int era = Index(s.PendingEvent);
        return s.Ending != null || !s.EpochCampaign || era < 0 || s.Player.Era < era || s.Player.EpochChoices.ContainsKey(era) || choice is < 0 or > 2 ? "Ta rada nie ma prawidłowego kontekstu epoki." : null;
    }
    public static bool Schedule(GameState s)
    {
        if (!s.EpochCampaign || s.PendingEvent != null) return false;
        int era = Enumerable.Range(0, s.Player.Era + 1).FirstOrDefault(i => !s.Player.EpochChoices.ContainsKey(i), -1);
        if (era < 0) return false; s.PendingEvent = Id(era); return true;
    }
    internal static void Apply(GameState s, int choice) { s.Player.EpochChoices.Add(Index(s.PendingEvent), choice); s.InvalidateLogistics(); }
    internal static void ManageAi(GameState s, Faction f)
    {
        if (!s.EpochCampaign) return;
        for (int era = 0; era <= f.Era; era++) if (!f.EpochChoices.ContainsKey(era))
        {
            int choice = f.Id == 1 ? 1 : f.Id == 2 ? 0 : 2;
            if (f.Gold + Debates[era].Policies[choice].Choice.Gold < 0) choice = Array.FindIndex(Debates[era].Policies,p=>f.Gold+p.Choice.Gold>=0);
            var c = Debates[era].Policies[choice].Choice; f.Gold += c.Gold; f.Stability += c.Stability; f.Commons += c.Commons; f.Nobles += c.Nobles; f.Scholars += c.Scholars; f.Prestige += c.Prestige;
            f.EpochChoices.Add(era, choice); Engine.Clamp(f); s.InvalidateLogistics();
        }
    }
}
