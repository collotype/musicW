using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MusicApp.Converters;

public class PageTypeMatchConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string pageType && parameter is string matchType)
        {
            return pageType.Equals(matchType, StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class StringMatchConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string stringValue && parameter is string matchValue)
        {
            return stringValue.Equals(matchValue, StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class EnumMatchConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var isMatch = value != null && parameter != null &&
                      (value.ToString()?.Equals(parameter?.ToString(), StringComparison.OrdinalIgnoreCase) ?? false);

        if (targetType == typeof(Visibility))
        {
            return isMatch ? Visibility.Visible : Visibility.Collapsed;
        }

        return isMatch;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isChecked && isChecked && parameter != null)
        {
            var enumType = Nullable.GetUnderlyingType(targetType) ?? targetType;
            var enumValue = parameter.ToString();

            if (enumType.IsEnum && !string.IsNullOrWhiteSpace(enumValue) &&
                System.Enum.TryParse(enumType, enumValue, ignoreCase: true, out var parsedValue))
            {
                return parsedValue;
            }

            return Binding.DoNothing;
        }
        return Binding.DoNothing;
    }
}

public class VolumeIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isMuted && isMuted)
        {
            return "🔇";
        }
        return "🔊";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class DoubleToPercentConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double doubleValue)
        {
            return doubleValue * 100;
        }
        return 0.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double doubleValue)
        {
            return doubleValue / 100;
        }
        return 0.0;
    }
}
