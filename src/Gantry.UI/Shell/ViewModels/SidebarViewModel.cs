using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gantry.Core.Domain.Collections;
using Gantry.Infrastructure.Persistence;
using System.Collections.ObjectModel;
using Gantry.Infrastructure.Services;
using Gantry.UI.Interfaces;
using Gantry.UI.Features.SourceControl.ViewModels;
using Gantry.UI.Features.Collections.ViewModels;
using Gantry.UI.Features.Collections.Services;
using System.Threading.Tasks;
using System.IO;
using System;
using System.Linq;

namespace Gantry.UI.Shell.ViewModels;

public enum SidebarPage
{
    None,
    Collections,
    History,
    SourceControl,
    NodeEditor
}

public partial class SidebarViewModel : ObservableObject
{
    private readonly FileSystemCollectionRepository _repository = new();
    private readonly WorkspaceService _workspaceService;
    private readonly CollectionService _collectionService;
    private readonly CollectionImportExportService _importExportService = new();

    [ObservableProperty]
    private ObservableCollection<ITreeItemViewModel> _collections = new();

    [ObservableProperty]
    private object? _selectedItem;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPaneOpen))]
    [NotifyPropertyChangedFor(nameof(IsCollectionsSelected))]
    [NotifyPropertyChangedFor(nameof(IsHistorySelected))]
    [NotifyPropertyChangedFor(nameof(IsSourceControlSelected))]
    [NotifyPropertyChangedFor(nameof(IsNodeEditorSelected))]
    private SidebarPage _activePage = SidebarPage.Collections; // Default start

    // Computed Properties for UI Bindings
    public bool IsPaneOpen => ActivePage != SidebarPage.None;
    public bool IsCollectionsSelected => ActivePage == SidebarPage.Collections;
    public bool IsHistorySelected => ActivePage == SidebarPage.History;
    public bool IsSourceControlSelected => ActivePage == SidebarPage.SourceControl;
    public bool IsNodeEditorSelected => ActivePage == SidebarPage.NodeEditor;

    [ObservableProperty]
    private bool _isBusy = false;

    [ObservableProperty]
    private SourceControlViewModel _sourceControl;

    [ObservableProperty]
    private Avalonia.Controls.GridLength _sidebarWidth = new(250);

    public ObservableCollection<NodeTaskViewModel> NodeTasks { get; } = new();

    // Dialog services
    public Func<Task<string?>>? CreateWorkspaceDialog { get; set; }

    public SidebarViewModel(WorkspaceService workspaceService)
    {
        _workspaceService = workspaceService;
        _collectionService = new CollectionService(_workspaceService);
        _sourceControl = new SourceControlViewModel(new GitService(), _workspaceService);

        if (!string.IsNullOrEmpty(_workspaceService.CurrentWorkspace?.Path))
        {
            LoadCollections(_workspaceService.CurrentWorkspace.Path);
        }

        _workspaceService.FileSystemChanged += OnFileSystemChanged;
    }

    private void OnFileSystemChanged(object? sender, EventArgs e)
    {
        Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (!string.IsNullOrEmpty(_workspaceService.CurrentWorkspace?.Path))
            {
                // TODO: Optimize this to only reload changed parts if possible
                // For now, full reload to ensure consistency
                LoadCollections(_workspaceService.CurrentWorkspace.Path);
            }
        });
    }

    [RelayCommand]
    public void LoadCollections(string path)
    {
        Collections.Clear();
        var rootCollection = _repository.LoadCollection(path);

        // Add sub-collections as top-level folders
        foreach (var sub in rootCollection.SubCollections)
        {
            var vm = new CollectionViewModel(sub, _workspaceService) { IsRoot = true };
            Collections.Add(vm);
        }

        // Add root files
        foreach (var req in rootCollection.Requests)
        {
            Collections.Add(new RequestItemViewModel(req));
        }

        // Add node tasks
        NodeTasks.Clear();
        foreach (var graph in rootCollection.NodeGraphs)
        {
            var vm = new NodeTaskViewModel(graph, _workspaceService, SelectRequestAsync);
            NodeTasks.Add(vm);
            Collections.Add(vm);
        }
    }

    [RelayCommand]
    private void OpenRequest(object item)
    {
        if (item is RequestItemViewModel requestVm)
        {
            RequestOpened?.Invoke(this, requestVm.Model);
        }
    }

    [RelayCommand]
    private void OpenCollection(object item)
    {
        if (item is CollectionViewModel colVm)
        {
            CollectionOpened?.Invoke(this, colVm);
        }
    }

    private void TogglePage(SidebarPage page)
    {
        if (ActivePage == page)
            ActivePage = SidebarPage.None; // Collapse if clicking same page
        else
            ActivePage = page; // Switch to new page
    }

    [RelayCommand]
    private void ShowCollections() => TogglePage(SidebarPage.Collections);

    [RelayCommand]
    private void ShowHistory() => TogglePage(SidebarPage.History);

    [RelayCommand]
    private void ShowSourceControl()
    {
        TogglePage(SidebarPage.SourceControl);
        if (IsSourceControlSelected)
            SourceControl.RefreshStatusCommand.Execute(null);
    }

    [RelayCommand]
    private void ShowNodeEditor() => TogglePage(SidebarPage.NodeEditor);

    [RelayCommand]
    private void AddTask()
    {
        var graph = new Gantry.Core.Domain.NodeEditor.NodeGraph
        {
            Name = $"Task {NodeTasks.Count + 1}",
            Path = System.IO.Path.Combine(_workspaceService.CurrentWorkspace?.Path ?? "", $"Task {NodeTasks.Count + 1}.json")
        };

        var newTask = new NodeTaskViewModel(graph, _workspaceService, SelectRequestAsync);
        NodeTasks.Add(newTask);

        // Save immediately to persist the new file
        _repository.SaveNodeGraph(graph);
    }

    private async Task<RequestItem?> SelectRequestAsync()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
        {
            var allRequests = GetAllRequests(Collections);
            var dialogVm = new Gantry.UI.Features.NodeEditor.ViewModels.AddRequestDialogViewModel(allRequests);
            var dialog = new Gantry.UI.Features.NodeEditor.Views.AddRequestDialog { DataContext = dialogVm };

            return await dialog.ShowDialog<RequestItem?>(desktop.MainWindow);
        }
        return null;
    }

    private System.Collections.Generic.IEnumerable<RequestItem> GetAllRequests(ObservableCollection<ITreeItemViewModel> items)
    {
        foreach (var item in items)
        {
            if (item is RequestItemViewModel req)
            {
                yield return req.Model;
            }
            else if (item is CollectionViewModel col)
            {
                foreach (var child in GetAllRequests(col.Children))
                {
                    yield return child;
                }
            }
        }
    }

    [RelayCommand]
    private void OpenTask(object item)
    {
        if (item is NodeTaskViewModel taskVm)
        {
            NodeEditorOpened?.Invoke(this, taskVm);
        }
    }

    [RelayCommand]
    private void AddCollection()
    {
        try
        {
            var collectionsPath = _workspaceService.CurrentWorkspace.Path;
            _collectionService.CreateCollection(collectionsPath);
            LoadCollections(collectionsPath);
        }
        catch (Exception ex)
        {
            RaiseError($"Failed to add collection: {ex.Message}");
        }
    }

    [RelayCommand]
    private void AddRequest(object item)
    {
        if (item is CollectionViewModel col)
        {
            try
            {
                var vm = _collectionService.CreateRequest(col);
                col.Children.Add(vm);

                // Start editing
                vm.EditableName = vm.Name;
                vm.IsEditing = true;
            }
            catch (Exception ex)
            {
                RaiseError($"Failed to add request: {ex.Message}");
            }
        }
    }

    [RelayCommand]
    private void AddFolder(object item)
    {
        if (item is CollectionViewModel col)
        {
            try
            {
                var vm = _collectionService.CreateFolder(col);
                col.Children.Add(vm);

                // Start editing
                vm.EditableName = vm.Name;
                vm.IsEditing = true;
            }
            catch (Exception ex)
            {
                RaiseError($"Failed to add folder: {ex.Message}");
            }
        }
    }

    [RelayCommand]
    private async Task ExportCollection(object item)
    {
        if (item is CollectionViewModel col && Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
        {
            var dialog = new Avalonia.Controls.SaveFileDialog
            {
                Title = "Export Collection",
                Filters = new System.Collections.Generic.List<Avalonia.Controls.FileDialogFilter>
                {
                    new() { Name = "OpenAPI 3.0", Extensions = new System.Collections.Generic.List<string> { "json" } },
                    new() { Name = "TypeSpec", Extensions = new System.Collections.Generic.List<string> { "tsp" } },
                    new() { Name = "Bruno", Extensions = new System.Collections.Generic.List<string> { "bru" } },
                    new() { Name = "JSON Schema", Extensions = new System.Collections.Generic.List<string> { "json" } }
                }
            };

            var result = await dialog.ShowAsync(desktop.MainWindow);
            if (result != null)
            {
                try
                {
                    await _importExportService.ExportAsync(col.Model, result);
                }
                catch (Exception ex)
                {
                    RaiseError($"Export failed: {ex.Message}");
                }
            }
        }
    }

    [RelayCommand]
    private async Task ImportCollection()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
        {
            var dialog = new Gantry.UI.Features.Collections.Views.ImportCollectionDialog();
            var result = await dialog.ShowDialog<bool?>(desktop.MainWindow);

            // Allow UI to update after dialog close
            await Task.Yield();

            if (result == true && !string.IsNullOrWhiteSpace(dialog.FilePath))
            {
                var path = dialog.FilePath;
                var type = dialog.SelectedType;

                // Ensure workspace exists
                if (_workspaceService.CurrentWorkspace == null)
                {
                    if (CreateWorkspaceDialog != null)
                    {
                        var newWorkspacePath = await CreateWorkspaceDialog.Invoke();
                        if (!string.IsNullOrWhiteSpace(newWorkspacePath))
                        {
                            // Create directory if it doesn't exist (though OpenFolderPicker usually returns existing folders)
                            if (!Directory.Exists(newWorkspacePath))
                            {
                                Directory.CreateDirectory(newWorkspacePath);
                            }
                            _workspaceService.OpenWorkspace(newWorkspacePath);
                        }
                        else
                        {
                            return; // User cancelled
                        }
                    }
                    else
                    {
                        RaiseError("No active workspace selected.");
                        return;
                    }
                }

                var workspacePath = _workspaceService.CurrentWorkspace!.Path;

                try
                {
                    var collection = await _importExportService.ImportAsync(path, type);

                    if (collection == null)
                    {
                        RaiseError("Imported collection is empty or invalid.");
                        return;
                    }

                    // Save to target directory
                    var safeName = string.Join("_", collection.Name.Split(Path.GetInvalidFileNameChars()));
                    collection.Path = Path.Combine(workspacePath, safeName);

                    _repository.SaveCollection(collection);

                    // Refresh
                    if (Directory.Exists(workspacePath))
                    {
                        LoadCollections(workspacePath);
                    }
                    else
                    {
                        var vm = new CollectionViewModel(collection, _workspaceService) { IsRoot = true };
                        Collections.Add(vm);
                    }
                }
                catch (Exception ex)
                {
                    RaiseError($"Import failed: {ex.Message}");
                }
            }
        }
    }

    private void RaiseError(string message)
    {
        System.Diagnostics.Debug.WriteLine($"[Sidebar Error] {message}");
        ErrorOccurred?.Invoke(this, message);

        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
        {
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var dialog = new Views.MessageBox("Error", message);
                await dialog.ShowDialog(desktop.MainWindow);
            });
        }
    }

    [RelayCommand]
    private void CompleteEdit(object item)
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;

            if (item is CollectionViewModel col && col.IsEditing)
            {
                if (!_collectionService.ValidateFileName(col.EditableName, out var errorMessage))
                {
                    RaiseError(errorMessage ?? "Invalid folder name");
                    return;
                }

                _collectionService.RenameItem(col, col.EditableName);
                col.IsEditing = false;
                col.RaiseNameChanged();
            }
            else if (item is RequestItemViewModel req && req.IsEditing)
            {
                if (!_collectionService.ValidateFileName(req.EditableName, out var errorMessage))
                {
                    RaiseError(errorMessage ?? "Invalid file name");
                    return;
                }

                _collectionService.RenameItem(req, req.EditableName);
                req.IsEditing = false;
                req.RaiseNameChanged();
            }
        }
        catch (Exception ex)
        {
            RaiseError($"Operation failed: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public event EventHandler<string>? ErrorOccurred;

    [RelayCommand]
    public void DeleteItem(object item)
    {
        try
        {
            if (item is ITreeItemViewModel treeItem)
            {
                _collectionService.DeleteItem(treeItem);

                // Remove from tree without reloading
                var parent = TreeItemHelper.FindParentCollection(Collections, treeItem);
                if (parent != null)
                {
                    parent.Children.Remove(treeItem);
                }
                else
                {
                    Collections.Remove(treeItem);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting item: {ex.Message}");
        }
    }

    [RelayCommand]
    public void RenameItem(object item)
    {
        if (item is CollectionViewModel col)
        {
            col.EditableName = col.Name;
            col.IsEditing = true;
        }
        else if (item is RequestItemViewModel req)
        {
            req.EditableName = Path.GetFileNameWithoutExtension(req.Name);
            req.IsEditing = true;
        }
    }

    [RelayCommand]
    private void CancelEdit(object item)
    {
        if (item is CollectionViewModel col) col.IsEditing = false;
        else if (item is RequestItemViewModel req) req.IsEditing = false;
    }

    // Drag and Drop Support
    private ITreeItemViewModel? _draggedItem;

    public void StartDrag(ITreeItemViewModel item)
    {
        _draggedItem = item;
    }

    public bool CanDrop(ITreeItemViewModel? target)
    {
        if (_draggedItem == null || target == null) return false;
        if (_draggedItem == target) return false;
        if (target is not CollectionViewModel) return false;

        if (_draggedItem is CollectionViewModel draggedCol && target is CollectionViewModel targetCol)
        {
            return !TreeItemHelper.IsDescendant(targetCol, draggedCol);
        }

        return true;
    }

    [RelayCommand]
    private void Drop(ITreeItemViewModel? target)
    {
        if (IsBusy) return;

        if (_draggedItem == null || target is not CollectionViewModel targetCol || !CanDrop(target))
        {
            _draggedItem = null;
            return;
        }

        try
        {
            IsBusy = true;
            var sourceParent = TreeItemHelper.FindParentCollection(Collections, _draggedItem)?.Children ?? Collections;

            _collectionService.MoveItem(_draggedItem, targetCol);

            sourceParent.Remove(_draggedItem);
            targetCol.Children.Add(_draggedItem);

            if (_draggedItem is CollectionViewModel col) col.RaiseNameChanged();
            else if (_draggedItem is RequestItemViewModel req) req.RaiseNameChanged();
        }
        catch (Exception ex)
        {
            RaiseError($"Error during drag-drop: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
            _draggedItem = null;
        }
    }

    public event EventHandler<RequestItem>? RequestOpened;
    public event EventHandler<CollectionViewModel>? CollectionOpened;
    public event EventHandler<NodeTaskViewModel>? NodeEditorOpened;
}
