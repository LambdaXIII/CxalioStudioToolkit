using System.Diagnostics;
using Xabe.FFmpeg;
namespace MediaKiller;

class MissionRunner
{
    public readonly Mission Mission;

    public void run()
    {
        var conversion = FFmpeg.Conversions.New()
            .AddParameter(string.Join(' ', Mission.GetCommandElements()));

        conversion.OnProgress += (sender, args) =>
        {
            ar percent = (int)(Math.Round(args.Duration.TotalSeconds / args.TotalLength.TotalSeconds, 2) * 100);
            Debug.WriteLine($"[{args.Duration} / {args.TotalLength}] {percent}%");
        };
    }
}
