using Crown.Core;
using System.Windows.Media.Media3D;

namespace Crown.Desktop;

internal static partial class WorldScene3D
{
    private static bool ReserveCraftYard(GameState s,City c)=>c.Buildings.Any(Manufacturing.Project)&&(c.Owner==0||Engine.Visible(s).Contains(c.Tile));
    private static bool InCraftYard(double x,double z)=>x>.20&&x<.90&&z<-.18&&z>-.80;
    private static void CraftingModel(SceneBatch batch,GameState s,City c)
    {
        var p=Terrain3D.Center(s.Tiles[c.Tile]);double x=p.X+.53,z=p.Y-.44,y=Terrain3D.Height(s,x,z);
        bool modern=s.Factions[c.Owner].Era>=8;
        if(modern)
        {
            batch.Mesh("crafting-walls","#ACA58F").Box(x,y,z,.29,.22,.26);
            batch.Mesh("crafting-roof-metal","#6A7878").Box(x,y+.22,z,.32,.017,.29);
        }
        else House(batch,x,y,z,.29,.20,.26,"#BBA78C");
        batch.Mesh("crafting-doors-wood","#6B5944").Box(x,y+.005,z+.134,.12,.13,.008);
        if(c.Buildings.Contains(Manufacturing.Toolmakers))
        {
            double ax=x-.25,az=z-.06,ay=Terrain3D.Height(s,ax,az);var metal=batch.Mesh("crafting-anvil-metal","#57625E");
            metal.Box(ax,ay,az,.10,.04,.07);metal.Box(ax,ay+.04,az,.04,.05,.04);metal.Box(ax,ay+.09,az,.14,.025,.06);
            batch.Mesh("crafting-forge-walls","#7B786B").Cylinder(x+.06,y+.21,z-.06,.025,.20,sides:7);
        }
        if(c.Buildings.Contains(Manufacturing.Weavers))
        {
            double lx=x,lz=z+.27,ly=Terrain3D.Height(s,lx,lz);var wood=batch.Mesh("crafting-loom-wood","#8B7656");
            foreach(int side in new[]{-1,1})wood.Cylinder(lx+side*.11,ly,lz,.009,.25,sides:6);
            wood.Tube(new(lx-.12,ly+.25,lz),new(lx+.12,ly+.25,lz),.009,6);
            batch.Mesh("crafting-loom-cloth","#938A9C").Quad(new(lx-.095,ly+.055,lz),new(lx+.095,ly+.055,lz),new(lx+.095,ly+.23,lz+.012),new(lx-.095,ly+.23,lz+.012),new(0,0,-1));
        }
        int stacks=Math.Clamp((int)Math.Ceiling((c.MarketStocks[TradeGood.Tools]+c.MarketStocks[TradeGood.Textiles])/8),0,6);
        for(int i=0;i<stacks;i++){double xx=x-.26+(i%2)*.075,zz=z+.13+(i/2)*.075;batch.Mesh("crafting-products-wood","#AC936B").Box(xx,Terrain3D.Height(s,xx,zz),zz,.064,.052,.064);}
        if(c.Buildings.Contains(Manufacturing.Guildhall))Flag(batch,x+.19,y,z,s.Factions[c.Owner].Color,.34);
    }
}
