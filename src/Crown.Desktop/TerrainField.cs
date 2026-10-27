using Crown.Core;
using System.Runtime.CompilerServices;

namespace Crown.Desktop;

// An immutable visual surface shared by geometry, camera and GPU bathymetry.
// Prepare is the synchronization boundary after explored terrain changes; sampling is O(1).
internal sealed class TerrainField
{
    internal int Width { get; }
    internal int Height { get; }
    internal double OriginX => -4.5;
    internal double OriginZ => -4.5;
    internal double SpanX { get; }
    internal double SpanZ { get; }
    internal ReadOnlyMemory<float> Values => values;
    internal RiverNetwork Rivers { get; }
    private readonly float[] values;
    private TerrainField(GameState s)
    {
        SpanX=s.Width*Math.Sqrt(3)+11;SpanZ=s.Height*1.5+11;
        Width=(int)Math.Ceiling(SpanX*8)+1;Height=(int)Math.Ceiling(SpanZ*8)+1;values=new float[Width*Height];
        var basis=new double[s.Tiles.Count];var known=new bool[basis.Length];var mountain=new bool[basis.Length];var water=new bool[basis.Length];
        foreach(var t in s.Tiles)
        {
            known[t.Id]=t.Explored;mountain[t.Id]=t.Explored&&t.Terrain==Terrain.Mountain;
            water[t.Id]=t.Explored&&t.Terrain is Terrain.Ocean or Terrain.Coast;
            basis[t.Id]=!t.Explored?.13:t.Terrain switch{Terrain.Ocean=>-.65,Terrain.Coast=>-.32,Terrain.Mountain=>.58,Terrain.Hills=>.72,Terrain.Forest=>.27,Terrain.Marsh=>.10,Terrain.Tundra=>.45,_=>.22};
        }
        for(int iz=0;iz<Height;iz++)for(int ix=0;ix<Width;ix++)
        {
            double x=OriginX+ix*SpanX/(Width-1),z=OriginZ+iz*SpanZ/(Height-1);
            int row=(int)Math.Round((z-1)/1.5);double sum=0,weight=0,revealed=0,waterWeight=0,mass=0;
            for(int y=row-3;y<=row+3;y++)
            {
                if(y<0||y>=s.Height)continue;int col=(int)Math.Round((x-1)/Math.Sqrt(3)-(y&1)*.5);
                for(int xx=col-3;xx<=col+3;xx++)
                {
                    if(xx<0||xx>=s.Width)continue;int id=y*s.Width+xx;
                    double dx=Math.Sqrt(3)*(xx+(y&1)*.5)+1-x,dz=y*1.5+1-z,d2=dx*dx+dz*dz;
                    if(d2<4.2){double w=1-d2/4.2;w*=w*w;sum+=basis[id]*w;weight+=w;if(known[id])revealed+=w;if(water[id])waterWeight+=w;}
                    if(mountain[id]&&d2<17.5){double w=1-d2/17.5;mass+=w*w*w;}
                }
            }
            if(weight<1e-8){values[iz*Width+ix]=-.65f;continue;}
            double warpX=x+(Terrain3D.Noise(x*.17,z*.17,s.Seed+43)-.5)*1.8;
            double warpZ=z+(Terrain3D.Noise(x*.17,z*.17,s.Seed+59)-.5)*1.8;
            double ridge=1-Math.Abs(2*Terrain3D.Noise(warpX*.63,warpZ*.25,s.Seed+29)-1);ridge*=ridge;
            double detail=1-Math.Abs(2*Terrain3D.Noise(warpX*1.8,warpZ*.9,s.Seed+93)-1);
            double land=1-waterWeight/weight,mountainHeight=(1-Math.Exp(-mass*.95))*land*land*(1.05+ridge*2.3+detail*.28);
            double ripple=((Terrain3D.Noise(x*1.1,z*1.1,s.Seed)-.5)*.12+(Terrain3D.Noise(x*3.7,z*3.7,s.Seed+8)-.5)*.028)*revealed/weight;
            values[iz*Width+ix]=(float)(sum/weight+mountainHeight+ripple);
        }
        Rivers=RiverNetwork.Create(s,Sample);
        CarveRivers();
    }
    private void CarveRivers()
    {
        var original=(float[])values.Clone();
        foreach(var path in Rivers.Paths)for(int i=1;i<path.Samples.Count;i++)
        {
            var a=path.Samples[i-1];var b=path.Samples[i];double radius=Math.Max(a.HalfWidth,b.HalfWidth)+.22;
            int minX=Math.Max(0,(int)Math.Floor((Math.Min(a.Position.X,b.Position.X)-radius-OriginX)/SpanX*(Width-1)));
            int maxX=Math.Min(Width-1,(int)Math.Ceiling((Math.Max(a.Position.X,b.Position.X)+radius-OriginX)/SpanX*(Width-1)));
            int minZ=Math.Max(0,(int)Math.Floor((Math.Min(a.Position.Y,b.Position.Y)-radius-OriginZ)/SpanZ*(Height-1)));
            int maxZ=Math.Min(Height-1,(int)Math.Ceiling((Math.Max(a.Position.Y,b.Position.Y)+radius-OriginZ)/SpanZ*(Height-1)));
            for(int z=minZ;z<=maxZ;z++)for(int x=minX;x<=maxX;x++)
            {
                var p=new System.Windows.Point(OriginX+x*SpanX/(Width-1),OriginZ+z*SpanZ/(Height-1));double distance=RiverNetwork.Distance(p,a.Position,b.Position,out double t);
                if(distance>radius)continue;double width=a.HalfWidth+(b.HalfWidth-a.HalfWidth)*t,water=a.Water+(b.Water-a.Water)*t;
                double bank=Math.Clamp(distance/width,0,1);bank=bank*bank*(3-2*bank);
                double target=water-.15+bank*.14;double blend=Math.Clamp((distance-width)/.22,0,1);blend=blend*blend*(3-2*blend);
                int index=z*Width+x;values[index]=Math.Min(values[index],(float)(target*(1-blend)+original[index]*blend));
            }
        }
    }
    internal double Sample(double x,double z)
    {
        double u=(x-OriginX)/SpanX*(Width-1),v=(z-OriginZ)/SpanZ*(Height-1);
        if(u<0||v<0||u>Width-1||v>Height-1)return -.65;
        int ix=Math.Min((int)u,Width-2),iz=Math.Min((int)v,Height-2);double fx=u-ix,fz=v-iz;
        return (values[iz*Width+ix]*(1-fx)+values[iz*Width+ix+1]*fx)*(1-fz)+(values[(iz+1)*Width+ix]*(1-fx)+values[(iz+1)*Width+ix+1]*fx)*fz;
    }
    private sealed class Holder { internal int Stamp;internal TerrainField? Field; }
    private static readonly ConditionalWeakTable<GameState,Holder> cache=new();
    internal static TerrainField Prepare(GameState s)
    {
        var hash=new HashCode();hash.Add(s.Seed);hash.Add(s.Width);hash.Add(s.Height);
        foreach(var tile in s.Tiles){hash.Add(tile.Explored);if(tile.Explored){hash.Add(tile.Terrain);hash.Add(tile.River);}}
        foreach(var city in s.Cities.Where(c=>s.Tiles[c.Tile].Explored).OrderBy(c=>c.Tile))hash.Add(city.Tile);
        int stamp=hash.ToHashCode();var holder=cache.GetOrCreateValue(s);
        if(holder.Field==null||holder.Stamp!=stamp){holder.Field=new TerrainField(s);holder.Stamp=stamp;}
        return holder.Field;
    }
    internal static TerrainField Current(GameState s)=>cache.TryGetValue(s,out var holder)&&holder.Field!=null?holder.Field:Prepare(s);
}
