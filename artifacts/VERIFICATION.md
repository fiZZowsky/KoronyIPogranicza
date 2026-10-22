# Weryfikacja wydania 0.3

Data: 22 października 2026 r. Środowisko: Windows, .NET SDK 10.0.401, .NET Desktop Runtime 10.0.12.

- Kompilacja Release całego rozwiązania: sukces, 0 ostrzeżeń, 0 błędów.
- 50 wykonywalnych specyfikacji silnika: sukces. Raport: `core-specs.txt`.
- 27 kontroli integracyjnych WPF: sukces. Raport: `ui/ui-smoke.txt`.
- Sprawdzone podstawy 0.1 i 0.2: gospodarka, badania, dialogi, dyplomacja, walka, osadnictwo, drogi, logistyka, blokady, marsze i zgodność podglądu ataku.
- Nowe kontrole: zachowanie żywności w wymianie, limity transportu, priorytet niedoborów, rynki bez dostępu do stolicy, blokada dostaw i towarów, rozliczanie importu przed głodem, pojemność mieszkań i opieki, wpływ dobrobytu, koszty priorytetów i pomocy oraz reakcje AI.
- Migracje sprawdzono pod kątem zachowania liczby mieszkańców, wymagań celu, połączeń drogowych i odstępu między ruchami. Sprawdzono miejscowe prawa, kontekst miasta w dialogu, zapobieganie powtórnemu odebraniu nagrody oraz zgodność po wczytaniu.
- Migracja v1 i v2 zachowuje drogi, rozkazy oraz oryginalne pliki w kopiach zapasowych. Stara, nieograniczona skala niepokojów jest dostosowywana do przedziału 0–100. Zapis v3 odrzuca błędny kontekst i historię migracji.
- Pełne symulacje czterech ziaren dotarły do zakończenia kampanii. Pasywny gracz przegrywał wskutek wojen lub dorobku przeciwników; to test integralności, nie pomiar balansu dla aktywnej osoby.
- Wizualnie sprawdzone widoki prowincji, zarządzania miastem i rady. Oznaczenia przepływu żywności i migracji są rozdzielone. Rendery pochodzą z WPF `RenderTargetBitmap`, bez przechwytywania pulpitu.
- Testy WPF obsługują przyciski i formularze używane w grze, w tym zmianę lokalnego priorytetu, pomoc, kartę mieszkaniową, integrację i ponowne wczytanie historii.
- Celowo wywołana awaria diagnostyczna zakończyła proces kodem 3. Raport: `process-exit-check.txt`. Katalog `diagnostics/` opisuje ten oczekiwany błąd, a nie nieudaną zwykłą rozgrywkę.
- Opublikowane wydanie uruchamia testy interfejsu i render menu z osadzoną ilustracją (`published-menu.png`). Paczka: `Korony-i-Pogranicza-0.3-Windows.zip`. Wymagany .NET Desktop Runtime 10 x64; poprzednie wydania są zachowane.

Nie wykonano wielogodzinnych testów balansu z ludźmi, badań użyteczności ani pomiarów na różnych komputerach. Grafika pozostaje mapą 2D, a migracje i wymiana są modelem uproszczonym.