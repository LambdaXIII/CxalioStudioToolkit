using CxStudio.CxConfig;
using Spectre.Console;

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

    public static readonly string AppName = "MediaKiller";
    public static readonly string AppVersion = "0.1.0";
    public static readonly CxConfigManager ConfigManaer = new(AppName, AppVersion);


    public static void Err(string message)
    {
        Console.Error.WriteLine(message);
    }

    public static void ReportPresets()
    {
        if (Instance.Presets.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]未指定合法的预设文件。[/]");
            return;
        }

        AnsiConsole.MarkupLine("发现 [yellow]{0}[/] 个预设文件:", Instance.Presets.Count);

        foreach (var preset in Instance.Presets)
        {
            AnsiConsole.MarkupLine("\t[yellow]{0}[/] <[cyan]{1}[/]>", preset.Name, preset.Description);
        }
    }

    public static void ReportSources()
    {
        if (Instance.Sources.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]未发现合法的来源路径。[/]");
            return;
        }
        AnsiConsole.MarkupLine("发现 [yellow]{0}[/] 个来源路径。", Instance.Sources.Count);
    }

}

