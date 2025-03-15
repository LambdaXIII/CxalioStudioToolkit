using CxStudio;

namespace MediaKiller;

class ScriptMaker
{
    private readonly TextWriter Writer;
    private readonly HashSet<string> Folders = [];

    public ScriptMaker(TextWriter? writer)
    {
        Writer = writer ?? throw new ArgumentNullException(nameof(writer));
    }

    ~ScriptMaker()
    {
        Writer.Flush();
        Writer.Close();
    }

    public void WriteMission(Mission mission)
    {
        foreach (ArgumentGroup oGroup in mission.Outputs)
        {
            string? folder = Path.GetDirectoryName(
                Path.GetFullPath(oGroup.FileName));

            if (folder is null || Folders.Contains(folder))
            {
                continue;
            }

            Folders.Add(folder);
            Writer.WriteLine($"mkdir -p {TextUtils.QuoteSpacedString(folder)}");
        }

        Writer.WriteLine(mission.FullCommand);
    }

}
