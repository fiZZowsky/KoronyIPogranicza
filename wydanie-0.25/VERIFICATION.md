# Weryfikacja wydania 0.25

Data: 27 października 2026 r. Windows, .NET SDK 10.0.401, docelowy .NET 10 Desktop. Sprzętowy Direct3D 11, feature level 11_0 (0xB000), WPF render tier 2 na bieżącym komputerze.

- Końcowa kompilacja Release zakończyła się bez ostrzeżeń i błędów; wersja pliku 0.25.0.0. Kod Crown.Core i format zapisu v19 nie były edytowane.
- Pełny Zweryfikuj.ps1 przeszedł 438 specyfikacji silnika w 20,7 s oraz 434 kontrole interfejsu w dwunastu trybach: łącznie 872 sprawdzenia. Raport: artifacts/verification-run-0.25.txt.
- Podział kontroli UI: 170 podstawowych, 22 wywiadu, 18 jeńców, 38 epok, 23 energetyki, 22 kryzysów, 34 sceny 3D, 21 obrazu GPU, 18 krajobrazu, 19 rzek, 22 środowiska i 27 modeli oraz oznaczeń zasobów. Procesy uruchamiano kolejno, z limitem 180 s dla podstawowego i 60 s dla pozostałych.
- Nowe 27 kontroli potwierdza rzeczywisty renderer GPU, geometrię twarzy i wyposażenia ludzi, detale koni, roślinność oraz sześć odrębnych reprezentacji zasobów. Wszystkie sprawdzane siatki mają skończone pozycje i jednostkowe normalne, a partie mieszczą się w limicie 20 001 wierzchołków.
- Każdy z sześciu rzeczywistych znaczników mapy ma pełną polską nazwę i oznaczenie rodzaju zasobu. Wybranie pola pokazuje opis oraz premię zgodną z Engine.TileYields. Zniknął skrót Fe. Przełącznik Zasoby ukrywa oznaczenia, a Legenda otwiera wszystkie sześć rodzajów i działa w przewijanym mniejszym oknie.
- Trafienie w geometrię konia wskazuje właściwe pole. Bliższy zoom, modele, etykiety i legenda zachowują dokładnie serializowany stan kampanii i jej RNG. Nieodkryte złoże żelaza nie uzyskuje ujawniającego znacznika. Pozostałe kontrole sprawdzają ukrytą geografię i wojsko, rozkazy, mosty, pogodę, zapis v19 oraz epoki historyczne i przyszłe.
- Obejrzano rzeczywiste zrzuty ludzi, koni, zboża, barwników, żelaza i legendy. Scenariusz diagnostyczny ma przygotowane złoża, odsłonięte pola, przyznane technologie i wyłączoną produkcję. To kontrola grafiki oraz interfejsu, nie naturalnie rozegrana wielogodzinna kampania.
- Końcowa źródłowa scena 3D ma 262 034 trójkąty terenu i 304 396 obiektów. Próbka dziesięciu klatek GPU przy 1168×765: mediana 52,4 ms, maksimum 67,9 ms na bieżącym komputerze. Większa liczba szczegółów zwiększa koszt względem 0.24; to krótka próbka renderowania i prezentacji, bez gwarancji FPS dla innych GPU, rozdzielczości lub całej kampanii. Potrzebne są dalsze prace nad LOD i wydajnością dużych scen.
- Bieżący autosave.crown skopiowano z osobnego 0.24 do 0.25 tylko przy braku pliku docelowego. Diagnostyka nie wykonuje autozapisu, również przy zamknięciu. Integralność zapisów i starszych paczek kontroluje artifacts/package-release-0.25.ps1.
- EPOKI.md wygenerowano z katalogu gry. ASSETS.md opisuje oryginalną geometrię i ścieżki wektorowe oraz ponowne użycie tekstur 0.21. Nie generowano nowych bitmap i nie pobierano zewnętrznych modeli. Uruchom.cmd preferuje wydanie-0.25.

Modele pozostają proceduralne i mają statyczne pozy, bez animacji szkieletowej. Zwierzęta rozwinięte w tej wersji to konie zasobowe i kawaleryjskie; nie dodano innych gatunków, hodowli ani symulacji ekosystemów. Drzewa wykorzystują dotychczasowe tekstury koron. Złoża są wizualizacją istniejących zasobów, bez wydobycia geologicznego, nowych towarów lub zmiany premii gospodarczych. Materiały i wiatr wymagają Direct3D; WPF ma prostsze oświetlenie. Oprawa nadal nie osiąga fotorealizmu ilustracji menu.

## Sprawdzenie gotowej paczki

- Opublikowane wydanie przeszło osobno wszystkie 434 kontrole w dwunastu trybach. Raporty: artifacts/published-*-ui-0.25/ui-smoke.txt oraz artifacts/published-verification-run-0.25.txt. Nowe 27 kontroli modeli przeszło w 12,2 s.
- Dodatkowy opublikowany przebieg --ui-smoke-3d --no-gpu przeszedł 34 kontrole w 25,3 s. Raport: artifacts/fallback-three-dimensional-ui-0.25/ui-smoke.txt. WPF zachowuje nowe modele i nazwy, z prostszym oświetleniem i bez nowych efektów GPU.
- Wymuszone błędy wszystkich dwunastu trybów zwróciły kod 3 i raport celowej awarii. Dowody: artifacts/process-exit-check-0.25.txt oraz artifacts/intentional-*-ui-0.25/ui-smoke.txt.
- Próbka gotowego GPU przy 1168×765: mediana 60,1 ms, maksimum 84,9 ms. To pomiar dziesięciu klatek bieżącej sceny na tym komputerze, bez gwarancji FPS. Potwierdza potrzebę dalszej optymalizacji większych scen.
- EXE, biblioteka interfejsu i Crown.Core.dll mają SHA256 identyczne z końcową kompilacją źródłową. Wydania 0.24 i 0.25 zachowują dokładny autosave SHA256 FDE20C05CA9832CB14A93E958BF6FC18278958CE818C43F59279829CF165A8FB.
- Archiwum zawiera 14 plików pod wydanie-0.25/, bez zapisów gracza, kopii i plików tymczasowych. Każdy wpis porównano przez SHA256 z katalogiem wydania. Starsze ZIP-y 0.24, 0.23 i 0.22 mają niezmienione hashe. Metadane archiwum: artifacts/package-0.25.txt.

Czasy procesów: podstawowy 154,1 s; wywiad 22,4 s; jeńcy 16,4 s; epoki 20,1 s; energetyka 12,8 s; kryzysy 16,2 s; 3D 25,2 s; GPU 13,5 s; krajobraz 9,0 s; rzeki 7,2 s; środowisko 16,8 s; modele 12,2 s. Raport: artifacts/published-durations-0.25.txt.