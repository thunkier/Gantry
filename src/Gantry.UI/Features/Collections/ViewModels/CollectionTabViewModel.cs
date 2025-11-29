using CommunityToolkit.Mvvm.ComponentModel;
using Gantry.UI.Shell.ViewModels;

namespace Gantry.UI.Features.Collections.ViewModels;

public partial class CollectionTabViewModel : TabViewModel
{
    public CollectionViewModel Collection { get; }

    public CollectionTabViewModel(CollectionViewModel collection)
    {
        Collection = collection;
        Title = collection.Name;
    }
}
