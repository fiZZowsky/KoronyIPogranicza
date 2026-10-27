cbuffer Frame : register(b0)
{
    row_major float4x4 ViewProjection;
    row_major float4x4 LightProjection;
    row_major float4x4 InverseProjection;
    float4 EyeTime;
    float4 SunExposure;
    float4 Screen; // inverse width, inverse height, shadow texel, shadow enabled
    float4 Settings; // x: postprocess, y: material detail, z: atmospheric density
    float4 TerrainMap; // origin x/z, span x/z of the shared visual height field
    float4 VisualOptions; // shore bathymetry, edge smoothing
    float4 SeasonPalette; // snow cover, deciduous autumn, wet surfaces, spring green
    float4 WeatherSettings; // rain, snowfall, fog multiplier, light multiplier
};
Texture2D Land : register(t0);
Texture2D Surfaces : register(t1);
Texture2D Foliage : register(t2);
Texture2D<float> ShadowMap : register(t3);
Texture2D Hdr : register(t4);
Texture2D<float> SceneDepth : register(t5);
Texture2D Pine : register(t6);
Texture2D Reflection : register(t7);
Texture2D<float> Elevation : register(t8);
SamplerState TextureSampler : register(s0);
SamplerComparisonState ShadowSampler : register(s1);
struct Vertex { float3 p:POSITION; float3 n:NORMAL; float2 uv:TEXCOORD; float4 color:COLOR; float4 surface:SURFACE; };
struct Pixel { float4 p:SV_POSITION; float3 world:WORLD; float3 n:NORMAL; float2 uv:TEXCOORD; float4 color:COLOR; float4 surface:SURFACE; float4 light:LIGHT; };
Pixel WorldVS(Vertex v)
{
    if(v.surface.x>4.5&&v.surface.x<5.5)v.p.xz+=float2(sin(v.p.z*2+EyeTime.w*.7),cos(v.p.x*2+EyeTime.w*.6))*(.012+WeatherSettings.x*.008);
    Pixel o; o.world=v.p; o.n=v.n; o.uv=v.uv; o.color=v.color; o.surface=v.surface;
    o.p=mul(float4(v.p,1),ViewProjection);o.light=mul(float4(v.p,1),LightProjection);return o;
}
Pixel ShadowVS(Vertex v)
{
    Pixel o=WorldVS(v);o.p=o.light;return o;
}
void ShadowPS(Pixel p)
{
    if(p.surface.x>4.5 && p.surface.x<5.5)clip((p.surface.w>.5?Pine.Sample(TextureSampler,p.uv).a:Foliage.Sample(TextureSampler,p.uv).a)-.44);
}
float Hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
float Noise(float2 p)
{
    float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);
    return lerp(lerp(Hash(i),Hash(i+float2(1,0)),f.x),lerp(Hash(i+float2(0,1)),Hash(i+1),f.x),f.y);
}
float3 Decode(float3 c){return pow(max(c,.001),2.2);}
float GroundHeight(float2 world)
{
    uint width,height;Elevation.GetDimensions(width,height);
    float2 dimensions=float2(width,height);float2 uv=(world-TerrainMap.xy)/TerrainMap.zw;
    return Elevation.SampleLevel(TextureSampler,(saturate(uv)*(dimensions-1)+.5)/dimensions,0);
}
float3 Tile(float2 uv,float2 quadrant)
{
    // Mirrored tile coordinates prevent a hard seam in generated nonperiodic scans.
    float2 q=1-abs(frac(uv*.5)*2-1);
    return Surfaces.Sample(TextureSampler,(quadrant+(.006+q*.988))*.5).rgb;
}
float3 Triplanar(float3 p,float3 n,float2 quadrant,float scale)
{
    float3 w=pow(abs(n),5);w/=max(.001,w.x+w.y+w.z);
    return Tile(p.zy*scale,quadrant)*w.x+Tile(p.xz*scale,quadrant)*w.y+Tile(p.xy*scale,quadrant)*w.z;
}
float Visibility(Pixel p,float3 n)
{
    if(Screen.w<.5)return 1;
    float3 q=p.light.xyz/p.light.w;float2 uv=q.xy*float2(.5,-.5)+.5;
    if(any(uv<.002)||any(uv>.998)||q.z<=0||q.z>=1)return 1;
    float bias=.00035+.0009*(1-saturate(dot(n,normalize(SunExposure.xyz))));float sum=0;
    [unroll]for(int x=-1;x<=1;x++)[unroll]for(int y=-1;y<=1;y++)sum+=ShadowMap.SampleCmpLevelZero(ShadowSampler,uv+float2(x,y)*Screen.z,q.z-bias);
    return sum/9;
}
float3 Sky(float3 d)
{
    float h=saturate(d.y*.65+.25);float3 sky=lerp(float3(.65,.72,.75),float3(.20,.39,.58),h);
    float sun=pow(saturate(dot(d,normalize(SunExposure.xyz))),75);sky+=float3(1.5,1.05,.55)*sun;
    float cloud=Noise(d.xz/max(.15,d.y+.3)*3)+Noise(d.xz/max(.15,d.y+.3)*9)*.22;
    float cover=saturate(WeatherSettings.x*.65+WeatherSettings.y*.4);
    sky=lerp(sky,float3(.43,.50,.54),cover);
    return lerp(sky,float3(.74,.77,.77),smoothstep(.59-cover*.18,.77,cloud)*saturate(d.y+.25)*(.6+cover*.2));
}
float3 Brdf(float3 albedo,float3 n,float3 v,float3 l,float rough,float metal)
{
    float3 h=normalize(v+l);float nv=max(.02,dot(n,v)),nl=saturate(dot(n,l)),nh=saturate(dot(n,h)),vh=saturate(dot(v,h));
    float a=rough*rough,a2=a*a,d=nh*nh*(a2-1)+1;float distribution=a2/max(.0001,3.14159265*d*d);
    float k=(rough+1)*(rough+1)/8;float geometry=nv/(nv*(1-k)+k)*nl/(nl*(1-k)+k);
    float3 f0=lerp(.04.xxx,albedo,metal),f=f0+(1-f0)*pow(1-vh,5);
    float3 spec=distribution*geometry*f/max(.001,4*nv*max(nl,.02));
    return ((1-f)*(1-metal)*albedo/3.14159265+spec)*nl;
}
float4 WorldPS(Pixel p,bool front:SV_IsFrontFace):SV_TARGET
{
    float kind=p.surface.x,rough=p.surface.y,metal=p.surface.z;
    if(Settings.w>.5&&Settings.w<1.5)clip(p.world.y-.055);
    float3 n=normalize(p.n);if(!front && kind>4.5 && kind<5.5)n=-n;float3 v=normalize(EyeTime.xyz-p.world),sun=normalize(SunExposure.xyz);
    if(kind>11.5)
    {
        float2 uv=p.uv*2-1;float opacity;
        if(kind>12.5)opacity=(1-smoothstep(.35,1,dot(uv,uv)))*p.color.a;
        else opacity=(1-smoothstep(.25,1,abs(uv.x)))*(1-abs(uv.y))*p.color.a;
        clip(opacity-.012);return float4(p.color.rgb,opacity);
    }
    float3 albedo=Decode(p.color.rgb);float alpha=p.color.a;float ao=1;
    if(kind<.5)
    {
        float3 region=Land.Sample(TextureSampler,p.uv).rgb;
        float3 grass=Tile(p.world.xz*1.65,float2(0,0));
        float3 rock=Triplanar(p.world,n,float2(1,0),.65);
        float slope=1-saturate(n.y);float stone=saturate(slope*3.6+smoothstep(.95,2.15,p.world.y)*.4);
        float snow=smoothstep(3.1,4.4,p.world.y)*saturate(n.y*1.5-.3);
        float3 detail=lerp(grass,rock,stone);
        float known=step(.02,abs(region.g-region.b));
        float3 base=region*(.55+detail*.9);base=lerp(base,lerp(float3(.27,.28,.25),float3(.64,.65,.62),rock.r),stone*.72);
        base=lerp(base,float3(.84,.86,.86),snow);base=lerp(region,base,known*Settings.y);
        float wet=(1-smoothstep(.05,.19,p.world.y))*VisualOptions.x*known;
        base=lerp(base,float3(.27,.25,.18)+rock*.15,wet*.7);
        float arid=smoothstep(.025,.085,region.r-region.g);
        base=lerp(base,base*float3(.97,.87,.69),SeasonPalette.y*.3*known);
        base=lerp(base,base*float3(.94,1.04,.92),SeasonPalette.w*.22*known);
        float winter=SeasonPalette.x*(1-arid*.92)*known*saturate(n.y*1.3-.15);
        winter*=.85+Noise(p.world.xz*5)*.15;
        base=lerp(base,float3(.78,.84,.87),winter);
        albedo=Decode(base);rough=.93;
        float height=dot(detail,float3(.299,.587,.114));float3 dp1=ddx(p.world),dp2=ddy(p.world);
        float3 r1=cross(dp2,n),r2=cross(n,dp1);float det=dot(dp1,r1);
        n=normalize(abs(det)*n-sign(det)*(ddx(height)*r1+ddy(height)*r2)*.005);
    }
    else if(kind>1.5&&kind<2.5)
    {
        float3 masonry=Triplanar(p.world,n,float2(0,1),8);
        albedo=Decode(masonry)*(.75+p.color.rgb*.45);rough=.87;
    }
    else if(kind>2.5&&kind<3.5)
    {
        albedo=Decode(Triplanar(p.world,n,float2(1,1),9))*.9;rough=.78;
    }
    else if(kind>4.5&&kind<5.5)
    {
        float4 leaves=p.surface.w>.5?Pine.Sample(TextureSampler,p.uv):Foliage.Sample(TextureSampler,p.uv);clip(leaves.a-.44);alpha=1;
        albedo=Decode(leaves.rgb)*p.color.rgb;rough=.86;ao=.88;
        if(p.surface.w<.5)
        {
            float luminance=dot(leaves.rgb,float3(.299,.587,.114));
            albedo=lerp(albedo,Decode(float3(.84,.42,.13)*(luminance+.25)),SeasonPalette.y*.88);
            albedo*=lerp(float3(1,1,1),float3(.88,1.06,.90),SeasonPalette.w*.3);
        }
        albedo=lerp(albedo,Decode(float3(.77,.83,.85)),SeasonPalette.x*saturate(p.n.y*.6+.4)*.65);
        n=normalize(lerp(n,float3(0,1,0),.45));
    }
    else if(kind>5.5&&kind<6.5)
    {
        float grain=Noise(p.world.xz*45+p.world.y*float2(1,3));albedo*=.55+grain*.55;rough=.91;
    }
    else if(kind>6.5&&kind<7.5){rough=.13;metal=.42;albedo=Decode(p.color.rgb)*.65;}
    if(kind>7.5&&kind<8.5)return float4(p.color.rgb*.9,alpha); // strategic overlays remain readable
    if(kind>8.5&&kind<9.5){float3 rock=Triplanar(p.world,n,float2(1,0),.15);albedo=Decode(lerp(rock*.7,float3(.82,.85,.85),smoothstep(7,13,p.world.y)*saturate(n.y+.4)));rough=.95;}
    if(kind>9.5&&kind<10.5){albedo=Decode(Triplanar(p.world,n,float2(0,1),5))*.54;rough=.98;}
    if(kind>10.5){albedo=Decode(Triplanar(p.world,n,float2(1,0),6))*p.color.rgb;rough=.96;}
    if((kind>1.5&&kind<3.5)||(kind>5.5&&kind<6.5)||(kind>8.5&&kind<11.5))
    {
        float settled=SeasonPalette.x*smoothstep(.35,.8,n.y)*(.83+Noise(p.world.xz*13)*.17);
        albedo=lerp(albedo,Decode(float3(.80,.85,.87)),settled);
    }
    if(kind<.5||(kind>1.5&&kind<4.5)||(kind>9.5&&kind<11.5))
    {
        albedo*=1-SeasonPalette.z*.16;rough=lerp(rough,.35,SeasonPalette.z*.65);
    }
    float visibility=Visibility(p,n);
    if(kind>.5&&kind<1.5)
    {
        float2 w=p.world.xz;
        float2 flow=float2(Noise(w*3.8+EyeTime.w*.025),Noise(w*4.1+23-EyeTime.w*.017))-.5;
        float ripple=Noise(w*8.3+EyeTime.w*.05)-.5;
        if(p.surface.w>1.5)
        {
            float current=Noise(float2(p.uv.x*5,p.uv.y*8-EyeTime.w*.55))-.5;
            flow=float2(current,Noise(float2(p.uv.x*9+7,p.uv.y*13-EyeTime.w*.8))-.5);ripple=current;
        }
        n=normalize(float3(flow.x*.035,1,flow.y*.035));
        float fresnel=.025+.975*pow(1-saturate(dot(n,v)),5);float3 reflected=Sky(reflect(-v,n));
        if(p.surface.w<.5&&Settings.w>1.5)reflected=lerp(reflected,Reflection.Sample(TextureSampler,p.p.xy*Screen.xy+n.xz*.012).rgb,.78);
        float3 water=float3(.025,.10,.105)*(1+ripple*.06);
        if(VisualOptions.x>.5)
        {
            float depth=max(.005,p.world.y-GroundHeight(w));
            float3 bed=float3(.24,.22,.13)*exp(-depth*float3(4.8,3.0,2.2));
            water=lerp(bed,float3(.013,.066,.085),1-exp(-depth*3.4));
            float foam=(1-smoothstep(.005,.07,depth))*saturate(Noise(w*18+EyeTime.w*.08)*1.7-.5);
            if(p.surface.w>1.5)foam=max(foam,pow(saturate(abs(p.uv.x-.5)*2),6)*smoothstep(-.15,.35,ripple)*.5);
            water=lerp(water,float3(.51,.59,.55),foam*.28);
        }
        float3 glint=Brdf(.02.xxx,n,v,sun,.22,.0)*float3(4.6,3.9,2.8)*visibility;
        return float4(lerp(water,reflected,fresnel*.85)+glint,1);
    }
    float3 ambient=lerp(float3(.14,.16,.12),float3(.34,.40,.46),saturate(n.y*.5+.5));
    float3 direct=Brdf(albedo,n,v,sun,rough,metal)*float3(4.7,4.05,3.0)*visibility;
    float3 color=direct+albedo*ambient*ao;
    if(kind>4.5&&kind<5.5)color+=albedo*float3(.6,.62,.32)*pow(saturate(dot(-v,sun)),3)*.22;
    if(kind>6.5&&kind<7.5)color+=Sky(reflect(-v,n))*.34;
    float fog=1-exp(-length(EyeTime.xyz-p.world)*Settings.z*exp(-max(0,p.world.y)*.18));
    color=lerp(color,float3(.49,.59,.65),min(.7,fog));return float4(color,alpha);
}
struct FullPixel {float4 p:SV_POSITION;float2 uv:TEXCOORD;};
FullPixel FullVS(uint id:SV_VertexID)
{
    FullPixel o;o.uv=float2((id<<1)&2,id&2);o.p=float4(o.uv*float2(2,-2)+float2(-1,1),0,1);return o;
}
float3 Film(float3 x){return saturate((x*(2.51*x+.03))/(x*(2.43*x+.59)+.14));}
float4 SkyPS(FullPixel p):SV_TARGET
{
    float4 far=mul(float4(p.uv*float2(2,-2)+float2(-1,1),1,1),InverseProjection);
    return float4(Sky(normalize(far.xyz/far.w-EyeTime.xyz)),1);
}
float4 PostPS(FullPixel p):SV_TARGET
{
    float3 color=Hdr.SampleLevel(TextureSampler,p.uv,0).rgb;float depth=SceneDepth.SampleLevel(TextureSampler,p.uv,0);
    if(Settings.x>.5)
    {
        float occlusion=0;
        [unroll]for(int i=0;i<8;i++)
        {
            float angle=i*2.399963;float2 offset=float2(cos(angle),sin(angle))*Screen.xy*(3+i*1.1);
            float other=SceneDepth.SampleLevel(TextureSampler,p.uv+offset,0);
            float difference=depth-other;occlusion+=step(.00015,difference)*saturate(1-abs(difference)*120);
        }
        color*=1-occlusion*.032;
        float3 bloom=0;
        [unroll]for(int j=0;j<4;j++)
        {
            float2 offset=float2(j==0?1:j==1?-1:0,j==2?1:j==3?-1:0)*Screen.xy*5;
            bloom+=max(0,Hdr.SampleLevel(TextureSampler,p.uv+offset,0).rgb-1.1);
        }
        color+=bloom*.035;
    }
    color=Film(color*SunExposure.w);color=pow(color,1/2.2);
    if(VisualOptions.y>.5)
    {
        float3 left=pow(Film(Hdr.SampleLevel(TextureSampler,p.uv-float2(Screen.x,0),0).rgb*SunExposure.w),1/2.2);
        float3 right=pow(Film(Hdr.SampleLevel(TextureSampler,p.uv+float2(Screen.x,0),0).rgb*SunExposure.w),1/2.2);
        float3 up=pow(Film(Hdr.SampleLevel(TextureSampler,p.uv-float2(0,Screen.y),0).rgb*SunExposure.w),1/2.2);
        float3 down=pow(Film(Hdr.SampleLevel(TextureSampler,p.uv+float2(0,Screen.y),0).rgb*SunExposure.w),1/2.2);
        float contrast=length(max(max(left,right),max(up,down))-min(min(left,right),min(up,down)));
        color=lerp(color,(left+right+up+down)*.25,smoothstep(.12,.6,contrast)*.32);
    }
    float vignette=saturate(1-dot(p.uv-.5,p.uv-.5)*.20);color*=vignette;
    float dither=(Hash(p.p.xy)-.5)/255;return float4(saturate(color+dither),1);
}
