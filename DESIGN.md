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

Poparcie trzech grup i globalna stabilność upraszczają prawdziwe społeczeństwo. Nie ma jednostkowych mieszkańców ani pełnego rynku cen. Technologie zmieniają statystyki dawnych rodzajów oddziałów; nie ma jeszcze historycznych modeli wojsk dla każdej epoki. Jednostki na jednym heksie są wybierane osobno w panelu. Stan granic i miast na odkrytej mapie nie przechowuje jeszcze osobnych migawek wywiadu.

Wersja 0.2 realizuje drogi, zasięg dostaw liczony po terenie, odcinanie połączeń, marsze wieloturowe i podgląd starcia. Każde miasto jest źródłem dostaw wojskowych; nie ma jeszcze limitów tonażu, magazynów frontowych, konwojów ani morskiego transportu. Droga na neutralnej ziemi ma opiekuna finansującego utrzymanie; zajęcie miasta może przejąć okoliczne drogi.

Wersja 0.3 dodaje potrzeby mieszkaniowe i opiekę, dobrobyt, kierunki rozwoju prowincji oraz ograniczoną wymianę nadwyżek żywności. Rynki powstają w każdej bezpiecznej sieci drogowej, także odłączonej od stolicy. Wymiana żywności zachowuje całkowitą nadwyżkę i jest ograniczona do 6 na miasto. Migracje przenoszą mieszkańców między własnymi miastami, nie tworzą populacji i nie przekraczają granic państwa. Ceny trzech towarów dodaje wersja 0.6, a ruchome karawany 0.7. Migracje międzynarodowe pozostają dalszym zakresem.

Sprawy głodu, mieszkań i przyjęcia przybyszów trafiają do rady z identyfikatorem konkretnego miasta. Ich lokalne prawa są dziedziczone razem z miastem. Ten sam typ problemu można rozstrzygnąć osobno w innych prowincjach; nie można wielokrotnie pobrać nagrody za tę samą sprawę w jednym miejscu. Przykładowo karta mieszkaniowa dodaje miejsca, domy cechowe wiążą je ze stałą opłatą, a integracja usuwa niepokoje kolejnych przybyszów.

Dyplomatyczne wypowiedzi są autorskimi, ustalonymi tekstami, a nie generowaniem przez LLM podczas gry. AI nie jest uczonym modelem: korzysta z wyszukiwania ścieżek i heurystyk. Poziom trudny podnosi jej przychód i produkcję do 130%, spokojny obniża je do 85%. Testy kampanii wykrywają błędy stanu; nie zastępują testów balansu i przyjemności rozgrywki.

Wersja 0.4 łączy uzasadnienia wojny z kierunkową pamięcią krzywd i reakcją pozostałych państw. Poselstwa mają czas podróży i jawny model odpowiedzi; stan armii lub relacji można zmienić przed powrotem posłów. Obsługują odszkodowanie i nieagresję.

Wersja 0.5 dodaje ogłoszony cel wojenny oraz zapisaną historię konfliktu. Cel rozlicza polityczną obietnicę, bez blokowania ruchu armii poza wskazanym miastem. Bilans wojny korzysta ze zmian kontroli względem początku konfliktu, obrażeń, siły armii i wyczerpania; odzyskanie miasta odwraca jego wagę. Dwustronne rokowania obejmują reparacje i zwrot jednego zajętego miasta założycielom. Warunki mają czas podróży i są ponownie sprawdzane przed wykonaniem w całości.

AI może odpowiedzieć kontrofertą lub wysłać własną propozycję przy wyczerpaniu albo porażkach. Pokój z graczem wymaga jego decyzji. Brak odpowiedzi ma termin, po którym oferta wygasa bez kończenia wojny. Pierwszy negocjowany traktat prowadzi do dialogu o odbudowie i pamięci ofiar. Nie ma jeszcze wielostronnych sojuszy, konferencji wszystkich uczestników wojny, dowolnej wymiany terytoriów ani rozbudowanych osobowości dyplomatycznych.

Miasta pamiętają założycieli. Lokalna lojalność zależy od potrzeb, polityki państwa, garnizonu i ustroju prowincji. Autonomia oddaje dochód za poparcie, zarząd wojskowy wymaga stałej obecności siły. Bunt obcego miasta jest deterministyczny i wymaga trzech kolejnych tur niskiej lojalności, poprzedzonych ostrzeżeniami. Wraca ono do istniejącego identyfikatora państwa założycieli, które może w ten sposób odzyskać ostatnie miasto. Nie ma jeszcze nowych państw secesyjnych ani wojny domowej we własnym rdzeniu. W zapisie sprzed v4 założycielem staje się dotychczasowy właściciel, ponieważ wcześniejszy format nie zachował tej informacji.

Wersja 0.6 wprowadza produkcję, zapasy i zużycie trzech towarów. Zamiast automatycznej premii za obecność towaru dobrobyt i przemysł korzystają z faktycznego zaspokojenia popytu. Rynki drogowe mają wspólne ceny, a każdy bilans zachowuje ilość towarów. Kontrakty zagraniczne księgują fizyczny towar i transfer złota. W 0.6 transport był abstrakcyjnym łącznikiem bezpiecznych stolic; wersja 0.7 zastępuje go karawanami. AI handluje między swoimi państwami, bez podpisywania umów za gracza. Embargo, rezerwa krajowa i swobodny eksport stawiają interesy mieszkańców wobec przychodów zagranicznych. Nie ma jeszcze kupców jako samodzielnych agentów, wyczerpywania złóż, kredytu ani pełnego zestawu cen wszystkich dóbr.

Wersja 0.7 wiąże kontrakty z ładunkami poruszającymi się po heksach. Płatność przy załadunku przenosi ryzyko na nabywcę, a jedna karawana w drodze na umowę wiąże przepustowość z geografią. Wygaśnięcie umowy nie usuwa opłaconego towaru. Ceny i potrzeby odbiorcy reagują dopiero na przybycie. Prognoza rynku kopiuje manifesty: nie zmienia pozycji, zapasów ani skarbca. Bilans globalny obejmuje magazyny, ładunki w drodze, konsumpcję, przepełnienie oraz jawne utraty.

Karawany przeliczają najtańszą bezpieczną trasę przy każdej fazie. Prawo tranzytu wymaga handlu państwa trzeciego z obiema stronami. Osłona wojskowa wynika z rzeczywistej pozycji oddziałów; nie ma osobnego rozkazu automatycznego podążania za wozem. Przechwycenie jest deterministyczne, nie losowe. Brak drogi, wojna stron i embargo dają trzy fazy na przywrócenie ruchu przed utratą ładunku. Model dotyczy trzech towarów zagranicznych; krajowa wymiana, żywność i wojskowe dostawy nadal używają swoich systemów. Nie ma flot, ubezpieczeń, samodzielnych przedsiębiorstw kupieckich ani adaptacyjnego planowania eskort przez AI.
## Kolejne etapy

1. **Balans i wygoda:** rozgrywki z ludźmi, pomiary tempa, tryb doradcy, komunikaty o zagrożonych trasach, dokładniejsza prezentacja skutków ekonomicznych i testy różnych skal DPI. Marsze, bilans utrzymania, podgląd bitwy i pełny zasięg wykonano w 0.2.
2. **Głębsza symulacja:** wojskowe magazyny i konwoje zaopatrzeniowe, migracje międzynarodowe, wyczerpanie złóż, klasy społeczne prowincji, wielostronne sojusze i wojny koalicyjne. Podstawową wymianę żywności i wewnętrzne migracje wykonano w 0.3, uzasadnienia wojny i proste poselstwa w 0.4, dwustronny negocjowany pokój i ogłoszone cele w 0.5, ceny i regionalny popyt na trzy towary oraz kontrakty w 0.6, lądowe karawany handlowe w 0.7.
3. **Dramaturgia:** długie wątki postaci, konflikty między prowincjami, sukcesja, dziedziczone instytucje, reakcje zagraniczne na konkretne obietnice, większa liczba zdarzeń zależnych od kontekstu.
4. **Oprawa 3D:** niezależny renderer korzystający z istniejącego silnika C#, modele miast rozwijające się wraz z populacją, realistyczne materiały PBR, oświetlenie, woda, pogoda, animacje armii i pełna obsługa kamery. Przed wyborem docelowego silnika potrzebny jest osobny prototyp wydajności i potwierdzenie zgodności z .NET 10.
5. **Pełna produkcja:** redakcja treści, dźwięk, dostępność, lokalizacja, migracje wersji zapisów, rozbudowa AI, testy sprzętowe, kampanie dla pozostałych państw i dystrybucja.

Nie ma podstaw, by na tym etapie deklarować jakość wyższą niż konkretna komercyjna gra. O tym zdecydują kompletność, testy z graczami i kolejne iteracje.
