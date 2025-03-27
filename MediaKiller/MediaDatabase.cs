using CxStudio.Core;
using CxStudio.FFmpegHelper;
using CxStudio.TUI;
using Microsoft.VisualBasic.FileIO;

namespace MediaKiller;

internal sealed class MediaDatabase
{
    private XEnv.Talker Talker { get; init; } = new("MediaDB");
    private static readonly Lazy<MediaDatabase> _instance = new(() => new MediaDatabase());
    public static MediaDatabase Instance => _instance.Value;

    public string? FFprobeBin { get; init; }

    private class Record
    {
        public required string HashCode { get; init; }
        public Time Duration { get; init; }
        public DateTime Created { get; init; }
        public DateTime LastUsed { get; set; }

        public string Compile()
        {
            return $"{HashCode},{Duration.TotalMilliseconds},{Created},{LastUsed}";
        }

        public static Record FromFields(IEnumerable<string> fields)
        {
            var f = fields.ToArray();
            return new Record
            {
                HashCode = f[0],
                Duration = new Time(long.Parse(f[1])),
                Created = DateTime.Parse(f[2]),
                LastUsed = DateTime.Parse(f[3])
            };
        }

        public bool IsExpirable() => LastUsed < DateTime.Now.AddDays(-7) || Created < DateTime.Now.AddDays(-30);

        public void UpdateLastUsed()
        {
            LastUsed = DateTime.Now;
        }
    }

    private Dictionary<string, Record> records = [];

    private MediaDatabase()
    {
        Talker.Whisper("正在初始化媒体信息库……");
        string table = XEnv.ConfigManaer.GetCacheFile("mediainfo.csv");
        if (File.Exists(table))
        {
            using var parser = new TextFieldParser(table) { Delimiters = new[] { "," } };
            while (!parser.EndOfData)
            {
                var fields = parser.ReadFields();
                if (fields is null || fields.Length < 5) continue;
                records.Add(fields[0], Record.FromFields(fields));
            }
        }

        Talker.Whisper($"已加载 {records.Count} 条记录。");

        FFprobeBin = XEnv.GetCommandPath("ffprobe");
        if (FFprobeBin is null)
            Talker.Whisper("未在系统范围内找到 ffprobe 程序。");
        else
            Talker.Whisper($"找到 FFprobe 位于：{FFprobeBin}");
    }

    public void SaveCaches()
    {
        string table = XEnv.ConfigManaer.GetCacheFile("mediainfo.csv");
        using var writer = new StreamWriter(table);
        bool expiring_enabled = records.Count > 3000;
        foreach (var r in records)
        {
            if (expiring_enabled && r.Value.IsExpirable())
                continue;
            writer.WriteLine(r.Value.Compile());
        }
        Talker.Whisper($"已保存 {records.Count} 条记录。");
    }

    public Time? GetDuration(string path)
    {
        var hash = path.GetHashCode().ToString();
        if (!records.ContainsKey(hash))
        {
            if (FFprobeBin is null)
                return null;

            Talker.Whisper("检测到 FFprobe: {0}", FFprobeBin);
            FFprobe prober = new(FFprobeBin);
            var duration = prober.GetFormatInfo(path)?.Duration;
            if (duration is null)
            {
                Talker.Whisper("无法获取 {0} 的时长信息。", path);
                return null;
            }
            else
            {
                Talker.Whisper("{0} 的时长：{1}", path, duration.Value.ToFormattedString());
            }

            records[hash] = new Record
            {
                HashCode = hash,
                Duration = (Time)duration,
                Created = DateTime.Now,
                LastUsed = DateTime.Now
            };
        }
        var r = records[hash];
        r.UpdateLastUsed();
        return r.Duration;
    }

    /*    public void SetDuration(string path, Time duration)
        {
            var r = new Record
            {
                HashCode = path.GetHashCode().ToString(),
                Duration = duration,
                Created = DateTime.Now,
                LastUsed = DateTime.Now
            };
            records[r.HashCode] = r;
        }*/
}
