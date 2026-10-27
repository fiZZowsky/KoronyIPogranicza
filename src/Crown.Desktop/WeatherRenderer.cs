using System.Numerics;

namespace Crown.Desktop;

internal sealed partial class PhotographicRenderer
{
    private nint weatherBuffer;
    internal int WeatherVertices { get; private set; }
    internal int LiveResources=>gpu.LiveResources;
    internal int SeasonIndex { get; private set; }
    private int visualSeed;
    internal bool SeasonalColors { get; set; }=true;
    internal VisualWeather Weather { get; set; }=VisualWeather.Seasonal;
    internal EnvironmentProfile Environment=>EnvironmentProfile.Resolve(SeasonIndex,SeasonalColors,Weather);
    private void RenderWeather(Vector3 eye,Vector3 target,double time,EnvironmentProfile profile)
    {
        gpu.Free(weatherBuffer);weatherBuffer=0;WeatherVertices=0;
        float intensity=Math.Max(profile.Rain,profile.Snowfall);if(intensity<=0)return;
        bool snow=profile.Snowfall>0;var direction=Vector3.Normalize(target-eye);var right=Vector3.Normalize(Vector3.Cross(direction,Vector3.UnitY));var up=Vector3.Normalize(Vector3.Cross(right,direction));
        var vertices=new List<GpuVertex>();int cx=(int)Math.Floor(target.X/1.5),cz=(int)Math.Floor(target.Z/1.5);
        float floor=(float)Math.Max(Terrain3D.SeaLevel,field!.Sample(target.X,target.Z))+.08f;
        float Hash(int x,int z,int layer,int salt)
        {
            uint n=unchecked((uint)(x*374761393+z*668265263+layer*144269+visualSeed+salt));n=(n^(n>>13))*1274126177;return (n^(n>>16))/(float)uint.MaxValue;
        }
        for(int z=cz-5;z<=cz+5;z++)for(int x=cx-5;x<=cx+5;x++)for(int layer=0;layer<4;layer++)
        {
            if(Hash(x,z,layer,29)>intensity)continue;
            double phase=Hash(x,z,layer,71)+time*(snow?.13:.65);phase-=Math.Floor(phase);
            float drift=(float)Math.Sin(time*.4+Hash(x,z,layer,109)*6.28)*(snow?.16f:.04f);
            var center=new Vector3((x+Hash(x,z,layer,3))*1.5f+drift,floor+8*(1-(float)phase),(z+Hash(x,z,layer,17))*1.5f);
            float flake=.008f+Hash(x,z,layer,193)*.007f;
            var horizontal=right*(snow?flake:.006f);var vertical=snow?up*flake:new Vector3(-.024f,.19f,.009f);
            var color=snow?new Vector4(.86f,.91f,.94f,.80f):new Vector4(.61f,.72f,.76f,.24f);
            var a=center-horizontal-vertical;var b=center+horizontal-vertical;var c=center+horizontal+vertical;var d=center-horizontal+vertical;
            void Add(Vector3 position,Vector2 uv)=>vertices.Add(new GpuVertex{Position=position,Normal=-direction,Uv=uv,Color=color,Surface=new(snow?13:12,1,0,0)});
            Add(a,new(0,1));Add(b,new(1,1));Add(c,new(1,0));Add(a,new(0,1));Add(c,new(1,0));Add(d,new(0,0));
        }
        WeatherVertices=vertices.Count;if(WeatherVertices==0)return;
        weatherBuffer=gpu.Buffer<GpuVertex>(vertices.ToArray(),1);gpu.Draw(weatherBuffer,WeatherVertices);
    }
}
