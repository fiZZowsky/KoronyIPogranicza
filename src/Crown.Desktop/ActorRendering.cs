namespace Crown.Desktop;

internal sealed partial class PhotographicRenderer
{
    private readonly Dictionary<int,nint[]> actorViews=[];
    private int boundActor=-1;
    private nint[] EnsureActorViews(int id)
    {
        if(actorViews.TryGetValue(id,out var cached))return cached;
        var views=new nint[3];int i=0;
        foreach(string channel in new[]{"base","normal","orm"}){var texture=Upload(ActorAssets.Texture(id,channel));views[i++]=texture.View;}
        actorViews[id]=views;return views;
    }
    private void DrawWithActors(DrawMesh mesh)
    {
        int id=(int)mesh.Kind-36;
        if(id>=0&&id<ActorAssets.Names.Length&&boundActor!=id)
        {
            gpu.ActorResources(EnsureActorViews(id));boundActor=id;
        }
        gpu.Draw(mesh.Buffer,mesh.Vertices);
    }
}
