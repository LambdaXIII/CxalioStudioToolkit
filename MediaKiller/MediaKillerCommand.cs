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

        [Description("The output directory.")]
        [CommandOption("-o|--output <OUTPUT>")]
        public string? Output { get; set; }

        [Description("Save missions as a script.")]
        [CommandOption("-s|--save-script <SCRIPT>")]
        public string? ScriptOutput { get; set; }

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

        foreach (Preset p in XEnv.Instance.Presets)
        {
            SourceExpander expander = new(p);
            MissionMaker maker = new(p);
            foreach (string source in expander.Expand(XEnv.Instance.Sources))
            {
                Mission m = maker.Make(source);
                AnsiConsole.WriteLine(m.GetFullCommand());
            }
        }

        return 0;
    } // Execute

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