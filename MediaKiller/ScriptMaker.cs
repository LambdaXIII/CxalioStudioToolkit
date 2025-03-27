using CxStudio.Core;

namespace MediaKiller;

class ScriptMaker
{
    private IEnumerable<Mission> _missions;
    private HashSet<string> _cached_folders = [];

    public ScriptMaker(IEnumerable<Mission> missions)
    {
        _missions = missions;
    }

    public IEnumerable<string> Lines()
    {
        foreach (Mission m in _missions)
        {
            foreach (var oGroup in m.Outputs)
            {
                string oFolder = Path.GetDirectoryName(oGroup.FileName)!;
                if (_cached_folders.Contains(oFolder)) continue;

                _cached_folders.Add(oFolder);
                yield return $"mkdir -p {oFolder.AutoQuote()}";
            }
            yield return m.FullCommand;
        }
    }

    public void WriteTo(ref StreamWriter writer)
    {
        foreach (string line in Lines())
            writer.WriteLine(line);
    }
}
