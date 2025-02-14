using System.Text.RegularExpressions;

namespace CxStudio
{
    public readonly struct Time
    {
        readonly long _ms = 0;
        readonly static string _timecodePattern = @"(\d{2}):(\d{2}):(\d{2})[;:](\d{2})";
        readonly static string _timestampPattern = @"(\d{2}):(\d{2}):(\d{2})[;:,.](\d{2,3})";

        public Time(double seconds)
        {
            _ms = (long)(Math.Round(seconds * 1000));
        }

        public Time(long ms)
        {
            _ms = ms;
        }

        public double ToSeconds()
        {
            return Math.Round(_ms / 1000.0, 2);
        }

        public long ToMilliseconds()
        {
            return _ms;
        }

        public static Time? FromTimecode(string timecode, Timebase timebase)
        {
            if (Regex.IsMatch(timecode, _timecodePattern))
            {
                var match = Regex.Match(timecode, _timecodePattern);
                var hours = int.Parse(match.Groups[1].Value);
                var minutes = int.Parse(match.Groups[2].Value);
                var seconds = int.Parse(match.Groups[3].Value);
                var milliseconds = int.Parse(match.Groups[4].Value) * timebase.MillisecondsPerFrame;
                return new Time(hours * 3600 + minutes * 60 + seconds + milliseconds / 1000.0);
            }
            return null;
        }

        public static Time? FromTimestamp(string timestamp)
        {
            if (Regex.IsMatch(timestamp, _timestampPattern))
            {
                var match = Regex.Match(timestamp, _timestampPattern);
                var hours = int.Parse(match.Groups[1].Value);
                var minutes = int.Parse(match.Groups[2].Value);
                var seconds = int.Parse(match.Groups[3].Value);
                var milliseconds = int.Parse(match.Groups[4].Value);
                return new Time(hours * 3600 + minutes * 60 + seconds + milliseconds / 1000.0);
            }
            return null;
        }

        public string ToTimecode(Timebase timebase)
        {
            var hours = _ms / 3600000;
            var minutes = (_ms % 3600000) / 60000;
            var seconds = (_ms % 60000) / 1000;
            var frames = (_ms % 1000) / timebase.MillisecondsPerFrame;
            return $"{hours:D2}:{minutes:D2}:{seconds:D2};{frames:D2}";
        }

        public string ToTimestamp()
        {
            var hours = _ms / 3600000;
            var minutes = (_ms % 3600000) / 60000;
            var seconds = (_ms % 60000) / 1000;
            var milliseconds = _ms % 1000;
            return $"{hours:D2}:{minutes:D2}:{seconds:D2}.{milliseconds:D3}";
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
                return t == this;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine("time", _ms);
        }
    }
}
