using CxStudio;
namespace MediaKiller;

class SourceExpander(Preset p)
{
    private readonly Preset Preset = p;
    private readonly DirectoryExpanderSettings DefaultSettings = new DirectoryExpanderSettings()
    {
        AcceptDirectories = false,
        AcceptFiles = true,
        IncludeSubDirectories = true,
        FileValidator = new ExtensionWhiteListChecker(p.AcceptableSuffixes)
    };

    public IEnumerable<string> Expand(IEnumerable<string> sources)
    {
        foreach (string source in sources)
        {
            DirectoryExpander expander = new(source, DefaultSettings);
            foreach (string file in expander.Expand())
            {
                yield return file;
            }
        }
    }
}


