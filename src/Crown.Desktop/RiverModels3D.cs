using Crown.Core;
using System.Windows;
using System.Windows.Media.Media3D;

namespace Crown.Desktop;

internal static partial class WorldScene3D
{
    private static void RiverEnvironment(SceneBatch batch,GameState s,RiverNetwork network)
    {
        foreach(var path in network.Paths)
        {
            for(int i=1;i<path.Samples.Count;i++)
            {
                var a=path.Samples[i-1];var b=path.Samples[i];
                Point3D P(RiverSample sample,double side)=>new(sample.Position.X+sample.Side.X*sample.HalfWidth*side,sample.Water,sample.Position.Y+sample.Side.Y*sample.HalfWidth*side);
                var water=batch.Mesh("rivers-channel","#376E6B");var up=new Vector3D(0,1,0);
                var p=P(a,-1);var q=P(b,-1);var r=P(b,1);var t=P(a,1);
                water.Triangle(p,r,q,up,up,up,new(0,a.Along),new(1,b.Along),new(0,b.Along));
                water.Triangle(p,t,r,up,up,up,new(0,a.Along),new(1,a.Along),new(1,b.Along));
                foreach(int side in new[]{-1,1})
                {
                    Point3D Bank(RiverSample sample,double offset)
                    {
                        double variation=offset>.1?(Terrain3D.Noise(sample.Position.X*9,sample.Position.Y*9,s.Seed+301)-.5)*.11:0;
                        var point=sample.Position+sample.Side*(side*(sample.HalfWidth+offset+variation));return Terrain3D.Ground(s,point.X,point.Y,.009);
                    }
                    batch.Mesh("river-bank-gravel","#7C8069").Quad(Bank(a,.015),Bank(b,.015),Bank(b,.18),Bank(a,.18),up);
                }
                if(i%7==0)RiverBankDetails(batch,s,b,path.From+i);
            }
            // Rounded ends overlap the adjoining reach, hiding cracks at confluences.
            foreach(var sample in new[]{path.Samples[0],path.Samples[^1]})
            {
                var mesh=batch.Mesh("rivers-junction","#376E6B");var center=new Point3D(sample.Position.X,sample.Water+.001,sample.Position.Y);var up=new Vector3D(0,1,0);
                for(int i=0;i<16;i++)
                {
                    Point3D P(int j){double angle=j*Math.PI/8;return center+new Vector3D(Math.Cos(angle)*sample.HalfWidth,0,Math.Sin(angle)*sample.HalfWidth);}
                    mesh.Triangle(center,P(i+1),P(i),up,up,up,new(.5,sample.Along),new(.5+Math.Cos((i+1)*Math.PI/8)*.5,sample.Along),new(.5+Math.Cos(i*Math.PI/8)*.5,sample.Along));
                }
                int node=ReferenceEquals(sample,path.Samples[0])?path.From:path.To;
                if(path.From!=path.To&&network.Paths.Count(p=>p.From==node||p.To==node)>1)continue;
                double sign=ReferenceEquals(sample,path.Samples[0])?-1:1;var outward=new Vector(sample.Side.Y,-sample.Side.X)*sign;
                Point3D Bank(int i,double extra)
                {
                    double angle=-Math.PI/2+i*Math.PI/16;var dir=outward*Math.Cos(angle)+sample.Side*Math.Sin(angle);var point=sample.Position+dir*(sample.HalfWidth+extra);
                    return Terrain3D.Ground(s,point.X,point.Y,.01);
                }
                for(int i=0;i<16;i++)batch.Mesh("river-bank-gravel","#7C8069").Quad(Bank(i,.015),Bank(i+1,.015),Bank(i+1,.18),Bank(i,.18),up);
            }
        }
    }
    private static void RiverBankDetails(SceneBatch batch,GameState s,RiverSample sample,int seed)
    {
        int side=(seed&1)==0?1:-1;var p=sample.Position+sample.Side*(side*(sample.HalfWidth+.15));
        if(s.Cities.Where(c=>s.Tiles[c.Tile].Explored).Any(c=>(Terrain3D.Center(s.Tiles[c.Tile])-p).Length<1.05))return;
        int tile=Terrain3D.TileAt(s,p.X,p.Y);if(tile<0||!s.Tiles[tile].Explored||!s.Tiles[tile].Passable||Navigation.RoadNode(s,tile))return;
        double y=Terrain3D.Height(s,p.X,p.Y);if(y<Terrain3D.SeaLevel)return;
        var random=new Random(unchecked(s.Seed+seed*3571));
        for(int i=0;i<5;i++)
        {
            double x=p.X+(random.NextDouble()-.5)*.12,z=p.Y+(random.NextDouble()-.5)*.12,h=.10+random.NextDouble()*.15,ground=Terrain3D.Height(s,x,z);
            var reed=batch.Mesh("river-reeds","#7D8A4F");reed.Tube(new(x,ground,z),new(x+.025,ground+h,z-.018),.0035,4);
            reed.Tube(new(x+.025,ground+h*.55,z-.018),new(x-.035,ground+h*.8,z+.018),.006,3);
            if(i%2==0)batch.Mesh("reed-heads","#8C7960").Cylinder(x+.025,ground+h,z-.018,.007,.035,sides:5);
        }
        if(seed%3==0)batch.Mesh("river-pebbles","#93998D").Sphere(p.X+.04,y+.018,p.Y,.055,.035,.04,7,4);
    }
    private static void RoadLink(SceneBatch batch,GameState s,Tile from,Tile to,RiverNetwork network)
    {
        var a=Terrain3D.Center(from);var b=Terrain3D.Center(to);var crossing=network.Crossing(a,b);
        if(crossing==null){Ribbon(batch.Mesh("roads","#C6B794"),s,a,b,.075,.036);return;}
        var direction=b-a;double length=direction.Length;direction.Normalize();
        double start=Math.Max(0,crossing.RoadT*length-crossing.HalfSpan),end=Math.Min(length,crossing.RoadT*length+crossing.HalfSpan);
        var left=a+direction*start;var right=a+direction*end;var center=new Point((left.X+right.X)/2,(left.Y+right.Y)/2);
        double deck=Math.Max(crossing.Water+.11,Math.Max(Terrain3D.Height(s,left.X,left.Y),Terrain3D.Height(s,right.X,right.Y))+.045);
        Approach(a,left,deck);Approach(b,right,deck);
        int previous=batch.TileId;batch.TileId=Terrain3D.TileAt(s,center.X,center.Y);
        int owner=from.RoadOwner>=0?from.RoadOwner:from.Owner>=0?from.Owner:to.RoadOwner>=0?to.RoadOwner:Math.Max(0,to.Owner);
        bool stone=!s.EpochCampaign||s.Factions[owner].Era>=4;
        try
        {
            batch.Rotated(new(center.X,deck,center.Y),Math.Atan2(direction.Y,direction.X),()=>
            {
                double span=end-start;var wood=batch.Mesh("bridge-deck-wood","#AD9672");
                int planks=Math.Max(4,(int)Math.Ceiling(span/.055));for(int i=0;i<planks;i++)wood.Box(center.X-span/2+(i+.5)*span/planks,deck,center.Y,span/planks*.94,.027,.20);
                foreach(int side in new[]{-1,1})
                {
                    var rail=batch.Mesh(stone?"bridge-fortification":"bridge-rails-wood",stone?"#B1AEA0":"#8B795B");
                    if(stone)rail.Box(center.X,deck+.027,center.Y+side*.105,span,.075,.032);
                    else
                    {
                        rail.Tube(new(center.X-span/2,deck+.11,center.Y+side*.105),new(center.X+span/2,deck+.11,center.Y+side*.105),.009,5);
                        for(int i=0;i<=4;i++)rail.Box(center.X-span/2+i*span/4,deck,center.Y+side*.105,.015,.13,.015);
                    }
                    for(int i=-1;i<=1;i+=2)
                    {
                        // Sample the world footprint before applying the model rotation.
                        var foot=center+direction*(i*span*.33)+new Vector(-direction.Y,direction.X)*(side*.065);double ground=Terrain3D.Height(s,foot.X,foot.Y)-.025;
                        batch.Mesh(stone?"bridge-fortification":"bridge-support-wood",stone?"#A7A593":"#7C7057").Box(center.X+i*span*.33,ground,center.Y+side*.065,stone?.055:.025,Math.Max(.03,deck-ground),.045);
                    }
                }
            });
        }
        finally{batch.TileId=previous;}
        void Approach(Point outside,Point edge,double height)
        {
            var d=edge-outside;if(d.Length<.001)return;var normal=new Vector(-d.Y,d.X);normal.Normalize();normal*=.04;int steps=Math.Max(1,(int)Math.Ceiling(d.Length/.10));
            Point3D P(double t,int side){var p=outside+d*t+normal*side;double ground=Terrain3D.Height(s,p.X,p.Y)+.036;double ramp=Math.Clamp((t-.65)/.35,0,1);return new(p.X,ground*(1-ramp)+height*ramp,p.Y);}
            for(int i=0;i<steps;i++)batch.Mesh("roads","#C6B794").Quad(P((double)i/steps,-1),P((i+1d)/steps,-1),P((i+1d)/steps,1),P((double)i/steps,1),new(0,1,0));
        }
    }
}
