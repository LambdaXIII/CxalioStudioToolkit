using System.Diagnostics;
using System.Text.RegularExpressions;

namespace CxStudio.FFmpegHelper;

public delegate void CodingStatusEventHandler(object sender, CodingStatus e);

public class FFmpeg
{
    private readonly string _ffmpegPath;
    private readonly string _ffmpegArguments;
    private DateTime _startTime;
    private CancellationTokenSource _cancelToken;

    public event CodingStatusEventHandler? CodingStatusChanged;

    private const string _statusPattern = @"frame=\s*(?<frame>\d+)\s+fps=\s*(?<fps>[\d.]+)\s+q=\s*(?<q>-?[\d.]+)\s+Lsize=\s*(?<Lsize>\d+\w+)\s+time=\s*(?<time>[\d:.]+)\s+bitrate=\s*(?<bitrate>[\d.]+[\w]+)/s\s+speed=\s*(?<speed>[\d.]+)\s*x";

    public FFmpeg(string ffmpeg_bin = "ffmpeg", string args = "")
    {
        _ffmpegPath = ffmpeg_bin;
        _ffmpegArguments = args;
        _startTime = DateTime.Now;
    }

    private ProcessStartInfo GetProcessStartInfo()
    {
        return new ProcessStartInfo
        {
            FileName = _ffmpegPath,
            Arguments = _ffmpegArguments,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = false
        };
    }

    private CodingStatus? ParseStatus(string? line)
    {
        if (line is null) return null;

        var match = Regex.Match(line, _statusPattern);
        if (!match.Success) return null;

        return new CodingStatus
        {
            FFmpegBin = _ffmpegPath,
            FfmpegArguments = _ffmpegArguments,
            ProcessStart = _startTime,
            CurrentFrame = ulong.Parse(match.Groups["frame"].Value),
            CurrentFps = double.Parse(match.Groups["fps"].Value),
            CurrentQ = double.Parse(match.Groups["q"].Value),
            CurrentLSize = FileSize.FromString(match.Groups["Lsize"].Value),
            CurrentTime = Time.FromTimestamp(match.Groups["time"].Value) ?? new(),
            CurrentBitrate = FileSize.FromString(match.Groups["bitrate"].Value),
            CurrentSpeed = double.Parse(match.Groups["speed"].Value)
        };
    }

    private void HandleOutput(object sender, DataReceivedEventArgs e)
    {
        var status = ParseStatus(e.Data);
        if (status is not null)
        {
            CodingStatusChanged?.Invoke(this, (CodingStatus)status);
        }
    }

    public void Run()
    {
        var process = new Process
        {
            StartInfo = GetProcessStartInfo(),
            EnableRaisingEvents = true
        };
        process.ErrorDataReceived += HandleOutput;
        process.OutputDataReceived += HandleOutput;

        process.Start();
        _startTime = DateTime.Now;

        process.BeginErrorReadLine();
        process.BeginOutputReadLine();

        process.WaitForExitAsync(_cancelToken.Token);
    }

    public void Cancel()
    {
        _cancelToken.Cancel();
    }
}
