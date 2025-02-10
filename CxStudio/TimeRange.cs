namespace CxStudio
{

    public interface TimeRangeSupport
    {
        public readonly Time Start { get }
        public readonly Time Duration { get }
        public readonly Time End { get { return Start + Duration; } }

        public bool Contains(Time time)
        {
            return Start <= time && time < End;
        }

        public bool Intersects(TimeRangeSupport other)
        {
            return Start <= other.End && other.Start <= End;
        }

    }

    public interface TimeRangeEditingSupport : TimeRangeSupport
    {
        public Time Start
        {
            get; set;
        }

        public Time Duration
        {
            get; set;
        }

        public Time End
        {
            get { return Start + Duration; }; set { Duration = value - Start; };
        }

        public void Shift(Time delta)
        {
            Start += delta;
        }
    }

}