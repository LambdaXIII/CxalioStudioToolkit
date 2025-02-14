using System.Text.RegularExpressions;

namespace CxStudio
{

    public interface ITagStringProvider
    {
        string Replace(string? param);
    }

    public class FileInfoProvider : ITagStringProvider
    {
        private string _source;

        public FileInfoProvider(string path)
        {
            _source = path;
        }

        public string Replace(string? param)
        {
            string? result = param switch
            {
                "basename" => Path.GetFileNameWithoutExtension(_source),
                "extension" => Path.GetExtension(_source),
                "parent_name" => Path.GetDirectoryName(_source),
                "filename" => Path.GetFileName(_source),
                "fullpath" => Path.GetFullPath(_source),
                "root" => Path.GetPathRoot(_source),
                _ => _source
            };
            return result ?? _source;
        }
    }

    class SimpleReplacementProvider(string replacement) : ITagStringProvider
    {
        public string Replace(string? param)
        {
            return replacement;
        }
    }


    public class TagReplacer
    {
        private readonly Dictionary<string, ITagStringProvider> _tagProviders;

        public TagReplacer()
        {
            _tagProviders = new Dictionary<string, ITagStringProvider>();
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
                input = Regex.Replace(input, pattern, match => tagProvider.Value.Replace(match.Groups[2].Success ? match.Groups[2].Value : null));
            }
            return input;
        }
    }



}//namespace
