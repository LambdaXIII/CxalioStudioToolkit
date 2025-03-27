using CxStudio.Core;
namespace MediaKiller;

struct Mission
{
    public string FFmpegPath;
    public string Source;

    public ArgumentGroup GlobalOptions;

    public List<ArgumentGroup> Inputs;
    public List<ArgumentGroup> Outputs;

    public required Preset Preset;

    public readonly string Name => Path.GetFileNameWithoutExtension(Source);

    public readonly Time? Duration => MediaDatabase.Instance.GetDuration(Source);

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
            cmd.Add(TextUtils.QuoteSpacedString(input.FileName));
        });

        Outputs.ForEach(Outputs =>
        {
            cmd.AddRange(Outputs.Arguments());
            cmd.Add(TextUtils.QuoteSpacedString(Outputs.FileName));
        });

        return cmd;
    }


    public readonly string CommandArgument => string.Join(' ', GetCommandElements());
    public readonly string FullCommand => $"{FFmpegPath} {CommandArgument}";


    public readonly Dictionary<string, ulong> GetTargetReport()
    {
        var result = new Dictionary<string, ulong>();

        foreach (var oGroup in Outputs)
        {
            var target = oGroup.FileName;
            if (File.Exists(target))
            {
                var size = (ulong)new FileInfo(target).Length;
                result.Add(target, size);
            }
        }

        return result;
    }

}
