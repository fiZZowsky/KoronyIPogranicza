# Korony i Pogranicza 0.41 — weryfikacja

Data: 2026-11-12. Windows, .NET 10, WPF i Direct3D 11.

## Wyniki

- Build Release: **0 błędów i 0 ostrzeżeń** (`build-0.41.txt`).
- Silnik: **752 PASS, 0 FAIL**, 34,1 s. Reguły symulacji i format v28 nie zmieniały się.
- **305 kontroli w 11 kolejnych trybach**: detale środowiska 23, scena początkowa 15, 3D 34, renderer 21, rzeki 19, krajobraz 18, otoczenie 22, modele 36, optymalizacja 27, epoki 44, samouczek 46. Każdy proces zakończony kodem 0. To zestaw celowany w zmiany; pełnych 28 trybów nie uruchamiano.
- Opublikowany EXE: **93 kontrole GPU i 19 WPF**. Nowe detale 23 GPU + 19 WPF, modele 36, rzeki 19 i rzeczywisty poprzedni zapis 15. Wszystkie pomyślne procesy zakończone kodem 0.
- Celowa awaria nowego trybu: oczekiwany **kod 3** i raport błędu.
- **8 kontroli** rzeczywistego autozapisu v28 z 0.40: dokładny round trip, izolowana kopia, rada, kontynuacja z identyczną losowością oraz zachowanie oryginału.
- SHA256 wszystkich **39** poprzednich plików kampanii pozostaje zgodny ze snapshotem.
- `git -c core.safecrlf=false diff --check`: bez błędów.

## Nowe kontrole i ocena wizualna

Każda ćwiartka atlasu ma rzeczywisty kanał alfa z przezroczystymi otworami i nieprzezroczystymi fragmentami. Materiały są współdzielone i zamrożone. Rośliny mają obie strony, skończone normalne i identyfikatory odkrytych pól. Test wykrył brak przypisania roślin brzegowych do pola; poprawiono przypisanie i ochronę nieodkrytej geografii, po czym pełny wybrany zestaw przeszedł.

Sprawdzono deterministyczny czas wizualny, zmianę obrazu przy podmuchach, stałą liczbę zasobów i brak ponownego wysyłania sceny podczas animacji oraz obrotów, zmniejszenie faktycznie wysyłanej geometrii na odległość, układ kompaktowy i niezmieniony zapis gry.

Obejrzano zrzuty obozu i lasu w nowym trybie oraz końcowy zbliżony widok rzeczywistej kampanii z opublikowanego EXE (`published-user-scene-0.41/camp-close.png`). Pierwszy render pokazywał zbyt wysokie, zwarte kępy i nadmiernie widoczny wzór dna; zmniejszono wysokość traw, rozszerzono kępy i silniej stłumiono obraz dna z głębokością.

Lokalny pomiar końcowego EXE podczas obrotu kamery: mediana **17,0 ms**, maksimum **27,0 ms**, 1168 × 765. To jeden zapis i jedno urządzenie, z odczytem obrazu na CPU. Nie jest gwarancją FPS lub temperatur dla innych komputerów i kampanii.

## Odtworzenie i ograniczenia

Źródła wyników: `core-specs-0.41.txt`, `graphics-results-0.41.txt`, `graphics-verification-run-0.41.txt`, `published-verification-0.41.txt`, `save-compatibility-0.41.txt`. Skrypty: `verify-graphics-0.41.ps1` i `verify-published-0.41.ps1`. Nowy tryb: `--ui-smoke-environment-detail <katalog raportu>`. Rzeczywisty zapis: `--ui-smoke-art-review --review-save <plik.crown> <katalog raportu>`.

Przebiegi używają oddzielnych zapisów i ustawień, a rzeczywisty input nie ingeruje w przygotowane scenariusze. WPF zachowuje statyczne karty z alfą i prostsze materiały; pełny wiatr, woda i cienie korzystają z GPU. Atlas roślin został wygenerowany przez imagegen, nie jest skanem botanicznym. Postacie nadal korzystają ze statycznych póz. Testy nie są oceną fotorealizmu, którego nadal nie osiągnięto.

Wydanie 0.41 ma osobny katalog oraz kopie poprzednich kampanii i ustawień. Paczka ZIP ma wyłącznie program i dokumentację, bez kampanii, osobistych ustawień i cache. Hash, rozmiar i zgodność 14 wpisów sprawdza `package-release-0.41.ps1`; wyniki w `package-0.41.txt`.
