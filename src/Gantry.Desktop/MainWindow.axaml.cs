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
using Gantry.UI.Services;

namespace Gantry.Desktop;

public partial class MainWindow : Window, IDialogService
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

            // Inject dialog service into TitleBar and Welcome
            _viewModel.TitleBar.DialogService = this;
            _viewModel.Welcome.OpenFolderDialog = ShowOpenFolderDialog;
            _viewModel.Welcome.CloneGitDialog = ShowImportGitDialog;

            DataContext = _viewModel;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error initializing view model: {ex.Message}");
            throw;
        }
    }

    // IDialogService implementation
    public async Task<string?> ShowOpenFolderDialog()
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

            return folders.FirstOrDefault()?.Path.LocalPath;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error opening folder dialog: {ex.Message}");
            return null;
        }
    }

    public async Task<string?> ShowCreateWorkspaceDialog()
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

            return folders.FirstOrDefault()?.Path.LocalPath;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error opening create workspace dialog: {ex.Message}");
            return null;
        }
    }

    public async Task<string?> ShowImportGitDialog()
    {
        try
        {
            var dialog = new GitCloneDialog();
            var result = await dialog.ShowDialog<bool?>(this);

            if (result == true && !string.IsNullOrWhiteSpace(dialog.GitUrl))
            {
                return dialog.GitUrl;
            }

            return null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error opening Git clone dialog: {ex.Message}");
            return null;
        }
    }

    private void OnTabDetachRequested(object? sender, TabViewModel tab)
    {
        if (tab == null)
        {
            Debug.WriteLine("Warning: Attempted to detach a null tab.");
            return;
        }

        _viewModel?.OnTabDetached(tab);
    }
}