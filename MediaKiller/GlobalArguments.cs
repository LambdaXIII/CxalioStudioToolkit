namespace MediaKiller;

internal sealed class GlobalArguments
{
    public List<Project> Projects { get; set; } = [];

    public List<string> Sources { get; set; } = [];

    public string OutputFolder { get; set; } = string.Empty;

    public bool Debug { get; set; } = false;

}

