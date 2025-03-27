using CxStudio.Core;
using CxStudio.TUI;
using Spectre.Console;
namespace MediaKiller;

internal sealed class MissionManager
{
    public readonly List<Mission> Missions = [];

    public readonly Dictionary<string, ulong> CompletedTargets = [];

    private XEnv.Talker Talker { get; init; } = new("MissionManager");

    public MissionManager AddMission(Preset preset, string source)
    {
        var maker = new MissionMaker(preset);
        Missions.Add(maker.Make(source));
        return this;
    }

    public MissionManager AddMissions(Preset preset, IEnumerable<string> sources)
    {
        var maker = new MissionMaker(preset);

        SourceExpander expander = new(preset);
        Talker.Whisper("为预设 {0} 扩展源文件列表……", preset.Name);
        int count = 0;
        foreach (var source in expander.Expand(sources))
        {
            var mission = maker.Make(source);
            Talker.Whisper(
                "新任务 {0} : {1} INPUTS => {2} OUTPUTS",
                mission.Name,
                mission.Inputs.Count,
                mission.Outputs.Count
                );

            Missions.Add(mission);
            count++;
        }
        Talker.Say("为预设 [blue]{0}[/] 生成 [yellow]{1}[/] 个任务。", preset.Name, count);
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
        Talker.Whisper("批量添加 {0} 个任务", missions.Count());
        return this;
    }

    private void AddResults(Dictionary<string, ulong> results)
    {
        foreach (var r in results)
            CompletedTargets[r.Key] = r.Value;
        Talker.Whisper("添加 {0} 个目标文件记录，目前已有 {1} 条记录。", results.Count, CompletedTargets.Count);
    }

    public void Run()
    {
        Talker.Whisper("转码过程开始");

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
            Talker.Whisper("开始计算任务总时长……");
            var durationProgressTask = ctx.AddTask("[green]统计时长[/]").MaxValue(missionCount).IsIndeterminate(true);
            var durationCounter = new MissionDurationCounter(Missions);
            var durationTask = durationCounter.Start();
            while (!durationTask.IsCompleted)
            {
                Thread.Sleep(5);
                if (XEnv.Instance.GlobalCancellation.IsCancellationRequested)
                {
                    durationProgressTask.Description("[red]正在取消[/]").IsIndeterminate(true);
                    Thread.Sleep(2000);
                    durationTask.Wait(XEnv.Instance.GlobalCancellation.Token);
                    return;
                }
                durationProgressTask.IsIndeterminate(durationCounter.FinishedCount < 1).Value(durationCounter.FinishedCount);
            }
            durationTask.Wait();
            double totalSeconds = durationTask.Result;

            Talker.Whisper("共有 {0} 个任务，原始文件时长总计 {1} 秒。", missionCount, totalSeconds);

            double completedTime = 0;

            var totalTask = ctx.AddTask("总体进度").MaxValue(totalSeconds);
            totalTask.StartTask();


            for (int i = 0; i < missionCount; i++)
            {
                Thread.Sleep(100);

                jobCounter.Value = (uint)i + 1;
                Mission currentMission = Missions[i];

                Talker.Whisper("开始执行任务 {0}：{1}", i + 1, currentMission.Name);

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
                Talker.Whisper("开始为任务 {0} 执行转码循环……", jobCounter.Value);

                while (!transcodingTask.IsCompleted)
                {
                    Thread.Sleep(100);
                    if (XEnv.Instance.GlobalCancellation.IsCancellationRequested)
                    {
                        Talker.Whisper("接收到取消信号，正在取消任务 {0}……", jobCounter.Value);
                        runner.Cancel();
                        break;
                    }
                }

                transcodingTask.Wait();
                Dictionary<string, ulong> result = transcodingTask.Result;
                AddResults(result);

                if (XEnv.Instance.GlobalCancellation.IsCancellationRequested)
                {
                    Talker.Say("[red]取消后续任务……[/]");
                    break;
                }

                currentProgressTask.StopTask();
                completedTime += runner.MaxTime;
                Talker.Whisper("任务 {0} 执行完毕。当前总体进度 {1} 秒。", jobCounter.Value, completedTime);
            } //for

            Talker.Whisper("全部任务遍历完毕。");

            if (totalTask.StartTime is not null)
            {
                var timeRange = DateTime.Now - totalTask.StartTime;
                Talker.Say("转码结束，用时 [yellow]{0}[/] 。", timeRange.Value.ToFormattedString());
            }

            var targetsCount = CompletedTargets.Count;
            if (targetsCount > 0)
            {
                ulong totalSize = CompletedTargets.Values.Aggregate((a, b) => a + b);
                FileSize size = FileSize.FromBytes(totalSize);
                Talker.Say("共生成 [yellow]{0}[/] 个目标文件，总计 [yellow]{1}[/] 。", targetsCount, size.FormattedString);
            }
        }); // process.start

        Talker.Whisper("转码过程结束");
    }//Run
}
