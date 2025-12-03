using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using Gantry.Core.Domain.Collections;
using Gantry.Core.Domain.NodeEditor;
using Gantry.UI.Features.Collections.ViewModels;
using Gantry.UI.Interfaces;

namespace Gantry.UI.Shell.ViewModels;

public partial class SearchViewModel : ObservableObject
{
    private readonly SidebarViewModel _sidebar;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<SearchResultItem> _searchResults = new();

    [ObservableProperty]
    private SearchResultItem? _selectedResult;

    [ObservableProperty]
    private bool _isPopupOpen;

    public event EventHandler<SearchResultItem>? ItemSelected;

    public SearchViewModel(SidebarViewModel sidebar)
    {
        _sidebar = sidebar;
    }

    partial void OnSearchTextChanged(string value)
    {
        PerformSearch();
    }

    partial void OnSelectedResultChanged(SearchResultItem? value)
    {
        if (value != null)
        {
            ItemSelected?.Invoke(this, value);
            SearchText = string.Empty;
            IsPopupOpen = false;
        }
    }

    private void PerformSearch()
    {
        SearchResults.Clear();

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            IsPopupOpen = false;
            return;
        }

        var query = SearchText.ToLowerInvariant();
        var results = new System.Collections.Generic.List<SearchResultItem>();

        // Search through collections recursively
        SearchTreeItems(_sidebar.Collections, query, results);

        // Sort by relevance and take top 10
        foreach (var result in results.OrderByDescending(r => r.Relevance).Take(10))
        {
            SearchResults.Add(result);
        }

        IsPopupOpen = SearchResults.Count > 0;
    }

    private void SearchTreeItems(ObservableCollection<ITreeItemViewModel> items, string query, System.Collections.Generic.List<SearchResultItem> results)
    {
        foreach (var item in items)
        {
            // Handle CollectionViewModel
            if (item is CollectionViewModel collection)
            {
                if (FuzzyMatch(collection.Name, query))
                {
                    results.Add(new SearchResultItem
                    {
                        Name = collection.Name,
                        Type = SearchResultType.Collection,
                        Path = collection.Model.Path ?? string.Empty,
                        Data = collection,
                        Relevance = CalculateRelevance(collection.Name, query)
                    });
                }

                // Search children recursively
                if (collection.Children.Count > 0)
                {
                    SearchTreeItems(collection.Children, query, results);
                }
            }
            // Handle RequestItemViewModel
            else if (item is RequestItemViewModel request)
            {
                if (FuzzyMatch(request.Name, query))
                {
                    results.Add(new SearchResultItem
                    {
                        Name = request.Name,
                        Type = SearchResultType.Request,
                        Path = request.Model.Path ?? string.Empty,
                        Data = request.Model,
                        Relevance = CalculateRelevance(request.Name, query)
                    });
                }
            }
            // Handle NodeTaskViewModel
            else if (item is NodeTaskViewModel nodeTask)
            {
                if (FuzzyMatch(nodeTask.Name, query))
                {
                    results.Add(new SearchResultItem
                    {
                        Name = nodeTask.Name,
                        Type = SearchResultType.NodeTask,
                        Path = nodeTask.Name,
                        Data = nodeTask,
                        Relevance = CalculateRelevance(nodeTask.Name, query)
                    });
                }
            }
        }
    }

    private bool FuzzyMatch(string target, string query)
    {
        return target.ToLowerInvariant().Contains(query);
    }

    private double CalculateRelevance(string target, string query)
    {
        var targetLower = target.ToLowerInvariant();
        
        // Exact match
        if (targetLower == query)
            return 100.0;

        // Starts with query
        if (targetLower.StartsWith(query))
            return 90.0;

        // Contains query
        if (targetLower.Contains(query))
        {
            // Higher score if query is closer to start
            int index = targetLower.IndexOf(query);
            return 50.0 + (50.0 * (1.0 - (double)index / targetLower.Length));
        }

        return 0.0;
    }

    [RelayCommand]
    private void ClosePopup()
    {
        IsPopupOpen = false;
    }
}

public class SearchResultItem
{
    public string Name { get; set; } = string.Empty;
    public SearchResultType Type { get; set; }
    public string Path { get; set; } = string.Empty;
    public object? Data { get; set; }
    public double Relevance { get; set; }

    public string TypeIcon => Type switch
    {
        SearchResultType.Request => "📄",
        SearchResultType.Collection => "📁",
        SearchResultType.NodeTask => "🔗",
        _ => "•"
    };
}

public enum SearchResultType
{
    Request,
    Collection,
    NodeTask
}
