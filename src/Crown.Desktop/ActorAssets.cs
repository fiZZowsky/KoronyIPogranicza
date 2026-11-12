using System.IO;
using System.IO.Compression;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace Crown.Desktop;

internal static class ActorAssets
{
    internal static readonly string[] Names=["horse","viking","knight","modern","soldier","civilian","robot","forager"];
    internal sealed record Model(Point3D[] Positions,Vector3D[] Normals,Point[] UV,int[] Indices);
    private static readonly Dictionary<string,Model[]> models=[];
    private static readonly Dictionary<(int,string),BitmapSource> images=[];
    private static readonly Dictionary<int,ImageBrush> brushes=[];
    internal static BitmapSource Texture(int id,string channel)
    {
        if(images.TryGetValue((id,channel),out var cached))return cached;
        var image=new BitmapImage();image.BeginInit();image.UriSource=new($"pack://application:,,,/Assets/Actors/{Names[id]}-{channel}.jpg");image.DecodePixelWidth=1024;image.CacheOption=BitmapCacheOption.OnLoad;image.EndInit();image.Freeze();images[(id,channel)]=image;return image;
    }
    internal static ImageBrush Brush(int id)
    {
        if(brushes.TryGetValue(id,out var cached))return cached;
        var brush=new ImageBrush(Texture(id,"base")){TileMode=TileMode.None,Stretch=Stretch.Fill};brush.Freeze();brushes[id]=brush;return brush;
    }
    internal static int MaterialId(string key)
    {
        if(key.StartsWith("organic-near-"))key=key[13..];else if(key.StartsWith("organic-far-"))key=key[12..];
        return key.StartsWith("actor-")&&key.Length>6&&key[6] is >='0' and <='7'?key[6]-'0':-1;
    }
    internal static Model[] Load(int id,bool rider=false)
    {
        string name=Names[id]+(rider?"-rider":"");if(models.TryGetValue(name,out var cached))return cached;
        using var source=Application.GetResourceStream(new Uri($"pack://application:,,,/Assets/Actors/{name}.cmesh"))?.Stream??throw new InvalidDataException("Brak modelu: "+name);
        using var stream=new BrotliStream(source,CompressionMode.Decompress);using var reader=new BinaryReader(stream);
        if(reader.ReadUInt32()!=0x314D5243||reader.ReadInt32()!=2)throw new InvalidDataException("Nieprawidłowy model: "+name);
        var result=new Model[2];
        for(int lod=0;lod<2;lod++)
        {
            int vertices=reader.ReadInt32(),count=reader.ReadInt32();if(vertices is <3 or >100000||count is <3 or >120000||count%3!=0)throw new InvalidDataException("Nieprawidłowy rozmiar siatki.");
            var positions=new Point3D[vertices];var normals=new Vector3D[vertices];var uv=new Point[vertices];
            for(int i=0;i<vertices;i++)
            {
                positions[i]=new(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());normals[i]=new(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());uv[i]=new(reader.ReadSingle(),reader.ReadSingle());
                if(!double.IsFinite(positions[i].X+positions[i].Y+positions[i].Z+uv[i].X+uv[i].Y)||!double.IsFinite(normals[i].Length)||Math.Abs(normals[i].Length-1)>.005)throw new InvalidDataException("Uszkodzone wierzchołki modelu.");
            }
            int[] indices=new int[count];for(int i=0;i<count;i++){indices[i]=reader.ReadInt32();if(indices[i]<0||indices[i]>=vertices)throw new InvalidDataException("Nieprawidłowy indeks modelu.");}
            result[lod]=new(positions,normals,uv,indices);
        }
        models[name]=result;return result;
    }
    internal static void Draw(SceneBatch batch,int id,double x,double y,double z,double height,bool rider=false,int identity=0)
    {
        var lods=Load(id,rider);string key=$"actor-{id}"+(rider?"-rider":"");
        void Build()=>batch.Organic(()=>
        {
            var mesh=batch.Mesh(key,"#FFFFFF");var model=lods[mesh.Simplified?1:0];
            Point3D P(int i){var p=model.Positions[i];return new(x+p.X*height,y+p.Y*height,z+p.Z*height);}
            for(int i=0;i<model.Indices.Length;i+=3)
            {
                // Keep individual WPF meshes and GPU upload chunks bounded.
                if(i%1536==0)mesh=batch.Mesh(key,"#FFFFFF");
                int a=model.Indices[i],b=model.Indices[i+1],c=model.Indices[i+2];
                mesh.Triangle(P(a),P(b),P(c),model.Normals[a],model.Normals[b],model.Normals[c],model.UV[a],model.UV[b],model.UV[c]);
            }
        });
        if(id is >0 and <6 or 7)batch.Character(new(x,y,z),height,Build,identity);else Build();
    }
}
