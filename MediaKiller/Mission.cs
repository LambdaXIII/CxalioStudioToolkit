namespace MediaKiller;

sealed class Mission
{
    public string FFmpegPath = string.Empty;

    public ArgumentGroup GlobalOptions = new();

    public List<ArgumentGroup> Inputs = [];
    public List<ArgumentGroup> Outputs = [];

    public List<string> CommandElements()
    {
        List<string> cmd =
        [
            FFmpegPath,
            .. GlobalOptions.Arguments(),
        ];
        Inputs.ForEach((input) => { cmd.AddRange(input.Arguments()); cmd.Add("-i"); cmd.Add(input.FileName ?? ""); });
        Outputs.ForEach(Outputs => { cmd.AddRange(Outputs.Arguments()); cmd.Add(Outputs.FileName ?? ""); });
        return cmd;
    }

    public string CompileFullCommand()
    {
        return string.Join(' ', CommandElements());
    }
}
