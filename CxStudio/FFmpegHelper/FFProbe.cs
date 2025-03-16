using System.Diagnostics;
using System.Text;
using System.Text.Json.Nodes;

namespace CxStudio.FFmpegHelper;

public class FFprobe
{
    public readonly string FFprobeBin;

    public FFprobe(string ffprobe_bin = "ffprobe")
    {
        FFprobeBin = ffprobe_bin;
    }

    public MediaFormatInfo? GetFormatInfo(string source)
    {
        try
        {
            string sourceFullPath = Path.GetFullPath(source);
            ProcessStartInfo info = new()
            {
                FileName = FFprobeBin,
                Arguments = $"-v quiet -print_format json -show_format {TextUtils.QuoteSpacedString(sourceFullPath)}",
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            Process process = new()
            {
                StartInfo = info,
                EnableRaisingEvents = true
            };
            process.Start();
            process.WaitForExit();

            JsonNode formatNode = JsonNode.Parse(process.StandardOutput.ReadToEnd())!["format"]!;

            var tags = formatNode["tags"]?.AsObject()?.ToDictionary(x => x.Key, x => x.Value!.GetValue<string>());

            return new MediaFormatInfo
            {
                FullPath = sourceFullPath,
                StreamCount = formatNode["nb_streams"]!.GetValue<uint>(),
                FormatName = formatNode["format_name"]!.GetValue<string>()!,
                FormatLongName = formatNode["format_long_name"]!.GetValue<string>()!,
                StartTime = new Time(double.Parse(formatNode["start_time"]!.GetValue<string>()!)),
                Duration = new Time(double.Parse(formatNode["duration"]!.GetValue<string>()!)),
                Size = FileSize.FromBytes(ulong.Parse(formatNode["size"]!.GetValue<string>()!)),
                Bitrate = FileSize.FromString(formatNode["bit_rate"]!.GetValue<string>()!),
                Tags = tags ?? []
            };
        }
        catch (Exception)
        {
            return null;
        }
    }
}
