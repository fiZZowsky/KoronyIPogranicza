# Zmiany

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
