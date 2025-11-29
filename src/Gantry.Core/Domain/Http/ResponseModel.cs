using System;
using System.Collections.Generic;

namespace Gantry.Core.Domain.Http;

public class ResponseModel
{
    public int StatusCode { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public Dictionary<string, string> Headers { get; set; } = new();
    public string Body { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public TimeSpan? TimeToFirstByte { get; set; }
    public long Size { get; set; }
    public bool IsSuccess => StatusCode >= 200 && StatusCode < 300;
}