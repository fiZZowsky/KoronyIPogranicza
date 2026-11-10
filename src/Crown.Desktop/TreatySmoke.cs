using Crown.Core;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Crown.Desktop;

public sealed partial class MainWindow
{
    private async Task TreatySmoke(string folder)
    {
        Directory.CreateDirectory(folder);var checks=new List<string>();
        void Check(bool ok,string text){if(!ok)throw new InvalidOperationException(text);checks.Add("PASS "+text);}
        void Click(string text){root.UpdateLayout();var b=Descendants<Button>(overlay??root).First(b=>b.Content?.ToString()==text);if(!b.IsEnabled)throw new InvalidOperationException("Disabled: "+text);b.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));root.UpdateLayout();}
        bool Text(string text){root.UpdateLayout();return Descendants<TextBlock>(overlay!).Any(t=>t.Text.Contains(text,StringComparison.OrdinalIgnoreCase));}
        void Resolve(){if(state.PendingEvent!=null){var e=ProvincialEvents.Current(state);int choice=Enumerable.Range(0,e.Choices.Length).First(i=>state.Player.Gold+e.Choices[i].Gold>=0);var r=Narrative.Resolve(state,choice);if(!r.Success)throw new InvalidOperationException(r.Message);}}
        void Step(){CloseModal();Resolve();Advance();Resolve();CloseModal();root.UpdateLayout();}
        async Task Capture(string name){root.UpdateLayout();await Task.Delay(120);map.RenderGpuFrame();var image=new RenderTargetBitmap((int)root.ActualWidth,(int)root.ActualHeight,96,96,PixelFormats.Pbgra32);image.Render(root);var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(image));using var file=File.Create(Path.Combine(folder,name+".png"));png.Save(file);}
        try
        {
            if(arguments.Contains("--smoke-force-failure"))throw new InvalidOperationException("Intentional treaty diagnostic failure.");
            var s=World.Create();Narrative.Resolve(s,0);s.Units.Clear();foreach(var c in s.Cities){c.ProductionPaused=true;c.Food=500;}
            foreach(var f in s.Factions){f.Gold=100000;f.Research="";}foreach(int owner in new[]{0,1}){s.Factions[owner].Technologies.UnionWith(EpochCatalog.All.Take(5).SelectMany(e=>e.Technologies));for(int i=0;i<5;i++)s.Factions[owner].EpochChoices[i]=0;}
            s.Relation(0,1).Trust=80;s.Relation(0,1).Trade=true;Start(s);root.UpdateLayout();await Task.Delay(150);tab="diplomacy";RefreshSide();Click("Traktaty i gwarancje");
            Check(Text("UMOWY, ZABEZPIECZENIA")&&Text("Brak nowych traktatów"),"The real diplomacy entry opens the new treaty registry");string before=SaveGame.Serialize(state);ShowTreaties();Check(SaveGame.Serialize(state)==before,"Browsing treaty history preserves campaign and RNG");
            Click("Zaproponuj traktat: "+state.Factions[1].Name);Check(Text("Współpraca badawcza")&&Text("Gwarancja pokoju")&&Text("depozyt po 80"),"The diplomatic conversation exposes both agreements and real securities");Click("Wróć bez wysyłania");Check(SaveGame.Serialize(state)==before,"Leaving the proposal conversation costs nothing");
            Click("Zaproponuj traktat: "+state.Factions[1].Name);double gold=state.Player.Gold;Click("Wyślij: Współpraca badawcza · 12 tur · 10 złota");var treaty=state.Treaties.Single();
            Check(state.Player.Gold==gold-10&&treaty.Status==TreatyStatus.Travelling&&treaty.EscrowFrom==0,"The actual send button pays only embassy costs");Step();Check(treaty.Status==TreatyStatus.Travelling,"The first ordinary turn cannot deliver an instant treaty reply");Step();ShowTreaties();
            Check(treaty.Status==TreatyStatus.AwaitingPlayer&&treaty.SignedTurn==null,"The reply arrives after two real turns without automatic signature");Click("Przeczytaj warunki #"+treaty.Id);Check(Text("RATYFIKACJA")&&Text("Depozyt: po 50")&&Text("2 złota"),"The ratification dialogue shows bonds recurring costs and liability");before=SaveGame.Serialize(state);Click("Zdecyduję później");Check(SaveGame.Serialize(state)==before,"Deferring ratification preserves both treasuries");
            Click("Przeczytaj warunki #"+treaty.Id);gold=state.Player.Gold;double foreign=state.Factions[1].Gold;Click("Ratyfikuj traktat #"+treaty.Id);
            Check(treaty.Status==TreatyStatus.Active&&state.Player.Gold==gold-50&&state.Factions[1].Gold==foreign-50&&treaty.EscrowFrom+treaty.EscrowTo==100,"The real signature locks both deposits atomically");Step();ShowTreaties();
            Check(treaty.FundedPhases==1&&treaty.LastFee==2&&treaty.LastScience>0&&Text("rzeczywista nauka stron"),"An ordinary funded phase produces auditable scientific cooperation");ShowTradePolicy();Check(Text("Zerwanie")&&Text("50 złota"),"The actual embargo screen warns about forfeiting research security");CloseModal();ShowTreaties();await Capture("treaty-research");
            before=SaveGame.Serialize(state);string path=Path.Combine(folder,"treaty-save.crown");SaveGame.Write(state,path);LoadPath(path);CloseModal();root.UpdateLayout();await Task.Delay(120);treaty=state.Treaties.Single();
            Check(state.Version==SaveGame.CurrentVersion&&SaveGame.Serialize(state)==before,"Desktop current-format reload preserves signatures bonds and phase evidence");
            while(state.Turn<treaty.ExpiresTurn)Step();ShowTreaties();Check(treaty.Status==TreatyStatus.Completed&&treaty.FundedPhases==12&&treaty.PayoutFrom==50&&treaty.PayoutTo==50&&Text("Dotrzymany"),"Twelve actual economic phases complete the treaty and refund both securities");
            gold=state.Player.Gold;Check(Treaties.Propose(state,0,TreatyKind.PeaceGuarantee,24,1).Success&&state.Player.Gold==gold,"An AI-originated proposal pays its own embassy without charging the player");var guarantee=state.Treaties.Last();Step();Step();ShowTreaties();Click("Przeczytaj warunki #"+guarantee.Id);
            Check(guarantee.Status==TreatyStatus.AwaitingPlayer&&Text("po 80")&&Text("nie blokuje przycisku wojny"),"The incoming guarantee awaits an informed player decision");Click("Ratyfikuj traktat #"+guarantee.Id);Check(guarantee.EscrowFrom==80&&guarantee.EscrowTo==80,"Explicitly signing the incoming guarantee locks actual security");Step();ShowTreaties();
            before=SaveGame.Serialize(state);Click("Rozważ zerwanie #"+guarantee.Id);Check(Text("stabilność -6")&&Text("80 złota"),"The withdrawal confirmation exposes the financial and political costs");Click("Dotrzymamy umowy");Check(SaveGame.Serialize(state)==before,"Cancelling withdrawal preserves the entire campaign");
            ConfirmWar(state.Factions[1]);Check(Text("Zerwanie")&&Text("80 złota"),"The existing war dialogue includes the current guarantee liability");foreign=state.Factions[1].Gold;Click("Wypowiedz wojnę: Wojna podboju");ShowTreaties();
            Check(state.Relation(0,1).War&&guarantee.Status==TreatyStatus.Broken&&guarantee.BreachedBy==0&&state.Factions[1].Gold==foreign+160,"The real declaration of war compensates the guaranteed victim exactly once");await Capture("treaty-breach");
            Width=1200;Height=800;ShowTreaties();root.UpdateLayout();foreach(var scroll in Descendants<ScrollViewer>(overlay!))scroll.ScrollToBottom();await Capture("treaty-compact");var back=Descendants<Button>(overlay!).Single(b=>b.Content?.ToString()=="Wróć do mapy");var bounds=back.TransformToAncestor(root).TransformBounds(new Rect(new Point(),back.RenderSize));
            Check(back.IsVisible&&bounds.Top>=0&&bounds.Bottom<=root.ActualHeight,"The complete treaty ledger and exit remain reachable in a compact window");
            checks.Add($"{checks.Count} treaty desktop integration checks passed.");File.WriteAllLines(Path.Combine(folder,"ui-smoke.txt"),checks);Close();
        }
        catch(Exception e){checks.Add(e.ToString());File.WriteAllLines(Path.Combine(folder,"ui-smoke.txt"),checks);Application.Current.Shutdown(3);}
    }
}
