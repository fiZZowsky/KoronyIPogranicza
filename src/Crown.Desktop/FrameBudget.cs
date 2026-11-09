namespace Crown.Desktop;

// CPU + GPU + readback time, not a claim about GPU-only timings or temperature.
internal sealed class FrameBudget
{
    internal double Scale {get;private set;}=1;
    internal double SmoothedMilliseconds {get;private set;}
    private int samples,cooldown;
    internal void Observe(double milliseconds,int targetFps)
    {
        if(!double.IsFinite(milliseconds)||milliseconds<=0)return;
        SmoothedMilliseconds=samples++==0?milliseconds:SmoothedMilliseconds*.85+milliseconds*.15;
        if(++cooldown<16)return;cooldown=0;double budget=1000d/Math.Clamp(targetFps,10,60);
        if(SmoothedMilliseconds>budget*1.15)Scale=Math.Max(.5,Scale-.125);
        else if(SmoothedMilliseconds<budget*.60)Scale=Math.Min(1,Scale+.125);
    }
    internal void Reset(){Scale=1;SmoothedMilliseconds=0;samples=cooldown=0;}
}
