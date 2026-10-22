# Korony i Pogranicza · 0.6

Autorska, jednoosobowa strategia turowa dla Windows, napisana w **C# / .NET 10**, z interfejsem i dialogami po polsku. Historycznie inspirowany świat jest fikcyjny. Gracz prowadzi Marchie Veyru od osadnictwa po odkrycia naukowe i instytucje nowoczesnego państwa.

To **grywalna wersja rozwojowa**, a nie ukończona produkcja o skali komercyjnej Cywilizacji. Fundamentem wyróżniającym projekt są pamięć polityczna państwa, stronnictwa społeczne i konsekwencje dialogów. Zmiana epoki nie usuwa miast ani historii.

## Uruchomienie

Uruchom `wydanie-0.6/KoronyIPogranicza.exe` albo główny `Uruchom.cmd`. Zachowaj pozostałe pliki z katalogu `wydanie-0.6` obok programu. Poprzednie wydania pozostają w osobnych katalogach.

**Nowy etap: Cena dobrobytu.** Wersja 0.6 dodaje zapasy i zużycie towarów, regionalne ceny, zagraniczne kontrakty importowe i eksportowe, podatki rynku, składy towarowe oraz politykę handlową. Szczegóły: `CHANGELOG.md`.

Wymagania: Windows 10/11 i **.NET Desktop Runtime 10 x64**. Na komputerze, na którym powstał projekt, ten runtime jest zainstalowany. Wydanie korzysta z zainstalowanego środowiska .NET, nie zawiera własnego runtime'u. Gra nie wymaga połączenia z Internetem ani konta.

## Co jest grywalne

- Świat 42 × 28 heksów: ziarno proceduralne, biomy, rzeki, zasoby, ruiny, mgła wojny, przybliżanie i minimapa. Stolice mają gwarantowane połączenie lądowe.
- Cztery fikcyjne państwa z różnymi premiami; gracz prowadzi jedno, trzy obsługuje AI. Trzy poziomy trudności zmieniają wydajność AI.
- Miasta, ludność, wzrost i głód, sezonowe plony, eksploatacja pól, farmy, kopalnie, placówki handlowe, produkcja i utrzymanie.
- 24 technologie z wymaganiami; 18 trwałych projektów miejskich, dwa projekty powtarzalne i sześć rodzajów oddziałów.
- Lud, rody i uczeni: ich poparcie wpływa na docelową stabilność, a ta na gospodarkę. Ustawy zmieniają podatki, ustrój i priorytety.
- 24 rodzaje wyjątkowych i lokalnych spraw rady oraz cztery wydarzenia powracające. Sprawy prowincji mogą pojawiać się osobno dla każdego miasta. Odpowiedzi zmieniają poparcie, prawa lokalne i przyszłe zdarzenia. Nadanie ziemi lub przywilejów zmienia późniejszą gałąź dialogów. Prawo traktu trwale zmienia dochody, naukę albo dostawy.
- Dyplomacja: dary, handel, krzywdy, trzy powody wypowiedzenia wojny, odszkodowania, pakty o nieagresji i rozejmy. Posłowie wracają z odpowiedzią po dwóch turach. AI zakłada miasta, bada, buduje, ulepsza ziemię i prowadzi działania wojenne.
- Negocjowany pokój z reparacjami i zwrotem miast, propozycjami AI, kontrofertami i terminem odpowiedzi. Historia wojny przechowuje ogłoszony cel, straty, bilans i końcowe rozliczenie obietnicy.
- Walka wręcz i dystansowa, doświadczenie, obrona terenu, oblężenia i przejmowanie miast. Woda i góry blokują ruch. Armie poza zasięgiem dostaw słabną; odpoczynek leczy.
- Drogi za 25 złota i 0,15 złota utrzymania/turę, automatyczna wycena połączeń miast, premie handlowe, blokowanie dostaw przez wojska i niszczenie wrogich dróg. Ruch między połączonymi odcinkami kosztuje 0,5 punktu.
- Rozkazy marszu zapisywane między turami i w plikach kampanii; podświetlenie zasięgu i trasy. Atak wymaga potwierdzenia podglądu zgodnego z modelem walki.
- Prowincje z własnymi potrzebami, kierunkiem rozwoju i dobrobytem. Dzielnice mieszkalne oraz lecznice ograniczają presję społeczną. Regionalna wymiana karmi miasta z niedoborem, a bezpieczne drogi umożliwiają przeprowadzki do lepszych warunków.
- Lojalność i pamięć założycieli miast. Autonomia oddaje część dochodów za większą lojalność i kulturę; zarząd wojskowy wymaga garnizonu. Podbite miasto może wrócić do założycieli po trzech turach lojalności poniżej 20.
- Fizyczne zapasy żelaza, soli i barwników, wydobycie z pracujących pól, regionalny popyt i ceny. Dostawy krajowe dzielą nadwyżkę w sieci drogowej. Kontrakty zagraniczne przenoszą towary i złoto; embargo, wojna i blokady przerywają dostawy.
- Zwycięstwo naukowe, kulturowe, przez zjednoczenie oraz punktowe po 320 turach. Kampania może zakończyć się wcześniej.
- Zapis i wczytanie stanu JSON z walidacją, atomowym zastępowaniem pliku i zapisanym stanem generatora losowego. Autozapis po turze i decyzji rady; kronika przechowuje wybory oraz ich skutki.

Długość i balans wymagają dalszych rozgrywek z udziałem człowieka. Automatyczne symulacje nie potwierdzają jakości wielogodzinnej rozgrywki.

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

Miasta jednej bezpiecznej sieci drogowej mają wspólną cenę. Bazy to 8 złota dla żelaza, 5 dla soli i 9 dla barwników. Cena wynika z `baza × (0,75 + popyt / (zapasy + nowa produkcja + 1))`, z limitem trzykrotnej bazy i zaokrągleniem do dwóch miejsc. Obliczana jest przed dostawami danego rozliczenia; import wpływa na cenę kolejnego rozliczenia. Wymiana krajowa najpierw pokrywa potrzeby z własnych zapasów miasta, następnie z nadwyżek połączonych miast.

W **Podpisz kontrakt handlowy** wybierz import lub eksport, miasta, towar i limit 1–6 jednostek na turę. Umowa kosztuje 15 złota i obowiązuje do rozliczenia tury początkowej +12. Limit to trzy aktywne umowy na każde państwo. Wymagane są targowiska i Szlaki handlowe po obu stronach, pokojowy traktat oraz bezpieczne połączenie każdego miasta ze swoją stolicą.

Cena kontraktowa wynosi 115% ceny regionalnej sprzedawcy. Przy rozliczeniu zależy od aktualnego rynku; podpis nie blokuje ceny, zapasu ani złota. Dostawa może być częściowa z powodu braku towarów lub funduszy. Płatność przechodzi bezpośrednio od nabywcy do sprzedawcy, bez mnożników trudności. Niedostępne towary nie są tworzone. Umowy rozliczane są w kolejności podpisania.

| Polityka | Skutek |
|---|---|
| Najpierw mieszkańcy | Eksport zostawia w regionie zapas równy jednej turze jego popytu |
| Swobodny eksport | Pozwala wywieźć towar przed zaspokojeniem własnych potrzeb |
| Embargo | Wstrzymuje import i eksport, pozostawiając wymianę krajową |

Zmiana polityki kosztuje 30 złota i ma odstęp ośmiu tur. Embargo, wojna, zerwanie traktatu i odcięcie stolicy wstrzymują kontrakt, ale czas nadal upływa. Przywrócenie warunków wznawia dostawy. Zmiana właściciela miasta rozwiązuje umowę przy następnym rozliczeniu; wygasłej lub rozwiązanej umowy nie wykonuje się ponownie. Umowę można samodzielnie rozwiązać w panelu. AI zawiera umowy między własnymi państwami; kontrakty Marchii wymagają działania gracza.

Zużycie tworzy podatek od obrotu: 8%, 12% lub 18% wartości towarów przy niskich, zwykłych lub wysokich podatkach. Trafia on do dochodu państwa; płatności zagraniczne są księgowane odrębnie, aby nie pobierać ich dwukrotnie. W budżecie i nagłówku podatki rynku są oparte na ostatnim rozliczeniu, a przepływ kontraktów na aktualnej prognozie. Dochód prywatnych gospodarstw pozostaje abstrakcyjny, podobnie jak dotychczasowy dochód miejski.

Nowa sprawa rady **Komu służy otwarty targ?** wiąże politykę handlową z poparciem i skarbem. Ceny obejmują trzy towary, bez samodzielnych agentów kupieckich. Zagraniczny transport jest abstrakcyjnym połączeniem bezpiecznych stolic; nie ma jeszcze poruszających się karawan ani wyszukiwania całej międzynarodowej drogi.

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
| Marsz do celu / podgląd ataku | PPM na odkrytym celu |
| Przybliżenie | Kółko myszy |
| Następny gotowy oddział | Tab |
| Centrum na stolicy | Home |
| Zakończ turę / otwórz oczekującą radę | Spacja |
| Zapisz / wczytaj | F5 / F9 |
| Zamknij okno dialogu | Esc |

Pliki `.crown` znajdują się domyślnie w `wydanie-0.6/saves/`. Format v6 przechowuje zapasy, bilanse, podatki, ceny, kontrakty i politykę rynku; odczytuje v1–v5. Starsze kampanie zaczynają z pustymi magazynami, bez dopisywania dawnych dostaw. V5 zachowuje wojny i rokowania. Wojny rozpoczęte wcześniej otrzymują rejestr od chwili wczytania, bez wymyślonych strat lub celów. V4 zachowuje założycieli, okupację, lojalność i poselstwa; w starszych formatach założycielem staje się obecny właściciel, a lojalność startuje od 75. V3 zachowuje potrzeby, prawa i migracje; v1/v2 otrzymują zrównoważony rozwój i dobrobyt 50. Nadpisanie starszego pliku tworzy odpowiednią kopię `.v1.bak`–`.v5.bak`. Nowe reguły zmieniają dalszy przebieg starych kampanii. `autosave.crown` jest nadpisywany; osobne zapisy pozwalają zachować poprzednie kampanie.

## Budowanie i weryfikacja

Bez zależności NuGet i bez zewnętrznego silnika gry. Potrzebny .NET SDK 10 dla Windows z pakietami referencyjnymi Windows Desktop.

```powershell
dotnet build KoronyIPogranicza.slnx -m:1 -c Release -p:MSBuildEnableWorkloadResolver=false -p:TargetPlatformDisplayName=Windows
dotnet run --project tests/Crown.Specs -c Release -p:MSBuildEnableWorkloadResolver=false
dotnet publish src/Crown.Desktop -c Release --self-contained false -o wydanie-0.6 -p:MSBuildEnableWorkloadResolver=false -p:TargetPlatformDisplayName=Windows
```

`NuGet.Config` celowo nie zawiera źródeł zewnętrznych. Parametr `TargetPlatformDisplayName` pozwala budować także w środowisku ograniczającym odczyt rejestru Windows. `Zweryfikuj.ps1` buduje, uruchamia specyfikacje silnika i testy integracji WPF. Raporty oraz rzeczywiste rendery aplikacji znajdują się w `artifacts/`.

## Architektura i dalsza produkcja

- `src/Crown.Core`: model świata, symulacja, AI, treść dialogów i serializacja; niezależne od WPF.
- `Navigation.cs`, `Movement.cs`, `Logistics.cs`, `Infrastructure.cs`, `Combat.cs`: wspólne wyszukiwanie tras, rozkazy, dostawy, gospodarka infrastruktury i model starcia z podglądem.
- `Regions.cs`, `ProvincialEvents.cs`: potrzeby i kierunki rozwoju miast, rynki regionalne, migracje oraz dialogi z lokalnymi skutkami.
- `DiplomaticCouncil.cs`, `ProvincialPolitics.cs`: poselstwa, krzywdy, uzasadnienia wojny, lojalność, ustroje i zmiany właściciela miast.
- `WarCouncil.cs`, `PeaceCouncil.cs`, `WarSaveRules.cs`: ogłoszone cele, bilans, rokowania, kontroferty, ratyfikacja i walidacja historii konfliktu.
- `Markets.cs`, `TradeActions.cs`, `MarketSaveRules.cs`, `MarketCouncil.cs`: produkcja i zużycie towarów, ceny, kontrakty, polityka handlowa, dialog oraz walidacja bilansów.
- `src/Crown.Desktop`: natywny interfejs WPF, rysowanie mapy, proceduralne materiały terenu i sterowanie.
- `tests/Crown.Specs`: wykonywalne specyfikacje bez bibliotek testowych; symulacje kampanii i test odtwarzalności zapisu.
- `DESIGN.md`: założenia projektu, ograniczenia i kierunek dalszych etapów.
- `CHANGELOG.md`: zmiany w aktualnym etapie produkcji.
- `ASSETS.md`: pochodzenie ilustracji AI, ścieżka i pełny prompt.

Obecna mapa jest **teksturowaną mapą 2D z umownymi budynkami i symbolami jednostek**. Realistyczna ilustracja menu nie jest obrazem rozgrywki. Pełna realistyczna oprawa 3D, animacje i udźwiękowienie pozostają do wykonania. Nie ma jeszcze flot, religii, szpiegostwa, multiplayera ani edytora treści. AI używa jawnych heurystyk; dyplomacja i symulacja społeczna wymagają rozbudowy przed pełnym wydaniem.
