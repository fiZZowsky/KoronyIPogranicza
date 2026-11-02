using Crown.Core;
using System.Windows.Media.Media3D;

namespace Crown.Desktop;

internal static partial class WorldScene3D
{
    internal static System.Windows.Point DistrictAnchor(GameState s,District d)=>ResourceAnchor(s,s.Tiles[d.Tile]);
    private static void DistrictModel(SceneBatch batch,GameState s,District d)
    {
        var p=DistrictAnchor(s,d);double x=p.X,z=p.Y,y=Terrain3D.Height(s,x,z);var paving=batch.Mesh("district-stone-foundations","#A49F89");paving.Box(x,y-.025,z,.86,.035,.76);
        if(d.Status is DistrictStatus.Building or DistrictStatus.Repairing)
        {
            var timber=batch.Mesh("district-scaffold-wood","#A39065");foreach(int a in new[]{-1,1})foreach(int b in new[]{-1,1})timber.Cylinder(x+a*.3,y,z+b*.25,.014,.38,sides:5);
            timber.Box(x,y+.22,z,.68,.015,.57);for(int i=0;i<5;i++)timber.Box(x-.23+i*.1,y+.025,z+.29,.08,.035,.19);
            batch.Mesh("district-construction-walls","#C0B69B").Box(x,y,z,.42,.05+d.Work/Math.Max(1,Districts.Cost(d))*.18,.34);return;
        }
        if(d.Status==DistrictStatus.Pillaged)
        {
            var rubble=batch.Mesh("district-ruins-walls","#635E50");rubble.Box(x-.2,y,z,.16,.18,.4);rubble.Box(x+.2,y,z+.16,.24,.09,.15);
            for(int i=0;i<5;i++)rubble.Sphere(x-.25+i*.11,y+.04,z-.15,.07,.045,.06,6,3);return;
        }
        int era=s.Factions[s.Cities.Single(c=>c.Id==d.CityId).Owner].Era;
        void Block(double xx,double zz,double w,double h,double depth)
        {
            if(era<9)House(batch,xx,y,zz,w,h,depth,"#B7AE91");else{batch.Mesh("district-civic-walls","#B7AE91").Box(xx,y,zz,w,h,depth);batch.Mesh("district-flat-roof","#777F7D").Box(xx,y+h,zz,w*1.05,.025,depth*1.05);}
        }
        switch(d.Kind)
        {
            case DistrictKind.Gardens:
                for(int i=0;i<4;i++){double xx=x-.28+(i%2)*.31,zz=z-.22+(i/2)*.29;batch.Mesh("district-garden-beds","#526E42").Box(xx,y+.015,zz,.25,.025,.2);for(int j=0;j<3;j++)WheatPlant(batch,s,new(xx-.07+j*.07,zz),.10);}
                Block(x+.29,z,.17,.13,.19);break;
            case DistrictKind.Workshops:
                Block(x-.12,z,.41,.22,.33);Block(x+.25,z+.12,.20,.15,.25);batch.Mesh("district-chimney-walls","#8D8572").Cylinder(x-.2,y+.2,z+.12,.045,.24,sides:7);
                batch.Mesh("district-forge-metal","#535C58").Box(x+.20,y+.04,z-.18,.12,.09,.12);break;
            case DistrictKind.Scholars:
                batch.Mesh("district-scholar-walls","#B7AE91").Box(x,y,z,.41,.26,.33);BuildingDetails(batch,x,y,z,.41,.26,.33);
                batch.Mesh("district-scholar-dome","#6C8B8E").Sphere(x,y+.28,z,.17,.12,.15,14,7);
                for(int i=-1;i<=1;i++)batch.Mesh("district-columns","#C4BBA2").Cylinder(x+i*.11,y,z-.24,.019,.21,sides:7);break;
            case DistrictKind.Exchange:
                for(int i=0;i<4;i++){double xx=x-.25+(i%2)*.44,zz=z-.22+(i/2)*.38;var wood=batch.Mesh("district-market-wood","#9C865F");wood.Box(xx,y,zz,.21,.09,.18);wood.Box(xx,y+.19,zz,.27,.018,.23);batch.Mesh("district-awning-"+i,i%2==0?"#B6A16D":"#82917D").Box(xx,y+.20,zz,.28,.015,.24);}
                Block(x,z,.18,.16,.19);break;
            case DistrictKind.Forum:
                batch.Mesh("district-forum-stone","#BDB99F").Cylinder(x,y,z,.28,.06,sides:14);for(int i=0;i<6;i++){double a=i*Math.PI/3;batch.Mesh("district-columns","#C4BBA2").Cylinder(x+Math.Cos(a)*.26,y+.06,z+Math.Sin(a)*.26,.022,.23,sides:7);}
                batch.Mesh("district-monument-stone","#C2BEA8").Cylinder(x,y+.06,z,.075,.29,.025,8);break;
        }
    }
}
