using CxStudio;

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
}
