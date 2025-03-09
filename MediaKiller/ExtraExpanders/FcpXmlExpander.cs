using CxStudio;
using Spectre.Console;
using System.Xml.Linq;

namespace MediaKiller.ExtraExpanders;

class FcpXmlExpander : ISourcePreExpander
{
    private readonly HashSet<string> _cache = [];
    public bool IsAcceptable(string path)
    {
        if (!path.EndsWith(".fcpxml", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        XDocument doc = XDocument.Load(path);

        if (doc.DocumentType is null || doc.DocumentType.Name != "fcpxml")
        {
            return false;
        }

        if (doc.Root is null || doc.Root.Name != "fcpxml")
        {
            return false;
        }

        return true;
    }

    public IEnumerable<string> Expand(string path)
    {
        XDocument doc = XDocument.Load(path);
        foreach (var node in doc.Descendants("media-rep"))
        {
            var src = node.Attribute("src");
            string p = TextUtils.UrlToPath(src?.Value ?? "") ?? "";

            if (p.Length == 0 || _cache.Contains(p))
                continue;

            if (XEnv.Instance.Debug) { AnsiConsole.WriteLine($"{src} -> {p}"); }

            _cache.Add(p);
            yield return p;
        }
    }
}

class FcpXmlDirectoryExpander : ISourcePreExpander
{
    private readonly FcpXmlExpander fcpXmlExpander = new();

    private string GetInfoPath(string path)
    {
        return Path.Combine(path, "Info.fcpxml");
    }
    public bool IsAcceptable(string path)
    {
        if (!path.EndsWith(".fcpxmld", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string content_path = GetInfoPath(path);
        return fcpXmlExpander.IsAcceptable(content_path);
    }

    public IEnumerable<string> Expand(string path)
    {
        string content_path = GetInfoPath(path);
        return fcpXmlExpander.Expand(content_path);
    }
}
