using Crown.Core;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Crown.Desktop;

public sealed partial class MainWindow
{
    private int tutorialStep=-1;
    private int tutorialTurn;
    private Border? tutorialHost;
    private GameState? welcomedCampaign;
    private readonly List<(Button Button,Brush Brush,Thickness Thickness)> tutorialHighlights=[];
    private bool TutorialActive=>tutorialStep>=0;

    private void ShowNewGame()
    {
        var p=Ui.Stack(Ui.Eyebrow("NOWA KAMPANIA"),Ui.Label("Wybierz poziom trudności",30,Ui.Text,true),
            Ui.Label("Każda nowa gra tworzy inny świat. Wybierz tempo wyzwania dla swojej historii.",14,Ui.Muted));
        foreach(var entry in new[]{("Spokojny","Na pierwszą kampanię. Sąsiedzi rozwijają się wolniej i wywierają mniejszą presję."),
            ("Standard","Równe warunki rozwoju. Dla graczy znających podstawy strategii."),
            ("Wymagający","Szybszy rozwój rywali i silniejsza presja. Dla doświadczonych władców.")})
        {
            var b=Ui.Button(entry.Item1,()=>
            {
                // World generation has its own deterministic RNG; menu browsing never consumes it.
                Start(World.Create(RandomNumberGenerator.GetInt32(1,int.MaxValue),entry.Item1));
                EnterCampaign();
            },entry.Item1=="Spokojny");
            p.Children.Add(b);p.Children.Add(Ui.Label(entry.Item2,13,Ui.Muted));Ui.Gap(p,12);
        }
        p.Children.Add(Ui.Button("Wróć do menu",CloseModal));Modal(p,650);
    }

    private void EnterCampaign()
    {
        if(state.Ending!=null)return;
        if((!diagnostic||arguments.Contains("--ui-smoke-onboarding"))&&preferences.ShowTutorialWelcome&&!ReferenceEquals(welcomedCampaign,state)){welcomedCampaign=state;ShowTutorialWelcome();}
        else if(state.PendingEvent!=null)ShowCouncil();
    }

    private void ShowTutorialWelcome()
    {
        var p=Ui.Stack(Ui.Eyebrow("WITAJ W DOLINIE VEYRU"),Ui.Label("Twoja historia zaczyna się tutaj",30,Ui.Text,true),
            Ui.Label("Chcesz, aby doradca poprowadził Cię przez pierwsze kroki? Pokaże, gdzie kliknąć, jak podjąć decyzję i jak przygotować pierwszy okres rozwoju. Uczysz się we własnej kampanii, we własnym tempie.",15,Ui.Muted));
        var remember=new CheckBox{Content="Nie pokazuj ponownie",Foreground=Ui.Text,Margin=new Thickness(0,20,0,16),FontSize=14};p.Children.Add(remember);
        var error=Ui.Label("",12,Ui.Red);
        void Accept(bool tutorial)
        {
            var value=preferences.Copy();value.ShowTutorialWelcome=remember.IsChecked!=true;
            if(value.ShowTutorialWelcome!=preferences.ShowTutorialWelcome&&!SavePreferences(value,out var message)){error.Text=message;return;}
            preferences=value;CloseModal();if(tutorial)BeginTutorial();else if(state.PendingEvent!=null)ShowCouncil();
        }
        p.Children.Add(Ui.Button("Rozpocznij samouczek",()=>Accept(true),true));
        p.Children.Add(Ui.Button("Graj bez samouczka",()=>Accept(false)));p.Children.Add(error);
        p.Children.Add(Ui.Label("Samouczek uruchomisz też w Poradniku. Powitanie włączysz ponownie w Ustawieniach → Rozgrywka.",12,Ui.Muted));Modal(p,680);
    }

    private void BeginTutorial()
    {
        CloseModal();tutorialStep=0;tutorialTurn=state.Turn;tab="realm";RefreshSide();RefreshTutorial();
    }
    private void StopTutorial()
    {
        tutorialStep=-1;RefreshTutorial();if(InCampaign)RefreshSide();
    }
    private void TutorialSignal(string action)
    {
        bool advance=(tutorialStep,action) switch
        {
            (1,"council")=>(state.PendingEvent==null), (2,"project")=>true, (3,"research")=>true,
            (4,"unit")=>true, (5,"move")=>true, (6,"turn")=>state.Turn>tutorialTurn,
            (7,"nav:history")=>true, _=>false
        };
        if(advance)TutorialNext();
    }
    private void TutorialNext()
    {
        tutorialStep++;
        if(tutorialStep==2&&state.Cities.FirstOrDefault(c=>c.Owner==0) is{} capital){Select(capital.Tile);map.Center(capital.Tile);}
        if(tutorialStep==6)tutorialTurn=state.Turn;
        RefreshSide();RefreshTutorial();
    }
    private void RefreshTutorial()
    {
        foreach(var h in tutorialHighlights){h.Button.BorderBrush=h.Brush;h.Button.BorderThickness=h.Thickness;}tutorialHighlights.Clear();
        if(tutorialHost!=null){shell?.Children.Remove(tutorialHost);tutorialHost=null;}
        if(!TutorialActive||!InCampaign||shell==null||overlay!=null)return;
        var (title,body)=tutorialStep switch
        {
            0=>("Spokojnie, krok po kroku","Na górze widzisz skarb, naukę i datę. Po lewej są działy państwa, po prawej szczegóły zaznaczonego miejsca. Na dole czekają rada, zapis i koniec tury. Na razie zajmiemy się tylko jedną rzeczą naraz."),
            1=>("Podejmij pierwszą decyzję",state.PendingEvent!=null?"Kliknij podświetloną Salę rady na dole. Przeczytaj wypowiedź i skutki pod odpowiedziami, a potem kliknij wybraną odpowiedź. Każda jest prawdziwą decyzją Twojej kampanii.":"Rada nie ma obecnie nowej sprawy. Gdy się pojawi, otwórz Salę rady na dole; pod każdą odpowiedzią zobaczysz skutki wyboru."),
            2=>("Zaplanuj pracę miasta","Wybraliśmy Twoją stolicę. W prawym panelu kliknij podświetlony przycisk Wybierz projekt. Obejrzyj koszty i wybierz budowę albo pozostaw obecny projekt. Produkcja przyrasta po zakończeniu tury. Postój z powodu technologii minie po jej odkryciu w Badaniach."),
            3=>("Sprawdź kierunek badań","Kliknij Badania na lewym pasku. Punkty nauki zasilają jedną technologię. Możesz rozpocząć dostępne badania albo kliknąć Pozostaw obecne badania. Nie musisz jeszcze poznawać całego drzewa epok."),
            4=>("Wybierz oddział","Kliknij podświetlony przycisk Następny oddział na dole. Kamera pokaże gotową do działania jednostkę. Jej ruch i zdrowie znajdziesz w prawym panelu. Oddziały możesz też wybierać lewym przyciskiem na mapie."),
            5=>("Wydaj rozkaz marszu","Kliknij PRAWYM przyciskiem sąsiednie, odkryte pole lądu bez obcego oddziału lub miasta. Zielone pola pokazują zasięg. Krótkie kliknięcie wydaje rozkaz; przeciąganie prawym przyciskiem obraca widok. Nie musisz atakować, aby wykonać to ćwiczenie."),
            6=>("Pozwól światu ruszyć dalej","Kliknij ZAKOŃCZ TURĘ w prawym dolnym rogu. Zostaną rozliczone plony, produkcja i badania, a sąsiedzi wykonają swoje działania. Jeśli rada czeka na odpowiedź, najpierw rozstrzygnij jej sprawę."),
            7=>("Zobacz ślad swoich wyborów","Czas minął. Kliknij podświetloną Kronikę po lewej. Znajdziesz tam daty decyzji i wydarzeń; część konsekwencji nadejdzie dopiero w przyszłości. Jeśli otworzyła się rada, odpowiedz albo wróć do mapy."),
            _=>("Możesz już pisać własną historię","Znasz podstawowy rytm: decyzja → miasto i badania → rozkazy → koniec tury → kronika. Zapisz grę przyciskiem na dole. Esc otwiera menu i ustawienia. Kolejne systemy poznawaj wtedy, gdy będą potrzebne; Poradnik jest zawsze po lewej.")
        };
        var p=Ui.Stack(Ui.Eyebrow($"DORADCA · KROK {tutorialStep+1} Z 9"),Ui.Label(title,22,Ui.Text,true),Ui.Label(body,14,Ui.Text));
        if(tutorialStep==0)p.Children.Add(Ui.Button("Pokaż pierwszy krok",TutorialNext,true));
        if(tutorialStep==1&&state.PendingEvent==null)p.Children.Add(Ui.Button("Rada nie ma spraw — dalej",TutorialNext,true));
        if(tutorialStep==2&&state.Cities.Any(c=>c.Owner==0))p.Children.Add(Ui.Button("Pokaż moją stolicę",()=>{var c=state.Cities.First(c=>c.Owner==0);Select(c.Tile);map.Center(c.Tile);}));
        if(tutorialStep==2&&!state.Cities.Any(c=>c.Owner==0))p.Children.Add(Ui.Button("Nie mam miasta — dalej",TutorialNext));
        if(tutorialStep==4&&!state.Units.Any(u=>u.Owner==0&&u.Actions>0&&u.Order==MilitaryOrder.None&&u.EscortCaravanId==null))p.Children.Add(Ui.Button("Brak gotowych oddziałów — dalej",()=>{tutorialStep=5;TutorialNext();}));
        if(tutorialStep==6&&state.Ending!=null)p.Children.Add(Ui.Button("Historia zakończona — dalej",TutorialNext));
        if(tutorialStep==5)p.Children.Add(Ui.Button("Pomiń ćwiczenie ruchu",TutorialNext));
        if(tutorialStep==8)p.Children.Add(Ui.Button("Zakończ samouczek",StopTutorial,true));
        else p.Children.Add(Ui.Button("Przerwij samouczek",StopTutorial));
        tutorialHost=new Border{Width=390,MaxHeight=Math.Max(230,ActualHeight-340),Background=Ui.Ink,BorderBrush=Ui.Gold,BorderThickness=new Thickness(1),Padding=new Thickness(18),Margin=new Thickness(20,110,20,20),HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Bottom,Child=new ScrollViewer{Content=p,VerticalScrollBarVisibility=ScrollBarVisibility.Auto}};
        Grid.SetColumn(tutorialHost,1);Grid.SetRow(tutorialHost,1);shell.Children.Add(tutorialHost);
        root.UpdateLayout();
        foreach(var b in Descendants<Button>(shell).Where(b=>IsTutorialTarget(b)))
        {tutorialHighlights.Add((b,b.BorderBrush,b.BorderThickness));b.BorderBrush=Ui.Gold;b.BorderThickness=new Thickness(3);}
        root.UpdateLayout();
        // Reveal the actual task control even when its panel needs scrolling.
        if(tutorialHighlights.Count>0)tutorialHighlights[0].Button.BringIntoView();
    }
    private bool IsTutorialTarget(Button b)=>tutorialStep switch
    {
        1=>b.Content?.ToString()=="Sala rady",2=>b.Content?.ToString()=="Wybierz projekt   →",
        3=>b.ToolTip?.ToString()=="Badania"||b.Content?.ToString()=="Pozostaw obecne badania",
        4=>b.Tag is GameAction.NextUnit,6=>b==endButton,7=>b.ToolTip?.ToString()=="Kronika",_=>false
    };
}
