namespace Gantry.Core.Domain.Collections;

public class HeaderItem
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool Disabled { get => !IsActive; set => IsActive = !value; }
}
