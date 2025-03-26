using CxStudio;
using CxStudio.TUI;
using Spectre.Console;

namespace MediaKiller;

internal sealed class MissionManager
{
    public readonly List<Mission> Missions = new List<Mission>();

    public readonly Dictionary<string, ulong> CompletedTargets = [];

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

    private void AddResults(Dictionary<string, ulong> results)
    {
        foreach (var r in results)
            CompletedTargets[r.Key] = r.Value;
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
                Thread.Sleep(100);

                jobCounter.Value = (uint)i + 1;
                Mission currentMission = Missions[i];

                MissionRunner runner = new(ref currentMission, ref jobCounter);

                totalTask.Description($"总体进度 {runner.PrettyNumber}");
                var currentProgressTask = ctx.AddTaskBefore(runner.PrettyName, totalTask);

                runner.ProgressUpdated += (sender, _) =>
                {
                    currentProgressTask
                    .IsIndeterminate(runner.CurrentTime <= 0)
                    .MaxValue(runner.MaxTime)
                    .Value(runner.CurrentTime);

                    string desc = runner.PrettyName;
                    if (runner.CurrentSpeed > 0)
                        desc += $" [grey][[{runner.CurrentSpeed:F2}x]][/]";

                    currentProgressTask.Description(desc);

                    totalTask.Value(completedTime + runner.CurrentTime);
                };

                var transcodingTask = runner.Start();

                while (!transcodingTask.IsCompleted)
                {
                    Thread.Sleep(100);
                    if (XEnv.Instance.GlobalCancellation.IsCancellationRequested)
                    {
                        runner.Cancel();
                        break;
                    }
                }

                transcodingTask.Wait();
                Dictionary<string, ulong> result = transcodingTask.Result;
                AddResults(result);

                if (XEnv.Instance.GlobalCancellation.IsCancellationRequested)
                {
                    AnsiConsole.MarkupLine("[red]取消后续任务……[/]");
                    break;
                }

                currentProgressTask.StopTask();
            } //for

            if (totalTask.StartTime is not null && totalTask.StopTime is not null)
            {
                var timeRange = totalTask.StopTime - totalTask.StartTime;
                AnsiConsole.MarkupLine("转码结束，总计耗时[yellow]{0}[/]", timeRange.Value.ToFormattedString());
            }

            var targetsCount = CompletedTargets.Count;
            if (targetsCount > 0)
            {
                ulong totalSize = CompletedTargets.Values.Aggregate((a, b) => a + b);
                FileSize size = FileSize.FromBytes(totalSize);
                AnsiConsole.MarkupLine("共生成 [yellow]{0}[/] 个目标文件，总计 [yellow]{1}[/] 。", targetsCount, size.ToString());
            }
        }); // process.start
    }//Run
}
