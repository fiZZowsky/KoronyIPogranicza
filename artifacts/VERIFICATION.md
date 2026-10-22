# Weryfikacja wydania 0.5

Data: 22 października 2026 r. Windows, .NET SDK 10.0.401, .NET Desktop Runtime 10.0.12.

- Kompilacja Release: sukces, 0 ostrzeżeń i 0 błędów.
- **85 specyfikacji silnika**: sukces (`core-specs.txt`). Względem 0.4 dodano 17 scenariuszy dotyczących celów wojennych, bilansu, rokowań i zapisów.
- **49 kontroli integracyjnych WPF**: sukces (`ui/ui-smoke.txt`). Opublikowane wydanie przeszło te same kontrole (`published-ui-0.5/ui-smoke.txt`), w tym 11 nowych ścieżek.
- Cele: wskazanie konkretnego miasta, odrzucanie celu we własnym państwie bez zmiany stanu, zapis inicjatora i granic początkowych, spełnienie celu przez zwrot miasta oraz konsekwencje nieuzyskania reparacji.
- Bilans: czystość raportu, odwracanie składnika terytorialnego po odzyskaniu miasta, symetria stron, zgodność zapisanych strat z podglądem rzeczywistej walki i ograniczenie wkładu obrażeń do ±25.
- Rokowania: opłata poselstwa tylko raz, odrzucanie niedozwolonej płatności i nieopłaconego poselstwa, dwie tury podróży, jednorazowa ratyfikacja, płatność między skarbcami oraz 12 tur rozejmu.
- Zwrot miast: zachowanie zdrowia, produkcji i projektu, powrót do założycieli, blokada oddania ostatniego miasta lub terytorium trzeciego państwa. Zmiana właściciela podczas podróży anuluje całość traktatu.
- Ewakuacja: oba państwa wycofują oddziały z terytorium drugiego sygnatariusza; ich wojska nie blokują sobie nawzajem schronienia. Jednostki tracą ruch i marsz do obcego celu.
- Niewypłacalność przy ratyfikacji: warunki są anulowane bez częściowego wykonania. Koszt podróży pozostaje wydany.
- Kontroferty: opóźnione przybycie, powiązanie z odrzuconą propozycją, oczekiwanie na zgodę gracza, przyjęcie bez powtórnego wykonania, odrzucenie i wygaśnięcie bez kończenia wojny.
- AI inicjuje propozycję przy wyczerpaniu; nie narzuca graczowi automatycznego pokoju. Upadek państwa zamyka jego wojnę i oczekujące rokowania.
- Pierwszy negocjowany pokój uruchamia sprawę rady o odbudowie. Wybór pomocy zachowuje opóźniony skutek ekonomiczny i społeczny.
- Zapis v5: deterministyczna kontynuacja wojny i negocjacji po wczytaniu; walidacja stron, terminów, identyfikatorów, powiązań kontrofert i historii. Migracja v4 zachowuje krzywdy, lojalność, ustrój i starsze poselstwa; dawne wojny nie otrzymują wymyślonych celów. Nadpisanie tworzy oryginalną kopię `.v4.bak`. Kontrole migracji v1–v3 również przeszły.
- Cztery pasywne kampanie zakończyły się po 203, 160, 199 i 174 turach dla ziaren 17421, 2026, 8 i 99. Maksymalna liczba miast wyniosła odpowiednio 16, 18, 16 i 21. Zakończenia: utrata ostatniego miasta gracza lub zwycięstwo kulturowe AI. To kontrola integralności, nie ocena balansu dla człowieka.
- WPF: raport wojny, formularz reparacji i zwrotu miasta, wysłanie warunków, ratyfikacja po dwóch turach, dialog odbudowy, otrzymanie kontroferty, odrzucenie, przyjęcie po wczytaniu i przywrócenie zakończonej historii wojny.
- Scenariusze WPF są przygotowanymi sytuacjami z dodatkowym złotem, ustawionym wyczerpaniem lub zajętym miastem. Nie reprezentują standardowego początku kampanii.
- Wizualnie sprawdzono rzeczywiste rendery raportu wojennego, rokowań, kontroferty, powojennej rady i końcowego panelu dyplomacji. Użyto WPF RenderTargetBitmap, bez przechwytywania pulpitu.
- Celowa awaria diagnostyki opublikowanego programu zwróciła oczekiwany kod 3 (`process-exit-check.txt`). Katalog `diagnostics/` zawiera opis tej oczekiwanej awarii.
- Wydanie `wydanie-0.5/` zawiera program i dokumentację; starsze katalogi i paczki są zachowane. Stare zapisy skopiowano lokalnie bez nadpisywania. Archiwum `Korony-i-Pogranicza-0.5-Windows.zip` zawiera wyłącznie pliki wydania, bez prywatnych zapisów.

Pozostają testy balansu z ludźmi, użyteczności i działania na różnych komputerach. Negocjacje są dwustronne i obejmują stałe warianty reparacji oraz jeden zwrot miasta. Nie ma jeszcze konferencji koalicyjnych ani pełnego rynku terytoriów. Oprawa pozostaje mapą 2D.