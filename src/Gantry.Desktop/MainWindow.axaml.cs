using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Gantry.Infrastructure.Network;
using Gantry.Infrastructure.Services;
using Gantry.UI.Features.Workspaces.Views;
using Gantry.UI.Shell.ViewModels;
using static Gantry.UI.Shell.ViewModels.SearchViewModel;

namespace Gantry.Desktop;

public partial class MainWindow : Window
{
    private MainWindowViewModel? _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        ConfigureWindowDecoration();
        InitializeViewModel();
    }

    private void ConfigureWindowDecoration()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.NoChrome;
                ExtendClientAreaTitleBarHeightHint = -1;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.PreferSystemChrome;
                ExtendClientAreaTitleBarHeightHint = -1;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error configuring window decoration: {ex.Message}");
        }
    }

    private void InitializeViewModel()
    {
        try
        {
            var workspaceService = new WorkspaceService();
            var httpService = new HttpService();

            _viewModel = new MainWindowViewModel(workspaceService, httpService, new VariableService());

            Debug.WriteLine($"MainWindowViewModel initialized. TitleBar instance: {_viewModel.TitleBar.InstanceId}");

            ConfigureDialogProviders(_viewModel);

            DataContext = _viewModel;
        
            // Wire up TitleBar event handlers
            _viewModel.TitleBar.NewRequestRequested += OnNewRequestRequested;
            _viewModel.TitleBar.NewCollectionRequested += OnNewCollectionRequested;
            _viewModel.TitleBar.NewNodeTaskRequested += OnNewNodeTaskRequested;
            _viewModel.TitleBar.SaveAllRequested += OnSaveAllRequested;
            _viewModel.TitleBar.CloseAllTabsRequested += OnCloseAllTabsRequested;

            // Wire up search
            _viewModel.TitleBar.InitializeSearch(_viewModel.Sidebar);
            _viewModel.TitleBar.SearchItemSelected += OnSearchItemSelected;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error initializing view model: {ex.Message}");
            throw;
        }
    }

    private void ConfigureDialogProviders(MainWindowViewModel viewModel)
    {
        if (viewModel?.TitleBar == null)
        {
            Debug.WriteLine("Warning: ViewModel or TitleBar is null. Dialog providers not configured.");
            return;
        }

        viewModel.TitleBar.OpenFolderDialog = OpenFolderDialogAsync;
        viewModel.TitleBar.CreateWorkspaceDialog = CreateWorkspaceDialogAsync;
        viewModel.TitleBar.ImportGitDialog = ImportGitDialogAsync;

        if (viewModel.Welcome != null)
        {
            viewModel.Welcome.OpenFolderDialog = OpenFolderDialogAsync;
            viewModel.Welcome.CloneGitDialog = ImportGitDialogAsync;
        }

        if (viewModel.Sidebar != null)
        {
            viewModel.Sidebar.CreateWorkspaceDialog = CreateWorkspaceDialogAsync;
        }
    }

    private async Task<string?> OpenFolderDialogAsync()
    {
        try
        {
            if (!StorageProvider.CanOpen)
            {
                Debug.WriteLine("Storage provider does not support opening folders.");
                return null;
            }

            var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Open Workspace Folder",
                AllowMultiple = false
            });

            var selectedPath = folders.FirstOrDefault()?.Path.LocalPath;

            if (selectedPath != null)
            {
                Debug.WriteLine($"Folder selected: {selectedPath}");
            }

            return selectedPath;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error opening folder dialog: {ex.Message}");
            return null;
        }
    }

    private async Task<string?> CreateWorkspaceDialogAsync()
    {
        try
        {
            if (!StorageProvider.CanOpen)
            {
                Debug.WriteLine("Storage provider does not support opening folders.");
                return null;
            }

            var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select Folder for New Workspace",
                AllowMultiple = false
            });

            var selectedPath = folders.FirstOrDefault()?.Path.LocalPath;

            if (selectedPath != null)
            {
                Debug.WriteLine($"Workspace location selected: {selectedPath}");
            }

            return selectedPath;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error opening create workspace dialog: {ex.Message}");
            return null;
        }
    }

    private async Task<string?> ImportGitDialogAsync()
    {
        try
        {
            var dialog = new GitCloneDialog();
            var result = await dialog.ShowDialog<bool?>(this);

            if (result == true)
            {
                var gitUrl = dialog.GitUrl;

                if (!string.IsNullOrWhiteSpace(gitUrl))
                {
                    Debug.WriteLine($"Git repository URL selected: {gitUrl}");
                    return gitUrl;
                }
                else
                {
                    Debug.WriteLine("Warning: Git dialog confirmed but URL is empty.");
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error opening Git clone dialog: {ex.Message}");
            return null;
        }
    }

    private void OnSearchItemSelected(object? sender, SearchResultItem item)
    {
        if (_viewModel == null) return;

        switch (item.Type)
        {
            case SearchResultType.Request:
                if (item.Data is Gantry.Core.Domain.Collections.RequestItem request)
                {
                    _viewModel.AddTabCommand.Execute(new Gantry.UI.Features.Requests.ViewModels.RequestViewModel(
                        new HttpService(),
                        new WorkspaceService(),
                        new VariableService(),
                        request));
                }
                break;

            case SearchResultType.Collection:
                if (item.Data is Gantry.UI.Features.Collections.ViewModels.CollectionViewModel collection)
                {
                    _viewModel.AddTabCommand.Execute(new Gantry.UI.Features.Collections.ViewModels.CollectionTabViewModel(collection));
                }
                break;

            case SearchResultType.NodeTask:
                if (item.Data is Gantry.UI.Features.Collections.ViewModels.NodeTaskViewModel nodeTask)
                {
                    _viewModel.AddTabCommand.Execute(new Gantry.UI.Shell.ViewModels.NodeEditorTabViewModel(nodeTask));
                }
                break;
        }
    }

    private void OnNewRequestRequested(object? sender, EventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.AddTabCommand.Execute(new Gantry.UI.Features.Requests.ViewModels.RequestViewModel(
                new HttpService(),
                new WorkspaceService(),
                new VariableService()));
        }
    }

    private async void OnNewCollectionRequested(object? sender, EventArgs e)
    {
        // TODO: Show create collection dialog
        await Task.CompletedTask;
    }

    private void OnNewNodeTaskRequested(object? sender, EventArgs e)
    {
        if (_viewModel != null)
        {
            var task = new Gantry.UI.Features.Collections.ViewModels.NodeTaskViewModel(
                new Gantry.Core.Domain.NodeEditor.NodeGraph { Name = "New Task" },
                new WorkspaceService());
            _viewModel.AddTabCommand.Execute(new Gantry.UI.Shell.ViewModels.NodeEditorTabViewModel(task));
        }
    }

    private void OnSaveAllRequested(object? sender, EventArgs e)
    {
        // TODO: Implement Save All
        Debug.WriteLine("Save All requested");
    }

    private void OnCloseAllTabsRequested(object? sender, EventArgs e)
    {
        if (_viewModel?.DockLayout?.RootPane?.Tabs != null)
        {
            while (_viewModel.DockLayout.RootPane.Tabs.Count > 0)
            {
                var tab = _viewModel.DockLayout.RootPane.Tabs[0];
                _viewModel.DockLayout.RootPane.Tabs.Remove(tab);
            }
        }
    }

    private void OnTabDetachRequested(object? sender, TabViewModel tab)
    {
        if (tab == null)
        {
            Debug.WriteLine("Warning: Attempted to detach a null tab.");
            return;
        }

        if (_viewModel != null)
        {
            _viewModel.OnTabDetached(tab);
        }
        else
        {
            Debug.WriteLine("Warning: Cannot detach tab - view model is not initialized.");
        }
    }
}