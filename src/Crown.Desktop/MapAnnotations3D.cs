using Crown.Core;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Crown.Desktop;

public sealed partial class MapView
{
    internal int OverlayTriangles { get; private set; }
    internal int AnnotationCount => annotations.Children.Count;
    private void RebuildOverlays()
    {
        if (State == null) return;
        var batch = new SceneBatch(); var visible = Engine.Visible(State);
        var supply = ShowSupply ? Logistics.For(State, 0) : null;
        foreach (var tile in State.Tiles.Where(t => t.Explored))
        {
            if (!visible.Contains(tile.Id)) WorldScene3D.HexFill(batch.Mesh("remembered", "#480F2530", true), State, tile, .022);
            if (ShowGrid) WorldScene3D.HexOutline(batch.Mesh("grid", "#829DA89B", true), State, tile, .012, .047);
            if (supply != null && tile.Passable)
            {
                bool blocked = supply.Interdicted.Contains(tile.Id), supplied = supply.Costs.GetValueOrDefault(tile.Id, double.PositiveInfinity) <= Logistics.Range(State, 0);
                WorldScene3D.HexFill(batch.Mesh(blocked ? "blocked" : supplied ? "supplied" : "unsupplied", blocked ? "#759C652F" : supplied ? "#60349A78" : "#70B85E51", true), State, tile, .060);
            }
            if (ShowPopulation && tile.Owner == 0)
            {
                var city = State.Cities.Where(c => c.Owner == 0 && World.Distance(tile, State.Tiles[c.Tile]) <= 2).OrderBy(c => World.Distance(tile, State.Tiles[c.Tile])).ThenBy(c => c.Id).FirstOrDefault();
                if (city != null) WorldScene3D.HexFill(batch.Mesh("prosperity-" + city.Id, city.Prosperity < 40 ? "#78B05A48" : city.Prosperity < 65 ? "#70C0A05B" : "#703AAB8D", true), State, tile, .063);
            }
        }
        if (SelectedTile >= 0 && SelectedTile < State.Tiles.Count && State.Tiles[SelectedTile].Explored)
            WorldScene3D.HexOutline(batch.Mesh("selected", "#F4D18E", true), State, State.Tiles[SelectedTile], .047, .085);
        var unit = State.Units.FirstOrDefault(u => u.Id == SelectedUnit && u.Owner == 0);
        if (unit != null)
        {
            foreach (int id in Navigation.ForUnit(State, unit, unit.Actions).Costs.Keys.Where(id => State.Tiles[id].Explored && id != unit.Tile))
                WorldScene3D.HexOutline(batch.Mesh("reachable", "#90B6D3C3", true), State, State.Tiles[id], .018, .07, .87);
            if (unit.Destination is int goal) Route(batch, Navigation.Route(State, unit, goal), "march", "#E8CF8C");
            if (hoverTile >= 0 && State.Tiles[hoverTile].Explored && hoverTile != unit.Tile) Route(batch, Navigation.Route(State, unit, hoverTile), "preview", "#BCD8DB");
        }
        Route(batch, PlannedRoad, "planned-road", "#EABF77");
        var cargo = State.Caravans.FirstOrDefault(c => c.Id == TrackedCaravanId && Caravans.Active(c));
        if (cargo != null)
        {
            var contract = State.TradeContracts.First(c => c.Id == cargo.ContractId);
            if (contract.Buyer == 0 || contract.Seller == 0) Route(batch, Caravans.Route(State, contract, cargo.Tile, EscortOrders.Attached(State, cargo)), "caravan-route", "#E4C278");
        }
        if (ShowPopulation)
        {
            foreach (var shipment in Regions.PlanFood(State, 0).Shipments)
            {
                var from = State.Cities.First(c => c.Id == shipment.Source); var to = State.Cities.First(c => c.Id == shipment.Destination);
                WorldScene3D.Ribbon(batch.Mesh("food-flow", "#E9CA89", true), State, Terrain3D.Center(State.Tiles[from.Tile]), Terrain3D.Center(State.Tiles[to.Tile]), .035, .105);
            }
            foreach (var movement in State.Migrations.Where(m => m.Owner == 0 && State.Turn - m.Turn <= 20).TakeLast(3))
            {
                var from = State.Cities.First(c => c.Id == movement.Source); var to = State.Cities.First(c => c.Id == movement.Destination);
                WorldScene3D.Ribbon(batch.Mesh("migration-flow", "#9CCFDC", true), State, Terrain3D.Center(State.Tiles[from.Tile]), Terrain3D.Center(State.Tiles[to.Tile]), .025, .12);
            }
        }
        OverlayTriangles = batch.Triangles; overlaysVisual.Content = batch.Freeze();
        RequestGpuFrame();
    }
    private void Route(SceneBatch batch, IReadOnlyList<int> route, string key, string color)
    {
        var mesh = batch.Mesh(key, color, true);
        for (int i = 1; i < route.Count; i++)
        {
            var a = State.Tiles[route[i - 1]]; var b = State.Tiles[route[i]];
            if (a.Explored && b.Explored) WorldScene3D.Ribbon(mesh, State, Terrain3D.Center(a), Terrain3D.Center(b), .038, .14);
        }
    }
    private bool TerrainOccludes(Point3D point)
    {
        var delta = point - camera.Position;
        for (int i = 2; i < 15; i++)
        {
            var p = camera.Position + delta * (i / 16d);
            if (Terrain3D.Height(State, p.X, p.Z) > p.Y + .12) return true;
        }
        return false;
    }
    private void UpdateAnnotations()
    {
        if (State == null || ActualWidth <= 0 || ActualHeight <= 0) return;
        annotations.Children.Clear(); var visible = Engine.Visible(State); var occupied = new List<Rect>();
        void Label(Point3D anchor, string text, Brush color, bool important = false, double size = 11)
        {
            var point = Project(anchor);
            if (point == null || point.Value.X < 15 || point.Value.Y < 15 || point.Value.X > ActualWidth - 15 || point.Value.Y > ActualHeight - 75 || TerrainOccludes(anchor)) return;
            var label = new Border { Child = Ui.Label(text, size, color), Padding = new(6, 2, 6, 2), Background = Ui.Brush(important ? "#ED192D31" : "#C71B3033"), BorderBrush = important ? Ui.Gold : Ui.Brush("#66768476"), BorderThickness = new(important ? 1 : .5), CornerRadius = new(3) };
            label.Measure(new(double.PositiveInfinity, double.PositiveInfinity));
            var rect = new Rect(point.Value.X - label.DesiredSize.Width / 2, point.Value.Y - label.DesiredSize.Height, label.DesiredSize.Width, label.DesiredSize.Height);
            if (!important && occupied.Any(r => r.IntersectsWith(rect))) return;
            Canvas.SetLeft(label, rect.X); Canvas.SetTop(label, rect.Y); annotations.Children.Add(label); occupied.Add(rect);
        }
        foreach (var city in State.Cities.Where(c => State.Tiles[c.Tile].Explored).OrderByDescending(c => c.Tile == SelectedTile).ThenBy(c => c.Id))
        {
            var p = Terrain3D.Center(State.Tiles[city.Tile]); int era = State.EpochCampaign ? State.Factions[city.Owner].Era : 4;
            Label(Terrain3D.Ground(State, p.X, p.Y, era >= 12 ? 1.12 : .9), city.Name + "  ·  " + city.Population, Ui.Text, city.Tile == SelectedTile, 12);
        }
        var selected = State.Units.FirstOrDefault(u => u.Id == SelectedUnit && u.Owner == 0);
        if (selected != null)
        {
            var p = Terrain3D.Center(State.Tiles[selected.Tile]); string order = selected.EscortCaravanId != null ? " · eskorta" : selected.Order == MilitaryOrder.Fortify ? " · umocnienia" : selected.Order == MilitaryOrder.Recover ? " · leczenie" : "";
            Label(Terrain3D.Ground(State, p.X - .42, p.Y + .30, .72), UnitModels.Name(selected) + $" · {selected.Health:0}%" + order, Ui.Gold, true);
        }
        if (ShowResources && distance < 26)
            foreach (var tile in State.Tiles.Where(t => t.Explored && t.Resource != Resource.None))
            {
                var p = Terrain3D.Center(tile); string code = tile.Resource switch { Resource.Grain => "Zboże", Resource.Iron => "Fe", Resource.Horses => "Konie", Resource.Stone => "Kamień", Resource.Salt => "Sól", _ => "Barwniki" };
                Label(Terrain3D.Ground(State, p.X + .52, p.Y + .18, .23), code, Ui.Brush("#EDDBB5"), false, 9);
            }
        foreach (var group in State.Units.Where(u => u.Owner == 0 || visible.Contains(u.Tile)).GroupBy(u => u.Tile).Where(g => g.Count() > 1))
        {
            var p = Terrain3D.Center(State.Tiles[group.Key]); Label(Terrain3D.Ground(State, p.X - .50, p.Y + .40, .49), $"Oddziały: {group.Count()}", Ui.Gold);
        }
        if (hoverTile >= 0 && State.Tiles[hoverTile].Explored)
        {
            var tile = State.Tiles[hoverTile]; var p = Terrain3D.Center(tile);
            Label(Terrain3D.Ground(State, p.X, p.Y, .12), Crown.Core.Content.TerrainName(tile.Terrain) + $" · {tile.X}, {tile.Y}", Ui.Text);
        }
    }
}
