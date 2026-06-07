using System;
using Cave;

namespace Tests.Cave.IO;

record PerformanceMeasureResult(string Name, long MemoryBytes, long Writes, long TryReads, long ObjReads, double ElapsedMs, string Notes = "") : BaseRecord
{
    double Seconds => ElapsedMs / 1000.0;
    public long WritesPerSec => (long)(Writes / Seconds);
    public long TryReadsPerSec => (long)(TryReads / Seconds);
    public long ObjReadsPerSec => (long)(ObjReads / Seconds);
    public double ReadWritePercent => Writes > 0 ? Math.Truncate(ObjReads * 1000d / Writes) / 10d : 0d;

    /// <summary>Calculates a weighted score based on reader/writer ratio.</summary>
    public double WeightedScore(int writerCount, int readerCount)
    {
        double rw, ww;
        if (readerCount * 2 > writerCount) { rw = 0.8; ww = 0.2; }
        else if (readerCount > writerCount) { rw = 0.6; ww = 0.4; }
        else if (readerCount == writerCount) { rw = 0.5; ww = 0.5; }
        else if (readerCount < writerCount * 2) { rw = 0.2; ww = 0.8; }
        else { rw = 0.4; ww = 0.6; }
        return rw * ObjReadsPerSec + ww * WritesPerSec;
    }
}
