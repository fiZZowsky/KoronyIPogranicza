using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace Crown.Desktop;

public sealed class App : Application
{
    [STAThread]
    public static int Main(string[] args)
    {
        var app=new App();app.DispatcherUnhandledException+=(o,e)=>
        {
            if(args.Any(a=>a.StartsWith("--render"))||args.Contains("--ui-smoke")){File.WriteAllText(args.Last()+".error.txt",e.Exception.ToString());e.Handled=true;app.Shutdown(2);return;}
            MessageBox.Show("Nie udało się wykonać operacji.\n"+e.Exception.Message,"Korony i Pogranicza",MessageBoxButton.OK,MessageBoxImage.Warning);e.Handled=true;
        };
        return app.Run(new MainWindow(args));
    }
}
