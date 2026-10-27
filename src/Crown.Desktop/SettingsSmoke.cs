using Crown.Core;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Crown.Desktop;

public sealed partial class MainWindow
{
    private bool injectingSettingsKey;
    private async Task SettingsSmoke(string folder)
    {
        Directory.CreateDirectory(folder);var checks=new List<string>();
        void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);checks.Add("PASS "+message);}
        Button Button(string title)=>Descendants<Button>(overlay??root).First(b=>b.Content?.ToString()==title);
        void Click(string title){Button(title).RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));root.UpdateLayout();}
        void KeyPress(Key key){injectingSettingsKey=true;try{RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice,PresentationSource.FromVisual(this)!,0,key){RoutedEvent=Keyboard.PreviewKeyDownEvent});}finally{injectingSettingsKey=false;}root.UpdateLayout();}
        void CameraKeyPress(Key key)=>map.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice,PresentationSource.FromVisual(this)!,0,key){RoutedEvent=Keyboard.KeyDownEvent});
        void SelectSetting(string title,int index)=>Descendants<ComboBox>(overlay!).Single(c=>c.Tag?.ToString()==title).SelectedIndex=index;
        void SliderSetting(string title,double value)=>Descendants<Slider>(overlay!).Single(c=>c.Tag?.ToString()==title).Value=value;
        void Toggle(string title,bool value)=>Descendants<CheckBox>(overlay!).Single(c=>c.Content?.ToString()==title).IsChecked=value;
        void Bind(GameAction action,Key key){Descendants<Button>(overlay!).Single(b=>b.Tag is GameAction a&&a==action).RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));KeyPress(key);}
        async Task Capture(string name)
        {
            root.UpdateLayout();await Task.Delay(120);map?.RenderGpuFrame();var image=new RenderTargetBitmap((int)root.ActualWidth,(int)root.ActualHeight,96,96,PixelFormats.Pbgra32);image.Render(root);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(image));using var output=File.Create(Path.Combine(folder,name+".png"));encoder.Save(output);
        }
        try
        {
            if(arguments.Contains("--smoke-force-failure"))throw new InvalidOperationException("Intentional settings diagnostic failure.");
            if(File.Exists(preferencesPath))File.Delete(preferencesPath);Landing();root.UpdateLayout();
            Check(Descendants<Button>(root).Any(b=>b.Content?.ToString()=="Ustawienia"),"The main menu has an actual settings entry");await Capture("settings-main-menu");Click("Ustawienia");
            Check(overlayScreen==OverlayScreen.Settings&&new[]{"Obraz","Dźwięk","Sterowanie","Interfejs","Rozgrywka"}.All(title=>Descendants<Button>(overlay!).Any(b=>b.Content?.ToString()==title)),"Settings exposes five separate sections");
            SelectSetting("Jakość grafiki",1);KeyPress(Key.Escape);Check(preferences.Quality==0&&!File.Exists(preferencesPath)&&overlay==null,"Esc from main-menu settings discards an unapplied draft");
            Click("Ustawienia");SelectSetting("Jakość grafiki",1);Click("Zastosuj");var loaded=PreferencesStore.Read(preferencesPath,out var warning);
            Check(loaded.Quality==1&&preferences.Quality==1&&warning==null,"Apply writes a real preferences file that reloads correctly");
            SelectSetting("Tryb wyświetlania",1);Click("Zastosuj");Check(WindowStyle==WindowStyle.None&&WindowState==WindowState.Maximized,"Borderless display mode changes the actual game window");
            SelectSetting("Tryb wyświetlania",0);Click("Zastosuj");Width=1600;Height=980;Check(WindowStyle==WindowStyle.SingleBorderWindow&&WindowState==WindowState.Normal,"Windowed mode restores a resizable decorated window");
            SliderSetting("Jasność",1.15);Click("Zastosuj");Check(Math.Abs(preferences.Brightness-1.15)<.001,"A second apply uses the newly edited draft rather than stale controls");await Capture("settings-video");
            Click("Dźwięk");SliderSetting("Głośność główna",.4);SliderSetting("Dźwięki interfejsu",.25);Click("Zastosuj");Check(Math.Abs(audio.EffectiveVolume-.1)<.0001,"Master and interface volumes combine in the actual audio player");
            Toggle("Wycisz wszystkie dźwięki",true);Click("Zastosuj");Check(audio.EffectiveVolume==0,"Mute reaches the actual audio player's effective volume");
            var wave=InterfaceAudio.Wave();Check(wave.Length>44&&System.Text.Encoding.ASCII.GetString(wave,0,4)=="RIFF"&&System.Text.Encoding.ASCII.GetString(wave,8,4)=="WAVE","Interface feedback generates a valid PCM WAV asset");await Capture("settings-audio");
            Click("Sterowanie");Descendants<Button>(overlay!).Single(b=>b.Tag is GameAction.NextUnit).RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));KeyPress(Key.F5);
            Check(capturingBinding==GameAction.NextUnit&&settingsDraft!.Bindings[GameAction.NextUnit]==Key.Tab&&settingsStatus!.Text.Contains("przypisany"),"Duplicate shortcuts are rejected without replacing an existing action");
            KeyPress(Key.F8);Bind(GameAction.RotateRight,Key.R);SliderSetting("Szybkość przesuwania mapy",1.5);Click("Zastosuj");Check(preferences.Bindings[GameAction.NextUnit]==Key.F8&&preferences.Bindings[GameAction.RotateRight]==Key.R,"Custom global and camera shortcuts are persisted");
            Descendants<Button>(overlay!).Single(b=>b.Tag is GameAction.Save).RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));KeyPress(Key.Escape);Check(capturingBinding==null&&overlayScreen==OverlayScreen.Settings,"Esc cancels key capture while keeping settings open");await Capture("settings-controls");
            Click("Interfejs");Toggle("Pokaż nazwy i ikony zasobów",false);Toggle("Pokaż siatkę pól",true);Toggle("Pokaż statystyki renderowania",true);Click("Zastosuj");
            Click("Rozgrywka");Toggle("Automatyczny zapis kampanii",false);Toggle("Potwierdzaj zakończenie tury",true);Click("Zastosuj");KeyPress(Key.Escape);
            var s=World.Create();Narrative.Resolve(s,0);string original=SaveGame.Serialize(s);Start(s);root.UpdateLayout();await Task.Delay(160);map.RenderGpuFrame();
            Check(map.Photographic is{} renderer&&renderer.MaximumWidth==1100&&!renderer.Reflections&&Math.Abs(renderer.Exposure-.87*1.15)<.001,"Starting a campaign applies saved render quality and brightness");
            Check(!map.ShowResources&&map.ShowGrid&&map.ShowRenderStatistics,"Starting a campaign applies interface preferences");
            Check(Descendants<Button>(shell).Any(b=>b.Content?.ToString()=="Następny oddział  F8"),"The footer displays the player's current shortcut rather than the default key");
            KeyPress(Key.Escape);Check(overlayScreen==OverlayScreen.Pause&&new[]{"Powrót do rozgrywki","Zapisz grę","Ustawienia","Wyjście do menu głównego"}.All(title=>Descendants<Button>(overlay!).Any(b=>b.Content?.ToString()==title)),"Esc opens the complete in-game pause menu");await Capture("settings-pause");
            await Task.Delay(150);int frames=map.Photographic!.Frames;await Task.Delay(250);Check(map.Photographic.Frames==frames&&map.InteractionSuspended,"Pause suspends automatic environment rendering and map interaction");
            double yaw=map.CameraYaw;CameraKeyPress(Key.R);KeyPress(Key.F8);Check(yaw==map.CameraYaw&&selectedUnit==-1&&!map.OrderScreen(new(300,300)),"Camera shortcuts, global actions and map orders are blocked behind pause");
            string manual=Path.Combine(folder,"pause-manual.crown");SavePathOverride=()=>null;Click("Zapisz grę");Check(overlayScreen==OverlayScreen.Pause&&lastManualSave==null,"Cancelling the native save-path selection preserves pause and campaign");
            SavePathOverride=()=>manual;Click("Zapisz grę");Check(File.Exists(manual)&&SaveGame.Serialize(SaveGame.Read(manual))==original&&pauseStatus!.Text.Contains("Zapisano"),"Pause save writes and reloads the exact campaign even with autosave disabled");
            SavePathOverride=()=>folder;Click("Zapisz grę");Check(overlayScreen==OverlayScreen.Pause&&pauseStatus!.Text.Contains("nie powiódł")&&SaveGame.Serialize(SaveGame.Read(manual))==original,"A failed manual save stays in pause and preserves the previous valid save");SavePathOverride=()=>manual;
            Click("Ustawienia");Check(overlayScreen==OverlayScreen.Settings,"Pause opens the same settings screen");KeyPress(Key.Escape);Check(overlayScreen==OverlayScreen.Pause,"Esc from pause settings returns to pause");KeyPress(Key.Escape);Check(overlay==null&&!map.InteractionSuspended,"Esc from pause resumes the actual campaign");
            KeyPress(Key.F8);Check(selectedUnit>=0&&state.Units.Any(u=>u.Id==selectedUnit&&u.Owner==0),"The rebound next-unit shortcut selects a real player unit");
            yaw=map.CameraYaw;CameraKeyPress(Key.R);Check(map.CameraYaw!=yaw,"The rebound camera shortcut changes the actual camera");yaw=map.CameraYaw;CameraKeyPress(Key.E);Check(map.CameraYaw==yaw,"The previous camera key no longer triggers its old action");
            Check(SaveGame.Serialize(state)==original,"Settings, pauses, saves, selection and camera changes preserve campaign state and RNG");
            int turn=state.Turn;KeyPress(Key.Space);Check(overlay!=null&&state.Turn==turn&&Descendants<Button>(overlay).Any(b=>b.Content?.ToString()=="Zakończ turę"),"The end-turn preference opens a real confirmation without advancing time");KeyPress(Key.Escape);Check(state.Turn==turn,"Cancelling end-turn confirmation does not advance the campaign");
            KeyPress(Key.Escape);Click("Wyjście do menu głównego");Check(!InCampaign&&state.Turn==turn&&Descendants<Button>(root).Any(b=>b.Content?.ToString()=="Wróć do bieżącej rozgrywki"),"Returning to main menu retains an unsaved campaign in memory");Click("Wróć do bieżącej rozgrywki");Check(InCampaign&&state.Turn==turn&&!map.ShowResources,"Returning from main menu restores the campaign and applied preferences");
            LoadPath(manual);CloseModal();root.UpdateLayout();await Task.Delay(120);map.RenderGpuFrame();Check(preferences.Bindings[GameAction.NextUnit]==Key.F8&&map.Photographic!.MaximumWidth==1100&&!map.ShowResources,"Loading a campaign keeps player preferences independent of its save");
            enableDiagnosticAutoSave=true;if(File.Exists(AutoPath))File.Delete(AutoPath);TryAutoSave(true);Check(!File.Exists(AutoPath),"Disabling autosave prevents a real diagnostic autosave write");
            KeyPress(Key.Escape);Click("Ustawienia");Click("Rozgrywka");Toggle("Automatyczny zapis kampanii",true);Click("Zastosuj");KeyPress(Key.Escape);TryAutoSave(true);Check(File.Exists(AutoPath)&&SaveGame.Serialize(SaveGame.Read(AutoPath))==original,"Enabling autosave writes the actual campaign to its isolated save directory");enableDiagnosticAutoSave=false;KeyPress(Key.Escape);
            int beforeTurn=state.Turn;KeyPress(Key.Space);Click("Zakończ turę");Check(state.Turn==beforeTurn+1,"Accepting end-turn confirmation advances exactly one turn");CloseModal();
            string invalid=Path.Combine(folder,"corrupt-settings.json");File.WriteAllText(invalid,"broken-json");var fallback=PreferencesStore.Read(invalid,out warning);Check(warning!=null&&fallback.Bindings[GameAction.Save]==Key.F5&&File.ReadAllText(invalid)=="broken-json","Corrupt preferences fall back safely without overwriting their source");
            var normalized=new GamePreferences{MasterVolume=8,Brightness=-5,PanSpeed=double.NaN,Bindings=new(){[GameAction.Save]=Key.Escape}}.Normalize();Check(normalized.MasterVolume==1&&normalized.Brightness==.6&&normalized.PanSpeed==1&&normalized.Bindings.Count==GamePreferences.DefaultBindings().Count,"Invalid preference ranges and bindings normalize to usable values");
            Width=1200;Height=800;await Task.Delay(140);KeyPress(Key.Escape);Click("Ustawienia");root.UpdateLayout();var apply=Button("Zastosuj");var location=apply.TransformToAncestor(root).Transform(new Point());Check(location.Y>=0&&location.Y+apply.ActualHeight<=root.ActualHeight,"Apply and return controls stay visible in the compact settings window");await Capture("settings-compact");
            Click("Przywróć domyślne");Check(preferences.Quality==1&&settingsDraft!.Quality==0,"Restore defaults prepares a draft without silently changing active settings");Click("Zastosuj");Check(preferences.Quality==0&&preferences.Bindings[GameAction.NextUnit]==Key.Tab&&preferences.AutoSave&&audio.EffectiveVolume>0,"Applying restored defaults resets every section together");KeyPress(Key.Escape);KeyPress(Key.Escape);
            ShowResourceLegend();KeyPress(Key.Escape);Check(overlay==null,"Esc closes an ordinary dialog before opening pause");KeyPress(Key.Escape);Check(overlayScreen==OverlayScreen.Pause,"A subsequent Esc opens pause after a normal dialog closes");
            Width=1600;Height=980;await Task.Delay(120);await Capture("settings-final");SavePathOverride=null;enableDiagnosticAutoSave=false;
            checks.Add($"{checks.Count} settings desktop integration checks passed.");File.WriteAllLines(Path.Combine(folder,"ui-smoke.txt"),checks);Close();
        }
        catch(Exception ex){enableDiagnosticAutoSave=false;checks.Add(ex.ToString());File.WriteAllLines(Path.Combine(folder,"ui-smoke.txt"),checks);if(!arguments.Contains("--smoke-force-failure"))await Capture("settings-failure");Application.Current.Shutdown(3);}
    }
}
