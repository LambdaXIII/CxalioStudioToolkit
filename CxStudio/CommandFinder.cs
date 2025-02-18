namespace CxStudio;

public class CommandFinder
{
    public bool SubDirectorySearchEnabled { get; set; } = false;

    public List<string> SearchPaths { get; } = [];


    public CommandFinder(bool containsEnvPaths = true, bool containsCurrentDir = true)
    {
        if (containsEnvPaths)
        {
            string? envPath = Environment.GetEnvironmentVariable("PATH");
            if (envPath is not null)
            {
                SearchPaths.AddRange(envPath.Split(';'));
            }
        }

        if (containsCurrentDir)
        {
            SearchPaths.Add(Directory.GetCurrentDirectory());
        }
    }

    private static string CleanPath(string path)
    {
        path = Path.GetFullPath(path);
        if (File.Exists(path))
        {
            path = Path.GetDirectoryName(path) ?? path;
        }
        path = path.TrimEnd(Path.DirectorySeparatorChar);
        return path;
    }


    public CommandFinder AddSearchPath(string path)
    {
        if (!SearchPaths.Contains(path))
            SearchPaths.Add(CleanPath(path));
        return this;
    }

    public string? Find(string cmd)
    {
        foreach (string search_path in SearchPaths)
        {
            string? full_path = Path.Combine(search_path, cmd);
            if (File.Exists(full_path))
            {
                return full_path;
            }
            if (SubDirectorySearchEnabled)
            {
                foreach (string sub_dir in Directory.GetDirectories(search_path))
                {
                    full_path = Path.Combine(sub_dir, cmd);
                    if (File.Exists(full_path))
                    {
                        return full_path;
                    }
                }
            }
        }
        return null;
    }

    public static string? QuickFind(string cmd)
    {
        return new CommandFinder().Find(cmd);
    }
}
