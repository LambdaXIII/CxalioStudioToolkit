using CxStudio
namespace MediaKiller;

struct Mission
{
    public string FFmpegPath = string.Empty;

    public ArgumentGroup GlobalOptions = new();

    public List<ArgumentGroup> Inputs = [];
    public List<ArgumentGroup> Outputs = [];

    public Mission() { }

    public readonly List<string> CommandElements()
    {
        List<string> cmd =
        [
            FFmpegPath,
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

    public readonly string FullCommand()
    {
        return string.Join(' ', CommandElements());
    }
}
