using CxStudio;
namespace MediaKiller;

class SourceExpander
{
    private readonly Preset preset;
    private readonly DirectoryExpanderSettings expanderSettings;

    public SourceExpander(Preset preset)
    {
        this.preset = preset;
        expanderSettings = new DirectoryExpanderSettings()
        {
            IncludeSubDirectories = true,
            AcceptFiles = true,
            AcceptDirectories = false,
        };
        expanderSettings.FileValidator = new ExtensionWhiteListChecker(this.preset.
            AcceptableSuffixes);

    }

    public IEnumerable<string> Expand(IEnumerable<string> sources)
    {
        foreach (var source in sources)
        {
            var expander = new DirectoryExpander(source, expanderSettings);
            foreach (var item in expander.Expand())
            {
                yield return item;
            }
        }
    }
}
