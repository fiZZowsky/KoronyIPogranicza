using Crown.Core;
using System.Windows.Controls;

namespace Crown.Desktop;

public sealed partial class MainWindow
{
    private void ShowEraEndeavours(int? selected=null,string? notice=null)
    {
        var f=state.Player;int era=selected??(f.ActiveEndeavour>=0?f.ActiveEndeavour:f.Era);var task=EraEndeavours.All[era];
        var p=Ui.Stack(Ui.Eyebrow("CYWILIZACJA Z WŁASNĄ HISTORIĄ"),Ui.Label("Przedsięwzięcia i dziedzictwo epok",29,Ui.Text,true),
            Ui.Label("Każda epoka stawia inne wymagania. Jedno przedsięwzięcie naraz; dopiero kolejne opłacone fazy utrzymania wszystkich warunków ustanawiają trwałe dziedzictwo. Niedobór lub brak środków zeruje ciągłość. Dawne epoki można nadrobić, ale bez zaliczania tur wstecz.",14,Ui.Muted));
        if(notice!=null)p.Children.Add(Ui.Label(notice,14,Ui.Gold));
        var choose=Ui.Combo(EpochCatalog.All.Select(e=>$"{e.Index+1:00} · {e.Name}"+(f.EraLegacies.Contains(e.Index)?" · dziedzictwo":"")).ToArray(),era);
        choose.SelectionChanged+=(_,_)=>ShowEraEndeavours(choose.SelectedIndex);p.Children.Add(choose);
        p.Children.Add(Ui.Label(task.Name,26,Ui.Text,true));p.Children.Add(Ui.Label(task.Task,15,Ui.Gold));
        p.Children.Add(Ui.Label("Wygląd miast: "+WorldScene3D.ArchitectureNames[era]+". Zabudowa pokazuje styl, a rzeczywiste budynki nadal wymagają budowy.",13,Ui.Muted));
        foreach(var r in EraEndeavours.Requirements(state,0,era))p.Children.Add(Ui.Label((r.Met?"Spełniono: ":"Brakuje: ")+r.Text,14,r.Met?Ui.Green:Ui.Red));
        p.Children.Add(Ui.Label("Trwały efekt: "+task.Legacy,16,Ui.Gold));
        if(f.ActiveEndeavour>=0)
        {
            p.Children.Add(Ui.Label($"Aktywny plan: {EraEndeavours.All[f.ActiveEndeavour].Name}\n{EraEndeavours.PaceName(f.EndeavourPace)} · ciągłość {f.EndeavourProgress}/{EraEndeavours.Required(f.EndeavourPace)} · {EraEndeavours.Fee(f.ActiveEndeavour,f.EndeavourPace)} złota/fazę",16,Ui.Text));
            p.Children.Add(Ui.Label(f.EndeavourReport,13,Ui.Muted));var cancel=Ui.Button("Odwołaj przedsięwzięcie",()=>{var r=EraEndeavours.Cancel(state);Do(r);if(r.Success)TryAutoSave(false);ShowEraEndeavours(era,r.Message);});cancel.IsEnabled=state.Ending==null&&f.LastEndeavourDecisionTurn!=state.Turn;p.Children.Add(cancel);
        }
        else if(f.EraLegacies.Contains(era))p.Children.Add(Ui.Label("Dziedzictwo ustanowione. Premie wymagające budynków ustają po ich utracie lub wyłączeniu.",14,Ui.Green));
        else p.Children.Add(Ui.Button("Porozmawiaj z radą o przedsięwzięciu",()=>ShowEndeavourCouncil(era),true));
        p.Children.Add(Ui.Label("Bilans złota uwzględnia planowaną opłatę. Koszt jest pobierany po gospodarce, również gdy warunki nie są spełnione. Brak środków nie tworzy długu. Zmiana planu raz na turę; odwołanie nie zwraca kosztów. Przejście do kolejnej epoki zachowuje rozpoczęty plan.",13,Ui.Muted));
        if(f.EraLegacies.Count>0)p.Children.Add(Ui.Label("Ustanowione dziedzictwa: "+string.Join(" · ",f.EraLegacies.Order().Select(e=>EraEndeavours.All[e].Name)),13,Ui.Green));
        p.Children.Add(Ui.Button("Wróć do epok",()=>ShowEpochs(era)));p.Children.Add(Ui.Button("Wróć do mapy",CloseModal));Modal(p,940);
    }
    private void ShowEndeavourCouncil(int era)
    {
        var task=EraEndeavours.All[era];var p=Ui.Stack(Ui.Eyebrow("ROZMOWA O DZIEDZICTWIE"),Ui.Label(task.Name,28,Ui.Text,true),
            Ui.Label("„Rozwój poznaje się po tym, co potrafimy utrzymać. Możemy działać stopniowo albo zapłacić więcej za intensywną koordynację. Nie obiecujmy samych nazw — potrzebne są rzeczywiste warunki.”",16,Ui.Text),
            Ui.Label(task.Task+"\nEfekt: "+task.Legacy,14,Ui.Gold));
        foreach(int mode in new[]{0,1})
        {
            int pace=mode;var error=EraEndeavours.StartError(state,era,pace);p.Children.Add(Ui.Label($"{EraEndeavours.PaceName(pace)} · rozpoczęcie {EraEndeavours.StartCost(era)} złota · {EraEndeavours.Fee(era,pace)} złota/fazę",14,Ui.Text));
            var b=Ui.Button(pace==0?"Przyjmij plan stopniowy":"Przyjmij plan intensywny",()=>{var r=EraEndeavours.Start(state,era,pace);Do(r);if(r.Success)TryAutoSave(false);ShowEraEndeavours(era,r.Message);},true);b.IsEnabled=error==null;p.Children.Add(b);if(error!=null)p.Children.Add(Ui.Label(error,13,Ui.Red));
        }
        p.Children.Add(Ui.Button("Wróć bez decyzji",()=>ShowEraEndeavours(era)));Modal(p,820);
    }
}
