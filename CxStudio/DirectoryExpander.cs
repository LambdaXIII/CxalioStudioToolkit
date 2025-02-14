namespace CxStudio
{

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

    public class ExtensionWhiteListChecker : IPathChecker
    {
        private HashSet<string> _extensions;
        public ExtensionWhiteListChecker(IEnumerable<string> exts)
        {
            _extensions = new HashSet<string>(exts);
        }
        public ExtensionWhiteListChecker Add(string extension)
        {
            _extensions.Add(extension);
            return this;
        }
        public bool Check(string path)
        {
            return _extensions.Contains(Path.GetExtension(path));
        }
    }



    public struct DirectoryExpanderSettings
    {
        public string RelativeAnchorPath;
        public bool AcceptFiles, AcceptDirectories, IncludeSubDirectories;
        public IPathChecker DirectoryValidator, FileValidator;

        public DirectoryExpanderSettings()
        {
            RelativeAnchorPath = string.Empty;
            AcceptFiles = true;
            AcceptDirectories = true;
            IncludeSubDirectories = true;
            DirectoryValidator = new DefaultPathChecker();
            FileValidator = DirectoryValidator;
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

        public IEnumerable<string> GetDirectories()
        {
            if (SourcePath == null)
                yield break;

            HashSet<int> cache = new HashSet<int>();

            if (Directory.Exists(SourcePath))
            {
                var directories = Directory.GetDirectories(SourcePath, "*", Settings.IncludeSubDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
                foreach (var directory in directories)
                {
                    if (Settings.AcceptDirectories && Settings.DirectoryValidator.Check(directory))
                    {
                        int hash = directory.GetHashCode();
                        if (!cache.Contains(hash))
                        {
                            cache.Add(hash);
                            yield return directory;
                        }
                    }
                }

                if (Settings.AcceptFiles)
                {
                    var files = Directory.GetFiles(SourcePath, "*", Settings.IncludeSubDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
                    foreach (var file in files)
                    {
                        int hash = file.GetHashCode();
                        if (Settings.FileValidator.Check(file))
                        {
                            if (!cache.Contains(hash))
                            {
                                cache.Add(hash);
                                yield return file;
                            }
                        }
                    }
                }
            }
            else if (File.Exists(SourcePath))
            {
                if (Settings.AcceptFiles && Settings.FileValidator.Check(SourcePath))
                {
                    yield return SourcePath;
                }
            }
        }
    }
}