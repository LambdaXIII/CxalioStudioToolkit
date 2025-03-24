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
        double durationSeconds = Missions.Sum(mission => mission.Duration?.ToSeconds() ?? 1);
        return new(durationSeconds);
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
        double totalSeconds = GetTotalDuration().ToSeconds();
        int missionCount = Missions.Count();
        JobCounter jobCounter = new((uint)missionCount);
        var progress = AnsiConsole.Progress()
            .HideCompleted(true)
            .Columns(new ProgressColumn[] {
                                new TaskDescriptionColumn(),
                                new SpinnerColumn(Spinner.Known.Circle),
                                new ProgressBarColumn(),
                                new PercentageColumn(),
                                new RemainingTimeColumn(),
            });

        progress.Start(ctx =>
        {

            var totalTask = ctx.AddTask("总体进度").MaxValue(totalSeconds);
            totalTask.StartTask();


            for (int i = 0; i < missionCount; i++)
            {
                jobCounter.Value = (uint)i + 1;
                Mission currentMission = Missions[i];
                double? currentDuration = currentMission.Duration?.ToSeconds();

                string missionName = $"[grey][[{jobCounter.Format()}]][/] [cyan]{currentMission.Name}[/]";
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
                    currentTime = status.CurrentTime?.ToSeconds() ?? currentTime;
                    currentTask.Increment(currentTime);
                    totalTask.Increment(currentTime);
                };
                //TODO

            }



        });
    }
}
