# Epoki i możliwe przyszłości — podręcznik wydania 0.26

Kampania obejmuje 18 głównych etapów, 90 technologii, 58 budynków rozwoju epok, 34 nowe modele wojsk i pięć programów pozaziemskich. Historyczne nazwy porządkują fikcyjny świat; regionalne podokresy grupujemy w większe bloki. Przyszłe epoki są scenariuszami gry, a nie prognozą lub obietnicą dostępności technologii.

Otwórz **Nauka → Epoki i rozwój świata**. Każdy etap ma cztery dziedziny i odkrycie zamykające dorobek. Badania następnego etapu wymagają tego zamknięcia. Dodatkowe wymagania technologii nadal obowiązują. Państwa rozwijają się oddzielnie i zachowują całą wcześniejszą wiedzę, miasta oraz instytucje.

Nowa kampania zaczyna się w paleolicie i ma limit 1600 tur. Kalendarz jest umowny: stopniowo zwalnia i uwzględnia najbardziej zaawansowane państwo; jego skala sięga roku 5000. Tempo wiedzy nie jest historycznym rozkładem dat. Stare kampanie zachowują zasady i limit 320 tur do wybrania **Rozszerz kampanię do 18 epok i 1600 tur**. Wtedy uznajemy zamknięcia epok poprzedzających posiadany już dorobek, zachowując miasta, środki, wywiad, długi, jednostki i plany.

## Zasady wspólne

- Nowe technologie dają opisane premie miastom. Budynki są fizycznymi projektami produkcji, a nie nagrodą przy zmianie nazwy epoki. Mieszkania, opieka, obrona i zasięg działają w dotychczasowych systemach.
- Energia jest lokalna dla bezpiecznej krajowej sieci drogowej. Suma produkcji energii zasilana jest także przez wybrane instytucje epok. Obiekt z popytem otrzymuje 25% + 75% × pokrycie energii swoich premii do żywności, produkcji, złota, nauki i kultury. Mieszkania i opieka pozostają, a utrzymanie jest płacone w całości. Energię współdzielą połączone miasta; wrogowie mogą odciąć połączenie. W wersji 0.18 przydział uwzględnia pierwszeństwo, priorytet zwykły i ograniczony. Miasta o równym priorytecie dzielą moc proporcjonalnie do potrzeb. Dyspozycje znajdziesz także bezpośrednio w panelu wybranego miasta.
- Zakłady energetyczne i wymagające energii można wyłączyć. Ustają ich premie, dostawy, opieka, energia i emisje; pozostają miejsca mieszkalne oraz 25% utrzymania obiektu. Ponowny rozruch kosztuje 5% kosztu budowy w złocie (w górę). Stan każdego obiektu i priorytet miasta zmienisz raz na turę. Zapis i wczytanie nie resetują tego ograniczenia. Wyłączona infrastruktura nie spełnia wymagań programu kosmicznego.
- Wodna elektrownia wymaga własnej rzeki przy mieście; geotermalna dwóch własnych pól wzgórz lub gór, licząc pole miasta. Moc wiatru i słońca zależy od sezonu oraz opisanego terenu; wartości to umowne reguły gry, bez pogody losowej lub naukowej prognozy. Moc zaokrąglamy w dół. Panel pokazuje bieżącą moc oraz nominalny katalog.
- Faza gospodarcza używa wspólnego planu energii i emisji z początku rozliczenia. Nowa elektrownia daje moc w następnej fazie; kolejność miast nie pozwala zasilić wcześniej ukończonego programu w tej samej turze. Kolejny sezon może zmienić prognozę.
- Od wersji 0.19 trzy kolejne fazy z dodatnim popytem i pokryciem poniżej 75% wywołują kryzys. Niepokoje miasta +2 i dobrobyt -1 na fazę; lud państwa -1 niezależnie od liczby miast, także AI. Delegacja własnego miasta oferuje pomoc, obietnicę lub odmowę. Obietnica wymaga trzech kolejnych przyszłych pełnych faz w sześć tur i działania usług protokołu. Wyłączenie tych usług nie jest naprawą. Rejestr w panelu energii pokazuje rzeczywiste dowody i wynik. Pełne zasady oraz skutki w README.
- Emisje i redukcje wszystkich państw zmieniają presję klimatyczną o ich sumę / 200 na turę, w zakresie 0–100. Presja ogranicza plony o 0,35% na punkt, maksymalnie 35%, przed utrzymaniem ludności. Redukcje wymagających energii obiektów również zależą od pokrycia; nie ma losowych katastrof klimatycznych.
- Każda epoka ma własny dialog z trzema trwałymi kierunkami rozwoju. Koszty polityczne i złoto są widoczne w radzie. Premie procentowe wybranych instytucji sumują się; infrastruktura energetyczna, emisje i zasięg są podane osobno. Wolna odpowiedź pozostaje dostępna przy pustym skarbie. Nie można zmienić już przyjętej instytucji ani odebrać jej drugi raz.
- Modele wojsk zachowują lądowe role piechoty, jazdy, zwiadu i artylerii, lecz mają własną siłę, ruch, zasięg i koszt. Utrzymanie nowego modelu to 1,4 + 0,6 × numer epoki od zera złota na turę. Jazda konna wymaga koni, czołgi i łaziki tego zasobu nie wymagają. Artyleria nie zdobywa miasta; jej dalszy ostrzał jest pośredni, z dotychczasowym sprawdzeniem przesłony dla dystansu dwóch pól.
- Modernizacja wymaga własnego miasta, dostaw, pełnego ruchu i braku eskorty. Koszt: 60 + 3 × przyrost bazowej siły, uwzględniającej posiadane premie materiałowe dawnych oddziałów. Słabszy model jest niedostępny. Zdrowie i doświadczenie pozostają, ruch i umocnienia przepadają. Możesz także rekrutować nowe modele w produkcji miast. Nowoczesne wojska są jednostkami lądowymi; nazwy orbitalne opisują ich pochodzenie, nie osobny system lotu.
- AI korzysta z tych samych technologii, energii, budynków, programów i modeli oraz opłaca modernizacje. Jej wybory instytucji wynikają z prostych preferencji i środków, nie generowanych dialogów. Dawna premia poziomu trudności nadal obowiązuje.

## 01. Paleolit

Prehistoria · łowcy i zbieracze. Ogień, narzędzia, wspólna pamięć i podział żywności tworzą pierwsze wspólnoty.

| Technologia | Nauka | Wymagania wewnętrzne | Efekt |
|---|---:|---|---|
| Narzędzia kamienne | 24 |  | Ogień, narzędzia, wspólna pamięć i podział żywności tworzą pierwsze wspólnoty. Premia w każdym mieście: żywność +1. |
| Kontrola ognia | 27 | Narzędzia kamienne | Ogień, narzędzia, wspólna pamięć i podział żywności tworzą pierwsze wspólnoty. Premia w każdym mieście: produkcja +1. |
| Wspólne łowy | 31 | Narzędzia kamienne | Ogień, narzędzia, wspólna pamięć i podział żywności tworzą pierwsze wspólnoty. Premia w każdym mieście: nauka +1. |
| Tradycja ustna | 34 | Narzędzia kamienne | Ogień, narzędzia, wspólna pamięć i podział żywności tworzą pierwsze wspólnoty. Premia w każdym mieście: złoto +1 · kultura +1. |
| Wiedza wspólnoty | 38 | Narzędzia kamienne, Kontrola ognia, Wspólne łowy, Tradycja ustna | Zamyka dorobek epoki i otwiera badania: Neolit. Premia w każdym mieście: nauka +1 · kultura +1. |

| Nowa infrastruktura | Produkcja | Technologia | Działanie |
|---|---:|---|---|
| Wspólne obozowisko | 30 | Kontrola ognia | żywność +2 · Mieszkania +2 |
| Pracownia krzemienia | 35 | Narzędzia kamienne | produkcja +3 |
| Grota pamięci | 40 | Tradycja ustna | nauka +1 · kultura +3 |

| Model wojsk | Siła bazowa | Ruch | Zasięg | Produkcja | Technologia |
|---|---:|---:|---:|---:|---|
| Drużyna łowców | 12 | 2 | 1 | 32 | Wspólne łowy |
| Tropiciele | 9 | 3 | 1 | 28 | Narzędzia kamienne |

**Rada: Kto zasiądzie przy ogniu?**

Mamy dość umiejętności, by przeżyć kolejny sezon. Spór dotyczy podziału łowów, pracy i wiedzy. Zwyczaj przyjęty teraz może przetrwać tysiąclecia.

- Zapasy będą wspólne. Lud +6 · Rody -4 Wspólny stół i ochrona najsłabszych. Trwale: żywność % +8
- Pierwszeństwo otrzymają drużyny łowieckie. Lud -3 · Rody +5 Narzędzia i dyscyplina kosztem równego podziału. Trwale: produkcja % +8
- Pamięć i nauka należą do wszystkich. Uczeni +5 Opowieści nie będą przywilejem jednego rodu. Trwale: nauka % +8

## 02. Neolit

Prehistoria · osiadłe społeczności. Rolnictwo i hodowla pozwalają budować osady. Własność zapasów staje się sprawą polityczną.

| Technologia | Nauka | Wymagania wewnętrzne | Efekt |
|---|---:|---|---|
| Rolnictwo | 32 |  | Farmy, spichlerze i stabilne plony. +1 żywności z każdej farmy. |
| Garncarstwo | 40 |  | Magazynowanie i wymiana. Odblokowuje targowisko i budowę dróg. |
| Udomowienie zwierząt | 57 | Rolnictwo | Rolnictwo i hodowla pozwalają budować osady. Własność zapasów staje się sprawą polityczną. Premia w każdym mieście: nauka +1. |
| Tkactwo | 63 | Rolnictwo | Rolnictwo i hodowla pozwalają budować osady. Własność zapasów staje się sprawą polityczną. Premia w każdym mieście: złoto +1 · kultura +1. |
| Społeczeństwa osiadłe | 70 | Rolnictwo, Garncarstwo, Udomowienie zwierząt, Tkactwo | Zamyka dorobek epoki i otwiera badania: Epoka brązu. Premia w każdym mieście: nauka +1 · kultura +1. |

| Nowa infrastruktura | Produkcja | Technologia | Działanie |
|---|---:|---|---|
| Studnia wspólnoty | 55 | Rolnictwo | żywność +3 · Opieka +3 |
| Dom tkaczy | 65 | Tkactwo | produkcja +2 · złoto +5 |
| Dom przekazywania wiedzy | 75 | Społeczeństwa osiadłe | nauka +4 · kultura +2 |

| Model wojsk | Siła bazowa | Ruch | Zasięg | Produkcja | Technologia |
|---|---:|---:|---:|---:|---|
| Straż osady | 20 | 2 | 1 | 55 | Udomowienie zwierząt |

**Rada: Czyja ziemia, czyje ziarno?**

Osiadłe rodziny proszą o zasady dostępu do ziemi. Duże gospodarstwa chcą sprzedawać nadwyżki, a strażnicy spichlerzy proponują wspólną rezerwę.

- Utrzymamy wspólną rezerwę żywności. Złoto -30 · Lud +5 Rolnicy oddają część pracy za bezpieczeństwo zapasów. Trwale: żywność % +10
- Pozwolimy łączyć gospodarstwa. Lud -5 · Rody +7 Większy obrót, silniejsze rody, zależni dzierżawcy. Trwale: złoto % +10
- Ziemię dostaną rodziny pracujące na niej. Rody -4 Samodzielność gospodarzy wspiera uprawy i pracę. Trwale: żywność % +4 · produkcja % +4

## 03. Epoka brązu

Pierwsze miasta i pisma. Metalurgia, rachunki i monumentalna budowa wymagają pracy oraz odpowiedzialnej administracji.

| Technologia | Nauka | Wymagania wewnętrzne | Efekt |
|---|---:|---|---|
| Górnictwo | 44 |  | Kopalnie, kamieniołomy i dostęp do rudy. |
| Metalurgia brązu | 68 | Górnictwo | Koszary. +3 siły podstawowych oddziałów. Modele nowych epok mają własną siłę. |
| Pismo | 65 | Garncarstwo | Biblioteki, agenci i kontrwywiad. +15% nauki w całym państwie. |
| Kamieniarstwo | 80 | Górnictwo | Mury i akwedukty. |
| Archiwa miejskie | 120 | Górnictwo, Metalurgia brązu, Pismo, Kamieniarstwo | Zamyka dorobek epoki i otwiera badania: Epoka żelaza. Premia w każdym mieście: nauka +1 · kultura +1. |

| Nowa infrastruktura | Produkcja | Technologia | Działanie |
|---|---:|---|---|
| Odlewnia brązu | 100 | Metalurgia brązu | produkcja +7 |
| Szkoła skrybów | 100 | Pismo | złoto +3 · nauka +7 |
| Cysterna miejska | 110 | Kamieniarstwo | żywność +4 · Mieszkania +3 · Opieka +3 |

| Model wojsk | Siła bazowa | Ruch | Zasięg | Produkcja | Technologia |
|---|---:|---:|---:|---:|---|
| Gwardia brązu | 27 | 2 | 1 | 85 | Metalurgia brązu |

**Rada: Pieczęć na rachunkach**

Pismo pozwala utrwalać zobowiązania. Skrybowie chcą publicznych zapisów, rody własnych archiwów, a budowniczowie priorytetu dostaw.

- Rachunki będą publiczne. Złoto -60 · Uczeni +6 Szkoły i jawność kosztem skarbu. Trwale: nauka % +10
- Rody poręczą za obrót. Lud -3 · Rody +6 Prywatne archiwa i prawo kupieckie. Trwale: złoto % +10
- Najpierw dostawy dla wspólnych budów. Lud -2 Sprawniejsza praca i dłuższe dostawy. Trwale: produkcja % +5 · zasięg dostaw +1

## 04. Epoka żelaza

Państwa terytorialne. Żelazo, jeździectwo i drogi zmieniają wojnę, produkcję i kontrolę nad odległymi ziemiami.

| Technologia | Nauka | Wymagania wewnętrzne | Efekt |
|---|---:|---|---|
| Żelazo | 465 | Metalurgia brązu, Kamieniarstwo | +5 siły podstawowych oddziałów; dodatkowe +3 z własnym złożem żelaza. Nowe modele mają własną siłę. |
| Jeździectwo | 300 | Metalurgia brązu, Rolnictwo | Kawaleria; wymaga koni w granicach państwa. |
| Irygacja | 85 | Rolnictwo | +2 żywności w miastach nad rzeką. |
| Budowa traktów | 188 | Żelazo | Żelazo, jeździectwo i drogi zmieniają wojnę, produkcję i kontrolę nad odległymi ziemiami. Premia w każdym mieście: złoto +2 · kultura +2. |
| Państwa terytorialne | 208 | Żelazo, Jeździectwo, Irygacja, Budowa traktów | Zamyka dorobek epoki i otwiera badania: Starożytność klasyczna. Premia w każdym mieście: nauka +2 · kultura +2. |

| Nowa infrastruktura | Produkcja | Technologia | Działanie |
|---|---:|---|---|
| Dymarki | 135 | Żelazo | produkcja +10 · Emisje +1 |
| Targ przy trakcie | 140 | Budowa traktów | złoto +12 |
| Strażnica graniczna | 160 | Państwa terytorialne | Obrona +8 · Zasięg dostaw +1 |

| Model wojsk | Siła bazowa | Ruch | Zasięg | Produkcja | Technologia |
|---|---:|---:|---:|---:|---|
| Piechota żelazna | 36 | 2 | 1 | 120 | Żelazo |
| Jazda pogranicza | 39 | 3 | 1 | 145 | Jeździectwo |

**Rada: Prawo traktu i żelaza**

Granice państwa oddalają się od stolicy. Jedni chcą patroli, inni wolnych targów, a osady wodnych inwestycji.

- Utrzymamy patrole państwowe. Złoto -90 · Stabilność +3 Bezpieczniejszy zasięg dostaw za opłatę. Trwale: zasięg dostaw +2
- Otworzymy trakty kupcom. Rody -4 Większy obrót, mniejszy udział rodów. Trwale: złoto % +12
- Woda i pola mają pierwszeństwo. Lud +4 Lokalne wspólnoty utrzymają kanały. Trwale: żywność % +10

## 05. Starożytność klasyczna

Miasta, obywatelstwo, filozofia. Moneta i wiedza łączą rynki, lecz pytanie o prawa obywateli dzieli państwo.

| Technologia | Nauka | Wymagania wewnętrzne | Efekt |
|---|---:|---|---|
| Szlaki handlowe | 345 | Pismo | Placówki handlowe. +25% dochodu i +2 do zasięgu dostaw. |
| Filozofia | 405 | Pismo | Świątynie i instytucje. +20% kultury. |
| Moneta | 600 | Szlaki handlowe | Banki, wspólny rynek i pożyczki państwowe. +2 złota z miasta. |
| Inżynieria | 540 | Kamieniarstwo, Pismo | Katapulty, warsztaty i +2 do zasięgu dostaw. |
| Instytucje klasyczne | 352 | Szlaki handlowe, Filozofia, Moneta, Inżynieria | Zamyka dorobek epoki i otwiera badania: Średniowiecze. Premia w każdym mieście: nauka +2 · kultura +2. |

| Nowa infrastruktura | Produkcja | Technologia | Działanie |
|---|---:|---|---|
| Akademia filozofów | 180 | Filozofia | nauka +12 · kultura +4 |
| Forum obywatelskie | 190 | Instytucje klasyczne | złoto +4 · kultura +12 |
| Plac inżynierów | 180 | Inżynieria | produkcja +12 · Mieszkania +4 |

| Model wojsk | Siła bazowa | Ruch | Zasięg | Produkcja | Technologia |
|---|---:|---:|---:|---:|---|
| Legion zawodowy | 47 | 2 | 1 | 180 | Instytucje klasyczne |
| Tabor oblężniczy | 34 | 2 | 2 | 200 | Inżynieria |

**Rada: Kto jest obywatelem?**

Miasta potrzebują wspólnego prawa. Akademie chcą głosu w radzie, kupcy jasnych umów, a możni utrzymania kontroli nad warsztatami.

- Rozszerzymy obywatelstwo i szkoły. Złoto -100 · Rody -5 · Uczeni +6 Wiedza kosztem wpływów rodowych. Trwale: nauka % +12
- Zagwarantujemy swobodę umów. Lud -3 Kupcy zyskują pewność prawa. Trwale: złoto % +12
- Rozwój oprzemy na warsztatach miejskich. Rody +3 Państwo skoordynuje pracę rzemieślników. Trwale: produkcja % +10

## 06. Średniowiecze

Wspólnoty, domeny i cechy. Młyny, uniwersytety i umowy lenne tworzą odmienne drogi do zamożności i władzy.

| Technologia | Nauka | Wymagania wewnętrzne | Efekt |
|---|---:|---|---|
| Służba publiczna | 660 | Filozofia, Szlaki handlowe | Sądy; -25% kosztów utrzymania miast, +2 miejsca mieszkalne i cel korupcji -8 w każdym mieście. |
| Uniwersytety | 1560 | Filozofia, Inżynieria | Uniwersytety; +20% nauki. |
| Trójpolówka | 468 | Służba publiczna | Młyny, uniwersytety i umowy lenne tworzą odmienne drogi do zamożności i władzy. Premia w każdym mieście: nauka +2. |
| Umowy lenne | 522 | Służba publiczna | Młyny, uniwersytety i umowy lenne tworzą odmienne drogi do zamożności i władzy. Premia w każdym mieście: złoto +2 · kultura +2. |
| Ład średniowieczny | 576 | Służba publiczna, Uniwersytety, Trójpolówka, Umowy lenne | Zamyka dorobek epoki i otwiera badania: Renesans. Premia w każdym mieście: nauka +2 · kultura +2. |

| Nowa infrastruktura | Produkcja | Technologia | Działanie |
|---|---:|---|---|
| Młyn wodny | 230 | Trójpolówka | żywność +10 · produkcja +7 |
| Dom cechowy | 240 | Umowy lenne | produkcja +7 · złoto +18 |
| Szpital wspólnoty | 260 | Ład średniowieczny | nauka +5 · Mieszkania +5 · Opieka +9 |

| Model wojsk | Siła bazowa | Ruch | Zasięg | Produkcja | Technologia |
|---|---:|---:|---:|---:|---|
| Zbrojni | 58 | 2 | 1 | 250 | Umowy lenne |
| Rycerstwo | 65 | 3 | 1 | 300 | Ład średniowieczny |

**Rada: Przysięga, cech czy wspólnota?**

Młyny i uczelnie zmieniają prowincje. Umowy lenne, cechy i wolne miasta proponują różne odpowiedzi na pytanie o podział pracy.

- Wesprzemy wolne miasta i uniwersytety. Złoto -150 · Rody -5 · Uczeni +7 Wykształcenie poza kontrolą domen. Trwale: nauka % +14
- Cechy otrzymają kartę pracy. Lud +4 Większa sprawność i większy wpływ zrzeszeń. Trwale: produkcja % +12
- Domeny utrzymają kanały i młyny. Lud -4 · Rody +6 Rolnictwo pozostaje związane z rodami. Trwale: żywność % +12

## 07. Renesans

Wczesna nowożytność. Druk, nawigacja i broń prochowa rozszerzają horyzonty oraz skutki rywalizacji.

| Technologia | Nauka | Wymagania wewnętrzne | Efekt |
|---|---:|---|---|
| Stal | 1740 | Żelazo, Inżynieria | +7 siły podstawowych oddziałów. Otwiera nowe projekty i modele epoki renesansu. |
| Druk | 1920 | Uniwersytety | +30% kultury; narodowa biblioteka. |
| Nawigacja | 1860 | Moneta, Inżynieria | Szlaki zamorskie: +8 złota za każdy traktat handlowy. |
| Proch i balistyka | 870 | Stal | Druk, nawigacja i broń prochowa rozszerzają horyzonty oraz skutki rywalizacji. Premia w każdym mieście: złoto +3 · kultura +3. |
| Metody renesansu | 960 | Stal, Druk, Nawigacja, Proch i balistyka | Zamyka dorobek epoki i otwiera badania: Oświecenie. Premia w każdym mieście: nauka +3 · kultura +3. |

| Nowa infrastruktura | Produkcja | Technologia | Działanie |
|---|---:|---|---|
| Oficyna drukarska | 310 | Druk | nauka +8 · kultura +20 |
| Arsenał prochowy | 340 | Proch i balistyka | produkcja +20 · Obrona +12 |
| Giełda kupiecka | 320 | Nawigacja | złoto +30 |

| Model wojsk | Siła bazowa | Ruch | Zasięg | Produkcja | Technologia |
|---|---:|---:|---:|---:|---|
| Muszkieterzy | 75 | 2 | 1 | 380 | Proch i balistyka |
| Artyleria prochowa | 62 | 2 | 2 | 420 | Proch i balistyka |

**Rada: Komu wolno drukować?**

Drukarze powielają książki, a arsenały broń. Możemy otworzyć obieg wiedzy, podporządkować go wojsku albo związać z wymianą kupiecką.

- Wiedza będzie krążyć bez przywileju. Złoto -220 · Rody -6 · Uczeni +8 Otwarte oficyny, mniej kontroli rodów. Trwale: nauka % +15
- Pierwszeństwo dostaną arsenały. Lud -4 · Rody +5 Skupienie zasobów na sile państwa. Trwale: produkcja % +14 · zasięg dostaw +1
- Zawrzemy kartę jarmarków i wydawców. Uczeni +3 Handel sfinansuje nowe przedsięwzięcia. Trwale: złoto % +14

## 08. Oświecenie

Nauka, prawa i reformy. Medycyna, doświadczenie i konstytucja każą pytać, komu służy postęp.

| Technologia | Nauka | Wymagania wewnętrzne | Efekt |
|---|---:|---|---|
| Medycyna | 2280 | Uniwersytety, Irygacja | +25% żywności, szybsze leczenie i +3 do pojemności opieki w każdym mieście. |
| Konstytucja | 4600 | Służba publiczna, Druk | +10 do docelowej stabilności; parlament i cel korupcji -6. |
| Metoda naukowa | 1170 | Medycyna | Medycyna, doświadczenie i konstytucja każą pytać, komu służy postęp. Premia w każdym mieście: nauka +3. |
| Prawa obywatelskie | 1305 | Medycyna | Medycyna, doświadczenie i konstytucja każą pytać, komu służy postęp. Premia w każdym mieście: złoto +3 · kultura +3. |
| Rozum publiczny | 1440 | Medycyna, Konstytucja, Metoda naukowa, Prawa obywatelskie | Zamyka dorobek epoki i otwiera badania: Rewolucja przemysłowa. Premia w każdym mieście: nauka +3 · kultura +3. |

| Nowa infrastruktura | Produkcja | Technologia | Działanie |
|---|---:|---|---|
| Liceum publiczne | 400 | Metoda naukowa | nauka +30 · kultura +15 |
| Dzielnica obywatelska | 420 | Prawa obywatelskie | złoto +10 · Mieszkania +12 |
| Kolegium medyczne | 440 | Medycyna | nauka +20 · Opieka +12 |

| Model wojsk | Siła bazowa | Ruch | Zasięg | Produkcja | Technologia |
|---|---:|---:|---:|---:|---|
| Piechota liniowa | 88 | 2 | 1 | 500 | Prawa obywatelskie |
| Artyleria polowa | 78 | 2 | 2 | 550 | Metoda naukowa |

**Rada: Prawo do postępu**

Medycy i obywatele chcą udziału w korzyściach nowych badań. Skarb pyta, kto poniesie koszty reform.

- Sfinansujemy naukę publiczną. Złoto -300 · Uczeni +8 Długofalowy rozwój kosztem bieżącego skarbu. Trwale: nauka % +16
- Otwarte warsztaty i wolne patenty. Rody -4 Wynalazki szybciej trafią do produkcji. Trwale: produkcja % +12 · złoto % +4
- Najpierw medycyna i bezpieczeństwo żywności. Lud +7 Postęp ma chronić ludność. Trwale: żywność % +12

## 09. Rewolucja przemysłowa

Para, kolej i masowa produkcja. Energia napędza fabryki; emisje obciążają wspólny klimat. Wzrost ma społeczną cenę.

| Technologia | Nauka | Wymagania wewnętrzne | Efekt |
|---|---:|---|---|
| Przemysł | 2700 | Stal, Moneta | Fabryki i +25% produkcji. |
| Maszyna parowa | 1724 | Przemysł | Energia napędza fabryki; emisje obciążają wspólny klimat. Wzrost ma społeczną cenę. Premia w każdym mieście: produkcja +3. |
| Kolej | 1950 | Przemysł | Energia napędza fabryki; emisje obciążają wspólny klimat. Wzrost ma społeczną cenę. Premia w każdym mieście: nauka +3. |
| Chemia przemysłowa | 2175 | Przemysł | Energia napędza fabryki; emisje obciążają wspólny klimat. Wzrost ma społeczną cenę. Premia w każdym mieście: złoto +3 · kultura +3. |
| Produkcja masowa | 2400 | Przemysł, Maszyna parowa, Kolej, Chemia przemysłowa | Zamyka dorobek epoki i otwiera badania: Nowoczesność. Premia w każdym mieście: nauka +3 · kultura +3. |

| Nowa infrastruktura | Produkcja | Technologia | Działanie |
|---|---:|---|---|
| Elektrownia cieplna | 520 | Maszyna parowa | Energia +24 · Emisje +6 · Utrzymanie złota/turę +4 |
| Kombinat stalowy | 560 | Przemysł | produkcja +50 · Popyt energii +12 · Emisje +4 · Utrzymanie złota/turę +3 · Produkcja, wiedza i dochód tego obiektu zależą od miejskiego pokrycia energii: 25–100%. |
| Terminal kolejowy | 580 | Kolej | złoto +40 · Popyt energii +4 · Zasięg dostaw +3 · Produkcja, wiedza i dochód tego obiektu zależą od miejskiego pokrycia energii: 25–100%. |

| Model wojsk | Siła bazowa | Ruch | Zasięg | Produkcja | Technologia |
|---|---:|---:|---:|---:|---|
| Strzelcy | 108 | 2 | 1 | 680 | Produkcja masowa |
| Haubice | 96 | 2 | 3 | 750 | Chemia przemysłowa |

**Rada: Dym nad doliną**

Przemysł potrzebuje energii. Nieograniczone spalanie przyspieszy produkcję, ale emisje wszystkich państw obciążą wspólne plony.

- Przyjmiemy normy i inspekcje. Złoto -450 · Lud +6 Redukcja emisji za koszt inwestycji i wolniejszy obrót. Trwale: złoto % -4 · emisje/miasto -4
- Produkcja ma pierwszeństwo. Lud -5 · Rody +6 Szybsza praca i dodatkowe emisje w każdym mieście. Trwale: produkcja % +20 · emisje/miasto +5
- Zbudujemy sieć usług komunalnych. Złoto -150 Publiczna energia wspiera mieszkańców. Trwale: żywność % +6 · energia/miasto +12

## 10. Nowoczesność

Elektryfikacja i społeczeństwo masowe. Sieci energetyczne, silniki i radio zwiększają możliwości państwa oraz skalę wojny.

| Technologia | Nauka | Wymagania wewnętrzne | Efekt |
|---|---:|---|---|
| Elektryczność | 5600 | Przemysł, Uniwersytety | +30% nauki i produkcji. |
| Silnik spalinowy | 2645 | Elektryczność | Sieci energetyczne, silniki i radio zwiększają możliwości państwa oraz skalę wojny. Premia w każdym mieście: produkcja +4. |
| Radio | 2990 | Elektryczność | Sieci energetyczne, silniki i radio zwiększają możliwości państwa oraz skalę wojny. Premia w każdym mieście: nauka +4. |
| Lotnictwo i aeronautyka | 3335 | Elektryczność | Sieci energetyczne, silniki i radio zwiększają możliwości państwa oraz skalę wojny. Premia w każdym mieście: złoto +4 · kultura +4. |
| Społeczeństwo masowe | 3680 | Elektryczność, Silnik spalinowy, Radio, Lotnictwo i aeronautyka | Zamyka dorobek epoki i otwiera badania: Epoka atomowa. Premia w każdym mieście: nauka +4 · kultura +4. |

| Nowa infrastruktura | Produkcja | Technologia | Działanie |
|---|---:|---|---|
| Sieć elektroenergetyczna | 650 | Elektryczność | Energia +40 · Emisje +3 · Utrzymanie złota/turę +5 |
| Zakłady silnikowe | 700 | Silnik spalinowy | produkcja +75 · Popyt energii +22 · Emisje +3 · Produkcja, wiedza i dochód tego obiektu zależą od miejskiego pokrycia energii: 25–100%. |
| Szpital powszechny | 700 | Społeczeństwo masowe | Popyt energii +6 · Mieszkania +8 · Opieka +18 · Produkcja, wiedza i dochód tego obiektu zależą od miejskiego pokrycia energii: 25–100%. |
| Elektrownia wodna | 900 | Elektryczność | Energia +45 · Utrzymanie złota/turę +3 · Wymaga własnej rzeki przy mieście. Stała moc, bez emisji. |

| Model wojsk | Siła bazowa | Ruch | Zasięg | Produkcja | Technologia |
|---|---:|---:|---:|---:|---|
| Piechota zmotoryzowana | 130 | 3 | 1 | 900 | Silnik spalinowy |
| Batalion pancerny | 150 | 4 | 1 | 1100 | Społeczeństwo masowe |

**Rada: Sieć dla wszystkich?**

Prąd i transport mogą połączyć społeczeństwo albo utrwalić zależność od kilku dostawców. Nowe fabryki bez energii tracą wydajność.

- Energia będzie usługą powszechną. Złoto -600 · Lud +8 Większe pokrycie potrzeb kosztem dochodów. Trwale: złoto % -3 · energia/miasto +25
- Koncesje sfinansują rozbudowę. Lud -5 · Rody +7 Większe wpływy i emisje. Trwale: złoto % +18 · emisje/miasto +3
- Przyjmiemy standardy oszczędności. Uczeni +5 Wydajniejsza gospodarka i mniejszy ślad. Trwale: produkcja % +8 · emisje/miasto -3

## 11. Epoka atomowa

Atom, elektronika i pierwsze rakiety. Reaktory i medycyna ratują życie, a technologie strategiczne stawiają nowe pytania o bezpieczeństwo.

| Technologia | Nauka | Wymagania wewnętrzne | Efekt |
|---|---:|---|---|
| Fizyka jądrowa | 3500 | Społeczeństwo masowe | Reaktory i medycyna ratują życie, a technologie strategiczne stawiają nowe pytania o bezpieczeństwo. Premia w każdym mieście: żywność +4. |
| Technika rakietowa | 4024 | Fizyka jądrowa | Reaktory i medycyna ratują życie, a technologie strategiczne stawiają nowe pytania o bezpieczeństwo. Premia w każdym mieście: produkcja +4. |
| Antybiotyki | 4550 | Fizyka jądrowa | Reaktory i medycyna ratują życie, a technologie strategiczne stawiają nowe pytania o bezpieczeństwo. Premia w każdym mieście: nauka +4. |
| Półprzewodniki | 5075 | Fizyka jądrowa | Reaktory i medycyna ratują życie, a technologie strategiczne stawiają nowe pytania o bezpieczeństwo. Premia w każdym mieście: złoto +4 · kultura +4. |
| Systemy epoki atomowej | 5600 | Fizyka jądrowa, Technika rakietowa, Antybiotyki, Półprzewodniki | Zamyka dorobek epoki i otwiera badania: Epoka informacyjna. Premia w każdym mieście: nauka +4 · kultura +4. |

| Nowa infrastruktura | Produkcja | Technologia | Działanie |
|---|---:|---|---|
| Elektrownia jądrowa | 900 | Fizyka jądrowa | Energia +85 · Emisje +1 · Utrzymanie złota/turę +10 |
| Ośrodek rakietowy | 950 | Technika rakietowa | produkcja +20 · nauka +65 · Popyt energii +20 · Produkcja, wiedza i dochód tego obiektu zależą od miejskiego pokrycia energii: 25–100%. |
| Rozgłośnia publiczna | 850 | Systemy epoki atomowej | kultura +65 · Popyt energii +10 · Produkcja, wiedza i dochód tego obiektu zależą od miejskiego pokrycia energii: 25–100%. |

| Model wojsk | Siła bazowa | Ruch | Zasięg | Produkcja | Technologia |
|---|---:|---:|---:|---:|---|
| Piechota zmechanizowana | 175 | 3 | 1 | 1400 | Systemy epoki atomowej |
| Artyleria rakietowa | 150 | 3 | 3 | 1600 | Technika rakietowa |

**Rada: Atom pod czyją kontrolą?**

Energia jądrowa i elektronika mogą wspierać dobrobyt. Zaufanie zależy od jawności i kontroli strategicznych instytucji.

- Jawny nadzór cywilny i badania. Złoto -800 · Uczeni +8 Nauka i niskoemisyjna energia. Trwale: nauka % +10 · energia/miasto +30
- Państwo skupi zasoby strategiczne. Lud -7 · Rody +6 Sprawniejsza produkcja i dostawy, mniejszy udział obywateli. Trwale: produkcja % +18 · zasięg dostaw +2
- Najpierw zdrowie i edukacja. Lud +8 Wyniki badań trafią do mieszkańców. Trwale: żywność % +10 · nauka % +8

## 12. Epoka informacyjna

Sieci cyfrowe i wiedza globalna. Komputery, ekologia i łączność pozwalają koordynować państwo. Prywatność staje się dobrem wspólnym.

| Technologia | Nauka | Wymagania wewnętrzne | Efekt |
|---|---:|---|---|
| Ekologia | 6200 | Medycyna, Przemysł | Przywraca równowagę: +10 stabilności, +15% żywności. |
| Informatyka | 5750 | Ekologia | Komputery, ekologia i łączność pozwalają koordynować państwo. Prywatność staje się dobrem wspólnym. Premia w każdym mieście: produkcja +4. |
| Internet | 6500 | Ekologia | Komputery, ekologia i łączność pozwalają koordynować państwo. Prywatność staje się dobrem wspólnym. Premia w każdym mieście: nauka +4. |
| Wielkie obserwatorium | 8000 | Elektryczność, Nawigacja, Druk | Odblokowuje projekt zwycięstwa naukowego: Atlas świata. |
| Społeczeństwo informacyjne | 8000 | Ekologia, Informatyka, Internet, Wielkie obserwatorium | Zamyka dorobek epoki i otwiera badania: Epoka automatyzacji. Premia w każdym mieście: nauka +4 · kultura +4. |

| Nowa infrastruktura | Produkcja | Technologia | Działanie |
|---|---:|---|---|
| Centrum danych | 1200 | Informatyka | nauka +110 · Popyt energii +25 · Utrzymanie złota/turę +6 · Produkcja, wiedza i dochód tego obiektu zależą od miejskiego pokrycia energii: 25–100%. |
| Fabryka półprzewodników | 1300 | Internet | produkcja +100 · złoto +25 · Popyt energii +28 · Produkcja, wiedza i dochód tego obiektu zależą od miejskiego pokrycia energii: 25–100%. |
| Dzielnica recyklingu | 1100 | Ekologia | żywność +20 · złoto +25 · Popyt energii +8 · Emisje -5 · Produkcja, wiedza i dochód tego obiektu zależą od miejskiego pokrycia energii: 25–100%. |
| Farma wiatrowa | 1250 | Ekologia | Energia +35 · Utrzymanie złota/turę +2 · Moc: latem ×0,8, zimą ×1,2; własne wzgórze przy mieście daje ×1,2. Pozostałe sezony ×1. |
| Farma słoneczna | 1300 | Ekologia | Energia +42 · Utrzymanie złota/turę +2 · Moc zimą ×0,65; pustynne pole miasta daje ×1,25. Pozostałe sezony ×1. |

| Model wojsk | Siła bazowa | Ruch | Zasięg | Produkcja | Technologia |
|---|---:|---:|---:|---:|---|
| Piechota sieciocentryczna | 205 | 3 | 1 | 1900 | Internet |
| Drony rozpoznawcze | 100 | 4 | 1 | 1500 | Informatyka |

**Rada: Kto posiada dane?**

Sieci zbierają informacje o obywatelach. Możemy chronić prywatność, powierzyć dane rynkowi albo zwiększyć kontrolę państwa.

- Dane obywateli podlegają ochronie. Złoto -1000 · Lud +8 · Uczeni +5 Badania oparte na zgodzie i zaufaniu. Trwale: nauka % +18
- Dane sfinansują rozwój usług. Lud -8 · Rody +6 Zyski platform kosztem poparcia. Trwale: złoto % +22
- Ujednolicimy systemy państwowe. Uczeni -4 Sprawniejsza praca i energetyka. Trwale: produkcja % +12 · energia/miasto +20

## 13. Epoka automatyzacji

Możliwa bliska przyszłość. Uczenie maszynowe, roboty i genomika mogą zmienić pracę. Ich korzyści można dzielić na różne sposoby.

| Technologia | Nauka | Wymagania wewnętrzne | Efekt |
|---|---:|---|---|
| Uczenie maszynowe | 7200 | Społeczeństwo informacyjne | Uczenie maszynowe, roboty i genomika mogą zmienić pracę. Ich korzyści można dzielić na różne sposoby. Premia w każdym mieście: żywność +5. |
| Robotyka | 8280 | Uczenie maszynowe | Uczenie maszynowe, roboty i genomika mogą zmienić pracę. Ich korzyści można dzielić na różne sposoby. Premia w każdym mieście: produkcja +5. |
| Genomika | 9360 | Uczenie maszynowe | Uczenie maszynowe, roboty i genomika mogą zmienić pracę. Ich korzyści można dzielić na różne sposoby. Premia w każdym mieście: nauka +5. |
| Inteligentne materiały | 10440 | Uczenie maszynowe | Uczenie maszynowe, roboty i genomika mogą zmienić pracę. Ich korzyści można dzielić na różne sposoby. Premia w każdym mieście: złoto +5 · kultura +5. |
| Karta automatyzacji | 11520 | Uczenie maszynowe, Robotyka, Genomika, Inteligentne materiały | Zamyka dorobek epoki i otwiera badania: Epoka planetarna. Premia w każdym mieście: nauka +5 · kultura +5. |

| Nowa infrastruktura | Produkcja | Technologia | Działanie |
|---|---:|---|---|
| Inteligentna sieć odnawialna | 1600 | Inteligentne materiały | Energia +110 · Emisje -4 · Utrzymanie złota/turę +8 |
| Fabryka robotyczna | 1700 | Robotyka | produkcja +170 · Popyt energii +45 · Produkcja, wiedza i dochód tego obiektu zależą od miejskiego pokrycia energii: 25–100%. |
| Klinika genomowa | 1700 | Genomika | żywność +45 · Popyt energii +20 · Opieka +25 · Produkcja, wiedza i dochód tego obiektu zależą od miejskiego pokrycia energii: 25–100%. |
| Elektrownia geotermalna | 1800 | Inteligentne materiały | Energia +100 · Utrzymanie złota/turę +8 · Dwa własne pola wzgórz lub gór przy mieście. Stała moc, bez emisji. |

| Model wojsk | Siła bazowa | Ruch | Zasięg | Produkcja | Technologia |
|---|---:|---:|---:|---:|---|
| Gwardia robotyczna | 240 | 3 | 1 | 2400 | Robotyka |
| Artyleria autonomiczna | 220 | 3 | 3 | 2700 | Uczenie maszynowe |

**Rada: Praca po automatyzacji**

Roboty wykonują coraz więcej zadań. To od nas zależy, czy zysk zamieni się w czas dla ludzi, koncentrację dochodów czy ścisłą kontrolę pracy.

- Podzielimy korzyści i sfinansujemy przekwalifikowanie. Złoto -1400 · Lud +12 · Rody -6 Wiedza i dostęp do żywności kosztem skarbu. Trwale: żywność % +12 · nauka % +15
- Przyspieszymy automatyzację zakładów. Lud -10 · Rody +8 Silny wzrost pracy, większe zapotrzebowanie na infrastrukturę. Trwale: produkcja % +25 · emisje/miasto +3
- Roboty będą infrastrukturą wspólnot. Lud +5 Mniejszy, szerzej dzielony wzrost. Trwale: produkcja % +12 · nauka % +8

## 14. Epoka planetarna

Możliwa przyszłość · wspólna planeta. Synteza jądrowa i obiegi zamknięte otwierają drogę odbudowie klimatu lub dalszej koncentracji przemysłu.

| Technologia | Nauka | Wymagania wewnętrzne | Efekt |
|---|---:|---|---|
| Energetyka fuzyjna | 10000 | Karta automatyzacji | Synteza jądrowa i obiegi zamknięte otwierają drogę odbudowie klimatu lub dalszej koncentracji przemysłu. Premia w każdym mieście: żywność +5. |
| Obiegi zamknięte | 11500 | Energetyka fuzyjna | Synteza jądrowa i obiegi zamknięte otwierają drogę odbudowie klimatu lub dalszej koncentracji przemysłu. Premia w każdym mieście: produkcja +5. |
| Inżynieria klimatu | 13000 | Energetyka fuzyjna | Synteza jądrowa i obiegi zamknięte otwierają drogę odbudowie klimatu lub dalszej koncentracji przemysłu. Premia w każdym mieście: nauka +5. |
| Sieć planetarna | 14500 | Energetyka fuzyjna | Synteza jądrowa i obiegi zamknięte otwierają drogę odbudowie klimatu lub dalszej koncentracji przemysłu. Premia w każdym mieście: złoto +5 · kultura +5. |
| Współpraca planetarna | 16000 | Energetyka fuzyjna, Obiegi zamknięte, Inżynieria klimatu, Sieć planetarna | Zamyka dorobek epoki i otwiera badania: Epoka orbitalna. Premia w każdym mieście: nauka +5 · kultura +5. |

| Nowa infrastruktura | Produkcja | Technologia | Działanie |
|---|---:|---|---|
| Elektrownia fuzyjna | 2300 | Energetyka fuzyjna | Energia +240 · Emisje -8 · Utrzymanie złota/turę +18 |
| Ośrodek odbudowy klimatu | 2400 | Inżynieria klimatu | kultura +35 · Popyt energii +45 · Emisje -25 · Produkcja, wiedza i dochód tego obiektu zależą od miejskiego pokrycia energii: 25–100%. |
| Instytut planetarny | 2200 | Sieć planetarna | nauka +170 · Popyt energii +35 · Mieszkania +15 · Produkcja, wiedza i dochód tego obiektu zależą od miejskiego pokrycia energii: 25–100%. |

| Model wojsk | Siła bazowa | Ruch | Zasięg | Produkcja | Technologia |
|---|---:|---:|---:|---:|---|
| Gwardia planetarna | 280 | 3 | 1 | 3300 | Współpraca planetarna |
| Bateria elektromagnetyczna | 260 | 3 | 3 | 3600 | Energetyka fuzyjna |

**Rada: Jaką planetę pozostawimy?**

Dysponujemy narzędziami do odbudowy klimatu. Możemy ograniczać szkody, zwiększać produkcję lub skupić się na wspólnej sieci energetycznej.

- Przyjmiemy program odbudowy biosfery. Złoto -2000 · Lud +10 Stała redukcja emisji w każdym mieście. Trwale: żywność % +10 · emisje/miasto -15
- Energia zasili kolejną falę przemysłu. Lud -7 Wysoka produkcja i dalsze obciążenie klimatu. Trwale: produkcja % +25 · emisje/miasto +8
- Energia i wiedza połączą państwo. Uczeni +8 Wzmocnimy lokalne sieci bez nowego dymu. Trwale: nauka % +12 · energia/miasto +60

## 15. Epoka orbitalna

Możliwa przyszłość · infrastruktura orbitalna. Wielokrotne starty, medycyna kosmiczna i montaż orbitalny pozwalają utrzymywać pierwsze stałe habitaty.

| Technologia | Nauka | Wymagania wewnętrzne | Efekt |
|---|---:|---|---|
| Wielokrotne systemy startowe | 14000 | Współpraca planetarna | Wielokrotne starty, medycyna kosmiczna i montaż orbitalny pozwalają utrzymywać pierwsze stałe habitaty. Premia w każdym mieście: żywność +5. |
| Montaż orbitalny | 16099 | Wielokrotne systemy startowe | Wielokrotne starty, medycyna kosmiczna i montaż orbitalny pozwalają utrzymywać pierwsze stałe habitaty. Premia w każdym mieście: produkcja +5. |
| Habitaty kosmiczne | 18200 | Wielokrotne systemy startowe | Wielokrotne starty, medycyna kosmiczna i montaż orbitalny pozwalają utrzymywać pierwsze stałe habitaty. Premia w każdym mieście: nauka +5. |
| Medycyna kosmiczna | 20300 | Wielokrotne systemy startowe | Wielokrotne starty, medycyna kosmiczna i montaż orbitalny pozwalają utrzymywać pierwsze stałe habitaty. Premia w każdym mieście: złoto +5 · kultura +5. |
| Przemysł orbitalny | 22400 | Wielokrotne systemy startowe, Montaż orbitalny, Habitaty kosmiczne, Medycyna kosmiczna | Zamyka dorobek epoki i otwiera badania: Epoka międzyplanetarna. Premia w każdym mieście: nauka +5 · kultura +5. |

| Nowa infrastruktura | Produkcja | Technologia | Działanie |
|---|---:|---|---|
| Stocznia orbitalna | 3000 | Montaż orbitalny | produkcja +240 · Popyt energii +65 · Produkcja, wiedza i dochód tego obiektu zależą od miejskiego pokrycia energii: 25–100%. |
| Administracja habitatów orbitalnych | 3100 | Habitaty kosmiczne | złoto +110 · Popyt energii +50 · Mieszkania +35 · Produkcja, wiedza i dochód tego obiektu zależą od miejskiego pokrycia energii: 25–100%. |
| Laboratorium mikrograwitacji | 3200 | Medycyna kosmiczna | nauka +260 · Popyt energii +55 · Produkcja, wiedza i dochód tego obiektu zależą od miejskiego pokrycia energii: 25–100%. |

| Model wojsk | Siła bazowa | Ruch | Zasięg | Produkcja | Technologia |
|---|---:|---:|---:|---:|---|
| Piechota orbitalna | 330 | 3 | 1 | 4200 | Medycyna kosmiczna |
| Łaziki wsparcia orbitalnego | 360 | 4 | 1 | 4700 | Montaż orbitalny |

**Rada: Orbita: dobro wspólne czy koncesja?**

Pierwsze habitaty będą zależeć od infrastruktury na planecie. Zasady dostępu zadecydują, kto skorzysta z nowej gospodarki.

- Przyjmiemy otwartą kartę badań orbitalnych. Złoto -2500 · Uczeni +10 Wiedza z orbity wróci do społeczeństwa. Trwale: nauka % +22
- Koncesjonariusze sfinansują habitaty. Lud -8 · Rody +10 Duże wpływy i silniejszy wpływ właścicieli. Trwale: złoto % +25
- Stacje będą siecią publiczną. Lud +6 Sprawniejsze dostawy i większa dostępność energii. Trwale: energia/miasto +70 · zasięg dostaw +2

## 16. Epoka międzyplanetarna

Możliwa przyszłość · Księżyc i Mars. Samowystarczalne placówki wymagają rolnictwa, napędu i prawa do decydowania o życiu daleko od ojczyzny.

| Technologia | Nauka | Wymagania wewnętrzne | Efekt |
|---|---:|---|---|
| Zasoby księżycowe | 19000 | Przemysł orbitalny | Samowystarczalne placówki wymagają rolnictwa, napędu i prawa do decydowania o życiu daleko od ojczyzny. Premia w każdym mieście: żywność +6. |
| Habitaty marsjańskie | 21850 | Zasoby księżycowe | Samowystarczalne placówki wymagają rolnictwa, napędu i prawa do decydowania o życiu daleko od ojczyzny. Premia w każdym mieście: produkcja +6. |
| Rolnictwo pozaziemskie | 24700 | Zasoby księżycowe | Samowystarczalne placówki wymagają rolnictwa, napędu i prawa do decydowania o życiu daleko od ojczyzny. Premia w każdym mieście: nauka +6. |
| Napęd fuzyjny | 27550 | Zasoby księżycowe | Samowystarczalne placówki wymagają rolnictwa, napędu i prawa do decydowania o życiu daleko od ojczyzny. Premia w każdym mieście: złoto +6 · kultura +6. |
| Logistyka międzyplanetarna | 30400 | Zasoby księżycowe, Habitaty marsjańskie, Rolnictwo pozaziemskie, Napęd fuzyjny | Zamyka dorobek epoki i otwiera badania: Epoka Układu Słonecznego. Premia w każdym mieście: nauka +6 · kultura +6. |

| Nowa infrastruktura | Produkcja | Technologia | Działanie |
|---|---:|---|---|
| Terminal księżycowy | 4000 | Zasoby księżycowe | produkcja +300 · Popyt energii +85 · Produkcja, wiedza i dochód tego obiektu zależą od miejskiego pokrycia energii: 25–100%. |
| Zespół farm pozaziemskich | 4100 | Rolnictwo pozaziemskie | żywność +110 · Popyt energii +75 · Opieka +30 · Produkcja, wiedza i dochód tego obiektu zależą od miejskiego pokrycia energii: 25–100%. |
| Instytut badań marsjańskich | 4200 | Habitaty marsjańskie | nauka +360 · Popyt energii +90 · Produkcja, wiedza i dochód tego obiektu zależą od miejskiego pokrycia energii: 25–100%. |

| Model wojsk | Siła bazowa | Ruch | Zasięg | Produkcja | Technologia |
|---|---:|---:|---:|---:|---|
| Korpus międzyplanetarny | 390 | 3 | 1 | 5400 | Habitaty marsjańskie |
| Wozy napędu fuzyjnego | 430 | 4 | 1 | 6000 | Napęd fuzyjny |

**Rada: Prawo ludzi poza planetą**

Ludzie przygotowujący placówki Księżyca i Marsa proszą o reprezentację. Odległość sprawia, że każda decyzja centrum ma inny ciężar niż na własnym kontynencie.

- Osady otrzymają samorząd i udział w badaniach. Złoto -3000 · Rody -6 · Uczeni +10 Wiedza kosztem wpływów centralnych. Trwale: nauka % +25
- Utrzymamy centralny plan dostaw. Lud -8 Produkcja i szlaki kosztem autonomii. Trwale: produkcja % +20 · zasięg dostaw +2
- Zagwarantujemy wspólną rezerwę życia. Lud +8 Żywność i energia dla wszystkich osad. Trwale: żywność % +18 · energia/miasto +90

## 17. Epoka Układu Słonecznego

Odległa możliwa przyszłość. Asteroidy i dalekie przekaźniki poszerzają gospodarkę. Ograniczone zasoby, energia i autonomia nadal mają znaczenie.

| Technologia | Nauka | Wymagania wewnętrzne | Efekt |
|---|---:|---|---|
| Górnictwo asteroid | 26000 | Logistyka międzyplanetarna | Asteroidy i dalekie przekaźniki poszerzają gospodarkę. Ograniczone zasoby, energia i autonomia nadal mają znaczenie. Premia w każdym mieście: żywność +6. |
| Eksploracja zewnętrznego układu | 29899 | Górnictwo asteroid | Asteroidy i dalekie przekaźniki poszerzają gospodarkę. Ograniczone zasoby, energia i autonomia nadal mają znaczenie. Premia w każdym mieście: produkcja +6. |
| Syntetyczne ekosystemy | 33800 | Górnictwo asteroid | Asteroidy i dalekie przekaźniki poszerzają gospodarkę. Ograniczone zasoby, energia i autonomia nadal mają znaczenie. Premia w każdym mieście: nauka +6. |
| Przekaźniki dalekiego kosmosu | 37700 | Górnictwo asteroid | Asteroidy i dalekie przekaźniki poszerzają gospodarkę. Ograniczone zasoby, energia i autonomia nadal mają znaczenie. Premia w każdym mieście: złoto +6 · kultura +6. |
| Wspólnota Układu | 41600 | Górnictwo asteroid, Eksploracja zewnętrznego układu, Syntetyczne ekosystemy, Przekaźniki dalekiego kosmosu | Zamyka dorobek epoki i otwiera badania: Epoka międzygwiezdna. Premia w każdym mieście: nauka +6 · kultura +6. |

| Nowa infrastruktura | Produkcja | Technologia | Działanie |
|---|---:|---|---|
| Odlewnia asteroid | 5200 | Górnictwo asteroid | produkcja +450 · Popyt energii +140 · Produkcja, wiedza i dochód tego obiektu zależą od miejskiego pokrycia energii: 25–100%. |
| Sieć dalekiego kosmosu | 5300 | Przekaźniki dalekiego kosmosu | nauka +500 · Popyt energii +120 · Produkcja, wiedza i dochód tego obiektu zależą od miejskiego pokrycia energii: 25–100%. |
| Habitat biosferyczny | 5400 | Syntetyczne ekosystemy | żywność +90 · Popyt energii +110 · Mieszkania +70 · Opieka +40 · Produkcja, wiedza i dochód tego obiektu zależą od miejskiego pokrycia energii: 25–100%. |

| Model wojsk | Siła bazowa | Ruch | Zasięg | Produkcja | Technologia |
|---|---:|---:|---:|---:|---|
| Straż habitatów Układu | 470 | 3 | 1 | 7000 | Syntetyczne ekosystemy |
| Bateria dalekiego zasięgu | 430 | 3 | 4 | 7500 | Przekaźniki dalekiego kosmosu |

**Rada: Kto dziedziczy zasoby Układu?**

Górnictwo asteroid tworzy nowe bogactwo. Nie musimy powtarzać dawnych reguł koncentracji własności.

- Zasoby sfinansują wspólną naukę. Złoto -4000 · Uczeni +12 Otwarte laboratoria i niższy ślad środowiskowy. Trwale: nauka % +28 · emisje/miasto -8
- Przyznamy długie koncesje wydobywcze. Lud -10 · Rody +12 Szybki wzrost przemysłu i wpływów właścicieli. Trwale: produkcja % +28 · złoto % +12
- Powstanie federacja samowystarczalnych habitatów. Lud +8 · Rody -5 Mniejsza zależność od centrum. Trwale: żywność % +18 · energia/miasto +110

## 18. Epoka międzygwiezdna

Spekulatywna daleka przyszłość. Sondy i statki pokoleniowe są przedsięwzięciami wielu generacji. Nie zakładamy podróży szybszej od światła.

| Technologia | Nauka | Wymagania wewnętrzne | Efekt |
|---|---:|---|---|
| Statki pokoleniowe | 35000 | Wspólnota Układu | Sondy i statki pokoleniowe są przedsięwzięciami wielu generacji. Nie zakładamy podróży szybszej od światła. Premia w każdym mieście: żywność +6. |
| Energetyka gwiazdowa | 40250 | Statki pokoleniowe | Sondy i statki pokoleniowe są przedsięwzięciami wielu generacji. Nie zakładamy podróży szybszej od światła. Premia w każdym mieście: produkcja +6. |
| Sondy międzygwiezdne | 45500 | Statki pokoleniowe | Sondy i statki pokoleniowe są przedsięwzięciami wielu generacji. Nie zakładamy podróży szybszej od światła. Premia w każdym mieście: nauka +6. |
| Cywilizacja rozproszona | 50750 | Statki pokoleniowe | Sondy i statki pokoleniowe są przedsięwzięciami wielu generacji. Nie zakładamy podróży szybszej od światła. Premia w każdym mieście: złoto +6 · kultura +6. |
| Karta międzygwiezdna | 56000 | Statki pokoleniowe, Energetyka gwiazdowa, Sondy międzygwiezdne, Cywilizacja rozproszona | Zamyka dorobek epoki i otwiera badania: dalsze projekty międzygwiezdne. Premia w każdym mieście: nauka +6 · kultura +6. |

| Nowa infrastruktura | Produkcja | Technologia | Działanie |
|---|---:|---|---|
| Kolektor energii gwiazdowej | 7000 | Energetyka gwiazdowa | Energia +700 · Emisje -30 · Utrzymanie złota/turę +40 |
| Archiwum cywilizacji | 7200 | Cywilizacja rozproszona | nauka +330 · kultura +550 · Popyt energii +180 · Produkcja, wiedza i dochód tego obiektu zależą od miejskiego pokrycia energii: 25–100%. |
| Stocznia statków pokoleniowych | 7500 | Statki pokoleniowe | produkcja +700 · nauka +160 · Popyt energii +240 · Produkcja, wiedza i dochód tego obiektu zależą od miejskiego pokrycia energii: 25–100%. |

| Model wojsk | Siła bazowa | Ruch | Zasięg | Produkcja | Technologia |
|---|---:|---:|---:|---:|---|
| Straż cywilizacji rozproszonej | 550 | 3 | 1 | 9000 | Cywilizacja rozproszona |
| Autonomiczne wozy ekspedycyjne | 590 | 5 | 1 | 10000 | Sondy międzygwiezdne |

**Rada: List do ludzi, których nie poznamy**

Statek pokoleniowy może wyruszyć poza Układ. Nie dożyjemy odpowiedzi. Jakie instytucje i zobowiązania powierzymy następnym pokoleniom?

- Otrzymają wolność i otwarte archiwum wiedzy. Złoto -5000 · Lud +10 · Uczeni +15 Samostanowienie i nauka zamiast odziedziczonej zależności. Trwale: nauka % +30
- Zachowamy ciągłość planu państwowego. Lud -10 · Rody +12 Silna baza przemysłowa i długie zobowiązania. Trwale: produkcja % +30
- Najpierw samowystarczalność i ochrona życia. Lud +12 Energia oraz żywność dla przyszłych pokoleń. Trwale: żywność % +22 · energia/miasto +150

## Programy kosmiczne i dalsza gra

Programy są narodowymi przedsięwzięciami z trwałymi przychodami w aktualnej stolicy. Każdy wymaga technologii, wcześniejszego programu, wskazanej infrastruktury w mieście budowy i pełnego pokrycia energii. Można umieszczać je w kolejce. Utrata zasilania wstrzymuje pracę bez utraty już zgromadzonej produkcji. Ukończonego programu nie można powielić w drugim mieście.

| Program | Produkcja | Infrastruktura | Skutek |
|---|---:|---|---|
| Stała stacja orbitalna | 6000 | Stocznia orbitalna | Stolica: +40 nauki, +20 złota na turę; dostawy państwa +2. |
| Samowystarczalna baza księżycowa | 10000 | Terminal księżycowy | Stolica: +80 produkcji i +30 złota na turę. |
| Osadnictwo marsjańskie | 15000 | Zespół farm pozaziemskich | Stolica: +60 żywności i +120 nauki na turę. |
| Wspólnota pasa asteroid | 22000 | Odlewnia asteroid | Stolica: +180 produkcji i +100 złota na turę. |
| Wysłanie statku pokoleniowego | 40000 | Stocznia statków pokoleniowych | Zwycięstwo naukowe; wymaga stabilności co najmniej 55 i pełnego pokrycia energii w mieście startu. |

Wysłanie statku pokoleniowego wymaga dodatkowo stabilności co najmniej 55 i daje zwycięstwo naukowe. Atlas świata pozostaje użytecznym projektem historycznym (+35 nauki, +25 kultury miasta) i nie kończy pełnej kampanii. Nowe zwycięstwo kulturowe wymaga epoki międzyplanetarnej, 300 000 kultury i 5000 prestiżu. Zjednoczenie nadal może zakończyć rozgrywkę wcześniej. Po zwycięstwie wybierz **Kontynuuj rozwój świata**, aby grać dalej do limitu; utraty ostatniego miasta lub końca limitu nie można w ten sposób ominąć.

Od 0.21 mapa planetarna jest przestrzenną sceną 3D z kamerą perspektywiczną. Uproszczone modele miast i wojsk zmieniają się przy rozwoju. Programy reprezentują stałe placówki poza planetą i ich gospodarkę, bez osobnej mapy Księżyca lub Marsa, sterowalnych flot, bitew orbitalnych czy lotu szybszego od światła. Dalsza produkcja może rozwinąć te systemy w osobne teatry działań.

## Kontekst i źródła inspiracji

- [Historic England — Stone Age to Iron Age](https://historicengland.org.uk/education/schools-resources/teaching-activities/timeline-stone-age-to-iron-age/): regionalny przykład rozróżnienia epok prehistorycznych, bez traktowania lokalnych dat jako uniwersalnych.
- [Luigi Cajani, Periodization — Oxford Handbook of World History](https://academic.oup.com/edited-volume/34513/chapter-abstract/292841417): różne sposoby podziału historii świata. Nazwy w grze są autorskim uproszczeniem.
- [NASA — Moon to Mars Architecture](https://www.nasa.gov/moontomarsarchitecture/): inspiracja etapowym budowaniem infrastruktury poza Ziemią. Opisane w grze przyszłe technologie, terminy, dochody i odległe epoki są fikcją projektową, nie deklaracją NASA.

Dane tabel są generowane z rzeczywistego katalogu gry. Regeneracja: `dotnet run --project tests/Crown.Specs -c Release -- --export-epochs EPOKI.md`.
