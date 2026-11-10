# Korony i Pogranicza 0.39 — weryfikacja

Data: 2026-11-10. Windows, C#/.NET 10, WPF i Direct3D 11.

## Wyniki

- Build Release: **0 błędów, 0 ostrzeżeń** (`build-0.39.txt`).
- Silnik: **752 PASS, 0 FAIL**, 34,1 s.
- Interfejs: **833 kontrole w 26 kolejnych trybach**. Każdy pomyślny przebieg zakończył się kodem procesu 0.
- Samouczek i nowa gra: **46 kontroli** rzeczywistych przycisków, decyzji i rozkazów, w tym zachowanie stanu/RNG, zapis preferencji, ich błąd zapisu, starsze ustawienia, pauza, powrót z menu, pominięcia i kompaktowy układ.
- Opublikowany EXE: **128 kontroli GPU** (samouczek 46, ustawienia 44, epoki 38) oraz **46 kontroli WPF** (`--no-gpu`). Wszystkie cztery procesy zakończone kodem 0.
- Celowo wymuszona awaria diagnostyki samouczka: oczekiwany **kod 3** i raport błędu.
- Rzeczywisty autozapis z katalogu 0.38 (zachowany format v27): **7 kontroli** odczytu/migracji do v28, dokładnego backupu izolowanej kopii, ponownego zapisu i deterministycznej kontynuacji. Oryginał niezmieniony.
- Obejrzane rendery: centralne powitanie, wybór trudności oraz doradca z podświetlonym projektem miasta przy 1200 × 800.
- `git -c core.safecrlf=false diff --check`: bez błędów.

Pełny zestaw UI: podstawy 170, wywiad 22, jeńcy 18, epoki 38, energia 23, obietnice energetyczne 22, 3D 34, materiały GPU 21, krajobraz 18, rzeki 19, pogoda 22, modele 36, optymalizacje 27, ustawienia 44, dzielnice 32, AI wojskowe 16, flota 27, transport 22, oblężenia 21, zapasy 26, przetwórstwo 26, odrębność epok 44, traktaty 22, rady prowincji 18, inwestycje 19 i samouczek 46.

## Raporty i powtarzalność

`verification-run-0.39.txt` zawiera silnik oraz podstawy, wywiad i jeńców. Pierwszy przebieg zatrzymał stary test epok, który oczekiwał natychmiastowego utworzenia kampanii po kliknięciu nowej gry. Dostosowano ten test do rzeczywistego wyboru trudności; `verification-remaining-0.39.txt` dokumentuje pomyślne wznowienie od epok do samouczka. Reguł symulacji nie zmieniano. `Zweryfikuj.ps1` zawiera kompletny przebieg wszystkich 26 trybów.

Gotowy EXE sprawdza `verify-published-0.39.ps1`; wyniki w `published-verification-0.39.txt`. Suma 752 + 833 kontroli i brak linii FAIL zostały niezależnie sprawdzone (`verification-counts-0.39.json`). Diagnostyka używa własnych katalogów zapisów i ustawień; rzeczywisty input nie ingeruje w przygotowane scenariusze.

## Zapisy i paczka

Format kampanii pozostaje **v28**. Żadne pole samouczka nie trafia do zapisu świata. Powitanie jest domyślnie włączoną preferencją lokalną, zgodną z plikami ustawień v1. Postęp nauki pozostaje w pamięci podczas pauzy i powrotu z menu; po wczytaniu innej kampanii lub ponownym uruchomieniu gracz może zacząć samouczek z Poradnika.

Snapshot obejmuje **37** istniejących plików kampanii; kontrola SHA256 potwierdza brak zmian. Nowe wydanie otrzymuje kopię dotychczasowych zapisów i ustawień bez nadpisywania istniejących plików. Testy nie zapisują w poprzednich wydaniach.

`package-release-0.39.ps1` sprawdza zgodność EXE i bibliotek z buildem, stare kampanie i archiwa oraz każdy plik wewnątrz ZIP-a. ZIP zawiera 14 plików programu i dokumentacji w katalogu `wydanie-0.39/`; pomija kampanie, ustawienia osobiste, cache i pliki tymczasowe. Hash i rozmiar gotowej paczki: `package-0.39.txt`.
