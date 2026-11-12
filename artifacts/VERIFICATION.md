# Korony i Pogranicza 0.40 — weryfikacja

Data: 2026-11-12. C#/.NET 10, Windows, WPF, Direct3D 11.

## Zakres i ograniczenia

To korekta sceny początkowej po zrzucie gracza, **nie osiągnięty fotorealizm**. Nowy człowiek obejmuje pierwsze dwie epoki; późniejsi aktorzy pozostają dotychczasowi. Pozy są statyczne, ubranie jest proste, roślinność i woda nadal wymagają dalszej produkcji. Materiały zgodności WPF są prostsze niż GPU. Testy sprawdzają działanie i integralność, nie przyznają jakości artystycznej.

## Wyniki

- Build Release: 0 błędów, 0 ostrzeżeń (`build-0.40.txt`).
- Silnik: **752 PASS, 0 FAIL**, 32,3 s. Reguły kampanii nie zmieniały się.
- **282 kontrole w 10 kolejnych trybach**: scena początkowa 15, 3D 34, renderer 21, rzeki 19, krajobraz 18, otoczenie 22, modele 36, optymalizacja 27, odrębność epok 44, samouczek 46. Wszystkie procesy zakończyły się kodem 0. To zestaw celowany w zmiany graficzne; pełnych 27 trybów w tej wersji nie uruchamiano.
- Opublikowany EXE: **85 kontroli GPU i 14 WPF**. Scena początkowa 15 GPU + 14 WPF, modele 36, rzeki 19 oraz 15 dodatkowych kontroli rzeczywistego zapisu z 0.39. Każdy pomyślny proces zakończył się kodem 0.
- Wymuszona awaria nowego przeglądu: oczekiwany kod 3 i raport.
- **8 kontroli rzeczywistego autozapisu v28 z 0.39**: pełny round trip, zapis izolowanej kopii, rada, rzeczywista kontynuacja i identyczna losowość. Oryginał pozostał bez zmian.
- SHA256 wszystkich **38** poprzednich plików kampanii zgadza się ze snapshotem.
- `git -c core.safecrlf=false diff --check`: bez błędów.

## Obejrzane obrazy i pomiar

Obejrzano rzeczywiste rendery zbliżenia obozu i ludzi przed oraz po korekcie, a także końcowy `published-user-scene-0.40/camp-close.png` z rzeczywistego autozapisu 0.39. Końcowy widok pokazuje otwarte schronienia, brak postaci fantasy, mniej nasycony teren, cieńsze zaznaczenie i stopniowane brzegi wraz z zakończeniem.

Lokalny pomiar gotowego EXE podczas obrotu kamery w tym zapisie: mediana **16,4 ms**, maksimum **29,9 ms**, 1165 × 758. Obejmuje odczyt obrazu na CPU i dotyczy jednego urządzenia oraz jednego ujęcia. Nie jest gwarancją FPS, temperatur ani wydajności innych scen. Bufory sceny nie są ponownie wysyłane przy obrocie; aktorzy i schronienia mają warianty odległe.

## Odtworzenie

`verify-graphics-0.40.ps1`, `graphics-results-0.40.txt`, `graphics-verification-run-0.40.txt`, `core-specs-0.40.txt`, `verify-published-0.40.ps1`, `published-verification-0.40.txt`, `save-compatibility-0.40.txt`. Dodatkowy przegląd istniejącego zapisu: `--ui-smoke-art-review --review-save <plik.crown> <katalog raportu>`; odczyt jest bezpieczny dla oryginału, zapisy testów są izolowane.

Format kampanii pozostaje v28. Wydanie ma osobny katalog i kopię poprzednich zapisów oraz ustawień, bez nadpisywania istniejących plików. ZIP zawiera wyłącznie program i dokumentację, bez osobistych ustawień i kampanii. Zgodność binariów, starych archiwów oraz wpisów ZIP sprawdza `package-release-0.40.ps1`; hash i rozmiar są w `package-0.40.txt`.
