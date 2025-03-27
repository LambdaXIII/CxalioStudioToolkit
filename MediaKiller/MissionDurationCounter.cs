namespace MediaKiller;

internal sealed class MissionDurationCounter
{
    private List<Mission> Missions { get; init; }

    public double TotalSeconds { get; private set; } = 0;
    private int _finishedCount = 0;
    public int FinishedCount => _finishedCount;


    public MissionDurationCounter(List<Mission> missions)
    {
        Missions = missions;
    }

    public double Run()
    {
        var times = Missions.AsParallel()
            .Select(mission =>
            {
                double s = mission.Duration.TotalSeconds;
                Interlocked.Increment(ref _finishedCount);
                return s;
            }).ToList();

        TotalSeconds = times.Sum();
        return TotalSeconds;
    }

    public Task<double> Start()
    {
        return Task.Run(() => Run());
    }

}
