using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Input;

namespace Crown.Desktop;

internal enum GameAction { Save,Load,EndTurn,NextUnit,Center,North,South,West,East,RotateLeft,RotateRight,TiltUp,TiltDown,ZoomIn,ZoomOut }
internal sealed record GamePreferences
{
    public int Version {get;set;}=1;
    public int DisplayMode {get;set;}
    public int Quality {get;set;}
    public bool Shadows {get;set;}=true;
    public bool Reflections {get;set;}=true;
    public bool EdgeSmoothing {get;set;}=true;
    public bool ShoreEffects {get;set;}=true;
    public bool SeasonalColors {get;set;}=true;
    public bool AutomaticDetail {get;set;}=true;
    public bool VisibilityCulling {get;set;}=true;
    public bool AnimateEnvironment {get;set;}=true;
    public VisualWeather Weather {get;set;}=VisualWeather.Seasonal;
    public bool NoonLight {get;set;}
    public double Brightness {get;set;}=1;
    public double MasterVolume {get;set;}=.7;
    public double InterfaceVolume {get;set;}=.5;
    public bool Muted {get;set;}
    public double PanSpeed {get;set;}=1;
    public double OrbitSensitivity {get;set;}=1;
    public double ZoomSensitivity {get;set;}=1;
    public bool InvertOrbitY {get;set;}
    public bool ShowResources {get;set;}=true;
    public bool ShowGrid {get;set;}
    public bool ShowStatistics {get;set;}
    public bool AutoSave {get;set;}=true;
    public bool ConfirmEndTurn {get;set;}
    public Dictionary<GameAction,Key> Bindings {get;set;}=DefaultBindings();
    internal GamePreferences Copy()=>this with{Bindings=new(Bindings)};
    internal static Dictionary<GameAction,Key> DefaultBindings()=>new()
    {
        [GameAction.Save]=Key.F5,[GameAction.Load]=Key.F9,[GameAction.EndTurn]=Key.Space,[GameAction.NextUnit]=Key.Tab,[GameAction.Center]=Key.Home,
        [GameAction.North]=Key.W,[GameAction.South]=Key.S,[GameAction.West]=Key.A,[GameAction.East]=Key.D,[GameAction.RotateLeft]=Key.Q,[GameAction.RotateRight]=Key.E,
        [GameAction.TiltUp]=Key.PageUp,[GameAction.TiltDown]=Key.PageDown,[GameAction.ZoomIn]=Key.OemPlus,[GameAction.ZoomOut]=Key.OemMinus
    };
    internal static string ActionName(GameAction action)=>action switch
    {
        GameAction.Save=>"Zapis gry",GameAction.Load=>"Wczytanie gry",GameAction.EndTurn=>"Koniec tury",GameAction.NextUnit=>"Następny oddział",GameAction.Center=>"Centruj stolicę",
        GameAction.North=>"Przesuń w górę",GameAction.South=>"Przesuń w dół",GameAction.West=>"Przesuń w lewo",GameAction.East=>"Przesuń w prawo",
        GameAction.RotateLeft=>"Obrót w lewo",GameAction.RotateRight=>"Obrót w prawo",GameAction.TiltUp=>"Wyższy kąt",GameAction.TiltDown=>"Niższy kąt",GameAction.ZoomIn=>"Przybliż",_=>"Oddal"
    };
    internal static string KeyName(Key key)=>key switch{Key.Space=>"Spacja",Key.OemPlus=>"+",Key.OemMinus=>"−",Key.PageUp=>"Page Up",Key.PageDown=>"Page Down",_=>key.ToString()};
    internal static bool AllowedKey(Key key)=>Enum.IsDefined(key)&&key is not (Key.None or Key.Escape or Key.System or Key.DeadCharProcessed or Key.ImeProcessed or Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin);
    internal string? Rebind(GameAction action,Key key)
    {
        if(!AllowedKey(key))return "Ten klawisz jest zarezerwowany. Esc zawsze otwiera menu lub wraca.";
        var conflict=Bindings.FirstOrDefault(pair=>pair.Value==key&&pair.Key!=action);
        if(Bindings.Any(pair=>pair.Value==key&&pair.Key!=action))return "Klawisz jest już przypisany: "+ActionName(conflict.Key)+". Wybierz inny.";
        Bindings[action]=key;return null;
    }
    internal void Preset(int quality){Quality=Math.Clamp(quality,0,2);Shadows=Quality<2;Reflections=Quality==0;EdgeSmoothing=true;AutomaticDetail=true;VisibilityCulling=true;}
    internal GamePreferences Normalize()
    {
        var p=Copy();p.Version=1;p.DisplayMode=Math.Clamp(p.DisplayMode,0,1);p.Quality=Math.Clamp(p.Quality,0,2);
        double Clamp(double value,double low,double high,double fallback)=>double.IsFinite(value)?Math.Clamp(value,low,high):fallback;
        p.Brightness=Clamp(p.Brightness,.6,1.4,1);p.MasterVolume=Clamp(p.MasterVolume,0,1,.7);p.InterfaceVolume=Clamp(p.InterfaceVolume,0,1,.5);
        p.PanSpeed=Clamp(p.PanSpeed,.3,2.5,1);p.OrbitSensitivity=Clamp(p.OrbitSensitivity,.3,2.5,1);p.ZoomSensitivity=Clamp(p.ZoomSensitivity,.3,2.5,1);
        if(!Enum.IsDefined(p.Weather))p.Weather=VisualWeather.Seasonal;
        if(p.Bindings.Count!=DefaultBindings().Count||DefaultBindings().Keys.Any(k=>!p.Bindings.ContainsKey(k))||p.Bindings.Values.Any(k=>!AllowedKey(k))||p.Bindings.Values.Distinct().Count()!=p.Bindings.Count)p.Bindings=DefaultBindings();
        return p;
    }
}

internal static class PreferencesStore
{
    private static readonly JsonSerializerOptions Options=new(){WriteIndented=true,Converters={new JsonStringEnumConverter()}};
    internal static GamePreferences Read(string path,out string? warning)
    {
        warning=null;if(!File.Exists(path))return new();
        try
        {
            var value=JsonSerializer.Deserialize<GamePreferences>(File.ReadAllText(path),Options);
            if(value==null||value.Version!=1||value.Bindings==null)throw new InvalidDataException("Nieprawidłowy format ustawień.");
            return value.Normalize();
        }
        catch(Exception ex)when(ex is IOException or UnauthorizedAccessException or JsonException or InvalidDataException or NotSupportedException)
        {warning="Nie udało się odczytać ustawień. Przywrócono domyślne; plik pozostał zachowany. "+ex.Message;return new();}
    }
    internal static void Write(string path,GamePreferences value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);string temporary=path+".tmp";
        try{File.WriteAllText(temporary,JsonSerializer.Serialize(value.Normalize(),Options));File.Move(temporary,path,true);}
        finally{if(File.Exists(temporary))File.Delete(temporary);}
    }
}
