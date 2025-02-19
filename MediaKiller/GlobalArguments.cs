namespace MediaKiller;

internal sealed class GlobalArguments
{
    public List<Preset> Presets { get; set; } = [];

    public List<string> Sources { get; set; } = [];

    public string? OutputFolder { get; set; } = null;

    public bool Debug { get; set; } = false;

}

