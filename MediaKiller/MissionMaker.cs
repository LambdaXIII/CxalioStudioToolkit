using CxStudio;

namespace MediaKiller;

internal sealed class MissionMaker(Preset preset)
{
    class PresetInfoProvider : ITagStringProvider
    {
        readonly Preset _preset;
        public PresetInfoProvider(Preset p)
        {
            _preset = p;
        }
        public string? Replace(string? param)
        {
            return param switch
            {
                "id" => _preset.Id,
                "name" => _preset.Name,
                "folder" => Path.GetDirectoryName(_preset.PresetPath),
                "target_suffix" => _preset.TargetSuffix,
                "target_folder" => _preset.TargetFolder,
                null => null,
                _ => null
            };
        }
    }

    class PresetCustomInfoProvider : ITagStringProvider
    {
        readonly Preset _preset;
        public PresetCustomInfoProvider(Preset p)
        {
            _preset = p;
        }
        public string? Replace(string? param)
        {
            if (param is null) return null;
            return _preset.CustomValues.TryGetValue(param, out string? value) ? value : null;
        }
    }

    class CountNumberProvider : ITagStringProvider
    {
        private int _count = 0;
        public string? Replace(string? param)
        {
            return (++_count).ToString();
        }
    }

    private readonly Preset _preset = preset;
    private readonly PresetInfoProvider _presetInfoProvider = new PresetInfoProvider(preset);
    private readonly PresetCustomInfoProvider _presetCustomInfoProvider = new PresetCustomInfoProvider(preset);
    private readonly CountNumberProvider _countNumberProvider = new CountNumberProvider();

    /*   public bool Overwrite
       {
           get
           {
               if (XEnv.Instance.NoOverwrite) return false;
               if (XEnv.Instance.ForceOverwrite) return true;
               return _preset.Overwrite;
           }
       }*/

    public Mission Make(string source)
    {
        source = Path.GetFullPath(source);

        TagReplacer tagReplacer = new TagReplacer()
            .InstallProvider("preset", _presetInfoProvider)
            .InstallProvider("profile", _presetInfoProvider)
            .InstallProvider("custom", _presetCustomInfoProvider)
            .InstallProvider("seq", _countNumberProvider)
            .InstallProvider("source", new FileInfoProvider(source));

        //prepare target information
        var sourceParts = source.Split(Path.DirectorySeparatorChar).Reverse().ToArray();
        List<string> parentParts = [];
        for (int i = 1; i < _preset.TargetKeepParentLevels; i++)
        {
            if (i >= sourceParts.Length) break;
            parentParts.Prepend(sourceParts[i]);
        }

        string targetExt = _preset.TargetSuffix;
        if (!targetExt.StartsWith('.'))
        {
            targetExt = '.' + targetExt;
        }
        string targetFileName = $"{Path.GetFileNameWithoutExtension(source)}{targetExt}";
        ;

        string target = tagReplacer.ReplaceTags(_preset.TargetFolder);
        if (XEnv.Instance.OutputFolder is not null)
        {
            if (Path.IsPathFullyQualified(target))
                target = XEnv.Instance.OutputFolder;
            else target = Path.Combine(XEnv.Instance.OutputFolder, target);
        }
        target = Path.Combine(target, string.Join(Path.DirectorySeparatorChar, parentParts), targetFileName);

        tagReplacer.InstallProvider("target", new FileInfoProvider(target));

        //make mission

        Mission mission = new()
        {
            Source = source,
            FFmpegPath = _preset.FFmpegPath,
            GlobalOptions = _preset.GlobalOptions,
            Preset = _preset
        };

        if (_preset.HardwareAccelerate.Length > 0)
            mission.GlobalOptions.AddArgument("-hwaccel", _preset.HardwareAccelerate);

        //mission.GlobalOptions.AddArgument(Overwrite ? "-y" : "-n");

        foreach (ArgumentGroup input in _preset.Inputs)
        {
            ArgumentGroup i = new()
            {
                FileName = tagReplacer.ReplaceTags(input.FileName)
            };
            foreach (var option in input.Options)
            {
                i.AddArgument(option.Key, option.Value switch
                {
                    string s => tagReplacer.ReplaceTags(s),
                    _ => null
                });
            }
            mission.Inputs.Add(i);
        }

        foreach (ArgumentGroup output in _preset.Outputs)
        {
            ArgumentGroup o = new()
            {
                FileName = tagReplacer.ReplaceTags(output.FileName)
            };
            foreach (var option in output.Options)
            {
                o.AddArgument(option.Key, option.Value switch
                {
                    string s => tagReplacer.ReplaceTags(s),
                    _ => null
                });
            }
            mission.Outputs.Add(o);
        }

        return mission;
    }
}
