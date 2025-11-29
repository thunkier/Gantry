using Avalonia.Data.Converters;
using Avalonia.Media;
using Gantry.Core.Interfaces;
using System;
using System.Globalization;

namespace Gantry.UI.Common.Converters;

public class GitStatusToIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is GitFileStatus status)
        {
            return status switch
            {
                GitFileStatus.Added => "A",
                GitFileStatus.Modified => "M",
                GitFileStatus.Deleted => "D",
                GitFileStatus.Renamed => "R",
                GitFileStatus.Copied => "C",
                GitFileStatus.Untracked => "U",
                _ => "?"
            };
        }
        return "?";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class GitStatusToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is GitFileStatus status)
        {
            return status switch
            {
                GitFileStatus.Added => new SolidColorBrush(Color.Parse("#4EC9B0")),      // Green
                GitFileStatus.Modified => new SolidColorBrush(Color.Parse("#569CD6")),   // Blue
                GitFileStatus.Deleted => new SolidColorBrush(Color.Parse("#F48771")),    // Red
                GitFileStatus.Renamed => new SolidColorBrush(Color.Parse("#C586C0")),    // Purple
                GitFileStatus.Copied => new SolidColorBrush(Color.Parse("#DCDCAA")),     // Yellow
                GitFileStatus.Untracked => new SolidColorBrush(Color.Parse("#9CDCFE")),  // Light Blue
                _ => new SolidColorBrush(Color.Parse("#CCCCCC"))                         // Gray
            };
        }
        return new SolidColorBrush(Color.Parse("#CCCCCC"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BoolToBackgroundConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue && boolValue)
        {
            return new SolidColorBrush(Color.Parse("#3C3C3C"));
        }
        return Brushes.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BoolToFontWeightConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue && boolValue)
        {
            return FontWeight.Bold;
        }
        return FontWeight.Normal;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
