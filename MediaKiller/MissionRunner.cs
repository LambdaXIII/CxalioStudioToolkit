using System.Diagnostics;
namespace MediaKiller;

class MissionRunner
{
    public readonly Mission Mission;

    public MissionRunner(Mission mission)
    {
        Mission = mission;
    }

    public void Run()
    {
        foreach (var output_group in Mission.Outputs)
        {
            string? dir = Path.GetDirectoryName(output_group.FileName);
            if (dir is not null)
                Directory.CreateDirectory(dir);
        }

        ProcessStartInfo start_info = new()
        {
            FileName = Mission.FFmpegPath,
            Arguments = string.Join(' ', Mission.GetCommandElements()),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = false,
        };

        using (Process process = new())
        {
            process.StartInfo = start_info;
            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    Console.WriteLine(e.Data ?? "");
            };
            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    Console.WriteLine(e.Data ?? "");
            };
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
        }
    }
}
