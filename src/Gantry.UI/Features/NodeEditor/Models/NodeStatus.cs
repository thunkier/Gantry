namespace Gantry.UI.Features.NodeEditor.Models;

/// <summary>
/// Represents the execution status of a node.
/// </summary>
public enum NodeStatus
{
    /// <summary>
    /// Node is idle and not executing.
    /// </summary>
    Idle,

    /// <summary>
    /// Node is currently executing.
    /// </summary>
    Running,

    /// <summary>
    /// Node execution completed successfully.
    /// </summary>
    Success,

    /// <summary>
    /// Node execution failed with an error.
    /// </summary>
    Error
}
