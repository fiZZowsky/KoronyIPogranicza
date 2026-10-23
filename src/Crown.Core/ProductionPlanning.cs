namespace Crown.Core;

public static class ProductionPlanning
{
    public const int QueueLimit=8;
    public static bool Repeatable(Project project)=>project.Unit!=null||project.Id is "treasury" or "festival";
    public static string? ProjectError(GameState s,City city,string id)
    {
        var project=Content.Projects.FirstOrDefault(p=>p.Id==id);
        if(project==null)return "Nieznany projekt.";
        if(!Repeatable(project)&&city.Buildings.Contains(id))return "Ten budynek został już ukończony.";
        if(project.Technology is string tech&&!s.Factions[city.Owner].Technologies.Contains(tech))return "Wymagana technologia: "+Content.Tech(tech).Name+".";
        if(project.Unit==UnitKind.Settler&&city.Population<3)return "Osadnicy wymagają co najmniej trzech mieszkańców; rekrutacja zużyje jednego.";
        if(UnitModels.RequiresHorses(project.Id)&&!Engine.HasResource(s,city.Owner,Resource.Horses))return "Jazda konna wymaga koni we własnych granicach.";
        return Epochs.ProjectError(s,city,project);
    }
    public static string? CurrentBlock(GameState s,City city)=>city.ProductionPaused?"Produkcja została wstrzymana przez gracza.":ProjectError(s,city,city.Project);
    public static string? EnqueueError(GameState s,int cityId,string id,bool repeat=false)
    {
        var city=s.Cities.FirstOrDefault(c=>c.Id==cityId&&c.Owner==0);
        var project=Content.Projects.FirstOrDefault(p=>p.Id==id);
        if(s.Ending!=null||city==null||project==null)return "Nieprawidłowe miasto lub projekt.";
        if(s.Factions[city.Owner].SpaceMilestones.Contains(id))return "Państwo ukończyło już ten program.";
        if(city.ProductionQueue.Count>=QueueLimit)return "Plan może zawierać najwyżej osiem oczekujących zleceń.";
        if(repeat&&!Repeatable(project))return "Powtarzać można oddziały, roboty publiczne i święto wspólnoty.";
        if(!Repeatable(project)&&(city.Buildings.Contains(id)||city.Project==id||city.ProductionQueue.Any(o=>o.Project==id)))
            return "Ten budynek już istnieje, jest budowany lub znajduje się w planie.";
        return null;
    }
    internal static void Next(GameState s,City city)
    {
        if(city.RepeatProject)city.ProductionQueue.Add(new(){Project=city.Project,Repeat=true});
        city.RepeatProject=false;
        while(city.ProductionQueue.Count>0)
        {
            var order=city.ProductionQueue[0];city.ProductionQueue.RemoveAt(0);var project=Content.Project(order.Project);
            if(!Repeatable(project)&&(city.Buildings.Contains(project.Id)||s.Factions[city.Owner].SpaceMilestones.Contains(project.Id)))
            {if(city.Owner==0)s.Log($"{city.Name}: pominięto ukończony już projekt {project.Name} w kolejce.","produkcja");continue;}
            city.Project=order.Project;city.RepeatProject=order.Repeat;
            if(city.Owner==0)s.Log($"{city.Name}: następne zlecenie — {project.Name}. "+(ProjectError(s,city,project.Id)??"Kontynuujemy bez utraty zgromadzonej nadwyżki produkcji."),"produkcja");
            return;
        }
        city.Project=s.EpochCampaign?Engine.AvailableProjects(s,city).FirstOrDefault(p=>p.Unit==null&&p.Id is not ("treasury" or "festival"))?.Id??"treasury":Engine.AvailableProjects(s,city).FirstOrDefault(p=>p.Unit==null)?.Id??"spearmen";
    }
    internal static void Validate(GameState s)
    {
        foreach(var city in s.Cities)
        {
            if(city.ProductionQueue==null||city.ProductionQueue.Count>QueueLimit||city.RepeatProject&&!Repeatable(Content.Project(city.Project)))
                throw new InvalidDataException("Uszkodzony plan produkcji miasta.");
            var buildings=new HashSet<string>();
            foreach(var order in city.ProductionQueue)
            {
                var project=order==null?null:Content.Projects.FirstOrDefault(p=>p.Id==order.Project);
                if(project==null||order!.Repeat&&!Repeatable(project)||!Repeatable(project)&&!buildings.Add(project.Id))
                    throw new InvalidDataException("Nieprawidłowe lub powielone zlecenie produkcji.");
            }
        }
    }
}

public static partial class Engine
{
    private static City? PlanningCity(GameState s,int cityId)=>s.Ending==null?s.Cities.FirstOrDefault(c=>c.Id==cityId&&c.Owner==0):null;
    public static Result EnqueueProject(GameState s,int cityId,string id,bool repeat=false)
    {
        if(ProductionPlanning.EnqueueError(s,cityId,id,repeat) is string error)return Result.Fail(error);
        var city=PlanningCity(s,cityId)!;city.ProductionQueue.Add(new(){Project=id,Repeat=repeat});
        s.Log($"{city.Name}: dodano do planu {Content.Project(id).Name}"+(repeat?" (powtarzane).":"."),"produkcja");
        return Result.Ok("Dodano zlecenie. Wymagania sprawdzimy przy jego wykonywaniu.");
    }
    public static Result MoveProductionOrder(GameState s,int cityId,int index,int direction)
    {
        var city=PlanningCity(s,cityId);
        if(city==null||direction is not (-1 or 1)||index<0||index>=city.ProductionQueue.Count||index+direction<0||index+direction>=city.ProductionQueue.Count)
            return Result.Fail("Nie można tak przesunąć zlecenia.");
        (city.ProductionQueue[index],city.ProductionQueue[index+direction])=(city.ProductionQueue[index+direction],city.ProductionQueue[index]);
        return Result.Ok("Zmieniono kolejność oczekujących zleceń bez zmiany obecnej produkcji.");
    }
    public static Result RemoveProductionOrder(GameState s,int cityId,int index)
    {
        var city=PlanningCity(s,cityId);if(city==null||index<0||index>=city.ProductionQueue.Count)return Result.Fail("Nie ma takiego zlecenia.");
        city.ProductionQueue.RemoveAt(index);return Result.Ok("Usunięto oczekujące zlecenie. Obecny projekt trwa dalej.");
    }
    public static Result ClearProductionQueue(GameState s,int cityId)
    {
        var city=PlanningCity(s,cityId);if(city==null||city.ProductionQueue.Count==0)return Result.Fail("Brak oczekujących zleceń do usunięcia.");
        city.ProductionQueue.Clear();return Result.Ok("Usunięto oczekujące zlecenia. Bieżący projekt i jego powtarzanie pozostają w mocy.");
    }
    public static Result SetProductionPaused(GameState s,int cityId,bool paused)
    {
        var city=PlanningCity(s,cityId);if(city==null||city.ProductionPaused==paused)return Result.Fail("Nieprawidłowe miasto lub produkcja już ma ten stan.");
        city.ProductionPaused=paused;s.Log($"{city.Name}: "+(paused?"wstrzymano produkcję.":"wznowiono produkcję."),"produkcja");
        return Result.Ok(paused?"Produkcja wstrzymana. Zgromadzona praca pozostaje, nowa nie jest odkładana.":"Produkcja wznowiona; wymagania projektu nadal obowiązują.");
    }
    public static Result SetProjectRepeat(GameState s,int cityId,bool repeat,int? index=null)
    {
        var city=PlanningCity(s,cityId);
        if(city==null||index is int i&&(i<0||i>=city.ProductionQueue.Count))return Result.Fail("Nieprawidłowe zlecenie.");
        string id=index is int n?city.ProductionQueue[n].Project:city.Project;
        bool current=index is int at?city.ProductionQueue[at].Repeat:city.RepeatProject;
        if(current==repeat||!ProductionPlanning.Repeatable(Content.Project(id)))return Result.Fail("To zlecenie nie może otrzymać wskazanego trybu powtarzania.");
        if(index is int slot)city.ProductionQueue[slot].Repeat=repeat;else city.RepeatProject=repeat;
        return Result.Ok(repeat?"Po ukończeniu zlecenie wróci na koniec kolejki.":"Zlecenie zostanie wykonane jednokrotnie.");
    }
    public static Result StartQueuedProject(GameState s,int cityId)
    {
        var city=PlanningCity(s,cityId);if(city==null||city.ProductionQueue.Count==0)return Result.Fail("Brak zlecenia do rozpoczęcia.");
        var order=city.ProductionQueue[0];var project=Content.Project(order.Project);
        if(!ProductionPlanning.Repeatable(project)&&city.Buildings.Contains(project.Id))return Result.Fail("Pierwszy budynek jest już ukończony. Usuń go z planu.");
        if(city.Project!=order.Project)city.Production*=.8;
        city.Project=order.Project;city.RepeatProject=order.Repeat;city.ProductionQueue.RemoveAt(0);
        s.Log($"{city.Name}: rozpoczęto pierwsze zlecenie {project.Name}. "+(ProductionPlanning.CurrentBlock(s,city)??"Wymagania spełnione."),"produkcja");
        return Result.Ok("Wybrano pierwsze zlecenie. Zmiana projektu zachowuje 80% pracy; ten sam projekt zachowuje całość. Stan wstrzymania pozostaje w mocy.");
    }
}
