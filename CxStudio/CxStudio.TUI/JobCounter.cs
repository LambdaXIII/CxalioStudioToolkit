namespace CxStudio.TUI;

public class JobCounter
{
    public uint MaxValue { get; init; }
    public uint MinValue { get; init; }

    private uint rawValue;

    public uint Value
    {
        get
        {
            if (rawValue < MinValue) return MinValue;
            if (rawValue > MaxValue) return MaxValue; return rawValue;
        }
        set { rawValue = value; }
    }

    public JobCounter(uint maxValue, uint minValue = 1)
    {
        MaxValue = maxValue;
        MinValue = minValue;
        rawValue = minValue;
    }

    public uint Increase(uint value = 1)
    {
        Value += 1;
        return rawValue;
    }

    public uint Decrease(uint value = 1)
    {
        Value -= value;
        return rawValue;
    }

    public uint Reset()
    {
        Value = MinValue;
        return Value;
    }

    public string Format(string pattern = "{0}/{1}")
    {
        string max = MaxValue.ToString();
        int digits = max.Length;
        string value = Value.ToString().PadLeft(digits, ' ');
        return string.Format(pattern, value, max);
    }

}
