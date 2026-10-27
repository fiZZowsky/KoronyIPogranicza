using System.Numerics;
using System.Windows.Input;

namespace Crown.Desktop;

public sealed partial class MapView
{
    private GamePreferences preferences=new();
    private bool suspended;
    internal bool InteractionSuspended=>suspended;
    internal Action<GamePreferences>? PreferencesChanged;
    internal void SuspendInteraction(bool value)
    {
        suspended=value;if(value){visualClock.Stop();dragging=false;ReleaseMouseCapture();}else visualClock.Start();
    }
    internal void Configure(GamePreferences value,bool interfaceDefaults=true)
    {
        preferences=value.Copy();animateEnvironment=value.AnimateEnvironment;ShowRenderStatistics=value.ShowStatistics;
        if(interfaceDefaults){ShowResources=value.ShowResources;ShowGrid=value.ShowGrid;}
        ApplyRendererPreferences();InvalidateVisual();
    }
    private void ApplyRendererPreferences()
    {
        if(photographic is not{} r)return;
        r.MaximumWidth=preferences.Quality switch{1=>1100,2=>900,_=>1600};r.Shadows=preferences.Shadows;r.Reflections=preferences.Reflections;
        r.ShoreEffects=preferences.ShoreEffects;r.EdgeSmoothing=preferences.EdgeSmoothing;r.SeasonalColors=preferences.SeasonalColors;
        r.AutomaticDetail=preferences.AutomaticDetail;r.VisibilityCulling=preferences.VisibilityCulling;r.Weather=preferences.Weather;r.Exposure=(float)(.87*preferences.Brightness);
        r.Sun=preferences.NoonLight?Vector3.Normalize(new(-.3f,1.5f,-.2f)):Vector3.Normalize(new(-.65f,.82f,-.35f));
    }
    private void PersistGraphicsMenu()
    {
        var p=preferences.Copy();p.AnimateEnvironment=animateEnvironment;p.ShowStatistics=ShowRenderStatistics;
        if(photographic is{} r)
        {
            p.Quality=r.MaximumWidth<=900?2:r.MaximumWidth<=1100?1:0;p.Shadows=r.Shadows;p.Reflections=r.Reflections;p.ShoreEffects=r.ShoreEffects;p.EdgeSmoothing=r.EdgeSmoothing;
            p.SeasonalColors=r.SeasonalColors;p.AutomaticDetail=r.AutomaticDetail;p.VisibilityCulling=r.VisibilityCulling;p.Weather=r.Weather;p.Brightness=r.Exposure/.87;p.NoonLight=r.Sun.Y>.7;
        }
        preferences=p;PreferencesChanged?.Invoke(p.Copy());
    }
    private bool CameraKey(Key key)
    {
        if(suspended)return false;
        var binding=preferences.Bindings.FirstOrDefault(pair=>pair.Value==key);
        if(!preferences.Bindings.ContainsValue(key))return false;
        switch(binding.Key)
        {
            case GameAction.North:PanPixels(0,55);break;case GameAction.South:PanPixels(0,-55);break;case GameAction.West:PanPixels(55,0);break;case GameAction.East:PanPixels(-55,0);break;
            case GameAction.RotateLeft:Orbit(-12,0);break;case GameAction.RotateRight:Orbit(12,0);break;case GameAction.TiltUp:Orbit(0,7);break;case GameAction.TiltDown:Orbit(0,-7);break;
            case GameAction.ZoomIn:Zoom(.8);break;case GameAction.ZoomOut:Zoom(1.25);break;default:return false;
        }
        return true;
    }
}
