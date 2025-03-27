using System.Text.RegularExpressions;

namespace CxStudio.Core;

public readonly struct Time(long ms)
{
    private readonly long _ms = ms;
    private readonly static string _timecodePattern = @"(\d{2}):(\d{2}):(\d{2})[;:](\d+)";
    private readonly static string _timestampPattern = @"(\d{2}):(\d{2}):(\d{2})[;:,.](\d{1,3})";

    public long TotalMilliseconds => _ms;
    public double TotalSeconds => _ms / 1000.0;
    public double TotalMinutes => _ms / 60000.0;
    public double TotalHours => _ms / 3600000.0;
    public double TotalDays => _ms / 86400000.0;


    public static Time FromMilliseconds(long ms)
    {
        return new Time(ms);
    }

    public static Time FromSeconds(double seconds)
    {
        return new Time((long)Math.Round(seconds * 1000));
    }

    public static Time FromMinutes(double minutes)
    {
        return new Time((long)Math.Round(minutes * 60000));
    }

    public static Time FromHours(double hours)
    {
        return new Time((long)Math.Round(hours * 3600000));
    }

    public static Time FromDays(double days)
    {
        return new Time((long)Math.Round(days * 86400000));
    }

    public ushort MilliSeconds => (ushort)(_ms % 1000);
    public ushort Seconds => (ushort)(_ms / 1000 % 60);
    public ushort Minutes => (ushort)(_ms / 60000 % 60);
    public ushort Hours => (ushort)(_ms / 3600000 % 24);
    public ushort Days => (ushort)(_ms / 86400000);

    public readonly string ToTimecode(Timebase timebase)
    {
        ushort frames = (ushort)Math.Round(MilliSeconds / 1000.0 * timebase.framerate);
        string framesStr = frames.ToString().PadLeft(timebase.framerate.ToString().Length, '0');
        var sep = timebase.dropframe ? ";" : ":";
        return $"{Hours:D2}:{Minutes:D2}:{Seconds:D2}{sep}{framesStr}";
    }

    public static Time? FromTimecode(string timecode, Timebase timebase)
    {
        if (Regex.IsMatch(timecode, _timecodePattern))
        {
            var match = Regex.Match(timecode, _timecodePattern);
            var hours = int.Parse(match.Groups[1].Value);
            var minutes = int.Parse(match.Groups[2].Value);
            var seconds = int.Parse(match.Groups[3].Value);
            var frames = int.Parse(match.Groups[4].Value);

            long ms1 = hours * 60 * 60 * 1000;
            long ms2 = minutes * 60 * 1000;
            long ms3 = seconds * 1000;
            long ms4 = (long)Math.Round(frames / (double)timebase.framerate * 1000);


            long total_ms = ms1 + ms2 + ms3 + ms4;
            return new Time(total_ms);
        }
        return null;
    }

    public string ToTimestamp()
    {
        return $"{Hours:D2}:{Minutes:D2}:{Seconds:D2}.{MilliSeconds:D3}";
    }

    public static Time? FromTimestamp(string timestamp)
    {
        if (Regex.IsMatch(timestamp, _timestampPattern))
        {
            var match = Regex.Match(timestamp, _timestampPattern);

            var hours = int.Parse(match.Groups[1].Value);
            var minutes = int.Parse(match.Groups[2].Value);
            var secondsStr = $"{match.Groups[3].Value}.{match.Groups[4].Value}";

            long ms1 = hours * 60 * 60 * 1000;
            long ms2 = minutes * 60 * 1000;
            long ms3 = (long)Math.Round(double.Parse(secondsStr) * 1000);

            long total_ms = ms1 + ms2 + ms3;
            return new Time(total_ms);
        }
        return null;
    }

    public static Time operator +(Time left, Time right)
    {
        return new Time(left._ms + right._ms);
    }

    public static Time operator -(Time left, Time right)
    {
        return new Time(left._ms - right._ms);
    }

    public static Time operator *(Time left, double right)
    {
        return new Time((long)(left._ms * right));
    }

    public static Time operator /(Time left, double right)
    {
        return new Time((long)(left._ms / right));
    }

    public static bool operator ==(Time left, Time right)
    {
        return left._ms == right._ms;
    }

    public static bool operator !=(Time left, Time right)
    {
        return left._ms != right._ms;
    }

    public static bool operator <(Time left, Time right)
    {
        return left._ms < right._ms;
    }

    public static bool operator >(Time left, Time right)
    {
        return left._ms > right._ms;
    }

    public static bool operator <=(Time left, Time right)
    {
        return left._ms <= right._ms;
    }

    public static bool operator >=(Time left, Time right)
    {
        return left._ms >= right._ms;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (obj is Time t)
        {
            return t == this || t._ms == _ms;
        }

        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine("time", _ms);
    }
}
