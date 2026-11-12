# Korony i Pogranicza 0.42 — weryfikacja

Data: 2026-11-12. C#/.NET 10, Windows, WPF i Direct3D 11.

## Wyniki

- Końcowy build Release: **0 błędów i 0 ostrzeżeń** (`build-0.42.txt`).
- Silnik: **752 PASS, 0 FAIL**, 33,7 s. Reguły symulacji nie zmieniały się.
- **374 kontrole w 13 sekwencyjnych trybach**: ruch postaci 25, ustawienia 44, detale środowiska 23, początkowa scena 15, 3D 34, renderer 21, rzeki 19, krajobraz 18, otoczenie 22, modele 36, optymalizacja 27, epoki 44 i samouczek 46. Wszystkie procesy pomyślnych przebiegów zwróciły 0. Jest to zestaw celowany; pełnych 29 trybów nie uruchamiano.
- Końcowy opublikowany EXE: **132 kontrole GPU i 15 WPF**: nowy ruch 25 GPU + 15 WPF, ustawienia 44, modele 36, optymalizacja 27. Wszystkie pięć procesów zakończone kodem 0.
- Celowo wymuszona awaria: oczekiwany **kod 3** i raport nowego trybu.
- **8 kontroli** rzeczywistego autozapisu v28 z 0.41: pełny round trip, izolowana kopia, rada, rzeczywista kontynuacja i identyczny stan/RNG po ponownym wczytaniu. Oryginał niezmieniony.
- **40** wcześniejszych plików kampanii zachowało SHA256 zgodny ze snapshotem.
- `git -c core.safecrlf=false diff --check`: bez błędów.

## Ruch, tożsamość i zasoby

Metadane każdej postaci są wyrównane z trójkątami, mają skończony punkt odniesienia i wysokość po przekształceniach modelu. Ciało, ubranie i włosy używają tego samego identyfikatora. Wygląd oparto na numerze oddziału i osoby w formacji, a nie na współrzędnych — rzeczywisty ruch na izolowanej kampanii potwierdza zachowanie tożsamości. Wczytanie rekonstruuje te same warianty bez nowych pól zapisu.

Niezależne kanały czasu pozwalają poruszać postaciami przy zatrzymanym otoczeniu. Wyłączenie obu utrzymuje identyczne piksele dla różnych czasów; czas zerowy daje dokładną pozę neutralną. Ten sam czas daje identyczny obraz. Pauza zatrzymuje automatyczne odświeżanie także wtedy, gdy aktywne są tylko postacie. Prawdziwy przełącznik w ustawieniach zapisuje preferencję i przekazuje ją rendererowi. Starsze ustawienia v1 domyślnie włączają nową funkcję.

24 renderowane kroki nie zmieniają liczby zasobów, nie przebudowują sceny i nie wysyłają ponownie geometrii. Zachowano 64-bajtowy format wierzchołka, wykorzystując nieużywane kanały materiałów dla metadanych ruchu. Dane postaci po stronie CPU są niezmienne i współdzielone. Rozszerzone granice przycinania zachowują każdy piksel przy włączaniu frustum culling dla poruszonych postaci. Dotychczasowe warianty odległe pozostają aktywne.

## Podgląd i ograniczenia

`published-motion-gpu-0.42/character-idle.gif` zawiera **40 rzeczywistych klatek renderera**, próbkowanych co 0,15 s przy zatrzymanym otoczeniu. Sprawdzono liczbę klatek i metadane opóźnienia; ponad 30 próbek ma różne obrazy. Podgląd nie jest pomiarem FPS gry. Format GIF korzysta z kodowania WIC i opisanych przez Microsoft pól kontroli klatek: https://learn.microsoft.com/en-us/windows/desktop/wic/-wic-native-image-format-metadata-queries . Obejrzano także zrzut bliskiej formacji z wariantami stroju oraz wzrostu.

Jest to **proceduralna animacja spoczynku**, nie pełny system szkieletowy. Marsz, walka, mimika i interakcje pozostają do wykonania. Konie, roboty i pojazdy nie otrzymują tej animacji. WPF pokazuje stałe pozy. Fotorealizm nadal nie został osiągnięty; testy potwierdzają działanie, a nie jakość artystyczną.

## Odtworzenie i paczka

`verify-graphics-0.42.ps1`, `graphics-results-0.42.txt`, `graphics-verification-run-0.42.txt`, `verify-published-0.42.ps1`, `published-verification-0.42.txt`, `core-specs-0.42.txt`, `save-compatibility-0.42.txt`. Nowy tryb: `--ui-smoke-character-motion <katalog raportu>`. Diagnostyka korzysta z własnych zapisów i ustawień oraz blokuje ingerencję rzeczywistego inputu w scenariusz.

Format kampanii pozostaje **v28**. Wydanie ma osobny katalog i kopie poprzednich zapisów oraz ustawień, bez nadpisywania istniejących plików. ZIP zawiera 14 plików programu i dokumentacji; pomija osobiste ustawienia, kampanie, cache i rendery testowe. Hash, rozmiar, zgodność wpisów z wydaniem i zgodność starszych archiwów sprawdza `package-release-0.42.ps1`; wynik w `package-0.42.txt`.
