using System.Text.RegularExpressions;

namespace CxStudio.FFmpegHelper;

public struct CodingStatus
{
    public string? FFmpegBin;
    public string? FfmpegArguments;
    public DateTime? ProcessStart;
    public ulong? CurrentFrame;
    public double? CurrentFps;
    public double? CurrentQ;
    public FileSize? CurrentSize;
    public Time? CurrentTime;
    public FileSize? CurrentBitrate;
    public double? CurrentSpeed;


    private const string FramePattern = @"frame=\s*(\d+)";
    private const string FpsPattern = @"fps=\s*([\d.]+)";
    private const string QPattern = @"q=\s*(-?[\d.]+)";
    private const string SizePattern = @"Lsize=\s*([\d.]+\w+)";
    private const string TimePattern = @"time=\s*([\d:.]+)";
    private const string BitratePattern = @"bitrate=\s*([\d.]+\w+/s)";
    private const string SpeedPattern = @"speed=\s*([\d.]+)x";

    public static CodingStatus FromStatusLine(string line)
    {
        CodingStatus result = new();

        var frameMatch = Regex.Match(line, FramePattern);
        if (frameMatch.Success)
            result.CurrentFrame = ulong.Parse(frameMatch.Groups[1].Value);

        var fpsMatch = Regex.Match(line, FpsPattern);
        if (fpsMatch.Success)
            result.CurrentFps = double.Parse(fpsMatch.Groups[1].Value);

        var qMatch = Regex.Match(line, QPattern);
        if (qMatch.Success)
            result.CurrentQ = double.Parse(qMatch.Groups[1].Value);

        var sizeMatch = Regex.Match(line, SizePattern);
        if (sizeMatch.Success)
            result.CurrentSize = FileSize.FromString(sizeMatch.Groups[1].Value);

        var timeMatch = Regex.Match(line, TimePattern);
        if (timeMatch.Success)
            result.CurrentTime = Time.FromTimestamp(timeMatch.Groups[1].Value);

        var bitrateMatch = Regex.Match(line, BitratePattern);
        if (bitrateMatch.Success)
            result.CurrentBitrate = FileSize.FromString(bitrateMatch.Groups[1].Value);

        var speedMatch = Regex.Match(line, SpeedPattern);
        if (speedMatch.Success)
            result.CurrentSpeed = double.Parse(speedMatch.Groups[1].Value);

        return result;
    }
}
