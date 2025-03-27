using CxStudio.Core;
using CxStudio.FFmpegHelper;
using Microsoft.VisualBasic.FileIO;

namespace MediaKiller;

internal sealed class MediaDatabase
{
    private XEnv.Talker Talker { get; init; } = new("MediaDB");
    private static readonly Lazy<MediaDatabase> _instance = new(() => new MediaDatabase());
    public static MediaDatabase Instance => _instance.Value;

    public string? FFprobeBin { get; init; }

    public class Record
    {
        public required string FullPath { get; init; }
        public Time Duration { get; init; }
        public FileSize Size { get; init; }
        public DateTime Created { get; init; }
        public DateTime LastUsed { get; set; }

        public string Compile()
        {
            return $"{FullPath},{Duration.TotalMilliseconds},{Size.Bytes},{Created},{LastUsed}";
        }

        public static Record FromFields(IEnumerable<string> fields)
        {
            var f = fields.ToArray();
            return new Record
            {
                FullPath = f[0],
                Duration = new Time(long.Parse(f[1])),
                Size = FileSize.FromBytes(ulong.Parse(f[2])),
                Created = DateTime.Parse(f[3]),
                LastUsed = DateTime.Parse(f[4])
            };
        }

        public bool IsExpirable() => LastUsed < DateTime.Now.AddDays(-7) || Created < DateTime.Now.AddDays(-30);

        public void UpdateLastUsed()
        {
            LastUsed = DateTime.Now;
        }

        public override string ToString()
        {
            return $"{FullPath} {Duration.ToFormattedString()} {Size.FormattedString} {Created} -> {LastUsed}";
        }
    }

    private readonly Dictionary<string, Record> _records = [];

    private MediaDatabase()
    {
        Talker.Whisper("正在初始化媒体信息库……");
        string table = XEnv.ConfigManaer.GetCacheFile("mediainfo.csv");
        Talker.Whisper("纪录文件位于：{0}", table);

        if (File.Exists(table))
        {
            using var parser = new TextFieldParser(table) { Delimiters = new[] { "," } };
            while (!parser.EndOfData)
            {
                var fields = parser.ReadFields();
                if (fields is null || fields.Length < 5) continue;
                var r = Record.FromFields(fields);
                //Talker.Whisper("加载记录：{0}", r.ToString());
                _records.Add(r.FullPath, r);
            }
        }

        Talker.Whisper($"已加载 {_records.Count} 条记录。");

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
        bool expiring_enabled = _records.Count > 3000;
        int expiring_count = 0;
        foreach (var r in _records)
        {
            if (expiring_enabled && r.Value.IsExpirable())
            {
                //Talker.Whisper("删除过期记录：{0}", r.Value.ToString());
                expiring_count++;
                continue;
            }
            writer.WriteLine(r.Value.Compile());
        }
        if (expiring_count > 0)
            Talker.Whisper($"删除了 {expiring_count} 条过期记录。");
        Talker.Whisper($"已保存 {_records.Count} 条记录。");
    }

    public Record? GetRecord(string path)
    {
        if (!_records.ContainsKey(path))
        {
            using Mutex mutex = new(true, "MediaDBGetRecord");
            mutex.WaitOne();
            if (!_records.ContainsKey(path))
            {
                if (FFprobeBin is null)
                    return null;

                Talker.Whisper("未找到记录，正在获取信息……");

                FFprobe prober = new(FFprobeBin);
                var info = prober.GetFormatInfo(path);

                if (info is null)
                {
                    Talker.Whisper("获取文件信息失败！ {0}", path);
                    return null;
                }

                var new_record = new Record
                {
                    FullPath = path,
                    Duration = info.Value.Duration,
                    Size = info.Value.Size,
                    Created = DateTime.Now,
                    LastUsed = DateTime.Now
                };
                Talker.Whisper("新记录：{0}", new_record.ToString());
                _records[new_record.FullPath] = new_record;
            }
        }

        _records[path].UpdateLastUsed();
        return _records[path];
    }

    public Time? GetDuration(string path)
    {
        path = Path.GetFullPath(path);
        var r = GetRecord(path);
        return r?.Duration;
    }

    public FileSize? GetFileSize(string path)
    {
        path = Path.GetFullPath(path);
        var r = GetRecord(path);
        return r?.Size;
    }

    public void Clear()
    {
        _records.Clear();
    }
}
