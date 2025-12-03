using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using Gantry.UI.Shell.ViewModels;
using Gantry.Core.Domain.Workspaces;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Linq;

namespace Gantry.UI.Shell.Views;

public partial class TitleBar : UserControl
{
    public TitleBar()
    {
        InitializeComponent();
        ConfigurePlatformUI();
    }

    private void ConfigurePlatformUI()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            WindowsCaptionButtons.IsVisible = true;
            MacSpacer.Width = 0;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            WindowsCaptionButtons.IsVisible = false;
            MacSpacer.Width = 70;
        }
        else
        {
            WindowsCaptionButtons.IsVisible = false;
            MacSpacer.Width = 0;
        }
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is TitleBarViewModel vm)
        {
            vm.OpenFolderDialog = ShowOpenFolderDialog;
            vm.ImportGitDialog = ShowImportGitDialog;
            vm.CreateWorkspaceDialog = ShowCreateWorkspaceDialog;
        }
    }

    private async Task<string?> ShowOpenFolderDialog()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider == null) return null;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Open Workspace Folder",
            AllowMultiple = false
        });

        return folders.Count > 0 ? folders[0].Path.LocalPath : null;
    }

    private async Task<string?> ShowCreateWorkspaceDialog()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider == null) return null;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Folder for New Workspace",
            AllowMultiple = false
        });

        return folders.Count > 0 ? folders[0].Path.LocalPath : null;
    }

    private async Task<string?> ShowImportGitDialog()
    {
        var dialog = new Gantry.UI.Features.Workspaces.Views.GitCloneDialog();
        var topLevel = TopLevel.GetTopLevel(this);
        
        if (topLevel is Window window)
        {
            var result = await dialog.ShowDialog<bool?>(window);
            if (result == true)
            {
                return dialog.GitUrl;
            }
        }

        return null;
    }

    private void OnSearchBoxGotFocus(object? sender, GotFocusEventArgs e)
    {
        // Show search results if there's text
        if (DataContext is TitleBarViewModel vm && vm.Search != null && !string.IsNullOrWhiteSpace(vm.Search.SearchText))
        {
            vm.Search.IsPopupOpen = true;
        }
    }

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // FIRST: Check if we're clicking on an interactive element
        if (e.Source is Visual visual)
        {
            // Don't handle events from buttons, menus, or menu items
            if (visual.FindAncestorOfType<Button>() != null ||
                visual.FindAncestorOfType<Menu>() != null ||
                visual.FindAncestorOfType<MenuItem>() != null)
            {
                return;
            }
        }

        // THEN: Handle double-click to maximize
        if (e.ClickCount == 2)
        {
            OnMaximizeClick(sender, e);
            return;
        }

        // Finally: Handle drag
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            var window = TopLevel.GetTopLevel(this) as Window;
            window?.BeginMoveDrag(e);
        }
    }

    private void OnRecentWorkspaceSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0 &&
            e.AddedItems[0] is string path &&
            DataContext is TitleBarViewModel vm)
        {
            var name = System.IO.Path.GetFileName(path);
            vm.CurrentWorkspace = new Workspace(name, path);

            if (sender is ListBox listBox)
            {
                listBox.SelectedItem = null;

                var button = listBox.FindAncestorOfType<Button>();
                if (button?.Flyout != null)
                {
                    button.Flyout.Hide();
                }
            }
        }
    }

    private void OnMinimizeClick(object? sender, RoutedEventArgs e)
    {
        var window = TopLevel.GetTopLevel(this) as Window;
        if (window != null)
        {
            window.WindowState = WindowState.Minimized;
        }
    }

    private void OnMaximizeClick(object? sender, RoutedEventArgs e)
    {
        var window = TopLevel.GetTopLevel(this) as Window;
        if (window != null)
        {
            window.WindowState = window.WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        var window = TopLevel.GetTopLevel(this) as Window;
        window?.Close();
    }

    private void OnCreateWorkspaceClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is TitleBarViewModel vm)
        {
            vm.CreateWorkspaceCommand.Execute(null);
        }
    }

    private void OnOpenFolderClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is TitleBarViewModel vm)
        {
            vm.OpenFolderCommand.Execute(null);
        }
    }

    private void OnImportGitClick(object? sender, RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("OnImportGitClick called");
        if (DataContext is TitleBarViewModel vm)
        {
            System.Diagnostics.Debug.WriteLine("Executing ImportFromGitCommand manually");
            vm.ImportFromGitCommand.Execute(null);
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"DataContext is not TitleBarViewModel. It is {DataContext?.GetType().Name ?? "null"}");
        }
    }
}