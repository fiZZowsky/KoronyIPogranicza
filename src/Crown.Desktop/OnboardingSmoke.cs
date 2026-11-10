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
    private async Task OnboardingSmoke(string folder)
    {
        Directory.CreateDirectory(folder);var checks=new List<string>();
        void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);checks.Add("PASS "+message);}
        Button Find(string title){root.UpdateLayout();return Descendants<Button>(overlay??root).First(b=>b.Content?.ToString()==title);}
        void Click(string title){var b=Find(title);if(!b.IsEnabled)throw new InvalidOperationException("Disabled: "+title);b.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));root.UpdateLayout();}
        void Nav(string name){root.UpdateLayout();Descendants<Button>(shell).Single(b=>b.ToolTip?.ToString()==name).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));root.UpdateLayout();}
        void Escape(){injectingSettingsKey=true;try{RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice,PresentationSource.FromVisual(this)!,0,Key.Escape){RoutedEvent=Keyboard.PreviewKeyDownEvent});}finally{injectingSettingsKey=false;}root.UpdateLayout();}
        void Decision(){root.UpdateLayout();Descendants<Button>(overlay!).First(b=>b.IsEnabled&&b.Content is StackPanel).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));root.UpdateLayout();}
        bool Visible(FrameworkElement e){root.UpdateLayout();var p=e.TransformToAncestor(root).Transform(new Point());return p.X>=0&&p.Y>=0&&p.X+e.ActualWidth<=root.ActualWidth+1&&p.Y+e.ActualHeight<=root.ActualHeight+1;}
        async Task Capture(string name){root.UpdateLayout();await Task.Delay(140);map?.RenderGpuFrame();var bmp=new RenderTargetBitmap((int)root.ActualWidth,(int)root.ActualHeight,96,96,PixelFormats.Pbgra32);bmp.Render(root);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bmp));using var output=File.Create(Path.Combine(folder,name+".png"));encoder.Save(output);}
        try
        {
            if(arguments.Contains("--smoke-force-failure"))throw new InvalidOperationException("Intentional onboarding diagnostic failure.");
            preferences.ShowTutorialWelcome=true;Landing();root.UpdateLayout();
            Check(!Descendants<TextBox>(root).Any()&&!Descendants<ComboBox>(root).Any(),"Main menu has no seed editor or upfront difficulty controls");await Capture("onboarding-menu");
            Click("ROZPOCZNIJ NOWĄ GRĘ   →");Check(state==null&&overlay!=null&&new[]{"Spokojny","Standard","Wymagający"}.All(t=>Descendants<Button>(overlay).Any(b=>b.Content?.ToString()==t)),"New game first opens three difficulty choices without generating a campaign");await Capture("onboarding-difficulty");
            Escape();Check(state==null&&overlay==null&&!InCampaign,"Cancelling difficulty returns to menu without creating a world");
            Click("ROZPOCZNIJ NOWĄ GRĘ   →");Click("Spokojny");int seed=state!.Seed;
            Check(state.Difficulty=="Spokojny"&&state.PendingEvent=="founding"&&Find("Rozpocznij samouczek")!=null,"A new campaign opens welcome before its founding decision");
            string untouched=SaveGame.Serialize(state);var welcome=Descendants<Border>(overlay!).First(b=>b.Width==680);var position=welcome.TransformToAncestor(root).Transform(new Point());
            Check(Math.Abs(position.X+welcome.ActualWidth/2-root.ActualWidth/2)<2&&Math.Abs(position.Y+welcome.ActualHeight/2-root.ActualHeight/2)<2,"Welcome is centered in the actual game window");
            Check(map.InteractionSuspended&&tutorialStep==-1,"Welcome pauses map input until the player responds");await Capture("onboarding-welcome");
            Descendants<CheckBox>(overlay!).Single().IsChecked=true;Click("Rozpocznij samouczek");
            Check(!preferences.ShowTutorialWelcome&&!PreferencesStore.Read(preferencesPath,out _).ShowTutorialWelcome,"Do not show again persists to the real isolated preferences file");
            Check(tutorialStep==0&&overlay==null&&!map.InteractionSuspended&&SaveGame.Serialize(state)==untouched,"Starting the tutorial changes no campaign data or RNG and leaves the map usable");
            Click("Pokaż pierwszy krok");Check(tutorialStep==1&&tutorialHighlights.Any(h=>h.Button.Content?.ToString()=="Sala rady"),"First task highlights the actual council button");
            Click("Sala rady");Check(tutorialHost==null||tutorialHost.Visibility==Visibility.Collapsed,"Coach yields its space to the council dialog");await Capture("onboarding-council");
            Click("Wróć do mapy — odpowiedz później");Check(tutorialStep==1&&state.PendingEvent=="founding","Deferring the council does not falsely complete the decision task");
            Click("Sala rady");Decision();Check(tutorialStep==2&&state.PendingEvent==null&&state.Chronicle.Any(e=>e.Category=="decyzja"),"Any real council answer completes the task and records its actual consequences");
            string afterDecision=SaveGame.Serialize(state);Check(tutorialHighlights.Any(h=>h.Button.Content?.ToString()=="Wybierz projekt   →"&&Visible(h.Button)),"Production task scrolls the actual city project button into view");await Capture("onboarding-city");
            Click("Wybierz projekt   →");Click("Zamknij");Check(tutorialStep==2,"Merely closing production does not count as planning it");
            Click("Wybierz projekt   →");Click("Pozostaw obecny projekt");Check(tutorialStep==3&&SaveGame.Serialize(state)==afterDecision,"Keeping the existing project advances without losing production or changing the save");
            Nav("Badania");Check(tutorialStep==3&&Find("Pozostaw obecne badania")!=null,"Opening research waits for a deliberate research decision");await Capture("onboarding-research");
            Click("Pozostaw obecne badania");Check(tutorialStep==4&&SaveGame.Serialize(state)==afterDecision,"Keeping existing research preserves science and campaign RNG");
            Click("Następny oddział  "+GamePreferences.KeyName(preferences.Bindings[GameAction.NextUnit]));
            Check(tutorialStep==5&&selectedUnit>=0,"The actual next-unit button selects a real unit and starts movement training");
            var unit=state.Units.Single(u=>u.Id==selectedUnit);int origin=unit.Tile;Order(origin);Check(tutorialStep==5,"A no-op order does not falsely complete movement training");
            var target=World.Neighbors(state,origin).First(t=>t.Passable&&t.Explored&&t.Owner is -1 or 0&&!state.Units.Any(u=>u.Owner!=0&&u.Tile==t.Id)&&!state.Cities.Any(c=>c.Owner!=0&&c.Tile==t.Id));Order(target.Id);
            Check(tutorialStep==6&&unit.Tile==target.Id,"The actual map order handler moves the unit before completing its task");await Capture("onboarding-first-period");
            int turn=state.Turn;Click("ZAKOŃCZ TURĘ   →");Check(state.Turn==turn+1&&tutorialStep==7,"Ending the first period advances the actual campaign exactly once");
            if(overlay!=null)CloseModal();Nav("Kronika");Check(tutorialStep==8&&tab=="history","Opening the real chronicle reaches the final lesson");await Capture("onboarding-complete");
            string completed=SaveGame.Serialize(state);Click("Zakończ samouczek");Check(!TutorialActive&&tutorialHost==null&&tutorialHighlights.Count==0&&SaveGame.Serialize(state)==completed,"Finishing removes coaching and highlights without changing the campaign");
            Nav("Poradnik");Check(Descendants<Expander>(side).Count()==8&&Descendants<Expander>(side).All(e=>!e.IsExpanded),"Reference guide starts with collapsed topics rather than a wall of text");Click("Rozpocznij samouczek krok po kroku");Check(tutorialStep==0,"Guide restarts the interactive tutorial after it was completed or dismissed");
            Click("Pokaż pierwszy krok");Escape();Check(overlayScreen==OverlayScreen.Pause&&tutorialStep==1,"Esc pauses the game while preserving tutorial progress");
            Click("Wyjście do menu głównego");Click("Wróć do bieżącej rozgrywki");Check(tutorialStep==1&&tutorialHost!=null&&overlay==null,"Returning from the main menu resumes the existing tutorial without another welcome");
            Click("Przerwij samouczek");Check(!TutorialActive&&SaveGame.Serialize(state)==completed,"Stopping a tutorial preserves all gameplay state");
            Escape();Click("Ustawienia");Click("Rozgrywka");var setting=Descendants<CheckBox>(overlay!).Single(c=>c.Content?.ToString()=="Pokaż powitanie z propozycją samouczka");Check(setting.IsChecked==false,"Gameplay settings reflect the welcome dismissal preference");
            setting.IsChecked=true;Escape();Check(!preferences.ShowTutorialWelcome,"Leaving settings discards an unapplied welcome preference");
            Click("Ustawienia");Click("Rozgrywka");Descendants<CheckBox>(overlay!).Single(c=>c.Content?.ToString()=="Pokaż powitanie z propozycją samouczka").IsChecked=true;Click("Zastosuj");Check(preferences.ShowTutorialWelcome&&PreferencesStore.Read(preferencesPath,out _).ShowTutorialWelcome,"Settings can re-enable the welcome and persist it");Escape();Escape();
            Landing();Click("ROZPOCZNIJ NOWĄ GRĘ   →");Click("Wymagający");Check(state.Difficulty=="Wymagający"&&state.Seed!=seed&&overlay!=null&&!TutorialActive,"Another campaign gets a fresh automatic world and the selected challenge");
            string beforeSkip=SaveGame.Serialize(state);Click("Graj bez samouczka");Check(!TutorialActive&&state.PendingEvent=="founding"&&overlay!=null&&SaveGame.Serialize(state)==beforeSkip,"Skipping goes to the founding council without consuming its decision");CloseModal();
            preferences.ShowTutorialWelcome=false;Landing();Click("ROZPOCZNIJ NOWĄ GRĘ   →");Click("Standard");Check(state.Difficulty=="Standard"&&Find("Wróć do mapy — odpowiedz później")!=null&&!TutorialActive,"Disabled welcome starts a standard campaign directly at its council");CloseModal();
            // Loading an old or advanced campaign never replaces its gameplay with a training scenario.
            string save=Path.Combine(folder,"tutorial-existing.crown");Narrative.Resolve(state,0);SaveGame.Write(state,save);string saved=SaveGame.Serialize(state);preferences.ShowTutorialWelcome=true;LoadPath(save);
            Check(Find("Rozpocznij samouczek")!=null&&SaveGame.Serialize(state)==saved,"An existing save can receive onboarding without losing campaign data");
            string originalPath=preferencesPath;preferencesPath=folder;Descendants<CheckBox>(overlay!).Single().IsChecked=true;Click("Graj bez samouczka");
            Check(overlay!=null&&preferences.ShowTutorialWelcome&&Descendants<TextBlock>(overlay).Any(t=>t.Text.Contains("Nie udało się zapisać")),"A preference write failure stays visible and never claims dismissal was saved");preferencesPath=originalPath;Click("Rozpocznij samouczek");Click("Pokaż pierwszy krok");
            Check(tutorialStep==1&&state.PendingEvent==null&&Find("Rada nie ma spraw — dalej")!=null,"Tutorial on a progressed save handles an already resolved council");Click("Rada nie ma spraw — dalej");
            WindowState=WindowState.Normal;Width=1200;Height=800;await Task.Delay(200);root.UpdateLayout();RefreshTutorial();root.UpdateLayout();
            Check(tutorialHost!=null&&Visible(tutorialHost)&&tutorialHighlights.Any(h=>Visible(h.Button)),"Coach and actual project target fit the compact game window");
            var coachScroll=(ScrollViewer)tutorialHost!.Child;coachScroll.ScrollToEnd();root.UpdateLayout();Check(Visible(Find("Przerwij samouczek")),"Tutorial exit remains reachable in the compact layout");await Capture("onboarding-compact");Click("Przerwij samouczek");
            var noUnits=World.CreateLegacy();Narrative.Resolve(noUnits,0);noUnits.Units.RemoveAll(u=>u.Owner==0);Start(noUnits);BeginTutorial();Click("Pokaż pierwszy krok");Click("Rada nie ma spraw — dalej");Click("Wybierz projekt   →");Click("Pozostaw obecny projekt");Nav("Badania");Click("Pozostaw obecne badania");
            Click("Brak gotowych oddziałów — dalej");Check(tutorialStep==6,"A campaign with no available units can proceed without a tutorial soft lock");Click("Przerwij samouczek");
            // The alternate path also uses real project and research buttons, rather than the keep actions.
            var alternative=World.CreateLegacy();Narrative.Resolve(alternative,0);Start(alternative);BeginTutorial();Click("Pokaż pierwszy krok");Click("Rada nie ma spraw — dalej");Click("Wybierz projekt   →");
            root.UpdateLayout();var projectButton=Descendants<Button>(overlay!).First(b=>b.Content is StackPanel);projectButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));root.UpdateLayout();Check(tutorialStep==3&&overlay==null,"Selecting an actual project also completes the production lesson");
            Nav("Badania");Click("Rozpocznij badania");Check(tutorialStep==4&&state.Player.Research=="pottery","Selecting a new available technology also completes the research lesson");
            Click("Następny oddział  "+GamePreferences.KeyName(preferences.Bindings[GameAction.NextUnit]));Click("Pomiń ćwiczenie ruchu");Check(tutorialStep==6,"Movement training can be explicitly skipped without requiring a risky order");
            state.Ending="Koniec historii";Refresh();Click("Historia zakończona — dalej");Nav("Kronika");Click("Zakończ samouczek");Check(!TutorialActive,"A completed campaign can finish its interface tour without an impossible turn");
            alternative=World.CreateLegacy();Narrative.Resolve(alternative,0);alternative.Player.Research="";Start(alternative);BeginTutorial();Click("Pokaż pierwszy krok");Click("Rada nie ma spraw — dalej");Click("Wybierz projekt   →");Click("Pozostaw obecny projekt");Nav("Badania");Click("Wrócę do badań później");Check(tutorialStep==4,"An existing save without active research has an explicit way forward");Click("Przerwij samouczek");
            string legacy=Path.Combine(folder,"legacy-settings.json");File.WriteAllText(legacy,"{\"Version\":1}");Check(PreferencesStore.Read(legacy,out var warning).ShowTutorialWelcome&&warning==null,"Older preference files default to showing onboarding without a settings migration");
            checks.Add($"{checks.Count} onboarding desktop integration checks passed.");File.WriteAllLines(Path.Combine(folder,"ui-smoke.txt"),checks);Close();
        }
        catch(Exception ex){checks.Add(ex.ToString());File.WriteAllLines(Path.Combine(folder,"ui-smoke.txt"),checks);if(!arguments.Contains("--smoke-force-failure"))await Capture("onboarding-failure");Application.Current.Shutdown(3);}
    }
}
