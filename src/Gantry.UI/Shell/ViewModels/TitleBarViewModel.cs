using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gantry.Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Gantry.UI.Shell.ViewModels;

public partial class TitleBarViewModel : ObservableObject
{
    private readonly WorkspaceService _workspaceService;

    // Commands injected from parent
    public IRelayCommand? NewRequestCommand { get; set; }
    public IRelayCommand? NewCollectionCommand { get; set; }
    public IRelayCommand? NewNodeTaskCommand { get; set; }
    public IRelayCommand? SaveAllCommand { get; set; }
    public IRelayCommand? CloseAllTabsCommand { get; set; }

    // Dialog service
    public Gantry.UI.Services.IDialogService? DialogService { get; set; }
    
    // Search ViewModel (Injected from MainWindow)
    [ObservableProperty]
    private SearchViewModel? _search;

    // Event to signal the View to open the Settings Window
    public event EventHandler? OpenSettingsRequested;

    public Guid InstanceId { get; } = Guid.NewGuid();

    public TitleBarViewModel(WorkspaceService workspaceService)
    {
        _workspaceService = workspaceService;
        _workspaceService.WorkspaceChanged += OnWorkspaceChanged;
    }

    public TitleBarViewModel() : this(new WorkspaceService()) { }

    private void OnWorkspaceChanged(object? sender, Core.Domain.Workspaces.Workspace? e)
    {
        OnPropertyChanged(nameof(RecentWorkspaces));
        OnPropertyChanged(nameof(CurrentWorkspace));
    }

    public IEnumerable<string> RecentWorkspaces => _workspaceService.RecentWorkspaces;

    public Core.Domain.Workspaces.Workspace? CurrentWorkspace
    {
        get => _workspaceService.CurrentWorkspace;
        set
        {
            if (value != null && value.Path != _workspaceService.CurrentWorkspace?.Path)
            {
                _workspaceService.OpenWorkspace(value.Path);
            }
        }
    }

    [RelayCommand]
    private void OpenSettings()
    {
        OpenSettingsRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private async Task CreateWorkspace()
    {
        if (DialogService == null) return;
        var path = await DialogService.ShowCreateWorkspaceDialog();
        if (string.IsNullOrWhiteSpace(path)) return;
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        _workspaceService.OpenWorkspace(path);
    }

    [RelayCommand]
    private async Task OpenFolder()
    {
        if (DialogService == null) return;
        var path = await DialogService.ShowOpenFolderDialog();
        if (string.IsNullOrWhiteSpace(path)) return;
        _workspaceService.OpenWorkspace(path);
    }

    [RelayCommand]
    private async Task ImportFromGit()
    {
        if (DialogService == null) return;
        var gitUrl = await DialogService.ShowImportGitDialog();
        if (string.IsNullOrWhiteSpace(gitUrl)) return;
        await Task.Run(() =>
        {
            var repoName = Path.GetFileNameWithoutExtension(gitUrl.Split('/').Last());
            var defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Gantry", "Workspaces");
            var targetPath = Path.Combine(defaultPath, repoName);
            if (!Directory.Exists(targetPath)) Directory.CreateDirectory(targetPath);
            Avalonia.Threading.Dispatcher.UIThread.Post(() => _workspaceService.OpenWorkspace(targetPath));
        });
    }

    [RelayCommand]
    private void NewWindow()
    {
        var processModule = System.Diagnostics.Process.GetCurrentProcess().MainModule;
        if (processModule != null) System.Diagnostics.Process.Start(processModule.FileName);
    }
}