using Crown.Core;
using System.Text;

internal static class EpochDocumentation
{
    public static void Write(string path)
    {
        var text = new StringBuilder();
        void Line(string value = "") => text.AppendLine(value);
        Line("# Epoki i możliwe przyszłości — podręcznik wydania 0.25"); Line();
        Line("Kampania obejmuje 18 głównych etapów, 90 technologii, 58 budynków rozwoju epok, 34 nowe modele wojsk i pięć programów pozaziemskich. Historyczne nazwy porządkują fikcyjny świat; regionalne podokresy grupujemy w większe bloki. Przyszłe epoki są scenariuszami gry, a nie prognozą lub obietnicą dostępności technologii."); Line();
        Line("Otwórz **Nauka → Epoki i rozwój świata**. Każdy etap ma cztery dziedziny i odkrycie zamykające dorobek. Badania następnego etapu wymagają tego zamknięcia. Dodatkowe wymagania technologii nadal obowiązują. Państwa rozwijają się oddzielnie i zachowują całą wcześniejszą wiedzę, miasta oraz instytucje."); Line();
        Line("Nowa kampania zaczyna się w paleolicie i ma limit 1600 tur. Kalendarz jest umowny: stopniowo zwalnia i uwzględnia najbardziej zaawansowane państwo; jego skala sięga roku 5000. Tempo wiedzy nie jest historycznym rozkładem dat. Stare kampanie zachowują zasady i limit 320 tur do wybrania **Rozszerz kampanię do 18 epok i 1600 tur**. Wtedy uznajemy zamknięcia epok poprzedzających posiadany już dorobek, zachowując miasta, środki, wywiad, długi, jednostki i plany."); Line();
        Line("## Zasady wspólne"); Line();
        Line("- Nowe technologie dają opisane premie miastom. Budynki są fizycznymi projektami produkcji, a nie nagrodą przy zmianie nazwy epoki. Mieszkania, opieka, obrona i zasięg działają w dotychczasowych systemach.");
        Line("- Energia jest lokalna dla bezpiecznej krajowej sieci drogowej. Suma produkcji energii zasilana jest także przez wybrane instytucje epok. Obiekt z popytem otrzymuje 25% + 75% × pokrycie energii swoich premii do żywności, produkcji, złota, nauki i kultury. Mieszkania i opieka pozostają, a utrzymanie jest płacone w całości. Energię współdzielą połączone miasta; wrogowie mogą odciąć połączenie. W wersji 0.18 przydział uwzględnia pierwszeństwo, priorytet zwykły i ograniczony. Miasta o równym priorytecie dzielą moc proporcjonalnie do potrzeb. Dyspozycje znajdziesz także bezpośrednio w panelu wybranego miasta.");
        Line("- Zakłady energetyczne i wymagające energii można wyłączyć. Ustają ich premie, dostawy, opieka, energia i emisje; pozostają miejsca mieszkalne oraz 25% utrzymania obiektu. Ponowny rozruch kosztuje 5% kosztu budowy w złocie (w górę). Stan każdego obiektu i priorytet miasta zmienisz raz na turę. Zapis i wczytanie nie resetują tego ograniczenia. Wyłączona infrastruktura nie spełnia wymagań programu kosmicznego.");
        Line("- Wodna elektrownia wymaga własnej rzeki przy mieście; geotermalna dwóch własnych pól wzgórz lub gór, licząc pole miasta. Moc wiatru i słońca zależy od sezonu oraz opisanego terenu; wartości to umowne reguły gry, bez pogody losowej lub naukowej prognozy. Moc zaokrąglamy w dół. Panel pokazuje bieżącą moc oraz nominalny katalog.");
        Line("- Faza gospodarcza używa wspólnego planu energii i emisji z początku rozliczenia. Nowa elektrownia daje moc w następnej fazie; kolejność miast nie pozwala zasilić wcześniej ukończonego programu w tej samej turze. Kolejny sezon może zmienić prognozę.");
        Line("- Od wersji 0.19 trzy kolejne fazy z dodatnim popytem i pokryciem poniżej 75% wywołują kryzys. Niepokoje miasta +2 i dobrobyt -1 na fazę; lud państwa -1 niezależnie od liczby miast, także AI. Delegacja własnego miasta oferuje pomoc, obietnicę lub odmowę. Obietnica wymaga trzech kolejnych przyszłych pełnych faz w sześć tur i działania usług protokołu. Wyłączenie tych usług nie jest naprawą. Rejestr w panelu energii pokazuje rzeczywiste dowody i wynik. Pełne zasady oraz skutki w README.");
        Line("- Emisje i redukcje wszystkich państw zmieniają presję klimatyczną o ich sumę / 200 na turę, w zakresie 0–100. Presja ogranicza plony o 0,35% na punkt, maksymalnie 35%, przed utrzymaniem ludności. Redukcje wymagających energii obiektów również zależą od pokrycia; nie ma losowych katastrof klimatycznych.");
        Line("- Każda epoka ma własny dialog z trzema trwałymi kierunkami rozwoju. Koszty polityczne i złoto są widoczne w radzie. Premie procentowe wybranych instytucji sumują się; infrastruktura energetyczna, emisje i zasięg są podane osobno. Wolna odpowiedź pozostaje dostępna przy pustym skarbie. Nie można zmienić już przyjętej instytucji ani odebrać jej drugi raz.");
        Line("- Modele wojsk zachowują lądowe role piechoty, jazdy, zwiadu i artylerii, lecz mają własną siłę, ruch, zasięg i koszt. Utrzymanie nowego modelu to 1,4 + 0,6 × numer epoki od zera złota na turę. Jazda konna wymaga koni, czołgi i łaziki tego zasobu nie wymagają. Artyleria nie zdobywa miasta; jej dalszy ostrzał jest pośredni, z dotychczasowym sprawdzeniem przesłony dla dystansu dwóch pól.");
        Line("- Modernizacja wymaga własnego miasta, dostaw, pełnego ruchu i braku eskorty. Koszt: 60 + 3 × przyrost bazowej siły, uwzględniającej posiadane premie materiałowe dawnych oddziałów. Słabszy model jest niedostępny. Zdrowie i doświadczenie pozostają, ruch i umocnienia przepadają. Możesz także rekrutować nowe modele w produkcji miast. Nowoczesne wojska są jednostkami lądowymi; nazwy orbitalne opisują ich pochodzenie, nie osobny system lotu.");
        Line("- AI korzysta z tych samych technologii, energii, budynków, programów i modeli oraz opłaca modernizacje. Jej wybory instytucji wynikają z prostych preferencji i środków, nie generowanych dialogów. Dawna premia poziomu trudności nadal obowiązuje."); Line();
        foreach (var e in EpochCatalog.All)
        {
            Line($"## {e.Index + 1:00}. {e.Name}"); Line(); Line(e.Period + ". " + e.Description); Line();
            Line("| Technologia | Nauka | Wymagania wewnętrzne | Efekt |"); Line("|---|---:|---|---|");
            foreach (string id in e.Technologies) { var t = Content.Tech(id); Line($"| {t.Name} | {t.Cost} | {string.Join(", ",t.Requires.Select(x=>Content.Tech(x).Name))} | {t.Description} |"); } Line();
            Line("| Nowa infrastruktura | Produkcja | Technologia | Działanie |"); Line("|---|---:|---|---|");
            foreach (var b in EpochCatalog.Buildings.Where(b => b.Era == e.Index)) Line($"| {b.Name} | {b.Cost} | {Content.Tech(b.Technology).Name} | {EpochCatalog.Describe(b)} |"); Line();
            Line("| Model wojsk | Siła bazowa | Ruch | Zasięg | Produkcja | Technologia |"); Line("|---|---:|---:|---:|---:|---|");
            foreach (var m in UnitModels.All.Where(m => m.Era == e.Index)) Line($"| {m.Name} | {m.Strength} | {m.Moves} | {m.Range} | {m.Cost} | {Content.Tech(m.Technology).Name} |"); Line();
            var d = EpochCouncils.Debates[e.Index]; Line("**Rada: " + d.Title + "**"); Line(); Line(d.Body); Line();
            for (int i=0;i<3;i++){var p=d.Policies[i];Line($"- {p.Choice.Text} {Narrative.Effects(EpochCouncils.Events[e.Index].Choices[i],EpochCouncils.Id(e.Index),i).Replace("\n"," ")}");} Line();
        }
        Line("## Programy kosmiczne i dalsza gra"); Line();
        Line("Programy są narodowymi przedsięwzięciami z trwałymi przychodami w aktualnej stolicy. Każdy wymaga technologii, wcześniejszego programu, wskazanej infrastruktury w mieście budowy i pełnego pokrycia energii. Można umieszczać je w kolejce. Utrata zasilania wstrzymuje pracę bez utraty już zgromadzonej produkcji. Ukończonego programu nie można powielić w drugim mieście."); Line();
        Line("| Program | Produkcja | Infrastruktura | Skutek |"); Line("|---|---:|---|---|");
        foreach(var p in Epochs.Programs)Line($"| {p.Name} | {p.Cost} | {Content.Project(p.Facility).Name} | {p.Effect} |"); Line();
        Line("Wysłanie statku pokoleniowego wymaga dodatkowo stabilności co najmniej 55 i daje zwycięstwo naukowe. Atlas świata pozostaje użytecznym projektem historycznym (+35 nauki, +25 kultury miasta) i nie kończy pełnej kampanii. Nowe zwycięstwo kulturowe wymaga epoki międzyplanetarnej, 300 000 kultury i 5000 prestiżu. Zjednoczenie nadal może zakończyć rozgrywkę wcześniej. Po zwycięstwie wybierz **Kontynuuj rozwój świata**, aby grać dalej do limitu; utraty ostatniego miasta lub końca limitu nie można w ten sposób ominąć."); Line();
        Line("Od 0.21 mapa planetarna jest przestrzenną sceną 3D z kamerą perspektywiczną. Uproszczone modele miast i wojsk zmieniają się przy rozwoju. Programy reprezentują stałe placówki poza planetą i ich gospodarkę, bez osobnej mapy Księżyca lub Marsa, sterowalnych flot, bitew orbitalnych czy lotu szybszego od światła. Dalsza produkcja może rozwinąć te systemy w osobne teatry działań."); Line();
        Line("## Kontekst i źródła inspiracji"); Line();
        Line("- [Historic England — Stone Age to Iron Age](https://historicengland.org.uk/education/schools-resources/teaching-activities/timeline-stone-age-to-iron-age/): regionalny przykład rozróżnienia epok prehistorycznych, bez traktowania lokalnych dat jako uniwersalnych.");
        Line("- [Luigi Cajani, Periodization — Oxford Handbook of World History](https://academic.oup.com/edited-volume/34513/chapter-abstract/292841417): różne sposoby podziału historii świata. Nazwy w grze są autorskim uproszczeniem.");
        Line("- [NASA — Moon to Mars Architecture](https://www.nasa.gov/moontomarsarchitecture/): inspiracja etapowym budowaniem infrastruktury poza Ziemią. Opisane w grze przyszłe technologie, terminy, dochody i odległe epoki są fikcją projektową, nie deklaracją NASA."); Line();
        Line("Dane tabel są generowane z rzeczywistego katalogu gry. Regeneracja: `dotnet run --project tests/Crown.Specs -c Release -- --export-epochs EPOKI.md`.");
        File.WriteAllText(path,text.ToString(),new UTF8Encoding(false));
    }
}
