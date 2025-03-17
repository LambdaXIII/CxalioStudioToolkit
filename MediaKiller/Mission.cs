using CxStudio;
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

    //public Mission(string source, Preset preset)
    //{
    //    Source = source;
    //    Preset = preset;
    //}

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
}
