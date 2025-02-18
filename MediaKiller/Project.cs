using CxStudio;
using Nett;

namespace MediaKiller;


public sealed class Project
{
    const string DefaultSuffixes = ".mov .mp4 .mkv .avi .wmv .flv .webm .m4v .ts .m2ts .m2t .mts .m2v .m4v .vob .3gp .3g2 .f4v .ogv .ogg .mpg .mpeg .mxf .asf .rm .rmvb .divx .xvid .h264 .h265 .hevc .vp8 .vp9 .av1 .avc .avchd .flac .mp3 .wav .m4a .aac .ogg .wma .flac .alac .aiff .ape .dsd .pcm .ac3 .dts .eac3 .mp2 .mpa .opus .mka .mkv .webm .flv .ts .m2ts .m2t .mts .m2v .m4v .vob .wav .m4a .aac .ogg .wma .flac .alac .aiff .ape .dsd .pcm .ac3 .dts .eac3 .mp2 .mpa .opus .mka";

    public string Id { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string FfmpegPath { get; private set; } = string.Empty;

    public HashSet<string> AcceptableSuffixes { get; private set; } = [];

    public string TargetFolder { get; private set; } = string.Empty;

    public string TargetSuffix { get; private set; } = string.Empty;

    public int TargetParentLevel { get; private set; } = 0;

    public struct DetailGroup { public string NamePattern; public string options; }

    public List<DetailGroup> InputGroups { get; private set; } = [];
    public List<DetailGroup> OutputGroups { get; private set; } = [];

    public static string DefaultSuffixes1 => DefaultSuffixes;

    private static string CheckSuffix(string suffix)
    {
        if (suffix[0] != '.')
        {
            suffix = '.' + suffix;
        }
        return suffix.ToLower();
    }

    private void ParseSourceTable(ref TomlTable sourceTable)
    {
        var table = sourceTable.ToDictionary();
        bool ignore_default_suffixes = table.ContainsKey("ignore_default_suffixes") && sourceTable.Get<bool>("ignore_default_suffixes");
        if (!ignore_default_suffixes)
        {
            foreach (var suffix in DefaultSuffixes.Split(' '))
            {
                this.AcceptableSuffixes.Add(CheckSuffix(suffix));
            }
        }
        if (table.TryGetValue("suffix_includes", out object? includes))
        {
            if (includes is string includesString)
            {
                foreach (var suffix in includesString.Split(' '))
                {
                    this.AcceptableSuffixes.Add(CheckSuffix(suffix));
                }
            }
            else if (includes is TomlArray includesArray)
            {
                foreach (var suffix in includesArray.To<string>())
                {
                    this.AcceptableSuffixes.Add(CheckSuffix(suffix));
                }
            }
        }
        if (table.TryGetValue("suffix_excludes", out object? excludes))
        {
            if (excludes is string excludesString)
            {
                foreach (var suffix in excludesString.Split(' '))
                {
                    this.AcceptableSuffixes.Remove(CheckSuffix(suffix));
                }
            }
            else if (excludes is TomlArray excludesArray)
            {
                foreach (var suffix in excludesArray.To<string>())
                {
                    this.AcceptableSuffixes.Remove(CheckSuffix(suffix));
                }
            }
        }
    }

    private void ParseGeneralTable(ref TomlTable generalTable)
    {
        this.Id = generalTable.Get<string>("profile_id");
        this.Name = generalTable.Get<string>("name");
        this.Description = generalTable.Get<string>("description");

        var ffmpeg_path = generalTable.Get<string>("ffmpeg_path") ?? "ffmpeg";
        CommandFinder finder = new();
        finder.AddSearchPath(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments))
            .AddSearchPath(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
        this.FfmpegPath = finder.Find(ffmpeg_path) ?? ffmpeg_path;
    }

    public static Project ParseToml(string profile_path)
    {
        Project profile = new();
        var pro_table = Toml.ReadFile(profile_path);

        TomlTable general_table = pro_table.Get<TomlTable>("general");
        profile.ParseGeneralTable(ref general_table);

        TomlTable source_table = pro_table.Get<TomlTable>("source");
        profile.ParseSourceTable(ref source_table);

        profile.TargetFolder = pro_table.Get<TomlTable>("target").Get<string>("folder");
        profile.TargetSuffix = pro_table.Get<TomlTable>("target").Get<string>("suffix");
        profile.TargetParentLevel = pro_table.Get<TomlTable>("target").Get<int>("target_parent_level");

        var inputGroups = pro_table.Get<TomlTableArray>("input");
        foreach (TomlTable group in inputGroups.Items)
        {
            profile.InputGroups.Add(new DetailGroup
            {
                NamePattern = group.Get<string>("name_pattern"),
                options = group.Get<string>("options")
            });
        }

        var outputGroups = pro_table.Get<TomlTableArray>("output");
        foreach (TomlTable group in outputGroups.Items)
        {
            profile.OutputGroups.Add(new DetailGroup
            {
                NamePattern = group.Get<string>("name_pattern"),
                options = group.Get<string>("options")
            });
        }

        return profile;
    }
}
