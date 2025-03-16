using CxStudio;
using CxStudio.FFmpegHelper;
using Spectre.Console;
namespace MediaKiller;

class MissionRunner
{
    public readonly Mission Mission;
    private ProgressTask? _pTask;
    private Time SourceDuration = new(-1.0);

    public MissionRunner(Mission mission, ProgressTask? task = null)
    {
        Mission = mission;
        _pTask = task;
    }

    private void UpdateProgress(object _, CodingStatus status)
    {
        if (_pTask is null)
            return;

        if (SourceDuration.ToSeconds() < 0)
        {
            _pTask.Value = -1;
            return;
        }

        double progress = status.CurrentTime?.ToSeconds() ?? -1.0 / SourceDuration.ToSeconds();
        _pTask.Value(progress);
    }

    private Time GetDuration(string source)
    {
        Time? dur = MediaDatabase.Instance.GetDuration(source);
        if (dur is null)
        {
            var ffprobe = new FFprobe(Mission.Preset.FFprobePath);
            ffprobe.GetFormatInfo(source);
            dur = ffprobe.GetFormatInfo(source)?.Duration;
            if (dur is not null)
                MediaDatabase.Instance.SetDuration(source, (Time)dur);
        }
        return dur ?? new Time(-1.0);
    }

    public void Run()
    {
        SourceDuration = GetDuration(Mission.Source);

        foreach (var oGroup in Mission.Outputs)
        {
            string? folder = Path.GetDirectoryName(Path.GetFullPath(oGroup.FileName));
            if (folder is not null)
                Directory.CreateDirectory(folder);

        }
        var ffmpeg = new FFmpeg(Mission.FFmpegPath, Mission.CommandArgument);
        ffmpeg.CodingStatusChanged += UpdateProgress;

        ffmpeg.Run();
    }
}
