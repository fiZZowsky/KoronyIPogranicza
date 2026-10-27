using System.Numerics;

namespace Crown.Desktop;

// Row-vector matrices and Direct3D's 0..w depth interval. Every pass supplies its
// own matrix: the reflected camera must not inherit the main camera's visibility.
internal sealed class RenderFrustum
{
    private readonly Vector4[] planes;
    internal RenderFrustum(Matrix4x4 m)
    {
        var x=new Vector4(m.M11,m.M21,m.M31,m.M41);var y=new Vector4(m.M12,m.M22,m.M32,m.M42);
        var z=new Vector4(m.M13,m.M23,m.M33,m.M43);var w=new Vector4(m.M14,m.M24,m.M34,m.M44);
        planes=[w+x,w-x,w+y,w-y,z,w-z];
    }
    internal bool Intersects(Vector3 minimum,Vector3 maximum)
    {
        var center=(minimum+maximum)*.5f;var half=(maximum-minimum)*.5f+new Vector3(.10f); // includes shader wind and numerical error
        foreach(var plane in planes)
        {
            var normal=new Vector3(plane.X,plane.Y,plane.Z);
            if(Vector3.Dot(normal,center)+plane.W+Vector3.Dot(Vector3.Abs(normal),half)<0)return false;
        }
        return true;
    }
}

internal readonly record struct RenderStatistics(int InputTriangles,int MainTriangles,int ShadowTriangles,int ReflectionTriangles,
    int MainDraws,int ShadowDraws,int ReflectionDraws,int CulledBatches,int CoarseTreeDraws);
