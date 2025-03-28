﻿using CxStudio;
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

    private Talker _talker { get; init; } = new("XEnv");

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

    public static void ShowPresetsReport()
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
    }

    public void HandleCancelation(object? _, ConsoleCancelEventArgs e)
    {
        AnsiConsole.MarkupLine("[grey]接收到 [red]取消[/] 信号，正在处理…[/]");
        GlobalCancellation.Cancel();
        e.Cancel = true;
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

    public static void Whisper(string? msg)
    {
        if (string.IsNullOrEmpty(msg)) return;
        WhisperHandler(XEnv.Instance._talker, msg);
    }

    public static void Whisper(string format, params object[] args)
    {
        WhisperHandler(XEnv.Instance._talker, string.Format(format, args));
    }

    public static void Say(string? msg)
    {
        if (string.IsNullOrEmpty(msg)) return;
        SayHandler(XEnv.Instance._talker, msg);
    }

    public static void Say(string format, params object[] args)
    {
        SayHandler(XEnv.Instance._talker, string.Format(format, args));
    }


    private static void SayHandler(Talker sender, string message)
    {
        AnsiConsole.MarkupLine(
            message
            //.Replace("[", "[[")
            //.Replace("]", "]]")
            );
    }

    private static void WhisperHandler(Talker sender, string message)
    {
        if (!XEnv.Instance.Debug) return;
        AnsiConsole.MarkupLine("[grey][[{0}]] {1}[/]", sender.Name,
            message
            //.Replace("[", "[[")
            //.Replace("]", "]]")
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
    }

}




