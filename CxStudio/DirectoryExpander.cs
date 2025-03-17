namespace CxStudio;


public interface IPathChecker
{
    bool Check(string path);
}

class DefaultPathChecker : IPathChecker
{
    public bool Check(string path) => true;
}

public class ChainPathChecker : IPathChecker
{
    private readonly List<IPathChecker> _checkers;

    public ChainPathChecker()
    {
        _checkers = [];
    }

    public ChainPathChecker(IEnumerable<IPathChecker> checkers)
    {
        _checkers = new List<IPathChecker>(checkers);
    }

    public ChainPathChecker Add(IPathChecker checker)
    {
        _checkers.Add(checker);
        return this;
    }

    public bool Check(string path)
    {
        foreach (var checker in _checkers)
        {
            if (!checker.Check(path))
                return false;
        }
        return true;
    }
}

public class FileExistenceChecker : IPathChecker
{
    public bool Check(string path) => File.Exists(path);
}

public class ExtensionWhiteListChecker : IPathChecker
{
    private HashSet<string> _extensions;
    public ExtensionWhiteListChecker(IEnumerable<string> exts)
    {
        _extensions = [.. exts];
    }
    public ExtensionWhiteListChecker Add(string extension)
    {
        _extensions.Add(extension);
        return this;
    }
    public bool Check(string path)
    {
        return _extensions.Contains(Path.GetExtension(path).ToLower());
    }
}



public struct DirectoryExpanderSettings
{
    public string RelativeAnchorPath;
    public bool AcceptFiles, AcceptDirectories, IncludeSubDirectories;
    public IPathChecker DirectoryChecker, FileChecker;

    public DirectoryExpanderSettings()
    {
        RelativeAnchorPath = string.Empty;
        AcceptFiles = true;
        AcceptDirectories = true;
        IncludeSubDirectories = true;
        DirectoryChecker = new DefaultPathChecker();
        FileChecker = DirectoryChecker;
    }
}

public class DirectoryExpander
{
    public DirectoryExpanderSettings Settings { get; }
    public string? SourcePath { get; }

    public DirectoryExpander(string source, DirectoryExpanderSettings? settings = null)
    {
        Settings = settings ?? new DirectoryExpanderSettings();
        SourcePath = source;
    }

    public DirectoryExpander(string source, ref DirectoryExpander other)
    {
        Settings = other.Settings;
        SourcePath = source;
    }

    public IEnumerable<string> Expand()
    {
        if (SourcePath == null)
            yield break;

        HashSet<int> cache = [];

        if (Directory.Exists(SourcePath))
        {
            var directories = Directory.GetDirectories(SourcePath, "*", Settings.IncludeSubDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            foreach (var directory in directories)
            {
                if (Settings.AcceptDirectories && Settings.DirectoryChecker.Check(directory))
                {
                    int hash = directory.GetHashCode();
                    if (!cache.Contains(hash) && Settings.AcceptDirectories)
                    {
                        yield return directory;
                    }
                    cache.Add(hash);
                }
            }

            if (Settings.AcceptFiles)
            {
                var files = Directory.GetFiles(SourcePath, "*", Settings.IncludeSubDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
                foreach (var file in files)
                {
                    int hash = file.GetHashCode();
                    if (Settings.FileChecker.Check(file))
                    {
                        if (!cache.Contains(hash))
                        {
                            yield return file;
                        }
                        cache.Add(hash);
                    }
                }
            }
        }
        else if (File.Exists(SourcePath))
        {
            if (Settings.AcceptFiles && Settings.FileChecker.Check(SourcePath))
            {
                yield return SourcePath;
            }
        }
    }
}