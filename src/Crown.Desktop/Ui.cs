using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Crown.Desktop;

internal static class Ui
{
    public static readonly Brush Ink=Brush("#101E23"),Panel=Brush("#15272C"),PanelLight=Brush("#1C3338"),Gold=Brush("#D1B273"),Text=Brush("#E8E5D9"),Muted=Brush("#99A9AA"),Line=Brush("#314348"),Green=Brush("#91BDA4"),Red=Brush("#D08B77");
    public static Brush Brush(string color) {var b=(SolidColorBrush)new BrushConverter().ConvertFromString(color)!;b.Freeze();return b;}
    public static TextBlock Label(string text,double size=13,Brush? color=null,bool serif=false)
        =>new(){Text=text,FontSize=size,Foreground=color??Text,TextWrapping=TextWrapping.Wrap,FontFamily=new FontFamily(serif?"Georgia":"Segoe UI"),LineHeight=size*1.5};
    public static TextBlock Eyebrow(string text)=>new(){Text=text.ToUpperInvariant(),FontSize=10,Foreground=Gold,FontWeight=FontWeights.SemiBold,Margin=new Thickness(0,0,0,12)};
    public static Button Button(string text,Action action,bool primary=false)
    {
        var b=new Button {Content=text,FontSize=12,FontFamily=new FontFamily("Segoe UI"),Foreground=primary?Ink:Text,Background=primary?Gold:PanelLight,BorderBrush=primary?Gold:Line,BorderThickness=new Thickness(1),Padding=new Thickness(14,10,14,10),Cursor=System.Windows.Input.Cursors.Hand,Margin=new Thickness(0,0,0,8),HorizontalContentAlignment=HorizontalAlignment.Left};
        var template=new ControlTemplate(typeof(Button));var border=new FrameworkElementFactory(typeof(Border));border.SetBinding(Border.BackgroundProperty,new System.Windows.Data.Binding("Background"){RelativeSource=new(System.Windows.Data.RelativeSourceMode.TemplatedParent)});border.SetBinding(Border.BorderBrushProperty,new System.Windows.Data.Binding("BorderBrush"){RelativeSource=new(System.Windows.Data.RelativeSourceMode.TemplatedParent)});border.SetValue(Border.BorderThicknessProperty,new Thickness(1));border.SetValue(Border.CornerRadiusProperty,new CornerRadius(3));var cp=new FrameworkElementFactory(typeof(ContentPresenter));cp.SetBinding(FrameworkElement.MarginProperty,new System.Windows.Data.Binding("Padding"){RelativeSource=new(System.Windows.Data.RelativeSourceMode.TemplatedParent)});cp.SetValue(ContentPresenter.RecognizesAccessKeyProperty,false);border.AppendChild(cp);template.VisualTree=border;
        var hover=new Trigger{Property=UIElement.IsMouseOverProperty,Value=true};hover.Setters.Add(new Setter(UIElement.OpacityProperty,.8));template.Triggers.Add(hover);var disabled=new Trigger{Property=UIElement.IsEnabledProperty,Value=false};disabled.Setters.Add(new Setter(UIElement.OpacityProperty,.38));template.Triggers.Add(disabled);b.Template=template;b.Click+=(o,e)=>action();return b;
    }
    public static Border Card(UIElement child,Thickness? padding=null)=>new(){Child=child,Background=PanelLight,BorderBrush=Line,BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(3),Padding=padding??new Thickness(14),Margin=new Thickness(0,0,0,12)};
    public static Border Rule()=>new(){Height=1,Background=Line,Margin=new Thickness(0,15,0,15)};
    public static StackPanel Stack(params UIElement[] children) {var p=new StackPanel();foreach(var c in children)p.Children.Add(c);return p;}
    public static void Gap(Panel panel,double height=12)=>panel.Children.Add(new Border{Height=height});
    public static void Bar(Panel panel,string label,double value,double max,Brush? color=null)
    {
        panel.Children.Add(Label(label,12,Muted));var grid=new Grid{Height=4,Background=Line,Margin=new Thickness(0,8,0,14)};
        grid.ColumnDefinitions.Add(new(){Width=new GridLength(Math.Max(.0001,Math.Min(value/max,1)),GridUnitType.Star)});grid.ColumnDefinitions.Add(new(){Width=new GridLength(Math.Max(.0001,1-Math.Min(value/max,1)),GridUnitType.Star)});
        grid.Children.Add(new Border{Background=color??Gold});panel.Children.Add(grid);
    }
    public static ComboBox Combo(string[] items,int selected)
    {
        var c=new ComboBox{ItemsSource=items,SelectedIndex=selected,Padding=new Thickness(9),Margin=new Thickness(0,6,0,14),FontSize=13,Foreground=Ink,MinHeight=34};return c;
    }
    public static string Number(double n)=>n.ToString("0",CultureInfo.InvariantCulture);
    public static string Signed(double n)=>(n>=0?"+":"")+n.ToString("0.0",CultureInfo.InvariantCulture);
}
