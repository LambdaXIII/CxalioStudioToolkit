namespace CxStudio.CxConfig;

public class CxConfigManager
{
    private static Lazy<string> _lazyConfigDirectory = new(
        () => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CxStudio")
        );
    public static string ConfigDirectory => _lazyConfigDirectory.Value;


    public string AppName { get; private set; }
    public string AppVersion { get; private set; }
    public string AppConfigDirectory { get; private set; }
    public string AppCacheDirectory { get; private set; }

    public CxConfigManager(string appName, string appVersion = "0")
    {
        AppName = appName;
        AppVersion = appVersion;
        AppConfigDirectory = Path.Combine(ConfigDirectory, appName);
        AppCacheDirectory = Path.Combine(ConfigDirectory, appName, "Cache");
    }

    public void ClearCaches()
    {
        if (Directory.Exists(AppCacheDirectory))
            Directory.Delete(AppCacheDirectory, true);
    }

    public string GetCacheFile(string path)
    {
        var cachePath = Path.Combine(AppCacheDirectory, path);
        var cacheDir = Path.GetDirectoryName(cachePath);
        if (!string.IsNullOrWhiteSpace(cacheDir) && !Directory.Exists(cacheDir))
            Directory.CreateDirectory(cacheDir);
        return cachePath;
    }

    public string GetConfigFile(string path)
    {
        var configPath = Path.Combine(AppConfigDirectory, path);
        var configDir = Path.GetDirectoryName(configPath);
        if (!string.IsNullOrWhiteSpace(configDir) && !Directory.Exists(configDir))
            Directory.CreateDirectory(configDir);
        return configPath;
    }
}
