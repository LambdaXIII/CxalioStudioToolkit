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
}
