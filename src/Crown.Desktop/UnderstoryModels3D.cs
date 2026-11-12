using Crown.Core;
using System.Windows;
using System.Windows.Media.Media3D;

namespace Crown.Desktop;

internal static partial class WorldScene3D
{
    private static void UnderstoryClump(SceneBatch batch,GameState s,Point p,double height,int variant,double yaw)
    {
        int tile=Terrain3D.TileAt(s,p.X,p.Y);if(tile<0||!s.Tiles[tile].Explored)return;
        int previous=batch.TileId;batch.TileId=tile;
        try
        {
        double y=Terrain3D.Height(s,p.X,p.Y);
        string key=variant switch{0=>"ground-grass-textured",1=>"wild-understory",2=>"river-reeds-textured",_=>"wild-herbs-textured"};
        var mesh=batch.Mesh(key,"#FFFFFF");double width=height*(variant is 1 or 3?.80:variant==0?1.0:.58);
        for(int card=0;card<2;card++)
        {
            double angle=yaw+card*Math.PI*.5;
            // The transparent root margin sinks into the ground instead of hovering above it.
            mesh.FoliageCard(new(p.X,y+height*.43,p.Y),new(Math.Cos(angle)*width,0,Math.Sin(angle)*width),new(0,height*.5,0));
        }
        }finally{batch.TileId=previous;}
    }
}
