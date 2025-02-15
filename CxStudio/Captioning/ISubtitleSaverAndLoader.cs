
using System.Collections.Generic;

namespace CxStudio.Captioning;

public interface ISubtitleLoader
{
    IEnumerable<StaticSubtitle> LoadSubtitle();
}

public interface ISubtitleSaver
{
    void SaveSubtitle(ref StaticSubtitle subtitle);
}