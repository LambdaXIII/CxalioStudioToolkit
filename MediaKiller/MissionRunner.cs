﻿using CxStudio;
using CxStudio.FFmpegHelper;
using Spectre.Console;
namespace MediaKiller;

class MissionRunner : IDisposable
{
    public readonly Mission Mission;
    private ProgressTask? _pTask;
    private Time SourceDuration = new(1.0);
    private bool disposedValue;

    public MissionRunner(Mission mission, ProgressTask? task = null)
    {
        Mission = mission;
        _pTask = task;
        _pTask?.IsIndeterminate(true);
        XEnv.DebugMsg($"Init MissionRunner: {Mission.Source}");
    }


    private void UpdateProgress(object _, CodingStatus status)
    {
        XEnv.DebugMsg(status.StatusLine);

        if (_pTask is null)
            return;

        if (SourceDuration.ToSeconds() < 0)
        {
            _pTask?.IsIndeterminate(true);
            return;
        }

        if (status.CurrentSpeed > 0)
            _pTask.Description($"[cyan]{Mission.Name}[/][grey] [[{status.CurrentSpeed:F2}x]][/]");
        else
            _pTask.Description($"[cyan]{Mission.Name}[/]");

        _pTask?.IsIndeterminate(false);
        _pTask?.Value(status.CurrentTime?.ToSeconds() ?? -1);
    }

    private Time GetDuration(string source)
    {
        Time? dur = MediaDatabase.Instance.GetDuration(source);
        if (dur is null)
        {
            XEnv.DebugMsg($"Getting duration of {source} from FFprobe");

            var ffprobe = new FFprobe(Mission.Preset.FFprobePath);
            ffprobe.GetFormatInfo(source);
            dur = ffprobe.GetFormatInfo(source)?.Duration;
            if (dur is not null)
                MediaDatabase.Instance.SetDuration(source, (Time)dur);
        }
        XEnv.DebugMsg($"Duration of {source}: {dur?.ToSeconds()}s");
        return dur ?? new Time(-1.0);
    }

    public bool Run()
    {
        XEnv.DebugMsg($"开始执行任务: {Mission.Source}");
        SourceDuration = GetDuration(Mission.Source);

        _pTask?.MaxValue(SourceDuration.ToSeconds())
            .Description($"[cyan]{Mission.Name}[/]")
            .StartTask();

        foreach (var oGroup in Mission.Outputs)
        {
            string? folder = Path.GetDirectoryName(Path.GetFullPath(oGroup.FileName));
            if (folder is not null)
                Directory.CreateDirectory(folder);
        }
        var ffmpeg = new FFmpeg(Mission.FFmpegPath, Mission.CommandArgument);
        ffmpeg.CodingStatusChanged += UpdateProgress;

        using var task = Task.Run(() => ffmpeg.Run());
        while (!task.IsCompleted)
        {
            if (XEnv.Instance.WannaQuit)
                ffmpeg.Cancel();
            Thread.Sleep(100);
        }

        task.Wait();

        if (!task.Result)
            CleanUpTargets();

        //_pTask?.StopTask();
        return task.Result;
    }

    public void CleanUpTargets()
    {
        foreach (var oGroup in Mission.Outputs)
        {
            var name = oGroup.FileName;
            if (File.Exists(name))
            {
                File.Delete(name);
                AnsiConsole.MarkupLine("[red]已清除未完成的目标文件:[/] [cyan]{0}[/]", Path.GetFileName(name));
            }
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // TODO: 释放托管状态(托管对象)
                _pTask?.StopTask();
            }

            // TODO: 释放未托管的资源(未托管的对象)并重写终结器
            // TODO: 将大型字段设置为 null
            _pTask = null;
            disposedValue = true;
        }
    }

    // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
    // ~MissionRunner()
    // {
    //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
