using System.Text.RegularExpressions;

namespace CxStudio;


public interface ITagStringProvider
{
    string? Replace(string? parameters);
}

public sealed class FileInfoProvider(string path) : ITagStringProvider
{
    private readonly string _source = path;

    public string? Replace(string? parameters)
    {
        string? result = parameters switch
        {
            "basename" => Path.GetFileNameWithoutExtension(_source),
            "extension" => Path.GetExtension(_source),
            "parent_name" => Path.GetDirectoryName(_source),
            "filename" => Path.GetFileName(_source),
            "fullpath" => Path.GetFullPath(_source),
            "absolute" => Path.GetFullPath(_source),
            "root" => Path.GetPathRoot(_source),
            _ => null
        };
        return result;
    }
}

internal sealed class SimpleReplacementProvider(string replacement) : ITagStringProvider
{
    public string? Replace(string? param)
    {
        return replacement;
    }
}


public class TagReplacer
{
    private readonly Dictionary<string, ITagStringProvider> _tagProviders;

    public TagReplacer()
    {
        _tagProviders = [];
    }

    public TagReplacer InstallProvider(string tag, ITagStringProvider provider)
    {
        _tagProviders.Add(tag, provider);
        return this;
    }

    public TagReplacer InstallProvider(string tag, string replacement)
    {
        _tagProviders.Add(tag, new SimpleReplacementProvider(replacement));
        return this;
    }

    public string ReplaceTags(string input)
    {
        foreach (var tagProvider in _tagProviders)
        {
            string pattern = $@"\$\{{{tagProvider.Key}(:(.*?))?\}}";
            input = Regex.Replace(input, pattern, match => tagProvider.Value.Replace(match.Groups[2].Success ? match.Groups[2].Value : null) ?? input);
        }
        return input;
    }
}



//namespace
