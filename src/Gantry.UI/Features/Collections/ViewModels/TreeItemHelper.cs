using System.Collections.ObjectModel;
using Gantry.UI.Interfaces;

namespace Gantry.UI.Features.Collections.ViewModels;

public static class TreeItemHelper
{
    public static bool IsDescendant(CollectionViewModel potentialDescendant, CollectionViewModel potentialAncestor)
    {
        foreach (var child in potentialAncestor.Children)
        {
            if (child == potentialDescendant)
                return true;

            if (child is CollectionViewModel childCol && IsDescendant(potentialDescendant, childCol))
                return true;
        }
        return false;
    }

    public static CollectionViewModel? FindParentCollection(ObservableCollection<ITreeItemViewModel> items, ITreeItemViewModel target)
    {
        foreach (var item in items)
        {
            if (item is CollectionViewModel col)
            {
                if (col.Children.Contains(target))
                    return col;

                var found = FindParentCollection(col.Children, target);
                if (found != null)
                    return found;
            }
        }
        return null;
    }
}
