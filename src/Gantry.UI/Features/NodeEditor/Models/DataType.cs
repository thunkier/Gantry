namespace Gantry.UI.Features.NodeEditor.Models;

/// <summary>
/// Represents the data type of a pin connection.
/// </summary>
public enum DataType
{
    /// <summary>
    /// Can connect to any type.
    /// </summary>
    Any,

    /// <summary>
    /// String data type.
    /// </summary>
    String,

    /// <summary>
    /// Numeric data type (integers and floats).
    /// </summary>
    Number,

    /// <summary>
    /// Boolean data type.
    /// </summary>
    Boolean,

    /// <summary>
    /// Complex object data type.
    /// </summary>
    Object,

    /// <summary>
    /// Array/collection data type.
    /// </summary>
    Array,

    /// <summary>
    /// Execution flow trigger (no data).
    /// </summary>
    Trigger
}
