# Zmiany

## 0.24 — Pory roku i pogoda · 27 października 2026

- Cztery widoczne warianty pór roku zgodne z turą kampanii: wiosenna zieleń, letnie materiały, jesienne korony drzew liściastych i zimowy śnieg. Drzewa iglaste zachowują zieleń jesienią. Zimą śnieg pojawia się na terenie, koronach, dachach i poziomych powierzchniach budowli. Pustynne materiały otrzymują znacznie mniej śniegu.
- Deszcz i opad śniegu jako przestrzenne cząstki Direct3D testowane względem głębokości terenu i budynków. Nurt, wiatr i opady korzystają z osobnego czasu wizualnego. Zachmurzenie, mgiełka, ekspozycja i mokre powierzchnie reagują na wybrany wariant pogody.
- Niebo renderowane do sceny HDR przed geometrią i opadami, dzięki czemu końcowa obróbka nie usuwa cząstek widocznych na jego tle. Neutralny materiał mgły wojny odróżniony od znanych skał i tundry przy nakładaniu detali oraz sezonowych barw.
- Menu Grafika: osobny przełącznik sezonowych materiałów oraz pogoda zgodna z porą roku, brak opadów, deszcz i śnieg. Tryb automatyczny pokazuje spokojną wiosnę i lato, lżejszy deszcz jesienią i śnieg zimą. Ustawienia dotyczą bieżącego widoku.
- Pora roku aktualizuje parametry shadera bez przebudowy terenu. Bufory opadów są zastępowane i zwalniane. Cząstki nie uczestniczą w wybieraniu pól. Reguły gospodarki, losowania kampanii i format v19 pozostają zgodne.
- Jedenasty tryb `--ui-smoke-environment`: cztery rzeczywiste obrazy GPU, stały czas, opady, powtarzane przełączanie i liczba zasobów natywnych, wybór miasta, wczytanie zimowego zapisu oraz mniejsze okno. Osobne wydanie `wydanie-0.24/`, zachowane starsze paczki i zapisy.
- To oprawa sezonów istniejącej kampanii, bez nowej symulacji meteorologicznej, powodzi, zamarzania wód lub fizycznego narastania pokrywy. Nie dodano nowych bitmap ani modeli zewnętrznych. Grafika nadal nie osiąga fotorealizmu ilustracji menu.

## 0.23 — Rzeki i przeprawy · 27 października 2026

- Rzeki korzystają ze wspólnej sieci próbkowanych odcinków i koryt wyciętych w polu wysokości. Sąsiednie odkryte pola łączą się w zakola z identycznymi punktami i poziomami wody na węzłach. Znane końcowe odcinki mogą dojść do odkrytego brzegu morza lub jeziora. Nieodkryte rzeki pozostają ukryte.
- Woda ma współrzędne wzdłuż koryta, animowany wzór nurtu, przybrzeżną pianę i kolor zależny od rzeczywistej różnicy poziomu wody i dna. Żwirowe brzegi, kamienie i kępy trzcin są geometrią 3D. Drzewa omijają koryta.
- Istniejące drogi tworzą przeprawy nad przecięciami z rzeką: pomosty z osobnych desek, przyczółki lub podpory, poręcze i dojazdy. Późniejsze warianty mają kamienne podpory i parapety. Historyczne miasto z drogą do wschodniego sąsiada otrzymuje dodatkową bramę, aby przeprawa nie kończyła się pełną kurtyną.
- Pole wysokości unieważnia się również po zmianie odkrytej rzeki lub położenia znanego miasta. Geometria pozostaje powtarzalna i nie używa losowań kampanii. Reguły dróg, ruchu, gospodarki i format zapisu v19 pozostają zgodne.
- Dziesiąty tryb diagnostyki `--ui-smoke-rivers`: ukryte odcinki, wycinanie koryta, powtarzalność po odczycie zapisu, ciągłość węzłów, przeprawa rzeczywistej drogi, wybór mostu, animacja, bufory GPU i mniejsze okno. Osobny katalog `wydanie-0.23/`, wcześniejsze paczki zachowane.
- To wizualna reprezentacja istniejących flag rzek i dróg, bez hydrodynamiki, żeglugi ani osobnego systemu budowania mostów. Nadal nie osiąga fotorealizmu ilustracji menu. Wykorzystano dotychczasowe tekstury, bez nowych bitmap AI lub obcych modeli.

## 0.22 — Krajobraz i miasta · 27 października 2026

- Wspólne, buforowane pole wysokości dla geometrii, kamery i materiału wody. Szerokie masywy z połączonymi grzbietami zastępują odrębne szczyty przypisane heksom. Gęstsza siatka terenu i ciągły, szczegółowy horyzont poza planszą.
- Kolor jezior i morza zależny od przybliżonej głębokości, widoczne płycizny, drobna piana oraz wilgotne brzegi. Osobny przełącznik efektów brzegowych. Uproszczony filtr kontrastu wygładza krawędzie; można go wyłączyć w menu Grafika.
- Miasta etapów 3–9 mają rozdzielone parcele, dwie główne ulice, plac, stragany i ratusz. Wzrost dodaje domy bez zmiany wcześniejszych parcel. Warianty późniejsze otrzymują belkowanie i wieżę. Wybudowane mury tworzą kurtyny, baszty i przestrzenną bramę z rzeczywistym otworem. Zachowano wizualizację infrastruktury przemysłowej.
- Obrót części modeli zachowuje normalne i przypisanie trójkątów do właściwych pól. Nieodkryty rodzaj terenu nie wpływa na pole wysokości. Oprawa i kamera nie zmieniają stanu kampanii ani jej generatora losowego.
- Dziewiąty tryb diagnostyki `--ui-smoke-landscape`: pamięć podręczna wysokości, ukryta geografia, stabilne parcele, wybór bramy, rzeczywiste zmiany pikseli po przełączeniu efektów i zasoby GPU podczas ruchu kamery. Osobne wydanie `wydanie-0.22/`, zachowane wcześniejsze paczki i zapis v19.
- Modele nadal są proceduralne, a oprawa nie osiąga fotorealizmu ilustracji menu. Nie dodano nowych tekstur AI ani zewnętrznych modeli; wykorzystano materiały wydania 0.21.

## 0.21 — Światło i materiały · 23 października 2026

- Sprzętowy renderer Direct3D 11 napisany w C#/.NET 10 i HLSL. Interfejs pozostaje natywny WPF; symulacja Crown.Core i format zapisu v19 nie zostały zmienione. Jawny tryb zgodności WPF przy niedostępnym GPU i parametr `--no-gpu`.
- Materiały z oświetleniem GGX, tekstury trawy, skał, kamienia i dachówek, mapa cieni 2048×2048 z filtracją PCF, wewnętrzne HDR, krzywa filmowa, przybliżona okluzja ekranowa i atmosfera. To początek bardziej realistycznej oprawy, bez deklaracji jakości fotorealistycznej ilustracji menu.
- Nowe korony dębów i świerków: przestrzenne gałęzie oraz tekstury liści i igieł z kanałem alfa, także w przebiegu cieni. Wiatr i drobny ruch wody zależą od osobnego czasu wizualnego, bez wpływu na turę lub losowania kampanii.
- Odbicia terenu i obiektów na płaszczyźnie jezior oraz morza, odbicia nieba i światła w rzekach. Rzeki meandrują, mają brzegi i omijają środek pola miasta. Gęstsza siatka terenu, urozmaicone grzbiety i dekoracyjne dalekie pasma poza planszą.
- Gęstsza zabudowa z podmurówkami, oknami, belkami i kominami; teksturowane mury i dachówki. Bardziej spójna skala formacji wojsk.
- Kamera krajobrazowa z szerokim obiektywem i nachyleniem do 15 stopni. Menu Grafika: jakość wysoka/zrównoważona, cienie, odbicia, animacja, wariant światła i ponowna inicjalizacja GPU.
- Semantyczne przypisanie trójkątów modeli do pól. Wystający fragment miasta lub oddziału wskazuje właściwe pole; dekoracyjne karty liści nie przechwytują rozkazów przez przezroczyste otwory.
- Ósmy tryb diagnostyki `--ui-smoke-photographic`: rzeczywiste obrazy GPU, zmiany pikseli po przełączaniu efektów, alfa liści, powtarzalność stałego czasu, zmiana rozdzielczości, pomiar klatek, wybór miasta i odczyt zapisu. Osobne wydanie `wydanie-0.21/`, zachowane poprzednie paczki i zapis gracza.

## 0.20 — Świat w trzech wymiarach · 23 października 2026

- Główna mapa zastąpiona rzeczywistą sceną WPF 3D w C#/.NET 10. Ciągłe wysokości terenu, wspólny atlas materiałów, woda, oświetlenie, przestrzenne lasy, pola, kopalnie, ruiny, drogi i granice.
- Modele miast zmieniające się wraz z epoką i ludnością: od obozowisk przez fortyfikacje i zabudowę przemysłową po wieżowce oraz przyszłe habitaty. Przestrzenne formacje sześciu ról wojsk, kawaleria, artyleria, pojazdy i karawany.
- Kamera perspektywiczna z obrotem, nachyleniem, przesuwaniem i zbliżeniem; sterowanie myszą, klawiaturą i przyciskami. Minimąpa pokazuje perspektywiczny obrys widoku.
- Wybór pola i rozkazy przez trafienie promieniem w geometrię, połączone z prawdziwym ruchem, podglądem walki i potwierdzonym atakiem. Nakładki zasięgu, marszu, logistyki i ludności zachowane.
- Mgła wojny ukrywa obce oddziały oraz nieodkrytą rzeźbę. Kamera i budowa sceny nie zmieniają stanu kampanii ani losowań. Zamrożone siatki grupowane materiałami; teren ponownie wykorzystywany przy aktualizacji obiektów.
- Format zapisów pozostaje v19. Nie trzeba zaczynać od nowa. Osobne wydanie `wydanie-0.20/`, zachowane poprzednie paczki i zapisy.
- 438 specyfikacji silnika i 327 sprawdzeń WPF, w tym 34 nowe sprawdzenia sceny 3D. Rzeczywiste zrzuty rozgrywki dla kilku epok. Jest to pierwsza wersja 3D z uproszczonymi modelami; PBR, animacje i bardziej szczegółowe zasoby pozostają do wykonania.

## 0.19 — Słowo rady pod napięciem · 23 października 2026

- Historyczne odczyty faktycznego przydziału energii z początku fazy gospodarczej. Osiem ostatnich faz miasta, bez zamiany aktualnej prognozy w fikcyjną historię. Nowe źródła i zmiany sezonu nie zmieniają wcześniejszych danych.
- Po trzech kolejnych fazach poniżej 75% pokrycia, przy dodatnim popycie: lokalne niepokoje +2 i dobrobyt -1 na fazę, lud państwa -1 niezależnie od liczby miast. Te same zasady dla AI. Jedna dobra faza przerywa serię.
- Delegatka Ira Senn przedstawia rzeczywisty kryzys własnego miasta. Trzy odpowiedzi: pomoc za 60 + 5 × ludność w chwili zgłoszenia złota, darmowa sprawdzalna obietnica lub jawna odmowa. Miejscowe i narodowe skutki są widoczne w radzie. Pomoc nie odtwarza energii.
- Obietnica wymaga trzech kolejnych przyszłych pełnych faz zasilania w sześć tur i działania wszystkich zakładów protokołu. Wyłączenie tych usług lub zastąpienie innymi nie zalicza odbudowy. Ostatnia dozwolona faza najpierw sprawdza wykonanie, potem termin. Dowody, postęp i zakończenie pozostają zapisane, bez ponownej nagrody lub kary.
- Utrata miasta łamie obietnicę, lecz nie nakłada miejscowej kary dawnego władcy na nowego. Nierozpatrzona delegacja zostaje zamknięta. Zmiana właściciela czyści lokalną historię odczytów; nie otwiera nowej sprawy za cudze stare niedobory.
- Jedna aktywna sprawa na miasto, 16 tur odstępu od zamknięcia. Rejestr kryzysów z protokołem, odczytami, terminem, postępem i wynikiem; przejście do naprawy zasilania. Odczyty i aktywna sprawa także w panelu energii miasta. Inny otwarty dialog zachowuje kontekst.
- Format v19 i kopia `.v18.bak`. Migracja v18 zachowuje całą kampanię oraz dyspozycje energetyczne, bez dopisywania starych kryzysów. Walidacja wiąże chronologię, potrzeby usług, źródło delegacji, przyszłe dostawy i możliwy wynik.
- Nowe specyfikacje i szósty tryb WPF `--ui-smoke-energy-cases`: prawdziwe kończenie tur, trzy gałęzie dialogu, budowa źródła, postęp, zapis w toku, sukces i porażka. Osobne wydanie `wydanie-0.19/`; zachowane poprzednie paczki i zapis gracza.

## 0.18 — Energia pod kontrolą · 23 października 2026

- Trzy priorytety zasilania miast. Najpierw wyższa grupa; w tej samej grupie podział proporcjonalny. Rzeczywiste zmiany nauki, pracy, dochodów i gotowości programu kosmicznego, bez tworzenia dodatkowej energii.
- Wyłączanie zakładów epok z energią lub popytem: ustają efekty działania, energia, potrzeby i emisje; pozostają fizyczne miejsca mieszkalne oraz 25% utrzymania. Rozruch kosztuje 5% ceny budowy w złocie. Priorytet miasta i stan każdego obiektu można zmienić raz na turę. Kolejki i zgromadzona praca pozostają.
- Cztery nowe obiekty: wodna elektrownia, farmy wiatrowe i słoneczne, elektrownia geotermalna. Teren ogranicza budowę wodnej i geotermalnej; wiatr i słońce mają sezonową i terenową moc. Łącznie 58 obiektów rozwoju epok i 76 trwałych projektów miejskich. Pełne wymagania i koszty w README i EPOKI.
- Jeden plan zasilania i emisji dla całej fazy gospodarczej. Nowe źródło zasila od kolejnej fazy. Program kosmiczny wymaga czynnej infrastruktury. Wyłączenie zakładu może przesunąć ograniczoną moc do innych miast.
- Naprawiona krajowa sieć drogowa przy zajętym mieście: blokowane miasto nie rozpoczyna przeszukiwania i nie łączy odciętych gałęzi. Priorytet nie omija blokady.
- AI uwzględnia emisje źródeł, płaci rozruch potrzebnych przejętych zakładów, wygasza zbędną emitującą moc i nadaje pierwszeństwo wybranemu programowi kosmicznemu. Bez cyklicznego płatnego uruchamiania zbędnej elektrowni.
- Panel dyspozycji z przydziałami całej sieci, bieżącą mocą zakładów, kosztami i ograniczeniami; dostęp z miasta i przeglądu świata. Oznaczenie wyłączonych obiektów w liście budynków, przewijanie w mniejszym oknie.
- Format v18. Migracja v17 zachowuje epoki, klimat, instytucje, modele, programy i wcześniejsze systemy. Nowe dyspozycje domyślnie zwykłe, bez fikcyjnych wyłączeń. Kopia `.v17.bak`, walidacja stanów i dat, deterministyczne odtwarzanie.
- Nowe specyfikacje gospodarki, geografii, AI i migracji; piąty, osobny tryb diagnostyki WPF. Osobne wydanie `wydanie-0.18/`, zachowane wcześniejsze wydania i zapisy.

## 0.17 — Od ognia do gwiazd · 23 października 2026

- 18 głównych etapów: paleolit, neolit, brąz, żelazo, starożytność klasyczna, średniowiecze, renesans, oświecenie, rewolucja przemysłowa, nowoczesność, atom, informacja, automatyzacja, planeta, orbita, świat międzyplanetarny, Układ Słoneczny i spekulatywna epoka międzygwiezdna.
- 90 technologii (66 nowych), cztery dziedziny oraz zamknięcie każdego etapu. Następny etap wymaga poprzedniego zamknięcia, a nie dowolnej liczby odkryć. Każda nowa technologia daje rzeczywistą premię miasta. Państwa rozwijają się osobno.
- 54 nowe budynki: od obozowisk i młynów przez sieci energetyczne, fabryki i centra danych po infrastrukturę orbitalną oraz stocznię statków pokoleniowych. Łącznie 72 trwałe projekty miejskie. Działają dochody, produkcja, wiedza, żywność, kultura, mieszkania, opieka, obrona, zasięg i utrzymanie. Filtr epoki w wyborze projektu.
- 34 nowe modele wojsk, łącznie 40 modeli jednostek w sześciu lądowych rolach. Własna siła, ruch, zasięg, koszt i utrzymanie. Dalsza artyleria korzysta z właściwego zasięgu, nie zdobywa miast. Czołgi i wozy przyszłości nie wymagają koni, jazda konna nadal tak.
- Modernizacja we własnym mieście z dostawami: złoto i pełny ruch za silniejszy znany model tej samej roli. Zdrowie i doświadczenie pozostają; umocnienia i marsz są anulowane. Porównanie i koszt uwzględniają dotychczasowe premie materiałowe dawnych wojsk, aby wykluczyć płatne osłabienie. Osobne symbole nowoczesnych jednostek i sylwetki miast epok w mapie 2D.
- Energia współdzielona w bezpiecznej krajowej sieci drogowej. Odcięcie źródła wpływa na zasilanie. Brak energii ogranicza do 25% premie wymagającego jej obiektu; pełne pokrycie daje 100%. Utrzymanie płacone jest w całości. Program kosmiczny wymaga pełnego zasilania.
- Wspólna presja klimatyczna od emisji i redukcji państw, wpływająca na plony. Energia, recykling, odbudowa klimatu i polityka tworzą różne drogi rozwoju. Bez losowych katastrof i deklarowania naukowej prognozy klimatu.
- 18 nowych dialogów i 54 trwałe kierunki instytucjonalne. Podział ziemi, prawa obywateli, druk, przemysł, energia, dane, automatyzacja, biosfera i autonomia placówek pozaziemskich łączą przyszłość z wyborami społecznymi. Nie można odebrać decyzji drugi raz; wolne opcje chronią pusty skarb.
- Pięć narodowych programów: stacja orbitalna, baza księżycowa, osadnictwo marsjańskie, wspólnota pasa asteroid i statek pokoleniowy. Rzeczywista produkcja, infrastruktura, energia i łańcuch wymagań; korzyści w aktualnej stolicy. Ukończony program nie jest powielany w innych miastach i jest pomijany w ich planach bez niszczenia pracy. Nie ma osobnej mapy kosmosu ani sterowalnych flot.
- Nowa kampania do 1600 tur i umowny kalendarz do roku 5000, uwzględniający rozwój świata. Naukowe zwycięstwo dopiero za statek pokoleniowy. Atlas jest użytecznym wcześniejszym osiągnięciem. Wyższe wymagania zwycięstwa kulturowego i możliwość kontynuowania świata po zwycięstwie do limitu.
- AI korzysta z technologii epok, energetyki, nowych budynków, modeli, modernizacji, programów i własnych instytucji. Nadal działa według heurystyk. Rozszerzone testy obejmują pełne 90 odkryć, całą nową infrastrukturę, realną produkcję programów i rozwój AI bez przyznawania badań lub budynków.
- Format v17, migracja v16 zachowująca niewolę, wywiad i wcześniejsze systemy, kopia `.v16.bak`. Stare kampanie rozszerza osobny przycisk, zachowując miasta i zasoby oraz uznając wcześniejszy dorobek. Walidacja epok, par programów, modeli, klimatu i kontekstu rady.
- Przegląd epok, infrastruktury, decyzji i modeli; panel energii, klimatu oraz programów. Podręcznik `EPOKI.md` generowany z faktycznych danych. Osobne wydanie `wydanie-0.17/`, zachowane wcześniejsze paczki i zapisy. Czwarty tryb diagnostyki WPF, wszystkie bez autozapisu i wykonywane kolejno.

## 0.16 — Ludzie za granicą · 23 października 2026

- Obcy agenci schwytani przez gracza czekają na decyzję jego rady. AI nie wykupuje ich automatycznie. Nowy kontekstowy dialog wskazuje osobę, państwo, miasto, środki wysyłającego i możliwość odzyskania naszego człowieka.
- Okup przenosi dokładnie 100 złota z obcego skarbu; brak środków wyłącza tę odpowiedź. Bez darmowo tworzonych pieniędzy i ponownej wypłaty za tę samą niewolę.
- Dwustronna wymiana zwalnia jednego obcego i jednego własnego agenta przetrzymywanego przez to samo państwo. Dostępna także podczas wojny i bez złota, z obu dialogów niewoli. Historia zachowuje wzajemne powiązanie operacji. Kilku jeńców nie znika po jednej wymianie.
- Pojednawcze zwolnienie przywraca obcego agenta do służby, zwiększa zaufanie o 12 oraz prestiż o 4, a obniża poparcie rodów o 3. Wcześniejsze krzywdy pozostają; wojna nie kończy się samoczynnie.
- Internowanie na osiem tur wiąże miejsce w obcej służbie: zaufanie -8, lud +3. Nie można go odwołać ani później sprzedać jeńca. Na początku tury końcowej ta sama osoba automatycznie wraca do służby, a raport i kronika odnotowują powrót. Zmiana kontroli miasta nie przenosi niewoli.
- Panel wywiadu pokazuje oczekujących i internowanych jeńców, terminy, dostęp do rady i raporty. Nowa sprawa nie zastępuje otwartej rozmowy; czekająca sprawa obcego agenta ma pierwszeństwo przed własnym okupem, aby pozostawić możliwość wymiany.
- Format v16 zachowuje decyzje, terminy i pary wymian. Migracja v15 nie otwiera dawnych zamkniętych niewoli, nie powiela płatności i zachowuje starych agentów. Nadpisanie pozostawia `.v15.bak`; dotychczasowe migracje nadal działają.
- Osobne wydanie `wydanie-0.16/`, zachowane starsze paczki i zapisy. Osobna diagnostyka WPF jeńców, wykonywana kolejno z pozostałymi trybami i bez autozapisu. To stałe rozstrzygnięcia rady, bez negocjowania ceny, przesłuchań, ucieczek i podwójnych agentów. Oprawa pozostaje 2D.

## 0.15 — Służba cieni · 23 października 2026

- Agenci po Piśmie, szkolenie za 60 złota, trzy miejsca na państwo i doświadczenie z operacji. Schwytani nadal zajmują miejsce; utraceni zostają w historii. Jeden agent prowadzi jedną operację.
- Rozpoznanie za 55 przez trzy tury: odkrycie trzech heksów i datowany raport o mieście oraz skarbie. Nie tworzy stałego podglądu. Kradzież badań za 110 przez cztery tury kopiuje część rzeczywistej dostępnej technologii do nauki, z limitem 120 punktów i bez darmowego odkrycia. Sabotaż za 95 przez cztery tury odbiera do 60 rzeczywistych punktów pracy bez niszczenia budynków i kolejki.
- Osiem tur odstępu siatki od poprzedniego rozpoczęcia w mieście danego państwa. Odwołanie zwalnia agenta, bez zwrotu i bez ominięcia odstępu. Zmiana właściciela celu przerywa misję; już poznane badania nie wypłacają ponownie nagrody.
- Osobne losowania sukcesu i wykrycia, z zapisanym generatorem. Szanse zależą od stopnia agenta, sądu, Pisma, stabilności i kontrwywiadu przy rozstrzygnięciu. Sukces może ujawnić sprawcę. Wykrycie: zaufanie -15, krzywdy +25, prestiż -2; wykryta porażka oznacza schwytanie.
- Kontrwywiad za 60 złota daje +25 obrony przez osiem tur i wpływa także na rozpoczęte operacje. Sąd daje +15, Pismo +5, wysoka stabilność +5. Ochrona wygasa na początku wskazanej tury i przy zmianie właściciela; aktywnej nie można przedłużać.
- Kontekstowy dialog o schwytanym agencie: okup 100, pokojowe przeprosiny za 40 z kosztem prestiżu i poparcia albo trwała utrata agenta z politycznymi skutkami. Płatności trafiają do państwa przetrzymującego agenta. Istniejąca rada nie jest nadpisywana. Utrata miasta nie przenosi niewoli na inne państwo.
- Wrogie AI od T20 finansuje własną kradzież badań lub sabotaż, reaguje ochroną na wykryte ingerencje i rozlicza niewolę swoich agentów. Niewykryte obce operacje nie zdradzają sprawcy w interfejsie; wykryte są dostępne w historii.
- Panel w Dyplomacji: rekrutacja, formularz misji, bieżące ryzyko, terminy, wycofanie, raporty, ochrona miast i rozstrzygnięcia niewoli. Powiadomienia kierują do panelu; szczegóły pozostają w raporcie i kronice.
- Format v15 i migracja v14 bez fikcyjnej starej działalności wywiadu. Długi, rozkazy i produkcja pozostają zachowane. Nadpisanie zachowuje `.v14.bak`. Walidacja obejmuje statusy, historię agentów, wyniki, terminy, nagrody i kontekst rady.
- Osobne wydanie `wydanie-0.15/`; starsze paczki i zapisy zachowane. Osobna diagnostyka WPF szpiegostwa, uruchamiana kolejno po dotychczasowych sprawdzeniach, z wyłączonym autozapisem. Model używa abstrakcyjnych siatek, bez agentów poruszanych po heksach i podwójnych agentów. Oprawa nadal jest mapą 2D.

## 0.14 — Pieczęć i dług · 22 października 2026

- Pożyczki 100, 200 i 300 złota finansowane przez zagraniczne skarbce, z rzeczywistym transferem kapitału. Moneta, pokój, traktat, zaufanie +10 i środki wierzyciela są sprawdzane przy podpisaniu. Dwie otwarte umowy, jedna na państwo; osiem tur odstępu między finansowaniami z tego samego źródła.
- Punktacja majątku uwzględnia otwarte zobowiązania i należności wierzycieli. Pożyczony kapitał nie tworzy darmowych punktów zwycięstwa; spłata nie odejmuje majątku drugi raz.
- Termin po 12 turach i stała cena 20% kapitału. Wcześniejsza spłata obejmuje całą umowną kwotę, przekazuje ją wierzycielom i daje +5 zaufania. Nie ma automatycznego potrącania lub dopisywania długu do kosztów każdej tury.
- Przypomnienie dwie tury przed terminem i kontekstowa rozmowa z wierzycielami. Pokazuje nazwę państwa, umowę, wypłacone środki i aktualną spłatę. Istniejąca rada nie jest zastępowana, a osobne długi wymagają osobnych decyzji.
- Jednorazowe odroczenie: opłata 20 od razu, dodatkowe 10% pierwotnego kapitału do długu, sześć tur od decyzji i zaufanie -5. Wymaga pokoju i czynnego państwa wierzycieli. Drugie odroczenie jest wyłączone.
- Zerwanie zobowiązania: stabilność -12, lud -4, rody -8, prestiż -10, zaufanie wierzycieli -30 i krzywdy +35. Koniec traktatu może zatrzymać opłacone karawany, bez zwrotu zakupu. Nowe finansowanie u wszystkich kupców zamknięte na 24 tury, lecz pozostałe umowy nadal obowiązują. Odmowa jest dostępna także bez złota.
- Wojna i upadek państwa wierzycieli nie usuwają długu ani możliwości spłaty. Nie ma darmowego umorzenia przez wypowiedzenie wojny. Po zakończeniu kampanii nie można zmieniać finansów.
- Panel skarbu z ofertą, wymaganiami, sumą zobowiązań, terminami, spłatą i historią. Dostęp z bilansu w Szlakach oraz Dyplomacji. Wcześniejsze polecenia i małe okno pozostają obsługiwane przez przewijanie.
- Format v14 zapisuje umowy, płatności, terminy, odroczenia, wynik i bieżące wezwanie. Migracja v13 zachowuje rozkazy armii, umocnienia, produkcję i rachunki bez dopisywania dawnych długów. Nadpisanie zachowuje `.v13.bak`; starsze migracje nadal działają.
- Osobne wydanie `wydanie-0.14/`, wcześniejsze paczki i zapisy zachowane. To pożyczki gracza od państw reprezentujących kupców, bez autonomicznego zadłużania AI, płynnych stóp, bankructwa firm i rynku obligacji. Oprawa pozostaje teksturowaną mapą 2D.

## 0.13 — Utrzymać pozycję · 22 października 2026

- Stały rozkaz okopywania: pełny ruch za przygotowanie, +20% siły obronnej po pierwszym rozliczeniu tury, +40% po drugim. Premia dotyczy oddziału, uwzględnia zdrowie i dostawy, a dodatek terenu jest liczony osobno. Nie wzmacnia natarcia ani nie jest przenoszona do miejskich murów.
- Rekonwalescencja do pełnego zdrowia: 20 zdrowia/turę z dostawami lub 28 po Medycynie; automatyczne przywrócenie gotowości. Trafienie przeciwnika przerywa rozkaz. Bez dostaw tylko 3 leczenia i zwykłe 5 strat, z dotychczasowym wyjątkiem zwiadowców. Brak bezpłatnego leczenia przy wydaniu rozkazu.
- Wydanie lub zmiana wymaga pełnego ruchu. Odwołanie nie zwraca punktów; w następnej turze dostępny jest nowy zapas. Marsz, atak, niszczenie drogi i przydział eskorty porzucają pozycje. Nieprawidłowe polecenie oraz zamknięcie podglądu walki zachowuje je. Przymusowe wycofanie usuwa przygotowanie.
- Panel rozkazów pokazuje warunki, poziom umocnień, leczenie i straty za brak zaopatrzenia. Przegląd armii odnajduje wszystkie oddziały i prowadzi do ich pozycji. Tab pomija stałe rozkazy i eskorty. Na mapie pojawiają się znaczniki F0–F2 i +.
- Podgląd walki opisuje umocnienia przeciwnika i utratę własnych przy ataku. Używa tej samej formuły co rzeczywista walka, także przy ostrzale. Przygotowane pozycje pozostają po obronie, dopóki oddział żyje.
- AI okopuje stacjonujące w pobliżu miast oddziały bojowe i kieruje rannych z dostawami na rekonwalescencję. Marsze, ofensywy i odwrót nadal korzystają z dotychczasowych heurystyk.
- Format v13 zapisuje rozkaz i stopień przygotowania. Migracja v12 zachowuje gospodarkę, plany produkcji, eskorty i marsze bez fikcyjnych umocnień. Nadpisanie starszego pliku zachowuje również `.v12.bak`. Walidacja odrzuca sprzeczne rozkazy i niemożliwe stopnie przygotowania.
- Osobne wydanie `wydanie-0.13/`; starsze wydania, paczki i zapisy pozostają zachowane. To obrona pozycyjna i odpoczynek w istniejącym modelu walki, bez osobnych modeli oblężenia, morale i obozów na mapie. Oprawa nadal jest teksturowaną mapą 2D.

## 0.12 — Miasta z planem · 22 października 2026

- Trwałe kolejki produkcji: do ośmiu oczekujących zleceń na miasto, dodawanie, usuwanie, przesuwanie i rozpoczęcie pierwszego od razu. Edycja przyszłych pozycji nie zmienia obecnego projektu, postępu ani skarbca.
- Automatyczne przejście zachowuje całą nadwyżkę pracy. Nadal najwyżej jedno ukończenie na miasto i turę. Już ukończone budynki są pomijane; pusty plan korzysta z dotychczasowego wyboru następnej budowy.
- Planowanie przyszłych technologii i rekrutacji. Wymagania są ponownie sprawdzane po badaniach i zmianach ludności. Brak technologii, koni lub mieszkańców zatrzymuje bieżący projekt i dalszą kolejkę bez dopisywania nowej pracy. Dotyczy również starszych projektów, które utraciły wymagania.
- Powtarzane oddziały, roboty publiczne i święta wracają na koniec kolejki. Wymagania, zużycie mieszkańców i utrzymanie pozostają aktywne. Trwałych budynków nie można powtarzać ani dodawać wielokrotnie do oczekującego planu.
- Wstrzymanie i wznowienie produkcji zachowuje pracę, plan i tryb powtarzania. Pozostała gospodarka działa dalej. Wyczyszczenie przyszłych pozycji nie odwołuje obecnego projektu ani jego powtarzania.
- Ręczne rozpoczęcie innego projektu zachowuje dotychczasowe 80% pracy; wybór tego samego zachowuje całość. Wybór nowego projektu zastępuje poprzedni tryb powtarzania, bez usuwania pozostałego planu i bez automatycznego wznowienia.
- Formularz z opisami wymagań, stanem postoju, licznikami i kontrolą kolejności. Zachowuje wybór projektu, powtarzanie i przewinięcie przy edycji. Dostęp z panelu miasta, wyboru budowy i zarządzania prowincją.
- Zmiana suwerenności usuwa przyszłe zlecenia, powtarzanie i wstrzymanie starego właściciela, również przy pokojowym zwrocie.
- Format v12 przechowuje plan i tryby. Migracja v11 zachowuje bieżący projekt, pracę, rachunki i otwartą radę bez dopisywania zleceń. Nadpisanie zachowuje oryginał w `.v11.bak`; wcześniejsze migracje nadal działają.
- Osobne wydanie `wydanie-0.12/`. Wcześniejsze wydania, archiwa i zapisy pozostają zachowane. Oprawa nadal jest mapą 2D. Brak globalnych szablonów, równoległych linii i długich kolejek planowanych przez AI.

## 0.11 — Księgi i wpływy · 22 października 2026

- Korupcja prowincji odbiera rzeczywisty procent miejskiego złota, z lokalnym rejestrem minionych ubytków. Dochód w bilansie skarbu jest już po tym potrąceniu. Prognozy nie naliczają strat i nie zmieniają stanu.
- Cel korupcji zależy od odległości od najstarszego własnego miasta, niepokojów, stabilności i ustroju. Sąd, Służba publiczna, Konstytucja i nadzór obniżają go. Zmiana o jeden punkt na turę; cel 0–65. Żywność, nauka i produkcja nie podlegają temu potrąceniu.
- Kontrola urzędu za 35 złota, wymagająca Pisma i podstaw w rachunkach. Raport po trzech turach obejmuje rzeczywiście utracone środki, a późniejsze braki są liczone osobno. Jedna otwarta sprawa na prowincję, odstęp 12 tur od wysłania poprzedniej.
- Kontekstowy dialog rady: jawny proces odzyskuje 75% z karą polityczną dla rodów i miasta, ugoda 25% z mniejszą redukcją korupcji i kosztem poparcia ludu. Niezależny urząd kosztuje dodatkowe 60 złota bez odzysku; mocniej ogranicza korupcję i ustanawia 12 tur nadzoru. Pełne skutki są widoczne przy wyborze.
- Historia kontroli, rachunki każdego miasta, stan dochodzenia i przycisk otwarcia właściwej sprawy. Gotowy protokół czeka za aktualnym dialogiem. Pusty skarbiec pozostawia dwa rozstrzygnięcia dostępne; odzysku nie można wypłacić drugi raz.
- Utrata prowincji przerywa jej otwartą kontrolę, zeruje dalsze nieujęte środki i kończy nadzór. Nie wypłaca złota nowemu właścicielowi. Zamknięte wyniki pozostają w historii także po odbiciu miasta.
- AI podlega ubytkom i może sfinansować uproszczone lokalne porządki za 60 złota, bez odzysku i bez odgrywania własnej komisji. Nie wybiera rozstrzygnięć za gracza.
- Format v11 przechowuje rachunki, protokoły, odpowiedzi i nadzór. Migracja v10 zachowuje tranzyt, eskorty i obietnice bez tworzenia wcześniejszych braków; nadpisanie zachowuje `.v10.bak`. Starsze migracje nadal działają.
- Osobne wydanie `wydanie-0.11/`, wcześniejsze katalogi, paczki i zapisy zachowane. Oprawa pozostaje teksturowaną mapą 2D. Korupcja jest agregatem prowincji; nie ma jeszcze osobnych urzędników ani sieci patronatu.

## 0.10 — Otwarte bramy · 22 października 2026

- Dwa warianty negocjowania przejazdu kupców: prośba oparta na zaufaniu lub oferta 60 złota za zgodę na 12 tur. Posłowie kosztują 20 złota i wracają po dwóch turach. Oferta płatna podnosi jawną ocenę o 35; 60 złota przechodzi do gospodarza dopiero po przyjęciu. Brak środków przy odpowiedzi oznacza odmowę.
- Zgoda obejmuje importy i eksporty Marchii przez ziemie wskazanego gospodarza, również wysłane już karawany i zamknięte kontrakty. Zastępuje jego traktaty z obiema stronami ładunku. Traktat kupującego ze sprzedawcą nadal jest konieczny; zgoda nie obejmuje transportów między samymi państwami AI.
- Wojna gospodarza z którąkolwiek stroną i embargo zawieszają przejazd. Eskorta pozostaje przed wojskowo niedostępną granicą, a karawana ponownie ocenia bezpieczeństwo bez jej ruchomej osłony.
- Raport „Granice i tranzyt” wskazuje brakujące zgody na jednym potencjalnym bezpiecznym korytarzu i prowadzi do rozmowy z właściwym państwem. Obsługuje kilka zamkniętych granic, neutralne objazdy i różnicę między brakiem zezwolenia a wojną, embargiem lub zagrożeniem. Dostępny z manifestu i formularza kontraktu.
- Dwuturowa podróż posłów nie zatrzymuje licznika postoju. Zgoda może uratować wóz przed trzecią blokadą; spóźniona odpowiedź nie przywraca porzuconego ładunku. Przyjęcie daje relacje +5, odmowa -5; wszystkie poselstwa korzystają ze wspólnego odstępu dziesięciu tur.
- Termin jest wyłączny: zgoda z T5 działa do fazy T16, wygasa przed rynkiem T17. Kronika i komunikat tury przypominają dwa ruchy wcześniej i w dniu wygaśnięcia. Nie ma automatycznego przedłużenia ani wcześniejszego odnawiania aktywnej zgody.
- Format v10 przechowuje nowe warianty poselstw. Ważność praw jest wyliczana z przyjętych odpowiedzi, bez osobnej kopii stanu traktatu. Migracja v9 zachowuje eskorty, karawany, obietnice i wcześniejszą dyplomację bez dopisywania zgód; nadpisanie tworzy `.v9.bak`. Starsze migracje pozostają obsługiwane.
- Osobne wydanie `wydanie-0.10/`. Poprzednie wydania i zapisy pozostają zachowane. To dwustronne zgody dla kupców gracza, bez wielostronnej konferencji, ubezpieczeń, wojskowego dostępu ani samodzielnego negocjowania tranzytu przez AI. Oprawa nadal jest teksturowaną mapą 2D.

## 0.9 — Straż na trakcie · 22 października 2026

- Rozkaz eskorty aktywnej karawany przez własny oddział stojący na jej polu. Jedna eskorta na transport, bez osadników, z co najmniej 0,5 pozostałego punktu ruchu. Przydział jest bezpłatny, zastępuje poprzedni marsz i nie przesuwa jednostki od razu.
- Wspólny marsz w fazie rynku zużywa pozostały ruch wojska i budżet karawany. Wóz czeka na wolniejszego towarzysza. Drogi zachowują koszt 0,5 punktu; ETA uwzględnia tempo oddziału i niewykorzystany ruch. Prognoza przemieszcza wyłącznie kopie jednostek.
- Wyznaczona osłona porusza się razem z transportem przez sąsiednie strefy zagrożenia, bez samoczynnego ataku. Bezpośrednie zajęcie pola wozu nadal oznacza przechwycenie. Kolejne karawany oceniają pozycje już przemieszczonych eskort, bez pozostawiania ochrony w dawnym miejscu.
- Handel nie daje wojskowego prawa przejścia. Przed pokojową obcą granicą, obcym miastem lub oddziałem eskorta się odłącza; wóz ponownie ocenia trasę bez jej ruchomej osłony. Wojsko pozostaje na ostatnim osiągniętym heksie. Raport ostrzega o przewidywanym odłączeniu i możliwej zmianie czasu przybycia.
- Rozkaz rezerwuje oddział: samodzielny marsz, atak i niszczenie dróg wymagają odwołania eskorty. Podczas służby nie ma biernego leczenia. Normalne utrzymanie i zaopatrzenie armii nadal działają. Anulowanie nie zwraca wydanych punktów ruchu.
- Ruch rzeczywistej eskorty odsłania teren i rozlicza mijane ruiny raz. Przybycie lub utrata ładunku, odłączenie i ewakuacja po przejęciu miasta usuwają nieaktualny rozkaz. Nowy transport tej samej umowy nie dziedziczy eskorty.
- AI może przydzielić własnego zdrowego zwiadowcę znajdującego się na polu transportu strony umowy i nie odsyła go do zwykłego ruchu podczas służby. Nie przejmuje kontroli nad jednostką gracza.
- Formularz wyznaczenia i odwołania w raporcie karawany, rozkazy z panelu jednostki, status wojska, punkty ruchu, oznaczenie E na mapie oraz prognoza wspólnej drogi.
- Zapis v9 przechowuje przydział eskorty. Migracja v8 zachowuje obietnice i karawany bez dopisywania rozkazów; nadpisanie tworzy `.v8.bak`. Odczyt v1–v7 nadal działa.
- Osobne wydanie `wydanie-0.9/`; poprzednie katalogi, zapisy i paczki pozostają zachowane. Oprawa nadal jest mapą 2D. Brak automatycznej zbiórki eskort i złożonego planowania ochrony szlaków przez AI.

## 0.8 — Kupcy pamiętają · 22 października 2026

- Utrata karawany importowej do własnego miasta otwiera kontekstową sprawę rady: konkretny manifest, towar, ilość, wartość zakupu i odbiorca. Otwarcie obniża poparcie ludu o 2 i zwiększa miejscowe niepokoje o 4. Limit jednej nowej delegacji z prowincji na dziesięć tur ogranicza powtarzanie spraw.
- Trzy odpowiedzi: dodatkowe odszkodowanie równe zaokrąglonej w górę wartości zakupu, obietnica zastępczej dostawy albo bezpłatna odmowa. Każda ma jawne skutki społeczne lub miejscowe. Odszkodowanie nie odtwarza towaru i nie jest zwrotem od zagranicznego sprzedawcy.
- Obietnica wskazuje towar, ilość, miasto i termin do fazy rynku tury decyzji +8. Rozlicza rzeczywiste zagraniczne przyjazdy, również wcześniej wysłanych karawan i kilka dostaw częściowych. Sam zakup, zapasy, produkcja, wcześniejsze przyjazdy oraz inny odbiorca lub towar nie wystarczają.
- Dotrzymanie daje lud +6, stabilność +2, prestiż +5 oraz miejscowy dobrobyt +8 i niepokoje -6. Złamanie daje lud -10, stabilność -4, prestiż -5 (minimum 0), a we własnym mieście dobrobyt -6 i niepokoje +10. Wynik i zaliczone manifesty pozostają w historii. Skutki rozliczają się raz.
- Przyjazd w ostatniej dozwolonej fazie może jeszcze spełnić obietnicę; późniejszy nie zmienia porażki. Utrata miasta zamyka delegację lub łamie podjętą obietnicę, bez nakładania miejscowej kary na nowego właściciela. Wygaśnięcie kontraktu nie usuwa opłaconych ładunków ani zobowiązania rady.
- Nie można utrzymywać dwóch równoległych obietnic tego samego towaru dla jednego miasta ani wielokrotnie zaliczyć manifestu. Delegacje nie zastępują już otwartej sprawy rady; po jej zamknięciu nie muszą czekać na turę podzielną przez pięć.
- Panel „Sprawy kupców i obietnice” pokazuje oczekujące delegacje, postęp, termin, zaliczone transporty i wynik. Pozwala otworzyć właściwy dialog i przejść do rynku miasta. Interfejs zgłasza rozliczenie obietnicy, a kronika zapisuje decyzję i konsekwencję.
- Zapis v8 zachowuje kontekst dialogu i cały przebieg zobowiązania. Migracja v7 zachowuje karawany bez fikcyjnych spraw za dawne straty; nadpisanie tworzy `.v7.bak`. Odczyt v1–v6 nadal działa.
- Osobne wydanie `wydanie-0.8/`; poprzednie katalogi, zapisy i paczki pozostają zachowane. Oprawa nadal jest mapą 2D; etap rozwija jeden wątek kupiecki, bez samodzielnych przedsiębiorstw i nowej klasy społecznej.

## 0.7 — Droga ma swoją cenę · 22 października 2026

- Zagraniczne kontrakty wysyłają fizyczne karawany zamiast dostarczać towary natychmiast. Nabywca płaci przy załadunku; ładunek pozostaje poza magazynami do przybycia. Zużycie, podatki i ceny odbiorcy reagują dopiero na realny przyjazd.
- Jedna karawana w drodze na umowę; limit 1–6 jednostek oznacza ładunek pojedynczej karawany. Długość podróży ogranicza częstotliwość dostaw. Wygaśnięcie i rozwiązanie umowy nie zawracają wcześniej opłaconych transportów.
- Ruch po heksach: 3 punkty na fazę rynku, drogi 0,5, trudny teren 2, zwykły 1. Podgląd czasu uwzględnia niewykorzystane punkty, które nie przechodzą między turami. Szlak przeliczany jest przy zmianach mapy i dyplomacji.
- Tranzyt przez trzecie państwo wymaga jego pokojowych traktatów handlowych z obiema stronami i braku jego embarga. Niedostępna trasa blokuje podpis umowy oraz nowe załadunki.
- Wojska państwa trzeciego walczącego z którąkolwiek ze stron mogą przechwycić nieosłonięty transport na swojej lub sąsiedniej pozycji. Oddział nabywcy lub sprzedawcy w promieniu jednego pola osłania przed sąsiednim zagrożeniem; bezpośrednia okupacja pola przechwytuje zawsze. Karawany omijają niebezpieczne heksy.
- Embargo, zerwany traktat, wojna stron i brak drogi zatrzymują karawanę. Po trzech kolejnych fazach postoju kupcy porzucają ładunek; zmiana właściciela miasta umowy także oznacza utratę. Nabywca ponosi stratę bez zwrotu płatności. Przechwycenie nie tworzy złota dla napastnika.
- Raport „Karawany i trasy”: manifesty, kwoty, pozycje, osłona, ostrzeżenia, postoje i prognoza przybycia. Mapa pokazuje wozy oraz śledzoną trasę na odkrytym terenie. Przycisk pozwala zakończyć śledzenie. Zatrzymanie i utrata wywołują komunikat; dostarczenie i utrata trafiają do kroniki.
- Zaktualizowano formularz kontraktu, bilans towarów, opis embarga i skutki dialogu rady. Model nie dodaje losowych rozbójników, automatycznego podążania eskorty ani flot.
- Zapis v7 przechowuje ładunki w drodze i ich historię. Migracja v6 zachowuje wykonane płatności i magazyny bez fikcyjnych karawan; nadpisanie tworzy `.v6.bak`. Odczyt v1–v5 nadal działa.
- Osobne wydanie `wydanie-0.7/`; wcześniejsze wydania i zapisy pozostają zachowane. Grafika nadal jest mapą 2D.

## 0.6 — Cena dobrobytu · 22 października 2026

- Zapasy żelaza, soli i barwników w każdym mieście. Pracujące pola wytwarzają towary; współdzielone pole przypada tylko jednemu miastu. Blokada wojskowa wstrzymuje wydobycie.
- Regionalny popyt zależny od ludności oraz warsztatów, fabryk i domów bankierskich. Ceny reagują na zapasy, nową produkcję i popyt, z jawną formułą. Połączone drogami miasta mają wspólną cenę i dzielą realną nadwyżkę.
- Bilans towarów pokazuje zapas początkowy, produkcję, dostawy krajowe i zagraniczne, eksport, zużycie, utratę i zapas końcowy. Nie tworzy towarów podczas przesyłania.
- Pełne zaopatrzenie w żelazo chroni produkcję przed karą do 10%; zużycie soli i barwników zmienia cel dobrobytu. Dostęp do zasobu w regionie nie przyznaje już samoczynnie premii dobrobytu ani towarów zagranicznych.
- Skład towarowy za 150 produkcji po odkryciu Monety dodaje 100 miejsc na każdy towar. Podstawowa pojemność to 50, targowisko dodaje 25. Nadwyżka ponad pojemność przepada i jest wykazana w bilansie.
- Kontrakty importowe i eksportowe na 12 tur, z limitem 1–6 jednostek na turę. Koszt przygotowania 15 złota; maksymalnie trzy aktywne umowy na państwo. Targowiska, technologia, pokojowy traktat i bezpieczne połączenia ze stolicami warunkują dostawy.
- Cena kontraktu wynosi 115% aktualnej ceny sprzedawcy. Realne towary i złoto zmieniają strony; dostawa jest ograniczana zasobami i budżetem. Umowa nie blokuje ceny ani środków i może dostarczyć tylko część ilości.
- Polityka „Najpierw mieszkańcy” pozostawia rezerwę krajową równą jednej turze popytu. Swobodny eksport pozwala wywieźć także tę rezerwę. Embargo wstrzymuje obie strony wymiany zagranicznej. Zmiana kosztuje 30 złota i blokuje kolejną na osiem tur.
- Wojna, zerwanie traktatu, embargo i blokada wstrzymują umowę bez zatrzymania jej czasu. Przywrócenie warunków wznawia dostawy. Przejęcie miasta rozwiązuje jego kontrakt przy rozliczeniu. Możliwe samodzielne rozwiązanie umowy.
- Podatki rynku zależne od zużycia i stawki podatkowej państwa; płatności zagraniczne są księgowane osobno i nie podlegają mnożnikowi trudności. Budżet oddziela ostatnie podatki od prognozy kontraktów.
- AI może zaopatrywać własne miasta kontraktami z innymi AI i budować składy przy nadwyżkach, zachowując potrzeby mieszkaniowe i wojskowe. Umowy Marchii wymagają decyzji gracza.
- Nowy panel „Rynek”, szczegółowy bilans miasta, formularz kontraktu, polityka handlowa i historia dostaw. Nowa sprawa rady „Komu służy otwarty targ?” wiąże regulację handlu z poparciem i skarbem.
- Zapis v6 zachowuje towary, ceny, podatki, politykę i kontrakty. Wczytuje v1–v5, uruchamiając nowy rynek z pustymi magazynami i zachowując dotychczasową historię. Nadpisanie v5 tworzy `.v5.bak`.
- Osobne wydanie `wydanie-0.6/`; poprzednie wersje i zapisy są zachowane. Grafika pozostaje 2D; nie ma jeszcze ruchomych karawan, całych tras międzynarodowych ani samodzielnych agentów kupieckich.

## 0.5 — Cena pokoju · 22 października 2026

- Wojny otrzymują oddzielną historię: początek, inicjatora, ogłoszony powód i cel, kontrolę miast na początku konfliktu, straty w walkach oraz końcowe rozliczenie. Podbój i wyzwolenie wskazują konkretne miasto, zadośćuczynienie wymaga uzyskania przynajmniej 80 złota reparacji.
- Jawny bilans wojny uwzględnia zmiany kontroli miast, obrażenia wojsk i umocnień, siłę armii oraz różnicę wyczerpania. Odzyskanie miasta odwraca jego składnik terytorialny. Bilans jest ograniczony do -100…100 i wpływa na negocjacje.
- Rokowania pozwalają żądać lub zaoferować 100/200 złota, zaproponować pokój bez reparacji oraz zwrócić jedno zajęte miasto jego założycielom. Ostatnie miasto państwa nie podlega zwrotowi. Pozostałe granice pozostają pod obecną kontrolą.
- Poselstwo kosztuje 20 złota i podróżuje dwie tury. W tym czasie wojna trwa. Zaufanie, sytuacja wojskowa, wyczerpanie, czas trwania wojny oraz cena traktatu wpływają na jawną ocenę odpowiedzi, z progiem 50.
- Odrzucona propozycja może przynieść kontrofertę. AI również wysyła własne warunki przy wyczerpaniu lub niekorzystnej sytuacji. Nie kończy już wojny z graczem jednostronnie: propozycję należy przyjąć, odrzucić albo pozostawić do wygaśnięcia po czterech turach od przybycia.
- Traktat jest rozliczany w całości po ponownym sprawdzeniu środków i właściciela miasta. Pieniądze przechodzą między skarbcami dopiero przy ratyfikacji. Zmiana sytuacji może anulować warunki; opłata podróży pozostaje wydana.
- Pokój daje 12 tur rozejmu, zmniejsza wyczerpanie obu stron o 15 i krzywdy o 20. Handel wymaga odrębnego wznowienia. Oddziały obu państw wycofują się z obcego terytorium; wspólna ewakuacja nie blokuje się nawzajem. Pokojowy zwrot miasta zachowuje zdrowie, produkcję i realizowany projekt.
- Spełnienie celu wojennego przy zakończeniu daje inicjatorowi prestiż +12 i lud +3; porzucenie celu kosztuje prestiż 5 oraz lud 3. Upadek państwa również zamyka jego wojny i oczekujące rokowania.
- Nowa sprawa rady „Kiedy milkną miecze” dotyczy odbudowy, pamięci ofiar i roli zwycięskiej armii. Wybór odbudowy zapisuje odroczony skutek ekonomiczny oraz społeczny.
- Okna przebiegu wojny, rokowań i odpowiedzi posłom pokazują warunki, składniki oceny, terminy i konsekwencje. Dyplomacja zachowuje wynik ostatniej wojny. Wczytanie przypomina o oczekującej odpowiedzi.
- Zapis v5 zachowuje wojny, ogłoszone cele, straty, historię ofert i kontrofert oraz ratyfikowane traktaty. Odczytuje v1–v4; dawne wojny zaczynają rejestr od chwili migracji, bez dopisywania nieznanych celów lub dawnych strat. Nadpisanie zapisu v4 tworzy `.v4.bak`.
- Osobne wydanie `wydanie-0.5/`, rozszerzone specyfikacje silnika, kontrole WPF i scenariusze kampanii. Poprzednie wydania i zapisy pozostają zachowane. Dyplomacja jest nadal dwustronna; nie ma wielostronnych konferencji, sojuszniczych frontów ani pełnej wymiany dowolnych terytoriów.

## 0.4 — Słowo i władza · 22 października 2026

- Kierunkowa pamięć krzywd między państwami. Agresja, zajmowanie miast i niszczenie dróg tworzą roszczenia po stronie pokrzywdzonego. Krzywdy maleją o 1 co osiem tur.
- Rada wojenna pokazuje trzy powody wojny i dokładne koszty przed rozkazem. Podbój kosztuje więcej poparcia i zaufanie pozostałych państw; zadośćuczynienie zużywa 30 krzywd, a wyzwolenie wymaga zajętego miasta własnych założycieli. Rozejm blokuje wszystkie powody wojny.
- Poselstwa z żądaniem 80 złota odszkodowania lub propozycją piętnastoturowej nieagresji. Podróż kosztuje 20 złota; odpowiedź przychodzi po dwóch turach. Kolejne warunki do tego samego państwa można wysłać po dziesięciu turach.
- Jawna ocena odpowiedzi uwzględnia zaufanie, względną siłę armii, krzywdy, wyczerpanie i nastawienie Kharów. Wynik rozstrzyga sytuacja po podróży, bez ukrytego losowania. Brak środków uniemożliwia wypłatę odszkodowania. Dar i rozwój armii mogą zmienić ocenę przed odpowiedzią.
- Przyjęte warunki, odmowy i anulowane wojną poselstwa trafiają do kroniki. Powrót posłów otwiera dyplomację. Wszczęcie nieuzasadnionej wojny przez AI także wywołuje krzywdy i reakcję pozostałych państw.
- Miasta pamiętają założycieli, lojalność, ustrój i okres oporu po podboju. Cel lojalności ma jawne składniki, a zmiana nie przekracza 3 punktów na turę. Lojalność wpływa na gospodarkę prowincji.
- Wspólna administracja, autonomia i zarząd wojskowy. Zmiana kosztuje 40 złota, z odstępem ośmiu tur. Autonomia zwiększa lojalność i kulturę kosztem wpływów. Zarząd wojskowy wymaga prawdziwego garnizonu oraz ogranicza produkcję i naukę.
- Podbite miasto z lojalnością poniżej 20 przez trzy kolejne rozliczenia wraca do państwa założycieli. Dwie pierwsze tury dają ostrzeżenia, a poprawa lojalności zeruje licznik. Granice i okoliczne drogi zmieniają właściciela, garnizon wycofuje się do własnego miasta; brak schronienia oznacza rozwiązanie oddziału.
- Nowa lokalna sprawa rady „Czyje prawa, czyja ziemia?” wiąże wybór odpowiedzi z ustrojem, lojalnością i poparciem społecznym. AI może ustanawiać autonomię w niespokojnych prowincjach.
- Zapis v4 obejmuje oczekujące odpowiedzi, krzywdy, założycieli, okupację i postęp buntu. Odczytuje v1–v3 i przy nadpisaniu zachowuje oryginalny plik. Historycznym miastom nadaje obecnego właściciela jako założyciela, ponieważ dawny format nie znał pochodzenia.
- Naprawione centrowanie mapy po uruchomieniu kolejnej kampanii i wczytaniu stanu. Kamera jest ustawiana po ułożeniu interfejsu. Rozszerzone testy sprawdzają widoczność stolicy, przyciski negocjacji, rady wojennej i ustroju oraz zgodność kontynuacji.
- Osobne wydanie `wydanie-0.4/`; poprzednie wydania i paczki pozostają zachowane. Grafika nadal korzysta z mapy 2D; cele wojenne, pokój i negocjacje są uproszczonym etapem rozwoju.

## 0.3 — Życie prowincji · 22 października 2026

- Każde miasto ma własny kierunek rozwoju, dobrobyt, potrzeby mieszkaniowe i pojemność opieki. Niezaspokojone potrzeby podnoszą niepokoje; dobrobyt zmienia gospodarkę, zbliżając się stopniowo do celu.
- Cztery kierunki rozwoju: równowaga, rolnictwo, rzemiosło i nauka. Zmiana kosztuje 15 złota, jest dostępna raz na turę i ma konkretne korzyści oraz koszty.
- Dzielnica mieszkalna daje 5 miejsc, lecznica 6 opieki. Akwedukt wspiera teraz również potrzeby mieszkańców. Gracz może przyznać pomoc żywnościową za 50 złota, raz na pięć tur w danej prowincji.
- Bezpieczne sieci drogowe tworzą regionalne rynki, także poza siecią stolicy. Połączone miasta dzielą się nadwyżką żywności po utrzymaniu ludności oraz dostępem do soli i barwników. Dostawy żywności mają limit 6 na miasto i turę; najpilniejsze niedobory mają pierwszeństwo. Import zmniejsza nadwyżkę dawcy i nie tworzy żywności.
- Blokada wojskowa przerywa również wymianę cywilną. Podgląd prowincji pokazuje miejscowy bilans, import, eksport i dostępne towary.
- Rodziny mogą migrować między połączonymi miastami własnego państwa. Muszą znaleźć wyższy dobrobyt, wolne mieszkania, opiekę i nadwyżkę żywności. Przeniesienie jednego mieszkańca zachowuje łączną populację. Przeprowadzki są ograniczone do jednego ruchu na państwo co cztery tury i mają odstęp ośmiu tur dla uczestniczącego miasta.
- Głód najpierw zwiększa niepokoje; co druga tura nieprzerwanego głodu zmniejsza ludność. Eksport/import jest rozliczany przed głodem i wzrostem miast.
- Trzy nowe sprawy rady dotyczą konkretnej prowincji: pustych spichlerzy, braku mieszkań i przyjęcia rodzin. Każdą można rozstrzygnąć osobno w innych miastach. Miejscowe prawa są trwałe i pozostają przy mieście.
- Karta mieszkaniowa, budownictwo cechowe, integracja, praca za dług oraz rada wspólnot mają różne skutki dla mieszkań, produkcji, opłat, kultury i kolejnych migracji. Rada pokazuje dokładne zmiany lokalne obok kosztów ogólnopaństwowych.
- Panel „Ludność”, okno zarządzania prowincją i nakładka mapy pokazują dobrobyt, żywność oraz zapisane migracje. Kierunki przepływu i ich opisy są oddzielone, aby import i wyjazdy były czytelne jednocześnie.
- AI reaguje na głód zmianą priorytetu i pomocą, a przy niedoborze mieszkań lub opieki wybiera odpowiednie budynki.
- Zapis v3 przechowuje lokalne prawa, priorytety, potrzeby, zdarzenia i historię migracji. Wczytuje v1 i v2, zachowując drogi i rozkazy; nowe pola otrzymują bezpieczne wartości początkowe. Nadpisanie starszego pliku zachowuje kopię `.v1.bak` lub `.v2.bak`.
- Aplikacja przekazuje kod błędu procesu z WPF do skryptu weryfikacji, dzięki czemu awaria testu interfejsu nie może wyglądać jak udane uruchomienie.

Wydanie znajduje się w `wydanie-0.3/`; główny `Uruchom.cmd` wybiera je przed poprzednimi wersjami. Starsze katalogi i paczki pozostają zachowane. Zmiany reguł wpływają na dalszy przebieg starych kampanii. Oprawa nadal jest mapą 2D, wymiana nie ma jeszcze cen rynkowych ani ruchomych karawan, a migracje dotyczą własnego państwa.

Proponowana nazwa commita:

```text
feat: add provincial needs, food trade, and population migration
```

## 0.2 — Szlaki i dostawy · 21 października 2026

- Budowa dróg na własnej i neutralnej, odkrytej ziemi po odkryciu Garncarstwa. Cena 25 złota, utrzymanie 0,15 złota za odcinek i turę. Droga może współistnieć z ulepszeniem pola.
- Jednym rozkazem można zbudować wycenione połączenie miasta z ośrodkiem administracji. Brak pieniędzy nie powoduje budowy części trasy. Istniejące odcinki są wykorzystywane ponownie.
- Bezpieczne połączenia dają prowincjom +3 bazowego złota i +1 nauki, przed podatkami i modyfikatorami wydajności. Stolicą pozostaje najstarsze zachowane miasto.
- Drogi obniżają koszt ruchu między połączonymi odcinkami i miastami do 0,5 punktu. Pełny zasięg ruchu jest podświetlany; najechanie na cel pokazuje trasę, a rozkaz marszu — jego dalszy przebieg.
- Dostawy przechodzą przez sieć dostępnego terenu. Omijają wodę i góry; uwzględniają lasy, wzgórza, drogi, zimę i granice. Podstawowy zasięg kosztowy to 6, a Szlaki handlowe i Inżynieria zwiększają go po 2.
- Wrogie oddziały odcinają własne pole i sąsiednie heksy. Własna piechota, kawaleria, łucznicy lub katapulty osłaniają zajmowane pole przed sąsiednią strefą wroga. Odcięte połączenie traci premię miejską. Zniszczenie wrogiej drogi kosztuje 1 punkt ruchu i daje 20 złota.
- Rozkaz marszu na kilka tur wykonuje dostępny odcinek od razu i wraca po kolejnych turach. Zablokowany marsz zostaje anulowany; samoczynnie nie rozpoczyna bitwy. Można go wycofać przyciskiem. Trasy ograniczają się do odkrytych pól.
- Podgląd walki pokazuje siłę obu stron, obrażenia, kontratak, pozostałe zdrowie i możliwość przejęcia miasta. Ten sam model oblicza wynik faktycznej walki. PPM otwiera podgląd, atak wymaga osobnego kliknięcia.
- Nowy panel „Szlaki” zestawia przychody, utrzymanie armii, administracji i dróg. Nakładka mapy rozróżnia dostawy, ich brak oraz blokadę.
- Nowa sprawa rady: prawo wspólnego traktu. Wybór wolnego przejazdu, myta lub patroli trwale zmienia naukę, dochody albo zasięg dostaw. Sprawa pojawia się po 10. turze, gdy istnieją co najmniej cztery odcinki i dwa bezpiecznie połączone miasta.
- AI buduje połączenia, wykorzystuje punkty ruchu na drogach i kieruje osłabione, odcięte wojska w stronę własnych miast.
- Format zapisu v2 przechowuje drogi, ich opiekunów, połówkowe punkty ruchu i cele marszu. Wczytuje v1 z domyślnymi pustymi drogami i rozkazami. Nadpisanie poprawnego zapisu v1 zachowuje jego kopię `.v1.bak`.
- Ruch bada również ruiny na polach pośrednich, a nie wyłącznie na końcu drogi. Interfejs nawigacji przewija się na mniejszych oknach.

Wersja 0.1 pozostaje w `wydanie/`, a 0.2 w `wydanie-0.2/`. Główny `Uruchom.cmd` wybiera nowszą wersję. Nowe zasady zmieniają przebieg starych kampanii po wczytaniu; migracja zachowuje stan i historię, ale nie obiecuje identycznego dalszego przebiegu jak w starym silniku.

Oprawa pozostaje mapą 2D. Nie dodano jeszcze rendereru 3D, limitów ładowności dostaw, magazynów frontowych ani transportu morskiego. Nie przeprowadzono wielogodzinnych testów balansu z ludźmi.
