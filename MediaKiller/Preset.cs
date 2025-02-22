using CxStudio;
using Nett;

namespace MediaKiller;

internal sealed class Preset
{
    public string PresetPath = string.Empty;

    public string Id = string.Empty;
    public string Name = string.Empty;
    public string Description = string.Empty;
    public string FFmpegPath = "ffmpeg";

    public bool Overwrite = false;
    public string HardwareAccelerate = "auto";
    public ArgumentGroup GlobalOptions = new();

    public readonly Dictionary<string, string> CustomValues = new();

    public readonly HashSet<string> AcceptableSuffixes = [];

    public string TargetSuffix = string.Empty;
    public string TargetFolder = string.Empty;
    public int TargetKeepParentLevels = 0;

    public readonly List<ArgumentGroup> Inputs = [];
    public readonly List<ArgumentGroup> Outputs = [];

    const string DefaultSuffixes = ".mov .mp4 .mkv .avi .wmv .flv .webm .m4v .ts .m2ts .m2t .mts .m2v .m4v .vob .3gp .3g2 .f4v .ogv .ogg .mpg .mpeg .mxf .asf .rm .rmvb .divx .xvid .h264 .h265 .hevc .vp8 .vp9 .av1 .avc .avchd .flac .mp3 .wav .m4a .aac .ogg .wma .flac .alac .aiff .ape .dsd .pcm .ac3 .dts .eac3 .mp2 .mpa .opus .mka .mkv .webm .flv .ts .m2ts .m2t .mts .m2v .m4v .vob .wav .m4a .aac .ogg .wma .flac .alac .aiff .ape .dsd .pcm .ac3 .dts .eac3 .mp2 .mpa .opus .mka";




    private void LoadGeneralTable(ref TomlTable profile)
    {
        var table = profile.TryGetValue("general", out var value) ? value as TomlTable : null;
        if (table is null)
            return;

        Id = table.Get<string>("preset_id");
        Name = table.Get<string>("name");
        Description = table.Get<string>("description");

        var ffmpeg = table.Get<string>("ffmpeg");
        FFmpegPath = CommandFinder.QuickFind(ffmpeg) ?? "ffmpeg";

        Overwrite = table.Get<bool>("overwrite");
        HardwareAccelerate = table.Get<string>("hardware_accelerate");

        string options = table.Get<string>("options");
        GlobalOptions.AddArguments(options);
    }

    private void LoadCustomTable(ref TomlTable profile)
    {
        var table = profile.TryGetValue("custom", out var value) ? value as TomlTable : null;
        if (table is null)
            return;

        foreach (var (key, v) in table)
        {
            CustomValues[key] = v.Get<string>();
        }
    }

    private void LoadSourceTable(ref TomlTable profile)
    {
        var table = profile.TryGetValue("source", out var value) ? value as TomlTable : null;
        if (table is null)
            return;

        bool ignore_default_suffixes = table.Get<bool>("ignore_default_suffixes");

        if (!ignore_default_suffixes)
        {
            foreach (var suffix in DefaultSuffixes.Split(' '))
            {
                AcceptableSuffixes.Add(suffix);
            }
        }

        var sourceDict = table.ToDictionary();
        sourceDict.TryGetValue("suffix_includes", out object? includes);
        switch (includes)
        {
            case string includesString:
                foreach (var suffix in includesString.Split(' '))
                {
                    AcceptableSuffixes.Add(suffix);
                }
                break;
            case TomlArray includesArray:
                foreach (var suffix in includesArray.To<string>())
                {
                    AcceptableSuffixes.Add(suffix);
                }
                break;
        }

        sourceDict.TryGetValue("suffix_excludes", out object? excludes);
        switch (excludes)
        {
            case string excludesString:
                foreach (var suffix in excludesString.Split(' '))
                {
                    AcceptableSuffixes.Remove(suffix);
                }
                break;
            case TomlArray excludesArray:
                foreach (var suffix in excludesArray.To<string>())
                {
                    AcceptableSuffixes.Remove(suffix);
                }
                break;
        }
    }

    private void LoadTargetTable(ref TomlTable profile)
    {
        var table = profile.TryGetValue("target", out var value) ? value as TomlTable : null;
        if (table is null)
            return;

        TargetSuffix = table.Get<string>("suffix");
        TargetFolder = table.Get<string>("folder");
        TargetKeepParentLevels = table.Get<int>("keep_parent_level");
    }

    private void LoadInputTables(ref TomlTable profile)
    {
        var tables = profile.TryGetValue("input", out var value) ? value as TomlTableArray : null;
        if (tables is null)
            return;

        foreach (TomlTable table in tables.Items)
        {
            var input = new ArgumentGroup();
            input.FileName = table.Get<string>("filename");
            input.AddArguments(table.Get<string>("options"));
            Inputs.Add(input);
        }
    }

    private void LoadOutputTables(ref TomlTable profile)
    {
        var tables = profile.TryGetValue("output", out var value) ? value as TomlTableArray : null;
        if (tables is null)
            return;

        foreach (TomlTable table in tables.Items)
        {
            var output = new ArgumentGroup();
            output.FileName = table.Get<string>("filename");
            output.AddArguments(table.Get<string>("options"));
            Outputs.Add(output);
        }
    }



    public static Preset Load(string path)
    {
        Preset result = new()
        {
            PresetPath = path
        };

        var profile = Toml.ReadFile(path);
        result.LoadGeneralTable(ref profile);
        result.LoadCustomTable(ref profile);
        result.LoadSourceTable(ref profile);
        result.LoadTargetTable(ref profile);
        result.LoadInputTables(ref profile);
        result.LoadOutputTables(ref profile);

        return result;
    }

}