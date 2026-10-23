using Crown.Core;
using System.Windows;
using System.Windows.Media;

namespace Crown.Desktop;

public sealed class MiniMap : FrameworkElement
{
    public GameState State {get;set;}=null!;
    public MapView Map {get;set;}=null!;
    public MiniMap(){Height=135;MouseLeftButtonDown+=(o,e)=>{if(State==null)return;var p=e.GetPosition(this);int x=Math.Clamp((int)(p.X/ActualWidth*State.Width),0,State.Width-1),y=Math.Clamp((int)(p.Y/ActualHeight*State.Height),0,State.Height-1);Map.Center(y*State.Width+x);};}
    protected override void OnRender(DrawingContext dc)
    {
        dc.DrawRectangle(Ui.Brush("#0D1B21"),new Pen(Ui.Line,1),new Rect(RenderSize));if(State==null)return;double w=ActualWidth/State.Width,h=ActualHeight/State.Height;
        foreach(var t in State.Tiles.Where(t=>t.Explored)){Brush b=t.Owner>=0?Ui.Brush(State.Factions[t.Owner].Color):Ui.Brush(t.Passable?"#5F725D":"#385F6A");dc.DrawRectangle(b,null,new Rect(t.X*w,t.Y*h,w+.1,h+.1));}
        double factorX=ActualWidth/(State.Width*34*Math.Sqrt(3)),factorY=ActualHeight/(State.Height*51);
        var footprint=Map.Footprint;var polygon=new StreamGeometry();using(var path=polygon.Open()){path.BeginFigure(new(footprint[0].X*factorX,footprint[0].Y*factorY),false,true);foreach(var p in footprint.Skip(1))path.LineTo(new(p.X*factorX,p.Y*factorY),true,false);}polygon.Freeze();
        dc.PushClip(new RectangleGeometry(new Rect(RenderSize)));dc.DrawGeometry(null,new Pen(Ui.Gold,1),polygon);dc.Pop();
    }
}
