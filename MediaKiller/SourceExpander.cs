using CxStudio;
using MediaKiller.ExtraExpanders;
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

    private readonly List<ISourcePreExpander> PreExpanders = [
        new LegacyXMLExpander(),new FcpXmlExpander(),new FcpXmlDirectoryExpander(),
        new FileListExpander(),new CsvMetadataExpander()
    ];


    private IEnumerable<string> PreExpand(IEnumerable<string> sources)
    {
        foreach (string source in sources)
        {
            bool pre_expanded = false;
            foreach (ISourcePreExpander pre_expander in PreExpanders)
            {
                if (pre_expander.IsAcceptable(source))
                {
                    pre_expanded = true;
                    foreach (string expanded in pre_expander.Expand(source))
                    {
                        yield return expanded;
                    }
                }
            }
            if (!pre_expanded) yield return source;
        }
    }

    public IEnumerable<string> Expand(IEnumerable<string> sources)
    {
        foreach (string source in PreExpand(sources))
        {
            DirectoryExpander expander = new(source, DefaultSettings);
            foreach (string file in expander.Expand())
            {
                yield return file;
            }
        }
    }
}


