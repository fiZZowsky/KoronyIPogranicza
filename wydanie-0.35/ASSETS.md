# Oprawa i pochodzenie plików

## Ilustracja doliny

Plik projektu: `src/Crown.Desktop/Assets/valley.png`. Używany jako tło menu, wbudowany w zasoby WPF. Powstał przy użyciu wbudowanego narzędzia **image_gen**, w trybie umiejętności **imagegen**, 21 października 2026 r. Jest koncepcją wizualną świata, nie zrzutem rozgrywki.

Pełny prompt:

> Use case: historical-scene. Asset type: cinematic background artwork for an original historically inspired grand strategy game for PC titled Korony i Pogranicza. No text in the artwork. Wide panoramic 16:9 composition, realistic aerial view from a rocky hill over a vast river valley, a small early medieval stone and timber city with terracotta roofs on the river bend, farmland, roads, dense forests, distant snowy mountains, two sailing ships on water. Archaeologically plausible, fictional European-inspired setting, no fantasy magic, no recognizable real landmarks. Photorealistic terrain and architectural materials, atmospheric perspective, soft morning light through clouds, muted olive green and teal landscape, warm golden highlights. Foreground left is dark rock and weathered grass suitable for a menu title overlay; city sits right of center, mountains across upper third. High quality realistic game concept art, sophisticated subdued colors, believable large scale. Avoid cartoon, UI, lettering, watermark, logos.

## Mapa i interfejs

Wydanie 0.31 dodaje mały znacznik skrzyń na załadowanym okręcie i wektorowe obrysy lądowania. Używa dotychczasowych materiałów; nie generowano bitmap ani nie pobierano modeli, fontów lub nagrań. Pasażerowie nie mają dodatkowych modeli lub animacji na pokładzie. Zrzuty --ui-smoke-transport przedstawiają rzeczywistą aplikację na przygotowanym akwenie, z przyznaną wiedzą, okrętem i oddziałami. Załadunek, rejs, wyładunek i kolonizacja wykonują normalne operacje oraz tury. Czteromiejscowy manifest do testu małego okna jest przygotowanym przypadkiem, nie naturalną historią badań.

Wydanie 0.30 dodaje autorską geometrię proceduralną portów i okrętów w NavalModels3D.cs: profilowany kadłub, pokład, burty, maszty, żagle, wiosła, działa, komin, mostek, sensory i przyszły moduł energetyczny. Dziewięć modeli reguł korzysta z kilku rodzin sylwetek. Ponownie użyto istniejących materiałów i tekstur 0.21. Nie generowano bitmap ani nie pobierano modeli, fontów lub nagrań. Zrzuty --ui-smoke-navy pochodzą z rzeczywistego programu na ręcznie przygotowanym akwenie z przyznaną wiedzą. Postęp portu i pierwszego okrętu jest ustawiany tuż przed ukończeniem, które wykonuje rzeczywista tura; katalog później otrzymuje przygotowane modele historyczne i przyszłe. Nie są to fotografie ani zapis naturalnej wielogodzinnej kampanii. Kadłuby i załogi nie mają animacji szkieletowej.

Wydanie 0.29 dodaje natywny panel WPF oceny zagrożeń wojskowych. Nie powstają nowe bitmapy, modele, fonty ani dźwięki; oprawa 3D korzysta z dotychczasowych zasobów. Zrzuty --ui-smoke-military-ai pochodzą z rzeczywistego programu na przygotowanym scenariuszu z jednostkami ustawionymi tak, aby sprawdzić przewagę, widoczność i brak dostaw. To diagnostyka interfejsu, nie naturalnie rozegrana kampania.

Wydanie 0.28 dodaje autorską proceduralną geometrię pięciu dzielnic, rusztowań i ruin w DistrictModels3D.cs. Wykorzystuje istniejące materiały i tekstury 0.21. Nie pobierano modeli ani nie generowano nowych bitmap. Podświetlenia i nazwy są elementami WPF. Zrzuty --ui-smoke-districts pochodzą z rzeczywistego programu na przygotowanej mapie z przyznanymi technologiami i środkami; część postępu jest ustawiana w scenariuszu dla kontroli ukończenia, pięciu modeli i grabieży. Oddzielne kontrole sprawdzają rzeczywisty postęp po końcu tury. Są to testy mechanik i wyglądu, nie zapis naturalnie rozegranej kampanii.

Wydanie 0.27 dodaje natywne panele WPF ustawień i menu gry. Nie dodano nowych bitmap, modeli ani fontów. InterfaceAudio generuje krótki monofoniczny sygnał PCM 22 050 Hz z sinusoidy i obwiedni, zapisując odtwarzalną pamięć podręczną WAV przy pierwszym odtworzeniu. Nie jest to nagranie zewnętrzne ani utwór muzyczny. Odtwarzanie używa MediaPlayer WPF; diagnostyka pozostaje bezgłośna. Zrzuty nowych paneli pochodzą z rzeczywistego programu.

Wydanie 0.26 dodaje z kodu projektu uproszczone warianty pni i koron drzew do dalekiego widoku. Wykorzystują te same tekstury 0.21 i nie wchodzą do geometrii WPF używanej przy wybieraniu pól. Podział partii GPU i kontrola widoczności nie tworzą nowych zasobów graficznych. Nie generowano ani nie pobierano bitmap, modeli lub fontów. Zrzuty --ui-smoke-rendering pochodzą z rzeczywistego renderera na odsłoniętym scenariuszu diagnostycznym z zagęszczonym pasem lasów i przyznanymi technologiami. Statystyki są odczytem bieżącej klatki, nie ilustracją wyników.

Wydanie 0.25 dodaje autorską geometrię proceduralną ludzi, koni, pni, korzeni, traw, kłosów, kwiatów, rud i kryształów. Sześć piktogramów zasobów to oryginalne ścieżki wektorowe WPF w ResourcePresentation.cs, bez zależności od znaków specjalnego fontu. Materiały korzystają z dotychczasowego atlasu i roślinności 0.21 oraz szumu w shaderze; nie pobierano modeli ani nie generowano nowych bitmap. Zrzuty --ui-smoke-models pochodzą z rzeczywistego renderera na przygotowanej mapie diagnostycznej z odsłoniętymi zasobami i przyznanymi technologiami. Modele postaci mają statyczne pozy.

Wydanie 0.24 realizuje sezonowe barwy i śnieg w shaderze oraz deszcz i płatki jako proceduralną geometrię Direct3D. Niebo również powstaje w shaderze. Ponownie wykorzystano atlas i tekstury roślin 0.21; nie generowano ani nie pobierano bitmap, fontów lub modeli. Zrzuty czterech pór roku przedstawiają rzeczywisty program na przygotowanej mapie diagnostycznej, z przyznaną wiedzą i ustawionymi turami sezonów.

Wydanie 0.23 dodaje proceduralne koryta, siatki wody z mapowaniem nurtu, żwirowe brzegi, trzciny, kamienie, mosty i dodatkową bramę miejską. Wszystkie te elementy powstają z kodu projektu. Ponownie wykorzystano atlas i tekstury roślin z 0.21; nie generowano nowych bitmap ani nie pobierano modeli. Scenariusz diagnostyczny rzek ma ręcznie przygotowane odkryte odcinki i drogę, a jego zrzuty pochodzą z rzeczywistego programu.

Wydanie 0.22 rozwija geometrię terenu i historycznych miast oraz shader wody. Grzbiety, ciągły daleki horyzont, parcele, stragany, kurtyny i brama łukowa powstają z kodu projektu. Nie wygenerowano nowych bitmap i nie pobrano modeli ani tekstur z Internetu. Renderer nadal używa trzech poniższych zasobów 0.21 pod ich pierwotnymi nazwami. Zrzuty diagnostyczne są obrazami rzeczywistego programu, z przygotowanym jeziorem i odsłoniętą mapą; nie są ilustracjami AI.

Od 0.21 scenę renderuje Direct3D 11 poprzez autorski kod C# i HLSL. WPF zachowuje geometrię dla trafiania promieniem i trybu zgodności. Materiały wykorzystują opisane poniżej tekstury AI. Geometria terenu, drzew, zabudowy i jednostek powstaje proceduralnie. Etykiety i minimapa są elementami interfejsu 2D. Fonty systemowe Segoe UI i Georgia nie są dołączane do dystrybucji. Nie wykorzystano zasobów, kodu, nazw państw ani dialogów z serii Civilization.

## Tekstury wydania 0.21

Wygenerowano 23 października 2026 r. wbudowanym narzędziem image_gen, przy użyciu umiejętności imagegen. Pliki są zapisane w src/Crown.Desktop/Assets i wbudowane w program. Mają fotograficzny wygląd, lecz są syntetycznymi obrazami AI, nie skanami rzeczywistych powierzchni. Atlas jest albedo; parametry chropowatości i metalu ustala shader. Tekstury roślin zachowują prawdziwy kanał alfa. Nie dodano zewnętrznych modeli 3D. Wszystkie poniższe zasoby są faktycznie używane przez renderer.

### surface-atlas-0.21.png

Pełny prompt (wbudowany tryb narzędzia):

> Use case: photorealistic-natural. Asset type: production-ready physically based 3D game material ALBEDO atlas, not a scene, no text. Primary request: one perfectly square image divided into exactly 4 equal square quadrants with no borders and no gaps. TOP LEFT: dense natural meadow grass, olive and straw green fine short blades with sparse dry stems, straight down macro photograph, even diffuse overcast light. TOP RIGHT: weathered grey limestone cliff surface, crevices and lichen, flat orthographic surface scan. BOTTOM LEFT: rough historical limestone masonry with small irregular beige-grey stones, fine mortar seams, a vertical wall viewed straight on. BOTTOM RIGHT: old terracotta roof tiles, closely packed overlapping small burnt clay tiles in rows viewed orthographically straight on. All four quadrants must be convincingly photorealistic scanned surfaces with very fine irregular detail, true neutral material colors, no directional baked lighting, no cast shadows, no perspective, no objects, no scenery, no labels, no border, no watermark. Each quadrant fills its square to its edges. Historical European landscape materials to match a realistic alpine river valley strategy game. Resolution as high as possible.

### oak-foliage-0.21.png

Pełny prompt (wbudowany tryb narzędzia):

> Use case: photorealistic-natural. Asset type: alpha-cutout foliage texture for a physically lit 3D tree, square image with actual transparent background. Subject: one natural broadleaf oak branch spray forming a loose irregular rounded cluster of hundreds of small detailed olive-green leaves, including fine twigs. Seen straight on, isolated, fully inside canvas with generous transparent margin on all four sides. Foliage must have many realistic small irregular transparent holes between leaves, varied leaf orientations, individual leaf veins, dusty olive and deep forest green colors, a few warm yellow leaves. Neutral diffuse overcast daylight, no harsh baked directional shadows, no ground or pot or tree trunk. Botanical photograph, not illustration, not stylized, no solid background, no glow, no outline, no text or watermark. This texture will be placed on many intersecting cards in a fully 3D tree crown, so the whole branch cluster should be compact and visually dense but clearly contain many separate leaves.

### spruce-foliage-0.21.png

Pełny prompt (wbudowany tryb narzędzia):

> Use case: photorealistic-natural. Asset type: transparent alpha-cutout photograph of one spruce fir branch cluster for a 3D conifer tree foliage card. A single compact fan shaped irregular green spruce bough, many fine needles with visible individual needle silhouettes and branching twigs. Isolated with genuinely transparent background. Fine realistic green needles and thin brown wood, forest green and dusty sage tips. Dense in center but small transparent gaps throughout. Whole cluster comfortably inside square canvas with transparent margin. Flat neutral diffuse daylight with no baked cast shadow. Photographic scanned vegetation, not a drawing or stylized game asset. No tree trunk, no ground, no pot, no text, no border, no watermark.

## Oprawa wybrzeża 0.32

Nowe żagle, olinowanie, relingi, polery, magazyny, beczki, umocnienia i gruz powstają z geometrii C#. Nie pobierano nowych modeli, zdjęć, dźwięków ani tekstur. Shader wykorzystuje istniejący atlas i osobne proceduralne właściwości drewna, płótna, metalu oraz wody. Nie są to skany ani fotogrametria. Obrazy siege-ui pokazują przygotowaną scenę testową z nadanymi technologiami i osłabionym obrońcą, następnie rzeczywisty załadunek, rozkaz rejsu, szturm i przygotowanie przyczółka. Służą weryfikacji, nie dowodzą balansu naturalnej kampanii.

## Magazyny wojskowe 0.33

Budynki, drzwi, dachy, skrzynie, metalowe opaski i proporce magazynów powstają w DepotModels3D z geometrii C#. Materiały wykorzystują istniejący atlas i cieniowanie z poprzednich wydań. Nie pobierano nowych bitmap, modeli ani dźwięków. Grafika pozostaje proceduralna.

Obrazy field-supply-ui pokazują przygotowany teren i przyznaną wiedzę. Test ustawia końcowy postęp rozpoczętej budowy, a potem kończy magazyn w zwykłej turze, wybiera politykę i rzeczywiście wytwarza pakiety. Testowe jednostki pobierają te zapasy przez przyciski gry. Położenie odciętego oddziału i osobny układ napadu są przygotowane dla kontroli przypadków; zużycie, straty, napad, łup i zapis są rzeczywistymi operacjami. Przemysłowy magazyn i duży spis oddziałów są osobnymi ustawieniami do oceny modeli oraz małego okna. To kontrola integracji, nie zapis naturalnej długiej kampanii.

## Warsztaty i cechy 0.34

CraftingModels3D tworzy budynki warsztatów, komin, kowadło, krosno, płótno, skrzynie i proporzec Domu cechów z geometrii C#. Materiały metalu i tkaniny korzystają z istniejącego renderera oraz atlasu. Nie pobierano modeli, bitmap ani dźwięków i nie generowano nowych ilustracji. Są to modele proceduralne, bez animowanych rzemieślników lub fotogrametrii.

Obrazy manufacturing-ui pochodzą z rzeczywistej aplikacji na przygotowanym terenie z przyznanymi technologiami, drogami, zagranicznym odbiorcą oraz początkowym żelazem i barwnikami. Scenariusz przyspiesza końcowy postęp wybranych budów; ukończenie, decyzje, pobranie surowców z połączonego miasta, wytwarzanie, konsumpcja cechów, płatny eksport i dostarczenie karawaną odbywają się przez normalne fazy i przyciski gry. Przerwanie drogi i dodatkowy stan do kontroli małego okna są przygotowanymi przypadkami. To diagnostyka integracji oraz oprawy, nie zapis naturalnej kampanii ani dowód fotorealizmu.

## Oprawa miast i epok 0.35

Wygenerowano 6 listopada 2026 r. wbudowanym narzędziem image_gen w domyślnym trybie umiejętności imagegen, bez zewnętrznego API lub pobierania modeli. Dwa nowe atlasy poniżej skopiowano do projektu i wbudowano w program. To syntetyczne obrazy AI o fotograficznym wyglądzie, nie fotografie, fotogrametria lub pełne skanowane zestawy PBR. Normalne reliefu, chropowatość i metaliczność nadal pochodzą z shadera. Nie zastępują kompletnych modeli budynków, postaci i ich animacji.

Geometria 18 centrów miast, portyków, wież, kopuł, hal, arkologii, anten i pierścieni powstaje w EraArchitecture3D. Elewacje są mapowane na cztery pionowe ściany geometryczne, z oddzielną geometrią dachów i wybranych bliskich detali. WPF używa przyciętych ćwiartek tego samego atlasu. Nie dodano nagrań ani cudzych fontów.

Zrzuty era-identity-ui są obrazami rzeczywistego programu. Scenariusz nadaje początkowy zapas żywności, po czym decyzja, opłaty i ukończenie pierwszego dziedzictwa zachodzą w trzech normalnych turach. Galeria przyznaje technologie kolejnych epok i przygotowuje wiosnę, zachowując tę samą mapę; nie jest naturalnym rozegraniem 18 epok. Wygląd centrum jest dekoracją stylu, a nie dowodem zbudowania zakładów. Dane pomiarów pochodzą z działającego renderera, nie ilustracji AI. Modele nadal są uproszczone, a oprawa nie osiąga pełnego fotorealizmu menu.

### civilization-materials-0.35.png

Plik: `src/Crown.Desktop/Assets/civilization-materials-0.35.png`. Rozmiar: 1254×1254. SHA256: `6320F12ED06487CA5072617DA7474AA8D4271AC213065C2551EB9462712FD905`.

Pełny prompt (wbudowane image_gen, tło nieprzezroczyste):

> Use case: photorealistic-natural. Asset type: production 3D strategy game material ALBEDO atlas, a perfectly square image divided into exactly FOUR EQUAL quadrants, no gutters, no frame, no text. Each quadrant is a close orthographic photograph of a flat surface under completely even diffuse neutral light, no cast shadows, no perspective, no scene. TOP LEFT: realistic rich brown dirt, small grey and ochre gravel embedded in compact earth, tiny dry grass roots, irregular fine natural variation. TOP RIGHT: historical muted red-brown handmade brick masonry with thin grey lime mortar, subtle weathering, believable small brick dimensions and fine porous surfaces. BOTTOM LEFT: grey modern asphalt and worn concrete aggregate, fine stone granules, subtle hairline cracks, no road markings. BOTTOM RIGHT: weathered oak timber planks, natural silver-brown grain and subtle dark seams, no nails or objects. Extremely detailed photographic surface microtexture, physically believable neutral albedo, no baked highlights, no vignette, no ornament, no labels, no watermark. All quadrants fill exactly one quarter of the canvas. Maximum useful texture detail for physically lit historical to futuristic cities and paths. This is a material atlas, not a collage of scenes.

### building-facades-0.35.png

Plik: `src/Crown.Desktop/Assets/building-facades-0.35.png`. Rozmiar: 1254×1254. SHA256: `AC495E5BC4CAEDB546BA3831277965796B00D810797321C628986F59750FD73B`.

Pełny prompt (wbudowane image_gen, tło nieprzezroczyste):

> Use case: photorealistic-natural. Asset type: production ALBEDO texture atlas for realistic 3D game BUILDING FACADES. Square canvas in exactly four equal quadrants without gutters, labels or borders. Each quarter contains an orthographic perfectly front-facing wall facade only, cropped to fill its full quadrant. No roof, sky, ground, people, props, street, perspective or cast shadow. Neutral overcast illumination, no directional baked lighting. TOP LEFT: historically plausible medieval European half-timbered building facade, two storeys, three window bays, ivory lime plaster between dark weathered oak beams, small leaded glazing, fine age and irregular plaster detail, no signage. TOP RIGHT: restrained renaissance or enlightenment town house facade, exactly two storeys and three window bays, warm cream lime plaster, fine stone window surrounds, dark green shutters, subtle weathering, no lettering. BOTTOM LEFT: authentic 19th century industrial workers' brick townhouse facade, two storeys, three window bays, weathered muted red-brown brick and pale lime mortar, fine timber sash windows, neutral weathering and chipped stone sills, no ivy or objects. BOTTOM RIGHT: contemporary elegant curtain wall facade, three storeys and three window bays, neutral lightly reflective blue-grey glass panes, slender matte silver aluminium frames and light grey stone spandrels, no scene reflections, no interior characters. Photographic fine architectural material detail, physically plausible dimensions, completely flat orthographic diffuse albedo photography. Architectural walls are the entire subject, not building photographs with backgrounds. Avoid stylization, cartoon, illustration, text, watermarks, deep shadow, 3D perspective, solid black window holes. Exactly one wall per quadrant, facade aligned with each quarter edges. The result will be sampled on real building geometry with lighting and shadows supplied by a game renderer.
