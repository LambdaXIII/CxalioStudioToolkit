namespace MediaKiller.ExtraExpanders;

internal sealed class FileListExpander : ISourcePreExpander
{
    private readonly HashSet<string> _cache = [];

    private static bool MightBeFilePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        string root = Path.GetPathRoot(path) ?? string.Empty;
        if (root.Length > 0)
            return true;

        if (path.Contains(Path.DirectorySeparatorChar) || path.Contains(Path.AltDirectorySeparatorChar))
            return true;

        if (path.Contains(Path.VolumeSeparatorChar))
            return true;

        foreach (char c in path)
        {
            if (Path.GetInvalidFileNameChars().Contains(c))
                return false;
        }

        return File.Exists(Path.GetFullPath(path));
    }


    public bool IsAcceptable(string path)
    {
        if (!path.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
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
        //TODO: Add Encoding check
        foreach (var line in File.ReadLines(path))
        {
            string src_path = line.Trim();
            if (!MightBeFilePath(src_path))
                continue;
            _cache.Add(src_path);
            yield return Path.GetFullPath(src_path);
        }
    }
}
