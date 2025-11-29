namespace Gantry.Core.Domain.Workspaces;

public class Workspace
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;

    public Workspace() { }

    public Workspace(string name, string path)
    {
        Name = name;
        Path = path;
    }
}
