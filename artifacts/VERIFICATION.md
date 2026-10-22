# Weryfikacja wydania 0.2

Data: 21 października 2026 r. Środowisko: Windows, .NET SDK 10.0.401, .NET Desktop Runtime 10.0.12.

- Kompilacja Release całego rozwiązania: sukces, 0 ostrzeżeń, 0 błędów.
- 33 wykonywalne specyfikacje silnika: sukces. Raport: `core-specs.txt`.
- 17 kontroli integracyjnych WPF: sukces. Raport: `ui/ui-smoke.txt`.
- Sprawdzone przyciski: rozpoczęcie kampanii, wybór dialogu, zmiana badań, reforma, projekt produkcji, traktat i kolejne tury. Sprawdzono również połączenie miasta, zlecenie i anulowanie marszu, jego kontynuację przez kolejne tury, otwarcie i wycofanie podglądu bitwy oraz potwierdzenie zgodnego z nim ataku.
- Sprawdzone pełne symulacje czterech ziaren do zakończenia kampanii; przetestowane również połączenia stolic i wszystkie odpowiedzi dialogowe. Symulacje prowadzą pasywnego gracza bez ręcznej ekspansji i nie mierzą balansu dla aktywnej osoby.
- Sprawdzono drogi, koszty utrzymania, gospodarcze premie połączeń, odcinanie tras, marsze i anulowanie rozkazów, ruiny na polach pośrednich, czystość podglądu starcia, zgodność obrażeń, nową politykę dialogową, migrację v1 i kopię starszego pliku zapisu.
- Wizualnie sprawdzone rendery panelu logistyki, podglądu walki i układu przy oknie 1200×800. Rendery pochodzą z WPF `RenderTargetBitmap`, bez przechwytywania pulpitu. Testy nie zastępują ręcznego sprawdzenia fizycznego myszy, klawiatury ani różnych monitorów i skal DPI.
- Sprawdzone opublikowane wydanie: uruchomienie i poprawny render menu z wbudowaną ilustracją (`published-menu.png`).
- Paczka: `Korony-i-Pogranicza-0.2-Windows.zip`, wydanie wymagające zainstalowanego .NET Desktop Runtime 10 x64. Poprzedni katalog i paczka 0.1 są zachowane.

Nie wykonano wielogodzinnych testów z ludźmi, badań użyteczności ani pomiarów na różnych komputerach. Wersja ma status prototypu rozwojowego.
