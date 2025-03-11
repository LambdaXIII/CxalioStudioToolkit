using CxStudio;
using Microsoft.VisualBasic.FileIO;

namespace MediaKiller.ExtraExpanders;

class CsvMetadataExpander : ISourcePreExpander
{
    private readonly HashSet<string> _cache = [];

    public bool IsAcceptable(string path)
    {
        if (!path.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        if (!File.Exists(path))
        {
            return false;
        }
        return true;
    }

    public IEnumerable<string> Expand(string path)
    {
        TextFieldParser parser = new(path);
        parser.Delimiters = [",", "\t"];

        string[]? header = parser.ReadFields();
        if (header is null)
            yield break;

        var field_filler = new TextUtils.FieldFiller(header);

        while (!parser.EndOfData)
        {
            string[]? values = parser.ReadFields();
            if (values is null)
                continue;
            var metadata = field_filler.Parse(values);
            string file_name = metadata["File Name"];
            string directory = metadata["Clip Directory"];
            yield return Path.Combine(directory, file_name);
        }
    }
}
