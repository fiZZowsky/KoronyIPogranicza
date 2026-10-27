using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Crown.Desktop;

internal sealed class SceneMesh
{
    private readonly MeshGeometry3D mesh = new();
    private readonly double uvX, uvZ;
    internal Point3D ScaleOrigin;
    internal double ModelScale = 1;
    internal double ModelYaw;
    internal int TileId=-1;
    internal readonly List<int> TileIds=[];
    public SceneMesh(double uvX = .36, double uvZ = .36) { this.uvX = uvX; this.uvZ = uvZ; }
    public int Triangles => mesh.TriangleIndices.Count / 3;
    public void Triangle(Point3D a, Point3D b, Point3D c, Vector3D? na = null, Vector3D? nb = null, Vector3D? nc = null, Point? ta = null, Point? tb = null, Point? tc = null)
    {
        var normal = Vector3D.CrossProduct(b - a, c - a); if (normal.LengthSquared < 1e-18) return; normal.Normalize();
        if(ModelScale!=1){a=ScaleOrigin+(a-ScaleOrigin)*ModelScale;b=ScaleOrigin+(b-ScaleOrigin)*ModelScale;c=ScaleOrigin+(c-ScaleOrigin)*ModelScale;}
        if(ModelYaw!=0)
        {
            double co=Math.Cos(ModelYaw),si=Math.Sin(ModelYaw);
            Point3D P(Point3D p){var q=p-ScaleOrigin;return new(ScaleOrigin.X+q.X*co-q.Z*si,p.Y,ScaleOrigin.Z+q.X*si+q.Z*co);}
            Vector3D N(Vector3D n)=>new(n.X*co-n.Z*si,n.Y,n.X*si+n.Z*co);
            a=P(a);b=P(b);c=P(c);normal=N(normal);if(na.HasValue)na=N(na.Value);if(nb.HasValue)nb=N(nb.Value);if(nc.HasValue)nc=N(nc.Value);
        }
        int start = mesh.Positions.Count;
        mesh.Positions.Add(a); mesh.Positions.Add(b); mesh.Positions.Add(c);
        mesh.TextureCoordinates.Add(ta ?? new(a.X * uvX, a.Z * uvZ)); mesh.TextureCoordinates.Add(tb ?? new(b.X * uvX, b.Z * uvZ)); mesh.TextureCoordinates.Add(tc ?? new(c.X * uvX, c.Z * uvZ));
        mesh.Normals.Add(na ?? normal); mesh.Normals.Add(nb ?? normal); mesh.Normals.Add(nc ?? normal);
        mesh.TriangleIndices.Add(start); mesh.TriangleIndices.Add(start + 1); mesh.TriangleIndices.Add(start + 2);
        TileIds.Add(TileId);
    }
    public void Quad(Point3D a, Point3D b, Point3D c, Point3D d, Vector3D outward)
    {
        if (Vector3D.DotProduct(Vector3D.CrossProduct(b - a, c - a), outward) >= 0) { Triangle(a, b, c); Triangle(a, c, d); }
        else { Triangle(a, c, b); Triangle(a, d, c); }
    }
    internal void FoliageCard(Point3D center, Vector3D right, Vector3D up)
    {
        var n = Vector3D.CrossProduct(right, up); n.Normalize();
        var a = center - right - up; var b = center + right - up; var c = center + right + up; var d = center - right + up;
        Triangle(a,b,c,n,n,n,new(0,1),new(1,1),new(1,0)); Triangle(a,c,d,n,n,n,new(0,1),new(1,0),new(0,0));
    }
    public void Box(double x, double y, double z, double width, double height, double depth)
    {
        double l = x - width / 2, r = x + width / 2, f = z - depth / 2, b = z + depth / 2, t = y + height;
        Quad(new(l, t, f), new(l, t, b), new(r, t, b), new(r, t, f), new(0, 1, 0));
        Quad(new(l, y, f), new(l, t, f), new(r, t, f), new(r, y, f), new(0, 0, -1));
        Quad(new(l, y, b), new(r, y, b), new(r, t, b), new(l, t, b), new(0, 0, 1));
        Quad(new(l, y, f), new(l, y, b), new(l, t, b), new(l, t, f), new(-1, 0, 0));
        Quad(new(r, y, f), new(r, t, f), new(r, t, b), new(r, y, b), new(1, 0, 0));
    }
    public void Cylinder(double x, double y, double z, double radius, double height, double topRadius = -1, int sides = 10)
    {
        if (topRadius < 0) topRadius = radius;
        for (int i = 0; i < sides; i++)
        {
            double a = i * Math.PI * 2 / sides, b = (i + 1) * Math.PI * 2 / sides;
            var p = new Point3D(x + Math.Cos(a) * radius, y, z + Math.Sin(a) * radius);
            var q = new Point3D(x + Math.Cos(b) * radius, y, z + Math.Sin(b) * radius);
            var r = new Point3D(x + Math.Cos(b) * topRadius, y + height, z + Math.Sin(b) * topRadius);
            var t = new Point3D(x + Math.Cos(a) * topRadius, y + height, z + Math.Sin(a) * topRadius);
            var na = new Vector3D(Math.Cos(a), (radius - topRadius) / Math.Max(height, .001), Math.Sin(a)); na.Normalize();
            var nb = new Vector3D(Math.Cos(b), (radius - topRadius) / Math.Max(height, .001), Math.Sin(b)); nb.Normalize();
            void Side(Point3D v1,Point3D v2,Point3D v3,Vector3D n1,Vector3D n2,Vector3D n3)
            {
                if(Vector3D.DotProduct(Vector3D.CrossProduct(v2-v1,v3-v1),n1+n2+n3)<0){(v2,v3)=(v3,v2);(n2,n3)=(n3,n2);}
                Triangle(v1,v2,v3,n1,n2,n3);
            }
            Side(p,q,r,na,nb,nb);Side(p,r,t,na,nb,na);
            if (topRadius > 0) Triangle(new(x, y + height, z), r, t);
        }
    }
    public void Sphere(double x, double y, double z, double rx, double ry, double rz, int sides = 10, int rings = 6)
    {
        Point3D P(int i, int j) { double a = i * Math.PI / rings, b = j * Math.PI * 2 / sides; return new(x + Math.Sin(a) * Math.Cos(b) * rx, y + Math.Cos(a) * ry, z + Math.Sin(a) * Math.Sin(b) * rz); }
        Vector3D N(Point3D p) { var n = new Vector3D((p.X - x) / (rx * rx), (p.Y - y) / (ry * ry), (p.Z - z) / (rz * rz)); n.Normalize(); return n; }
        void Smooth(Point3D a, Point3D b, Point3D c)
        {
            if (Vector3D.DotProduct(Vector3D.CrossProduct(b - a, c - a), a - new Point3D(x, y, z)) < 0) (b, c) = (c, b);
            Triangle(a, b, c, N(a), N(b), N(c));
        }
        for (int i = 0; i < rings; i++) for (int j = 0; j < sides; j++)
        {
            var a = P(i, j); var b = P(i + 1, j); var c = P(i + 1, j + 1); var d = P(i, j + 1);
            Smooth(a, b, c); Smooth(a, c, d);
        }
    }
    public void Tube(Point3D start, Point3D end, double radius, int sides = 8)
    {
        var direction = end - start; if (direction.LengthSquared < 1e-12) return; direction.Normalize();
        var u = Vector3D.CrossProduct(direction, Math.Abs(direction.Y) > .9 ? new(1, 0, 0) : new(0, 1, 0)); u.Normalize(); var v = Vector3D.CrossProduct(direction, u);
        for (int i = 0; i < sides; i++)
        {
            double a = i * Math.PI * 2 / sides, b = (i + 1) * Math.PI * 2 / sides;
            var p = (u * Math.Cos(a) + v * Math.Sin(a)) * radius; var q = (u * Math.Cos(b) + v * Math.Sin(b)) * radius;
            Quad(start + p, start + q, end + q, end + p, p + q); Triangle(end, end + p, end + q);
        }
    }
    public MeshGeometry3D Freeze() { mesh.Freeze(); return mesh; }
}

internal sealed class SceneBatch
{
    private Point3D scaleOrigin;
    private double modelScale=1;
    private double modelYaw;
    internal int TileId=-1;
    internal void Scaled(Point3D origin,double scale,Action build)
    {
        var previous=scaleOrigin;double previousScale=modelScale;scaleOrigin=origin;modelScale=scale;
        try{build();}finally{scaleOrigin=previous;modelScale=previousScale;}
    }
    internal void Rotated(Point3D origin,double radians,Action build)
    {
        var previous=scaleOrigin;double previousYaw=modelYaw;scaleOrigin=origin;modelYaw=radians;
        try{build();}finally{scaleOrigin=previous;modelYaw=previousYaw;}
    }
    private SceneMesh Configure(SceneMesh mesh){mesh.ScaleOrigin=scaleOrigin;mesh.ModelScale=modelScale;mesh.ModelYaw=modelYaw;mesh.TileId=TileId;return mesh;}
    private readonly Dictionary<string, SceneMesh> meshes = [];
    private readonly Dictionary<string, Material> materials = [];
    private readonly Dictionary<string, int> chunks = [];
    private string Slot(string key)
    {
        int chunk = chunks.GetValueOrDefault(key); string slot = key + ":" + chunk;
        if (meshes.TryGetValue(slot, out var m) && m.Triangles >= 5500) { chunks[key] = ++chunk; slot = key + ":" + chunk; }
        return slot;
    }
    public SceneMesh Mesh(string key, string color, bool emissive = false)
    {
        key = Slot(key);
        if (meshes.TryGetValue(key, out var existing)) return Configure(existing);
        var brush = Ui.Brush(color); Material m = emissive ? new EmissiveMaterial(brush) : new DiffuseMaterial(brush); m.Freeze(); materials[key] = m;
        var result = new SceneMesh(); meshes[key] = result; return Configure(result);
    }
    public SceneMesh Terrain(Crown.Core.Terrain terrain, bool remembered)
    {
        string key = Slot("terrain-" + terrain + remembered);
        if (meshes.TryGetValue(key, out var existing)) return existing;
        var brush = new ImageBrush(Terrain3D.Texture(terrain)) { TileMode = TileMode.Tile, Viewport = new Rect(0, 0, 1, 1), Opacity = 1 }; brush.Freeze();
        Material material = new DiffuseMaterial(brush); material.Freeze(); materials[key] = material;
        var result = new SceneMesh(); meshes[key] = result; return result;
    }
    public SceneMesh Foliage(bool spruce)
    {
        string key=Slot(spruce?"foliage-spruce":"foliage-oak");if(meshes.TryGetValue(key,out var existing))return Configure(existing);
        var image=new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/Assets/"+(spruce?"spruce":"oak")+"-foliage-0.21.png"));image.Freeze();
        var brush=new ImageBrush(image){ViewportUnits=BrushMappingMode.Absolute,Viewport=new(0,0,1,1)};brush.Freeze();
        var material=new DiffuseMaterial(brush);material.Freeze();materials[key]=material;var mesh=new SceneMesh();meshes[key]=mesh;return Configure(mesh);
    }
    public SceneMesh Landscape(Crown.Core.GameState state)
    {
        string key = Slot("landscape"); if (meshes.TryGetValue(key, out var m)) return m;
        const string materialKey = "landscape-material";
        if (!materials.TryGetValue(materialKey, out var material))
        {
            var brush = new ImageBrush(Terrain3D.Atlas(state)) { ViewportUnits = BrushMappingMode.Absolute, Viewport = new Rect(0,0,1,1), TileMode = TileMode.None }; brush.Freeze(); material = new DiffuseMaterial(brush); material.Freeze(); materials[materialKey] = material;
        }
        materials[key] = material; var result = new SceneMesh(1 / (state.Width * Math.Sqrt(3) + 2), 1 / (state.Height * 1.5 + 2)); meshes[key] = result; return result;
    }
    public SceneMesh Sea()
    {
        const string key = "sea"; if (meshes.TryGetValue(key, out var m)) return m;
        var material = new MaterialGroup(); material.Children.Add(new DiffuseMaterial(Ui.Brush("#367A89"))); material.Children.Add(new SpecularMaterial(Ui.Brush("#B8DDE1"), 70)); material.Freeze();
        materials[key] = material; return meshes[key] = new();
    }
    public int Triangles => meshes.Values.Sum(m => m.Triangles);
    public Model3DGroup Freeze()
    {
        var group = new Model3DGroup();
        foreach (var pair in meshes.Where(p => p.Value.Triangles != 0))
        {
            var model = new GeometryModel3D(pair.Value.Freeze(), materials[pair.Key]);
            model.SetValue(SceneSurface.KeyProperty, pair.Key);model.SetValue(SceneSurface.TileIdsProperty,pair.Value.TileIds.ToArray());
            if(pair.Key.StartsWith("foliage"))model.BackMaterial=materials[pair.Key];group.Children.Add(model);
        }
        group.Freeze(); return group;
    }
}
