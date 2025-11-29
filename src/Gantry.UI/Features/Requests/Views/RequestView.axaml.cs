using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using AvaloniaEdit;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using Gantry.UI.Features.Requests.Services;
using Gantry.UI.Features.Requests.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Gantry.UI.Features.Requests.Views;

public partial class RequestView : UserControl
{
    private TextEditor? _urlEditor;
    private CompletionWindow? _completionWindow;
    private readonly ToolTip _toolTip = new();

    public RequestView()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _urlEditor = this.FindControl<TextEditor>("UrlEditor");

        if (_urlEditor != null)
        {
            // Syntax Highlighting
            _urlEditor.TextArea.TextView.LineTransformers.Add(new UrlSyntaxHighlighting());

            // Autocomplete
            _urlEditor.TextArea.TextEntering += OnTextEntering;
            _urlEditor.TextArea.TextEntered += OnTextEntered;

            // Hover
            _urlEditor.PointerMoved += OnPointerMoved;
            _urlEditor.PointerExited += OnPointerExited;

            // Single line behavior
            _urlEditor.TextArea.Options.EnableHyperlinks = false;
            _urlEditor.TextArea.Options.EnableEmailHyperlinks = false;
        }
    }

    private void OnTextEntering(object? sender, TextInputEventArgs e)
    {
        if (e.Text?.Length > 0 && _completionWindow != null)
        {
            if (!char.IsLetterOrDigit(e.Text[0]) && e.Text[0] != '_' && e.Text[0] != '-')
            {
                // Commit insertion if non-identifier character is typed
                _completionWindow.CompletionList.RequestInsertion(e);
            }
        }
    }

    private void OnTextEntered(object? sender, TextInputEventArgs e)
    {
        if (e.Text == "$" || e.Text == "{" || e.Text == "?" || e.Text == "&")
        {
            ShowCompletion(e.Text);
        }
    }

    private void ShowCompletion(string trigger)
    {
        if (_urlEditor == null || DataContext is not RequestViewModel vm) return;

        _completionWindow = new CompletionWindow(_urlEditor.TextArea);
        IList<ICompletionData> data = _completionWindow.CompletionList.CompletionData;

        if (trigger == "$" || trigger == "{")
        {
            // Variables
            var variables = GetAllVariables(vm);
            foreach (var v in variables)
            {
                data.Add(new UrlCompletionData($"{{{v.Key}}}", v.Value, "Variable"));
            }
        }
        else if (trigger == "?" || trigger == "&")
        {
            // Params
            foreach (var p in vm.Params)
            {
                if (!string.IsNullOrEmpty(p.Key))
                {
                    data.Add(new UrlCompletionData($"{p.Key}={p.Value}", p.Description, "Param"));
                }
            }
        }

        if (data.Count > 0)
        {
            _completionWindow.Show();
            _completionWindow.Closed += delegate { _completionWindow = null; };
        }
    }

    private IEnumerable<KeyValuePair<string, string>> GetAllVariables(RequestViewModel vm)
    {
        // Local variables
        foreach (var v in vm.Variables)
        {
            if (!string.IsNullOrEmpty(v.Key)) yield return new KeyValuePair<string, string>(v.Key, v.Value);
        }

        // Collection variables (Need to traverse up, but for now we only have access to what's in VM or Model)
        // Ideally RequestViewModel should expose all available variables or we traverse the model parent
        var current = vm.Model.Parent;
        while (current != null)
        {
            foreach (var v in current.Variables)
            {
                if (v.Enabled && !string.IsNullOrEmpty(v.Key))
                {
                    yield return new KeyValuePair<string, string>(v.Key, v.Value);
                }
            }
            current = current.Parent;
        }
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_urlEditor == null || DataContext is not RequestViewModel vm) return;

        var point = e.GetPosition(_urlEditor.TextArea.TextView);
        var pos = _urlEditor.GetPositionFromPoint(point);

        if (pos.HasValue)
        {
            var offset = _urlEditor.Document.GetOffset(pos.Value.Location);
            var word = GetWordAtOffset(_urlEditor.Document, offset);

            if (!string.IsNullOrEmpty(word))
            {
                // Check if it's a variable
                if (word.StartsWith("${") && word.EndsWith("}"))
                {
                    var varName = word.Substring(2, word.Length - 3);
                    var value = ResolveVariable(vm, varName);

                    ToolTip.SetTip(_urlEditor, $"Variable: {varName}\nValue: {value}");
                    ToolTip.SetIsOpen(_urlEditor, true);
                    return;
                }

                // Check if it's a param key
                // Simple check: look back for ? or &
                // This is a bit naive but works for simple cases
            }
        }

        ToolTip.SetIsOpen(_urlEditor, false);
    }

    private void OnPointerExited(object? sender, PointerEventArgs e)
    {
        if (_urlEditor != null)
        {
            ToolTip.SetIsOpen(_urlEditor, false);
        }
    }

    private string GetWordAtOffset(TextDocument document, int offset)
    {
        if (offset < 0 || offset >= document.TextLength) return string.Empty;

        // Find start
        int start = offset;
        while (start > 0)
        {
            char c = document.GetCharAt(start - 1);
            if (char.IsWhiteSpace(c) || c == '/' || c == '?' || c == '&' || c == '=') break;
            start--;
        }

        // Find end
        int end = offset;
        while (end < document.TextLength)
        {
            char c = document.GetCharAt(end);
            if (char.IsWhiteSpace(c) || c == '/' || c == '?' || c == '&' || c == '=') break;
            end++;
        }

        if (start >= end) return string.Empty;
        return document.GetText(start, end - start);
    }

    private string ResolveVariable(RequestViewModel vm, string key)
    {
        // Check local
        var local = vm.Variables.FirstOrDefault(v => v.Key == key);
        if (local != null) return local.Value;

        // Check parents
        var current = vm.Model.Parent;
        while (current != null)
        {
            var v = current.Variables.FirstOrDefault(x => x.Enabled && x.Key == key);
            if (v != null) return v.Value;
            current = current.Parent;
        }

        return "(not found)";
    }
}
