using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gantry.Core.Domain.Workspaces;
using Gantry.Infrastructure.Services;
using Gantry.Core.Interfaces;
using Gantry.UI.Features.Requests.ViewModels;
using Gantry.UI.Features.Collections.ViewModels;
using Gantry.UI.Features.Workspaces.ViewModels;
using Gantry.UI.Shell.Docking;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace Gantry.UI.Shell.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly SidebarViewModel _sidebar;
    private readonly WorkspaceService _workspaceService;
    private readonly IHttpService _httpService;
    private readonly IVariableService _variableService;

    public TitleBarViewModel TitleBar { get; }
    public WelcomeViewModel Welcome { get; }

    [ObservableProperty]
    private DockLayoutViewModel _dockLayout = new();

    [ObservableProperty]
    private Workspace? _currentWorkspace;

    public MainWindowViewModel(WorkspaceService workspaceService, IHttpService httpService, IVariableService variableService)
    {
        _workspaceService = workspaceService;
        _httpService = httpService;
        _variableService = variableService;

        TitleBar = new TitleBarViewModel(_workspaceService);
        _sidebar = new SidebarViewModel(_workspaceService);
        Welcome = new WelcomeViewModel(_workspaceService);

        _workspaceService.WorkspaceChanged += OnWorkspaceChanged;

        _sidebar.RequestOpened += OnRequestOpened;
        _sidebar.CollectionOpened += OnCollectionOpened;
        _sidebar.NodeEditorOpened += OnNodeEditorOpened;

        _ = InitializeAsync();
    }

    public MainWindowViewModel() : this(new WorkspaceService(), new Gantry.Infrastructure.Network.HttpService(), new Gantry.Infrastructure.Services.VariableService()) { }

    private async Task InitializeAsync()
    {
        await _workspaceService.InitializeAsync();
    }

    private void OnWorkspaceChanged(object? sender, Workspace? workspace)
    {
        CurrentWorkspace = workspace;

        if (workspace != null)
        {
            if (CountAllTabs() == 0)
            {
                AddTab(new RequestViewModel(_httpService, _workspaceService, _variableService));
            }

            _sidebar.LoadCollectionsCommand.Execute(workspace.Path);
        }
    }

    public SidebarViewModel Sidebar => _sidebar;

    private void OnRequestOpened(object? sender, Gantry.Core.Domain.Collections.RequestItem e)
    {
        AddTab(new RequestViewModel(_httpService, _workspaceService, _variableService, e));
    }

    private void OnCollectionOpened(object? sender, CollectionViewModel e)
    {
        AddTab(new CollectionTabViewModel(e));
    }

    private void OnNodeEditorOpened(object? sender, NodeTaskViewModel task)
    {
        var existing = FindTab<NodeEditorTabViewModel>(t => t.NodeTask == task);

        if (existing != null)
        {
            // Logic to focus the tab would go here
        }
        else
        {
            AddTab(new NodeEditorTabViewModel(task));
        }
    }

    [RelayCommand]
    private void AddTab(TabViewModel tab)
    {
        DockLayout.AddTab(tab);
        tab.CloseRequested += OnTabCloseRequested;
    }

    private void OnTabCloseRequested(object? sender, EventArgs e)
    {
        if (sender is TabViewModel tab)
        {
            tab.CloseRequested -= OnTabCloseRequested;
            RemoveTabFromDock(tab);

            if (CountAllTabs() == 0)
            {
                AddTab(new RequestViewModel(_httpService, _workspaceService, _variableService));
            }
        }
    }

    private void RemoveTabFromDock(TabViewModel tab)
    {
        var pane = FindPane(DockLayout.RootPane, p => p.Tabs.Contains(tab));
        pane?.Tabs.Remove(tab);
    }

    private T? FindTab<T>(Func<T, bool> predicate) where T : TabViewModel
    {
        return FindTabInPane(DockLayout.RootPane, predicate);
    }

    private T? FindTabInPane<T>(DockPaneViewModel pane, Func<T, bool> predicate) where T : TabViewModel
    {
        foreach (var tab in pane.Tabs.OfType<T>())
        {
            if (predicate(tab)) return tab;
        }
        T? found = null;
        if (pane.FirstChild != null) found = FindTabInPane(pane.FirstChild, predicate);
        if (found != null) return found;
        if (pane.SecondChild != null) found = FindTabInPane(pane.SecondChild, predicate);

        return found;
    }

    private DockPaneViewModel? FindPane(DockPaneViewModel current, Func<DockPaneViewModel, bool> predicate)
    {
        if (predicate(current)) return current;

        DockPaneViewModel? found = null;

        if (current.FirstChild != null) found = FindPane(current.FirstChild, predicate);
        if (found != null) return found;
        if (current.SecondChild != null) found = FindPane(current.SecondChild, predicate);

        return found;
    }

    private int CountAllTabs()
    {
        int count = 0;
        void Traverse(DockPaneViewModel p)
        {
            count += p.Tabs.Count;

            if (p.FirstChild != null) Traverse(p.FirstChild);

            if (p.SecondChild != null) Traverse(p.SecondChild);
        }

        Traverse(DockLayout.RootPane);

        return count;
    }

    public void OnTabDetached(TabViewModel tab)
    {
        var window = new DetachedTabWindow(tab);
        window.RedockRequested += (s, t) =>
        {
            AddTab(t);

            if (s is DetachedTabWindow w) w.Close();
        };
        window.Show();
    }
}