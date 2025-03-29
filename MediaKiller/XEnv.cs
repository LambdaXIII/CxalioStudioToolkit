using CxStudio.Core;
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

    public bool ForceOverwrite = false;
    public bool NoOverwrite = false;


    private static readonly Lazy<XEnv> _instance = new(() => new XEnv());

    public static XEnv Instance => _instance.Value;

    public static readonly string AppName = "MediaKiller";
    public static readonly string AppVersion = "0.1.0";
    public static readonly CxConfigManager ConfigManaer = new(AppName, AppVersion);

    public CancellationTokenSource GlobalCancellation = new();
    public CancellationTokenSource GlobalForceCancellation = new();
    public event Action? UserCancelled;

    private DateTime? LastCancelTime = null;

    private XEnv()
    {
        Console.CancelKeyPress += HandleCancelation;
    }

    public static void ShowBanner()
    {
        AnsiConsole.Write(new FigletText("MediaKiller").Color(Color.Orange3));
        AnsiConsole.MarkupLine("[yellow]MediaKiller[/] [blue]v{0}[/]", AppVersion);
        AnsiConsole.MarkupLine("[grey]A transcode tool from Cxalio Studio ToolKit[/]");
        AnsiConsole.Write("\n\n");
    }

    /*    public static void ShowPresetsReport()
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

        public static void ShowSourcesReport()
        {
            if (Instance.Sources.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]未发现合法的来源路径。[/]");
                return;
            }
            AnsiConsole.MarkupLine("发现 [yellow]{0}[/] 个来源路径。", Instance.Sources.Count);
        }*/

    public void HandleCancelation(object? _, ConsoleCancelEventArgs e)
    {
        if (LastCancelTime is DateTime lastTime && (DateTime.Now - lastTime).TotalSeconds < 3)
        {
            Say("好啦好啦，这就退出 ~");
            Say("[red]正在强制终止[/]");
            GlobalForceCancellation.Cancel();
            Thread.Sleep(3000);
            Environment.Exit(1);
            return;
        }
        Say("[grey]接收到 [red]取消[/] 信号，正在处理…[/]");

        GlobalCancellation.Cancel();
        UserCancelled?.Invoke();
        e.Cancel = true;

        LastCancelTime = DateTime.Now;
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


    private readonly HashSet<string> _garbageFiles = [];
    private readonly Mutex _garbageMutex = new();
    public void MarkGarbage(string filename)
    {
        _garbageMutex.WaitOne();
        _garbageFiles.Add(filename);
        _garbageMutex.ReleaseMutex();
    }

    private readonly HashSet<string> _succeededFiles = [];
    private readonly Mutex _succeededMutex = new();
    public void MarkSucceededFile(string filename)
    {
        _succeededMutex.WaitOne();
        _succeededFiles.Add(filename);
        _succeededMutex.ReleaseMutex();
    }

    public static void Whisper(string? msg)
    {
        if (Instance.Debug || string.IsNullOrEmpty(msg)) return;
        AnsiConsole.MarkupLine("[grey][[GLOBAL]] {0}[/]", msg);
    }

    public static void Whisper(string format, params object[] args)
    {
        AnsiConsole.MarkupLine("[grey][[GLOBAL]] {0}[/]",
            Markup.Escape(
                string.Format(format, args)
                )
            );
    }

    public static void Say(string? msg)
    {
        if (string.IsNullOrEmpty(msg)) return;
        AnsiConsole.MarkupLine(msg);
    }

    public static void Say(string format, params object[] args)
    {
        AnsiConsole.MarkupLine(format, args);
    }


    private static void SayHandler(Talker sender, string message)
    {
        AnsiConsole.MarkupLine(
            message
            );
    }

    private static void WhisperHandler(Talker sender, string message)
    {
        if (!XEnv.Instance.Debug) return;
        AnsiConsole.MarkupLine("[grey][[{0}]] {1}[/]", sender.Name,
            message
            );
    }

    public delegate void TalkHandler(Talker sender, string message);
    public class Talker
    {
        public string Name;
        public event TalkHandler? WannaSay;
        public event TalkHandler? WannaWhisper;

        public Talker(string name = "")
        {
            Name = name;
            WannaSay += XEnv.SayHandler;
            if (XEnv.Instance.Debug)
                WannaWhisper += XEnv.WhisperHandler;
        }

        public void Say(string message)
        {
            WannaSay?.Invoke(this, message);
        }

        public void Say(string format, params object[] args)
        {
            WannaSay?.Invoke(this, string.Format(format, args));
        }

        public void Whisper(string message)
        {
            WannaWhisper?.Invoke(this, message);
        }

        public void Whisper(string format, params object[] args)
        {
            WannaWhisper?.Invoke(this, string.Format(format, args));
        }

        public void ReInstall()
        {
            WannaSay -= XEnv.SayHandler;
            WannaSay += XEnv.SayHandler;
            WannaWhisper -= XEnv.WhisperHandler;
            if (XEnv.Instance.Debug)
                WannaWhisper += XEnv.WhisperHandler;
        }
    }


    public void CleanUpEverything()
    {
        _garbageMutex.WaitOne();
        if (_garbageFiles.Count > 0)
        {
            Say("[grey]清理垃圾文件：[/]");

            foreach (var filename in _garbageFiles)
            {
                if (!File.Exists(filename)) continue;
                File.Delete(filename);
                AnsiConsole.Write("\t");
                AnsiConsole.Write(
                    new TextPath(filename)
                    .LeafColor(Color.Grey)
                    .RootColor(Color.Grey)
                    .StemColor(Color.Grey)
                    .SeparatorColor(Color.Grey)
                    );
                AnsiConsole.Write(Text.NewLine);
            }

            _garbageFiles.Clear();
        }
        _garbageMutex.ReleaseMutex();
    }

    public void ReportResults()
    {
        _succeededMutex.WaitOne();
        int count = _succeededFiles.Count;
        if (count > 0)
        {
            ulong totalBytes = (ulong)_succeededFiles.Sum(file => new FileInfo(file).Length);
            FileSize totalSize = FileSize.FromBytes(totalBytes);
            Say("成功处理了 [yellow]{0}[/] 个文件，总大小为 [green]{1}[/] 。", count, totalSize.FormattedString);
        }
        _succeededMutex.ReleaseMutex();
    }
}




