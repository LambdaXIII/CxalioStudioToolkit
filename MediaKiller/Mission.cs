namespace MediaKiller;

sealed class Mission
{
    public struct OptionPackage
    {
        public string FileName;
        public Dictionary<string, string?> Options;

        public static string CheckKey(string key)
        {
            key = key.Trim();
            if (key[0] != '-')
            {
                key = '-' + key;
            }
            return key;
        }

        public readonly IEnumerable<string> Arguments()
        {
            foreach (var (key, value) in Options)
            {
                if (value is null)
                {
                    yield return CheckKey(key);
                }
                else
                {
                    yield return CheckKey(key);
                    yield return value;
                }
            }
        }
    }

    public string FFmpegPath = string.Empty;

    public OptionPackage GlobalOptions = new();

    public List<OptionPackage> Inputs = [];
    public List<OptionPackage> Outputs = [];

    public List<string> CommandElements()
    {
        List<string> cmd =
        [
            FFmpegPath,
            .. GlobalOptions.Arguments(),
        ];
        Inputs.ForEach((input) => { cmd.AddRange(input.Arguments()); cmd.Add("-i"); cmd.Add(input.FileName); });
        Outputs.ForEach(Outputs => { cmd.AddRange(Outputs.Arguments()); cmd.Add(Outputs.FileName); });
        return cmd;
    }

    public string CompileFullCommand()
    {
        return string.Join(' ', CommandElements());
    }
}
