
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CxStudio.Captioning;

public class SrtLoader : ISubtitleLoader, IDisposable
{
    private readonly StreamReader sr;


    private static readonly string sequenceNumberPattern = @"^\d+$";
    private static readonly string timePattern = @"^(\d{2}:\d{2}:\d{2},\d{3}) --> (\d{2}:\d{2}:\d{2},\d{3})$";

    private enum SrtState
    {
        Ready,
        Time,
        Content
    }

    public SrtLoader(ref Stream stream)
    {
        sr = new StreamReader(stream);
    }

    public IEnumerable<StaticSubtitle> LoadSubtitle()
    {
        SrtState state = SrtState.Ready;
        StaticSubtitle subtitle = new StaticSubtitle();
        string? line;
        List<string> contentLines = new List<string>();

        while ((line = sr.ReadLine()) != null)
        {
            switch (state)
            {
                case SrtState.Ready:
                    if (Regex.IsMatch(line, sequenceNumberPattern))
                    {
                        state = SrtState.Time;
                        subtitle.commet = line; // Save Srt Sequence Number as comment
                    }
                    break;

                case SrtState.Time:
                    var match = Regex.Match(line, timePattern);
                    if (match.Success)
                    {
                        subtitle.start = Time.FromTimestamp(match.Groups[1].Value);
                        subtitle.end = Time.FromTimestamp(match.Groups[2].Value);
                        state = SrtState.Content;
                    }
                    else
                    {
                        state = SrtState.Ready; // Reset state if time pattern is invalid
                    }
                    break;

                case SrtState.Content:
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        subtitle.content = string.Join(Environment.NewLine, contentLines);
                        contentLines.Clear();
                        yield return subtitle;
                        subtitle = new StaticSubtitle();
                        state = SrtState.Ready;
                    }
                    else
                    {
                        contentLines.Add(line);
                    }
                    break;
            }
        }

        // Handle the last subtitle block if the file does not end with a blank line
        if (state == SrtState.Content && contentLines.Count > 0)
        {
            subtitle.content = string.Join(Environment.NewLine, contentLines);
            yield return subtitle;
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            sr.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true); GC.SuppressFinalize(this);
    }
}
