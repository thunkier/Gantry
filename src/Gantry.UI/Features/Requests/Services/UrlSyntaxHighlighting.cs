using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using System.Text.RegularExpressions;

namespace Gantry.UI.Features.Requests.Services;

public class UrlSyntaxHighlighting : DocumentColorizingTransformer
{
    private static readonly Regex VariableRegex = new(@"\$\{(.+?)\}", RegexOptions.Compiled);
    private static readonly Regex ParamRegex = new(@"[?&]([^=]+)=([^&]*)", RegexOptions.Compiled);

    protected override void ColorizeLine(DocumentLine line)
    {
        var text = CurrentContext.Document.GetText(line);

        // Highlight Variables
        foreach (Match match in VariableRegex.Matches(text))
        {
            ChangeLinePart(
                line.Offset + match.Index,
                line.Offset + match.Index + match.Length,
                element =>
                {
                    element.TextRunProperties.SetForegroundBrush(Brushes.CornflowerBlue);
                });
        }

        // Highlight Params
        foreach (Match match in ParamRegex.Matches(text))
        {
            // Key
            ChangeLinePart(
                line.Offset + match.Groups[1].Index,
                line.Offset + match.Groups[1].Index + match.Groups[1].Length,
                element =>
                {
                    element.TextRunProperties.SetForegroundBrush(Brushes.Orange);
                });

            // Value
            ChangeLinePart(
                line.Offset + match.Groups[2].Index,
                line.Offset + match.Groups[2].Index + match.Groups[2].Length,
                element =>
                {
                    element.TextRunProperties.SetForegroundBrush(Brushes.MediumPurple);
                });
        }
    }
}
