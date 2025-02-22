namespace MediaKiller;

internal sealed class XEnv
{
    public List<Preset> Presets { get; set; } = [];

    public List<string> Sources { get; set; } = [];

    public string? OutputFolder { get; set; } = null;

    public string? ScriptOutput { get; set; } = null;

    public bool Debug { get; set; } = false;


    private static readonly Lazy<XEnv> _instance = new(() => new XEnv());
    private XEnv() { }
    public static XEnv Instance => _instance.Value;


    public static void Err(string message)
    {
        Console.Error.WriteLine(message);
    }

}

