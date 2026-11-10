# Korony i Pogranicza 0.38 — weryfikacja

Data: 2026-11-10. Windows, C#/.NET 10, WPF i Direct3D 11. Framework-dependent; wymagany .NET Desktop Runtime 10 x64.

## Wyniki

- Końcowy build Release: **0 błędów, 0 ostrzeżeń**.
- Silnik: **752 PASS, 0 FAIL**, 35,0 s. W tym 21 nowych specyfikacji inwestycji przemysłowych i koncesji.
- Pełny interfejs: **787 kontroli w 25 sekwencyjnych trybach**, zakończenie procesu 0.
- Opublikowany EXE: **447 kontroli GPU w 17 trybach**, wszystkie kody wyjścia 0.
- Opublikowany EXE z `--no-gpu`: **269 kontroli WPF w 11 trybach**, wszystkie kody wyjścia 0.
- **25 prób celowej awarii** zakończonych wymaganym kodem 3 z raportem błędu.
- Po końcowym dopasowaniu pozycji jeźdźców, siodła, skali konia i dwustronnych materiałów: ponowny build/publish i **36 kontroli modeli GPU + 36 kontroli modeli WPF** na tym końcowym EXE. Wszystkie przeszły; obejrzano zrzut kawalerii. Reguł silnika po pełnym przebiegu nie zmieniano.
- Rzeczywisty autozapis poprzedniego wydania: 7 dodatkowych sprawdzeń migracji, identycznego backupu, round tripu, wznowienia i zachowania oryginału.
- `git -c core.safecrlf=false diff --check`: bez błędów.

Pełny zestaw UI obejmuje: podstawowa rozgrywka 170, wywiad 22, jeńcy 18, epoki 38, energia 23, obietnice energetyczne 22, 3D 34, materiały GPU 21, krajobraz 18, rzeki 19, pogoda 22, modele 36, optymalizacje 27, ustawienia 44, dzielnice 32, AI wojskowe 16, flota 27, transport 22, oblężenia 21, zapasy 26, przetwórstwo 26, odrębność epok 44, traktaty 22, rady prowincji 18 i inwestycje przemysłowe 19.

## Modele i materiały

Siedem modeli źródłowych CC0, dziewięć siatek docelowych z dwoma LOD (w tym dwie pozy jeźdźców), 21 map materiałów. Usunięto dawne PersonShape/HorseShape. Sprawdzono odczyt wszystkich siatek, rzeczywiste UV, skończone jednostkowe normalne, porcjowanie geometrii, dwa poziomy szczegółowości, dekodowanie wszystkich trzech kanałów do 1024 pikseli i materiały dwustronne. Dane źródeł i ich SHA256 znajdują się w `assets-0.38/manifest.json`; hashe plików docelowych w `assets-0.38/runtime-manifest.json`.

Faktyczne scenariusze sprawdzają: renderowanie koni i ludzi, legendę zasobów i pełne nazwy, wybieranie pól przez model, ochronę nieodkrytych złóż, brak mutacji kampanii/RNG przy kamerze i podglądzie, działanie przycisku **Przyjrzyj się oddziałowi** oraz obsługę okna 1200×800. Obejrzano zbliżenia ludzi, koni i kawalerii z końcowego EXE. WPF używa tej samej geometrii i mapy koloru; zaawansowane normalne i szorstkość/metaliczność są ścieżką GPU.

Pozy są wypalone ze szkieletu i dodatkowo dopasowane dla jeźdźców. **Nie jest to odtwarzanie animacji szkieletowej podczas gry.** Zestaw strojów rozróżnia szerokie grupy, nie kompletną autentyczną garderobę każdej z 18 epok. Modele z katalogu High Detail nie są przedstawiane jako skany ani dowód ukończonego fotorealizmu całej oprawy. Źródła i warunki opisuje ASSETS.md.

## Inwestycje przemysłowe

Sprawdzono koszty podpisu i nadzoru, brak natychmiastowej rozbudowy i towarów, osiem rzeczywistych faz, wymagane partie i różne finansowane fazy, zakupy gotowych wyrobów bez zaliczania celu, niewłaściwy wyrób, wstrzymanie pracy, brak środków, brak podwójnego obciążenia, opóźniony początek koncesji oraz deterministyczne wznowienie.

Niezapłacona rata nie skraca liczby wymaganych wpłat. Zapłata zeruje serię niedoborów; trzy kolejne niedobory odbierają jeden poziom urządzeń i naliczają konsekwencje dokładnie raz. Dwadzieścia cztery faktyczne wpłaty kończą koncesję z zachowaniem urządzeń. Historyczny raport większej przepustowości pozostaje poprawny po odebraniu poziomu w zakładzie obecnie wstrzymanym. Sprawdzono potwierdzane odwołanie bez zwrotu, limity, kontrolę właściciela, blokadę podczas rady, walidację fałszywych dowodów oraz zmianę suwerenności.

Scenariusz UI przechodzi przez rzeczywiste rozmowy i przyciski, publiczną rozbudowę, późniejszą koncesję, wpłatę po zakończeniu, zapis/wczytanie obu stanów i przewijanie całego rejestru. Przygotowano infrastrukturę, większy skarb, żywność i działające kopalnie. To test integracji, nie dowód naturalnego balansu kilkudziesięciu godzin kampanii.

## Wydajność i ograniczenia pomiarów

Scena modeli: **117 730 trójkątów blisko / 15 112 daleko**, około **87% mniej geometrii aktorów** w oddaleniu. Wartość nie oznacza takiego samego wzrostu FPS całej gry. Tekstury i siatki źródłowe są współdzielone; ruch kamery nie tworzy nowych materiałów GPU.

Próbka 10 klatek opublikowanego programu: mediana **28,8 ms**, maksimum **32,2 ms**, obraz **1165×758**, feature level 0xB000. Pomiar dotyczy tej maszyny i sceny, obejmuje ścieżkę renderowania/odczytu, nie jest gwarancją płynności na innych konfiguracjach. Testy używają ustalonej rozdzielczości; normalna gra zachowuje adaptacyjną rozdzielczość, limit klatek i oszczędzanie bezczynności.

Końcowy recheck modeli trwał 20,6 s w GPU i 17,1 s w WPF. Brak szerokiego benchmarku słabszych komputerów i wielogodzinnego ręcznego grania w tę wersję. Automatyczne długie symulacje pozostają częścią specyfikacji silnika.

## Kampanie i archiwa

Format v28 odczytuje v1–v27. Test v27 potwierdza zachowanie dokładnej `.v27.bak` oraz brak wymyślonych urządzeń. Zweryfikowano niezmienność SHA256 **36 wcześniejszych autozapisów** według osobnych wartości w `autosave-baselines-0.38.json`. Katalog 0.38 otrzymał kopię aktualnego autozapisu 0.37 bez nadpisywania oryginału.

Faktyczny plik gracza miał **format v27, etap 1**; SHA256 `41C6F2F03CE5E26AE04DCE61470B5713ADEB5EDCA335BB1EF028BA06116F1822`. Odczytano go, wykonano dokładny round trip, zapisano tylko izolowaną kopię z identycznym backupem, a następnie wznowiono dwa stany do etapu 2 z identycznymi wynikami i RNG. Oryginał pozostał bajt w bajt niezmieniony. Zachowano istniejącą politykę lokalną, bez dodawania poziomów przemysłu.

Pakowanie sprawdza 14 plików wydania i każdego pliku SHA256, zgodność binariów ze źródłowym buildem oraz wcześniej wydane ZIP-y 0.22–0.37. Archiwum zawiera program i dokumentację. Nie zawiera kampanii gracza, jego ustawień, cache audio ani roboczych GLB. Wynik i SHA256 nowego ZIP-a: `package-0.38.txt`.

## Odtworzenie i dowody

- `build-0.38.txt`, `verification-run-0.38.txt` — build, 752 specyfikacje i 787 kontroli UI.
- `published-verification-run-0.38.txt`, `published-durations-0.38.txt`, `process-exit-check-0.38.txt` — opublikowany EXE i oczekiwane awarie.
- `final-model-recheck-0.38.txt`, `final-models-gpu-0.38/`, `final-models-wpf-0.38/` — końcowy podgląd po dopasowaniu jeźdźca i siodła; rzeczywiste zrzuty i 36 kontroli w każdym backendzie.
- `industrial-investment-ui-0.38/`, `published-industrial-investment-ui-0.38/`, `fallback-industrial-investment-ui-0.38/` — rozmowy, rozbudowa, koncesja i paragony.
- `actual-save-compatibility-0.38.txt`, `save-compatibility-0.38/` — sprawdzenie rzeczywistego poprzedniego zapisu na izolowanej kopii.
- `assets-0.38/` — pochodzenie, manifesty, licencja MIT i narzędzie przygotowania siatek. Oryginalne GLB są roboczymi pobraniami wyłączonymi z kontroli wersji i ZIP-a.

Polecenia: `./Zweryfikuj.ps1`, `./artifacts/verify-published-0.38.ps1`, `./artifacts/package-release-0.38.ps1`. Osobny końcowy test modeli używa `--ui-smoke-models`, z opcjonalnym `--no-gpu`, i zawsze katalogu diagnostycznego jako ostatniego argumentu.
