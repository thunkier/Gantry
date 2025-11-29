using System;

namespace Gantry.Core.Domain.Collections;

public class RequestHistoryItem
{
    public DateTime Timestamp { get; set; }
    public int StatusCode { get; set; }
    public long DurationMs { get; set; }
    public long Size { get; set; }
}
