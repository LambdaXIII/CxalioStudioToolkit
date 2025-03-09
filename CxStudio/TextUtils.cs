using System.Text.RegularExpressions;
using System.Web;

namespace CxStudio;

public class TextUtils
{
    private static readonly Regex _urlpattern = new(@"^file://(localhost/|/)", RegexOptions.Compiled);

    public static string QuoteSpacedString(string text)
    {
        if (text.Contains(' '))
        {
            return '"' + text + '"';
        }
        return text;
    }

    public static string? UrlToPath(string url)
    {
        url = HttpUtility.UrlDecode(url);
        if (_urlpattern.IsMatch(url))
        {
            return _urlpattern.Replace(url, string.Empty);
        }
        return null;
    }
}
