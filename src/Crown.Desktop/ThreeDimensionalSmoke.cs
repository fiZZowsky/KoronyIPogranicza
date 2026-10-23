using Crown.Core;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace Crown.Desktop;

public sealed partial class MainWindow
{
    private async Task ThreeDimensionalSmoke(string folder)
    {
        Directory.CreateDirectory(folder); var checks = new List<string>(); var watch = Stopwatch.StartNew();
        void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); checks.Add("PASS " + message); }
        async Task Capture(string name)
        {
            UpdateLayout(); root.UpdateLayout(); await Task.Delay(120);
            var bitmap = new RenderTargetBitmap((int)root.ActualWidth, (int)root.ActualHeight, 96, 96, PixelFormats.Pbgra32); bitmap.Render(root);
            var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap)); string path = Path.Combine(folder, name + ".png"), temp = path + ".tmp";
            using (var output = File.Create(temp)) encoder.Save(output); File.Move(temp, path, true);
        }
        void ClickCamera(string name)
        {
            var button = Descendants<Button>(map).Single(b => b.Content?.ToString() == name); button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); root.UpdateLayout();
        }
        IEnumerable<MeshGeometry3D> Meshes(Model3D model)
        {
            if (model is GeometryModel3D geometry && geometry.Geometry is MeshGeometry3D mesh) yield return mesh;
            if (model is Model3DGroup group) foreach (var child in group.Children) foreach (var childMesh in Meshes(child)) yield return childMesh;
        }
        GameState EpochFixture(int era)
        {
            var s = World.Create(); Narrative.Resolve(s, 0); foreach (var f in s.Factions) { f.Gold = 10000; f.Research = ""; }
            s.Player.Technologies.UnionWith(EpochCatalog.All.Take(era + 1).SelectMany(e => e.Technologies));
            for (int i = 0; i <= era; i++) s.Player.EpochChoices[i] = 1;
            var home = s.Cities.First(c => c.Owner == 0); home.Population = 12;
            foreach (var t in s.Tiles.Where(t => World.Distance(t, s.Tiles[home.Tile]) <= 9)) t.Explored = true;
            foreach (var c in s.Cities) c.ProductionPaused = true;
            if (era >= 5) home.Buildings.UnionWith(["walls", "market", "granary"]);
            if (era >= 9) home.Buildings.UnionWith(["coal_station", "steel_mill"]);
            if (era >= 13) home.Buildings.UnionWith(["fusion_plant", "solar_farm"]);
            foreach (var t in World.Neighbors(s, home.Tile).Where(t => t.Passable && !s.Cities.Any(c => c.Tile == t.Id)).Take(2)) t.Improvement = Improvement.Farm;
            s.InvalidateLogistics(); return s;
        }
        try
        {
            if (arguments.Contains("--smoke-force-failure")) throw new InvalidOperationException("Intentional 3D diagnostic failure.");
            var s = World.Create(); Narrative.Resolve(s, 0); string original = SaveGame.Serialize(s); Start(s); root.UpdateLayout();
            Check(Descendants<Viewport3D>(map).Count() == 1 && map.Viewport3D.Camera is PerspectiveCamera, "New game mounts one genuine Viewport3D with a perspective camera");
            Check(map.Scene?.TerrainTriangles > 60000 && map.Scene.ObjectTriangles > 0, "Terrain and world objects contain actual depth-tested triangle meshes");
            Check(Meshes(map.Scene!.Terrain).All(m => m.IsFrozen && m.Positions.Count <= 20001), "Static terrain is frozen and split into bounded mesh batches");
            Check(Meshes(map.Scene.Terrain).Concat(Meshes(map.Scene.Objects)).All(m => m.Positions.All(p => double.IsFinite(p.X) && double.IsFinite(p.Y) && double.IsFinite(p.Z)) && m.Normals.All(n => double.IsFinite(n.X) && Math.Abs(n.Length - 1) < .001)), "Mesh positions and smooth normals are finite and valid");
            Check(map.Scene.TreeCount > 0 && map.Scene.CityIds.Contains(s.Cities[0].Id), "Forests and the starting settlement are spatial geometry rather than map symbols");
            Check(map.Scene.UnitIds.Contains(s.Units.First(u => u.Owner == 0).Id), "Own military formations are present in the 3D scene");
            Check(SaveGame.Serialize(state) == original, "Rendering a new campaign leaves simulation state and RNG untouched"); await Capture("3d-new-campaign");
            var capital = s.Cities[0]; var center = Terrain3D.Center(s.Tiles[capital.Tile]); var point = map.Project(Terrain3D.Ground(s, center.X, center.Y, .08));
            Check(point != null && map.Pick(point.Value) == capital.Tile, "A ray through the projected capital hits its actual scene geometry");
            Check(map.Viewport.Contains(MapView.CenterOf(s.Tiles[capital.Tile])), "Perspective minimap footprint includes the centred city");
            int builds = map.SceneBuildCount; double yaw = map.CameraYaw; ClickCamera("↶");
            Check(map.CameraYaw != yaw && map.SceneBuildCount == builds, "Camera rotation uses the WPF control without rebuilding the world");
            var rotated = map.Project(Terrain3D.Ground(s, center.X + 1, center.Y, .1)); ClickCamera("+");
            Check(map.CameraDistance < 13 && rotated != map.Project(Terrain3D.Ground(s, center.X + 1, center.Y, .1)), "Zoom changes perspective projection through its actual control");
            map.Orbit(360, 1000); Check(map.CameraPitch == 78, "Camera elevation is clamped above the world"); map.Orbit(0, -1000); Check(map.CameraPitch == 28, "Camera cannot orbit beneath the terrain");
            map.Zoom(.00001); Check(map.CameraDistance == 5, "Near camera distance is bounded"); map.Zoom(100000); Check(map.CameraDistance == 60, "Far camera distance is bounded");
            map.ResetCamera(); map.PanPixels(80, 30); Check(!map.Camera.LookDirection.Equals(new Vector3D()) && map.Footprint.All(p => double.IsFinite(p.X) && double.IsFinite(p.Y)), "Pan retains a finite world camera and minimap footprint"); map.Center(capital.Tile);
            Check(SaveGame.Serialize(state) == original, "Orbit, pan and zoom do not advance turns or modify the save");
            var destination = World.Neighbors(s, capital.Tile).First(t => t.Passable && !s.Units.Any(u => u.Owner != 0 && u.Tile == t.Id)); destination.Explored = true; destination.Terrain = Terrain.Grassland; destination.Resource = Resource.None; destination.Ruin = false; destination.Improvement = Improvement.None; s.InvalidateLogistics(); Refresh();
            center = Terrain3D.Center(s.Tiles[capital.Tile]); point = map.Project(Terrain3D.Ground(s, center.X, center.Y, .08)); map.SelectScreen(point!.Value);
            Check(selectedTile == capital.Tile && selectedUnit >= 0, "3D screen selection reaches the real city and unit selection handler");
            var unit = s.Units.First(u => u.Id == selectedUnit); center = Terrain3D.Center(destination); point = map.Project(Terrain3D.Ground(s, center.X, center.Y, .05));
            Check(point != null && map.Pick(point.Value) == destination.Id, "Perspective picking identifies an adjacent ground surface"); map.OrderScreen(point!.Value);
            Check(unit.Tile == destination.Id && map.Scene.UnitIds.Contains(unit.Id), "A 3D screen order moves the actual military unit and refreshes its geometry");
            var hidden = s.Tiles.First(t => !t.Explored && World.Distance(t, s.Tiles[capital.Tile]) > 10); double hiddenHeight = Terrain3D.Height(s, Terrain3D.Center(hidden).X, Terrain3D.Center(hidden).Y);
            hidden.Terrain = hidden.Terrain == Terrain.Mountain ? Terrain.Grassland : Terrain.Mountain;
            Check(Terrain3D.Height(s, Terrain3D.Center(hidden).X, Terrain3D.Center(hidden).Y) == hiddenHeight, "Unexplored terrain elevation does not reveal the hidden terrain type");
            var enemy = Engine.AddUnit(s, 1, hidden.Id, UnitKind.Spearmen); Refresh(); Check(!map.Scene.UnitIds.Contains(enemy.Id) && !map.Scene.CityIds.Contains(s.Cities[1].Id), "Fog hides unseen enemy formations and undiscovered cities");
            map.ShowGrid = true; map.ShowSupply = true; map.InvalidateVisual(); Check(map.OverlayTriangles > 0, "Grid, selection, movement range and logistics are terrain-following 3D overlays"); await Capture("3d-logistics");
            map.ShowSupply = false; map.ShowPopulation = true; map.InvalidateVisual(); Check(map.AnnotationCount > 0 && map.OverlayTriangles > 0, "Population overlays and projected city annotations remain available");
            string path = Path.Combine(folder, "three-dimensional-save.crown"); string before = SaveGame.Serialize(state); SaveGame.Write(state, path); LoadPath(path); CloseModal();
            Check(SaveGame.Serialize(state) == before && map.Viewport3D.Camera is PerspectiveCamera, "Existing save format reloads exact gameplay into the 3D renderer");
            var battle = World.CreateLegacy(); Narrative.Resolve(battle, 0); var homeCity = battle.Cities[0]; battle.Units.Clear();
            var fighter = Engine.AddUnit(battle, 0, homeCity.Tile, UnitKind.Spearmen); var enemyTile = World.Neighbors(battle, homeCity.Tile).First(t => t.Passable);
            enemyTile.Explored = true; enemyTile.Terrain = Terrain.Grassland; enemyTile.Resource = Resource.None; enemyTile.Ruin = false;
            var defender = Engine.AddUnit(battle, 1, enemyTile.Id, UnitKind.Spearmen); battle.Relation(0, 1).War = true; WarCouncil.EnsureActive(battle); battle.InvalidateLogistics();
            Start(battle); Select(fighter.Tile); map.Center(fighter.Tile); root.UpdateLayout(); var enemyCenter = Terrain3D.Center(enemyTile); var enemyPoint = map.Project(Terrain3D.Ground(battle, enemyCenter.X, enemyCenter.Y, .06));
            Check(enemyPoint != null && map.Pick(enemyPoint.Value) == enemyTile.Id, "Ray picking resolves a visible hostile formation on the battlefield");
            map.OrderScreen(enemyPoint!.Value); root.UpdateLayout();
            Check(overlay != null && Descendants<TextBlock>(overlay).Any(t => t.Text == "PODGLĄD STARCIA") && defender.Health == 100, "A 3D hostile order opens the real battle preview before changing health");
            Descendants<Button>(overlay!).Single(b => b.Content?.ToString() == "Wykonaj atak").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); root.UpdateLayout();
            Check(defender.Health < 100 && fighter.Actions == 0 && overlay == null, "Confirmed 3D combat applies real damage and consumes the unit's actions"); await Capture("3d-combat");
            foreach (int era in new[] { 5, 9, 14, 17 })
            {
                Start(EpochFixture(era)); map.ShowResources = false; map.Orbit(15, -8); map.Zoom(.86); Refresh();
                Check(map.Scene!.ObjectTriangles > 1000 && state.Player.Era == era, "City architecture builds spatial models for era " + (era + 1));
                await Capture("3d-era-" + (era + 1));
            }
            Start(EpochFixture(9)); var home = state.Cities[0]; state.Units.Clear();
            foreach (var role in Enum.GetValues<UnitKind>())
            {
                var tile = World.Neighbors(state, home.Tile).Where(t => t.Passable).ElementAtOrDefault((int)role % 4) ?? state.Tiles[home.Tile];
                var model = UnitModels.All.Where(m => m.Role == role && m.Era <= 9).LastOrDefault(); Engine.AddUnit(state, 0, tile.Id, role, model?.Id);
            }
            Refresh(); Check(map.Scene!.UnitIds.Count >= 6, "All six land roles have renderable spatial models, including cavalry and artillery"); await Capture("3d-military-models");
            Width = 1200; Height = 800; await Task.Delay(120); UpdateLayout(); map.Center(home.Tile); root.UpdateLayout();
            Check(map.ActualWidth > 500 && map.ActualHeight > 350 && Descendants<Button>(map).Single(b => b.Content?.ToString() == "Widok 3D").IsVisible, "Compact window retains a usable 3D viewport and camera controls"); await Capture("3d-compact");
            Width = 1600; Height = 980; await Task.Delay(120); UpdateLayout(); Start(EpochFixture(5)); map.ShowResources = false; map.Orbit(15, -10); map.Zoom(.83); Refresh(); await Capture("3d-final");
            checks.Add($"{checks.Count} 3D desktop integration checks passed."); checks.Add($"Elapsed {watch.Elapsed.TotalSeconds:0.0}s; render tier {RenderCapability.Tier >> 16}; final terrain {map.Scene!.TerrainTriangles} triangles; objects {map.Scene.ObjectTriangles} triangles.");
            File.WriteAllLines(Path.Combine(folder, "ui-smoke.txt"), checks); Close();
        }
        catch (Exception ex) { checks.Add(ex.ToString()); File.WriteAllLines(Path.Combine(folder, "ui-smoke.txt"), checks); if (!arguments.Contains("--smoke-force-failure")) await Capture("3d-failure"); Application.Current.Shutdown(3); }
    }
}
