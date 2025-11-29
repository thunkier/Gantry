using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Gantry.UI.Shell.Docking;

/// <summary>
/// Data Transfer Object for serializing the dock layout to JSON.
/// </summary>
public class DockLayoutDto
{
    [JsonPropertyName("rootPane")]
    public DockPaneDto? RootPane { get; set; }
}

/// <summary>
/// Data Transfer Object for serializing a dock pane to JSON.
/// </summary>
public class DockPaneDto
{
    [JsonPropertyName("tabs")]
    public List<TabDto> Tabs { get; set; } = new();

    [JsonPropertyName("activeTabIndex")]
    public int ActiveTabIndex { get; set; } = -1;

    [JsonPropertyName("firstChild")]
    public DockPaneDto? FirstChild { get; set; }

    [JsonPropertyName("secondChild")]
    public DockPaneDto? SecondChild { get; set; }

    [JsonPropertyName("orientation")]
    public string Orientation { get; set; } = "Horizontal";

    [JsonPropertyName("splitRatio")]
    public double SplitRatio { get; set; } = 0.5;
}

/// <summary>
/// Data Transfer Object for serializing a tab to JSON.
/// </summary>
public class TabDto
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("tabType")]
    public string TabType { get; set; } = string.Empty;

    [JsonPropertyName("requestPath")]
    public string? RequestPath { get; set; }

    [JsonPropertyName("collectionPath")]
    public string? CollectionPath { get; set; }

    [JsonPropertyName("nodeTaskId")]
    public string? NodeTaskId { get; set; }
}
