using System;
using System.Globalization;
using Avalonia.Data.Converters;
using AvaloniaEdit.Document;

namespace Gantry.UI.Common.Converters;

public class StringToTextDocumentConverter : IValueConverter
{
    public static readonly StringToTextDocumentConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string s)
        {
            return new TextDocument(s);
        }
        return new TextDocument("");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TextDocument doc)
        {
            return doc.Text;
        }
        return string.Empty;
    }
}
