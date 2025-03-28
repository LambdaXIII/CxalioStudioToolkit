using CxStudio.FFmpegHelper;
using CxStudio.TUI;

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
    public double MaxTime => Mission.Duration.TotalSeconds;
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
        Talker.Whisper("触发取消操作。");
    }

    private void HandleCodingStatus(CodingStatus status)
    {
        CurrentTime = (MaxTime > 1 && status.CurrentTime is not null)
            ? status.CurrentTime.Value.TotalSeconds
            : -1;
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
                Talker.Say($"[red]删除未完成的目标文件：[/] [cyan]{Path.GetFileName(file)}[/]");
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

                Talker.Say($"新建目标文件夹:[yellow]{folderName}[/]");
            }
        }
    }

    public bool Run()
    {
        Talker.Whisper("开始转码任务");

        bool hasErr = false;

        var ffmpegBin = Mission.Preset.GetFFmpegBin();
        //Talker.Whisper("FFmpeg 路径：{0}", ffmpegBin ?? "不可用");

        if (ffmpegBin is null)
        {
            Talker.Say($"{PrettyNumber}{PrettyName} [red]跳过[/] [grey](未找到可用的 FFmpeg)[/]");
            return false;
        }

        if (Mission.TargetConflicted)
        {
            Talker.Say($"{PrettyNumber}{PrettyName} [red]无法执行[/] [grey](目标与源文件冲突)[/]");
            return false;
        }

        if (!Mission.Overwrite && Mission.TargetExisted)
        {
            Talker.Say($"{PrettyNumber}{PrettyName} [yellow]跳过[/] [grey](目标文件已存在)[/]");
            return true;
        }

        Talker.Whisper("任务预检查通过");

        CreateTargetFolders();

        var ffmpeg = new FFmpeg(ffmpegBin, Mission.CommandArgument);
        ffmpeg.CodingStatusChanged += HandleCodingStatus;

        Started?.Invoke(this, EventArgs.Empty);

        Task<bool> task = Task.Run(() => { return ffmpeg.Run(); });
        Talker.Whisper("FFmpeg 进程已启动");

        while (!task.IsCompleted)
        {
            Thread.Sleep(100);
            if (_cts.IsCancellationRequested)
            {
                Talker.Whisper("接收到取消信号，正在取消任务……");
                ffmpeg.Cancel();
                break;
            }

        }

        try { task.Wait(); }
        catch
        {
            Talker.Whisper("检测到未定义的错误……");
            hasErr = true;
        }

        if (_cts.IsCancellationRequested)
        {
            Talker.Say($"{PrettyNumber} {PrettyName} [red]用户取消[/]");
        }
        else if (hasErr || !task.Result)
        {
            Talker.Say($"{PrettyNumber} {PrettyName} [red]失败[/] [grey](未知错误)[/]");
        }
        else
        {
            Talker.Say($"{PrettyNumber} {PrettyName} [green]完成[/]");
        }

        Finished?.Invoke(this, EventArgs.Empty);

        return (!hasErr) && (!IsCancelled) && (task.Result);
    }



    public Task<bool> Start()
    {
        var run = Task.Run(() => Run())
            .ContinueWith((p) =>
            {
                if (p.Result)
                    Mission.Targets.ForEach(path => { XEnv.Instance.MarkSucceededFile(path); });
                else
                    Mission.Targets.ForEach(path => { XEnv.Instance.MarkGarbage(path); });
                return p.Result;
            });
        return run;
    }
}
