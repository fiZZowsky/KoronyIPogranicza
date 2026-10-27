using Crown.Core;
using System.Windows;
using System.Windows.Media.Media3D;

namespace Crown.Desktop;

internal static partial class WorldScene3D
{
    internal static Point ResourceAnchor(GameState s,Tile tile)
    {
        var center=Terrain3D.Center(tile);var rivers=TerrainField.Current(s).Rivers;
        var offsets=new[]{new Vector(.49,.17),new Vector(.42,-.43),new Vector(-.46,-.32),new Vector(-.42,.42)};
        foreach(var offset in offsets){var p=center+offset;if(rivers.Distance(p)>.48&&Terrain3D.Height(s,p.X,p.Y)>Terrain3D.SeaLevel+.025)return p;}
        return center+offsets[0];
    }
    private static void ResourceModel(SceneBatch batch,GameState s,Tile tile)
    {
        var p=ResourceAnchor(s,tile);var random=new Random(unchecked(s.Seed+tile.Id*3571));
        switch(tile.Resource)
        {
            case Resource.Grain:
                for(int row=0;row<3;row++)for(int col=0;col<4;col++)WheatPlant(batch,s,new(p.X+(col-1.5)*.075,p.Y+(row-1)*.09),.16+random.NextDouble()*.055);
                break;
            case Resource.Horses:
                for(int i=0;i<2;i++)
                {
                    double x=p.X+(i-.5)*.20,z=p.Y+(i-.5)*.15,y=Terrain3D.Height(s,x,z),yaw=i==0?-.5:.9;
                    batch.Rotated(new(x,y,z),yaw,()=>Horse(batch,x,y,z,.60,i==0?"#876747":"#AAA99B"));
                }
                break;
            case Resource.Dyes:
                for(int i=0;i<14;i++)FlowerPlant(batch,s,new(p.X+(random.NextDouble()-.5)*.43,p.Y+(random.NextDouble()-.5)*.34),.10+random.NextDouble()*.10,true);
                break;
            case Resource.Salt:
                Rock(batch,s,p,.20,.055,tile.Id,"salt-bed","#CFCEB8");
                for(int i=0;i<8;i++)
                {
                    double x=p.X+(random.NextDouble()-.5)*.28,z=p.Y+(random.NextDouble()-.5)*.25,y=Terrain3D.Height(s,x,z),size=.035+random.NextDouble()*.035;
                    batch.Rotated(new(x,y,z),random.NextDouble()*Math.PI,()=>batch.Mesh("salt-crystals","#DCE3D8").Box(x,y,z,size,size*(.7+random.NextDouble()),size));
                }
                break;
            default:
                for(int i=0;i<4;i++)
                {
                    var q=new Point(p.X+(random.NextDouble()-.5)*.28,p.Y+(random.NextDouble()-.5)*.26);
                    Rock(batch,s,q,.10+random.NextDouble()*.07,.08+random.NextDouble()*.12,tile.Id*7+i,tile.Resource==Resource.Iron?"iron-ore":"stone-outcrop",tile.Resource==Resource.Iron?"#777F7C":"#B1AE9D");
                }
                break;
        }
    }
    private static void Rock(SceneBatch batch,GameState s,Point center,double radius,double height,int seed,string key,string color)
    {
        var random=new Random(seed);const int sides=9;var radiusVariation=Enumerable.Range(0,sides).Select(_=>.74+random.NextDouble()*.35).ToArray();double y=Terrain3D.Height(s,center.X,center.Y)-height*.18;
        Point3D P(int row,int side)
        {
            double a=side*Math.PI*2/sides,r=radius*radiusVariation[side]*(row==0?.92:row==1?1:.38);return new(center.X+Math.Cos(a)*r+(row==2?radius*.13:0),row==0?Terrain3D.Height(s,center.X+Math.Cos(a)*r,center.Y+Math.Sin(a)*r)-.025:y+height*(row==1?.58:1),center.Y+Math.Sin(a)*r);
        }
        var mesh=batch.Mesh(key,color);for(int row=0;row<2;row++)for(int i=0;i<sides;i++)mesh.Quad(P(row,i),P(row,(i+1)%sides),P(row+1,(i+1)%sides),P(row+1,i),new(Math.Cos(i*Math.PI*2/sides),.5,Math.Sin(i*Math.PI*2/sides)));
        for(int i=0;i<sides;i++)mesh.Triangle(new(center.X+radius*.13,y+height,center.Y),P(2,(i+1)%sides),P(2,i));
    }
    private static void WheatPlant(SceneBatch batch,GameState s,Point p,double height)
    {
        double y=Terrain3D.Height(s,p.X,p.Y);var stem=batch.Mesh("crop-stems-wood","#B9A268");stem.Tube(new(p.X,y,p.Y),new(p.X+.014,y+height,p.Y),.0026,4);
        var grain=batch.Mesh("wheat-grain","#D4BA79");
        for(int i=0;i<3;i++)foreach(int side in new[]{-1,1})grain.Sphere(p.X+.014+side*.008,y+height-.035+i*.014,p.Y,.009,.012,.006,6,3);
        Blade(batch,new(p.X,y+height*.3,p.Y),.052,height*.35,"crop-leaves","#939953",.6);
    }
    private static void FlowerPlant(SceneBatch batch,GameState s,Point p,double height,bool dye)
    {
        double y=Terrain3D.Height(s,p.X,p.Y);batch.Mesh("plant-stems","#6D8251").Tube(new(p.X,y,p.Y),new(p.X,y+height,p.Y),.0028,4);
        Blade(batch,new(p.X,y+height*.25,p.Y),.046,height*.35,"plant-leaves","#788E53",.9);
        var petals=batch.Mesh(dye?"dye-petals":"wild-petals",dye?"#A77BAA":"#DCCBA0");var center=new Point3D(p.X,y+height,p.Y);
        for(int i=0;i<5;i++)
        {
            double a=i*Math.PI*2/5;var along=new Vector3D(Math.Cos(a)*.024,.002,Math.Sin(a)*.024);var across=new Vector3D(-Math.Sin(a)*.008,0,Math.Cos(a)*.008);
            petals.Quad(center,center+along*.6+across,center+along,center+along*.6-across,new(0,1,0));
        }
        batch.Mesh("flower-centers","#D4BC68").Sphere(p.X,y+height+.004,p.Y,.006,.004,.006,6,3);
    }
    private static void Blade(SceneBatch batch,Point3D root,double width,double height,string key,string color,double angle)
    {
        var right=new Vector3D(Math.Cos(angle)*width*.14,0,Math.Sin(angle)*width*.14);var bend=new Vector3D(-Math.Sin(angle)*width*.5,0,Math.Cos(angle)*width*.5);var tip=root+new Vector3D(0,height,0)+bend;var middle=root+new Vector3D(0,height*.6,0)+bend*.3;
        var mesh=batch.Mesh(key,color);mesh.Triangle(root-right,root+right,middle+right,null,null,null,new(0,0),new(1,0),new(1,.6));mesh.Triangle(root-right,middle+right,middle-right,null,null,null,new(0,0),new(1,.6),new(0,.6));mesh.Triangle(middle-right,middle+right,tip,null,null,null,new(0,.6),new(1,.6),new(.5,1));
    }
    private static void GroundPlants(SceneBatch batch,GameState s,Tile tile)
    {
        if(tile.Improvement!=Improvement.None||tile.Ruin||Navigation.RoadNode(s,tile.Id)||s.Cities.Any(c=>c.Tile==tile.Id))return;
        if(tile.Terrain is not (Terrain.Grassland or Terrain.Marsh or Terrain.Tundra or Terrain.Forest))return;
        var center=Terrain3D.Center(tile);var random=new Random(unchecked(s.Seed+tile.Id*7723));
        for(int i=0;i<4;i++)
        {
            var p=new Point(center.X+(random.NextDouble()-.5)*1.3,center.Y+(random.NextDouble()-.5)*1.3);
            if(TerrainField.Current(s).Rivers.Distance(p)<.45||tile.Resource!=Resource.None&&(ResourceAnchor(s,tile)-p).Length<.38)continue;
            double y=Terrain3D.Height(s,p.X,p.Y);if(y<Terrain3D.SeaLevel+.02)continue;
            for(int j=0;j<6;j++)Blade(batch,new(p.X+(random.NextDouble()-.5)*.07,y,p.Y+(random.NextDouble()-.5)*.07),.028,.04+random.NextDouble()*.05,"ground-grass",tile.Terrain==Terrain.Tundra?"#969575":"#8A9A62",random.NextDouble()*Math.PI);
            if(tile.Terrain==Terrain.Grassland&&i==1&&tile.Resource==Resource.None)FlowerPlant(batch,s,p,.10,false);
        }
    }
}
