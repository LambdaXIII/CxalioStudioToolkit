using CxStudio;
namespace MediaKiller;

class SourceExpander(IEnumerable<Preset> presets, IEnumerable<string> sources)
{
    private readonly IEnumerable<Preset> Presets = presets;
    private readonly IEnumerable<string> Sources = sources;
    static DirectoryExpanderSettings PublicExpanderSettings => new()
    {
        IncludeSubDirectories = true,
        AcceptDirectories = false,
        AcceptFiles = true,
    };

    private static DirectoryExpander MakeExpander(Preset preset, string source)
    {
        var settings = PublicExpanderSettings;
        settings.FileValidator = new ExtensionWhiteListChecker(preset.AcceptableSuffixes);
        return new(source, settings);
    }

    public IEnumerable<string> Expand()
    {
        foreach (var preset in Presets)
        {
            foreach (var source in Sources)
            {
                var expander = MakeExpander(preset, source);
                foreach (var path in expander.Expand())
                {
                    yield return path;
                }
            }
        }
    }

}


