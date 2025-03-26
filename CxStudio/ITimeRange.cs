namespace CxStudio;


public interface ITimeRange
{
    public Time Start { get; }
    public Time Duration { get; }
    public Time End { get { return Start + Duration; } }

    public bool Contains(Time time)
    {
        return Start <= time && time < End;
    }

    public bool Intersects(ITimeRange other)
    {
        return Start <= other.End && other.Start <= End;
    }

}

public interface ITimeRangeEditingSupport : ITimeRange
{
    public new Time Start
    {
        get; set;
    }

    public new Time Duration
    {
        get; set;
    }

    public new Time End
    {
        get { return Start + Duration; }
        set { Duration = value - Start; }
    }

    public void Shift(Time delta)
    {
        Start += delta;
    }
}