using Crown.Core;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Crown.Desktop;

/// <summary>Deterministic terrain material with fractal relief and directional illumination.</summary>
internal static class TerrainTexture
{
    private static double Noise(double x,double y,int seed)
    {
        int ix=(int)Math.Floor(x),iy=(int)Math.Floor(y);double fx=x-ix,fy=y-iy;fx=fx*fx*(3-2*fx);fy=fy*fy*(3-2*fy);
        double a=Hash(ix,iy,seed)*(1-fx)+Hash(ix+1,iy,seed)*fx,b=Hash(ix,iy+1,seed)*(1-fx)+Hash(ix+1,iy+1,seed)*fx;return a*(1-fy)+b*fy;
    }
    private static double Hash(int x,int y,int seed)
    {
        uint n=unchecked((uint)(x*374761393+y*668265263+seed*1447));n=(n^(n>>13))*1274126177;return (n^(n>>16))/(double)uint.MaxValue;
    }
    private static double Height(double x,double y,int seed,bool mountain)
    {
        double a=Noise(x*.12,y*.12,seed),b=Noise(x*.29,y*.29,seed+7),c=Noise(x*.65,y*.65,seed+29);
        return mountain?Math.Pow(1-Math.Abs(a*2-1),2)*.6+b*.28+c*.12:a*.65+b*.25+c*.1;
    }
    public static BitmapSource Create(Tile tile,int seed)
    {
        const int w=100,h=116;byte[] pixels=new byte[w*h*4];var center=MapView.CenterOf(tile);
        bool mountain=tile.Terrain==Terrain.Mountain,hills=tile.Terrain==Terrain.Hills,water=tile.Terrain is Terrain.Ocean or Terrain.Coast;
        (double r,double g,double b)=tile.Terrain switch
        {
            Terrain.Grassland=>(123,132,81),Terrain.Forest=>(68,89,62),Terrain.Hills=>(125,130,93),Terrain.Mountain=>(130,137,127),Terrain.Desert=>(184,163,114),Terrain.Tundra=>(153,166,145),Terrain.Marsh=>(94,120,88),Terrain.Coast=>(55,108,119),_=>(29,69,86)
        };
        for(int y=0;y<h;y++)for(int x=0;x<w;x++)
        {
            double wx=(center.X-34+x*68.0/w)/7,wy=(center.Y-34+y*68.0/h)/7;
            double low=Noise(wx*.22,wy*.22,seed),grain=Hash((int)(wx*29),(int)(wy*29),seed),detail=Noise(wx*2.4,wy*2.4,seed+80);
            double bright=.84+low*.24+(detail-.5)*.16+(grain-.5)*.12;
            double rr=r,gg=g,bb=b;
            if(hills||mountain)
            {
                double elevation=Height(wx,wy,seed,mountain),dx=Height(wx+.18,wy,seed,mountain)-elevation,dy=Height(wx,wy+.18,seed,mountain)-elevation;
                bright*=Math.Clamp(.85-dx*9-dy*7,.4,1.55);
                if(mountain)
                {
                    double stone=Math.Clamp((elevation-.3)*2.7,0,1);rr=93*(1-stone)+151*stone;gg=109*(1-stone)+151*stone;bb=85*(1-stone)+137*stone;
                    double snow=Math.Clamp((elevation-.68+detail*.06)*11,0,1);rr=rr*(1-snow)+222*snow;gg=gg*(1-snow)+228*snow;bb=bb*(1-snow)+218*snow;
                }
                else {rr+=(elevation-.5)*22;gg+=(elevation-.5)*12;}
            }
            else if(water){bright=.85+low*.2+detail*.06;double ripple=Math.Sin(wy*22+wx*3+detail*4);bright+=ripple>.93?.06:0;}
            else if(tile.Terrain==Terrain.Grassland){rr+=(low-.5)*22;gg+=(detail-.5)*15;}
            int i=(y*w+x)*4;pixels[i]=(byte)Math.Clamp(bb*bright,0,255);pixels[i+1]=(byte)Math.Clamp(gg*bright,0,255);pixels[i+2]=(byte)Math.Clamp(rr*bright,0,255);pixels[i+3]=255;
        }
        var image=BitmapSource.Create(w,h,96,96,PixelFormats.Bgra32,null,pixels,w*4);image.Freeze();return image;
    }
}
