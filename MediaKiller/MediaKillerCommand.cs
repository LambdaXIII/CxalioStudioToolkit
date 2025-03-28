using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace MediaKiller;


internal sealed class MediaKillerCommand : Command<MediaKillerCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [Description("The input files to process.")]
        [CommandArgument(0, "<INPUTS>")]
        public string[]? Inputs { get; set; }

        [Description("Generate a new profile instead of processing it.")]
        [CommandOption("-g|--generate")]
        [DefaultValue(false)]
        public bool GenerateProfile { get; init; }

        [Description("Force overwrite target files.")]
        [CommandOption("-y|--force-overwrite")]
        [DefaultValue(false)]
        public bool ForceOverwrite { get; init; }

        [Description("Force NOT to overwrite target files.")]
        [CommandOption("-n|--no-overwrite")]
        [DefaultValue(false)]
        public bool NoOverwrite { get; init; }

        [Description("The output directory.")]
        [CommandOption("-o|--output <OUTPUT>")]
        public string? Output { get; init; }

        [Description("Save missions as a script.")]
        [CommandOption("-s|--save-script <SCRIPT>")]
        public string? ScriptOutput { get; init; }

        [Description("Enable debug mode.")]
        [CommandOption("-d|--debug")]
        [DefaultValue(false)]
        public bool Debug { get; set; }

        [Description("Clear media information caches")]
        [CommandOption("-c|--clean")]
        [DefaultValue(false)]
        public bool ClearCaches { get; set; }
    }

    private readonly XEnv.Talker Talker = new("MainLoop");

    public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
    {
        XEnv.ShowBanner();

        int exitCode = 0;
        XEnv.Instance.Debug = settings.Debug;
        Talker.ReInstall();
        Talker.Whisper("Debug mode enabled.");

        XEnv.Instance.OutputFolder = settings.Output ?? Environment.CurrentDirectory;
        XEnv.Instance.ScriptOutput = settings.ScriptOutput;

        XEnv.Instance.ForceOverwrite = settings.ForceOverwrite;
        if (XEnv.Instance.ForceOverwrite) Talker.Say("已启用[red]强制覆写[/]模式，将忽略预设文件中的相关设置。");

        XEnv.Instance.NoOverwrite = settings.NoOverwrite;
        if (XEnv.Instance.NoOverwrite) Talker.Say("已启用[green]禁止覆写[/]模式，将忽略任何相关设置并拒绝覆盖目标文件。");

        if (settings.ClearCaches)
        {
            Talker.Say("将重新生成所有媒体信息");
            MediaDB.Instance.ClearCaches();
        }

        try
        {
            exitCode = MainProcess(ref settings);
        }
        catch (OperationCanceledException)
        {
            Talker.Say("[red]任务被强制取消[/]");
        }
        catch (Exception ex)
        {

            AnsiConsole.Write(Text.NewLine);
            AnsiConsole.Write(new Rule("[red]错误报告[/]").LeftJustified());
            AnsiConsole.WriteException(ex);
            AnsiConsole.Write(new Rule("[red]END[/]").RightJustified());
            AnsiConsole.Write(Text.NewLine);
            exitCode = 1;
        }
        finally
        {
            MediaDB.Instance.SaveRecords();
            XEnv.Instance.ReportResults();
            XEnv.Instance.CleanUpEverything();
            if (XEnv.Instance.GlobalCancellation.IsCancellationRequested)
                Talker.Say("[grey]任务[red]并未未全部完成[/]，下次使用[yellow]同样的参数[/]并[yellow]添加 -n 选项[/]，即可[green]跳过已完成的任务[/]。[/]");
        }
        return exitCode;
    } // Execute

    private int MainProcess(ref Settings settings)
    {
        Talker.Whisper("MediaKiller started.  :)");

        if (settings.GenerateProfile)
        {
            SaveSamplePreset(settings.Inputs?.FirstOrDefault("") ?? "");
            return 0;
        }

        foreach (var input in settings.Inputs ?? [])
        {
            string lowerExt = Path.GetExtension(input).ToLower();
            if (string.IsNullOrEmpty(lowerExt))
            {
                string i = Path.GetFullPath(input) + ".toml";
                if (File.Exists(i))
                {
                    XEnv.Instance.Presets.Add(Preset.Load(i));
                    continue;
                }
            }

            if (lowerExt == ".toml")
            {
                XEnv.Instance.Presets.Add(Preset.Load(input));
            }
            else
            {
                XEnv.Instance.Sources.Add(input);
            }
        }

        XEnv.ShowPresetsReport();
        XEnv.ShowSourcesReport();

        MissionManager manager = new();
        manager.AddMissions(XEnv.Instance.Presets, XEnv.Instance.Sources);

        if (XEnv.Instance.Presets.Count > 1)
            Talker.Say(
                "为 [yellow]{0}[/] 个预设生成共计 [yellow]{1}[/] 个任务。",
                XEnv.Instance.Presets.Count,
                manager.Missions.Count
                );

        if (XEnv.Instance.ScriptOutput is not null)
        {
            ExportScript(XEnv.Instance.ScriptOutput, manager.Missions);
            return 0;
        }

        manager.Run();
        Talker.Whisper("MediaKiller finished.  :)");
        return 0;
    }//MainProcess

    private void ExportScript(string target, IEnumerable<Mission> missions)
    {
        Talker.Say("生成目标脚本 [cyan]{0}[/] ...", Path.GetFileName(target));

        using StreamWriter writer = new(target);
        ScriptMaker script_maker = new(missions);
        foreach (var line in script_maker.Lines())
        {
            XEnv.Whisper(line);
            writer.WriteLine(line);
        }

        Talker.Say("[green]脚本生成完毕。[/]");
    }

    public void SaveSamplePreset(string path)
    {
        if (path == string.Empty)
        {
            AnsiConsole.WriteException(new ArgumentException("No path specified."));
        }

        path = Path.GetFullPath(path);
        if (Path.GetExtension(path) != ".toml")
        {
            path += ".toml";
        }

        Talker.Say("生成示例预设： [yellow]{0}[/]", Path.GetFileName(path));

        var assembly = typeof(MediaKillerCommand).Assembly;

        using var stream = assembly.GetManifestResourceStream("MediaKiller.example_preset.toml")
            ?? throw new FileNotFoundException("Embedded resource not found: example_preset.toml");

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var reader = new StreamReader(stream);
        using var writer = new StreamWriter(path);
        while (!reader.EndOfStream)
        {
            writer.WriteLine(reader.ReadLine());
        }

        Talker.Say("生成完毕，[red]请在修改之后使用！[/]");
    }
}