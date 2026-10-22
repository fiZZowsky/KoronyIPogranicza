# Zmiany

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
