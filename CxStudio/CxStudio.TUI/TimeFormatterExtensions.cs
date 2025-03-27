using CxStudio.Core;

namespace CxStudio.TUI;

public static class TimeFormatterExtensions
{
    public static string ToFormattedString(this TimeSpan timeSpan)
    {
        string result = string.Empty;

        if (timeSpan.Days > 0)
            result += $"{timeSpan.Days}日";

        if (timeSpan.Hours > 0)
            result += $"{timeSpan.Hours}小时";

        if (timeSpan.Minutes > 0)
            result += $"{timeSpan.Minutes}分";

        if (timeSpan.Seconds > 0 && timeSpan.Days <= 0)
            result += $"{timeSpan.Seconds}秒";

        if (timeSpan.Milliseconds > 0 && timeSpan.TotalSeconds <= 0)
            result += $"{timeSpan.Milliseconds}毫秒";

        if (timeSpan.TotalMilliseconds < 1)
            result = "0秒";

        return result;
    }

    public static string ToFormattedString(this Time time)
    {
        return TimeSpan.FromMilliseconds(time.TotalMilliseconds).ToFormattedString();
    }
}
