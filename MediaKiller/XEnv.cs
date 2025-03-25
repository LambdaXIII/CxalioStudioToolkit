using CxStudio;
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

    public static XEnv Instance => _instance.Value;

    public static readonly string AppName = "MediaKiller";
    public static readonly string AppVersion = "0.1.0";
    public static readonly CxConfigManager ConfigManaer = new(AppName, AppVersion);

    public CancellationTokenSource GlobalCancellation = new();


    private XEnv()
    {
        Console.CancelKeyPress += HandleCancelation;
    }

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

    public void HandleCancelation(object? _, ConsoleCancelEventArgs e)
    {
        AnsiConsole.MarkupLine("接收到 [red]取消[/] 信号，正在处理……");
        GlobalCancellation.Cancel();
        e.Cancel = true;
    }

    public static void DebugMsg(string? msg)
    {
        if (Instance.Debug && msg is not null)
        {
            AnsiConsole.MarkupLine(
                "[grey]{0}[/]",
                msg.Replace("[", "[[").Replace("]", "]]")
                );
        }
    }

    public static string? GetCommandPath(string cmd)
    {
        var finder = new CommandFinder();

        string personal = Environment.GetFolderPath(Environment.SpecialFolder.Personal);

        finder.AddSearchPath(Path.Combine(personal, "Download/"))
            .AddSearchPath(Path.Combine(personal, ".local/bin/"))
            .AddSearchPath(Path.Combine(personal, ".bin/"))
            .AddSearchPath(Path.Combine(personal, "bin/"))
            .AddSearchPath(Path.Combine(personal, "Desktop/"))
            .AddSearchPath(Path.Combine(personal, "ffmpeg/"))
            .AddSearchPath(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));

        return finder.Find(cmd);
    }


}

