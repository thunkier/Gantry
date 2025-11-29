using Avalonia.Controls;
using Avalonia.Interactivity;
using Gantry.UI.Features.Collections.ViewModels;
using Gantry.UI.Features.Requests.ViewModels;
using Gantry.UI.Shell.ViewModels;
using Gantry.UI.Interfaces;

namespace Gantry.UI.Features.Collections.Views;

public partial class ImportCollectionDialog : Window
{
    public string FilePath => ((ImportCollectionDialogViewModel)DataContext!).FilePath;
    public string SelectedType => ((ImportCollectionDialogViewModel)DataContext!).SelectedType;

    public ImportCollectionDialog()
    {
        InitializeComponent();
        DataContext = new ImportCollectionDialogViewModel();
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private void OnImportClick(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }
}
