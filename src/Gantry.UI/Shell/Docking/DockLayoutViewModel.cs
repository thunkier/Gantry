using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Gantry.UI.Shell.ViewModels;

namespace Gantry.UI.Shell.Docking;

/// <summary>
/// Represents the layout structure for the docking system.
/// Contains a tree of panes and their tabs.
/// </summary>
public partial class DockLayoutViewModel : ObservableObject
{
    [ObservableProperty]
    private DockPaneViewModel _rootPane;

    public DockLayoutViewModel()
    {
        _rootPane = new DockPaneViewModel();
    }

    /// <summary>
    /// Adds a tab to the specified pane. If no pane is specified, adds to the first available pane.
    /// </summary>
    public void AddTab(TabViewModel tab, DockPaneViewModel? targetPane = null)
    {
        var pane = targetPane ?? FindFirstLeafPane(RootPane);
        if (pane != null)
        {
            pane.Tabs.Add(tab);
            pane.ActiveTab = tab;
        }
    }

    /// <summary>
    /// Moves a tab from one pane to another.
    /// </summary>
    public void MoveTab(TabViewModel tab, DockPaneViewModel sourcePane, DockPaneViewModel targetPane)
    {
        if (sourcePane.Tabs.Contains(tab))
        {
            sourcePane.Tabs.Remove(tab);
            targetPane.Tabs.Add(tab);
            targetPane.ActiveTab = tab;
        }
    }

    /// <summary>
    /// Finds the first leaf pane (pane that contains tabs, not other panes).
    /// </summary>
    private DockPaneViewModel? FindFirstLeafPane(DockPaneViewModel pane)
    {
        if (pane.IsLeaf)
            return pane;

        if (pane.FirstChild != null)
        {
            var result = FindFirstLeafPane(pane.FirstChild);
            if (result != null)
                return result;
        }

        if (pane.SecondChild != null)
        {
            return FindFirstLeafPane(pane.SecondChild);
        }

        return null;
    }

    /// <summary>
    /// Converts the layout to a DTO for serialization.
    /// </summary>
    public DockLayoutDto ToDto()
    {
        return new DockLayoutDto
        {
            RootPane = RootPane.ToDto()
        };
    }

    /// <summary>
    /// Creates a layout from a DTO.
    /// </summary>
    public static DockLayoutViewModel FromDto(DockLayoutDto dto)
    {
        var layout = new DockLayoutViewModel();
        if (dto.RootPane != null)
        {
            layout.RootPane = DockPaneViewModel.FromDto(dto.RootPane);
        }
        return layout;
    }
}

/// <summary>
/// Represents a single pane in the dock layout.
/// A pane can either contain tabs (leaf) or contain two child panes (split).
/// </summary>
public partial class DockPaneViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<TabViewModel> _tabs = new();

    [ObservableProperty]
    private TabViewModel? _activeTab;

    [ObservableProperty]
    private DockPaneViewModel? _firstChild;

    [ObservableProperty]
    private DockPaneViewModel? _secondChild;

    [ObservableProperty]
    private DockSplitOrientation _orientation = DockSplitOrientation.Horizontal;

    [ObservableProperty]
    private double _splitRatio = 0.5;

    public DockPaneViewModel()
    {
        Tabs.CollectionChanged += Tabs_CollectionChanged;
    }

    private void Tabs_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
        {
            if (e.OldItems != null && ActiveTab != null && e.OldItems.Contains(ActiveTab))
            {
                // Active tab was removed, select another one
                ActiveTab = Tabs.LastOrDefault();
            }
        }
    }

    /// <summary>
    /// Returns true if this pane contains tabs (leaf node), false if it contains child panes (split node).
    /// </summary>
    public bool IsLeaf => FirstChild == null && SecondChild == null;

    /// <summary>
    /// Splits this pane horizontally (top/bottom).
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSplit))]
    public void SplitHorizontal()
    {
        Split(DockSplitOrientation.Horizontal);
    }

    /// <summary>
    /// Splits this pane vertically (left/right).
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSplit))]
    public void SplitVertical()
    {
        Split(DockSplitOrientation.Vertical);
    }

    private bool CanSplit() => IsLeaf && Tabs.Count > 0;

    private void Split(DockSplitOrientation orientation)
    {
        if (!IsLeaf)
            return; // Already split

        // Move current tabs to first child
        FirstChild = new DockPaneViewModel();
        foreach (var tab in Tabs)
        {
            FirstChild.Tabs.Add(tab);
        }
        FirstChild.ActiveTab = ActiveTab;

        // Create second child
        SecondChild = new DockPaneViewModel();

        // Clear current tabs
        Tabs.Clear();
        ActiveTab = null;

        Orientation = orientation;

        // Notify that IsLeaf property changed
        OnPropertyChanged(nameof(IsLeaf));
    }

    /// <summary>
    /// Converts the pane to a DTO for serialization.
    /// </summary>
    public DockPaneDto ToDto()
    {
        var dto = new DockPaneDto
        {
            Orientation = Orientation.ToString(),
            SplitRatio = SplitRatio,
            ActiveTabIndex = ActiveTab != null ? Tabs.IndexOf(ActiveTab) : -1
        };

        if (IsLeaf)
        {
            // Serialize tabs
            foreach (var tab in Tabs)
            {
                dto.Tabs.Add(TabViewModel.ToDto(tab));
            }
        }
        else
        {
            // Serialize child panes
            dto.FirstChild = FirstChild?.ToDto();
            dto.SecondChild = SecondChild?.ToDto();
        }

        return dto;
    }

    /// <summary>
    /// Creates a pane from a DTO (Note: tabs need to be recreated by caller).
    /// </summary>
    public static DockPaneViewModel FromDto(DockPaneDto dto)
    {
        var pane = new DockPaneViewModel
        {
            SplitRatio = dto.SplitRatio
        };

        if (Enum.TryParse<DockSplitOrientation>(dto.Orientation, out var orientation))
        {
            pane.Orientation = orientation;
        }

        if (dto.FirstChild != null && dto.SecondChild != null)
        {
            // Has child panes (split)
            pane.FirstChild = FromDto(dto.FirstChild);
            pane.SecondChild = FromDto(dto.SecondChild);
        }
        // Note: Tabs are not recreated here - caller must handle tab restoration

        return pane;
    }
}

public enum DockSplitOrientation
{
    Horizontal,
    Vertical
}
