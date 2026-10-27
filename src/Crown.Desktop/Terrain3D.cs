using Crown.Core;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Media.Imaging;

namespace Crown.Desktop;

internal static class Terrain3D
{
    public const double Radius = 1, SeaLevel = .045;
    public static Point Center(Tile tile) => new(Math.Sqrt(3) * (tile.X + (tile.Y & 1) * .5) + 1, tile.Y * 1.5 + 1);
    public static int TileAt(GameState s, double x, double z)
    {
        int row = (int)Math.Round((z - 1) / 1.5), best = -1; double distance = double.MaxValue;
        for (int y = row - 1; y <= row + 1; y++)
        {
            if (y < 0 || y >= s.Height) continue;
            int column = (int)Math.Round((x - 1) / Math.Sqrt(3) - (y & 1) * .5);
            for (int xx = column - 1; xx <= column + 1; xx++)
            {
                if (xx < 0 || xx >= s.Width) continue;
                int id = y * s.Width + xx; var p = Center(s.Tiles[id]); double d = (p.X - x) * (p.X - x) + (p.Y - z) * (p.Y - z);
                if (d < distance) { best = id; distance = d; }
            }
        }
        return distance <= 1.01 ? best : -1;
    }
    private static double Hash(int x, int z, int seed)
    {
        uint value = unchecked((uint)(x * 374761393 + z * 668265263 + seed * 1447));
        value = (value ^ (value >> 13)) * 1274126177; return (value ^ (value >> 16)) / (double)uint.MaxValue;
    }
    public static double Noise(double x, double z, int seed)
    {
        int ix = (int)Math.Floor(x), iz = (int)Math.Floor(z); double fx = x - ix, fz = z - iz;
        fx = fx * fx * (3 - 2 * fx); fz = fz * fz * (3 - 2 * fz);
        return (Hash(ix, iz, seed) * (1 - fx) + Hash(ix + 1, iz, seed) * fx) * (1 - fz) +
               (Hash(ix, iz + 1, seed) * (1 - fx) + Hash(ix + 1, iz + 1, seed) * fx) * fz;
    }
    public static double Height(GameState s, double x, double z) => TerrainField.Current(s).Sample(x,z);
    public static Point3D Ground(GameState s, double x, double z, double lift = 0) => new(x, Height(s, x, z) + lift, z);
    public static Vector3D Normal(GameState s, double x, double z)
    {
        const double d = .035;
        var n = new Vector3D(Height(s, x - d, z) - Height(s, x + d, z), 2 * d, Height(s, x, z - d) - Height(s, x, z + d)); n.Normalize(); return n;
    }
    public static Color Color(Terrain terrain) => (Color)ColorConverter.ConvertFromString(terrain switch
    {
        Terrain.Grassland => "#82905B", Terrain.Forest => "#4B6842", Terrain.Hills => "#93936F", Terrain.Mountain => "#969A91",
        Terrain.Desert => "#CDBB88", Terrain.Marsh => "#6D8768", Terrain.Tundra => "#B3BBAC", Terrain.Coast => "#8A9680", _ => "#465F65"
    });
    public static BitmapSource Texture(Terrain terrain)
    {
        const int size = 96; var pixels = new byte[size * size * 4]; var color = Color(terrain);
        for (int z = 0; z < size; z++) for (int x = 0; x < size; x++)
        {
            // Periodic coordinates keep material repetitions free of hard texture seams.
            double wave = Math.Sin(x * Math.PI * 2 / size) * Math.Cos(z * Math.PI * 4 / size);
            double detail = Hash(x, z, (int)terrain + 4021) - .5; double shade = .92 + wave * .055 + detail * .16;
            int at = (z * size + x) * 4;
            pixels[at] = (byte)Math.Clamp(color.B * shade, 0, 255); pixels[at + 1] = (byte)Math.Clamp(color.G * shade, 0, 255); pixels[at + 2] = (byte)Math.Clamp(color.R * shade, 0, 255); pixels[at + 3] = 255;
        }
        var result = BitmapSource.Create(size, size, 96, 96, PixelFormats.Bgra32, null, pixels, size * 4); result.Freeze(); return result;
    }
    public static BitmapSource Atlas(GameState state)
    {
        const int width = 1024, height = 640; var pixels = new byte[width * height * 4];
        double worldWidth = state.Width * Math.Sqrt(3) + 2, worldHeight = state.Height * 1.5 + 2;
        var colors = Enum.GetValues<Terrain>().ToDictionary(t => t, Color);
        for (int iz = 0; iz < height; iz++) for (int ix = 0; ix < width; ix++)
        {
            double x = ix * worldWidth / (width - 1), z = iz * worldHeight / (height - 1); int row = (int)Math.Round((z - 1) / 1.5);
            double red = 0, green = 0, blue = 0, weight = 0;
            for (int y = row - 1; y <= row + 1; y++)
            {
                if (y < 0 || y >= state.Height) continue;
                int column = (int)Math.Round((x - 1) / Math.Sqrt(3) - (y & 1) * .5);
                for (int xx = column - 1; xx <= column + 1; xx++)
                {
                    if (xx < 0 || xx >= state.Width) continue;
                    var tile = state.Tiles[y * state.Width + xx]; var center = Center(tile); double dx = center.X - x, dz = center.Y - z;
                    double w = Math.Max(0, 1 - (dx * dx + dz * dz) / 2.7); w *= w;
                    var c = tile.Explored ? colors[tile.Terrain] : System.Windows.Media.Color.FromRgb(105, 125, 126);
                    red += c.R * w; green += c.G * w; blue += c.B * w; weight += w;
                }
            }
            if (weight == 0) weight = 1;
            double grain = .89 + Noise(x * 4.7, z * 4.7, state.Seed + 71) * .15 + (Hash(ix, iz, state.Seed) - .5) * .07;
            double altitude = Height(state, x, z), snow = Math.Clamp((altitude - 3.1) * .7, 0, 1);
            int at = (iz * width + ix) * 4;
            pixels[at] = (byte)Math.Clamp((blue / weight * (1 - snow) + 220 * snow) * grain, 0, 255);
            pixels[at + 1] = (byte)Math.Clamp((green / weight * (1 - snow) + 225 * snow) * grain, 0, 255);
            pixels[at + 2] = (byte)Math.Clamp((red / weight * (1 - snow) + 222 * snow) * grain, 0, 255); pixels[at + 3] = 255;
        }
        var result = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, pixels, width * 4); result.Freeze(); return result;
    }
}
