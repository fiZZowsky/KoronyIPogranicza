namespace Crown.Desktop;

internal enum VisualWeather { Seasonal, Clear, Rain, Snow }
internal readonly record struct EnvironmentProfile(float SnowCover,float Autumn,float Wetness,float Spring,float Rain,float Snowfall,float Fog,float Light)
{
    internal static EnvironmentProfile Resolve(int season,bool seasonalColors,VisualWeather selected)
    {
        bool automatic=selected==VisualWeather.Seasonal;
        var weather=automatic?season switch{2=>VisualWeather.Rain,3=>VisualWeather.Snow,_=>VisualWeather.Clear}:selected;
        float rain=weather==VisualWeather.Rain?(automatic?.35f:.85f):0,snow=weather==VisualWeather.Snow?(automatic?.45f:.85f):0;
        return new(seasonalColors&&season==3?.82f:0,seasonalColors&&season==2?.92f:0,rain>0?.72f:seasonalColors&&season==0?.12f:0,seasonalColors&&season==0?1:0,rain,snow,1+rain*1.4f+snow*.9f,1-rain*.22f-snow*.10f);
    }
}
