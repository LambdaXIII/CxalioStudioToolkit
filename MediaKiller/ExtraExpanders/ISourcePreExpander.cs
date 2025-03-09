namespace MediaKiller.ExtraExpanders;

public interface ISourcePreExpander
{
    bool IsAcceptable(string path);
    IEnumerable<string> Expand(string path);
}
