# Oprawa i pochodzenie plików

## Ilustracja doliny

Plik projektu: `src/Crown.Desktop/Assets/valley.png`. Używany jako tło menu, wbudowany w zasoby WPF. Powstał przy użyciu wbudowanego narzędzia **image_gen**, w trybie umiejętności **imagegen**, 21 października 2026 r. Jest koncepcją wizualną świata, nie zrzutem rozgrywki.

Pełny prompt:

> Use case: historical-scene. Asset type: cinematic background artwork for an original historically inspired grand strategy game for PC titled Korony i Pogranicza. No text in the artwork. Wide panoramic 16:9 composition, realistic aerial view from a rocky hill over a vast river valley, a small early medieval stone and timber city with terracotta roofs on the river bend, farmland, roads, dense forests, distant snowy mountains, two sailing ships on water. Archaeologically plausible, fictional European-inspired setting, no fantasy magic, no recognizable real landmarks. Photorealistic terrain and architectural materials, atmospheric perspective, soft morning light through clouds, muted olive green and teal landscape, warm golden highlights. Foreground left is dark rock and weathered grass suitable for a menu title overlay; city sits right of center, mountains across upper third. High quality realistic game concept art, sophisticated subdued colors, believable large scale. Avoid cartoon, UI, lettering, watermark, logos.

## Mapa i interfejs

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
