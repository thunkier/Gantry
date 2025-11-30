namespace Gantry.Core.Domain.Settings;

public class Variable
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Type { get; set; } = "string";
    public string Description { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
}
