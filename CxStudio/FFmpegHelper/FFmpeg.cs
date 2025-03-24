using System.Diagnostics;
using System.Text;

namespace CxStudio.FFmpegHelper;

public delegate void CodingStatusEventHandler(object sender, CodingStatus e);

public class FFmpeg
{
    public readonly string FFmpegBin;
    public readonly string FFmpegArguments;
    private DateTime _startTime;

    public event CodingStatusEventHandler? CodingStatusChanged;

    private CancellationTokenSource _cancellation;

    public FFmpeg(string ffmpeg_bin = "ffmpeg", string args = "")
    {
        FFmpegBin = ffmpeg_bin;
        FFmpegArguments = args;
        _startTime = DateTime.Now;
        _cancellation = new();
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

        CodingStatusChanged?.Invoke(this, (CodingStatus)status);
    }

    public void Cancel()
    {
        _cancellation.Cancel();
    }

    public bool Run()
    {
        using var process = new Process
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

        return process.ExitCode == 0 && !_cancellation.IsCancellationRequested;
    }
}
