using Crown.Core;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Crown.Desktop;

public sealed partial class MapView : Grid
{
    public GameState State { get; set; } = null!;
    public int SelectedTile { get; set; } = -1;
    public int SelectedUnit { get; set; } = -1;
    public bool ShowResources { get; set; } = true;
    public bool ShowGrid { get; set; }
    public bool ShowSupply { get; set; }
    public bool ShowPopulation { get; set; }
    public int TrackedCaravanId { get; set; } = -1;
    public IReadOnlyList<int> PlannedRoad { get; set; } = [];
    public Action<int>? SelectTile;
    public Action<int>? OrderUnit;
    public Action? CameraChanged;
    private readonly Viewport3D viewport = new() { ClipToBounds = false, IsHitTestVisible = false };
    private readonly PerspectiveCamera camera = new() { FieldOfView = 52, NearPlaneDistance = .08, FarPlaneDistance = 180, UpDirection = new(0, 1, 0) };
    private readonly ModelVisual3D worldVisual = new(), overlaysVisual = new();
    private readonly Canvas annotations = new() { IsHitTestVisible = false };
    private readonly Image photographicImage = new() { Stretch = Stretch.Fill, IsHitTestVisible = false };
    private readonly TextBlock rendererStatus = new() { IsHitTestVisible = false, FontSize = 10, Foreground = Ui.Muted, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom, Margin = new(0, 0, 18, 58) };
    private PhotographicRenderer? photographic;
    private bool gpuPending, gpuAttempted;
    private readonly System.Windows.Threading.DispatcherTimer animationTimer=new(System.Windows.Threading.DispatcherPriority.Background){Interval=TimeSpan.FromMilliseconds(100)};
    private readonly System.Diagnostics.Stopwatch visualClock=System.Diagnostics.Stopwatch.StartNew();
    private bool animateEnvironment=true;
    internal PhotographicRenderer? Photographic => photographic;
    internal string? GpuFailure { get; private set; }
    private WorldScene? scene;
    private int sceneSignature;
    private int terrainSignature;
    private int hoverTile = -1;
    private Point target = new(18, 24), down, last;
    private double distance = 13, yaw = -30, pitch = 48;
    private bool dragging, moved, orbiting;
    private MouseButton heldButton;
    private long lastHover;
    internal WorldScene? Scene => scene;
    internal int SceneBuildCount { get; private set; }
    internal PerspectiveCamera Camera => camera;
    internal Viewport3D Viewport3D => viewport;
    internal double CameraDistance => distance;
    internal double CameraYaw => yaw;
    internal double CameraPitch => pitch;
    public MapView()
    {
        ClipToBounds = true; Focusable = true;
        Background = new LinearGradientBrush((Color)ColorConverter.ConvertFromString("#A2BEC6"), (Color)ColorConverter.ConvertFromString("#344F55"), 90);
        viewport.Camera = camera; viewport.Children.Add(worldVisual); viewport.Children.Add(overlaysVisual);
        Children.Add(viewport); Children.Add(photographicImage); Children.Add(annotations); Children.Add(rendererStatus); AddCameraControls();
        animationTimer.Tick+=(_,_)=>{if(animateEnvironment&&photographic!=null&&!gpuPending&&IsVisible)RenderGpuFrame(visualClock.Elapsed.TotalSeconds);};
        Loaded+=(_,_)=>animationTimer.Start();
        Unloaded += (_, _) => { animationTimer.Stop();photographic?.Dispose(); photographic = null; photographicImage.Source = null; gpuAttempted = false; };
        MouseDown += (_, e) =>
        {
            if (e.OriginalSource is Button) return;
            Focus(); heldButton = e.ChangedButton; down = last = e.GetPosition(this); dragging = true; moved = false;
            orbiting = heldButton is MouseButton.Middle or MouseButton.Right || Keyboard.Modifiers.HasFlag(ModifierKeys.Shift); CaptureMouse(); e.Handled = true;
        };
        MouseMove += (_, e) =>
        {
            var point = e.GetPosition(this);
            if (dragging)
            {
                if ((point - down).Length > 4) moved = true;
                if (moved) { var delta = point - last; if (orbiting) Orbit(delta.X * .35, delta.Y * .23); else PanPixels(delta.X, delta.Y); }
                last = point;
            }
            else if (State != null && Environment.TickCount64 - lastHover >= 70)
            {
                lastHover = Environment.TickCount64; int id = Pick(point);
                if (hoverTile != id) { hoverTile = id; RebuildOverlays(); UpdateAnnotations(); }
            }
        };
        MouseUp += (_, e) =>
        {
            if (!dragging || e.ChangedButton != heldButton) return;
            dragging = false; ReleaseMouseCapture();
            if (!moved) { if (heldButton == MouseButton.Left) SelectScreen(e.GetPosition(this)); else if (heldButton == MouseButton.Right) OrderScreen(e.GetPosition(this)); }
            e.Handled = true;
        };
        LostMouseCapture += (_, _) => dragging = false;
        MouseLeave += (_, _) => { if (!dragging && hoverTile != -1) { hoverTile = -1; RebuildOverlays(); UpdateAnnotations(); } };
        MouseWheel += (_, e) => { Zoom(e.Delta > 0 ? .86 : 1.16); e.Handled = true; };
        KeyDown += (_, e) =>
        {
            switch (e.Key)
            {
                case Key.Q: Orbit(-12, 0); break; case Key.E: Orbit(12, 0); break;
                case Key.PageUp: Orbit(0, 7); break; case Key.PageDown: Orbit(0, -7); break;
                case Key.W: PanPixels(0, 55); break; case Key.S: PanPixels(0, -55); break;
                case Key.A: PanPixels(55, 0); break; case Key.D: PanPixels(-55, 0); break;
                default: return;
            }
            e.Handled = true;
        };
        SizeChanged += (_, _) => UpdateCamera(); Loaded += (_, _) => UpdateCamera();
    }
    private void AddCameraControls()
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Bottom, HorizontalAlignment = HorizontalAlignment.Left, Margin = new(16, 0, 0, 15) };
        foreach (var (label, action, tip) in new (string, Action, string)[]
        {
            ("↶", () => Orbit(-20, 0), "Obróć kamerę w lewo · Q"), ("↷", () => Orbit(20, 0), "Obróć kamerę w prawo · E"),
            ("+", () => Zoom(.8), "Przybliż · kółko myszy"), ("−", () => Zoom(1.25), "Oddal · kółko myszy"),
            ("Kąt +", () => Orbit(0, 8), "Wyższy kąt · Page Up"), ("Kąt −", () => Orbit(0, -8), "Niższy kąt · Page Down"),
            ("Widok 3D", ResetCamera, "Przywróć widok nad wybranym polem"),
            ("Krajobraz", LandscapeCamera, "Niska kamera krajobrazowa"),
            ("Grafika", GraphicsMenu, "Jakość renderowania i efekty")
        })
        { var b = Ui.Button(label, action); b.ToolTip = tip; b.Margin = new(0, 0, 5, 0); b.Padding = new(9, 7, 9, 7); b.Focusable = false; panel.Children.Add(b); }
        Children.Add(panel);
    }
    public static Point CenterOf(Tile tile) { var p = Terrain3D.Center(tile); return new(p.X * 34, p.Y * 34); }
    public void Reset(GameState state)
    {
        State = state; scene = null; hoverTile = -1; TrackedCaravanId = -1; PlannedRoad = [];
        SelectedTile = state.Cities.FirstOrDefault(c => c.Owner == 0)?.Tile ?? 0; SelectedUnit = -1;
        distance = 13; yaw = -30; pitch = 48;camera.FieldOfView=52; target = Terrain3D.Center(state.Tiles[SelectedTile]); InvalidateVisual(); UpdateCamera();
    }
    public void Center(int id)
    {
        if (State == null || id < 0 || id >= State.Tiles.Count) return;
        target = Terrain3D.Center(State.Tiles[id]); EnsureScene(); UpdateCamera();
    }
    public void ResetCamera() { distance = 13; yaw = -30; pitch = 48;camera.FieldOfView=52; if (SelectedTile >= 0) Center(SelectedTile); else UpdateCamera(); }
    public void Orbit(double azimuth, double elevation) { yaw = ((yaw + azimuth) % 360 + 360) % 360; pitch = Math.Clamp(pitch + elevation, 15, 78); UpdateCamera(); }
    internal void LandscapeCamera(){pitch=15;distance=7;yaw=20;camera.FieldOfView=80;if(SelectedTile>=0)Center(SelectedTile);else UpdateCamera();}
    private void GraphicsMenu()
    {
        var menu=new ContextMenu();
        void Option(string label,Action action){var item=new MenuItem{Header=label};item.Click+=(_,_)=>{action();RequestGpuFrame();};menu.Items.Add(item);}
        Option("Jakość wysoka · cienie i odbicia",()=>{if(photographic is{} r){r.MaximumWidth=1600;r.Reflections=true;r.PostProcessing=true;r.Shadows=true;}});
        Option("Jakość zrównoważona · mniej obciążenia",()=>{if(photographic is{} r){r.MaximumWidth=1100;r.Reflections=false;r.PostProcessing=true;r.Shadows=true;}});
        Option("Cienie: włącz / wyłącz",()=>{if(photographic is{} r)r.Shadows=!r.Shadows;});
        Option("Odbicia: włącz / wyłącz",()=>{if(photographic is{} r)r.Reflections=!r.Reflections;});
        Option("Animowane otoczenie: włącz / wyłącz",()=>animateEnvironment=!animateEnvironment);
        Option("Ponów inicjalizację Direct3D 11",()=>{photographic?.Dispose();photographic=null;gpuAttempted=false;GpuFailure=null;});
        Option("Światło: poranek / południe",()=>{if(photographic is{} r)r.Sun=r.Sun.Y<.7?System.Numerics.Vector3.Normalize(new(-.3f,1.5f,-.2f)):System.Numerics.Vector3.Normalize(new(-.65f,.82f,-.35f));});
        menu.IsOpen=true;
    }
    public void Zoom(double factor) { if (!double.IsFinite(factor) || factor <= 0) return; distance = Math.Clamp(distance * factor, 5, 60); UpdateCamera(); }
    internal void PanPixels(double dx, double dy)
    {
        double angle = yaw * Math.PI / 180, scale = distance * 2 * Math.Tan(camera.FieldOfView * Math.PI / 360) / Math.Max(1, ActualWidth);
        target.X += (-dx * Math.Cos(angle) - dy * Math.Sin(angle)) * scale;
        target.Y += (dx * Math.Sin(angle) - dy * Math.Cos(angle)) * scale;
        if (State != null) { target.X = Math.Clamp(target.X, -1, State.Width * Math.Sqrt(3) + 2); target.Y = Math.Clamp(target.Y, -1, State.Height * 1.5 + 2); }
        UpdateCamera();
    }
    private void UpdateCamera()
    {
        if (State == null) return;
        double a = yaw * Math.PI / 180, p = pitch * Math.PI / 180, y = Math.Max(Terrain3D.SeaLevel, Terrain3D.Height(State, target.X, target.Y));
        var focus = new Point3D(target.X, y + .07, target.Y);
        camera.Position = focus + new Vector3D(Math.Sin(a) * Math.Cos(p) * distance, Math.Sin(p) * distance, Math.Cos(a) * Math.Cos(p) * distance);
        var position=camera.Position;position.Y=Math.Max(position.Y,Math.Max(Terrain3D.SeaLevel,Terrain3D.Height(State,position.X,position.Z))+.3);camera.Position=position;
        camera.LookDirection = focus - camera.Position;
        UpdateAnnotations(); CameraChanged?.Invoke(); RequestGpuFrame();
    }
    private (Vector3D Forward, Vector3D Right, Vector3D Up) Basis()
    {
        var forward = camera.LookDirection; forward.Normalize(); var right = Vector3D.CrossProduct(forward, camera.UpDirection); right.Normalize(); var up = Vector3D.CrossProduct(right, forward); up.Normalize(); return (forward, right, up);
    }
    internal Point? Project(Point3D world)
    {
        if (ActualWidth <= 0 || ActualHeight <= 0) return null;
        var (forward, right, up) = Basis(); var delta = world - camera.Position; double depth = Vector3D.DotProduct(delta, forward); if (depth <= camera.NearPlaneDistance) return null;
        double focal = ActualWidth / (2 * Math.Tan(camera.FieldOfView * Math.PI / 360));
        return new(ActualWidth / 2 + Vector3D.DotProduct(delta, right) * focal / depth, ActualHeight / 2 - Vector3D.DotProduct(delta, up) * focal / depth);
    }
    internal Vector3D Ray(Point point)
    {
        var (forward, right, up) = Basis(); double half = Math.Tan(camera.FieldOfView * Math.PI / 360);
        var result = forward + right * ((point.X * 2 / Math.Max(1, ActualWidth) - 1) * half) + up * ((1 - point.Y * 2 / Math.Max(1, ActualHeight)) * half * ActualHeight / Math.Max(1, ActualWidth)); result.Normalize(); return result;
    }
    internal int Pick(Point point)
    {
        if (State == null || point.X < 0 || point.Y < 0 || point.X > ActualWidth || point.Y > ActualHeight) return -1;
        int tile = -1; double nearest = double.MaxValue;
        VisualTreeHelper.HitTest(worldVisual, null, result =>
        {
            if (result is RayMeshGeometry3DHitTestResult hit && hit.DistanceToRayOrigin < nearest)
            {
                // Leaf cards include transparent holes; decorative canopies must not intercept orders.
                if(hit.ModelHit is GeometryModel3D foliage&&SceneSurface.Key(foliage).StartsWith("foliage"))return HitTestResultBehavior.Continue;
                int candidate = Terrain3D.TileAt(State, hit.PointHit.X, hit.PointHit.Z);
                if(hit.ModelHit is GeometryModel3D model&&model.GetValue(SceneSurface.TileIdsProperty) is int[] ids&&hit.VertexIndex1/3<ids.Length&&ids[hit.VertexIndex1/3]>=0)candidate=ids[hit.VertexIndex1/3];
                if (candidate >= 0) { tile = candidate; nearest = hit.DistanceToRayOrigin; }
            }
            return HitTestResultBehavior.Continue;
        }, new RayHitTestParameters(camera.Position, Ray(point)));
        return tile;
    }
    internal bool SelectScreen(Point point) { int id = Pick(point); if (id < 0) return false; SelectTile?.Invoke(id); return true; }
    internal bool OrderScreen(Point point) { int id = Pick(point); if (id < 0) return false; OrderUnit?.Invoke(id); return true; }
    internal Point[] Footprint
    {
        get
        {
            double height = State == null ? 0 : Terrain3D.Height(State, target.X, target.Y); var points = new List<Point>();
            foreach (var screen in new[] { new Point(0, 0), new Point(ActualWidth, 0), new Point(ActualWidth, ActualHeight), new Point(0, ActualHeight) })
            {
                var ray = Ray(screen); double t = ray.Y < -.01 ? Math.Clamp((height - camera.Position.Y) / ray.Y, 0, 100) : distance * 3;
                var p = camera.Position + ray * t; points.Add(new(p.X * 34, p.Z * 34));
            }
            return points.ToArray();
        }
    }
    public Rect Viewport
    {
        get { var points = Footprint; return new(new Point(points.Min(p => p.X), points.Min(p => p.Y)), new Point(points.Max(p => p.X), points.Max(p => p.Y))); }
    }
    private int Signature()
    {
        var h = new HashCode(); h.Add(State.Seed); h.Add(State.Turn); h.Add(SelectedUnit);
        foreach (var t in State.Tiles) { h.Add(t.Explored); h.Add(t.Terrain); h.Add(t.Owner); h.Add(t.Resource); h.Add(t.Improvement); h.Add(t.Road); h.Add(t.Ruin); h.Add(t.River); }
        foreach (var f in State.Factions) h.Add(f.Era);
        foreach (var c in State.Cities) { h.Add(c.Id); h.Add(c.Tile); h.Add(c.Owner); h.Add(c.Population); foreach (var b in c.Buildings) h.Add(b); }
        foreach (var u in State.Units) { h.Add(u.Id); h.Add(u.Tile); h.Add(u.Owner); h.Add(u.Kind); h.Add(u.ModelId); }
        foreach (var c in State.Caravans) { h.Add(c.Id); h.Add(c.Tile); h.Add(c.Status); }
        return h.ToHashCode();
    }
    private void EnsureScene()
    {
        if (State == null) return; int signature = Signature(); if (scene != null && signature == sceneSignature) return;
        var hash = new HashCode(); hash.Add(State.Seed); foreach(var tile in State.Tiles){hash.Add(tile.Explored);hash.Add(tile.Terrain);}
        int terrainHash = hash.ToHashCode();
        scene = WorldScene3D.Build(State, SelectedUnit, scene != null && terrainHash == terrainSignature ? scene : null); terrainSignature = terrainHash; sceneSignature = signature; SceneBuildCount++;
        var group = new Model3DGroup();
        group.Children.Add(new AmbientLight(Color.FromRgb(76, 89, 99)));
        group.Children.Add(new DirectionalLight(Color.FromRgb(255, 243, 213), new(-.55, -1, -.35)));
        group.Children.Add(new DirectionalLight(Color.FromRgb(68, 94, 115), new(.65, -.35, .65)));
        group.Children.Add(scene.Terrain); group.Children.Add(scene.Objects); group.Freeze(); worldVisual.Content = group;
    }
    public new void InvalidateVisual()
    {
        if (State != null) { EnsureScene(); RebuildOverlays(); UpdateAnnotations(); }
        RequestGpuFrame();
        base.InvalidateVisual();
    }
    private void RequestGpuFrame()
    {
        if (gpuPending || State == null || scene == null) return; gpuPending = true;
        Dispatcher.BeginInvoke(new Action(() => { gpuPending = false; RenderGpuFrame(); }), System.Windows.Threading.DispatcherPriority.Render);
    }
    internal void RenderGpuFrame(double visualTime=0)
    {
        if (!IsLoaded || ActualWidth < 2 || ActualHeight < 2 || scene == null) return;
        try
        {
            if (!gpuAttempted)
            {
                gpuAttempted = true;
                if (Environment.GetCommandLineArgs().Contains("--no-gpu")) throw new InvalidOperationException("Tryb zgodności wybrany parametrem --no-gpu.");
                photographic = new PhotographicRenderer(); GpuFailure = null;
            }
            if (photographic == null) return;
            photographic.Scene(scene, overlaysVisual.Content);
            photographic.Render(camera, (int)ActualWidth, (int)ActualHeight,visualTime);
            photographicImage.Source = photographic.Image; viewport.Visibility = Visibility.Hidden;
            rendererStatus.Text = "DIRECT3D 11 · CIENIE · MATERIAŁY · HDR";
        }
        catch (Exception ex)
        {
            GpuFailure = ex.ToString(); photographic?.Dispose(); photographic = null; photographicImage.Source = null; viewport.Visibility = Visibility.Visible;
            rendererStatus.Text = "TRYB ZGODNOŚCI WPF · " + ex.Message.Split('\n')[0]; rendererStatus.ToolTip = ex.Message;
            System.Diagnostics.Debug.WriteLine(GpuFailure);
        }
    }
}
