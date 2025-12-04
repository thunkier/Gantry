using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Gantry.UI.Shell.ViewModels;
using Gantry.Core.Domain.Workspaces;
using System.Runtime.InteropServices;
using System;

namespace Gantry.UI.Shell.Views;

public partial class TitleBar : UserControl
{
    public TitleBar()
    {
        InitializeComponent();
        ConfigurePlatformUI();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is TitleBarViewModel vm)
        {
            // Subscribe to the request to open settings
            vm.OpenSettingsRequested -= OnOpenSettingsRequested; // Prevent duplicates
            vm.OpenSettingsRequested += OnOpenSettingsRequested;
        }
    }

    private void OnOpenSettingsRequested(object? sender, EventArgs e)
    {
        // Resolve the Window
        var topLevel = TopLevel.GetTopLevel(this) as Window;
        
        // Instantiate the dialog (DataContext is set inside its constructor per previous turn)
        var settingsDialog = new AppSettingsDialog();
        
        if (topLevel != null)
        {
            settingsDialog.ShowDialog(topLevel);
        }
        else
        {
            settingsDialog.Show();
        }
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

    private void OnSearchBoxGotFocus(object? sender, GotFocusEventArgs e)
    {
        // Search popup is handled by binding in XAML
    }

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is Visual visual)
        {
            if (visual.FindAncestorOfType<Button>() != null ||
                visual.FindAncestorOfType<Menu>() != null ||
                visual.FindAncestorOfType<MenuItem>() != null)
            {
                return;
            }
        }

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
        if (TopLevel.GetTopLevel(this) is Window window)
        {
            window.WindowState = WindowState.Minimized;
        }
    }

    private void OnMaximizeClick(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is Window window)
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
}