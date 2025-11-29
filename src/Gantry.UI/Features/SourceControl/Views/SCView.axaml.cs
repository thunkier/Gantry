using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Gantry.Core.Interfaces;
using Gantry.UI.Features.SourceControl.ViewModels;
using Gantry.UI.Interfaces;

namespace Gantry.UI.Features.SourceControl.Views;

public partial class SCView : UserControl
{
    public SCView()
    {
        InitializeComponent();

        // Add keyboard shortcut for commit (Ctrl+Enter)
        KeyDown += OnKeyDown;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            if (DataContext is SourceControlViewModel vm)
            {
                vm.CommitCommand.Execute(null);
                e.Handled = true;
            }
        }
    }

    private void OnFileClick(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border { DataContext: GitFileInfo file } &&
            DataContext is SourceControlViewModel vm)
        {
            vm.SelectedFile = file;
            e.Handled = true;
        }
    }
}
