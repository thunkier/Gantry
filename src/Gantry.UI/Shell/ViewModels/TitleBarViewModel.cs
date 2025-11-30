using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gantry.Core.Domain.Workspaces;
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

    // Services for UI interactions (Dialogs)
    public Func<Task<string?>>? CreateWorkspaceDialog { get; set; }
    public Func<Task<string?>>? OpenFolderDialog { get; set; }
    public Func<Task<string?>>? ImportGitDialog { get; set; }

    public Guid InstanceId { get; } = Guid.NewGuid();

    public TitleBarViewModel(WorkspaceService workspaceService)
    {
        System.Diagnostics.Debug.WriteLine($"TitleBarViewModel Created: {InstanceId}");
        _workspaceService = workspaceService;
        _workspaceService.WorkspaceChanged += OnWorkspaceChanged;
    }

    // Fallback for previewer
    public TitleBarViewModel() : this(new WorkspaceService()) { }

    private void OnWorkspaceChanged(object? sender, Workspace? e)
    {
        OnPropertyChanged(nameof(RecentWorkspaces));
        OnPropertyChanged(nameof(CurrentWorkspace));
    }

    public IEnumerable<string> RecentWorkspaces => _workspaceService.RecentWorkspaces;

    public Workspace? CurrentWorkspace
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
    private async Task CreateWorkspace()
    {
        if (CreateWorkspaceDialog == null) return;

        var name = await CreateWorkspaceDialog.Invoke();
        if (string.IsNullOrWhiteSpace(name)) return;

        var defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Gantry", "Workspaces");
        var path = Path.Combine(defaultPath, name);

        if (!Directory.Exists(path)) Directory.CreateDirectory(path);

        _workspaceService.OpenWorkspace(path);
    }

    [RelayCommand]
    private async Task OpenFolder()
    {
        if (OpenFolderDialog == null) return;

        var path = await OpenFolderDialog.Invoke();
        if (string.IsNullOrWhiteSpace(path)) return;

        _workspaceService.OpenWorkspace(path);
    }

    [RelayCommand]
    private async Task ImportFromGit()
    {
        System.Diagnostics.Debug.WriteLine($"ImportFromGit command executed on Instance: {InstanceId}");
        if (ImportGitDialog == null)
        {
            System.Diagnostics.Debug.WriteLine($"ImportGitDialog is null on Instance: {InstanceId}");
            return;
        }

        var gitUrl = await ImportGitDialog.Invoke();
        if (string.IsNullOrWhiteSpace(gitUrl)) return;

        await Task.Run(() =>
        {
            var repoName = Path.GetFileNameWithoutExtension(gitUrl.Split('/').Last());
            var defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Gantry", "Workspaces");
            var targetPath = Path.Combine(defaultPath, repoName);

            if (!Directory.Exists(targetPath)) Directory.CreateDirectory(targetPath);

            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                _workspaceService.OpenWorkspace(targetPath);
            });
        });
    }
    [RelayCommand]
    private void NewWindow()
    {
        var processModule = System.Diagnostics.Process.GetCurrentProcess().MainModule;
        if (processModule != null)
        {
            System.Diagnostics.Process.Start(processModule.FileName);
        }
    }
    [RelayCommand]
    private async Task OpenSettings()
    {
        var dialog = new Gantry.UI.Shell.Views.AppSettingsDialog();
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (desktop.MainWindow != null)
            {
                await dialog.ShowDialog(desktop.MainWindow);
            }
        }
    }
}
