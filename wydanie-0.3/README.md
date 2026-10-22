# Korony i Pogranicza · 0.3

Autorska, jednoosobowa strategia turowa dla Windows, napisana w **C# / .NET 10**, z interfejsem i dialogami po polsku. Historycznie inspirowany świat jest fikcyjny. Gracz prowadzi Marchie Veyru od osadnictwa po odkrycia naukowe i instytucje nowoczesnego państwa.

To **grywalna wersja rozwojowa**, a nie ukończona produkcja o skali komercyjnej Cywilizacji. Fundamentem wyróżniającym projekt są pamięć polityczna państwa, stronnictwa społeczne i konsekwencje dialogów. Zmiana epoki nie usuwa miast ani historii.

## Uruchomienie

Uruchom `wydanie-0.3/KoronyIPogranicza.exe` albo główny `Uruchom.cmd`. Zachowaj pozostałe pliki z katalogu `wydanie-0.3` obok programu. Poprzednie wydania pozostają w `wydanie/` i `wydanie-0.2/`.

**Nowy etap: Życie prowincji.** Wersja 0.3 dodaje regionalną wymianę żywności i towarów, potrzeby mieszkaniowe i opiekę, dobrobyt, migracje ludności, miejscowe kierunki rozwoju oraz dialogi dotyczące konkretnych miast. Szczegóły: `CHANGELOG.md`.

Wymagania: Windows 10/11 i **.NET Desktop Runtime 10 x64**. Na komputerze, na którym powstał projekt, ten runtime jest zainstalowany. Wydanie korzysta z zainstalowanego środowiska .NET, nie zawiera własnego runtime'u. Gra nie wymaga połączenia z Internetem ani konta.

## Co jest grywalne

- Świat 42 × 28 heksów: ziarno proceduralne, biomy, rzeki, zasoby, ruiny, mgła wojny, przybliżanie i minimapa. Stolice mają gwarantowane połączenie lądowe.
- Cztery fikcyjne państwa z różnymi premiami; gracz prowadzi jedno, trzy obsługuje AI. Trzy poziomy trudności zmieniają wydajność AI.
- Miasta, ludność, wzrost i głód, sezonowe plony, eksploatacja pól, farmy, kopalnie, placówki handlowe, produkcja i utrzymanie.
- 24 technologie z wymaganiami; 17 trwałych projektów miejskich, dwa projekty powtarzalne i sześć rodzajów oddziałów.
- Lud, rody i uczeni: ich poparcie wpływa na docelową stabilność, a ta na gospodarkę. Ustawy zmieniają podatki, ustrój i priorytety.
- 21 rodzajów wyjątkowych i lokalnych spraw rady oraz cztery wydarzenia powracające. Sprawy prowincji mogą pojawiać się osobno dla każdego miasta. Odpowiedzi zmieniają poparcie, prawa lokalne i przyszłe zdarzenia. Nadanie ziemi lub przywilejów zmienia późniejszą gałąź dialogów. Prawo traktu trwale zmienia dochody, naukę albo dostawy.
- Dyplomacja: dary, relacje, traktaty dające dochód, wypowiedzenie wojny, reparacje, rozejmy. AI zakłada miasta, bada, buduje, ulepsza ziemię i prowadzi działania wojenne.
- Walka wręcz i dystansowa, doświadczenie, obrona terenu, oblężenia i przejmowanie miast. Woda i góry blokują ruch. Armie poza zasięgiem dostaw słabną; odpoczynek leczy.
- Drogi za 25 złota i 0,15 złota utrzymania/turę, automatyczna wycena połączeń miast, premie handlowe, blokowanie dostaw przez wojska i niszczenie wrogich dróg. Ruch między połączonymi odcinkami kosztuje 0,5 punktu.
- Rozkazy marszu zapisywane między turami i w plikach kampanii; podświetlenie zasięgu i trasy. Atak wymaga potwierdzenia podglądu zgodnego z modelem walki.
- Prowincje z własnymi potrzebami, kierunkiem rozwoju i dobrobytem. Dzielnice mieszkalne oraz lecznice ograniczają presję społeczną. Regionalna wymiana karmi miasta z niedoborem, a bezpieczne drogi umożliwiają przeprowadzki do lepszych warunków.
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

Połączone bezpiecznymi drogami miasta tworzą regionalne rynki, także bez połączenia ze stolicą. Nadwyżka po wyżywieniu mieszkańców może być eksportowana do miasta z deficytem: maksymalnie 6 na miasto i turę w każdą stronę. Najpilniejsze niedobory mają pierwszeństwo. Eksport zmniejsza żywność pozostającą na wzrost, import nie tworzy nowej żywności. Blokada przerywa wymianę. Sól i barwniki z pól w zasięgu miast są współdzielone w tej samej sieci; traktat i targowisko mogą zapewnić dodatkowe towary zagraniczne.

Przeludnienie, niedostateczna opieka i brak żywności zwiększają niepokoje i obniżają docelowy dobrobyt. Dobrobyt zbliża się do celu o maksymalnie 2 punkty na turę i zmienia wydajność produkcji, nauki, złota i kultury w przedziale 90–110%. Pierwsza tura głodu powoduje niepokoje; co druga kolejna tura z pustymi zapasami i niedoborem odbiera mieszkańca.

Migracje wewnątrz państwa są sprawdzane co cztery tury. Miasto źródłowe musi mieć co najmniej trzech mieszkańców i problemy lokalne. Cel musi leżeć w tej samej bezpiecznej sieci, mieć wolne mieszkania i opiekę, nadwyżkę żywności co najmniej 2 oraz dobrobyt wyższy o przynajmniej 10. Przenosi się jeden mieszkaniec; liczba ludności pozostaje stała. Dane miasto uczestniczy ponownie najwcześniej po ośmiu turach. Napływ zwiększa niepokoje o 4, chyba że przyjęto prawo integracji.

Głód, brak mieszkań i napływ ludności wywołują sprawy rady przypisane do wskazanego miasta. Każda taka sprawa jest rozstrzygana raz na prowincję, z odstępem co najmniej 15 tur między sprawami. Lokalne prawa pozostają przy mieście i są widoczne w jego panelu. Nie jest to pełny rynek cen ani symulacja indywidualnych gospodarstw.

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

Pliki `.crown` znajdują się domyślnie w `wydanie-0.3/saves/`. Możesz wczytać zapisy z wcześniejszych katalogów. Format v3 przechowuje potrzeby, miejscowe prawa i historię migracji; odczytuje v1 i v2, nadając prowincjom zrównoważony rozwój i dobrobyt 50. Nadpisanie starszego pliku tworzy kopię `.v1.bak` lub `.v2.bak`. Nowe reguły gospodarki zmieniają dalszy przebieg starych kampanii. `autosave.crown` jest nadpisywany; osobne zapisy pozwalają zachować poprzednie kampanie.

## Budowanie i weryfikacja

Bez zależności NuGet i bez zewnętrznego silnika gry. Potrzebny .NET SDK 10 dla Windows z pakietami referencyjnymi Windows Desktop.

```powershell
dotnet build KoronyIPogranicza.slnx -m:1 -c Release -p:MSBuildEnableWorkloadResolver=false -p:TargetPlatformDisplayName=Windows
dotnet run --project tests/Crown.Specs -c Release -p:MSBuildEnableWorkloadResolver=false
dotnet publish src/Crown.Desktop -c Release --self-contained false -o wydanie-0.3 -p:MSBuildEnableWorkloadResolver=false -p:TargetPlatformDisplayName=Windows
```

`NuGet.Config` celowo nie zawiera źródeł zewnętrznych. Parametr `TargetPlatformDisplayName` pozwala budować także w środowisku ograniczającym odczyt rejestru Windows. `Zweryfikuj.ps1` buduje, uruchamia specyfikacje silnika i testy integracji WPF. Raporty oraz rzeczywiste rendery aplikacji znajdują się w `artifacts/`.

## Architektura i dalsza produkcja

- `src/Crown.Core`: model świata, symulacja, AI, treść dialogów i serializacja; niezależne od WPF.
- `Navigation.cs`, `Movement.cs`, `Logistics.cs`, `Infrastructure.cs`, `Combat.cs`: wspólne wyszukiwanie tras, rozkazy, dostawy, gospodarka infrastruktury i model starcia z podglądem.
- `Regions.cs`, `ProvincialEvents.cs`: potrzeby i kierunki rozwoju miast, rynki regionalne, migracje oraz dialogi z lokalnymi skutkami.
- `src/Crown.Desktop`: natywny interfejs WPF, rysowanie mapy, proceduralne materiały terenu i sterowanie.
- `tests/Crown.Specs`: wykonywalne specyfikacje bez bibliotek testowych; symulacje kampanii i test odtwarzalności zapisu.
- `DESIGN.md`: założenia projektu, ograniczenia i kierunek dalszych etapów.
- `CHANGELOG.md`: zmiany w aktualnym etapie produkcji.
- `ASSETS.md`: pochodzenie ilustracji AI, ścieżka i pełny prompt.

Obecna mapa jest **teksturowaną mapą 2D z umownymi budynkami i symbolami jednostek**. Realistyczna ilustracja menu nie jest obrazem rozgrywki. Pełna realistyczna oprawa 3D, animacje i udźwiękowienie pozostają do wykonania. Nie ma jeszcze flot, religii, szpiegostwa, multiplayera ani edytora treści. AI używa jawnych heurystyk; dyplomacja i symulacja społeczna wymagają rozbudowy przed pełnym wydaniem.
