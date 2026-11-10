## Importowane postacie i konie — 0.38

Wersja 0.38 zastępuje modele anatomiczne budowane z brył **zewnętrznymi siatkami 3D Grab3D, udostępnionymi jako CC0 1.0**. Pochodzenie i warunki: https://grab3d.com/license/ oraz https://grab3d.com/how-we-make-models/. Są to modele publikowane przez Grab3D, nie skany fotogrametryczne ani nasza ręczna rzeźba. Zestaw obejmuje konia, wikinga, rycerza, współczesnego żołnierza, żołnierza przyszłości, cywila i robota. Nie należy utożsamiać oznaczenia High Detail ze skanem lub ukończonym fotorealizmem gry.

| Model | Trójkąty źródła | Blisko | Daleko | Źródło |
|---|---:|---:|---:|---|
| horse | 731120 | 9932 | 1382 | https://grab3d.com/models/animals/horse/ |
| viking | 287867 | 7994 | 1000 | https://grab3d.com/models/characters/viking/ |
| knight | 291910 | 7994 | 990 | https://grab3d.com/models/characters/armored-knight/ |
| modern | 278969 | 7994 | 996 | https://grab3d.com/models/characters/soldier/ |
| soldier | 238850 | 8000 | 1000 | https://grab3d.com/models/characters/sci-fi-soldier/ |
| civilian | 262348 | 8000 | 1000 | https://grab3d.com/models/characters/man-base-character/ |
| robot | 252216 | 7988 | 998 | https://grab3d.com/models/characters/robot/ |

Pliki docelowe: `src/Crown.Desktop/Assets/Actors/`. Dziewięć skompresowanych siatek `.cmesh` (w tym dwie pozycje jeźdźców) i 21 oryginalnych map JPG: `*-base.jpg`, `*-normal.jpg`, `*-orm.jpg`. Runtime dekoduje tekstury do szerokości 1024 i współdzieli je pomiędzy instancjami. GPU wykorzystuje kolor, normalne i szorstkość/metaliczność; prostszy WPF wykorzystuje kolor i geometrię. Żadne pobieranie nie następuje w trakcie gry.

Przygotowanie offline: `artifacts/assets-0.38/bake-actors.mjs`, z użyciem `meshopt_simplifier.mjs` z oficjalnego repozytorium https://github.com/zeux/meshoptimizer/tree/master/js (MIT; zachowano plik `meshoptimizer-LICENSE.md`). Odczyt GLB, interpolacja pozy z istniejącego klipu chodzenia, skinning, normalizacja wysokości, redukcja geometrii z uwzględnieniem UV i normalnych, pakowanie Brotli. Pozycje jeźdźców otrzymały dodatkowe wygięcie dolnej części ciała. Źródłowe JPG zostały wyodrębnione bez retuszu. Nie generowano bitmap narzędziem image_gen w tym etapie.

To **statyczne wypalone pozy**, nie odtwarzanie animacji szkieletowej podczas rozgrywki. Dobór modeli rozróżnia szerokie grupy epok i role; autentyczna garderoba każdej z 18 epok wymaga dalszej produkcji. W szczególności wczesne epoki mają obecnie wspólny model inspirowany historycznie. Prawa i stan symulacji nie zależą od stroju.

Pełne sumy źródeł oraz statystyki redukcji zapisano w `artifacts/assets-0.38/manifest.json`; hash każdego pliku docelowego w `runtime-manifest.json`. Oryginały GLB są danymi roboczymi i nie wchodzą do wydania ani do kontroli wersji; można je odtworzyć z podanych publicznych adresów. Narzędzie przygotowania nie jest zależnością uruchomieniową gry.

