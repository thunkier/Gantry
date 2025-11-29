using Gantry.UI.Features.Collections.ViewModels;
using Gantry.UI.Features.Requests.ViewModels;
using Gantry.UI.Interfaces;

namespace Gantry.UI.Features.NodeEditor.ViewModels;

public record NodeDropArgs(RequestItemViewModel Request, double X, double Y);
