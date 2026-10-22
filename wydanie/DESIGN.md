# Korony i Pogranicza — kierunek projektu

## Tożsamość

Strategia o ciągłości państwa. Rozwój ma wynikać z geografii, instytucji i decyzji podejmowanych wobec konkretnych ludzi. Nie chodzi o odtworzenie reguł, interfejsu czy treści innej gry. W fikcyjnej Dolinie Veyru ścierają się wspólnoty rolnicze, rody zbrojne, uczeni i kupcy. Ich interesy stanowią podstawę dialogów.

Pierwsza kampania obsługuje Marchie Veyru; Liga Talassu, Arweńska Unia i Klanowe Ziemie Kharu są sąsiadami. Gracz zachowuje miasta, badania i historię przez wszystkie etapy rozwoju. Epoka jest opisem dorobku wiedzy, nie momentem wymuszonego porzucenia państwa.

## Zrealizowana pętla

Odkrywanie → osadnictwo → nadwyżka żywności → mieszkańcy → produkcja i wiedza → instytucje i armia → nowe interesy społeczne → wybór rady → bezpośrednia zmiana i przyszła konsekwencja → korekta polityki.

Przykład: karta wolnej ziemi kosztuje 30 złota i poparcie rodów, a wzmacnia lud. Po ośmiu turach przynosi dochód. Po 35. turze wraca pytanie, czy chronić gospodarzy przed wykupem długów. Utrzymanie lub złamanie obietnicy wpływa na stabilność, prestiż i poparcie. Te wartości zmieniają wydajność miast.

Skutki liczbowe odpowiedzi w interfejsie są generowane z tych samych danych co symulacja. Zapisy obejmują oczekujące skutki i generator losowy, więc wczytanie nie gubi zobowiązań.

## Granice obecnego modelu

320 tur to limit, a nie gwarantowany czas gry. Kalendarz jest umowny: każda tura przesuwa datę o 15 lat; cykl czterech sezonów reprezentuje warunki gospodarcze tego okresu. Nie jest to dosłowna symulacja czterech pór jednego roku.

Poparcie trzech grup i globalna stabilność upraszczają prawdziwe społeczeństwo. Nie ma jednostkowych mieszkańców ani pełnego rynku cen. Technologie zmieniają statystyki dawnych rodzajów oddziałów; nie ma jeszcze historycznych modeli wojsk dla każdej epoki. Jednostki na jednym heksie są wybierane osobno w panelu. Ruch daje się wykonać w ramach bieżących punktów; nie ma rozkazów marszu wieloturowego. Nie ma dróg jako osobnego ulepszenia. Zasięg dostaw to odległość od miasta, a nie model sieci logistycznej. Stan granic i miast na odkrytej mapie nie przechowuje jeszcze osobnych migawek wywiadu.

Dyplomatyczne wypowiedzi są autorskimi, ustalonymi tekstami, a nie generowaniem przez LLM podczas gry. AI nie jest uczonym modelem: korzysta z wyszukiwania ścieżek i heurystyk. Poziom trudny podnosi jej przychód i produkcję do 130%, spokojny obniża je do 85%. Testy kampanii wykrywają błędy stanu; nie zastępują testów balansu i przyjemności rozgrywki.

## Kolejne etapy

1. **Balans i wygoda:** rozgrywki z ludźmi, pomiary tempa, tryb doradcy, wieloturowe marsze, zestawianie kosztów utrzymania, dokładny podgląd bitwy i podświetlenie pełnego zasięgu.
2. **Głębsza symulacja:** regionalne rynki, drogi i dostawy, migracje ludności, wyczerpanie złóż, klasy społeczne prowincji, traktaty z warunkami i mechanika uzasadnienia wojny.
3. **Dramaturgia:** długie wątki postaci, konflikty między prowincjami, sukcesja, dziedziczone instytucje, reakcje zagraniczne na konkretne obietnice, większa liczba zdarzeń zależnych od kontekstu.
4. **Oprawa 3D:** niezależny renderer korzystający z istniejącego silnika C#, modele miast rozwijające się wraz z populacją, realistyczne materiały PBR, oświetlenie, woda, pogoda, animacje armii i pełna obsługa kamery. Przed wyborem docelowego silnika potrzebny jest osobny prototyp wydajności i potwierdzenie zgodności z .NET 10.
5. **Pełna produkcja:** redakcja treści, dźwięk, dostępność, lokalizacja, migracje wersji zapisów, rozbudowa AI, testy sprzętowe, kampanie dla pozostałych państw i dystrybucja.

Nie ma podstaw, by na tym etapie deklarować jakość wyższą niż konkretna komercyjna gra. O tym zdecydują kompletność, testy z graczami i kolejne iteracje.
