using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Crown.Desktop;

internal static class OrganicMaterials
{
    private static readonly Lazy<ImageBrush[]> atlas=new(()=>
    {
        var image=BitmapFrame.Create(new Uri("pack://application:,,,/Assets/organic-materials-0.37.png"),BitmapCreateOptions.PreservePixelFormat,BitmapCacheOption.OnLoad);
        int w=image.PixelWidth/2,h=image.PixelHeight/2;
        return Enumerable.Range(0,4).Select(i=>
        {
            var crop=new CroppedBitmap(image,new Int32Rect(i%2*w,i/2*h,w,h));crop.Freeze();
            var brush=new ImageBrush(crop){TileMode=TileMode.Tile,Viewport=new Rect(0,0,1,1)};brush.Freeze();return brush;
        }).ToArray();
    });
    internal static int Index(string key)=>key.StartsWith("skin")?0:key.StartsWith("horse-coat")?1:key.StartsWith("uniform-")||key.StartsWith("cloak-fabric")?2:key.StartsWith("saddle-leather")||key.StartsWith("boots")?3:-1;
    internal static ImageBrush Material(int i)=>atlas.Value[i];
    private static readonly Lazy<ImageBrush> wheat=new(()=>
    {
        var image=BitmapFrame.Create(new Uri("pack://application:,,,/Assets/wheat-tuft-0.37.png"));image.Freeze();var brush=new ImageBrush(image);brush.Freeze();return brush;
    });
    internal static ImageBrush Wheat=>wheat.Value;
}
