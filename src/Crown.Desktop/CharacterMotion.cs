using System.Windows.Media.Media3D;

namespace Crown.Desktop;

// Immutable visual metadata, carried alongside triangle ownership. No campaign clock or RNG.
internal sealed record CharacterFrame(Point3D Origin,double Height,int Identity=0)
{
    internal static readonly CharacterFrame Empty=new(default,0);
    internal bool Active=>Height>0;
}

internal static class CharacterAppearance
{
    internal static int Variant(int identity)=>(identity^(identity>>8))&3;
    internal static float Phase(int identity)
    {
        uint key=unchecked((uint)identity*0x9e3779b9);key^=key>>16;key*=0x7feb352d;key^=key>>15;
        return (key&65535)/65536f;
    }
    internal static double Height(int variant)=>.415+variant*.010;
    internal static string Hide(int variant)=>variant switch{0=>"#7A6954",1=>"#8B7057",2=>"#655F51",_=>"#806C59"};
}
