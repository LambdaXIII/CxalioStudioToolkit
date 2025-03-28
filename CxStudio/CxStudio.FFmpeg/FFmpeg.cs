using CxStudio.Core;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace CxStudio.FFmpegHelper;

public delegate void CodingStatusEventHandler(CodingStatus e);

public class FFmpeg
{
    public readonly string FFmpegBin;
    public readonly string FFmpegArguments;

    private DateTime? _startTime;
    private DateTime? _endTime;
    private CodingStatus? _previousStatus;
    private CancellationTokenSource _cancellation = new();

    public event CodingStatusEventHandler? CodingStatusChanged;
    public event Action? Started;
    public event Action? Stopped;

    private readonly Mutex _mutex = new();

    public FFmpeg(string ffmpeg_bin = "ffmpeg", string args = "")
    {
        FFmpegBin = ffmpeg_bin;
        FFmpegArguments = args;

        Started += () =>
        {
            _startTime = DateTime.Now;
        };

        Stopped += () =>
        {
            _endTime = DateTime.Now;
            if (_previousStatus is CodingStatus s)
            {
                s.ProcessEnd = _endTime;
                CodingStatusChanged?.Invoke(s);
            }
        };
    }

    private ProcessStartInfo GetProcessStartInfo()
    {
        return new ProcessStartInfo
        {
            FileName = FFmpegBin,
            Arguments = FFmpegArguments,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            RedirectStandardInput = true
        };
    }

    private void HandleOutput(object sender, DataReceivedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Data))
            return;

        var status = CodingStatus.FromStatusLine(e.Data);
        status.FFmpegBin = FFmpegBin;
        status.FfmpegArguments = FFmpegArguments;
        status.ProcessStart = _startTime;

        _previousStatus = status;
        CodingStatusChanged?.Invoke((CodingStatus)status);
    }

    public void Cancel()
    {
        _cancellation.Cancel();
    }

    public bool Run()
    {
        _mutex.WaitOne();

        using var process = new Process
        {
            StartInfo = GetProcessStartInfo(),
            EnableRaisingEvents = true
        };
        process.ErrorDataReceived += HandleOutput;
        process.OutputDataReceived += HandleOutput;

        process.Start();

        Started?.Invoke();

        process.BeginErrorReadLine();
        process.BeginOutputReadLine();

        while (!process.HasExited)
        {
            if (_cancellation.Token.IsCancellationRequested)
            {
                process.StandardInput.Write("q");
                Thread.Sleep(1000);
                if (!process.HasExited)
                    process.Kill();
                break;
            }
            Thread.Sleep(100);
        }

        process.WaitForExit();

        Stopped?.Invoke();

        _mutex.ReleaseMutex();
        return process.ExitCode == 0 && !_cancellation.IsCancellationRequested;
    }

    public MediaFormatInfo? GetFormatInfo(string path)
    {
        MediaFormatInfo? result = null;
        string fullPath = Path.GetFullPath(path);
        _mutex.WaitOne();

        try
        {
            ProcessStartInfo pStartInfo = new()
            {
                FileName = FFmpegBin,
                ArgumentList = { "-hide_banner", "-i", fullPath },
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            Process process = new()
            {
                StartInfo = pStartInfo,
            };

            process.Start();
            process.WaitForExit(TimeSpan.FromSeconds(5));



            var lines =
                process.StandardError.ReadToEnd().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
                .Concat(
                    process.StandardOutput.ReadToEnd().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
                    ).ToArray();


            Dictionary<string, string> tags = [];
            uint streams = 0;
            Time? start = null;
            Time? duration = null;
            FileSize? bitrate = null;

            string durationPat = @"Duration: (\d\d:\d\d:\d\d.\d+)";
            string startPat = @"start: ([\d.]+)";
            string bitratePat = @"bitrate: ([\d.]+ \w\w/s)";
            string streamPat = @"Stream #\d+:\d+\.+: ";
            string dataPat = @"^\s+([^\s]+): ([^\s]+)\s*$";

            foreach (string line in lines)
            {

                var match = Regex.Match(line, dataPat);
                if (match.Success) tags[match.Groups[1].ToString()] = match.Groups[2].ToString();

                if (duration is null)
                {
                    match = Regex.Match(line, durationPat);
                    if (match.Success)
                        duration = Time.FromTimestamp(
                            match.Groups[1].ToString()
                            );
                }

                if (bitrate is null)
                {
                    match = Regex.Match(line, bitratePat);
                    if (match.Success)
                        bitrate = FileSize.FromString(
                            match.Groups[1].ToString());
                }

                if (start is null)
                {
                    match = Regex.Match(line, startPat);
                    if (match.Success)
                        start = Time.FromSeconds(
                            double.Parse(
                                match.Groups[1].ToString()));
                }

                match = Regex.Match(line, streamPat);
                if (match.Success) streams++;
            }

            result = new MediaFormatInfo
            {
                FullPath = fullPath,
                StreamCount = streams,
                StartTime = start ?? Time.Zero,
                Duration = duration ?? Time.Zero,
                Size = FileSize.FromFile(fullPath),
                Bitrate = bitrate ?? FileSize.Zero
            };

        }//try
        finally { _mutex.ReleaseMutex(); }
        return result;
    }//GetFormatInfo
}
