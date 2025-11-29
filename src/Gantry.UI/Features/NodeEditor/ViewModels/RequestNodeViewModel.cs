using Gantry.Core.Domain.Collections;
using Gantry.UI.Features.NodeEditor.Models;

namespace Gantry.UI.Features.NodeEditor.ViewModels;

/// <summary>
/// Represents a node for an HTTP request in the node editor.
/// </summary>
public partial class RequestNodeViewModel : NodeViewModel
{
    public RequestItem RequestItem { get; }

    public RequestNodeViewModel(RequestItem requestItem, double x, double y) : base(requestItem.Name, x, y)
    {
        RequestItem = requestItem;

        // Default inputs/outputs with proper data types
        AddInput("Trigger", DataType.Trigger);
        AddOutput("On Success", DataType.Trigger);
        AddOutput("On Failure", DataType.Trigger);

        // Variable inputs (simplified for now)
        AddInput("Variables", DataType.Object);

        // Response output
        AddOutput("Response Body", DataType.String);
        AddOutput("Status Code", DataType.Number);
    }

    /// <summary>
    /// Gets the color for the HTTP method badge.
    /// </summary>
    public string MethodColor => RequestItem.Request.Method switch
    {
        "GET" => "Green",
        "POST" => "Blue",
        "PUT" => "Orange",
        "DELETE" => "Red",
        "PATCH" => "Purple",
        _ => "Gray"
    };
}
