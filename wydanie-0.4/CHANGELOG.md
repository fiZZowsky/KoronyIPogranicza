# Zmiany

## 0.4 — Słowo i władza · 22 października 2026

- Kierunkowa pamięć krzywd między państwami. Agresja, zajmowanie miast i niszczenie dróg tworzą roszczenia po stronie pokrzywdzonego. Krzywdy maleją o 1 co osiem tur.
- Rada wojenna pokazuje trzy powody wojny i dokładne koszty przed rozkazem. Podbój kosztuje więcej poparcia i zaufanie pozostałych państw; zadośćuczynienie zużywa 30 krzywd, a wyzwolenie wymaga zajętego miasta własnych założycieli. Rozejm blokuje wszystkie powody wojny.
- Poselstwa z żądaniem 80 złota odszkodowania lub propozycją piętnastoturowej nieagresji. Podróż kosztuje 20 złota; odpowiedź przychodzi po dwóch turach. Kolejne warunki do tego samego państwa można wysłać po dziesięciu turach.
- Jawna ocena odpowiedzi uwzględnia zaufanie, względną siłę armii, krzywdy, wyczerpanie i nastawienie Kharów. Wynik rozstrzyga sytuacja po podróży, bez ukrytego losowania. Brak środków uniemożliwia wypłatę odszkodowania. Dar i rozwój armii mogą zmienić ocenę przed odpowiedzią.
- Przyjęte warunki, odmowy i anulowane wojną poselstwa trafiają do kroniki. Powrót posłów otwiera dyplomację. Wszczęcie nieuzasadnionej wojny przez AI także wywołuje krzywdy i reakcję pozostałych państw.
- Miasta pamiętają założycieli, lojalność, ustrój i okres oporu po podboju. Cel lojalności ma jawne składniki, a zmiana nie przekracza 3 punktów na turę. Lojalność wpływa na gospodarkę prowincji.
- Wspólna administracja, autonomia i zarząd wojskowy. Zmiana kosztuje 40 złota, z odstępem ośmiu tur. Autonomia zwiększa lojalność i kulturę kosztem wpływów. Zarząd wojskowy wymaga prawdziwego garnizonu oraz ogranicza produkcję i naukę.
- Podbite miasto z lojalnością poniżej 20 przez trzy kolejne rozliczenia wraca do państwa założycieli. Dwie pierwsze tury dają ostrzeżenia, a poprawa lojalności zeruje licznik. Granice i okoliczne drogi zmieniają właściciela, garnizon wycofuje się do własnego miasta; brak schronienia oznacza rozwiązanie oddziału.
- Nowa lokalna sprawa rady „Czyje prawa, czyja ziemia?” wiąże wybór odpowiedzi z ustrojem, lojalnością i poparciem społecznym. AI może ustanawiać autonomię w niespokojnych prowincjach.
- Zapis v4 obejmuje oczekujące odpowiedzi, krzywdy, założycieli, okupację i postęp buntu. Odczytuje v1–v3 i przy nadpisaniu zachowuje oryginalny plik. Historycznym miastom nadaje obecnego właściciela jako założyciela, ponieważ dawny format nie znał pochodzenia.
- Naprawione centrowanie mapy po uruchomieniu kolejnej kampanii i wczytaniu stanu. Kamera jest ustawiana po ułożeniu interfejsu. Rozszerzone testy sprawdzają widoczność stolicy, przyciski negocjacji, rady wojennej i ustroju oraz zgodność kontynuacji.
- Osobne wydanie `wydanie-0.4/`; poprzednie wydania i paczki pozostają zachowane. Grafika nadal korzysta z mapy 2D; cele wojenne, pokój i negocjacje są uproszczonym etapem rozwoju.

## 0.3 — Życie prowincji · 22 października 2026

- Każde miasto ma własny kierunek rozwoju, dobrobyt, potrzeby mieszkaniowe i pojemność opieki. Niezaspokojone potrzeby podnoszą niepokoje; dobrobyt zmienia gospodarkę, zbliżając się stopniowo do celu.
- Cztery kierunki rozwoju: równowaga, rolnictwo, rzemiosło i nauka. Zmiana kosztuje 15 złota, jest dostępna raz na turę i ma konkretne korzyści oraz koszty.
- Dzielnica mieszkalna daje 5 miejsc, lecznica 6 opieki. Akwedukt wspiera teraz również potrzeby mieszkańców. Gracz może przyznać pomoc żywnościową za 50 złota, raz na pięć tur w danej prowincji.
- Bezpieczne sieci drogowe tworzą regionalne rynki, także poza siecią stolicy. Połączone miasta dzielą się nadwyżką żywności po utrzymaniu ludności oraz dostępem do soli i barwników. Dostawy żywności mają limit 6 na miasto i turę; najpilniejsze niedobory mają pierwszeństwo. Import zmniejsza nadwyżkę dawcy i nie tworzy żywności.
- Blokada wojskowa przerywa również wymianę cywilną. Podgląd prowincji pokazuje miejscowy bilans, import, eksport i dostępne towary.
- Rodziny mogą migrować między połączonymi miastami własnego państwa. Muszą znaleźć wyższy dobrobyt, wolne mieszkania, opiekę i nadwyżkę żywności. Przeniesienie jednego mieszkańca zachowuje łączną populację. Przeprowadzki są ograniczone do jednego ruchu na państwo co cztery tury i mają odstęp ośmiu tur dla uczestniczącego miasta.
- Głód najpierw zwiększa niepokoje; co druga tura nieprzerwanego głodu zmniejsza ludność. Eksport/import jest rozliczany przed głodem i wzrostem miast.
- Trzy nowe sprawy rady dotyczą konkretnej prowincji: pustych spichlerzy, braku mieszkań i przyjęcia rodzin. Każdą można rozstrzygnąć osobno w innych miastach. Miejscowe prawa są trwałe i pozostają przy mieście.
- Karta mieszkaniowa, budownictwo cechowe, integracja, praca za dług oraz rada wspólnot mają różne skutki dla mieszkań, produkcji, opłat, kultury i kolejnych migracji. Rada pokazuje dokładne zmiany lokalne obok kosztów ogólnopaństwowych.
- Panel „Ludność”, okno zarządzania prowincją i nakładka mapy pokazują dobrobyt, żywność oraz zapisane migracje. Kierunki przepływu i ich opisy są oddzielone, aby import i wyjazdy były czytelne jednocześnie.
- AI reaguje na głód zmianą priorytetu i pomocą, a przy niedoborze mieszkań lub opieki wybiera odpowiednie budynki.
- Zapis v3 przechowuje lokalne prawa, priorytety, potrzeby, zdarzenia i historię migracji. Wczytuje v1 i v2, zachowując drogi i rozkazy; nowe pola otrzymują bezpieczne wartości początkowe. Nadpisanie starszego pliku zachowuje kopię `.v1.bak` lub `.v2.bak`.
- Aplikacja przekazuje kod błędu procesu z WPF do skryptu weryfikacji, dzięki czemu awaria testu interfejsu nie może wyglądać jak udane uruchomienie.

Wydanie znajduje się w `wydanie-0.3/`; główny `Uruchom.cmd` wybiera je przed poprzednimi wersjami. Starsze katalogi i paczki pozostają zachowane. Zmiany reguł wpływają na dalszy przebieg starych kampanii. Oprawa nadal jest mapą 2D, wymiana nie ma jeszcze cen rynkowych ani ruchomych karawan, a migracje dotyczą własnego państwa.

Proponowana nazwa commita:

```text
feat: add provincial needs, food trade, and population migration
```

## 0.2 — Szlaki i dostawy · 21 października 2026

- Budowa dróg na własnej i neutralnej, odkrytej ziemi po odkryciu Garncarstwa. Cena 25 złota, utrzymanie 0,15 złota za odcinek i turę. Droga może współistnieć z ulepszeniem pola.
- Jednym rozkazem można zbudować wycenione połączenie miasta z ośrodkiem administracji. Brak pieniędzy nie powoduje budowy części trasy. Istniejące odcinki są wykorzystywane ponownie.
- Bezpieczne połączenia dają prowincjom +3 bazowego złota i +1 nauki, przed podatkami i modyfikatorami wydajności. Stolicą pozostaje najstarsze zachowane miasto.
- Drogi obniżają koszt ruchu między połączonymi odcinkami i miastami do 0,5 punktu. Pełny zasięg ruchu jest podświetlany; najechanie na cel pokazuje trasę, a rozkaz marszu — jego dalszy przebieg.
- Dostawy przechodzą przez sieć dostępnego terenu. Omijają wodę i góry; uwzględniają lasy, wzgórza, drogi, zimę i granice. Podstawowy zasięg kosztowy to 6, a Szlaki handlowe i Inżynieria zwiększają go po 2.
- Wrogie oddziały odcinają własne pole i sąsiednie heksy. Własna piechota, kawaleria, łucznicy lub katapulty osłaniają zajmowane pole przed sąsiednią strefą wroga. Odcięte połączenie traci premię miejską. Zniszczenie wrogiej drogi kosztuje 1 punkt ruchu i daje 20 złota.
- Rozkaz marszu na kilka tur wykonuje dostępny odcinek od razu i wraca po kolejnych turach. Zablokowany marsz zostaje anulowany; samoczynnie nie rozpoczyna bitwy. Można go wycofać przyciskiem. Trasy ograniczają się do odkrytych pól.
- Podgląd walki pokazuje siłę obu stron, obrażenia, kontratak, pozostałe zdrowie i możliwość przejęcia miasta. Ten sam model oblicza wynik faktycznej walki. PPM otwiera podgląd, atak wymaga osobnego kliknięcia.
- Nowy panel „Szlaki” zestawia przychody, utrzymanie armii, administracji i dróg. Nakładka mapy rozróżnia dostawy, ich brak oraz blokadę.
- Nowa sprawa rady: prawo wspólnego traktu. Wybór wolnego przejazdu, myta lub patroli trwale zmienia naukę, dochody albo zasięg dostaw. Sprawa pojawia się po 10. turze, gdy istnieją co najmniej cztery odcinki i dwa bezpiecznie połączone miasta.
- AI buduje połączenia, wykorzystuje punkty ruchu na drogach i kieruje osłabione, odcięte wojska w stronę własnych miast.
- Format zapisu v2 przechowuje drogi, ich opiekunów, połówkowe punkty ruchu i cele marszu. Wczytuje v1 z domyślnymi pustymi drogami i rozkazami. Nadpisanie poprawnego zapisu v1 zachowuje jego kopię `.v1.bak`.
- Ruch bada również ruiny na polach pośrednich, a nie wyłącznie na końcu drogi. Interfejs nawigacji przewija się na mniejszych oknach.

Wersja 0.1 pozostaje w `wydanie/`, a 0.2 w `wydanie-0.2/`. Główny `Uruchom.cmd` wybiera nowszą wersję. Nowe zasady zmieniają przebieg starych kampanii po wczytaniu; migracja zachowuje stan i historię, ale nie obiecuje identycznego dalszego przebiegu jak w starym silniku.

Oprawa pozostaje mapą 2D. Nie dodano jeszcze rendereru 3D, limitów ładowności dostaw, magazynów frontowych ani transportu morskiego. Nie przeprowadzono wielogodzinnych testów balansu z ludźmi.
