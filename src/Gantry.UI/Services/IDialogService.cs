using System.Threading.Tasks;

namespace Gantry.UI.Services;

public interface IDialogService
{
    Task<string?> ShowOpenFolderDialog();
    Task<string?> ShowCreateWorkspaceDialog();
    Task<string?> ShowImportGitDialog();
}
