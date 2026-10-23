using System.Runtime.InteropServices;
using System.Text;

namespace Crown.Desktop;

// All signatures follow the Windows SDK d3d11.h. This boundary owns its COM references;
// native resources never enter simulation or save data.
internal sealed unsafe class NativeGpu : IDisposable
{
    [DllImport("d3d11.dll", CallingConvention = CallingConvention.StdCall)]
    private static extern int D3D11CreateDevice(nint adapter, uint driver, nint software, uint flags, uint* levels, uint count, uint sdk, out nint device, out uint level, out nint context);
    [DllImport("d3dcompiler_47.dll", CallingConvention = CallingConvention.StdCall)]
    private static extern int D3DCompile(byte* source, nuint length, [MarshalAs(UnmanagedType.LPStr)] string name, nint defines, nint include, [MarshalAs(UnmanagedType.LPStr)] string entry, [MarshalAs(UnmanagedType.LPStr)] string target, uint flags, uint flags2, out nint code, out nint errors);
    internal readonly nint Device, Context;
    internal readonly uint FeatureLevel;
    private readonly HashSet<nint> owned = [];
    private bool disposed;
    private static readonly Dictionary<string, byte[]> shaderCache = [];
    internal static void Check(int result, string operation) { if (result < 0) throw new InvalidOperationException($"Direct3D 11: {operation} (0x{result:X8}).", Marshal.GetExceptionForHR(result)); }
    internal static void* V(nint instance, int index) => (void*)(*(nint**)instance)[index];
    internal static void Release(nint instance) { if (instance != 0) ((delegate* unmanaged[Stdcall]<nint, uint>)V(instance, 2))(instance); }
    private nint Own(nint value) { if (value != 0) owned.Add(value); return value; }
    internal void Free(nint value) { if (owned.Remove(value)) Release(value); }
    public NativeGpu()
    {
        uint level = 0xb000;
        Check(D3D11CreateDevice(0, 1, 0, 0x20, &level, 1, 7, out Device, out FeatureLevel, out Context), "create hardware device, feature level 11_0");
    }
    [StructLayout(LayoutKind.Sequential)] internal struct BufferDesc { internal uint Bytes, Usage, Bind, CpuAccess, Misc, Stride; }
    [StructLayout(LayoutKind.Sequential)] internal struct TextureDesc { internal uint Width, Height, Mips, Array, Format, Samples, Quality, Usage, Bind, CpuAccess, Misc; }
    [StructLayout(LayoutKind.Sequential)] internal struct Data { internal nint Pointer; internal uint Pitch, SlicePitch; }
    [StructLayout(LayoutKind.Sequential)] internal struct Mapped { internal nint Pointer; internal uint Pitch, DepthPitch; }
    [StructLayout(LayoutKind.Sequential)] internal struct Viewport { internal float X, Y, Width, Height, Near, Far; }
    [StructLayout(LayoutKind.Sequential)] internal struct Element { internal nint Name; internal uint SemanticIndex, Format, Slot, Offset, Class, Step; }
    internal nint Buffer<T>(ReadOnlySpan<T> values, uint bind) where T : unmanaged
    {
        fixed (T* p = values) { var desc = new BufferDesc { Bytes = (uint)(values.Length * sizeof(T)), Bind = bind }; var data = new Data { Pointer = (nint)p }; nint result; Check(((delegate* unmanaged[Stdcall]<nint, BufferDesc*, Data*, nint*, int>)V(Device, 3))(Device, &desc, &data, &result), "create buffer"); return Own(result); }
    }
    internal nint ConstantBuffer(int size)
    {
        var desc = new BufferDesc { Bytes = (uint)((size + 15) / 16 * 16), Bind = 4 }; nint result;
        Check(((delegate* unmanaged[Stdcall]<nint, BufferDesc*, Data*, nint*, int>)V(Device, 3))(Device, &desc, null, &result), "create constants"); return Own(result);
    }
    internal void Update<T>(nint resource, in T value) where T : unmanaged
    { fixed (T* p = &value) ((delegate* unmanaged[Stdcall]<nint, nint, uint, nint, void*, uint, uint, void>)V(Context, 48))(Context, resource, 0, 0, p, 0, 0); }
    internal nint Texture(int width, int height, uint format, uint bind, byte[]? pixels = null, bool staging = false, bool mipmaps = false)
    {
        var desc = new TextureDesc { Width = (uint)width, Height = (uint)height, Mips = mipmaps ? 0u : 1u, Array = 1, Format = format, Samples = 1, Bind = bind, Usage = staging ? 3u : 0u, CpuAccess = staging ? 0x20000u : 0, Misc = mipmaps ? 1u : 0u }; nint result;
        fixed (byte* p = pixels)
        {
            var data = new Data { Pointer = (nint)p, Pitch = (uint)(width * 4) };
            Check(((delegate* unmanaged[Stdcall]<nint, TextureDesc*, Data*, nint*, int>)V(Device, 5))(Device, &desc, pixels != null && !mipmaps ? &data : null, &result), "create texture");
            if (pixels != null && mipmaps) ((delegate* unmanaged[Stdcall]<nint, nint, uint, nint, void*, uint, uint, void>)V(Context, 48))(Context, result, 0, 0, p, (uint)(width * 4), 0);
        }
        return Own(result);
    }
    internal nint View(nint resource, int method, uint format = 0)
    {
        nint result; uint* desc = stackalloc uint[6]; for (int i = 0; i < 6; i++) desc[i] = 0;
        desc[0] = format; desc[1] = method == 7 ? 4u : 3u;
        if (method == 7) desc[3] = 1;
        Check(((delegate* unmanaged[Stdcall]<nint, nint, void*, nint*, int>)V(Device, method))(Device, resource, format == 0 ? null : desc, &result), "create resource view"); return Own(result);
    }
    internal void Mips(nint view) => ((delegate* unmanaged[Stdcall]<nint, nint, void>)V(Context, 54))(Context, view);
    internal (nint Shader, byte[] Bytecode) Shader(string source, string entry, bool vertex)
    {
        string cacheKey = entry + source;
        if (shaderCache.TryGetValue(cacheKey, out var cached))
        {
            fixed (byte* p = cached)
            {
                nint result; Check(((delegate* unmanaged[Stdcall]<nint, byte*, nuint, nint, nint*, int>)V(Device, vertex ? 12 : 15))(Device, p, (nuint)cached.Length, 0, &result), "create cached shader " + entry);
                return (Own(result), cached);
            }
        }
        var text = Encoding.UTF8.GetBytes(source); nint code = 0, errors = 0;
        try
        {
            fixed (byte* p = text)
            {
                int hr = D3DCompile(p, (nuint)text.Length, "CrownLighting.hlsl", 0, 0, entry, vertex ? "vs_5_0" : "ps_5_0", 1u << 15, 0, out code, out errors);
                if (hr < 0) throw new InvalidOperationException("HLSL " + entry + ": " + (errors == 0 ? $"0x{hr:X8}" : Marshal.PtrToStringAnsi(BlobData(errors), (int)BlobSize(errors))));
            }
            nint shader; var ptr = BlobData(code); var size = BlobSize(code);
            Check(((delegate* unmanaged[Stdcall]<nint, nint, nuint, nint, nint*, int>)V(Device, vertex ? 12 : 15))(Device, ptr, size, 0, &shader), "create shader " + entry);
            var bytes = new byte[(int)size]; Marshal.Copy(ptr, bytes, 0, bytes.Length); shaderCache[cacheKey] = bytes; return (Own(shader), bytes);
        }
        finally { Release(code); Release(errors); }
    }
    private static nint BlobData(nint blob) => ((delegate* unmanaged[Stdcall]<nint, nint>)V(blob, 3))(blob);
    private static nuint BlobSize(nint blob) => ((delegate* unmanaged[Stdcall]<nint, nuint>)V(blob, 4))(blob);
    internal nint InputLayout(byte[] code)
    {
        string[] names = ["POSITION", "NORMAL", "TEXCOORD", "COLOR", "SURFACE"];
        var strings = names.Select(Marshal.StringToHGlobalAnsi).ToArray();
        try
        {
            Element* elements = stackalloc Element[5]; uint[] formats = [6, 6, 16, 2, 2]; uint[] offsets = [0, 12, 24, 32, 48];
            for (int i = 0; i < 5; i++) elements[i] = new Element { Name = strings[i], Format = formats[i], Offset = offsets[i] };
            fixed (byte* p = code) { nint result; Check(((delegate* unmanaged[Stdcall]<nint, Element*, uint, byte*, nuint, nint*, int>)V(Device, 11))(Device, elements, 5, p, (nuint)code.Length, &result), "create input layout"); return Own(result); }
        }
        finally { foreach (var p in strings) Marshal.FreeHGlobal(p); }
    }
    internal nint Rasterizer(bool shadow = false)
    {
        uint* desc = stackalloc uint[10]; for(int i=0;i<10;i++)desc[i]=0;
        desc[0] = 3; desc[1] = 1; desc[3] = shadow ? 450u : 0u; desc[5] = shadow ? (uint)BitConverter.SingleToInt32Bits(1.8f) : 0; desc[6] = 1;
        nint result; Check(((delegate* unmanaged[Stdcall]<nint, void*, nint*, int>)V(Device, 22))(Device, desc, &result), "create rasterizer"); return Own(result);
    }
    internal nint Sampler(bool shadow = false)
    {
        uint* desc = stackalloc uint[13]; for(int i=0;i<13;i++)desc[i]=0;
        desc[0] = shadow ? 0x95u : 0x55u; desc[1] = desc[2] = desc[3] = shadow ? 4u : 1u; desc[5] = 8; desc[6] = shadow ? 4u : 1u;
        for(int i=7;i<=10;i++)desc[i]=(uint)BitConverter.SingleToInt32Bits(1);
        desc[12] = (uint)BitConverter.SingleToInt32Bits(float.MaxValue);
        nint result; Check(((delegate* unmanaged[Stdcall]<nint, void*, nint*, int>)V(Device, 23))(Device, desc, &result), "create sampler"); return Own(result);
    }
    internal nint DepthState(bool write)
    {
        uint* desc = stackalloc uint[13]; for(int i=0;i<13;i++)desc[i]=0;
        desc[0]=1;desc[1]=write?1u:0;desc[2]=4;
        desc[5]=desc[6]=desc[7]=desc[9]=desc[10]=desc[11]=1;desc[8]=desc[12]=8;
        nint result;Check(((delegate* unmanaged[Stdcall]<nint,void*,nint*,int>)V(Device,21))(Device,desc,&result),"create depth state");return Own(result);
    }
    internal nint BlendState(bool enabled)
    {
        uint* desc = stackalloc uint[66];for(int i=0;i<66;i++)desc[i]=0;
        for(int i=0;i<8;i++){int start=2+i*8;desc[start]=enabled?1u:0;desc[start+1]=5;desc[start+2]=6;desc[start+3]=1;desc[start+4]=1;desc[start+5]=6;desc[start+6]=1;desc[start+7]=15;}
        nint result;Check(((delegate* unmanaged[Stdcall]<nint,void*,nint*,int>)V(Device,20))(Device,desc,&result),"create blend state");return Own(result);
    }
    internal void Targets(nint render, nint depth) { ((delegate* unmanaged[Stdcall]<nint,uint,nint*,nint,void>)V(Context,33))(Context,render==0?0u:1u,render==0?null:&render,depth); }
    internal void Clear(nint target,float r,float g,float b,float a=1){float* c=stackalloc float[4]{r,g,b,a};((delegate* unmanaged[Stdcall]<nint,nint,float*,void>)V(Context,50))(Context,target,c);}
    internal void ClearDepth(nint depth)=>((delegate* unmanaged[Stdcall]<nint,nint,uint,float,byte,void>)V(Context,53))(Context,depth,1,1,0);
    internal void SetViewport(int width,int height){var v=new Viewport{Width=width,Height=height,Far=1};((delegate* unmanaged[Stdcall]<nint,uint,Viewport*,void>)V(Context,44))(Context,1,&v);}
    internal void Raster(nint state)=>((delegate* unmanaged[Stdcall]<nint,nint,void>)V(Context,43))(Context,state);
    internal void Depth(nint state)=>((delegate* unmanaged[Stdcall]<nint,nint,uint,void>)V(Context,36))(Context,state,0);
    internal void Blend(nint state)=>((delegate* unmanaged[Stdcall]<nint,nint,nint,uint,void>)V(Context,35))(Context,state,0,uint.MaxValue);
    internal void Shaders(nint vs,nint ps){((delegate* unmanaged[Stdcall]<nint,nint,nint,uint,void>)V(Context,11))(Context,vs,0,0);((delegate* unmanaged[Stdcall]<nint,nint,nint,uint,void>)V(Context,9))(Context,ps,0,0);}
    internal void Layout(nint layout){((delegate* unmanaged[Stdcall]<nint,nint,void>)V(Context,17))(Context,layout);((delegate* unmanaged[Stdcall]<nint,uint,void>)V(Context,24))(Context,4);}
    internal void Constants(nint buffer){((delegate* unmanaged[Stdcall]<nint,uint,uint,nint*,void>)V(Context,7))(Context,0,1,&buffer);((delegate* unmanaged[Stdcall]<nint,uint,uint,nint*,void>)V(Context,16))(Context,0,1,&buffer);}
    internal void Resources(params nint[] views){fixed(nint* p=views)((delegate* unmanaged[Stdcall]<nint,uint,uint,nint*,void>)V(Context,8))(Context,0,(uint)views.Length,p);}
    internal void Samplers(nint regular,nint shadow){nint* p=stackalloc nint[2]{regular,shadow};((delegate* unmanaged[Stdcall]<nint,uint,uint,nint*,void>)V(Context,10))(Context,0,2,p);}
    internal void Draw(nint buffer,int count){uint stride=64,offset=0;((delegate* unmanaged[Stdcall]<nint,uint,uint,nint*,uint*,uint*,void>)V(Context,18))(Context,0,1,&buffer,&stride,&offset);((delegate* unmanaged[Stdcall]<nint,uint,uint,void>)V(Context,13))(Context,(uint)count,0);}
    internal void Triangle()=>((delegate* unmanaged[Stdcall]<nint,uint,uint,void>)V(Context,13))(Context,3,0);
    internal void Copy(nint destination,nint source)=>((delegate* unmanaged[Stdcall]<nint,nint,nint,void>)V(Context,47))(Context,destination,source);
    internal Mapped Map(nint texture){Mapped data;Check(((delegate* unmanaged[Stdcall]<nint,nint,uint,uint,uint,Mapped*,int>)V(Context,14))(Context,texture,0,1,0,&data),"read frame");return data;}
    internal void Unmap(nint texture)=>((delegate* unmanaged[Stdcall]<nint,nint,uint,void>)V(Context,15))(Context,texture,0);
    public void Dispose()
    {
        if(disposed)return;disposed=true;
        ((delegate* unmanaged[Stdcall]<nint,void>)V(Context,110))(Context);
        foreach(var resource in owned)Release(resource);owned.Clear();Release(Context);Release(Device);
    }
}
