# Oprawa i pochodzenie plików

## Ilustracja doliny

Plik projektu: `src/Crown.Desktop/Assets/valley.png`. Używany jako tło menu, wbudowany w zasoby WPF. Powstał przy użyciu wbudowanego narzędzia **image_gen**, w trybie umiejętności **imagegen**, 21 października 2026 r. Jest koncepcją wizualną świata, nie zrzutem rozgrywki.

Pełny prompt:

> Use case: historical-scene. Asset type: cinematic background artwork for an original historically inspired grand strategy game for PC titled Korony i Pogranicza. No text in the artwork. Wide panoramic 16:9 composition, realistic aerial view from a rocky hill over a vast river valley, a small early medieval stone and timber city with terracotta roofs on the river bend, farmland, roads, dense forests, distant snowy mountains, two sailing ships on water. Archaeologically plausible, fictional European-inspired setting, no fantasy magic, no recognizable real landmarks. Photorealistic terrain and architectural materials, atmospheric perspective, soft morning light through clouds, muted olive green and teal landscape, warm golden highlights. Foreground left is dark rock and weathered grass suitable for a menu title overlay; city sits right of center, mountains across upper third. High quality realistic game concept art, sophisticated subdued colors, believable large scale. Avoid cartoon, UI, lettering, watermark, logos.

## Mapa i interfejs

Od 0.20 główna mapa korzysta z rzeczywistej sceny WPF 3D. Atlas terenu i ciągłe wysokości powstają w `Terrain3D.cs` z deterministycznego szumu i danych odkrytych pól. `SceneGeometry.cs`, `WorldScene3D.cs` oraz `WorldModels3D.cs` tworzą siatki terenu, wody, drzew, zabudowy, wojsk i karawan. Geometria oraz materiały są autorskim kodem C#; nie pobierano zewnętrznych modeli ani tekstur. To uproszczona oprawa proceduralna bez PBR i animacji. Etykiety i minimapa są elementami interfejsu 2D. Fonty systemowe: Segoe UI i Georgia; nie są dołączane do dystrybucji. Nie wykorzystano zasobów, kodu, nazw państw ani dialogów z serii Civilization.
