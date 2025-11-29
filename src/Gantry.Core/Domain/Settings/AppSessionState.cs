namespace Gantry.Core.Domain.Settings;

public class AppSessionState
{
    public string? LastActiveWorkspacePath { get; set; }
    public List<string> RecentWorkspaces { get; set; } = new();
}