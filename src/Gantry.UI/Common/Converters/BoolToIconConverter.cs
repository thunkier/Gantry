using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace Gantry.UI.Common.Converters;

public class BoolToIconConverter : IValueConverter
{
    public static readonly BoolToIconConverter Instance = new();

    // Box/Package Icon (Material Design)
    private static readonly StreamGeometry RootIcon = StreamGeometry.Parse("M21,16.5C21,16.88 20.79,17.21 20.47,17.38L12.57,21.82C12.41,21.94 12.21,22 12,22C11.79,22 11.59,21.94 11.43,21.82L3.53,17.38C3.21,17.21 3,16.88 3,16.5V7.5C3,7.12 3.21,6.79 3.53,6.62L11.43,2.18C11.59,2.06 11.79,2 12,2C12.21,2 12.41,2.06 12.57,2.18L20.47,6.62C20.79,6.79 21,7.12 21,7.5V16.5M12,4.15L6.04,7.5L12,10.85L17.96,7.5L12,4.15M5,15.91L11,19.29V12.58L5,9.21V15.91M19,15.91V9.21L13,12.58V19.29L19,15.91Z");

    // Folder Icon (Material Design)
    private static readonly StreamGeometry FolderIcon = StreamGeometry.Parse("M20,18H4V8H20M20,6H12L10,4H4C2.89,4 2,4.89 2,6V18A2,2 0 0,0 4,20H20A2,2 0 0,0 22,18V8C22,6.89 21.1,6 20,6Z");

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isRoot && isRoot)
        {
            return RootIcon;
        }
        return FolderIcon;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}