using CxStudio;
using CxStudio.FFmpegHelper;
using CxStudio.TUI;
using Spectre.Console;

namespace MediaKiller;

internal sealed class MissionManager
{
    public readonly List<Mission> Missions = new List<Mission>();

    public MissionManager AddMission(Preset preset, string source)
    {
        var maker = new MissionMaker(preset);
        Missions.Add(maker.Make(source));
        return this;
    }

    public MissionManager AddMissions(Preset preset, IEnumerable<string> sources)
    {
        var maker = new MissionMaker(preset);
        foreach (var source in sources)
            Missions.Add(maker.Make(source));
        return this;
    }

    public MissionManager AddMissions(IEnumerable<Preset> presets, IEnumerable<string> sources)
    {
        foreach (Preset preset in presets)
            AddMissions(preset, sources);
        return this;
    }

    public MissionManager AddMissions(IEnumerable<Mission> missions)
    {
        Missions.AddRange(missions);
        return this;
    }

    public Time GetTotalDuration()
    {
        double durationSeconds = Missions.Sum(mission => mission.Duration?.TotalSeconds ?? 1);
        return Time.FromSeconds(durationSeconds);
    }

    public static void CreateTargetFolder(Mission mission)
    {
        foreach (var oGroup in mission.Outputs)
        {
            string? folder = Path.GetDirectoryName(Path.GetFullPath(oGroup.FileName));
            if (folder is null)
                continue;
            Directory.CreateDirectory(folder);
            XEnv.DebugMsg($"新建目标文件夹： {folder}");
        }
    }

    public void Run()
    {
        double totalSeconds = GetTotalDuration().TotalSeconds;
        int missionCount = Missions.Count();
        JobCounter jobCounter = new((uint)missionCount);

        var progress = AnsiConsole.Progress()
            .HideCompleted(true)
            .Columns(new ProgressColumn[] {
                                new TaskDescriptionColumn(),
                                new SpinnerColumn(new CxSpinner()),
                                new ProgressBarColumn(),
                                new PercentageColumn(),
                                new RemainingTimeColumn(),
            });

        progress.Start(ctx =>
        {
            double completedTime = 0;

            var totalTask = ctx.AddTask("总体进度").MaxValue(totalSeconds);
            totalTask.StartTask();


            for (int i = 0; i < missionCount; i++)
            {
                jobCounter.Value = (uint)i + 1;
                Mission currentMission = Missions[i];

                totalTask.Description($"总体进度 [grey][[{jobCounter.Format()}]][/]");

                double? currentDuration = currentMission.Duration?.TotalSeconds;

                string missionName = $"[cyan]{currentMission.Name}[/]";

                var currentTask = ctx.AddTaskBefore(missionName, totalTask);
                if (currentDuration is null)
                    currentTask.IsIndeterminate(true);
                else
                    currentTask
                    .IsIndeterminate(false)
                    .MaxValue(currentDuration.Value);

                var ffmpegBin = currentMission.Preset.GetFFmpegBin();
                if (ffmpegBin is null)
                {
                    AnsiConsole.MarkupLine("[red]未找到可用的 FFmpeg，任务跳过[/]");
                    currentTask.StopTask();
                    totalTask.Increment(currentDuration ?? 1);
                    continue;
                }

                var ffmpeg = new FFmpeg(ffmpegBin, currentMission.CommandArgument);
                double currentTime = 0;
                ffmpeg.CodingStatusChanged += (sender, status) =>
                {
                    currentTime = status.CurrentTime?.TotalSeconds ?? currentTime;
                    currentTask.Value(currentTime);
                    totalTask.Value(completedTime + currentTime);

                    string desc = status.CurrentSpeed is null ? missionName : $"{missionName} [grey][[{status.CurrentSpeed.Value:F2}x]][/]";
                    currentTask.Description(desc);

                };

                Task<bool> transcodingTask = Task.Run(() => { return ffmpeg.Run(); });
                while (!transcodingTask.IsCompleted)
                {
                    Thread.Sleep(100);
                    if (XEnv.Instance.GlobalCancellation.IsCancellationRequested)
                    {
                        ffmpeg.Cancel();
                        break;
                    }
                }

                transcodingTask.Wait();
                bool result = transcodingTask.Result;
                if (result)
                {
                    AnsiConsole.MarkupLine("{0} [green]已完成[/]", missionName);

                }
                else
                {
                    AnsiConsole.MarkupLine("{0} [red]失败[/]", missionName);
                    currentTask.IsIndeterminate(true);
                    foreach (var oGroup in currentMission.Outputs)
                    {
                        var name = oGroup.FileName;
                        if (File.Exists(name))
                        {
                            File.Delete(name);
                            AnsiConsole.MarkupLine("[red]已清除未完成的目标文件:[/] [cyan]{0}[/]", Path.GetFileName(name));
                        }
                    }
                }
                currentTask.StopTask();
                completedTime += currentDuration ?? 1;

                if (XEnv.Instance.GlobalCancellation.IsCancellationRequested)
                {
                    AnsiConsole.MarkupLine("[red]正在取消后续计划…[/]");
                    break;
                }

                Thread.Sleep(100);
            } //for

            //totalTask.StopTask();

            XEnv.DebugMsg("等待全部任务结束……");
            while (!ctx.IsFinished)
            {
                Thread.Sleep(100);
                if (XEnv.Instance.GlobalCancellation.IsCancellationRequested)
                    break;
            }

            if (totalTask.StartTime is not null && totalTask.StopTime is not null)
            {
                var timeRange = totalTask.StopTime - totalTask.StartTime;
                AnsiConsole.MarkupLine("转码结束，总计耗时[yellow]{0}[/]", timeRange.Value.ToFormattedString());
            }
        }); // process.start
    }//Run
}
