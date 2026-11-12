using System.IO;
using System.Text;
using System.Windows.Media.Imaging;

namespace Crown.Desktop;

internal static class MotionPreview
{
    internal static void Save(string path,IReadOnlyList<BitmapSource> frames)
    {
        var encoder=new GifBitmapEncoder();foreach(var frame in frames)encoder.Frames.Add(BitmapFrame.Create(frame));
        using var raw=new MemoryStream();encoder.Save(raw);byte[] bytes=raw.ToArray();
        using var output=new MemoryStream();int header=13+((bytes[10]&128)!=0?3*(1<<((bytes[10]&7)+1)):0);output.Write(bytes,0,header);
        output.Write([0x21,0xff,11]);output.Write(Encoding.ASCII.GetBytes("NETSCAPE2.0"));output.Write([3,1,0,0,0]);
        // WIC's encoder supplies palettes and LZW blocks. Add timing without re-encoding pixels.
        int at=header;bool control=false;
        void Blocks(){while(true){int size=bytes[at++];output.WriteByte((byte)size);if(size==0)break;output.Write(bytes,at,size);at+=size;}}
        while(at<bytes.Length)
        {
            int marker=bytes[at++];
            if(marker==0x3b){output.WriteByte(0x3b);break;}
            if(marker==0x21)
            {
                int label=bytes[at++];output.Write([0x21,(byte)label]);
                if(label==0xf9){var block=bytes.AsSpan(at,6).ToArray();block[1]=(byte)((block[1]&3)|4);block[2]=15;block[3]=0;output.Write(block);at+=6;control=true;}
                else Blocks();
            }
            else if(marker==0x2c)
            {
                if(!control)output.Write([0x21,0xf9,4,4,15,0,0,0]);control=false;
                output.WriteByte(0x2c);int flags=bytes[at+8];output.Write(bytes,at,9);at+=9;
                if((flags&128)!=0){int colors=3*(1<<((flags&7)+1));output.Write(bytes,at,colors);at+=colors;}
                output.WriteByte(bytes[at++]);Blocks();
            }
            else throw new InvalidDataException("Nieprawidłowy blok podglądu GIF.");
        }
        File.WriteAllBytes(path,output.ToArray());
    }
}
