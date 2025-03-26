using CxStudio.FFmpegHelper;
using CxStudio.TUI;
using Spectre.Console;

namespace MediaKiller;

class MissionRunner
{
    public Mission Mission { get; init; }
    public JobCounter JobCounter { get; init; }

    private CancellationTokenSource _cts = new();
    public string PrettyNumber => $"[grey][[{JobCounter.Format()}]][/]";
    public string PrettyName => $"[cyan]{Name}[/]";

    public double CurrentSpeed { get; private set; } = 0;
    public double CurrentTime { get; private set; } = 0;
    public double MaxTime => Mission.Duration?.TotalSeconds ?? 1;
    public string Name => Mission.Name;

    public bool IsCancelled => _cts.IsCancellationRequested;

    public event EventHandler? Started;
    public event EventHandler? Finished;
    public event EventHandler? ProgressUpdated;

    private XEnv.Talker Talker { get; init; }

    public MissionRunner(ref Mission mission, ref JobCounter jobCounter)
    {
        Mission = mission;
        JobCounter = jobCounter;
        Talker = new(Name);
    }

    public void Cancel()
    {
        _cts.Cancel();
    }

    private void HandleCodingStatus(object sender, CodingStatus status)
    {
        CurrentTime = status.CurrentTime?.TotalSeconds ?? CurrentTime;
        CurrentSpeed = status.CurrentSpeed ?? CurrentSpeed;
        ProgressUpdated?.Invoke(this, EventArgs.Empty);
    }

    private void CleanUpTargets()
    {
        foreach (var oGroup in Mission.Outputs)
        {
            var file = oGroup.FileName;
            if (File.Exists(file))
            {
                File.Delete(file);
                AnsiConsole.MarkupLine($"[red]删除未完成的目标文件：[/] [cyan]{Path.GetFileName(file)}[/]");
            }
        }
    }

    private void CreateTargetFolders()
    {
        foreach (var oGroup in Mission.Outputs)
        {
            var folder = Path.GetDirectoryName(Path.GetFullPath(oGroup.FileName));
            if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
                var folderName = Path.GetRelativePath(Mission.Preset.TargetFolder, folder);
                if (string.IsNullOrEmpty(folderName))
                    folderName = Path.GetFileName(folder);

                AnsiConsole.MarkupLine($"新建目标文件夹:[yellow]{folderName}[/]");
            }
        }
    }

    public bool Run()
    {
        bool hasErr = false;

        var ffmpegBin = Mission.Preset.GetFFmpegBin();
        if (ffmpegBin is null)
        {
            AnsiConsole.MarkupLine($"{PrettyNumber}{PrettyName} [red]跳过[/] [grey](未找到可用的 FFmpeg)[/]");
            return false;
        }

        if (Mission.TargetConflicted)
        {
            AnsiConsole.MarkupLine($"{PrettyNumber}{PrettyName} [red]无法执行[/] [grey](目标与源文件冲突)[/]");
            return false;
        }

        if (!Mission.Overwrite && Mission.TargetExisted)
        {
            AnsiConsole.MarkupLine($"{PrettyNumber}{PrettyName} [yellow]跳过[/] [grey](目标文件已存在)[/]");
            return true;
        }

        CreateTargetFolders();

        var ffmpeg = new FFmpeg(ffmpegBin, Mission.CommandArgument);
        ffmpeg.CodingStatusChanged += HandleCodingStatus;

        Started?.Invoke(this, EventArgs.Empty);

        Task<bool> task = Task.Run(() => { return ffmpeg.Run(); });

        while (!task.IsCompleted)
        {
            Thread.Sleep(100);
            if (_cts.IsCancellationRequested)
            {
                ffmpeg.Cancel();
            }

        }

        try { task.Wait(); }
        catch
        {
            hasErr = true;
        }

        if (_cts.IsCancellationRequested)
        {
            AnsiConsole.MarkupLine($"{PrettyNumber}{PrettyName} [red]用户取消[/]");
        }
        else if (hasErr || !task.Result)
        {
            AnsiConsole.MarkupLine($"{PrettyNumber}{PrettyName} [red]失败[/] [grey](未知错误)[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"{PrettyNumber}{PrettyName} [green]完成[/]");
        }

        Finished?.Invoke(this, EventArgs.Empty);

        return (!hasErr) && (!IsCancelled) && (task.Result);
    }



    public Task<Dictionary<string, ulong>> Start()
    {
        var run = Task.Run(() => Run())
            .ContinueWith((p) =>
            {
                if (!p.Result)
                    CleanUpTargets();
                return Mission.GetTargetReport();
            });
        return run;
    }
}
