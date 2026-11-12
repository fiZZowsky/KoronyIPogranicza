using Crown.Core;
using System.Windows.Media.Media3D;

namespace Crown.Desktop;

internal static partial class WorldScene3D
{
    internal static readonly string[] ArchitectureNames=["Obozowiska i kamienie pamięci","Długie domy i spichlerze","Miasta tarasowe z brązu","Kamienne strażnice","Portyki i forum","Strzeliste wieże i szachulec","Kopuły i pałace","Kolumnady i instytucje publiczne","Ceglane hale i kominy","Miasto elektryczne i art déco","Tarasowe centra publiczne","Szklane wieże sieciowe","Kampusy automatyzacji","Zielone arkologie","Antenny i pierścienie orbitalne","Habitaty i kopuły","Wielopoziomowe biosfery","Smukłe megastruktury"];
    private static void EraCivic(SceneBatch b,GameState s,City c,int era)
    {
        var p=Terrain3D.Center(s.Tiles[c.Tile]);double x=p.X,z=p.Y,y=Terrain3D.Height(s,x,z);
        var stone=b.Mesh("era-stone-walls-"+era,"#C6BEA8");var brick=b.Mesh("era-brick-"+era,"#AD8065");
        var metal=b.Mesh("era-metal-"+era,"#849494");var glass=b.Mesh("era-glass-"+era,"#61858F");
        var wood=b.Mesh("era-timber-"+era,"#AB957B");var concrete=b.Mesh("era-concrete-"+era,"#C2C6BC");
        void Columns(double roof,int count=6){for(int i=0;i<count;i++)stone.Cylinder(x-.23+i*.46/(count-1),y+.04,z+.17,.018,roof-.04,sides:8);stone.Box(x,y+roof,z,.57,.045,.46);}
        void Windows(double height,int floors){var m=b.Mesh("detail-era-glass-"+era,"#526A70");for(int f=0;f<floors;f++)for(int col=-2;col<=2;col++){double yy=y+.075+f*height/floors;foreach(int side in new[]{-1,1})m.Box(x+col*.075,yy,z+side*.202,.039,.045,.006);}}
        switch(era)
        {
            case 0:
                var fire=b.Mesh("camp-hearth-stone","#79796D");
                for(int i=0;i<11;i++){double a=i*Math.PI*2/11;fire.Sphere(x+Math.Cos(a)*.058,y+.007,z+Math.Sin(a)*.058,.013,.010,.012,8,5);}
                var logs=b.Mesh("camp-charred-wood","#39352D");
                for(int i=0;i<3;i++){double a=i*Math.PI/3;logs.Tube(new(x+Math.Cos(a)*.045,y+.015,z+Math.Sin(a)*.045),new(x-Math.Cos(a)*.045,y+.015,z-Math.Sin(a)*.045),.009,8);}
                foreach(int side in new[]{-1,1})wood.Tube(new(x+side*.09,y,z-.15),new(x+side*.09,y+.15,z-.15),.005,8);
                wood.Tube(new(x-.10,y+.15,z-.15),new(x+.10,y+.15,z-.15),.004,8);break;
            case 1:
                House(b,x,y+.045,z,.3,.16,.5,"#B4A47F");
                for(int i=-1;i<=1;i++)wood.Box(x+i*.1,y,z,.025,.07,.43);
                b.Mesh("era-grain-thatch","#9B9569").Cylinder(x+.24,y,z,.085,.19,.065,12);break;
            case 2:
                for(int i=0;i<4;i++)stone.Box(x,y+i*.065,z,.54-i*.1,.065,.48-i*.085);
                stone.Box(x,y+.26,z,.17,.16,.17);wood.Box(x,y+.3,z+.09,.045,.1,.008);break;
            case 3:
                stone.Box(x,y,z,.32,.34,.32);stone.Box(x,y+.34,z,.37,.045,.37);
                for(int i=0;i<8;i++){double a=i*Math.PI/4;stone.Box(x+Math.Cos(a)*.16,y+.385,z+Math.Sin(a)*.16,.04,.05,.04);}break;
            case 4:
                stone.Box(x,y,z,.59,.04,.48);Columns(.30);b.Mesh("hall-tower","#C6BEA8").Box(x,y+.345,z,.13,.06,.14);
                b.Mesh("roofs","#A37459").Cylinder(x,y+.35,z,.30,.14,0,4);break;
            case 5:
                House(b,x,y,z,.30,.27,.45,"#BDB6A1");stone.Box(x+.17,y,z-.04,.14,.59,.15);
                b.Mesh("hall-tower","#BDB6A1").Box(x+.17,y+.4,z-.04,.145,.06,.155);
                b.Mesh("era-timber-spire","#756C57").Cylinder(x+.17,y+.59,z-.04,.105,.32,0,8);Windows(.18,2);break;
            case 6:
                stone.Box(x,y,z,.48,.24,.40);Columns(.26,5);stone.Cylinder(x,y+.31,z,.16,.10,sides:16);
                b.Mesh("roofs-dome","#9C8065").Sphere(x,y+.40,z,.17,.15,.17,16,8);Windows(.2,2);break;
            case 7:
                stone.Box(x,y,z,.5,.26,.4);Columns(.28);stone.Box(x,y+.325,z,.53,.04,.44);
                stone.Box(x,y+.365,z,.3,.08,.27);Windows(.21,2);break;
            case 8:
                brick.Box(x,y,z,.5,.23,.4);
                for(int i=-1;i<=1;i++){double xx=x+i*.16;b.Mesh("era-metal-roof","#677772").Box(xx,y+.23,z,.15,.035,.43);glass.Box(xx,y+.25,z-.11,.13,.04,.008);}
                brick.Cylinder(x+.23,y,z-.17,.036,.64,.025,10);Windows(.18,2);break;
            case 9:
                for(int i=0;i<4;i++)concrete.Box(x,y+i*.14,z,.38-i*.065,.14,.37-i*.05);
                metal.Box(x,y+.56,z,.026,.16,.026);Windows(.40,4);break;
            case 10:
                concrete.Box(x,y,z,.52,.18,.42);concrete.Box(x,y+.18,z,.38,.16,.32);glass.Box(x,y+.21,z+.165,.33,.08,.008);
                concrete.Cylinder(x,y+.34,z,.13,.09,sides:16);metal.Cylinder(x,y+.43,z,.04,.035,sides:12);Windows(.16,2);break;
            case 11:
                glass.Box(x,y,z,.28,.66,.28);metal.Box(x,y+.66,z,.30,.025,.30);
                for(int i=1;i<=7;i++)metal.Box(x,y+i*.08,z,.285,.008,.285);
                concrete.Box(x-.22,y,z,.12,.31,.24);break;
            case 12:
                for(int side=-1;side<=1;side+=2){concrete.Box(x+side*.16,y,z,.19,.30,.36);glass.Box(x+side*.16,y+.08,z+.183,.15,.16,.008);}
                glass.Box(x,y+.17,z,.18,.10,.16);metal.Cylinder(x,y+.29,z,.11,.035,sides:12);break;
            case 13:
                for(int i=0;i<4;i++){double xx=x+(i%2==0?-.03:.03);concrete.Box(xx,y+i*.13,z,.43-i*.075,.13,.39-i*.06);b.Mesh("era-garden-leaves","#5D7951").Box(xx,y+i*.13+.12,z,.45-i*.075,.018,.41-i*.06);}
                glass.Box(x,y+.52,z,.13,.12,.12);break;
            case 14:
                concrete.Cylinder(x,y,z,.20,.36,.14,16);glass.Cylinder(x,y+.08,z,.203,.20,.17,16);
                metal.Cylinder(x,y+.36,z,.025,.42,sides:8);Ring(b,x,y+.54,z,.24,"era-metal-ring");
                for(int side=-1;side<=1;side+=2)metal.Box(x+side*.25,y+.12,z,.15,.018,.28);break;
            case 15:
                concrete.Box(x,y,z,.5,.06,.44);glass.Sphere(x-.10,y+.15,z,.19,.17,.19,16,8);
                concrete.Cylinder(x+.18,y+.06,z,.10,.32,sides:12);Ring(b,x-.1,y+.12,z,.19,"era-metal-ring");break;
            case 16:
                concrete.Cylinder(x,y,z,.08,.67,sides:12);for(int i=0;i<3;i++){double yy=y+.18+i*.18;Ring(b,x,yy,z,.23-i*.025,"era-concrete-ring");glass.Cylinder(x,yy-.03,z,.17-i*.025,.10,sides:16);}
                b.Mesh("era-garden-leaves","#6F8963").Cylinder(x,y+.65,z,.15,.035,sides:16);break;
            default:
                for(int i=0;i<3;i++){double a=i*Math.PI*2/3;concrete.Cylinder(x+Math.Cos(a)*.11,y,z+Math.Sin(a)*.11,.065,.76+i*.09,.018,10);}
                Ring(b,x,y+.38,z,.24,"era-metal-ring");Ring(b,x,y+.60,z,.19,"era-metal-ring");glass.Cylinder(x,y+.25,z,.09,.45,.04,12);break;
        }
    }
    private static void Ring(SceneBatch b,double x,double y,double z,double radius,string key)
    {
        var mesh=b.Mesh(key,"#AAB8B0");for(int i=0;i<24;i++){double a=i*Math.PI/12,aa=(i+1)*Math.PI/12;mesh.Tube(new(x+Math.Cos(a)*radius,y,z+Math.Sin(a)*radius),new(x+Math.Cos(aa)*radius,y,z+Math.Sin(aa)*radius),.014,6);}
    }
    private static void EraFacade(SceneBatch b,double x,double y,double z,double w,double h,double d,int era)
    {
        // Bounded near-only facade geometry; the structural silhouette is retained at distance.
        var windows=b.Mesh("detail-era-glass-facade","#4D6971");var trim=b.Mesh("detail-era-stone-walls-trim","#D0C4A9");
        int floors=Math.Clamp((int)(h/.11),1,6);for(int f=0;f<floors;f++)for(int side=-1;side<=1;side+=2)
        {
            double yy=y+.04+f*h/floors;
            trim.Box(x,yy-.008,z+side*(d*.5+.007),w*.61,.009,.012);
        }
        if(era>=8&&era<=10)b.Mesh("detail-era-brick-chimney","#96735E").Box(x+w*.28,y+h,z,.023,.065,.028);
        if(era>=11)b.Mesh("detail-era-metal-solar","#425E69").Box(x,y+h+.025,z,w*.75,.012,d*.65);
    }
}
