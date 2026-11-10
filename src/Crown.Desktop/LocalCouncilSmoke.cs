using Crown.Core;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Crown.Desktop;

public sealed partial class MainWindow
{
    private async Task LocalCouncilSmoke(string folder)
    {
        Directory.CreateDirectory(folder);var checks=new List<string>();
        void Check(bool ok,string text){if(!ok)throw new InvalidOperationException(text);checks.Add("PASS "+text);}
        void Click(string text){root.UpdateLayout();var b=Descendants<Button>(overlay??root).First(b=>b.Content?.ToString()==text);if(!b.IsEnabled)throw new InvalidOperationException("Disabled: "+text);b.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));root.UpdateLayout();}
        bool Text(string text){root.UpdateLayout();return Descendants<TextBlock>(overlay??root).Any(t=>t.Text.Contains(text,StringComparison.OrdinalIgnoreCase));}
        void Resolve(){if(state.PendingEvent!=null){var e=ProvincialEvents.Current(state);int i=Enumerable.Range(0,e.Choices.Length).First(i=>state.Player.Gold+e.Choices[i].Gold>=0);var r=Narrative.Resolve(state,i);if(!r.Success)throw new InvalidOperationException(r.Message);}}
        void Step(){CloseModal();Resolve();Advance();Resolve();CloseModal();root.UpdateLayout();}
        async Task Capture(string name){root.UpdateLayout();await Task.Delay(120);map.RenderGpuFrame();var image=new RenderTargetBitmap((int)root.ActualWidth,(int)root.ActualHeight,96,96,PixelFormats.Pbgra32);image.Render(root);var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(image));using var file=File.Create(Path.Combine(folder,name+".png"));png.Save(file);}
        try
        {
            if(arguments.Contains("--smoke-force-failure"))throw new InvalidOperationException("Intentional local council diagnostic failure.");
            var s=World.Create();Narrative.Resolve(s,0);s.Player.Gold=10000;s.ContinueAfterVictory=true;s.Player.Research="";foreach(var c in s.Cities){c.Food=500;c.ProductionPaused=true;}Start(s);root.UpdateLayout();await Task.Delay(140);
            Check(map.Photographic!=null||arguments.Contains("--no-gpu"),"The actual desktop starts with the selected renderer");
            var city=state.Cities[0];ShowProvince(city);Check(Text("Rada i stronnictwa prowincji"),"The province screen provides the new council entry point");Click("Rada i stronnictwa prowincji");
            Check(Text("Rodziny")&&Text("Cechy")&&Text("Miejscowe rody"),"Three local constituencies expose their real support");
            Check(Text("cztery udane")&&Text("30 złota")&&Text("niepokoje +8"),"The council explains delivery costs and political failure before signing");
            string before=SaveGame.Serialize(state);Click("Uzgodnij: "+LocalCouncils.Name(LocalMandate.Granaries));Check(Text("12 żywności")&&Text("Podpisz lokalną umowę"),"The actual dialogue exposes the food conditions and final signature");Click("Wróć do rozmów");Check(SaveGame.Serialize(state)==before,"Leaving the conversation preserves money politics and chronology");
            Click("Uzgodnij: "+LocalCouncils.Name(LocalMandate.Granaries));double gold=state.Player.Gold;Click("Podpisz lokalną umowę");
            Check(city.Council.Mandate==LocalMandate.Granaries&&state.Player.Gold==gold-30,"The actual signing button starts the mandate and pays once");
            Check(Text("Rodziny · 58%")&&Text("Miejscowe rody · 45%"),"Competing groups react immediately to the decision");
            Check(Text(CampaignCalendar.At(state,1))&&!Text("T1"),"The signed decision is shown with its captured historical year");await Capture("local-council-signed");
            Step();ShowLocalCouncil(city);Check(city.Council.Phases==1&&city.Council.Delivered==1&&city.Council.Paid==city.Council.PhaseFee,"An ordinary economic turn fulfils and funds the first phase");
            before=SaveGame.Serialize(state);string path=Path.Combine(folder,"local-council.crown");SaveGame.Write(state,path);LoadPath(path);CloseModal();city=state.Cities[0];Check(SaveGame.Serialize(state)==before,"Desktop reload preserves exact calendar support and paid phase evidence");
            for(int i=0;i<5;i++)Step();ShowLocalCouncil(city);
            Check(city.Council.Mandate==LocalMandate.None&&city.Council.Charters.Contains(LocalMandate.Granaries)&&city.Council.Delivered>=4,"Six actual economic phases establish the permanent food charter");
            Check(Text("USTANOWIONA")&&Text("Trwała karta ustanowiona"),"The final council ledger explains the durable outcome");
            Check(LocalCouncils.Bonus(city).Food==2&&LocalCouncils.Loyalty(city)>0,"The delivered charter affects city yields and political loyalty");await Capture("local-council-completed");
            CloseModal();tab="history";RefreshSide();root.UpdateLayout();Check(Descendants<TextBlock>(side).Any(t=>t.Text.Contains(CampaignCalendar.At(state,1),StringComparison.OrdinalIgnoreCase)&&t.Text.Contains("decyzja",StringComparison.OrdinalIgnoreCase)),"The actual chronicle dates the original decision by its recorded year");
            Check(!Descendants<TextBlock>(side).Any(t=>System.Text.RegularExpressions.Regex.IsMatch(t.Text,@"\bT\d+\b|TURA \d")),"The chronicle contains no abbreviated turn labels");
            Width=1200;Height=800;ShowLocalCouncil(city);root.UpdateLayout();foreach(var scroll in Descendants<ScrollViewer>(overlay!))scroll.ScrollToBottom();await Capture("local-council-compact");var back=Descendants<Button>(overlay!).Single(b=>b.Content?.ToString()=="Wróć do prowincji");var bounds=back.TransformToAncestor(root).TransformBounds(new Rect(new Point(),back.RenderSize));
            Check(back.IsVisible&&bounds.Top>=0&&bounds.Bottom<=root.ActualHeight,"All local choices and the exit remain reachable in a compact window");
            Click("Wróć do prowincji");Check(Text(city.Name)&&Text("Rada i stronnictwa prowincji"),"Leaving the council returns to the selected province");
            checks.Add($"{checks.Count} calendar and local council desktop integration checks passed.");File.WriteAllLines(Path.Combine(folder,"ui-smoke.txt"),checks);Close();
        }
        catch(Exception e){checks.Add(e.ToString());if(map?.GpuFailure!=null)checks.Add(map.GpuFailure);File.WriteAllLines(Path.Combine(folder,"ui-smoke.txt"),checks);Application.Current.Shutdown(3);}
    }
}
