using Crown.Core;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Crown.Desktop;

public sealed partial class MainWindow
{
    private enum OverlayScreen{None,General,Pause,Settings}
    private OverlayScreen overlayScreen;
    private GamePreferences preferences=new();
    private GamePreferences? settingsDraft;
    private GameAction? capturingBinding;
    private string settingsSection="Obraz";
    private string? preferencesWarning;
    private string preferencesPath="";
    private bool settingsFromPause;
    private bool diagnostic;
    private bool enableDiagnosticAutoSave;
    private InterfaceAudio audio=null!;
    private TextBlock? settingsStatus,pauseStatus;
    private ScrollViewer? settingsBody;
    private ScrollViewer? landingScroll;
    private readonly Dictionary<string,Button> settingsNavigation=[];
    private string? lastManualSave;
    internal Func<string?>? SavePathOverride;
    private bool InCampaign=>state!=null&&shell!=null&&root.Children.Contains(shell);
    private void InitializePreferences()
    {
        diagnostic=arguments.Any(a=>a.StartsWith("--ui-smoke")||a.StartsWith("--render"));
        preferencesPath=diagnostic?Path.Combine(arguments.Last(),"settings-test.json"):Path.Combine(AppContext.BaseDirectory,"settings.json");
        preferences=diagnostic?new():PreferencesStore.Read(preferencesPath,out preferencesWarning);
        audio=new InterfaceAudio(diagnostic?arguments.Last():Path.Combine(AppContext.BaseDirectory,"cache"),diagnostic);audio.Configure(preferences);
        AddHandler(Button.ClickEvent,new RoutedEventHandler((_,e)=>{if(e.OriginalSource is not Button{Content:"Sprawdź dźwięk"})audio.Play(preferences);}),true);
        PreviewKeyDown+=PreferencesKey;Closed+=(_,_)=>audio.Dispose();SizeChanged+=(_,_)=>{if(landingScroll!=null)landingScroll.MaxHeight=Math.Max(400,ActualHeight-70);};ApplyDisplayMode();
    }
    private void ApplyDisplayMode()
    {
        if(preferences.DisplayMode==1){WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;WindowState=WindowState.Maximized;}
        else if(WindowStyle==WindowStyle.None){WindowState=WindowState.Normal;WindowStyle=WindowStyle.SingleBorderWindow;ResizeMode=ResizeMode.CanResize;}
    }
    private bool SavePreferences(GamePreferences value,out string? error)
    {
        error=null;
        try{PreferencesStore.Write(preferencesPath,value);return true;}
        catch(Exception ex)when(ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException or NotSupportedException)
        {error="Nie udało się zapisać ustawień: "+ex.Message;return false;}
    }
    private void GraphicsPreferencesChanged(GamePreferences value)
    {
        preferences=value.Normalize();if(!SavePreferences(preferences,out var error))Toast(error!,false);
    }
    private void UpdateShortcutHints()
    {
        foreach(var button in Descendants<Button>(shell).Where(b=>b.Tag is GameAction))
        {
            var action=(GameAction)button.Tag;button.Content=(action==GameAction.Save?"Zapisz":action==GameAction.Load?"Wczytaj":"Następny oddział")+"  "+GamePreferences.KeyName(preferences.Bindings[action]);
        }
        if(status!=null&&status.Text.StartsWith("LPM:"))status.Text=$"LPM: wybór / przesunięcie · PPM: rozkaz / obrót · {GamePreferences.KeyName(preferences.Bindings[GameAction.RotateLeft])}/{GamePreferences.KeyName(preferences.Bindings[GameAction.RotateRight])}: obrót · Kółko: zoom";
    }
    private void ShowPause()
    {
        if(!InCampaign||busy)return;
        var p=Ui.Stack(Ui.Eyebrow("KORONY I POGRANICZA"),Ui.Label("Menu gry",34,Ui.Text,true),Ui.Label($"Tura {state.Turn} · {state.Season}\nRozgrywka czeka na Twój powrót.",13,Ui.Muted));Ui.Gap(p,18);
        p.Children.Add(Ui.Button("Powrót do rozgrywki",CloseModal,true));p.Children.Add(Ui.Button("Zapisz grę",Save));p.Children.Add(Ui.Button("Ustawienia",()=>ShowSettings(true)));
        p.Children.Add(Ui.Button("Wyjście do menu głównego",()=>{if(!TryAutoSave(true)){if(pauseStatus!=null){pauseStatus.Text="Nie udało się wykonać autozapisu. Zapisz grę ręcznie przed wyjściem.";pauseStatus.Foreground=Ui.Red;}return;}Landing();}));
        pauseStatus=Ui.Label(preferences.AutoSave?"Autozapis włączony. Esc wraca do rozgrywki.":"Autozapis wyłączony. Pamiętaj o ręcznym zapisie. Esc wraca do rozgrywki.",12,Ui.Muted);p.Children.Add(pauseStatus);
        Modal(p,540);overlayScreen=OverlayScreen.Pause;
    }
    private void ShowSettings(bool fromPause=false)
    {
        CloseModal();settingsFromPause=fromPause;settingsDraft=preferences.Copy();settingsSection="Obraz";
        var layout=new Grid{Height=Math.Clamp(ActualHeight-190,460,650)};layout.RowDefinitions.Add(new(){Height=GridLength.Auto});layout.RowDefinitions.Add(new());layout.RowDefinitions.Add(new(){Height=GridLength.Auto});
        var title=Ui.Stack(Ui.Eyebrow("PREFERENCJE GRACZA"),Ui.Label("Ustawienia",34,Ui.Text,true));layout.Children.Add(title);
        var middle=new Grid{Margin=new(0,16,0,12)};middle.ColumnDefinitions.Add(new(){Width=new GridLength(165)});middle.ColumnDefinitions.Add(new());Grid.SetRow(middle,1);layout.Children.Add(middle);
        var navigation=new StackPanel{Margin=new(0,0,22,0)};middle.Children.Add(navigation);
        settingsNavigation.Clear();foreach(string section in new[]{"Obraz","Dźwięk","Sterowanie","Interfejs","Rozgrywka"}){var button=Ui.Button(section,()=>{settingsSection=section;capturingBinding=null;RenderSettingsSection();});settingsNavigation[section]=button;navigation.Children.Add(button);}
        settingsBody=new ScrollViewer{VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,Padding=new(0,0,12,0)};Grid.SetColumn(settingsBody,1);middle.Children.Add(settingsBody);
        settingsStatus=Ui.Label(preferencesWarning??"Zmiany zaczną działać po kliknięciu Zastosuj. Esc wraca bez stosowania nowych zmian.",12,Ui.Muted);
        var footer=Ui.Stack(settingsStatus);var buttons=new WrapPanel{Margin=new(0,10,0,0)};footer.Children.Add(buttons);
        foreach(var pair in new (string,Action,bool)[]{("Zastosuj",ApplySettings,true),("Przywróć domyślne",()=>{settingsDraft=new();capturingBinding=null;RenderSettingsSection();settingsStatus.Text="Domyślne ustawienia są przygotowane. Kliknij Zastosuj, aby je zapisać.";},false),("Wróć",LeaveSettings,false)})
        {var button=Ui.Button(pair.Item1,pair.Item2,pair.Item3);button.Margin=new(0,0,10,0);buttons.Children.Add(button);}
        Grid.SetRow(footer,2);layout.Children.Add(footer);
        // Modal replaces the previous screen; keep the draft across its cleanup.
        var draft=settingsDraft;Modal(layout,1020);settingsDraft=draft;settingsFromPause=fromPause;overlayScreen=OverlayScreen.Settings;RenderSettingsSection();
    }
    private void RenderSettingsSection()
    {
        if(settingsDraft==null||settingsBody==null)return;foreach(var pair in settingsNavigation){pair.Value.BorderBrush=pair.Key==settingsSection?Ui.Gold:Ui.Line;pair.Value.Foreground=pair.Key==settingsSection?Ui.Gold:Ui.Text;}var p=settingsDraft;var panel=Ui.Stack(Ui.Label(settingsSection,25,Ui.Text,true));settingsBody.Content=panel;settingsBody.ScrollToTop();
        void Toggle(string title,bool value,Action<bool> change)
        {
            var box=new CheckBox{Content=title,IsChecked=value,Foreground=Ui.Text,FontSize=13,Margin=new(0,9,0,9),Name=""};box.Checked+=(_,_)=>change(true);box.Unchecked+=(_,_)=>change(false);panel.Children.Add(box);
        }
        void Choose(string title,string[] values,int selected,Action<int> change)
        {panel.Children.Add(Ui.Label(title,12,Ui.Muted));var combo=Ui.Combo(values,selected);combo.Tag=title;combo.SelectionChanged+=(_,_)=>{if(combo.SelectedIndex>=0)change(combo.SelectedIndex);};panel.Children.Add(combo);}
        void Range(string title,double value,double min,double max,Action<double> change,string suffix="%")
        {
            var label=Ui.Label(title+" · "+(value*100).ToString("0")+suffix,12,Ui.Muted);panel.Children.Add(label);
            var slider=new Slider{Minimum=min,Maximum=max,Value=value,TickFrequency=.05,SmallChange=.05,LargeChange=.1,Margin=new(0,8,0,18),Tag=title};slider.ValueChanged+=(_,_)=>{change(slider.Value);label.Text=title+" · "+(slider.Value*100).ToString("0")+suffix;};panel.Children.Add(slider);
        }
        switch(settingsSection)
        {
            case "Obraz":
                Choose("Tryb wyświetlania",["Okno","Pełny ekran bez ramek"],p.DisplayMode,v=>p.DisplayMode=v);
                Choose("Jakość grafiki",["Wysoka","Zrównoważona","Niska"],p.Quality,v=>{p.Preset(v);RenderSettingsSection();});
                panel.Children.Add(Ui.Label("Jakość ustala rozdzielczość renderowania oraz początkowe ustawienia cieni i odbić. Poszczególne efekty możesz zmienić poniżej.",12,Ui.Muted));
                Range("Jasność",p.Brightness,.6,1.4,v=>p.Brightness=v);Toggle("Cienie",p.Shadows,v=>p.Shadows=v);Toggle("Odbicia wody",p.Reflections,v=>p.Reflections=v);Toggle("Wygładzanie krawędzi",p.EdgeSmoothing,v=>p.EdgeSmoothing=v);
                Toggle("Brzegi i głębokość wody",p.ShoreEffects,v=>p.ShoreEffects=v);Toggle("Barwy pór roku",p.SeasonalColors,v=>p.SeasonalColors=v);Toggle("Automatyczne szczegóły otoczenia",p.AutomaticDetail,v=>p.AutomaticDetail=v);Toggle("Pomijanie geometrii poza kadrem",p.VisibilityCulling,v=>p.VisibilityCulling=v);Toggle("Animowane otoczenie",p.AnimateEnvironment,v=>p.AnimateEnvironment=v);
                Choose("Pogoda",["Zgodna z porą roku","Bez opadów","Deszcz","Śnieg"],(int)p.Weather,v=>p.Weather=(VisualWeather)v);Choose("Światło",["Poranek","Południe"],p.NoonLight?1:0,v=>p.NoonLight=v==1);
                panel.Children.Add(Ui.Label("Efekty wymagają Direct3D 11. Tryb zgodności WPF zachowuje prostsze materiały.",12,Ui.Muted));break;
            case "Dźwięk":
                Toggle("Wycisz wszystkie dźwięki",p.Muted,v=>p.Muted=v);Range("Głośność główna",p.MasterVolume,0,1,v=>p.MasterVolume=v);Range("Dźwięki interfejsu",p.InterfaceVolume,0,1,v=>p.InterfaceVolume=v);
                panel.Children.Add(Ui.Button("Sprawdź dźwięk",()=>{audio.Play(p,true);settingsStatus!.Text=audio.Failure==null?"Odsłuch używa głośności ustawionej w tym panelu.":"Dźwięk jest niedostępny: "+audio.Failure; }));
                panel.Children.Add(Ui.Label("Krótkie sygnały towarzyszą przyciskom. Muzyka i odgłosy świata są jeszcze w produkcji. Odsłuch nie zapisuje zmian.",13,Ui.Muted));break;
            case "Sterowanie":
                Range("Szybkość przesuwania mapy",p.PanSpeed,.3,2.5,v=>p.PanSpeed=v);Range("Czułość obrotu myszy",p.OrbitSensitivity,.3,2.5,v=>p.OrbitSensitivity=v);Range("Czułość kółka myszy",p.ZoomSensitivity,.3,2.5,v=>p.ZoomSensitivity=v);Toggle("Odwróć pionowy obrót myszy",p.InvertOrbitY,v=>p.InvertOrbitY=v);
                panel.Children.Add(Ui.Label("Kliknij skrót i naciśnij nowy klawisz. Esc anuluje przypisanie. Każda akcja ma osobny klawisz; Esc pozostaje klawiszem menu.",12,Ui.Muted));
                foreach(var action in Enum.GetValues<GameAction>())
                {
                    var row=new Grid{Margin=new(0,5,0,0)};row.ColumnDefinitions.Add(new());row.ColumnDefinitions.Add(new(){Width=new GridLength(140)});var label=Ui.Label(GamePreferences.ActionName(action),13);label.VerticalAlignment=VerticalAlignment.Center;row.Children.Add(label);
                    var button=Ui.Button(GamePreferences.KeyName(p.Bindings[action]),()=>{capturingBinding=action;settingsStatus!.Text="Naciśnij klawisz: "+GamePreferences.ActionName(action)+". Esc anuluje.";});button.Tag=action;Grid.SetColumn(button,1);row.Children.Add(button);panel.Children.Add(row);
                }
                panel.Children.Add(Ui.Label("LPM wybiera lub przesuwa mapę. Krótki PPM wydaje rozkaz, przeciągany obraca. Środkowy przycisk również obraca. Skróty kamery działają, gdy mapa ma fokus.",12,Ui.Muted));break;
            case "Interfejs":
                Toggle("Pokaż nazwy i ikony zasobów",p.ShowResources,v=>p.ShowResources=v);Toggle("Pokaż siatkę pól",p.ShowGrid,v=>p.ShowGrid=v);Toggle("Pokaż statystyki renderowania",p.ShowStatistics,v=>p.ShowStatistics=v);
                panel.Children.Add(Ui.Label("Ustawienia są stosowane także po wczytaniu kampanii. Przełączniki na mapie pozwalają tymczasowo zmienić jej widok.",13,Ui.Muted));break;
            case "Rozgrywka":
                Toggle("Automatyczny zapis kampanii",p.AutoSave,v=>p.AutoSave=v);Toggle("Potwierdzaj zakończenie tury",p.ConfirmEndTurn,v=>p.ConfirmEndTurn=v);
                panel.Children.Add(Ui.Label("Autozapis zachowuje kampanię po turze, decyzji rady i przy opuszczaniu gry. Ręczny zapis pozostaje dostępny również po wyłączeniu autozapisu. Zmiana tych opcji nie przesuwa tury.",13,Ui.Muted));
                panel.Children.Add(Ui.Label("Poziom trudności i ziarno świata wybierasz przed rozpoczęciem nowej kampanii.",12,Ui.Muted));break;
        }
    }
    private void ApplySettings()
    {
        if(settingsDraft==null)return;var value=settingsDraft.Normalize();
        if(!SavePreferences(value,out var error)){settingsStatus!.Text=error;settingsStatus.Foreground=Ui.Red;return;}
        preferences=value;settingsDraft=value.Copy();audio.Configure(value);if(InCampaign){map.Configure(value);UpdateShortcutHints();}ApplyDisplayMode();capturingBinding=null;RenderSettingsSection();
        settingsStatus!.Text="Ustawienia zastosowane i zapisane.";settingsStatus.Foreground=Ui.Green;preferencesWarning=null;
    }
    private void LeaveSettings(){bool pause=settingsFromPause;CloseModal();if(pause&&InCampaign)ShowPause();}
    private void PreferencesKey(object sender,KeyEventArgs e)
    {
        var modifiers=injectingSettingsKey?ModifierKeys.None:Keyboard.Modifiers;
        if(capturingBinding is{} action&&settingsDraft!=null)
        {
            e.Handled=true;if(e.Key==Key.Escape){capturingBinding=null;settingsStatus!.Text="Przypisanie anulowane.";return;}
            string? error=modifiers==ModifierKeys.None?settingsDraft.Rebind(action,e.Key):"Użyj pojedynczego klawisza bez Ctrl, Alt lub Shift.";
            settingsStatus!.Text=error??"Nowy skrót przygotowany. Kliknij Zastosuj.";if(error==null){capturingBinding=null;RenderSettingsSection();}return;
        }
        if(e.Key==Key.Escape)
        {
            e.Handled=true;if(e.IsRepeat)return;if(overlayScreen==OverlayScreen.Settings)LeaveSettings();else if(overlay!=null)CloseModal();else if(InCampaign)ShowPause();return;
        }
        if(!InCampaign||overlay!=null||busy||e.OriginalSource is TextBox or ComboBox or Slider||modifiers!=ModifierKeys.None)return;
        var pair=preferences.Bindings.FirstOrDefault(p=>p.Value==e.Key);if(!preferences.Bindings.ContainsValue(e.Key))return;
        if(e.IsRepeat&&pair.Key is GameAction.Save or GameAction.Load or GameAction.EndTurn or GameAction.NextUnit or GameAction.Center){e.Handled=true;return;}
        switch(pair.Key)
        {
            case GameAction.Save:Save();break;case GameAction.Load:Load();break;case GameAction.EndTurn:Advance();break;case GameAction.NextUnit:NextUnit();break;
            case GameAction.Center:map.Center(state.Cities.FirstOrDefault(c=>c.Owner==0)?.Tile??selectedTile);break;default:return;
        }
        e.Handled=true;
    }
}
