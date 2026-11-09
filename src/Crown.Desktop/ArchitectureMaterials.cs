using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Crown.Desktop;

internal static class ArchitectureMaterials
{
    private static readonly Lazy<ImageBrush[]> facades=new(()=>
    {
        var atlas=BitmapFrame.Create(new Uri("pack://application:,,,/Assets/building-facades-0.35.png"),BitmapCreateOptions.PreservePixelFormat,BitmapCacheOption.OnLoad);
        int w=atlas.PixelWidth/2,h=atlas.PixelHeight/2;
        return Enumerable.Range(0,4).Select(i=>
        {
            var crop=new CroppedBitmap(atlas,new Int32Rect((i%2)*w,(i/2)*h,w,h));crop.Freeze();
            var brush=new ImageBrush(crop){Stretch=Stretch.Fill,TileMode=TileMode.None};brush.Freeze();return brush;
        }).ToArray();
    });
    internal static ImageBrush Facade(int index)=>facades.Value[Math.Clamp(index,0,3)];
}
