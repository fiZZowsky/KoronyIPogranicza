using System.Numerics;
using System.Windows.Media.Media3D;

namespace Crown.Desktop;

internal sealed partial class PhotographicRenderer
{
    internal bool VisibilityCulling {get;set;}=true;
    internal bool AutomaticDetail {get;set;}=true;
    internal RenderStatistics Statistics {get;private set;}
    internal int SpatialBatches=>meshCache.Values.Sum(b=>b.Length);
    internal int SceneUploads {get;private set;}
    private WorldScene? lastScene;
    private Model3D? lastOverlay;
    private const float CellSize=8;
    private DrawMesh[] ConvertSpatial(GeometryModel3D model,int mapWidth,bool spatial)
    {
        var mesh=(MeshGeometry3D)model.Geometry;if(meshCache.TryGetValue(mesh,out var cached))return cached;
        string key=SceneSurface.Key(model);var material=ReadMaterial(model.Material);var c=material.Color;float kind=Kind(key,material.Emissive);
        byte detail=key.StartsWith("lod-")?(byte)2:key.StartsWith("bark")||key.StartsWith("foliage")?(byte)1:key.StartsWith("ground-grass")||key.StartsWith("wild-")?(byte)3:(byte)0;
        var ids=(int[]?)model.GetValue(SceneSurface.TileIdsProperty);
        var groups=new Dictionary<(int X,int Z),List<GpuVertex>>();
        for(int triangle=0;triangle<mesh.TriangleIndices.Count/3;triangle++)
        {
            var a=mesh.Positions[mesh.TriangleIndices[triangle*3]];var b=mesh.Positions[mesh.TriangleIndices[triangle*3+1]];var d=mesh.Positions[mesh.TriangleIndices[triangle*3+2]];
            double x=(a.X+b.X+d.X)/3,z=(a.Z+b.Z+d.Z)/3;
            if(ids!=null&&ids[triangle]>=0)
            {
                int tile=ids[triangle],row=tile/mapWidth,col=tile%mapWidth;x=Math.Sqrt(3)*(col+(row&1)*.5)+1;z=row*1.5+1;
            }
            var cell=spatial?((int)Math.Floor(x/CellSize),(int)Math.Floor(z/CellSize)):(0,0);
            if(!groups.TryGetValue(cell,out var vertices))groups[cell]=vertices=[];
            for(int corner=0;corner<3;corner++)
            {
                int id=mesh.TriangleIndices[triangle*3+corner];var p=mesh.Positions[id];var n=mesh.Normals[id];var uv=mesh.TextureCoordinates[id];
                vertices.Add(new GpuVertex{Position=new((float)p.X,(float)p.Y,(float)p.Z),Normal=new((float)n.X,(float)n.Y,(float)n.Z),Uv=new((float)uv.X,(float)uv.Y),
                    Color=new(c.R/255f,c.G/255f,c.B/255f,c.A/255f),Surface=new(kind,.85f,0,key.Contains("spruce")?1:key.StartsWith("rivers")?2:0)});
            }
        }
        var draws=new List<DrawMesh>();
        try
        {
            foreach(var (cell,vertices) in groups)
            {
                var data=vertices.ToArray();var minimum=new Vector3(float.MaxValue);var maximum=new Vector3(float.MinValue);
                foreach(var vertex in data){minimum=Vector3.Min(minimum,vertex.Position);maximum=Vector3.Max(maximum,vertex.Position);}
                draws.Add(new(gpu.Buffer<GpuVertex>(data,1),data.Length,(minimum+maximum)*.5f,kind,c.A<255,minimum,maximum,new((cell.X+.5f)*CellSize,(cell.Z+.5f)*CellSize),detail));
            }
        }
        catch{foreach(var draw in draws)gpu.Free(draw.Buffer);throw;}
        var result=draws.ToArray();meshCache.Add(mesh,result);return result;
    }
    private bool UseDetail(DrawMesh mesh,Vector3 eye)
    {
        if(!AutomaticDetail)return mesh.Detail!=2;
        float distance=Vector3.Distance(eye,new(mesh.Cell.X,(float)(field?.Sample(mesh.Cell.X,mesh.Cell.Y)??0),mesh.Cell.Y));
        return mesh.Detail switch{1=>distance<25,2=>distance>=25,3=>distance<18,_=>true};
    }
}
