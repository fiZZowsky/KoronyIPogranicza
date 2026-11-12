using Crown.Core;
using System.Windows;

namespace Crown.Desktop;

internal sealed record RiverSample(Point Position,Vector Side,double Water,double HalfWidth,double Along);
internal sealed record RiverPath(int From,int To,IReadOnlyList<RiverSample> Samples);
internal sealed record RiverCrossing(Point Position,double RoadT,double Water,double HalfSpan);

// Visual geography only. Every path is built from explored tiles, without campaign RNG.
internal sealed class RiverNetwork
{
    internal IReadOnlyList<RiverPath> Paths { get; }
    private RiverNetwork(IReadOnlyList<RiverPath> paths)=>Paths=paths;
    internal static RiverNetwork Create(GameState s,Func<double,double,double> height)
    {
        var paths=new List<RiverPath>();var cities=s.Cities.Select(c=>c.Tile).ToHashSet();
        Point Center(Tile tile){var p=Terrain3D.Center(tile);return cities.Contains(tile.Id)?new(p.X+1.20,p.Y-.08):p;}
        void Add(int from,int to,Point a,Point b)
        {
            var delta=b-a;if(delta.Length<.05)return;var side=new Vector(-delta.Y,delta.X);side.Normalize();
            double bend=(Terrain3D.Noise(a.X*.2,b.Y*.2,s.Seed+71)-.5)*.34;
            Point? Other(int node,int excluded)
            {
                var points=World.Neighbors(s,node).Where(t=>t.Id!=excluded&&t.Explored&&t.Passable&&t.River).Select(Center).ToArray();
                return points.Length==0?null:new Point(points.Average(p=>p.X),points.Average(p=>p.Y));
            }
            Vector m0=from==to?delta:(b-(Other(from,to)??a))*.5,m1=from==to?delta:((Other(to,from)??b)-a)*.5;
            if(Other(from,to)==null)m0=delta;if(Other(to,from)==null)m1=delta;
            Point Curve(double t)
            {
                if(t<=0)return a;if(t>=1)return b;double t2=t*t,t3=t2*t;
                return new Point((2*t3-3*t2+1)*a.X+(t3-2*t2+t)*m0.X+(-2*t3+3*t2)*b.X+(t3-t2)*m1.X,
                    (2*t3-3*t2+1)*a.Y+(t3-2*t2+t)*m0.Y+(-2*t3+3*t2)*b.Y+(t3-t2)*m1.Y)+side*(Math.Pow(Math.Sin(t*Math.PI),2)*bend);
            }
            int steps=Math.Max(12,(int)Math.Ceiling(delta.Length/.075));var samples=new List<RiverSample>();double along=0;
            double ay=Math.Max(Terrain3D.SeaLevel+.004,height(a.X,a.Y)-.025),by=Math.Max(Terrain3D.SeaLevel+.004,height(b.X,b.Y)-.025);
            for(int i=0;i<=steps;i++)
            {
                double t=(double)i/steps;var p=Curve(t);var tangent=Curve(Math.Min(1,t+.001))-Curve(Math.Max(0,t-.001));var normal=new Vector(-tangent.Y,tangent.X);normal.Normalize();
                if(i>0)along+=(p-samples[^1].Position).Length;
                double surface=i==0?ay:i==steps?by:Math.Max(Terrain3D.SeaLevel+.004,Math.Min(ay+(by-ay)*t,height(p.X,p.Y)-.025));
                samples.Add(new(p,normal,surface,.20+.025*Math.Sin(t*Math.PI),along));
            }
            paths.Add(new(from,to,samples));
        }
        foreach(var tile in s.Tiles.Where(t=>t.Explored&&t.River&&t.Passable))
        {
            var neighbors=World.Neighbors(s,tile.Id).Where(t=>t.Explored&&t.Passable&&t.River).OrderBy(t=>t.Id).ToArray();var p=Center(tile);
            if(neighbors.Length==0)Add(tile.Id,tile.Id,new(p.X-.10,p.Y-.8),new(p.X+.10,p.Y+.8));
            foreach(var n in neighbors.Where(n=>n.Id>tile.Id))Add(tile.Id,n.Id,p,Center(n));
            // Only a known terminal reach may connect to a known coast.
            if(neighbors.Length<=1)
            {
                var coast=World.Neighbors(s,tile.Id).Where(t=>t.Explored&&t.Terrain is Terrain.Coast or Terrain.Ocean).OrderBy(t=>t.Id).FirstOrDefault();
                if(coast!=null)Add(tile.Id,coast.Id,p,Terrain3D.Center(coast));
            }
        }
        return new(paths);
    }
    internal static double Distance(Point point,Point a,Point b,out double t)
    {
        var d=b-a;t=d.LengthSquared<1e-12?0:Math.Clamp(Vector.Multiply(point-a,d)/d.LengthSquared,0,1);return (point-(a+d*t)).Length;
    }
    internal double Distance(Point point)
    {
        double nearest=double.MaxValue;
        foreach(var path in Paths)for(int i=1;i<path.Samples.Count;i++)nearest=Math.Min(nearest,Distance(point,path.Samples[i-1].Position,path.Samples[i].Position,out _));
        return nearest;
    }
    internal RiverCrossing? Crossing(Point a,Point b)
    {
        var road=b-a;double length=road.Length;if(length<.01)return null;RiverCrossing? result=null;
        static double Cross(Vector a,Vector b)=>a.X*b.Y-a.Y*b.X;
        foreach(var path in Paths)for(int i=1;i<path.Samples.Count;i++)
        {
            var p=path.Samples[i-1];var q=path.Samples[i];var river=q.Position-p.Position;double denominator=Cross(road,river);
            double sine=Math.Abs(denominator)/Math.Max(.00001,length*river.Length);if(sine<.30)continue;
            var offset=p.Position-a;double t=Cross(offset,river)/denominator,u=Cross(offset,road)/denominator;
            if(t<0||t>1||u<0||u>1)continue;
            var crossing=new RiverCrossing(a+road*t,t,p.Water+(q.Water-p.Water)*u,Math.Min(.85,(p.HalfWidth+.15)/sine));
            if(result==null||t<result.RoadT)result=crossing;
        }
        return result;
    }
}
