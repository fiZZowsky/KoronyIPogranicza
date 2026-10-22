# Weryfikacja wydania 0.6

Data: 22 października 2026 r. Windows, .NET SDK 10.0.401, .NET Desktop Runtime 10.0.12.

- Kompilacja Release: sukces, 0 ostrzeżeń i 0 błędów.
- **105 specyfikacji silnika**: sukces (`core-specs.txt`). Względem 0.5 dodano 20 scenariuszy rynku i handlu.
- **61 kontroli WPF**: sukces (`ui/ui-smoke.txt`). Opublikowane wydanie przeszło te same kontrole (`published-ui-0.6/ui-smoke.txt`), w tym 12 nowych ścieżek rynku.
- Produkcja: pojedyncze przypisanie pracującego zasobu przy nakładaniu się zasięgów, pierwszeństwo najbliższego miasta, brak wydobycia na neutralnym polu i blokada dostaw przez wojsko.
- Bilans: czystość prognozy, globalne zachowanie towarów, zgodność przesyłek krajowych, ograniczenie zapasów i wykazywanie utraty ponad pojemność. Magazyn zachowuje nadwyżkę, która bez niego przepada.
- Ceny: reakcja na popyt i zapasy, górny limit oraz wspólna cena w bezpiecznej sieci drogowej.
- Rozliczenie: zużycie i opodatkowanie raz na turę, zapisana historia ostatniego bilansu, wpływ zaopatrzenia na produkcję i cel dobrobytu, zgodność raportu gospodarczego z dochodem państwa.
- Kontrakty: transfer realnych ilości, zgodne płatności między skarbcami bez tworzenia złota, rezerwa dla mieszkańców i możliwość jej wywozu przy swobodnym eksporcie.
- Ubóstwo nabywcy: dostawy częściowe, brak darmowego towaru przy zerowym budżecie i brak debetu. Nieprawidłowa ilość, brak opłaty oraz duplikat umowy nie zmieniają stanu.
- Polityka: koszt i odstęp zmian, dialog rady, embargo wstrzymujące dostawy bez usuwania umowy, wznowienie po przywróceniu warunków. Wojna i brak traktatu również wstrzymują handel.
- Cykl umów: wygaśnięcie, rozwiązanie przez gracza, zatrzymanie po przejęciu miasta, brak powtórnego wykonywania płatności i zwalnianie limitu po rozwiązaniu kontraktu.
- AI: import z innego państwa AI przy rzeczywistym niedoborze, bez możliwości podpisania umowy za gracza. Limit trzech aktywnych umów obowiązuje każdą stronę.
- Zapis v6: odrzucanie nieprawidłowych magazynów, bilansów, terminów i płatności; deterministyczna kontynuacja aktywnych kontraktów po wczytaniu. Migracja v5 zachowuje wojny i rokowania oraz zaczyna z pustymi magazynami. Nadpisanie zachowuje oryginał w `.v5.bak`. Kontrole formatów v1–v4 również przeszły.
- Cztery pasywne kampanie zakończyły się po 135, 140, 128 i 110 turach dla ziaren 17421, 2026, 8 i 99. Maksymalne liczby miast: 20, 16, 18, 16. Wszystkie zakończyły się utratą ostatniego miasta gracza. Są to kontrole integralności, nie dowód dobrego balansu dla człowieka.
- WPF: prognoza krajowej przesyłki, formularz importu soli, realna dostawa i podatki po turze, szczegółowy bilans, wybór składu towarowego, embargo, wznowienie poprzez dialog rady, zapis i wczytanie, rozwiązanie umowy oraz układ przy mniejszym oknie.
- Scenariusze WPF są przygotowane: dodatkowe złoto, magazyn dostawcy, targowiska, technologie i sieć dróg. W scenariuszu rady przestawiono numer tury, aby sprawdzić regulację po odstępie. Nie są to standardowe warunki startowe ani test czasu potrzebnego do osiągnięcia tej sytuacji.
- Wizualnie sprawdzono rzeczywiste rendery rynku, bilansu miasta, kontraktu, polityki i dialogu rady; finalny panel opublikowanego programu także został obejrzany. Rendery pochodzą z WPF RenderTargetBitmap, bez przechwytywania pulpitu.
- Opublikowana diagnostyka zwróciła oczekiwany kod 3 po celowym błędzie (`process-exit-check.txt`). Katalog `diagnostics/` zawiera opis tej oczekiwanej awarii testowej.
- Wydanie `wydanie-0.6/` i paczka `Korony-i-Pogranicza-0.6-Windows.zip` zawierają program i dokumentację. ZIP nie zawiera prywatnych zapisów. Stare zapisy skopiowano lokalnie bez nadpisywania; wcześniejsze katalogi i paczki pozostają zachowane.

Wciąż potrzebne są wielogodzinne rozgrywki z ludźmi, balans i testy sprzętowe. Rynek obejmuje trzy towary, bez wyczerpywania złóż i samodzielnych agentów kupieckich. Transport zagraniczny jest abstrakcyjnym łącznikiem bezpiecznych stolic, bez ruchomych karawan i pełnego wyszukiwania ich tras. Oprawa pozostaje mapą 2D.