using Crown.Core;
using System.Windows;
using System.Windows.Controls;

namespace Crown.Desktop;

public sealed partial class MainWindow
{
    private void ShowProductionQueue(City city,string? selectedProject=null,bool repeating=false,double scrollOffset=0)
    {
        var active=Crown.Core.Content.Project(city.Project);string? block=ProductionPlanning.CurrentBlock(state,city);
        var p=Ui.Stack(Ui.Eyebrow("PLAN MIEJSKICH WARSZTATÓW"),Ui.Label(city.Name,30,Ui.Text,true),
            Ui.Label($"TERAZ · {active.Name}"+(city.RepeatProject?" · POWTARZANE":""),20,Ui.Gold,true),
            Ui.Label($"Zgromadzona praca {city.Production:0.##}/{active.Cost} · potencjał {Engine.CityYields(state,city).Production:0.##}/turę\n"+
                (block??"Produkcja trwa. Po zakończeniu wybierzemy pierwsze oczekujące zlecenie."),13,block==null?Ui.Muted:Ui.Red),
            Ui.Label("Plan obejmuje do ośmiu zleceń po obecnym projekcie. Automatyczne przejście zachowuje całą nadwyżkę pracy; kończymy najwyżej jeden projekt na miasto i turę. Przy pustym planie miasto wraca do zwykłego wyboru następnej budowy.",13,Ui.Muted));
        ScrollViewer? Scroller()=>overlay is Grid layer?layer.Children.OfType<System.Windows.Controls.Border>().FirstOrDefault()?.Child as ScrollViewer:null;
        void Apply(Result result)
        {double offset=Scroller()?.VerticalOffset??0;Do(result);if(result.Success){TryAutoSave(false);ShowProductionQueue(city,selectedProject,repeating,offset);}}
        p.Children.Add(Ui.Button(city.ProductionPaused?"Wznów produkcję":"Wstrzymaj produkcję",()=>Apply(Engine.SetProductionPaused(state,city.Id,!city.ProductionPaused))));
        if(ProductionPlanning.Repeatable(active))p.Children.Add(Ui.Button(city.RepeatProject?"Nie powtarzaj obecnego projektu":"Powtarzaj obecny projekt",()=>Apply(Engine.SetProjectRepeat(state,city.Id,!city.RepeatProject))));
        Ui.Gap(p,10);
        var projects=Crown.Core.Content.Projects;
        var selection=Ui.Combo(projects.Select(project=>$"{project.Name} · {project.Cost} produkcji").ToArray(),Math.Max(0,Array.FindIndex(projects,p=>p.Id==selectedProject)));
        var repeat=new CheckBox{Content="Po ukończeniu wróć na koniec kolejki",IsChecked=repeating,Foreground=Ui.Text,Margin=new Thickness(0,0,0,12)};
        var preview=Ui.Label("",12,Ui.Gold);
        var add=Ui.Button("Dodaj wybrany projekt do kolejki",()=>Apply(Engine.EnqueueProject(state,city.Id,projects[selection.SelectedIndex].Id,repeat.IsChecked==true)),true);
        void Update()
        {
            var chosen=projects[selection.SelectedIndex];repeat.IsEnabled=ProductionPlanning.Repeatable(chosen);if(!repeat.IsEnabled)repeat.IsChecked=false;
            selectedProject=chosen.Id;repeating=repeat.IsChecked==true;
            string? error=ProductionPlanning.EnqueueError(state,city.Id,chosen.Id,repeat.IsChecked==true);add.IsEnabled=error==null;
            preview.Text=chosen.Description+"\n"+(error??(ProductionPlanning.ProjectError(state,city,chosen.Id) is string need?"Można zaplanować. Obecne ograniczenie: "+need:"Obecnie spełniasz wymagania."));
        }
        selection.SelectionChanged+=(_,_)=>Update();repeat.Checked+=(_,_)=>Update();repeat.Unchecked+=(_,_)=>Update();Update();
        p.Children.Add(Ui.Eyebrow("DODAJ ZLECENIE"));p.Children.Add(selection);p.Children.Add(repeat);p.Children.Add(preview);p.Children.Add(add);
        p.Children.Add(Ui.Label("Możesz planować nieodkryte budowle i przyszłą rekrutację. Wymagania sprawdzamy w każdej turze; zablokowany projekt zachowuje dotychczasową pracę i nie odkłada nowej. Wstrzymanie działa tak samo na pracę; wzrost, żywność i pozostała gospodarka trwają dalej. Powtarzać można oddziały, roboty publiczne i święta.",12,Ui.Muted));
        p.Children.Add(Ui.Rule());p.Children.Add(Ui.Eyebrow($"OCZEKUJĄCE ZLECENIA · {city.ProductionQueue.Count}/{ProductionPlanning.QueueLimit}"));
        if(city.ProductionQueue.Count==0)p.Children.Add(Ui.Label("Plan jest pusty. Dodaj zlecenie, aby określić, co powstanie po obecnym projekcie.",14,Ui.Gold));
        for(int index=0;index<city.ProductionQueue.Count;index++)
        {
            int slot=index;var order=city.ProductionQueue[slot];var project=Crown.Core.Content.Project(order.Project);
            var q=Ui.Stack(Ui.Label($"{slot+1:00} · {project.Name}"+(order.Repeat?" · POWTARZANE":""),20,Ui.Gold,true),
                Ui.Label($"{project.Cost} produkcji · "+(ProductionPlanning.ProjectError(state,city,order.Project)??"Obecnie spełniasz wymagania."),12,Ui.Muted));
            var actions=new WrapPanel();void ActionButton(Button button){button.Margin=new Thickness(0,0,8,8);actions.Children.Add(button);}
            var up=Ui.Button("Wyżej #"+(slot+1),()=>Apply(Engine.MoveProductionOrder(state,city.Id,slot,-1)));up.IsEnabled=slot>0;ActionButton(up);
            var down=Ui.Button("Niżej #"+(slot+1),()=>Apply(Engine.MoveProductionOrder(state,city.Id,slot,1)));down.IsEnabled=slot+1<city.ProductionQueue.Count;ActionButton(down);
            if(ProductionPlanning.Repeatable(project))ActionButton(Ui.Button((order.Repeat?"Zakończ powtarzanie #":"Powtarzaj #")+(slot+1),()=>Apply(Engine.SetProjectRepeat(state,city.Id,!order.Repeat,slot))));
            ActionButton(Ui.Button("Usuń zlecenie #"+(slot+1),()=>Apply(Engine.RemoveProductionOrder(state,city.Id,slot))));q.Children.Add(actions);p.Children.Add(Ui.Card(q));
        }
        if(city.ProductionQueue.Count>0)
        {
            p.Children.Add(Ui.Button("Rozpocznij pierwsze zlecenie teraz",()=>Apply(Engine.StartQueuedProject(state,city.Id))));
            p.Children.Add(Ui.Label("Ręczna zmiana obecnego projektu zachowuje 80% pracy; wybór tego samego zachowuje całość. Rozpoczęcie nie znosi wstrzymania ani wymagań. Powtarzanie wcześniejszego projektu zostaje zastąpione ustawieniem nowego zlecenia.",12,Ui.Muted));
            p.Children.Add(Ui.Button("Usuń wszystkie oczekujące zlecenia",()=>Apply(Engine.ClearProductionQueue(state,city.Id))));
        }
        p.Children.Add(Ui.Button("Wybierz budowę od razu",()=>ShowProjects(city)));p.Children.Add(Ui.Button("Wróć do mapy",CloseModal));Modal(p,830);
        if(scrollOffset>0){root.UpdateLayout();Scroller()?.ScrollToVerticalOffset(scrollOffset);}
    }
}
