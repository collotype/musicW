using System.Globalization;
using System.Collections;
using System.Windows.Data;

namespace MusicApp.Converters;

public class SliderProgressConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length >= 3 &&
            values[0] is double value &&
            values[1] is double maximum &&
            values[2] is double actualWidth &&
            maximum > 0)
        {
            return (value / maximum) * actualWidth;
        }
        return 0.0;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var invert = parameter?.ToString()?.ToLower() == "invert";
        var isNull = value == null || string.IsNullOrEmpty(value.ToString());

        if (invert)
        {
            return isNull ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        }

        return isNull ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var invert = parameter?.ToString()?.ToLower() == "invert";
        var boolValue = value is true;

        if (invert)
        {
            return !boolValue ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        }

        return boolValue ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class CountToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var invert = parameter?.ToString()?.ToLowerInvariant() == "invert";
        var hasItems = value switch
        {
            int count => count > 0,
            ICollection collection => collection.Count > 0,
            IEnumerable enumerable => enumerable.Cast<object?>().Any(),
            _ => false
        };

        if (invert)
        {
            return hasItems ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
        }

        return hasItems ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class FormatTimeSpanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is TimeSpan timeSpan)
        {
            if (timeSpan.TotalHours >= 1)
            {
                return $"{(int)timeSpan.TotalHours}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
            }
            return $"{(int)timeSpan.TotalMinutes}:{timeSpan.Seconds:D2}";
        }
        return "0:00";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class FormatNumberConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is long num)
        {
            if (num >= 1_000_000)
                return $"{num / 1_000_000.0:F1}M";
            if (num >= 1_000)
                return $"{num / 1_000.0:F1}K";
            return num.ToString();
        }
        return "0";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class ImageSourceConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string url && !string.IsNullOrEmpty(url))
        {
            return url;
        }
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
