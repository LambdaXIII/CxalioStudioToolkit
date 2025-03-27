using CxStudio.Core;
using System.Diagnostics;
using System.Text;
using System.Text.Json.Nodes;
namespace CxStudio.FFmpegHelper;

public class FFprobe
{
    public readonly string FFprobeBin;
    public static readonly string CommonArguments = "-v quiet -print_format json -show_format";

    public FFprobe(string ffprobe_bin = "ffprobe")
    {
        FFprobeBin = ffprobe_bin;
    }

    private ProcessStartInfo MakeProcessStartInfo(string source)
    {

        var result = new ProcessStartInfo
        {
            FileName = FFprobeBin,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
        foreach (var a in CommonArguments.Split(' '))
            result.ArgumentList.Add(a);

        //result.ArgumentList.Add(TextUtils.QuoteSpacedString(source));
        result.ArgumentList.Add(source);
        //result.ArgumentList.Add(">");
        //result.ArgumentList.Add(target);
        return result;
    }

    public MediaFormatInfo? GetFormatInfo(string source, int timeout = 10000)
    {
        string sourceFullPath = Path.GetFullPath(source);
        //string reportPath = Path.GetTempFileName();
        ProcessStartInfo info = MakeProcessStartInfo(sourceFullPath);

        //Console.WriteLine($"{FFprobeBin} {String.Join(" ", info.ArgumentList)}");

        using Process process = new()
        {
            StartInfo = info,
            EnableRaisingEvents = true
        };

        process.Start();
        bool done = process.WaitForExit(timeout);

        if (!done)
        {
            //Console.WriteLine("FFprobe timeout");
            process.Kill();
            //Console.Write(process.StandardOutput);
            //Console.Write(File.ReadAllText(reportPath));
            return null;
        }

        string totalOutput = process.StandardOutput.ReadToEnd();
        //+ process.StandardError.ReadToEnd();

        //string totalOutput = File.ReadAllText(reportPath);

        JsonNode formatNode = JsonNode.Parse(totalOutput)!["format"]!;

        var tags = formatNode["tags"]?.AsObject()?.ToDictionary(x => x.Key, x => x.Value!.GetValue<string>());

        return new MediaFormatInfo
        {
            FullPath = sourceFullPath,
            StreamCount = formatNode["nb_streams"]!.GetValue<uint>(),
            FormatName = formatNode["format_name"]!.GetValue<string>()!,
            FormatLongName = formatNode["format_long_name"]!.GetValue<string>()!,
            StartTime = Time.FromSeconds(double.Parse(formatNode["start_time"]!.GetValue<string>()!)),
            Duration = Time.FromSeconds(double.Parse(formatNode["duration"]!.GetValue<string>()!)),
            Size = FileSize.FromBytes(ulong.Parse(formatNode["size"]!.GetValue<string>()!)),
            Bitrate = FileSize.FromString(formatNode["bit_rate"]!.GetValue<string>()!),
            Tags = tags ?? []
        };
    }
}
