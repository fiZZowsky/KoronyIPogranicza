# Korony i Pogranicza · 0.1

Autorska, jednoosobowa strategia turowa dla Windows, napisana w **C# / .NET 10**, z interfejsem i dialogami po polsku. Historycznie inspirowany świat jest fikcyjny. Gracz prowadzi Marchie Veyru od osadnictwa po odkrycia naukowe i instytucje nowoczesnego państwa.

To **grywalna wersja rozwojowa**, a nie ukończona produkcja o skali komercyjnej Cywilizacji. Fundamentem wyróżniającym projekt są pamięć polityczna państwa, stronnictwa społeczne i konsekwencje dialogów. Zmiana epoki nie usuwa miast ani historii.

## Uruchomienie

Uruchom `wydanie/KoronyIPogranicza.exe` albo `Uruchom.cmd`. Zachowaj pozostałe pliki z katalogu `wydanie` obok programu.

Wymagania: Windows 10/11 i **.NET Desktop Runtime 10 x64**. Na komputerze, na którym powstał projekt, ten runtime jest zainstalowany. Wydanie korzysta z zainstalowanego środowiska .NET, nie zawiera własnego runtime'u. Gra nie wymaga połączenia z Internetem ani konta.

## Co jest grywalne

- Świat 42 × 28 heksów: ziarno proceduralne, biomy, rzeki, zasoby, ruiny, mgła wojny, przybliżanie i minimapa. Stolice mają gwarantowane połączenie lądowe.
- Cztery fikcyjne państwa z różnymi premiami; gracz prowadzi jedno, trzy obsługuje AI. Trzy poziomy trudności zmieniają wydajność AI.
- Miasta, ludność, wzrost i głód, sezonowe plony, eksploatacja pól, farmy, kopalnie, placówki handlowe, produkcja i utrzymanie.
- 24 technologie z wymaganiami; 15 trwałych projektów miejskich, dwa projekty powtarzalne i sześć rodzajów oddziałów.
- Lud, rody i uczeni: ich poparcie wpływa na docelową stabilność, a ta na gospodarkę. Ustawy zmieniają podatki, ustrój i priorytety.
- 17 wyjątkowych spraw rady oraz cztery wydarzenia powracające. Odpowiedzi mają konkretne koszty, wpływ na poparcie, trwałe znaczniki historii i następstwa po kilku lub kilkunastu turach. Nadanie ziemi lub przywilejów zmienia późniejszą gałąź dialogów.
- Dyplomacja: dary, relacje, traktaty dające dochód, wypowiedzenie wojny, reparacje, rozejmy. AI zakłada miasta, bada, buduje, ulepsza ziemię i prowadzi działania wojenne.
- Walka wręcz i dystansowa, doświadczenie, obrona terenu, oblężenia i przejmowanie miast. Woda i góry blokują ruch. Armie poza zasięgiem dostaw słabną; odpoczynek leczy.
- Zwycięstwo naukowe, kulturowe, przez zjednoczenie oraz punktowe po 320 turach. Kampania może zakończyć się wcześniej.
- Zapis i wczytanie stanu JSON z walidacją, atomowym zastępowaniem pliku i zapisanym stanem generatora losowego. Autozapis po turze i decyzji rady; kronika przechowuje wybory oraz ich skutki.

Długość i balans wymagają dalszych rozgrywek z udziałem człowieka. Automatyczne symulacje nie potwierdzają jakości wielogodzinnej rozgrywki.

## Pierwsze dziesięć tur

1. Rozpocznij nową historię i odpowiedz kanclerce w sali rady. Każda opcja pokazuje pełne zmiany liczbowe oraz przyszłe skutki.
2. Wybierz zwiadowcę LPM. Wskaż PPM pobliskie odkryte pole, aby przesunąć oddział i odsłonić kolejne ziemie. Ruiny dają złoto, naukę i prestiż.
3. Odkryj Rolnictwo. W Wysogrodzie wybierz spichlerz, a potem osadników. Rekrutacja wymaga co najmniej trzech mieszkańców i zużywa jednego.
4. Miasto można założyć na lądzie co najmniej cztery pola od innych miast. Wybierz osadników i przycisk założenia miasta w panelu.
5. Własne pola możesz ulepszać za złoto. Miasto wykorzystuje najlepsze pola w promieniu dwóch heksów, do limitu zależnego od ludności. Ulepszenie odległego, niepracującego pola nie daje dochodu.
6. Zachowaj rezerwę skarbu. Biblioteki podnoszą naukę, traktaty dają dochód, a nadmierna armia i wysokie podatki mają koszty społeczne.

## Sterowanie i zapisy

| Akcja | Sterowanie |
|---|---|
| Wybierz pole / oddział | LPM; przy kilku oddziałach wybierz konkretny w prawym panelu |
| Przesuń mapę | Przeciągnij LPM |
| Ruch / atak | PPM na celu w zasięgu |
| Przybliżenie | Kółko myszy |
| Następny gotowy oddział | Tab |
| Centrum na stolicy | Home |
| Zakończ turę / otwórz oczekującą radę | Spacja |
| Zapisz / wczytaj | F5 / F9 |
| Zamknij okno dialogu | Esc |

Pliki `.crown` znajdują się domyślnie w `wydanie/saves/`. `autosave.crown` jest nadpisywany; osobne zapisy pozwalają zachować poprzednie kampanie. Kopiuj cały katalog `saves`, aby przenieść historię. Nie zapisuj jedynej kopii kampanii w katalogu tymczasowym.

## Budowanie i weryfikacja

Bez zależności NuGet i bez zewnętrznego silnika gry. Potrzebny .NET SDK 10 dla Windows z pakietami referencyjnymi Windows Desktop.

```powershell
dotnet build KoronyIPogranicza.slnx -m:1 -c Release -p:MSBuildEnableWorkloadResolver=false -p:TargetPlatformDisplayName=Windows
dotnet run --project tests/Crown.Specs -c Release -p:MSBuildEnableWorkloadResolver=false
dotnet publish src/Crown.Desktop -c Release --self-contained false -o wydanie -p:MSBuildEnableWorkloadResolver=false -p:TargetPlatformDisplayName=Windows
```

`NuGet.Config` celowo nie zawiera źródeł zewnętrznych. Parametr `TargetPlatformDisplayName` pozwala budować także w środowisku ograniczającym odczyt rejestru Windows. `Zweryfikuj.ps1` buduje, uruchamia specyfikacje silnika i testy integracji WPF. Raporty oraz rzeczywiste rendery aplikacji znajdują się w `artifacts/`.

## Architektura i dalsza produkcja

- `src/Crown.Core`: model świata, symulacja, AI, treść dialogów i serializacja; niezależne od WPF.
- `src/Crown.Desktop`: natywny interfejs WPF, rysowanie mapy, proceduralne materiały terenu i sterowanie.
- `tests/Crown.Specs`: wykonywalne specyfikacje bez bibliotek testowych; symulacje kampanii i test odtwarzalności zapisu.
- `DESIGN.md`: założenia projektu, ograniczenia i kierunek dalszych etapów.
- `ASSETS.md`: pochodzenie ilustracji AI, ścieżka i pełny prompt.

Obecna mapa jest **teksturowaną mapą 2D z umownymi budynkami i symbolami jednostek**. Realistyczna ilustracja menu nie jest obrazem rozgrywki. Pełna realistyczna oprawa 3D, animacje i udźwiękowienie pozostają do wykonania. Nie ma jeszcze flot, religii, szpiegostwa, multiplayera ani edytora treści. AI używa jawnych heurystyk; dyplomacja i symulacja społeczna wymagają rozbudowy przed pełnym wydaniem.
