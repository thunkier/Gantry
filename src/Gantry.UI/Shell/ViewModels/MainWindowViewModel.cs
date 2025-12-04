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
    public SearchViewModel Search { get; }

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
        Search = new SearchViewModel(_sidebar);

        TitleBar.Search = Search;

        // Inject commands into TitleBar
        TitleBar.NewRequestCommand = NewRequestCommand;
        TitleBar.NewCollectionCommand = NewCollectionCommand;
        TitleBar.NewNodeTaskCommand = NewNodeTaskCommand;
        TitleBar.SaveAllCommand = SaveAllCommand;
        TitleBar.CloseAllTabsCommand = CloseAllTabsCommand;

        // Subscribe to search results
        Search.ItemSelected += OnSearchItemSelected;

        _workspaceService.WorkspaceChanged += OnWorkspaceChanged;

        _sidebar.RequestOpened += OnRequestOpened;
        _sidebar.CollectionOpened += OnCollectionOpened;
        _sidebar.NodeEditorOpened += OnNodeEditorOpened;

        InitializeAsync();
    }

    public SidebarViewModel Sidebar => _sidebar;

    private void InitializeAsync()
    {
        if (_workspaceService.CurrentWorkspace != null)
        {
            CurrentWorkspace = _workspaceService.CurrentWorkspace;
            _sidebar.LoadCollections(_workspaceService.CurrentWorkspace.Path);
        }
    }

    private void OnWorkspaceChanged(object? sender, Workspace? workspace)
    {
        CurrentWorkspace = workspace;
        if (workspace != null)
        {
            _sidebar.LoadCollections(workspace.Path);
        }
    }

    private void OnRequestOpened(object? sender, Core.Domain.Collections.RequestItem request)
    {
        AddTab(new RequestViewModel(_httpService, _workspaceService, _variableService, request));
    }

    private void OnCollectionOpened(object? sender, CollectionViewModel collection)
    {
        AddTab(new CollectionTabViewModel(collection));
    }

    private void OnNodeEditorOpened(object? sender, NodeTaskViewModel nodeTask)
    {
        AddTab(new NodeEditorTabViewModel(nodeTask));
    }

    private void OnSearchItemSelected(object? sender, SearchResultItem item)
    {
        switch (item.Type)
        {
            case SearchResultType.Request:
                if (item.Data is Core.Domain.Collections.RequestItem request)
                {
                    AddTab(new RequestViewModel(_httpService, _workspaceService, _variableService, request));
                }
                break;

            case SearchResultType.Collection:
                if (item.Data is CollectionViewModel collection)
                {
                    AddTab(new CollectionTabViewModel(collection));
                }
                break;

            case SearchResultType.NodeTask:
                if (item.Data is NodeTaskViewModel nodeTask)
                {
                    AddTab(new NodeEditorTabViewModel(nodeTask));
                }
                break;
        }
    }

    [RelayCommand]
    private void AddTab(TabViewModel tab)
    {
        DockLayout.RootPane.Tabs.Add(tab);
        DockLayout.RootPane.ActiveTab = tab;
    }

    [RelayCommand]
    private void NewRequest()
    {
        AddTab(new RequestViewModel(_httpService, _workspaceService, _variableService));
    }

    [RelayCommand]
    private void NewCollection()
    {
        // TODO: Show create collection dialog
    }

    [RelayCommand]
    private void NewNodeTask()
    {
        var task = new NodeTaskViewModel(
            new Core.Domain.NodeEditor.NodeGraph { Name = "New Task" },
            _workspaceService);
        AddTab(new NodeEditorTabViewModel(task));
    }

    [RelayCommand]
    private void SaveAll()
    {
        // TODO: Implement Save All
        System.Diagnostics.Debug.WriteLine("Save All requested");
    }

    [RelayCommand]
    private void CloseAllTabs()
    {
        DockLayout.RootPane.Tabs.Clear();
    }

    public void OnTabDetached(TabViewModel tab)
    {
        DockLayout.RootPane.Tabs.Remove(tab);
    }
}