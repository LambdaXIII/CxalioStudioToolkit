using CxStudio.FFmpegHelper;
using Spectre.Console;
namespace MediaKiller;

class MissionRunner
{
    public readonly Mission Mission;

    public MissionRunner(Mission mission)
    {
        Mission = mission;
    }

    public void Run()
    {
        foreach (var oGroup in Mission.Outputs)
        {
            string? folder = Path.GetDirectoryName(Path.GetFullPath(oGroup.FileName));
            if (folder is not null)
                Directory.CreateDirectory(folder);

        }
        var ffmpeg = new FFmpeg(Mission.FFmpegPath, Mission.CommandArgument);
        ffmpeg.CodingStatusChanged += (sender, status) =>
        {
            AnsiConsole.MarkupLine($"[red]{status.CurrentFrame}[/]");
        };

        ffmpeg.Run();
    }
}
