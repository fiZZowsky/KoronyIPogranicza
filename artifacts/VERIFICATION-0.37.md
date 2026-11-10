# Korony i Pogranicza 0.37 — weryfikacja

Data: 2026-11-10. Windows, .NET 10, WPF, rzeczywisty Direct3D 11 feature level 0xB000. Wydanie framework-dependent; wymaga zainstalowanego .NET Desktop Runtime 10 x64.

## Wynik

- Końcowy build Release: **0 błędów, 0 ostrzeżeń**.
- Końcowy silnik: **731 PASS, 0 FAIL**, 35,6 s; w tym 26 nowych specyfikacji kalendarza i polityki prowincji.
- Pełny zestaw interfejsu: **765 kontroli w 24 sekwencyjnych trybach**, zakończenie procesu 0.
- Opublikowany EXE: **425 kontroli GPU w 16 trybach**, każde uruchomienie zakończone kodem 0.
- Opublikowany EXE z `--no-gpu`: **250 kontroli WPF w 10 trybach**, każde uruchomienie zakończone kodem 0.
- **24 celowe awarie diagnostyczne**: każda zgłosiła błąd i zakończyła proces wymaganym kodem 3.
- Rzeczywisty autozapis poprzedniego wydania: 7 dodatkowych sprawdzeń migracji, dokładnej kopii, zachowania oryginału i deterministycznego wznowienia.
- `git -c core.safecrlf=false diff --check`: bez błędów.

Pełny przebieg `Zweryfikuj.ps1` obejmował 730 specyfikacji silnika i wszystkie 765 kontroli UI. Następnie rozszerzono walidację opłaty rady do legalnej maksymalnej populacji, dodano test dużego miasta oraz poprawiono filtrowanie atlasu i mipmap pszenicy. Końcowy silnik uruchomiono ponownie — 731 PASS. Właśnie ten końcowy build opublikowano i sprawdzono w powyższych 16 trybach GPU i 10 trybach WPF. Po tych kontrolach nie zmieniano kodu produktu.

## Pełny interfejs

| Tryb | Kontrole |
|---|---:|
| Podstawowa rozgrywka, rada, wojny, handel, pożyczki | 170 |
| Szpiegostwo | 22 |
| Jeńcy wywiadu | 18 |
| Epoki | 38 |
| Energia | 23 |
| Obietnice energetyczne | 22 |
| Integracja 3D | 34 |
| Materiały i oprawa GPU | 21 |
| Krajobraz | 18 |
| Rzeki i mosty | 19 |
| Pory roku i pogoda | 22 |
| Ludzie, konie, rośliny i oznaczenia zasobów | 33 |
| Optymalizacje renderowania | 27 |
| Ustawienia i menu gry | 44 |
| Dzielnice | 32 |
| AI wojskowe | 16 |
| Flota | 27 |
| Transport morski | 22 |
| Oblężenia | 21 |
| Zapasy marszowe | 26 |
| Przetwórstwo | 26 |
| Odrębność epok | 44 |
| Traktaty i gwarancje | 22 |
| Kalendarz i rady prowincji | 18 |
| **Razem** | **765** |

## Zakres nowych sprawdzeń

Kalendarz: utrwalone daty decyzji, niezmienność historii po zmianie epoki, oznaczanie szacunków, dalszy upływ lat po przyspieszonym rozwoju, przewidywanie terminów bez dodawania prehistorycznych odstępów do średniowiecza, stara chronologia, czystość podglądu i round tripu oraz odrzucanie cofającej się osi czasu.

Rady: koszty podpisu i rzeczywistego wykonania, konkurujące poparcie, cztery dostawy na sześć faz, brak darmowej karty bez środków, głód, brak biblioteki, zawieszone szkoły, wstrzymany warsztat, stała stawka, brak podwójnej opłaty za tę samą fazę, różne trwałe premie, niedotrzymanie obietnicy, limity ponownego podpisu, zmiana właściciela, legalne duże miasta, AI, nieprawidłowe dowody wpłat, wznowienie po zapisie i rzeczywista faza gospodarcza.

UI: rzeczywiste przyciski rozmowy, powrót bez kosztu, potwierdzany podpis, zmiana poparcia, sześć zwykłych tur gospodarki, ustanowienie karty, odczyt wpłat, pełne wczytanie przez desktop, daty kroniki i dostępność opcji w oknie 1200×800. To przygotowany scenariusz z większą rezerwą żywności i skarbem, nie dowód naturalnego balansu całej kampanii.

Grafika: rzeczywiste materiały bitmapowe skóry, sierści i tkanin, lokalne UV, jednostkowe skończone normalne, ograniczone porcje geometrii, oba poziomy szczegółowości, teksturowane dwustronne kępy pszenicy, faktyczna przezroczystość, wybieranie pól i zasobów, nieujawnianie nieodkrytych złóż oraz brak zmian symulacji podczas obsługi kamery i legendy. Obejrzano końcowe zrzuty z opublikowanego EXE; zachowanie niezmodyfikowanej alfy PNG potwierdzono w kodzie i kontroli zasobu.

## Pomiary i ograniczenia

Scenariusz modeli: **44 642 trójkąty blisko / 15 778 daleko**, czyli około **65% mniej geometrii organicznej** w oddaleniu. To liczba geometrii modeli w przygotowanej scenie, nie procent przyspieszenia całej gry. Kamera wybiera jedną wersję na komórkę, a WPF usuwa podwójną wersję oddaloną. Materiały są współdzielone.

Końcowy EXE, próba 10 klatek w scenariuszu graficznym: mediana **37,5 ms**, maksimum **40,4 ms**, obraz **1165×758**. Pomiar obejmuje ścieżkę GPU, odczytu i prezentacji na tym komputerze; nie jest gwarancją FPS ani badaniem wielu konfiguracji. Diagnostyka używa ustalonych ustawień, bez adaptacyjnej rozdzielczości. Normalna gra zachowuje adaptacyjną rozdzielczość, limit klatek i oszczędzanie bezczynności.

Zmieniono rozmiar rzeczywistego okna na 1200×800: mapa i cel GPU miały **765×578**; profil z ograniczeniem 900 pikseli również przeszedł. Diagnostyka czeka na zakończenie zmiany natywnego rozmiaru i układu WPF. Nie zmniejszono wymaganej asercji wielkości.

To bardziej szczegółowe modele proceduralne i materiały albedo z mikronormalnymi, a nie skanowane postacie lub gotowa oprawa fotorealistyczna. Nie dodano riggingu, animacji szkieletowej ani pełnego zestawu nowych modeli artystycznych. Pszenica używa powierzchni z alfą. Brak testu wielogodzinnego grania przez człowieka i benchmarku słabszych GPU; pełne symulacje silnika nadal wchodzą do zestawu specyfikacji.

## Zachowanie kampanii

Zweryfikowano niezmienność SHA256 **35 wcześniejszych autozapisów** według osobnych wartości w `autosave-baselines-0.37.json`. Nowy katalog otrzymał kopię autozapisu 0.36 bez nadpisywania wcześniejszych plików. Oryginalny plik to faktycznie format **v19**, mimo że przechowywany jest przy programie 0.36. Jego SHA256: `FDE20C05CA9832CB14A93E958BF6FC18278958CE818C43F59279829CF165A8FB`.

Osobne sprawdzenie odczytało ten plik, migrowało go do v27, wykonało dokładny round trip, zapisało wyłącznie izolowaną kopię z identyczną `.v19.bak`, a potem wznowiło dwa stany z identycznym wynikiem i RNG. Oryginał pozostał bez zmian. Nowy test v26 osobno potwierdza dokładną `.v26.bak`, brak wymyślonych koalicji i zamianę dawnych kodów dat w historii na jawne przybliżenia.

## Artefakty i odtworzenie

- `build-0.37.txt`, `final-core-specs-0.37.txt`, `verification-run-0.37.txt`.
- `published-verification-run-0.37.txt`, `published-durations-0.37.txt`, `process-exit-check-0.37.txt`.
- `actual-save-compatibility-0.37.txt` i pomocniczy projekt `save-compatibility-0.37/`.
- `published-models-ui-0.37/`, `published-calendar-locals-ui-0.37/`, `published-photographic-ui-0.37/`: końcowe zrzuty i rejestry faktycznego EXE.
- `preflight-verification-0.37.txt`, `resize-preflight-verification-0.37.txt`, `photographic-resize-preflight-0.37.*`: zachowane wcześniejsze nieudane próby. Pierwsza dotyczyła starej asercji tekstu „T…”, druga nieustalonego rozmiaru okna; końcowe przebiegi przeszły.
- Źródła: `./Zweryfikuj.ps1`. Opublikowany EXE: `./artifacts/verify-published-0.37.ps1`.
- Archiwum: `package-release-0.37.ps1` sprawdza 14 plików i ich SHA256, zgodność binariów z końcowym buildem, oryginalne autozapisy i wcześniejsze ZIP-y. Wynik oraz SHA256 archiwum zapisuje w `package-0.37.txt`.

Archiwum zawiera program i dokumentację; nie zawiera kampanii gracza, ustawień osobistych, pamięci podręcznej audio ani plików tymczasowych.
