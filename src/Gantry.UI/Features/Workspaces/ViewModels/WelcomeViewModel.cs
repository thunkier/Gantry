using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gantry.Infrastructure.Services;

namespace Gantry.UI.Features.Workspaces.ViewModels;

/// <summary>
/// ViewModel for the welcome screen shown when no workspace is loaded.
/// </summary>
public partial class WelcomeViewModel : ObservableObject
{
    private readonly WorkspaceService _workspaceService;

    [ObservableProperty]
    private ObservableCollection<RecentWorkspace> _recentWorkspaces = new();

    [ObservableProperty]
    private bool _hasRecentWorkspaces;

    public Func<Task<string?>>? OpenFolderDialog { get; set; }
    public Func<Task<string?>>? CloneGitDialog { get; set; }

    public WelcomeViewModel(WorkspaceService workspaceService)
    {
        _workspaceService = workspaceService;
        LoadRecentWorkspaces();
    }

    private void LoadRecentWorkspaces()
    {
        var recent = _workspaceService.RecentWorkspaces
            .Take(3)
            .Select(path => new RecentWorkspace
            {
                Path = path,
                Name = System.IO.Path.GetFileName(path) ?? path
            })
            .ToList();

        RecentWorkspaces = new ObservableCollection<RecentWorkspace>(recent);
        HasRecentWorkspaces = RecentWorkspaces.Any();
    }

    [RelayCommand]
    private async Task OpenFolderAsync()
    {
        if (OpenFolderDialog == null) return;

        var folderPath = await OpenFolderDialog();
        if (!string.IsNullOrEmpty(folderPath))
        {
            _workspaceService.OpenWorkspace(folderPath);
        }
    }

    [RelayCommand]
    private async Task CloneGitAsync()
    {
        if (CloneGitDialog == null) return;

        var gitUrl = await CloneGitDialog();
        if (!string.IsNullOrEmpty(gitUrl))
        {
            // TODO: Implement Git cloning in WorkspaceService
            // For now, just open the clone dialog
        }
    }

    [RelayCommand]
    private void OpenRecentWorkspace(RecentWorkspace workspace)
    {
        if (workspace?.Path != null)
        {
            _workspaceService.OpenWorkspace(workspace.Path);
        }
    }

    [RelayCommand]
    private void ShowMoreWorkspaces()
    {
        var allRecent = _workspaceService.RecentWorkspaces
            .Select(path => new RecentWorkspace
            {
                Path = path,
                Name = System.IO.Path.GetFileName(path) ?? path
            })
            .ToList();

        RecentWorkspaces = new ObservableCollection<RecentWorkspace>(allRecent);
    }
}

/// <summary>
/// Represents a recent workspace entry.
/// </summary>
public class RecentWorkspace
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
}
