# Weryfikacja wydania 0.4

Data: 22 października 2026 r. Środowisko: Windows, .NET SDK 10.0.401, .NET Desktop Runtime 10.0.12.

- Kompilacja Release rozwiązania: sukces, 0 ostrzeżeń, 0 błędów.
- **68 specyfikacji silnika**: sukces (`core-specs.txt`). Obejmują wcześniejsze etapy i 18 nowych scenariuszy polityki oraz dyplomacji.
- **38 kontroli integracyjnych WPF**: sukces (`ui/ui-smoke.txt`). Opublikowany program przeszedł te same kontrole (`published-ui-0.4/ui-smoke.txt`).
- Sprawdzone: czystość podglądu wojny, odrzucenie nielegalnych rozkazów bez zmiany stanu, dokładne koszty agresji, reakcje pozostałych państw, kierunek krzywd, wyzwolenie i zużywanie roszczeń.
- Poselstwa: koszt, odstęp, wspólny limit rozmowy, odpowiedź po dwóch turach, ocena sytuacji w dniu powrotu, niewypłacalność adresata, odmowa bez tworzenia nowych krzywd, anulowanie przez wojnę i brak wielokrotnego wypłacania odszkodowania.
- Nieagresja: blokada wojny gracza i silniejszej, wrogiej AI podczas trwania paktu; możliwość wojny po wygaśnięciu.
- Polityka prowincji: gospodarcze koszty ustrojów, odstęp zmian, rzeczywisty garnizon, pochodzenie miasta, okupacja, powrót do założycieli i lokalne skutki dialogu.
- Bunt: dwa ostrzeżenia przed utratą miasta, odtworzenie licznika po zapisie, odzyskanie kontroli przez poprawę lojalności, transfer granic, wycofanie garnizonu i usunięcie nieaktualnej sprawy rady. Własne miasta nie przechodzą do przypadkowego państwa.
- Integracja walki: rzeczywiste zdobycie miasta i zniszczenie drogi tworzą krzywdy po stronie pokrzywdzonego.
- Zapis v4: deterministyczna kontynuacja z oczekującym poselstwem i późniejszymi decyzjami; odrzucanie uszkodzonych misji, lojalności i roszczeń. Migracje v1/v2/v3 zachowują dotychczasowe dane i oryginały w kopiach `.v1.bak`, `.v2.bak`, `.v3.bak`.
- Cztery pasywne kampanie: ziarno 17421 zakończone po 279 turach, 2026 po 220, 8 po 205, 99 po 191. AI rozszerzyła państwa do 15–19 miast łącznie. Zakończenia obejmują utratę ostatniego miasta i zwycięstwa kulturowe AI. Są to kontrole integralności, nie ocena balansu dla człowieka.
- Testy WPF obsługują rzeczywiste przyciski negocjacji, deklaracji wojny, anulowania rozkazu, ustroju, dialogu autonomii i wczytania. Sprawdzają też położenie stolicy w widocznym obszarze po zmianie kampanii i wczytaniu.
- Scenariusze WPF nadają dodatkowe złoto, relacje, krzywdy lub miasta, aby w krótkim teście osiągnąć konkretne sytuacje. Nie są to standardowe warunki nowej kampanii.
- Wizualnie sprawdzono renderowane widoki negocjacji, rady wojennej, odpowiedzi posłów, rady prowincji i zarządzania. Rendery pochodzą z WPF `RenderTargetBitmap`, bez przechwytywania pulpitu.
- Celowo wywołana awaria opublikowanej diagnostyki zwróciła kod 3 (`process-exit-check.txt`). Katalog `diagnostics/` opisuje oczekiwaną awarię testową. Test interfejsu w skrypcie weryfikacji ma limit 60 sekund.
- Wydanie `wydanie-0.4/` i paczka `Korony-i-Pogranicza-0.4-Windows.zip` zawierają program oraz dokumentację. ZIP nie zawiera prywatnych zapisów. Do lokalnego katalogu wydania skopiowano stare zapisy bez nadpisywania. Poprzednie katalogi i paczki pozostają zachowane.

Nadal wymagane są wielogodzinne rozgrywki z ludźmi, balans i testy na różnych komputerach. Dyplomacja obsługuje dwa rodzaje poselstw i uproszczony pokój; AI odpowiada na warunki, lecz nie prowadzi własnych wielostronnych negocjacji. Grafika pozostaje mapą 2D.