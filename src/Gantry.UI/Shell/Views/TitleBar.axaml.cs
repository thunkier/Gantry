using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Gantry.UI.Shell.ViewModels;
using Gantry.Core.Domain.Workspaces;
using System.Runtime.InteropServices;

namespace Gantry.UI.Shell.Views;

public partial class TitleBar : UserControl
{
    public TitleBar()
    {
        InitializeComponent();

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

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is Visual visual)
        {
            // Prevent dragging if clicking the Workspace Switcher button or other interactables
            if (visual.FindAncestorOfType<Button>() != null)
            {
                return;
            }
        }

        // Handle Double-Click to Maximize/Restore
        if (e.ClickCount == 2)
        {
            OnMaximizeClick(sender, e);
            return;
        }

        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            var window = TopLevel.GetTopLevel(this) as Window;
            window?.BeginMoveDrag(e);
        }
    }

    private void OnRecentWorkspaceSelected(object? sender, SelectionChangedEventArgs e)
    {
        // When a user clicks a path in the "Recent" list
        if (e.AddedItems.Count > 0 &&
            e.AddedItems[0] is string path &&
            DataContext is TitleBarViewModel vm)
        {
            // Create a temporary workspace object to trigger the ViewModel's setter,
            // which in turn calls _workspaceService.OpenWorkspace(path)
            var name = System.IO.Path.GetFileName(path);
            vm.CurrentWorkspace = new Workspace(name, path);

            // Reset selection so the same item can be clicked again in the future
            if (sender is ListBox listBox)
            {
                listBox.SelectedItem = null;

                // Helper to close the flyout by finding the parent Button
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