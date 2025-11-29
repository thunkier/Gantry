using Avalonia.Media;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using System;

namespace Gantry.UI.Features.Requests.Services;

public class UrlCompletionData : ICompletionData
{
    public UrlCompletionData(string text, string description, string type)
    {
        Text = text;
        Description = description;
        Type = type;
    }

    public IImage? Image => null;

    public string Text { get; }

    public object Content => Text;

    public object Description { get; }

    public double Priority => 0;

    public string Type { get; } // "Variable" or "Param"

    public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
    {
        textArea.Document.Replace(completionSegment, Text);
    }
}
