using Crown.Core;
using System.Windows;

namespace Crown.Desktop;

internal static partial class WorldScene3D
{
    private static void Person(SceneBatch batch,double x,double y,double z,string color,int era,UnitKind role,bool rider=false,int identity=0)
    {
        if(era<2&&!rider){Forager(batch,x,y,z,identity);return;}
        int id=rider?(era>=5?2:1):era>=14?6:era>=12?4:era>=8?3:era>=5&&role is UnitKind.Spearmen or UnitKind.Cavalry?2:1;
        if(role==UnitKind.Settler&&era>=8)id=5;
        ActorAssets.Draw(batch,id,x,y,z,.43,rider,identity);
    }
    private static void Horse(SceneBatch batch,double x,double y,double z,double scale=1,string coat="#80654B",bool tack=false)
    {
        ActorAssets.Draw(batch,0,x,y,z,.50*scale);
        if(tack){var leather=batch.Mesh("saddle-leather","#5F5140");leather.Sphere(x,y+.384*scale,z+.02*scale,.075*scale,.018*scale,.075*scale,16,8);
            foreach(int side in new[]{-1,1})leather.Tube(new(x+side*.09*scale,y+.37*scale,z+.025*scale),new(x+side*.105*scale,y+.24*scale,z+.035*scale),.0035*scale,6);}
    }
    internal static Point UnitAnchor(GameState s,Unit unit,int slot)
    {
        if(Navy.IsShip(unit)){var center=Terrain3D.Center(s.Tiles[unit.Tile]);return new(center.X-.26+slot*.26,center.Y+.05);}
        var p=Terrain3D.Center(s.Tiles[unit.Tile]);return s.Cities.Any(c=>c.Tile==unit.Tile)?new(p.X,p.Y+.30+slot*.20):new(p.X-.34+slot*.29,p.Y+.28);
    }
}
