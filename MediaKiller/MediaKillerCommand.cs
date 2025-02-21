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
        [DefaultValue(true)]
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
        GlobalArguments.Instance.OutputFolder = settings.Output ?? Environment.CurrentDirectory;
        GlobalArguments.Instance.Debug = settings.Debug;

        foreach (var input in settings.Inputs ?? [])
        {
            string lowerExt = Path.GetExtension(input).ToLower();
            if (string.IsNullOrEmpty(lowerExt))
            {
                string i = Path.GetFullPath(input) + ".toml";
                if (File.Exists(i))
                {
                    GlobalArguments.Instance.Presets.Add(Preset.Load(i));
                    continue;
                }
            }

            if (lowerExt == ".toml")
            {
                GlobalArguments.Instance.Presets.Add(Preset.Load(input));
            }
            else
            {
                GlobalArguments.Instance.Sources.Add(input);
            }
        }

        foreach (var p in GlobalArguments.Instance.Presets)
        {
            Console.WriteLine(p.Name);
        }

        SourceExpander sourceExpander = new(GlobalArguments.Instance.Presets[0]);
        foreach (var source in sourceExpander.Expand(GlobalArguments.Instance.Sources))
        {
            Console.WriteLine(source);
        }

        return 0;
    }
}