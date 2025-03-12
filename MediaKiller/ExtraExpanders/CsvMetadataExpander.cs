using Microsoft.VisualBasic.FileIO;

namespace MediaKiller.ExtraExpanders;

class CsvMetadataExpander : ISourcePreExpander
{
    private readonly HashSet<string> _cache = [];

    private class FieldChecker(string[] headers)
    {
        private readonly string[] _headers = headers;
        public string? GetValue(string key, string[] values)
        {
            int index = Array.IndexOf(_headers, key);
            if (index == -1)
                return null;
            return values[index];
        }
    }

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

        FieldChecker checker = new(header);

        while (!parser.EndOfData)
        {
            string[]? values = parser.ReadFields();
            if (values is null)
                continue;
            string file_name = checker.GetValue("File Name", values) ?? "";
            string directory = checker.GetValue("Clip Directory", values) ?? "";
            yield return Path.Combine(directory, file_name);
        }
    }
}
