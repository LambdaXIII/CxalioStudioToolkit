using System.Text.RegularExpressions;
using System.Web;

namespace CxStudio;

public class TextUtils
{


    public static string QuoteSpacedString(string text)
    {
        if (text.Contains(' '))
        {
            return '"' + text + '"';
        }
        return text;
    }

    private static readonly Regex _urlpattern = new(@"^file://(localhost/|/)", RegexOptions.Compiled);
    public static string? UrlToPath(string url)
    {
        url = HttpUtility.UrlDecode(url);
        if (_urlpattern.IsMatch(url))
        {
            return _urlpattern.Replace(url, string.Empty);
        }
        return null;
    }

    public class FieldFiller(IEnumerable<string> titles)
    {
        private readonly List<string> _titles = (List<string>)titles;

        public Dictionary<string, string> Parse(IEnumerable<string> values)
        {
            Dictionary<string, string> result = [];
            foreach (var (title, value) in _titles.Zip(values))
            {
                result[title] = value;
            }
            return result;
        }
    }
}
