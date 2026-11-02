using Crown.Core;
using System.Windows;
using System.Windows.Controls;

namespace Crown.Desktop;

public sealed partial class MainWindow
{
    private (int CityId,DistrictKind Kind)? districtPlacement;
    internal static string DistrictYieldText(Yields y)=>string.Join(" · ",new[]{(y.Food,"żywności"),(y.Production,"produkcji"),(y.Gold,"złota"),(y.Science,"nauki"),(y.Culture,"kultury")}.Where(x=>x.Item1>0).Select(x=>$"+{x.Item1:0.##} {x.Item2}"));
    internal static string DistrictStatusText(District d)=>d.Status switch{DistrictStatus.Active=>"Czynna",DistrictStatus.Building=>"Budowa",DistrictStatus.Repairing=>"Odbudowa",_=>"Splądrowana"};
    private void DistrictResult(Result result,City city)
    {Do(result);if(result.Success)TryAutoSave(false);ShowDistrictPlan(city,result.Message);}
    private void ShowDistrictPlan(City city,string? notice=null)
    {
        var districts=state.Districts.Where(d=>d.CityId==city.Id).OrderBy(d=>d.Id).ToArray();var y=Engine.CityYields(state,city);double work=Districts.Allocation(state,city,y.Production);
        var p=Ui.Stack(Ui.Eyebrow("PLAN ROZWOJU MIASTA"),Ui.Label(city.Name+" · dzielnice",31,Ui.Text,true),
            Ui.Label($"Zajęte miejsca: {districts.Length}/{Districts.Limit(city)} · populacja {city.Population}\nLimit rośnie o jedno miejsce co 4 mieszkańców, do pięciu dzielnic. Każdy rodzaj można zbudować raz.",13,Ui.Muted),
            Ui.Label($"Podział bieżącej produkcji: {work:0.##} na dzielnicę · {Math.Max(0,y.Production-work):0.##} na zwykły projekt.",13,Ui.Gold));
        if(notice!=null)p.Children.Insert(1,Ui.Label(notice,13,Ui.Gold));
        var share=Ui.Combo(["0% · wstrzymaj dzielnicę","25% · przewaga zwykłego projektu","50% · równy podział","100% · pierwszeństwo dzielnicy"],Array.IndexOf(new[]{0,25,50,100},city.DistrictShare));share.Tag="district-share";
        p.Children.Add(share);p.Children.Add(Ui.Button("Zastosuj podział pracy",()=>DistrictResult(Districts.SetShare(state,city.Id,new[]{0,25,50,100}[share.SelectedIndex]),city)));
        p.Children.Add(Ui.Label("Nakład można zmienić raz na turę. Wstrzymanie produkcji miasta zatrzymuje także dzielnicę. Niewykorzystana praca po jej ukończeniu trafia do zwykłego projektu.",12,Ui.Muted));
        foreach(var d in districts)
        {
            var t=state.Tiles[d.Tile];var card=Ui.Stack(Ui.Label(Districts.Type(d.Kind).Name+" · "+DistrictStatusText(d),21,Ui.Text,true),
                Ui.Label($"Pole {t.X}, {t.Y} · "+(d.Funding==DistrictFunding.Public?"Fundusze publiczne":"Przywilej cechowy")+$" · utrzymanie {Districts.Upkeep(d):0.##} złota/turę",12,Ui.Muted));
            if(d.Status is DistrictStatus.Building or DistrictStatus.Repairing)Ui.Bar(card,$"Praca {d.Work:0.##}/{Districts.Cost(d):0}",d.Work,Districts.Cost(d));
            if(d.Status is DistrictStatus.Building or DistrictStatus.Repairing)card.Children.Add(Ui.Label(work>0?$"Przy obecnej produkcji: około {Math.Ceiling((Districts.Cost(d)-d.Work)/work):0} tur.":"Bez przydzielonej pracy budowa nie postępuje.",12,Ui.Muted));
            card.Children.Add(Ui.Label(Districts.Block(state,d)??(d.Status==DistrictStatus.Active?DistrictYieldText(Districts.Preview(state,city,d.Tile,d.Kind,d.Funding)):"Premie rozpoczną się po ukończeniu."),12,Ui.Gold));
            int id=d.Id;card.Children.Add(Ui.Button("Pokaż dzielnicę #"+id,()=>{CloseModal();Select(d.Tile);map.Center(d.Tile);}));
            if(d.Status==DistrictStatus.Building)card.Children.Add(Ui.Button("Porzuć budowę #"+id,()=>{var confirm=Ui.Stack(Ui.Label("Porzucić budowę?",29,Ui.Text,true),Ui.Label("Pole zostanie zwolnione. Wydane złoto i zgromadzona praca nie wrócą do miasta.",13,Ui.Muted),Ui.Button("Porzuć bez zwrotu kosztów",()=>DistrictResult(Districts.Cancel(state,id),city)),Ui.Button("Zachowaj budowę",()=>ShowDistrictPlan(city)));Modal(confirm,580);}));
            if(d.Status==DistrictStatus.Pillaged)card.Children.Add(Ui.Button("Odbuduj dzielnicę #"+id,()=>DistrictResult(Districts.Repair(state,id),city)));
            p.Children.Add(Ui.Card(card));
        }
        p.Children.Add(Ui.Eyebrow("NOWA DZIELNICA"));
        foreach(var type in Districts.Types)
        {
            var card=Ui.Stack(Ui.Label(type.Name,22,Ui.Text,true),Ui.Label(type.Description,13,Ui.Muted),Ui.Label($"{type.Cost} produkcji · utrzymanie publiczne {type.Upkeep:0.##} złota/turę · {Crown.Core.Content.Tech(type.Technology).Name}",12,Ui.Gold));
            int count=state.Tiles.Count(t=>Districts.SiteError(state,city,t.Id,type.Kind)==null);var button=Ui.Button("Wybierz miejsce: "+type.Name,()=>BeginDistrictPlacement(city,type.Kind),true);button.IsEnabled=count>0;card.Children.Add(button);
            card.Children.Add(Ui.Label(count>0?$"Dostępne pola: {count}. Lokalizację i premie porównasz na mapie.":!state.Player.Technologies.Contains(type.Technology)?"Najpierw odkryj wymaganą technologię.":Districts.Construction(state,city)!=null?"Najpierw ukończ lub porzuć obecną budowę.":"Ten rodzaj już istnieje, brakuje miejsca w planie lub wolnych pól.",12,Ui.Muted));p.Children.Add(Ui.Card(card));
        }
        p.Children.Add(Ui.Label("Budowa zajmuje produktywne pole w promieniu dwóch pól miasta. Chronione są zasoby, ruiny i istniejące ulepszenia. Drogi mogą współistnieć z dzielnicą. Premie sąsiedztwa zmieniają się wraz z otoczeniem; wzrastają też wraz z rozwojem epok.",12,Ui.Muted));
        p.Children.Add(Ui.Button("Wróć do mapy",CloseModal));Modal(p,850);
    }
    private void BeginDistrictPlacement(City city,DistrictKind kind)
    {
        CloseModal();districtPlacement=(city.Id,kind);map.DistrictSites=state.Tiles.Where(t=>Districts.SiteError(state,city,t.Id,kind)==null).Select(t=>t.Id).ToArray();
        map.DistrictPlanningText=$"{city.Name} · {Districts.Type(kind).Name}\nWybierz zielone pole. Esc anuluje planowanie.";map.Center(city.Tile);map.InvalidateVisual();Toast("Wskaż miejsce dzielnicy na mapie. Zielone obrysy oznaczają dostępne pola.");
    }
    private void CancelDistrictPlacement()
    {districtPlacement=null;if(map!=null){map.DistrictSites=[];map.DistrictPlanningText="";map.InvalidateVisual();}Toast("Planowanie zakończone.");}
    private bool HandleDistrictSelection(int tile)
    {
        if(districtPlacement is not{} plan)return false;var city=state.Cities.FirstOrDefault(c=>c.Id==plan.CityId&&c.Owner==0);
        if(Districts.SiteError(state,city,tile,plan.Kind) is{} error){Toast(error,false);return true;}
        ShowDistrictCharter(city!,tile,plan.Kind);return true;
    }
    private void ShowDistrictCharter(City city,int tile,DistrictKind kind)
    {
        var t=state.Tiles[tile];var type=Districts.Type(kind);var lost=Engine.TileYields(t,state.Player);
        var p=Ui.Stack(Ui.Eyebrow("RADA MIASTA · MISTRZYNI RENA, PLANISTKA"),Ui.Label(type.Name,31,Ui.Text,true),
            Ui.Label($"„To miejsce zmieni {city.Name}. Rzeka, sąsiedzi i dostęp do surowców określą korzyści. Musimy też zdecydować, kto zapłaci i zachowa wpływy.”",16,Ui.Text,true),
            Ui.Label($"Pole {t.X}, {t.Y} · obecny potencjał: {DistrictYieldText(lost)}\nPo wytyczeniu teren nie będzie już uprawiany. Budowa: {type.Cost} produkcji z miejskiego budżetu pracy.",13,Ui.Muted));
        foreach(var funding in Enum.GetValues<DistrictFunding>())
        {
            string label=funding==DistrictFunding.Public?"Fundusze publiczne":"Przywilej cechowy";int price=Districts.SurveyCost(funding);var sample=new District{Kind=kind,Status=DistrictStatus.Active,Funding=funding};
            var card=Ui.Stack(Ui.Label(label,23,Ui.Text,true),Ui.Label($"Przygotowanie: {price} złota · utrzymanie: {Districts.Upkeep(sample):0.##}/turę\nPrzewidywana premia: {DistrictYieldText(Districts.Preview(state,city,tile,kind,funding))}",13,Ui.Gold),
                Ui.Label(funding==DistrictFunding.Public?"Pełny dochód dzielnicy. Lud +2, rody −1.":"Dochód 85% wariantu publicznego, utrzymanie 50%. Lud −2, rody +2. Przywilej pozostaje związany z dzielnicą.",13,Ui.Muted));
            var button=Ui.Button($"Rozpocznij: {label} · {price} złota",()=>{var result=Districts.Begin(state,city.Id,tile,kind,funding);if(result.Success){CloseModal();CancelDistrictPlacement();Do(result);TryAutoSave(false);}else Toast(result.Message,false);},true);button.IsEnabled=state.Player.Gold>=price;card.Children.Add(button);p.Children.Add(Ui.Card(card));
        }
        p.Children.Add(Ui.Button("Zmień miejsce",CloseModal));p.Children.Add(Ui.Button("Anuluj planowanie",()=>{CloseModal();CancelDistrictPlacement();}));Modal(p,820);
    }
    private bool DistrictKnown(District d)=>state.Tiles[d.Tile].Explored&&(state.Cities.Any(c=>c.Id==d.CityId&&c.Owner==0)||Engine.Visible(state).Contains(d.Tile));
    private void DistrictFieldPanel(District d)
    {
        if(!DistrictKnown(d))return;var city=state.Cities.Single(c=>c.Id==d.CityId);var card=Ui.Stack(Ui.Label(Districts.Type(d.Kind).Name,22,Ui.Gold,true),Ui.Label(city.Name+" · "+DistrictStatusText(d),13,Ui.Text),Ui.Label(Districts.Block(state,d)??DistrictYieldText(Districts.Preview(state,city,d.Tile,d.Kind,d.Funding)),12,Ui.Muted));
        if(d.Status is DistrictStatus.Building or DistrictStatus.Repairing)Ui.Bar(card,$"Praca {d.Work:0.##}/{Districts.Cost(d):0}",d.Work,Districts.Cost(d));
        if(city.Owner==0)card.Children.Add(Ui.Button("Zarządzaj dzielnicami: "+city.Name,()=>ShowDistrictPlan(city)));
        foreach(var unit in state.Units.Where(u=>u.Tile==d.Tile&&u.Owner==0&&u.Kind!=UnitKind.Settler))
        {int id=unit.Id;var pillage=Ui.Button("Splądruj dzielnicę #"+id,()=>{var result=Districts.Pillage(state,id);Do(result);if(result.Success)TryAutoSave(false);});pillage.IsEnabled=city.Owner!=0&&state.Relation(0,city.Owner).War&&d.Status==DistrictStatus.Active&&unit.Actions>=1&&unit.EscortCaravanId==null;card.Children.Add(pillage);}
        side.Children.Add(Ui.Card(card));
    }
}
