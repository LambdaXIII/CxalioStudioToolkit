using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CxStudio.Captioning;

class SrtSaver:ISubtitleSaver,IDisposable
{
    private readonly StreamWriter sw;
    private uint sequenceNumber = 0;

    public SrtSaver(ref Stream sw)
    {
        this.sw = new StreamWriter(sw);
    }

    public void SaveSubtitle(ref StaticSubtitle subtitle)
    {
        sw.WriteLine(sequenceNumber);
        sw.WriteLine($"{subtitle.start?.ToTimestamp()} --> {subtitle.end?.ToTimestamp()}");
        sw.WriteLine(subtitle.content);
        sw.WriteLine();
        sequenceNumber++;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            sw.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true); GC.SuppressFinalize(this);
    }
}

