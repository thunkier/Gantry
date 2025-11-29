using System.Collections.ObjectModel;

namespace Gantry.UI.Interfaces;

public interface ITreeItemViewModel
{
    string Name { get; }
    ObservableCollection<ITreeItemViewModel> Children { get; }
}
