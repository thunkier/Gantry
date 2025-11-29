using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gantry.UI.Features.Collections.ViewModels;

public partial class ImportCollectionDialogViewModel : ObservableObject
{
    [ObservableProperty]
    private string _filePath = string.Empty;

    [ObservableProperty]
    private string _selectedType = "Gantry JSON";

    public List<string> ImportTypes { get; } = new() { "Gantry JSON", "Postman Collection v2.1" };

    [RelayCommand]
    private async Task BrowseFile()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
        {
            var topLevel = Avalonia.Controls.TopLevel.GetTopLevel(desktop.MainWindow);
            if (topLevel == null) return;

            var fileType = SelectedType == "Postman Collection v2.1"
                ? new Avalonia.Platform.Storage.FilePickerFileType("Postman Collection") { Patterns = new[] { "*.json" } }
                : new Avalonia.Platform.Storage.FilePickerFileType("Gantry Collection") { Patterns = new[] { "*.json" } };

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
            {
                Title = "Select Collection File",
                AllowMultiple = false,
                FileTypeFilter = new[] { fileType, new Avalonia.Platform.Storage.FilePickerFileType("All Files") { Patterns = new[] { "*.*" } } }
            });

            if (files.Count >= 1)
            {
                FilePath = files[0].Path.LocalPath;
            }
        }
    }
}
