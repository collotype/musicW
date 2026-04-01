using Nocturne.App.Models.Enums;
using System.Globalization;
using System.Windows.Data;

namespace Nocturne.App.Converters;

public sealed class RepeatModeToGlyphConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            RepeatMode.One => "1",
            RepeatMode.All => "\uE8EE",
            _ => "\uE8EE"
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
