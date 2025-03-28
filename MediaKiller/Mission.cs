using CxStudio.Core;
using CxStudio.FFmpegHelper;
namespace MediaKiller;

struct Mission
{
    public required string FFmpegPath { get; init; }
    public required string Source { get; init; }

    public ArgumentGroup GlobalOptions;

    public List<ArgumentGroup> Inputs;
    public List<ArgumentGroup> Outputs;

    public required Preset Preset;

    public readonly string Name => Path.GetFileNameWithoutExtension(Source);

    private static readonly SimpleCache<MediaFormatInfo> formatInfoCache = new();

    private Time? _duration;
    public Time Duration
    {
        get
        {
            _duration ??= MediaDB.Instance.GetDuration(Source) ?? Time.OneSecond;
            return _duration.Value;
        }
    }

    private FileSize? _sourceSize;
    public FileSize SourceSize
    {
        get { _sourceSize ??= MediaDB.Instance.GetFileSize(Source); return _sourceSize.Value; }
    }

    public readonly bool TargetConflicted
    {
        get
        {
            string s = Source;
            return Outputs.Any((output) => output.FileName == s);
        }
    }

    public readonly bool TargetExisted
    {
        get
        {
            return Outputs.Any((output) => File.Exists(output.FileName));
        }
    }

    public readonly bool Overwrite
    {
        get
        {
            if (XEnv.Instance.NoOverwrite) return false;
            if (XEnv.Instance.ForceOverwrite) return true;
            return Preset.Overwrite;
        }
    }

    public Mission()
    {
        FFmpegPath = "ffmpeg";
        Source = string.Empty;
        GlobalOptions = new();
        Inputs = [];
        Outputs = [];
    }


    public readonly List<string> GetCommandElements()
    {
        List<string> cmd =
        [
            .. GlobalOptions.Arguments(),
        ];

        cmd.Add(Overwrite ? "-y" : "-n");

        Inputs.ForEach((input) =>
        {
            cmd.AddRange(input.Arguments());
            cmd.Add("-i");
            cmd.Add(input.FileName.AutoQuote());
        });

        Outputs.ForEach(Outputs =>
        {
            cmd.AddRange(Outputs.Arguments());
            cmd.Add(Outputs.FileName.AutoQuote());
        });

        return cmd;
    }


    public readonly string CommandArgument => string.Join(' ', GetCommandElements());
    public readonly string FullCommand => $"{FFmpegPath} {CommandArgument}";


    public List<string> Targets => [.. Outputs.Select(oGroup => oGroup.FileName)];

}
