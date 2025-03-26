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
        public bool Debug { get; init; }
    }

    public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
    {
        XEnv.Instance.OutputFolder = settings.Output ?? Environment.CurrentDirectory;
        XEnv.Instance.ScriptOutput = settings.ScriptOutput;
        XEnv.Instance.Debug = settings.Debug;
        XEnv.Instance.ForceOverwrite = settings.ForceOverwrite;
        if (XEnv.Instance.ForceOverwrite) AnsiConsole.MarkupLine("已启用[red]强制覆写[/]模式，将忽略预设文件中的相关设置。");
        XEnv.Instance.NoOverwrite = settings.NoOverwrite;
        if (XEnv.Instance.NoOverwrite) AnsiConsole.MarkupLine("已启用[green]禁止覆写[/]模式，将忽略任何相关设置并拒绝覆盖目标文件。");

        XEnv.DebugMsg("MediaKiller started.  :)");

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

        XEnv.ReportPresets();
        XEnv.ReportSources();


        List<Mission> missions = [];
        foreach (Preset p in XEnv.Instance.Presets)
        {
            XEnv.DebugMsg($"Expanding sources for preset {p.Name}");

            SourceExpander expander = new(p);
            MissionMaker maker = new(p);
            foreach (string source in expander.Expand(XEnv.Instance.Sources))
            {
                missions.Add(maker.Make(source));
                XEnv.DebugMsg($"Mission added: {source}");
            }
        }

        AnsiConsole.MarkupLine("为 [yellow]{0}[/] 个预设生成 [yellow]{1}[/] 个任务。", XEnv.Instance.Presets.Count, missions.Count);

        if (XEnv.Instance.ScriptOutput is not null)
            ExportScript(XEnv.Instance.ScriptOutput, missions);

        //Transcode(missions);

        MissionManager manager = new();
        manager.AddMissions(missions);
        manager.Run();

        return 0;
    } // Execute

    private void ExportScript(string target, IEnumerable<Mission> missions)
    {
        AnsiConsole.MarkupLine("生成目标脚本 [cyan]{0}[/] ...", Path.GetFileName(target));

        using StreamWriter writer = new(target);
        ScriptMaker script_maker = new(missions);
        foreach (var line in script_maker.Lines())
        {
            writer.WriteLine(line);
            if (XEnv.Instance.Debug)
                AnsiConsole.MarkupLine("[grey]{0}[/]", line);
        }

        AnsiConsole.MarkupLine("[cyan]脚本生成完毕。[cyan]");
    }

    public static void SaveSamplePreset(string path)
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

        AnsiConsole.MarkupLine("生成示例预设： [yellow]{0}[/]", Path.GetFileName(path));

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

        AnsiConsole.MarkupLine("生成完毕，[red]请在修改之后使用！[/]");
    }
}