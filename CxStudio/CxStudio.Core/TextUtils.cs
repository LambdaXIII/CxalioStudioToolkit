using System.Text.RegularExpressions;
using System.Web;

namespace CxStudio.Core;

public static class TextUtils
{
    public static string AutoQuote(this string text, IEnumerable<string>? tokens)
    {
        bool needQuote = false;
        if (tokens is null)
            needQuote = true;
        else foreach (var token in tokens)
            {
                if (text.Contains(token))
                {
                    needQuote = true;
                    break;
                }
            }

        return needQuote ? $"\"{text}\"" : text;
    }

    public static string AutoQuote(this string text, string tokens = " ")
    {
        var ts = tokens.AsEnumerable<char>()
            .Select(c => c.ToString());
        return AutoQuote(text, ts);
    }

    public static string AutoUnquote(this string text)
    {
        if (text.StartsWith('"') && text.EndsWith('"'))
            return text[1..^1];
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
}
