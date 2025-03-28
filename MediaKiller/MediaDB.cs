using CxStudio.Core;
using CxStudio.FFmpegHelper;
using System.Security.Cryptography;
using System.Text;

namespace MediaKiller;

internal class MediaDB
{
    private class Record
    {
        public string Key { get; private set; }
        public Time Duration { get; private set; }
        public FileSize Size { get; private set; }
        public DateTime Created { get; private set; }
        public DateTime LastUsed { get; private set; }
        private Mutex m_mutex = new();
        public Record(string key, Time duration, FileSize size)
        {
            Key = key;
            Duration = duration;
            Size = size;
            Created = DateTime.Now;
            LastUsed = DateTime.Now;
        }
        public Record(IEnumerable<string> fields)
        {
            if (fields.Count() < 5)
                throw new ArgumentException("Invalid field count");

            Key = fields.ElementAt(0).AutoUnquote();
            Duration = Time.FromSeconds(double.Parse(fields.ElementAt(1)));
            Size = FileSize.FromBytes(ulong.Parse(fields.ElementAt(2)));
            Created = DateTime.Parse(fields.ElementAt(3));
            LastUsed = DateTime.Parse(fields.ElementAt(4));
        }
        public Record(MediaFormatInfo formatInfo)
        {
            Key = MakeKey(formatInfo.FullPath);
            Duration = formatInfo.Duration;
            Size = formatInfo.Size;
            Created = DateTime.Now;
            LastUsed = DateTime.Now;
        }
        public void UpdateLastUsed()
        {
            m_mutex.WaitOne();
            LastUsed = DateTime.Now;
            m_mutex.ReleaseMutex();
        }
        public bool IsExpirable => LastUsed < DateTime.Now.AddDays(-7) || Created < DateTime.Now.AddDays(-30);
        public List<string> Fields => [
            Key.AutoQuote(","),
            Duration.TotalSeconds.ToString(),
            Size.Bytes.ToString(),
            Created.ToString(),
            LastUsed.ToString()
            ];
        public override string ToString() => $"({Key}) {Duration.ToFormattedString()} {Size.FormattedString} {Created} -> {LastUsed}";
    }


    private readonly XEnv.Talker Talker = new("MediaDB");
    private readonly Dictionary<string, Record> Database = [];
    private readonly Mutex DataMutex = new();

    private readonly Dictionary<string, Task<Record?>> TaskBase = [];
    private readonly Mutex TaskBaseMutex = new();

    private readonly HashSet<string> FailedSources = [];
    private readonly Mutex FailedSourcesMutex = new();

    //public readonly string? FFprobeBin = XEnv.GetCommandPath("ffprobe");
    public readonly string? FFmpegBin = XEnv.GetCommandPath("ffmpeg");

    private readonly string SaveFilePath;

    private static readonly Dictionary<string, string> KeyCaches = [];
    private static string MakeKey(string path)
    {
        if (!KeyCaches.ContainsKey(path))
        {
            using SHA256 sha = SHA256.Create();
            byte[] hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(path));
            StringBuilder hexString = new StringBuilder(hashBytes.Length * 2);
            foreach (byte b in hashBytes)
            {
                hexString.AppendFormat("{0:x2}", b);
            }
            KeyCaches[path] = hexString.ToString();
        }
        return KeyCaches[path];
    }

    private void AddRecord(Record record)
    {
        DataMutex.WaitOne();
        Database[record.Key] = record;
        Talker.Whisper("新纪录:{0}", record.ToString());
        DataMutex.ReleaseMutex();
    }

    private void MarkFailed(string key)
    {
        FailedSourcesMutex.WaitOne();
        FailedSources.Add(key);
        FailedSourcesMutex.ReleaseMutex();
    }

    private Record? GetRecordBG(string path)
    {
        string key = MakeKey(path);

        if (FFmpegBin is null)
            return null;
        if (FailedSources.Contains(path))
            return null;

        Database.TryGetValue(key, out Record? result);
        if (result is null)
        {
            Talker.Whisper("开始解析源文件信息：{0}", path);
            //FFprobe ffprobe = new(FFprobeBin);
            //var minfo = ffprobe.GetFormatInfo(path);
            FFmpeg ffmpeg = new(FFmpegBin);
            var minfo = ffmpeg.GetFormatInfo(path);
            if (minfo is null)
            {
                Talker.Whisper("文件解析失败：{0}", path);
                MarkFailed(path);
            }
            else
            {
                result = new Record(minfo.Value);
                AddRecord(result);
            }
        }
        return result;
    }

    private Record? GetRecord(string path)
    {
        Database.TryGetValue(MakeKey(path), out Record? result);
        if (result is Record x)
            return x;

        TaskBase.TryGetValue(path, out var getter);

        if (getter is null)
        {
            TaskBaseMutex.WaitOne();
            if (!TaskBase.ContainsKey(path))
            {
                var task = Task.Run(() => { return GetRecordBG(path); });
                TaskBase.Add(path, task);
                getter = task;
            }
            TaskBaseMutex.ReleaseMutex();
        }

        getter?.Wait();
        result = getter?.Result;
        result?.UpdateLastUsed();
        return result;
    }

    public Time? GetDuration(string path)
    {
        Record? record = GetRecord(path);
        return record?.Duration;
    }

    public FileSize? GetFileSize(string path)
    {
        Record? record = GetRecord(path);
        return record?.Size;
    }

    private void LoadRecords(string saveFilePath)
    {
        if (!File.Exists(saveFilePath))
            return;

        DataMutex.WaitOne();

        int count = 0;

        foreach (var line in File.ReadAllLines(saveFilePath))
        {
            var fields = line.Split(',');
            if (fields.Length < 5)
                continue;
            Record record = new(fields);
            Database[record.Key] = record;
            count++;
        }

        DataMutex.ReleaseMutex();

        Talker.Whisper("从 {0} 加载了 {1} 条媒体信息。", saveFilePath, count);
    }

    public void SaveRecords(string? saveFilePath = null)
    {
        saveFilePath ??= SaveFilePath;

        DataMutex.WaitOne();

        List<Record> toBeSaved = [
           .. Database.Values
                .Where(r => !r.IsExpirable && !FailedSources.Contains(r.Key))
                .OrderByDescending(r => r.LastUsed)
                .Take(3000)
            ];

        DataMutex.ReleaseMutex();

        using StreamWriter writer = new(saveFilePath);

        foreach (var record in toBeSaved)
            writer.WriteLine(string.Join(",", record.Fields));

        Talker.Say("Saved {0} records.", toBeSaved.Count);
        int deltaCount = Database.Count - toBeSaved.Count;
        if (deltaCount > 0)
            Talker.Say("Deleted {0} expired records.", deltaCount);

        if (FailedSources.Count > 0)
        {
            Talker.Say("Failed to get info for {0} files.", FailedSources.Count);
            Talker.Whisper("包含以下文件：");
            foreach (string source in FailedSources)
                Talker.Whisper("\t{0}", source);
        }
    }

    public void ClearCaches()
    {
        DataMutex.WaitOne();
        Database.Clear();
        DataMutex.ReleaseMutex();
    }

    private MediaDB()
    {
        Talker.Whisper("正在初始化媒体信息库……");
        SaveFilePath = XEnv.ConfigManaer.GetCacheFile("mediainfo.csv");
        Talker.Whisper("纪录文件位于：{0}", SaveFilePath);
        LoadRecords(SaveFilePath);
        Talker.Whisper($"已加载 {Database.Count} 条记录。");
    }

    private static readonly Lazy<MediaDB> _instance = new(() => new MediaDB());
    public static MediaDB Instance => _instance.Value;
}
