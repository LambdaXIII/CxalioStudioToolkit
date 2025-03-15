using System.Text.RegularExpressions;

namespace CxStudio;

public struct FileSize
{
    public enum Standards : ushort
    {
        SI = 1000,
        IEC = 1024
    }

    public Standards Standard { get; init; }

    private ulong _byte;

    public readonly ulong Bytes => _byte;
    public readonly double Kilobytes => _byte / Math.Pow((ushort)Standard, 1);
    public readonly double Megabytes => _byte / Math.Pow((ushort)Standard, 2);
    public readonly double Gigabytes => _byte / Math.Pow((ushort)Standard, 3);
    public readonly double Terabytes => _byte / Math.Pow((ushort)Standard, 4);
    public readonly double Petabytes => _byte / Math.Pow((ushort)Standard, 5);
    public readonly double Exabytes => _byte / Math.Pow((ushort)Standard, 6);
    public readonly double Zettabytes => _byte / Math.Pow((ushort)Standard, 7);
    public readonly double Yottabytes => _byte / Math.Pow((ushort)Standard, 8);



    static public FileSize FromBytes(ulong bytes, Standards standard = Standards.SI)
    {
        return new FileSize
        {
            Standard = standard,
            _byte = bytes
        };
    }

    static public FileSize FromKilobytes(double kilobytes, Standards standard = Standards.SI)
    {
        return new FileSize
        {
            Standard = standard,
            _byte = (ulong)(kilobytes * Math.Pow((ushort)standard, 1))
        };
    }

    static public FileSize FromMegabytes(double megabytes, Standards standard = Standards.SI)
    {
        return new FileSize
        {
            Standard = standard,
            _byte = (ulong)(megabytes * Math.Pow((ushort)standard, 2))
        };
    }

    static public FileSize FromGigabytes(double gigabytes, Standards standard = Standards.SI)
    {
        return new FileSize
        {
            Standard = standard,
            _byte = (ulong)(gigabytes * Math.Pow((ushort)standard, 3))
        };
    }

    static public FileSize FromTerabytes(double terabytes, Standards standard = Standards.SI)
    {
        return new FileSize
        {
            Standard = standard,
            _byte = (ulong)(terabytes * Math.Pow((ushort)standard, 4))
        };
    }

    static public FileSize FromPetabytes(double petabytes, Standards standard = Standards.SI)
    {
        return new FileSize
        {
            Standard = standard,
            _byte = (ulong)(petabytes * Math.Pow((ushort)standard, 5))
        };
    }

    static public FileSize FromExabytes(double exabytes, Standards standard = Standards.SI)
    {
        return new FileSize
        {
            Standard = standard,
            _byte = (ulong)(exabytes * Math.Pow((ushort)standard, 6))
        };
    }

    static public FileSize FromZettabytes(double zettabytes, Standards standard = Standards.SI)
    {
        return new FileSize
        {
            Standard = standard,
            _byte = (ulong)(zettabytes * Math.Pow((ushort)standard, 7))
        };
    }

    static public FileSize FromYottabytes(double yottabytes, Standards standard = Standards.SI)
    {
        return new FileSize
        {
            Standard = standard,
            _byte = (ulong)(yottabytes * Math.Pow((ushort)standard, 8))
        };
    }

    static public FileSize FromString(string size, Standards standard = Standards.SI)
    {
        var match = Regex.Match(size, @"(?<size>\d+(\.\d+)?)\s*(?<unit>[KMGTPEZY]i?B)", RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            throw new FormatException("Invalid size format.");
        }
        var value = double.Parse(match.Groups["size"].Value);
        var unit = match.Groups["unit"].Value.ToUpper();
        return unit switch
        {
            "KB" => FromKilobytes(value, standard),
            "MB" => FromMegabytes(value, standard),
            "GB" => FromGigabytes(value, standard),
            "TB" => FromTerabytes(value, standard),
            "PB" => FromPetabytes(value, standard),
            "EB" => FromExabytes(value, standard),
            "ZB" => FromZettabytes(value, standard),
            "YB" => FromYottabytes(value, standard),
            _ => throw new FormatException("Invalid size format.")
        };
    }

    public override string ToString()
    {
        var units = new[] { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
        var values = new[] { Bytes, Kilobytes, Megabytes, Gigabytes, Terabytes, Petabytes, Exabytes, Zettabytes, Yottabytes };

        for (int i = values.Length - 1; i >= 0; i--)
        {
            if (values[i] >= 1)
            {
                return $"{values[i]:F2} {units[i]}";
            }
        }

        return $"{Bytes} B";
    }
}
