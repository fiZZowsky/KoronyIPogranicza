namespace Crown.Core;

/// <summary>Historical dates are captured, never recalculated from a later technological era.</summary>
public static class CampaignCalendar
{
    public static int CurrentYear(GameState s)
    {
        if(s.CalendarYears.TryGetValue(s.Turn,out int recorded))return recorded;
        int year=s.EpochCampaign?Epochs.WorldYear(s):-2400+(s.Turn-1)*15;
        var previous=s.CalendarYears.Where(p=>p.Key<s.Turn).DefaultIfEmpty().MaxBy(p=>p.Key);
        return previous.Key==0?year:Math.Max(year,previous.Value+s.Turn-previous.Key);
    }
    public static string Format(int year) => year < 0 ? $"{-year:N0} p.n.e." : $"{Math.Max(1,year):N0} n.e.";
    public static int Remember(GameState s)
    {
        if(s.CalendarYears.TryGetValue(s.Turn,out int year))return year;
        year=s.Year;
        if(s.CalendarYears.Count>0)year=Math.Max(year,s.CalendarYears.Values.Max());
        s.CalendarYears[s.Turn]=year;return year;
    }
    public static string At(GameState s,int? turn)
    {
        if(turn is null or <=0)return "brak daty";
        if(turn==s.Turn)return Format(s.Year);
        if(s.CalendarYears.TryGetValue(turn.Value,out int year))return Format(year);
        int Baseline(int t)=>s.EpochCampaign?Epochs.Year(t):-2400+(t-1)*15;
        // Older saves did not retain the year of each phase; label estimates explicitly.
        int estimate=turn>s.Turn?Math.Max(Baseline(turn.Value),s.Year+turn.Value-s.Turn):Baseline(turn.Value);
        return "ok. "+Format(estimate);
    }
    public static void Validate(GameState s)
    {
        if(s.CalendarYears==null||s.CalendarYears.Count>1601||s.CalendarYears.Any(p=>p.Key<1||p.Key>s.Turn||p.Value is < -40000 or > 1000000))
            throw new InvalidDataException("Nieprawidłowy kalendarz kampanii.");
        if(s.Version<27)
        {
            s.CalendarYears.Clear();Remember(s);
            string DateText(string text)=>System.Text.RegularExpressions.Regex.Replace(text,@"\bT(\d+)\b",m=>int.TryParse(m.Groups[1].Value,out int turn)?At(s,turn):m.Value);
            foreach(var e in s.Chronicle)e.Text=DateText(e.Text);
            foreach(var e in s.Deferred)e.Text=DateText(e.Text);
            foreach(var e in s.SpyOperations)e.Report=DateText(e.Report);
            foreach(var e in s.Demands)e.Reply=DateText(e.Reply);
            foreach(var e in s.PeaceOffers)e.Reply=DateText(e.Reply);
            foreach(var e in s.Treaties)e.Report=DateText(e.Report);
        }
        int previous=-40000;
        foreach(var date in s.CalendarYears.OrderBy(p=>p.Key)){if(date.Value<previous)throw new InvalidDataException("Kalendarz cofa lata historii.");previous=date.Value;}
    }
}
