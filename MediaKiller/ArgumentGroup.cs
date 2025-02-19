namespace MediaKiller;

public struct ArgumentGroup
{
    public readonly Dictionary<string, string?> Options = [];
    public string FileName = string.Empty;

    public ArgumentGroup() { }

    public static string CheckKey(string key)
    {
        key = key.Trim();
        if (key[0] != '-')
        {
            key = '-' + key;
        }
        return key;
    }

    public readonly ArgumentGroup AddArgument(string name, string? value = null)
    {
        string key = CheckKey(name);
        Options[key] = value;
        return this;
    }

    public readonly ArgumentGroup AddArguments(string arguments)
    {
        string? last_key = null;
        foreach (string arg in arguments.Split(' '))
        {
            if (arg[0] == '-')
            {
                last_key = arg;
                AddArgument(arg);
            }
            else if (last_key is not null)
            {
                AddArgument(last_key, arg);
                last_key = null;
            }
        }
        return this;
    }

    public readonly ArgumentGroup RemoveArgument(string name)
    {
        string key = CheckKey(name);
        Options.Remove(key);
        return this;
    }

    public readonly IEnumerable<string> Arguments()
    {
        foreach (var (key, value) in Options)
        {
            if (value is null)
            {
                yield return key;
            }
            else
            {
                yield return key;
                yield return value;
            }
        }
    }
}
