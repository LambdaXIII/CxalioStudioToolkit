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

        [Description("Enable debug mode.")]
        [CommandOption("-d|--debug")]
        [DefaultValue(false)]
        public bool Debug { get; init; }
    }

    public readonly GlobalArguments Arguments = new();

    public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
    {
        Arguments.OutputFolder = settings.Output ?? ".";
        Arguments.Debug = settings.Debug;
        foreach (var input in settings.Inputs ?? [])
        {
            if (Path.GetExtension(input) == ".toml")
            {
                Preset p = Preset.Load(input);

                if (settings.Output != null)
                    p.OverrideTargetFolder = settings.Output;

                Arguments.Projects.Add(Preset.Load(input));
            }
            else
            {
                Arguments.Sources.Add(input);
            }
        }

        return 0;
    }
}