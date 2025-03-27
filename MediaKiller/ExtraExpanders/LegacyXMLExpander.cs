using CxStudio.Core;
using System.Xml.Linq;

namespace MediaKiller.ExtraExpanders;

internal sealed class LegacyXMLExpander : ISourcePreExpander
{
    private readonly XEnv.Talker Talker = new("LegacyXMLExpander");

    private readonly HashSet<string> _cache = [];

    public bool IsAcceptable(string path)
    {
        if (!path.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!File.Exists(path))
        {
            return false;
        }

        XDocument doc = XDocument.Load(path);

        if (doc.DocumentType is null || doc.DocumentType.Name != "xmeml")
        {
            return false;
        }

        if (doc.Root is null || doc.Root.Name != "xmeml")
        {
            return false;
        }

        return true;
    }

    public IEnumerable<string> Expand(string path)
    {
        Talker.Whisper("Expanding {0}", path);

        XDocument doc = XDocument.Load(path);
        foreach (var node in doc.Descendants("pathurl"))
        {
            string src_path = TextUtils.UrlToPath(node.Value) ?? "";

            if (src_path.Length == 0 || _cache.Contains(src_path))
                continue;

            _cache.Add(src_path);
            Talker.Whisper("Found {0}", src_path);
            yield return src_path;
        }
    }
}

