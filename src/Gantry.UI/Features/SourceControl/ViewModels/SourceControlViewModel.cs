using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gantry.Core.Interfaces;
using Gantry.Infrastructure.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Gantry.UI.Features.SourceControl.ViewModels;

public partial class SourceControlViewModel : ObservableObject
{
    private readonly IGitService _gitService;
    private readonly WorkspaceService _workspaceService;

    // File status collections
    [ObservableProperty]
    private ObservableCollection<GitFileInfo> _stagedFiles = new();

    [ObservableProperty]
    private ObservableCollection<GitFileInfo> _unstagedFiles = new();

    // Selected file and diff
    [ObservableProperty]
    private GitFileInfo? _selectedFile;

    [ObservableProperty]
    private string _fileDiff = string.Empty;

    // Commit functionality
    [ObservableProperty]
    private string _commitMessage = string.Empty;

    [ObservableProperty]
    private int _commitMessageLength;

    // Git state
    [ObservableProperty]
    private bool _isGitRepo;

    [ObservableProperty]
    private string _currentBranch = string.Empty;

    // Remote operations
    [ObservableProperty]
    private string _remoteName = "origin";

    [ObservableProperty]
    private string _remoteUrl = string.Empty;

    // Branch management
    [ObservableProperty]
    private ObservableCollection<GitBranchInfo> _branches = new();

    [ObservableProperty]
    private GitBranchInfo? _selectedBranch;

    [ObservableProperty]
    private string _newBranchName = string.Empty;

    // Commit history
    [ObservableProperty]
    private ObservableCollection<GitCommitInfo> _commitHistory = new();

    [ObservableProperty]
    private GitCommitInfo? _selectedCommit;

    // Stash management
    [ObservableProperty]
    private ObservableCollection<GitStashInfo> _stashes = new();

    [ObservableProperty]
    private GitStashInfo? _selectedStash;

    [ObservableProperty]
    private string _stashMessage = string.Empty;

    // UI state
    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    private int _selectedTabIndex;

    public SourceControlViewModel(IGitService gitService, WorkspaceService workspaceService)
    {
        _gitService = gitService;
        _workspaceService = workspaceService;
        RefreshStatus();
    }

    partial void OnCommitMessageChanged(string value)
    {
        CommitMessageLength = value.Length;
    }

    partial void OnSelectedFileChanged(GitFileInfo? value)
    {
        if (value != null)
        {
            LoadFileDiff(value);
        }
        else
        {
            FileDiff = string.Empty;
        }
    }

    // Repository operations
    [RelayCommand]
    private void RefreshStatus()
    {
        var path = _workspaceService.CurrentWorkspace?.Path;
        if (string.IsNullOrEmpty(path)) return;

        IsRefreshing = true;
        try
        {
            IsGitRepo = _gitService.IsGitRepo(path);
            if (IsGitRepo)
            {
                CurrentBranch = _gitService.GetCurrentBranch(path);

                RefreshFileStatus();
                LoadBranches();
                LoadCommitHistory();
                LoadStashes();

                StatusMessage = "Status refreshed";
            }
        }
        catch (System.Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task InitializeRepo()
    {
        var path = _workspaceService.CurrentWorkspace?.Path;
        if (string.IsNullOrEmpty(path)) return;

        var dialog = new Views.CreateRepositoryDialog();
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
        {
            var result = await dialog.ShowDialog<bool?>(desktop.MainWindow);
            if (result == true)
            {
                try
                {
                    _gitService.Initialize(path, dialog.CreateGitIgnore, dialog.CreateReadme);
                    RefreshStatus();
                    StatusMessage = "Repository initialized successfully";
                }
                catch (System.Exception ex)
                {
                    StatusMessage = $"Error initializing repository: {ex.Message}";
                }
            }
        }
    }

    // File operations
    private void RefreshFileStatus()
    {
        var path = _workspaceService.CurrentWorkspace?.Path;
        if (string.IsNullOrEmpty(path)) return;

        StagedFiles.Clear();
        UnstagedFiles.Clear();

        foreach (var file in _gitService.GetStagedFiles(path))
        {
            StagedFiles.Add(file);
        }

        foreach (var file in _gitService.GetUnstagedFiles(path))
        {
            UnstagedFiles.Add(file);
        }
    }

    [RelayCommand]
    private void StageFile(GitFileInfo file)
    {
        var path = _workspaceService.CurrentWorkspace?.Path;
        if (string.IsNullOrEmpty(path) || !IsGitRepo) return;

        try
        {
            _gitService.Stage(path, file.FilePath);
            RefreshFileStatus();
            StatusMessage = $"Staged {file.FilePath}";
        }
        catch (System.Exception ex)
        {
            StatusMessage = $"Error staging file: {ex.Message}";
        }
    }

    [RelayCommand]
    private void UnstageFile(GitFileInfo file)
    {
        var path = _workspaceService.CurrentWorkspace?.Path;
        if (string.IsNullOrEmpty(path) || !IsGitRepo) return;

        try
        {
            _gitService.Unstage(path, file.FilePath);
            RefreshFileStatus();
            StatusMessage = $"Unstaged {file.FilePath}";
        }
        catch (System.Exception ex)
        {
            StatusMessage = $"Error unstaging file: {ex.Message}";
        }
    }

    [RelayCommand]
    private void StageAll()
    {
        var path = _workspaceService.CurrentWorkspace?.Path;
        if (string.IsNullOrEmpty(path) || !IsGitRepo) return;

        try
        {
            _gitService.StageAll(path);
            RefreshFileStatus();
            StatusMessage = "All changes staged";
        }
        catch (System.Exception ex)
        {
            StatusMessage = $"Error staging all: {ex.Message}";
        }
    }

    [RelayCommand]
    private void UnstageAll()
    {
        var path = _workspaceService.CurrentWorkspace?.Path;
        if (string.IsNullOrEmpty(path) || !IsGitRepo) return;

        try
        {
            _gitService.UnstageAll(path);
            RefreshFileStatus();
            StatusMessage = "All changes unstaged";
        }
        catch (System.Exception ex)
        {
            StatusMessage = $"Error unstaging all: {ex.Message}";
        }
    }

    [RelayCommand]
    private void DiscardChanges(GitFileInfo file)
    {
        var path = _workspaceService.CurrentWorkspace?.Path;
        if (string.IsNullOrEmpty(path) || !IsGitRepo) return;

        try
        {
            _gitService.DiscardChanges(path, file.FilePath);
            RefreshFileStatus();
            StatusMessage = $"Discarded changes in {file.FilePath}";
        }
        catch (System.Exception ex)
        {
            StatusMessage = $"Error discarding changes: {ex.Message}";
        }
    }

    private void LoadFileDiff(GitFileInfo file)
    {
        var path = _workspaceService.CurrentWorkspace?.Path;
        if (string.IsNullOrEmpty(path) || !IsGitRepo) return;

        try
        {
            FileDiff = _gitService.GetDiff(path, file.FilePath, file.IsStaged);
        }
        catch (System.Exception ex)
        {
            FileDiff = $"Error loading diff: {ex.Message}";
        }
    }

    // Commit operations
    [RelayCommand]
    private void Commit()
    {
        var path = _workspaceService.CurrentWorkspace?.Path;
        if (string.IsNullOrEmpty(path) || !IsGitRepo) return;

        if (string.IsNullOrWhiteSpace(CommitMessage))
        {
            StatusMessage = "Please enter a commit message";
            return;
        }

        if (!StagedFiles.Any())
        {
            StatusMessage = "No staged changes to commit";
            return;
        }

        try
        {
            _gitService.Commit(path, CommitMessage);
            CommitMessage = string.Empty;
            RefreshStatus();
            StatusMessage = "Changes committed successfully";
        }
        catch (System.Exception ex)
        {
            StatusMessage = $"Error committing: {ex.Message}";
        }
    }

    private void LoadCommitHistory()
    {
        var path = _workspaceService.CurrentWorkspace?.Path;
        if (string.IsNullOrEmpty(path)) return;

        CommitHistory.Clear();
        foreach (var commit in _gitService.GetCommitHistory(path, 100))
        {
            CommitHistory.Add(commit);
        }
    }

    // Branch operations
    private void LoadBranches()
    {
        var path = _workspaceService.CurrentWorkspace?.Path;
        if (string.IsNullOrEmpty(path)) return;

        Branches.Clear();
        foreach (var branch in _gitService.GetBranches(path).Where(b => !b.IsRemote))
        {
            Branches.Add(branch);
        }
    }

    [RelayCommand]
    private void CreateBranch()
    {
        var path = _workspaceService.CurrentWorkspace?.Path;
        if (string.IsNullOrEmpty(path) || !IsGitRepo) return;

        if (string.IsNullOrWhiteSpace(NewBranchName))
        {
            StatusMessage = "Please enter a branch name";
            return;
        }

        try
        {
            _gitService.CreateBranch(path, NewBranchName);
            NewBranchName = string.Empty;
            LoadBranches();
            StatusMessage = "Branch created successfully";
        }
        catch (System.Exception ex)
        {
            StatusMessage = $"Error creating branch: {ex.Message}";
        }
    }

    [RelayCommand]
    private void SwitchBranch(GitBranchInfo branch)
    {
        var path = _workspaceService.CurrentWorkspace?.Path;
        if (string.IsNullOrEmpty(path) || !IsGitRepo) return;

        try
        {
            _gitService.CheckoutBranch(path, branch.Name);
            RefreshStatus();
            StatusMessage = $"Switched to branch {branch.Name}";
        }
        catch (System.Exception ex)
        {
            StatusMessage = $"Error switching branch: {ex.Message}";
        }
    }

    [RelayCommand]
    private void DeleteBranch(GitBranchInfo branch)
    {
        var path = _workspaceService.CurrentWorkspace?.Path;
        if (string.IsNullOrEmpty(path) || !IsGitRepo) return;

        if (branch.IsCurrent)
        {
            StatusMessage = "Cannot delete current branch";
            return;
        }

        try
        {
            _gitService.DeleteBranch(path, branch.Name);
            LoadBranches();
            StatusMessage = $"Branch {branch.Name} deleted";
        }
        catch (System.Exception ex)
        {
            StatusMessage = $"Error deleting branch: {ex.Message}";
        }
    }

    // Remote operations
    [RelayCommand]
    private void Push()
    {
        var path = _workspaceService.CurrentWorkspace?.Path;
        if (string.IsNullOrEmpty(path) || !IsGitRepo) return;

        try
        {
            _gitService.Push(path, RemoteName, CurrentBranch);
            StatusMessage = $"Pushed to {RemoteName}/{CurrentBranch}";
        }
        catch (System.Exception ex)
        {
            StatusMessage = $"Error pushing: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Pull()
    {
        var path = _workspaceService.CurrentWorkspace?.Path;
        if (string.IsNullOrEmpty(path) || !IsGitRepo) return;

        try
        {
            _gitService.Pull(path, RemoteName, CurrentBranch);
            RefreshStatus();
            StatusMessage = $"Pulled from {RemoteName}/{CurrentBranch}";
        }
        catch (System.Exception ex)
        {
            StatusMessage = $"Error pulling: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Fetch()
    {
        var path = _workspaceService.CurrentWorkspace?.Path;
        if (string.IsNullOrEmpty(path) || !IsGitRepo) return;

        try
        {
            _gitService.Fetch(path, RemoteName);
            StatusMessage = $"Fetched from {RemoteName}";
        }
        catch (System.Exception ex)
        {
            StatusMessage = $"Error fetching: {ex.Message}";
        }
    }

    [RelayCommand]
    private void AddRemote()
    {
        var path = _workspaceService.CurrentWorkspace?.Path;
        if (string.IsNullOrEmpty(path) || !IsGitRepo || string.IsNullOrEmpty(RemoteUrl)) return;

        try
        {
            _gitService.AddRemote(path, RemoteName, RemoteUrl);
            RemoteUrl = string.Empty;
            StatusMessage = $"Remote {RemoteName} added";
        }
        catch (System.Exception ex)
        {
            StatusMessage = $"Error adding remote: {ex.Message}";
        }
    }

    // Stash operations
    private void LoadStashes()
    {
        var path = _workspaceService.CurrentWorkspace?.Path;
        if (string.IsNullOrEmpty(path)) return;

        Stashes.Clear();
        foreach (var stash in _gitService.GetStashes(path))
        {
            Stashes.Add(stash);
        }
    }

    [RelayCommand]
    private void CreateStash()
    {
        var path = _workspaceService.CurrentWorkspace?.Path;
        if (string.IsNullOrEmpty(path) || !IsGitRepo) return;

        var message = string.IsNullOrWhiteSpace(StashMessage) ? "WIP" : StashMessage;

        try
        {
            _gitService.Stash(path, message);
            StashMessage = string.Empty;
            LoadStashes();
            RefreshFileStatus();
            StatusMessage = "Changes stashed successfully";
        }
        catch (System.Exception ex)
        {
            StatusMessage = $"Error creating stash: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ApplyStash(GitStashInfo stash)
    {
        var path = _workspaceService.CurrentWorkspace?.Path;
        if (string.IsNullOrEmpty(path) || !IsGitRepo) return;

        try
        {
            _gitService.ApplyStash(path, stash.Index);
            RefreshFileStatus();
            StatusMessage = "Stash applied successfully";
        }
        catch (System.Exception ex)
        {
            StatusMessage = $"Error applying stash: {ex.Message}";
        }
    }

    [RelayCommand]
    private void PopStash(GitStashInfo stash)
    {
        var path = _workspaceService.CurrentWorkspace?.Path;
        if (string.IsNullOrEmpty(path) || !IsGitRepo) return;

        try
        {
            _gitService.PopStash(path, stash.Index);
            LoadStashes();
            RefreshFileStatus();
            StatusMessage = "Stash popped successfully";
        }
        catch (System.Exception ex)
        {
            StatusMessage = $"Error popping stash: {ex.Message}";
        }
    }

    [RelayCommand]
    private void DropStash(GitStashInfo stash)
    {
        var path = _workspaceService.CurrentWorkspace?.Path;
        if (string.IsNullOrEmpty(path) || !IsGitRepo) return;

        try
        {
            _gitService.DropStash(path, stash.Index);
            LoadStashes();
            StatusMessage = "Stash dropped successfully";
        }
        catch (System.Exception ex)
        {
            StatusMessage = $"Error dropping stash: {ex.Message}";
        }
    }
}
