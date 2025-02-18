namespace MediaKiller;

internal sealed class MissionMaker(Preset p)
{
    private readonly Preset _preset = p;

    public Mission Make(string input)
    {
        Mission mission = new()
        {
            FFmpegPath = _preset.FFmpegPath,
            GlobalOptions = _preset.GlobalOptions
        };

        if (_preset.HardwareAcceleration.Length > 0)
            mission.GlobalOptions.AddArgument("-hwaccel", _preset.HardwareAcceleration);

        mission.GlobalOptions.AddArgument(_preset.Overwrite ? "-y" : "-n");

        //TODO: Add custom values

        return mission;
    }
}
