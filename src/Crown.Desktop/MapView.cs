using Crown.Core;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Crown.Desktop;

public sealed class MapView : FrameworkElement
{
    public GameState State {get;set;} = null!;
    public int SelectedTile {get;set;}=-1;
    public int SelectedUnit {get;set;}=-1;
    public bool ShowResources {get;set;}=true;
    public bool ShowGrid {get;set;}
    public bool ShowSupply {get;set;}
    public bool ShowPopulation {get;set;}
    public IReadOnlyList<int> PlannedRoad {get;set;} = [];
    private int hoverTile=-1;
    public Action<int>? SelectTile;
    public Action<int>? OrderUnit;
    public Action? CameraChanged;
    private double scale=1.05,panX,panY;
    private Point down,last;
    private bool dragging,moved;
    private const double R=34;
    private static readonly Typeface Sans=new("Segoe UI"),Serif=new("Georgia");
    private readonly Dictionary<int,DrawingGroup> cache=[];
    private readonly Dictionary<string,Brush> brushes=[];
    private readonly Dictionary<int,StreamGeometry> hexes=[];
    public MapView()
    {
        ClipToBounds=true;Focusable=true;Cursor=Cursors.Arrow;
        MouseWheel+=(o,e)=> {var p=e.GetPosition(this);double old=scale;scale=Math.Clamp(scale*(e.Delta>0?1.12:.89),.42,2.1);panX=p.X-(p.X-panX)*scale/old;panY=p.Y-(p.Y-panY)*scale/old;InvalidateVisual();CameraChanged?.Invoke();};
        MouseLeftButtonDown+=(o,e)=>{Focus();down=last=e.GetPosition(this);dragging=true;moved=false;CaptureMouse();};
        MouseMove+=(o,e)=>{var p=e.GetPosition(this);if(dragging){if((p-down).Length>4)moved=true;if(moved){panX+=p.X-last.X;panY+=p.Y-last.Y;InvalidateVisual();CameraChanged?.Invoke();}last=p;}else if(State!=null){int id=Hit(p);if(id!=hoverTile){hoverTile=id;InvalidateVisual();}}};
        MouseLeave+=(o,e)=>{hoverTile=-1;InvalidateVisual();};
        MouseLeftButtonUp+=(o,e)=>{if(!dragging)return;dragging=false;ReleaseMouseCapture();if(!moved){int id=Hit(e.GetPosition(this));if(id>=0)SelectTile?.Invoke(id);}};
        MouseRightButtonUp+=(o,e)=>{int id=Hit(e.GetPosition(this));if(id>=0)OrderUnit?.Invoke(id);};
        SizeChanged+=(o,e)=>InvalidateVisual();
    }
    public void Reset(GameState state){State=state;cache.Clear();hexes.Clear();SelectedTile=state.Cities.FirstOrDefault(c=>c.Owner==0)?.Tile??state.Cities.FirstOrDefault()?.Tile??0;SelectedUnit=-1;scale=1.05;}
    public void Center(int id) {var p=CenterOf(State.Tiles[id]);panX=ActualWidth*.46-p.X*scale;panY=ActualHeight*.52-p.Y*scale;InvalidateVisual();CameraChanged?.Invoke();}
    public Rect Viewport => new(-panX/scale,-panY/scale,ActualWidth/scale,ActualHeight/scale);
    public static Point CenterOf(Tile t)=>new(R*Math.Sqrt(3)*(t.X+(t.Y&1)*.5)+R,R*1.5*t.Y+R);
    private int Hit(Point p)
    {
        double wx=(p.X-panX)/scale,wy=(p.Y-panY)/scale;int y=(int)Math.Round((wy-R)/(R*1.5));
        int best=-1;double d=double.MaxValue;for(int yy=y-1;yy<=y+1;yy++)
        {
            if(yy<0||yy>=State.Height)continue;int x=(int)Math.Round((wx-R)/(R*Math.Sqrt(3))-(yy&1)*.5);
            for(int xx=x-1;xx<=x+1;xx++)if(xx>=0&&xx<State.Width){var t=State.Tiles[yy*State.Width+xx];double a=(CenterOf(t)-new Point(wx,wy)).LengthSquared;if(a<d){d=a;best=t.Id;}}
        }return d<R*R?best:-1;
    }
    private Brush B(string c){if(!brushes.TryGetValue(c,out var b)){b=Ui.Brush(c);brushes[c]=b;}return b;}
    private StreamGeometry Hex(Tile t)
    {
        if(hexes.TryGetValue(t.Id,out var g))return g;g=new();using(var ctx=g.Open()){var p=CenterOf(t);for(int i=0;i<6;i++){double a=(30+60*i)*Math.PI/180;var v=new Point(p.X+R*Math.Cos(a),p.Y+R*Math.Sin(a));if(i==0)ctx.BeginFigure(v,true,true);else ctx.LineTo(v,true,false);}}g.Freeze();hexes[t.Id]=g;return g;
    }
    private static StreamGeometry Poly(params Point[] points){var g=new StreamGeometry();using(var c=g.Open()){c.BeginFigure(points[0],true,true);foreach(var p in points.Skip(1))c.LineTo(p,true,false);}g.Freeze();return g;}
    private void Text(DrawingContext dc,string text,Point p,double size,Brush color,bool serif=false)
    {
        var ft=new FormattedText(text,CultureInfo.GetCultureInfo("pl-PL"),FlowDirection.LeftToRight,serif?Serif:Sans,size,color,1);dc.DrawText(ft,new(p.X-ft.Width/2,p.Y));
    }
    protected override void OnRender(DrawingContext dc)
    {
        dc.DrawRectangle(B("#10232B"),null,new Rect(RenderSize));if(State==null)return;
        dc.PushTransform(new TranslateTransform(panX,panY));dc.PushTransform(new ScaleTransform(scale,scale));var bounds=Viewport;bounds.Inflate(R*3,R*3);
        var visible=Engine.Visible(State);var tiles=State.Tiles.Where(t=>bounds.Contains(CenterOf(t))).ToArray();
        var supply=ShowSupply?Logistics.For(State,0):null;
        foreach(var t in tiles)
        {
            var p=CenterOf(t);if(!t.Explored)
            {
                dc.DrawGeometry(B((t.Id*17%5)==0?"#1B3035":"#1A2D33"),new Pen(B("#263C40"),.5),Hex(t));
                if(t.Id%37==0)Text(dc,"·",p,20,B("#3B5052"));continue;
            }
            if(!cache.TryGetValue(t.Id,out var drawing)){drawing=new DrawingGroup();using(var d=drawing.Open())DrawTerrain(d,t);drawing.Freeze();cache[t.Id]=drawing;}dc.DrawDrawing(drawing);
            if(t.Improvement!=Improvement.None)DrawImprovement(dc,t);
            if(Navigation.RoadNode(State,t.Id))DrawRoad(dc,t);
            if(t.Owner>=0)
            {
                Color col=(Color)ColorConverter.ConvertFromString(State.Factions[t.Owner].Color);dc.DrawGeometry(new SolidColorBrush(Color.FromArgb(21,col.R,col.G,col.B)),null,Hex(t));
                foreach(var n in World.Neighbors(State,t.Id))if(n.Owner!=t.Owner)
                {
                    var np=CenterOf(n);var mid=new Point((np.X+p.X)/2,(np.Y+p.Y)/2);Vector tangent=np-p;tangent.Normalize();var v=new Vector(-tangent.Y,tangent.X)*R*.5;
                    dc.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(200,col.R,col.G,col.B)),2),mid-v,mid+v);
                }
            }
            if(ShowGrid)dc.DrawGeometry(null,new Pen(B("#667B72"),.55),Hex(t));
            if(supply!=null&&t.Passable)
            {
                bool blocked=supply.Interdicted.Contains(t.Id),inRange=supply.Costs.GetValueOrDefault(t.Id,double.PositiveInfinity)<=Logistics.Range(State,0);
                dc.DrawGeometry(B(blocked?"#809C652F":inRange?"#4A369B77":"#4AAA5552"),null,Hex(t));
            }
            if(ShowPopulation&&t.Owner==0)
            {
                var city=State.Cities.Where(c=>c.Owner==0&&World.Distance(t,State.Tiles[c.Tile])<=2).OrderBy(c=>World.Distance(t,State.Tiles[c.Tile])).FirstOrDefault();
                if(city!=null)dc.DrawGeometry(B(city.Prosperity<40?"#65AF5947":city.Prosperity<65?"#55BEA164":"#5537AB8B"),null,Hex(t));
            }
            if(t.Ruin){dc.DrawRectangle(B("#8A8064"),null,new Rect(p.X-7,p.Y-4,4,12));dc.DrawRectangle(B("#B0A384"),null,new Rect(p.X+3,p.Y-8,4,16));dc.DrawLine(new Pen(B("#BFB08A"),3),new(p.X-9,p.Y-9),new(p.X+9,p.Y-12));}
            if(ShowResources&&t.Resource!=Resource.None)
            {
                var at=new Point(p.X+16,p.Y+15);dc.DrawEllipse(B("#D91B2929"),new Pen(B("#B3A37D"),.65),at,8,8);
                Text(dc,t.Resource switch{Resource.Grain=>"麦",Resource.Iron=>"Fe",Resource.Horses=>"H",Resource.Stone=>"◆",Resource.Salt=>"S",_=>"✿"},new(at.X,at.Y-6),8,B("#E4D2A3"));
            }
            if(!visible.Contains(t.Id))dc.DrawGeometry(B("#6F0C1A22"),null,Hex(t));
        }
        if(SelectedUnit>=0)
        {
            var u=State.Units.FirstOrDefault(u=>u.Id==SelectedUnit&&u.Owner==0);
            if(u!=null)
            {
                foreach(var reachable in Navigation.ForUnit(State,u,u.Actions).Costs)
                {
                    var t=State.Tiles[reachable.Key];if(reachable.Key==u.Tile||!bounds.Contains(CenterOf(t)))continue;
                    dc.DrawGeometry(B("#242CFFFF"),new Pen(B("#778BC8BB"),1),Hex(t));
                }
                if(u.Destination is int goal)DrawRoute(dc,Navigation.Route(State,u,goal),u.Actions,true);
                if(hoverTile>=0&&State.Tiles[hoverTile].Explored&&hoverTile!=u.Tile)
                    DrawRoute(dc,Navigation.Route(State,u,hoverTile),u.Actions,false);
            }
        }
        DrawRoute(dc,PlannedRoad,double.PositiveInfinity,false);
        if(ShowPopulation)DrawPopulationFlows(dc);
        foreach(var c in State.Cities.Where(c=>State.Tiles[c.Tile].Explored&&bounds.Contains(CenterOf(State.Tiles[c.Tile]))))DrawCity(dc,c);
        foreach(var group in State.Units.Where(u=>bounds.Contains(CenterOf(State.Tiles[u.Tile]))&&(u.Owner==0||visible.Contains(u.Tile))).GroupBy(u=>u.Tile))
        {
            var u=group.FirstOrDefault(u=>u.Id==SelectedUnit)??group.First();DrawUnit(dc,u,group.Count());
        }
        if(SelectedTile>=0&&State.Tiles[SelectedTile].Explored)dc.DrawGeometry(null,new Pen(B("#F4DEA4"),2.5),Hex(State.Tiles[SelectedTile]));
        dc.Pop();dc.Pop();
        if(ShowSupply)Text(dc,"DOSTAWY  ·  zielony: docierają   /   czerwony: brak   /   pomarańczowy: blokada",new(ActualWidth*.5,ActualHeight-32),11,B("#E4E2C8"));
        if(ShowPopulation)Text(dc,"DOBROBYT  ·  czerwony: niski / zielony: wysoki  ·  złoto: żywność / błękit: migracje",new(ActualWidth*.5,ActualHeight-32),10,B("#E4E2C8"));
        var fade=new LinearGradientBrush(Color.FromArgb(90,6,17,23),Colors.Transparent,90);dc.DrawRectangle(fade,null,new Rect(0,0,ActualWidth,90));
    }
    private void DrawPopulationFlows(DrawingContext dc)
    {
        void Flow(int source,int destination,string text,string color,double offset)
        {
            var from=State.Cities.First(c=>c.Id==source);var to=State.Cities.First(c=>c.Id==destination);
            var a=CenterOf(State.Tiles[from.Tile]);var b=CenterOf(State.Tiles[to.Tile]);
            Vector direction=b-a;if(direction.Length<1)return;direction.Normalize();var end=b-direction*32;
            var normal=new Vector(-direction.Y,direction.X);if(source>destination)normal=-normal;
            var shift=normal*offset;a+=shift;b+=shift;end+=shift;
            dc.DrawLine(new Pen(B(color),2.2){DashStyle=DashStyles.Dash},a,end);
            var tangent=new Vector(-direction.Y,direction.X)*4;
            dc.DrawGeometry(B(color),null,Poly(end,end-direction*11+tangent,end-direction*11-tangent));
            var mid=new Point((a.X+b.X)/2,(a.Y+b.Y)/2);dc.DrawRoundedRectangle(B("#E015282D"),null,new Rect(mid.X-25,mid.Y-9,50,20),3,3);
            Text(dc,text,new(mid.X,mid.Y-7),9,B(color));
        }
        foreach(var shipment in Regions.PlanFood(State,0).Shipments)Flow(shipment.Source,shipment.Destination,$"{shipment.Amount:0.#} żyw.","#E1C188",-16);
        foreach(var movement in State.Migrations.Where(m=>m.Owner==0&&State.Turn-m.Turn<=20).TakeLast(3))
            Flow(movement.Source,movement.Destination,"+1 os.","#91CBDD",16);
    }
    private void DrawRoad(DrawingContext dc,Tile tile)
    {
        var p=CenterOf(tile);
        if(tile.Road)dc.DrawEllipse(B("#B5A178"),null,p,2.5,2);
        foreach(var next in World.Neighbors(State,tile.Id).Where(t=>t.Explored&&Navigation.RoadNode(State,t.Id)))
        {
            var q=CenterOf(next);var mid=new Point((p.X+q.X)/2,(p.Y+q.Y)/2);
            dc.DrawLine(new Pen(B("#765B4A36"),5),p,mid);
            dc.DrawLine(new Pen(B("#C1B18A"),2.2),p,mid);
            dc.DrawLine(new Pen(B("#807C6B4C"),.6),p,mid);
        }
    }
    private void DrawRoute(DrawingContext dc,IReadOnlyList<int> path,double budget,bool standingOrder)
    {
        double spent=0;
        for(int i=1;i<path.Count;i++)
        {
            spent+=Navigation.MovementCost(State,path[i-1],path[i]);
            var pen=new Pen(B(spent<=budget?"#C4E2C7":"#D1B273"),standingOrder?2.6:1.7){DashStyle=DashStyles.Dash};
            dc.DrawLine(pen,CenterOf(State.Tiles[path[i-1]]),CenterOf(State.Tiles[path[i]]));
        }
        if(path.Count>1)dc.DrawEllipse(null,new Pen(B("#EAD6A0"),1.8),CenterOf(State.Tiles[path[^1]]),7,7);
    }
    private void DrawTerrain(DrawingContext dc,Tile t)
    {
        var p=CenterOf(t);string[] colors=t.Terrain switch
        {
            Terrain.Ocean=>["#244A59","#193A49"],Terrain.Coast=>["#427780","#2F5F6B"],Terrain.Forest=>["#52644B","#394F3C"],Terrain.Hills=>["#8C8A65","#62694F"],Terrain.Mountain=>["#9B9B8C","#656E67"],Terrain.Desert=>["#C4AD79","#A28B60"],Terrain.Tundra=>["#9CA590","#7C8C78"],Terrain.Marsh=>["#6D8260","#4B6A59"],_=>["#9B9B65","#788650"]
        };
        var gradient=new LinearGradientBrush((Color)ColorConverter.ConvertFromString(colors[0]),(Color)ColorConverter.ConvertFromString(colors[1]),60);
        dc.DrawGeometry(gradient,new Pen(B(colors[1]),.7),Hex(t));dc.PushClip(Hex(t));
        dc.DrawImage(TerrainTexture.Create(t,State.Seed),new Rect(p.X-R,p.Y-R,R*2,R*2));
        var rand=new Random(unchecked(t.Id*7927+State.Seed));
        for(int i=0;i<18;i++)
        {
            double x=p.X+rand.Next(-32,33),y=p.Y+rand.Next(-31,32);var light=i%3==0?B("#10FFEDB1"):B("#0F1A2D21");
            dc.DrawEllipse(light,null,new Point(x,y),rand.Next(2,10),rand.Next(1,4));
        }
        if(t.Terrain is Terrain.Ocean or Terrain.Coast)
        {
            for(int i=0;i<5;i++){double y=p.Y-22+i*10+rand.Next(5),x=p.X-25+rand.Next(14);dc.DrawLine(new Pen(B("#2DADD0C9"),.7),new(x,y),new(x+rand.Next(7,28),y-1));}
            if(t.Terrain==Terrain.Coast)foreach(var n in World.Neighbors(State,t.Id).Where(n=>n.Passable)){var np=CenterOf(n);var mid=new Point((p.X+np.X)/2,(p.Y+np.Y)/2);dc.DrawEllipse(B("#4CA6C9B3"),null,mid,14,11);}
        }
        if(t.Terrain==Terrain.Forest)
        {
            for(int i=0;i<26;i++)
            {
                double x=p.X+rand.Next(-29,30),y=p.Y+rand.Next(-25,27),r=rand.Next(3,7);
                dc.DrawEllipse(B("#55232D23"),null,new(x+3,y+3),r*1.3,r*.6);
                dc.DrawEllipse(B("#314A35"),null,new(x,y),r,r*.8);
                dc.DrawEllipse(B(i%4==0?"#7D8B50":"#5B7546"),null,new(x-1,y-2),r*.8,r*.7);
                dc.DrawEllipse(B("#2BBDCC7A"),null,new(x-2,y-3),r*.4,r*.25);
            }
        }
        if(t.Terrain==Terrain.Mountain)
        {
            for(int i=0;i<6;i++)
            {
                double x=p.X+rand.Next(-22,23),y=p.Y+rand.Next(-20,21);
                dc.DrawLine(new Pen(B("#42667663"),.8),new(x,y),new(x+rand.Next(4,12),y+rand.Next(4,14)));
            }
        }
        if(t.River)
        {
            var ns=World.Neighbors(State,t.Id).Where(n=>n.River&&n.Y!=t.Y).ToArray();
            Point start=new(p.X-5,p.Y-R),end=new(p.X+4,p.Y+R);
            var a=ns.FirstOrDefault(n=>n.Y<t.Y);var b=ns.FirstOrDefault(n=>n.Y>t.Y);
            if(a!=null){var q=CenterOf(a);start=new((p.X+q.X)/2,(p.Y+q.Y)/2);}
            if(b!=null){var q=CenterOf(b);end=new((p.X+q.X)/2,(p.Y+q.Y)/2);}
            var river=new StreamGeometry();using(var path=river.Open()){path.BeginFigure(start,false,false);path.QuadraticBezierTo(p,end,true,false);}
            dc.DrawGeometry(null,new Pen(B("#727A54"),8),river);dc.DrawGeometry(null,new Pen(B("#6298A0"),4),river);dc.DrawGeometry(null,new Pen(B("#A9C6BB"),.7),river);
        }
        dc.Pop();
    }
    private void DrawImprovement(DrawingContext dc,Tile t)
    {
        var p=CenterOf(t);if(t.Improvement==Improvement.Farm)
        {
            dc.DrawGeometry(B("#B5A26A"),new Pen(B("#68724D"),1),Poly(new(p.X-19,p.Y-8),new(p.X+14,p.Y-13),new(p.X+21,p.Y+8),new(p.X-13,p.Y+14)));
            for(int i=0;i<6;i++)dc.DrawLine(new Pen(B("#837D49"),1),new(p.X-15+i*5,p.Y-8),new(p.X-9+i*5,p.Y+10));
        }
        else if(t.Improvement==Improvement.Mine){dc.DrawEllipse(B("#394139"),new Pen(B("#9C9473"),4),p,10,7);dc.DrawLine(new Pen(B("#BDA174"),3),new(p.X-12,p.Y+10),new(p.X-6,p.Y-12));dc.DrawLine(new Pen(B("#BDA174"),3),new(p.X+12,p.Y+10),new(p.X+6,p.Y-12));}
        else House(dc,p.X,p.Y,14,12);
    }
    private void House(DrawingContext dc,double x,double y,double w,double h)
    {
        dc.DrawEllipse(B("#45202B21"),null,new(x+4,y+4),w*.7,4);dc.DrawRectangle(B("#C3B68C"),null,new Rect(x-w/2,y-h,w,h));dc.DrawRectangle(B("#8C876B"),null,new Rect(x,y-h,w/2,h));
        dc.DrawGeometry(B("#825E49"),null,Poly(new(x-w/2-2,y-h),new(x,y-h-7),new(x+w/2+2,y-h)));dc.DrawRectangle(B("#514F42"),null,new Rect(x-2,y-5,3,5));
    }
    private void DrawCity(DrawingContext dc,City c)
    {
        var p=CenterOf(State.Tiles[c.Tile]);var color=B(State.Factions[c.Owner].Color);var random=new Random(c.Tile*19);
        dc.DrawEllipse(B("#526C7353"),null,new(p.X,p.Y+3),27,17);
        if(c.Buildings.Contains("walls"))dc.DrawEllipse(null,new Pen(B("#B6AF94"),4),new(p.X,p.Y),27,18);
        for(int i=0;i<12;i++)House(dc,p.X+random.Next(-22,23),p.Y+random.Next(-8,14),random.Next(5,10),random.Next(5,11));
        House(dc,p.X,p.Y-5,13,23);dc.DrawLine(new Pen(B("#E1CE9C"),1),new(p.X,p.Y-34),new(p.X,p.Y-49));dc.DrawGeometry(color,null,Poly(new(p.X,p.Y-49),new(p.X+13,p.Y-45),new(p.X,p.Y-40)));
        string name=c.Name.ToUpperInvariant();var ft=new FormattedText(name,CultureInfo.GetCultureInfo("pl-PL"),FlowDirection.LeftToRight,Sans,10,B("#F0E8D4"),1);double width=Math.Max(100,ft.Width+40);
        var rect=new Rect(p.X-width/2,p.Y+31,width,23);dc.DrawRoundedRectangle(B("#F014252A"),new Pen(color,.8),rect,2,2);dc.DrawText(ft,new(p.X-ft.Width/2-8,p.Y+35));Text(dc,c.Population.ToString(),new(p.X+width/2-13,p.Y+34),10,color);
        if(c.Health<100){dc.DrawRectangle(B("#202E28"),null,new Rect(p.X-22,p.Y+23,44,3));dc.DrawRectangle(B("#D09D71"),null,new Rect(p.X-22,p.Y+23,44*c.Health/100,3));}
    }
    private void DrawUnit(DrawingContext dc,Unit u,int count)
    {
        var p=CenterOf(State.Tiles[u.Tile]);p.Offset(-15,-16);bool selected=u.Id==SelectedUnit;var col=B(State.Factions[u.Owner].Color);
        dc.DrawEllipse(B("#4C0E1514"),null,new(p.X+3,p.Y+5),13,8);dc.DrawEllipse(B(selected?"#E2D4AB":"#172D32"),new Pen(col,selected?2.5:1.7),p,12,12);
        Brush symbol=selected?B("#172D32"):col;
        if(u.Kind==UnitKind.Settler){dc.DrawEllipse(null,new Pen(symbol,1.4),new(p.X,p.Y-3),3,3);dc.DrawLine(new Pen(symbol,1.4),new(p.X,p.Y),new(p.X,p.Y+7));dc.DrawLine(new Pen(symbol,1.4),new(p.X-5,p.Y+3),new(p.X+5,p.Y+3));}
        else if(u.Kind==UnitKind.Archer){var g=new StreamGeometry();using(var x=g.Open()){x.BeginFigure(new(p.X-3,p.Y-7),false,false);x.QuadraticBezierTo(new(p.X+9,p.Y),new(p.X-3,p.Y+7),true,false);}dc.DrawGeometry(null,new Pen(symbol,1.7),g);dc.DrawLine(new Pen(symbol,1),new(p.X-3,p.Y-7),new(p.X-3,p.Y+7));}
        else Text(dc,u.Kind switch{UnitKind.Scout=>"◇",UnitKind.Cavalry=>"♞",UnitKind.Catapult=>"⊕",_=>"⚔"},new(p.X,p.Y-10),15,symbol);
        dc.DrawRectangle(B("#20312B"),null,new Rect(p.X-9,p.Y+15,18,2));dc.DrawRectangle(B(u.Health>50?"#A7BE8A":"#D58B6D"),null,new Rect(p.X-9,p.Y+15,18*u.Health/100,2));
        if(count>1){dc.DrawEllipse(B("#C9B07B"),null,new(p.X+12,p.Y-10),6,6);Text(dc,count.ToString(),new(p.X+12,p.Y-16),8,B("#13252A"));}
    }
}

public sealed class MiniMap : FrameworkElement
{
    public GameState State {get;set;}=null!;
    public MapView Map {get;set;}=null!;
    public MiniMap(){Height=135;MouseLeftButtonDown+=(o,e)=>{if(State==null)return;var p=e.GetPosition(this);int x=Math.Clamp((int)(p.X/ActualWidth*State.Width),0,State.Width-1),y=Math.Clamp((int)(p.Y/ActualHeight*State.Height),0,State.Height-1);Map.Center(y*State.Width+x);};}
    protected override void OnRender(DrawingContext dc)
    {
        dc.DrawRectangle(Ui.Brush("#0D1B21"),new Pen(Ui.Line,1),new Rect(RenderSize));if(State==null)return;double w=ActualWidth/State.Width,h=ActualHeight/State.Height;
        foreach(var t in State.Tiles.Where(t=>t.Explored)){Brush b=t.Owner>=0?Ui.Brush(State.Factions[t.Owner].Color):Ui.Brush(t.Passable?"#5F725D":"#385F6A");dc.DrawRectangle(b,null,new Rect(t.X*w,t.Y*h,w+.1,h+.1));}
        var view=Map.Viewport;double factorX=ActualWidth/(State.Width*34*Math.Sqrt(3)),factorY=ActualHeight/(State.Height*51);var r=new Rect(view.X*factorX,view.Y*factorY,view.Width*factorX,view.Height*factorY);r.Intersect(new Rect(RenderSize));if(!r.IsEmpty)dc.DrawRectangle(null,new Pen(Ui.Gold,1),r);
    }
}
