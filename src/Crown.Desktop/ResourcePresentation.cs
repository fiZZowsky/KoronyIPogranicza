using Crown.Core;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Crown.Desktop;

internal sealed record ResourceMarker(int Tile,Resource Resource);
internal static class ResourcePresentation
{
    internal static string Color(Resource resource)=>resource switch
    {
        Resource.Grain=>"#E2C37A",Resource.Iron=>"#B3C4CB",Resource.Horses=>"#CEA57A",Resource.Stone=>"#C5C4B5",Resource.Salt=>"#E3E9DD",Resource.Dyes=>"#CB9CC3",_=>"#B8C7C2"
    };
    internal static string Description(Resource resource)=>resource switch
    {
        Resource.Grain=>"Zboże wspiera wyżywienie mieszkańców.",
        Resource.Iron=>"Ruda żelaza jest surowcem dla przemysłu, dawnych wojsk i rynku.",
        Resource.Horses=>"Konie są potrzebne do rekrutacji modeli kawalerii konnej.",
        Resource.Stone=>"Kamień wspiera produkcję na wykorzystywanym polu.",
        Resource.Salt=>"Sól jest towarem rynku i zaspokaja potrzeby mieszkańców.",
        Resource.Dyes=>"Rośliny barwierskie reprezentują źródło barwników dla rynku i mieszkańców.",_=>"Na tym polu nie odkryto zasobu."
    };
    internal static string Bonus(Resource resource)
    {
        var faction=new Faction();var baseline=Engine.TileYields(new Tile{Terrain=Terrain.Grassland},faction);var value=Engine.TileYields(new Tile{Terrain=Terrain.Grassland,Resource=resource},faction);var parts=new List<string>();
        if(value.Food>baseline.Food)parts.Add($"+{value.Food-baseline.Food:0.#} żywności");
        if(value.Production>baseline.Production)parts.Add($"+{value.Production-baseline.Production:0.#} produkcji");
        if(value.Gold>baseline.Gold)parts.Add($"+{value.Gold-baseline.Gold:0.#} złota");
        return string.Join(" · ",parts);
    }
    internal static FrameworkElement Icon(Resource resource,double size=22)
    {
        string path=resource switch
        {
            Resource.Grain=>"M12,22 L12,3 M12,8 C5,8 4,4 5,3 C10,3 12,6 12,8 M12,12 C19,12 21,8 20,7 C15,7 12,10 12,12 M12,16 C5,16 3,12 4,11 C9,11 12,14 12,16 M12,20 C19,20 21,16 20,15 C15,15 12,18 12,20",
            Resource.Iron=>"M3,18 L5,8 L13,3 L21,8 L22,18 L15,22 Z M5,8 L12,12 L13,3 M12,12 L15,22 M12,12 L21,8 M3,18 L12,12",
            Resource.Horses=>"M5,22 L7,13 L5,10 L9,5 L10,1 L13,5 L16,3 L17,9 L22,14 L20,17 L15,15 L13,22 Z M13,9 L14,9",
            Resource.Stone=>"M2,20 L3,13 L9,10 L14,14 L13,21 Z M10,9 L12,3 L19,4 L22,10 L17,14 M15,21 L17,15 L22,15 L23,21 Z",
            Resource.Salt=>"M3,20 L3,9 L7,4 L11,9 L11,20 Z M11,20 L11,6 L15,1 L19,6 L19,20 Z M19,20 L19,13 L22,10 L24,13 L24,20 M3,9 L7,12 L11,9 M11,6 L15,9 L19,6 M15,9 L15,20",
            Resource.Dyes=>"M12,22 L12,12 M12,18 Q5,19 5,14 Q10,13 12,18 M12,16 Q19,17 20,12 Q15,12 12,16 M12,11 C5,14 2,8 7,6 C6,0 13,0 14,5 C20,2 24,9 17,11 C17,17 10,17 12,11 Z",
            _=>"M3,3 L21,21 M21,3 L3,21"
        };
        var drawing=new Canvas{Width=26,Height=26};drawing.Children.Add(new System.Windows.Shapes.Path{Data=Geometry.Parse(path),Stroke=Ui.Brush(Color(resource)),StrokeThickness=1.55,StrokeLineJoin=PenLineJoin.Round,StrokeStartLineCap=PenLineCap.Round,StrokeEndLineCap=PenLineCap.Round});
        return new Viewbox{Width=size,Height=size,Child=drawing,Margin=new Thickness(0,0,6,0),IsHitTestVisible=false};
    }
    internal static FrameworkElement Label(Resource resource,double fontSize=11)
    {
        var row=new StackPanel{Orientation=Orientation.Horizontal};row.Children.Add(Icon(resource,18));var text=Ui.Label(Crown.Core.Content.ResourceName(resource),fontSize,Ui.Brush(Color(resource)));text.VerticalAlignment=VerticalAlignment.Center;text.TextWrapping=TextWrapping.NoWrap;row.Children.Add(text);return row;
    }
    internal static string Tooltip(Resource resource)=>Crown.Core.Content.ResourceName(resource)+"\n"+Description(resource)+"\nPremia pola: "+Bonus(resource)+"\nMiasto korzysta z pól faktycznie wykorzystywanych przez mieszkańców.";
}

public sealed partial class MainWindow
{
    private void ShowResourceLegend()
    {
        var panel=Ui.Stack(Ui.Eyebrow("MAPA I GOSPODARKA"),Ui.Label("Zasoby i ich oznaczenia",30,Ui.Text,true),Ui.Label("Pełna nazwa i piktogram oznaczają rodzaj zasobu. Modele na mapie pokazują jego występowanie.",13,Ui.Muted));
        foreach(var resource in Enum.GetValues<Resource>().Where(r=>r!=Resource.None))
        {
            var row=Ui.Stack(ResourcePresentation.Label(resource,17),Ui.Label(ResourcePresentation.Description(resource),12,Ui.Muted),Ui.Label("Premia pola: "+ResourcePresentation.Bonus(resource),12,Ui.Gold));panel.Children.Add(Ui.Card(row));
        }
        panel.Children.Add(Ui.Label("Premia zasobu jest częścią potencjału pola. Dochód miasta zależy od pól faktycznie wykorzystywanych. Żelazo, sól i barwniki mają również osobny rynek towarowy. Legenda opisuje rodzaje zasobów, bez ujawniania ich nieodkrytych położeń.",12,Ui.Muted));
        panel.Children.Add(Ui.Button("Wróć do mapy",CloseModal));Modal(panel,720);
    }
}
