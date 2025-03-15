namespace CxStudio.FFmpegHelper;

public struct MediaFormatInfo
{
    public string FullPath { get; init; }
    public readonly string FileName => Path.GetFileName(FullPath);
    public uint StreamCount { get; init; }
    public string FormatName { get; init; }
    public string FormatLongName { get; init; }
    public Time StartTime { get; init; }
    public Time Duration { get; init; }
    public FileSize Size { get; init; }
    public FileSize Bitrate { get; init; }

    public Dictionary<string, string> Tags { get; init; }

    public readonly string? this[string key]
    {
        get => Tags[key];
        set
        {
            if (value is not null)
                Tags[key] = value;
        }
    }
}
