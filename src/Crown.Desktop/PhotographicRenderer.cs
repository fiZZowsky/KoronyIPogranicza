using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace Crown.Desktop;

internal static class SceneSurface
{
    internal static readonly DependencyProperty KeyProperty = DependencyProperty.RegisterAttached("Key", typeof(string), typeof(SceneSurface), new PropertyMetadata("solid"));
    internal static readonly DependencyProperty TileIdsProperty=DependencyProperty.RegisterAttached("TileIds",typeof(int[]),typeof(SceneSurface),new PropertyMetadata(null));
    internal static string Key(GeometryModel3D model) => (string)model.GetValue(KeyProperty);
}

// Hardware shading is independent of WPF geometry and simulation. A bounded staging
// readback lets WPF compose native controls and capture the actual GPU frame reliably.
internal sealed partial class PhotographicRenderer : IDisposable
{
    private readonly NativeGpu gpu;
    private readonly nint worldVs, worldPs, shadowVs, shadowPs, fullVs, skyPs, postPs, layout, constants;
    private readonly nint raster, shadowRaster, sampler, shadowSampler, depthWrite, depthRead, opaque, alpha;
    private readonly nint surfacesTexture, surfacesView, leavesTexture, leavesView, pineTexture, pineView, shadowTexture, shadowView, shadowDepth;
    private nint landTexture, landView, hdrTexture, hdrTarget, hdrView, depthTexture, depthTarget, depthView, finalTexture, finalTarget, staging;
    private nint reflectionTexture,reflectionTarget,reflectionView,reflectionDepthTexture,reflectionDepth;
    private nint elevationTexture,elevationView;
    private TerrainField? field;
    private readonly Dictionary<MeshGeometry3D, DrawMesh> meshCache = new(ReferenceEqualityComparer.Instance);
    private readonly List<DrawMesh> world = [], overlays = [];
    private Model3DGroup? lastTerrain;
    private readonly HashSet<MeshGeometry3D> overlayMeshes = new(ReferenceEqualityComparer.Instance);
    private bool disposed;
    private int width, height;
    internal WriteableBitmap? Image { get; private set; }
    internal int Frames { get; private set; }
    internal double LastFrameMilliseconds { get; private set; }
    internal int ResourceMeshes => meshCache.Count;
    internal int ShadowSize => 2048;
    internal uint FeatureLevel => gpu.FeatureLevel;
    internal bool Shadows { get; set; } = true;
    internal bool MaterialDetail { get; set; } = true;
    internal bool PostProcessing { get; set; } = true;
    internal bool Reflections { get; set; } = true;
    internal bool ShoreEffects { get; set; } = true;
    internal bool EdgeSmoothing { get; set; } = true;
    internal int MaximumWidth { get; set; } = 1600;
    internal float Exposure { get; set; } = .87f;
    internal Vector3 Sun { get; set; } = Vector3.Normalize(new(-.65f, .82f, -.35f));
    [StructLayout(LayoutKind.Sequential)] private struct GpuVertex { internal Vector3 Position, Normal; internal Vector2 Uv; internal Vector4 Color, Surface; }
    [StructLayout(LayoutKind.Sequential)] private struct Frame
    {
        internal Matrix4x4 ViewProjection, LightProjection, InverseProjection;
        internal Vector4 EyeTime, SunExposure, Screen, Settings, TerrainMap, VisualOptions,SeasonPalette,WeatherSettings;
    }
    private sealed record DrawMesh(nint Buffer, int Vertices, Vector3 Center, float Radius, float Kind, bool Transparent);
    public PhotographicRenderer()
    {
        gpu = new();
        try
        {
            using var stream = Application.GetResourceStream(new Uri("pack://application:,,,/Shaders/Landscape.hlsl"))!.Stream;
            using var reader = new StreamReader(stream); string shader = reader.ReadToEnd();
            var program = gpu.Shader(shader, "WorldVS", true); worldVs = program.Shader; layout = gpu.InputLayout(program.Bytecode);
            worldPs = gpu.Shader(shader, "WorldPS", false).Shader; shadowVs = gpu.Shader(shader, "ShadowVS", true).Shader; shadowPs = gpu.Shader(shader, "ShadowPS", false).Shader;
            fullVs = gpu.Shader(shader, "FullVS", true).Shader;skyPs=gpu.Shader(shader,"SkyPS",false).Shader; postPs = gpu.Shader(shader, "PostPS", false).Shader;
            constants = gpu.ConstantBuffer(Marshal.SizeOf<Frame>()); raster = gpu.Rasterizer(); shadowRaster = gpu.Rasterizer(true);
            sampler = gpu.Sampler(); shadowSampler = gpu.Sampler(true); depthWrite = gpu.DepthState(true); depthRead = gpu.DepthState(false); opaque = gpu.BlendState(false); alpha = gpu.BlendState(true);
            (surfacesTexture, surfacesView) = Upload(Asset("surface-atlas-0.21.png")); (leavesTexture, leavesView) = Upload(Asset("oak-foliage-0.21.png"));
            (pineTexture,pineView)=Upload(Asset("spruce-foliage-0.21.png"));
            shadowTexture = gpu.Texture(ShadowSize, ShadowSize, 39, 0x48); shadowDepth = gpu.View(shadowTexture, 10, 40); shadowView = gpu.View(shadowTexture, 7, 41);
        }
        catch { gpu.Dispose(); throw; }
    }
    private static BitmapSource Asset(string name) => BitmapFrame.Create(new Uri("pack://application:,,,/Assets/" + name), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
    private (nint Texture, nint View) Upload(BitmapSource source)
    {
        var image = source.Format == PixelFormats.Bgra32 ? source : new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
        int stride = image.PixelWidth * 4; var pixels = new byte[stride * image.PixelHeight]; image.CopyPixels(pixels, stride, 0);
        var texture = gpu.Texture(image.PixelWidth, image.PixelHeight, 87, 0x28, pixels, mipmaps: true); var view = gpu.View(texture, 7); gpu.Mips(view); return (texture, view);
    }
    private static IEnumerable<GeometryModel3D> Models(Model3D model)
    {
        if (model is GeometryModel3D geometry) yield return geometry;
        if (model is Model3DGroup group) foreach (var child in group.Children) foreach (var item in Models(child)) yield return item;
    }
    private static (System.Windows.Media.Color Color, bool Emissive, BitmapSource? Texture) ReadMaterial(Material material)
    {
        if (material is MaterialGroup group) return ReadMaterial(group.Children[0]);
        Brush? brush = material is DiffuseMaterial diffuse ? diffuse.Brush : material is EmissiveMaterial emissive ? emissive.Brush : null;
        return (brush is SolidColorBrush solid ? solid.Color : Colors.White, material is EmissiveMaterial, (brush as ImageBrush)?.ImageSource as BitmapSource);
    }
    private static float Kind(string key, bool emissive)
    {
        if (emissive) return 8;
        if(key.StartsWith("distant-mountains"))return 9;
        if(key.StartsWith("streets"))return 10;
        if(key.StartsWith("river-bank")||key.StartsWith("river-pebbles"))return 11;
        if(key.StartsWith("ground-grass")||key.StartsWith("crop-leaves")||key.StartsWith("plant-leaves"))return 14;
        if(key.StartsWith("stone-outcrop")||key.StartsWith("salt-bed"))return 15;
        if(key.StartsWith("iron-ore"))return 16;
        if(key.StartsWith("salt-crystals"))return 17;
        if(key.StartsWith("horse-coat-")||key.StartsWith("horse-mane-tail"))return 18;
        if(key.StartsWith("helmets")||key.StartsWith("spear-heads")||key.StartsWith("shield-boss")||key.StartsWith("weapons")||key.StartsWith("gun-metal"))return 19;
        if (key.StartsWith("landscape")) return 0;
        if (key.StartsWith("sea") || key.StartsWith("rivers")) return 1;
        if (key.StartsWith("foliage")) return 5;
        if (key.Contains("roof") && !key.Contains("flat")) return 3;
        if (key.Contains("walls") || key.Contains("fortification") || key.Contains("ruins") || key.Contains("civic-tower") || key.Contains("hall-tower")) return 2;
        if (key.Contains("bark") || key.Contains("wood") || key.Contains("beam")) return 6;
        if (key.Contains("glass") || key.Contains("dome")) return 7;
        return 4;
    }
    private DrawMesh Convert(GeometryModel3D model)
    {
        var mesh = (MeshGeometry3D)model.Geometry; if (meshCache.TryGetValue(mesh, out var cached)) return cached;
        var material = ReadMaterial(model.Material); var c = material.Color; float kind = Kind(SceneSurface.Key(model), material.Emissive);
        var vertices = new GpuVertex[mesh.TriangleIndices.Count]; Vector3 min = new(float.MaxValue), max = new(float.MinValue);
        for (int i = 0; i < vertices.Length; i++)
        {
            int id = mesh.TriangleIndices[i]; var p = mesh.Positions[id]; var n = mesh.Normals[id]; var uv = mesh.TextureCoordinates[id];
            var position = new Vector3((float)p.X, (float)p.Y, (float)p.Z); min = Vector3.Min(min, position); max = Vector3.Max(max, position);
            string key=SceneSurface.Key(model);
            vertices[i] = new GpuVertex { Position = position, Normal = new((float)n.X, (float)n.Y, (float)n.Z), Uv = new((float)uv.X, (float)uv.Y), Color = new(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f), Surface = new(kind, .85f, 0, key.Contains("spruce")?1:key.StartsWith("rivers")?2:0) };
        }
        var draw = new DrawMesh(gpu.Buffer<GpuVertex>(vertices, 1), vertices.Length, (min + max) / 2, (max - min).Length() / 2, kind, c.A < 255);
        meshCache[mesh] = draw; return draw;
    }
    internal void Scene(WorldScene scene, Model3D? overlay)
    {
        SeasonIndex=scene.Season;visualSeed=scene.Seed;
        world.Clear(); overlays.Clear(); overlayMeshes.Clear(); var retained = new HashSet<MeshGeometry3D>(ReferenceEqualityComparer.Instance);
        if (!ReferenceEquals(scene.Terrain, lastTerrain))
        {
            gpu.Free(landView); gpu.Free(landTexture); var atlas = Models(scene.Terrain).Select(m => ReadMaterial(m.Material).Texture).First(t => t != null)!;
            (landTexture, landView) = Upload(atlas); lastTerrain = scene.Terrain;
        }
        if(!ReferenceEquals(field,scene.Field))
        {
            gpu.Free(elevationView);gpu.Free(elevationTexture);field=scene.Field;
            elevationTexture=gpu.Texture(field.Width,field.Height,41,8,MemoryMarshal.AsBytes(field.Values.Span).ToArray());elevationView=gpu.View(elevationTexture,7);
        }
        foreach (var model in Models(scene.Terrain).Concat(Models(scene.Objects))) { world.Add(Convert(model)); retained.Add((MeshGeometry3D)model.Geometry); }
        if (overlay != null) foreach (var model in Models(overlay)) { overlays.Add(Convert(model)); var mesh = (MeshGeometry3D)model.Geometry; retained.Add(mesh); overlayMeshes.Add(mesh); }
        foreach (var mesh in meshCache.Keys.Where(m => !retained.Contains(m)).ToArray()) { gpu.Free(meshCache[mesh].Buffer); meshCache.Remove(mesh); }
    }
    private void Resize(int w, int h)
    {
        if (width == w && height == h) return;
        gpu.Targets(0, 0); gpu.Resources(0, 0, 0, 0, 0, 0,0,0,0);
        foreach (var resource in new[] { hdrTarget, hdrView, hdrTexture, depthTarget, depthView, depthTexture, finalTarget, finalTexture, staging,reflectionTarget,reflectionView,reflectionTexture,reflectionDepth,reflectionDepthTexture }) gpu.Free(resource);
        width = w; height = h;
        hdrTexture = gpu.Texture(w, h, 10, 0x28); hdrTarget = gpu.View(hdrTexture, 9); hdrView = gpu.View(hdrTexture, 7);
        depthTexture = gpu.Texture(w, h, 39, 0x48); depthTarget = gpu.View(depthTexture, 10, 40); depthView = gpu.View(depthTexture, 7, 41);
        finalTexture = gpu.Texture(w, h, 87, 0x20); finalTarget = gpu.View(finalTexture, 9); staging = gpu.Texture(w, h, 87, 0, staging: true);
        reflectionTexture=gpu.Texture(Math.Max(1,w/2),Math.Max(1,h/2),10,0x28);reflectionTarget=gpu.View(reflectionTexture,9);reflectionView=gpu.View(reflectionTexture,7);
        reflectionDepthTexture=gpu.Texture(Math.Max(1,w/2),Math.Max(1,h/2),40,0x40);reflectionDepth=gpu.View(reflectionDepthTexture,10);
        Image = new WriteableBitmap(w, h, 96, 96, PixelFormats.Bgra32, null);
    }
    internal void Render(PerspectiveCamera camera, int w, int h, double time = 0)
    {
        if (disposed || w < 1 || h < 1 || world.Count == 0) return; var watch = Stopwatch.StartNew();
        // Pixel count is bounded even on a high-DPI or maximized window.
        float limit=Math.Clamp(MaximumWidth,800,2000);
        float scale = Math.Min(1, Math.Min(limit/w,limit*.75f/h)); w = Math.Max(1, (int)(w * scale)); h = Math.Max(1, (int)(h * scale)); Resize(w, h);
        var eye = new Vector3((float)camera.Position.X, (float)camera.Position.Y, (float)camera.Position.Z);
        var direction = new Vector3((float)camera.LookDirection.X, (float)camera.LookDirection.Y, (float)camera.LookDirection.Z); var target = eye + direction;
        var view = Matrix4x4.CreateLookAt(eye, target, Vector3.UnitY);
        float fov = (float)(2 * Math.Atan(Math.Tan(camera.FieldOfView * Math.PI / 360) * h / w));
        var projection = Matrix4x4.CreatePerspectiveFieldOfView(fov, (float)w / h, .08f, 180);
        var vp = view * projection; Matrix4x4.Invert(vp, out var inverse);
        float extent = Math.Clamp(direction.Length() * 1.55f, 16, 100);
        var lp = Matrix4x4.CreateLookAt(target + Sun * 65, target, Vector3.UnitY) * Matrix4x4.CreateOrthographic(extent, extent, .1f, 150);
        var frame = new Frame { ViewProjection = vp, LightProjection = lp, InverseProjection = inverse, EyeTime = new(eye, (float)time), SunExposure = new(Sun, Exposure), Screen = new(1f / w, 1f / h, 1f / ShadowSize, Shadows ? 1 : 0), Settings = new(PostProcessing ? 1 : 0, MaterialDetail ? 1 : 0, .006f, Reflections?2:0) };
        frame.TerrainMap=new((float)field!.OriginX,(float)field.OriginZ,(float)field.SpanX,(float)field.SpanZ);frame.VisualOptions=new(ShoreEffects?1:0,EdgeSmoothing?1:0,0,0);
        var environment=Environment;frame.SeasonPalette=new(environment.SnowCover,environment.Autumn,environment.Wetness,environment.Spring);frame.WeatherSettings=new(environment.Rain,environment.Snowfall,environment.Fog,environment.Light);frame.Settings.Z*=environment.Fog;frame.SunExposure.W*=environment.Light;
        gpu.Update(constants, frame); gpu.Constants(constants); gpu.Samplers(sampler, shadowSampler); gpu.Layout(layout); gpu.Blend(opaque); gpu.Depth(depthWrite);
        gpu.Resources(landView, surfacesView, leavesView, 0, 0, 0,pineView,0,elevationView);
        if (Shadows)
        {
            gpu.Targets(0, shadowDepth); gpu.ClearDepth(shadowDepth); gpu.SetViewport(ShadowSize, ShadowSize); gpu.Raster(shadowRaster); gpu.Shaders(shadowVs, shadowPs);
            foreach (var mesh in world.Where(m => !m.Transparent && m.Kind != 1 && Vector3.Distance(m.Center, target) - m.Radius < extent)) gpu.Draw(mesh.Buffer, mesh.Vertices);
        }
        if(Reflections)
        {
            var mirrored=frame;mirrored.ViewProjection=Matrix4x4.CreateReflection(new Plane(Vector3.UnitY,-(float)Terrain3D.SeaLevel))*vp;
            mirrored.EyeTime.Y=2*(float)Terrain3D.SeaLevel-eye.Y;mirrored.Settings.W=1;gpu.Update(constants,mirrored);
            gpu.Targets(reflectionTarget,reflectionDepth);gpu.Clear(reflectionTarget,.49f,.59f,.65f);gpu.ClearDepth(reflectionDepth);gpu.SetViewport(Math.Max(1,w/2),Math.Max(1,h/2));gpu.Raster(raster);gpu.Shaders(worldVs,worldPs);
            gpu.Resources(landView,surfacesView,leavesView,shadowView,0,0,pineView,0,elevationView);
            foreach(var mesh in world.Where(m=>!m.Transparent&&m.Kind!=1))gpu.Draw(mesh.Buffer,mesh.Vertices);
            gpu.Update(constants,frame);
        }
        gpu.Targets(hdrTarget, depthTarget); gpu.Clear(hdrTarget, .42f, .52f, .61f); gpu.ClearDepth(depthTarget); gpu.SetViewport(w, h); gpu.Raster(raster);
        gpu.Depth(depthRead);gpu.Layout(0);gpu.Shaders(fullVs,skyPs);gpu.Triangle();
        gpu.Depth(depthWrite);gpu.Layout(layout);gpu.Shaders(worldVs,worldPs);
        gpu.Resources(landView, surfacesView, leavesView, shadowView, 0, 0,pineView,Reflections?reflectionView:0,elevationView);
        foreach (var mesh in world.Where(m => !m.Transparent)) gpu.Draw(mesh.Buffer, mesh.Vertices);
        gpu.Blend(alpha); gpu.Depth(depthRead);
        foreach (var mesh in world.Where(m => m.Transparent).OrderByDescending(m => Vector3.DistanceSquared(m.Center, eye))) gpu.Draw(mesh.Buffer, mesh.Vertices);
        foreach (var mesh in overlays) gpu.Draw(mesh.Buffer, mesh.Vertices);
        RenderWeather(eye,target,time,environment);
        gpu.Targets(finalTarget, 0); gpu.Resources(landView, surfacesView, leavesView, shadowView, hdrView, depthView,pineView,0,elevationView); gpu.Shaders(fullVs, postPs); gpu.Layout(0); gpu.Blend(opaque); gpu.Triangle();
        gpu.Targets(0, 0); gpu.Resources(0, 0, 0, 0, 0, 0,0,0,0); gpu.Copy(staging, finalTexture);
        var mapped = gpu.Map(staging);
        try { Image!.WritePixels(new Int32Rect(0, 0, w, h), mapped.Pointer, checked((int)mapped.Pitch * h), (int)mapped.Pitch); }
        finally { gpu.Unmap(staging); }
        Frames++; LastFrameMilliseconds = watch.Elapsed.TotalMilliseconds;
    }
    public void Dispose() { if (disposed) return; disposed = true; meshCache.Clear(); world.Clear(); overlays.Clear(); gpu.Dispose(); }
}
