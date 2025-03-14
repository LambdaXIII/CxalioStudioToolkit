using CxStudio;
using Microsoft.VisualBasic.FileIO;

namespace MediaKiller;

internal sealed class MediaDatabase
{
    private static readonly Lazy<MediaDatabase> _instance = new(() => new MediaDatabase());
    public static MediaDatabase Instance => _instance.Value;

    private class Record
    {
        public required string HashCode { get; init; }
        public Time Duration { get; init; }
        public DateTime Created { get; init; }
        public DateTime LastUsed { get; set; }

        public string Compile()
        {
            return $"{HashCode},{Duration.ToMilliseconds()},{Created},{LastUsed}";
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
    }

    ~MediaDatabase()
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
    }

    public Time? GetDuration(string path)
    {
        var hash = path.GetHashCode().ToString();
        if (!records.ContainsKey(hash))
            return null;
        var r = records[hash];
        r.UpdateLastUsed();
        return r.Duration;
    }

    public void SetDuration(string path, Time duration)
    {
        var r = new Record
        {
            HashCode = path.GetHashCode().ToString(),
            Duration = duration,
            Created = DateTime.Now,
            LastUsed = DateTime.Now
        };
        records[r.HashCode] = r;
    }
}
