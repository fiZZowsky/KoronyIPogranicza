using System.IO;
using System.Windows.Media;

namespace Crown.Desktop;

internal sealed class InterfaceAudio : IDisposable
{
    private readonly MediaPlayer player=new();
    private readonly bool silent;
    private readonly string path;
    private bool opened;
    private long lastPlay;
    internal string? Failure {get;private set;}
    internal double EffectiveVolume {get;private set;}
    internal InterfaceAudio(string folder,bool silent)
    {
        this.silent=silent;path=Path.Combine(folder,"interface-tone.wav");player.MediaFailed+=(_,e)=>Failure=e.ErrorException.Message;
    }
    internal static byte[] Wave()
    {
        const int rate=22050,count=2205;using var stream=new MemoryStream();using var writer=new BinaryWriter(stream);
        writer.Write("RIFF"u8);writer.Write(36+count*2);writer.Write("WAVEfmt "u8);writer.Write(16);writer.Write((short)1);writer.Write((short)1);writer.Write(rate);writer.Write(rate*2);writer.Write((short)2);writer.Write((short)16);writer.Write("data"u8);writer.Write(count*2);
        for(int i=0;i<count;i++){double t=i/(double)rate,envelope=Math.Sin(Math.PI*i/count)*Math.Exp(-t*24);writer.Write((short)(Math.Sin(2*Math.PI*660*t)*envelope*7000));}return stream.ToArray();
    }
    internal void Configure(GamePreferences p){EffectiveVolume=p.Muted?0:p.MasterVolume*p.InterfaceVolume;player.Volume=EffectiveVolume;}
    internal void Play(GamePreferences p,bool preview=false)
    {
        Configure(p);if(silent||EffectiveVolume<=0||!preview&&Environment.TickCount64-lastPlay<70)return;
        lastPlay=Environment.TickCount64;
        try
        {
            if(!opened){Directory.CreateDirectory(Path.GetDirectoryName(path)!);if(!File.Exists(path))File.WriteAllBytes(path,Wave());player.Open(new Uri(path));opened=true;}
            player.Position=TimeSpan.Zero;player.Play();
        }
        catch(Exception ex)when(ex is IOException or UnauthorizedAccessException or InvalidOperationException or NotSupportedException){Failure=ex.Message;}
    }
    public void Dispose()=>player.Close();
}
