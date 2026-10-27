# Korony i Pogranicza · 0.22

Autorska, jednoosobowa strategia turowa dla Windows, napisana w **C# / .NET 10**, z interfejsem i dialogami po polsku. Historycznie inspirowany świat jest fikcyjny. Gracz prowadzi Marchie Veyru od prehistorii przez historię do możliwej cywilizacji międzygwiezdnej.

To **grywalna wersja rozwojowa**, a nie ukończona produkcja o skali komercyjnej Cywilizacji. Fundamentem wyróżniającym projekt są pamięć polityczna państwa, stronnictwa społeczne i konsekwencje dialogów. Zmiana epoki nie usuwa miast ani historii.

## Uruchomienie

Uruchom `wydanie-0.22/KoronyIPogranicza.exe` albo główny `Uruchom.cmd`. Zachowaj pozostałe pliki z katalogu `wydanie-0.22` obok programu. Poprzednie wydania pozostają w osobnych katalogach.

**Nowy etap: Krajobraz i miasta.** Wersja 0.22 rozwija oprawę Direct3D 11: połączone górskie grzbiety, szczegółowy daleki horyzont, płycizny i mokre brzegi oraz historyczne miasta z ulicami, rynkiem i bramami. Dodano przełączniki efektów brzegowych i wygładzania krawędzi. To kolejny krok w stronę realizmu; grafika nadal nie osiąga fotorealizmu ilustracji menu. Wszystkie dotychczasowe systemy, 18 epok i zapis v19 pozostają. Pełny katalog: [EPOKI.md](EPOKI.md). Szczegóły: `CHANGELOG.md`.

Wymagania: Windows 10/11 i **.NET Desktop Runtime 10 x64**. Na komputerze, na którym powstał projekt, ten runtime jest zainstalowany. Wydanie korzysta z zainstalowanego środowiska .NET, nie zawiera własnego runtime'u. Nowa oprawa wymaga sprzętowego Direct3D 11 z feature level 11_0. Gdy inicjalizacja GPU się nie powiedzie, gra jawnie przechodzi do prostszego renderera WPF; parametr `--no-gpu` wymusza ten tryb. Gra nie wymaga połączenia z Internetem ani konta.

## Grafika i mapa 3D od 0.21

Scenę renderuje Direct3D 11, a interfejs, kamera i testowanie geometrii pozostają w WPF. Silnik reguł Crown.Core jest niezależny. Materiały mają oświetlenie mikrofasetowe GGX, atlas trawy, skał, kamienia i dachówek, cienie PCF z mapy 2048×2048, przybliżoną okluzję ekranową, mgiełkę atmosferyczną i filmową krzywą tonalną. HDR dotyczy obliczeń wewnętrznych; obraz jest wyświetlany w zwykłym formacie 8-bitowym.

Drzewa mają przestrzenne pnie i gałęzie oraz krzyżujące się płaszczyzny z wyciętymi liśćmi dębu i igłami świerku. Prześwity uczestniczą również w cieniach. Korony lekko poruszają się na wietrze. Budynki mają tekstury kamienia i dachówek, okna, belki, podmurówki i kominy. Powiększono gęstość zabudowy. Wojska zachowują formacje w bardziej spójnej skali. Teren ma gęstszą siatkę i dodatkową rzeźbę grzbietów. Rzeki meandrują i mają brzegi. Jeziora oraz morze odbijają scenę na płaszczyźnie wody; rzeki wykorzystują światło i odbicie nieba. Odległe pasmo poza siatką jest dekoracją, bez pól rozgrywki.

**Krajobraz** ustawia niską, szerokokątną kamerę. **Widok 3D** przywraca kamerę strategiczną. **Grafika** udostępnia jakość wysoką i zrównoważoną, przełączniki cieni, odbić i animacji oraz zmianę światła poranek/południe. Jakość zrównoważona ogranicza szerokość obrazu do 1100 pikseli i wyłącza kosztowniejsze odbicia. Ustawienia dotyczą bieżącego widoku i nie są częścią zapisu kampanii. Renderowanie otoczenia odświeża się do 10 razy na sekundę; kamera i rozkazy wywołują dodatkowe klatki. To nie jest deklaracja stałych 10 ani 60 FPS na różnych GPU.

Sterowanie pozostaje zgodne: krótki LPM wybiera, przeciągany przesuwa; krótki PPM wydaje rozkaz, przeciągany obraca kamerę. Działają kółko, W/A/S/D, Q/E oraz PageUp/PageDown. Minimąpa pokazuje obrys perspektywicznego widoku. Nieodkryte pola zachowują neutralną wysokość i materiał; niewidoczne obce wojsko nie jest renderowane. Modele budynków i jednostek wskazują własne pole także wtedy, gdy wystają poza jego obrys. Dekoracyjne płaszczyzny liści nie blokują kliknięć przez ich przezroczyste miejsca.

Ilustracja menu pozostaje docelową inspiracją, a nie zrzutem gry. Modele są nadal proceduralne, a tekstury wygenerowano z AI; nie są skanami fotogrametrycznymi. Brakuje szczegółowych rzeźbionych modeli, animowanych postaci, pełnego oświetlenia globalnego, fizycznego nieba i zaawansowanego LOD. Stare zapisy działają bez zmiany formatu v19 i bez rozpoczynania kampanii od nowa.

## Krajobraz i miasta od 0.22

Góry korzystają ze wspólnego pola wysokości, z szerokimi masywami i nieregularnymi, połączonymi grzbietami. To samo pole opisuje geometrię, wysokość kamery i dno dla materiału wody. Odkrycie nowych ziem przebudowuje powierzchnię; ukryty rodzaj terenu nie wpływa na jej kształt. Dalekie pasmo ma gęstszą, ciągłą siatkę i pozostaje dekoracją poza planszą.

Kolor jezior i morza zależy od przybliżonej głębokości: płycizny mają jaśniejsze dno i drobną pianę, głębsza woda jest ciemniejsza. Nisko położone brzegi mają wilgotny materiał. To efekt graficzny przy stałym poziomie wody, bez symulacji przypływów czy erozji. Rzeki zachowują własny materiał i umowną głębokość. Menu **Grafika** umożliwia osobne przełączanie brzegów i wygładzania krawędzi. Wygładzanie jest lekkim filtrem kontrastu, bez próbkowania czasowego.

Miasta etapów 3–9, od brązu do rewolucji przemysłowej, mają stabilny układ parcel, dwie główne ulice, plac i stragany. Wzrost dodaje domy bez przestawiania istniejących. Późniejsze warianty otrzymują belkowanie i wieżę ratusza. Wybudowane mury pokazują kurtyny, baszty i przestrzenną bramę z prawdziwym otworem. Istniejąca infrastruktura przemysłowa nadal jest widoczna. Parcele i stragany są dekoracją: nie dodają osobnej produkcji ani kontraktów. Miasta pozostałych epok zachowują dotychczasowe sylwetki. Kampanie można kontynuować bez migracji zapisu.

## Kryzysy i obietnice od 0.19

Otwórz **Nauka → Energia, klimat i kosmos → Kryzysy i obietnice energetyczne**. Panel zarządzania energią miasta także prowadzi do jego spraw i pokazuje osiem ostatnich rzeczywistych faz gospodarki. Bieżący podgląd mocy jest prognozą; nowa elektrownia i kolejny sezon nie zmieniają zapisanych odczytów.

Trzy kolejne fazy z popytem większym od zera i pokryciem **poniżej 75%** oznaczają długi niedobór. Od trzeciej takiej fazy miasto otrzymuje **+2 niepokojów, -1 dobrobytu**, a poparcie ludu państwa spada o **1** na fazę niezależnie od liczby dotkniętych miast. Dotyczy to także AI. Jedna dobra faza lub brak aktywnych potrzeb przerywa serię.

W mieście gracza powstaje delegacja. Trwająca sprawa nie jest powielana; kolejna może powstać najwcześniej **16 tur po zamknięciu poprzedniej**. Inny otwarty dialog zachowuje pierwszeństwo. Rejestr pamięta zakłady, fazy, pokrycie i cenę pomocy z chwili zgłoszenia.

| Odpowiedź rady | Koszt i skutek |
|---|---|
| Pomoc rodzinom | 60 + 5 × mieszkańcy w chwili zgłoszenia złota. Lud +4, stabilność +1; miasto: niepokoje -8, dobrobyt +4. Energia nie powstaje. |
| Obietnica odbudowy | Bez zapłaty, lud +2. Wymaga trzech kolejnych przyszłych faz pełnego zasilania w sześć tur; wszystkie zakłady wskazane w protokole muszą działać. |
| Odmowa interwencji | Bez zapłaty. Lud -4, rody +4; miasto: niepokoje +4. Brak zobowiązania do dostaw. |

Sukces obietnicy: **lud +6, stabilność +2, prestiż +8**, w mieście **dobrobyt +6, niepokoje -6**. Porażka: **lud -10, stabilność -4, prestiż -8 (minimum 0)**, we własnym mieście **dobrobyt -6, niepokoje +10**. Koszty i skutki są widoczne w dialogu.

Decyzja w T4 z terminem „początek T10” obejmuje fazy gospodarcze T4–T9. Trzy pełne fazy T7, T8 i T9 spełniają zobowiązanie także na ostatnim rozliczeniu. Niedobór resetuje serię. Samo zbudowanie elektrowni nie zalicza jej działania w już rozpoczętej fazie. Nowa elektrownia daje moc od następnej. Wyłączenie obiecanych zakładów albo zasilenie tylko innych usług nie oznacza naprawy. Całe aktywne miasto musi mieć 100% pokrycia. Utrata miasta łamie obietnicę, bez przekładania miejscowej kary na nowego właściciela. Oczekująca delegacja utraconego miasta zostaje zamknięta.

Każde zobowiązanie zachowuje do sześciu odczytów jako dowód, nawet po przewinięciu ośmiopozycyjnej historii miasta. Wynik nie jest później wypłacany drugi raz. Wczytanie zachowuje postęp, termin i wybór. Stare kampanie zaczynają pomiary od kolejnej rzeczywistej fazy; nie naliczamy zaległych kryzysów.

To lokalny konflikt interesów pracowników i usług, bez symulacji osobnych gospodarstw domowych, cen detalicznych energii, strajków zawodowych, losowych awarii lub osobnych delegacji gracza w obcych państwach. AI ponosi te same ogólne skutki niedoborów i reaguje dotychczasowym zarządzaniem zasilaniem oraz prowincją.

## Zarządzanie energią od 0.18

Wybierz własne miasto i **Zasilanie → zarządzaj energią** albo **Nauka → Energia, klimat i kosmos → Zarządzaj energią**. Podgląd pokazuje rzeczywistą sieć drogową, moc, potrzeby i przydział każdego miasta.

- **Pierwszeństwo**, **Zwykły**, **Ograniczony**: najpierw zasilamy wyższą grupę; w tej samej grupie moc dzielimy proporcjonalnie do potrzeb. Sam priorytet nie tworzy energii i nie usuwa blokady. Zmiana miasta wpływa na wyniki pozostałych członków sieci.
- **Wyłączenie zakładu** usuwa jego dochody, pracę, wiedzę, żywność, kulturę, energię, zapotrzebowanie, opiekę, dostawy i emisje. Fizyczne miejsca mieszkalne pozostają. Utrzymanie tego obiektu spada do 25%, zamiast znikać. Inne koszty administracji miasta nadal obowiązują. Dotyczy obiektów katalogu epok z energią lub zapotrzebowaniem, nie dawnych bibliotek, koszar czy murów.
- **Rozruch** kosztuje 5% kosztu budowy w złocie, zaokrąglone w górę. Nie niszczy zgromadzonej produkcji ani kolejki. Priorytet miasta oraz stan każdego zakładu zmienisz raz na turę; wczytanie zapisu nie omija ograniczenia. Wyłączona stocznia lub inna wymagana infrastruktura blokuje program kosmiczny.
- **Elektrownia wodna**: 900 produkcji, Elektryczność, 45 mocy i 3 złota utrzymania; wymaga własnej rzeki na polu miasta lub sąsiednim. **Farma wiatrowa**: 1250 produkcji, Ekologia, bazowo 35 mocy i 2 złota; latem ×0,8, zimą ×1,2, własne wzgórze przy mieście ×1,2. **Farma słoneczna**: 1300 produkcji, Ekologia, bazowo 42 mocy i 2 złota; zimą ×0,65, pustynne pole miasta ×1,25. **Elektrownia geotermalna**: 1800 produkcji, Inteligentne materiały, 100 mocy i 8 złota; dwa własne pola wzgórz lub gór przy mieście, licząc pole miasta. Wszystkie cztery mają zerowe emisje w modelu gry. Moc zaokrąglamy w dół. Teren ogranicza wybór miejsca budowy, bez wyczerpywania złóż.
- Faza gospodarcza korzysta z jednego planu zasilania i emisji sprzed rozliczenia miast. Elektrownia ukończona w tej fazie zasila następną; kolejność miast nie daje darmowej energii. Interfejs po zakończeniu tury pokazuje prognozę nowego sezonu.
- AI wybiera dostępne źródła z uwzględnieniem emisji, wyłącza zbędne emitujące elektrownie, uruchamia potrzebne odziedziczone zakłady i nadaje pierwszeństwo wybranemu programowi kosmicznemu. To heurystyki, bez prognozowania całej zimowej rezerwy.

Energia jest nadal abstrakcyjną mocą sieci drogowej. Nie ma magazynów energii, wydobycia paliwa, strat przesyłowych, osobnych linii wysokiego napięcia ani handlu energią. Sezonowe mnożniki są zasadami gry, nie symulacją fizyczną.

## Co jest grywalne

- Świat 42 × 28 heksów: ziarno proceduralne, biomy, rzeki, zasoby, ruiny, mgła wojny, przybliżanie i minimapa. Stolice mają gwarantowane połączenie lądowe.
- Cztery fikcyjne państwa z różnymi premiami; gracz prowadzi jedno, trzy obsługuje AI. Trzy poziomy trudności zmieniają wydajność AI.
- Miasta, ludność, wzrost i głód, sezonowe plony, eksploatacja pól, farmy, kopalnie, placówki handlowe, produkcja i utrzymanie.
- 18 etapów od paleolitu do możliwej epoki międzygwiezdnej; 90 technologii, 72 trwałe projekty miejskie, dwa projekty powtarzalne, 40 modeli jednostek w sześciu lądowych rolach oraz pięć narodowych programów kosmicznych.
- Lud, rody i uczeni: ich poparcie wpływa na docelową stabilność, a ta na gospodarkę. Ustawy zmieniają podatki, ustrój i priorytety.
- 47 rodzajów wyjątkowych i kontekstowych spraw rady oraz cztery wydarzenia powracające. Sprawy prowincji mogą pojawiać się osobno dla każdego miasta; delegacje kupców odnoszą się do konkretnych utraconych karawan. Odpowiedzi zmieniają poparcie, prawa lokalne i przyszłe zdarzenia. Nadanie ziemi lub przywilejów zmienia późniejszą gałąź dialogów. Prawo traktu trwale zmienia dochody, naukę albo dostawy.
- Dyplomacja: dary, handel, krzywdy, trzy powody wypowiedzenia wojny, odszkodowania, pakty o nieagresji i rozejmy. Posłowie wracają z odpowiedzią po dwóch turach. AI zakłada miasta, bada, buduje, ulepsza ziemię i prowadzi działania wojenne.
- Negocjowany pokój z reparacjami i zwrotem miast, propozycjami AI, kontrofertami i terminem odpowiedzi. Historia wojny przechowuje ogłoszony cel, straty, bilans i końcowe rozliczenie obietnicy.
- Walka wręcz i dystansowa, doświadczenie, obrona terenu, oblężenia i przejmowanie miast. Woda i góry blokują ruch. Armie poza zasięgiem dostaw słabną; odpoczynek leczy.
- Drogi za 25 złota i 0,15 złota utrzymania/turę, automatyczna wycena połączeń miast, premie handlowe, blokowanie dostaw przez wojska i niszczenie wrogich dróg. Ruch między połączonymi odcinkami kosztuje 0,5 punktu.
- Rozkazy marszu zapisywane między turami i w plikach kampanii; podświetlenie zasięgu i trasy. Atak wymaga potwierdzenia podglądu zgodnego z modelem walki.
- Prowincje z własnymi potrzebami, kierunkiem rozwoju i dobrobytem. Dzielnice mieszkalne oraz lecznice ograniczają presję społeczną. Regionalna wymiana karmi miasta z niedoborem, a bezpieczne drogi umożliwiają przeprowadzki do lepszych warunków.
- Lojalność i pamięć założycieli miast. Autonomia oddaje część dochodów za większą lojalność i kulturę; zarząd wojskowy wymaga garnizonu. Podbite miasto może wrócić do założycieli po trzech turach lojalności poniżej 20.
- Fizyczne zapasy żelaza, soli i barwników, wydobycie z pracujących pól, regionalny popyt i ceny. Dostawy krajowe dzielą nadwyżkę w sieci drogowej. Kontrakty zagraniczne przenoszą towary i złoto; embargo, wojna i blokady przerywają dostawy.
- Karawany poruszające się po heksach, podgląd trasy i czasu przybycia, tranzyt przez państwa trzecie, przechwytywanie przez wroga i osłona przez pobliskie wojska. Nabywca płaci przy wysyłce i ponosi ryzyko utraty.
- Delegacje kupieckie po stratach importów, pomoc zależna od wartości manifestu i obietnice zastępczych dostaw do wskazanego miasta. Termin, postęp, manifesty i polityczny wynik pozostają w historii kampanii.
- Wyznaczone eskorty karawan: wspólny marsz, koszt pozostałego ruchu, rezerwacja oddziału, brak odpoczynku i zakończenie przy niedostępnej wojskowo granicy. Zwiadowcy AI mogą objąć eskortę na polu własnego transportu.
- Zwycięstwo naukowe, kulturowe, przez zjednoczenie oraz punktowe po 1600 turach w pełnej kampanii (320 w starej przed rozszerzeniem). Po zwycięstwie pełnej kampanii można kontynuować rozwój świata do limitu. Kampania może zakończyć się wcześniej.
- Zapis i wczytanie stanu JSON z walidacją, atomowym zastępowaniem pliku i zapisanym stanem generatora losowego. Autozapis po turze i decyzji rady; kronika przechowuje wybory oraz ich skutki.

Długość i balans wymagają dalszych rozgrywek z udziałem człowieka. Automatyczne symulacje nie potwierdzają jakości wielogodzinnej rozgrywki.

## Epoki: od ognia do gwiazd

W panelu **Nauka** otwórz **Epoki i rozwój świata**. Każdy etap ma pięć technologii: cztery dziedziny i odkrycie zamykające dorobek. Kolejny etap wymaga zamknięcia poprzedniego. Epok nie wyliczamy już z samej liczby odkryć. Państwa zachowują całą wcześniejszą wiedzę i rozwijają się osobno.

Paleolit → neolit → brąz → żelazo → starożytność klasyczna → średniowiecze → renesans → oświecenie → rewolucja przemysłowa → nowoczesność → epoka atomowa → informacyjna → automatyzacji → planetarna → orbitalna → międzyplanetarna → Układu Słonecznego → międzygwiezdna.

To 18 głównych bloków przyjętych dla fikcyjnego świata; lokalne podokresy grupujemy. Przyszłość jest autorskim scenariuszem, bez deklarowania pewności wynalazków czy historycznej konieczności takiego rozwoju. Wszystkie epoki, technologie, koszty, budynki, modele i decyzje opisuje **[podręcznik EPOKI.md](EPOKI.md)**, generowany z faktycznego katalogu gry.

- Każda epoka wnosi trzy nowe budynki i własny dialog z trzema trwałymi kierunkami rozwoju. Wybory zmieniają produkcję, wiedzę, żywność, złoto, energię, emisje albo dostawy, wraz z kosztami społecznymi i finansowymi. Starsze budynki i wątki nadal istnieją.
- Modele obejmują łowców, legion, rycerstwo, muszkieterów, piechotę liniową, haubice, czołgi, wojska zmechanizowane, robotyczne i ekspedycyjne. Mają własną siłę, ruch, zasięg, produkcję i rosnące utrzymanie. Własne miasto z dostawami pozwala zmodernizować weterana za złoto i cały ruch, zachowując zdrowie oraz doświadczenie. Modernizację znajdziesz w panelu jednostki i stałych rozkazów.
- **Energia, klimat i kosmos** pokazuje energię współdzieloną w bezpiecznej sieci drogowej. Brak energii ogranicza do 25% premie obiektu, który jej potrzebuje; pełne pokrycie daje 100%. Działa na żywność, produkcję, złoto, naukę i kulturę; mieszkania i opieka pozostają. Utrzymanie płacisz w całości. Zniszczone połączenie może odciąć zasilanie.
- Emisje wszystkich państw zmieniają presję klimatyczną o sumę / 200 na turę. Każdy punkt presji ogranicza plony o 0,35%, do 35% przy 100. Odbudowa klimatu i wybrane instytucje mogą odwrócić presję. Nie ma losowych katastrof lub szczegółowej fizyki atmosfery.
- Pięć narodowych programów: stacja orbitalna, baza księżycowa, osadnictwo marsjańskie, wspólnota pasa asteroid i wysłanie statku pokoleniowego. Wymagają wcześniejszego programu, technologii, lokalnej infrastruktury oraz pełnej energii; końcowy także stabilności 55. Ukończenie daje zapisany dorobek państwa i przychody w stolicy. Placówki są abstrakcyjnymi programami; mapa pozostaje planetarna i nie ma jeszcze sterowalnych flot ani osobnych map kosmosu.
- Nowa kampania ma 1600 tur, umowny kalendarz do roku 5000 i przyszłe zwycięstwo naukowe za wysłanie statku pokoleniowego. Atlas świata daje naukę oraz kulturę i nie kończy pełnej kampanii. Nowe zwycięstwo kulturowe wymaga epoki międzyplanetarnej, 300 000 kultury i 5000 prestiżu. Po zwycięstwie przycisk **Kontynuuj rozwój świata** pozwala grać dalej do limitu; nie omija utraty ostatniego miasta.

W starej kampanii wybierz **Nauka → Epoki i rozwój świata → Rozszerz kampanię do 18 epok i 1600 tur**. Rozszerzenie zachowuje miasta, skarb, wojska, długi, wywiad i decyzje; uznaje fundamenty epok poprzedzających posiadany dorobek. Przed rozszerzeniem stary zapis zachowuje dawny limit i zasady zwycięstwa. Nowe reguły nie dopisują dawnych emisji ani fikcyjnych programów kosmicznych.

## Wywiad: służba cieni

Po odkryciu **Pisma** otwórz **Dyplomacja → Wywiad i kontrwywiad**. Szkolenie kosztuje **60 złota**. Państwo może mieć trzech agentów w służbie, wliczając osoby w niewoli. Utraceni pozostają w historii, ale zwalniają miejsce. Agent jest zarządzany w panelu; nie jest jednostką przemieszczaną po mapie.

| Operacja | Koszt z góry | Czas | Efekt sukcesu |
|---|---|---|---|
| Rozpoznanie miasta | 55 | 3 tury | Odkrycie pól w promieniu trzech heksów; datowany raport o ludności, zdrowiu, projekcie, pracy, garnizonie i skarbie państwa |
| Kradzież badań | 110 | 4 tury | 35% kosztu kopiowanej technologii, w górę do całego punktu, najwyżej 120 nauki do puli |
| Sabotaż produkcji | 95 | 4 tury | Utrata do 60 punktów rzeczywiście zgromadzonej pracy w mieście |

Wybierz odkryte obce miasto i dostępnego agenta. Pokój lub traktat nie są wymagane; te same zasady ryzyka obowiązują podczas wojny. Agent prowadzi jedną operację. Siatka w tym samym mieście wymaga ośmiu tur odstępu od rozpoczęcia poprzedniej operacji danego państwa, również po jej odwołaniu. Wycofanie jest możliwe przed rozstrzygnięciem i od razu zwalnia agenta, bez zwrotu kosztu.

Rozpoznanie nie daje stałego podglądu: raport opisuje stan z jednej tury, a odkryte pole nadal podlega zwykłej mgle widoczności. Kradzież wybiera najtańszą technologię znaną celowi, nieznaną wysyłającemu, z już spełnionymi wymaganiami. Dodaje naukę do wspólnej puli bez automatycznego odkrycia i bez zabierania punktów ofierze. Jeśli przed końcem poznasz tę technologię, misja zostanie odwołana bez nagrody lub zwrotu. Sabotaż nie usuwa budynków, kolejki ani wstrzymania produkcji. Jeśli miasto zdąży skończyć budowę, efekt na pozostałej pracy może być mniejszy, nawet zerowy. Zmiana właściciela celu przerywa trwające misje bez zwrotu; nie uwalnia agentów już schwytanych przez dawne państwo.

**Ryzyko** liczymy ponownie przy rozstrzygnięciu po produkcji i ruchach AI. Podgląd pokazuje bieżące szanse, które mogą się zmienić. Obrona to +5 za Pismo właściciela, +15 za sąd, +5 przy stabilności co najmniej 70 i +25 za czynną ochronę kontrwywiadu. Ochrona kosztuje 60 i działa przez osiem tur, np. kupiona w T1 do początku T9; nie można jej przedłużać, gdy jeszcze trwa. Zmiana właściciela ją kończy. Działa również przeciw już wysłanym operacjom.

Sukces bazowy to 85% rozpoznania, 60% kradzieży i 65% sabotażu. Dodaj 5 punktów procentowych za stopień agenta i odejmij obronę miasta; wynik ograniczamy do 20–95%. Wykrycie bazowe to odpowiednio 20%, 35% i 45%, powiększone o połowę obrony z dzieleniem całkowitym i pomniejszone o 3 punkty za stopień; granice 5–80%. Sukces daje +2 doświadczenia, niewykryta porażka +1. Każde trzy punkty doświadczenia dają stopień, najwyżej piąty; zapis przechowuje do 30 doświadczenia.

Sukces i wykrycie mają osobne losowania. Sukces z wykryciem zachowuje efekt, ale obciąża relacje. Wykrycie to zaufanie -15, krzywdy poszkodowanego +25 i prestiż wysyłającego -2. Niepowodzenie z wykryciem oznacza niewolę. Generator i wyniki są zapisywane, więc odtworzenie tych samych działań z tego samego zapisu daje ten sam przebieg. Podgląd nie losuje wyniku.

**Schwytany agent** czeka na decyzję rady, za aktualnie otwartą sprawą. Okup **100 złota** trafia do państwa przetrzymującego agenta i przywraca go do służby. Publiczne przeprosiny wymagają pokoju, kosztują **40 złota**, **8 prestiżu** i **3 poparcia ludu**, dając powrót i **+8 zaufania**. Wyparcie się jest darmowe, lecz bezpowrotnie traci agenta; **prestiż -4**, **rody +3**. Czwarta odpowiedź pozwala wymienić go za agenta tego państwa w naszej niewoli, bez opłaty. Wcześniejsze skutki wykrycia nadal obowiązują. Wojna i pusty skarbiec nie blokują możliwości odmowy. Podbój dawnego celu nie przenosi niewoli na nowego właściciela miasta.

**AI** korzysta z tych samych kosztów, ryzyka, efektów, agentów i ochrony. Od T20 raz na własną fazę w cyklu 12 tur rozważa operację przeciw państwu, z którym walczy lub ma zaufanie poniżej -10. Wymaga Pisma i co najmniej 250 złota przed rekrutacją oraz wysłaniem. Wybiera dostępne badania, a gdy ich nie ma — sabotaż miasta z co najmniej 20 pracy. Po wykrytej ingerencji może kupić kontrwywiad za własne złoto. Za agenta schwytanego przez inne AI płaci okup, jeśli ma 100 złota; w przeciwnym razie go porzuca. Gdy agenta schwyta gracz, AI czeka na decyzję jego rady. Wykryte obce operacje są w raporcie, a niewykryte nie zdradzają sprawcy w panelu lub kronice.

To abstrakcyjny model siatek z ustalonym czasem operacji, bez osobnych tras podróży, podwójnych agentów, zabójstw, zamachów stanu i ręcznych operacji na karawanach. AI korzysta z informacji o świecie dostępnych dotychczasowej symulacji. Balans ryzyka i kosztów nadal wymaga rozgrywek z ludźmi.

## Jeńcy wywiadu: ludzie za granicą

Schwytanie obcego agenta otwiera osobną sprawę rady. **Dyplomacja → Wywiad i kontrwywiad → Obcy agenci w naszej niewoli** pokazuje osoby oczekujące na decyzję i internowanych. Decyzje dotyczą konkretnych ludzi i trafiają do raportu oraz kroniki.

| Decyzja | Skutek |
|---|---|
| Okup | Dokładnie 100 złota z obcego skarbu do naszego; agent wraca. Przycisk jest niedostępny, jeśli wysyłający nie ma środków. |
| Wymiana | Jeden własny agent przetrzymywany przez to samo państwo wraca w zamian za jednego obcego. Bez opłaty; dostępne również podczas wojny. |
| Zwolnienie z listem | Agent wraca od razu. Zaufanie +12, prestiż +4, poparcie rodów -3. |
| Internowanie | Agent pozostaje w niewoli przez osiem tur i zajmuje jedno z trzech miejsc w swojej służbie. Zaufanie -8, lud +3. |

Internowanie zarządzone w T5 kończy się na początku T13; powrót następuje automatycznie. Decyzji nie można cofnąć, sprzedać internowanego ani użyć go do wymiany. Wojna, pusty skarb i zmiana właściciela miasta nie skracają terminu. Model nie nalicza kosztów utrzymania jeńców.

Wymiana jest też czwartą odpowiedzią w dotychczasowym dialogu o naszym schwytanym agencie. Wymaga nierozstrzygniętej sprawy obcego agenta tego samego państwa; przy kilku kandydatach wybieramy najdłużej przetrzymywaną osobę, następnie niższy identyfikator. Jedna decyzja zwalnia dokładnie jedną parę. Jeśli obie strony czekają na rozstrzygnięcie, sprawa obcego agenta trafia przed nasz okup, bez zastępowania już otwartej rozmowy.

Żadna decyzja nie cofa wcześniejszych krzywd i skutków wykrycia. Zwolnienie pomaga zaufaniu, ale samo nie kończy wojny. Zamknięte sprawy pozostają w raportach; agenci, którzy wrócili, mogą znów prowadzić operacje. Nie ma negocjowania dowolnej ceny, wymiany grupowej, ucieczek, przesłuchań ani podwójnych agentów.

## Skarb: pieczęć i dług

W **Szlaki → Pożyczki i zobowiązania** lub tym samym panelu w **Dyplomacji** wybierz państwo kupców i kwotę. Potrzebujesz **Monety**, pokoju, traktatu handlowego i zaufania co najmniej +10. Oba państwa muszą jeszcze istnieć, a skarb wspierający kupców musi mieć kapitał. Kwota rzeczywiście przechodzi z jego zasobów do Twoich; AI odczuje mniejszy zapas złota.

| Finansowanie teraz | Spłata po 12 turach | Po jednorazowym odroczeniu |
|---|---|---|
| 100 złota | 120 | 130 + osobna opłata 20 |
| 200 złota | 240 | 260 + osobna opłata 20 |
| 300 złota | 360 | 390 + osobna opłata 20 |

Najwyżej **dwie otwarte pożyczki**, w tym jedna od kupców danego państwa. Kolejną z tego samego państwa można podpisać dopiero po ośmiu turach od poprzedniej i po jej zamknięciu. Wcześniejsza spłata nadal obejmuje całą umówioną kwotę; nie ma rabatu za niewykorzystane tury. Panel pokazuje łączne zobowiązania, historię, terminy i zapłacone kwoty. Saldo dochodu na turę nie odejmuje tych jednorazowych płatności. Punktacja majątku opiera się na złocie powiększonym o należności i pomniejszonym o otwarte zobowiązania. Sam kapitał pożyczki nie tworzy darmowych punktów zwycięstwa; odsetki są przyszłym kosztem pożyczkobiorcy i należnością wierzyciela.

Na dwie tury przed terminem pojawia się przypomnienie w kronice i komunikacie tury, jeśli pierwszeństwa nie mają inne pilne wiadomości. Umowa z T1 zapada w T13. Pieniądze nie znikają automatycznie: kupcy przychodzą do rady. Istniejący dialog zachowuje pierwszeństwo; wezwanie czeka bez dopisywania karnych odsetek. Każdą pożyczkę trzeba rozstrzygnąć osobno.

- **Spłata:** przelew pełnej umówionej kwoty wierzycielom, zaufanie +5. Możliwa z panelu także wcześniej i także podczas wojny. Wojna oraz upadek państwa wierzycieli nie kasują umowy.
- **Odroczenie:** tylko raz i dopiero w rozmowie w terminie. Zapłać natychmiast 20 złota, zwiększ dług o 10% pierwotnego kapitału i otrzymaj sześć tur od bieżącej decyzji; zaufanie -5. Kupcy wymagają pokoju i istniejącego państwa, lecz nie ponawiają warunku traktatu i zaufania. Drugiego odroczenia nie będzie.
- **Zerwanie zobowiązania:** bez wypłaty złota; stabilność -12, lud -4, rody -8, prestiż -10. Zaufanie wierzycieli -30, ich krzywdy +35. Traktat handlowy z nimi kończy się od razu. Opłacone karawany wymagające tego traktatu mogą utknąć i przepaść po trzech kolejnych postojach bez zwrotu ceny. Nowe pożyczki od wszystkich kupców są zamknięte przez 24 tury. Pozostałe otwarte umowy nadal obowiązują.

Zerwanie w T13 blokuje finansowanie do T36 włącznie; od T37 można znów starać się o pożyczkę, spełniając zwykłe warunki. Skutki społeczne i relacje są ograniczane do ich zwykłych przedziałów. Nawet pusty skarbiec ma dostępną odmowę, więc termin nie blokuje kampanii bez możliwej decyzji. Zakończenie kampanii kończy dalsze działania.

To model finansowania państwa w abstrakcyjnych turach, bez dziennego oprocentowania, licytacji obligacji, przejmowania miast przez wierzycieli lub pożyczek zaciąganych samodzielnie przez AI. Kupcy są reprezentowani przez skarb wskazanego państwa; po jego upadku dawny rachunek nadal przyjmuje rozliczenie.

## Wojsko: utrzymać pozycję

Wybierz własny oddział i otwórz **Rozkazy wojskowe**, albo użyj **Wojsko i stałe rozkazy**, aby odnaleźć całą armię. Tab pomija oddziały z tymi rozkazami oraz przydzielone eskorty. Możesz nadal wybrać je na mapie lub w przeglądzie armii. Na mapie `F0`, `F1`, `F2` oznacza przygotowanie pozycji, a `+` rekonwalescencję.

- **Okop oddział:** pełny ruch jest zużywany na przygotowanie. Po rozliczeniu pierwszej tury obrona oddziału rośnie o 20%, po drugiej o 40%; nie ma dalszego wzrostu. Premia mnoży siłę obrońcy po uwzględnieniu zdrowia i dostaw, przed osobnym dodatkiem +5 z lasu lub wzgórz. Nie wzmacnia ataku ani obrony samego miasta, gdy nie ma już garnizonu. Ostrzał również podlega tej premii.
- **Rekonwalescencja:** także wymaga i zużywa pełny ruch, zastępując przygotowane pozycje. Leczy 20 zdrowia na turę z dostawami lub 28 po Medycynie, do limitu 100. Przy pełnym zdrowiu rozkaz kończy się automatycznie; trafienie przeciwnika przerywa go wcześniej. Zwykły odpoczynek oraz okopywanie leczą odpowiednio 12 lub 20 po Medycynie.
- **Bez dostaw:** każdy odpoczynek leczy tylko 3 zdrowia, po czym zwykłe wojsko traci 5. Najpierw stosujemy limit 100, potem straty; oddział może więc tracić zdrowie mimo rekonwalescencji. Zwiadowcy i osadnicy zachowują dotychczasowe zwolnienie ze strat za brak dostaw. Osadnicy nie mogą przyjąć tych rozkazów wojskowych.
- **Powrót do działania:** odwołanie rozkazu usuwa umocnienia i nie zwraca wydanego ruchu. W kolejnej turze dostępny jest nowy ruch. Poprawny marsz, rozkaz dalszego marszu, atak, niszczenie drogi lub przydział eskorty porzuca stały rozkaz i całą premię. Odrzucona czynność i anulowany podgląd walki niczego nie odbierają. Przymusowe wycofanie po utracie miasta lub pokoju również usuwa pozycje.

Umocnienia utrzymują się po ostrzale i walce obronnej, dopóki oddział żyje i pozostaje na pozycji. Rozkazy nie pobierają dodatkowego złota; zwykłe utrzymanie armii nadal obowiązuje. AI przygotowuje stacjonujące przy miastach oddziały bojowe oraz kieruje rannych z dostawami na rekonwalescencję. W czasie wojny nadal wykorzystuje dotychczasowe reguły ataku i odwrotu.

## Poselstwa o prawo przejazdu

**Granice i tranzyt** w raporcie karawany lub **Sprawdź granice wybranej trasy** w formularzu kontraktu wskazuje brakujące zgody na jednym potencjalnym bezpiecznym korytarzu. Dostępny neutralny objazd wyklucza zgłoszenie blokady granic. Jeśli droga wymaga kilku państw, każde musi wyrazić własną zgodę. Raport nie proponuje poselstwa jako sposobu obejścia wojny, embarga, gór lub okupacji przez wrogie wojsko.

W **Dyplomacja → Przejazd kupców** wybierz prośbę o bezpłatny tranzyt albo jednorazową opłatę 60 złota. Podróż posłów kosztuje zawsze 20 złota i trwa dwie tury. Ocena to zaufanie +10 za traktat z gospodarzem +35 za ofertę opłaty -15 wobec Kharu; przyjęcie wymaga 15. Nie ma losowania. Sytuację i środki sprawdzamy ponownie w dniu odpowiedzi. Opłata 60 złota przechodzi do gospodarza dopiero po przyjęciu; jeśli ją wcześniej wydasz, propozycja zostanie odrzucona. Przyjęcie daje relacje +5, odmowa -5. Obowiązuje wspólna dla wszystkich poselstw przerwa dziesięciu tur i jedna rozmowa z danym państwem na turę.

Zgoda działa przez 12 faz rynku od tury przyjęcia, także dla już wysłanych ładunków oraz rozwiązanych lub wygasłych kontraktów. Zastępuje wyłącznie wymóg traktatów gospodarza z obiema stronami transportu. Traktat między kupującym i sprzedawcą nadal jest konieczny. Zgoda nie obejmuje transportów między samymi państwami AI ani przemarszu wojska. Wojna z którąkolwiek stroną i embargo zawieszają przejazd; po ich ustaniu pozostały czas zgody może zostać wykorzystany.

Zgoda przyjęta w T5 pozwala przejeżdżać w fazach T5–T16 i wygasa przed rynkiem T17. Na dwie tury przed końcem oraz w dniu wygaśnięcia pojawia się wpis kroniki i komunikat przy zmianie tury, o ile pierwszeństwa nie mają pilniejsze wieści. Aktywnej zgody nie można przedłużyć z góry; po wygaśnięciu można wysłać nowe poselstwo. Eskorta zatrzyma się przed wojskowo niedostępną granicą. Zablokowana karawana nadal przepada przy trzecim kolejnym postoju, również wewnątrz obcego kraju. Poselstwo nie zatrzymuje tego licznika i może wrócić za późno; utraconego manifestu nie przywraca.

## Rachunki prowincji i korupcja

W **Ludność → Rachunki i kontrole prowincji** sprawdzisz korupcję, jej cel, bieżący i ostatni ubytek dochodów oraz nieujęte kwoty z poprzednich rozliczeń. Rachunki są dostępne również z zarządzania miastem. Bilans skarbu podaje dochód miast już po potrąceniu korupcji: nie odejmuje jej drugi raz.

Korupcja odbiera swój procent złota wytwarzanego przez miasto, po dotychczasowych premiach podatkowych i wydajności. Nie zabiera osobno podatków rynkowych, płatności za towary, traktatów ani jednorazowych nagród. Przy pobraniu dochodu utracona kwota trafia do lokalnego rejestru, a korupcja przesuwa się o jeden punkt w stronę celu. Prognozy nie tworzą braków.

Cel korupcji wynosi `6 + 2 × odległość od stolicy + niepokoje / 5 + (100 - stabilność) / 10`, z dzieleniem całkowitym. Autonomia i zarząd wojskowy dodają po 8 względem wspólnej administracji; sąd odejmuje 12, Służba publiczna 8, Konstytucja 6, a czynny nadzór 20. Wynik mieści się w przedziale 0–65. Stolica to najstarsze obecnie własne miasto. Drogi nie zmieniają bezpośrednio tego celu.

Kontrola wymaga **Pisma**, korupcji co najmniej 10% albo 10 złota nieujętych dochodów. Wysłanie kosztuje **35 złota**, wynik wraca po **trzech turach**. W chwili powstania raportu ustalona kwota zostaje zamknięta, a nowe straty liczymy osobno. To nie jest wypłata. Gotowe raporty czekają w historii i sali rady, nie zastępując już otwartej sprawy. Kolejna kontrola tej samej prowincji jest możliwa po 12 turach od wysłania poprzedniej i po jej rozpatrzeniu.

| Rozstrzygnięcie | Skutek finansowy i miejscowy | Skutek dla stronnictw |
|---|---|---|
| Jawny proces | Odzyskanie 75% ustalonych braków; korupcja -25 punktów, niepokoje +6, lojalność -4 | Stabilność -2, lud +6, rody -10 |
| Niezależny urząd | Dodatkowo -60 złota, bez odzyskania starych braków; korupcja -35, niepokoje -5, lojalność +4; cel korupcji -20 przez 12 tur | Lud +3, rody -6, uczeni +8 |
| Ugoda z rodami | Odzyskanie 25% ustalonych braków; korupcja -8, niepokoje -3, lojalność +3 | Lud -5, rody +6 |

Odzysk zaokrąglamy w dół do pełnego złota, pozostała część ustalonych braków przepada lub jest umarzana. Decyzja nie obejmuje nowych strat i nie może być powtórzona. Pusty skarbiec nadal pozwala na proces albo ugodę. Nadzór ustanowiony w T4 obowiązuje do początku T16. Przejęcie miasta zeruje nieobjęte raportem braki i nadzór, zamyka otwarte kontrole poprzedniego właściciela i nie wypłaca środków zdobywcy. Odbicie miasta nie przywraca zamkniętego raportu. Utracona opłata za kontrolerów nie jest zwracana.

AI ponosi te same procentowe straty miejskie, z uwzględnieniem mnożnika dochodu poziomu trudności. Korzysta z uproszczonego porządkowania urzędu: przy korupcji co najmniej 30 po zmianie celu, skarbcu co najmniej 180 i odstępie 12 tur wydaje 60 złota, obniża korupcję o 25 i ustanawia nadzór na osiem tur. Odpisuje wcześniejsze braki, nie otrzymuje odzysku. Nie przeprowadza własnych trwających trzy tury komisji ani decyzji rady.

## Plany produkcji miast

Wybierz własne miasto i otwórz **Plan produkcji**. Ten sam formularz znajdziesz w zarządzaniu prowincją i przez **Wybierz projekt → Zaplanuj kolejkę produkcji**. Bieżący projekt jest osobno; kolejka mieści do **ośmiu następnych zleceń**. Dodanie, usuwanie i przesuwanie przyszłych pozycji nie kosztuje złota ani zgromadzonej pracy. Formularz zachowuje wybrany projekt, tryb powtarzania i miejsce przewinięcia po edycji.

W planie mogą znaleźć się budowle wymagające przyszłych odkryć oraz oddziały, dla których nie masz jeszcze zasobów lub mieszkańców. Warunki są sprawdzane przy każdej fazie produkcji, po badaniach i zmianach ludności. Uniwersytet poczeka na swoją technologię, kawaleria na konie, a osadnicy na co najmniej trzech mieszkańców. Zablokowany bieżący projekt nie znika ani nie przepuszcza dalszych zleceń. Zachowuje zgromadzoną pracę i **nie odkłada nowej**; dotyczy to również dawnych projektów, które straciły wymagania. Po ich odzyskaniu produkcja rusza automatycznie. Możesz ręcznie wybrać inne zlecenie wcześniej.

Po ukończeniu bieżącego projektu miasto wybiera pierwszą pozycję i zachowuje **100% nadwyżki pracy**. Kończymy najwyżej jeden projekt na miasto i turę, nawet przy dużej nadwyżce. Już ukończone budynki pomijamy w przyszłej kolejce. Przy pustym planie pozostaje dotychczasowy automatyczny wybór dostępnej budowy. Jednego trwałego budynku nie można dodać wielokrotnie do kolejki ani oznaczyć jako powtarzanego.

**Powtarzanie** działa dla oddziałów, robót publicznych i święta wspólnoty. Ukończone zamówienie wraca na koniec planu, za pozostałe zlecenia. Samotne powtarzane zamówienie realizuje się ponownie. Utrzymanie i wymagania oddziałów nadal obowiązują; każda rekrutacja osadników zużywa mieszkańca, a po spadku populacji do dwóch następna czeka. Wyłączenie powtarzania nie usuwa obecnego projektu. Usunięcie wszystkich oczekujących pozycji też nie odwołuje powtarzania bieżącego zlecenia — służy do tego osobny przycisk.

**Wstrzymanie produkcji** zachowuje obecny postęp i plan; nie powstają nowe punkty pracy ani ukończenia. Żywność, wzrost, dochody, korupcja i inne procesy miejskie działają dalej. Wznowienie nie omija wymagań. Punkty niewykorzystanej produkcji nie są automatycznie zamieniane na złoto lub naukę.

**Rozpocznij pierwsze zlecenie teraz** pozwala przerwać aktualny projekt, również na rzecz projektu oczekującego na warunki. Podobnie jak zwykła ręczna zmiana zachowuje 80% pracy. Wybranie tego samego projektu zachowuje całość. Tryb powtarzania zostaje zastąpiony ustawieniem nowego zlecenia, pozostały plan pozostaje w mocy, a wstrzymanie nie jest znoszone automatycznie. Po zmianie suwerenności miasta kolejka, powtarzanie i wstrzymanie poprzedniej administracji są usuwane; dotyczy to również pokojowego zwrotu. AI nadal wybiera swoje budowy za pomocą dotychczasowych heurystyk, bez układania własnych długich kolejek.

## Pierwsze dziesięć tur

1. Rozpocznij nową historię i odpowiedz kanclerce w sali rady. Każda opcja pokazuje pełne zmiany liczbowe oraz przyszłe skutki.
2. Wybierz zwiadowcę LPM. Wskaż PPM pobliskie odkryte pole, aby przesunąć oddział i odsłonić kolejne ziemie. Ruiny dają złoto, naukę i prestiż.
3. Odkryj Rolnictwo. W Wysogrodzie wybierz spichlerz, a potem osadników. Rekrutacja wymaga co najmniej trzech mieszkańców i zużywa jednego.
4. Miasto można założyć na lądzie co najmniej cztery pola od innych miast. Wybierz osadników i przycisk założenia miasta w panelu.
5. Własne pola możesz ulepszać za złoto. Miasto wykorzystuje najlepsze pola w promieniu dwóch heksów, do limitu zależnego od ludności. Ulepszenie odległego, niepracującego pola nie daje dochodu.
6. Zachowaj rezerwę skarbu. Biblioteki podnoszą naukę, traktaty dają dochód, a nadmierna armia i wysokie podatki mają koszty społeczne.
7. Po Garncarstwie buduj drogi na własnej lub neutralnej ziemi. Panel „Szlaki” wycenia całe połączenie ze stolicą i pokazuje przychody oraz koszty utrzymania. Bezpiecznie połączone prowincje mają premię do bazowego złota i nauki.

## Dostawy i rozkazy

Każde własne miasto wysyła dostawy. Liczy się koszt dostępnej trasy, a nie odległość w linii prostej: zwykły ląd kosztuje 1, lasy, wzgórza i mokradła 2, a połączone drogi 0,5. Zima podnosi koszt poza drogami o 25%; na obcej ziemi transport kosztuje dodatkowe 0,5. Podstawowy zasięg kosztowy to 6; Szlaki handlowe i Inżynieria zwiększają go po 2. Wybrane patrole rady mogą dodać jeszcze 1.

Wrogie oddziały bojowe blokują własne pole i sąsiedztwo. Własne wojska bojowe osłaniają zajmowany heks przed sąsiednią strefą przeciwnika. Droga odcięta przez wroga nie daje premii połączenia miasta. Bez dostaw siła spada do 72%, a armie tracą 5 zdrowia/turę; zwiadowcy i osadnicy nie ponoszą strat marszowych. Włącz przycisk „Dostawy”, aby zobaczyć obszary zaopatrzenia i blokady.

PPM na odkrytym, odległym polu wydaje rozkaz marszu. Oddział wykona dostępny odcinek teraz i będzie szedł dalej po następnych turach. Przy zmianie granic lub blokadzie może znaleźć inną dostępną trasę; jeśli żadna nie istnieje, przerwie marsz. Samodzielnie nie atakuje. Rozkaz można anulować w panelu oddziału. PPM na przeciwniku otwiera podgląd starcia; dopiero „Wykonaj atak” zużywa ruch i rozstrzyga walkę.

## Życie prowincji

Panel **Ludność** pokazuje potrzeby i migracje, a **Zarządzaj** otwiera działania dla konkretnego miasta. W widoku miasta przycisk „Mieszkańcy” prowadzi do tego samego okna.

- Miejsca mieszkalne: początkowo 6; dzielnica mieszkalna +5, akwedukt +2, służba publiczna +2, parlament +3. Miejscowe prawa mogą dodać kolejne 2.
- Pojemność opieki: początkowo 6; rzeka +1, akwedukt +4, lecznica +6, medycyna +3. Priorytet rzemiosła obniża ją o 1.
- Kierunek rozwoju kosztuje 15 złota, zmienia się raz na turę: rolnictwo +20% żywności i -10% produkcji; rzemiosło +20% produkcji i -10% żywności; nauka +25% nauki i -10% złota. Równowaga nie ma tych modyfikatorów.
- Pomoc kosztuje 50 złota: +25 zapasów żywności, -8 niepokojów i +4 dobrobytu, raz na pięć tur w danym mieście.

Połączone bezpiecznymi drogami miasta tworzą regionalne rynki, także bez połączenia ze stolicą. Nadwyżka żywności po wyżywieniu mieszkańców może być eksportowana do miasta z deficytem: maksymalnie 6 na miasto i turę w każdą stronę. Najpilniejsze niedobory mają pierwszeństwo. Eksport zmniejsza żywność pozostającą na wzrost, import nie tworzy nowej żywności. Blokada przerywa wymianę. Sól i barwniki z zasobów oraz magazynów są widoczne jako dostęp regionu do towaru; wpływ na dobrobyt wynika teraz z rzeczywistego zużycia. Sam traktat i targowisko nie przyznają już automatycznych towarów zagranicznych.

Przeludnienie, niedostateczna opieka i brak żywności zwiększają niepokoje i obniżają docelowy dobrobyt. Dobrobyt zbliża się do celu o maksymalnie 2 punkty na turę i zmienia wydajność produkcji, nauki, złota i kultury w przedziale 90–110%. Pierwsza tura głodu powoduje niepokoje; co druga kolejna tura z pustymi zapasami i niedoborem odbiera mieszkańca.

Migracje wewnątrz państwa są sprawdzane co cztery tury. Miasto źródłowe musi mieć co najmniej trzech mieszkańców i problemy lokalne. Cel musi leżeć w tej samej bezpiecznej sieci, mieć wolne mieszkania i opiekę, nadwyżkę żywności co najmniej 2 oraz dobrobyt wyższy o przynajmniej 10. Przenosi się jeden mieszkaniec; liczba ludności pozostaje stała. Dane miasto uczestniczy ponownie najwcześniej po ośmiu turach. Napływ zwiększa niepokoje o 4, chyba że przyjęto prawo integracji.

Głód, brak mieszkań i napływ ludności wywołują sprawy rady przypisane do wskazanego miasta. Każda taka sprawa jest rozstrzygana raz na prowincję, z odstępem co najmniej 15 tur między sprawami. Lokalne prawa pozostają przy mieście i są widoczne w jego panelu. Nie ma symulacji indywidualnych gospodarstw; ceny obejmują trzy towary opisane poniżej.

## Rynki, ceny i kontrakty

Panel **Rynek** pokazuje prognozę zapasów, produkcji, popytu i cen. **Bilans towarów** rozpisuje każdą jednostkę: zapas początkowy + produkcja + import krajowy i zagraniczny - eksport - zużycie - utrata = zapas końcowy. Żywność nadal ma odrębne spichlerze i system przesyłek.

| Towar | Popyt na turę | Skutek zużycia |
|---|---|---|
| Żelazo | 0,15 × mieszkańcy, +1 za warsztat, +2 za fabrykę | Mnożnik produkcji od 0,90 przy braku do 1,00 przy pełnym zaspokojeniu |
| Sól | 0,35 × mieszkańcy | Wraz z barwnikami zmienia cel dobrobytu |
| Barwniki | 0,20 × mieszkańcy, +1 za dom bankierski | Wraz z solą zmienia cel dobrobytu |

Wpływ obu dóbr na docelowy dobrobyt wynosi `zaokrąglij(6 × (pokrycie soli + pokrycie barwników)) - 4`, czyli od -4 do +8. Nie zmienia dobrobytu natychmiast: ten nadal zbliża się do celu o maksymalnie 2 punkty na turę. Żelazo z magazynów nie zastępuje warunków surowcowych dotychczasowych premii bojowych.

Pracujące własne pole żelaza, soli lub barwników produkuje 2 jednostki towaru na turę. Kopalnia dodaje 2 dla żelaza, placówka handlowa 1 dla towaru. Pole współdzielone przez zasięgi miast przypada tylko jednemu pracującemu miastu, z pierwszeństwem najbliższego, potem starszego. Nie produkuje pod blokadą. Nie ma jeszcze wyczerpywania złóż.

Miasto mieści 50 jednostek każdego towaru; targowisko dodaje 25, **Skład towarowy** kolejne 100. Skład kosztuje 150 produkcji i wymaga technologii **Moneta**. Nadmiar po zużyciu przepada i jest wykazany w bilansie. Zajęcie miasta przejmuje jego zapasy, a stare raporty podatkowe są zerowane.

Miasta jednej bezpiecznej sieci drogowej mają wspólną cenę. Bazy to 8 złota dla żelaza, 5 dla soli i 9 dla barwników. Cena wynika z `baza × (0,75 + popyt / (zapasy + nowa produkcja + przybyłe ładunki + 1))`, z limitem trzykrotnej bazy i zaokrągleniem do dwóch miejsc. Obliczana jest po przybyciu karawan, przed nowymi załadunkami i zużyciem. Wymiana krajowa najpierw pokrywa potrzeby z własnych zapasów miasta, następnie z nadwyżek połączonych miast.

W **Podpisz kontrakt handlowy** wybierz import lub eksport, miasta, towar i limit 1–6 jednostek na karawanę. Umowa kosztuje 15 złota i obowiązuje do rozliczenia tury początkowej +12. Limit to trzy aktywne umowy na każde państwo i jedna karawana w drodze na umowę. Wymagane są targowiska i Szlaki handlowe po obu stronach, pokojowy traktat, bezpieczne połączenie każdego miasta ze swoją stolicą oraz dostępna trasa lądowa.

Cena kontraktowa wynosi 115% ceny regionalnej sprzedawcy. Przy załadunku zależy od aktualnego rynku; podpis nie blokuje ceny, zapasu ani złota. Ładunek może być częściowy z powodu braku towarów lub funduszy. Nabywca płaci przy wysyłce bezpośrednio sprzedawcy, bez mnożników trudności. Towar opuszcza magazyny sprzedawcy i pozostaje w transporcie aż do przybycia; import w bilansie odbiorcy oznacza dopiero faktyczny przyjazd. Umowy rozliczane są w kolejności podpisania.

### Karawany i ochrona dróg

Panel **Rynek → Karawany i trasy** pokazuje manifesty, zapłacone kwoty, pozycje, pozostały czas podróży i przyczyny postoju. **Pokaż na mapie** centruje kamerę i zaznacza pozostałą trasę na odkrytych heksach. Znacznik wozu oznacza karawanę; pomarańczowy kolor — postój. Można zakończyć śledzenie przyciskiem w panelu rynku. Kliknięcie pola z własnym transportem pozwala otworzyć raport także z panelu terenu.

Karawana otrzymuje 3 punkty ruchu na fazę rynku. Połączone drogi kosztują 0,5, lasy, wzgórza i mokradła 2, pozostały ląd 1. Niewykorzystane punkty nie przechodzą między turami. Załadowany transport zaczyna marsz przy następnym rozliczeniu. Podany czas jest prognozą według obecnej trasy, bez przyszłych blokad; transport nie używa sezonowych modyfikatorów dostaw wojskowych.

Szlak biegnie przez ziemie obu stron i neutralny ląd. Przejazd przez trzecie państwo wymaga jego pokojowych traktatów handlowych z obiema stronami albo czasowej zgody na przewóz ładunków Marchii. Gospodarz musi pozostawać w pokoju z obiema stronami i nie stosować embarga. Zmiany dróg, granic i wojsk powodują ponowne wyszukiwanie najtańszej bezpiecznej trasy, bez teleportowania ładunku.

Wrogie wobec którejkolwiek ze stron oddziały państwa trzeciego stwarzają zagrożenie na swoim polu i w sąsiedztwie. Oddział nabywcy albo sprzedawcy (również zwiadowca, bez osadników) w promieniu jednego pola osłania przed sąsiednią strefą zagrożenia; miasto stron osłania własne pole. Wrogi oddział bezpośrednio na heksie transportu przechwytuje go zawsze. Karawana omija nieosłonięte zagrożenia; jeśli przeciwnik podejdzie do jej obecnej pozycji, przed końcem tury trzeba zapewnić osłonę lub usunąć przeciwnika. Wyznaczona eskorta może teraz podążać razem z transportem. Nie ma osobnej komendy ataku na wóz.

### Rozkaz eskorty

1. W **Rynek → Karawany i trasy** wybierz **Pokaż na mapie** i przesuń własny oddział na pole aktywnego wozu. Eskorta nie zbiera się sama ani nie ściga karawany z odległej pozycji.
2. W raporcie wybierz **Wyznacz eskortę**, a następnie gotowy oddział. Ten sam rozkaz można wydać z panelu jednostki stojącej na polu wozu. Potrzebne jest co najmniej 0,5 pozostałego punktu ruchu. Osadnicy są wykluczeni; jedna karawana może mieć jeden wyznaczony oddział, należący do strony umowy.
3. Przy kolejnym rozliczeniu rynku oddział i wóz ruszą razem. Rozkaz zastępuje wcześniejszy marsz, ale jego przyjęcie nie przesuwa wojska i nie pobiera złota. Zwykłe utrzymanie armii działa dalej.

Wspólny ruch zużywa pozostałe punkty oddziału oraz budżet 3 punktów karawany. Włócznicy mają 2 punkty, więc na zwykłym lądzie przejdą z wozem do dwóch heksów, a po połączonej drodze do czterech. Zwiadowcy i kawaleria mają 3 punkty. Brak punktów na kolejny heks powoduje postój; następna tura przywraca zwykły budżet wojska. Niewykorzystany ruch nie przechodzi między turami. Trzy kolejne pełne fazy postoju nadal oznaczają utratę ładunku.

Eskorta nie wykonuje samodzielnego marszu, ataku ani niszczenia dróg i nie korzysta z biernego leczenia podczas służby. Aby użyć oddziału inaczej, wybierz **Odwołaj eskortę** w raporcie lub panelu jednostki. Wydane punkty nie wracają. Dostawy wojskowe i straty z ich braku działają normalnie. Ruch eskorty odsłania teren i może zbadać mijane ruiny; prognoza nie przyznaje za nie nagród.

Traktat handlowy **nie daje prawa przejścia armii**. Gdy kolejne pole jest niedostępne wojskowo — np. pokojowe obce terytorium, obce miasto albo pole obcego oddziału — eskorta kończy się na ostatnim osiągniętym heksie. Wóz ponownie ocenia dalszą trasę bez ruchomej osłony i może kontynuować samotnie lub czekać. Oddział nie wraca automatycznie do stolicy. Algorytm wybiera trasę karawany; nie szuka osobnego wojskowego objazdu dla zachowania eskorty. Raport ostrzega o przewidywanym odłączeniu. Szacowany czas uwzględnia ruch oddziału na bieżącej trasie; po odłączeniu i zmianie bezpieczeństwa może się zmienić.

Eskorta utrzymuje istniejącą ochronę przed sąsiednią strefą zagrożenia. Nie rozgrywa automatycznej bitwy i nie daje odporności na bezpośrednie zajęcie pola wozu przez wroga. Przybycie, utrata transportu albo odłączenie oddziału kończą rozkaz. Ewakuacja po zdobyciu miasta również usuwa nieaktualny przydział. Kolejna karawana tej samej umowy wymaga nowego rozkazu.

Raport pokazuje nazwę i państwo oddziału, pozostały ruch oraz prognozę. Na mapie litera **E** oznacza wyznaczoną eskortę. AI może przydzielić własnego, zdrowego zwiadowcę stojącego na polu transportu swojej strony umowy i zachowuje go przy wozie; nie przejmuje rozkazów gracza. Nie planuje jeszcze złożonych marszów na miejsce zbiórki ani obrony całej sieci drogowej.

Embargo, wojna między stronami i zerwany traktat zatrzymują wysłany transport. Brak dostępnej trasy także powoduje postój. **Trzy kolejne fazy postoju oznaczają utratę całego ładunku bez zwrotu zapłaty**; wznowienie marszu zeruje licznik. Zmiana właściciela któregokolwiek miasta umowy oznacza utratę przy następnym rozliczeniu. Zdarzenia są zapisywane w kronice, a interfejs zgłasza kłopoty na szlaku. Nie ma losowych rozbójników ani ubezpieczenia; przechwycenie wynika ze stanu mapy i nie daje napastnikowi nowego złota.

Wygaśnięcie lub dobrowolne rozwiązanie umowy zatrzymuje kolejne załadunki. Już opłacone karawany kontynuują podróż i mogą dotrzeć później. Gdy poprzedni transport dotrze albo zostanie utracony, aktywna umowa może wysłać kolejny w tej samej fazie. Długa trasa ogranicza więc częstotliwość zaopatrzenia.

| Polityka | Skutek |
|---|---|
| Najpierw mieszkańcy | Eksport zostawia w regionie zapas równy jednej turze jego popytu |
| Swobodny eksport | Pozwala wywieźć towar przed zaspokojeniem własnych potrzeb |
| Embargo | Wstrzymuje import i eksport, pozostawiając wymianę krajową |

Zmiana polityki kosztuje 30 złota i ma odstęp ośmiu tur. Embargo, wojna, zerwanie traktatu i odcięcie stolicy wstrzymują nowe załadunki, ale czas umowy nadal upływa. Wysłany ładunek stosuje opisane wyżej zasady karawan; nie musi ponownie wracać do sieci stolicy. Zmiana właściciela miasta rozwiązuje umowę przy następnym rozliczeniu. Umowę można samodzielnie rozwiązać w panelu. AI zawiera umowy między własnymi państwami; kontrakty Marchii wymagają działania gracza. Transporty AI podlegają tym samym regułom ruchu i ryzyka.

Zużycie tworzy podatek od obrotu: 8%, 12% lub 18% wartości towarów przy niskich, zwykłych lub wysokich podatkach. Trafia on do dochodu państwa; płatności zagraniczne są księgowane odrębnie, aby nie pobierać ich dwukrotnie. W budżecie i nagłówku podatki rynku są oparte na ostatnim rozliczeniu, a przepływ kontraktów na aktualnej prognozie. Dochód prywatnych gospodarstw pozostaje abstrakcyjny, podobnie jak dotychczasowy dochód miejski.

Sprawa rady **Komu służy otwarty targ?** wiąże politykę handlową z poparciem i skarbem. Wybór embarga obejmuje teraz również już opłacone transporty. Ceny obejmują trzy towary; nie ma samodzielnych przedsiębiorstw kupieckich, frachtu morskiego ani kredytu.

## Kupcy pamiętają: straty i obietnice

Utrata importowej karawany do nadal własnego miasta może otworzyć **Wozy, które nie wróciły**. Delegacja odwołuje się do rzeczywistego manifestu, wartości zakupu, towaru i odbiorcy. Otwarcie sprawy oznacza lud -2 i miejscowe niepokoje +4. Jedno miasto otwiera najwyżej jedną nową sprawę na dziesięć tur, niezależnie od liczby strat. Straty eksportów do obcych odbiorców i straty po przejęciu miasta nie wywołują krajowej delegacji.

W panelu **Rynek → Sprawy kupców i obietnice** można otworzyć oczekującą sprawę, przejrzeć zobowiązania oraz ich wyniki. Delegacja pojawia się też przy najbliższym planowaniu rady, bez czekania na turę podzielną przez pięć. Wcześniejsza sprawa rady musi najpierw otrzymać odpowiedź.

| Odpowiedź | Koszt i bezpośredni skutek |
|---|---|
| Odszkodowanie | Koszt równy zapłaconej wartości utraconego ładunku, zaokrąglonej w górę do pełnego złota; lud +5, rody +2; w mieście dobrobyt +8, niepokoje -6 |
| Obietnica zastępczej dostawy | Lud +2 teraz; trzeba sprowadzić ilość i rodzaj towaru z utraconego manifestu do wskazanego miasta, do fazy rynku tury decyzji +8 włącznie |
| Odmowa pomocy | Bez opłaty; lud -5, rody -4; w mieście niepokoje +8 |

Odszkodowanie jest dodatkową pomocą ze skarbu, nie zwrotem pierwotnej zapłaty od sprzedawcy. Nie odtwarza towarów. Odmowa pozostaje dostępna także przy pustym skarbie. Wszystkie skutki społeczne i miejscowe podlegają zwykłym granicom 0–100.

Obietnicę wypełnia suma **faktycznych zagranicznych przyjazdów po decyzji**, z właściwym towarem i odbiorcą. Można zsumować kilka częściowych dostaw; liczy się również karawana wysłana wcześniej, która dopiero dotrze. Nie liczą się wcześniejsze przyjazdy, samo opłacenie ładunku, własne zapasy, produkcja, wymiana krajowa, inny towar ani inne miasto. Nie można utrzymywać dwóch obietnic tego samego towaru dla jednej prowincji ani rozliczyć tego samego manifestu dwukrotnie. Wygaśnięcie kontraktu nie usuwa obietnicy: liczy się przybycie opłaconego transportu.

- **Dotrzymane słowo:** lud +6, stabilność +2, prestiż +5; w mieście dobrobyt +8 i niepokoje -6.
- **Złamana obietnica:** lud -10, stabilność -4, prestiż -5 (minimum 0); we własnym mieście dobrobyt -6 i niepokoje +10.

Ostatnia dozwolona faza najpierw rozlicza przyjazdy, a dopiero potem ewentualną porażkę. Spóźniona dostawa nie zmienia już wyniku. Utrata miasta zamyka nierozpatrzoną delegację, a podjętą obietnicę łamie; nowy właściciel nie otrzymuje dodatkowej kary miejscowej. Każdy wynik rozlicza się raz i trafia do kroniki. Zapis zachowuje termin oraz identyfikatory zaliczonych ładunków. To rozbudowa jednego wątku kupieckiego; nie ma jeszcze osobnych przedsiębiorstw, dowolnie składanych obietnic ani złożonych osobowości kupców.

## Poselstwa i uzasadnienia wojny

W **Dyplomacji** wybierz **Negocjuj warunki**. Podróż kosztuje 20 złota. Odszkodowanie wymaga co najmniej 30 krzywd; przyjęcie przynosi 80 złota i zużywa 30 krzywd. Pakt o nieagresji blokuje wypowiedzenie wojny przez obie strony przez 15 tur od odpowiedzi. Przyjęcie poprawia relacje o 5, odmowa obniża je o 5. Do tego samego państwa nowe warunki można wysłać po dziesięciu turach. Jedna rozmowa na państwo na turę obejmuje także dary i traktaty.

Odpowiedź przychodzi po dwóch turach, według sytuacji w dniu powrotu: zaufania, względnej siły armii, krzywd i wyczerpania wojną. Kharowie trudniej przyjmują warunki. Okno pokazuje bieżącą ocenę i próg 15; odszkodowanie wymaga też 80 złota w skarbcu sąsiada. Nie ma ukrytego losowania. Powrót posłów otwiera panel dyplomacji, a odpowiedź pozostaje w kronice i zapisie.

- **Podbój:** lud -8, stabilność -6, zaufanie pozostałych państw -12; ofiara otrzymuje 40 krzywd.
- **Zadośćuczynienie:** wymaga i zużywa 30 własnych krzywd; lud -2, stabilność -1, bez kary zaufania u innych państw.
- **Wyzwolenie:** dostępne, gdy sąsiad zajmuje miasto założone przez Marchie; lud -2, stabilność -1, bez kary zaufania u innych państw.

Każda wojna zrywa handel i obniża relacje z przeciwnikiem o 35. Uzasadniona wojna daje przeciwnikowi 15 krzywd. Zdobycie miasta dodaje pokonanemu 30, zniszczenie drogi 12; krzywdy maleją o 1 co osiem tur. Rozpoczęcie wojny anuluje oczekujące poselstwo. AI również podlega rozejmom, a jego agresja tworzy krzywdy i reakcję pozostałych państw.

## Wojna i cena pokoju

Przy wypowiedzeniu wojny wybierz konkretne miasto jako cel podboju lub wyzwolenia. Zadośćuczynienie zobowiązuje do uzyskania co najmniej 80 złota reparacji. Cel nie ogranicza sterowania armią, lecz jest rozliczany przy pokoju: spełniony daje inicjatorowi +12 prestiżu i +3 poparcia ludu, niespełniony -5 prestiżu (do minimum 0) i -3 poparcia. Miasto musi wtedy należeć do inicjatora; wystarczy także odzyskanie go przez traktat.

W **Przebiegu wojny** zobaczysz bilans -100…100. Miasta przeciwnika utrzymane od początku konfliktu dają po 20, utrata własnych odejmuje po 20; odzyskanie odwraca składnik. Różnica zadanych obrażeń daje maksymalnie ±25, bieżąca siła armii ±20, a względne wyczerpanie ±15. Bilans nie narzuca pokoju, lecz wpływa na ocenę warunków.

Wybierz **Negocjuj pokój**, następnie żądanie lub zapłatę 100/200 złota albo pokój bez reparacji. Opcjonalnie dodaj zwrot jednego zajętego miasta jego założycielom po dowolnej stronie konfliktu. Nie można oddać ostatniego miasta państwa. Reszta granic pozostaje pod obecną kontrolą.

Poselstwo kosztuje 20 złota. Nową propozycję można wysłać po czterech turach i zamknięciu trwających rokowań. Odpowiedź nadejdzie po dwóch turach; wojna trwa w czasie podróży. Ocena przyjęcia ma próg 50: bazowe 35, do +20 za czas wojny, +0,6 za każdy punkt wyczerpania rozmówcy, -0,8 za każdy punkt jego przewagi w bilansie wojny, relacje / 10, płatność na jego rzecz / 5 i ±30 za zwrot miasta. Interfejs pokazuje bieżący wynik; ostateczny uwzględnia sytuację przy odpowiedzi.

Płatności są wykonywane przy ratyfikacji, bez rezerwowania środków. Brak złota lub zmiana sytuacji miasta anuluje całość warunków, bez częściowej zapłaty czy transferu. Opłata podróży nie wraca. Pokojowy zwrot zachowuje zdrowie miasta, zgromadzoną produkcję i realizowany projekt.

Odrzucenie może przynieść kontrofertę. AI wysyła również własne warunki po co najmniej ośmiu turach wojny, gdy jest wyczerpana albo przegrywa. Propozycje AI podróżują dwie tury i czekają cztery kolejne tury na decyzję gracza; nie zatrzymują kończenia tur. Można je przyjąć, odrzucić lub pozostawić do wygaśnięcia. Powrót posłów i wczytanie gry kierują do dyplomacji, gdy potrzebna jest odpowiedź. AI nie narzuca graczowi automatycznego rozejmu; między sobą państwa AI mogą zawierać prosty pokój z wyczerpania.

Traktat daje 12 tur rozejmu, wyczerpanie obu stron -15 oraz krzywdy -20. Handel wymaga nowego traktatu. Wojska z terytorium drugiego sygnatariusza wycofują się do własnych miast, z ruchem 0 i anulowanym marszem; bez schronienia zostają rozwiązane. Pierwszy negocjowany pokój wywołuje dialog **Kiedy milkną miecze** o odbudowie, ofiarach i armii. Wojny i oferty pozostają w kronice oraz zapisie. Nie ma jeszcze wielostronnych konferencji, negocjacji sojuszniczych ani dowolnej wymiany terytoriów.

## Lojalność i władza lokalna

Okno **Zarządzaj** pokazuje każdy składnik celu lojalności. Lojalność zbliża się do celu o maksymalnie 3 punkty na turę. Wpływa na wydajność produkcji, złota, nauki i kultury mnożnikiem 0,85 + 0,002 × lojalność. Cel zależy od stabilności, dobrobytu, niepokojów, podatków, połączenia ze stolicą, garnizonu, miejscowych praw i podboju.

Zmiana ustroju prowincji kosztuje 40 złota i blokuje następną zmianę na osiem tur:

| Ustrój | Lojalność i gospodarka |
|---|---|
| Wspólna administracja | Pełne wpływy, bez dodatkowej premii lojalności |
| Autonomia | Cel lojalności +18, złoto ×0,70, kultura ×1,20 |
| Zarząd wojskowy | Cel +10 z garnizonem lub -12 bez niego; produkcja ×0,90, nauka ×0,80 |

Garnizonem jest piechota, łucznicy, kawaleria lub katapulta na polu miasta; zwiadowca i osadnicy nie wystarczą. Każdy garnizon dodatkowo daje +6 do celu. Przejęcie obcego miasta ustawia lojalność 30, niepokoje 55 i opór przez 12 tur, obniżający cel o 20. Obca administracja ma dalszą karę -10. Powrót do założycieli ustawia lojalność 65 i usuwa okupację.

Jeśli obce miasto kończy trzy kolejne rozliczenia polityki z lojalnością poniżej 20, wraca do państwa założycieli. Dwie pierwsze tury dają ostrzeżenia. Podniesienie lojalności do 20 zeruje licznik. Wojsko ustępującego władcy przenosi się do najbliższego własnego miasta bez obcego oddziału; bez takiego schronienia zostaje rozwiązane. Powrót miasta nie rozpoczyna sam wojny, lecz daje dotychczasowemu władcy 30 krzywd. Własne miasta mogą tracić wydajność, ale nie przechodzą do arbitralnego obcego państwa.

Dialog **Czyje prawa, czyja ziemia?** pozwala odpowiedzieć lokalnej radzie. Autonomia, zarząd wojskowy i wspólna administracja mają osobne skutki społeczne i miejscowe. AI może przyznać autonomię przy lojalności poniżej 40. Rozruchy, dialogi i zmiany ustroju pozostają w kronice.

## Sterowanie i zapisy

| Akcja | Sterowanie |
|---|---|
| Wybierz pole / oddział | LPM; przy kilku oddziałach wybierz konkretny w prawym panelu |
| Przesuń mapę | Przeciągnij LPM |
| Obróć kamerę | Przeciągnij PPM, środkowy przycisk lub Shift + LPM; Q / E |
| Przesuń kamerę klawiaturą | W / A / S / D |
| Zmień nachylenie | PageUp / PageDown lub przyciski Kąt + / Kąt − |
| Przywróć kamerę | Przycisk Widok 3D |
| Marsz do celu / podgląd ataku | PPM na odkrytym celu |
| Przybliżenie | Kółko myszy |
| Następny gotowy oddział | Tab |
| Centrum na stolicy | Home |
| Zakończ turę / otwórz oczekującą radę | Spacja |
| Zapisz / wczytaj | F5 / F9 |
| Zamknij okno dialogu | Esc |

Pliki `.crown` znajdują się domyślnie w `wydanie-0.22/saves/`. Format v19 dodaje historię odczytów, sprawy energetyczne, dowody dostaw i kontekst rady. Odczytuje v1–v18; migracja zachowuje dotychczasowe dyspozycje i świat bez wymyślania dawnych niedoborów. Nadpisanie v18 zachowuje `.v18.bak`. Format v18 dodaje priorytety zasilania, rejestr wyłączonych zakładów i daty ostatnich dyspozycji. Odczytuje v1–v17; v17 zachowuje pełną historię epok i zaczyna ze zwykłym priorytetem oraz wszystkimi zakładami czynnymi. Zapis tworzy kopię `.v17.bak` przed nadpisaniem poprzedniej wersji. Format v17 dodaje pełne kampanie epok, instytucje, presję klimatyczną, programy kosmiczne, modele jednostek i kontynuowanie po zwycięstwie; odczytuje v1–v16. Migracja v16 zachowuje dawne niewole i decyzje, bez dopisywania nowej historii. Rozszerzenie kampanii jest osobną decyzją w panelu epok. V16 dodaje decyzje o obcych jeńcach, terminy internowania i powiązania wymienionych osób; odczytuje v1–v15. Migracja v15 zachowuje dotychczasowe niewole oraz zamknięte rozstrzygnięcia, bez ponownego pobierania okupu i otwierania dawnych spraw. V15 zachowuje agentów, operacje, wyniki, raporty, niewolę, jej rozstrzygnięcia i ochronę miast; odczytuje v1–v14. Migracja v14 zachowuje długi, rozkazy armii, produkcję i rachunki, zaczynając bez fikcyjnych agentów i misji. V14 zachowuje umowy pożyczek, spłaty, odroczenia, odmowy i kontekst rozmowy z wierzycielami. Migracja v13 zachowuje rozkazy wojskowe, przygotowanie pozycji, kolejki i rachunki bez dopisywania dawnych długów. V13 zachowuje stałe rozkazy wojskowe i stopień przygotowania pozycji. Migracja v12 zachowuje kolejki produkcji, rachunki, kontrole, eskorty i dotychczasowe marsze, bez dopisywania umocnień lub rekonwalescencji. V12 zachowuje kolejki produkcji, powtarzanie i wstrzymanie pracy. Migracja v11 zachowuje bieżący projekt, zgromadzoną pracę, rachunki i otwarte kontrole bez dopisywania zamówień. V11 zachowuje korupcję, ubytki dochodów, kontrole, nierozpatrzone raporty, odzyskane środki i nadzór urzędu. Migracja v10 zachowuje poselstwa i zgody na tranzyt, obietnice oraz eskorty, zaczynając nowe rachunki od zera. Nie dopisuje dawnych braków. V10 przechowuje nowe warianty poselstw i wynikające z przyjętych odpowiedzi prawa tranzytu. Migracja v9 zachowuje eskorty, karawany, obietnice i wcześniejsze poselstwa bez dopisywania zgód na przejazd. V9 zachowuje rozkazy eskorty i ich powiązanie z karawanami. Migracja v8 zachowuje karawany i obietnice, bez przydzielania wojska wstecz. V8 przechowuje delegacje, decyzje, terminy, wyniki i zaliczone manifesty. Migracja v7 zachowuje karawany i ich historię bez tworzenia delegacji za dawne straty. V6 zachowuje magazyny, bilanse, podatki, ceny, płatności, politykę i kontrakty, bez dopisywania wstecz karawan. Dawne dostawy pozostają wykonane. Kampanie sprzed v6 zaczynają z pustymi magazynami. V5 zachowuje wojny i rokowania. Wojny rozpoczęte wcześniej otrzymują rejestr od chwili wczytania, bez wymyślonych strat lub celów. V4 zachowuje założycieli, okupację, lojalność i poselstwa; w starszych formatach założycielem staje się obecny właściciel, a lojalność startuje od 75. V3 zachowuje potrzeby, prawa i migracje; v1/v2 otrzymują zrównoważony rozwój i dobrobyt 50. Nadpisanie starszego pliku tworzy odpowiednią kopię `.v1.bak`–`.v18.bak`. Nowe reguły zmieniają dalszy przebieg starych kampanii. `autosave.crown` jest nadpisywany; osobne zapisy pozwalają zachować poprzednie kampanie.

## Budowanie i weryfikacja

Bez zależności NuGet i bez zewnętrznego silnika gry. Potrzebny .NET SDK 10 dla Windows z pakietami referencyjnymi Windows Desktop.

```powershell
dotnet build KoronyIPogranicza.slnx -m:1 -c Release -p:MSBuildEnableWorkloadResolver=false -p:TargetPlatformDisplayName=Windows
dotnet run --project tests/Crown.Specs -c Release -p:MSBuildEnableWorkloadResolver=false
dotnet publish src/Crown.Desktop -c Release --self-contained false -o wydanie-0.22 -p:MSBuildEnableWorkloadResolver=false -p:TargetPlatformDisplayName=Windows
```

`NuGet.Config` celowo nie zawiera źródeł zewnętrznych. Parametr `TargetPlatformDisplayName` pozwala budować także w środowisku ograniczającym odczyt rejestru Windows. `Zweryfikuj.ps1` buduje, uruchamia specyfikacje silnika i testy integracji WPF. Raporty oraz rzeczywiste rendery aplikacji znajdują się w `artifacts/`. Szpiegostwo ma osobny przebieg `--ui-smoke-espionage`, a jeńcy `--ui-smoke-custody`; epoki mają przebieg `--ui-smoke-epochs`; energetyka ma przebieg `--ui-smoke-power`; kryzysy mają przebieg `--ui-smoke-energy-cases`; scena 3D ma przebieg `--ui-smoke-3d`; oprawa GPU ma przebieg `--ui-smoke-photographic`; krajobraz i historyczne miasta mają przebieg `--ui-smoke-landscape`; skrypt wykonuje wszystkie dziewięć trybów kolejno. Starsze scenariusze regresji używają dawnego wariantu kampanii, a nowy test obejmuje rzeczywisty domyślny start pełnej historii. Podstawowy przebieg ma limit 180 sekund ze względu na liczbę renderów, a pozostałe instancje 60 sekund. Diagnostyka nie wykonuje autozapisu, również przy zamknięciu programu.

## Architektura i dalsza produkcja

- `src/Crown.Core`: model świata, symulacja, AI, treść dialogów i serializacja; niezależne od WPF.
- `Navigation.cs`, `Movement.cs`, `Logistics.cs`, `Infrastructure.cs`, `Combat.cs`: wspólne wyszukiwanie tras, rozkazy, dostawy, gospodarka infrastruktury i model starcia z podglądem.
- `Espionage.cs`, `SpyCouncil.cs`, `SpySaveRules.cs`, `SpyPanels.cs`: agenci, operacje, kontrwywiad, losy schwytanych i raporty.
- `TreasuryLoans.cs`, `DebtCouncil.cs`, `LoanSaveRules.cs`, `LoanPanels.cs`: finansowanie, zobowiązania, decyzje wierzycieli, walidacja i panel skarbu.
- `MilitaryOrders.cs`, `MilitaryPanels.cs`: stałe rozkazy, leczenie, przygotowane pozycje, walidacja i przegląd armii.
- `Regions.cs`, `ProvincialEvents.cs`: potrzeby i kierunki rozwoju miast, rynki regionalne, migracje oraz dialogi z lokalnymi skutkami.
- `DiplomaticCouncil.cs`, `ProvincialPolitics.cs`: poselstwa, krzywdy, uzasadnienia wojny, lojalność, ustroje i zmiany właściciela miast.
- `WarCouncil.cs`, `PeaceCouncil.cs`, `WarSaveRules.cs`: ogłoszone cele, bilans, rokowania, kontroferty, ratyfikacja i walidacja historii konfliktu.
- `Markets.cs`, `TradeActions.cs`, `MarketSaveRules.cs`, `MarketCouncil.cs`: produkcja i zużycie towarów, ceny, kontrakty, polityka handlowa, dialog oraz walidacja bilansów.
- `Caravans.cs`, `CaravanSaveRules.cs`: ruch ładunków, tranzyt, osłona, postoje, przechwytywanie i walidacja manifestów.
- `TradeCouncil.cs`, `TradeCaseSaveRules.cs`: delegacje po stratach, dynamiczne dialogi, obietnice zależne od przyjazdów i walidacja ich historii.
- `EscortOrders.cs`, `EscortSaveRules.cs`: przydziały eskorty, wspólna prognoza ruchu, rezerwacja wojska, odłączanie i walidacja rozkazów.
- `TransitCouncil.cs`, `TransitPanels.cs`: czasowe prawa przejazdu wynikające z przyjętych poselstw, diagnoza granic, ostrzeżenia i rozmowy z gospodarzem szlaku.
- `Administration.cs`, `AuditCouncil.cs`, `AuditSaveRules.cs`, `AuditPanels.cs`: ubytki dochodów, kontrola rachunków, dynamiczne rozstrzygnięcia, nadzór, historia i walidacja.
- `ProductionPlanning.cs`, `ProductionPanels.cs`: kolejki, wymagania i blokady, powtarzanie, wstrzymanie, przejścia między projektami oraz walidacja planów.
- `EpochCatalog.cs`, `Epochs.cs`, `EpochCouncils.cs`, `UnitModels.cs`, `EpochSaveRules.cs`, `EpochPanels.cs`: katalog 18 epok, chronologia badań, energia, klimat, modele i modernizacje, programy kosmiczne, instytucje i walidacja. [EPOKI.md](EPOKI.md) opisuje pełną zawartość.
- `CustodyCouncil.cs`, `CustodySaveRules.cs`, `CustodySmoke.cs`: decyzje o obcych jeńcach, wzajemne wymiany, internowanie, walidacja historii i diagnostyka WPF.
- `src/Crown.Desktop`: natywny interfejs WPF oraz scena 3D w C#/.NET 10.
- `MapView3D.cs`, `MapAnnotations3D.cs`, `MiniMap.cs`: kamera, trafianie promieniem, nakładki, etykiety i minimapa.
- `Terrain3D.cs`, `SceneGeometry.cs`, `WorldScene3D.cs`, `WorldModels3D.cs`: wysokości, atlas terenu, siatki, materiały i przestrzenne modele świata.
- `NativeGpu.cs`, `PhotographicRenderer.cs`, `Shaders/Landscape.hlsl`: zarządzanie Direct3D 11, materiały, cienie, odbicia, HDR i prezentacja w WPF.
- `NaturalModels3D.cs`, `PhotographicSmoke.cs`: roślinność, detale, otoczenie oraz testy obrazu GPU i zgodności z rozgrywką.
- `ThreeDimensionalSmoke.cs`: integracja rzeczywistej sceny z wyborem, ruchem, walką, zapisem i epokami.
- `tests/Crown.Specs`: wykonywalne specyfikacje bez bibliotek testowych; symulacje kampanii i test odtwarzalności zapisu.
- `DESIGN.md`: założenia projektu, ograniczenia i kierunek dalszych etapów.
- `CHANGELOG.md`: zmiany w aktualnym etapie produkcji.
- `ASSETS.md`: pochodzenie ilustracji AI, ścieżka i pełny prompt.

Obecna główna mapa jest **interaktywną sceną Direct3D 11 z teksturami, cieniami i odbiciami**. Realistyczna ilustracja menu nie jest obrazem rozgrywki. Fotorealizm na jej poziomie, szczegółowe modele, animacje postaci i udźwiękowienie pozostają do wykonania. Nie ma jeszcze flot, religii, multiplayera ani edytora treści. AI używa jawnych heurystyk; dyplomacja i symulacja społeczna wymagają rozbudowy przed pełnym wydaniem.
