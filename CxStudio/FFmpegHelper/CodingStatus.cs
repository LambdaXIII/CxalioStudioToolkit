namespace CxStudio.FFmpegHelper;

public struct CodingStatus
{
    public string FFmpegBin { get; init; }
    public string FfmpegArguments { get; init; }
    public DateTime ProcessStart { get; init; }
    public ulong CurrentFrame { get; init; }
    public double CurrentFps { get; init; }
    public double CurrentQ { get; init; }
    public FileSize CurrentLSize { get; init; }
    public Time CurrentTime { get; init; }
    public FileSize CurrentBitrate { get; init; }
    public double CurrentSpeed { get; init; }

}
