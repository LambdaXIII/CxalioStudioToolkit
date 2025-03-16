using System.Diagnostics;
using System.Text;

namespace CxStudio.FFmpegHelper;

public delegate void CodingStatusEventHandler(object sender, CodingStatus e);

public class FFmpeg
{
    public readonly string FFmpegBin;
    public readonly string FFmpegArguments;
    private DateTime _startTime;
    private CancellationTokenSource _cancelToken;

    public event CodingStatusEventHandler? CodingStatusChanged;

    public FFmpeg(string ffmpeg_bin = "ffmpeg", string args = "")
    {
        FFmpegBin = ffmpeg_bin;
        FFmpegArguments = args;
        _startTime = DateTime.Now;
        _cancelToken = new();
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
            StandardErrorEncoding = Encoding.UTF8
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

        //await process.WaitForExitAsync(_cancelToken.Token);
        process.WaitForExit();
    }

    public void Cancel()
    {
        _cancelToken.Cancel();
    }
}
