using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Crown.Desktop;

internal static class UnderstoryMaterials
{
    private static readonly Lazy<BitmapSource> image=new(()=>
    {
        var value=BitmapFrame.Create(new Uri("pack://application:,,,/Assets/understory-atlas-0.41.png"),BitmapCreateOptions.PreservePixelFormat,BitmapCacheOption.OnLoad);value.Freeze();return value;
    });
    private static readonly Lazy<ImageBrush[]> brushes=new(()=>Enumerable.Range(0,4).Select(i=>
    {
        var atlas=Image;int w=atlas.PixelWidth/2,h=atlas.PixelHeight/2;
        var crop=new CroppedBitmap(atlas,new Int32Rect(i%2*w,i/2*h,w,h));crop.Freeze();
        var brush=new ImageBrush(crop){Stretch=Stretch.Fill,TileMode=TileMode.None};brush.Freeze();return brush;
    }).ToArray());
    internal static BitmapSource Image=>image.Value;
    internal static ImageBrush Brush(int index)=>brushes.Value[index];
    internal static int Index(string key)=>key.StartsWith("ground-grass-textured")?0:key.StartsWith("wild-understory")?1:key.StartsWith("river-reeds-textured")?2:key.StartsWith("wild-herbs-textured")?3:-1;
}
