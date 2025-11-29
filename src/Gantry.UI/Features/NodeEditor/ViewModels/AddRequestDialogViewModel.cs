using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gantry.Core.Domain.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Gantry.UI.Features.NodeEditor.ViewModels;

public partial class AddRequestDialogViewModel : ObservableObject
{
    private readonly List<RequestItem> _allRequests;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private RequestItem? _selectedRequest;

    public ObservableCollection<RequestItem> FilteredRequests { get; } = new();

    public AddRequestDialogViewModel(IEnumerable<RequestItem> requests)
    {
        _allRequests = requests.ToList();
        FilterRequests();
    }

    partial void OnSearchQueryChanged(string value)
    {
        FilterRequests();
    }

    private void FilterRequests()
    {
        FilteredRequests.Clear();
        var query = SearchQuery?.ToLowerInvariant() ?? string.Empty;

        var matches = _allRequests.Where(r =>
            string.IsNullOrWhiteSpace(query) ||
            r.Name.ToLowerInvariant().Contains(query) ||
            r.Request.Url.ToLowerInvariant().Contains(query)
        );

        foreach (var match in matches)
        {
            FilteredRequests.Add(match);
        }
    }
}
