using Crown.Core;

namespace Crown.Desktop;

internal static partial class WorldScene3D
{
    private static bool ReserveDepotYard(GameState s,City c)=>FieldLogistics.Active(s,c)&&(c.Owner==0||Engine.Visible(s).Contains(c.Tile));
    private static bool InDepotYard(double x,double z)=>x<-.20&&x>-.90&&z>.18&&z<.80;
    private static void DepotModel(SceneBatch batch,GameState s,City c)
    {
        var center=Terrain3D.Center(s.Tiles[c.Tile]);double x=center.X-.57,z=center.Y+.48,y=Terrain3D.Height(s,x,z);
        bool industrial=s.Factions[c.Owner].Era>=8;
        if(industrial)
        {
            batch.Mesh("logistics-depot-walls","#A7A392").Box(x,y,z,.29,.18,.23);
            batch.Mesh("logistics-depot-metal","#687872").Box(x,y+.18,z,.32,.018,.26);
            batch.Mesh("logistics-depot-doors-wood","#5F6253").Box(x,y+.008,z-.119,.13,.14,.009);
        }
        else
        {
            House(batch,x,y,z,.29,.17,.23,"#B2A48A");
            batch.Mesh("logistics-depot-doors-wood","#706047").Box(x,y+.008,z-.119,.12,.12,.009);
        }
        var wood=batch.Mesh("logistics-depot-crates-wood","#9E845D");
        int crates=Math.Clamp((int)Math.Ceiling(c.MilitarySupplies/12),0,10);
        for(int i=0;i<crates;i++)
        {
            double xx=x+.23+(i%2)*.075,zz=z-.12+(i/2%3)*.075,yy=Terrain3D.Height(s,xx,zz)+(i/6)*.06;
            wood.Box(xx,yy,zz,.063,.057,.063);
            batch.Mesh("logistics-depot-bands-metal","#59615A").Box(xx,yy+.058,zz,.012,.003,.066);
        }
        Flag(batch,x-.19,y,z,s.Factions[c.Owner].Color,.30);
    }
}
