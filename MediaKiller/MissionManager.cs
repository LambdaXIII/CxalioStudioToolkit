using CxStudio.Core;
using CxStudio.TUI;
using Spectre.Console;
namespace MediaKiller;

internal sealed class MissionManager
{
    public readonly List<Mission> Missions = [];

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
            double totalSeconds = 0;
            DateTime startTime = DateTime.Now;

            var durationProgressTask = ctx.AddTask("[green]统计时长[/]").MaxValue(missionCount).IsIndeterminate(true);

            foreach (var m in Missions)
            {
                totalSeconds += m.Duration.TotalSeconds;
                durationProgressTask.Increment(1);
                if (XEnv.Instance.GlobalForceCancellation.IsCancellationRequested) return;
            }

            durationProgressTask.StopTask();


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

                totalTask.Description($"总体进度");
                var currentProgressTask = ctx.AddTaskBefore(runner.PrettyName, totalTask);

                runner.ProgressUpdated += (sender, _) =>
                {
                    string desc = $"{runner.PrettyNumber} {runner.PrettyName}";
                    if (runner.CurrentSpeed > 0)
                        desc += $" [grey][[{runner.CurrentSpeed:F2}x]][/]";
                    currentProgressTask
                        .Description(desc)
                        .IsIndeterminate(runner.CurrentTime <= 0)
                        .MaxValue(runner.MaxTime)
                        .Value(runner.CurrentTime);
                };

                var transcodingTask = runner.Start();
                Talker.Whisper("开始为任务 {0} 执行转码循环……", jobCounter.Value);

                bool wannaCancelCurrent = false;
                var cancelAction = () => { wannaCancelCurrent = true; };
                XEnv.Instance.UserCancelled += cancelAction;

                while (!transcodingTask.IsCompleted)
                {
                    Thread.Sleep(500);

                    if (wannaCancelCurrent || XEnv.Instance.GlobalForceCancellation.IsCancellationRequested)
                    {
                        Talker.Whisper("正在取消任务{0}……", jobCounter.Value);
                        runner.Cancel();
                        currentProgressTask.IsIndeterminate(true);
                        //transcodingTask.Wait(XEnv.Instance.GlobalForceCancellation.Token);
                        transcodingTask.Wait();
                        break;
                    }

                    var totalTaskCurrentTime = completedTime + runner.CurrentTime;
                    var totalTaskRealTime = (DateTime.Now - startTime).TotalSeconds;
                    var speed = totalTaskCurrentTime / totalTaskRealTime;
                    string totalDesc = "总体进度";
                    if (speed > 0)
                        totalDesc += $" [grey][[{speed:F2}x]][/]";
                    totalTask.Description(totalDesc).Value(totalTaskCurrentTime);
                }

                //transcodingTask.Wait();
                bool result = transcodingTask.Result;

                if (XEnv.Instance.GlobalForceCancellation.IsCancellationRequested)
                {
                    Talker.Say("[red]取消后续任务……[/]");
                    break;
                }

                currentProgressTask.StopTask();
                completedTime += runner.MaxTime;
                Talker.Whisper("任务 {0} 执行完毕。当前总体进度 {1} 秒。", jobCounter.Value, completedTime);
            } //for

            Talker.Whisper("全部任务遍历完毕。");

            var timeRange = DateTime.Now - startTime;
            Talker.Say("转码结束，用时 [yellow]{0}[/] 。", timeRange.ToFormattedString());
        }); // process.start

        Talker.Whisper("转码过程结束");
    }//Run
}
