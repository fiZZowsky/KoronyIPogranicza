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
Texture2D Civilization : register(t9);
Texture2D Facades : register(t10);
Texture2D Organic : register(t11);
Texture2D Wheat : register(t12);
Texture2D ActorBase : register(t13);
Texture2D ActorNormal : register(t14);
Texture2D ActorOrm : register(t15);
Texture2D Understory : register(t16);
SamplerState TextureSampler : register(s0);
SamplerComparisonState ShadowSampler : register(s1);
struct Vertex { float3 p:POSITION; float3 n:NORMAL; float2 uv:TEXCOORD; float4 color:COLOR; float4 surface:SURFACE; };
struct Pixel { float4 p:SV_POSITION; float3 world:WORLD; float3 n:NORMAL; float2 uv:TEXCOORD; float4 color:COLOR; float4 surface:SURFACE; float4 light:LIGHT; };
Pixel WorldVS(Vertex v)
{
    if(v.surface.x>4.5&&v.surface.x<5.5)v.p.xz+=float2(sin(v.p.z*2+EyeTime.w*.7),cos(v.p.x*2+EyeTime.w*.6))*(.012+WeatherSettings.x*.008);
    if(v.surface.x>13.5&&v.surface.x<14.5)v.p.xz+=float2(sin(v.p.z*3+EyeTime.w*.9),cos(v.p.x*3+EyeTime.w*.8))*.005*saturate(v.uv.y)*saturate(v.uv.y);
    if(v.surface.x>34.5&&v.surface.x<35.5)v.p.xz+=float2(sin(v.p.x*2.3+EyeTime.w*.8),cos(v.p.z*2.1+EyeTime.w*.7))*.014*pow(1-v.uv.y,2);
    if(v.surface.x>45.5&&v.surface.x<46.5)
    {
        float bend=pow(saturate(1-v.uv.y),2);
        v.p.xz+=float2(sin(v.p.z*6+EyeTime.w*1.2),cos(v.p.x*5+EyeTime.w*.9))*.009*bend;
    }
    Pixel o; o.world=v.p; o.n=v.n; o.uv=v.uv; o.color=v.color; o.surface=v.surface;
    o.p=mul(float4(v.p,1),ViewProjection);o.light=mul(float4(v.p,1),LightProjection);return o;
}
Pixel ShadowVS(Vertex v)
{
    Pixel o=WorldVS(v);o.p=o.light;return o;
}
float2 UnderstoryUV(Pixel p)
{
    float index=clamp(floor(p.surface.w+.5),0,3);
    return (float2(fmod(index,2),floor(index/2))+.006+saturate(p.uv)*.988)*.5;
}
void ShadowPS(Pixel p)
{
    if(p.surface.x>45.5&&p.surface.x<46.5)clip(Understory.Sample(TextureSampler,UnderstoryUV(p)).a-.38);
    if(p.surface.x>34.5&&p.surface.x<35.5)clip(Wheat.Sample(TextureSampler,p.uv).a-.38);
    if(p.surface.x>4.5 && p.surface.x<5.5)clip((p.surface.w>.5?Pine.Sample(TextureSampler,p.uv).a:Foliage.Sample(TextureSampler,p.uv).a)-.44);
}
float Hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
float Noise(float2 p)
{
    float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);
    return lerp(lerp(Hash(i),Hash(i+float2(1,0)),f.x),lerp(Hash(i+float2(0,1)),Hash(i+1),f.x),f.y);
}
float3 Decode(float3 c){return pow(max(c,.001),2.2);}
float3 ReliefNormal(float3 position,float3 normal,float height,float amount)
{
    float3 dx=ddx(position),dy=ddy(position),rx=cross(dy,normal),ry=cross(normal,dx);
    float determinant=dot(dx,rx);
    return normalize(max(abs(determinant),.00000001)*normal-sign(determinant)*(ddx(height)*rx+ddy(height)*ry)*amount);
}
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
float3 CivilTile(float2 uv,float2 quadrant)
{
    float2 q=1-abs(frac(uv*.5)*2-1);
    return Civilization.Sample(TextureSampler,(quadrant+.006+q*.988)*.5).rgb;
}
float3 CivilSurface(float3 p,float3 n,float2 quadrant,float scale)
{
    float3 w=pow(abs(n),5);w/=max(.001,w.x+w.y+w.z);
    return CivilTile(p.zy*scale,quadrant)*w.x+CivilTile(p.xz*scale,quadrant)*w.y+CivilTile(p.xy*scale,quadrant)*w.z;
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
    float3 n=normalize(p.n);if(!front && ((kind>4.5 && kind<5.5)||(kind>13.5&&kind<14.5)||(kind>19.5&&kind<20.5)||(kind>34.5&&kind<35.5)||(kind>45.5&&kind<46.5)))n=-n;float3 v=normalize(EyeTime.xyz-p.world),sun=normalize(SunExposure.xyz);
    if(kind>11.5&&kind<13.5)
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
        float2 grassUV=p.world.xz*1.35;
        float macro=Noise(p.world.xz*.47);
        float3 grass=lerp(Tile(grassUV,float2(0,0)),Tile(grassUV*.73+float2(3.47,7.13),float2(0,0)),smoothstep(.25,.75,macro)*.65);
        float3 rock=grass;
        [branch]if(1-saturate(n.y)>.015||p.world.y>.95)rock=Triplanar(p.world,n,float2(1,0),.65);
        [branch]if(Settings.y>.5&&length(EyeTime.xyz-p.world)<30){float soil=smoothstep(.52,.78,Noise(p.world.xz*.72))* .32;grass=lerp(grass,CivilTile(p.world.xz*2,float2(0,0)),soil);}
        float slope=1-saturate(n.y);float stone=saturate(slope*3.6+smoothstep(.95,2.15,p.world.y)*.4);
        float snow=smoothstep(3.1,4.4,p.world.y)*saturate(n.y*1.5-.3);
        float3 detail=lerp(grass,rock,stone);
        float known=step(.02,abs(region.g-region.b));
        float3 base=region*(.48+detail*.72);
        float luma=dot(base,float3(.299,.587,.114));base=lerp(luma.xxx,base,.72);
        base*=lerp(float3(.78,.83,.74),float3(1.05,1.02,.94),smoothstep(.2,.8,macro));base=lerp(base,lerp(float3(.27,.28,.25),float3(.64,.65,.62),rock.r),stone*.72);
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
        n=ReliefNormal(p.world,n,dot(masonry,float3(.299,.587,.114)),.012*Settings.y);
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
        float grain=Noise(float2(p.world.x*58+p.world.z*5,p.world.y*11+p.world.z*2));
        float pores=Noise(p.world.xz*135+p.world.y*17);
        albedo*=.64+grain*.35+pores*.08;rough=lerp(.63,.9,grain);
        n=ReliefNormal(p.world,n,grain+pores*.13,.003*Settings.y);
    }
    else if(kind>6.5&&kind<7.5){rough=.13;metal=.42;albedo=Decode(p.color.rgb)*.65;}
    if(kind>7.5&&kind<8.5)return float4(p.color.rgb*.9,alpha); // strategic overlays remain readable
    if(kind>8.5&&kind<9.5){float3 rock=Triplanar(p.world,n,float2(1,0),.15);albedo=Decode(lerp(rock*.7,float3(.82,.85,.85),smoothstep(7,13,p.world.y)*saturate(n.y+.4)));rough=.95;}
    if(kind>9.5&&kind<10.5){albedo=Decode(Triplanar(p.world,n,float2(0,1),5))*.54;rough=.98;}
    if(kind>10.5&&kind<11.5){albedo=Decode(Triplanar(p.world,n,float2(1,0),6))*p.color.rgb;rough=.96;}
    if(kind>13.5&&kind<14.5)
    {
        n=normalize(lerp(n,float3(0,1,0),.45));albedo*=1+Noise(p.world.xz*25)*.12;
        albedo=lerp(albedo,albedo*float3(1.1,.84,.62),SeasonPalette.y*.5);
        albedo=lerp(albedo,Decode(float3(.77,.83,.85)),SeasonPalette.x*.75);rough=.92;
    }
    if(kind>14.5&&kind<17.5)
    {
        float3 stone=Triplanar(p.world,n,float2(1,0),5);albedo=Decode(stone)*p.color.rgb;rough=.88;
        if(kind>15.5&&kind<16.5)
        {
            float vein=smoothstep(.58,.73,Noise(p.world.xz*24+p.world.y*7));albedo=lerp(albedo*.65,Decode(float3(.39,.22,.12)),vein*.55);metal=.12;rough=.74;
        }
        if(kind>16.5){albedo=Decode(p.color.rgb)*(.88+Noise(p.world.xz*95)*.12);rough=.34;metal=.02;}
        albedo=lerp(albedo,Decode(float3(.80,.85,.87)),SeasonPalette.x*smoothstep(.4,.85,n.y)*.85);
    }
    if(kind>18.5&&kind<19.5){albedo=Decode(p.color.rgb);rough=.48;metal=.62;}
    if(kind>19.5&&kind<20.5)
    {
        float weave=sin(p.world.x*780)*sin(p.world.y*780+p.world.z*230);
        float wear=Noise(p.world.xy*31+p.world.z*7);
        weave*=1-saturate(fwidth(p.world.x*780)+fwidth(p.world.y*780+p.world.z*230));
        albedo*=.78+wear*.18+weave*.025;rough=.97;
        n=ReliefNormal(p.world,n,weave,.00015*Settings.y);
    }
    if(kind>20.5&&kind<21.5)
    {
        float wear=Noise(p.world.xz*27+p.world.y*11),salt=Noise(p.world.xz*93+p.world.y*43);
        float rust=smoothstep(.62,.88,wear)*(1-smoothstep(.04,.28,p.world.y))*.65;
        albedo=lerp(albedo*(.82+salt*.15),Decode(float3(.27,.12,.055)),rust);
        rough=lerp(.36,.84,rust);metal=lerp(.72,.12,rust);
        n=ReliefNormal(p.world,n,salt,.0008*Settings.y);
    }
    if(kind>17.5&&kind<18.5){albedo=Decode(p.color.rgb)*(.88+Noise(p.world.xz*float2(160,24)+p.world.y*37)*.19);rough=.96;}
    if((kind>1.5&&kind<3.5)||(kind>5.5&&kind<6.5)||(kind>8.5&&kind<11.5))
    {
        float settled=SeasonPalette.x*smoothstep(.35,.8,n.y)*(.83+Noise(p.world.xz*13)*.17);
        albedo=lerp(albedo,Decode(float3(.80,.85,.87)),settled);
    }
    if(kind<.5||(kind>1.5&&kind<4.5)||(kind>9.5&&kind<11.5))
    {
        albedo*=1-SeasonPalette.z*.16;rough=lerp(rough,.35,SeasonPalette.z*.65);
    }
    if(kind>21.5&&kind<26.5)
    {
        float2 quadrant=kind<22.5?float2(0,0):kind<23.5?float2(1,0):kind<24.5?float2(0,1):kind<25.5?float2(1,1):float2(0,1);
        float scale=kind<22.5?5:kind<23.5?6:kind<24.5?5:kind<25.5?4:3;
        float3 surface=CivilSurface(p.world,n,quadrant,scale);
        if(kind>25.5)surface=lerp(.67.xxx,surface,.25);
        albedo=Decode(surface)*(.70+p.color.rgb*.35);rough=kind>24.5&&kind<25.5?.78:.91;
        n=ReliefNormal(p.world,n,dot(surface,float3(.299,.587,.114)),.007*Settings.y);
        albedo*=1-SeasonPalette.z*.18;rough=lerp(rough,.38,SeasonPalette.z*.6);
        albedo=lerp(albedo,Decode(float3(.80,.85,.87)),SeasonPalette.x*smoothstep(.4,.85,n.y)*.8);
    }
    if(kind>26.5&&kind<30.5)
    {
        float index=kind-27;float2 quadrant=float2(fmod(index,2),floor(index/2));
        float2 uv=(quadrant+.008+saturate(p.uv)*.984)*.5;
        float3 facade=Facades.Sample(TextureSampler,uv).rgb;
        albedo=Decode(facade);rough=kind>29.5?.30:.84;metal=kind>29.5?.15:0;
        n=ReliefNormal(p.world,n,dot(facade,float3(.299,.587,.114)),.0015*Settings.y);
        albedo*=1-SeasonPalette.z*.1;
    }
    if(kind>30.5&&kind<34.5)
    {
        float index=kind-31;float2 quadrant=float2(fmod(index,2),floor(index/2));
        float2 tiled=p.uv*float2(2,3);float2 uv=(quadrant+.008+frac(tiled)*.984)*.5;
        // Preserve the unwrapped derivatives: frac seams must not sample other atlas quadrants.
        float3 sample=Organic.SampleGrad(TextureSampler,uv,ddx(tiled)*.492,ddy(tiled)*.492).rgb;
        float luminance=dot(sample,float3(.299,.587,.114));
        if(kind>32.5&&kind<33.5)sample=lerp(sample,luminance.xxx,.7)*(.55+p.color.rgb*.7);
        if(kind>31.5&&kind<32.5){float pale=saturate((p.color.g-.48)*4);sample=lerp(lerp(sample,luminance.xxx,.25),luminance.xxx*1.35,pale);}
        if(kind>33.5)sample*=.3+p.color.rgb*.65;
        albedo=Decode(sample);rough=kind<31.5?.58:kind<32.5?.78:kind<33.5?.94:.63;
        n=ReliefNormal(p.world,n,Noise(p.world.xy*100+p.world.z*17),.00005*Settings.y);
    }
    if(kind>34.5&&kind<35.5)
    {
        float4 wheat=Wheat.Sample(TextureSampler,p.uv);clip(wheat.a-.38);alpha=1;
        albedo=Decode(wheat.rgb/max(wheat.a,.001));rough=.92;
        albedo=lerp(albedo,Decode(float3(.79,.83,.84)),SeasonPalette.x*.35);
    }
    if(kind>35.5&&kind<43.5)
    {
        albedo=Decode(ActorBase.Sample(TextureSampler,p.uv).rgb);
        float3 orm=ActorOrm.Sample(TextureSampler,p.uv).rgb;rough=clamp(orm.g,.12,.98);metal=saturate(orm.b);
        float3 dp1=ddx(p.world),dp2=ddy(p.world);float2 duv1=ddx(p.uv),duv2=ddy(p.uv);
        float determinant=duv1.x*duv2.y-duv1.y*duv2.x;
        if(abs(determinant)>.0000001)
        {
            float3 tangent=(dp1*duv2.y-dp2*duv1.y)/determinant;
            float3 bitangent=(-dp1*duv2.x+dp2*duv1.x)/determinant;
            tangent=normalize(tangent-n*dot(n,tangent));bitangent=normalize(bitangent-n*dot(n,bitangent));
            float3 detail=ActorNormal.Sample(TextureSampler,p.uv).xyz*2-1;
            n=normalize(tangent*detail.x+bitangent*detail.y+n*detail.z);
        }
    }
    if(kind>43.5&&kind<44.5)
    {
        float3 region=Land.Sample(TextureSampler,p.world.xz/(TerrainMap.zw-9)).rgb;
        float3 grass=Tile(p.world.xz*1.35,float2(0,0));
        float3 edge=region*(.48+grass*.72);float luma=dot(edge,float3(.299,.587,.114));edge=lerp(luma.xxx,edge,.72)*.9;
        float grit=Noise(p.world.xz*90)*.045;
        albedo=Decode(lerp(float3(.22,.215,.18)+grit,edge,smoothstep(.1,1,p.uv.x)));
        rough=lerp(.57,.94,p.uv.x);
    }
    if(kind>44.5&&kind<45.5)
    {
        float2 tiled=p.uv*float2(2,2);float2 uv=(float2(1,1)+.008+frac(tiled)*.984)*.5;
        float3 hide=Organic.SampleGrad(TextureSampler,uv,ddx(tiled)*.492,ddy(tiled)*.492).rgb;
        float grain=dot(hide,float3(.299,.587,.114));
        albedo=Decode(float3(.41,.34,.255)*(.64+grain*.55));rough=.95;
        n=ReliefNormal(p.world,n,grain,.0002*Settings.y);
    }
    if(kind>45.5&&kind<46.5)
    {
        float4 plant=Understory.Sample(TextureSampler,UnderstoryUV(p));clip(plant.a-.38);alpha=1;
        float3 tint=plant.rgb/max(plant.a,.001);float luminance=dot(tint,float3(.299,.587,.114));
        if(p.surface.w<.5)tint=lerp(tint,luminance*float3(.84,.95,.67),.52);
        tint=lerp(tint,luminance*float3(.95,.77,.45),SeasonPalette.y*.45);
        albedo=Decode(tint);rough=.93;ao=lerp(.65,1,saturate(1-p.uv.y));
        albedo=lerp(albedo,Decode(float3(.78,.83,.84)),SeasonPalette.x*.74);
        n=normalize(lerp(n,float3(0,1,0),.55));
    }
    float visibility=Visibility(p,n);
    if(kind>.5&&kind<1.5)
    {
        float2 w=p.world.xz;
        // Directional wave slopes are the analytic gradient of the same four wave bands.
        // They remain stable in world space rather than swimming with the camera.
        float time=EyeTime.w;float2 flow=0;float ripple=0;
        [unroll]for(int band=0;band<4;band++)
        {
            float angle=.42+band*1.73;float2 direction=float2(cos(angle),sin(angle));
            float frequency=2.7+band*4.9,phase=dot(w,direction)*frequency-time*(.65+band*.39);
            float amplitude=.032/(1+band*.8);flow+=direction*cos(phase)*amplitude;
            ripple+=sin(phase)/(4+band*2);
        }
        if(p.surface.w>1.5)
        {
            float current=Noise(float2(p.uv.x*12,p.uv.y*30-EyeTime.w*.55))-.5;
            float fine=Noise(float2(p.uv.x*90+3,p.uv.y*160-EyeTime.w*2))-.5;
            float2 slopes=float2(current*.08+fine*.035,(Noise(float2(p.uv.x*65+7,p.uv.y*110-EyeTime.w*1.4))-.5)*.08);
            float3 dp1=ddx(p.world),dp2=ddy(p.world);float2 uv1=ddx(p.uv),uv2=ddy(p.uv);float determinant=uv1.x*uv2.y-uv1.y*uv2.x;
            if(abs(determinant)>.00000001)
            {
                float2 across=normalize(((dp1*uv2.y-dp2*uv1.y)/determinant).xz);
                float2 along=normalize(((-dp1*uv2.x+dp2*uv1.x)/determinant).xz);
                flow=across*slopes.x+along*slopes.y;
            }
            ripple=current*.12+fine*.06;
        }
        n=normalize(float3(-flow.x,1,-flow.y));
        float fresnel=.025+.975*pow(1-saturate(dot(n,v)),5);float3 reflected=Sky(reflect(-v,n));
        if(p.surface.w<.5&&Settings.w>1.5)reflected=lerp(reflected,Reflection.Sample(TextureSampler,p.p.xy*Screen.xy+n.xz*.012).rgb,.78);
        float3 water=float3(.025,.10,.105)*(1+ripple*.06);
        if(VisualOptions.x>.5)
        {
            float depth=max(.005,p.world.y-GroundHeight(w));
            float3 bedColor=lerp(float3(.26,.24,.19),Tile(w*6,float2(1,0))*.62,.38);
            float3 bed=Decode(bedColor)*.68*exp(-depth*float3(4.8,3.0,2.2));
            water=lerp(bed,float3(.010,.040,.047),1-exp(-depth*18));
            water*=lerp(.55,1,visibility);
            float foam=(1-smoothstep(.005,.07,depth))*saturate(Noise(w*18+EyeTime.w*.08)*1.7-.5);
            float shoreBand=sin(depth*110-time*1.8+ripple*2);
            if(p.surface.w<1.5)foam=max(foam,smoothstep(.50,.94,shoreBand)*(1-smoothstep(.015,.16,depth))*.55);
            if(p.surface.w>1.5)foam*=.35;
            water=lerp(water,float3(.51,.59,.55),foam*.28);
        }
        float3 glint=Brdf(.02.xxx,n,v,sun,.14+WeatherSettings.x*.08,.0)*float3(4.6,3.9,2.8)*visibility;
        float3 ocean=lerp(water,reflected,fresnel*.85)+glint;
        float haze=1-exp(-length(EyeTime.xyz-p.world)*Settings.z*.7);
        return float4(lerp(ocean,float3(.49,.59,.65),min(.7,haze)),1);
    }
    float3 ambient=lerp(float3(.14,.16,.12),float3(.34,.40,.46),saturate(n.y*.5+.5));
    float3 direct=Brdf(albedo,n,v,sun,rough,metal)*float3(3.4,3.3,3.05)*visibility;
    float3 color=direct+albedo*ambient*ao;
    if(metal>.1)color+=Sky(reflect(-v,n))*lerp(.04.xxx,albedo,metal)*(.38*(1-rough*.65));
    if(kind>19.5&&kind<20.5)color+=albedo*pow(saturate(dot(-v,sun)),3)*.10;
    if((kind>4.5&&kind<5.5)||(kind>34.5&&kind<35.5)||(kind>45.5&&kind<46.5))color+=albedo*float3(.6,.62,.32)*pow(saturate(dot(-v,sun)),3)*.22;
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
        if(VisualOptions.z>.5&&depth<.99999)
        {
            float4 reconstructed=mul(float4(p.uv*float2(2,-2)+float2(-1,1),depth,1),InverseProjection);
            float3 center=reconstructed.xyz/reconstructed.w;
            float3 normal=normalize(cross(ddx(center),ddy(center)));if(dot(normal,EyeTime.xyz-center)<0)normal=-normal;
            float radius=clamp(90/max(.5,length(EyeTime.xyz-center)),3,24);
            [unroll]for(int i=0;i<12;i++)
            {
                float angle=i*2.399963;float2 uv=p.uv+float2(cos(angle),sin(angle))*Screen.xy*radius*sqrt((i+.5)/12);
                uv=clamp(uv,Screen.xy*.5,1-Screen.xy*.5);
                float other=SceneDepth.SampleLevel(TextureSampler,uv,0);
                float4 sampleWorld=mul(float4(uv*float2(2,-2)+float2(-1,1),other,1),InverseProjection);
                float3 delta=sampleWorld.xyz/sampleWorld.w-center;float len=length(delta);
                float horizon=saturate(dot(normal,delta)/max(.0001,len)-.10);
                occlusion+=horizon*(1-smoothstep(.04,.48,len))*step(other,.99999)*step(.002,len);
            }
        }
        color*=1-saturate(occlusion/12)*.72;
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
