using System;
using System.Collections.Generic;

namespace Gantry.Core.Domain.Http;

public class SavedResponse
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public string Body { get; set; } = string.Empty;
    public Dictionary<string, string> Headers { get; set; } = new();
    public DateTime Timestamp { get; set; }
    public long DurationMs { get; set; }
    public long Size { get; set; }
}
