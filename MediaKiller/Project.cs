using Nett;

namespace MediaKiller;


public sealed class Project
{
    const string DefaultSuffixes = ".mov .mp4 .mkv .avi .wmv .flv .webm .m4v .ts .m2ts .m2t .mts .m2v .m4v .vob .3gp .3g2 .f4v .ogv .ogg .mpg .mpeg .mxf .asf .rm .rmvb .divx .xvid .h264 .h265 .hevc .vp8 .vp9 .av1 .avc .avchd .flac .mp3 .wav .m4a .aac .ogg .wma .flac .alac .aiff .ape .dsd .pcm .ac3 .dts .eac3 .mp2 .mpa .opus .mka .mkv .webm .flv .ts .m2ts .m2t .mts .m2v .m4v .vob .wav .m4a .aac .ogg .wma .flac .alac .aiff .ape .dsd .pcm .ac3 .dts .eac3 .mp2 .mpa .opus .mka";

    public string Id { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string FfmpegPath { get; private set; } = string.Empty;

    public HashSet<string> AcceptableSuffixes { get; private set; } = new();

    public string TargetFolder { get; private set; } = string.Empty;

    public string TargetSuffix { get; private set; } = string.Empty;

    public int TargetParentLevel { get; private set; } = 0;

    public struct DetailGroup { public string NamePattern; public string options; }

    public List<DetailGroup> InputGroups { get; private set; } = new();
    public List<DetailGroup> OutputGroups { get; private set; } = new();

    public static string DefaultSuffixes1 => DefaultSuffixes;

    private static string CheckSuffix(string suffix)
    {
        if (suffix[0] != '.')
        {
            suffix = '.' + suffix;
        }
        return suffix.ToLower();
    }

    public static Project ParseToml(string profile_path)
    {
        Project profile = new();
        var pro_table = Toml.ReadFile(profile_path);

        profile.Id = pro_table.Get<TomlTable>("general").Get<string>("profile_id");
        profile.Name = pro_table.Get<TomlTable>("general").Get<string>("name");
        profile.Description = pro_table.Get<TomlTable>("general").Get<string>("description");
        profile.FfmpegPath = pro_table.Get<TomlTable>("general").Get<string>("ffmpeg_path");

        if (!pro_table.Get<TomlTable>("source").Get<bool>("ignore_default_suffixes"))
        {

            foreach (var suffix in DefaultSuffixes.Split(' '))
            {
                profile.AcceptableSuffixes.Add(CheckSuffix(suffix));
            }

        }

        foreach (var suffix in pro_table.Get<TomlTable>("source").Get<TomlArray>("suffix_includes").To<string>())
        {
            profile.AcceptableSuffixes.Add(CheckSuffix(suffix));
        }

        foreach (var suffix in pro_table.Get<TomlTable>("source").Get<TomlArray>("suffix_excludes").To<string>())
        {
            profile.AcceptableSuffixes.Remove(CheckSuffix(suffix));
        }

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
